using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;
using RimWorldDevGateway.Performance;
using RimWorldDevGateway.PerformanceTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayPerformanceEndToEndAdapterTests
{
    [Test]
    public void Prepare_failure_still_registers_and_runs_performance_cleanup()
    {
        var calls = new List<string>();
        var service = new FailingPrepareService(calls);
        var context = new GatewayEndToEndTestContext(
            () => 0,
            () => 0,
            type => type == typeof(GatewayPerformanceRunService) ? service : null);
        var adapter = new GatewayPerformanceEndToEndAdapter(
            PerformanceTestContract.Describe(typeof(FixtureBenchmark)));

        Assert.Throws<InvalidOperationException>(() => adapter.Arrange(context));
        Assert.Multiple(() =>
        {
            Assert.That(context.RunDeferredCleanup(), Is.True);
            Assert.That(calls, Is.EqualTo(new[] { "prepare", "cleanup" }));
        });
    }

    private sealed class FailingPrepareService : GatewayPerformanceRunService
    {
        private readonly IList<string> calls;

        public FailingPrepareService(IList<string> calls) => this.calls = calls;

        public override void Prepare(PerformanceTestDescriptor descriptor, IEndToEndContext context)
        {
            calls.Add("prepare");
            throw new InvalidOperationException("expected");
        }

        public override IEnumerable<EndToEndStep> BeginWarmUp(
            PerformanceTestDescriptor descriptor,
            IEndToEndContext context) => Array.Empty<EndToEndStep>();

        public override IEnumerable<EndToEndStep> BeginSample(
            PerformanceTestDescriptor descriptor,
            IEndToEndContext context) => Array.Empty<EndToEndStep>();

        public override IEnumerable<EndToEndStep> CompleteSample(
            PerformanceTestDescriptor descriptor,
            IEndToEndContext context) => Array.Empty<EndToEndStep>();

        public override void Cleanup() => calls.Add("cleanup");
    }

    [RimWorldPerformanceTest(
        "gateway.adapter-fixture",
        EndToEndTestContract.GatewayPackageId,
        EndToEndTestContract.GatewayPackageId,
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId,
        ComparisonId = "gateway.adapter-fixture")]
    public sealed class FixtureBenchmark : IRimWorldPerformanceTest
    {
        public void Arrange(IEndToEndContext context) { }
        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
            Enumerable.Empty<EndToEndStep>().GetEnumerator();
    }
}
