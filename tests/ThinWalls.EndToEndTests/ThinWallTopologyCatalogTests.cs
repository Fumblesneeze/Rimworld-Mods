using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using ThinWalls.Rendering;
using UnityEngine;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest(
    "thin-walls.native-material-topology-catalog",
    "fumblesneeze.thinwalls",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.thinwalls",
    MaxFrames = 1_500,
    MaxGameTicks = 1_000,
    MaxWallClockSeconds = 60)]
public sealed class ThinWallTopologyCatalogTest : IRimWorldEndToEndTest
{
    private const int CleanCaptureEnvelopeRadius = 13;
    private static readonly ThingDef InvisibleCatalogAnchorDef = new()
    {
        defName = "TW_E2E_InvisibleCatalogAnchor",
        label = "invisible catalog anchor",
        thingClass = typeof(Thing),
        category = ThingCategory.Item,
        drawerType = DrawerType.None,
        selectable = false,
        useHitPoints = false,
        stackLimit = 1,
    };
    private readonly List<Thing> fixtures = new();
    private readonly Dictionary<int, IntVec3> vertices = new();
    private Map map = null!;
    private CellRect catalogEnvelope;
    private bool originalScreenshotMode;
    private EndToEndGizmoOption build = null!;
    private readonly Dictionary<IntVec3, TerrainDef> originalTerrain = new();
    private readonly List<Thing> farFraming = new();
    private float ordinaryPixelsPerCell;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        int originalTicks = Find.TickManager.TicksGame;
        int originalAbsoluteStart = Find.TickManager.gameStartAbsTick;
        WeatherDef originalWeather = map.weatherManager.curWeather;
        WeatherDef originalLastWeather = map.weatherManager.lastWeather;
        int originalWeatherAge = map.weatherManager.curWeatherAge;
        float originalPrevSkyTargetLerp = map.weatherManager.prevSkyTargetLerp;
        float originalCurrSkyTargetLerp = map.weatherManager.currSkyTargetLerp;
        originalScreenshotMode = Find.ScreenshotModeHandler.Active;
        bool originalGodMode = DebugSettings.godMode;
        DebugSettings.godMode = true;
        NormalizeToClearNoon(map);
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
            foreach (var terrain in originalTerrain) map.terrainGrid.SetTerrain(terrain.Key, terrain.Value);
        });
        IntVec3 center = FindClearCenter(map);
        catalogEnvelope = CellRect.CenteredOn(center, CleanCaptureEnvelopeRadius);
        foreach (var cell in catalogEnvelope.Cells)
        {
            originalTerrain[cell] = map.terrainGrid.TerrainAt(cell);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
        }
        build = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(Array.Empty<string>(), new[] { "Structure" })
            .Single(option => !option.Disabled && option.BuildableDefName == ThinWallUtility.ThinWallDefName &&
                              option.Interaction == EndToEndGizmoInteraction.Drag);
        foreach (int offset in new[] { -22, 22 })
        {
            Thing marker = ThingMaker.MakeThing(InvisibleCatalogAnchorDef);
            GenSpawn.Spawn(marker, center + new IntVec3(offset, 0, offset), map);
            fixtures.Add(marker);
            farFraming.Add(marker);
        }
        for (int mask = 0; mask < 16; mask++)
        {
            int column = mask % 4;
            int row = mask / 4;
            IntVec3 vertex = center + new IntVec3((column - 1) * 4, 0, (row - 1) * 4);
            vertices[mask] = vertex;
            if (mask == 0)
            {
                Thing marker = ThingMaker.MakeThing(InvisibleCatalogAnchorDef);
                GenSpawn.Spawn(marker, vertex, map);
                fixtures.Add(marker);
            }
        }
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        if (Find.WindowStack.Windows.Any(window => window.GetType().FullName == "LudeonTK.EditWindow_Log"))
            yield return new WindowCancelActionStep("close startup log", "LudeonTK.EditWindow_Log");
        yield return new ScreenshotStep("before native topology designations", fixtures.Except(farFraming).Select(thing => thing.ThingID), 110);
        for (int mask = 1; mask < 16; mask++)
        foreach (var direction in Directions((HybridWallRayMask)mask))
        {
            OwnedEdge edge = OwnerFor(vertices[mask], direction);
            var cell = new EndToEndMapCell(edge.Cell.x, edge.Cell.z);
            yield return new GizmoActionStep($"mask {mask}: designate {direction} ray", Array.Empty<string>(), build.RuntimeType,
                EndToEndGizmoInteraction.Drag, (EndToEndCardinalRotation)(int)edge.Side,
                new EndToEndBuildMaterial("BlocksGranite"), stableGizmoId: build.StableId,
                startCell: cell, endCell: cell, architectCategoryDefNames: new[] { "Structure" });
            var wall = edge.Cell.GetThingList(map).OfType<Building_ThinWall>().Single(thing => thing.OwnedSide == edge.Side);
            fixtures.Add(wall);
        }
        string[] ids = fixtures.Except(farFraming).Select(thing => thing.ThingID).ToArray();
        yield return new ScreenshotModeActionStep(
            "enable native screenshot mode for the clean topology catalog",
            enabled: true);
        yield return new AssertionStep(
            "supporting fixture contains all sixteen linked masks in stable order",
            _ =>
            {
                EndToEndAssert.Equal(16, vertices.Count, "The topology catalog must contain mask 0 through mask 15.");
                AssertCaptureEnvelopeClean();
                for (int mask = 1; mask < 16; mask++)
                {
                    HybridWallDirection[] expected = Directions((HybridWallRayMask)mask).ToArray();
                    Building_ThinWall[] incident = fixtures
                        .OfType<Building_ThinWall>()
                        .Where(wall => EdgeTouchesVertex(wall.OwnedEdge.Shared, vertices[mask]))
                        .ToArray();
                    EndToEndAssert.Equal(expected.Length, incident.Length,
                        $"Mask {mask} must have exactly its {expected.Length} incident rays.");
                }
            });
        yield return new CheckpointStep(
            "complete Thin-only linked topology keys",
            _ => Enumerable.Range(0, 16).ToDictionary(
                    mask => $"mask{mask:D2}",
                    mask => ((HybridWallRayMask)mask).ToString()));
        yield return new CameraActionStep("ordinary zoom of all sixteen Thin-only linked states", ids, 110);
        yield return new AssertionStep(
            "ordinary topology catalog is cleanly framed inside the screenshot-mode viewport",
            _ => AssertCatalogViewport(ids, minimumMarginPixels: 32));
        yield return new ScreenshotStep("all sixteen Core-derived Thin Wall linked states at ordinary zoom", ids, 110);
        yield return new CheckpointStep("native source material and ordinary zoom identity", _ =>
        {
            ordinaryPixelsPerCell = PixelsPerCell();
            var materials = fixtures.OfType<Building_ThinWall>().Select(wall => map.mapDrawer.SectionAt(wall.Position))
                .Distinct().Select(section => section.GetLayer(typeof(SectionLayer_ThingsGeneral)))
                .SelectMany(layer => layer.subMeshes).Where(mesh => !mesh.disabled && mesh.mesh.vertexCount > 0)
                .Select(mesh => mesh.material).Distinct().ToArray();
            EndToEndAssert.True(materials.Any(material => material.mainTexture?.name == "Wall_Atlas_Bricks"),
                "Native placement must produce a mesh bound to the actual Core wall atlas.");
            EndToEndAssert.True(materials.All(material => material.mainTexture?.name.StartsWith("TW_", StringComparison.Ordinal) != true),
                "Completed catalog meshes must not silently fall back to painted Thin Walls textures.");
            return new Dictionary<string, string>
            {
                ["pixelsPerCell"] = ordinaryPixelsPerCell.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                ["materials"] = string.Join(";", materials.Select(material => material.name + ":" + material.mainTexture?.name + ":" + material.shader.name)),
                ["sourceRgbaSha256"] = string.Join(";", materials
                    .Where(material => material.mainTexture != null)
                    .GroupBy(material => material.mainTexture.name)
                    .Select(group => group.Key + ":" + HashRgba(group.First().mainTexture)))
            };
        });
        yield return new CameraActionStep("far useful zoom of all sixteen Thin-only linked states",
            ids.Concat(farFraming.Select(thing => thing.ThingID)), 100);
        yield return new AssertionStep(
            "far topology catalog is cleanly framed and remains free of the normal game interface",
            _ => AssertCatalogViewport(ids, minimumMarginPixels: 72));
        yield return new ScreenshotStep("all sixteen Core-derived Thin Wall linked states at far useful zoom", ids, 195);
        yield return new CheckpointStep("materially different far zoom scale", _ =>
        {
            float far = PixelsPerCell();
            EndToEndAssert.True(far <= ordinaryPixelsPerCell * 0.65f, "Far capture must have at least 35% smaller cell scale than ordinary.");
            return new Dictionary<string, string> { ["pixelsPerCell"] = far.ToString("R", System.Globalization.CultureInfo.InvariantCulture) };
        });
        yield return new ScreenshotModeActionStep(
            "restore the topology catalog's prior screenshot-mode state",
            originalScreenshotMode);
    }

    private float PixelsPerCell()
    {
        Vector3 origin = vertices[15].ToVector3();
        return Vector3.Distance(Find.Camera.WorldToScreenPoint(origin), Find.Camera.WorldToScreenPoint(origin + Vector3.right));
    }

    private static string HashRgba(Texture texture)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture temporary = RenderTexture.GetTemporary(
            texture.width,
            texture.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default);
        Texture2D? readable = null;
        try
        {
            Graphics.Blit(texture, temporary);
            RenderTexture.active = temporary;
            readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0, false);
            readable.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            Color32[] pixels = readable.GetPixels32();
            var bytes = new byte[pixels.Length * 4];
            for (int i = 0; i < pixels.Length; i++)
            {
                int offset = i * 4;
                bytes[offset] = pixels[i].r;
                bytes[offset + 1] = pixels[i].g;
                bytes[offset + 2] = pixels[i].b;
                bytes[offset + 3] = pixels[i].a;
            }
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
            if (readable != null) UnityEngine.Object.Destroy(readable);
        }
    }

    public void Cleanup(IEndToEndContext context)
    {
        foreach (Thing fixture in fixtures.AsEnumerable().Reverse())
        {
            if (!fixture.Destroyed)
            {
                fixture.Destroy(DestroyMode.Vanish);
            }
        }
        fixtures.Clear();
        vertices.Clear();
    }

    private static OwnedEdge OwnerFor(IntVec3 vertex, HybridWallDirection direction) => direction switch
    {
        HybridWallDirection.West => new OwnedEdge(new IntVec3(vertex.x - 1, 0, vertex.z - 1), ThinWallSide.North),
        HybridWallDirection.East => new OwnedEdge(new IntVec3(vertex.x, 0, vertex.z - 1), ThinWallSide.North),
        HybridWallDirection.South => new OwnedEdge(new IntVec3(vertex.x - 1, 0, vertex.z - 1), ThinWallSide.East),
        HybridWallDirection.North => new OwnedEdge(new IntVec3(vertex.x - 1, 0, vertex.z), ThinWallSide.East),
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };

    private static IEnumerable<HybridWallDirection> Directions(HybridWallRayMask mask)
    {
        if (mask.HasFlag(HybridWallRayMask.North)) yield return HybridWallDirection.North;
        if (mask.HasFlag(HybridWallRayMask.East)) yield return HybridWallDirection.East;
        if (mask.HasFlag(HybridWallRayMask.South)) yield return HybridWallDirection.South;
        if (mask.HasFlag(HybridWallRayMask.West)) yield return HybridWallDirection.West;
    }

    private static bool EdgeTouchesVertex(SharedEdge edge, IntVec3 vertex)
    {
        IntVec3 first = edge.PositiveSide == ThinWallSide.North
            ? new IntVec3(edge.AnchorCell.x, 0, edge.AnchorCell.z + 1)
            : new IntVec3(edge.AnchorCell.x + 1, 0, edge.AnchorCell.z);
        IntVec3 second = edge.PositiveSide == ThinWallSide.North
            ? new IntVec3(first.x + 1, 0, first.z)
            : new IntVec3(first.x, 0, first.z + 1);
        return first == vertex || second == vertex;
    }

    private void AssertCatalogViewport(IEnumerable<string> ids, int minimumMarginPixels)
    {
        AssertCaptureEnvelopeClean();
        EndToEndAssert.True(
            Find.ScreenshotModeHandler.Active,
            "The topology catalog must use RimWorld's native screenshot mode so normal UI cannot contaminate blind evidence.");
        foreach (string id in ids)
        {
            Thing thing = Current.Game.CurrentMap.listerThings.AllThings
                .Single(candidate => candidate.ThingID == id);
            Vector3 screen = Find.Camera.WorldToScreenPoint(thing.DrawPos);
            EndToEndAssert.True(
                screen.x >= minimumMarginPixels &&
                screen.x <= Screen.width - minimumMarginPixels &&
                screen.y >= minimumMarginPixels &&
                screen.y <= Screen.height - minimumMarginPixels,
                $"Catalog target {id} projected to ({screen.x:F1},{screen.y:F1}) outside the safe {minimumMarginPixels}px viewport margin.");
        }
    }

    private void AssertCaptureEnvelopeClean()
    {
        EndToEndAssert.True(
            catalogEnvelope.All(cell => IsFreeOfAtmosphericContamination(cell, map)),
            "The complete capture envelope must remain free of gas and pollution overlays.");
        EndToEndAssert.True(
            catalogEnvelope.All(cell => cell.GetThingList(map)
                .All(thing => fixtures.Contains(thing) || !IsTransientVisualContaminant(thing))),
            "No untracked pawn, item, mote, gas, ethereal effect, or psychic emitter may enter the capture envelope.");
    }

    private static IntVec3 FindClearCenter(Map map)
    {
        float safeRadius = Math.Min(60f, Math.Min(map.Size.x, map.Size.z) * 0.5f - 5f);
        foreach (IntVec3 candidate in GenRadial.RadialCellsAround(map.Center, safeRadius, true))
        {
            CellRect area = CellRect.CenteredOn(candidate, CleanCaptureEnvelopeRadius);
            if (!area.InBounds(map))
            {
                continue;
            }

            bool clear = true;
            foreach (IntVec3 cell in area)
            {
                if (cell.GetEdifice(map) != null ||
                    !IsFreeOfAtmosphericContamination(cell, map) ||
                    cell.GetThingList(map).Any(IsTransientVisualContaminant))
                {
                    clear = false;
                    break;
                }
            }
            if (clear)
            {
                return candidate;
            }
        }

        throw new EndToEndAssertionException("Could not find a clear area for the 16-state Thin Wall catalog.");
    }

    private static bool IsFreeOfAtmosphericContamination(IntVec3 cell, Map target) =>
        target.gasGrid.DensityAt(cell, GasType.BlindSmoke) == 0 &&
        target.gasGrid.DensityAt(cell, GasType.ToxGas) == 0 &&
        target.gasGrid.DensityAt(cell, GasType.RotStink) == 0 &&
        target.gasGrid.DensityAt(cell, GasType.DeadlifeDust) == 0 &&
        !cell.IsPolluted(target);

    private static bool IsTransientVisualContaminant(Thing thing) => thing.def.category switch
    {
        ThingCategory.Pawn => true,
        ThingCategory.Item => true,
        ThingCategory.Gas => true,
        ThingCategory.Mote => true,
        ThingCategory.Ethereal => true,
        ThingCategory.PsychicEmitter => true,
        _ => false,
    };

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
}
