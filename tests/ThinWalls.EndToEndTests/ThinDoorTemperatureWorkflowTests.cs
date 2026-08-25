using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest(
    "thin-walls.thin-door-temperature-exchange",
    "fumblesneeze.thinwalls",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.thinwalls",
    MaxFrames = 4_800,
    MaxGameTicks = 12_000,
    MaxWallClockSeconds = 180)]
public sealed class ThinDoorTemperatureWorkflowTest : IRimWorldEndToEndTest
{
    private readonly List<Thing> fixtures = new();
    private readonly HashSet<IntVec3> roofCells = new();
    private Map map = null!;
    private IntVec3 doorPairCenter;
    private IntVec3 wallPairCenter;
    private Building_ThinDoor door = null!;
    private Room doorColdRoom = null!;
    private Room doorHotRoom = null!;
    private Room wallColdRoom = null!;
    private Room wallHotRoom = null!;
    private float doorGapBefore;
    private float wallGapBefore;
    private bool originalTemperatureOverlay;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        doorPairCenter = FindClearCenter(map, 18);
        wallPairCenter = doorPairCenter + new IntVec3(0, 0, 11);
        originalTemperatureOverlay = Find.PlaySettings.showTemperatureOverlay;
        Find.PlaySettings.showTemperatureOverlay = true;

        BuildTwoRoomFixture(doorPairCenter, useDoor: true);
        BuildTwoRoomFixture(wallPairCenter, useDoor: false);
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();

        doorColdRoom = SampleLeftRoom(doorPairCenter);
        doorHotRoom = SampleRightRoom(doorPairCenter);
        wallColdRoom = SampleLeftRoom(wallPairCenter);
        wallHotRoom = SampleRightRoom(wallPairCenter);
        SetTemperaturePair(doorColdRoom, doorHotRoom);
        SetTemperaturePair(wallColdRoom, wallHotRoom);
        doorGapBefore = TemperatureGap(doorColdRoom, doorHotRoom);
        wallGapBefore = TemperatureGap(wallColdRoom, wallHotRoom);

        context.DeferCleanup(() =>
        {
            Find.PlaySettings.showTemperatureOverlay = originalTemperatureOverlay;
            foreach (Thing thing in fixtures.ToArray())
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }

            foreach (IntVec3 cell in roofCells)
            {
                map.roofGrid.SetRoof(cell, null);
            }
            map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        string[] ids = fixtures.Where(thing => !thing.Destroyed).Select(thing => thing.ThingID).ToArray();
        yield return new AssertionStep(
            "two roofed room pairs begin with equivalent temperature splits",
            _ =>
            {
                EndToEndAssert.False(ReferenceEquals(doorColdRoom, doorHotRoom),
                    "The Thin Door must remain a room divider while closed; " +
                    $"door={door.Position}/{door.OwnedSide}, " +
                    $"edgeBlocked={Rooms.ThinEdgeRegionUtility.BlocksCardinalBoundary(map, door.Position, door.OwnedEdge.OppositeCell)}, " +
                    $"leftRoom={doorColdRoom?.ID.ToString() ?? "null"}/{doorColdRoom?.CellCount.ToString() ?? "null"}, " +
                    $"rightRoom={doorHotRoom?.ID.ToString() ?? "null"}/{doorHotRoom?.CellCount.ToString() ?? "null"}.");
                EndToEndAssert.False(ReferenceEquals(wallColdRoom, wallHotRoom),
                    "The Thin Wall must remain a room divider.");
                EndToEndAssert.False(doorColdRoom!.UsesOutdoorTemperature || doorHotRoom!.UsesOutdoorTemperature,
                    "The door fixture rooms must be fully roofed indoor rooms.");
                EndToEndAssert.False(wallColdRoom.UsesOutdoorTemperature || wallHotRoom.UsesOutdoorTemperature,
                    "The wall fixture rooms must be fully roofed indoor rooms.");
                EndToEndAssert.False(door.def.holdsRoof,
                    "Temperature participation must not turn the Thin Door into roof support.");
                EndToEndAssert.True(Math.Abs(doorGapBefore - wallGapBefore) < 0.01f,
                    "Both fixture pairs must begin with the same temperature difference.");
            });
        yield return new SelectionActionStep(
            "clear selection for temperature-overlay evidence",
            Array.Empty<string>(),
            additive: false);
        yield return new CameraActionStep(
            "frame door-exchange and solid-wall temperature controls",
            ids,
            paddingPixels: 100);
        yield return new ScreenshotStep(
            "before running time both roofed room pairs show the same hot cold split",
            ids,
            paddingPixels: 100);
        yield return new TimeControlActionStep(
            "run native room temperature simulation",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "Thin Door rooms measurably exchange heat across only the owned edge",
            _ => TemperatureGap(doorColdRoom, doorHotRoom) <= doorGapBefore - 10f,
            new EndToEndDeadline(3_600, 10_000, TimeSpan.FromSeconds(100)));
        yield return new TimeControlActionStep(
            "pause after Thin Door temperature exchange",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "door pair converges while solid Thin Wall pair remains isolated",
            _ =>
            {
                float doorGapAfter = TemperatureGap(doorColdRoom, doorHotRoom);
                float wallGapAfter = TemperatureGap(wallColdRoom, wallHotRoom);
                EndToEndAssert.True(doorGapAfter < doorGapBefore - 10f,
                    "The rooms across the closed Thin Door must measurably converge.");
                EndToEndAssert.True(wallGapAfter > doorGapAfter + 1f,
                    "The otherwise-identical Thin Wall control must remain more isolated than the door pair.");
                EndToEndAssert.False(ReferenceEquals(doorColdRoom, doorHotRoom),
                    "Temperature exchange must not merge room identities.");
            });
        yield return new ScreenshotStep(
            "after simulation only the Thin Door room pair visibly converges",
            ids,
            paddingPixels: 100);
        yield return new CheckpointStep("record Thin Door room temperature outcome", _ =>
            new Dictionary<string, string>
            {
                ["doorGapBefore"] = doorGapBefore.ToString("F2"),
                ["doorGapAfter"] = TemperatureGap(doorColdRoom, doorHotRoom).ToString("F2"),
                ["wallGapBefore"] = wallGapBefore.ToString("F2"),
                ["wallGapAfter"] = TemperatureGap(wallColdRoom, wallHotRoom).ToString("F2"),
                ["doorRoomsRemainDistinct"] = (!ReferenceEquals(doorColdRoom, doorHotRoom)).ToString(),
                ["thinDoorHoldsRoof"] = door.def.holdsRoof.ToString(),
            });
    }

