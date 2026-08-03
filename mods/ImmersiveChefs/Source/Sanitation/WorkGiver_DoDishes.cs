using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

public sealed class WorkGiver_DoDishes : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.HaulableEver);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
    {
        return IsDirtyWare(thing) &&
               !thing.IsForbidden(pawn) &&
               pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Some) &&
               TryFindDestination(pawn, thing, out _);
    }

    public override Job? JobOnThing(Pawn pawn, Thing thing, bool forced = false)
    {
        if (!HasJobOnThing(pawn, thing, forced) || !TryFindDestination(pawn, thing, out var destination))
        {
            return null;
        }

        return CreateJob(thing, destination);
    }

    internal static Job CreateJob(Thing thing, DishwashingDestination destination)
    {
        var job = JobMaker.MakeJob(ImmersiveChefsDefOf.ImmersiveChefs_DoDishes, thing, destination.Target);
        if (destination.IsSafeHandwashingSource)
        {
            job.SetTarget(TargetIndex.C, destination.Target);
        }

        if (destination.Target.Thing is ThingWithComps dishwasher)
        {
            job.count = Math.Max(1, dishwasher.GetComp<CompDishwasher>()?.CountCanAccept(thing) ?? 1);
        }
        else
        {
            job.count = thing.stackCount;
        }

        return job;
    }

    private static bool IsDirtyWare(Thing thing)
    {
        return thing.def.GetModExtension<KitchenwareExtension>()?.product is
                   KitchenwareProduct.Cookware or KitchenwareProduct.Plate or KitchenwareProduct.Cutlery &&
               (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true;
    }

    internal static bool TryFindDestination(Pawn pawn, Thing dirtyWare, out DishwashingDestination destination)
    {
        var dishwashers = pawn.Map.listerThings.AllThings
            .Where(thing => thing.def == ImmersiveChefsDefOf.ImmersiveChefs_Dishwasher ||
                            thing.def == ImmersiveChefsDefOf.ImmersiveChefs_IndustrialDishwasher)
            .Where(thing =>
                !thing.IsForbidden(pawn) &&
                pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Some))
            .Where(thing => !ProcessorFrameworkAdapter.Controls(thing))
            .Where(thing => (thing as ThingWithComps)?.GetComp<CompDishwasher>()?.CanAccept(dirtyWare) == true)
            .OrderBy(thing => thing.Position.DistanceToSquared(dirtyWare.Position))
            .ToList();
        if (dishwashers.Count > 0 && ImmersiveChefsMod.Settings.PreferDishwashers)
        {
            destination = DishwashingDestination.ForDishwasher(dishwashers[0]);
            return true;
        }

        if (HandwashingSourceFinder.TryFind(pawn, dirtyWare.Position, out destination))
        {
            return true;
        }

        if (dishwashers.Count > 0)
        {
            destination = DishwashingDestination.ForDishwasher(dishwashers[0]);
            return true;
        }

        destination = DishwashingDestination.Invalid;
        return false;
    }
}

internal readonly struct DishwashingDestination
{
    private DishwashingDestination(LocalTargetInfo target, WashProvenance provenance, bool handwashing)
    {
        Target = target;
        Provenance = provenance;
        IsHandwashing = handwashing;
    }

    internal LocalTargetInfo Target { get; }

    internal WashProvenance Provenance { get; }

    internal bool IsHandwashing { get; }

    internal bool IsSafeHandwashingSource => IsHandwashing && Provenance == WashProvenance.Safe;

    internal static DishwashingDestination Invalid =>
        new(LocalTargetInfo.Invalid, WashProvenance.None, handwashing: false);

    internal static DishwashingDestination ForDishwasher(Thing dishwasher) =>
        new(dishwasher, WashProvenance.Safe, handwashing: false);

    internal static DishwashingDestination ForHandwashing(
        LocalTargetInfo target,
        WashProvenance provenance) =>
        new(target, provenance, handwashing: true);
}

