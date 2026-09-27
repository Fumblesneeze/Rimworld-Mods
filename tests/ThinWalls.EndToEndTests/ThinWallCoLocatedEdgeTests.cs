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

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest("thin-walls.native-colocated-blueprints-and-shelf",
    "fumblesneeze.thinwalls", "brrainz.harmony", EndToEndTestContract.CorePackageId,
    "fumblesneeze.thinwalls", MaxFrames = 9000, MaxGameTicks = 5000, MaxWallClockSeconds = 240)]
public sealed class ThinWallCoLocatedEdgeTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private IntVec3 center;
    private readonly List<Thing> markers = new();
    private readonly List<Thing> created = new();
    private EndToEndGizmoOption wallBuild = null!;
    private EndToEndGizmoOption doorBuild = null!;
    private EndToEndGizmoOption shelfBuild = null!;

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap;
        center = map.Center;
        for (int radius = 0; radius < 60; radius++)
        {
            IntVec3 candidate = map.Center + new IntVec3(radius, 0, 0);
            if (CellRect.CenteredOn(candidate, 9).Cells.All(c => c.InBounds(map) && !c.Fogged(map) &&
                c.Standable(map) && !c.Roofed(map) && c.GetEdifice(map) == null))
            { center = candidate; break; }
        }
        bool originalGod = DebugSettings.godMode;
        DebugSettings.godMode = false;
        context.DeferCleanup(() => DebugSettings.godMode = originalGod);
        int originalTicks = Find.TickManager.TicksGame;
        int originalStart = Find.TickManager.gameStartAbsTick;
        WeatherDef originalWeather = map.weatherManager.curWeather;
        WeatherDef originalLast = map.weatherManager.lastWeather;
        int originalAge = map.weatherManager.curWeatherAge;
        float previousLerp = map.weatherManager.prevSkyTargetLerp;
        float currentLerp = map.weatherManager.currSkyTargetLerp;
        ThinWallConstructionLifecycleTest.NormalizeToClearNoon(map);
        context.DeferCleanup(() =>
        {
            Find.TickManager.DebugSetTicksGame(originalTicks);
            Find.TickManager.gameStartAbsTick = originalStart;
            map.weatherManager.curWeather = originalWeather;
            map.weatherManager.lastWeather = originalLast;
            map.weatherManager.curWeatherAge = originalAge;
            map.weatherManager.prevSkyTargetLerp = previousLerp;
            map.weatherManager.currSkyTargetLerp = currentLerp;
            map.weatherManager.ResetSkyTargetLerpCache();
        });
        var originalTerrains = new Dictionary<IntVec3, TerrainDef>();
        foreach (IntVec3 cell in CellRect.CenteredOn(center, 9))
        {
            originalTerrains[cell] = cell.GetTerrain(map);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
        }
        context.DeferCleanup(() =>
        {
            foreach (Thing thing in created.Concat(markers).Distinct())
                if (thing.Spawned && !thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
            foreach (var pair in originalTerrains) map.terrainGrid.SetTerrain(pair.Key, pair.Value);
        });
        foreach (int x in new[] { -8, 8 })
        foreach (int z in new[] { -7, 7 })
        {
            Thing marker = ThingMaker.MakeThing(ThingDefOf.WoodLog);
            marker.stackCount = 1;
            GenSpawn.Spawn(marker, center + new IntVec3(x, 0, z), map);
            markers.Add(marker);
        }
        var catalog = context.GetRequiredService<IEndToEndGizmoCatalog>();
        var structure = catalog.Query(Array.Empty<string>(), new[] { "Structure" });
        wallBuild = structure.Single(x => x.BuildableDefName == "TW_ThinWall" && !x.Disabled);
        doorBuild = structure.Single(x => x.BuildableDefName == "TW_ThinDoor" && !x.Disabled);
        shelfBuild = catalog.Query(Array.Empty<string>(), new[] { "Furniture" })
            .Single(x => x.BuildableDefName == "ShelfSmall" && !x.Disabled);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause for native edge placement", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep("frame empty edge fixture", markers.Select(x => x.ThingID), 40);
        yield return Shot("before any clicks");
        string[] materials = { "BlocksGranite", "WoodLog", "Plasteel", "Uranium" };
        for (int i = 0; i < 4; i++)
        {
            IntVec3 cell = center + new IntVec3(-6 + i * 4, 0, 4);
            yield return Begin(wallBuild, cell, materials[i]);
            for (int turn = 0; turn < i; turn++)
                yield return DesignatorSessionActionStep.RotateRight("choose U starting side with E");
            yield return Shot($"hover before first click orientation {i}");
            var prior = new List<(Thing Thing, Rot4 Rotation)>();
            for (int edge = 0; edge < 3; edge++)
            {
                if (edge > 0) yield return DesignatorSessionActionStep.RotateLeft("Q while retaining the same designator");
                yield return DesignatorSessionActionStep.Commit("click another edge of the SAME cell", keepActive: true);
                int count = edge + 1;
                yield return new AssertionStep("all previous blueprint identities and rotations survive", _ =>
                {
                    foreach (var old in prior)
                    {
                        EndToEndAssert.True(old.Thing.Spawned && !old.Thing.Destroyed, "An earlier blueprint was replaced.");
                        EndToEndAssert.Equal(old.Rotation, old.Thing.Rotation, "An earlier blueprint rotated.");
                    }
                    Thing[] current = cell.GetThingList(map).Where(x => x is Blueprint_ThinWall).ToArray();
                    EndToEndAssert.Equal(count, current.Length, "Each different edge needs its own blueprint.");
                    Thing latest = current.Single(x => prior.All(old => old.Thing != x));
                    prior.Add((latest, latest.Rotation));
                    created.Add(latest);
                });
                yield return Shot($"after click {count} orientation {i}");
            }
            yield return DesignatorSessionActionStep.Commit("duplicate edge rejected without deleting any blueprint",
                expectRejected: true, keepActive: true);
            yield return DesignatorSessionActionStep.Cancel("leave completed three-edge blueprint U");
            foreach (var bp in prior)
            {
                yield return new SelectionActionStep("select each co-located edge independently", new[] { bp.Thing.ThingID }, false);
                yield return new AssertionStep("native brackets use the shared edge center", _ => AssertBrackets(bp.Thing));
                yield return Shot($"edge selection {i} {bp.Rotation}");
            }
            yield return new SelectionActionStep("clear edge selection", Array.Empty<string>(), false);
            yield return Place(shelfBuild, cell, "WoodLog", i);
            yield return new AssertionStep("small shelf blueprint fits the logical U cell", _ =>
            {
                Thing shelf = cell.GetThingList(map).Single(x => x.def == ThingDefOf.ShelfSmall.blueprintDef);
                created.Add(shelf);
                EndToEndAssert.Equal(3, prior.Count(x => x.Thing.Spawned), "Shelf must not wipe edge blueprints.");
            });
        }
        yield return Shot("four blueprint Us with small shelf blueprints");

        // Explicit developer-mode precondition for a separate native instant-build visual specimen.
        // Every wall and shelf below is still produced by its architect action, not direct spawn.
        DebugSettings.godMode = true;
        for (int i = 0; i < 4; i++)
        {
            IntVec3 cell = center + new IntVec3(-6 + i * 4, 0, -2);
            yield return Begin(wallBuild, cell, materials[i]);
            for (int turn = 0; turn < i; turn++)
                yield return DesignatorSessionActionStep.RotateRight("rotate completed U start");
            for (int edge = 0; edge < 3; edge++)
            {
                if (edge > 0) yield return DesignatorSessionActionStep.RotateLeft("rotate to next completed U edge");
                yield return DesignatorSessionActionStep.Commit("native instant-build U edge", keepActive: true);
            }
            yield return DesignatorSessionActionStep.Cancel("finish U wall input");
            created.AddRange(cell.GetThingList(map).Where(x => x is Building_ThinWall));
            yield return Shot($"completed U before shelf {i}");
            yield return Place(shelfBuild, cell, "WoodLog", i);
            Building shelf = cell.GetThingList(map).OfType<Building>().Single(x => x.def == ThingDefOf.ShelfSmall);
            created.Add(shelf);
            yield return new AssertionStep("shelf and all three walls retain stable logical and visual coordinates", _ =>
            {
                EndToEndAssert.Equal(3, cell.GetThingList(map).OfType<Building_ThinWall>().Count(), "Shelf wiped U walls.");
                var occupiedSides = cell.GetThingList(map).OfType<Building_ThinWall>().Select(wall => (int)wall.OwnedSide).ToArray();
                int openSide = Enumerable.Range(0, 4).Single(side => !occupiedSides.Contains(side));
                EndToEndAssert.Equal(1 + 2 * openSide, BuildingAppearanceControls.Get(shelf).OffsetStep,
                    "Native shelf placement must assign the gizmo preset toward the U opening.");
                EndToEndAssert.False(BuildingAppearanceControls.Get(shelf).OffsetIsManual, "Placement must retain automatic mode.");
                Vector3 before = shelf.DrawPos;
                map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Things);
                for (int draw = 0; draw < 10; draw++) EndToEndAssert.Equal(before, shelf.DrawPos, "Offset must not drift.");
                EndToEndAssert.Equal(cell, shelf.Position, "Visual clearance must not relocate logical cells.");
            });
            yield return new CameraActionStep("close view of shelf inside U", new[] { shelf.ThingID }, 100);
            yield return Shot($"small shelf inside completed U orientation {i}");
            yield return new CheckpointStep("U shelf projected dimensions and displacement", _ =>
                new Dictionary<string, string>
                {
                    ["position"] = shelf.Position.ToString(), ["rotation"] = shelf.Rotation.ToString(),
                    ["drawPosition"] = shelf.DrawPos.ToString("F5"),
                    ["drawSize"] = shelf.Graphic.drawSize.ToString("F5"),
                    ["texture"] = shelf.Graphic.MatAt(shelf.Rotation, shelf).mainTexture.name,
                    ["structuralHalfWidth"] = (17f / 60f).ToString("R"),
                    ["appearanceOffset"] = BuildingAppearanceControls.Get(shelf).OffsetStep.ToString(),
                });
        }
        yield return new CameraActionStep("ordinary zoom U catalog", markers.Select(x => x.ThingID), 40);
        yield return Shot("all completed and planned Us ordinary zoom");
        yield return new CameraActionStep("farther zoom U catalog", markers.Select(x => x.ThingID), 220);
        yield return Shot("all completed and planned Us farther zoom");

        DebugSettings.godMode = false;
        IntVec3 doorCell = center + new IntVec3(0, 0, -6);
        yield return Begin(doorBuild, doorCell, "Gold");
        yield return DesignatorSessionActionStep.Commit("first thin door blueprint", keepActive: true);
        yield return DesignatorSessionActionStep.RotateLeft("rotate door preview");
        yield return DesignatorSessionActionStep.Commit("second thin door edge", keepActive: true);
        yield return DesignatorSessionActionStep.Cancel("finish thin door L input");
        created.AddRange(doorCell.GetThingList(map).OfType<Blueprint_ThinDoor>());
        yield return new AssertionStep("two distinct door blueprints coexist", _ =>
            EndToEndAssert.Equal(2, doorCell.GetThingList(map).OfType<Blueprint_ThinDoor>().Count(), "Door blueprint replacement."));
        yield return new CameraActionStep("close thin door blueprints", created.Where(x => x.Position == doorCell).Select(x => x.ThingID), 100);
        yield return Shot("two thin door blueprints edge-rendered");
        foreach (var step in ThinWallNativeLogEvidence.Capture(context)) yield return step;
    }

    private DesignatorSessionActionStep Begin(EndToEndGizmoOption build, IntVec3 cell, string stuff) =>
        DesignatorSessionActionStep.Begin("begin native wall/door hover", Array.Empty<string>(),
            build.RuntimeType, build.StableId, new[] { "Structure" },
            new EndToEndMapCell(cell.x, cell.z), new EndToEndBuildMaterial(stuff));

    private GizmoActionStep Place(EndToEndGizmoOption build, IntVec3 cell, string stuff, int rotation) => new(
        "place small shelf through native Furniture tool", Array.Empty<string>(), build.RuntimeType,
        build.Interaction!.Value, (EndToEndCardinalRotation)rotation, new EndToEndBuildMaterial(stuff),
        stableGizmoId: build.StableId, startCell: new EndToEndMapCell(cell.x, cell.z),
        endCell: new EndToEndMapCell(cell.x, cell.z), architectCategoryDefNames: new[] { "Furniture" });

    private ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);

    private static void AssertBrackets(Thing thing)
    {
        Vector3[] brackets = new Vector3[4];
        SelectionDrawerUtility.CalculateSelectionBracketPositionsWorld<object>(brackets, thing,
            thing.DrawPos, Vector2.one, new Dictionary<object, float>(), Vector2.zero,
            jumpDistanceFactor: 0f, deselectedJumpFactor: 0f);
        Vector3 average = brackets.Aggregate(Vector3.zero, (sum, next) => sum + next) / 4f;
        ThinWallUtility.TryGetOwnedEdge(thing, out OwnedEdge edge);
        Vector3 expected = ThinWallRenderGeometry.StructuralCenter(edge, average.y);
        EndToEndAssert.True((average - expected).sqrMagnitude < 0.00001f, "Selection center does not coincide with shared edge.");
        float width = brackets.Max(p => p.x) - brackets.Min(p => p.x);
        float depth = brackets.Max(p => p.z) - brackets.Min(p => p.z);
        bool horizontal = edge.Shared.PositiveSide == ThinWallSide.North;
        EndToEndAssert.True(Math.Abs(width - (horizontal ? 1f : 34f / 60f)) < .0001f &&
            Math.Abs(depth - (horizontal ? 34f / 60f : 1f)) < .0001f, "Selection envelope dimensions differ from the edge mesh.");
    }
}
