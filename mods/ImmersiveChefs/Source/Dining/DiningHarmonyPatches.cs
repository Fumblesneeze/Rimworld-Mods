using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

[HarmonyPatch(typeof(Building), nameof(Building.MaxItemsInCell), MethodType.Getter)]
internal static class DiningSurfaceItemCapacityPatch
{
    private static void Postfix(Building __instance, ref int __result)
    {
        if (__result < 2 && __instance.def.surfaceType == SurfaceType.Eat)
        {
            __result = 2;
        }
    }
}

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
        internal KitchenMaterialKind PlateMaterial { get; set; } = KitchenMaterialKind.OtherMetal;
        internal KitchenMaterialKind CutleryMaterial { get; set; } = KitchenMaterialKind.OtherMetal;
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
        snapshot.PlateMaterial = KitchenwareRuntime.Describe(
            snapshot.EmbeddedWare?.PeekPlateThing())?.Material ?? KitchenMaterialKind.OtherMetal;
        snapshot.CutleryMaterial = KitchenwareRuntime.Describe(
            DiningSessionRegistry.Current(ingester)?.Cutlery)?.Material ?? KitchenMaterialKind.OtherMetal;
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

                var plateMaterial = dining?.PlateServiceSnapshot?.Material ?? __state.PlateMaterial;
                var cutleryMaterial = KitchenwareRuntime.Describe(dining?.Cutlery)?.Material ??
                                      __state.CutleryMaterial;
                ToxicKitchenwareExposureRuntime.Apply(
                    ingester,
                    serving.CookwareMaterial,
                    plateMaterial,
                    cutleryMaterial,
                    __result);

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
            __result = DiningReservationSafety.KeepNativeResult(
                __result,
                DiningSessionRegistry.TryAttach(pawn, job, meal));
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
        if (meal is not ThingWithComps mealWithComps)
        {
            return;
        }

        var plate = mealWithComps.GetComp<CompEmbeddedWare>()?.PeekPlateThing();
        if (plate is not null)
        {
            var speed = (plate as ThingWithComps)?.GetComp<CompKitchenwareStats>()?
                .CurrentStats.CookingSpeedFactor ?? 1f;
            durationMultiplier /= Math.Max(0.1f, speed);
        }
    }

    private static void Postfix(Pawn chewer, TargetIndex ingestibleInd, Toil __result)
    {
        if (!DiningPawnPolicy.AppliesPlateEatingSpeed(chewer.RaceProps.Humanlike))
        {
            return;
        }

        var originalInitAction = __result.initAction;
        __result.initAction = () =>
        {
            originalInitAction?.Invoke();
            ApplyCurrentTemperatureDuration(chewer, ingestibleInd);
        };
    }

    private static void ApplyCurrentTemperatureDuration(Pawn chewer, TargetIndex ingestibleInd)
    {
        var meal = chewer.CurJob?.GetTarget(ingestibleInd).Thing as ThingWithComps;
        if (meal is null)
        {
            return;
        }

        if (TemperatureOwnership.ImmersiveChefsFeaturesActive &&
            ImmersiveChefsMod.Settings.MealTemperatureEnabled &&
            meal.GetComp<CompCulinaryState>()?.PeekCurrentServing() is { } serving)
        {
            var multiplier = ThermalCalculator.EatingDurationMultiplier(
                ThermalCalculator.BandFor(serving.TemperatureCelsius));
            if (multiplier > 1f && chewer.jobs.curDriver is { } driver)
            {
                driver.ticksLeftThisToil = Math.Max(
                    1,
                    (int)Math.Round(
                        driver.ticksLeftThisToil * multiplier,
                        MidpointRounding.AwayFromZero));
            }
        }
    }
}

internal enum DiningMealPickupPlan
{
    ApproachAndCarry,
    ReheatAfterVanillaInventoryTransfer,
    AlreadyCarried,
    SkipOptionalReheat
}

