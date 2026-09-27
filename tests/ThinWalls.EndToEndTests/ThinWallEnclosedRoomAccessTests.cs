using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using Verse;
using Verse.AI;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest(
    "thin-walls.native-enclosed-room-access",
    "fumblesneeze.thinwalls",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.thinwalls",
    MaxFrames = 60_000,
    MaxGameTicks = 70_000,
    MaxWallClockSeconds = 960)]
public sealed class ThinWallEnclosedRoomAccessTest : IRimWorldEndToEndTest
{
    private static readonly string[] PathFailureMarkers =
    {
        "ran out of path nodes",
        "got an invalid path from path finder",
    };

    private readonly List<Thing> fixtures = new();
    private readonly List<Building_ThinWall> walls = new();
    private readonly List<Thing> materialPiles = new();
    private Map map = null!;
    private IntVec3 center;
    private Pawn builder = null!;
    private Pawn mover = null!;
    private Thing outsideStartMarker = null!;
    private Thing insideTargetMarker = null!;
    private Building_ThinDoor door = null!;
    private EndToEndGizmoOption wallBuild = null!;
    private EndToEndGizmoOption doorBuild = null!;
    private EndToEndGizmoOption buildRoofDesignator = null!;
    private bool originalGodMode;
    private int baselinePathFailureCount;
    private int wallWaitStartTick;
    private int roofPhaseStartTick;
    private int observedWalls;
    private int observedBlueprints;
    private int observedFrames;
    private string builderJob = "";
    private string reachabilitySummary = "";
    private bool firstInteriorArrivalLogged;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        center = FindClearCenter(map, 10);
        originalGodMode = DebugSettings.godMode;
        DebugSettings.godMode = false;

        IEndToEndGizmoCatalog catalog = context.GetRequiredService<IEndToEndGizmoCatalog>();
        wallBuild = SingleBuild(catalog, ThinWallUtility.ThinWallDefName);
        doorBuild = SingleBuild(catalog, ThinWallUtility.ThinDoorDefName);

        builder = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        for (int attempt = 0;
             attempt < 32 &&
             (builder.WorkTagIsDisabled(WorkTags.Constructing) ||
              builder.WorkTagIsDisabled(WorkTags.Hauling) ||
              builder.WorkTypeIsDisabled(WorkTypeDefOf.Construction) ||
              builder.WorkTypeIsDisabled(WorkTypeDefOf.Hauling));
             attempt++)
        {
            builder = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        }

        builder.Name = new NameTriple("Edge", "Wright", "Builder");
        builder.workSettings.EnableAndInitialize();
        builder.workSettings.SetPriority(WorkTypeDefOf.Construction, 1);
        builder.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
        EndToEndAssert.False(builder.WorkTypeIsDisabled(WorkTypeDefOf.Construction),
            "The generated builder must be capable of construction work.");
        GenSpawn.Spawn(builder, center + new IntVec3(4, 0, 3), map);

        mover = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        mover.Name = new NameTriple("Edge", "Access", "Tester");
        GenSpawn.Spawn(mover, center + new IntVec3(-3, 0, -1), map);
        mover.drafter.Drafted = true;

        outsideStartMarker = SpawnMarker(center + new IntVec3(-3, 0, -1));
        insideTargetMarker = SpawnMarker(center + new IntVec3(-2, 0, 0));
        fixtures.Add(builder);
        fixtures.Add(mover);
        fixtures.Add(outsideStartMarker);
        fixtures.Add(insideTargetMarker);

