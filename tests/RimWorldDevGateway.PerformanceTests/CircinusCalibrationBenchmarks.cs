using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.EndToEndTesting;
using RimWorldDevGateway.PerformanceTesting;
using Verse;

namespace RimWorldDevGateway.PerformanceTests;

internal static class CalibrationContract
{
    public const string Owner = "fumblesneeze.rimworlddevgateway";
    public const string Subject = "fumblesneeze.rimworlddevgateway";
    public const string Comparison = "gateway.circinus-calibration";
    public const int Seed = 60161;
    public const int NativeTickComponentCount = 32_768;
}

[RimWorldPerformanceTest(
    "gateway.circinus-calibration.instrumented",
    CalibrationContract.Owner,
    CalibrationContract.Subject,
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    DeterministicSeed = CalibrationContract.Seed,
    WorkloadVersion = "gateway-calibration/v4",
    ComparisonId = CalibrationContract.Comparison,
    WarmUpTicks = 300,
    SampleTicks = 3_000,
    GameSpeed = PerformanceGameSpeed.Superfast,
    Repetitions = 1,
    EvidenceLens = PerformanceEvidenceLens.ProductInstrumented)]
[PerformanceMethodSelector(
    PerformanceMethodSelectorKind.Method,
    "RimWorldDevGateway.GatewayGameControlController::Capture()",
    "gateway-game-control")]
[PerformanceMethodSelector(
    PerformanceMethodSelectorKind.Method,
    "RimWorldDevGateway.PerformanceTests.CalibrationTickComponent::MapComponentTick()",
    "native-adaptive-calibration")]
[PerformanceMethodSelector(
    PerformanceMethodSelectorKind.HarmonyOwner,
    CalibrationContract.Owner,
    "exact-native-tick-window")]
public sealed class InstrumentedCalibrationBenchmark : CalibrationBenchmark { }

[RimWorldPerformanceTest(
    "gateway.circinus-calibration.armed-disabled",
    CalibrationContract.Owner,
    CalibrationContract.Subject,
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    DeterministicSeed = CalibrationContract.Seed,
    WorkloadVersion = "gateway-calibration/v4",
    ComparisonId = CalibrationContract.Comparison,
    WarmUpTicks = 300,
    SampleTicks = 3_000,
    GameSpeed = PerformanceGameSpeed.Superfast,
    Repetitions = 1,
    EvidenceLens = PerformanceEvidenceLens.ArmedDisabledWrapper)]
[PerformanceMethodSelector(
    PerformanceMethodSelectorKind.Method,
    "RimWorldDevGateway.GatewayGameControlController::Capture()",
    "gateway-game-control")]
[PerformanceMethodSelector(
    PerformanceMethodSelectorKind.Method,
    "RimWorldDevGateway.PerformanceTests.CalibrationTickComponent::MapComponentTick()",
    "native-adaptive-calibration")]
[PerformanceMethodSelector(
    PerformanceMethodSelectorKind.HarmonyOwner,
    CalibrationContract.Owner,
    "exact-native-tick-window")]
public sealed class ArmedDisabledCalibrationBenchmark : CalibrationBenchmark { }

[RimWorldPerformanceTest(
    "gateway.circinus-calibration.disarmed",
    CalibrationContract.Owner,
    CalibrationContract.Subject,
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    DeterministicSeed = CalibrationContract.Seed,
    WorkloadVersion = "gateway-calibration/v4",
    ComparisonId = CalibrationContract.Comparison,
    WarmUpTicks = 300,
    SampleTicks = 3_000,
    GameSpeed = PerformanceGameSpeed.Superfast,
    Repetitions = 1,
    EvidenceLens = PerformanceEvidenceLens.FullyDisarmed)]
[PerformanceMethodSelector(
    PerformanceMethodSelectorKind.Method,
    "RimWorldDevGateway.GatewayGameControlController::Capture()",
    "gateway-game-control")]
[PerformanceMethodSelector(
    PerformanceMethodSelectorKind.Method,
    "RimWorldDevGateway.PerformanceTests.CalibrationTickComponent::MapComponentTick()",
    "native-adaptive-calibration")]
[PerformanceMethodSelector(
    PerformanceMethodSelectorKind.HarmonyOwner,
    CalibrationContract.Owner,
    "exact-native-tick-window")]
public sealed class DisarmedCalibrationBenchmark : CalibrationBenchmark { }

