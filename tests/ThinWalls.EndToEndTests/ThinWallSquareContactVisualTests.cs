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
    "thin-walls.native-square-contact-catalog",
    "fumblesneeze.thinwalls",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.thinwalls",
    MaxFrames = 2_400,
    MaxGameTicks = 2_000,
    MaxWallClockSeconds = 90)]
public sealed class ThinWallSquareContactVisualTest : IRimWorldEndToEndTest
{
    private readonly List<Thing> fixtures = new();
    private readonly List<Building_ThinWall> thinWalls = new();
    private readonly Dictionary<string, IntVec3> layoutVertices = new();
    private readonly Dictionary<IntVec3, TerrainDef> contextTerrains = new();
    private readonly List<(Thing Thing, IntVec3 Position, Rot4 Rotation)> displacedThings = new();
    private Map map = null!;
    private IntVec3 center;
    private CellRect? fixtureArea;
    private EndToEndGizmoOption build = null!;
    private bool originalGodMode;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        originalGodMode = DebugSettings.godMode;
        context.DeferCleanup(() =>
        {
            var cleanupFailures = new List<Exception>();
            try
            {
                DebugSettings.godMode = originalGodMode;
            }
            catch (Exception exception)
            {
                cleanupFailures.Add(exception);
            }

            Thing[] newlySpawnedThinStructures = fixtureArea.HasValue
                ? fixtureArea.Value.Cells
                    .SelectMany(cell => cell.GetThingList(map))
                    .Where(thing => ThinWallUtility.IsThinEdgeDef(thing.def))
                    .Distinct()
                    .ToArray()
                : Array.Empty<Thing>();
            foreach (Thing thing in fixtures
                         .Concat<Thing>(thinWalls)
                         .Concat(newlySpawnedThinStructures)
                         .Distinct()
                         .ToArray())
            {
                try
                {
                    if (!thing.Destroyed)
                    {
                        thing.Destroy(DestroyMode.Vanish);
                    }
                }
                catch (Exception exception)
                {
                    cleanupFailures.Add(exception);
                }
            }

            foreach ((IntVec3 cell, TerrainDef terrain) in contextTerrains)
            {
                try
                {
                    map.terrainGrid.SetTerrain(cell, terrain);
                }
                catch (Exception exception)
                {
                    cleanupFailures.Add(exception);
                }
            }

            foreach ((Thing thing, IntVec3 position, Rot4 rotation) in displacedThings)
            {
                try
                {
                    if (!thing.Destroyed && !thing.Spawned)
                    {
                        GenSpawn.Spawn(thing, position, map, rotation);
                    }
                }
                catch (Exception exception)
                {
                    cleanupFailures.Add(exception);
                }
            }

            if (cleanupFailures.Count > 0)
            {
                throw new AggregateException(
                    "One or more square-contact fixture cleanup operations failed.",
                    cleanupFailures);
            }
        });

        center = FindClearCenter(map, 18);
        fixtureArea = CellRect.CenteredOn(center, 18);
        NormalizeBuildTerrain(fixtureArea.Value);
        DisplaceIncidentalThings(fixtureArea.Value);
        DebugSettings.godMode = true;

        foreach (IntVec3 markerCell in new[]
                 {
                     center + new IntVec3(-17, 0, -12),
                     center + new IntVec3(17, 0, -12),
                     center + new IntVec3(-17, 0, 12),
                     center + new IntVec3(17, 0, 12),
                 })
        {
            Thing marker = ThingMaker.MakeThing(ThingDefOf.WoodLog);
            GenSpawn.Spawn(marker, markerCell, map);
            fixtures.Add(marker);
        }

        AddLayout("side-t-north", new IntVec3(-12, 0, 7),
            new[] { Quadrant.SouthWest, Quadrant.SouthEast });
        AddLayout("side-t-south", new IntVec3(-4, 0, 7),
            new[] { Quadrant.NorthWest, Quadrant.NorthEast });
        AddLayout("side-t-east", new IntVec3(4, 0, 7),
            new[] { Quadrant.SouthWest, Quadrant.NorthWest });
        AddLayout("side-t-west", new IntVec3(12, 0, 7),
            new[] { Quadrant.SouthEast, Quadrant.NorthEast });

        AddLayout("square-corner-ne", new IntVec3(-12, 0, -4), new[] { Quadrant.SouthWest });
        AddLayout("square-corner-nw", new IntVec3(-4, 0, -4), new[] { Quadrant.SouthEast });
        AddLayout("square-corner-se", new IntVec3(4, 0, -4), new[] { Quadrant.NorthWest });
        AddLayout("square-corner-sw", new IntVec3(12, 0, -4), new[] { Quadrant.NorthEast });

        IntVec3 nonContact = center + new IntVec3(0, 0, -12);
        layoutVertices["offset-non-contact"] = nonContact;
        SpawnRegularWall(nonContact + new IntVec3(-2, 0, -1));
        SpawnRegularWall(nonContact + new IntVec3(-1, 0, -1));

