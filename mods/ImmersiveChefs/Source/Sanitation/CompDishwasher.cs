using RimWorld;
using Verse;

namespace ImmersiveChefs;

public sealed class CompProperties_Dishwasher : CompProperties
{
    public CompProperties_Dishwasher()
    {
        compClass = typeof(CompDishwasher);
    }

    public float basePlateCapacity = 16f;
    public int baseCycleTicks = 2500;
    public int baseLoadingTicks = 1000;
    public bool requiresDubsWater = true;
    public float waterPerPlateEquivalent = 0.1f;
}

public static class DishwasherCapacityPolicy
{
    public static float ScaleForRestart(float baseCapacity, float scale)
    {
        var boundedScale = Math.Max(0.5f, Math.Min(4f, scale));
        return Math.Max(0.01f, baseCapacity) * boundedScale;
    }

    public static int CountAccepted(float capacity, float used, float perItem, int stackCount)
    {
        var boundedPerItem = Math.Max(0.01f, perItem);
        var free = Math.Max(0f, capacity - used);
        return Math.Max(0, Math.Min(stackCount, (int)Math.Floor((free + 0.001f) / boundedPerItem)));
    }

    public static float ProcessorCapacityFactor(float configuredPlateEquivalent)
    {
        return Math.Max(0.01f, configuredPlateEquivalent);
    }
}

public static class DishwasherCyclePolicy
{
    public static bool CanAcceptAdditionalWare(
        int progressTicks,
        bool waterDebitedForCycle,
        bool batchCaptured = false)
    {
        return progressTicks == 0 && !waterDebitedForCycle && !batchCaptured;
    }

    public static int ResetLoadingWindow(int baseLoadingTicks)
    {
        return Math.Max(0, baseLoadingTicks);
    }

    public static int AdvanceLoadingWindow(int remainingTicks, int elapsedTicks)
    {
        return Math.Max(0, remainingTicks - Math.Max(0, elapsedTicks));
    }

    public static bool ShouldRequestWater(
        bool hasContents,
        int loadingTicksRemaining,
        bool batchCaptured,
        bool waterDebited)
    {
        return hasContents && loadingTicksRemaining <= 0 && batchCaptured && !waterDebited;
    }

    public static float CaptureWaterCharge(
        float finalPlateEquivalentLoad,
        float waterPerPlateEquivalent)
    {
        return Math.Max(0.001f,
            Math.Max(0f, finalPlateEquivalentLoad) * Math.Max(0f, waterPerPlateEquivalent));
    }

    public static bool CanContinueWithWater(
        bool requiresDubsWater,
        bool waterDebited,
        bool hasSuppliedConnection,
        bool residualSupplyRequired,
        bool hasResidualSupply)
    {
        return !requiresDubsWater ||
               (waterDebited &&
                hasSuppliedConnection &&
                (!residualSupplyRequired || hasResidualSupply));
    }
}

public sealed class CompDishwasher : ThingComp, IThingHolder
{
    private ThingOwner<Thing>? contents;
    private int progressTicks;
    private int capturedCycleTicks;
    private int loadingTicksRemaining;
    private bool batchCaptured;
    private bool waterDebitedForCycle;
    private bool residualWaterSupplyRequiredForCycle;
    private float capturedWaterCharge;
    private string pauseReason = string.Empty;
    private IntVec3 lastKnownPosition = IntVec3.Invalid;

    private ThingOwner<Thing> Contents => contents ??= new ThingOwner<Thing>(this, LookMode.Deep);

    public float Capacity => Props.basePlateCapacity;

    public float UsedCapacity => Contents.InnerListForReading.Sum(PlateEquivalents);

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        lastKnownPosition = parent.Position;
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        if (parent.Position.IsValid)
        {
            lastKnownPosition = parent.Position;
        }

        base.PostDeSpawn(map, mode);
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        if (Contents.Any && previousMap is not null)
        {
            EjectAll(previousMap, lastKnownPosition.IsValid ? lastKnownPosition : parent.Position);
        }

