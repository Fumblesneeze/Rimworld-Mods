using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using ThinWalls.Rendering;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest(
    "thin-walls.native-visual-identity-catalog",
    "fumblesneeze.thinwalls",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.thinwalls",
    MaxFrames = 6_000,
    MaxGameTicks = 9_500,
    MaxWallClockSeconds = 210)]
public sealed class ThinWallVisualIdentityTest : IRimWorldEndToEndTest
{
    private const int FixtureRadius = 25;
    private static readonly ThingDef InvisibleRouteTargetDef = new()
    {
        defName = "TW_E2E_InvisibleRouteTarget",
        label = "invisible route target",
        thingClass = typeof(Thing),
        category = ThingCategory.Item,
        drawerType = DrawerType.None,
        selectable = false,
        useHitPoints = false,
        stackLimit = 1,
    };
    private readonly List<Thing> fixtures = new();
    private readonly List<Building_ThinWall> walls = new();
    private readonly List<Building_ThinDoor> doors = new();
    private readonly List<Building> regularWalls = new();
    private readonly List<Thing> contextFixtures = new();
    private readonly List<Building> contextRegularWalls = new();
    private readonly List<Building_ThinWall> presentationJunctionWalls = new();
    private readonly Dictionary<IntVec3, TerrainDef> contextTerrains = new();
    private readonly HashSet<IntVec3> contextOccupied = new();
    private readonly List<(Thing Thing, IntVec3 Position, Rot4 Rotation)> displacedThings = new();
    private Map map = null!;
    private IntVec3 center;
    private IntVec3 presentationCenter;
    private EndToEndGizmoOption build = null!;
    private EndToEndGizmoOption doorBuild = null!;
    private Pawn doorPawn = null!;
    private Thing doorCrossingMarker = null!;
    private Pawn contextPawn = null!;
    private Thing contextCrossingMarker = null!;
    private Thing presentationExitMarker = null!;
    private Building_Door contextExteriorDoor = null!;
    private bool originalScreenshotMode;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        center = FindClearCenter(map, FixtureRadius);
        presentationCenter = FindClearCenterAwayFrom(map, center, minimumDistance: 60, radius: 18);

