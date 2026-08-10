using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.toxic-kitchenware-no-dose",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 24_000,
    MaxWallClockSeconds = 280)]
public sealed class ToxicKitchenwareNoDoseDiningTest : IRimWorldEndToEndTest
{
    private const string ObserverHarmonyId =
        "fumblesneeze.immersivechefs.e2e.toxic-kitchenware-no-dose-observer";

    private Map map = null!;
    private IntVec3 center;
    private Pawn diner = null!;
    private ThingWithComps abortedMeal = null!;
    private ThingWithComps abortedCutlery = null!;
    private ThingWithComps legacyMeal = null!;
    private ThingWithComps legacyCutlery = null!;
    private ThingWithComps steelMeal = null!;
    private ThingWithComps steelPlate = null!;
    private ThingWithComps steelCutlery = null!;
    private Pawn? animal;
    private ThingWithComps? animalMeal;

    public void Arrange(IEndToEndContext context)
    {
        var previousExposureScale = ImmersiveChefsMod.Settings.ToxicKitchenwareExposureScale;
        var previousTemperature = ImmersiveChefsMod.Settings.MealTemperatureEnabled;
        context.DeferCleanup(() =>
        {
            ImmersiveChefsMod.Settings.ToxicKitchenwareExposureScale = previousExposureScale;
            ImmersiveChefsMod.Settings.MealTemperatureEnabled = previousTemperature;
        });
        ImmersiveChefsMod.Settings.ToxicKitchenwareExposureScale = 1f;
        ImmersiveChefsMod.Settings.MealTemperatureEnabled = false;

        map = Current.Game.CurrentMap;
        center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);

        diner = FoodSearchE2EFixture.CreateColonist("Toxic ware control diner");
        FoodSearchE2EFixture.SetHunger(diner, 0.95f);
        GenSpawn.Spawn(diner, center + (IntVec3.West * 3), map);
        diner.drafter.Drafted = true;

        abortedMeal = MakePlatedMeal(
            KitchenMaterialKind.Uranium,
            ThingDefOf.Uranium,
            center + (IntVec3.East * 3));
        abortedCutlery = SpawnCutlery(ThingDefOf.Uranium, center);