        build = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(Array.Empty<string>(), new[] { "Structure" })
            .Single(option => !option.Disabled &&
                              option.BuildableDefName == ThinWallUtility.ThinWallDefName &&
                              option.Interaction == EndToEndGizmoInteraction.Drag);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return Ray("north Thin ray enters the side of a continuous ordinary run",
            layoutVertices["side-t-north"], ContactDirection.North);
        yield return Ray("south Thin ray enters the side of a continuous ordinary run",
            layoutVertices["side-t-south"], ContactDirection.South);
        yield return Ray("east Thin ray enters the side of a continuous ordinary run",
            layoutVertices["side-t-east"], ContactDirection.East);
        yield return Ray("west Thin ray enters the side of a continuous ordinary run",
            layoutVertices["side-t-west"], ContactDirection.West);

        yield return Ray("north square corner contact", layoutVertices["square-corner-ne"], ContactDirection.North);
        yield return Ray("north reflected square corner contact", layoutVertices["square-corner-nw"], ContactDirection.North);
        yield return Ray("south square corner contact", layoutVertices["square-corner-se"], ContactDirection.South);
        yield return Ray("south reflected square corner contact", layoutVertices["square-corner-sw"], ContactDirection.South);

        IntVec3 offset = layoutVertices["offset-non-contact"];
        yield return Ray(
            "deliberately offset Thin ray remains disconnected from the nearby ordinary run",
            offset + new IntVec3(2, 0, 0),
            ContactDirection.East);

        yield return new WaitUntilStep(
            "native designations materialize every square-contact and non-contact Thin ray",
            _ => CaptureWalls() == 18,
            new EndToEndDeadline(600, 900, TimeSpan.FromSeconds(30)));
        yield return new AssertionStep(
            "square-contact catalog retains exact supporting geometry",
            _ =>
            {
                foreach (string key in new[] { "side-t-north", "side-t-south", "side-t-east", "side-t-west" })
                {
                    IntVec3 vertex = layoutVertices[key];
                    int ordinaryQuadrants = fixtures.OfType<Building>()
                        .Count(wall => wall.def == ThingDefOf.Wall && TouchesVertex(wall.Position, vertex));
                    EndToEndAssert.Equal(2, ordinaryQuadrants,
                        $"{key} must keep an uninterrupted two-quadrant ordinary wall run at its T vertex.");
                }

                IntVec3 nonContact = layoutVertices["offset-non-contact"];
                EndToEndAssert.True(thinWalls
                        .Where(wall => wall.Position.InHorDistOf(nonContact, 4f))
                        .All(wall => fixtures.OfType<Building>()
                            .Where(building => building.def == ThingDefOf.Wall)
                            .All(regular => !EdgeTouchesCell(wall.OwnedEdge.Shared, regular.Position))),
                    "The deliberate near-miss must share no exact edge endpoint with a regular wall cell.");
            });
        yield return new SelectionActionStep(
            "clear selection from square-contact catalog",
            Array.Empty<string>(),
            additive: false);

        string[] sideTIds = IdsNear(new[]
        {
            layoutVertices["side-t-north"],
            layoutVertices["side-t-south"],
            layoutVertices["side-t-east"],
            layoutVertices["side-t-west"],
        }, 3f);
        yield return new CameraActionStep("close four rotated side-T contacts", sideTIds, paddingPixels: 110);
        yield return new ScreenshotStep(
            "continuous regular runs accept square Thin Wall side-T contacts in four rotations",
            sideTIds,
            110);

        string[] squareIds = IdsNear(new[]
        {
            layoutVertices["square-corner-ne"],
            layoutVertices["square-corner-nw"],
            layoutVertices["square-corner-se"],
            layoutVertices["square-corner-sw"],
        }, 3f);
        yield return new CameraActionStep("close reflected square end contacts", squareIds, paddingPixels: 110);
        yield return new ScreenshotStep(
            "ordinary and Thin Walls meet with abrupt orthogonal shoulders and no diagonal wedges",
            squareIds,
            110);

