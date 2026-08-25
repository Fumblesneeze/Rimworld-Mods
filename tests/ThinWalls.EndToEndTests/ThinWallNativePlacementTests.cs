using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using ThinWalls.Placement;
using ThinWalls.Rendering;
using UnityEngine;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest(
    "thin-walls.native-building-placement-boundaries",
    "fumblesneeze.thinwalls",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.thinwalls",
    MaxFrames = 3_600,
    MaxGameTicks = 4_000,
    MaxWallClockSeconds = 150)]
public sealed class ThinWallNativePlacementTest : IRimWorldEndToEndTest
{
    private static readonly ThingDef InvisibleWorkbenchCaptureAnchorDef = new()
    {
        defName = "TW_E2E_InvisibleWorkbenchCaptureAnchor",
        label = "invisible workbench capture anchor",
        thingClass = typeof(Thing),
        category = ThingCategory.Item,
        drawerType = DrawerType.None,
        selectable = false,
        useHitPoints = false,
        stackLimit = 1,
    };
    private readonly List<Thing> fixtures = new();
    private readonly List<Thing> workbenchCaptureAnchors = new();
    private readonly Dictionary<IntVec3, TerrainDef> presentationTerrains = new();
    private Map map = null!;
    private IntVec3 center;
    private ThingDef thinWallDef = null!;
    private ThingDef thinDoorDef = null!;
    private ThingDef stoolDef = null!;
    private ThingDef tableDef = null!;
    private ThingDef largeBuildingDef = null!;
    private ThingDef workbenchDef = null!;
    private EndToEndGizmoOption thinWallBuild = null!;
    private EndToEndGizmoOption thinDoorBuild = null!;
    private EndToEndGizmoOption stoolBuild = null!;
    private EndToEndGizmoOption tableBuild = null!;
    private EndToEndGizmoOption workbenchBuild = null!;
    private EndToEndGizmoOption largeBuildingBuild = null!;
    private EndToEndGizmoOption coreWallBuild = null!;
    private bool originalGodMode;
    private bool originalScreenshotMode;
    private bool originalShadowRendering;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        center = FindClearCenter(map, 13);
        thinWallDef = DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName);
        thinDoorDef = DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinDoorDefName);
        stoolDef = DefDatabase<ThingDef>.GetNamed("Stool");
        tableDef = DefDatabase<ThingDef>.GetNamed("Table1x2c");
        workbenchDef = DefDatabase<ThingDef>.GetNamed("HandTailoringBench");
        largeBuildingDef = DefDatabase<ThingDef>.GetNamed("SimpleResearchBench");
        originalGodMode = DebugSettings.godMode;
        originalScreenshotMode = Find.ScreenshotModeHandler.Active;
        originalShadowRendering = DebugViewSettings.drawShadows;
        int originalTicks = Find.TickManager.TicksGame;
        int originalAbsoluteStart = Find.TickManager.gameStartAbsTick;
        WeatherDef originalWeather = map.weatherManager.curWeather;
        WeatherDef originalLastWeather = map.weatherManager.lastWeather;
        int originalWeatherAge = map.weatherManager.curWeatherAge;
        float originalPrevSkyTargetLerp = map.weatherManager.prevSkyTargetLerp;
        float originalCurrSkyTargetLerp = map.weatherManager.currSkyTargetLerp;
        context.DeferCleanup(() =>
        {
            Find.ScreenshotModeHandler.Active = originalScreenshotMode;
            if (DebugViewSettings.drawShadows != originalShadowRendering)
            {
                DebugViewSettings.drawShadows = originalShadowRendering;
                DebugViewSettings.drawShadowsToggled();
            }
            DebugSettings.godMode = originalGodMode;
            Find.TickManager.DebugSetTicksGame(originalTicks);
            Find.TickManager.gameStartAbsTick = originalAbsoluteStart;
            map.weatherManager.curWeather = originalWeather;
            map.weatherManager.lastWeather = originalLastWeather;
            map.weatherManager.curWeatherAge = originalWeatherAge;
            map.weatherManager.prevSkyTargetLerp = originalPrevSkyTargetLerp;
            map.weatherManager.currSkyTargetLerp = originalCurrSkyTargetLerp;
            map.weatherManager.ResetSkyTargetLerpCache();
            foreach (Thing thing in fixtures.ToArray())
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }

            foreach ((IntVec3 cell, TerrainDef terrain) in presentationTerrains)
            {
                map.terrainGrid.SetTerrain(cell, terrain);
            }
        });
        DebugSettings.godMode = true;
        Find.ScreenshotModeHandler.Active = true;
        NormalizeToClearNoon(map);
        NormalizePresentationFloor(CellRect.CenteredOn(center + new IntVec3(5, 0, -5), 4));

        IntVec3 captureNorthBenchPosition = center + new IntVec3(5, 0, -5);
        IntVec3 captureSouthBenchPosition = captureNorthBenchPosition + IntVec3.South;
        CellRect captureNorthRect = GenAdj.OccupiedRect(captureNorthBenchPosition, Rot4.South, workbenchDef.Size);
        CellRect captureSouthRect = GenAdj.OccupiedRect(captureSouthBenchPosition, Rot4.North, workbenchDef.Size);
        foreach (IntVec3 anchorCell in new[]
                 {
                     new IntVec3(Math.Min(captureNorthRect.minX, captureSouthRect.minX), 0, Math.Min(captureNorthRect.minZ, captureSouthRect.minZ)),
                     new IntVec3(Math.Max(captureNorthRect.maxX, captureSouthRect.maxX), 0, Math.Min(captureNorthRect.minZ, captureSouthRect.minZ)),
                     new IntVec3(Math.Min(captureNorthRect.minX, captureSouthRect.minX), 0, Math.Max(captureNorthRect.maxZ, captureSouthRect.maxZ)),
                     new IntVec3(Math.Max(captureNorthRect.maxX, captureSouthRect.maxX), 0, Math.Max(captureNorthRect.maxZ, captureSouthRect.maxZ)),
                 })
        {
            Thing anchor = ThingMaker.MakeThing(InvisibleWorkbenchCaptureAnchorDef);
            GenSpawn.Spawn(anchor, anchorCell, map);
            workbenchCaptureAnchors.Add(anchor);
            fixtures.Add(anchor);
        }

        foreach (IntVec3 markerCell in new[]
                 {
                     center + new IntVec3(-11, 0, -9),
                     center + new IntVec3(11, 0, -9),
                     center + new IntVec3(-11, 0, 9),
                     center + new IntVec3(11, 0, 9),
                 })
        {
            Thing marker = ThingMaker.MakeThing(ThingDefOf.WoodLog);
            GenSpawn.Spawn(marker, markerCell, map);
            fixtures.Add(marker);
        }

        IEndToEndGizmoCatalog catalog = context.GetRequiredService<IEndToEndGizmoCatalog>();
        thinWallBuild = SingleBuild(catalog, "Structure", thinWallDef, EndToEndGizmoInteraction.Drag);
        thinDoorBuild = SingleBuild(catalog, "Structure", thinDoorDef, EndToEndGizmoInteraction.Drag);
        stoolBuild = SingleBuild(catalog, "Furniture", stoolDef, EndToEndGizmoInteraction.Place);
        tableBuild = SingleBuild(catalog, "Furniture", tableDef, EndToEndGizmoInteraction.Place);
        workbenchBuild = SingleBuild(catalog, "Production", workbenchDef, EndToEndGizmoInteraction.Place);
        largeBuildingBuild = SingleBuild(catalog, "Production", largeBuildingDef, EndToEndGizmoInteraction.Place);
        coreWallBuild = SingleBuild(catalog, "Structure", ThingDefOf.Wall, EndToEndGizmoInteraction.Drag);

    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        IntVec3 sameCell = center + new IntVec3(-7, 0, 0);
        yield return new CameraActionStep("frame native placement boundary fixtures",
            fixtures.Select(thing => thing.ThingID), paddingPixels: 100);
        yield return new ScreenshotStep("before native thin-wall and furniture placement",
            fixtures.Select(thing => thing.ThingID), paddingPixels: 100);

        yield return BuildThinWall("place a south-edge thin wall for same-cell furniture", new OwnedEdge(sameCell, ThinWallSide.South));
        yield return WaitForWall("same-cell thin wall materializes", new OwnedEdge(sameCell, ThinWallSide.South));
        Building_ThinWall sameCellWall = FindWall(new OwnedEdge(sameCell, ThinWallSide.South))!;
        fixtures.Add(sameCellWall);
        yield return PlaceBuilding("place a one-cell stool in the thin-wall owner cell", stoolBuild, sameCell,
            EndToEndCardinalRotation.South, expectRejected: false);
        yield return new WaitUntilStep(
            "one-cell stool materializes without replacing its thin wall",
            _ => FindBuilding(stoolDef, sameCell) is not null,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        Building stool = FindBuilding(stoolDef, sameCell)!;
        fixtures.Add(stool);
        IntVec3 oppositeCell = sameCell + IntVec3.South;
        yield return PlaceBuilding(
            "place a second stool on the opposite side of the same thin wall",
            stoolBuild,
            oppositeCell,
            EndToEndCardinalRotation.North,
            expectRejected: false);
        yield return new WaitUntilStep(
            "opposite-side stool materializes without replacing the thin wall",
            _ => FindBuilding(stoolDef, oppositeCell) is not null,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        Building oppositeStool = FindBuilding(stoolDef, oppositeCell)!;
        fixtures.Add(oppositeStool);
        yield return new AssertionStep(
            "both adjacent cells preserve furniture around their shared thin wall",
            _ =>
            {
                EndToEndAssert.True(sameCellWall.Spawned && stool.Spawned && oppositeStool.Spawned,
                    "Native two-sided placement must preserve both buildings and the edge Thing.");
                EndToEndAssert.True(sameCell.Standable(map) && oppositeCell.Standable(map),
                    "Both adjacent cells must remain standable with compatible furniture.");
            });

        IntVec3 northBenchPosition = center + new IntVec3(5, 0, -5);
        IntVec3 southBenchPosition = northBenchPosition + IntVec3.South;
        CellRect northBenchFootprint = GenAdj.OccupiedRect(
            northBenchPosition,
            Rot4.South,
            workbenchDef.Size);
        IntVec3 benchWallStart = northBenchFootprint.Cells.OrderBy(cell => cell.x).First();
        IntVec3 benchWallEnd = northBenchFootprint.Cells.OrderByDescending(cell => cell.x).First();
        string[] workbenchCaptureAnchorIds = workbenchCaptureAnchors.Select(anchor => anchor.ThingID).ToArray();
        yield return new ShadowRenderingActionStep(
            "disable ordinary sun shadows for exact structural measurement stages",
            enabled: false);
        yield return new CameraActionStep(
            "pin the exact two-sided workbench measurement viewport",
            workbenchCaptureAnchorIds,
            paddingPixels: 120);
        yield return new ScreenshotStep(
            "empty same-camera workbench measurement background",
            workbenchCaptureAnchorIds,
            paddingPixels: 120);
        yield return BuildThinWallLine(
            "place a three-cell divider behind two opposing workbench backs",
            benchWallStart,
            benchWallEnd,
            ThinWallSide.South);
        yield return new WaitUntilStep(
            "three-cell workbench divider materializes",
            _ => northBenchFootprint.Cells.All(cell =>
                FindWall(new OwnedEdge(cell, ThinWallSide.South)) is not null),
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        Building_ThinWall[] benchWalls = northBenchFootprint.Cells
            .Select(cell => FindWall(new OwnedEdge(cell, ThinWallSide.South))!)
            .ToArray();
        fixtures.AddRange(benchWalls);
        yield return new ScreenshotStep(
            "same-camera Thin Wall divider without either workbench",
            workbenchCaptureAnchorIds,
            paddingPixels: 120);
        yield return PlaceBuilding(
            "place a hand tailoring bench wholly on the north side of the divider",
            workbenchBuild,
            northBenchPosition,
            EndToEndCardinalRotation.South,
            expectRejected: false,
            architectCategory: "Production");
        yield return new WaitUntilStep(
            "north opposing workbench materializes before the second workbench",
            _ => FindBuilding(workbenchDef, northBenchPosition) is not null,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        Building northWorkbench = FindBuilding(workbenchDef, northBenchPosition)!;
        fixtures.Add(northWorkbench);
        yield return new ScreenshotStep(
            "same-camera north workbench and Thin Wall reference",
            workbenchCaptureAnchorIds,
            paddingPixels: 120);
        yield return PlaceBuilding(
            "place a hand tailoring bench wholly on the south side of the divider",
            workbenchBuild,
            southBenchPosition,
            EndToEndCardinalRotation.North,
            expectRejected: false,
            architectCategory: "Production");
        yield return new WaitUntilStep(
            "both opposing workbenches materialize around the same divider",
            _ => FindBuilding(workbenchDef, northBenchPosition) is not null &&
                 FindBuilding(workbenchDef, southBenchPosition) is not null,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        Building southWorkbench = FindBuilding(workbenchDef, southBenchPosition)!;
        fixtures.Add(southWorkbench);
        yield return new AssertionStep(
            "two multi-cell workbenches occupy opposite sides without crossing or obscuring the Thin Wall",
            _ =>
            {
                EndToEndAssert.True(
                    northWorkbench.Spawned && southWorkbench.Spawned && benchWalls.All(wall => wall.Spawned),
                    "Both workbenches and every segment of their shared divider must remain spawned.");
                EndToEndAssert.True(
                    !northWorkbench.OccupiedRect().Overlaps(southWorkbench.OccupiedRect()),
                    "The two accepted workbench footprints must remain on opposite sides of the divider.");
                EndToEndAssert.True(benchWalls.All(wall =>
                        !ThinWallPlacementRules.FootprintCrossesEdge(
                            northWorkbench.OccupiedRect(), wall.OwnedEdge.Shared) &&
                        !ThinWallPlacementRules.FootprintCrossesEdge(
                            southWorkbench.OccupiedRect(), wall.OwnedEdge.Shared)),
                    "Neither accepted workbench footprint may cross any segment of the divider.");
                Vector3 northLogical = LogicalCenter(northWorkbench);
                Vector3 southLogical = LogicalCenter(southWorkbench);
                EndToEndAssert.True(
                    Math.Abs(northWorkbench.DrawPos.x - northLogical.x) <= 0.0001f &&
                    Math.Abs(northWorkbench.DrawPos.z - (northLogical.z + (27f / 60f))) <= 0.0001f,
                    "The north workbench sprite must move 27/60 cell north from its south perimeter Thin Wall, derived from its deeper 41-pixel wall-facing alpha half plus one safety pixel.");
                EndToEndAssert.True(
                    Math.Abs(southWorkbench.DrawPos.x - southLogical.x) <= 0.0001f &&
                    Math.Abs(southWorkbench.DrawPos.z - (southLogical.z - (19f / 60f))) <= 0.0001f,
                    "The south workbench sprite must move 19/60 cell south from its north perimeter Thin Wall, derived independently from its 32-pixel wall-facing alpha half plus one safety pixel.");
            });
        string[] workbenchIds = benchWalls.Select(wall => wall.ThingID)
            .Concat(new[] { northWorkbench.ThingID, southWorkbench.ThingID })
            .ToArray();
        yield return new SelectionActionStep(
            "clear selection before two-sided workbench visual evidence",
            Array.Empty<string>(),
            additive: false);
        yield return new ScreenshotStep(
            "same-camera completed two-sided workbench structural measurement without shadows",
            workbenchCaptureAnchorIds,
            paddingPixels: 120);
        yield return new ShadowRenderingActionStep(
            "restore ordinary sun shadows for the public workbench render",
            enabled: true);
        yield return new WaitUntilStep(
            "ordinary shadow rendering state is restored before publication capture",
            _ => DebugViewSettings.drawShadows,
            new EndToEndDeadline(60, 60, TimeSpan.FromSeconds(5)));
        yield return new ScreenshotStep(
            "workbenches remain visually intact on both sides of a continuous Thin Wall",
            workbenchCaptureAnchorIds,
            paddingPixels: 120);
        yield return new CameraActionStep(
            "ordinary zoom two-sided workbench divider",
            workbenchIds,
            paddingPixels: 190);
        yield return new ScreenshotStep(
            "ordinary zoom preserves both shifted workbenches and the balanced divider",
            workbenchIds,
            paddingPixels: 190);
        yield return new CameraActionStep(
            "far useful zoom two-sided workbench divider",
            workbenchIds,
            paddingPixels: 270);
        yield return new ScreenshotStep(
            "far zoom keeps both workbenches discernible from the Thin Wall",
            workbenchIds,
            paddingPixels: 270);
        yield return new AssertionStep(
            "cached map-mesh planes use the same bounded offset as realtime building graphics",
            _ =>
            {
                AssertMapMeshPlaneCenter(
                    northWorkbench,
                    LogicalCenter(northWorkbench) +
                    new Vector3(0f, 0f, 27f / 60f));
                AssertMapMeshPlaneCenter(
                    southWorkbench,
                    LogicalCenter(southWorkbench) -
                    new Vector3(0f, 0f, 19f / 60f));
            });

        IntVec3 blueprintBenchPosition = center + new IntVec3(0, 0, -5);
        CellRect blueprintBenchFootprint = GenAdj.OccupiedRect(
            blueprintBenchPosition,
            Rot4.South,
            workbenchDef.Size);
        yield return BuildThinWallLine(
            "place the divider behind a native workbench blueprint",
            blueprintBenchFootprint.Cells.OrderBy(cell => cell.x).First(),
            blueprintBenchFootprint.Cells.OrderByDescending(cell => cell.x).First(),
            ThinWallSide.South);
        yield return new WaitUntilStep(
            "native workbench blueprint divider fully materializes",
            _ => blueprintBenchFootprint.Cells.All(cell =>
                FindWall(new OwnedEdge(cell, ThinWallSide.South)) is not null),
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        Building_ThinWall[] blueprintBenchWalls = blueprintBenchFootprint.Cells
            .Select(cell => FindWall(new OwnedEdge(cell, ThinWallSide.South))!)
            .ToArray();
        fixtures.AddRange(blueprintBenchWalls);
        DebugSettings.godMode = false;
        yield return PlaceBuilding(
            "designate a hand tailoring bench blueprint beside its Thin Wall",
            workbenchBuild,
            blueprintBenchPosition,
            EndToEndCardinalRotation.South,
            expectRejected: false,
            architectCategory: "Production");
        yield return new WaitUntilStep(
            "native workbench blueprint materializes beside its Thin Wall",
            _ => FindWorkbenchBlueprint(blueprintBenchPosition) is not null,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        Blueprint_Build workbenchBlueprint = FindWorkbenchBlueprint(blueprintBenchPosition)!;
        fixtures.Add(workbenchBlueprint);
        yield return new AssertionStep(
            "native workbench blueprint uses the same realtime and cached-mesh displacement",
            _ =>
            {
                Vector3 expected = LogicalCenter(workbenchBlueprint) +
                                   new Vector3(0f, 0f, 27f / 60f);
                EndToEndAssert.True(
                    Vector3.Distance(workbenchBlueprint.DrawPos, expected) <= 0.0001f,
                    "The native workbench blueprint must use the 27/60 direction-specific final-building clearance from its south perimeter Thin Wall.");
                EndToEndAssert.True(blueprintBenchWalls.All(wall => wall.Spawned),
                    "Native workbench blueprint placement must preserve all three perimeter Thin Walls.");
                AssertMapMeshPlaneCenter(workbenchBlueprint, expected);
            });

        IntVec3 frameBenchPosition = center + new IntVec3(-5, 0, -5);
        CellRect frameBenchFootprint = GenAdj.OccupiedRect(
            frameBenchPosition,
            Rot4.South,
            workbenchDef.Size);
        DebugSettings.godMode = true;
        yield return BuildThinWallLine(
            "place the divider behind a workbench frame lifecycle fixture",
            frameBenchFootprint.Cells.OrderBy(cell => cell.x).First(),
            frameBenchFootprint.Cells.OrderByDescending(cell => cell.x).First(),
            ThinWallSide.South);
        yield return new WaitUntilStep(
            "workbench frame divider fully materializes",
            _ => frameBenchFootprint.Cells.All(cell =>
                FindWall(new OwnedEdge(cell, ThinWallSide.South)) is not null),
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        Building_ThinWall[] frameBenchWalls = frameBenchFootprint.Cells
            .Select(cell => FindWall(new OwnedEdge(cell, ThinWallSide.South))!)
            .ToArray();
        fixtures.AddRange(frameBenchWalls);
        DebugSettings.godMode = false;
        yield return PlaceBuilding(
            "designate the workbench that will advance through the native blueprint-to-frame transition",
            workbenchBuild,
            frameBenchPosition,
            EndToEndCardinalRotation.South,
            expectRejected: false,
            architectCategory: "Production");
        yield return new WaitUntilStep(
            "workbench frame lifecycle begins as a native blueprint",
            _ => FindWorkbenchBlueprint(frameBenchPosition) is not null,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        Blueprint_Build frameSourceBlueprint = FindWorkbenchBlueprint(frameBenchPosition)!;
        fixtures.Add(frameSourceBlueprint);
        Pawn? phaseWorker = map.mapPawns.FreeColonistsSpawned.FirstOrDefault();
        if (phaseWorker == null)
        {
            phaseWorker = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            IntVec3 phaseWorkerCell = GenRadial.RadialCellsAround(center, 12f, true)
                .First(cell => cell.InBounds(map) && cell.Standable(map) &&
                               !cell.GetThingList(map).OfType<Pawn>().Any());
            GenSpawn.Spawn(phaseWorker, phaseWorkerCell, map);
            fixtures.Add(phaseWorker);
        }
        EndToEndAssert.True(
            frameSourceBlueprint.TryReplaceWithSolidThing(phaseWorker, out Thing frameThing, out bool jobEnded),
            "The native Blueprint_Build transition must produce the supporting workbench Frame fixture.");
        EndToEndAssert.False(jobEnded,
            "The supporting native blueprint-to-frame transition must not end a worker job as incompletable.");
        var workbenchFrame = (Frame)frameThing;
        fixtures.Remove(frameSourceBlueprint);
        fixtures.Add(workbenchFrame);
        yield return new AssertionStep(
            "native workbench frame keeps the exact realtime displacement without changing its footprint",
            _ =>
            {
                Vector3 expected = LogicalCenter(workbenchFrame) +
                                   new Vector3(0f, 0f, 23f / 60f);
                EndToEndAssert.True(
                    Vector3.Distance(workbenchFrame.DrawPos, expected) <= 0.0001f,
                    "The native workbench Frame must use the 23/60 clearance measured from its actual 1.15x footprint underfield rather than the completed bench sprite.");
                EndToEndAssert.Equal(
                    frameBenchFootprint,
                    workbenchFrame.OccupiedRect(),
                    "The render-only frame offset must not alter the native workbench footprint.");
                EndToEndAssert.True(frameBenchWalls.All(wall => wall.Spawned),
                    "The native workbench blueprint-to-frame transition must preserve all three perimeter Thin Walls.");
            });
        string[] phaseIds = new[] { workbenchBlueprint.ThingID, workbenchFrame.ThingID }
            .Concat(blueprintBenchWalls.Select(wall => wall.ThingID))
            .Concat(frameBenchWalls.Select(wall => wall.ThingID))
            .ToArray();
        yield return new SelectionActionStep(
            "clear selection before blueprint and frame displacement evidence",
            Array.Empty<string>(),
            additive: false);
        yield return new CameraActionStep(
            "frame native workbench blueprint and frame offsets",
            phaseIds,
            paddingPixels: 130);
        yield return new ScreenshotStep(
            "blueprint and frame remain offset from their perimeter Thin Walls",
            phaseIds,
            paddingPixels: 130);
        DebugSettings.godMode = true;

        IntVec3 fullCell = center + new IntVec3(-7, 0, 4);
        var fullCellOwner = new OwnedEdge(fullCell, ThinWallSide.South);
        yield return BuildThinWall("place a south-edge thin wall for full-cell coexistence", fullCellOwner);
        yield return WaitForWall("full-cell coexistence thin wall materializes", fullCellOwner);
        Building_ThinWall fullCellWall = FindWall(fullCellOwner)!;
        fixtures.Add(fullCellWall);
        yield return PlaceBuilding(
            "place a full-cell Core wall in a thin-wall owner cell",
            coreWallBuild,
            fullCell,
            EndToEndCardinalRotation.North,
            expectRejected: false,
            architectCategory: "Structure");
        yield return new WaitUntilStep(
            "full-cell Core wall materializes without replacing its thin wall",
            _ => FindBuilding(ThingDefOf.Wall, fullCell) is not null,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        Building sharedCoreWall = FindBuilding(ThingDefOf.Wall, fullCell)!;
        fixtures.Add(sharedCoreWall);
        yield return new AssertionStep(
            "Core wall material selection remains exact without a rotation",
            _ => EndToEndAssert.Equal(
                ThingDefOf.Steel,
                sharedCoreWall.Stuff,
                "the co-located Core wall's native Stuff"));

        var completedCrossingLocations = new List<IntVec3>();
        for (int rotationIndex = 0; rotationIndex < 4; rotationIndex++)
        {
            var crossingRotation = new Rot4(rotationIndex);
            IntVec3 crossingLocation = center + new IntVec3(-8 + rotationIndex * 5, 0, -9);
            CellRect crossingFootprint = GenAdj.OccupiedRect(
                crossingLocation,
                crossingRotation,
                largeBuildingDef.Size);
            SharedEdge crossingEdge = ThinWallPlacementRules.InternalEdges(crossingFootprint).First();
            OwnedEdge crossingOwner = ThinWallUtility.Owners(crossingEdge)
                .First(owner => crossingFootprint.Contains(owner.Cell));
            yield return BuildThinWall(
                $"place completed {crossingRotation} edge through a future rotated table footprint",
                crossingOwner);
            yield return WaitForWall($"{crossingRotation} crossing edge materializes", crossingOwner);
            Building_ThinWall crossingWall = FindWall(crossingOwner)!;
            fixtures.Add(crossingWall);
            completedCrossingLocations.Add(crossingLocation);
            yield return PlaceBuilding(
                $"reject native {crossingRotation} 3x2/2x3 footprint across its completed Thin Wall",
                largeBuildingBuild,
                crossingLocation,
                Rotation(crossingRotation),
                expectRejected: true,
                architectCategory: "Production");
        }

        var phaseMatrix = new List<(ThingDef BuildingDef, IntVec3 Position)>();

        IntVec3 completedDoorLocation = center + new IntVec3(-9, 0, 10);
        Rot4 completedDoorRotation = Rot4.North;
        OwnedEdge completedDoorOwner = InternalOwner(completedDoorLocation, completedDoorRotation, largeBuildingDef);
        yield return BuildThinDoor("place a completed Thin Door through a future 3x2 footprint", completedDoorOwner);
        yield return new WaitUntilStep(
            "completed crossing Thin Door materializes",
            _ => FindThinWallPhase<Building_ThinDoor>(completedDoorOwner) is not null,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        Building_ThinDoor completedDoor = FindThinWallPhase<Building_ThinDoor>(completedDoorOwner)!;
        fixtures.Add(completedDoor);
        yield return PlaceBuilding(
            "reject a 3x2 building across a completed Thin Door",
            largeBuildingBuild,
            completedDoorLocation,
            Rotation(completedDoorRotation),
            expectRejected: true,
            architectCategory: "Production");
        phaseMatrix.Add((largeBuildingDef, completedDoorLocation));

        IntVec3 doorBlueprintLocation = center + new IntVec3(-3, 0, 10);
        Rot4 doorBlueprintRotation = Rot4.East;
        OwnedEdge doorBlueprintOwner = InternalOwner(doorBlueprintLocation, doorBlueprintRotation, largeBuildingDef);
        DebugSettings.godMode = false;
        yield return BuildThinDoor("designate a Thin Door blueprint through a future 2x3 footprint", doorBlueprintOwner);
        yield return new WaitUntilStep(
            "crossing Thin Door blueprint reserves its edge",
            _ => FindThinWallPhase<Blueprint>(doorBlueprintOwner) is not null,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        Blueprint doorBlueprint = FindThinWallPhase<Blueprint>(doorBlueprintOwner)!;
        fixtures.Add(doorBlueprint);
        yield return PlaceBuilding(
            "reject a 2x3 building across a Thin Door blueprint",
            largeBuildingBuild,
            doorBlueprintLocation,
            Rotation(doorBlueprintRotation),
            expectRejected: true,
            architectCategory: "Production");
        phaseMatrix.Add((largeBuildingDef, doorBlueprintLocation));
        DebugSettings.godMode = true;

        IntVec3 wallFrameLocation = center + new IntVec3(3, 0, 10);
        Rot4 wallFrameRotation = Rot4.South;
        OwnedEdge wallFrameOwner = InternalOwner(wallFrameLocation, wallFrameRotation, largeBuildingDef);
        Frame wallFrame = SpawnFrame(thinWallDef, wallFrameOwner);
        yield return PlaceBuilding(
            "reject a 3x2 building across a native Thin Wall frame",
            largeBuildingBuild,
            wallFrameLocation,
            Rotation(wallFrameRotation),
            expectRejected: true,
            architectCategory: "Production");
        phaseMatrix.Add((largeBuildingDef, wallFrameLocation));

        IntVec3 doorFrameLocation = center + new IntVec3(8, 0, 10);
        Rot4 doorFrameRotation = Rot4.West;
        OwnedEdge doorFrameOwner = InternalOwner(doorFrameLocation, doorFrameRotation, largeBuildingDef);
        Frame doorFrame = SpawnFrame(thinDoorDef, doorFrameOwner);
        yield return PlaceBuilding(
            "reject a 2x3 building across a native Thin Door frame",
            largeBuildingBuild,
            doorFrameLocation,
            Rotation(doorFrameRotation),
            expectRejected: true,
            architectCategory: "Production");
        phaseMatrix.Add((largeBuildingDef, doorFrameLocation));

        IntVec3 doorPerimeterLocation = center + new IntVec3(-9, 0, 6);
        Rot4 doorPerimeterRotation = Rot4.North;
        CellRect doorPerimeterFootprint = GenAdj.OccupiedRect(
            doorPerimeterLocation,
            doorPerimeterRotation,
            largeBuildingDef.Size);
        OwnedEdge doorPerimeterOwner = new(
            new IntVec3(doorPerimeterFootprint.minX, 0, doorPerimeterFootprint.minZ),
            ThinWallSide.West);
        yield return BuildThinDoor("place a Thin Door on the exterior perimeter of a future 3x2 building", doorPerimeterOwner);
        yield return new WaitUntilStep(
            "perimeter Thin Door materializes",
            _ => FindThinWallPhase<Building_ThinDoor>(doorPerimeterOwner) is not null,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        Building_ThinDoor perimeterDoor = FindThinWallPhase<Building_ThinDoor>(doorPerimeterOwner)!;
        fixtures.Add(perimeterDoor);
        yield return PlaceBuilding(
            "accept a 3x2 building wholly on one side of its perimeter Thin Door",
            largeBuildingBuild,
            doorPerimeterLocation,
            Rotation(doorPerimeterRotation),
            expectRejected: false,
            architectCategory: "Production");
        yield return new WaitUntilStep(
            "non-crossing 3x2 perimeter control materializes",
            _ => FindBuilding(largeBuildingDef, doorPerimeterLocation) is not null,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        Building perimeterBuilding = FindBuilding(largeBuildingDef, doorPerimeterLocation)!;
        fixtures.Add(perimeterBuilding);

        IntVec3 openLocation = center + new IntVec3(0, 0, 7);
        Rot4 openRotation = Rot4.North;
        CellRect openFootprint = GenAdj.OccupiedRect(openLocation, openRotation, tableDef.Size);
        IntVec3 openOwnerCell = openFootprint.Cells.First();
        OwnedEdge exteriorOwner = new(openOwnerCell, ThinWallSide.West);
        if (ThinWallPlacementRules.FootprintCrossesEdge(openFootprint, exteriorOwner.Shared))
        {
            exteriorOwner = new OwnedEdge(openOwnerCell, ThinWallSide.South);
        }

        yield return BuildThinWall("place an exterior edge on a future multi-cell table owner", exteriorOwner);
        yield return WaitForWall("exterior table edge materializes", exteriorOwner);
        Building_ThinWall exteriorWall = FindWall(exteriorOwner)!;
        fixtures.Add(exteriorWall);
        yield return PlaceBuilding(
            "place a native multi-cell table wholly on one side of its thin wall",
            tableBuild,
            openLocation,
            Rotation(openRotation),
            expectRejected: false);
        yield return new WaitUntilStep(
            "non-crossing multi-cell table materializes",
            _ => FindBuilding(tableDef, openLocation) is not null,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        Building openTable = FindBuilding(tableDef, openLocation)!;
        fixtures.Add(openTable);

        SharedEdge tableInternalEdge = ThinWallPlacementRules.InternalEdges(openTable.OccupiedRect()).First();
        OwnedEdge tableCrossingOwner = ThinWallUtility.Owners(tableInternalEdge)
            .First(owner => openTable.OccupiedRect().Contains(owner.Cell));
        yield return BuildThinWall(
            "reject a native thin wall through an existing table footprint",
            tableCrossingOwner,
            expectRejected: true);

        IntVec3 plannedLocation = center + new IntVec3(7, 0, 0);
        CellRect plannedFootprint = GenAdj.OccupiedRect(plannedLocation, Rot4.North, tableDef.Size);
        SharedEdge plannedEdge = ThinWallPlacementRules.InternalEdges(plannedFootprint).First();
        OwnedEdge plannedOwner = ThinWallUtility.Owners(plannedEdge)
            .First(owner => plannedFootprint.Contains(owner.Cell));
        DebugSettings.godMode = false;
        yield return BuildThinWall("designate an unfinished edge through a future table footprint", plannedOwner);
        yield return new WaitUntilStep(
            "planned thin-wall blueprint reserves its edge",
            _ => FindThinWallPhase<Blueprint>(plannedOwner) is not null,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        Blueprint plannedBlueprint = FindThinWallPhase<Blueprint>(plannedOwner)!;
        fixtures.Add(plannedBlueprint);
        yield return PlaceBuilding(
            "reject a native table footprint that crosses a thin-wall blueprint",
            tableBuild,
            plannedLocation,
            EndToEndCardinalRotation.North,
            expectRejected: true);
        DebugSettings.godMode = true;

        yield return new SelectionActionStep(
            "select accepted co-located and non-crossing placement results",
            new[]
            {
                sameCellWall.ThingID,
                stool.ThingID,
                oppositeStool.ThingID,
                fullCellWall.ThingID,
                sharedCoreWall.ThingID,
                exteriorWall.ThingID,
                openTable.ThingID,
            },
            additive: false);
        yield return new CameraActionStep(
            "frame native placement acceptance and rejection catalog",
            fixtures.Select(thing => thing.ThingID),
            paddingPixels: 130);
        yield return new ScreenshotStep(
            "after same-cell and multi-cell native placement boundaries",
            fixtures.Select(thing => thing.ThingID),
            paddingPixels: 130);
        yield return new AssertionStep(
            "native placement accepts only footprints that do not cross reserved edges",
            _ =>
            {
                EndToEndAssert.True(completedCrossingLocations.All(location =>
                        FindBuilding(largeBuildingDef, location) is null),
                    "Every rotated completed crossing edge must leave no rejected 3x2/2x3 building.");
                EndToEndAssert.True(phaseMatrix.All(testCase =>
                        FindBuilding(testCase.BuildingDef, testCase.Position) is null),
                    "Completed, blueprint, and frame Thin Wall/Thin Door crossings must all reject their native building.");
                EndToEndAssert.True(FindBuilding(tableDef, plannedLocation) is null,
                    "The planned crossing edge must leave no rejected table.");
                EndToEndAssert.True(FindWall(tableCrossingOwner) is null,
                    "The existing table footprint must leave no rejected Thin Wall.");
                EndToEndAssert.True(openTable.Spawned && exteriorWall.Spawned,
                    "The non-crossing multi-cell table and owner edge must coexist.");
                EndToEndAssert.True(fullCellWall.Spawned && sharedCoreWall.Spawned,
                    "A full-cell Core wall and its Thin Wall edge must coexist without wiping either Thing.");
                EndToEndAssert.True(perimeterDoor.Spawned && perimeterBuilding.Spawned,
                    "An exterior perimeter Thin Door must remain legal beside a non-crossing 3x2 building.");
            });
    }

    private static Vector3 LogicalCenter(Thing thing)
    {
        CellRect occupied = thing.OccupiedRect();
        return new Vector3(
            (occupied.minX + occupied.maxX + 1) * 0.5f,
            thing.def.altitudeLayer.AltitudeFor(),
            (occupied.minZ + occupied.maxZ + 1) * 0.5f);
    }

    private void AssertMapMeshPlaneCenter(Thing thing, Vector3 expectedCenter)
    {
        Section section = map.mapDrawer.SectionAt(thing.Position);
        var layer = (SectionLayer_ThingsGeneral)section.GetLayer(typeof(SectionLayer_ThingsGeneral));
        section.RegenerateSingleLayer(layer);
        var vertexStarts = layer.subMeshes.ToDictionary(subMesh => subMesh, subMesh => subMesh.verts.Count);
        try
        {
            System.Reflection.MethodInfo? takePrintFrom = typeof(SectionLayer_ThingsGeneral).GetMethod(
                "TakePrintFrom",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            EndToEndAssert.True(takePrintFrom != null,
                "The live SectionLayer_ThingsGeneral must expose its protected native TakePrintFrom path.");
            takePrintFrom!.Invoke(layer, new object[] { thing });

            var graphicCenters = new List<Vector3>();
            var shadowCenters = new List<Vector3>();
            foreach (LayerSubMesh subMesh in layer.subMeshes)
            {
                int firstAdded = vertexStarts.TryGetValue(subMesh, out int existingCount)
                    ? existingCount
                    : 0;
                if (subMesh.verts.Count == firstAdded)
                {
                    continue;
                }
                if (ReferenceEquals(subMesh.material, MatBases.SunShadowFade))
                {
                    EndToEndAssert.Equal(
                        10,
                        subMesh.verts.Count - firstAdded,
                        $"Printing only {thing.LabelCap} must append exactly one native ten-vertex shadow mesh.");
                    shadowCenters.Add((subMesh.verts[firstAdded] + subMesh.verts[firstAdded + 1] +
                                       subMesh.verts[firstAdded + 2] + subMesh.verts[firstAdded + 3]) / 4f);
                }
                else
                {
                    for (int index = firstAdded; index + 3 < subMesh.verts.Count; index += 4)
                    {
                        graphicCenters.Add((subMesh.verts[index] + subMesh.verts[index + 1] +
                                            subMesh.verts[index + 2] + subMesh.verts[index + 3]) / 4f);
                    }
                }
            }

            EndToEndAssert.True(
                graphicCenters.Any(center => Math.Abs(center.x - expectedCenter.x) <= 0.0001f &&
                                             Math.Abs(center.z - expectedCenter.z) <= 0.0001f),
                $"The exact native print for {thing.LabelCap} must append a graphic plane centered at " +
                $"({expectedCenter.x:F4},{expectedCenter.z:F4}); observed " +
                string.Join(",", graphicCenters.Select(center => $"({center.x:F4},{center.z:F4})")));

            Vector3 logical = LogicalCenter(thing);
            EndToEndAssert.False(
                graphicCenters.Any(center => Math.Abs(center.x - logical.x) <= 0.0001f &&
                                             Math.Abs(center.z - logical.z) <= 0.0001f),
                $"The exact native print for {thing.LabelCap} must not append an unshifted main plane.");

            ShadowData? shadow = thing.Graphic.data?.shadowData;
            if (shadow != null)
            {
                Vector3 expectedShadowCenter = expectedCenter + shadow.offset.RotatedBy(thing.Rotation);
                Vector3 unshiftedShadowCenter = logical + shadow.offset.RotatedBy(thing.Rotation);
                EndToEndAssert.True(
                    shadowCenters.Any(center =>
                        Math.Abs(center.x - expectedShadowCenter.x) <= 0.0001f &&
                        Math.Abs(center.z - expectedShadowCenter.z) <= 0.0001f),
                    $"The exact native shadow print for {thing.LabelCap} must move with its graphic to " +
                    $"({expectedShadowCenter.x:F4},{expectedShadowCenter.z:F4}); observed " +
                    string.Join(",", shadowCenters.Select(center => $"({center.x:F4},{center.z:F4})")));
                EndToEndAssert.False(
                    shadowCenters.Any(center =>
                        Math.Abs(center.x - unshiftedShadowCenter.x) <= 0.0001f &&
                        Math.Abs(center.z - unshiftedShadowCenter.z) <= 0.0001f),
                    $"The exact native shadow print for {thing.LabelCap} must not retain its unshifted center.");
            }
        }
        finally
        {
            section.RegenerateSingleLayer(layer);
        }
    }

    private Blueprint_Build? FindWorkbenchBlueprint(IntVec3 position) =>
        map.listerThings.ThingsOfDef(workbenchDef.blueprintDef)
            .OfType<Blueprint_Build>()
            .SingleOrDefault(blueprint => blueprint.Position == position);

    private OwnedEdge InternalOwner(IntVec3 position, Rot4 rotation, ThingDef buildingDef)
    {
        CellRect footprint = GenAdj.OccupiedRect(position, rotation, buildingDef.Size);
        SharedEdge edge = ThinWallPlacementRules.InternalEdges(footprint).First();
        return ThinWallUtility.Owners(edge).First(owner => footprint.Contains(owner.Cell));
    }

    private Frame SpawnFrame(ThingDef edgeDef, OwnedEdge owner)
    {
        var frame = (Frame)ThingMaker.MakeThing(edgeDef.frameDef, ThingDefOf.Steel);
        frame.SetFactionDirect(Faction.OfPlayer);
        Rot4 rotation = new((int)owner.Side);
        GenSpawn.Spawn(frame, owner.Cell, map, rotation);
        EndToEndAssert.Equal(
            rotation.AsInt,
            frame.Rotation.AsInt,
            "Native frame spawn must retain the requested Thin edge rotation.");
        fixtures.Add(frame);
        return frame;
    }

    private GizmoActionStep BuildThinWall(string name, OwnedEdge edge, bool expectRejected = false) => new(
        name,
        Array.Empty<string>(),
        thinWallBuild.RuntimeType,
        EndToEndGizmoInteraction.Drag,
        Rotation(edge.Side),
        new EndToEndBuildMaterial("Steel"),
        stableGizmoId: thinWallBuild.StableId,
        startCell: new EndToEndMapCell(edge.Cell.x, edge.Cell.z),
        endCell: new EndToEndMapCell(edge.Cell.x, edge.Cell.z),
        architectCategoryDefNames: new[] { "Structure" },
        expectRejected: expectRejected);

    private GizmoActionStep BuildThinDoor(string name, OwnedEdge edge, bool expectRejected = false) => new(
        name,
        Array.Empty<string>(),
        thinDoorBuild.RuntimeType,
        EndToEndGizmoInteraction.Drag,
        Rotation(edge.Side),
        new EndToEndBuildMaterial("Steel"),
        stableGizmoId: thinDoorBuild.StableId,
        startCell: new EndToEndMapCell(edge.Cell.x, edge.Cell.z),
        endCell: new EndToEndMapCell(edge.Cell.x, edge.Cell.z),
        architectCategoryDefNames: new[] { "Structure" },
        expectRejected: expectRejected);

    private GizmoActionStep BuildThinWallLine(
        string name,
        IntVec3 start,
        IntVec3 end,
        ThinWallSide side) => new(
        name,
        Array.Empty<string>(),
        thinWallBuild.RuntimeType,
        EndToEndGizmoInteraction.Drag,
        Rotation(side),
        new EndToEndBuildMaterial("Steel"),
        stableGizmoId: thinWallBuild.StableId,
        startCell: new EndToEndMapCell(start.x, start.z),
        endCell: new EndToEndMapCell(end.x, end.z),
        architectCategoryDefNames: new[] { "Structure" });

    private GizmoActionStep PlaceBuilding(
        string name,
        EndToEndGizmoOption option,
        IntVec3 cell,
        EndToEndCardinalRotation rotation,
        bool expectRejected,
        string architectCategory = "Furniture")
    {
        var mapCell = new EndToEndMapCell(cell.x, cell.z);
        if (option.Interaction == EndToEndGizmoInteraction.Drag)
        {
            return new GizmoActionStep(
                name,
                Array.Empty<string>(),
                option.RuntimeType,
                EndToEndGizmoInteraction.Drag,
                new EndToEndBuildMaterial("Steel"),
                stableGizmoId: option.StableId,
                startCell: mapCell,
                endCell: mapCell,
                architectCategoryDefNames: new[] { architectCategory },
                expectRejected: expectRejected);
        }

        return new GizmoActionStep(
            name,
            Array.Empty<string>(),
            option.RuntimeType,
            EndToEndGizmoInteraction.Place,
            rotation,
            new EndToEndBuildMaterial("Steel"),
            stableGizmoId: option.StableId,
            startCell: mapCell,
            architectCategoryDefNames: new[] { architectCategory },
            expectRejected: expectRejected);
    }

    private WaitUntilStep WaitForWall(string name, OwnedEdge edge) => new(
        name,
        _ => FindWall(edge) is not null,
        new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));

    private Building_ThinWall? FindWall(OwnedEdge edge) => edge.Cell.GetThingList(map)
        .OfType<Building_ThinWall>()
        .SingleOrDefault(wall => wall.OwnedEdge.Equals(edge));

    private T? FindThinWallPhase<T>(OwnedEdge edge) where T : Thing => edge.Cell.GetThingList(map)
        .OfType<T>()
        .SingleOrDefault(thing => ThinWallUtility.TryGetOwnedEdge(thing, out OwnedEdge actual) && actual.Equals(edge));

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

    private void NormalizePresentationFloor(CellRect area)
    {
        foreach (IntVec3 cell in area.Cells)
        {
            if (!cell.InBounds(map))
            {
                continue;
            }

            if (!presentationTerrains.ContainsKey(cell))
            {
                presentationTerrains.Add(cell, cell.GetTerrain(map));
            }
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.WoodPlankFloor);
        }
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
        for (int x = -60; x <= 60; x += 24)
        {
            for (int z = -60; z <= 60; z += 24)
            {
                IntVec3 candidate = map.Center + new IntVec3(x, 0, z);
                CellRect area = CellRect.CenteredOn(candidate, radius);
                if (area.InBounds(map) && area.Cells.All(cell =>
                        cell.Standable(map) &&
                        cell.GetThingList(map).Count == 0 &&
                        SupportsFixtureAffordances(cell.GetTerrain(map))))
                {
                    return candidate;
                }
            }
        }

        throw new EndToEndAssertionException("Could not find a clear Thin Walls native-placement area.");
    }

    private static bool SupportsFixtureAffordances(TerrainDef terrain) =>
        terrain.affordances.Contains(TerrainAffordanceDefOf.Light) &&
        terrain.affordances.Contains(TerrainAffordanceDefOf.Medium) &&
        terrain.affordances.Contains(TerrainAffordanceDefOf.Heavy);
}
