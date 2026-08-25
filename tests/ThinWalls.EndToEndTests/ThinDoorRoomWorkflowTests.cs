using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using Verse;
using Verse.AI;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest(
    "thin-walls.native-thin-door-room-workflow",
    "fumblesneeze.thinwalls",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.thinwalls",
    MaxFrames = 7_200,
    MaxGameTicks = 9_000,
    MaxWallClockSeconds = 210)]
public sealed class ThinDoorRoomWorkflowTest : IRimWorldEndToEndTest
{
    private readonly List<Thing> fixtures = new();
    private readonly List<Building_ThinWall> walls = new();
    private Map map = null!;
    private IntVec3 center;
    private Pawn pawn = null!;
    private Thing ownerMarker = null!;
    private Thing outsideMarker = null!;
    private Building_ThinDoor door = null!;
    private EndToEndGizmoOption wallBuild = null!;
    private EndToEndGizmoOption doorBuild = null!;
    private bool originalGodMode;
    private bool observedDoorOpening;
    private int crossOrderTick;
    private int holdOpenObservationTick;
    private IntVec3 sampledPosition;
    private readonly List<string> crossingTrace = new();

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        center = FindClearCenter(map, 12);
        originalGodMode = DebugSettings.godMode;
        DebugSettings.godMode = true;

        IEndToEndGizmoCatalog catalog = context.GetRequiredService<IEndToEndGizmoCatalog>();
        wallBuild = SingleBuild(catalog, ThinWallUtility.ThinWallDefName);
        doorBuild = SingleBuild(catalog, ThinWallUtility.ThinDoorDefName);

        pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        pawn.Name = new NameTriple("Edge", "Door", "Tester");
        GenSpawn.Spawn(pawn, center + new IntVec3(2, 0, -1), map);
        pawn.drafter.Drafted = true;
        ownerMarker = SpawnMarker(center + new IntVec3(2, 0, 0));
        outsideMarker = SpawnMarker(center + new IntVec3(4, 0, 0));
        fixtures.Add(pawn);
        fixtures.Add(ownerMarker);
        fixtures.Add(outsideMarker);

