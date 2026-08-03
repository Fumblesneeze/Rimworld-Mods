using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

[HarmonyPatch(typeof(Thing), nameof(Thing.Ingested), typeof(Pawn), typeof(float))]
internal static class DiningIngestionOutcomePatch
{
    private sealed class Snapshot
    {
        internal Snapshot(ThingDef mealDef, CompEmbeddedWare? embeddedWare)
        {
            MealDef = mealDef;
            EmbeddedWare = embeddedWare;
        }

        internal ThingDef MealDef { get; }
        internal CompEmbeddedWare? EmbeddedWare { get; }
        internal CulinaryServingRecord? Serving { get; set; }
        internal bool Completed { get; set; }
    }

    private static void Prefix(Thing __instance, Pawn ingester, out Snapshot? __state)
    {
        __state = null;
        if (__instance is not ThingWithComps meal || !MealCoveragePolicy.IsCovered(meal.def) ||
            !DiningPawnPolicy.AppliesDiningConsequences(ingester.RaceProps.Humanlike))
        {
            return;
        }

        var snapshot = new Snapshot(meal.def, meal.GetComp<CompEmbeddedWare>());
        __state = snapshot;
        DiningSessionRegistry.TryAttachTravel(ingester, meal);
        DiningSessionRegistry.BeginIngestion(ingester);
        snapshot.Serving = meal.GetComp<CompCulinaryState>()?.PeekCurrentServing();
        snapshot.Serving?.AddContamination(
            DiningSessionRegistry.Current(ingester)?.TravelPlateContamination ?? ContaminationSources.None);
    }

    private static void Postfix(Pawn ingester, float __result, Snapshot? __state)
    {
        if (__state is null)
        {
            return;
        }

        if (!CaravanDiningPolicy.WasIngested(__result))
        {
            __state.EmbeddedWare?.AbortIngestion();
            DiningSessionRegistry.EndIngestion(ingester);
            __state.Completed = true;
            return;
        }

        var dining = DiningSessionRegistry.Current(ingester);
        try
        {
            if (__state.Serving is { } serving)
            {
                serving.AddContamination(SanitationContamination.ForCutlery(
                    dining?.CutleryWasDirty == true,
                    dining?.CutleryWasWildWaterWashed == true
                        ? WashProvenance.WildWater
                        : WashProvenance.Safe));

                DiningExperience.Apply(ingester, __state.MealDef, serving, dining);
            }
        }
        finally
        {
            DiningSessionRegistry.Complete(ingester);
            __state.Completed = true;
        }
    }

    private static Exception? Finalizer(Pawn ingester, Snapshot? __state, Exception? __exception)
    {
        if (__state is not null && !__state.Completed)
        {
            __state.EmbeddedWare?.AbortIngestion();
            DiningSessionRegistry.EndIngestion(ingester);
        }

        return __exception;
    }
}

[HarmonyPatch(typeof(JobDriver_Ingest), nameof(JobDriver_Ingest.TryMakePreToilReservations))]
internal static class IngestReservationPatch
{
    private static void Postfix(JobDriver_Ingest __instance, ref bool __result)
    {
        if (!__result)
        {
            return;
        }

        var pawn = __instance.GetActor();
        var job = pawn.CurJob;
        var meal = job?.GetTarget(TargetIndex.A).Thing;
        if (job is not null && meal is not null)
        {
            __result = DiningSessionRegistry.TryAttach(pawn, job, meal);
        }
    }
}

[HarmonyPatch(typeof(Toils_Ingest), nameof(Toils_Ingest.ChewIngestible))]
internal static class PlateEatingSpeedPatch
{
    private static void Prefix(Pawn chewer, ref float durationMultiplier, TargetIndex ingestibleInd)
    {
        if (!DiningPawnPolicy.AppliesPlateEatingSpeed(chewer.RaceProps.Humanlike))
        {
            return;
        }

        var meal = chewer.CurJob?.GetTarget(ingestibleInd).Thing;
        var plate = (meal as ThingWithComps)?.GetComp<CompEmbeddedWare>()?.PeekPlateThing();
        if (plate is null)
        {
            return;
        }

        var speed = (plate as ThingWithComps)?.GetComp<CompKitchenwareStats>()?
            .CurrentStats.CookingSpeedFactor ?? 1f;
        durationMultiplier /= Math.Max(0.1f, speed);
    }
}

