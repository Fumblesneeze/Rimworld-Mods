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
    "imranfish.xmlextensions",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 24_000,
    MaxWallClockSeconds = 270)]
public sealed class AdverseMealOutcomeTracerTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private DiningCase cleanCase = null!;
    private DiningCase contaminatedCase = null!;

    public void Arrange(IEndToEndContext context)
    {
        PreserveSettings(context);

        map = Current.Game.CurrentMap;
        var centers = FindRoomCenters(map, 2);
        FoodSearchE2EFixture.BuildSealedRoom(map, centers[0]);
        FoodSearchE2EFixture.BuildSealedRoom(map, centers[1]);

        cleanCase = CreateCase(
            "Clean comparison diner",
            centers[0],
            ContaminationSources.None,
            dirtyWare: false);
        contaminatedCase = CreateCase(
            "Contaminated comparison diner",
            centers[1],
            ContaminationSources.DirtyCookware |
            ContaminationSources.DirtyPlate |
            ContaminationSources.DirtyCutlery,
            dirtyWare: true);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before the matched clean and contaminated meal workflow",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new CameraActionStep(
            "frame both passive comparison fixtures",
            new[]
            {
                cleanCase.Diner.ThingID,
                cleanCase.Meal.ThingID,
                contaminatedCase.Diner.ThingID,
                contaminatedCase.Meal.ThingID
            },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "observe matched clean and contaminated meals before player action",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "matched fixtures are passive and use normal configured risk",
            _ => AssertPassiveFixtures());

        foreach (var step in RunNativeIngestion(context, cleanCase))
        {
            yield return step;
        }

        foreach (var step in RunNativeIngestion(context, contaminatedCase))
        {
            yield return step;
        }

        yield return new AssertionStep(
            "both native ingestions conserve their exact settings at ordinary risk",
            _ => AssertCompletedCases());
        yield return new CheckpointStep(
            "matched clean and contaminated meal result",
            _ => new Dictionary<string, string>
            {
                ["cleanConfiguredPoisonChance"] = cleanCase.ConfiguredPoisonChance.ToString("0.000"),
                ["contaminatedConfiguredPoisonChance"] = contaminatedCase.ConfiguredPoisonChance.ToString("0.000"),
                ["cleanObservedFoodPoisoning"] = HasFoodPoisoning(cleanCase.Diner).ToString(),
                ["contaminatedObservedFoodPoisoning"] = HasFoodPoisoning(contaminatedCase.Diner).ToString(),
                ["cleanMealDestroyed"] = cleanCase.Meal.Destroyed.ToString(),
                ["contaminatedMealDestroyed"] = contaminatedCase.Meal.Destroyed.ToString(),
                ["cleanPlateId"] = cleanCase.Plate.ThingID,
                ["cleanCutleryId"] = cleanCase.Cutlery.ThingID,
                ["contaminatedPlateId"] = contaminatedCase.Plate.ThingID,
                ["contaminatedCutleryId"] = contaminatedCase.Cutlery.ThingID
            });
    }

    private IEnumerable<EndToEndStep> RunNativeIngestion(
        IEndToEndContext context,
        DiningCase diningCase)
    {
        var label = diningCase.Label;
        yield return new SelectionActionStep(
            $"select the drafted {label}",
            new[] { diningCase.Diner.ThingID },
            additive: false);
        var draftToggle = RequiredDraftToggle(
            context,
            diningCase.Diner,
            expectedCurrentState: true);
        yield return new GizmoActionStep(
            $"undraft the {label} through the native colonist gizmo",
            new[] { diningCase.Diner.ThingID },
            draftToggle.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draftToggle.StableId);

        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(diningCase.Diner.ThingID, diningCase.Meal.ThingID);
        var consume = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, consume.Length,
            $"Expected one enabled native Consume option for the {label}; observed " +
            string.Join(", ", options.Select(option =>
                $"'{option.Label}' (disabled={option.Disabled})")));
        yield return new FloatMenuActionStep(
            $"order native consumption for the {label}",
            diningCase.Diner.ThingID,
            diningCase.Meal.ThingID,
            consume[0].StableId);
        yield return new TimeControlActionStep(
            $"run ordinary ingestion for the {label}",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            $"ordinary ingest toils acquire the exact cutlery for the {label}",
            _ => !diningCase.Meal.Destroyed &&
                 diningCase.Diner.CurJobDef == JobDefOf.Ingest &&
                 !diningCase.Cutlery.Spawned &&
                 ReferenceEquals(
                     diningCase.Cutlery.holdingOwner,
                     diningCase.Diner.inventory?.innerContainer),
            new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(45)));
        yield return new SelectionActionStep(
            $"select the {label} during native ingestion",
            new[] { diningCase.Diner.ThingID },
            additive: false);
        yield return new CameraActionStep(
            $"frame native ingestion for the {label}",
            new[]
            {
                diningCase.Diner.ThingID,
                diningCase.Meal.ThingID
            },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            $"observe native ingestion for the {label}",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new TimeControlActionStep(
            $"finish native ingestion for the {label}",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            $"native ingestion returns the exact ware for the {label}",
            _ => CaseCompleted(diningCase),
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(70)));
        yield return new TimeControlActionStep(
            $"pause after native ingestion for the {label}",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            $"select the {label} after eating",
            new[] { diningCase.Diner.ThingID },
            additive: false);
        yield return new PawnInspectTabActionStep(
            $"open the native Needs tab for the {label}",
            diningCase.Diner.ThingID,
            EndToEndPawnInspectTab.Needs);
        yield return new ScreenshotStep(
            $"observe ordinary dining thoughts for the {label}",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            $"select the exact returned plate for the {label}",
            new[] { diningCase.Plate.ThingID },
            additive: false);
        yield return new CameraActionStep(
            $"frame the exact returned setting for the {label}",
            new[]
            {
                diningCase.Diner.ThingID,
                diningCase.Plate.ThingID,
                diningCase.Cutlery.ThingID
            },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            $"observe the exact returned plate for the {label}",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            $"select the exact returned cutlery for the {label}",
            new[] { diningCase.Cutlery.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            $"observe the exact returned cutlery for the {label}",
            Array.Empty<string>(),
            paddingPixels: 0);
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
        settings.FoodPoisoningEffectScale = 1f;
        settings.MaximumCustomPoisonChance = 0.50f;
        settings.MealTemperatureEnabled = true;
        settings.AutoMicrowaveBelow = -100f;
        settings.WareRequirementMode = WareRequirementMode.Strict;
        settings.DirtyWareFallback = DirtyWareFallback.Always;
        settings.ColonyDiningStandards = false;
    }

    private DiningCase CreateCase(
        string label,
        IntVec3 center,
        ContaminationSources contamination,
        bool dirtyWare)
    {
        var diner = FoodSearchE2EFixture.CreateColonist(label);
        FoodSearchE2EFixture.SetHunger(diner, 0.20f);
        GenSpawn.Spawn(diner, center + (IntVec3.West * 3), map);
        diner.drafter.Drafted = true;

        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        meal.stackCount = 1;
        meal.GetComp<CompCulinaryState>()!.ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                qualityScore: 10,
                temperatureCelsius: -20f,
                contamination: contamination,
                microwaveReheatCount: 0,
                lastThermalTick: Math.Max(1, Find.TickManager.TicksGame))
        });

        var plate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.WoodLog);
        var cutlery = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cutlery",
            ThingDefOf.WoodLog);
        if (dirtyWare)
        {
            plate.GetComp<CompSanitation>()!.MarkDirty();
            cutlery.GetComp<CompSanitation>()!.MarkDirty();
        }

        EndToEndAssert.True(
            meal.GetComp<CompEmbeddedWare>()!.TryEmbedPlate(plate),
            $"The {label} must bind its exact plate once.");
        GenSpawn.Spawn(cutlery, center, map);
        GenSpawn.Spawn(meal, center + (IntVec3.East * 3), map);

        return new DiningCase(
            label,
            diner,
            meal,
            plate,
            cutlery,
            contamination,
            dirtyWare);
    }

    private void AssertPassiveFixtures()
    {
        AssertPassiveCase(cleanCase);
        AssertPassiveCase(contaminatedCase);
        AssertGlobalWareConservation();

        EndToEndAssert.Equal(1f, ImmersiveChefsMod.Settings.FoodPoisoningEffectScale,
            "The native comparison must use the ordinary production effect scale.");
        EndToEndAssert.Equal(0.50f, ImmersiveChefsMod.Settings.MaximumCustomPoisonChance,
            "The native comparison must use the ordinary production risk cap.");
        EndToEndAssert.True(cleanCase.ConfiguredPoisonChance < contaminatedCase.ConfiguredPoisonChance,
            "The matched contaminated serving must have greater configured risk than the clean serving.");
        EndToEndAssert.True(
            Math.Abs(
                contaminatedCase.ConfiguredPoisonChance -
                cleanCase.ConfiguredPoisonChance -
                0.20f) < 0.0001f,
            "The three contaminated-setting contributors must add their halved twenty percentage points exactly.");
    }

    private void AssertPassiveCase(DiningCase diningCase)
    {
        EndToEndAssert.True(diningCase.Diner.Drafted,
            $"The {diningCase.Label} must remain drafted until the native player action begins.");
        EndToEndAssert.True(diningCase.Diner.CurJobDef != JobDefOf.Ingest,
            $"The passive {diningCase.Label} must not ingest before the native order.");
        var serving = diningCase.Meal.GetComp<CompCulinaryState>()!.Servings.Single();
        EndToEndAssert.Equal(10, serving.QualityScore,
            $"The {diningCase.Label} must begin with the matched culinary-quality score.");
        EndToEndAssert.Equal(-20f, serving.TemperatureCelsius,
            $"The {diningCase.Label} must begin with the matched frozen temperature.");
        EndToEndAssert.Equal(diningCase.InitialContamination, serving.Contamination,
            $"The {diningCase.Label} must retain its exact initial contamination.");
        EndToEndAssert.Equal(
            diningCase.InitiallyDirty,
            diningCase.Plate.GetComp<CompSanitation>()!.IsDirty,
            $"The {diningCase.Label} plate sanitation must match its declared case.");
        EndToEndAssert.True(diningCase.Cutlery.Spawned,
            $"The exact {diningCase.Label} cutlery must begin spawned.");
        EndToEndAssert.Equal(
            diningCase.InitiallyDirty,
            diningCase.Cutlery.GetComp<CompSanitation>()!.IsDirty,
            $"The {diningCase.Label} cutlery sanitation must match its declared case.");
        EndToEndAssert.Equal(0, MemoryCount(diningCase.Diner, "ImmersiveChefs_CulinaryQuality"),
            $"The passive {diningCase.Label} must begin without a culinary-quality memory.");
        EndToEndAssert.Equal(0, MemoryCount(diningCase.Diner, "ImmersiveChefs_MealTemperature"),
            $"The passive {diningCase.Label} must begin without a meal-temperature memory.");
        EndToEndAssert.Equal(0, MemoryCount(diningCase.Diner, "ImmersiveChefs_DiningExperience"),
            $"The passive {diningCase.Label} must begin without a dining-experience memory.");
        EndToEndAssert.Equal(0, FoodPoisoningCount(diningCase.Diner),
            $"The passive {diningCase.Label} must begin without food poisoning.");

        diningCase.ConfiguredPoisonChance = DiningOutcomeCalculator.FinalPoisonChance(
            new DiningRiskInputs(
                baseChance: 0f,
                qualityScore: serving.QualityScore,
                thermalBand: ThermalCalculator.BandFor(serving.TemperatureCelsius),
                contamination: serving.Contamination,
                plateServiceScore: KitchenwareRuntime.ServiceScore(diningCase.Plate),
                cutleryServiceScore: KitchenwareRuntime.ServiceScore(diningCase.Cutlery),
                microwaveReheatCount: serving.MicrowaveReheatCount,
                microwaveExtraPercentagePoints: ImmersiveChefsMod.Settings.MicrowaveExtraPoisonChance,
                effectScale: ImmersiveChefsMod.Settings.FoodPoisoningEffectScale,
                maximumChance: ImmersiveChefsMod.Settings.MaximumCustomPoisonChance));
        EndToEndAssert.True(diningCase.ConfiguredPoisonChance > 0f &&
                            diningCase.ConfiguredPoisonChance < 0.50f,
            $"The {diningCase.Label} must use an ordinary non-forced probability below the player cap.");
    }

    private EndToEndGizmoOption RequiredDraftToggle(
        IEndToEndContext context,
        Pawn diner,
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

    private bool CaseCompleted(DiningCase diningCase)
    {
        return diningCase.Meal.Destroyed &&
               diningCase.Plate.Spawned &&
               diningCase.Cutlery.Spawned &&
               MemoryStage(diningCase.Diner, "ImmersiveChefs_CulinaryQuality") == 0 &&
               MemoryStage(diningCase.Diner, "ImmersiveChefs_MealTemperature") == 4;
    }

    private void AssertCompletedCases()
    {
        AssertCompletedCase(cleanCase);
        AssertCompletedCase(contaminatedCase);
        AssertGlobalWareConservation();
    }

    private void AssertCompletedCase(DiningCase diningCase)
    {
        EndToEndAssert.True(diningCase.Meal.Destroyed,
            $"Ordinary RimWorld ingestion must consume the exact {diningCase.Label} meal.");
        EndToEndAssert.Equal(0, MemoryStage(diningCase.Diner, "ImmersiveChefs_CulinaryQuality"),
            $"Quality 10 must visibly apply the Awful culinary thought for the {diningCase.Label}.");
        EndToEndAssert.Equal(4, MemoryStage(diningCase.Diner, "ImmersiveChefs_MealTemperature"),
            $"The frozen {diningCase.Label} must visibly apply the Frozen temperature thought.");
        EndToEndAssert.Equal(1, MemoryCount(diningCase.Diner, "ImmersiveChefs_CulinaryQuality"),
            $"The {diningCase.Label} must add exactly one culinary-quality memory.");
        EndToEndAssert.Equal(1, MemoryCount(diningCase.Diner, "ImmersiveChefs_MealTemperature"),
            $"The {diningCase.Label} must add exactly one meal-temperature memory.");
        EndToEndAssert.True(
            diningCase.Plate.Spawned && diningCase.Plate.GetComp<CompSanitation>()!.IsDirty,
            $"The exact {diningCase.Label} plate must return dirty after eating.");
        EndToEndAssert.True(
            diningCase.Cutlery.Spawned && diningCase.Cutlery.GetComp<CompSanitation>()!.IsDirty,
            $"The exact {diningCase.Label} cutlery must return dirty after eating.");
        EndToEndAssert.Equal(1, diningCase.Plate.stackCount,
            $"The exact {diningCase.Label} plate must remain one physical unit.");
        EndToEndAssert.Equal(1, diningCase.Cutlery.stackCount,
            $"The exact {diningCase.Label} cutlery must remain one physical unit.");
    }

    private void AssertGlobalWareConservation()
    {
        EndToEndAssert.Equal(
            2,
            CountGlobalWareUnits(cleanCase.Plate.def),
            "The matched comparison must conserve exactly two plates without duplication or loss.");
        EndToEndAssert.Equal(
            2,
            CountGlobalWareUnits(cleanCase.Cutlery.def),
            "The matched comparison must conserve exactly two cutlery sets without duplication or loss.");
    }

    private int CountGlobalWareUnits(ThingDef def)
    {
        var spawned = map.listerThings.ThingsOfDef(def).Sum(thing => thing.stackCount);
        var heldByPawns = new[] { cleanCase.Diner, contaminatedCase.Diner }.Sum(pawn =>
            (pawn.inventory?.innerContainer
                 .Where(thing => thing.def == def)
                 .Sum(thing => thing.stackCount) ?? 0) +
            (pawn.carryTracker?.CarriedThing is { } carried && carried.def == def
                ? carried.stackCount
                : 0));
        var embedded = string.Equals(
            def.defName,
            "ImmersiveChefs_Plate",
            StringComparison.Ordinal)
            ? new[] { cleanCase.Meal, contaminatedCase.Meal }
                .Where(meal => !meal.Destroyed)
                .Sum(meal => meal.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount ?? 0)
            : 0;
        return spawned + heldByPawns + embedded;
    }

    private static int MemoryStage(Pawn diner, string defName)
    {
        var thoughtDef = DefDatabase<ThoughtDef>.GetNamed(defName);
        return diner.needs?.mood?.thoughts?.memories
                   .GetFirstMemoryOfDef(thoughtDef)?.CurStageIndex ?? -1;
    }

    private static int MemoryCount(Pawn diner, string defName)
    {
        var thoughtDef = DefDatabase<ThoughtDef>.GetNamed(defName);
        return diner.needs?.mood?.thoughts?.memories.Memories
                   .Count(memory => memory.def == thoughtDef) ?? 0;
    }

    private static int FoodPoisoningCount(Pawn diner) => diner.health.hediffSet.hediffs.Count(
        hediff => hediff.def == HediffDefOf.FoodPoisoning);

    private static bool HasFoodPoisoning(Pawn diner) => FoodPoisoningCount(diner) > 0;

    private static IReadOnlyList<IntVec3> FindRoomCenters(Map map, int count)
    {
        var centers = new List<IntVec3>();
        for (var x = -54; x <= 54; x += 18)
        {
            for (var z = -54; z <= 54; z += 18)
            {
                var candidate = map.Center + new IntVec3(x, 0, z);
                if (!candidate.InBounds(map) ||
                    centers.Any(center => center.DistanceToSquared(candidate) < 225) ||
                    !SquareIsUsable(map, candidate, 6))
                {
                    continue;
                }

                centers.Add(candidate);
                if (centers.Count == count)
                {
                    return centers;
                }
            }
        }

        throw new EndToEndAssertionException(
            "Could not find two separated matched-ingestion rooms.");
    }

    private static bool SquareIsUsable(Map map, IntVec3 center, int radius)
    {
        for (var x = -radius; x <= radius; x++)
        {
            for (var z = -radius; z <= radius; z++)
            {
                var cell = center + new IntVec3(x, 0, z);
                if (!cell.InBounds(map) ||
                    !cell.Walkable(map) ||
                    cell.GetEdifice(map) is not null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private sealed class DiningCase
    {
        public DiningCase(
            string label,
            Pawn diner,
            ThingWithComps meal,
            ThingWithComps plate,
            ThingWithComps cutlery,
            ContaminationSources initialContamination,
            bool initiallyDirty)
        {
            Label = label;
            Diner = diner;
            Meal = meal;
            Plate = plate;
            Cutlery = cutlery;
            InitialContamination = initialContamination;
            InitiallyDirty = initiallyDirty;
        }

        public string Label { get; }
        public Pawn Diner { get; }
        public ThingWithComps Meal { get; }
        public ThingWithComps Plate { get; }
        public ThingWithComps Cutlery { get; }
        public ContaminationSources InitialContamination { get; }
        public bool InitiallyDirty { get; }
        public float ConfiguredPoisonChance { get; set; }
    }
}
