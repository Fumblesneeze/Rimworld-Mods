using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using UnityEngine;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest(
    "thin-walls.native-directional-visual-catalog",
    "fumblesneeze.thinwalls",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.thinwalls",
    MaxFrames = 3_600,
    MaxGameTicks = 5_000,
    MaxWallClockSeconds = 150)]
public sealed class ThinWallNativeDesignatorVisualTest : IRimWorldEndToEndTest
{
    private readonly List<Thing> fixtures = new();
    private readonly List<Building_ThinWall> walls = new();
    private Map map = null!;
    private IntVec3 center;
    private EndToEndGizmoOption build = null!;
    private Building regularWall = null!;
    private Dictionary<string, string> baselineCamera = null!;
    private Dictionary<string, string> southPreviewCamera = null!;
    private Dictionary<string, string> eastPreviewCamera = null!;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        center = FindClearCenter(map, 9);
        bool originalGodMode = DebugSettings.godMode;
        DebugSettings.godMode = true;
        context.DeferCleanup(() => DebugSettings.godMode = originalGodMode);
        context.DeferCleanup(() =>
        {
            foreach (Thing thing in fixtures.Concat<Thing>(walls).ToArray())
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        });

        foreach (IntVec3 cell in new[]
                 {
                     center + new IntVec3(-7, 0, -7),
                     center + new IntVec3(7, 0, -7),
                     center + new IntVec3(-7, 0, 7),
                     center + new IntVec3(7, 0, 7),
                 })
        {
            Thing marker = ThingMaker.MakeThing(ThingDefOf.WoodLog);
            marker.stackCount = 1;
            GenSpawn.Spawn(marker, cell, map);
            fixtures.Add(marker);
        }

        regularWall = (Building)ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
        regularWall.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(regularWall, center + new IntVec3(4, 0, 0), map);
        fixtures.Add(regularWall);

        EndToEndGizmoOption[] matches = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(Array.Empty<string>(), new[] { "Structure" })
            .Where(option => !option.Disabled &&
                             option.BuildableDefName == ThinWallUtility.ThinWallDefName &&
                             option.Interaction == EndToEndGizmoInteraction.Drag)
            .ToArray();
        EndToEndAssert.Equal(1, matches.Length,
            "Structure must expose exactly one enabled native Thin Walls drag designator");
        build = matches[0];
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CameraActionStep(
            "frame empty thin-wall catalog area",
            fixtures.Select(thing => thing.ThingID),
            paddingPixels: 90);
        yield return new ScreenshotStep(
            "before native directional thin-wall drags",
            fixtures.Select(thing => thing.ThingID),
            paddingPixels: 90);

