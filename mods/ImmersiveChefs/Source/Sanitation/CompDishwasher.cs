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
    public static int CountAccepted(float capacity, float used, float perItem, int stackCount)
    {
        var boundedPerItem = Math.Max(0.01f, perItem);
        var free = Math.Max(0f, capacity - used);
        return Math.Max(0, Math.Min(stackCount, (int)Math.Floor((free + 0.001f) / boundedPerItem)));
    }
}

public sealed class CompDishwasher : ThingComp, IThingHolder
{
    private ThingOwner<Thing>? contents;
    private int progressTicks;
    private int capturedCycleTicks;
    private bool waterDebitedForCycle;
    private float capturedWaterCharge;
    private string pauseReason = string.Empty;
    private IntVec3 lastKnownPosition = IntVec3.Invalid;

    private ThingOwner<Thing> Contents => contents ??= new ThingOwner<Thing>(this, LookMode.Deep);

    public float Capacity => Props.basePlateCapacity * ImmersiveChefsMod.Settings.DishwasherCapacityScale;

    public float UsedCapacity => Contents.InnerListForReading.Sum(PlateEquivalents);

    internal float WaterPerPlateEquivalent => Props.waterPerPlateEquivalent;

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
               progressTicks == 0 && !waterDebitedForCycle &&
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
        if (transferred > 0 && capturedCycleTicks <= 0)
        {
            capturedCycleTicks = Math.Max(1, (int)Math.Round(
                Props.baseCycleTicks * ImmersiveChefsMod.Settings.DishwashingWorkScale));
        }

        return transferred > 0;
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
            return;
        }
        if (!Contents.Any)
        {
            progressTicks = 0;
            capturedCycleTicks = 0;
            waterDebitedForCycle = false;
            capturedWaterCharge = 0f;
            pauseReason = string.Empty;
            return;
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
        progressTicks = 0;
        capturedCycleTicks = 0;
        pauseReason = string.Empty;
    }

    public override string CompInspectStringExtra()
    {
        var status = !Contents.Any
            ? "idle"
            : string.IsNullOrEmpty(pauseReason)
                ? $"washing ({Math.Min(100, (int)(100f * progressTicks / Math.Max(1, capturedCycleTicks)))}%)"
                : $"paused: {pauseReason}";
        return $"Dishwasher: {status}\nCapacity: {UsedCapacity:0.##}/{Capacity:0.##} place settings";
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (!Contents.Any)
        {
            yield break;
        }

        yield return new Command_Action
        {
            defaultLabel = "Eject dishes",
            defaultDesc = "Cancel this cycle and return the exact dishes without cleaning them.",
            action = EjectAll
        };
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref contents, "contents", this);
        Scribe_Values.Look(ref progressTicks, "progressTicks", 0);
        Scribe_Values.Look(ref capturedCycleTicks, "capturedCycleTicks", 0);
        Scribe_Values.Look(ref waterDebitedForCycle, "waterDebitedForCycle", false);
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
            capturedWaterCharge = Math.Max(0.001f, UsedCapacity * Props.waterPerPlateEquivalent);
            if (!DubsWaterAdapter.TryConsumeCycleWater(parent, capturedWaterCharge, out reason))
            {
                return false;
            }

            waterDebitedForCycle = true;
        }


        if (Props.requiresDubsWater && waterDebitedForCycle &&
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.DubsBadHygiene) &&
            !DubsWaterAdapter.IsConnected(parent))
        {
            reason = "water supply disconnected";
            return false;
        }

        reason = string.Empty;
        return true;
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
        progressTicks = 0;
        capturedCycleTicks = 0;
        waterDebitedForCycle = false;
        capturedWaterCharge = 0f;
        pauseReason = string.Empty;
    }

    private static float PlateEquivalents(Thing thing) =>
        PlateEquivalentsPerItem(thing) * thing.stackCount;

    private static float PlateEquivalentsPerItem(Thing thing) =>
        thing.def.GetModExtension<KitchenwareExtension>()?.plateEquivalent ?? 1f;
}