    private void BuildTwoRoomFixture(IntVec3 center, bool useDoor)
    {
        for (int x = -2; x <= 3; x++)
        {
            SpawnWall(center + new IntVec3(x, 0, -2), ThinWallSide.South);
            SpawnWall(center + new IntVec3(x, 0, 2), ThinWallSide.North);
        }

        for (int z = -2; z <= 2; z++)
        {
            SpawnWall(center + new IntVec3(-2, 0, z), ThinWallSide.West);
            SpawnWall(center + new IntVec3(3, 0, z), ThinWallSide.East);
            if (useDoor && z == 0)
            {
                door = SpawnDoor(center + new IntVec3(0, 0, z), ThinWallSide.East);
            }
            else
            {
                SpawnWall(center + new IntVec3(0, 0, z), ThinWallSide.East);
            }
        }

        SpawnCoreSupport(center + new IntVec3(-1, 0, 0));
        SpawnCoreSupport(center + new IntVec3(2, 0, 0));
        for (int x = -2; x <= 3; x++)
        {
            for (int z = -2; z <= 2; z++)
            {
                IntVec3 cell = center + new IntVec3(x, 0, z);
                map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
                roofCells.Add(cell);
            }
        }
    }

    private void SpawnWall(IntVec3 cell, ThinWallSide side)
    {
        var wall = (Building_ThinWall)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName),
            DefDatabase<ThingDef>.GetNamed("BlocksGranite"));
        wall.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(wall, cell, map, new Rot4((int)side));
        fixtures.Add(wall);
    }

    private Building_ThinDoor SpawnDoor(IntVec3 cell, ThinWallSide side)
    {
        var result = (Building_ThinDoor)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinDoorDefName),
            DefDatabase<ThingDef>.GetNamed("BlocksGranite"));
        result.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(result, cell, map, new Rot4((int)side));
        fixtures.Add(result);
        return result;
    }

    private void SpawnCoreSupport(IntVec3 cell)
    {
        Building support = (Building)ThingMaker.MakeThing(
            ThingDefOf.Wall,
            DefDatabase<ThingDef>.GetNamed("BlocksGranite"));
        support.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(support, cell, map);
        fixtures.Add(support);
    }

    private Room SampleLeftRoom(IntVec3 center) => (center + new IntVec3(-2, 0, 0)).GetRoom(map);

    private Room SampleRightRoom(IntVec3 center) => (center + new IntVec3(3, 0, 0)).GetRoom(map);

    private static void SetTemperaturePair(Room cold, Room hot)
    {
        cold.Temperature = 0f;
        hot.Temperature = 40f;
    }

    private static float TemperatureGap(Room first, Room second) =>
        Math.Abs(first.Temperature - second.Temperature);

    private static IntVec3 FindClearCenter(Map map, int radius)
    {
        for (int x = -60; x <= 60; x += 24)
        {
            for (int z = -60; z <= 45; z += 24)
            {
                IntVec3 candidate = map.Center + new IntVec3(x, 0, z);
                CellRect area = CellRect.CenteredOn(candidate + new IntVec3(0, 0, 5), radius);
                if (area.InBounds(map) && area.Cells.All(cell =>
                        cell.Standable(map) &&
                        !cell.Fogged(map) &&
                        cell.GetRoof(map) == null &&
                        cell.GetThingList(map).Count == 0))
                {
                    return candidate;
                }
            }
        }

        throw new EndToEndAssertionException("Could not find a clear Thin Door temperature fixture area.");
    }
}