        IntVec3 previewCell = center + new IntVec3(-5, 0, 5);
        yield return CameraProjectionCheckpoint(
            "record the exact baseline map camera and viewport used by preview differencing",
            values => baselineCamera = values);
        yield return new ScreenshotStep(
            "full-viewport background before activating the single-cell preview",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return CameraIdentityUnchangedStep(
            "baseline camera remains exact through the end-of-frame screenshot",
            () => baselineCamera);
        yield return DesignatorSessionActionStep.Begin(
            "activate wood Thin Wall hover preview before clicking",
            Array.Empty<string>(),
            build.RuntimeType,
            build.StableId,
            new[] { "Structure" },
            new EndToEndMapCell(previewCell.x, previewCell.z),
            new EndToEndBuildMaterial("WoodLog"));
        yield return new CameraActionStep(
            "reassert the exact catalog camera before the south-edge preview",
            fixtures.Select(thing => thing.ThingID),
            paddingPixels: 90);
        yield return PreviewProjectionCheckpoint(
            "record the native south shared-edge projection used by the screenshot gate",
            new OwnedEdge(previewCell, ThinWallSide.South),
            values => southPreviewCamera = values);
        yield return new ScreenshotStep(
            "single-cell south-edge hover preview before clicking",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return CameraIdentityUnchangedStep(
            "south preview camera remains exact through the end-of-frame screenshot",
            () => southPreviewCamera);
        yield return DesignatorSessionActionStep.RotateLeft(
            "press the native Q-equivalent designator rotation action");
        yield return new CameraActionStep(
            "reassert the exact catalog camera before the east-edge preview",
            fixtures.Select(thing => thing.ThingID),
            paddingPixels: 90);
        yield return PreviewProjectionCheckpoint(
            "record the native east shared-edge projection used by the screenshot gate",
            new OwnedEdge(previewCell, ThinWallSide.East),
            values => eastPreviewCamera = values);
        yield return new ScreenshotStep(
            "single-cell east-edge hover preview after Q rotation",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return CameraIdentityUnchangedStep(
            "east preview camera remains exact through the end-of-frame screenshot",
            () => eastPreviewCamera);
        yield return DesignatorSessionActionStep.RotateRight(
            "press the native E-equivalent designator rotation action");
        yield return new ScreenshotStep(
            "single-cell south-edge hover preview restored after E rotation",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return DesignatorSessionActionStep.RotateLeft(
            "choose east edge again before the single-cell click");
        yield return DesignatorSessionActionStep.Commit(
            "click the previewed east edge without entering a second drag cell");

        yield return Drag("wood eastward line owns south edges", center + new IntVec3(-4, 0, 0), center + new IntVec3(3, 0, 0), EndToEndCardinalRotation.South, "WoodLog");
        yield return Drag("steel westward line owns north edges", center + new IntVec3(3, 0, 0), center + new IntVec3(-4, 0, 0), EndToEndCardinalRotation.North, "Steel");
        yield return Drag("granite northward line owns east edges", center + new IntVec3(0, 0, -4), center + new IntVec3(0, 0, 3), EndToEndCardinalRotation.East, "BlocksGranite");
        yield return Drag("wood southward line owns west edges", center + new IntVec3(0, 0, 3), center + new IntVec3(0, 0, -4), EndToEndCardinalRotation.West, "WoodLog");
        yield return Drag(
            "reject reverse-direction opposite owner on the occupied south shared edges",
            center + new IntVec3(3, 0, -1),
            center + new IntVec3(-4, 0, -1),
            EndToEndCardinalRotation.North,
            "Steel",
            expectRejected: true);

        yield return new WaitUntilStep(
            "native designator materializes directional walls",
            _ => CaptureWalls() > 0,
            new EndToEndDeadline(240, 600, TimeSpan.FromSeconds(20)));
        yield return new CheckpointStep(
            "capture native directional wall distribution",
            _ => new Dictionary<string, string>
            {
                ["wallCount"] = walls.Count.ToString(),
                ["north"] = walls.Count(wall => wall.OwnedSide == ThinWallSide.North).ToString(),
                ["east"] = walls.Count(wall => wall.OwnedSide == ThinWallSide.East).ToString(),
                ["south"] = walls.Count(wall => wall.OwnedSide == ThinWallSide.South).ToString(),
                ["west"] = walls.Count(wall => wall.OwnedSide == ThinWallSide.West).ToString(),
                ["wood"] = walls.Count(wall => wall.Stuff == ThingDefOf.WoodLog).ToString(),
                ["steel"] = walls.Count(wall => wall.Stuff == ThingDefOf.Steel).ToString(),
                ["granite"] = walls.Count(wall => wall.Stuff?.defName == "BlocksGranite").ToString(),
            });
        yield return new CameraActionStep(
            "frame observed directional thin walls",
            walls.Select(wall => wall.ThingID).Concat(new[] { regularWall.ThingID }),
            paddingPixels: 90);
        yield return new ScreenshotStep(
            "diagnostic after native directional drags",
            walls.Select(wall => wall.ThingID).Concat(new[] { regularWall.ThingID }),
            paddingPixels: 90);
        yield return new AssertionStep(
            "native drags preserve directions and reject an opposite owner on the same shared edge",
            _ =>
            {
                EndToEndAssert.Equal(33, walls.Count,
                    "One Q/E-oriented click plus four accepted eight-cell native lines must produce thirty-three unique shared edges");
                Building_ThinWall clicked = walls.Single(wall => wall.Position == previewCell);
                EndToEndAssert.Equal(ThinWallSide.East, clicked.OwnedSide,
                    "A single-cell click must retain the Q/E preview orientation because no second drag cell was entered");
                Building_ThinWall[] centerOwners = center.GetThingList(map).OfType<Building_ThinWall>().ToArray();
                EndToEndAssert.Equal(4, centerOwners.Select(wall => wall.OwnedSide).Distinct().Count(),
                    "The central free cell must hold all four independently oriented thin walls");
                EndToEndAssert.True(centerOwners.All(wall => wall.def.passability == Traversability.Standable),
                    "Every co-located owner must leave its cell standable");
                var south = new OwnedEdge(center, ThinWallSide.South).Shared;
                EndToEndAssert.Equal(1,
                    ThinWallUtility.ThingsOnSharedEdge(map, south, completedOnly: true).Count(),
                    "The reverse-direction designation must not create an opposite owner");
                EndToEndAssert.True(walls.Any(wall => wall.Position.AdjacentTo8WayOrInside(regularWall.Position)),
                    "A thin-wall segment must terminate against the regular wall fixture");
            });
        yield return new SelectionActionStep(
            "select native thin-wall visual catalog",
            walls.Select(wall => wall.ThingID).Concat(new[] { regularWall.ThingID }),
            additive: false);
        yield return new CameraActionStep(
            "frame directional thin-wall visual catalog",
            walls.Select(wall => wall.ThingID).Concat(new[] { regularWall.ThingID }),
            paddingPixels: 90);
        yield return new ScreenshotStep(
            "after native drags showing cardinal strips junctions unique edges and regular wall join",
            walls.Select(wall => wall.ThingID).Concat(new[] { regularWall.ThingID }),
            paddingPixels: 90);
        yield return new CheckpointStep(
            "native directional catalog outcome",
            _ => new Dictionary<string, string>
            {
                ["wallCount"] = walls.Count.ToString(),
                ["centerOwnerCount"] = center.GetThingList(map).OfType<Building_ThinWall>().Count().ToString(),
                ["southSharedEdgeOwnerCount"] = ThinWallUtility.ThingsOnSharedEdge(
                    map,
                    new OwnedEdge(center, ThinWallSide.South).Shared,
                    completedOnly: true).Count().ToString(),
                ["regularWallThingId"] = regularWall.ThingID,
                ["singleCellPreviewSide"] = walls.Single(wall => wall.Position == previewCell).OwnedSide.ToString(),
            });
    }

    private GizmoActionStep Drag(
        string name,
        IntVec3 start,
        IntVec3 end,
        EndToEndCardinalRotation rotation,
        string stuffDefName,
        bool expectRejected = false)
    {
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
            architectCategoryDefNames: new[] { "Structure" },
            expectRejected: expectRejected);
    }

    private static CheckpointStep PreviewProjectionCheckpoint(
        string name,
        OwnedEdge edge,
        Action<Dictionary<string, string>> retainCamera) => new(
        name,
        _ =>
        {
            float altitude = AltitudeLayer.Blueprint.AltitudeFor();
            Vector3 ownerCenter = new(edge.Cell.x + 0.5f, altitude, edge.Cell.z + 0.5f);
            Vector3 normal = edge.Side switch
            {
                ThinWallSide.North => Vector3.forward,
                ThinWallSide.East => Vector3.right,
                ThinWallSide.South => Vector3.back,
                ThinWallSide.West => Vector3.left,
                _ => throw new ArgumentOutOfRangeException(nameof(edge)),
            };
            Vector3 oppositeCenter = ownerCenter + normal;
            Vector3 boundaryCenter = ownerCenter + normal * 0.5f;
            Vector3 tangent = edge.Side is ThinWallSide.North or ThinWallSide.South
                ? Vector3.right
                : Vector3.forward;
            Vector3 start = boundaryCenter - tangent * 0.5f;
            Vector3 end = boundaryCenter + tangent * 0.5f;
            Vector3 screen = Find.Camera.WorldToScreenPoint(boundaryCenter);
            Vector3 startScreen = Find.Camera.WorldToScreenPoint(start);
            Vector3 endScreen = Find.Camera.WorldToScreenPoint(end);
            Vector3 ownerScreen = Find.Camera.WorldToScreenPoint(ownerCenter);
            Vector3 oppositeScreen = Find.Camera.WorldToScreenPoint(oppositeCenter);
            Dictionary<string, string> values = CameraProjectionValues();
            retainCamera(CopyCameraIdentity(values));
            values["screenX"] = screen.x.ToString("F3", CultureInfo.InvariantCulture);
            values["screenYTop"] = (UI.screenHeight - screen.y).ToString("F3", CultureInfo.InvariantCulture);
            values["startScreenX"] = startScreen.x.ToString("F3", CultureInfo.InvariantCulture);
            values["startScreenYTop"] = (UI.screenHeight - startScreen.y).ToString("F3", CultureInfo.InvariantCulture);
            values["endScreenX"] = endScreen.x.ToString("F3", CultureInfo.InvariantCulture);
            values["endScreenYTop"] = (UI.screenHeight - endScreen.y).ToString("F3", CultureInfo.InvariantCulture);
            values["ownerCellCenterScreenX"] = ownerScreen.x.ToString("F3", CultureInfo.InvariantCulture);
            values["ownerCellCenterScreenYTop"] = (UI.screenHeight - ownerScreen.y).ToString("F3", CultureInfo.InvariantCulture);
            values["oppositeCellCenterScreenX"] = oppositeScreen.x.ToString("F3", CultureInfo.InvariantCulture);
            values["oppositeCellCenterScreenYTop"] = (UI.screenHeight - oppositeScreen.y).ToString("F3", CultureInfo.InvariantCulture);
            values["ownedSide"] = edge.Side.ToString();
            return values;
        });

    private static CheckpointStep CameraProjectionCheckpoint(
        string name,
        Action<Dictionary<string, string>> retainCamera) => new(
        name,
        _ =>
        {
            Dictionary<string, string> values = CameraProjectionValues();
            retainCamera(CopyCameraIdentity(values));
            return values;
        });

    private static AssertionStep CameraIdentityUnchangedStep(
        string name,
        Func<Dictionary<string, string>> expected) => new(
        name,
        _ =>
        {
            Dictionary<string, string> actual = CameraProjectionValues();
            foreach ((string key, string value) in expected())
            {
                EndToEndAssert.Equal(value, actual[key],
                    $"The exact camera field '{key}' changed while the end-of-frame screenshot was captured.");
            }
        });

    private static Dictionary<string, string> CopyCameraIdentity(Dictionary<string, string> values) =>
        values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    private static Dictionary<string, string> CameraProjectionValues()
    {
        Vector3 cameraPosition = Find.CameraDriver.transform.position;
        return new Dictionary<string, string>
        {
            ["mapId"] = "map-" + Current.Game.CurrentMap.uniqueID,
            ["cameraWorldX"] = cameraPosition.x.ToString("F4", CultureInfo.InvariantCulture),
            ["cameraWorldZ"] = cameraPosition.z.ToString("F4", CultureInfo.InvariantCulture),
            ["cameraRootSize"] = Find.CameraDriver.RootSize.ToString("F4", CultureInfo.InvariantCulture),
            ["viewportX"] = "0",
            ["viewportY"] = "0",
            ["screenshotWidth"] = UI.screenWidth.ToString(CultureInfo.InvariantCulture),
            ["screenshotHeight"] = UI.screenHeight.ToString(CultureInfo.InvariantCulture),
        };
    }

    private int CaptureWalls()
    {
        walls.Clear();
        walls.AddRange(map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName))
            .OfType<Building_ThinWall>()
            .Where(wall => wall.Position.InHorDistOf(center, 8f)));
        return walls.Count;
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

        throw new EndToEndAssertionException("Could not find a clear Thin Walls visual-catalog area.");
    }
}
