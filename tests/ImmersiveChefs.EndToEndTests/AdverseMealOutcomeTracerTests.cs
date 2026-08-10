using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.adverse-meal-outcome-tracer",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_800,
    MaxGameTicks = 18_000,
    MaxWallClockSeconds = 210)]
public sealed class AdverseMealOutcomeTracerTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn diner = null!;
    private ThingWithComps meal = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private float configuredPoisonChance;

    public void Arrange(IEndToEndContext context)
    {
        PreserveSettings(context);

        map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);

        diner = FoodSearchE2EFixture.CreateColonist("Adverse meal diner");
        FoodSearchE2EFixture.SetHunger(diner, 0.20f);
        GenSpawn.Spawn(diner, center + (IntVec3.West * 3), map);
        diner.drafter.Drafted = true;

        meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        meal.stackCount = 1;
        meal.GetComp<CompCulinaryState>()!.ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                qualityScore: 10,
                temperatureCelsius: -20f,
                contamination: ContaminationSources.DirtyCookware |
                               ContaminationSources.DirtyPlate |
                               ContaminationSources.DirtyCutlery,
                microwaveReheatCount: 0,
                lastThermalTick: Math.Max(1, Find.TickManager.TicksGame))
        });

        plate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.WoodLog);
        plate.GetComp<CompSanitation>()!.MarkDirty();
        EndToEndAssert.True(
            meal.GetComp<CompEmbeddedWare>()!.TryEmbedPlate(plate),
            "The adverse meal fixture must bind its exact dirty plate once.");

        cutlery = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cutlery",
            ThingDefOf.WoodLog);
        cutlery.GetComp<CompSanitation>()!.MarkDirty();
        GenSpawn.Spawn(cutlery, center, map);
        GenSpawn.Spawn(meal, center + (IntVec3.East * 3), map);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before the adverse meal workflow",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the passive awful frozen dirty meal",
            new[] { meal.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the passive adverse meal fixture",
            new[] { diner.ThingID, meal.ThingID, cutlery.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "observe the passive awful frozen dirty meal before player action",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "the adverse fixture is passive and conserves its exact ware",
            _ => AssertPassiveFixture());

        yield return new SelectionActionStep(
            "select the drafted adverse-meal diner",
            new[] { diner.ThingID },
            additive: false);
        var draftToggle = RequiredDraftToggle(context, expectedCurrentState: true);
        yield return new GizmoActionStep(
            "undraft the diner through the native colonist gizmo",
            new[] { diner.ThingID },
            draftToggle.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draftToggle.StableId);
        yield return new TimeControlActionStep(
            "run ordinary game time for autonomous food seeking",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "ordinary food seeking starts native ingestion with exact dirty cutlery",
            _ => !meal.Destroyed &&
                 diner.CurJobDef == JobDefOf.Ingest &&
                 !cutlery.Spawned &&
                 ReferenceEquals(cutlery.holdingOwner, diner.inventory?.innerContainer),
            new EndToEndDeadline(1_200, 4_500, TimeSpan.FromSeconds(60)));
        yield return new SelectionActionStep(
            "select the diner during autonomous ingestion",
            new[] { diner.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe ordinary ingestion after native undraft and time actions",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish ordinary adverse-meal ingestion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "ordinary ingestion returns dirty ware and applies visible adverse outcomes",
            _ => OutcomeCompleted(),
            new EndToEndDeadline(2_400, 9_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause after the adverse meal outcome",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the adverse meal applies each exact outcome once",
            _ => AssertCompletedOutcome());

        yield return new SelectionActionStep(
            "select the adverse-meal diner after eating",
            new[] { diner.ThingID },
            additive: false);
        yield return new PawnInspectTabActionStep(
            "open the native Needs tab for adverse meal thoughts",
            diner.ThingID,
            EndToEndPawnInspectTab.Needs);
        yield return new ScreenshotStep(
            "observe awful frozen dirty dining thoughts after ingestion",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new PawnInspectTabActionStep(
            "open the native Health tab for deterministic food poisoning",
            diner.ThingID,
            EndToEndPawnInspectTab.Health);
        yield return new ScreenshotStep(
            "observe deterministic food poisoning after adverse ingestion",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new SelectionActionStep(
            "select the exact returned adverse plate",
            new[] { plate.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the exact returned adverse dining ware",
            new[] { diner.ThingID, plate.ThingID, cutlery.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "observe the exact returned dirty plate",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select the exact returned adverse cutlery",
            new[] { cutlery.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe the exact returned dirty cutlery",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "adverse meal tracer result",
            _ => new Dictionary<string, string>
            {
                ["mealDestroyed"] = meal.Destroyed.ToString(),
                ["qualityThoughtStage"] = MemoryStage("ImmersiveChefs_CulinaryQuality").ToString(),
                ["temperatureThoughtStage"] = MemoryStage("ImmersiveChefs_MealTemperature").ToString(),
                ["diningThoughtStage"] = MemoryStage("ImmersiveChefs_DiningExperience").ToString(),
                ["foodPoisoning"] = diner.health.hediffSet.HasHediff(HediffDefOf.FoodPoisoning).ToString(),
                ["configuredPoisonChance"] = configuredPoisonChance.ToString("0.000"),
                ["plateId"] = plate.ThingID,
                ["plateDirty"] = plate.GetComp<CompSanitation>()!.IsDirty.ToString(),
                ["cutleryId"] = cutlery.ThingID,
                ["cutleryDirty"] = cutlery.GetComp<CompSanitation>()!.IsDirty.ToString()
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
        var priorDirtyWareFallback = settings.DirtyWareFallback;
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
            settings.DirtyWareFallback = priorDirtyWareFallback;
            settings.ColonyDiningStandards = priorColonyDiningStandards;
        });
        settings.CulinaryQualityEnabled = true;
        settings.QualityMoodScale = 1f;
        settings.FoodPoisoningEffectScale = 3f;
        settings.MaximumCustomPoisonChance = 1f;
        settings.MealTemperatureEnabled = true;
        settings.AutoMicrowaveBelow = -100f;
        settings.WareRequirementMode = WareRequirementMode.Strict;
        settings.DirtyWareFallback = DirtyWareFallback.Always;
        settings.ColonyDiningStandards = false;
    }

    private void AssertPassiveFixture()
    {
        EndToEndAssert.True(diner.Drafted,
            "The diner must remain drafted until the native player action begins the workflow.");
        EndToEndAssert.True(diner.CurJobDef != JobDefOf.Ingest,
            "The passive fixture must not begin ingestion before native undrafting and time control.");
        var serving = meal.GetComp<CompCulinaryState>()!.Servings.Single();
        EndToEndAssert.Equal(10, serving.QualityScore,
            "The tracer must begin with an awful culinary-quality score.");
        EndToEndAssert.Equal(-20f, serving.TemperatureCelsius,
            "The tracer must begin with a frozen culinary temperature.");
        EndToEndAssert.Equal(
            ContaminationSources.DirtyCookware |
            ContaminationSources.DirtyPlate |
            ContaminationSources.DirtyCutlery,
            serving.Contamination,
            "The tracer must begin with dirty cookware, plate, and cutlery contamination.");
        EndToEndAssert.True(plate.GetComp<CompSanitation>()!.IsDirty,
            "The exact embedded plate must begin dirty.");
        EndToEndAssert.True(cutlery.Spawned && cutlery.GetComp<CompSanitation>()!.IsDirty,
            "The exact available cutlery must begin spawned and dirty.");
        EndToEndAssert.Equal(0, MemoryCount("ImmersiveChefs_CulinaryQuality"),
            "The passive diner must begin without a culinary-quality memory.");
        EndToEndAssert.Equal(0, MemoryCount("ImmersiveChefs_MealTemperature"),
            "The passive diner must begin without a meal-temperature memory.");
        EndToEndAssert.Equal(0, MemoryCount("ImmersiveChefs_DiningExperience"),
            "The passive diner must begin without a dining-experience memory.");
        EndToEndAssert.Equal(0, FoodPoisoningCount(),
            "The passive diner must begin without food poisoning.");

        configuredPoisonChance = DiningOutcomeCalculator.FinalPoisonChance(
            new DiningRiskInputs(
                baseChance: 0f,
                qualityScore: serving.QualityScore,
                thermalBand: ThermalCalculator.BandFor(serving.TemperatureCelsius),
                contamination: serving.Contamination,
                plateServiceScore: KitchenwareRuntime.ServiceScore(plate),
                cutleryServiceScore: KitchenwareRuntime.ServiceScore(cutlery),
                microwaveReheatCount: serving.MicrowaveReheatCount,
                microwaveExtraPercentagePoints: ImmersiveChefsMod.Settings.MicrowaveExtraPoisonChance,
                effectScale: ImmersiveChefsMod.Settings.FoodPoisoningEffectScale,
                maximumChance: ImmersiveChefsMod.Settings.MaximumCustomPoisonChance));
        EndToEndAssert.Equal(1f, configuredPoisonChance,
            "Even a zero vanilla base chance must calculate to a deterministic 100% adverse-meal risk.");
        AssertWareConservation();
    }

    private EndToEndGizmoOption RequiredDraftToggle(
        IEndToEndContext context,
        bool expectedCurrentState)
    {
        var candidates = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { diner.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == expectedCurrentState &&
                string.Equals(
                    option.HotKeyDefName,
                    "Command_ColonistDraft",
                    StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(1, candidates.Length,
            "The selected diner must expose one current native Draft toggle in the expected state.");
        return candidates[0];
    }

    private bool OutcomeCompleted()
    {
        return meal.Destroyed &&
               plate.Spawned &&
               cutlery.Spawned &&
               diner.health.hediffSet.HasHediff(HediffDefOf.FoodPoisoning) &&
               MemoryStage("ImmersiveChefs_CulinaryQuality") == 0 &&
               MemoryStage("ImmersiveChefs_MealTemperature") == 4 &&
               MemoryStage("ImmersiveChefs_DiningExperience") == 4;
    }

    private void AssertCompletedOutcome()
    {
        EndToEndAssert.True(meal.Destroyed,
            "Ordinary RimWorld ingestion must consume the exact adverse meal.");
        EndToEndAssert.Equal(0, MemoryStage("ImmersiveChefs_CulinaryQuality"),
            "Quality 10 must visibly apply the Awful culinary thought.");
        EndToEndAssert.Equal(4, MemoryStage("ImmersiveChefs_MealTemperature"),
            "The frozen meal must visibly apply the Frozen temperature thought.");
        EndToEndAssert.Equal(4, MemoryStage("ImmersiveChefs_DiningExperience"),
            "The dirty setting must visibly apply the worst dining thought.");
        EndToEndAssert.True(diner.health.hediffSet.HasHediff(HediffDefOf.FoodPoisoning),
            "The configured deterministic final risk must apply vanilla food poisoning.");
        EndToEndAssert.Equal(1, MemoryCount("ImmersiveChefs_CulinaryQuality"),
            "The completed ingestion must add exactly one culinary-quality memory.");
        EndToEndAssert.Equal(1, MemoryCount("ImmersiveChefs_MealTemperature"),
            "The completed ingestion must add exactly one meal-temperature memory.");
        EndToEndAssert.Equal(1, MemoryCount("ImmersiveChefs_DiningExperience"),
            "The completed ingestion must add exactly one dining-experience memory.");
        EndToEndAssert.Equal(1, FoodPoisoningCount(),
            "The completed ingestion must add exactly one vanilla food-poisoning hediff.");
        EndToEndAssert.True(plate.Spawned && plate.GetComp<CompSanitation>()!.IsDirty,
            "The exact embedded plate must return dirty after eating.");
        EndToEndAssert.True(cutlery.Spawned && cutlery.GetComp<CompSanitation>()!.IsDirty,
            "The exact acquired cutlery must return dirty after eating.");
        EndToEndAssert.Equal(1, plate.stackCount,
            "The exact returned plate must remain one physical unit.");
        EndToEndAssert.Equal(1, cutlery.stackCount,
            "The exact returned cutlery must remain one physical unit.");
        AssertWareConservation();
    }

    private void AssertWareConservation()
    {
        EndToEndAssert.Equal(
            1,
            FoodSearchE2EFixture.CountThingUnits(map, plate.def, meal, diner),
            "The tracer must conserve exactly one plate across all holders.");
        EndToEndAssert.Equal(
            1,
            FoodSearchE2EFixture.CountThingUnits(map, cutlery.def, meal, diner),
            "The tracer must conserve exactly one cutlery set across all holders.");
    }

    private int MemoryStage(string defName)
    {
        var thoughtDef = DefDatabase<ThoughtDef>.GetNamed(defName);
        return diner.needs?.mood?.thoughts?.memories
                   .GetFirstMemoryOfDef(thoughtDef)?.CurStageIndex ?? -1;
    }

    private int MemoryCount(string defName)
    {
        var thoughtDef = DefDatabase<ThoughtDef>.GetNamed(defName);
        return diner.needs?.mood?.thoughts?.memories.Memories
                   .Count(memory => memory.def == thoughtDef) ?? 0;
    }

    private int FoodPoisoningCount() => diner.health.hediffSet.hediffs.Count(
        hediff => hediff.def == HediffDefOf.FoodPoisoning);
}
