using System.Collections.Generic;
using RimWorldDevGateway.EndToEndTesting;
using RimWorldDevGateway.PerformanceTesting;

namespace PerformanceHost.ValidFixtures;

[RimWorldPerformanceTest(
    "alpha.base",
    "alpha.mod",
    "alpha.mod",
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    "alpha.mod",
    DeterministicSeed = 7123,
    WorkloadVersion = "alpha/v2",
    WarmUpTicks = 900,
    SampleTicks = 3600,
    GameSpeed = PerformanceGameSpeed.Fast,
    Repetitions = 2,
    EvidenceLens = PerformanceEvidenceLens.ArmedDisabledWrapper,
    ComparisonId = "alpha.base",
    ProductAbsentControlId = "gateway.alpha-control")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "Alpha.Work::Tick", "tick")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, "alpha.harmony", "patch")]
[PerformanceThroughputCheckpoint("meals", 12)]
[PerformanceThroughputCheckpoint("washes", 3)]
public sealed class BaseBenchmark : NoOpBenchmark
{
    static BaseBenchmark()
    {
        throw new System.InvalidOperationException("Metadata discovery must not run static constructors.");
    }
}

[RimWorldPerformanceTest(
    "alpha.base-instrumented",
    "alpha.mod",
    "alpha.mod",
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    "alpha.mod",
    DeterministicSeed = 7123,
    WorkloadVersion = "alpha/v2",
    WarmUpTicks = 900,
    SampleTicks = 3600,
    GameSpeed = PerformanceGameSpeed.Fast,
    Repetitions = 2,
    EvidenceLens = PerformanceEvidenceLens.ProductInstrumented,
    ComparisonId = "alpha.base",
    ProductAbsentControlId = "gateway.alpha-control")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "Alpha.Work::Tick", "tick")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, "alpha.harmony", "patch")]
[PerformanceThroughputCheckpoint("meals", 12)]
[PerformanceThroughputCheckpoint("washes", 3)]
public sealed class BaseInstrumentedBenchmark : NoOpBenchmark { }

[RimWorldPerformanceTest(
    "alpha.base-disarmed",
    "alpha.mod",
    "alpha.mod",
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    "alpha.mod",
    DeterministicSeed = 7123,
    WorkloadVersion = "alpha/v2",
    WarmUpTicks = 900,
    SampleTicks = 3600,
    GameSpeed = PerformanceGameSpeed.Fast,
    Repetitions = 2,
    EvidenceLens = PerformanceEvidenceLens.FullyDisarmed,
    ComparisonId = "alpha.base",
    ProductAbsentControlId = "gateway.alpha-control")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "Alpha.Work::Tick", "tick")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, "alpha.harmony", "patch")]
[PerformanceThroughputCheckpoint("meals", 12)]
[PerformanceThroughputCheckpoint("washes", 3)]
public sealed class BaseDisarmedBenchmark : NoOpBenchmark { }

[RimWorldPerformanceTest(
    "alpha.optional",
    "alpha.mod",
    "alpha.mod",
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    "optional.mod",
    "alpha.mod",
    WorkloadVersion = "alpha/v2-optional",
    ComparisonId = "alpha.optional")]
public sealed class OptionalBenchmark : NoOpBenchmark
{
}

[RimWorldPerformanceTest(
    "alpha.optional-armed-disabled",
    "alpha.mod",
    "alpha.mod",
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    "optional.mod",
    "alpha.mod",
    WorkloadVersion = "alpha/v2-optional",
    ComparisonId = "alpha.optional",
    EvidenceLens = PerformanceEvidenceLens.ArmedDisabledWrapper)]
public sealed class OptionalArmedDisabledBenchmark : NoOpBenchmark { }

[RimWorldPerformanceTest(
    "alpha.optional-disarmed",
    "alpha.mod",
    "alpha.mod",
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    "optional.mod",
    "alpha.mod",
    WorkloadVersion = "alpha/v2-optional",
    ComparisonId = "alpha.optional",
    EvidenceLens = PerformanceEvidenceLens.FullyDisarmed)]
public sealed class OptionalDisarmedBenchmark : NoOpBenchmark { }

public abstract class NoOpBenchmark : IRimWorldPerformanceTest
{
    public void Arrange(IEndToEndContext context) { }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield break;
    }
}