public abstract class CalibrationBenchmark : IRimWorldPerformanceTest
{
    public void Arrange(IEndToEndContext context)
    {
        var map = Find.CurrentMap ?? throw new InvalidOperationException(
            "The Circinus calibration requires one playable map.");

        // Quicktest deliberately chooses a new world/map for every process. Anchor the disposable
        // map to one world-grid coordinate and one climate/time before measuring so date, seasonal
        // temperature, wild spawners, weather and lighting do not vary between evidence lenses.
        map.Parent.Tile = new PlanetTile(0);
        map.TileInfo.PrimaryBiome = BiomeDefOf.SeaIce;
        map.TileInfo.temperature = 20f;
        map.TileInfo.rainfall = 0f;
        map.TileInfo.swampiness = 0f;
        map.TileInfo.pollution = 0f;
        Find.TickManager.DebugSetTicksGame(0);
        // DebugSetTicksGame may preserve absolute time by changing the start tick. Re-apply a
        // positive initialized value afterwards, then compensate for this generated world's
        // local date offset until every map reports day one at 06:00.
        Find.TickManager.gameStartAbsTick = GenDate.TicksPerYear + 15_000;
        for (var pass = 0; pass < 2; pass++)
        {
            var localTicksIntoYear =
                GenLocalDate.DayOfYear(map) * GenDate.TicksPerDay + GenLocalDate.DayTick(map);
            Find.TickManager.gameStartAbsTick += 15_000 - localTicksIntoYear;
        }
        if (GenLocalDate.DayOfYear(map) != 0 || GenLocalDate.HourInteger(map) != 6)
            throw new InvalidOperationException("Failed to normalize the calibration's local date and time.");

        var cells = map.AllCells.ToArray();

        // Direct fixture setup is outside the measured window. Normalize the generated quicktest
        // map so the three fresh-process evidence lenses do not compare unrelated terrain, roofs,
        // plants, pawns, weather, camera positions, or animation workloads.
        foreach (var cell in cells)
            if (map.roofGrid.Roofed(cell)) map.roofGrid.SetRoof(cell, null);
        foreach (var thing in map.listerThings.AllThings.ToArray())
        {
            if (!thing.Spawned) continue;
            if (thing.def.destroyable) thing.Destroy(DestroyMode.Vanish);
            else thing.DeSpawn(DestroyMode.Vanish);
        }
        foreach (var cell in cells)
        {
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
            map.snowGrid.SetDepth(cell, 0f);
        }

        map.weatherManager.curWeather = WeatherDefOf.Clear;
        map.weatherManager.lastWeather = WeatherDefOf.Clear;
        map.weatherManager.curWeatherAge = 0;
        map.weatherManager.prevSkyTargetLerp = 1f;
        map.weatherManager.currSkyTargetLerp = 1f;
        map.weatherManager.ResetSkyTargetLerpCache();
        map.mapDrawer.RegenerateEverythingNow();
        Find.CameraDriver.SetRootPosAndSize(map.Center.ToVector3Shifted(), 42f);

        // Establish the declared seed as the actual global game RNG state immediately before
        // warm-up, after all generated-world cleanup has finished. The repeated no-op component
        // calls then arrive through Map.MapPreTick's native MapComponentTick loop; the fixture
        // never invokes the profiled method directly merely to manufacture adaptive counts.
        var priorRandState = SeedGlobalRand(CalibrationContract.Seed);
        context.DeferCleanup(() => RestoreGlobalRand(priorRandState));
        var calibrationComponents = new List<CalibrationTickComponent>(
            CalibrationContract.NativeTickComponentCount);
        for (var index = 0; index < CalibrationContract.NativeTickComponentCount; index++)
        {
            var component = new CalibrationTickComponent(map);
            calibrationComponents.Add(component);
            map.components.Add(component);
        }
        context.DeferCleanup(() =>
        {
            foreach (var component in calibrationComponents)
                map.components.Remove(component);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield break;
    }

    private static ulong SeedGlobalRand(int seed)
    {
        var stateProperty = RequireRandStateProperty();
        var prior = (ulong)stateProperty.GetValue(null, null)!;
        ulong seeded;
        Rand.PushState(seed);
        try
        {
            seeded = (ulong)stateProperty.GetValue(null, null)!;
        }
        finally
        {
            Rand.PopState();
        }

        stateProperty.SetValue(null, seeded, null);
        return prior;
    }

    private static void RestoreGlobalRand(ulong state) =>
        RequireRandStateProperty().SetValue(null, state, null);

    private static PropertyInfo RequireRandStateProperty()
    {
        var property = typeof(Rand).GetProperty(
            "StateCompressed", BindingFlags.Static | BindingFlags.NonPublic);
        if (property is null || property.PropertyType != typeof(ulong) ||
            property.GetMethod is null || property.SetMethod is null)
            throw new MissingMemberException(typeof(Rand).FullName, "StateCompressed");
        return property;
    }
}

public sealed class CalibrationTickComponent : MapComponent
{
    public CalibrationTickComponent(Map map) : base(map) { }

    public override void MapComponentTick() { }
}
