using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

public sealed class WorkGiver_PlateMeals : WorkGiver_Scanner
{
    private Pawn? cachedPawn;
    private Thing? cachedThing;
    private Job? cachedJob;
    private int cachedTick = -1;
    private bool cachedForced;
    private bool hasCachedResult;

    public override ThingRequest PotentialWorkThingRequest =>
        ThingRequest.ForGroup(ThingRequestGroup.HaulableEver);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
    {
        cachedPawn = pawn;
        cachedThing = thing;
        cachedForced = forced;
        cachedTick = Find.TickManager?.TicksGame ?? -1;
        cachedJob = TryCreateJob(pawn, thing, recordFailedOpportunity: true);
        hasCachedResult = true;
        return cachedJob is not null;
    }

    public override Job? JobOnThing(Pawn pawn, Thing thing, bool forced = false)
    {
        var tick = Find.TickManager?.TicksGame ?? -1;
        if (hasCachedResult &&
            ReferenceEquals(cachedPawn, pawn) &&
            ReferenceEquals(cachedThing, thing) &&
            cachedForced == forced &&
            cachedTick == tick)
        {
            var result = cachedJob;
            ClearCachedResult();
            return result;
        }

        ClearCachedResult();
        return TryCreateJob(pawn, thing, recordFailedOpportunity: true);
    }

    private static Job? TryCreateJob(
        Pawn pawn,
        Thing thing,
        bool recordFailedOpportunity)
    {
        if (ImmersiveChefsMod.Settings.WareRequirementMode == WareRequirementMode.Off ||
            !ImportedMealPlatingRuntime.NeedsPlating(thing) ||
            thing.IsForbidden(pawn) ||
            !pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Some))
        {
            return null;
        }

        var embedded = (thing as ThingWithComps)?.GetComp<CompEmbeddedWare>();
        if (embedded is null ||
            !TryFindSurface(pawn, thing, out var surface) ||
            !TryFindPlate(pawn, thing, embedded, out var plate, out var count))
        {
            if (recordFailedOpportunity)
            {
                ImportedMealPlatingRuntime.RecordFailedOpportunity(pawn, thing, embedded);
            }

            return null;
        }

        var job = JobMaker.MakeJob(
            ImmersiveChefsDefOf.ImmersiveChefs_PlateMeals,
            thing,
            plate,
            surface);
        job.count = count;
        return job;
    }

    private void ClearCachedResult()
    {
        cachedPawn = null;
        cachedThing = null;
        cachedJob = null;
        cachedTick = -1;
        cachedForced = false;
        hasCachedResult = false;
    }

    private static bool TryFindSurface(Pawn pawn, Thing meal, out Thing surface)
    {
        surface = pawn.Map.listerThings.AllThings
            .OfType<Building_WorkTable>()
            .Where(table => table.def.AllRecipes.Any(MealCoveragePolicy.IsCovered))
            .Where(table => IsOperational(table) && !table.IsForbidden(pawn))
            .Where(table => pawn.CanReserveAndReach(
                table,
                PathEndMode.InteractionCell,
                Danger.Some))
            .OrderBy(table => table.Position.DistanceToSquared(meal.Position))
            .FirstOrDefault()!;
        return surface is not null;
    }

    private static bool TryFindPlate(
        Pawn pawn,
        Thing meal,
        CompEmbeddedWare embedded,
        out Thing plate,
        out int count)
    {
        plate = null!;
        count = 0;
        var complexity = MealComplexityRuntime.Classify(meal.def);
        var candidates = pawn.Map.listerThings.AllThings
            .Where(candidate =>
                 candidate.def.GetModExtension<KitchenwareExtension>()?.product ==
                 KitchenwareProduct.Plate)
            .Where(candidate => PlateMaterialEligibilityRuntime.Allows(candidate, complexity))
            .Where(candidate => !candidate.IsForbidden(pawn) &&
                                pawn.CanReach(candidate, PathEndMode.Touch, Danger.Some) &&
                                pawn.CanReserve(candidate, 1, 1))
            .Select(candidate => new
            {
                Thing = candidate,
                Dirty = (candidate as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true,
                Score = KitchenwareRuntime.ServiceScore(candidate) -
                        candidate.Position.DistanceToSquared(meal.Position) * 0.01f
            })
            .OrderBy(candidate => candidate.Dirty)
            .ThenByDescending(candidate => candidate.Score)
            .ToList();
        var emergency = ImportedMealPlatingRuntime.HasEmergencyDiner(pawn, meal);
        var selection = WareSelectionPolicy.Select(
            ImmersiveChefsMod.Settings.WareRequirementMode,
            ImmersiveChefsMod.Settings.DirtyWareFallback,
            emergency,
            candidates.Any(candidate => !candidate.Dirty),
            candidates.Any(candidate => candidate.Dirty));
        var wantDirty = selection.Use == WareUse.Dirty;
        if (selection.Use is not (WareUse.Clean or WareUse.Dirty))
        {
            return false;
        }

        var missing = Math.Max(0, meal.stackCount - embedded.EmbeddedPlateCount);
        foreach (var candidate in candidates.Where(candidate => candidate.Dirty == wantDirty))
        {
            var reservableCount = Math.Min(missing, PlateMaterialEligibilityRuntime.CountEligible(candidate.Thing, complexity));
            while (reservableCount > 0 && !pawn.CanReserve(candidate.Thing, 1, reservableCount))
            {
                reservableCount--;
            }

            if (reservableCount <= 0)
            {
                continue;
            }

            plate = candidate.Thing;
            count = reservableCount;
            return true;
        }

        return false;
    }

    internal static bool IsOperational(ThingWithComps thing)
    {
        return (thing.GetComp<CompPowerTrader>()?.PowerOn ?? true) &&
               (thing.GetComp<CompRefuelable>()?.HasFuel ?? true) &&
               (thing.GetComp<CompFlickable>()?.SwitchIsOn ?? true) &&
               !(thing.GetComp<CompBreakdownable>()?.BrokenDown ?? false);
    }
}

