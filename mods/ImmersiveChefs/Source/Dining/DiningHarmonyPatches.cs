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
                serving.AddContamination(SanitationContamination.ForSilverware(
                    dining?.SilverwareWasDirty == true,
                    dining?.SilverwareWasWildWaterWashed == true
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
internal static class IngestSilverwareToilsPatch
{
    private static void Postfix(JobDriver_Ingest __instance, ref IEnumerable<Toil> __result)
    {
        __result = AddSilverwarePickup(__instance, __result);
    }

    private static IEnumerable<Toil> AddSilverwarePickup(
        JobDriver_Ingest driver,
        IEnumerable<Toil> original)
    {
        var pawn = driver.GetActor();
        var originalTargetC = pawn.CurJob?.GetTarget(TargetIndex.C) ?? LocalTargetInfo.Invalid;
        if (pawn.CurJob is { } plateJob && DiningSessionRegistry.HasPlatePickup(plateJob) &&
            DiningSessionRegistry.PlateFor(plateJob) is { } plate)
        {
            yield return new Toil
            {
                initAction = () => pawn.CurJob?.SetTarget(TargetIndex.C, plate),
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.Touch);
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
            DiningSessionRegistry.SilverwareFor(job) is { } silverware)
        {
            yield return new Toil
            {
                initAction = () => pawn.CurJob?.SetTarget(TargetIndex.C, silverware),
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.Touch);
            yield return new Toil
            {
                initAction = () => DiningSessionRegistry.Pickup(pawn),
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        if (pawn.CurJob is { } microwaveJob && DiningSessionRegistry.MicrowaveFor(microwaveJob) is { } microwave)
        {
            yield return new Toil
            {
                initAction = () => pawn.CurJob?.SetTarget(TargetIndex.C, microwave.parent),
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_Haul.StartCarryThing(
                TargetIndex.A,
                putRemainderInQueue: false,
                subtractNumTakenFromJobCount: false,
                failIfStackCountLessThanJobCount: false,
                reserve: false,
                canTakeFromInventory: true);
            yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.InteractionCell);
            yield return Toils_General.Wait(microwave.HeatingTicks, TargetIndex.C)
                .WithProgressBarToilDelay(TargetIndex.C);
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

        if (pawn.CurJob is { } restoreJob)
        {
            yield return new Toil
            {
                initAction = () => restoreJob.SetTarget(TargetIndex.C, originalTargetC),
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        foreach (var toil in original)
        {
            yield return toil;
        }
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
        contamination |= SanitationContamination.ForSilverware(
            dining?.SilverwareWasDirty == true,
            dining?.SilverwareWasWildWaterWashed == true
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
            dining?.SilverwareServiceScore,
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
