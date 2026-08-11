using System.Collections.Generic;
using RimWorldDevGateway.EndToEndTesting;
using RimWorldDevGateway.PerformanceTesting;

namespace RimWorldDevGateway.PerformanceTests;

internal static class CalibrationContract
{
    public const string Owner = "fumblesneeze.rimworlddevgateway";
    public const string Subject = "fumblesneeze.rimworlddevgateway";
    public const string Comparison = "gateway.circinus-calibration";
}

[RimWorldPerformanceTest(
    "gateway.circinus-calibration.instrumented",
    CalibrationContract.Owner,
    CalibrationContract.Subject,
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    DeterministicSeed = 60161,
    WorkloadVersion = "gateway-calibration/v1",
    ComparisonId = CalibrationContract.Comparison,
    WarmUpTicks = 120,
    SampleTicks = 600,
    GameSpeed = PerformanceGameSpeed.Normal,
    Repetitions = 1,
    EvidenceLens = PerformanceEvidenceLens.ProductInstrumented)]
[PerformanceMethodSelector(
    PerformanceMethodSelectorKind.Method,
    "RimWorldDevGateway.GatewayGameControlController::Capture()",
    "gateway-game-control")]
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
    DeterministicSeed = 60161,
    WorkloadVersion = "gateway-calibration/v1",
    ComparisonId = CalibrationContract.Comparison,
    WarmUpTicks = 120,
    SampleTicks = 600,
    GameSpeed = PerformanceGameSpeed.Normal,
    Repetitions = 1,
    EvidenceLens = PerformanceEvidenceLens.ArmedDisabledWrapper)]
[PerformanceMethodSelector(
    PerformanceMethodSelectorKind.Method,
    "RimWorldDevGateway.GatewayGameControlController::Capture()",
    "gateway-game-control")]
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
    DeterministicSeed = 60161,
    WorkloadVersion = "gateway-calibration/v1",
    ComparisonId = CalibrationContract.Comparison,
    WarmUpTicks = 120,
    SampleTicks = 600,
    GameSpeed = PerformanceGameSpeed.Normal,
    Repetitions = 1,
    EvidenceLens = PerformanceEvidenceLens.FullyDisarmed)]
[PerformanceMethodSelector(
    PerformanceMethodSelectorKind.Method,
    "RimWorldDevGateway.GatewayGameControlController::Capture()",
    "gateway-game-control")]
[PerformanceMethodSelector(
    PerformanceMethodSelectorKind.HarmonyOwner,
    CalibrationContract.Owner,
    "exact-native-tick-window")]
public sealed class DisarmedCalibrationBenchmark : CalibrationBenchmark { }

public abstract class CalibrationBenchmark : IRimWorldPerformanceTest
{
    public void Arrange(IEndToEndContext context) { }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield break;
    }
}
