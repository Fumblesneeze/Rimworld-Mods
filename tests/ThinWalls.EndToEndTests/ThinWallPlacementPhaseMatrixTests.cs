using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using ThinWalls.Placement;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest(
    "thin-walls.native-building-crossing-phase-matrix",
    "fumblesneeze.thinwalls",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.thinwalls",
    MaxFrames = 7_200,
    MaxGameTicks = 4_000,
    MaxWallClockSeconds = 210)]
public sealed class ThinWallPlacementPhaseMatrixTest : IRimWorldEndToEndTest
{
    private readonly List<Thing> fixtures = new();
    private readonly List<MatrixCase> cases = new();
    private readonly Dictionary<IntVec3, TerrainDef> contextTerrains = new();
    private readonly List<(Thing Thing, IntVec3 Position, Rot4 Rotation)> displacedThings = new();
    private Map map = null!;
    private IntVec3 center;
    private ThingDef thinWallDef = null!;
    private ThingDef thinDoorDef = null!;
    private ThingDef buildingDef = null!;
    private EndToEndGizmoOption thinWallBuild = null!;
    private EndToEndGizmoOption thinDoorBuild = null!;
    private EndToEndGizmoOption buildingBuild = null!;
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

            foreach (Thing thing in fixtures.ToArray())
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
                    "One or more crossing-matrix fixture cleanup operations failed.",
                    cleanupFailures);
            }
        });

        center = FindClearCenter(map, 20);
        CellRect fixtureArea = CellRect.CenteredOn(center, 20);
        NormalizeBuildTerrain(fixtureArea);
        DisplaceIncidentalThings(fixtureArea);
        thinWallDef = DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName);
        thinDoorDef = DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinDoorDefName);
        buildingDef = DefDatabase<ThingDef>.GetNamed("SimpleResearchBench");

        IEndToEndGizmoCatalog catalog = context.GetRequiredService<IEndToEndGizmoCatalog>();
        thinWallBuild = SingleBuild(catalog, "Structure", thinWallDef, EndToEndGizmoInteraction.Drag);
        thinDoorBuild = SingleBuild(catalog, "Structure", thinDoorDef, EndToEndGizmoInteraction.Drag);
        buildingBuild = SingleBuild(catalog, "Production", buildingDef, EndToEndGizmoInteraction.Place);

        foreach (IntVec3 markerCell in new[]
                 {
                     center + new IntVec3(-19, 0, -19),
                     center + new IntVec3(19, 0, -19),
                     center + new IntVec3(-19, 0, 19),
                     center + new IntVec3(19, 0, 19),
                 })
        {
            Thing marker = ThingMaker.MakeThing(ThingDefOf.WoodLog);
            GenSpawn.Spawn(marker, markerCell, map);
            fixtures.Add(marker);
        }
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        ThinWallSide[] ownerForms =
        {
            ThinWallSide.East,
            ThinWallSide.West,
            ThinWallSide.North,
            ThinWallSide.South,
        };
        ThingDef[] edgeDefs = { thinWallDef, thinDoorDef };
        MatrixPhase[] phases = { MatrixPhase.Completed, MatrixPhase.Blueprint, MatrixPhase.Frame };
        int caseIndex = 0;

        foreach (ThingDef edgeDef in edgeDefs)
        foreach (MatrixPhase phase in phases)
        foreach (ThinWallSide ownerForm in ownerForms)
        {
            int column = caseIndex % 6;
            int row = caseIndex / 6;
            IntVec3 buildingPosition = center + new IntVec3(-15 + column * 6, 0, -15 + row * 8);
            Rot4 buildingRotation = new(caseIndex % 4);
            CellRect footprint = GenAdj.OccupiedRect(buildingPosition, buildingRotation, buildingDef.Size);
            ThinWallSide positiveSide = ownerForm is ThinWallSide.East or ThinWallSide.West
                ? ThinWallSide.East
                : ThinWallSide.North;
            SharedEdge shared = ThinWallPlacementRules.InternalEdges(footprint)
                .First(edge => edge.PositiveSide == positiveSide);
            OwnedEdge owner = ThinWallUtility.Owners(shared).Single(candidate => candidate.Side == ownerForm);
            var matrixCase = new MatrixCase(
                edgeDef,
                phase,
                ownerForm,
                buildingPosition,
                buildingRotation,
                footprint,
                owner);
            cases.Add(matrixCase);

            if (phase == MatrixPhase.Completed)
            {
                DebugSettings.godMode = true;
                yield return BuildEdge(
                    $"place completed {edgeDef.defName} with {ownerForm} owner",
                    edgeDef,
                    owner);
                yield return new WaitUntilStep(
                    $"completed {edgeDef.defName} {ownerForm} owner materializes",
                    _ => FindPhase<Building>(matrixCase) is not null,
                    new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
                Thing completed = FindPhase<Building>(matrixCase)!;
                fixtures.Add(completed);
            }
            else if (phase == MatrixPhase.Blueprint)
            {
                DebugSettings.godMode = false;
                yield return BuildEdge(
                    $"designate {edgeDef.defName} blueprint with {ownerForm} owner",
                    edgeDef,
                    owner);
                yield return new WaitUntilStep(
                    $"{edgeDef.defName} blueprint {ownerForm} owner reserves its edge",
                    _ => FindPhase<Blueprint>(matrixCase) is not null,
                    new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
                Thing blueprint = FindPhase<Blueprint>(matrixCase)!;
                fixtures.Add(blueprint);
            }
            else
            {
                DebugSettings.godMode = true;
                Frame frame = SpawnFrame(edgeDef, owner);
                fixtures.Add(frame);
            }

            yield return PlaceRejectedBuilding(
                $"reject {buildingRotation} 3x2/2x3 building across {phase} {edgeDef.defName} {ownerForm} owner",
                buildingPosition,
                buildingRotation);
            caseIndex++;
        }

        var acceptedControls = new List<(MatrixCase TestCase, Building Building)>();
        ThingDef[] controlEdgeDefs = { thinWallDef, thinDoorDef, thinWallDef, thinDoorDef };
        MatrixPhase[] controlPhases =
        {
            MatrixPhase.Blueprint,
            MatrixPhase.Blueprint,
            MatrixPhase.Frame,
            MatrixPhase.Frame,
        };
        ThinWallSide[] controlSides =
        {
            ThinWallSide.West,
            ThinWallSide.South,
            ThinWallSide.East,
            ThinWallSide.North,
        };
        for (int controlIndex = 0; controlIndex < controlSides.Length; controlIndex++)
        {
            IntVec3 buildingPosition = center + new IntVec3(-12 + controlIndex * 8, 0, 16);
            Rot4 buildingRotation = new(controlIndex);
            CellRect footprint = GenAdj.OccupiedRect(buildingPosition, buildingRotation, buildingDef.Size);
            OwnedEdge owner = ExteriorOwner(footprint, controlSides[controlIndex]);
            ThingDef edgeDef = controlEdgeDefs[controlIndex];
            MatrixPhase phase = controlPhases[controlIndex];
            var control = new MatrixCase(
                edgeDef,
                phase,
                controlSides[controlIndex],
                buildingPosition,
                buildingRotation,
                footprint,
                owner);

            if (phase == MatrixPhase.Blueprint)
            {
                DebugSettings.godMode = false;
                yield return BuildEdge(
                    $"designate exterior {edgeDef.defName} blueprint on {owner.Side} perimeter",
                    edgeDef,
                    owner);
                yield return new WaitUntilStep(
                    $"exterior {edgeDef.defName} blueprint reserves only its perimeter",
                    _ => FindPhase<Blueprint>(control) is not null,
                    new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
                fixtures.Add(FindPhase<Blueprint>(control)!);
            }
            else
            {
                DebugSettings.godMode = true;
                Frame frame = SpawnFrame(edgeDef, owner);
                fixtures.Add(frame);
            }

            DebugSettings.godMode = true;
            yield return PlaceBuilding(
                $"accept {buildingRotation} 3x2/2x3 building beside exterior {phase} {edgeDef.defName}",
                buildingPosition,
                buildingRotation,
                expectRejected: false);
            yield return new WaitUntilStep(
                $"exterior {phase} {edgeDef.defName} control building materializes",
                _ => FindBuilding(buildingDef, buildingPosition) is not null,
                new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
            Building building = FindBuilding(buildingDef, buildingPosition)!;
            fixtures.Add(building);
            acceptedControls.Add((control, building));
        }

        DebugSettings.godMode = true;
        yield return new AssertionStep(
            "every wall and door phase blocks both axes and both owner representations",
            _ =>
            {
                EndToEndAssert.Equal(24, cases.Count,
                    "The native matrix must contain two edge types, three phases, and four owner forms.");
                EndToEndAssert.True(cases.All(testCase =>
                        ThinWallPlacementRules.FootprintCrossesEdge(testCase.Footprint, testCase.Owner.Shared)),
                    "Every arranged edge must be an actual internal adjacency of its rotated footprint.");
                EndToEndAssert.True(cases.All(testCase =>
                        FindBuilding(buildingDef, testCase.BuildingPosition) is null),
                    "No native 3x2/2x3 building may materialize across any Thin Wall/Thin Door phase or owner form.");
                EndToEndAssert.True(cases.All(testCase => FindAnyPhase(testCase) is not null),
                    "Every blocking completed, blueprint, or frame edge must remain present after rejection.");
                EndToEndAssert.Equal(4, acceptedControls.Count,
                    "Blueprint/frame Wall/Door exterior controls must cover both axes.");
                EndToEndAssert.True(acceptedControls.All(control =>
                        control.Building.Spawned &&
                        FindAnyPhase(control.TestCase) is not null &&
                        !ThinWallPlacementRules.FootprintCrossesEdge(
                            control.TestCase.Footprint,
                            control.TestCase.Owner.Shared)),
                    "Every exterior-only blueprint/frame control must preserve both the native building and Thin phase.");
            });
        yield return new CheckpointStep(
            "crossing phase matrix inventory",
            _ => new Dictionary<string, string>
            {
                ["caseCount"] = cases.Count.ToString(),
                ["edgeTypes"] = string.Join(",", cases.Select(testCase => testCase.EdgeDef.defName).Distinct()),
                ["phases"] = string.Join(",", cases.Select(testCase => testCase.Phase).Distinct()),
                ["ownerForms"] = string.Join(",", cases.Select(testCase => testCase.OwnerForm).Distinct()),
                ["rotations"] = string.Join(",", cases.Select(testCase => testCase.BuildingRotation).Distinct()),
                ["acceptedExteriorControls"] = acceptedControls.Count.ToString(),
            });
        yield return new CameraActionStep(
            "frame native crossing phase matrix",
            fixtures.Select(thing => thing.ThingID),
            paddingPixels: 90);
        yield return new ScreenshotStep(
            "all rejected crossing buildings leave their wall and door phases intact",
            fixtures.Select(thing => thing.ThingID),
            paddingPixels: 90);
    }

    private GizmoActionStep BuildEdge(string name, ThingDef edgeDef, OwnedEdge owner)
    {
        EndToEndGizmoOption option = edgeDef == thinWallDef ? thinWallBuild : thinDoorBuild;
        var mapCell = new EndToEndMapCell(owner.Cell.x, owner.Cell.z);
        return new GizmoActionStep(
            name,
            Array.Empty<string>(),
            option.RuntimeType,
            EndToEndGizmoInteraction.Drag,
            Rotation(owner.Side),
            new EndToEndBuildMaterial("Steel"),
            stableGizmoId: option.StableId,
            startCell: mapCell,
            endCell: mapCell,
            architectCategoryDefNames: new[] { "Structure" });
    }

    private GizmoActionStep PlaceRejectedBuilding(string name, IntVec3 position, Rot4 rotation)
    {
        return PlaceBuilding(name, position, rotation, expectRejected: true);
    }

    private GizmoActionStep PlaceBuilding(
        string name,
        IntVec3 position,
        Rot4 rotation,
        bool expectRejected)
    {
        return new GizmoActionStep(
            name,
            Array.Empty<string>(),
            buildingBuild.RuntimeType,
            EndToEndGizmoInteraction.Place,
            Rotation(rotation),
            new EndToEndBuildMaterial("Steel"),
            stableGizmoId: buildingBuild.StableId,
            startCell: new EndToEndMapCell(position.x, position.z),
            architectCategoryDefNames: new[] { "Production" },
            expectRejected: expectRejected);
    }

    private static OwnedEdge ExteriorOwner(CellRect footprint, ThinWallSide side)
    {
        IntVec3 ownerCell = side switch
        {
            ThinWallSide.West => new IntVec3(footprint.minX, 0, footprint.minZ),
            ThinWallSide.East => new IntVec3(footprint.maxX, 0, footprint.minZ),
            ThinWallSide.South => new IntVec3(footprint.minX, 0, footprint.minZ),
            ThinWallSide.North => new IntVec3(footprint.minX, 0, footprint.maxZ),
            _ => throw new ArgumentOutOfRangeException(nameof(side)),
        };
        return new OwnedEdge(ownerCell, side);
    }

    private Frame SpawnFrame(ThingDef edgeDef, OwnedEdge owner)
    {
        var frame = (Frame)ThingMaker.MakeThing(edgeDef.frameDef, ThingDefOf.Steel);
        frame.SetFactionDirect(Faction.OfPlayer);
        var rotation = new Rot4((int)owner.Side);
        GenSpawn.Spawn(frame, owner.Cell, map, rotation);
        EndToEndAssert.Equal(rotation, frame.Rotation,
            "The supporting native Frame fixture must retain the requested Thin-edge owner form.");
        return frame;
    }

    private T? FindPhase<T>(MatrixCase testCase) where T : Thing =>
        testCase.Owner.Cell.GetThingList(map)
            .OfType<T>()
            .SingleOrDefault(thing =>
                thing.def == PhaseDef(testCase) &&
                ThinWallUtility.TryGetOwnedEdge(thing, out OwnedEdge actual) &&
                actual.Equals(testCase.Owner));

    private Thing? FindAnyPhase(MatrixCase testCase) => testCase.Owner.Cell.GetThingList(map)
        .SingleOrDefault(thing =>
            thing.def == PhaseDef(testCase) &&
            ThinWallUtility.TryGetOwnedEdge(thing, out OwnedEdge actual) &&
            actual.Equals(testCase.Owner));

    private static ThingDef PhaseDef(MatrixCase testCase) => testCase.Phase switch
    {
        MatrixPhase.Completed => testCase.EdgeDef,
        MatrixPhase.Blueprint => testCase.EdgeDef.blueprintDef,
        MatrixPhase.Frame => testCase.EdgeDef.frameDef,
        _ => throw new ArgumentOutOfRangeException(),
    };

    private Building? FindBuilding(ThingDef def, IntVec3 position) => map.listerThings.ThingsOfDef(def)
        .OfType<Building>()
        .SingleOrDefault(building => building.Position == position);

    private static EndToEndGizmoOption SingleBuild(
        IEndToEndGizmoCatalog catalog,
        string category,
        ThingDef def,
        EndToEndGizmoInteraction interaction) => catalog.Query(Array.Empty<string>(), new[] { category })
        .Single(option => !option.Disabled &&
                          option.BuildableDefName == def.defName &&
                          option.Interaction == interaction);

    private static EndToEndCardinalRotation Rotation(Rot4 rotation) => rotation.AsInt switch
    {
        0 => EndToEndCardinalRotation.North,
        1 => EndToEndCardinalRotation.East,
        2 => EndToEndCardinalRotation.South,
        3 => EndToEndCardinalRotation.West,
        _ => throw new ArgumentOutOfRangeException(nameof(rotation)),
    };

    private static EndToEndCardinalRotation Rotation(ThinWallSide side) => side switch
    {
        ThinWallSide.North => EndToEndCardinalRotation.North,
        ThinWallSide.East => EndToEndCardinalRotation.East,
        ThinWallSide.South => EndToEndCardinalRotation.South,
        ThinWallSide.West => EndToEndCardinalRotation.West,
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };

    private static IntVec3 FindClearCenter(Map map, int radius)
    {
        IntVec3? best = null;
        int bestSupportedCells = -1;
        for (int x = -60; x <= 60; x += 20)
        for (int z = -60; z <= 60; z += 20)
        {
            IntVec3 candidate = map.Center + new IntVec3(x, 0, z);
            CellRect area = CellRect.CenteredOn(candidate, radius);
            if (!area.InBounds(map))
            {
                continue;
            }

            int supportedCells = area.Cells.Count(cell => SupportsFixtureAffordances(cell.GetTerrain(map)));
            if (supportedCells > bestSupportedCells)
            {
                best = candidate;
                bestSupportedCells = supportedCells;
            }
        }

        return best ?? throw new EndToEndAssertionException(
            "Could not find an in-bounds Thin Walls crossing-matrix area.");
    }

    private void NormalizeBuildTerrain(CellRect area)
    {
        TerrainDef soil = TerrainDefOf.Soil;
        EndToEndAssert.True(SupportsFixtureAffordances(soil),
            "Core soil must support every crossing-matrix fixture.");
        foreach (IntVec3 cell in area.Cells.Where(cell =>
                     !SupportsFixtureAffordances(cell.GetTerrain(map))))
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

    private static bool SupportsFixtureAffordances(TerrainDef terrain) =>
        terrain.affordances.Contains(TerrainAffordanceDefOf.Light) &&
        terrain.affordances.Contains(TerrainAffordanceDefOf.Medium) &&
        terrain.affordances.Contains(TerrainAffordanceDefOf.Heavy);

    private enum MatrixPhase
    {
        Completed,
        Blueprint,
        Frame,
    }

    private sealed class MatrixCase
    {
        public MatrixCase(
            ThingDef edgeDef,
            MatrixPhase phase,
            ThinWallSide ownerForm,
            IntVec3 buildingPosition,
            Rot4 buildingRotation,
            CellRect footprint,
            OwnedEdge owner)
        {
            EdgeDef = edgeDef;
            Phase = phase;
            OwnerForm = ownerForm;
            BuildingPosition = buildingPosition;
            BuildingRotation = buildingRotation;
            Footprint = footprint;
            Owner = owner;
        }

        public ThingDef EdgeDef { get; }
        public MatrixPhase Phase { get; }
        public ThinWallSide OwnerForm { get; }
        public IntVec3 BuildingPosition { get; }
        public Rot4 BuildingRotation { get; }
        public CellRect Footprint { get; }
        public OwnedEdge Owner { get; }
    }
}