internal static class DiningMealPickupPolicy
{
    internal static DiningMealPickupPlan For(
        bool spawned,
        bool heldInCarrierInventory,
        bool alreadyCarried,
        bool nativeToilsStartWithInventoryTransfer)
    {
        if (alreadyCarried)
        {
            return DiningMealPickupPlan.AlreadyCarried;
        }

        if (heldInCarrierInventory)
        {
            return nativeToilsStartWithInventoryTransfer
                ? DiningMealPickupPlan.ReheatAfterVanillaInventoryTransfer
                : DiningMealPickupPlan.SkipOptionalReheat;
        }

        return spawned
            ? DiningMealPickupPlan.ApproachAndCarry
            : DiningMealPickupPlan.SkipOptionalReheat;
    }
}

internal static class AssistedFeedingMicrowavePolicy
{
    internal static bool ShouldReserve(
        bool pasteDispenser,
        bool mealHeldInFeederInventory) =>
        !pasteDispenser && !mealHeldInFeederInventory;
}

internal static class DiningMealToilOrder
{
    internal static IEnumerable<T> InsertAfterInventoryTransfer<T>(
        IEnumerable<T> nativeToils,
        IEnumerable<T> reheatToils)
    {
        using var nativeEnumerator = nativeToils.GetEnumerator();
        if (!nativeEnumerator.MoveNext())
        {
            yield break;
        }

        yield return nativeEnumerator.Current;
        foreach (var toil in reheatToils)
        {
            yield return toil;
        }

        while (nativeEnumerator.MoveNext())
        {
            yield return nativeEnumerator.Current;
        }
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

        if (pawn.CurJob is { } heatingJob &&
            DiningSessionRegistry.HeatingSourceFor(heatingJob) is { } heatingSource)
        {
            var meal = heatingJob.GetTarget(TargetIndex.A).Thing;
            var pickupPlan = DiningMealPickupPolicy.For(
                meal?.Spawned == true,
                meal is not null &&
                ReferenceEquals(meal.holdingOwner, pawn.inventory?.innerContainer),
                meal is not null && ReferenceEquals(meal, pawn.carryTracker?.CarriedThing),
                driver is JobDriver_Ingest);
            if (pickupPlan == DiningMealPickupPlan.ReheatAfterVanillaInventoryTransfer)
            {
                // JobDriver_Ingest latched eatingFromInventory when this job started.
                // Its first native toil performs the inventory-to-carrier transfer. Run
                // that toil before walking to the selected heat source, then leave the heated meal
                // in the carrier for the remaining native ingest toils.
                foreach (var toil in DiningMealToilOrder.InsertAfterInventoryTransfer(
                             original,
                             HeatCarriedMeal(pawn, heatingSource, dropAfterHeating: false)))
                {
                    yield return toil;
                }
            }
            else
            {
                if (pickupPlan != DiningMealPickupPlan.SkipOptionalReheat)
                {
                    if (pickupPlan == DiningMealPickupPlan.ApproachAndCarry)
                    {
                        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
                        yield return Toils_Haul.StartCarryThing(
                            TargetIndex.A,
                            putRemainderInQueue: false,
                            subtractNumTakenFromJobCount: false,
                            failIfStackCountLessThanJobCount: false,
                            reserve: false,
                            canTakeFromInventory: false);
                    }

                    foreach (var toil in HeatCarriedMeal(pawn, heatingSource, dropAfterHeating: true))
                    {
                        yield return toil;
                    }
                }

                foreach (var toil in original)
                {
                    yield return toil;
                }
            }
        }
        else
        {
            foreach (var toil in original)
            {
                yield return toil;
            }
        }

        yield return new Toil
        {
            initAction = () => CommonSenseAdapter.TryQueueCommittedHandoff(pawn, driver.job),
            defaultCompleteMode = ToilCompleteMode.Instant
        };
    }