[HarmonyPatch(typeof(JobDriver_Ingest), "MakeNewToils")]
internal static class IngestCutleryToilsPatch
{
    private static void Postfix(JobDriver_Ingest __instance, ref IEnumerable<Toil> __result)
    {
        __result = AddCutleryPickup(__instance, __result);
    }

    internal static IEnumerable<Toil> AddCutleryPickup(
        JobDriver driver,
        IEnumerable<Toil> original)
    {
        var pawn = driver.GetActor();
        if (pawn.CurJob is { } plateJob && DiningSessionRegistry.HasPlatePickup(plateJob) &&
            DiningSessionRegistry.PlateFor(plateJob) is { } plate)
        {
            yield return GotoCapturedThing(plate, PathEndMode.Touch);
            yield return new Toil
            {
                initAction = () =>
                {
                    DiningSessionRegistry.PickupPlate(pawn);
                    if (pawn.CurJob?.GetTarget(TargetIndex.A).Thing is { } meal &&
                        MealCoveragePolicy.IsCovered(meal.def))
                    {
                        DiningSessionRegistry.BindReservedPlate(pawn, meal);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        if (pawn.CurJob is { } job && DiningSessionRegistry.HasPickup(job) &&
            DiningSessionRegistry.CutleryFor(job) is { } cutlery)
        {
            if (cutlery.Spawned)
            {
                yield return GotoCapturedThing(cutlery, PathEndMode.Touch);
            }

            yield return new Toil
            {
                initAction = () => DiningSessionRegistry.Pickup(pawn),
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        if (pawn.CurJob is { } microwaveJob && DiningSessionRegistry.MicrowaveFor(microwaveJob) is { } microwave)
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_Haul.StartCarryThing(
                TargetIndex.A,
                putRemainderInQueue: false,
                subtractNumTakenFromJobCount: false,
                failIfStackCountLessThanJobCount: false,
                reserve: false,
                canTakeFromInventory: true);
            yield return GotoCapturedThing(microwave.parent, PathEndMode.InteractionCell);
            yield return WaitAtCapturedThing(microwave.parent, microwave.HeatingTicks);
            yield return new Toil
            {
                initAction = () =>
                {
                    var carried = pawn.carryTracker.CarriedThing;
                    if (carried is not null)
                    {
                        microwave.TryReheat(carried);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return Toils_Haul.DropCarriedThing();
        }

        foreach (var toil in original)
        {
            yield return toil;
        }
    }

    private static Toil GotoCapturedThing(Thing target, PathEndMode pathEndMode)
    {
        var toil = ToilMaker.MakeToil("ImmersiveChefs_GotoReservedWare");
        toil.initAction = () => toil.actor.pather.StartPath(target, pathEndMode);
        toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
        toil.AddFailCondition(() => target.DestroyedOrNull() || !target.Spawned);
        return toil;
    }

    private static Toil WaitAtCapturedThing(Thing target, int duration)
    {
        var toil = Toils_General.Wait(duration);
        var originalTickAction = toil.tickAction;
        Effecter? progressEffecter = null;

        toil.debugName = "ImmersiveChefs_HeatAtCapturedMicrowave";
        toil.handlingFacing = true;
        toil.tickAction = () =>
        {
            originalTickAction?.Invoke();

            var actor = toil.actor;
            actor.rotationTracker.FaceTarget(target);
            if (actor.Faction != Faction.OfPlayer)
            {
                return;
            }

            progressEffecter ??= EffecterDefOf.ProgressBar.Spawn();
            progressEffecter.EffectTick(target, TargetInfo.Invalid);
            if (progressEffecter.children[0] is SubEffecter_ProgressBar progressBar)
            {
                var progress = 1f - (float)actor.jobs.curDriver.ticksLeftThisToil / duration;
                progressBar.mote.progress = Math.Max(0f, Math.Min(1f, progress));
                progressBar.mote.offsetZ = -0.5f;
            }
        };
        toil.AddFinishAction(() =>
        {
            progressEffecter?.Cleanup();
            progressEffecter = null;
        });
        toil.AddFailCondition(() => target.DestroyedOrNull() || !target.Spawned);
        return toil;
    }
}

[HarmonyPatch(typeof(JobDriver_FoodFeedPatient), nameof(JobDriver_FoodFeedPatient.TryMakePreToilReservations))]
internal static class FeedPatientReservationPatch
{
    private static void Postfix(JobDriver_FoodFeedPatient __instance, ref bool __result)
    {
        if (!__result)
        {
            return;
        }

        var feeder = __instance.GetActor();
        var job = feeder.CurJob;
        var patient = job?.GetTarget(TargetIndex.B).Pawn;
        var foodSource = job?.GetTarget(TargetIndex.A).Thing;
        if (job is not null && patient is not null && foodSource is not null)
        {
            __result = DiningSessionRegistry.TryAttachAssisted(feeder, patient, job, foodSource);
        }
    }
}

[HarmonyPatch(typeof(JobDriver_FoodFeedPatient), "MakeNewToils")]
internal static class FeedPatientCutleryToilsPatch
{
    private static void Postfix(JobDriver_FoodFeedPatient __instance, ref IEnumerable<Toil> __result)
    {
        __result = IngestCutleryToilsPatch.AddCutleryPickup(__instance, __result);
    }
}

[HarmonyPatch(typeof(Building_NutrientPasteDispenser), nameof(Building_NutrientPasteDispenser.TryDispenseFood))]
internal static class NutrientPastePlatePatch
{
    private static void Postfix(Building_NutrientPasteDispenser __instance, Thing? __result)
    {
        if (__result is null || __instance.Map is null)
        {
            return;
        }

        var pawn = __instance.Map.mapPawns.AllPawnsSpawned
            .Where(candidate => candidate.RaceProps.Humanlike &&
                                candidate.CurJob?.GetTarget(TargetIndex.A).Thing == __instance)
            .OrderBy(candidate => candidate.Position.DistanceToSquared(__instance.InteractionCell))
            .FirstOrDefault();
        if (pawn is not null)
        {
            DiningSessionRegistry.BindPastePlate(pawn, __result);
        }
    }
}

[HarmonyPatch(typeof(CompFoodPoisonable), nameof(CompFoodPoisonable.PostIngested))]
internal static class UnifiedFoodPoisoningPatch
{
    private static readonly AccessTools.FieldRef<CompFoodPoisonable, float> PoisonPercent =
        AccessTools.FieldRefAccess<CompFoodPoisonable, float>("poisonPct");

    private static void Prefix(CompFoodPoisonable __instance, Pawn ingester, out float __state)
    {
        __state = PoisonPercent(__instance);
        if (__instance.parent is not ThingWithComps meal ||
            !MealCoveragePolicy.IsCovered(meal.def) ||
            !DiningPawnPolicy.AppliesDiningConsequences(ingester.RaceProps.Humanlike))
        {
            return;
        }

        var record = meal.GetComp<CompCulinaryState>()?.PeekCurrentServing();
        if (record is null)
        {
            return;
        }

        var dining = DiningSessionRegistry.Current(ingester);
        var contamination = record.Contamination;
        contamination |= SanitationContamination.ForCutlery(
            dining?.CutleryWasDirty == true,
            dining?.CutleryWasWildWaterWashed == true
                ? WashProvenance.WildWater
                : WashProvenance.Safe);

        var embeddedPlate = meal.GetComp<CompEmbeddedWare>()?.PeekPlateThing();
        var servicePlate = dining?.Plate ?? embeddedPlate;
        var plateSanitation = (servicePlate as ThingWithComps)?.GetComp<CompSanitation>();
        contamination |= SanitationContamination.ForPlate(
            plateSanitation?.IsDirty == true,
            plateSanitation?.WashProvenance ?? WashProvenance.None);

        var settings = ImmersiveChefsMod.Settings;
        var band = settings.MealTemperatureEnabled
            ? ThermalCalculator.BandFor(record.TemperatureCelsius)
            : ThermalBand.RoomTemperature;
        PoisonPercent(__instance) = DiningOutcomeCalculator.FinalPoisonChance(new DiningRiskInputs(
            __state,
            settings.CulinaryQualityEnabled ? record.QualityScore : 50,
            band,
            contamination,
            servicePlate is { } plate
                ? KitchenwareRuntime.ServiceScore(plate)
                : null,
            dining?.CutleryServiceScore,
            record.MicrowaveReheatCount,
            settings.MicrowaveExtraPoisonChance,
            settings.FoodPoisoningEffectScale,
            settings.MaximumCustomPoisonChance));
    }

    private static void Postfix(CompFoodPoisonable __instance, float __state)
    {
        PoisonPercent(__instance) = __state;
    }
}