public sealed class JobDriver_PlateMeals : JobDriver
{
    private const int PlatingTicks = 240;
    private Thing? heldPlates;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed) &&
               pawn.Reserve(job.targetB, job, 1, job.count, null, errorOnFailed) &&
               pawn.Reserve(job.targetC, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
        this.FailOnDestroyedNullOrForbidden(TargetIndex.B);
        this.FailOnDestroyedNullOrForbidden(TargetIndex.C);
        AddFinishAction(_ => ReturnUnattachedThings());

        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
        yield return Toils_General.DoAtomic(PickupPlates);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.DoAtomic(PickupMealStack);
        yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.InteractionCell);
        var plateAtSurface = Toils_General.WaitWith(
            TargetIndex.C,
            PlatingTicks,
            useProgressBar: true);
        plateAtSurface.AddFailCondition(() =>
            TargetThingC is not ThingWithComps surface ||
            !WorkGiver_PlateMeals.IsOperational(surface));
        yield return plateAtSurface;
        yield return Toils_General.DoAtomic(AttachPlates);
        yield return Toils_Haul.DropCarriedThing();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref heldPlates, "heldPlates");
    }

    private void PickupPlates()
    {
        var source = TargetThingB;
        var inventory = pawn.inventory?.innerContainer;
        if (source is null || source.Destroyed || inventory is null)
        {
            EndIncompletable();
            return;
        }

        var count = Math.Min(job.count, source.stackCount);
        if (!PlateMaterialEligibilityRuntime.PreparePickup(source, MealComplexityRuntime.Classify(TargetThingA.def), count))
        {
            EndIncompletable();
            return;
        }
        var picked = count < source.stackCount ? source.SplitOff(count) : source;
        if (picked.Spawned)
        {
            picked.DeSpawn(DestroyMode.Vanish);
        }

        if (!inventory.TryAdd(picked, canMergeWithExistingStacks: false))
        {
            GenPlace.TryPlaceThing(picked, pawn.Position, pawn.Map, ThingPlaceMode.Near);
            EndIncompletable();
            return;
        }

        heldPlates = picked;
    }

    private void PickupMealStack()
    {
        var meal = TargetThingA;
        if (meal is null || meal.Destroyed || pawn.carryTracker.TryStartCarry(meal, meal.stackCount, false) <= 0)
        {
            EndIncompletable();
        }
    }

    private void AttachPlates()
    {
        if (pawn.carryTracker.CarriedThing is not ThingWithComps meal ||
            meal.GetComp<CompEmbeddedWare>() is not { } embedded ||
            heldPlates is null)
        {
            EndIncompletable();
            return;
        }

        var culinary = meal.GetComp<CompCulinaryState>();
        var remaining = Math.Min(job.count, meal.stackCount - embedded.EmbeddedPlateCount);
        while (remaining-- > 0 && !heldPlates.Destroyed && heldPlates.stackCount > 0)
        {
            var servingIndex = embedded.EmbeddedPlateCount;
            var sanitation = (heldPlates as ThingWithComps)?.GetComp<CompSanitation>();
            var contamination = SanitationContamination.ForPlate(
                sanitation?.IsDirty == true,
                sanitation?.WashProvenance ?? WashProvenance.None);
            if (!embedded.TryEmbedPlate(heldPlates))
            {
                break;
            }

            culinary?.AddContaminationToServing(servingIndex, contamination);
        }
    }

    private void ReturnUnattachedThings()
    {
        if (heldPlates is { Destroyed: false } &&
            ReferenceEquals(heldPlates.holdingOwner, pawn.inventory?.innerContainer) &&
            pawn.MapHeld is { } plateMap)
        {
            pawn.inventory!.innerContainer.TryDrop(
                heldPlates,
                pawn.PositionHeld,
                plateMap,
                ThingPlaceMode.Near,
                out _);
        }

        if (pawn.carryTracker.CarriedThing is not null && pawn.MapHeld is not null)
        {
            pawn.carryTracker.TryDropCarriedThing(
                pawn.PositionHeld,
                ThingPlaceMode.Near,
                out _);
        }
    }

    private void EndIncompletable()
    {
        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
    }
}