    private static IEnumerable<Toil> HeatCarriedMeal(
        Pawn pawn,
        MealHeatingSource heatingSource,
        bool dropAfterHeating)
    {
        var interrupted = false;
        yield return GotoOptionalHeatingSource(
            heatingSource,
            onInterrupted: () => interrupted = true);
        yield return WaitAtOptionalHeatingSource(
            heatingSource,
            onInterrupted: () => interrupted = true);
        yield return new Toil
        {
            initAction = () =>
            {
                var carried = pawn.carryTracker?.CarriedThing;
                if (carried is not null &&
                    MealHeatingPolicy.CanApplyCompletedCycle(
                        interrupted,
                        heatingSource.IsOperational))
                {
                    heatingSource.TryHeat(carried);
                }
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };

        if (dropAfterHeating)
        {
            yield return Toils_Haul.DropCarriedThing();
        }
    }

    private static Toil GotoCapturedThing(
        Thing target,
        PathEndMode pathEndMode,
        Func<bool>? additionalFailCondition = null)
    {
        var toil = ToilMaker.MakeToil("ImmersiveChefs_GotoReservedWare");
        toil.initAction = () => toil.actor.pather.StartPath(target, pathEndMode);
        toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
        toil.AddFailCondition(() =>
            target.DestroyedOrNull() ||
            !target.Spawned ||
            additionalFailCondition?.Invoke() == true);
        return toil;
    }

    internal static Toil GotoOptionalHeatingSource(
        MealHeatingSource source,
        bool normalDeliveryFallback = false,
        Action? onInterrupted = null)
    {
        var toil = ToilMaker.MakeToil("ImmersiveChefs_GotoOptionalHeatingSource");
        toil.initAction = () =>
        {
            if (!source.IsOperational)
            {
                HandleUnavailableSource(
                    toil,
                    source,
                    normalDeliveryFallback,
                    onInterrupted: onInterrupted);
                return;
            }

            toil.actor.pather.StartPath(source.Thing, source.PathEndMode);
        };
        toil.tickAction = () =>
        {
            if (source.IsOperational)
            {
                return;
            }

            toil.actor.pather.StopDead();
            HandleUnavailableSource(
                toil,
                source,
                normalDeliveryFallback,
                onInterrupted: onInterrupted);
        };
        toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
        return toil;
    }

    internal static Toil WaitAtOptionalHeatingSource(
        MealHeatingSource source,
        bool normalDeliveryFallback = false,
        Action? onInterrupted = null)
    {
        var target = source.Thing;
        var duration = source.Profile.HeatingTicks;
        var toil = Toils_General.Wait(duration);
        var originalInitAction = toil.initAction;
        var originalTickAction = toil.tickAction;
        Effecter? progressEffecter = null;
        var microwaveUse = source.Kind == MealHeatingSourceKind.Microwave
            ? target.MapHeld?.physicalInteractionReservationManager
            : null;
        Job? heatingJob = null;

        void TryBeginMicrowaveUse()
        {
            var actor = toil.actor;
            if (microwaveUse?.FirstReserverOf(target) is { } user && user != actor)
            {
                return;
            }

            microwaveUse?.Reserve(actor, heatingJob, target);
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            actor.jobs.curDriver.ticksLeftThisToil = duration;
        }

        toil.debugName = "ImmersiveChefs_HeatAtCapturedSource";
        toil.handlingFacing = true;
        toil.initAction = () =>
        {
            originalInitAction?.Invoke();
            if (!source.IsOperational)
            {
                HandleUnavailableSource(
                    toil,
                    source,
                    normalDeliveryFallback,
                    completeDelayNormally: true,
                    onInterrupted: onInterrupted);
            }
            else if (microwaveUse is not null)
            {
                heatingJob = toil.actor.CurJob;
                // Approach never books the appliance. Only a pawn physically at the
                // heating toil acquires use; waiting consumes none of its cycle.
                toil.defaultCompleteMode = ToilCompleteMode.Never;
                TryBeginMicrowaveUse();
            }
        };
        toil.tickAction = () =>
        {
            var actor = toil.actor;
            if (!source.IsOperational)
            {
                toil.defaultCompleteMode = ToilCompleteMode.Delay;
                HandleUnavailableSource(
                    toil,
                    source,
                    normalDeliveryFallback,
                    completeDelayNormally: true,
                    onInterrupted: onInterrupted);
                return;
            }

            if (microwaveUse is not null && toil.defaultCompleteMode == ToilCompleteMode.Never)
            {
                heatingJob ??= actor.CurJob;
                TryBeginMicrowaveUse();
                actor.rotationTracker.FaceTarget(target);
                return;
            }

            originalTickAction?.Invoke();

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
            if (heatingJob is not null)
            {
                microwaveUse?.TryRelease(toil.actor, heatingJob, target);
            }
            progressEffecter?.Cleanup();
            progressEffecter = null;
        });
        return toil;
    }