        base.PostDestroy(mode, previousMap);
    }

    public bool CanAccept(Thing thing)
    {
        if (ProcessorFrameworkAdapter.Controls(parent))
        {
            return false;
        }

        return thing is ThingWithComps withComps &&
               withComps.GetComp<CompSanitation>()?.IsDirty == true &&
               parent.TryGetComp<CompPowerTrader>()?.PowerOn != false &&
               DishwasherCyclePolicy.CanAcceptAdditionalWare(
                   progressTicks,
                   waterDebitedForCycle,
                   batchCaptured) &&
               CountCanAccept(thing) > 0;
    }

    public int CountCanAccept(Thing thing)
    {
        return DishwasherCapacityPolicy.CountAccepted(
            Capacity,
            UsedCapacity,
            PlateEquivalentsPerItem(thing),
            thing.stackCount);
    }

    public bool TryAcceptFrom(Pawn pawn)
    {
        var carried = pawn.carryTracker.CarriedThing;
        if (carried is null || !CanAccept(carried))
        {
            return false;
        }

        var count = CountCanAccept(carried);
        if (count <= 0)
        {
            return false;
        }

        var transferred = pawn.carryTracker.innerContainer.TryTransferToContainer(
            carried,
            Contents,
            count);
        if (transferred > 0)
        {
            if (capturedCycleTicks <= 0)
            {
                capturedCycleTicks = Math.Max(1, (int)Math.Round(
                    Props.baseCycleTicks * ImmersiveChefsMod.Settings.DishwashingWorkScale));
            }

            loadingTicksRemaining = DishwasherCyclePolicy.ResetLoadingWindow(Props.baseLoadingTicks);
        }

        return transferred > 0;
    }

    internal bool TryAcceptTrackedWare(Pawn pawn, Thing ware)
    {
        var inventory = pawn.inventory?.innerContainer;
        if (inventory is null || !ReferenceEquals(ware.holdingOwner, inventory) || !CanAccept(ware) ||
            !PickUpAndHaulAdapter.TryResolveTrackedItems(pawn, ware, out var tracked, out _))
        {
            return false;
        }

        if (!PickUpAndHaulAdapter.TryRemoveResolvedTrackedItem(tracked!, ware, out _))
        {
            return false;
        }

        var transferred = inventory.TryTransferToContainer(ware, Contents, 1);
        if (transferred <= 0)
        {
            if (!PickUpAndHaulAdapter.TryRegister(pawn, ware, out _) && pawn.MapHeld is { } map)
            {
                inventory.TryDrop(ware, pawn.PositionHeld, map, ThingPlaceMode.Near, out _);
            }
            return false;
        }

        if (capturedCycleTicks <= 0)
        {
            capturedCycleTicks = Math.Max(1, (int)Math.Round(
                Props.baseCycleTicks * ImmersiveChefsMod.Settings.DishwashingWorkScale));
        }

        loadingTicksRemaining = DishwasherCyclePolicy.ResetLoadingWindow(Props.baseLoadingTicks);
        return true;
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (parent.Spawned)
        {
            lastKnownPosition = parent.Position;
        }
        if (ProcessorFrameworkAdapter.Controls(parent))
        {
            TickProcessorCycle();
            return;
        }
        if (!Contents.Any)
        {
            ResetCycleState();
            return;
        }

        if (loadingTicksRemaining > 0)
        {
            loadingTicksRemaining = DishwasherCyclePolicy.AdvanceLoadingWindow(
                loadingTicksRemaining,
                250);
            pauseReason = string.Empty;
            if (loadingTicksRemaining > 0)
            {
                return;
            }
        }

        if (!batchCaptured)
        {
            batchCaptured = true;
            capturedWaterCharge = DishwasherCyclePolicy.CaptureWaterCharge(
                UsedCapacity,
                Props.waterPerPlateEquivalent);
        }

        if (!CanProgress(out pauseReason))
        {
            return;
        }

        progressTicks += 250;
        if (progressTicks < Math.Max(1, capturedCycleTicks))
        {
            return;
        }

        foreach (var thing in Contents.InnerListForReading)
        {
            (thing as ThingWithComps)?.GetComp<CompSanitation>()?.MarkClean(WashProvenance.Safe);
        }

        EjectAll();
    }

    public override string CompInspectStringExtra()
    {
        if (ProcessorFrameworkAdapter.Controls(parent))
        {
            var processorLoad = ProcessorFrameworkAdapter.UsedPlateEquivalentCapacity(parent);
            var processorStatus = processorLoad <= 0f
                ? "ImmersiveChefs_Dishwasher_StatusIdle".Translate()
                : loadingTicksRemaining > 0
                    ? "ImmersiveChefs_Dishwasher_StatusLoading".Translate()
                    : !string.IsNullOrEmpty(pauseReason)
                        ? "ImmersiveChefs_Dishwasher_StatusPaused".Translate(TranslatePauseReason(pauseReason))
                        : "ImmersiveChefs_Dishwasher_StatusWashing".Translate(
                            ProcessorFrameworkAdapter.ProgressPercent(parent).ToString("0"));
            return "ImmersiveChefs_Dishwasher_Inspect".Translate(
                processorStatus,
                processorLoad.ToString("0.##"),
                Capacity.ToString("0.##"));
        }

        var status = !Contents.Any
            ? "ImmersiveChefs_Dishwasher_StatusIdle".Translate()
            : loadingTicksRemaining > 0
                ? "ImmersiveChefs_Dishwasher_StatusLoading".Translate()
            : string.IsNullOrEmpty(pauseReason)
                ? "ImmersiveChefs_Dishwasher_StatusWashing".Translate(
                    Math.Min(100, (int)(100f * progressTicks / Math.Max(1, capturedCycleTicks))))
                : "ImmersiveChefs_Dishwasher_StatusPaused".Translate(TranslatePauseReason(pauseReason));
        return "ImmersiveChefs_Dishwasher_Inspect".Translate(
            status,
            UsedCapacity.ToString("0.##"),
            Capacity.ToString("0.##"));
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (ProcessorFrameworkAdapter.Controls(parent))
        {
            if (ProcessorFrameworkAdapter.HasContents(parent))
            {
                yield return new Command_Action
                {
                    defaultLabel = "ImmersiveChefs_Dishwasher_EjectLabel".Translate(),
                    defaultDesc = "ImmersiveChefs_Dishwasher_EjectDescription".Translate(),
                    action = () => ProcessorFrameworkAdapter.EjectAllDirty(parent)
                };
            }

            yield break;
        }

        if (!Contents.Any)
        {
            yield break;
        }

        yield return new Command_Action
        {
            defaultLabel = "ImmersiveChefs_Dishwasher_EjectLabel".Translate(),
            defaultDesc = "ImmersiveChefs_Dishwasher_EjectDescription".Translate(),
            action = EjectAll
        };
    }

    private static string TranslatePauseReason(string reason)
    {
        return reason == "no power"
            ? "ImmersiveChefs_Dishwasher_PauseNoPower".Translate()
            : "ImmersiveChefs_Dishwasher_PauseNoWater".Translate();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref contents, "contents", this);
        Scribe_Values.Look(ref progressTicks, "progressTicks", 0);
        Scribe_Values.Look(ref capturedCycleTicks, "capturedCycleTicks", 0);
        Scribe_Values.Look(ref loadingTicksRemaining, "loadingTicksRemaining", 0);
        Scribe_Values.Look(ref batchCaptured, "batchCaptured", false);
        Scribe_Values.Look(ref waterDebitedForCycle, "waterDebitedForCycle", false);
        Scribe_Values.Look(
            ref residualWaterSupplyRequiredForCycle,
            "residualWaterSupplyRequiredForCycle",
            false);
        Scribe_Values.Look(ref capturedWaterCharge, "capturedWaterCharge", 0f);
        Scribe_Values.Look(ref pauseReason, "pauseReason", string.Empty);
        Scribe_Values.Look(ref lastKnownPosition, "lastKnownPosition", IntVec3.Invalid);
        contents ??= new ThingOwner<Thing>(this, LookMode.Deep);
    }

    public ThingOwner GetDirectlyHeldThings() => Contents;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    IThingHolder? IThingHolder.ParentHolder => parent.ParentHolder;

    private CompProperties_Dishwasher Props => (CompProperties_Dishwasher)props;

    internal bool CanAcceptProcessorWare(bool hasProcessorContents)
    {
        if (parent.TryGetComp<CompPowerTrader>()?.PowerOn == false)
        {
            return false;
        }

        return !hasProcessorContents ||
               (loadingTicksRemaining > 0 && DishwasherCyclePolicy.CanAcceptAdditionalWare(
                   progressTicks,
                   waterDebitedForCycle,
                   batchCaptured));
    }

    internal void NotifyProcessorAdmission(bool startedNewBatch)
    {
        if (startedNewBatch)
        {
            progressTicks = 0;
            capturedCycleTicks = 0;
            batchCaptured = false;
            waterDebitedForCycle = false;
            residualWaterSupplyRequiredForCycle = false;
            capturedWaterCharge = 0f;
        }

        loadingTicksRemaining = DishwasherCyclePolicy.ResetLoadingWindow(Props.baseLoadingTicks);
        pauseReason = string.Empty;
    }

    internal bool ProcessorCycleCanProgress()
    {
        if (!batchCaptured || loadingTicksRemaining > 0)
        {
            return false;
        }

        if (parent.TryGetComp<CompPowerTrader>()?.PowerOn == false)
        {
            return false;
        }

        return DishwasherCyclePolicy.CanContinueWithWater(
            RequiresDubsWater,
            waterDebitedForCycle,
            DubsWaterAdapter.HasSuppliedConnection(parent),
            residualWaterSupplyRequiredForCycle,
            DubsWaterAdapter.CanSupplyCycleWater(parent, 0.001f));
    }

    internal void NotifyProcessorEmptied()
    {
        ResetCycleState();
    }

    private bool CanProgress(out string reason)
    {
        var power = parent.GetComp<CompPowerTrader>();
        if (power is not null && !power.PowerOn)
        {
            reason = "no power";
            return false;
        }

        if (Props.requiresDubsWater &&
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.DubsBadHygiene) &&
            !waterDebitedForCycle)
        {
            if (!DubsWaterAdapter.TryConsumeCycleWater(parent, capturedWaterCharge, out reason))
            {
                return false;
            }

            waterDebitedForCycle = true;
            residualWaterSupplyRequiredForCycle =
                DubsWaterAdapter.CanSupplyCycleWater(parent, 0.001f);
        }


        if (!DishwasherCyclePolicy.CanContinueWithWater(
                RequiresDubsWater,
                waterDebitedForCycle,
                DubsWaterAdapter.HasSuppliedConnection(parent),
                residualWaterSupplyRequiredForCycle,
                DubsWaterAdapter.CanSupplyCycleWater(parent, 0.001f)))
        {
            reason = "water supply unavailable";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private bool RequiresDubsWater =>
        Props.requiresDubsWater &&
        ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.DubsBadHygiene);

    private void TickProcessorCycle()
    {
        var usedCapacity = ProcessorFrameworkAdapter.UsedPlateEquivalentCapacity(parent);
        if (usedCapacity <= 0f)
        {
            ResetCycleState();
            return;
        }

        if (loadingTicksRemaining > 0)
        {
            loadingTicksRemaining = DishwasherCyclePolicy.AdvanceLoadingWindow(
                loadingTicksRemaining,
                250);
            pauseReason = string.Empty;
            if (loadingTicksRemaining > 0)
            {
                return;
            }
        }

        if (!batchCaptured)
        {
            batchCaptured = true;
            capturedWaterCharge = DishwasherCyclePolicy.CaptureWaterCharge(
                usedCapacity,
                Props.waterPerPlateEquivalent);
        }

        if (parent.TryGetComp<CompPowerTrader>()?.PowerOn == false)
        {
            pauseReason = "no power";
            return;
        }

        if (RequiresDubsWater && DishwasherCyclePolicy.ShouldRequestWater(
                hasContents: true,
                loadingTicksRemaining: loadingTicksRemaining,
                batchCaptured: batchCaptured,
                waterDebited: waterDebitedForCycle))
        {
            if (!DubsWaterAdapter.TryConsumeCycleWater(parent, capturedWaterCharge, out pauseReason))
            {
                return;
            }

            waterDebitedForCycle = true;
            residualWaterSupplyRequiredForCycle =
                DubsWaterAdapter.CanSupplyCycleWater(parent, 0.001f);
        }

        if (!DishwasherCyclePolicy.CanContinueWithWater(
                RequiresDubsWater,
                waterDebitedForCycle,
                DubsWaterAdapter.HasSuppliedConnection(parent),
                residualWaterSupplyRequiredForCycle,
                DubsWaterAdapter.CanSupplyCycleWater(parent, 0.001f)))
        {
            pauseReason = "water supply unavailable";
            return;
        }

        pauseReason = string.Empty;
    }

    private void EjectAll()
    {
        if (parent.Map is not { } map)
        {
            return;
        }

        EjectAll(map, parent.InteractionCell);
    }

    private void EjectAll(Map map, IntVec3 position)
    {
        var dropPosition = position.IsValid && position.InBounds(map) ? position : map.Center;
        Contents.TryDropAll(dropPosition, map, ThingPlaceMode.Near);
        ResetCycleState();
    }

    private void ResetCycleState()
    {
        progressTicks = 0;
        capturedCycleTicks = 0;
        loadingTicksRemaining = 0;
        batchCaptured = false;
        waterDebitedForCycle = false;
        residualWaterSupplyRequiredForCycle = false;
        capturedWaterCharge = 0f;
        pauseReason = string.Empty;
    }

    private static float PlateEquivalents(Thing thing) =>
        PlateEquivalentsPerItem(thing) * thing.stackCount;

    private static float PlateEquivalentsPerItem(Thing thing) =>
        thing.def.GetModExtension<KitchenwareExtension>()?.plateEquivalent ?? 1f;
}
