using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

[HarmonyPatch(
    typeof(WorkGiver_HaulGeneral),
    nameof(WorkGiver_HaulGeneral.JobOnThing),
    typeof(Pawn),
    typeof(Thing),
    typeof(bool))]
internal static class GastronomyClaimedWareHaulGeneralPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Pawn pawn, Thing t, bool forced, ref Job? __result)
    {
        if (forced || pawn.Map?.GetComponent<MapComponent_GastronomyDishClearing>()?.IsClaimed(t) != true)
        {
            return true;
        }

        __result = null;
        return false;
    }
}

[HarmonyPatch(
    typeof(Pawn_JobTracker),
    nameof(Pawn_JobTracker.TryTakeOrderedJobPrioritizedWork),
    typeof(Job),
    typeof(WorkGiver),
    typeof(IntVec3))]
internal static class GastronomyClaimedWareForcedHaulPatch
{
    [HarmonyPostfix]
    private static void Postfix(
        Pawn ___pawn,
        Job job,
        WorkGiver giver,
        bool __result)
    {
        if (!__result || !job.playerForced || giver is not WorkGiver_HaulGeneral ||
            job.targetA.Thing is not { } claimedWare)
        {
            return;
        }

        ___pawn.Map?.GetComponent<MapComponent_GastronomyDishClearing>()
            ?.ReleaseClaimFor(claimedWare);
    }
}

