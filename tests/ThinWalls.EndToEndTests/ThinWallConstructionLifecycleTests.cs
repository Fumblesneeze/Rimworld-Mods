using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using Verse;
using Verse.AI;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest(
    "thin-walls.native-construction-save-deconstruct",
    "fumblesneeze.thinwalls",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.thinwalls",
    MaxFrames = 7_200,
    MaxGameTicks = 18_000,
    MaxWallClockSeconds = 210)]
public sealed class ThinWallConstructionLifecycleTest : IRimWorldEndToEndTest
{
    private readonly List<Thing> fixtures = new();
    private Map map = null!;
    private ThingDef thinWallDef = null!;
    private Pawn builder = null!;
    private Building coreWall = null!;
    private EndToEndGizmoOption build = null!;
    private IntVec3 center;
    private IntVec3 buildCell;
    private IntVec3 cancelCell;
    private Thing? blueprint;
    private Frame? frame;
    private Building_ThinWall? wall;
    private string wallThingId = string.Empty;
    private ThingDef? usedStuff;
    private int initialAvailableStuff;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        thinWallDef = DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName);
        center = FindClearCenter(map, 8);
        buildCell = center;
        cancelCell = center + new IntVec3(0, 0, 2);

        int originalTicks = Find.TickManager.TicksGame;
        int originalAbsoluteStart = Find.TickManager.gameStartAbsTick;
        WeatherDef originalWeather = map.weatherManager.curWeather;
        WeatherDef originalLastWeather = map.weatherManager.lastWeather;
        int originalWeatherAge = map.weatherManager.curWeatherAge;
        float originalPrevSkyTargetLerp = map.weatherManager.prevSkyTargetLerp;
        float originalCurrSkyTargetLerp = map.weatherManager.currSkyTargetLerp;
        NormalizeToClearNoon(map);
        context.DeferCleanup(() =>
        {
            Find.TickManager.DebugSetTicksGame(originalTicks);
            Find.TickManager.gameStartAbsTick = originalAbsoluteStart;
            map.weatherManager.curWeather = originalWeather;
            map.weatherManager.lastWeather = originalLastWeather;
            map.weatherManager.curWeatherAge = originalWeatherAge;
            map.weatherManager.prevSkyTargetLerp = originalPrevSkyTargetLerp;
            map.weatherManager.currSkyTargetLerp = originalCurrSkyTargetLerp;
            map.weatherManager.ResetSkyTargetLerpCache();
        });

        builder = GenerateBuilder();
        GenSpawn.Spawn(builder, center + new IntVec3(0, 0, -1), map);
        fixtures.Add(builder);

        foreach (ThingDef stuff in new[]
                 {
                     ThingDefOf.WoodLog,
                     ThingDefOf.Steel,
                     DefDatabase<ThingDef>.GetNamed("BlocksGranite"),
                 })
        {
            Thing stack = ThingMaker.MakeThing(stuff);
            stack.stackCount = 3;
            GenSpawn.Spawn(stack, center + new IntVec3(-2, 0, fixtures.Count), map);
            fixtures.Add(stack);
        }

        build = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(Array.Empty<string>(), new[] { "Structure" })
            .Single(option => !option.Disabled &&
                              option.BuildableDefName == ThinWallUtility.ThinWallDefName &&
                              option.Interaction == EndToEndGizmoInteraction.Drag);

        context.DeferCleanup(() =>
        {
            Map cleanupMap = Find.CurrentMap;
            HashSet<string> fixtureIds = fixtures.Select(thing => thing.ThingID).ToHashSet();
            foreach (Thing thing in cleanupMap.listerThings.AllThings
                         .Where(thing => fixtureIds.Contains(thing.ThingID))
                         .ToArray())
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }

            foreach (Thing thing in cleanupMap.listerThings.AllThings
                         .Where(thing => thing.Position.InHorDistOf(center, 8f) &&
                                         ThinWallUtility.IsThinWallDef(thing.def))
                         .ToArray())
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }

        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CameraActionStep(
            "frame ordinary thin-wall construction area",
            fixtures.Select(thing => thing.ThingID),
            paddingPixels: 170);
        yield return new ScreenshotStep(
            "before native thin-wall designation with three-unit material stacks",
            fixtures.Select(thing => thing.ThingID),
            paddingPixels: 150);

        yield return BuildAt("designate one thin wall through native Structure tool", buildCell);
        yield return new WaitUntilStep(
            "native designation creates one ordinary blueprint",
            _ => (blueprint = FindPhase<Blueprint>(buildCell)) is not null,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        usedStuff = ((Blueprint_Build)blueprint!).stuffToUse;
        EndToEndAssert.NotNull(usedStuff, "The stuff-selecting designator must retain one concrete material.");
        initialAvailableStuff = CountNearby(usedStuff!);
        EndToEndAssert.Equal(3, initialAvailableStuff,
            "The selected construction material fixture must begin with exactly three units.");
        EndToEndAssert.True(blueprint is Blueprint_ThinWall,
            "The implied blueprint must use the Thin Walls edge renderer.");
        EndToEndAssert.True(builder.Ideo?.MembersCanBuild(blueprint) != false,
            "A Thin Wall is ordinary architecture and must not be ideology-locked.");

        yield return new SelectionActionStep(
            "clear selection before blueprint visual evidence",
            Array.Empty<string>(),
            additive: false);
        yield return new CameraActionStep(
            "close frame the ordinary thin-wall blueprint",
            new[] { blueprint.ThingID },
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "unselected native thin-wall blueprint before hauling",
            new[] { blueprint.ThingID },
            paddingPixels: 180);

        IReadOnlyList<EndToEndFloatMenuOption> allBlueprintOptions = context
            .GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(builder.ThingID, blueprint.ThingID);
        EndToEndFloatMenuOption[] prioritizeOptions = allBlueprintOptions
            .Where(option => !option.Disabled &&
                             option.Label.IndexOf("prioritize", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        JobFailReason.Clear();
        bool canConstruct = GenConstruct.CanConstruct(blueprint, builder, false, false);
        string constructFailure = JobFailReason.HaveReason ? JobFailReason.Reason : "none";
        EndToEndAssert.Equal(1, prioritizeOptions.Length,
            "The capable builder must expose exactly one enabled native prioritize-construction order; " +
            "canConstruct=" + canConstruct +
            "; canConstructForced=" + GenConstruct.CanConstruct(blueprint, builder, false, true) +
            "; normalDanger=" + builder.NormalMaxDanger() +
            "; pawnIdeo=" + (builder.Ideo?.name ?? "none") +
            "; ideoCanBuild=" + (builder.Ideo?.MembersCanBuild(blueprint).ToString() ?? "n/a") +
            "; failure=" + constructFailure +
            "; blocking=" + (GenConstruct.FirstBlockingThing(blueprint, builder)?.LabelCap.ToString() ?? "none") +
            "; canTouch=" + GenConstruct.CanTouchTargetFromValidCell(blueprint, builder) +
            "; canReserve=" + builder.CanReserve(blueprint) +
            "; canReach=" + builder.CanReach(blueprint, Verse.AI.PathEndMode.Touch, Danger.Deadly) +
            "; canReserveReach=" + builder.CanReserveAndReach(
                blueprint,
                Verse.AI.PathEndMode.Touch,
                Danger.Deadly) +
            "; blueprintFaction=" + (blueprint.Faction?.Name ?? "none") +
            "; materialCost=" + string.Join(",", ((Blueprint_Build)blueprint).TotalMaterialCost()
                .Select(cost => cost.thingDef.defName + ":" + cost.count)) +
            "; options=" + string.Join(" | ", allBlueprintOptions.Select(option =>
                option.Label + " [disabled=" + option.Disabled + "]")));
        yield return new FloatMenuActionStep(
            "prioritize the ordinary thin-wall blueprint",
            builder.ThingID,
            blueprint.ThingID,
            prioritizeOptions[0].StableId);
        yield return new TimeControlActionStep(
            "run ordinary hauling and construction",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new CheckpointStep(
            "ordinary construction preconditions",
            _ => new Dictionary<string, string>
            {
                ["builderConstructionDisabled"] = builder.WorkTypeIsDisabled(WorkTypeDefOf.Construction).ToString(),
                ["builderCurrentJob"] = builder.CurJobDef?.defName ?? "none",
                ["blueprintStuff"] = ((Blueprint_Build)blueprint!).stuffToUse?.defName ?? "none",
                ["steelAvailable"] = CountNearby(ThingDefOf.Steel).ToString(),
            });
        yield return new WaitUntilStep(
            "blueprint advances to a native frame",
            _ => (frame = FindPhase<Frame>(buildCell)) is not null,
            new EndToEndDeadline(2_400, 6_000, TimeSpan.FromSeconds(80)));
        EndToEndAssert.Equal(thinWallDef.frameDef.thingClass, frame!.GetType(),
            "The implied native frame class must reach the dedicated Thin Walls draw patch.");
        yield return new TimeControlActionStep(
            "pause the real thin-wall frame for visual evidence",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "clear selection before frame visual evidence",
            Array.Empty<string>(),
            additive: false);
        yield return new CameraActionStep(
            "close frame the ordinary thin-wall construction frame",
            new[] { frame!.ThingID },
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "unselected ordinary thin-wall frame during construction",
            new[] { frame!.ThingID },
            paddingPixels: 180);
        yield return new TimeControlActionStep(
            "resume ordinary construction after frame evidence",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "builder completes the thin wall through ordinary Construction work",
            _ => (wall = FindPhase<Building_ThinWall>(buildCell)) is not null,
            new EndToEndDeadline(3_600, 10_000, TimeSpan.FromSeconds(100)));
        yield return new TimeControlActionStep(
            "pause after native thin-wall construction",
            paused: true,
            EndToEndGameSpeed.Normal);

        coreWall = (Building)ThingMaker.MakeThing(ThingDefOf.Wall, usedStuff);
        coreWall.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(coreWall, buildCell + new IntVec3(2, 0, 0), map);
        fixtures.Add(coreWall);
        wallThingId = wall!.ThingID;
        string builderThingId = builder.ThingID;
        string coreWallThingId = coreWall.ThingID;
        yield return new AssertionStep(
            "construction consumes three material and creates a half-durability standable wall",
            _ =>
            {
                EndToEndAssert.Equal(0, CountNearby(usedStuff!),
                    "Exactly the three selected material units must be consumed.");
                EndToEndAssert.Equal(3, thinWallDef.costStuffCount,
                    "The live Thin Wall Def must require three units.");
                EndToEndAssert.Equal(coreWall.MaxHitPoints, wall.MaxHitPoints * 2,
                    "Matching Stuff must give the Core wall exactly twice the Thin Wall durability.");
                EndToEndAssert.Equal(Traversability.Standable, wall.def.passability,
                    "The completed owner cell must remain standable.");
                EndToEndAssert.False(wall.def.holdsRoof,
                    "The completed Thin Wall must not hold roofs.");
            });
        yield return new SelectionActionStep(
            "clear selection before completed wall comparison",
            Array.Empty<string>(),
            additive: false);
        yield return new CameraActionStep(
            "close frame completed thin wall and matching Core wall",
            new[] { wall.ThingID, coreWall.ThingID },
            paddingPixels: 160);
        yield return new ScreenshotStep(
            "unselected completed thin wall beside matching Stuff Core wall",
            new[] { wall.ThingID, coreWall.ThingID },
            paddingPixels: 160);

        yield return BuildAt("designate second thin wall for native cancellation", cancelCell);
        yield return new WaitUntilStep(
            "second designation creates a cancellable blueprint",
            _ => FindPhase<Blueprint>(cancelCell) is not null,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        Thing cancelBlueprint = FindPhase<Blueprint>(cancelCell)!;
        EndToEndGizmoOption cancel = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { cancelBlueprint.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled &&
                              option.Interaction == EndToEndGizmoInteraction.Invoke &&
                              option.Label.IndexOf("cancel", StringComparison.OrdinalIgnoreCase) >= 0);
        yield return new GizmoActionStep(
            "cancel second thin-wall blueprint through native command",
            new[] { cancelBlueprint.ThingID },
            cancel.RuntimeType,
            EndToEndGizmoInteraction.Invoke,
            stableGizmoId: cancel.StableId);
        yield return new WaitUntilStep(
            "native cancellation removes only the selected blueprint",
            _ => cancelBlueprint.Destroyed,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        yield return new AssertionStep(
            "cancellation leaves completed wall and co-located map content intact",
            _ => EndToEndAssert.True(wall is { Spawned: true } && builder.Spawned,
                "Cancelling another segment must not affect the completed wall or builder."));

        yield return new SaveLoadActionStep(
            "save and reload completed thin-wall owner",
            "thin-walls-construction-lifecycle");
        yield return new WaitUntilStep(
            "same thin-wall identity returns exactly once after reload",
            _ =>
            {
                map = Find.CurrentMap;
                wall = map.listerThings.AllThings
                    .OfType<Building_ThinWall>()
                    .SingleOrDefault(candidate => candidate.ThingID == wallThingId);
                builder = map.listerThings.AllThings
                    .OfType<Pawn>()
                    .SingleOrDefault(candidate => candidate.ThingID == builderThingId)!;
                coreWall = map.listerThings.AllThings
                    .OfType<Building>()
                    .SingleOrDefault(candidate => candidate.ThingID == coreWallThingId)!;
                return wall is not null && builder is not null && coreWall is not null;
            },
            new EndToEndDeadline(2_400, 2_000, TimeSpan.FromSeconds(80)));
        yield return new AssertionStep(
            "save load preserves owner edge material and hit points once",
            _ =>
            {
                EndToEndAssert.Equal(usedStuff, wall!.Stuff,
                    "Reloaded owner must retain the same Stuff Def.");
                EndToEndAssert.Equal(wall.MaxHitPoints, wall.HitPoints,
                    "Reloaded owner must retain its completed hit points.");
                EndToEndAssert.Equal(1, map.listerThings.AllThings.Count(thing => thing.ThingID == wallThingId),
                    "Reload must restore the segment identity exactly once.");
            });

        EndToEndGizmoOption deconstruct = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { wall!.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled &&
                              option.Interaction == EndToEndGizmoInteraction.Invoke &&
                              option.Label.IndexOf("deconstruct", StringComparison.OrdinalIgnoreCase) >= 0);
        yield return new GizmoActionStep(
            "order native deconstruction of reloaded thin wall",
            new[] { wall.ThingID },
            deconstruct.RuntimeType,
            EndToEndGizmoInteraction.Invoke,
            stableGizmoId: deconstruct.StableId);
        IReadOnlyList<EndToEndFloatMenuOption> deconstructOptions = context
            .GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(builder.ThingID, wall.ThingID);
        EndToEndFloatMenuOption[] prioritizeDeconstruction = deconstructOptions
            .Where(option => !option.Disabled &&
                             option.Label.IndexOf("prioritize", StringComparison.OrdinalIgnoreCase) >= 0 &&
                             option.Label.IndexOf("deconstruct", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, prioritizeDeconstruction.Length,
            "The reloaded wall must expose one enabled native prioritize-deconstruction order; " +
            "builder=" + builder.Position +
            "; wall=" + wall.Position +
            "; side=" + wall.OwnedSide +
            "; canReachTouch=" + builder.CanReach(wall, PathEndMode.Touch, Danger.Deadly) +
            "; options=" + string.Join(" | ", deconstructOptions.Select(option =>
                option.Label + " [disabled=" + option.Disabled + "]")));
        yield return new FloatMenuActionStep(
            "prioritize ordinary deconstruction of the reloaded thin wall",
            builder.ThingID,
            wall.ThingID,
            prioritizeDeconstruction[0].StableId);
        yield return new TimeControlActionStep(
            "run ordinary thin-wall deconstruction",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "ordinary Construction removes the selected thin wall",
            _ => wall.Destroyed,
            new EndToEndDeadline(3_000, 8_000, TimeSpan.FromSeconds(100)));
        yield return new TimeControlActionStep(
            "pause after thin-wall deconstruction",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep(
            "after native thin-wall cancellation save-load and deconstruction",
            new[] { builder.ThingID, coreWall.ThingID },
            paddingPixels: 190);
        yield return new CheckpointStep(
            "native construction lifecycle outcome",
            _ => new Dictionary<string, string>
            {
                ["wallThingId"] = wallThingId,
                ["stuffDef"] = usedStuff!.defName,
                ["thinMaxHitPoints"] = wall.MaxHitPoints.ToString(),
                ["coreMaxHitPoints"] = coreWall.MaxHitPoints.ToString(),
                ["cancelledBlueprintDestroyed"] = cancelBlueprint.Destroyed.ToString(),
                ["deconstructed"] = wall.Destroyed.ToString(),
            });
    }

    private GizmoActionStep BuildAt(string name, IntVec3 cell) => new(
        name,
        Array.Empty<string>(),
        build.RuntimeType,
        EndToEndGizmoInteraction.Drag,
        EndToEndCardinalRotation.South,
        new EndToEndBuildMaterial("Steel"),
        stableGizmoId: build.StableId,
        startCell: new EndToEndMapCell(cell.x, cell.z),
        endCell: new EndToEndMapCell(cell.x, cell.z),
        architectCategoryDefNames: new[] { "Structure" });

    private T? FindPhase<T>(IntVec3 cell) where T : Thing =>
        cell.GetThingList(map)
            .OfType<T>()
            .SingleOrDefault(thing => ThinWallUtility.IsThinWallDef(thing.def));

    private int CountNearby(ThingDef stuff) => map.listerThings.ThingsOfDef(stuff)
        .Where(thing => thing.Spawned && thing.Position.InHorDistOf(buildCell, 8f))
        .Sum(thing => thing.stackCount);

    private static Pawn GenerateBuilder()
    {
        for (int attempt = 0; attempt < 64; attempt++)
        {
            Pawn candidate = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            if (candidate.WorkTypeIsDisabled(WorkTypeDefOf.Construction) ||
                candidate.health.capacities.GetLevel(PawnCapacityDefOf.Moving) < 0.9f ||
                candidate.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) < 0.9f)
            {
                candidate.Destroy(DestroyMode.Vanish);
                continue;
            }

            candidate.Name = new NameTriple("Thin", "Wall Builder", "Tester");
            candidate.inventory?.innerContainer.ClearAndDestroyContents();
            candidate.workSettings.EnableAndInitialize();
            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (!candidate.WorkTypeIsDisabled(workType))
                {
                    candidate.workSettings.SetPriority(workType, 0);
                }
            }

            candidate.workSettings.SetPriority(WorkTypeDefOf.Construction, 1);
            candidate.skills.GetSkill(SkillDefOf.Construction).Level = 20;
            if (candidate.needs?.food is { } food)
            {
                food.CurLevelPercentage = 1f;
            }

            if (candidate.needs?.rest is { } rest)
            {
                rest.CurLevelPercentage = 1f;
            }

            return candidate;
        }

        throw new EndToEndAssertionException("Could not generate a capable Thin Walls builder.");
    }

    private static void NormalizeToClearNoon(Map target)
    {
        Find.TickManager.DebugSetTicksGame(0);
        Find.TickManager.gameStartAbsTick = GenDate.TicksPerYear + 30_000;
        for (int pass = 0; pass < 2; pass++)
        {
            int local = GenLocalDate.DayOfYear(target) * GenDate.TicksPerDay + GenLocalDate.DayTick(target);
            Find.TickManager.gameStartAbsTick += 30_000 - local;
        }

        target.weatherManager.curWeather = WeatherDefOf.Clear;
        target.weatherManager.lastWeather = WeatherDefOf.Clear;
        target.weatherManager.curWeatherAge = 0;
        target.weatherManager.prevSkyTargetLerp = 1f;
        target.weatherManager.currSkyTargetLerp = 1f;
        target.weatherManager.ResetSkyTargetLerpCache();
    }

    private static IntVec3 FindClearCenter(Map map, int radius)
    {
        for (int x = -60; x <= 60; x += 20)
        {
            for (int z = -60; z <= 60; z += 20)
            {
                IntVec3 candidate = map.Center + new IntVec3(x, 0, z);
                CellRect area = CellRect.CenteredOn(candidate, radius);
                if (area.InBounds(map) && area.Cells.All(cell =>
                        cell.Standable(map) &&
                        !cell.Fogged(map) &&
                        cell.GetThingList(map).Count == 0 &&
                        ThinWallEndToEndFixtureTerrain.SupportsEveryFixtureBuild(cell, map)))
                {
                    return candidate;
                }
            }
        }

        throw new EndToEndAssertionException("Could not find a clear Thin Walls construction area.");
    }
}