        context.DeferCleanup(() =>
        {
            DebugSettings.godMode = originalGodMode;
            foreach (Thing thing in fixtures.Concat<Thing>(walls).Append(door).Concat(materialPiles)
                         .Where(thing => thing != null).Distinct().ToArray())
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
        yield return new AssertionStep(
            "fixture materials exist for natural construction",
            _ =>
            {
                ThingDef blocks = DefDatabase<ThingDef>.GetNamed("BlocksGranite");
                foreach (IntVec3 offset in new IntVec3[]
                         {
                             new(5, 0, 3),
                             new(5, 0, 4),
                             new(6, 0, 3)
                         })
                {
                    Thing pile = ThingMaker.MakeThing(blocks);
                    pile.stackCount = Math.Min(pile.def.stackLimit, 75);
                    GenSpawn.Spawn(pile, center + offset, map);
                    materialPiles.Add(pile);
                    EndToEndAssert.True(pile.Spawned, "The spawned material pile must be haulable.");
                }
            });

        // The player order of operations: walls are designated first, the door last,
        // and the room only becomes traversable when the final structure completes.
        yield return EdgeDrag("designate south thin-wall boundary", -2, -2, 2, -2,
            EndToEndCardinalRotation.South, wallBuild);
        yield return EdgeDrag("designate north thin-wall boundary", 2, 2, -2, 2,
            EndToEndCardinalRotation.North, wallBuild);
        yield return EdgeDrag("designate west thin-wall boundary", -2, 2, -2, -2,
            EndToEndCardinalRotation.West, wallBuild);
        yield return EdgeDrag("designate east thin-wall boundary", 2, -2, 2, -1,
            EndToEndCardinalRotation.East, wallBuild);
        yield return EdgeDrag("designate upper east boundary above door opening", 2, 1, 2, 2,
            EndToEndCardinalRotation.East, wallBuild);

        yield return new TimeControlActionStep("run hauling and wall construction", false, EndToEndGameSpeed.Fast);
        wallWaitStartTick = Find.TickManager.TicksGame;
        yield return new WaitUntilStep(
            "colonists build the thin walls through ordinary construction jobs",
            _ => ObserveConstructionProgress() >= 18 ||
                 Find.TickManager.TicksGame - wallWaitStartTick >= 10_000,
            new EndToEndDeadline(16_000, 22_000, TimeSpan.FromSeconds(240)));
        yield return new TimeControlActionStep("pause before the construction nudge", true, EndToEndGameSpeed.Normal);
        foreach (Thing pending in PendingConstructibles())
        {
            EndToEndFloatMenuOption? prioritize = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
                .Query(builder.ThingID, pending.ThingID)
                .FirstOrDefault(option => !option.Disabled &&
                                          option.Label.IndexOf("priorit", StringComparison.OrdinalIgnoreCase) >= 0);
            if (prioritize != null)
            {
                yield return new FloatMenuActionStep(
                    $"prioritize the pending constructible at {pending.Position}",
                    builder.ThingID,
                    pending.ThingID,
                    prioritize.StableId);
            }
        }

        yield return new TimeControlActionStep("run the prioritized wall construction", false, EndToEndGameSpeed.Fast);
        yield return new WaitUntilStep(
            "colonists complete all thin walls",
            _ => ObserveConstructionProgress() >= 18,
            new EndToEndDeadline(16_000, 22_000, TimeSpan.FromSeconds(220)));
        yield return new TimeControlActionStep("pause after wall completion", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep(
            "frame the room during the wall construction checkpoint",
            AllFixtureIds(),
            paddingPixels: 200);
        yield return new ScreenshotStep(
            "wall construction checkpoint map state",
            AllFixtureIds(),
            paddingPixels: 200);
        yield return new AssertionStep(
            "colonists complete the thin walls through ordinary construction jobs",
            _ =>
            {
                if (ObserveConstructionProgress() >= 18)
                {
                    return;
                }

                StringBuilder stall = new();
                stall.Append($"walls={observedWalls}, blueprints={observedBlueprints}, frames={observedFrames}, ");
                stall.Append($"builder={builder.Position} job={builderJob}, ");
                stall.Append($"tick={Find.TickManager.TicksGame - wallWaitStartTick} after start. Pending: ");
                foreach (Frame frame in PendingFrames())
                {
                    stall.Append($"[{frame.Position}/{(ThinWallSide)frame.Rotation.AsInt} " +
                                 $"touch={builder.CanReach(frame, Verse.AI.PathEndMode.Touch, Danger.Deadly)}] ");
                }

                EndToEndAssert.Fail("Natural construction stalled: " + stall);
            });

        yield return EdgeDrag("designate one thin door on the exclusive east edge last", 2, 0, 2, 0,
            EndToEndCardinalRotation.East, doorBuild);
        yield return new TimeControlActionStep("run thin door construction", false, EndToEndGameSpeed.Fast);
        yield return new WaitUntilStep(
            "the door completes and the room becomes authorized-traversable",
            _ => CaptureStructures() && RoomSplitExists(),
            new EndToEndDeadline(4_000, 6_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep("pause after the sealed room gains its door", true, EndToEndGameSpeed.Normal);

        yield return new AssertionStep(
            "natural construction produced the sealed room and closed door",
            _ =>
            {
                EndToEndAssert.Equal(19, walls.Count, "The sealed perimeter needs all 19 edge walls.");
                EndToEndAssert.False(ReferenceEquals(center.GetRoom(map), (center + new IntVec3(4, 0, 0)).GetRoom(map)),
                    "The completed loop must split its interior from the outside region.");
                EndToEndAssert.False(door.Open, "The access workflow starts with a closed Thin Door.");
            });

        // The reported setup: a thick wall outside with its roof extending toward the room.
        yield return new AssertionStep(
            "an unconnected thick wall anchors the roof extension",
            _ =>
            {
                ThingDef blocks = DefDatabase<ThingDef>.GetNamed("BlocksGranite");
                foreach (IntVec3 offset in new IntVec3[] { new(4, 0, 0), new(5, 0, 0) })
                {
                    Building wall = (Building)ThingMaker.MakeThing(ThingDefOf.Wall, blocks);
                    GenSpawn.Spawn(wall, center + offset, map);
                    fixtures.Add(wall);
                }

                EndToEndAssert.Equal(2, CountThickWalls(),
                    "The roof anchor thick walls must stand east of the thin room.");
            });

        buildRoofDesignator = FindBuildRoofDesignator(context.GetRequiredService<IEndToEndGizmoCatalog>(),
            out string[] buildRoofDesignatorCategories);
        yield return new GizmoActionStep(
            "designate roof over the thick wall extending toward the thin room",
            Array.Empty<string>(),
            buildRoofDesignator.RuntimeType,
            EndToEndGizmoInteraction.Drag,
            stableGizmoId: buildRoofDesignator.StableId,
            startCell: new EndToEndMapCell(center.x + 3, center.z - 1),
            endCell: new EndToEndMapCell(center.x + 5, center.z + 1),
            architectCategoryDefNames: buildRoofDesignatorCategories);
        yield return new TimeControlActionStep("run roof construction over the boundary", false, EndToEndGameSpeed.Fast);
        roofPhaseStartTick = Find.TickManager.TicksGame;
        yield return new WaitUntilStep(
            "the roof workflow runs over the thin-room boundary",
            _ => Find.TickManager.TicksGame - roofPhaseStartTick >= 6_000,
            new EndToEndDeadline(8_000, 12_000, TimeSpan.FromSeconds(180)));
        yield return new TimeControlActionStep("pause after roof work settles", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the roof-extension workflow roofed the boundary beside the thin room",
            _ =>
            {
                int roofed = 0;
                foreach (IntVec3 cell in new CellRect(center.x + 3, center.z - 1, 3, 3).Cells)
                {
                    if (map.roofGrid.RoofAt(cell) != null)
                    {
                        roofed++;
                    }
                }

                EndToEndAssert.True(roofed > 0,
                    $"The roof extension must leave constructed roof cells beside the thin room.");
                AssertNoPathfinderFailures();
            });

        yield return new AssertionStep(
            "the access fixture is re-anchored after the long construction window",
            _ =>
            {
                IntVec3 outsideStart = center + new IntVec3(-3, 0, -1);
                IntVec3 insideTarget = center + new IntVec3(-2, 0, 0);
                outsideStartMarker.Destroy(DestroyMode.Vanish);
                insideTargetMarker.Destroy(DestroyMode.Vanish);
                outsideStartMarker = SpawnMarker(outsideStart);
                insideTargetMarker = SpawnMarker(insideTarget);
                fixtures.Add(outsideStartMarker);
                fixtures.Add(insideTargetMarker);
                mover.drafter.Drafted = true;
                mover.Position = outsideStart;
                mover.Notify_Teleported();
                EndToEndAssert.Equal(outsideStart, mover.Position,
                    "The mover must start the access phase from the west outside cell.");
                EndToEndAssert.Equal(insideTarget, insideTargetMarker.Position,
                    "The interior target marker must sit on the west wall owner cell.");
            });

        yield return new AssertionStep(
            "the west-side interior target is reachable and concretely pathable",
            _ =>
            {
                TraverseParms parms = TraverseParms.For(mover);
                Pathing.ThinWallMapComponent component = map.GetComponent<Pathing.ThinWallMapComponent>();
                bool bridgeReachable = component.TryBridgeEdges(
                    mover.Position, insideTargetMarker.Position, PathEndMode.OnCell, parms);
                bool nativeReachable = map.reachability.CanReach(
                    mover.Position, insideTargetMarker.Position, PathEndMode.OnCell, parms);
                PawnPath diagnosticPath = map.pathFinder.FindPathNow(
                    mover.Position, insideTargetMarker.Position, parms, peMode: PathEndMode.OnCell);
                bool pathFound = diagnosticPath.Found;
                int pathNodes = pathFound ? diagnosticPath.NodesLeftCount : 0;
                string pathNodesText = "";
                if (pathFound)
                {
                    foreach (IntVec3 node in diagnosticPath.NodesReversed)
                    {
                        pathNodesText += " " + node;
                    }

                    diagnosticPath.Dispose();
                }

                var md = map.pathFinder.MapData;
                var ci = map.cellIndices;
                string startConnections = md.CellConnectionsAt(ci.CellToIndex(mover.Position)).ToString();
                string targetConnections = md.CellConnectionsAt(ci.CellToIndex(insideTargetMarker.Position)).ToString();
                reachabilitySummary =
                    $"bridge={bridgeReachable}, native={nativeReachable}, pathFound={pathFound}({pathNodes} nodes:{pathNodesText}), " +
                    $"mover={mover.Position}, marker={insideTargetMarker.Position}, markerExpected={center + new IntVec3(-2, 0, 0)}, " +
                    $"edges={component.OwnedEdgeCount}, doors={component.DoorCount}, doorRegistered={component.HasEdge(door.OwnedEdge.Shared)}, " +
                    $"startConn=[{startConnections}], targetConn=[{targetConnections}]";
                EndToEndAssert.True(bridgeReachable,
                    $"The edge graph must admit the authorized door route: {reachabilitySummary}.");
                EndToEndAssert.True(nativeReachable,
                    $"Native reachability must accept the same route: {reachabilitySummary}.");
                EndToEndAssert.True(pathFound,
                    $"The native pathfinder must produce a concrete route: {reachabilitySummary}.");
                EndToEndAssert.True(pathNodes > 3,
                    $"The native route must go around through the door, not slip a wall corner: {reachabilitySummary}.");
                baselinePathFailureCount = CountPathFailureMessages();
                EndToEndAssert.Equal(0, baselinePathFailureCount,
                    "The baseline must be free of pathfinder failures before the access orders.");
            });

        string goHere = "";
        yield return new AssertionStep(
            "the native movement menu exposes the authorized interior order",
            _ =>
            {
                var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
                    .Query(mover.ThingID, insideTargetMarker.ThingID)
                    .ToArray();
                EndToEndFloatMenuOption? enabled = options.FirstOrDefault(option => !option.Disabled &&
                    option.Label.IndexOf("go here", StringComparison.OrdinalIgnoreCase) >= 0);
                reachabilitySummary += ", menu=[" +
                    string.Join(" | ", options.Select(option => $"{option.Label}:disabled={option.Disabled}")) + "]";
                EndToEndAssert.NotNull(enabled,
                    "The native movement menu must offer an enabled Go here order into the sealed room. " +
                    reachabilitySummary);
                goHere = enabled!.StableId;
            });
        yield return new FloatMenuActionStep(
            "order drafted pawn from the west outside cell into the sealed interior",
            mover.ThingID,
            insideTargetMarker.ThingID,
            goHere);
        yield return new AssertionStep(
            "the native move order starts a movement job before the run window",
            _ => EndToEndAssert.NotNull(mover.CurJob,
                "The enabled native Go here order must start a Goto job."));
        yield return new TimeControlActionStep("run the native around-the-door route", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "pawn reaches the sealed interior target through the authorized door",
            _ => mover.Position == insideTargetMarker.Position,
            new EndToEndDeadline(2_400, 3_600, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep("pause after the interior arrival", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the native access workflow stays free of pathfinder failures",
            _ =>
            {
                firstInteriorArrivalLogged = true;
                AssertNoPathfinderFailures();
            });
        yield return new CameraActionStep(
            "frame the pawn inside the sealed thin room",
            AllFixtureIds(),
            paddingPixels: 170);
        yield return new ScreenshotStep(
            "pawn stands inside the thin-walls-only room after the native move order",
            AllFixtureIds(),
            paddingPixels: 170);

        // Player follow-up from the same sealed room: repeatedly re-order across the
        // boundary while the door opens and closes, exactly like correcting a move order.
        yield return new FloatMenuActionStep(
            "order the pawn back outside through the door",
            mover.ThingID,
            outsideStartMarker.ThingID,
            GoHereOption(context, outsideStartMarker));
        yield return new TimeControlActionStep("run the return trip", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "pawn reaches the outside start cell again",
            _ => mover.Position == outsideStartMarker.Position,
            new EndToEndDeadline(2_400, 3_600, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep("pause after the return trip", true, EndToEndGameSpeed.Normal);
        yield return new FloatMenuActionStep(
            "repeat the interior order through the freshly closed door",
            mover.ThingID,
            insideTargetMarker.ThingID,
            GoHereOption(context, insideTargetMarker));
        yield return new TimeControlActionStep("run the repeated interior order", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the repeated order reaches the interior target again",
            _ => mover.Position == insideTargetMarker.Position,
            new EndToEndDeadline(2_400, 3_600, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep("pause after the repeated interior order", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "repeated sealed-room access stays free of pathfinder failures",
            _ => AssertNoPathfinderFailures());
        yield return new CameraActionStep(
            "frame the sealed room beside the roofed thick wall",
            AllFixtureIds(),
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "pawn inside the thin room with the roofed thick wall outside",
            AllFixtureIds(),
            paddingPixels: 220);
        yield return new CheckpointStep("record enclosed room access workflow", _ => new Dictionary<string, string>
        {
            ["firstInteriorArrival"] = firstInteriorArrivalLogged.ToString(),
            ["pathFailuresBaseline"] = baselinePathFailureCount.ToString(),
            ["reachability"] = reachabilitySummary,
        });
    }

    private EndToEndGizmoOption FindBuildRoofDesignator(IEndToEndGizmoCatalog catalog, out string[] categories)
    {
        string label = "BuildRoof".Translate().ToString();
        foreach (string[] candidateCategories in new[]
                 {
                     new[] { "Orders" },
                     new[] { "Structure" },
                     new[] { "Zone" },
                     new[] { "Misc" },
                     new[] { "Furniture" }
                 })
        {
            EndToEndGizmoOption? candidate = catalog.Query(Array.Empty<string>(), candidateCategories)
                .Where(option => !option.Disabled)
                .FirstOrDefault(option => option.Label.IndexOf(label, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          option.Label.IndexOf("build roof", StringComparison.OrdinalIgnoreCase) >= 0);
            if (candidate != null)
            {
                categories = candidateCategories;
                return candidate;
            }
        }

        throw new EndToEndAssertionException(
            "Could not discover the native build-roof designator in any architect category.");
    }

    private void AssertNoPathfinderFailures()
    {
        int failures = CountPathFailureMessages();
        StringBuilder details = new();
        details.Append($"mover={mover.Position}, target={insideTargetMarker.Position}, ");
        details.Append($"moving={mover.pather.MovingNow}, next={mover.pather.nextCell}, ");
        details.Append($"job={mover.CurJob?.def?.defName ?? "null"}, doorOpen={door.Open}, ");
        details.Append($"reachability={reachabilitySummary}, ");
        details.Append($"pathFailures={baselinePathFailureCount}->{failures}");
        EndToEndAssert.Equal(0, failures - baselinePathFailureCount,
            "The sealed-room workflow must not log pathfinder failures. " + details);
    }

    private static int CountPathFailureMessages() => Log.Messages.Count(message =>
        message.type == LogMessageType.Error &&
        PathFailureMarkers.Any(marker => message.text.Contains(marker)));

    private int CountCompletedWalls() => map.listerThings
        .ThingsOfDef(DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName))
        .OfType<Building_ThinWall>()
        .Count(wall => wall.Position.InHorDistOf(center, 8f));

    private IEnumerable<Frame> PendingFrames() => map.listerThings
        .ThingsOfDef(DefDatabase<ThingDef>.GetNamed("Frame_TW_ThinWall"))
        .OfType<Frame>()
        .Where(frame => frame.Position.InHorDistOf(center, 8f));

    private IEnumerable<Thing> PendingConstructibles() => PendingFrames()
        .Concat<Thing>(map.listerThings
            .ThingsOfDef(DefDatabase<ThingDef>.GetNamed("Blueprint_TW_ThinWall"))
            .Where(thing => thing.Position.InHorDistOf(center, 8f)));

    private int ObserveConstructionProgress()
    {
        observedWalls = CountCompletedWalls();
        observedBlueprints = map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("Blueprint_TW_ThinWall")).Count;
        observedFrames = map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("Frame_TW_ThinWall")).Count;
        builderJob = builder.CurJob?.def.defName + "@" +
                     (builder.CurJob != null && builder.CurJob.GetTarget(Verse.AI.TargetIndex.A).HasThing
                         ? builder.CurJob.GetTarget(Verse.AI.TargetIndex.A).Thing.ThingID.ToString()
                         : builder.CurJob?.GetTarget(Verse.AI.TargetIndex.A).Cell.ToString() ?? "-");
        return observedWalls;
    }

    private int CountThickWalls() => map.listerThings.ThingsOfDef(ThingDefOf.Wall)
        .Count(wall => wall.Position.InHorDistOf(center, 12f));

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

    private Thing SpawnMarker(IntVec3 cell)
    {
        Thing marker = ThingMaker.MakeThing(ThingDefOf.WoodLog);
        marker.stackCount = 1;
        GenSpawn.Spawn(marker, cell, map);
        marker.SetForbidden(true, warnOnFail: false);
        return marker;
    }

    private string GoHereOption(IEndToEndContext context, Thing target) =>
        context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(mover.ThingID, target.ThingID)
            .Single(option => !option.Disabled &&
                              option.Label.IndexOf("go here", StringComparison.OrdinalIgnoreCase) >= 0)
            .StableId;

    private IEnumerable<string> AllFixtureIds() => fixtures
        .Concat<Thing>(walls)
        .Concat(materialPiles)
        .Append(door)
        .Where(thing => thing != null && !thing.Destroyed)
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

        throw new EndToEndAssertionException("Could not find a clear enclosed room access area.");
    }
}