        string[] nonContactIds = IdsNear(new[] { layoutVertices["offset-non-contact"] }, 5f);
        yield return new CameraActionStep("close deliberate offset non-contact", nonContactIds, paddingPixels: 130);
        yield return new ScreenshotStep(
            "nearby offset silhouettes remain visually disconnected",
            nonContactIds,
            130);
    }

    private GizmoActionStep Ray(string name, IntVec3 vertex, ContactDirection direction)
    {
        OwnedEdge first = OwnerFor(vertex, direction);
        OwnedEdge second = OwnerFor(vertex + DirectionVector(direction), direction);
        return new GizmoActionStep(
            name,
            Array.Empty<string>(),
            build.RuntimeType,
            EndToEndGizmoInteraction.Drag,
            Rotation(first.Side),
            new EndToEndBuildMaterial("BlocksGranite"),
            stableGizmoId: build.StableId,
            startCell: new EndToEndMapCell(first.Cell.x, first.Cell.z),
            endCell: new EndToEndMapCell(second.Cell.x, second.Cell.z),
            architectCategoryDefNames: new[] { "Structure" });
    }

    private void AddLayout(string key, IntVec3 offset, IEnumerable<Quadrant> quadrants)
    {
        IntVec3 vertex = center + offset;
        layoutVertices[key] = vertex;
        foreach (Quadrant quadrant in quadrants)
        {
            SpawnRegularWall(QuadrantCell(vertex, quadrant));
        }
    }

    private void SpawnRegularWall(IntVec3 cell)
    {
        var wall = (Building)ThingMaker.MakeThing(
            ThingDefOf.Wall,
            DefDatabase<ThingDef>.GetNamed("BlocksGranite"));
        wall.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(wall, cell, map);
        fixtures.Add(wall);
    }

    private int CaptureWalls()
    {
        thinWalls.Clear();
        thinWalls.AddRange(map.listerThings
            .ThingsOfDef(DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName))
            .OfType<Building_ThinWall>()
            .Where(wall => wall.Position.InHorDistOf(center, 18f)));
        return thinWalls.Count;
    }

    private string[] IdsNear(IEnumerable<IntVec3> points, float radius)
    {
        IntVec3[] centers = points.ToArray();
        return fixtures.Concat<Thing>(thinWalls)
            .Where(thing => centers.Any(point => thing.Position.InHorDistOf(point, radius)))
            .Select(thing => thing.ThingID)
            .Distinct()
            .ToArray();
    }

    private static IntVec3 QuadrantCell(IntVec3 vertex, Quadrant quadrant) => quadrant switch
    {
        Quadrant.SouthWest => vertex + new IntVec3(-1, 0, -1),
        Quadrant.SouthEast => vertex + new IntVec3(0, 0, -1),
        Quadrant.NorthWest => vertex + new IntVec3(-1, 0, 0),
        Quadrant.NorthEast => vertex,
        _ => throw new ArgumentOutOfRangeException(nameof(quadrant)),
    };

    private static OwnedEdge OwnerFor(IntVec3 vertex, ContactDirection direction) => direction switch
    {
        ContactDirection.West => new OwnedEdge(vertex + new IntVec3(-1, 0, -1), ThinWallSide.North),
        ContactDirection.East => new OwnedEdge(vertex + new IntVec3(0, 0, -1), ThinWallSide.North),
        ContactDirection.South => new OwnedEdge(vertex + new IntVec3(-1, 0, -1), ThinWallSide.East),
        ContactDirection.North => new OwnedEdge(vertex + new IntVec3(-1, 0, 0), ThinWallSide.East),
        _ => throw new ArgumentOutOfRangeException(nameof(direction)),
    };

    private static IntVec3 DirectionVector(ContactDirection direction) => direction switch
    {
        ContactDirection.North => IntVec3.North,
        ContactDirection.East => IntVec3.East,
        ContactDirection.South => IntVec3.South,
        ContactDirection.West => IntVec3.West,
        _ => throw new ArgumentOutOfRangeException(nameof(direction)),
    };

    private static bool TouchesVertex(IntVec3 cell, IntVec3 vertex) =>
        cell.x is var x && cell.z is var z &&
        (x == vertex.x - 1 || x == vertex.x) &&
        (z == vertex.z - 1 || z == vertex.z);

    private static bool EdgeTouchesCell(SharedEdge edge, IntVec3 cell)
    {
        IntVec3 first = edge.PositiveSide == ThinWallSide.North
            ? new IntVec3(edge.AnchorCell.x, 0, edge.AnchorCell.z + 1)
            : new IntVec3(edge.AnchorCell.x + 1, 0, edge.AnchorCell.z);
        IntVec3 second = edge.PositiveSide == ThinWallSide.North
            ? first + IntVec3.East
            : first + IntVec3.North;
        return TouchesVertex(cell, first) || TouchesVertex(cell, second);
    }

    private static EndToEndCardinalRotation Rotation(ThinWallSide side) =>
        (EndToEndCardinalRotation)(int)side;

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

                int supportedCells = area.Cells.Count(cell =>
                    ThinWallEndToEndFixtureTerrain.SupportsEveryFixtureBuild(cell, map));
                if (supportedCells > bestSupportedCells)
                {
                    best = candidate;
                    bestSupportedCells = supportedCells;
                }
            }
        }

        return best ?? throw new EndToEndAssertionException(
            "Could not find an in-bounds square-contact visual-evidence area.");
    }

    private void NormalizeBuildTerrain(CellRect area)
    {
        TerrainDef soil = TerrainDefOf.Soil;
        EndToEndAssert.True(
            soil.affordances.Contains(TerrainAffordanceDefOf.Light) &&
            soil.affordances.Contains(TerrainAffordanceDefOf.Medium) &&
            soil.affordances.Contains(TerrainAffordanceDefOf.Heavy),
            "Core soil must support every square-contact fixture.");
        foreach (IntVec3 cell in area.Cells.Where(cell =>
                     !ThinWallEndToEndFixtureTerrain.SupportsEveryFixtureBuild(cell, map)))
        {
            contextTerrains[cell] = cell.GetTerrain(map);
            map.terrainGrid.SetTerrain(cell, soil);
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

    private enum Quadrant
    {
        SouthWest,
        SouthEast,
        NorthWest,
        NorthEast
    }

    private enum ContactDirection
    {
        North,
        East,
        South,
        West
    }
}
