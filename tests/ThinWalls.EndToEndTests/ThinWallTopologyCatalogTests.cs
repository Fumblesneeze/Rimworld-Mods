using System;
using System.Collections;
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

[RimWorldEndToEndTest(
    "thin-walls.supporting-complete-linked-topology-catalog",
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
        NormalizeToClearNoon(map);
        context.DeferCleanup(() =>
        {
            Find.ScreenshotModeHandler.Active = originalScreenshotMode;
            Find.TickManager.DebugSetTicksGame(originalTicks);
            Find.TickManager.gameStartAbsTick = originalAbsoluteStart;
            map.weatherManager.curWeather = originalWeather;
            map.weatherManager.lastWeather = originalLastWeather;
            map.weatherManager.curWeatherAge = originalWeatherAge;
            map.weatherManager.prevSkyTargetLerp = originalPrevSkyTargetLerp;
            map.weatherManager.currSkyTargetLerp = originalCurrSkyTargetLerp;
            map.weatherManager.ResetSkyTargetLerpCache();
        });
        IntVec3 center = FindClearCenter(map);
        catalogEnvelope = CellRect.CenteredOn(center, CleanCaptureEnvelopeRadius);
        ThingDef wallDef = DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName);
        ThingDef stuff = DefDatabase<ThingDef>.GetNamed("BlocksGranite");
        for (int mask = 0; mask < 16; mask++)
        {
            int column = mask % 4;
            int row = mask / 4;
            IntVec3 vertex = center + new IntVec3((column - 1) * 4, 0, (row - 1) * 4);
            vertices[mask] = vertex;
            foreach (HybridWallDirection direction in Directions((HybridWallRayMask)mask))
            {
                OwnedEdge edge = OwnerFor(vertex, direction);
                var wall = (Building_ThinWall)ThingMaker.MakeThing(wallDef, stuff);
                wall.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(wall, edge.Cell, map, new Rot4((int)edge.Side));
                fixtures.Add(wall);
            }

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
        string[] ids = fixtures.Select(thing => thing.ThingID).ToArray();
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
        yield return new CameraActionStep("far useful zoom of all sixteen Thin-only linked states", ids, 195);
        yield return new AssertionStep(
            "far topology catalog is cleanly framed and remains free of the normal game interface",
            _ => AssertCatalogViewport(ids, minimumMarginPixels: 72));
        yield return new ScreenshotStep("all sixteen Core-derived Thin Wall linked states at far useful zoom", ids, 195);
        yield return new ScreenshotModeActionStep(
            "restore the topology catalog's prior screenshot-mode state",
            originalScreenshotMode);
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
