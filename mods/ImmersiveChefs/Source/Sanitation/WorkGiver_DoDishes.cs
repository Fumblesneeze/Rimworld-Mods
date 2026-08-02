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

        var job = JobMaker.MakeJob(ImmersiveChefsDefOf.ImmersiveChefs_DoDishes, thing, destination);
        if (destination.Thing is ThingWithComps dishwasher)
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
                   KitchenwareProduct.Cookware or KitchenwareProduct.Plate or KitchenwareProduct.Silverware &&
               (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true;
    }

    internal static bool TryFindDestination(Pawn pawn, Thing dirtyWare, out LocalTargetInfo target)
    {
        var dishwashers = pawn.Map.listerThings.AllThings
            .Where(thing => thing.def == ImmersiveChefsDefOf.ImmersiveChefs_Dishwasher ||
                            thing.def == ImmersiveChefsDefOf.ImmersiveChefs_IndustrialDishwasher)
            .Where(thing => !thing.IsForbidden(pawn) && pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Some))
            .Where(thing => !ProcessorFrameworkAdapter.Controls(thing))
            .Where(thing => (thing as ThingWithComps)?.GetComp<CompDishwasher>()?.CanAccept(dirtyWare) == true)
            .OrderBy(thing => thing.Position.DistanceToSquared(dirtyWare.Position))
            .ToList();
        if (dishwashers.Count > 0 && ImmersiveChefsMod.Settings.PreferDishwashers)
        {
            target = dishwashers[0];
            return true;
        }

        if (HandwashingSourceFinder.TryFind(pawn, dirtyWare.Position, out target))
        {
            return true;
        }

        if (dishwashers.Count > 0)
        {
            target = dishwashers[0];
            return true;
        }

        target = LocalTargetInfo.Invalid;
        return false;
    }
}

internal static class HandwashingSourceFinder
{
    internal static bool TryFind(Pawn pawn, IntVec3 origin, out LocalTargetInfo target)
    {
        var source = pawn.Map.listerThings.AllThings
            .Where(IsNamedWaterSource)
            .Where(HasUsableWater)
            .Where(thing => !thing.IsForbidden(pawn) && pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Some))
            .OrderBy(thing => WaterSourcePriority(thing.def.defName))
            .ThenBy(thing => thing.Position.DistanceToSquared(origin))
            .FirstOrDefault();
        if (source is not null)
        {
            target = source;
            return true;
        }

        if (ImmersiveChefsMod.Settings.AllowTerrainHandwashing)
        {
            foreach (var cell in GenRadial.RadialCellsAround(origin, 40f, useCenter: true))
            {
                if (cell.InBounds(pawn.Map) && pawn.Map.terrainGrid.TerrainAt(cell).IsWater &&
                    pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some))
                {
                    target = cell;
                    return true;
                }
            }
        }

        target = LocalTargetInfo.Invalid;
        return false;
    }

    private static bool HasUsableWater(Thing thing)
    {
        if (!ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.DubsBadHygiene) ||
            !DubsWaterAdapter.IsPlumbedDubsFixture(thing))
        {
            return true;
        }

        return DubsWaterAdapter.IsOperationalFixture(thing) &&
               DubsWaterAdapter.CanSupplyCycleWater(thing);
    }

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
