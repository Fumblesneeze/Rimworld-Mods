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
    public static int CaptureLoadDuration(int baseCycleTicks, float workScale)
    {
        return Math.Max(1, (int)Math.Round(
            Math.Max(1, baseCycleTicks) * Math.Max(0.01f, workScale)));
    }

    public static int AdvanceLoad(int progressTicks, int elapsedTicks, int capturedDurationTicks)
    {
        return Math.Min(
            Math.Max(1, capturedDurationTicks),
            Math.Max(0, progressTicks) + Math.Max(0, elapsedTicks));
    }

    public static float AdmissionWaterCharge(
        float admittedPlateEquivalentLoad,
        float waterPerPlateEquivalent)
    {
        return Math.Max(0.001f,
            Math.Max(0f, admittedPlateEquivalentLoad) * Math.Max(0f, waterPerPlateEquivalent));
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

public enum DishwasherUtilityBlocker
{
    None,
    NoPower,
    NoWater
}

public static class DishwasherUtilityPolicy
{
    public static bool HasActivePower(
        bool powerOn,
        float currentEnergyGainRate,
        float currentStoredEnergy)
    {
        return HasActivePower(
            powerOn,
            currentEnergyGainRate,
            currentStoredEnergy,
            hasUnpoweredDesiredLoad: false);
    }

    public static bool HasActivePower(
        bool powerOn,
        float currentEnergyGainRate,
        float currentStoredEnergy,
        bool hasUnpoweredDesiredLoad)
    {
        if (!powerOn)
        {
            return false;
        }

        if (currentStoredEnergy > 0.0001f)
        {
            return true;
        }

        return currentEnergyGainRate >= -0.0001f && !hasUnpoweredDesiredLoad;
    }

    public static DishwasherUtilityBlocker Resolve(
        bool powerOn,
        bool activePowerAvailable,
        bool requiresDubsWater,
        bool waterDebited,
        bool hasSuppliedConnection,
        bool hasCycleWater)
    {
        if (!powerOn || !activePowerAvailable)
        {
            return DishwasherUtilityBlocker.NoPower;
        }

        if (requiresDubsWater &&
            (!hasSuppliedConnection || (!waterDebited && !hasCycleWater)))
        {
            return DishwasherUtilityBlocker.NoWater;
        }

        return DishwasherUtilityBlocker.None;
    }
}

public static class DishwasherInspectPolicy
{
    public static bool ShouldSuppressOptionalDiagnostic(
        string? parentDefName,
        string? assemblyName,
        string? compTypeName)
    {
        return parentDefName is "ImmersiveChefs_Dishwasher" or "ImmersiveChefs_IndustrialDishwasher" &&
               assemblyName?.IndexOf("BadHygiene", StringComparison.OrdinalIgnoreCase) >= 0 &&
               compTypeName?.StartsWith("DubsBadHygiene.", StringComparison.Ordinal) == true;
    }
}

public sealed class CompDishwasher : ThingComp, IThingHolder
{
    private ThingOwner<Thing>? contents;
    private List<Thing> localLoadThings = new();
    private List<int> localLoadProgressTicks = new();
    private List<int> localLoadDurationTicks = new();

    // Kept as read-compatible save fields so an in-progress sealed batch from an older build
    // can be migrated into independently timed local loads without losing its progress.
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
               CountCanAccept(thing) > 0;
    }

    public int CountCanAccept(Thing thing)
    {
        if (ProcessorFrameworkAdapter.Controls(parent) || !HasActivePower())
        {
            return 0;
        }

        var capacityCount = DishwasherCapacityPolicy.CountAccepted(
            Capacity,
            UsedCapacity,
            PlateEquivalentsPerItem(thing),
            thing.stackCount);
        return CountSupportedByAdmissionWater(thing, capacityCount);
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
            count,
            out Thing transferredThing,
            canMergeWithExistingStacks: false);
        if (transferred <= 0)
        {
            return false;
        }

        if (!TryCommitAdmissionWater(PlateEquivalents(transferredThing), out _))
        {
            if (!TryReturnRejectedAdmission(
                transferredThing,
                pawn.carryTracker.innerContainer,
                pawn,
                allowMerge: true,
                out _))
            {
                Log.ErrorOnce(
                    "[ImmersiveChefs] A rejected dishwasher admission could not be returned to its pawn or map.",
                    194820731);
            }
            return false;
        }

        AddLocalLoad(transferredThing);
        return true;
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

        var transferred = inventory.TryTransferToContainer(
            ware,
            Contents,
            1,
            out Thing transferredThing,
            canMergeWithExistingStacks: false);
        if (transferred <= 0)
        {
            if (!PickUpAndHaulAdapter.TryRegister(pawn, ware, out _) && pawn.MapHeld is { } map)
            {
                inventory.TryDrop(ware, pawn.PositionHeld, map, ThingPlaceMode.Near, out _);
            }
            return false;
        }

        if (!TryCommitAdmissionWater(PlateEquivalents(transferredThing), out _))
        {
            if (!TryReturnRejectedAdmission(
                transferredThing,
                inventory,
                pawn,
                allowMerge: false,
                out var returned))
            {
                Log.ErrorOnce(
                    "[ImmersiveChefs] A rejected tracked dishwasher admission could not be returned to its pawn or map.",
                    194820732);
            }
            else if (returned is not null && ReferenceEquals(returned.holdingOwner, inventory) &&
                     !PickUpAndHaulAdapter.TryRegister(pawn, returned, out _) &&
                     pawn.MapHeld is { } map)
            {
                DiningWarePlacementRuntime.DropAt(returned, pawn.PositionHeld, map);
            }
            return false;
        }

        AddLocalLoad(transferredThing);
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

        EnsureLocalLoadRecords();

        if (!CanProgress(out pauseReason))
        {
            return;
        }

        for (var index = localLoadThings.Count - 1; index >= 0; index--)
        {
            var ware = localLoadThings[index];
            if (ware is null || ware.Destroyed || !ReferenceEquals(ware.holdingOwner, Contents))
            {
                RemoveLocalLoadAt(index);
                continue;
            }

            localLoadProgressTicks[index] = DishwasherCyclePolicy.AdvanceLoad(
                localLoadProgressTicks[index],
                250,
                localLoadDurationTicks[index]);
            if (localLoadProgressTicks[index] < localLoadDurationTicks[index])
            {
                continue;
            }

            (ware as ThingWithComps)?.GetComp<CompSanitation>()?.MarkClean(WashProvenance.Safe);
            if (parent.Map is { } map && Contents.TryDrop(
                    ware,
                    parent.InteractionCell,
                    map,
                    ThingPlaceMode.Near,
                    out _))
            {
                RemoveLocalLoadAt(index);
            }
        }

        if (!Contents.Any)
        {
            ResetCycleState();
        }
    }

    public override string CompInspectStringExtra()
    {
        var utilityBlocker = InspectUtilityBlocker();
        if (ProcessorFrameworkAdapter.Controls(parent))
        {
            var processorLoad = ProcessorFrameworkAdapter.UsedPlateEquivalentCapacity(parent);
            var processorStatus = utilityBlocker != DishwasherUtilityBlocker.None
                ? TranslateUtilityBlocker(utilityBlocker)
                : processorLoad <= 0f
                ? "ImmersiveChefs_Dishwasher_StatusIdle".Translate()
                : !string.IsNullOrEmpty(pauseReason)
                    ? "ImmersiveChefs_Dishwasher_StatusPaused".Translate(TranslatePauseReason(pauseReason))
                    : "ImmersiveChefs_Dishwasher_StatusWashing".Translate(
                        ProcessorFrameworkAdapter.ProgressPercent(parent).ToString("0"));
            return "ImmersiveChefs_Dishwasher_Inspect".Translate(
                processorStatus,
                processorLoad.ToString("0.##"),
                Capacity.ToString("0.##"));
        }

        var status = utilityBlocker != DishwasherUtilityBlocker.None
            ? TranslateUtilityBlocker(utilityBlocker)
            : !Contents.Any
            ? "ImmersiveChefs_Dishwasher_StatusIdle".Translate()
            : string.IsNullOrEmpty(pauseReason)
                ? "ImmersiveChefs_Dishwasher_StatusWashing".Translate(
                    LocalProgressPercent().ToString("0"))
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

    private static TaggedString TranslateUtilityBlocker(DishwasherUtilityBlocker blocker)
    {
        return blocker == DishwasherUtilityBlocker.NoPower
            ? "ImmersiveChefs_Dishwasher_PauseNoPower".Translate()
            : "ImmersiveChefs_Dishwasher_PauseNoWater".Translate();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref contents, "contents", this);
        Scribe_Collections.Look(ref localLoadThings, "localLoadThings", LookMode.Reference);
        Scribe_Collections.Look(ref localLoadProgressTicks, "localLoadProgressTicks", LookMode.Value);
        Scribe_Collections.Look(ref localLoadDurationTicks, "localLoadDurationTicks", LookMode.Value);
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
        localLoadThings ??= new List<Thing>();
        localLoadProgressTicks ??= new List<int>();
        localLoadDurationTicks ??= new List<int>();
        if (Scribe.mode == LoadSaveMode.PostLoadInit && !ProcessorFrameworkAdapter.Controls(parent))
        {
            EnsureLocalLoadRecords();
        }
    }

    public ThingOwner GetDirectlyHeldThings() => Contents;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    IThingHolder? IThingHolder.ParentHolder => parent.ParentHolder;

    private CompProperties_Dishwasher Props => (CompProperties_Dishwasher)props;

    internal bool CanAcceptProcessorWare(Thing ware, int requestedCount = 1)
    {
        return CountCanAcceptProcessorWare(ware, requestedCount) > 0;
    }

    internal int CountCanAcceptProcessorWare(Thing ware, int requestedCount)
    {
        if (ware is null || requestedCount <= 0 || !HasActivePower())
        {
            return 0;
        }

        return CountCanAcceptProcessorWare(
            PlateEquivalentsPerItem(ware),
            Math.Min(requestedCount, ware.stackCount));
    }

    internal int CountCanAcceptProcessorWare(float plateEquivalentsPerItem, int requestedCount)
    {
        if (requestedCount <= 0 || !HasActivePower())
        {
            return 0;
        }

        return CountSupportedByAdmissionWater(plateEquivalentsPerItem, requestedCount);
    }

    internal bool TryCommitProcessorAdmission(float admittedPlateEquivalents, out string reason)
    {
        return TryCommitAdmissionWater(admittedPlateEquivalents, out reason);
    }

    internal bool ProcessorCycleCanProgress()
    {
        var blocker = CurrentUtilityBlocker(
            cycleWaterAvailable: !RequiresDubsWater ||
                                 waterDebitedForCycle ||
                                 DubsWaterAdapter.CanSupplyCycleWater(parent, 0.001f));
        if (blocker != DishwasherUtilityBlocker.None)
        {
            pauseReason = blocker == DishwasherUtilityBlocker.NoPower
                ? "no power"
                : "water supply unavailable";
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
        if (!EnsureLegacyWaterDebit(out reason))
        {
            return false;
        }

        var blocker = CurrentUtilityBlocker(
            cycleWaterAvailable: !RequiresDubsWater ||
                                 waterDebitedForCycle ||
                                 DubsWaterAdapter.CanSupplyCycleWater(parent, 0.001f));
        if (blocker == DishwasherUtilityBlocker.NoPower)
        {
            reason = "no power";
            return false;
        }

        if (blocker == DishwasherUtilityBlocker.NoWater)
        {
            reason = "water supply unavailable";
            return false;
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

        if (!HasActivePower())
        {
            pauseReason = "no power";
            return;
        }

        if (!EnsureLegacyWaterDebit(out pauseReason))
        {
            return;
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

    private DishwasherUtilityBlocker InspectUtilityBlocker()
    {
        return CurrentUtilityBlocker(
            cycleWaterAvailable: !RequiresDubsWater ||
                                 waterDebitedForCycle ||
                                 DubsWaterAdapter.CanSupplyCycleWater(parent, 0.001f));
    }

    private DishwasherUtilityBlocker CurrentUtilityBlocker(bool cycleWaterAvailable)
    {
        var power = parent.TryGetComp<CompPowerTrader>();
        return DishwasherUtilityPolicy.Resolve(
            powerOn: power?.PowerOn == true,
            activePowerAvailable: HasActivePower(power),
            requiresDubsWater: RequiresDubsWater,
            waterDebited: waterDebitedForCycle,
            hasSuppliedConnection: !RequiresDubsWater || DubsWaterAdapter.HasSuppliedConnection(parent),
            hasCycleWater: cycleWaterAvailable);
    }

    private bool HasActivePower()
    {
        return !parent.IsBrokenDown() &&
               FlickUtility.WantsToBeOn(parent) &&
               HasActivePower(parent.TryGetComp<CompPowerTrader>());
    }

    private static bool HasActivePower(CompPowerTrader? power)
    {
        var net = power?.PowerNet;
        return power is not null && net is not null && DishwasherUtilityPolicy.HasActivePower(
            power.PowerOn,
            net.CurrentEnergyGainRate(),
            net.CurrentStoredEnergy(),
            HasUnpoweredDesiredLoad(net, power));
    }

    private static bool HasUnpoweredDesiredLoad(PowerNet net, CompPowerTrader dishwasherPower)
    {
        foreach (var candidate in net.powerComps)
        {
            if (ReferenceEquals(candidate, dishwasherPower) ||
                candidate.PowerOn ||
                candidate.Props.PowerConsumption <= 0f ||
                !candidate.parent.Spawned ||
                candidate.parent.IsBrokenDown() ||
                !FlickUtility.WantsToBeOn(candidate.parent))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private int CountSupportedByAdmissionWater(Thing ware, int capacityCount)
    {
        return CountSupportedByAdmissionWater(PlateEquivalentsPerItem(ware), capacityCount);
    }

    private int CountSupportedByAdmissionWater(float plateEquivalentsPerItem, int capacityCount)
    {
        if (capacityCount <= 0)
        {
            return 0;
        }

        if (!RequiresDubsWater)
        {
            return capacityCount;
        }

        if (!DubsWaterAdapter.HasSuppliedConnection(parent))
        {
            return 0;
        }

        for (var count = capacityCount; count > 0; count--)
        {
            var charge = DishwasherCyclePolicy.AdmissionWaterCharge(
                plateEquivalentsPerItem * count,
                Props.waterPerPlateEquivalent);
            if (DubsWaterAdapter.CanSupplyCycleWater(parent, charge))
            {
                return count;
            }
        }

        return 0;
    }

    private bool TryReturnRejectedAdmission(
        Thing ware,
        ThingOwner destination,
        Pawn pawn,
        bool allowMerge,
        out Thing? returned)
    {
        var expectedCount = ware.stackCount;
        var restoredCount = Contents.TryTransferToContainer(
            ware,
            destination,
            expectedCount,
            out var restored,
            canMergeWithExistingStacks: allowMerge);
        returned = restoredCount > 0 ? restored : null;
        if (restoredCount >= expectedCount)
        {
            return true;
        }

        if (ware.Destroyed || !ReferenceEquals(ware.holdingOwner, Contents))
        {
            return restoredCount == expectedCount;
        }

        var map = pawn.MapHeld ?? parent.MapHeld;
        var position = pawn.PositionHeld.IsValid ? pawn.PositionHeld : parent.InteractionCell;
        if (map is null || !position.InBounds(map))
        {
            return false;
        }

        returned = DiningWarePlacementRuntime.DropAt(ware, position, map);
        if (returned is not null)
        {
            return true;
        }

        Contents.Remove(ware);
        returned = GenSpawn.Spawn(ware, position, map);
        return returned.Spawned;
    }

    private bool TryCommitAdmissionWater(float admittedPlateEquivalents, out string reason)
    {
        reason = string.Empty;
        if (RequiresDubsWater)
        {
            var charge = DishwasherCyclePolicy.AdmissionWaterCharge(
                admittedPlateEquivalents,
                Props.waterPerPlateEquivalent);
            if (!DubsWaterAdapter.HasSuppliedConnection(parent) ||
                !DubsWaterAdapter.CanSupplyCycleWater(parent, charge) ||
                !DubsWaterAdapter.TryConsumeCycleWater(parent, charge, out reason))
            {
                reason = string.IsNullOrEmpty(reason) ? "water supply unavailable" : reason;
                return false;
            }

            // Preserve the established interruption rule when residual supply existed after this
            // admission, while still allowing an exactly sufficient final admission to reach zero.
            residualWaterSupplyRequiredForCycle =
                DubsWaterAdapter.CanSupplyCycleWater(parent, 0.001f);
        }

        waterDebitedForCycle = true;
        pauseReason = string.Empty;
        reason = string.Empty;
        return true;
    }

    private bool EnsureLegacyWaterDebit(out string reason)
    {
        if (waterDebitedForCycle || !RequiresDubsWater)
        {
            waterDebitedForCycle = true;
            reason = string.Empty;
            return true;
        }

        var legacyLoad = ProcessorFrameworkAdapter.Controls(parent)
            ? ProcessorFrameworkAdapter.UsedPlateEquivalentCapacity(parent)
            : UsedCapacity;
        if (legacyLoad <= 0f)
        {
            reason = string.Empty;
            return true;
        }

        return TryCommitAdmissionWater(legacyLoad, out reason);
    }

    private void AddLocalLoad(Thing ware)
    {
        if (localLoadThings.Any(candidate => ReferenceEquals(candidate, ware)))
        {
            return;
        }

        localLoadThings.Add(ware);
        localLoadProgressTicks.Add(0);
        localLoadDurationTicks.Add(DishwasherCyclePolicy.CaptureLoadDuration(
            Props.baseCycleTicks,
            ImmersiveChefsMod.Settings.DishwashingWorkScale));
        ClearLegacyBatchMarkers();
    }

    private void EnsureLocalLoadRecords()
    {
        var alignedCount = Math.Min(
            localLoadThings.Count,
            Math.Min(localLoadProgressTicks.Count, localLoadDurationTicks.Count));
        while (localLoadThings.Count > alignedCount)
        {
            localLoadThings.RemoveAt(localLoadThings.Count - 1);
        }
        while (localLoadProgressTicks.Count > alignedCount)
        {
            localLoadProgressTicks.RemoveAt(localLoadProgressTicks.Count - 1);
        }
        while (localLoadDurationTicks.Count > alignedCount)
        {
            localLoadDurationTicks.RemoveAt(localLoadDurationTicks.Count - 1);
        }

        for (var index = localLoadThings.Count - 1; index >= 0; index--)
        {
            var ware = localLoadThings[index];
            if (ware is null || ware.Destroyed || !ReferenceEquals(ware.holdingOwner, Contents))
            {
                RemoveLocalLoadAt(index);
                continue;
            }

            localLoadDurationTicks[index] = Math.Max(1, localLoadDurationTicks[index]);
            localLoadProgressTicks[index] = Math.Max(
                0,
                Math.Min(localLoadProgressTicks[index], localLoadDurationTicks[index]));
        }

        var migratedDuration = capturedCycleTicks > 0
            ? capturedCycleTicks
            : DishwasherCyclePolicy.CaptureLoadDuration(
                Props.baseCycleTicks,
                ImmersiveChefsMod.Settings.DishwashingWorkScale);
        foreach (var ware in Contents.InnerListForReading)
        {
            if (localLoadThings.Any(candidate => ReferenceEquals(candidate, ware)))
            {
                continue;
            }

            localLoadThings.Add(ware);
            localLoadDurationTicks.Add(Math.Max(1, migratedDuration));
            localLoadProgressTicks.Add(Math.Max(
                0,
                Math.Min(progressTicks, Math.Max(1, migratedDuration))));
        }

        ClearLegacyBatchMarkers();
    }

    private float LocalProgressPercent()
    {
        EnsureLocalLoadRecords();
        var maximum = 0f;
        for (var index = 0; index < localLoadThings.Count; index++)
        {
            maximum = Math.Max(
                maximum,
                100f * localLoadProgressTicks[index] / Math.Max(1, localLoadDurationTicks[index]));
        }

        return Math.Min(100f, maximum);
    }

    private void RemoveLocalLoadAt(int index)
    {
        localLoadThings.RemoveAt(index);
        localLoadProgressTicks.RemoveAt(index);
        localLoadDurationTicks.RemoveAt(index);
    }

    private void ClearLegacyBatchMarkers()
    {
        progressTicks = 0;
        capturedCycleTicks = 0;
        loadingTicksRemaining = 0;
        batchCaptured = false;
        capturedWaterCharge = 0f;
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
        localLoadThings.Clear();
        localLoadProgressTicks.Clear();
        localLoadDurationTicks.Clear();
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