    private static void HandleUnavailableSource(
        Toil toil,
        MealHeatingSource source,
        bool normalDeliveryFallback,
        bool completeDelayNormally = false,
        Action? onInterrupted = null)
    {
        var actor = toil.actor;
        onInterrupted?.Invoke();
        if (MealHeatingPolicy.ShouldCancelWhenUnavailable(source.Kind, normalDeliveryFallback))
        {
            actor.jobs.EndCurrentJob(JobCondition.Incompletable);
            return;
        }

        if (completeDelayNormally)
        {
            actor.jobs.curDriver.ticksLeftThisToil = 1;
            return;
        }

        actor.jobs.curDriver.ReadyForNextToil();
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
            __result = DiningReservationSafety.KeepNativeResult(
                __result,
                DiningSessionRegistry.TryAttachAssisted(feeder, patient, job, foodSource));
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

        var pawn = DispenserMealBindingRuntime.FindCurrentGetter(__instance);
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

    private static void Prefix(CompFoodPoisonable __instance, Pawn ingester, out PoisoningPatchState __state)
    {
        var originalPoisonPercent = PoisonPercent(__instance);
        var previousAttribution = FoodPoisonAttributionContext.Replace(FoodPoisonRiskContributor.None);
        __state = new PoisoningPatchState(originalPoisonPercent, previousAttribution);
        try
        {
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
            var ownsTemperature = TemperatureOwnership.ImmersiveChefsFeaturesActive &&
                                  settings.MealTemperatureEnabled;
            var band = ownsTemperature
                ? ThermalCalculator.BandFor(record.TemperatureCelsius)
                : ThermalBand.RoomTemperature;
            var risk = DiningOutcomeCalculator.CalculatePoisonRisk(new DiningRiskInputs(
                originalPoisonPercent,
                settings.CulinaryQualityEnabled ? record.QualityScore : 50,
                band,
                contamination,
                servicePlate is { } plate
                    ? KitchenwareRuntime.ServiceScore(plate)
                    : null,
                dining?.CutleryServiceScore,
                ownsTemperature ? record.MicrowaveReheatCount : 0,
                settings.MicrowaveExtraPoisonChance,
                settings.FoodPoisoningEffectScale,
                settings.MaximumCustomPoisonChance));
            PoisonPercent(__instance) = risk.FinalChance;
            FoodPoisonAttributionContext.Replace(risk.LargestPositiveContributor);
        }
        catch
        {
            PoisonPercent(__instance) = originalPoisonPercent;
            FoodPoisonAttributionContext.Replace(previousAttribution);
            throw;
        }
    }

    private static Exception? Finalizer(
        CompFoodPoisonable __instance,
        PoisoningPatchState __state,
        Exception? __exception)
    {
        PoisonPercent(__instance) = __state.OriginalPoisonPercent;
        FoodPoisonAttributionContext.Replace(__state.PreviousAttribution);
        return __exception;
    }

    private readonly struct PoisoningPatchState
    {
        internal PoisoningPatchState(
            float originalPoisonPercent,
            FoodPoisonRiskContributor previousAttribution)
        {
            OriginalPoisonPercent = originalPoisonPercent;
            PreviousAttribution = previousAttribution;
        }

        internal float OriginalPoisonPercent { get; }
        internal FoodPoisonRiskContributor PreviousAttribution { get; }
    }
}
