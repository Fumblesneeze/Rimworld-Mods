using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.culinary-state-save-load-outcomes",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.immersivechefs",
    MaxFrames = 5_400,
    MaxGameTicks = 18_000,
    MaxWallClockSeconds = 210)]
public sealed class CulinaryStateOutcomePersistenceTest : IRimWorldEndToEndTest
{
    private const string SaveName = "ImmersiveChefsCulinaryOutcomePersistence";
    private Map map = null!;
    private Pawn diner = null!;
    private ThingWithComps meal = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private CulinaryServingSnapshot expectedServing;
    private string dinerId = string.Empty;
    private string mealId = string.Empty;
    private string plateId = string.Empty;
    private string cutleryId = string.Empty;
    private HashSet<Message> initialLiveMessages = null!;
    private string dinerLabel = string.Empty;
    private string mealLabel = string.Empty;

    public void Arrange(IEndToEndContext context)
    {
        PreserveSettings(context);
        InstallLocalizedPoisonInspectLeakProbe(context);
        initialLiveMessages = LiveMessages().ToHashSet();
        var savePath = GenFilePaths.FilePathForSavedGame(SaveName);
        context.DeferCleanup(() =>
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
        });

        map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);

        diner = FoodSearchE2EFixture.CreateColonist("Culinary outcome diner");
        FoodSearchE2EFixture.SetHunger(diner, 0.95f);
        GenSpawn.Spawn(diner, center + (IntVec3.West * 3), map);

        meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        meal.stackCount = 1;
        var serving = new CulinaryServingRecord(
            qualityScore: 72,
            temperatureCelsius: -20f,
            contamination: ContaminationSources.DirtyCookware |
                           ContaminationSources.DirtyPlate,
            microwaveReheatCount: 2,
            lastThermalTick: Math.Max(1, Find.TickManager.TicksGame),
            hiddenSourceDefNames: new[] { "RawRice" },
            hiddenDietaryFlags: DietaryFlags.Plant | DietaryFlags.VegetarianCompatible);
        expectedServing = serving.Capture();
        meal.GetComp<CompCulinaryState>()!.ReplaceServings(new[] { serving });
        meal.GetComp<CompIngredients>()!.RegisterIngredient(
            DefDatabase<ThingDef>.GetNamed("RawRice"));

        plate = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Plate", ThingDefOf.WoodLog);
        plate.GetComp<CompSanitation>()!.MarkClean(WashProvenance.WildWater);
        plate.GetComp<CompSanitation>()!.MarkDirty();
        EndToEndAssert.True(
            meal.GetComp<CompEmbeddedWare>()!.TryEmbedPlate(plate),
            "The saved outcome fixture must embed its exact dirty plate once.");
        cutlery = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Cutlery", ThingDefOf.WoodLog);

        GenSpawn.Spawn(cutlery, center, map);
        GenSpawn.Spawn(meal, center + (IntVec3.East * 3), map);
        dinerId = diner.ThingID;
        mealId = meal.ThingID;
        plateId = plate.ThingID;
        cutleryId = cutlery.ThingID;
        dinerLabel = diner.LabelShort;
        mealLabel = meal.LabelCapNoCount;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause the seeded culinary serving before save",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the plated culinary serving before save",
            new[] { mealId },
            additive: false);
        yield return new CameraActionStep(
            "frame the culinary save-load fixture",
            new[] { dinerId, mealId, cutleryId },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "observe private poison and wash provenance hidden before ingestion",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "ordinary inspection hides poison and wild-water state",
            _ =>
            {
                LocalizedPoisonInspectLeakProbe.InvocationCount = 0;
                var mealInspect = meal.GetInspectString();
                var plateInspect = plate.GetComp<CompSanitation>()!.CompInspectStringExtra();
                EndToEndAssert.True(
                    LocalizedPoisonInspectLeakProbe.InvocationCount > 0,
                    "The exact food-poison component probe must attempt a localized inspect contribution.");
                EndToEndAssert.True(
                    mealInspect.IndexOf(LocalizedPoisonInspectLeakProbe.Sentinel, StringComparison.Ordinal) < 0,
                    "Covered meal inspection must structurally suppress the food-poison component regardless of locale.");
                EndToEndAssert.True(
                    plateInspect.IndexOf("wild-water", StringComparison.OrdinalIgnoreCase) < 0,
                    "Ordinary plate inspection must not expose wild-water wash provenance.");
                EndToEndAssert.Equal(WashProvenance.WildWater,
                    plate.GetComp<CompSanitation>()!.WashProvenance,
                    "Hiding provenance must not alter its internal sanitation state.");
                AssertPersistedState();
            });
        yield return new SaveLoadActionStep(
            "save and load culinary state through RimWorld",
            SaveName);
        yield return new AssertionStep(
            "resolve the same serving and ware after load",
            _ =>
            {
                ResolveLoadedThings();
                AssertPersistedState();
            });
        yield return new SelectionActionStep(
            "select the same plated culinary serving after load",
            new[] { mealId },
            additive: false);
        yield return new CameraActionStep(
            "frame the same culinary fixture after load",
            new[] { dinerId, mealId, cutleryId },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "observe frozen excellent dirty-plated serving after load",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "activate hunger only for the explicit loaded-serving order",
            _ => FoodSearchE2EFixture.SetHunger(diner, 0.20f));

        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(dinerId, mealId);
        var consume = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, consume.Length,
            "Expected one enabled native Consume option for the loaded culinary serving; observed " +
            string.Join(", ", options.Select(option =>
                $"'{option.Label}' (disabled={option.Disabled})")));
        yield return new FloatMenuActionStep(
            "order native consumption of the loaded culinary serving",
            dinerId,
            mealId,
            consume[0].StableId);
        yield return new TimeControlActionStep(
            "run native loaded-serving ingestion",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "ordinary ingest toils acquire the exact cutlery",
            _ => !meal.Destroyed &&
                 diner.CurJobDef == JobDefOf.Ingest &&
                 !cutlery.Spawned &&
                 ReferenceEquals(cutlery.holdingOwner, diner.inventory?.innerContainer),
            new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(45)));
        yield return new SelectionActionStep(
            "select diner during loaded-serving ingestion",
            new[] { dinerId },
            additive: false);
        yield return new ScreenshotStep(
            "observe native ingestion after culinary state reload",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new TimeControlActionStep(
            "finish loaded-serving ingestion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "loaded serving applies visible thoughts poisoning and dirty ware",
            _ => OutcomeCompleted(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause after loaded-serving outcome",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "loaded serving applies each exact outcome once",
            _ => AssertCompletedOutcome());
        yield return new ScreenshotStep(
            "observe food poisoning attributed to dirty cookware",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new SelectionActionStep(
            "select culinary outcome diner",
            new[] { dinerId },
            additive: false);
        yield return new PawnInspectTabActionStep(
            "open the native Needs tab",
            dinerId,
            EndToEndPawnInspectTab.Needs);
        yield return new ScreenshotStep(
            "observe culinary temperature and dirty-setting thoughts",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new PawnInspectTabActionStep(
            "open the native Health tab",
            dinerId,
            EndToEndPawnInspectTab.Health);
        yield return new ScreenshotStep(
            "observe deterministic food poisoning after loaded meal",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select exact returned loaded-serving plate",
            new[] { plateId },
            additive: false);
        yield return new CameraActionStep(
            "frame exact returned loaded-serving ware",
            new[] { dinerId, plateId, cutleryId },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "observe exact dirty plate after loaded dining",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select exact returned loaded-serving cutlery",
            new[] { cutleryId },
            additive: false);
        yield return new ScreenshotStep(
            "observe exact dirty cutlery after loaded dining",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "culinary save-load and outcome result",
            _ => new Dictionary<string, string>
            {
                ["mealId"] = mealId,
                ["mealDestroyed"] = meal.Destroyed.ToString(),
                ["plateId"] = plateId,
                ["plateDirty"] = plate.GetComp<CompSanitation>()!.IsDirty.ToString(),
                ["cutleryId"] = cutleryId,
                ["cutleryDirty"] = cutlery.GetComp<CompSanitation>()!.IsDirty.ToString(),
                ["qualityThoughtStage"] = MemoryStage("ImmersiveChefs_CulinaryQuality").ToString(),
                ["temperatureThoughtStage"] = MemoryStage("ImmersiveChefs_MealTemperature").ToString(),
                ["diningThoughtStage"] = MemoryStage("ImmersiveChefs_DiningExperience").ToString(),
                ["foodPoisoning"] = diner.health.hediffSet.HasHediff(HediffDefOf.FoodPoisoning).ToString()
            });
    }

    private void PreserveSettings(IEndToEndContext context)
    {
        var settings = ImmersiveChefsMod.Settings;
        var priorCulinaryQualityEnabled = settings.CulinaryQualityEnabled;
        var priorQualityMoodScale = settings.QualityMoodScale;
        var priorFoodPoisoningEffectScale = settings.FoodPoisoningEffectScale;
        var priorMaximumCustomPoisonChance = settings.MaximumCustomPoisonChance;
        var priorMealTemperatureEnabled = settings.MealTemperatureEnabled;
        var priorAutoMicrowaveBelow = settings.AutoMicrowaveBelow;
        var priorWareRequirementMode = settings.WareRequirementMode;
        var priorColonyDiningStandards = settings.ColonyDiningStandards;
        context.DeferCleanup(() =>
        {
            settings.CulinaryQualityEnabled = priorCulinaryQualityEnabled;
            settings.QualityMoodScale = priorQualityMoodScale;
            settings.FoodPoisoningEffectScale = priorFoodPoisoningEffectScale;
            settings.MaximumCustomPoisonChance = priorMaximumCustomPoisonChance;
            settings.MealTemperatureEnabled = priorMealTemperatureEnabled;
            settings.AutoMicrowaveBelow = priorAutoMicrowaveBelow;
            settings.WareRequirementMode = priorWareRequirementMode;
            settings.ColonyDiningStandards = priorColonyDiningStandards;
        });
        settings.CulinaryQualityEnabled = true;
        settings.QualityMoodScale = 1f;
        settings.FoodPoisoningEffectScale = 3f;
        settings.MaximumCustomPoisonChance = 1f;
        settings.MealTemperatureEnabled = true;
        settings.AutoMicrowaveBelow = -10f;
        settings.WareRequirementMode = WareRequirementMode.Strict;
        settings.ColonyDiningStandards = false;
    }

    private void ResolveLoadedThings()
    {
        map = Current.Game.CurrentMap;
        diner = map.mapPawns.AllPawnsSpawned.SingleOrDefault(pawn => pawn.ThingID == dinerId) ??
                throw new EndToEndAssertionException(
                    $"The loaded map lost diner {dinerId}; present pawns: " +
                    string.Join(", ", map.mapPawns.AllPawnsSpawned.Select(pawn => pawn.ThingID)));
        meal = map.listerThings.AllThings.OfType<ThingWithComps>()
                   .SingleOrDefault(thing => thing.ThingID == mealId) ??
               throw new EndToEndAssertionException(
                   $"The loaded map lost meal {mealId}; present simple meals: " +
                   string.Join(", ", map.listerThings.ThingsOfDef(ThingDefOf.MealSimple)
                       .Select(thing => thing.ThingID)));
        cutlery = map.listerThings.AllThings.OfType<ThingWithComps>()
                       .SingleOrDefault(thing => thing.ThingID == cutleryId) ??
                   throw new EndToEndAssertionException(
                       $"The loaded map lost cutlery {cutleryId}; present cutlery: " +
                       string.Join(", ", map.listerThings.ThingsOfDef(
                               DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"))
                           .Select(thing => thing.ThingID)));
        plate = meal.GetComp<CompEmbeddedWare>()!.PeekPlateThing() as ThingWithComps ??
                throw new EndToEndAssertionException(
                    "The loaded culinary serving lost its exact embedded plate.");
    }

    private void AssertPersistedState()
    {
        EndToEndAssert.Equal(mealId, meal.ThingID,
            "Native save/load must preserve the exact meal identity.");
        EndToEndAssert.Equal(1, meal.stackCount,
            "Native save/load must preserve one physical serving.");
        EndToEndAssert.Equal(plateId, plate.ThingID,
            "Native save/load must preserve the exact embedded plate identity.");
        EndToEndAssert.Equal(1, plate.stackCount,
            "Native save/load must preserve one physical embedded plate.");
        EndToEndAssert.True(plate.GetComp<CompSanitation>()!.IsDirty,
            "Native save/load must preserve the plate's dirty state.");
        EndToEndAssert.Equal(WashProvenance.WildWater,
            plate.GetComp<CompSanitation>()!.WashProvenance,
            "Native save/load must preserve hidden wild-water provenance without displaying it.");
        EndToEndAssert.Equal(cutleryId, cutlery.ThingID,
            "Native save/load must preserve the exact clean cutlery identity.");
        EndToEndAssert.True(!cutlery.GetComp<CompSanitation>()!.IsDirty,
            "Native save/load must preserve clean unused cutlery.");

        var servings = meal.GetComp<CompCulinaryState>()!.Servings;
        EndToEndAssert.Equal(1, servings.Count,
            "Native save/load must preserve exactly one culinary record.");
        var actual = servings[0].Capture();
        EndToEndAssert.Equal(expectedServing.QualityScore, actual.QualityScore,
            "Culinary quality must survive save/load.");
        EndToEndAssert.Equal(expectedServing.TemperatureCelsius, actual.TemperatureCelsius,
            "Culinary temperature must survive save/load.");
        EndToEndAssert.Equal(expectedServing.Contamination, actual.Contamination,
            "Culinary contamination must survive save/load.");
        EndToEndAssert.Equal(expectedServing.MicrowaveReheatCount, actual.MicrowaveReheatCount,
            "Microwave history must survive save/load.");
        EndToEndAssert.Equal(expectedServing.LastThermalTick, actual.LastThermalTick,
            "Thermal bookkeeping must survive save/load.");
        EndToEndAssert.Equal(
            string.Join("|", expectedServing.HiddenSourceDefNames),
            string.Join("|", actual.HiddenSourceDefNames),
            "Hidden culinary provenance must survive save/load exactly.");
        EndToEndAssert.Equal(expectedServing.HiddenDietaryFlags, actual.HiddenDietaryFlags,
            "Hidden dietary flags must survive save/load.");
        EndToEndAssert.Equal(expectedServing.CookwareMaterial, actual.CookwareMaterial,
            "Hidden cookware material provenance must survive save/load.");
        EndToEndAssert.Equal(
            "RawRice",
            string.Join("|", meal.GetComp<CompIngredients>()!.ingredients
                .Select(ingredient => ingredient.defName)),
            "Public ingredient provenance must survive save/load exactly once.");
    }

    private bool OutcomeCompleted()
    {
        return meal.Destroyed &&
               plate.Spawned &&
               cutlery.Spawned &&
               diner.health.hediffSet.HasHediff(HediffDefOf.FoodPoisoning) &&
               MemoryStage("ImmersiveChefs_CulinaryQuality") == 4 &&
               MemoryStage("ImmersiveChefs_MealTemperature") == 4 &&
               MemoryStage("ImmersiveChefs_DiningExperience") == 4;
    }

    private void AssertCompletedOutcome()
    {
        EndToEndAssert.True(meal.Destroyed,
            "The exact loaded serving must be consumed through native ingestion.");
        EndToEndAssert.Equal(4, MemoryStage("ImmersiveChefs_CulinaryQuality"),
            "Quality 72 must produce the Excellent culinary thought.");
        EndToEndAssert.Equal(4, MemoryStage("ImmersiveChefs_MealTemperature"),
            "The consumed frozen serving must produce the Frozen temperature thought.");
        EndToEndAssert.Equal(4, MemoryStage("ImmersiveChefs_DiningExperience"),
            "The contaminated setting must produce one dirty-tableware thought.");
        EndToEndAssert.True(diner.health.hediffSet.HasHediff(HediffDefOf.FoodPoisoning),
            "The configured deterministic 100% final risk must visibly apply food poisoning.");
        var poisonMessages = LiveMessages()
            .Where(message => !initialLiveMessages.Contains(message) &&
                              message.text.IndexOf(
                                  "has gotten food poisoning from",
                                  StringComparison.OrdinalIgnoreCase) >= 0 &&
                              message.text.IndexOf(dinerLabel, StringComparison.OrdinalIgnoreCase) >= 0 &&
                              message.text.IndexOf(mealLabel, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, poisonMessages.Length,
            "The completed ingestion must produce one new native food-poisoning notification correlated to the exact diner and meal.");
        EndToEndAssert.True(
            poisonMessages[0].text.IndexOf("Cause: Dirty cookware.", StringComparison.OrdinalIgnoreCase) >= 0,
            "The native notification must name dirty cookware, the deterministic largest contributor, rather than unknown. " +
            $"Observed: {poisonMessages[0].text}");
        EndToEndAssert.True(plate.Spawned && plate.GetComp<CompSanitation>()!.IsDirty,
            "The exact loaded plate must return dirty after eating.");
        EndToEndAssert.True(cutlery.Spawned && cutlery.GetComp<CompSanitation>()!.IsDirty,
            "The exact acquired cutlery must return dirty after eating.");
        EndToEndAssert.Equal(1, plate.stackCount,
            "The exact returned plate must remain one physical unit.");
        EndToEndAssert.Equal(1, cutlery.stackCount,
            "The exact returned cutlery must remain one physical unit.");
        EndToEndAssert.Equal(1,
            FoodSearchE2EFixture.CountThingUnits(map, plate.def, meal, diner),
            "Loaded dining must conserve exactly one plate across all holders.");
        EndToEndAssert.Equal(1,
            FoodSearchE2EFixture.CountThingUnits(map, cutlery.def, meal, diner),
            "Loaded dining must conserve exactly one cutlery set across all holders.");
    }

    private int MemoryStage(string defName)
    {
        var thoughtDef = DefDatabase<ThoughtDef>.GetNamed(defName);
        return diner.needs?.mood?.thoughts?.memories
                   .GetFirstMemoryOfDef(thoughtDef)?.CurStageIndex ?? -1;
    }

    private static IReadOnlyList<Message> LiveMessages()
    {
        return (IReadOnlyList<Message>)(HarmonyLib.AccessTools
            .Field(typeof(Messages), "liveMessages")
            .GetValue(null) ?? Array.Empty<Message>());
    }

    private static void InstallLocalizedPoisonInspectLeakProbe(IEndToEndContext context)
    {
        const string owner = "fumblesneeze.immersivechefs.e2e.localized-poison-inspect-probe";
        var harmony = new HarmonyLib.Harmony(owner);
        context.DeferCleanup(() => harmony.UnpatchAll(owner));
        harmony.Patch(
            HarmonyLib.AccessTools.Method(typeof(ThingComp), nameof(ThingComp.CompInspectStringExtra)),
            postfix: new HarmonyLib.HarmonyMethod(
                typeof(LocalizedPoisonInspectLeakProbe),
                nameof(LocalizedPoisonInspectLeakProbe.Postfix)));
    }

    private static class LocalizedPoisonInspectLeakProbe
    {
        internal const string Sentinel = "Vergiftungszustand: verborgen";
        internal static int InvocationCount;

        [HarmonyLib.HarmonyPriority(HarmonyLib.Priority.Normal)]
        internal static void Postfix(ThingComp __instance, ref string __result)
        {
            if (__instance is CompFoodPoisonable)
            {
                InvocationCount++;
                __result = Sentinel;
            }
        }
    }
}