        legacyMeal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        legacyMeal.stackCount = 1;
        GenSpawn.Spawn(legacyMeal, center + new IntVec3(2, 0, -2), map);
        legacyCutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);

        steelMeal = MakeUnspawnedPlatedMeal(KitchenMaterialKind.Steel, ThingDefOf.Steel);
        steelPlate = (ThingWithComps)steelMeal.GetComp<CompEmbeddedWare>()!.PeekPlateThing()!;
        steelCutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);

        NoDoseObserver.Reset(diner);
        var target = AccessTools.Method(
            typeof(HealthUtility),
            nameof(HealthUtility.AdjustSeverity),
            new[] { typeof(Pawn), typeof(HediffDef), typeof(float) });
        EndToEndAssert.NotNull(target,
            "The exact vanilla toxic-severity application seam must remain available.");
        var observer = new Harmony(ObserverHarmonyId);
        observer.Patch(
            target,
            prefix: new HarmonyMethod(typeof(NoDoseObserver), nameof(NoDoseObserver.Prefix)));
        context.DeferCleanup(() =>
        {
            observer.Unpatch(target, HarmonyPatchType.Prefix, ObserverHarmonyId);
            NoDoseObserver.Reset(null);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before no-dose controls",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the no-dose control diner",
            new[] { diner.ThingID },
            additive: false);

        var draftToggle = RequiredDraftToggle(context);
        yield return new GizmoActionStep(
            "undraft for the explicit aborted ingest order",
            new[] { diner.ThingID },
            draftToggle.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draftToggle.StableId);
        yield return new AssertionStep(
            "activate hunger for the abort control only",
            _ => FoodSearchE2EFixture.SetHunger(diner, 0.20f));

        var abortConsume = EnabledConsume(context, diner, abortedMeal);
        yield return new FloatMenuActionStep(
            "order native consumption from an all-uranium setting",
            diner.ThingID,
            abortedMeal.ThingID,
            abortConsume);
        yield return new AssertionStep(
            "native order begins before any nutrition is ingested",
            _ => EndToEndAssert.Equal(
                JobDefOf.Ingest,
                diner.CurJobDef,
                "The aborted control must begin RimWorld's native Ingest job."));
        draftToggle = RequiredDraftToggle(context);
        yield return new SelectionActionStep(
            "keep the abort-control diner selected",
            new[] { diner.ThingID },
            additive: false);
        yield return new GizmoActionStep(
            "draft through the native toggle to interrupt ingestion",
            new[] { diner.ThingID },
            draftToggle.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draftToggle.StableId);
        yield return new WaitUntilStep(
            "the drafted pawn abandons ingestion without a toxic dose",
            _ => diner.CurJobDef != JobDefOf.Ingest &&
                 !abortedMeal.Destroyed &&
                 NoDoseObserver.Doses.Count == 0,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        yield return new CameraActionStep(
            "frame the still-intact aborted uranium meal",
            new[] { diner.ThingID, abortedMeal.ThingID },
            180);
        yield return new ScreenshotStep(
            "observe drafted interruption leaving the uranium meal intact",
            Array.Empty<string>(),
            0);
        yield return new AssertionStep(
            "remove only abort-control fixtures before the next case",
            _ => RemoveAbortedFixtures());

        draftToggle = RequiredDraftToggle(context);
        yield return new GizmoActionStep(
            "undraft for legacy unplated ingestion",
            new[] { diner.ThingID },
            draftToggle.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draftToggle.StableId);
        yield return new AssertionStep(
            "activate the legacy meal and its non-toxic cutlery",
            _ =>
            {
                FoodSearchE2EFixture.SetHunger(diner, 0.20f);
                GenSpawn.Spawn(legacyCutlery, center, map);
            });
        yield return new SelectionActionStep(
            "select the legacy unplated meal before eating",
            new[] { legacyMeal.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe the imported legacy meal with no bound plate",
            Array.Empty<string>(),
            0);
        var legacyConsume = EnabledConsume(context, diner, legacyMeal);
        yield return new FloatMenuActionStep(
            "order native legacy meal consumption",
            diner.ThingID,
            legacyMeal.ThingID,
            legacyConsume);
        yield return new TimeControlActionStep(
            "run native legacy ingestion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "legacy ingestion completes without toxic buildup",
            _ => legacyMeal.Destroyed &&
                 NoDoseObserver.Doses.Count == 0 &&
                 !HasToxicBuildup(diner),
            new EndToEndDeadline(1_500, 5_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause after legacy ingestion",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep(
            "observe the diner after toxicity-free legacy ingestion",
            new[] { diner.ThingID },
            180);
        yield return new AssertionStep(
            "remove the returned legacy cutlery before the steel case",
            _ => DestroyIfPresent(legacyCutlery));

        yield return new AssertionStep(
            "activate the explicit all-steel setting",
            _ =>
            {
                FoodSearchE2EFixture.SetHunger(diner, 0.20f);
                GenSpawn.Spawn(steelMeal, center + (IntVec3.East * 3), map);
                GenSpawn.Spawn(steelCutlery, center, map);
            });
        var steelConsume = EnabledConsume(context, diner, steelMeal);
        yield return new FloatMenuActionStep(
            "order native consumption from the all-steel setting",
            diner.ThingID,
            steelMeal.ThingID,
            steelConsume);
        yield return new TimeControlActionStep(
            "run native all-steel ingestion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "all-steel ingestion returns its setting without toxic buildup",
            _ => steelMeal.Destroyed &&
                 steelPlate.Spawned &&
                 steelCutlery.Spawned &&
                 NoDoseObserver.Doses.Count == 0 &&
                 !HasToxicBuildup(diner),
            new EndToEndDeadline(1_500, 5_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause after all-steel ingestion",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the returned all-steel setting",
            new[] { steelPlate.ThingID, steelCutlery.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the returned all-steel setting",
            new[] { steelPlate.ThingID, steelCutlery.ThingID },
            180);
        yield return new ScreenshotStep(
            "observe ordinary steel dining without toxic buildup",
            Array.Empty<string>(),
            0);

        yield return new AssertionStep(
            "activate a hungry animal with an uranium-plated meal",
            _ => ActivateAnimalCase());
        yield return new TimeControlActionStep(
            "run native animal food seeking",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the animal begins its own native ingest job",
            _ => animal?.CurJobDef == JobDefOf.Ingest && animalMeal is { Destroyed: false },
            new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(45)));
        yield return new SelectionActionStep(
            "select the animal during native meal ingestion",
            new[] { animal!.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the animal and uranium-plated meal",
            new[] { animal.ThingID, animalMeal!.ThingID },
            180);
        yield return new ScreenshotStep(
            "observe an animal eating without kitchenware exposure",
            Array.Empty<string>(),
            0);
        yield return new WaitUntilStep(
            "animal ingestion completes without toxic buildup",
            _ => animalMeal!.Destroyed &&
                 NoDoseObserver.Doses.Count == 0 &&
                 !HasToxicBuildup(animal!),
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(70)));
        yield return new TimeControlActionStep(
            "pause after animal no-dose control",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "all native exclusion controls applied no material dose",
            _ =>
            {
                EndToEndAssert.Equal(0, NoDoseObserver.Doses.Count,
                    "Abort, legacy, non-toxic, and animal native paths must apply no toxic-ware dose.");
                EndToEndAssert.False(HasToxicBuildup(diner),
                    "The human no-dose controls must not create vanilla ToxicBuildup.");
                EndToEndAssert.False(HasToxicBuildup(animal!),
                    "Animal ingestion must not create vanilla ToxicBuildup.");
            });
        yield return new CheckpointStep(
            "toxic kitchenware no-dose controls",
            _ => new Dictionary<string, string>
            {
                ["abortedFixtureRemovedAfterIntactProof"] = abortedMeal.Destroyed.ToString(),
                ["legacyMealConsumed"] = legacyMeal.Destroyed.ToString(),
                ["steelMealConsumed"] = steelMeal.Destroyed.ToString(),
                ["animalMealConsumed"] = animalMeal!.Destroyed.ToString(),
                ["positiveToxicDoseCount"] = NoDoseObserver.Doses.Count.ToString()
            });
    }

    private string EnabledConsume(IEndToEndContext context, Pawn pawn, Thing meal)
    {
        var candidates = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(pawn.ThingID, meal.ThingID)
            .Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, candidates.Length,
            $"Expected one enabled native Consume option for {meal.ThingID}.");
        return candidates[0].StableId;
    }

    private EndToEndGizmoOption RequiredDraftToggle(IEndToEndContext context)
    {
        var candidates = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { diner.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Toggle &&
                string.Equals(option.HotKeyDefName, "Command_ColonistDraft", StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(1, candidates.Length,
            "The selected control colonist must expose one current native Draft toggle.");
        return candidates[0];
    }

    private ThingWithComps MakePlatedMeal(
        KitchenMaterialKind cookwareMaterial,
        ThingDef plateStuff,
        IntVec3 position)
    {
        var meal = MakeUnspawnedPlatedMeal(cookwareMaterial, plateStuff);
        GenSpawn.Spawn(meal, position, map);
        return meal;
    }

    private static ThingWithComps MakeUnspawnedPlatedMeal(
        KitchenMaterialKind cookwareMaterial,
        ThingDef plateStuff)
    {
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        meal.stackCount = 1;
        meal.GetComp<CompCulinaryState>()!.ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                qualityScore: 55,
                temperatureCelsius: 21f,
                contamination: ContaminationSources.None,
                microwaveReheatCount: 0,
                lastThermalTick: 0,
                cookwareMaterial: cookwareMaterial)
        });
        var plate = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Plate", plateStuff);
        EndToEndAssert.True(
            meal.GetComp<CompEmbeddedWare>()!.TryEmbedPlate(plate),
            "The no-dose fixture must bind its exact plate.");
        return meal;
    }

    private ThingWithComps SpawnCutlery(ThingDef stuff, IntVec3 position)
    {
        var cutlery = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Cutlery", stuff);
        GenSpawn.Spawn(cutlery, position, map);
        return cutlery;
    }

    private void RemoveAbortedFixtures()
    {
        var plate = abortedMeal.GetComp<CompEmbeddedWare>()?.ReleasePlateThing();
        DestroyIfPresent(plate);
        DestroyIfPresent(abortedMeal);
        DestroyIfPresent(abortedCutlery);
        FoodSearchE2EFixture.SetHunger(diner, 0.95f);
    }

    private void ActivateAnimalCase()
    {
        DestroyIfPresent(steelPlate);
        DestroyIfPresent(steelCutlery);
        FoodSearchE2EFixture.SetHunger(diner, 0.95f);
        diner.drafter.Drafted = true;

        animal = PawnGenerator.GeneratePawn(
            DefDatabase<PawnKindDef>.GetNamed("Muffalo"),
            faction: null);
        GenSpawn.Spawn(animal, center + (IntVec3.West * 2), map);
        animal.needs.food.CurLevelPercentage = 0.03f;
        NoDoseObserver.AddTarget(animal);

        animalMeal = MakePlatedMeal(
            KitchenMaterialKind.Uranium,
            ThingDefOf.Uranium,
            center + (IntVec3.East * 2));
    }

    private static bool HasToxicBuildup(Pawn pawn) =>
        pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup) is not null;

    private static void DestroyIfPresent(Thing? thing)
    {
        if (thing is { Destroyed: false })
        {
            thing.Destroy(DestroyMode.Vanish);
        }
    }

    private static class NoDoseObserver
    {
        private static readonly HashSet<Pawn> Targets = new();

        internal static List<float> Doses { get; } = new();

        internal static void Reset(Pawn? pawn)
        {
            Targets.Clear();
            if (pawn is not null)
            {
                Targets.Add(pawn);
            }
            Doses.Clear();
        }

        internal static void AddTarget(Pawn pawn) => Targets.Add(pawn);

        public static void Prefix(Pawn __0, HediffDef __1, float __2)
        {
            if (Targets.Contains(__0) &&
                ReferenceEquals(__1, HediffDefOf.ToxicBuildup) &&
                __2 > 0f)
            {
                Doses.Add(__2);
            }
        }
    }
}