public sealed class WorkGiver_DoDishes : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.HaulableEver);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
    {
        return IsDirtyWare(thing) &&
               (forced || !IsClaimedByGastronomy(pawn, thing)) &&
               !thing.IsForbidden(pawn) &&
               pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Some) &&
               TryFindDestination(pawn, thing, out _);
    }

    private static bool IsClaimedByGastronomy(Pawn pawn, Thing thing) =>
        pawn.Map?.GetComponent<MapComponent_GastronomyDishClearing>()
            ?.IsClaimed(thing) == true;

    public override Job? JobOnThing(Pawn pawn, Thing thing, bool forced = false)
    {
        if (!HasJobOnThing(pawn, thing, forced) || !TryFindDestination(pawn, thing, out var destination))
        {
            return null;
        }

        var job = CreateJob(thing, destination);
        ConfigurePickUpAndHaulBatch(pawn, thing, destination, job);
        return job;
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

    internal static Job CreateExactWareJob(Thing thing, DishwashingDestination destination)
    {
        var job = CreateJob(thing, destination);
        job.count = 1;
        return job;
    }

    internal static bool IsDirtyWare(Thing thing)
    {
        return thing.def.GetModExtension<KitchenwareExtension>()?.product is
                   KitchenwareProduct.Cookware or KitchenwareProduct.Plate or KitchenwareProduct.Cutlery &&
               (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true;
    }

    private static void ConfigurePickUpAndHaulBatch(
        Pawn pawn,
        Thing primary,
        DishwashingDestination destination,
        Job job)
    {
        if (!PickUpAndHaulAdapter.CanTrack(pawn))
        {
            return;
        }

        var candidates = new List<Thing> { primary };
        candidates.AddRange(pawn.Map.listerThings.AllThings
            .Where(thing => !ReferenceEquals(thing, primary))
            .Where(IsDirtyWare)
            .Where(thing => thing.Position.DistanceToSquared(primary.Position) <=
                            DishwashingBatchPolicy.SearchRadius * DishwashingBatchPolicy.SearchRadius)
            .OrderBy(thing => thing.Position.DistanceToSquared(primary.Position)));
        candidates = candidates.Distinct().ToList();

        var descriptors = candidates.Select(thing =>
        {
            var eligible = !thing.IsForbidden(pawn) &&
                           pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Some);
            var sameSource = eligible &&
                             TryFindDestination(pawn, thing, out var candidateDestination) &&
                             SameTarget(candidateDestination.Target, destination.Target);
            return new DishwashingBatchCandidate(
                thing.ThingID,
                thing.Position.DistanceToSquared(primary.Position),
                thing.stackCount,
                Math.Max(0.001f, thing.GetStatValue(StatDefOf.Mass)),
                sameSource,
                eligible,
                thing.def.GetModExtension<KitchenwareExtension>()?.plateEquivalent ?? 1f);
        }).ToList();
        var capacity = MassUtility.Capacity(pawn);
        var availableMass = Math.Max(0f, capacity * (1f - MassUtility.EncumbrancePercent(pawn)));
        var availableDishwasherCapacity = destination.IsHandwashing
            ? float.MaxValue
            : AvailableDishwasherCapacity(destination.Target.Thing);
        var selection = DishwashingBatchPolicy.Select(
            descriptors,
            availableMass,
            availableDishwasherCapacity);
        if (selection.Count == 0 || selection.All(item => item.Id != primary.ThingID))
        {
            return;
        }

        var byId = candidates.ToDictionary(thing => thing.ThingID, StringComparer.Ordinal);
        job.SetTarget(TargetIndex.A, LocalTargetInfo.Invalid);
        job.targetQueueA = selection.Select(item => (LocalTargetInfo)byId[item.Id]).ToList();
        job.countQueue = selection.Select(item => item.Count).ToList();
        job.count = 1;
    }

    private static float AvailableDishwasherCapacity(Thing? dishwasher)
    {
        if (dishwasher is not ThingWithComps building ||
            building.GetComp<CompDishwasher>() is not { } local)
        {
            return 0f;
        }

        return ProcessorFrameworkAdapter.Controls(building)
            ? ProcessorFrameworkAdapter.AvailablePlateEquivalentCapacity(building)
            : Math.Max(0f, local.Capacity - local.UsedCapacity);
    }

    private static bool SameTarget(LocalTargetInfo left, LocalTargetInfo right)
    {
        if (left.HasThing || right.HasThing)
        {
            return left.HasThing && right.HasThing && ReferenceEquals(left.Thing, right.Thing);
        }

        return left.Cell == right.Cell;
    }

    internal static bool TryFindDestination(Pawn pawn, Thing dirtyWare, out DishwashingDestination destination)
    {
        var dishwashers = FindAcceptingDishwashers(
                pawn,
                new[] { dirtyWare },
                dirtyWare.Position,
                includeProcessor: PickUpAndHaulAdapter.CanTrack(pawn))
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

    internal static bool TryFindSharedDestination(
        Pawn pawn,
        IReadOnlyList<Thing> exactWare,
        out DishwashingDestination destination)
    {
        if (exactWare.Count == 0 || exactWare.Any(thing =>
                !IsDirtyWare(thing) ||
                thing.IsForbidden(pawn) ||
                !pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Some)))
        {
            destination = DishwashingDestination.Invalid;
            return false;
        }

        var origin = exactWare[0].Position;
        var dishwashers = FindAcceptingDishwashers(
            pawn,
            exactWare,
            origin,
            includeProcessor: false).ToList();
        if (dishwashers.Count > 0 && ImmersiveChefsMod.Settings.PreferDishwashers)
        {
            destination = DishwashingDestination.ForDishwasher(dishwashers[0]);
            return true;
        }

        if (HandwashingSourceFinder.TryFind(pawn, origin, out destination))
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

    internal static bool HasReachablePotentialSharedDestination(
        Pawn pawn,
        IReadOnlyList<Thing> exactWare)
    {
        if (pawn.Map.listerThings.AllThings.Any(thing =>
                (thing.def == ImmersiveChefsDefOf.ImmersiveChefs_Dishwasher ||
                 thing.def == ImmersiveChefsDefOf.ImmersiveChefs_IndustrialDishwasher) &&
                !thing.IsForbidden(pawn) &&
                pawn.CanReach(thing, PathEndMode.Touch, Danger.Some) &&
                !ProcessorFrameworkAdapter.Controls(thing)))
        {
            return true;
        }

        return exactWare.Count > 0 &&
               HandwashingSourceFinder.HasReachablePotentialSource(
                   pawn,
                   exactWare[0].Position);
    }

    private static IEnumerable<Thing> FindAcceptingDishwashers(
        Pawn pawn,
        IReadOnlyList<Thing> exactWare,
        IntVec3 origin,
        bool includeProcessor)
    {
        var requiredCapacity = exactWare.Sum(thing =>
            thing.def.GetModExtension<KitchenwareExtension>()?.plateEquivalent ?? 1f);
        return pawn.Map.listerThings.AllThings
            .Where(thing => thing.def == ImmersiveChefsDefOf.ImmersiveChefs_Dishwasher ||
                            thing.def == ImmersiveChefsDefOf.ImmersiveChefs_IndustrialDishwasher)
            .Where(thing =>
                !thing.IsForbidden(pawn) &&
                pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Some))
            .Where(thing =>
            {
                if (ProcessorFrameworkAdapter.Controls(thing))
                {
                    return includeProcessor &&
                           exactWare.All(ware => ProcessorFrameworkAdapter.CanAcceptTrackedWare(thing, ware)) &&
                           ProcessorFrameworkAdapter.AvailablePlateEquivalentCapacity(thing) + 0.0001f >=
                           requiredCapacity;
                }

                return (thing as ThingWithComps)?.GetComp<CompDishwasher>() is { } comp &&
                       exactWare.All(comp.CanAccept) &&
                       comp.Capacity - comp.UsedCapacity + 0.0001f >= requiredCapacity;
            })
            .OrderBy(thing => thing.Position.DistanceToSquared(origin));
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
            .Select(thing => new { Thing = thing, Source = ClassifyObjectSource(pawn, thing) })
            .Where(candidate => candidate.Source.HasValue)
            .Where(candidate =>
                !candidate.Thing.IsForbidden(pawn) &&
                pawn.CanReserveAndReach(candidate.Thing, PathEndMode.Touch, Danger.Some))
            .OrderBy(candidate => WashSourcePolicy.Priority(candidate.Source!.Value.Kind))
            .ThenBy(candidate => candidate.Thing.Position.DistanceToSquared(origin))
            .FirstOrDefault();
        if (source is not null)
        {
            destination = DishwashingDestination.ForHandwashing(
                source.Thing,
                source.Source!.Value.Provenance);
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

    internal static bool HasReachablePotentialSource(Pawn pawn, IntVec3 origin)
    {
        if (pawn.Map.listerThings.AllThings.Any(thing =>
                IsPotentialObjectSource(pawn, thing) &&
                !thing.IsForbidden(pawn) &&
                pawn.CanReach(thing, PathEndMode.Touch, Danger.Some)))
        {
            return true;
        }

        if (!ImmersiveChefsMod.Settings.AllowTerrainHandwashing)
        {
            return false;
        }

        return GenRadial.RadialCellsAround(origin, 40f, useCenter: true).Any(cell =>
            cell.InBounds(pawn.Map) &&
            pawn.Map.terrainGrid.TerrainAt(cell).IsWater &&
            pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some));
    }

    private static bool IsPotentialObjectSource(Pawn pawn, Thing thing)
    {
        if (!IsFromDubsBadHygiene(thing))
        {
            return IsNamedWaterSource(thing);
        }

        return DubsWaterAdapter.TryClassifyHandwashingSource(
                   pawn,
                   thing,
                   out _,
                   out var pawnAllowed,
                   out _,
                   out _) &&
               pawnAllowed;
    }

    private static HandwashingObjectSource? ClassifyObjectSource(Pawn pawn, Thing thing)
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
        if (fromDubs)
        {
            if (!DubsWaterAdapter.TryClassifyHandwashingSource(
                    pawn,
                    thing,
                    out var kind,
                    out var pawnAllowed,
                    out var dubsOperational,
                    out var hasAvailableWater))
            {
                return null;
            }

            var provenance = WashSourcePolicy.ClassifyDubsSource(
                kind,
                ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.DubsBadHygiene),
                pawnAllowed,
                operational && dubsOperational,
                hasAvailableWater);
            return provenance.HasValue
                ? new HandwashingObjectSource(kind, provenance.Value)
                : null;
        }

        if (!IsNamedWaterSource(thing) || !operational)
        {
            return null;
        }

        return new HandwashingObjectSource(
            GenericWaterSourceKind(thing.def.defName),
            WashProvenance.WildWater);
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

    private static HandwashingSourceKind GenericWaterSourceKind(string defName)
    {
        if (defName.IndexOf("WaterBowl", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return HandwashingSourceKind.HauledWater;
        }

        return defName.IndexOf("Well", StringComparison.OrdinalIgnoreCase) >= 0
            ? HandwashingSourceKind.Well
            : HandwashingSourceKind.ConnectedFixture;
    }
}

internal enum HandwashingSourceKind
{
    DubsKitchenSink,
    ConnectedFixture,
    HauledWater,
    Well
}

internal readonly struct HandwashingObjectSource
{
    internal HandwashingObjectSource(HandwashingSourceKind kind, WashProvenance provenance)
    {
        Kind = kind;
        Provenance = provenance;
    }

    internal HandwashingSourceKind Kind { get; }

    internal WashProvenance Provenance { get; }
}

internal static class WashSourcePolicy
{
    internal static int Priority(HandwashingSourceKind kind) => (int)kind;

    internal static WashProvenance? ClassifyDubsSource(
        HandwashingSourceKind kind,
        bool dubsIntegrationEnabled,
        bool pawnAllowed,
        bool operational,
        bool hasAvailableWater)
    {
        if (!dubsIntegrationEnabled || !pawnAllowed || !operational || !hasAvailableWater)
        {
            return null;
        }

        return kind is HandwashingSourceKind.DubsKitchenSink or HandwashingSourceKind.ConnectedFixture
            ? WashProvenance.Safe
            : WashProvenance.WildWater;
    }

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