internal static class ImportedMealPlatingRuntime
{
    internal static bool NeedsPlating(Thing thing)
    {
        if (!MealCoveragePolicy.IsCovered(thing.def) || thing is not ThingWithComps meal ||
            meal.GetComp<CompEmbeddedWare>() is not { } embedded)
        {
            return false;
        }

        return ImportedMealPlatingPolicy.Evaluate(
            ImmersiveChefsMod.Settings.WareRequirementMode,
            coveredMeal: true,
            thing.stackCount,
            embedded.EmbeddedPlateCount,
            embedded.PlatingOpportunityFailed,
            emergency: false,
            canPlateNow: false).PlatesRequired > 0;
    }

    internal static bool AllowDining(Thing foodSource, Pawn eater)
    {
        if (!MealCoveragePolicy.IsCovered(foodSource.def) ||
            foodSource is not ThingWithComps meal ||
            meal.GetComp<CompEmbeddedWare>() is not { } embedded)
        {
            return true;
        }

        var emergency = eater.needs?.food?.CurLevelPercentage <=
                        ImmersiveChefsMod.Settings.EmergencyHungerThreshold;
        return ImportedMealPlatingPolicy.Evaluate(
            ImmersiveChefsMod.Settings.WareRequirementMode,
            coveredMeal: true,
            foodSource.stackCount,
            embedded.EmbeddedPlateCount,
            embedded.PlatingOpportunityFailed,
            emergency,
            canPlateNow: false).AllowDining;
    }

    internal static void RecordFailedOpportunity(
        Pawn worker,
        Thing meal,
        CompEmbeddedWare? embedded)
    {
        if (embedded is null)
        {
            return;
        }

        var decision = ImportedMealPlatingPolicy.Evaluate(
            ImmersiveChefsMod.Settings.WareRequirementMode,
            MealCoveragePolicy.IsCovered(meal.def),
            meal.stackCount,
            embedded.EmbeddedPlateCount,
            embedded.PlatingOpportunityFailed,
            HasEmergencyDiner(worker, meal),
            canPlateNow: false);
        if (decision.RecordFailedOpportunity)
        {
            embedded.RecordFailedPlatingOpportunity();
        }
    }

    internal static bool HasEmergencyDiner(Pawn worker, Thing meal)
    {
        return worker.Map.mapPawns.FreeColonistsSpawned.Any(candidate =>
            DiningPawnPolicy.AppliesDiningConsequences(candidate.RaceProps.Humanlike) &&
            candidate.needs?.food?.CurLevelPercentage <=
            ImmersiveChefsMod.Settings.EmergencyHungerThreshold &&
            candidate.CanReach(meal, PathEndMode.Touch, Danger.Some));
    }

    internal static bool HasPotentialPlatingSurface(Map map)
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamedSilentFail("Cooking");
        var cooks = map.mapPawns.FreeColonistsSpawned
            .Where(pawn => !pawn.Downed && !pawn.InMentalState)
            .Where(pawn => cooking is null || !pawn.WorkTypeIsDisabled(cooking))
            .ToList();
        return map.listerThings.AllThings
            .OfType<Building_WorkTable>()
            .Any(table => table.def.AllRecipes.Any(MealCoveragePolicy.IsCovered) &&
                          (table.GetComp<CompPowerTrader>()?.PowerOn ?? true) &&
                          (table.GetComp<CompRefuelable>()?.HasFuel ?? true) &&
                          (table.GetComp<CompFlickable>()?.SwitchIsOn ?? true) &&
                          !(table.GetComp<CompBreakdownable>()?.BrokenDown ?? false) &&
                          cooks.Any(pawn => !table.IsForbidden(pawn) &&
                                            pawn.CanReach(
                                                table,
                                                PathEndMode.InteractionCell,
                                                Danger.Some)));
    }
}

[HarmonyPatch(typeof(RimWorld.FoodUtility), "IsFoodSourceOnMapSociallyProper")]
internal static class ImportedMealDiningGatePatch
{
    private static void Postfix(Thing t, Pawn eater, ref bool __result)
    {
        if (__result)
        {
            __result = ImportedMealPlatingRuntime.AllowDining(t, eater);
        }
    }
}

[HarmonyPatch(
    typeof(RimWorld.FoodUtility),
    "FoodOptimality",
    new[] { typeof(Pawn), typeof(Thing), typeof(ThingDef), typeof(float), typeof(bool) })]
internal static class PlatedMealFoodOptimalityPatch
{
    private static void Postfix(Thing foodSource, ref float __result)
    {
        var embedded = (foodSource as ThingWithComps)?.GetComp<CompEmbeddedWare>();
        __result += ImportedMealPlatingPolicy.FoodOptimalityBonus(
            MealCoveragePolicy.IsCovered(foodSource.def),
            foodSource.stackCount,
            embedded?.EmbeddedPlateCount ?? 0);
    }
}