        context.DeferCleanup(() =>
        {
            DebugSettings.godMode = originalGodMode;
            foreach (Thing thing in fixtures.Concat<Thing>(walls).Append(door).Where(thing => thing != null).Distinct().ToArray())
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
        yield return EdgeDrag("build enclosed south thin-wall boundary", -2, -2, 2, -2,
            EndToEndCardinalRotation.South, wallBuild);
        yield return EdgeDrag("build enclosed north thin-wall boundary", 2, 2, -2, 2,
            EndToEndCardinalRotation.North, wallBuild);
        yield return EdgeDrag("build enclosed west thin-wall boundary", -2, 2, -2, -2,
            EndToEndCardinalRotation.West, wallBuild);
        yield return EdgeDrag("build lower east boundary up to door opening", 2, -2, 2, -1,
            EndToEndCardinalRotation.East, wallBuild);
        yield return EdgeDrag("build upper east boundary above door opening", 2, 1, 2, 2,
            EndToEndCardinalRotation.East, wallBuild);
        yield return EdgeDrag("build one native thin door on the exclusive east edge", 2, 0, 2, 0,
            EndToEndCardinalRotation.East, doorBuild);

        yield return new WaitUntilStep(
            "native designators complete an enclosed room and one edge door",
            _ => CaptureStructures() && RoomSplitExists(),
            new EndToEndDeadline(900, 1_800, TimeSpan.FromSeconds(45)));
        yield return new CameraActionStep(
            "frame completed Thin Wall room and closed Thin Door",
            AllFixtureIds(),
            paddingPixels: 170);
        yield return new ScreenshotStep(
            "closed Thin Door divides the enclosed Thin Wall room from outdoors",
            AllFixtureIds(),
            paddingPixels: 170);
        yield return new AssertionStep(
            "completed edge loop participates in native rooms without roof support",
            _ =>
            {
                Room inside = center.GetRoom(map);
                Room outside = (center + new IntVec3(4, 0, 0)).GetRoom(map);
                EndToEndAssert.False(ReferenceEquals(inside, outside),
                    "A completed Thin Wall/Thin Door loop must split its interior from the outside region.");
                EndToEndAssert.False(inside.TouchesMapEdge,
                    "The Thin Wall interior room must not inherit the outside region's map-edge flag.");
                EndToEndAssert.False(door.def.holdsRoof,
                    "Room participation must not make Thin Doors into roof supports.");
                EndToEndAssert.True(door.Position.Standable(map),
                    "The door owner cell must remain standable inside the room.");
                EndToEndAssert.Equal(ThinWallSide.East, door.OwnedSide,
                    "Drawing the Thin Door must not mutate its persistent east-edge ownership.");
            });

        string ownerOption = GoHereOption(context, ownerMarker);
        yield return new SelectionActionStep("select pawn for non-door-side movement", new[] { pawn.ThingID }, false);
        yield return new FloatMenuActionStep(
            "order pawn into the Thin Door owner tile from its open south side",
            pawn.ThingID,
            ownerMarker.ThingID,
            ownerOption);
        yield return new TimeControlActionStep("run non-door-side owner-cell movement", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "pawn reaches the owner cell without opening the east-edge door",
            _ => pawn.Position == door.Position,
            new EndToEndDeadline(1_200, 1_800, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep("pause on freely usable door owner cell", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "non-door sides of a Thin Door tile remain ordinary movement",
            _ => EndToEndAssert.False(door.Open,
                "Entering the owner tile from a non-door side must not open or wait on the Thin Door."));
        yield return new ScreenshotStep(
            "pawn stands on the closed Thin Door tile without obstruction",
            AllFixtureIds(),
            paddingPixels: 170);

        string outsideOption = GoHereOption(context, outsideMarker);
        yield return new FloatMenuActionStep(
            "order pawn across the closed Thin Door edge",
            pawn.ThingID,
            outsideMarker.ThingID,
            outsideOption);
        yield return new AssertionStep(
            "edge-door destination is natively reachable for its authorized pawn",
            _ =>
            {
                TraverseParms parms = TraverseParms.For(pawn);
                bool edgeReachable = map.GetComponent<Pathing.ThinWallMapComponent>()
                    .CanReachThroughEdges(pawn.Position, outsideMarker.Position, PathEndMode.OnCell, parms);
                bool nativeReachable = map.reachability.CanReach(
                    pawn.Position,
                    outsideMarker.Position,
                    PathEndMode.OnCell,
                    parms);
                PawnPath diagnosticPath = map.pathFinder.FindPathNow(
                    pawn.Position,
                    outsideMarker.Position,
                    parms,
                    peMode: PathEndMode.OnCell);
                bool pathFound = diagnosticPath.Found;
                int pathNodes = pathFound ? diagnosticPath.NodesLeftCount : 0;
                diagnosticPath.Dispose();
                EndToEndAssert.True(door.PawnCanOpen(pawn),
                    "The player colonist must be authorized to open its faction's Thin Door.");
                EndToEndAssert.True(edgeReachable,
                    "The Thin Walls edge graph must connect the two regions through an authorized Thin Door.");
                EndToEndAssert.True(nativeReachable,
                    "Native reachability must accept the same authorized Thin Door route.");
                EndToEndAssert.True(pathFound,
                    $"The native pathfinder must produce the authorized Thin Door path (nodes={pathNodes}).");
                EndToEndAssert.NotNull(pawn.CurJob,
                    "The native Go here command must start a movement job across the Thin Door edge.");
                crossOrderTick = Find.TickManager.TicksGame;
                sampledPosition = pawn.Position;
                crossingTrace.Clear();
            });
        yield return new TimeControlActionStep("run native edge-door opening and crossing", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "native pather starts and visibly waits for Thin Door opening",
            _ =>
            {
                SampleCrossing();
                observedDoorOpening |= door.Open;
                return (door.Open && pawn.Position == door.Position) ||
                       pawn.Position == outsideMarker.Position ||
                       Find.TickManager.TicksGame - crossOrderTick >= 300;
            },
            new EndToEndDeadline(900, 1_200, TimeSpan.FromSeconds(35)));
        yield return new AssertionStep(
            "authorized edge crossing reaches the Thin Door opening state",
            _ =>
            {
                EndToEndAssert.True(
                    door.Open,
                    $"Door remained closed after 300 ticks: pawn={pawn.Position}, next={pawn.pather.nextCell}, " +
                    $"moving={pawn.pather.MovingNow}, job={pawn.CurJob?.def?.defName ?? "null"}, " +
                    $"door={door.Position}/{door.OwnedSide}, target={outsideMarker.Position}, " +
                    $"canOpen={door.PawnCanOpen(pawn)}, canPass={door.CanPhysicallyPass(pawn)}, " +
                    $"trace={string.Join(";", crossingTrace)}.");
                EndToEndAssert.Equal(ThinWallSide.East, door.OwnedSide,
                    "The open animation must remain on the designated east edge.");
            });
        yield return new SelectionActionStep(
            "clear selection so the opening leaves remain unobscured",
            Array.Empty<string>(),
            false);
        yield return new CameraActionStep(
            "frame the opening Thin Door and its adjacent Thin Walls",
            DoorFocusIds(),
            paddingPixels: 110);
        yield return new ScreenshotStep(
            "Thin Door leaves visibly open while pawn waits at the owned edge",
            DoorFocusIds(),
            paddingPixels: 110);
        yield return new WaitUntilStep(
            "pawn crosses only after the Thin Door opening sequence",
            _ =>
            {
                observedDoorOpening |= door.Open;
                return pawn.Position == outsideMarker.Position;
            },
            new EndToEndDeadline(2_400, 3_600, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep("pause after Thin Door crossing", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "door-edge crossing uses the native door interaction",
            _ =>
            {
                EndToEndAssert.True(observedDoorOpening,
                    "The pawn must visibly open the edge door before crossing it.");
                EndToEndAssert.Equal(outsideMarker.Position, pawn.Position,
                    "The native Go here order must reach the outside target through the door edge.");
                EndToEndAssert.Equal(ThinWallSide.East, door.OwnedSide,
                    "Crossing and drawing must preserve the designated east edge.");
            });
        yield return new CameraActionStep(
            "frame the unobstructed Thin Door immediately after crossing",
            DoorFocusIds(),
            paddingPixels: 110);
        yield return new ScreenshotStep(
            "pawn arrived outside after opening and crossing the Thin Door edge",
            DoorFocusIds(),
            paddingPixels: 110);

        string returnOption = GoHereOption(context, ownerMarker);
        yield return new FloatMenuActionStep(
            "order pawn back across the Thin Door edge onto its owner tile",
            pawn.ThingID,
            ownerMarker.ThingID,
            returnOption);
        yield return new TimeControlActionStep("run reverse Thin Door crossing", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "reverse crossing reaches the otherwise-free owner tile",
            _ => pawn.Position == door.Position,
            new EndToEndDeadline(1_800, 2_400, TimeSpan.FromSeconds(55)));
        yield return new TimeControlActionStep("pause on owner tile after reverse crossing", true, EndToEndGameSpeed.Normal);

        EndToEndGizmoOption holdOpen = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { door.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled &&
                              option.Interaction == EndToEndGizmoInteraction.Toggle &&
                              option.Label.IndexOf("hold open", StringComparison.OrdinalIgnoreCase) >= 0);
        yield return new GizmoActionStep(
            "toggle Thin Door hold-open through its ordinary selected-door command",
            new[] { door.ThingID },
            holdOpen.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: holdOpen.StableId);
        yield return new AssertionStep(
            "Thin Door enters hold-open state while pawn occupies the owner cell",
            _ =>
            {
                EndToEndAssert.True(door.HoldOpen,
                    "The native Hold open command must toggle the inherited door state.");
                EndToEndAssert.True(door.Open,
                    "The door must remain visibly open after the reverse crossing.");
                EndToEndAssert.Equal(door.Position, pawn.Position,
                    "The pawn must remain on the standable owner tile during the close-delay test.");
                holdOpenObservationTick = Find.TickManager.TicksGame;
            });
        yield return new TimeControlActionStep("run beyond automatic close delay while held open", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "hold-open survives beyond the ordinary close delay",
            _ => Find.TickManager.TicksGame - holdOpenObservationTick >= 180,
            new EndToEndDeadline(600, 900, TimeSpan.FromSeconds(25)));
        yield return new TimeControlActionStep("pause after hold-open delay", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "hold-open keeps only the door edge open",
            _ =>
            {
                EndToEndAssert.True(door.Open && door.HoldOpen,
                    "Hold open must keep the Thin Door open beyond its ordinary close delay.");
                EndToEndAssert.Equal(door.Position, pawn.Position,
                    "Waiting in the owner cell must remain ordinary standable-cell behavior.");
            });
        yield return new ScreenshotStep(
            "held-open Thin Door while pawn occupies its free owner tile",
            DoorFocusIds(),
            paddingPixels: 110);

        yield return new GizmoActionStep(
            "turn Thin Door hold-open back off through the ordinary command",
            new[] { door.ThingID },
            holdOpen.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: holdOpen.StableId);
        yield return new TimeControlActionStep("run automatic Thin Door closing", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "Thin Door closes although pawn still stands in the owner tile",
            _ => !door.Open,
            new EndToEndDeadline(1_200, 1_800, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep("pause after owner-cell close", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "owner-cell occupancy does not hold a Thin Door open",
            _ =>
            {
                EndToEndAssert.False(door.HoldOpen,
                    "The second native toggle must clear Hold open.");
                EndToEndAssert.False(door.Open,
                    "An owner-cell occupant whose next step does not cross the edge must not refresh door timing.");
                EndToEndAssert.Equal(door.Position, pawn.Position,
                    "The closed Thin Door must coexist with a pawn in its standable owner cell.");
            });
        yield return new ScreenshotStep(
            "Thin Door closes on its edge without obstructing its occupied owner tile",
            DoorFocusIds(),
            paddingPixels: 110);

        Building_ThinWall breachWall = walls.Single(wall =>
            wall.Position == center + new IntVec3(0, 0, -2) && wall.OwnedSide == ThinWallSide.South);
        EndToEndGizmoOption deconstruct = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { breachWall.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled &&
                              option.Interaction == EndToEndGizmoInteraction.Invoke &&
                              option.Label.IndexOf("deconstruct", StringComparison.OrdinalIgnoreCase) >= 0);
        yield return new GizmoActionStep(
            "deconstruct one Thin Wall boundary through its native command",
            new[] { breachWall.ThingID },
            deconstruct.RuntimeType,
            EndToEndGizmoInteraction.Invoke,
            stableGizmoId: deconstruct.StableId);
        yield return new WaitUntilStep(
            "room regions remerge after the player opens the boundary",
            _ => breachWall.Destroyed && ReferenceEquals(
                center.GetRoom(map),
                (center + new IntVec3(4, 0, 0)).GetRoom(map)),
            new EndToEndDeadline(900, 1_500, TimeSpan.FromSeconds(40)));
        yield return new ScreenshotStep(
            "native Thin Wall removal opens and remerges the room",
            AllFixtureIds().Where(id => id != breachWall.ThingID),
            paddingPixels: 170);
        yield return new CheckpointStep("record Thin Door and room workflow", _ => new Dictionary<string, string>
        {
            ["thinWallCount"] = walls.Count.ToString(),
            ["doorOpenedBeforeCrossing"] = observedDoorOpening.ToString(),
            ["holdOpenToggledAndReleased"] = (!door.HoldOpen && !door.Open).ToString(),
            ["doorOwnerCellStandable"] = door.Position.Standable(map).ToString(),
            ["roomRemergedAfterBreach"] = ReferenceEquals(
                center.GetRoom(map),
                (center + new IntVec3(4, 0, 0)).GetRoom(map)).ToString(),
            ["doorHoldsRoof"] = door.def.holdsRoof.ToString(),
        });
    }

    private GizmoActionStep EdgeDrag(
        string name,
        int startX,
        int startZ,
        int endX,
        int endZ,
        EndToEndCardinalRotation side,
        EndToEndGizmoOption option) => new(
        name,
        Array.Empty<string>(),
        option.RuntimeType,
        EndToEndGizmoInteraction.Drag,
        side,
        new EndToEndBuildMaterial("BlocksGranite"),
        stableGizmoId: option.StableId,
        startCell: new EndToEndMapCell(center.x + startX, center.z + startZ),
        endCell: new EndToEndMapCell(center.x + endX, center.z + endZ),
        architectCategoryDefNames: new[] { "Structure" });

    private bool CaptureStructures()
    {
        walls.Clear();
        walls.AddRange(map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName))
            .OfType<Building_ThinWall>()
            .Where(wall => wall.Position.InHorDistOf(center, 8f)));
        door = map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinDoorDefName))
            .OfType<Building_ThinDoor>()
            .SingleOrDefault(candidate => candidate.Position == center + new IntVec3(2, 0, 0))!;
        return walls.Count == 19 && door != null;
    }

    private bool RoomSplitExists()
    {
        Room inside = center.GetRoom(map);
        Room outside = (center + new IntVec3(4, 0, 0)).GetRoom(map);
        return inside != null && outside != null && !ReferenceEquals(inside, outside);
    }

    private void SampleCrossing()
    {
        IntVec3 current = pawn.Position;
        if (current == sampledPosition)
        {
            return;
        }

        bool doorEdge = ThinWallUtility.StepCrossesEdge(
            sampledPosition,
            current,
            door.OwnedEdge.Shared);
        bool wallEdge = ThinWallUtility.BlocksStep(map, sampledPosition, current);
        crossingTrace.Add($"{sampledPosition}>{current}:door={doorEdge},wall={wallEdge}");
        sampledPosition = current;
    }

    private Thing SpawnMarker(IntVec3 cell)
    {
        Thing marker = ThingMaker.MakeThing(ThingDefOf.WoodLog);
        marker.stackCount = 1;
        GenSpawn.Spawn(marker, cell, map);
        return marker;
    }

    private string GoHereOption(IEndToEndContext context, Thing target) =>
        context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(pawn.ThingID, target.ThingID)
            .Single(option => !option.Disabled &&
                              option.Label.IndexOf("go here", StringComparison.OrdinalIgnoreCase) >= 0)
            .StableId;

    private IEnumerable<string> AllFixtureIds() => fixtures
        .Concat<Thing>(walls)
        .Append(door)
        .Where(thing => thing != null && !thing.Destroyed)
        .Select(thing => thing.ThingID);

    private IEnumerable<string> DoorFocusIds() => walls
        .Where(wall => wall.Position.x == door.Position.x &&
                       Math.Abs(wall.Position.z - door.Position.z) <= 1)
        .Cast<Thing>()
        .Append(door)
        .Where(thing => !thing.Destroyed)
        .Select(thing => thing.ThingID);

    private static EndToEndGizmoOption SingleBuild(IEndToEndGizmoCatalog catalog, string defName) =>
        catalog.Query(Array.Empty<string>(), new[] { "Structure" })
            .Single(option => !option.Disabled &&
                              option.BuildableDefName == defName &&
                              option.Interaction == EndToEndGizmoInteraction.Drag);

    private static IntVec3 FindClearCenter(Map map, int radius)
    {
        for (int x = -60; x <= 60; x += 24)
        {
            for (int z = -60; z <= 60; z += 24)
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

        throw new EndToEndAssertionException("Could not find a clear Thin Door room workflow area.");
    }
}