internal static class HandwashingSourceFinder
{
    internal static bool TryFind(Pawn pawn, IntVec3 origin, out DishwashingDestination destination)
    {
        var source = pawn.Map.listerThings.AllThings
            .Where(IsNamedWaterSource)
            .Select(thing => new { Thing = thing, Provenance = ClassifyObjectSource(thing) })
            .Where(candidate => candidate.Provenance.HasValue)
            .Where(candidate =>
                !candidate.Thing.IsForbidden(pawn) &&
                pawn.CanReserveAndReach(candidate.Thing, PathEndMode.Touch, Danger.Some))
            .OrderBy(candidate => WaterSourcePriority(candidate.Thing.def.defName))
            .ThenBy(candidate => candidate.Thing.Position.DistanceToSquared(origin))
            .FirstOrDefault();
        if (source is not null)
        {
            destination = DishwashingDestination.ForHandwashing(source.Thing, source.Provenance!.Value);
            return true;
        }

        if (ImmersiveChefsMod.Settings.AllowTerrainHandwashing)
        {
            foreach (var cell in GenRadial.RadialCellsAround(origin, 40f, useCenter: true))
            {
                if (cell.InBounds(pawn.Map) && pawn.Map.terrainGrid.TerrainAt(cell).IsWater &&
                    pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some))
                {
                    destination = DishwashingDestination.ForHandwashing(cell, WashProvenance.WildWater);
                    return true;
                }
            }
        }

        destination = DishwashingDestination.Invalid;
        return false;
    }

    private static WashProvenance? ClassifyObjectSource(Thing thing)
    {
        var power = thing.TryGetComp<CompPowerTrader>();
        var fuel = thing.TryGetComp<CompRefuelable>();
        var flick = thing.TryGetComp<CompFlickable>();
        var breakdown = thing.TryGetComp<CompBreakdownable>();
        var operational = (power is null || power.PowerOn) &&
                          (fuel is null || fuel.HasFuel) &&
                          (flick is null || flick.SwitchIsOn) &&
                          (breakdown is null || !breakdown.BrokenDown);
        var fromDubs = IsFromDubsBadHygiene(thing);
        return WashSourcePolicy.ClassifyObjectSource(
            fromDubs,
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.DubsBadHygiene),
            fromDubs && DubsWaterAdapter.IsPlumbedDubsFixture(thing),
            operational && (!fromDubs || DubsWaterAdapter.IsOperationalFixture(thing)),
            !fromDubs || DubsWaterAdapter.CanSupplyCycleWater(thing));
    }

    private static bool IsFromDubsBadHygiene(Thing thing) =>
        string.Equals(
            thing.def.modContentPack?.PackageId,
            "Dubwise.DubsBadHygiene",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsNamedWaterSource(Thing thing)
    {
        var name = thing.def.defName;
        return name.IndexOf("KitchenSink", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Sink", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("WaterBowl", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Well", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static int WaterSourcePriority(string defName)
    {
        if (defName.IndexOf("KitchenSink", StringComparison.OrdinalIgnoreCase) >= 0) return 0;
        if (defName.IndexOf("Sink", StringComparison.OrdinalIgnoreCase) >= 0) return 1;
        if (defName.IndexOf("WaterBowl", StringComparison.OrdinalIgnoreCase) >= 0) return 2;
        return 3;
    }
}

internal static class WashSourcePolicy
{
    internal static WashProvenance? ClassifyObjectSource(
        bool fromDubs,
        bool dubsIntegrationEnabled,
        bool validatedDubsFixture,
        bool operational,
        bool hasCycleWater)
    {
        if (!operational)
        {
            return null;
        }

        if (!fromDubs)
        {
            return WashProvenance.WildWater;
        }

        return dubsIntegrationEnabled && validatedDubsFixture && hasCycleWater
            ? WashProvenance.Safe
            : null;
    }
}