        bool originalGodMode = DebugSettings.godMode;
        int originalTicks = Find.TickManager.TicksGame;
        int originalAbsoluteStart = Find.TickManager.gameStartAbsTick;
        WeatherDef originalWeather = map.weatherManager.curWeather;
        WeatherDef originalLastWeather = map.weatherManager.lastWeather;
        int originalWeatherAge = map.weatherManager.curWeatherAge;
        float originalPrevSkyTargetLerp = map.weatherManager.prevSkyTargetLerp;
        float originalCurrSkyTargetLerp = map.weatherManager.currSkyTargetLerp;
        originalScreenshotMode = Find.ScreenshotModeHandler.Active;
        context.DeferCleanup(() =>
        {
            Find.ScreenshotModeHandler.Active = originalScreenshotMode;
            DebugSettings.godMode = originalGodMode;
            Find.TickManager.DebugSetTicksGame(originalTicks);
            Find.TickManager.gameStartAbsTick = originalAbsoluteStart;
            map.weatherManager.curWeather = originalWeather;
            map.weatherManager.lastWeather = originalLastWeather;
            map.weatherManager.curWeatherAge = originalWeatherAge;
            map.weatherManager.prevSkyTargetLerp = originalPrevSkyTargetLerp;
            map.weatherManager.currSkyTargetLerp = originalCurrSkyTargetLerp;
            map.weatherManager.ResetSkyTargetLerpCache();
        });
        context.DeferCleanup(() =>
        {
            foreach (Thing thing in fixtures
                         .Concat<Thing>(walls)
                         .Concat(doors)
                         .Concat(regularWalls)
                         .Distinct()
                         .ToArray())
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }

            foreach ((IntVec3 cell, TerrainDef terrain) in contextTerrains)
            {
                map.terrainGrid.SetTerrain(cell, terrain);
            }

            foreach ((Thing thing, IntVec3 position, Rot4 rotation) in displacedThings)
            {
                if (!thing.Destroyed && !thing.Spawned)
                {
                    GenSpawn.Spawn(thing, position, map, rotation);
                }
            }
        });

        DebugSettings.godMode = true;
        NormalizeToClearNoon(map);
        NormalizeBuildTerrain(CellRect.CenteredOn(center, FixtureRadius));
        DisplaceIncidentalThings(CellRect.CenteredOn(center, FixtureRadius));
        NormalizeBuildTerrain(CellRect.CenteredOn(presentationCenter, 18));
        DisplaceIncidentalThings(CellRect.CenteredOn(presentationCenter, 18));
        SpawnPresentationJunctions();

        foreach ((int z, string stuffDefName) in new[]
                 {
                     (2, "BlocksGranite"),
                     (5, "WoodLog"),
                     (8, "Steel"),
                 })
        {
            for (int x = 12; x <= 14; x++)
            {
                var regularWall = (Building)ThingMaker.MakeThing(
                    ThingDefOf.Wall,
                    DefDatabase<ThingDef>.GetNamed(stuffDefName));
                regularWall.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(regularWall, center + new IntVec3(x, 0, z), map);
                regularWalls.Add(regularWall);
            }
        }

        SpawnRegularRun(-14, -12, 10, "BlocksGranite");
        SpawnRegularRun(15, 17, -8, "WoodLog");
        SpawnRegularRun(12, 14, -1, "Steel");
        SpawnRegularRun(-13, -11, 13, "BlocksGranite");
        SpawnRegularRun(0, 1, 17, "BlocksGranite");
        SpawnRegularRun(-3, -1, 11, "BlocksGranite");
        SpawnRegularRun(3, 5, 11, "BlocksGranite");
        SpawnRegularRun(-17, -15, 6, "WoodLog");
        SpawnRegularRun(-17, -15, 0, "Steel");
        SpawnRegularRun(17, 20, -12, "BlocksGranite");
        SpawnRegularRun(17, 20, -4, "BlocksGranite");
        SpawnRegularVerticalRun(-18, -14, -11, "BlocksGranite");
        SpawnRegularVerticalRun(-18, -20, -17, "BlocksGranite");
        BuildContextRoom();

        doorPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        doorPawn.Name = new NameTriple("Visual", "Door", "Tester");
        GenSpawn.Spawn(doorPawn, center + new IntVec3(-10, 0, 15), map);
        fixtures.Add(doorPawn);
        doorPawn.drafter.Drafted = true;
        doorCrossingMarker = SpawnInvisibleRouteTarget(center + new IntVec3(-10, 0, 10));

        contextPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        contextPawn.Name = new NameTriple("Mara", "Vale", "");
        GenSpawn.Spawn(contextPawn, center + new IntVec3(-3, 0, -19), map);
        fixtures.Add(contextPawn);
        contextPawn.drafter.Drafted = true;
        contextCrossingMarker = SpawnInvisibleRouteTarget(center + new IntVec3(3, 0, -19));
        presentationExitMarker = SpawnInvisibleRouteTarget(center + new IntVec3(-1, 0, -25));
        EndToEndAssert.True(
            new[] { doorCrossingMarker.ThingID, contextCrossingMarker.ThingID, presentationExitMarker.ThingID }
                .Distinct()
                .Count() == 3,
            "The three invisible navigation targets must have distinct native Thing IDs.");
        contextFixtures.Add(contextPawn);
        contextFixtures.Add(contextCrossingMarker);

        IEndToEndGizmoCatalog catalog = context.GetRequiredService<IEndToEndGizmoCatalog>();
        EndToEndGizmoOption[] matches = catalog
            .Query(Array.Empty<string>(), new[] { "Structure" })
            .Where(option => !option.Disabled &&
                             option.BuildableDefName == ThinWallUtility.ThinWallDefName &&
                             option.Interaction == EndToEndGizmoInteraction.Drag)
            .ToArray();
        EndToEndAssert.Equal(1, matches.Length,
            "Structure must expose exactly one enabled native Thin Walls drag designator");
        build = matches[0];
        EndToEndGizmoOption[] doorMatches = catalog
            .Query(Array.Empty<string>(), new[] { "Structure" })
            .Where(option => !option.Disabled &&
                             option.BuildableDefName == ThinWallUtility.ThinDoorDefName &&
                             option.Interaction == EndToEndGizmoInteraction.Drag)
            .ToArray();
        EndToEndAssert.Equal(1, doorMatches.Length,
            "Structure must expose exactly one enabled native Thin Door drag designator");
        doorBuild = doorMatches[0];
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        if (Find.WindowStack.Windows.Any(window =>
                string.Equals(
                    window.GetType().FullName,
                    "LudeonTK.EditWindow_Log",
                    StringComparison.Ordinal)))
        {
            yield return new WindowCancelActionStep(
                "close the startup developer log before observing Thin Walls",
                "LudeonTK.EditWindow_Log");
        }

        yield return Drag("seven-segment granite horizontal running-bond sample", -10, 7, -4, 7, EndToEndCardinalRotation.South, "BlocksGranite");
        yield return Drag("seven-segment granite north-south running-bond sample", -8, 6, -8, 0, EndToEndCardinalRotation.West, "BlocksGranite");
        yield return Drag("wood OSB horizontal projection sample", -3, 7, 1, 7, EndToEndCardinalRotation.South, "WoodLog");
        yield return Drag("wood OSB north-south projection sample", -2, 5, -2, 2, EndToEndCardinalRotation.West, "WoodLog");
        yield return Drag("steel riveted horizontal projection sample", 3, 7, 7, 7, EndToEndCardinalRotation.South, "Steel");
        yield return Drag("steel riveted north-south projection sample", 4, 5, 4, 2, EndToEndCardinalRotation.West, "Steel");

        yield return Drag("wood L horizontal arm", -10, -3, -8, -3, EndToEndCardinalRotation.South, "WoodLog");
        yield return Drag("wood L vertical arm", -10, 0, -10, -3, EndToEndCardinalRotation.West, "WoodLog");
        yield return Drag("steel T crossbar", -4, -3, 0, -3, EndToEndCardinalRotation.South, "Steel");
        yield return Drag("steel T stem", -2, 0, -2, -3, EndToEndCardinalRotation.West, "Steel");
        yield return Drag("granite plus horizontal arm", 4, -3, 8, -3, EndToEndCardinalRotation.South, "BlocksGranite");
        yield return Drag("granite plus vertical arm", 6, -1, 6, -5, EndToEndCardinalRotation.West, "BlocksGranite");
        yield return Drag("granite extension into material-matched Core wall", 9, 2, 11, 2, EndToEndCardinalRotation.South, "BlocksGranite");
        yield return Drag("wood extension into material-matched Core wall", 9, 5, 11, 5, EndToEndCardinalRotation.South, "WoodLog");
        yield return Drag("steel extension into material-matched Core wall", 9, 8, 11, 8, EndToEndCardinalRotation.South, "Steel");
        yield return Drag("reverse granite horizontal extension into Core wall", -11, 10, -9, 10, EndToEndCardinalRotation.South, "BlocksGranite");
        yield return Drag("wood north-ray extension into Core wall", 15, -7, 15, -5, EndToEndCardinalRotation.West, "WoodLog");
        yield return Drag("steel south-ray extension into Core wall", 12, -4, 12, -2, EndToEndCardinalRotation.West, "Steel");
        yield return Drag("west ray at mixed Core-wall vertex", -3, 17, -1, 17, EndToEndCardinalRotation.South, "BlocksGranite");
        yield return Drag("south ray at mixed Core-wall vertex", 0, 14, 0, 16, EndToEndCardinalRotation.West, "BlocksGranite");
        yield return Drag("west ray into south-side Core wall", -6, 11, -4, 11, EndToEndCardinalRotation.North, "BlocksGranite");
        yield return Drag("east ray into south-side Core wall", 8, 11, 6, 11, EndToEndCardinalRotation.North, "BlocksGranite");
        yield return Drag("north ray into west-side Core wall", -15, 7, -15, 9, EndToEndCardinalRotation.East, "WoodLog");
        yield return Drag("south ray into west-side Core wall", -15, -3, -15, -1, EndToEndCardinalRotation.East, "Steel");
        yield return Drag("isolated south side-T centered between two Core walls", 19, -15, 19, -13, EndToEndCardinalRotation.West, "BlocksGranite");
        yield return Drag("isolated north side-T centered between two Core walls", 19, -3, 19, -1, EndToEndCardinalRotation.West, "BlocksGranite");
        yield return Drag("isolated west side-T centered between two Core walls", -21, -12, -19, -12, EndToEndCardinalRotation.South, "BlocksGranite");
        yield return Drag("isolated east side-T centered between two Core walls", -17, -18, -15, -18, EndToEndCardinalRotation.South, "BlocksGranite");

        yield return Door("granite mixed-run horizontal Thin Door", -10, 13, EndToEndCardinalRotation.South, "BlocksGranite");
        yield return Door("granite catalog horizontal Thin Door", 10, 20, EndToEndCardinalRotation.South, "BlocksGranite");
        yield return Door("granite catalog north-south Thin Door", 12, 19, EndToEndCardinalRotation.East, "BlocksGranite");
        yield return Door("granite catalog reverse north-south Thin Door", 8, 19, EndToEndCardinalRotation.West, "BlocksGranite");
        yield return Drag("granite East-door south continuation", 12, 18, 12, 18, EndToEndCardinalRotation.East, "BlocksGranite");
        yield return Drag("granite East-door north continuation", 12, 20, 12, 20, EndToEndCardinalRotation.East, "BlocksGranite");
        yield return Drag("granite West-door south continuation", 8, 18, 8, 18, EndToEndCardinalRotation.West, "BlocksGranite");
        yield return Drag("granite West-door north continuation", 8, 20, 8, 20, EndToEndCardinalRotation.West, "BlocksGranite");
        yield return Door("wood closed horizontal Thin Door", -3, 13, EndToEndCardinalRotation.South, "WoodLog");
        yield return Door("wood closed north-south Thin Door", -1, 13, EndToEndCardinalRotation.East, "WoodLog");
        yield return Door("steel closed horizontal Thin Door", 4, 13, EndToEndCardinalRotation.South, "Steel");
        yield return Door("steel closed north-south Thin Door", 6, 13, EndToEndCardinalRotation.East, "Steel");
        yield return Drag("granite Thin Wall continuation after Core-wall-connected Thin Door", -9, 13, -7, 13, EndToEndCardinalRotation.South, "BlocksGranite");

        string[] damageMaterials = { "BlocksGranite", "WoodLog", "Steel" };
        int[] damageStarts = { -8, -2, 4 };
        for (int materialIndex = 0; materialIndex < damageMaterials.Length; materialIndex++)
        {
            for (int gradeIndex = 0; gradeIndex < 3; gradeIndex++)
            {
                int x = damageStarts[materialIndex] + gradeIndex * 2;
                yield return Drag(
                    damageMaterials[materialIndex] + " damage grade " + (gradeIndex + 1),
                    x,
                    -10,
                    x,
                    -10,
                    EndToEndCardinalRotation.South,
                    damageMaterials[materialIndex]);
            }

            yield return Drag(
                damageMaterials[materialIndex] + " north-south damage projection",
                damageStarts[materialIndex],
                -7,
                damageStarts[materialIndex],
                -7,
                EndToEndCardinalRotation.West,
                damageMaterials[materialIndex]);
        }

        yield return Drag("steel unique shared-edge damage run", 9, -13, 11, -13, EndToEndCardinalRotation.South, "Steel");

        yield return Drag("wood lived-in room partition south run", 1, -22, 1, -20, EndToEndCardinalRotation.West, "WoodLog");
        yield return Door("wood lived-in room centered partition Thin Door", 1, -19, EndToEndCardinalRotation.West, "WoodLog");
        yield return Drag("wood lived-in room partition north run", 1, -18, 1, -16, EndToEndCardinalRotation.West, "WoodLog");

        yield return new WaitUntilStep(
            "native designator materializes the visual identity catalog",
            _ => CaptureWalls() == 134 && CaptureDoors() == 9,
            new EndToEndDeadline(480, 1_200, TimeSpan.FromSeconds(30)));
        yield return new AssertionStep(
            "both owner orientations keep connected closed Thin Door leaves inside collinear runs",
            _ =>
            {
                foreach ((int x, ThinWallSide side) in new[]
                         {
                             (8, ThinWallSide.West),
                             (12, ThinWallSide.East),
                         })
                {
                    Building_ThinDoor connected = RequireDoor(center + new IntVec3(x, 0, 19), side);
                    SharedEdge edge = connected.OwnedEdge.Shared;
                    EndToEndAssert.True(walls.Any(wall =>
                            wall.OwnedEdge.Shared.PositiveSide == edge.PositiveSide &&
                            wall.OwnedEdge.Shared.AnchorCell.x == edge.AnchorCell.x &&
                            wall.OwnedEdge.Shared.AnchorCell.z == edge.AnchorCell.z - 1),
                        $"{side} connected door requires a collinear completed Thin Wall immediately south.");
                    EndToEndAssert.True(walls.Any(wall =>
                            wall.OwnedEdge.Shared.PositiveSide == edge.PositiveSide &&
                            wall.OwnedEdge.Shared.AnchorCell.x == edge.AnchorCell.x &&
                            wall.OwnedEdge.Shared.AnchorCell.z == edge.AnchorCell.z + 1),
                        $"{side} connected door requires a collinear completed Thin Wall immediately north.");
                }
            });
        var damageTargets = new List<EndToEndHitPointFixture>();
        float[] ratios = { 0.72f, 0.48f, 0.22f };
        for (int materialIndex = 0; materialIndex < damageMaterials.Length; materialIndex++)
        {
            for (int gradeIndex = 0; gradeIndex < ratios.Length; gradeIndex++)
            {
                int x = damageStarts[materialIndex] + gradeIndex * 2;
                damageTargets.Add(new EndToEndHitPointFixture(
                    RequireWall(center + new IntVec3(x, 0, -10), ThinWallSide.South).ThingID,
                    ratios[gradeIndex]));
            }
        }

        Building_ThinWall[] damagedVertical = damageMaterials
            .Select((_, materialIndex) => RequireWall(
                center + new IntVec3(damageStarts[materialIndex], 0, -7),
                ThinWallSide.West))
            .ToArray();
        Building_ThinWall damagedJunctionPainter = RequireWall(
            center + new IntVec3(-10, 0, -3),
            ThinWallSide.South);
        Building_ThinWall damagedUniqueEdgeOwner = RequireWall(
            center + new IntVec3(10, 0, -13),
            ThinWallSide.South);
        foreach (Building_ThinWall vertical in damagedVertical)
        {
            damageTargets.Add(new EndToEndHitPointFixture(vertical.ThingID, 0.48f));
        }
        damageTargets.Add(new EndToEndHitPointFixture(damagedJunctionPainter.ThingID, 0.48f));
        damageTargets.Add(new EndToEndHitPointFixture(damagedUniqueEdgeOwner.ThingID, 0.22f));
        Building_ThinDoor moderatelyDamagedGraniteDoor = RequireDoor(
            center + new IntVec3(10, 0, 20), ThinWallSide.South);
        Building_ThinDoor heavilyDamagedWoodDoor = RequireDoor(
            center + new IntVec3(-1, 0, 13), ThinWallSide.East);
        Building_ThinDoor severelyDamagedSteelDoor = RequireDoor(
            center + new IntVec3(4, 0, 13), ThinWallSide.South);
        damageTargets.Add(new EndToEndHitPointFixture(moderatelyDamagedGraniteDoor.ThingID, 0.72f));
        damageTargets.Add(new EndToEndHitPointFixture(heavilyDamagedWoodDoor.ThingID, 0.48f));
        damageTargets.Add(new EndToEndHitPointFixture(severelyDamagedSteelDoor.ThingID, 0.22f));
        yield return new SupportingHitPointFixtureActionStep(
            "supporting setup for wall door junction and unique-edge damage rendering",
            damageTargets);
        yield return new TimeControlActionStep(
            "allow newly constructed Thin Doors to finish their native close cycle",
            false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "every untouched Thin Door is fully closed before closed-state evidence",
            _ => doors.All(door => !door.Open && door.OpenFraction <= 0.05f),
            new EndToEndDeadline(900, 1_500, TimeSpan.FromSeconds(35)));
        yield return new TimeControlActionStep(
            "pause the fully closed Thin Door catalog",
            true,
            EndToEndGameSpeed.Normal);
        IntVec3 mixedVertex = center + new IntVec3(0, 0, 17);
        Building_ThinWall[] nativeMixedRayOwners = walls
            .Where(wall => EdgeTouchesCorner(wall.OwnedEdge.Shared, mixedVertex))
            .ToArray();
        yield return new AssertionStep(
            "mixed regular-wall vertex contains exactly the two native designated incident owners",
            _ =>
            {
                EndToEndAssert.Equal(
                    2,
                    nativeMixedRayOwners.Length,
                    "The mixed join evidence must contain only two incident edge owners.");
                EndToEndAssert.True(
                    nativeMixedRayOwners.Any(wall =>
                        wall.Position == center + new IntVec3(-1, 0, 17) &&
                        wall.OwnedSide == ThinWallSide.South),
                    "The native west-ray endpoint owner must reach the mixed vertex.");
                EndToEndAssert.True(
                    nativeMixedRayOwners.Any(wall =>
                        wall.Position == center + new IntVec3(0, 0, 16) &&
                        wall.OwnedSide == ThinWallSide.West),
                    "The native south-ray endpoint owner must reach the mixed vertex.");
            });
        yield return new CheckpointStep("record visual identity catalog composition", _ =>
        {
            Dictionary<string, string> material = DescribeCoreWallMaterial(walls[0]);
            return new Dictionary<string, string>
        {
            ["wallCount"] = walls.Count.ToString(),
            ["stone"] = walls.Count(wall => wall.Stuff?.defName == "BlocksGranite").ToString(),
            ["wood"] = walls.Count(wall => wall.Stuff == ThingDefOf.WoodLog).ToString(),
            ["metal"] = walls.Count(wall => wall.Stuff == ThingDefOf.Steel).ToString(),
            ["damaged"] = walls.Count(wall => wall.HitPoints < wall.MaxHitPoints).ToString(),
            ["verticalDamaged"] = damagedVertical.Count(wall => wall.HitPoints < wall.MaxHitPoints).ToString(),
            ["junctionPainterDamaged"] = (damagedJunctionPainter.HitPoints < damagedJunctionPainter.MaxHitPoints).ToString(),
            ["uniqueSharedEdgeDamaged"] = (damagedUniqueEdgeOwner.HitPoints < damagedUniqueEdgeOwner.MaxHitPoints).ToString(),
            ["doorCount"] = doors.Count.ToString(),
            ["horizontalDoors"] = doors.Count(door => door.OwnedSide == ThinWallSide.North || door.OwnedSide == ThinWallSide.South).ToString(),
            ["verticalDoors"] = doors.Count(door => door.OwnedSide == ThinWallSide.East || door.OwnedSide == ThinWallSide.West).ToString(),
            ["damagedDoors"] = doors.Count(door => door.HitPoints < door.MaxHitPoints).ToString(),
            ["regularWallJoinCount"] = regularWalls.Count.ToString(),
            ["connectedRegularWallRuns"] = "16",
            ["isolatedVertexCenteredSideTRuns"] = "4",
            ["contextOuterShellWalls"] = contextRegularWalls.Count.ToString(),
            ["contextFurniture"] = contextFixtures.Count(thing => thing is Building).ToString(),
            ["nativeMixedRayOwners"] = nativeMixedRayOwners.Length.ToString(),
            ["coreMaterialName"] = material["name"],
            ["coreMaterialTexture"] = material["texture"],
            ["coreMaterialScale"] = material["scale"],
            ["coreMaterialOffset"] = material["offset"],
        };
        });

        yield return new ScreenshotModeActionStep(
            "enable native screenshot mode for clean visual-identity evidence",
            enabled: true);

        string[] farUsefulIds = walls
            .Where(wall => wall.Position.x >= center.x - 11 && wall.Position.x <= center.x + 14 &&
                           wall.Position.z >= center.z - 5 && wall.Position.z <= center.z + 13)
            .Select(wall => wall.ThingID)
            .Concat(doors
                .Where(door => door.Position.x >= center.x - 11 && door.Position.x <= center.x + 14 &&
                               door.Position.z >= center.z - 5 && door.Position.z <= center.z + 13)
                .Select(door => door.ThingID))
            .Concat(regularWalls
                .Where(wall => wall.Position.x >= center.x - 11 && wall.Position.x <= center.x + 14 &&
                               wall.Position.z >= center.z - 5 && wall.Position.z <= center.z + 13)
                .Select(wall => wall.ThingID))
            .ToArray();
        yield return new CameraActionStep("far useful gameplay zoom visual identity catalog", farUsefulIds, paddingPixels: 140);
        yield return new ScreenshotStep("far useful daylight material and junction readability", farUsefulIds, 140);

        string[] longStoneRunIds = walls
            .Where(wall => wall.Stuff?.defName == "BlocksGranite" &&
                           ((wall.Position.z == center.z + 7 &&
                             wall.Position.x >= center.x - 10 && wall.Position.x <= center.x - 4) ||
                            (wall.Position.x == center.x - 8 &&
                             wall.Position.z >= center.z && wall.Position.z <= center.z + 6)))
            .Select(wall => wall.ThingID)
            .ToArray();
        yield return new CameraActionStep("close seven-segment stone running-bond projections", longStoneRunIds, paddingPixels: 70);
        yield return new ScreenshotStep("close uninterrupted three-course horizontal and north-south stone bond", longStoneRunIds, 70);
        yield return new CameraActionStep("ordinary zoom seven-segment stone running-bond projections", longStoneRunIds, paddingPixels: 150);
        yield return new ScreenshotStep("ordinary zoom staggered stone courses remain readable without cell seams", longStoneRunIds, 150);
        yield return new CameraActionStep("far useful zoom seven-segment stone running-bond projections", longStoneRunIds, paddingPixels: 250);
        yield return new ScreenshotStep("far useful zoom stone wall height top plane and course rhythm remain distinct", longStoneRunIds, 250);

        foreach ((string material, string label, int horizontalMinX, int horizontalMaxX, int horizontalZ,
                     int verticalX, int verticalMinZ, int verticalMaxZ) in new[]
                 {
                     ("BlocksGranite", "stone dressed masonry", -10, -4, 7, -8, 0, 6),
                     ("WoodLog", "wood OSB plates", -3, 1, 7, -2, 2, 5),
                     ("Steel", "riveted metal plates", 3, 7, 7, 4, 2, 5),
                 })
        {
            string[] materialIds = walls
                .Where(wall => wall.Stuff?.defName == material &&
                               (wall.Position.z == center.z + horizontalZ &&
                                wall.Position.x >= center.x + horizontalMinX &&
                                wall.Position.x <= center.x + horizontalMaxX ||
                                wall.Position.x == center.x + verticalX &&
                                wall.Position.z >= center.z + verticalMinZ &&
                                wall.Position.z <= center.z + verticalMaxZ))
                .Select(wall => wall.ThingID)
                .ToArray();
            yield return new CameraActionStep("close " + label + " horizontal and north-south projection", materialIds, paddingPixels: 90);
            yield return new ScreenshotStep("close daylight " + label + " projection", materialIds, 90);
        }

        string[] junctionIds = walls
            .Where(wall => wall.Position.z >= center.z - 5 && wall.Position.z <= center.z)
            .Select(wall => wall.ThingID)
            .ToArray();
        yield return new CameraActionStep("normal gameplay zoom L T plus and regular-wall joins", junctionIds, paddingPixels: 120);
        yield return new ScreenshotStep("daylight L T and plus thin-wall junctions", junctionIds, 120);

        foreach ((string label, string material) in new[]
                 {
                     ("isolated wood L junction", "WoodLog"),
                     ("isolated steel T junction", "Steel"),
                     ("isolated granite plus junction", "BlocksGranite"),
                 })
        {
            string[] isolatedJunctionIds = presentationJunctionWalls
                .Where(wall => wall.Stuff?.defName == material)
                .Select(wall => wall.ThingID)
                .ToArray();
            yield return new CameraActionStep("presentation close view of " + label, isolatedJunctionIds, paddingPixels: 75);
            yield return new ScreenshotStep(label + " retains complete arms and native attached shadow", isolatedJunctionIds, 75);
        }

        foreach ((string material, string label, int z) in new[]
                 {
                     ("BlocksGranite", "granite masonry", 2),
                     ("WoodLog", "wood OSB", 5),
                     ("Steel", "riveted steel", 8),
                 })
        {
            string[] joinIds = walls
                .Where(wall => wall.Position.z == center.z + z &&
                               wall.Position.x >= center.x + 9 &&
                               wall.Stuff?.defName == material)
                .Select(wall => wall.ThingID)
                .Concat(regularWalls
                    .Where(wall => wall.Position.z == center.z + z)
                    .Select(wall => wall.ThingID))
                .ToArray();
            yield return new CameraActionStep("close " + label + " hybrid Core-wall merge", joinIds, paddingPixels: 90);
            yield return new ScreenshotStep(
                "unchanged Core wall with material-matched " + label + " extension to Thin Wall",
                joinIds,
                90);
        }

        string[] reverseJoinIds = walls
            .Where(wall => wall.Position.z == center.z + 10 && wall.Position.x <= center.x - 9)
            .Select(wall => wall.ThingID)
            .Concat(regularWalls
                .Where(wall => wall.Position.z == center.z + 10)
                .Select(wall => wall.ThingID))
            .ToArray();
        yield return new CameraActionStep("close reverse horizontal Core-wall continuation", reverseJoinIds, paddingPixels: 90);
        yield return new ScreenshotStep("east-ray Thin Wall merges into connected ordinary walls", reverseJoinIds, 90);

        foreach ((string label, int x, int minZ, int maxZ, int regularMinX, int regularMaxX, int regularZ) in new[]
                 {
                     ("wood north-ray", 15, -7, -5, 15, 17, -8),
                     ("steel south-ray", 12, -4, -2, 12, 14, -1),
                 })
        {
            string[] verticalJoinIds = walls
                .Where(wall => wall.Position.x == center.x + x &&
                               wall.Position.z >= center.z + minZ && wall.Position.z <= center.z + maxZ)
                .Select(wall => wall.ThingID)
                .Concat(regularWalls
                    .Where(wall => wall.Position.x >= center.x + regularMinX &&
                                   wall.Position.x <= center.x + regularMaxX &&
                                   wall.Position.z == center.z + regularZ)
                    .Select(wall => wall.ThingID))
                .ToArray();
            yield return new CameraActionStep("close " + label + " Core-wall continuation", verticalJoinIds, paddingPixels: 90);
            yield return new ScreenshotStep(
                label + " Thin Wall merges into one material-matched ordinary wall without another material touching it",
                verticalJoinIds,
                90);
        }

        string[] southSideHorizontalJoinIds = walls
            .Where(wall => wall.Position.z == center.z + 11 &&
                           wall.Position.x >= center.x - 6 && wall.Position.x <= center.x + 8)
            .Select(wall => wall.ThingID)
            .Concat(regularWalls
                .Where(wall => wall.Position.z == center.z + 11 &&
                               wall.Position.x >= center.x - 3 && wall.Position.x <= center.x + 5)
                .Select(wall => wall.ThingID))
            .ToArray();
        yield return new CameraActionStep("close west and east rays into south-side Core walls", southSideHorizontalJoinIds, paddingPixels: 110);
        yield return new ScreenshotStep("both horizontal rays use square in-raster shoulders at south-side ordinary wall surfaces", southSideHorizontalJoinIds, 110);

        string[] westSideVerticalJoinIds = walls
            .Where(wall => wall.Position.x == center.x - 15 &&
                           wall.Position.z >= center.z - 3 && wall.Position.z <= center.z + 9)
            .Select(wall => wall.ThingID)
            .Concat(regularWalls
                .Where(wall => wall.Position.x >= center.x - 17 && wall.Position.x <= center.x - 15 &&
                               (wall.Position.z == center.z || wall.Position.z == center.z + 6))
                .Select(wall => wall.ThingID))
            .ToArray();
        yield return new CameraActionStep("close north and south rays into west-side Core walls", westSideVerticalJoinIds, paddingPixels: 110);
        yield return new ScreenshotStep("both vertical rays use square in-raster shoulders at west-side ordinary wall surfaces", westSideVerticalJoinIds, 110);

        foreach ((string label, string[] ids) in new[]
                 {
                     (
                         "south-going vertex-centered side-T",
                         walls.Where(wall => wall.Position.x == center.x + 19 &&
                                             wall.Position.z >= center.z - 15 && wall.Position.z <= center.z - 13)
                             .Select(wall => wall.ThingID)
                             .Concat(regularWalls.Where(wall => wall.Position.z == center.z - 12 &&
                                                                wall.Position.x >= center.x + 17 && wall.Position.x <= center.x + 21)
                                 .Select(wall => wall.ThingID))
                             .ToArray()),
                     (
                         "north-going vertex-centered side-T",
                         walls.Where(wall => wall.Position.x == center.x + 19 &&
                                             wall.Position.z >= center.z - 3 && wall.Position.z <= center.z - 1)
                             .Select(wall => wall.ThingID)
                             .Concat(regularWalls.Where(wall => wall.Position.z == center.z - 4 &&
                                                                wall.Position.x >= center.x + 17 && wall.Position.x <= center.x + 21)
                                 .Select(wall => wall.ThingID))
                             .ToArray()),
                     (
                         "west-going vertex-centered side-T",
                         walls.Where(wall => wall.Position.z == center.z - 12 &&
                                             wall.Position.x >= center.x - 21 && wall.Position.x <= center.x - 19)
                             .Select(wall => wall.ThingID)
                             .Concat(regularWalls.Where(wall => wall.Position.x == center.x - 18 &&
                                                                wall.Position.z >= center.z - 14 && wall.Position.z <= center.z - 10)
                                 .Select(wall => wall.ThingID))
                             .ToArray()),
                     (
                         "east-going vertex-centered side-T",
                         walls.Where(wall => wall.Position.z == center.z - 18 &&
                                             wall.Position.x >= center.x - 17 && wall.Position.x <= center.x - 15)
                             .Select(wall => wall.ThingID)
                             .Concat(regularWalls.Where(wall => wall.Position.x == center.x - 18 &&
                                                                wall.Position.z >= center.z - 20 && wall.Position.z <= center.z - 16)
                                 .Select(wall => wall.ThingID))
                             .ToArray()),
                 })
        {
            yield return new CameraActionStep("close " + label, ids, paddingPixels: 65);
            yield return new ScreenshotStep(
                label + " aligns one Thin centerline with equal regular-wall halves and no displaced branch",
                ids,
                65);
        }

        string[] multiRayJoinIds = walls
            .Where(wall => wall.Position.z >= center.z + 14 && wall.Position.z <= center.z + 17 &&
                           wall.Position.x >= center.x - 3 && wall.Position.x <= center.x)
            .Select(wall => wall.ThingID)
            .Concat(regularWalls
                .Where(wall => wall.Position.z == center.z + 17 && wall.Position.x >= center.x && wall.Position.x <= center.x + 1)
                .Select(wall => wall.ThingID))
            .ToArray();
        yield return new CameraActionStep("close native mixed-ray regular-wall vertex composition", multiRayJoinIds, paddingPixels: 110);
        yield return new ScreenshotStep("native west and south rays meet one mixed-width regular-wall union without folded overlays", multiRayJoinIds, 110);

        foreach ((string material, string label) in new[]
                 {
                     ("BlocksGranite", "granite Thin Doors"),
                     ("WoodLog", "wood plate Thin Doors"),
                     ("Steel", "riveted steel Thin Doors"),
                 })
        {
            string[] doorIds = doors
                .Where(door => door.Stuff?.defName == material &&
                               (material == "BlocksGranite"
                                   ? door.Position.z >= center.z + 19
                                   : door.Position.z == center.z + 13))
                .Select(door => door.ThingID)
                .ToArray();
            yield return new CameraActionStep("close horizontal and north-south " + label, doorIds, paddingPixels: 100);
            yield return new ScreenshotStep(
                label + " retain edge perspective and damage inside both leaf silhouettes",
                doorIds,
                100);
        }

        string[] contextIds = walls
            .Where(wall => wall.Position.z <= center.z - 16)
            .Select(wall => wall.ThingID)
            .Concat(doors
                .Where(door => door.Position.z <= center.z - 16)
                .Select(door => door.ThingID))
            .Concat(contextRegularWalls.Select(wall => wall.ThingID))
            .Concat(contextFixtures.Select(thing => thing.ThingID))
            .ToArray();
        Building_ThinDoor contextDoor = doors.Single(door =>
            door.Position == center + new IntVec3(1, 0, -19));
        yield return new AssertionStep(
            "closed lived-in Thin Door divides the furnished room",
            _ =>
            {
                SharedEdge edge = contextDoor.OwnedEdge.Shared;
                EndToEndAssert.True(
                    walls.Any(wall =>
                        wall.OwnedEdge.Shared.PositiveSide == edge.PositiveSide &&
                        wall.OwnedEdge.Shared.AnchorCell.x == edge.AnchorCell.x &&
                        wall.OwnedEdge.Shared.AnchorCell.z == edge.AnchorCell.z - 1),
                    "The furnished-room door requires a collinear completed Thin Wall immediately south of its edge.");
                EndToEndAssert.True(
                    walls.Any(wall =>
                        wall.OwnedEdge.Shared.PositiveSide == edge.PositiveSide &&
                        wall.OwnedEdge.Shared.AnchorCell.x == edge.AnchorCell.x &&
                        wall.OwnedEdge.Shared.AnchorCell.z == edge.AnchorCell.z + 1),
                    "The furnished-room door requires a collinear completed Thin Wall immediately north of its edge.");
                EndToEndAssert.True(
                    HybridWallDoorGeometryCompiler.Compile(contextDoor.OpenFraction).IsFullyClosed,
                    "The closed furnished-room capture must use the continuous closed leaf union phase.");
                EndToEndAssert.True(
                    contextExteriorDoor.Position == center + new IntVec3(-1, 0, -23) &&
                    contextExteriorDoor.def == ThingDefOf.Door,
                    "The occupied room shell requires a real Core exterior Door into its common room.");
                IntVec3 outsideApproach = center + new IntVec3(-1, 0, -24);
                IntVec3 insideApproach = center + new IntVec3(-1, 0, -22);
                EndToEndAssert.True(
                    outsideApproach.Standable(map) && insideApproach.Standable(map),
                    "The exterior Door requires standable cardinal approach cells inside and outside the room.");
                EndToEndAssert.True(
                    contextExteriorDoor.PawnCanOpen(contextPawn) &&
                    map.reachability.CanReach(
                        outsideApproach,
                        insideApproach,
                        PathEndMode.OnCell,
                        TraverseParms.For(contextPawn)),
                    "The pictured Core Door must provide a native pawn-reachable route from outdoors into the room.");
                Building_Bed bed = contextFixtures.OfType<Building_Bed>().Single();
                CellRect bedRect = GenAdj.OccupiedRect(bed.Position, bed.Rotation, bed.def.Size);
                IntVec3 bedHead = BedUtility.GetSleepingSlotPos(0, bed.Position, bed.Rotation, bed.def.Size);
                IntVec3 bedBackingCell = bedHead + bed.Rotation.Opposite.FacingCell;
                IntVec3 bedFoot = BedUtility.GetFeetSlotPos(0, bed.Position, bed.Rotation, bed.def.Size);
                IntVec3 bedApproach = bedFoot + bed.Rotation.FacingCell;
                EndToEndAssert.True(
                    bedRect.Contains(bedHead) &&
                    bedBackingCell.GetEdifice(map)?.def == ThingDefOf.Wall &&
                    bedApproach.Standable(map) &&
                    bedApproach.GetEdifice(map) is null,
                    "The pictured bed's actual BedUtility head cell must back directly onto a wall while its actual foot approach stays clear.");
                Building table = contextFixtures.Single(thing => thing.def.defName == "Table2x2c") as Building
                                 ?? throw new InvalidOperationException("The furnished room has no table.");
                Building[] chairs = contextFixtures
                    .OfType<Building>()
                    .Where(building => building.def.defName == "DiningChair")
                    .ToArray();
                AssertDiningChairsServeTable(table, chairs);
                EndToEndAssert.True(
                    !outsideApproach.GetThingList(map).OfType<Building>().Any() &&
                    !insideApproach.GetThingList(map).OfType<Building>().Any(),
                    "The exterior Door's two approach cells must contain no furniture or other building obstruction.");
                Room west = (center + new IntVec3(-3, 0, -19)).GetRoom(map);
                Room east = (center + new IntVec3(3, 0, -19)).GetRoom(map);
                EndToEndAssert.True(!ReferenceEquals(west, east),
                    "The complete Thin Wall partition and closed Thin Door must create two Core rooms.");
            });
        yield return new CameraActionStep("ordinary gameplay zoom furnished Thin Wall room", contextIds, paddingPixels: 105);
        yield return new ScreenshotStep("furnished colony room is visibly divided by a closed Thin Wall and Thin Door", contextIds, 105);

        string contextGoHere = GoHereOption(context, contextPawn, contextCrossingMarker);
        yield return new SelectionActionStep("select colonist for furnished-room Thin Door crossing", new[] { contextPawn.ThingID }, false);
        yield return new FloatMenuActionStep(
            "order colonist across the furnished-room Thin Door",
            contextPawn.ThingID,
            contextCrossingMarker.ThingID,
            contextGoHere);
        yield return new TimeControlActionStep("run furnished-room Thin Door opening", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "native room crossing fully opens the Thin Door and reaches the opposite room",
            _ => contextDoor.OpenFraction >= 0.95f &&
                 contextPawn.Position == contextCrossingMarker.Position &&
                 contextPawn.Position.DistanceTo(contextDoor.Position) >= 2f,
            new EndToEndDeadline(900, 1_200, TimeSpan.FromSeconds(35)));
        yield return new TimeControlActionStep("pause with furnished-room Thin Door fully open", true, EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep("clear furnished-room pawn selection", Array.Empty<string>(), false);
        yield return new CameraActionStep("close view of open furnished-room Thin Door passage", contextIds, paddingPixels: 75);
        yield return new ScreenshotStep("retracted Thin Door leaves expose a traversable passage between furnished rooms", contextIds, 75);

        string[] mixedDoorIds = doors
            .Where(door => door.Position == center + new IntVec3(-10, 0, 13))
            .Select(door => door.ThingID)
            .Concat(walls
                .Where(wall => wall.Position.z == center.z + 13 && wall.Position.x >= center.x - 9 && wall.Position.x <= center.x - 7)
                .Select(wall => wall.ThingID))
            .Concat(regularWalls
                .Where(wall => wall.Position.z == center.z + 13 && wall.Position.x >= center.x - 13 && wall.Position.x <= center.x - 11)
                .Select(wall => wall.ThingID))
            .ToArray();
        yield return new AssertionStep(
            "closed mixed-run Thin Door remains visually unobscured",
            _ =>
            {
                Building_ThinDoor target = doors.Single(door => door.Position == center + new IntVec3(-10, 0, 13));
                EndToEndAssert.True(!target.Open && target.OpenFraction <= 0.05f,
                    "The closed-state screenshot requires fully closed leaves, not an initial or residual mover fraction.");
                EndToEndAssert.True(doorPawn.Position.DistanceTo(target.Position) >= 2f,
                    "The evidence pawn must not cover the closed Thin Door leaves.");
            });
        yield return new CameraActionStep("close Core wall Thin Door Thin Wall mixed run", mixedDoorIds, paddingPixels: 110);
        yield return new ScreenshotStep("closed Thin Door connects ordinary and Thin Wall structures", mixedDoorIds, 110);

        string goHere = GoHereOption(context, doorPawn, doorCrossingMarker);
        yield return new SelectionActionStep("select pawn for mixed-run Thin Door opening", new[] { doorPawn.ThingID }, false);
        yield return new FloatMenuActionStep(
            "order pawn through Core-wall-connected Thin Door",
            doorPawn.ThingID,
            doorCrossingMarker.ThingID,
            goHere);
        yield return new TimeControlActionStep("run mixed-run Thin Door opening", false, EndToEndGameSpeed.Normal);
        Building_ThinDoor mixedDoor = doors.Single(door => door.Position == center + new IntVec3(-10, 0, 13));
        yield return new WaitUntilStep(
            "native crossing fully opens Core-wall-connected Thin Door and reaches the far side",
            _ => mixedDoor.OpenFraction >= 0.95f &&
                 doorPawn.Position == doorCrossingMarker.Position &&
                 doorPawn.Position.DistanceTo(mixedDoor.Position) >= 2f,
            new EndToEndDeadline(900, 1_200, TimeSpan.FromSeconds(35)));
        yield return new TimeControlActionStep("pause while mixed-run Thin Door is visibly open", true, EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep("clear pawn selection from open mixed run", Array.Empty<string>(), false);
        yield return new CameraActionStep("close open Core wall Thin Door Thin Wall mixed run", mixedDoorIds, paddingPixels: 110);
        yield return new ScreenshotStep("open Thin Door leaves reveal passage inside a continuous mixed wall run", mixedDoorIds, 110);

        string[] damageIds = walls
            .Where(wall => wall.Position.z == center.z - 10 || wall.Position.z == center.z - 7)
            .Select(wall => wall.ThingID)
            .ToArray();
        yield return new CameraActionStep("close horizontal and north-south damage material catalog", damageIds, paddingPixels: 100);
        yield return new ScreenshotStep("localized damage remains clipped to both stone wood and metal projections", damageIds, 100);

        foreach ((string material, string label) in new[]
                 {
                     ("BlocksGranite", "stone moderate heavy and severe damage"),
                     ("WoodLog", "wood moderate heavy and severe damage"),
                     ("Steel", "steel moderate heavy and severe damage"),
                 })
        {
            string[] materialDamageIds = walls
                .Where(wall => wall.Position.z == center.z - 10 && wall.Stuff?.defName == material)
                .OrderBy(wall => wall.Position.x)
                .Select(wall => wall.ThingID)
                .ToArray();
            yield return new CameraActionStep("presentation close view of " + label, materialDamageIds, paddingPixels: 45);
            yield return new ScreenshotStep(label + " remain distinct at Workshop detail scale", materialDamageIds, 45);
        }

        string[] uniqueEdgeIds = walls
            .Where(wall => wall.Position.z == center.z - 13)
            .Select(wall => wall.ThingID)
            .ToArray();
        yield return new CameraActionStep("close unique shared-edge steel damage run", uniqueEdgeIds, paddingPixels: 120);
        yield return new ScreenshotStep("unique shared-edge steel run has no duplicate owner overdraw", uniqueEdgeIds, 120);

        string exitRoom = GoHereOption(context, contextPawn, presentationExitMarker);
        yield return new SelectionActionStep("select furnished-room colonist for clean presentation exit", new[] { contextPawn.ThingID }, false);
        yield return new FloatMenuActionStep(
            "order furnished-room colonist outdoors through both real doors",
            contextPawn.ThingID,
            presentationExitMarker.ThingID,
            exitRoom);
        yield return new TimeControlActionStep("run furnished-room presentation exit", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "colonist reaches the exterior presentation marker",
            _ => contextPawn.Position == presentationExitMarker.Position,
            new EndToEndDeadline(1_200, 1_600, TimeSpan.FromSeconds(40)));
        int bothDoorsClosedAt = -1;
        yield return new WaitUntilStep(
            "both furnished-room doors remain closed through their mover animations after the native exit",
            _ =>
            {
                if (contextDoor.Open || contextDoor.OpenFraction > 0.05f || contextExteriorDoor.Open)
                {
                    bothDoorsClosedAt = -1;
                    return false;
                }

                if (bothDoorsClosedAt < 0)
                {
                    bothDoorsClosedAt = Find.TickManager.TicksGame;
                }

                return Find.TickManager.TicksGame - bothDoorsClosedAt >= 90;
            },
            new EndToEndDeadline(1_500, 2_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep("pause the clean furnished-room presentation", true, EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep("clear the furnished-room presentation selection", Array.Empty<string>(), false);
        string[] presentationRoomIds = walls
            .Where(wall => wall.Position.z <= center.z - 16)
            .Select(wall => wall.ThingID)
            .Concat(doors
                .Where(door => door.Position.z <= center.z - 16)
                .Select(door => door.ThingID))
            .Concat(contextRegularWalls.Select(wall => wall.ThingID))
            .Concat(contextFixtures.OfType<Building>().Select(building => building.ThingID))
            .ToArray();
        yield return new CameraActionStep("clean ordinary-zoom furnished Thin Walls presentation", presentationRoomIds, paddingPixels: 260);
        yield return new ScreenshotStep(
            "furnished divided room remains coherent after the pawn exits through both doors",
            presentationRoomIds,
            260);
        yield return new ScreenshotModeActionStep(
            "restore the visual catalog's prior screenshot-mode state",
            originalScreenshotMode);
    }

    private GizmoActionStep Drag(
        string name,
        int startX,
        int startZ,
        int endX,
        int endZ,
        EndToEndCardinalRotation rotation,
        string stuffDefName)
    {
        IntVec3 start = center + new IntVec3(startX, 0, startZ);
        IntVec3 end = center + new IntVec3(endX, 0, endZ);
        return new GizmoActionStep(
            name,
            Array.Empty<string>(),
            build.RuntimeType,
            EndToEndGizmoInteraction.Drag,
            rotation,
            new EndToEndBuildMaterial(stuffDefName),
            stableGizmoId: build.StableId,
            startCell: new EndToEndMapCell(start.x, start.z),
            endCell: new EndToEndMapCell(end.x, end.z),
            architectCategoryDefNames: new[] { "Structure" });
    }

    private GizmoActionStep Door(
        string name,
        int x,
        int z,
        EndToEndCardinalRotation rotation,
        string stuffDefName)
    {
        IntVec3 cell = center + new IntVec3(x, 0, z);
        return new GizmoActionStep(
            name,
            Array.Empty<string>(),
            doorBuild.RuntimeType,
            EndToEndGizmoInteraction.Drag,
            rotation,
            new EndToEndBuildMaterial(stuffDefName),
            stableGizmoId: doorBuild.StableId,
            startCell: new EndToEndMapCell(cell.x, cell.z),
            endCell: new EndToEndMapCell(cell.x, cell.z),
            architectCategoryDefNames: new[] { "Structure" });
    }

    private Building_ThinWall RequireWall(IntVec3 cell, ThinWallSide side)
    {
        return cell.GetThingList(map)
            .OfType<Building_ThinWall>()
            .Single(wall => wall.OwnedSide == side);
    }

    private Building_ThinDoor RequireDoor(IntVec3 cell, ThinWallSide side)
    {
        return cell.GetThingList(map)
            .OfType<Building_ThinDoor>()
            .Single(door => door.OwnedSide == side);
    }

    private void SpawnRegularRun(int minX, int maxX, int z, string stuffDefName)
    {
        for (int x = minX; x <= maxX; x++)
        {
            var regularWall = (Building)ThingMaker.MakeThing(
                ThingDefOf.Wall,
                DefDatabase<ThingDef>.GetNamed(stuffDefName));
            regularWall.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(regularWall, center + new IntVec3(x, 0, z), map);
            regularWalls.Add(regularWall);
        }
    }

    private void SpawnRegularVerticalRun(int x, int minZ, int maxZ, string stuffDefName)
    {
        for (int z = minZ; z <= maxZ; z++)
        {
            var regularWall = (Building)ThingMaker.MakeThing(
                ThingDefOf.Wall,
                DefDatabase<ThingDef>.GetNamed(stuffDefName));
            regularWall.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(regularWall, center + new IntVec3(x, 0, z), map);
            regularWalls.Add(regularWall);
        }
    }

    private void BuildContextRoom()
    {
        TerrainDef floor = DefDatabase<TerrainDef>.GetNamed("WoodPlankFloor");
        for (int x = -5; x <= 4; x++)
        {
            for (int z = -22; z <= -16; z++)
            {
                IntVec3 cell = center + new IntVec3(x, 0, z);
                if (!contextTerrains.ContainsKey(cell))
                {
                    contextTerrains[cell] = cell.GetTerrain(map);
                }
                map.terrainGrid.SetTerrain(cell, floor);
            }
        }

        for (int x = -6; x <= 5; x++)
        {
            if (x == -1)
            {
                SpawnContextDoor(center + new IntVec3(x, 0, -23));
            }
            else
            {
                SpawnContextRegularWall(center + new IntVec3(x, 0, -23));
            }
            SpawnContextRegularWall(center + new IntVec3(x, 0, -15));
        }
        for (int z = -22; z <= -16; z++)
        {
            SpawnContextRegularWall(center + new IntVec3(-6, 0, z));
            SpawnContextRegularWall(center + new IntVec3(5, 0, z));
        }

        SpawnContextFurniture("Table2x2c", center + new IntVec3(-4, 0, -18), Rot4.North, "WoodLog");
        SpawnContextFurniture("DiningChair", center + new IntVec3(-4, 0, -19), Rot4.North, "WoodLog");
        SpawnContextFurniture("DiningChair", center + new IntVec3(-4, 0, -16), Rot4.South, "WoodLog");
        SpawnContextFurniture("DiningChair", center + new IntVec3(-5, 0, -18), Rot4.East, "WoodLog");
        SpawnContextFurniture("DiningChair", center + new IntVec3(-2, 0, -18), Rot4.West, "WoodLog");
        // GenAdj shifts even-length South-facing footprints one cell south. Root at -16 keeps
        // the actual BedUtility head cell at -16, directly backed by the north wall at -15.
        SpawnContextFurniture("Bed", center + new IntVec3(3, 0, -16), Rot4.South, "WoodLog");
    }

    private void SpawnContextRegularWall(IntVec3 cell)
    {
        var wall = (Building)ThingMaker.MakeThing(
            ThingDefOf.Wall,
            DefDatabase<ThingDef>.GetNamed("BlocksGranite"));
        wall.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(wall, cell, map);
        regularWalls.Add(wall);
        contextRegularWalls.Add(wall);
    }

    private void SpawnContextDoor(IntVec3 cell)
    {
        contextExteriorDoor = (Building_Door)ThingMaker.MakeThing(ThingDefOf.Door, ThingDefOf.WoodLog);
        contextExteriorDoor.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(contextExteriorDoor, cell, map, Rot4.North);
        fixtures.Add(contextExteriorDoor);
        contextFixtures.Add(contextExteriorDoor);
    }

    private void SpawnContextFurniture(string defName, IntVec3 root, Rot4 rotation, string stuffDefName)
    {
        ThingDef def = DefDatabase<ThingDef>.GetNamed(defName);
        ThingDef? stuff = def.MadeFromStuff
            ? DefDatabase<ThingDef>.GetNamed(stuffDefName)
            : null;
        CellRect occupied = GenAdj.OccupiedRect(root, rotation, def.Size);
        EndToEndAssert.True(
            occupied.Cells.All(cell =>
                cell.InBounds(map) &&
                cell.x > center.x - 6 && cell.x < center.x + 5 &&
                cell.z > center.z - 23 && cell.z < center.z - 15 &&
                contextOccupied.Add(cell)),
            defName + " must occupy unique interior cells inside the furnished room shell.");

        var building = (Building)ThingMaker.MakeThing(def, stuff);
        building.SetFactionDirect(Faction.OfPlayer);
        if (building is Building_Bed bed)
        {
            bed.Medical = true;
        }
        GenSpawn.Spawn(building, root, map, rotation);
        fixtures.Add(building);
        contextFixtures.Add(building);
    }

    private static void AssertDiningChairsServeTable(Building table, IReadOnlyCollection<Building> chairs)
    {
        CellRect tableRect = table.OccupiedRect();
        EndToEndAssert.Equal(4, chairs.Count,
            "The 2x2 publication table requires exactly four intentionally attached dining chairs.");
        foreach (Building chair in chairs)
        {
            IntVec3 cell = chair.Position;
            Rot4 expectedFacing;
            bool adjacent;
            if (cell.x == tableRect.minX - 1 && cell.z >= tableRect.minZ && cell.z <= tableRect.maxZ)
            {
                expectedFacing = Rot4.East;
                adjacent = true;
            }
            else if (cell.x == tableRect.maxX + 1 && cell.z >= tableRect.minZ && cell.z <= tableRect.maxZ)
            {
                expectedFacing = Rot4.West;
                adjacent = true;
            }
            else if (cell.z == tableRect.minZ - 1 && cell.x >= tableRect.minX && cell.x <= tableRect.maxX)
            {
                expectedFacing = Rot4.North;
                adjacent = true;
            }
            else if (cell.z == tableRect.maxZ + 1 && cell.x >= tableRect.minX && cell.x <= tableRect.maxX)
            {
                expectedFacing = Rot4.South;
                adjacent = true;
            }
            else
            {
                expectedFacing = Rot4.Invalid;
                adjacent = false;
            }

            EndToEndAssert.True(adjacent && chair.Rotation == expectedFacing,
                $"Dining chair {chair.ThingID} at {cell} must occupy a cardinal cell immediately adjacent to the table's exact {tableRect} footprint and face it; no empty-cell gap or floating chair is allowed.");
        }
    }

    private void DisplaceIncidentalThings(CellRect area)
    {
        Thing[] things = area.Cells
            .SelectMany(cell => cell.GetThingList(map))
            .Distinct()
            .ToArray();
        foreach (Thing thing in things)
        {
            displacedThings.Add((thing, thing.Position, thing.Rotation));
            thing.DeSpawn();
        }
    }

    private static string GoHereOption(IEndToEndContext context, Pawn pawn, Thing marker)
    {
        return context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(pawn.ThingID, marker.ThingID)
            .Single(option => !option.Disabled &&
                              option.Label.IndexOf("go here", StringComparison.OrdinalIgnoreCase) >= 0)
            .StableId;
    }

    private Thing SpawnInvisibleRouteTarget(IntVec3 cell)
    {
        Thing target = ThingMaker.MakeThing(InvisibleRouteTargetDef);
        EndToEndAssert.True(target.thingIDNumber >= 0,
            "Navigation targets must receive a native unique Thing ID before they are spawned.");
        EndToEndAssert.Equal(DrawerType.None, target.def.drawerType,
            "Navigation targets used by visual evidence must not render or emit quantity labels.");
        GenSpawn.Spawn(target, cell, map);
        fixtures.Add(target);
        return target;
    }

    private static bool EdgeTouchesCorner(SharedEdge edge, IntVec3 corner)
    {
        if (edge.PositiveSide == ThinWallSide.North)
        {
            int z = edge.AnchorCell.z + 1;
            return corner.z == z &&
                   (corner.x == edge.AnchorCell.x || corner.x == edge.AnchorCell.x + 1);
        }

        int x = edge.AnchorCell.x + 1;
        return corner.x == x &&
               (corner.z == edge.AnchorCell.z || corner.z == edge.AnchorCell.z + 1);
    }

    private int CaptureWalls()
    {
        walls.Clear();
        walls.AddRange(map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName))
            .OfType<Building_ThinWall>()
            .Where(wall => wall.Position.InHorDistOf(center, FixtureRadius)));
        return walls.Count;
    }

    private int CaptureDoors()
    {
        doors.Clear();
        doors.AddRange(map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinDoorDefName))
            .OfType<Building_ThinDoor>()
            .Where(door => door.Position.InHorDistOf(center, FixtureRadius)));
        return doors.Count;
    }

    private static Dictionary<string, string> DescribeCoreWallMaterial(Building source)
    {
        Graphic graphic = ThingDefOf.Wall.graphicData.Graphic;
        graphic = graphic.GetColoredVersion(graphic.Shader, source.DrawColor, source.DrawColorTwo);
        while (graphic is Graphic_Linked linked)
        {
            graphic = linked.SubGraphic;
        }
        if (graphic is Graphic_Appearances appearances)
        {
            graphic = appearances.SubGraphicFor(source.Stuff);
        }

        Material material = graphic.MatSingleFor(source);
        Texture texture = material.mainTexture;
        return new Dictionary<string, string>
        {
            ["name"] = material.name,
            ["texture"] = $"{texture.name}|{texture.width}x{texture.height}",
            ["scale"] = material.mainTextureScale.ToString(),
            ["offset"] = material.mainTextureOffset.ToString(),
        };
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

    private void SpawnPresentationJunctions()
    {
        ThingDef wood = DefDatabase<ThingDef>.GetNamed("WoodLog");
        ThingDef steel = DefDatabase<ThingDef>.GetNamed("Steel");
        ThingDef granite = DefDatabase<ThingDef>.GetNamed("BlocksGranite");

        for (int x = -14; x <= -10; x++)
        {
            SpawnPresentationWall(presentationCenter + new IntVec3(x, 0, 0), ThinWallSide.South, wood);
        }
        for (int z = 0; z <= 4; z++)
        {
            SpawnPresentationWall(presentationCenter + new IntVec3(-14, 0, z), ThinWallSide.West, wood);
        }

        for (int x = -2; x <= 2; x++)
        {
            SpawnPresentationWall(presentationCenter + new IntVec3(x, 0, 0), ThinWallSide.South, steel);
        }
        for (int z = 0; z <= 4; z++)
        {
            SpawnPresentationWall(presentationCenter + new IntVec3(0, 0, z), ThinWallSide.West, steel);
        }

        for (int x = 12; x <= 16; x++)
        {
            SpawnPresentationWall(presentationCenter + new IntVec3(x, 0, 0), ThinWallSide.South, granite);
        }
        for (int z = -2; z <= 2; z++)
        {
            SpawnPresentationWall(presentationCenter + new IntVec3(14, 0, z), ThinWallSide.West, granite);
        }
    }

    private void SpawnPresentationWall(IntVec3 cell, ThinWallSide side, ThingDef stuff)
    {
        ThingDef wallDef = DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName);
        var wall = (Building_ThinWall)ThingMaker.MakeThing(wallDef, stuff);
        wall.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(wall, cell, map, new Rot4((int)side));
        fixtures.Add(wall);
        presentationJunctionWalls.Add(wall);
    }

    private static IntVec3 FindClearCenterAwayFrom(Map target, IntVec3 avoid, int minimumDistance, int radius)
    {
        for (int x = -100; x <= 100; x += 20)
        {
            for (int z = -100; z <= 100; z += 20)
            {
                IntVec3 candidate = target.Center + new IntVec3(x, 0, z);
                if (candidate.DistanceTo(avoid) >= minimumDistance &&
                    CellRect.CenteredOn(candidate, radius).InBounds(target))
                {
                    return candidate;
                }
            }
        }

        throw new EndToEndAssertionException("Could not find an isolated Thin Walls presentation-junction area.");
    }

    private static IntVec3 FindClearCenter(Map map, int radius)
    {
        IntVec3? best = null;
        int bestSupportedCells = -1;
        for (int x = -60; x <= 60; x += 20)
        {
            for (int z = -60; z <= 60; z += 20)
            {
                IntVec3 candidate = map.Center + new IntVec3(x, 0, z);
                CellRect area = CellRect.CenteredOn(candidate, radius);
                if (!area.InBounds(map))
                {
                    continue;
                }

                int supportedCells = area.Cells.Count(cell => HasFixtureAffordances(cell.GetTerrain(map)));
                if (supportedCells > bestSupportedCells)
                {
                    best = candidate;
                    bestSupportedCells = supportedCells;
                }
            }
        }

        return best ?? throw new EndToEndAssertionException("Could not find an in-bounds Thin Walls visual-identity area.");
    }

    private void NormalizeBuildTerrain(CellRect area)
    {
        TerrainDef soil = TerrainDefOf.Soil;
        EndToEndAssert.True(HasFixtureAffordances(soil),
            "Core soil must support every Thin Wall fixture material.");
        foreach (IntVec3 cell in area.Cells.Where(cell => !HasFixtureAffordances(cell.GetTerrain(map))))
        {
            if (!contextTerrains.ContainsKey(cell))
            {
                contextTerrains[cell] = cell.GetTerrain(map);
            }
            map.terrainGrid.SetTerrain(cell, soil);
        }
    }

    private static bool HasFixtureAffordances(TerrainDef terrain) =>
        terrain.affordances.Contains(TerrainAffordanceDefOf.Light) &&
        terrain.affordances.Contains(TerrainAffordanceDefOf.Medium) &&
        terrain.affordances.Contains(TerrainAffordanceDefOf.Heavy);
}
