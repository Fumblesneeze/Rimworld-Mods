using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.dishwasher-research-progression",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "fumblesneeze.immersivechefs",
    MaxFrames = 3_600,
    MaxGameTicks = 8_000,
    MaxWallClockSeconds = 150)]
public sealed class DishwasherResearchProgressionTest : IRimWorldEndToEndTest
{
    private static readonly string[] ProfessionalKitchenBuildables =
    {
        "ImmersiveChefs_IndustrialDishwasher",
        "ImmersiveChefs_PrepStation",
        "ImmersiveChefs_SauceStation",
        "ImmersiveChefs_MeatStation",
        "ImmersiveChefs_VegetableStation",
        "ImmersiveChefs_PastryStation"
    };

    private IEndToEndGizmoCatalog catalog = null!;
    private Map map = null!;
    private Thing marker = null!;
    private IntVec3 domesticCell;
    private IntVec3 industrialCell;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        catalog = context.GetRequiredService<IEndToEndGizmoCatalog>();
        PreserveResearch(context);

        SetResearchFinished("Electricity", finished: true);
        SetResearchFinished("ImmersiveChefs_Dishwashing", finished: false);
        SetResearchFinished("ImmersiveChefs_ProfessionalKitchens", finished: false);

        var cells = FindPlacementCells(map);
        domesticCell = cells[0];
        industrialCell = cells[1];
        marker = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("DiningChair"), ThingDefOf.WoodLog);
        marker.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(marker, domesticCell + new IntVec3(0, 0, 5), map, Rot4.South);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before inspecting locked kitchen research",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the empty researched-building placement area",
            new[] { marker.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the researched-building placement area",
            new[] { marker.ThingID },
            paddingPixels: 300);
        yield return new AssertionStep(
            "dishwashers remain absent before their own research",
            _ =>
            {
                AssertBuildable("ImmersiveChefs_Dishwasher", expected: false);
                foreach (var defName in ProfessionalKitchenBuildables)
                {
                    AssertBuildable(defName, expected: false);
                }
            });
        yield return new ArchitectCategoryActionStep(
            "open Production before dishwashing research",
            "Production",
            open: true);
        yield return new ScreenshotStep(
            "native Production menu before dishwashing research",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new ArchitectCategoryActionStep(
            "close Production before dishwashing research",
            "Production",
            open: false);

        SetResearchFinished("ImmersiveChefs_Dishwashing", finished: true);
        yield return new AssertionStep(
            "dishwashing research unlocks only the domestic appliance",
            _ =>
            {
                AssertBuildable("ImmersiveChefs_Dishwasher", expected: true);
                foreach (var defName in ProfessionalKitchenBuildables)
                {
                    AssertBuildable(defName, expected: false);
                }
            });
        yield return new ArchitectCategoryActionStep(
            "open Production after dishwashing research",
            "Production",
            open: true);
        yield return new ScreenshotStep(
            "domestic dishwasher visible after dishwashing research",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new ArchitectCategoryActionStep(
            "close Production after dishwashing research",
            "Production",
            open: false);

        SetResearchFinished("Machining", finished: true);
        SetResearchFinished("ImmersiveChefs_ProfessionalKitchens", finished: true);
        yield return new AssertionStep(
            "professional kitchens unlocks the industrial appliance and station set",
            _ =>
            {
                AssertBuildable("ImmersiveChefs_Dishwasher", expected: true);
                foreach (var defName in ProfessionalKitchenBuildables)
                {
                    AssertBuildable(defName, expected: true);
                }
            });
        yield return new ArchitectCategoryActionStep(
            "open Production after professional kitchens research",
            "Production",
            open: true);
        yield return new ScreenshotStep(
            "industrial dishwasher and station set visible after professional kitchens research",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new ArchitectCategoryActionStep(
            "close Production after professional kitchens research",
            "Production",
            open: false);

        var domestic = GetEnabledPlacement("ImmersiveChefs_Dishwasher");
        yield return new GizmoActionStep(
            "place a domestic dishwasher blueprint through the unlocked native designator",
            Array.Empty<string>(),
            domestic.RuntimeType,
            EndToEndGizmoInteraction.Place,
            EndToEndCardinalRotation.East,
            stableGizmoId: domestic.StableId,
            startCell: new EndToEndMapCell(domesticCell.x, domesticCell.z),
            architectCategoryDefNames: new[] { "Production" });
        yield return new WaitUntilStep(
            "the domestic dishwasher blueprint appears",
            _ => FindBlueprint("ImmersiveChefs_Dishwasher") is not null,
            new EndToEndDeadline(300, 1_000, TimeSpan.FromSeconds(15)));

        var industrial = GetEnabledPlacement("ImmersiveChefs_IndustrialDishwasher");
        yield return new GizmoActionStep(
            "place an industrial dishwasher blueprint through the unlocked native designator",
            Array.Empty<string>(),
            industrial.RuntimeType,
            EndToEndGizmoInteraction.Place,
            EndToEndCardinalRotation.West,
            stableGizmoId: industrial.StableId,
            startCell: new EndToEndMapCell(industrialCell.x, industrialCell.z),
            architectCategoryDefNames: new[] { "Production" });
        yield return new WaitUntilStep(
            "the industrial dishwasher blueprint appears",
            _ => FindBlueprint("ImmersiveChefs_IndustrialDishwasher") is not null,
            new EndToEndDeadline(300, 1_000, TimeSpan.FromSeconds(15)));

        var domesticBlueprint = FindBlueprint("ImmersiveChefs_Dishwasher")!;
        var industrialBlueprint = FindBlueprint("ImmersiveChefs_IndustrialDishwasher")!;
        yield return new SelectionActionStep(
            "select both research-unlocked dishwasher blueprints",
            new[] { domesticBlueprint.ThingID, industrialBlueprint.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame both research-unlocked dishwasher blueprints",
            new[] { domesticBlueprint.ThingID, industrialBlueprint.ThingID, marker.ThingID },
            paddingPixels: 200);
        yield return new ScreenshotStep(
            "native blueprints placed only after both research unlocks",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "dishwasher research progression result",
            _ => new Dictionary<string, string>
            {
                ["domesticBlueprint"] = domesticBlueprint.ThingID,
                ["industrialBlueprint"] = industrialBlueprint.ThingID,
                ["unlockedProfessionalBuildables"] = string.Join(",", ProfessionalKitchenBuildables)
            });
    }

    private void AssertBuildable(string defName, bool expected)
    {
        var enabled = catalog.Query(Array.Empty<string>(), new[] { "Production" })
            .Any(option =>
                option.Interaction == EndToEndGizmoInteraction.Place &&
                string.Equals(option.BuildableDefName, defName, StringComparison.Ordinal) &&
                !option.Disabled);
        EndToEndAssert.Equal(expected, enabled,
            defName + (expected
                ? " must be enabled in the native Production catalog."
                : " must remain unavailable in the native Production catalog."));
    }

    private EndToEndGizmoOption GetEnabledPlacement(string defName)
    {
        var matches = catalog.Query(Array.Empty<string>(), new[] { "Production" })
            .Where(option =>
                option.Interaction == EndToEndGizmoInteraction.Place &&
                string.Equals(option.BuildableDefName, defName, StringComparison.Ordinal) &&
                !option.Disabled)
            .ToArray();
        EndToEndAssert.Equal(1, matches.Length,
            "Expected exactly one enabled native placement designator for " + defName + ".");
        return matches[0];
    }

    private Thing? FindBlueprint(string buildableDefName) =>
        map.listerThings.AllThings.SingleOrDefault(thing =>
            string.Equals(
                thing.def.entityDefToBuild?.defName,
                buildableDefName,
                StringComparison.Ordinal));

    private static IReadOnlyList<IntVec3> FindPlacementCells(Map map)
    {
        var domestic = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Dishwasher");
        var industrial = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_IndustrialDishwasher");
        foreach (var first in GenRadial.RadialCellsAround(map.Center, 55f, useCenter: true))
        {
            if (!first.InBounds(map) ||
                !GenConstruct.CanPlaceBlueprintAt(
                    domestic,
                    first,
                    Rot4.East,
                    map,
                    godMode: false).Accepted)
            {
                continue;
            }

            foreach (var second in GenRadial.RadialCellsAround(first, 14f, useCenter: false))
            {
                if (second.InBounds(map) &&
                    second.DistanceToSquared(first) >= 64 &&
                    GenConstruct.CanPlaceBlueprintAt(
                        industrial,
                        second,
                        Rot4.West,
                        map,
                        godMode: false).Accepted)
                {
                    return new[] { first, second };
                }
            }
        }

        throw new EndToEndAssertionException(
            "Could not find two bounded native dishwasher blueprint cells.");
    }

    private static void PreserveResearch(IEndToEndContext context)
    {
        var names = new[]
        {
            "Electricity",
            "Machining",
            "ImmersiveChefs_Dishwashing",
            "ImmersiveChefs_ProfessionalKitchens"
        };
        var original = names.ToDictionary(
            name => name,
            name => GetResearchProgress().TryGetValue(
                DefDatabase<ResearchProjectDef>.GetNamed(name),
                out var value)
                    ? (Present: true, Value: value)
                    : (Present: false, Value: 0f),
            StringComparer.Ordinal);
        context.DeferCleanup(() =>
        {
            var progress = GetResearchProgress();
            foreach (var pair in original)
            {
                var project = DefDatabase<ResearchProjectDef>.GetNamed(pair.Key);
                if (pair.Value.Present)
                {
                    progress[project] = pair.Value.Value;
                }
                else
                {
                    progress.Remove(project);
                }
            }
        });
    }

    private static void SetResearchFinished(string defName, bool finished)
    {
        var project = DefDatabase<ResearchProjectDef>.GetNamed(defName);
        var progress = GetResearchProgress();
        if (!finished)
        {
            progress.Remove(project);
            return;
        }

        if (!project.IsFinished)
        {
            Find.ResearchManager.FinishProject(
                project,
                doCompletionDialog: false,
                researcher: null,
                doCompletionLetter: false);
        }
    }

    private static Dictionary<ResearchProjectDef, float> GetResearchProgress()
    {
        var field = typeof(ResearchManager).GetField(
            "progress",
            BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new EndToEndAssertionException(
                "Could not resolve RimWorld's research progress store.");
        return (Dictionary<ResearchProjectDef, float>)field.GetValue(Find.ResearchManager);
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.local-dishwasher-capacity-persistence",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "fumblesneeze.immersivechefs",
    MaxFrames = 11_000,
    MaxGameTicks = 45_000,
    MaxWallClockSeconds = 390)]
public sealed class LocalDishwasherCapacityPersistenceTest : IRimWorldEndToEndTest
{
    private const string SaveName = "ImmersiveChefs_E2E_DishwasherCapacityPersistence";
    private readonly List<WareSnapshot> industrialSnapshots = new();
    private Map map = null!;
    private ThingWithComps domestic = null!;
    private ThingWithComps industrial = null!;
    private Pawn domesticWorker = null!;
    private Pawn industrialWorker = null!;
    private ThingWithComps domesticLoad = null!;
    private ThingWithComps domesticOverflow = null!;
    private ThingWithComps industrialOverflow = null!;
    private readonly List<ThingWithComps> industrialLoads = new();
    private WareSnapshot domesticSnapshot = null!;
    private string domesticId = string.Empty;
    private string industrialId = string.Empty;
    private string domesticWorkerId = string.Empty;
    private string industrialWorkerId = string.Empty;
    private string domesticOverflowId = string.Empty;
    private string industrialOverflowId = string.Empty;

    public void Arrange(IEndToEndContext context)
    {
        PreserveSettings(context);
        var savePath = GenFilePaths.FilePathForSavedGame(SaveName);
        context.DeferCleanup(() =>
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
        });

        map = Current.Game.CurrentMap;
        var centers = FindRoomCenters(map, 2);

        FoodSearchE2EFixture.BuildSealedRoom(map, centers[0]);
        FoodSearchE2EFixture.BuildSealedRoom(map, centers[1]);
        DispenserE2EFixture.SpawnConduitGrid(map, centers[0], 5, 5);
        DispenserE2EFixture.SpawnConduitGrid(map, centers[1], 5, 5);
        DispenserE2EFixture.SpawnPowerSources(map, centers[0] + new IntVec3(4, 0, 4), 1);
        DispenserE2EFixture.SpawnPowerSources(map, centers[1] + new IntVec3(4, 0, 4), 1);

        domestic = DispenserE2EFixture.SpawnBuilding(
            map,
            "ImmersiveChefs_Dishwasher",
            centers[0]);
        industrial = DispenserE2EFixture.SpawnBuilding(
            map,
            "ImmersiveChefs_IndustrialDishwasher",
            centers[1]);
        DispenserE2EFixture.SettlePower(map, new[] { domestic, industrial }, 400);
        EndToEndAssert.False(ProcessorFrameworkAdapter.Controls(domestic),
            "The exact base group must exercise the local dishwasher cycle.");
        EndToEndAssert.False(ProcessorFrameworkAdapter.Controls(industrial),
            "The exact base group must exercise the local industrial cycle.");
        EndToEndAssert.Equal(16f, domestic.GetComp<CompDishwasher>()!.Capacity,
            "The finalized domestic dishwasher must expose 16 plate-equivalents.");
        EndToEndAssert.Equal(64f, industrial.GetComp<CompDishwasher>()!.Capacity,
            "The finalized industrial dishwasher must expose 64 plate-equivalents.");

        domesticWorker = CreateCleaner("Domestic capacity cleaner");
        industrialWorker = CreateCleaner("Industrial capacity cleaner");
        GenSpawn.Spawn(domesticWorker, centers[0] + (IntVec3.South * 3), map);
        GenSpawn.Spawn(industrialWorker, centers[1] + (IntVec3.South * 3), map);

        domesticLoad = MakeDirtyPlateStack(ThingDefOf.Steel, 16, QualityCategory.Normal, 73);
        domesticOverflow = MakeDirtyPlateStack(ThingDefOf.WoodLog, 1, QualityCategory.Poor, 41);
        GenSpawn.Spawn(domesticLoad, centers[0] + (IntVec3.East * 3), map);
        GenSpawn.Spawn(domesticOverflow, centers[0] + (IntVec3.West * 3), map);
        domesticOverflow.SetForbidden(true, warnOnFail: false);

        industrialLoads.Add(MakeDirtyPlateStack(
            ThingDefOf.WoodLog,
            25,
            QualityCategory.Poor,
            37));
        industrialLoads.Add(MakeDirtyPlateStack(
            ThingDefOf.Steel,
            25,
            QualityCategory.Normal,
            68));
        industrialLoads.Add(MakeDirtyPlateStack(
            ThingDefOf.Silver,
            14,
            QualityCategory.Excellent,
            82));
        industrialOverflow = MakeDirtyPlateStack(
            ThingDefOf.Gold,
            1,
            QualityCategory.Masterwork,
            59);
        var industrialStagingCell = centers[1] + (IntVec3.South * 2);
        GenSpawn.Spawn(industrialLoads[0], industrialStagingCell, map);
        GenSpawn.Spawn(industrialLoads[1], industrialStagingCell, map);
        GenSpawn.Spawn(industrialLoads[2], industrialStagingCell, map);
        GenSpawn.Spawn(industrialOverflow, centers[1] + (IntVec3.East * 7), map);
        industrialOverflow.SetForbidden(true, warnOnFail: false);

        domesticSnapshot = WareSnapshot.Capture(domesticLoad);
        industrialSnapshots.AddRange(industrialLoads.Select(WareSnapshot.Capture));
        domesticId = domestic.ThingID;
        industrialId = industrial.ThingID;
        domesticWorkerId = domesticWorker.ThingID;
        industrialWorkerId = industrialWorker.ThingID;
        domesticOverflowId = domesticOverflow.ThingID;
        industrialOverflowId = industrialOverflow.ThingID;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var cleaning = WorkTypeDefOf.Cleaning;
        yield return new TimeControlActionStep(
            "pause before native domestic dishwasher hauling",
            paused: true,
            EndToEndGameSpeed.Normal);
        Activate(domesticWorker, cleaning);
        yield return new TimeControlActionStep(
            "run ordinary domestic dishwashing work",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the domestic cleaner starts Doing dishes",
            _ => IsDoingDishesTo(domesticWorker, domestic),
            new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(35)));
        yield return new SelectionActionStep(
            "select the domestic cleaner and dishwasher during native hauling",
            new[] { domesticWorker.ThingID, domestic.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame native domestic dishwasher hauling",
            new[] { domesticWorker.ThingID, domestic.ThingID, domesticLoad.ThingID },
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "ordinary cleaner hauling a dirty stack to the domestic dishwasher",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new TimeControlActionStep(
            "finish domestic dishwasher admission",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the domestic dishwasher reaches exactly 16 place settings",
            _ => Nearly(domestic.GetComp<CompDishwasher>()!.UsedCapacity, 16f),
            new EndToEndDeadline(1_200, 6_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause the full domestic dishwasher",
            paused: true,
            EndToEndGameSpeed.Normal);
        domesticOverflow.SetForbidden(false, warnOnFail: false);
        domesticWorker.jobs.EndCurrentJob(JobCondition.InterruptForced);
        var domesticProbeEndTick = Find.TickManager.TicksGame + 500;
        yield return new TimeControlActionStep(
            "let ordinary Cleaning reconsider the allowed domestic overflow",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the full domestic appliance rejects allowed overflow through ordinary work time",
            _ => Find.TickManager.TicksGame >= domesticProbeEndTick,
            new EndToEndDeadline(600, 1_500, TimeSpan.FromSeconds(20)));
        yield return new TimeControlActionStep(
            "pause after the domestic capacity rejection window",
            paused: true,
            EndToEndGameSpeed.Normal);
        Deactivate(domesticWorker, cleaning);
        yield return new AssertionStep(
            "the domestic appliance is full and rejects overflow",
            _ => AssertDomesticFull());
        yield return new SelectionActionStep(
            "select the full domestic dishwasher",
            new[] { domestic.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the full domestic dishwasher and rejected overflow",
            new[] { domestic.ThingID, domesticOverflow.ThingID, domesticWorker.ThingID },
            paddingPixels: 200);
        yield return new ScreenshotStep(
            "domestic dishwasher visibly holds 16 of 16 dirty place settings",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new SaveLoadActionStep(
            "save and load a full dirty dishwasher through RimWorld",
            SaveName);
        yield return new AssertionStep(
            "the same full dirty dishwasher stack survives native loading",
            _ =>
            {
                ResolveLoadedFixtures();
                AssertDomesticFull();
            });
        yield return new SelectionActionStep(
            "select the same full domestic dishwasher after loading",
            new[] { domestic.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the same full domestic dishwasher after loading",
            new[] { domestic.ThingID, domesticOverflow.ThingID },
            paddingPixels: 200);
        yield return new ScreenshotStep(
            "same domestic 16 of 16 dirty stack after native save load",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish the loaded domestic dishwasher cycle",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the exact domestic stack returns clean",
            _ => domesticLoad.Spawned &&
                 !domesticLoad.GetComp<CompSanitation>()!.IsDirty &&
                 Nearly(domestic.GetComp<CompDishwasher>()!.UsedCapacity, 0f),
            new EndToEndDeadline(1_800, 8_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause after the loaded domestic cycle",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the domestic cycle preserves the exact stack and overflow",
            _ => AssertDomesticCompleted());
        yield return new SelectionActionStep(
            "select the exact returned domestic plate stack",
            new[] { domesticLoad.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the exact returned domestic stack",
            new[] { domesticLoad.ThingID, domesticOverflow.ThingID, domestic.ThingID },
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "exact domestic stack returned clean while overflow remains dirty",
            Array.Empty<string>(),
            paddingPixels: 0);

        domesticOverflow.SetForbidden(true, warnOnFail: false);
        Activate(industrialWorker, cleaning);
        yield return new TimeControlActionStep(
            "run ordinary industrial dishwashing work",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the industrial cleaner starts Doing dishes",
            _ => IsDoingDishesTo(industrialWorker, industrial),
            new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(35)));
        yield return new SelectionActionStep(
            "select the industrial cleaner and dishwasher during native hauling",
            new[] { industrialWorker.ThingID, industrial.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame native industrial dishwasher hauling",
            new[] { industrialWorker.ThingID, industrial.ThingID }
                .Concat(industrialLoads.Select(thing => thing.ThingID)),
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "ordinary cleaner hauling mixed dirty stacks to the industrial dishwasher",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new TimeControlActionStep(
            "finish industrial dishwasher admission",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the industrial dishwasher reaches exactly 64 place settings",
            _ => Nearly(industrial.GetComp<CompDishwasher>()!.UsedCapacity, 64f),
            new EndToEndDeadline(1_800, 8_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause the full industrial dishwasher",
            paused: true,
            EndToEndGameSpeed.Normal);
        industrialOverflow.SetForbidden(false, warnOnFail: false);
        industrialWorker.jobs.EndCurrentJob(JobCondition.InterruptForced);
        var industrialProbeEndTick = Find.TickManager.TicksGame + 500;
        yield return new TimeControlActionStep(
            "let ordinary Cleaning reconsider the allowed industrial overflow",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the full industrial appliance rejects allowed overflow through ordinary work time",
            _ => Find.TickManager.TicksGame >= industrialProbeEndTick,
            new EndToEndDeadline(600, 1_500, TimeSpan.FromSeconds(20)));
        yield return new TimeControlActionStep(
            "pause after the industrial capacity rejection window",
            paused: true,
            EndToEndGameSpeed.Normal);
        Deactivate(industrialWorker, cleaning);
        yield return new AssertionStep(
            "the industrial appliance is full and rejects overflow",
            _ => AssertIndustrialFull());
        yield return new SelectionActionStep(
            "select the full industrial dishwasher",
            new[] { industrial.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the full industrial dishwasher and rejected overflow",
            new[] { industrial.ThingID, industrialOverflow.ThingID, industrialWorker.ThingID },
            paddingPixels: 200);
        yield return new ScreenshotStep(
            "industrial dishwasher visibly holds 64 of 64 mixed place settings",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish the industrial dishwasher cycle",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "all exact industrial stacks return clean",
            _ => industrialLoads.All(thing =>
                     thing.Spawned && !thing.GetComp<CompSanitation>()!.IsDirty) &&
                 Nearly(industrial.GetComp<CompDishwasher>()!.UsedCapacity, 0f),
            new EndToEndDeadline(1_800, 8_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause after the industrial cycle",
            paused: true,
            EndToEndGameSpeed.Normal);
        domesticOverflow.SetForbidden(false, warnOnFail: false);
        yield return new AssertionStep(
            "the industrial cycle preserves every exact mixed stack",
            _ => AssertIndustrialCompleted());
        yield return new SelectionActionStep(
            "select one exact returned industrial plate stack",
            new[] { industrialLoads[0].ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame all exact returned industrial stacks",
            industrialLoads.Select(thing => thing.ThingID)
                .Append(industrialOverflow.ThingID)
                .Append(industrial.ThingID),
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "exact mixed industrial stacks returned clean while overflow remains dirty",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "local dishwasher capacity persistence result",
            _ => new Dictionary<string, string>
            {
                ["domestic"] = domesticId + ":16:" + domesticSnapshot.ThingId,
                ["industrial"] = industrialId + ":64:" +
                                 string.Join(",", industrialSnapshots.Select(snapshot => snapshot.ThingId)),
                ["domesticOverflow"] = domesticOverflowId,
                ["industrialOverflow"] = industrialOverflowId,
                ["save"] = SaveName
            });
    }

    private void AssertDomesticFull()
    {
        var comp = domestic.GetComp<CompDishwasher>()!;
        EndToEndAssert.True(Nearly(comp.UsedCapacity, 16f),
            "The domestic dishwasher must hold exactly 16 plate-equivalents.");
        EndToEndAssert.False(comp.CanAccept(domesticOverflow),
            "The full domestic dishwasher must reject one additional dirty plate.");
        EndToEndAssert.True(domesticOverflow.Spawned &&
                            domesticOverflow.GetComp<CompSanitation>()!.IsDirty,
            "Rejected domestic overflow must remain spawned and dirty.");
        var held = comp.GetDirectlyHeldThings().Cast<Thing>().ToList();
        EndToEndAssert.Equal(1, held.Count,
            "The domestic dishwasher must retain one exact stack.");
        domesticLoad = (ThingWithComps)held[0];
        domesticSnapshot.AssertSame(domesticLoad, expectedDirty: true);
    }

    private void AssertDomesticCompleted()
    {
        domesticSnapshot.AssertSame(domesticLoad, expectedDirty: false);
        EndToEndAssert.True(domesticLoad.Spawned,
            "The exact domestic stack must return to the map.");
        EndToEndAssert.True(domesticOverflow.Spawned &&
                            domesticOverflow.GetComp<CompSanitation>()!.IsDirty,
            "Domestic overflow must remain untouched and dirty.");
        AssertTotalPlateUnits(82);
    }

    private void AssertIndustrialFull()
    {
        var comp = industrial.GetComp<CompDishwasher>()!;
        EndToEndAssert.True(Nearly(comp.UsedCapacity, 64f),
            "The industrial dishwasher must hold exactly 64 plate-equivalents.");
        EndToEndAssert.False(comp.CanAccept(industrialOverflow),
            "The full industrial dishwasher must reject one additional dirty plate.");
        EndToEndAssert.True(industrialOverflow.Spawned &&
                            industrialOverflow.GetComp<CompSanitation>()!.IsDirty,
            "Rejected industrial overflow must remain spawned and dirty.");
        var held = comp.GetDirectlyHeldThings()
            .OfType<ThingWithComps>()
            .ToDictionary(thing => thing.ThingID, StringComparer.Ordinal);
        EndToEndAssert.Equal(3, held.Count,
            "The industrial dishwasher must retain all three distinct mixed stacks.");
        industrialLoads.Clear();
        foreach (var snapshot in industrialSnapshots)
        {
            EndToEndAssert.True(held.TryGetValue(snapshot.ThingId, out var thing),
                "The industrial dishwasher lost exact stack " + snapshot.ThingId + ".");
            snapshot.AssertSame(thing!, expectedDirty: true);
            industrialLoads.Add(thing!);
        }
    }

    private void AssertIndustrialCompleted()
    {
        var byId = industrialLoads.ToDictionary(thing => thing.ThingID, StringComparer.Ordinal);
        foreach (var snapshot in industrialSnapshots)
        {
            EndToEndAssert.True(byId.TryGetValue(snapshot.ThingId, out var thing),
                "The industrial output lost exact stack " + snapshot.ThingId + ".");
            snapshot.AssertSame(thing!, expectedDirty: false);
            EndToEndAssert.True(thing!.Spawned,
                "Industrial output stack " + snapshot.ThingId + " must return to the map.");
        }

        EndToEndAssert.True(industrialOverflow.Spawned &&
                            industrialOverflow.GetComp<CompSanitation>()!.IsDirty,
            "Industrial overflow must remain untouched and dirty.");
        AssertTotalPlateUnits(82);
    }

    private void ResolveLoadedFixtures()
    {
        map = Current.Game.CurrentMap;
        domestic = ResolveSpawned<ThingWithComps>(domesticId);
        industrial = ResolveSpawned<ThingWithComps>(industrialId);
        domesticWorker = ResolvePawn(domesticWorkerId);
        industrialWorker = ResolvePawn(industrialWorkerId);
        domesticOverflow = ResolveSpawned<ThingWithComps>(domesticOverflowId);
        industrialOverflow = ResolveSpawned<ThingWithComps>(industrialOverflowId);
        var held = domestic.GetComp<CompDishwasher>()!.GetDirectlyHeldThings()
            .OfType<ThingWithComps>()
            .SingleOrDefault(thing => thing.ThingID == domesticSnapshot.ThingId) ??
            throw new EndToEndAssertionException(
                "Native save/load lost the exact held domestic stack " + domesticSnapshot.ThingId + ".");
        domesticLoad = held;

        industrialLoads.Clear();
        foreach (var snapshot in industrialSnapshots)
        {
            industrialLoads.Add(ResolveSpawned<ThingWithComps>(snapshot.ThingId));
        }
    }

    private T ResolveSpawned<T>(string thingId) where T : Thing =>
        map.listerThings.AllThings.OfType<T>().SingleOrDefault(thing => thing.ThingID == thingId) ??
        throw new EndToEndAssertionException("The loaded map lost exact Thing " + thingId + ".");

    private Pawn ResolvePawn(string thingId) =>
        map.mapPawns.AllPawnsSpawned.SingleOrDefault(pawn => pawn.ThingID == thingId) ??
        throw new EndToEndAssertionException("The loaded map lost exact pawn " + thingId + ".");

    private void AssertTotalPlateUnits(int expected)
    {
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var spawned = map.listerThings.ThingsOfDef(plateDef).Sum(thing => thing.stackCount);
        var held = domestic.GetComp<CompDishwasher>()!.GetDirectlyHeldThings()
                       .Cast<Thing>().Sum(thing => thing.stackCount) +
                   industrial.GetComp<CompDishwasher>()!.GetDirectlyHeldThings()
                       .Cast<Thing>().Sum(thing => thing.stackCount);
        EndToEndAssert.Equal(expected, spawned + held,
            "Dishwasher admission/output must conserve every physical plate unit.");
    }

    private static void Activate(Pawn pawn, WorkTypeDef cleaning)
    {
        pawn.workSettings.SetPriority(cleaning, 1);
        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    private static void Deactivate(Pawn pawn, WorkTypeDef cleaning)
    {
        pawn.workSettings.SetPriority(cleaning, 0);
        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    private static bool IsDoingDishesTo(Pawn pawn, Thing dishwasher) =>
        pawn.CurJobDef == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
        ReferenceEquals(pawn.CurJob?.GetTarget(TargetIndex.B).Thing, dishwasher);

    private static ThingWithComps MakeDirtyPlateStack(
        ThingDef stuff,
        int count,
        QualityCategory quality,
        int hitPoints)
    {
        var plate = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Plate", stuff);
        plate.stackCount = count;
        plate.HitPoints = Math.Min(plate.MaxHitPoints, hitPoints);
        plate.GetComp<CompQuality>()!.SetQuality(quality, ArtGenerationContext.Colony);
        plate.GetComp<CompSanitation>()!.MarkDirty();
        return plate;
    }

    private static Pawn CreateCleaner(string name)
    {
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var pawn = FoodSearchE2EFixture.CreateColonist(name);
            if (!pawn.WorkTypeIsDisabled(WorkTypeDefOf.Cleaning) &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.9f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.9f)
            {
                if (pawn.needs?.food is { } food)
                {
                    food.CurLevelPercentage = 1f;
                }

                if (pawn.needs?.rest is { } rest)
                {
                    rest.CurLevelPercentage = 1f;
                }

                for (var hour = 0; hour < 24; hour++)
                {
                    pawn.timetable?.SetAssignment(hour, TimeAssignmentDefOf.Work);
                }

                return pawn;
            }

            pawn.Destroy(DestroyMode.Vanish);
        }

        throw new EndToEndAssertionException("Could not generate a capable dishwasher cleaner.");
    }

    private static IReadOnlyList<IntVec3> FindRoomCenters(Map map, int count)
    {
        var centers = new List<IntVec3>();
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 70f, useCenter: true))
        {
            if (centers.Any(center => center.DistanceToSquared(candidate) < 256) ||
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

        throw new EndToEndAssertionException(
            "Could not find two separated dishwasher capacity rooms.");
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
                    cell.GetEdifice(map) is not null ||
                    cell.GetFirstPawn(map) is not null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void PreserveSettings(IEndToEndContext context)
    {
        var settings = ImmersiveChefsMod.Settings;
        var priorPrefer = settings.PreferDishwashers;
        var priorTerrain = settings.AllowTerrainHandwashing;
        var priorWorkScale = settings.DishwashingWorkScale;
        context.DeferCleanup(() =>
        {
            settings.PreferDishwashers = priorPrefer;
            settings.AllowTerrainHandwashing = priorTerrain;
            settings.DishwashingWorkScale = priorWorkScale;
        });
        settings.PreferDishwashers = true;
        settings.AllowTerrainHandwashing = false;
        settings.DishwashingWorkScale = 0.25f;
    }

    private static bool Nearly(float actual, float expected) =>
        Math.Abs(actual - expected) < 0.001f;

    private sealed class WareSnapshot
    {
        private WareSnapshot(
            string thingId,
            string defName,
            string stuffDefName,
            QualityCategory quality,
            int hitPoints,
            int stackCount)
        {
            ThingId = thingId;
            DefName = defName;
            StuffDefName = stuffDefName;
            Quality = quality;
            HitPoints = hitPoints;
            StackCount = stackCount;
        }

        internal string ThingId { get; }
        private string DefName { get; }
        private string StuffDefName { get; }
        private QualityCategory Quality { get; }
        private int HitPoints { get; }
        private int StackCount { get; }

        internal static WareSnapshot Capture(ThingWithComps thing) => new(
            thing.ThingID,
            thing.def.defName,
            thing.Stuff?.defName ?? string.Empty,
            thing.GetComp<CompQuality>()!.Quality,
            thing.HitPoints,
            thing.stackCount);

        internal void AssertSame(ThingWithComps thing, bool expectedDirty)
        {
            EndToEndAssert.Equal(ThingId, thing.ThingID,
                "Dishwashing must preserve exact stack identity.");
            EndToEndAssert.Equal(DefName, thing.def.defName,
                "Dishwashing must preserve the ware Def.");
            EndToEndAssert.Equal(StuffDefName, thing.Stuff?.defName ?? string.Empty,
                "Dishwashing must preserve Stuff.");
            EndToEndAssert.Equal(Quality, thing.GetComp<CompQuality>()!.Quality,
                "Dishwashing must preserve crafting quality.");
            EndToEndAssert.Equal(HitPoints, thing.HitPoints,
                "Dishwashing must preserve hit points.");
            EndToEndAssert.Equal(StackCount, thing.stackCount,
                "Dishwashing must preserve the physical stack count.");
            EndToEndAssert.Equal(expectedDirty, thing.GetComp<CompSanitation>()!.IsDirty,
                "Dishwashing must change only the expected sanitation state.");
        }
    }
}
