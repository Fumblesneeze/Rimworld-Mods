using System.Collections.Generic;
using RimWorldDevGateway.EndToEndTesting;
using RimWorldDevGateway.PerformanceTesting;

namespace PerformanceHost.ControlFixtures;

[RimWorldPerformanceTest(
    "gateway.alpha-present",
    "fumblesneeze.rimworlddevgateway",
    "alpha.mod",
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    "alpha.mod",
    ComparisonId = "gateway.alpha-neutral",
    ProductAbsentControlId = "gateway.alpha-absent")]
[PerformanceThroughputCheckpoint("colonists-active", 1)]
public sealed class PresentInstrumentedBenchmark : NoOpBenchmark { }

[RimWorldPerformanceTest(
    "gateway.alpha-present-armed-disabled",
    "fumblesneeze.rimworlddevgateway",
    "alpha.mod",
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    "alpha.mod",
    ComparisonId = "gateway.alpha-neutral",
    ProductAbsentControlId = "gateway.alpha-absent",
    EvidenceLens = PerformanceEvidenceLens.ArmedDisabledWrapper)]
[PerformanceThroughputCheckpoint("colonists-active", 1)]
public sealed class PresentArmedDisabledBenchmark : NoOpBenchmark { }

[RimWorldPerformanceTest(
    "gateway.alpha-present-disarmed",
    "fumblesneeze.rimworlddevgateway",
    "alpha.mod",
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    "alpha.mod",
    ComparisonId = "gateway.alpha-neutral",
    ProductAbsentControlId = "gateway.alpha-absent",
    EvidenceLens = PerformanceEvidenceLens.FullyDisarmed)]
[PerformanceThroughputCheckpoint("colonists-active", 1)]
public sealed class PresentDisarmedBenchmark : NoOpBenchmark { }

[RimWorldPerformanceTest(
    "gateway.alpha-absent",
    "fumblesneeze.rimworlddevgateway",
    "alpha.mod",
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    ComparisonId = "gateway.alpha-neutral-absent",
    EvidenceLens = PerformanceEvidenceLens.ProductAbsentControl)]
[PerformanceThroughputCheckpoint("colonists-active", 1)]
public sealed class AbsentBenchmark : NoOpBenchmark { }

public abstract class NoOpBenchmark : IRimWorldPerformanceTest, IPerformanceThroughputCounter
{
    public void Arrange(IEndToEndContext context) { }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield break;
    }

    public long Read(string id) => 1;
}
