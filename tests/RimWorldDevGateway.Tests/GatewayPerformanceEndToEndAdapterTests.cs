using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;
using RimWorldDevGateway.Performance;
using RimWorldDevGateway.PerformanceTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayPerformanceEndToEndAdapterTests
{
    [Test]
    public void Adapter_completes_post_warmup_preparation_before_starting_the_measured_sample()
    {
        PhaseFixtureBenchmark.Calls.Clear();
        var service = new PhaseRecordingService(PhaseFixtureBenchmark.Calls);
        var context = new GatewayEndToEndTestContext(
            () => 0,
            () => 0,
            type => type == typeof(GatewayPerformanceRunService) ? service : null);
        var adapter = new GatewayPerformanceEndToEndAdapter(
            PerformanceTestContract.Describe(typeof(PhaseFixtureBenchmark)));

        adapter.Arrange(context);
        using var execution = adapter.Execute(context);
        while (execution.MoveNext()) { }

        Assert.That(PhaseFixtureBenchmark.Calls, Is.EqualTo(new[]
        {
            "arrange", "warmup", "prepare-sample", "begin-sample", "execute", "complete-sample",
            "validate-sample", "post-sample-evidence"
        }));
    }

    [TestCase(599, 600, true)]
    [TestCase(600, 600, false)]
    [TestCase(601, 600, false)]
    [TestCase(601, -1, true)]
    public void Tick_boundary_gate_allows_only_native_ticks_before_the_exact_target(
        int current,
        int target,
        bool expected)
    {
        Assert.That(PerformanceTickBoundaryGate.ShouldRunTick(current, target), Is.EqualTo(expected));
    }

    [Test]
    public void Coordinated_service_uses_native_time_steps_and_bounds_the_runtime_session_around_the_workload()
    {
        var calls = new List<string>();
        var backend = new RecordingBackend(calls);
        var timestamp = 1000L;
        var memory = 2000L;
        var collections = new[] { 1, 2, 3 };
        var tick = 10;
        var service = new CoordinatedGatewayPerformanceRunService(
            backend,
            () => timestamp,
            () => memory,
            generation => collections[generation]);
        var context = new GatewayEndToEndTestContext(() => 0, () => tick, _ => null);
        var descriptor = PerformanceTestContract.Describe(typeof(ShortFixtureBenchmark));

        service.Prepare(descriptor, context);
        using var warmup = service.BeginWarmUp(descriptor, context).GetEnumerator();
        Assert.That(warmup.MoveNext(), Is.True);
        Assert.That(warmup.Current, Is.TypeOf<TimeControlActionStep>());
        Assert.That(calls, Is.EqualTo(new[] { "prepare", "arm:40" }));
        Assert.That(warmup.MoveNext(), Is.True);
        Assert.That(warmup.Current, Is.TypeOf<WaitUntilStep>());
        tick = 40;
        Assert.That(((WaitUntilStep)warmup.Current).Predicate(context), Is.True);
        Assert.That(warmup.MoveNext(), Is.True);
        Assert.That(warmup.Current, Is.TypeOf<TimeControlActionStep>());
        Assert.That(warmup.MoveNext(), Is.False);
        Assert.That(calls, Is.EqualTo(new[] { "prepare", "arm:40", "confirm:40", "disarm" }));

        var begin = service.BeginSample(descriptor, context).GetEnumerator();
        Assert.That(begin.MoveNext(), Is.True);
        Assert.That(begin.Current, Is.TypeOf<ScreenshotStep>());
        Assert.That(calls, Is.EqualTo(new[] { "prepare", "arm:40", "confirm:40", "disarm" }));
        Assert.That(begin.MoveNext(), Is.True);
        Assert.That(begin.Current, Is.TypeOf<TimeControlActionStep>());
        Assert.That(calls, Is.EqualTo(new[]
        {
            "prepare", "arm:40", "confirm:40", "disarm", "start", "arm:100"
        }));

        tick = 100;
        timestamp = 1600;
        memory = 2600;
        collections = new[] { 2, 4, 6 };
        var complete = service.CompleteSample(descriptor, context).GetEnumerator();
        Assert.That(complete.MoveNext(), Is.True);
        Assert.That(complete.Current, Is.TypeOf<WaitUntilStep>());
        Assert.That(((WaitUntilStep)complete.Current).Predicate(context), Is.True);
        Assert.That(complete.MoveNext(), Is.True);
        Assert.That(complete.Current, Is.TypeOf<TimeControlActionStep>());
        Assert.That(calls.Last(), Is.EqualTo("arm:100"));
        Assert.That(complete.MoveNext(), Is.True);
        Assert.That(complete.Current, Is.TypeOf<WaitUntilStep>());
        Assert.That(((WaitUntilStep)complete.Current).Predicate(context), Is.True);
        Assert.That(complete.MoveNext(), Is.True);
        Assert.That(complete.Current, Is.TypeOf<CheckpointStep>());

        Assert.Multiple(() =>
        {
            Assert.That(calls, Is.EqualTo(new[]
            {
                "prepare", "arm:40", "confirm:40", "disarm", "start", "arm:100",
                "confirm:100", "disarm", "complete"
            }));
            Assert.That(backend.Window!.StartGameTick, Is.EqualTo(40));
            Assert.That(backend.Window.EndGameTick, Is.EqualTo(100));
            Assert.That(backend.Window.ManagedMemoryStartBytes, Is.EqualTo(2000));
            Assert.That(backend.Window.ManagedMemoryEndBytes, Is.EqualTo(2600));
            Assert.That(((CheckpointStep)complete.Current).Capture(context)["normalized"],
                Is.EqualTo("normalized.json"));
        });
        Assert.That(complete.MoveNext(), Is.True);
        Assert.That(complete.Current, Is.TypeOf<ScreenshotStep>());
        Assert.That(complete.MoveNext(), Is.False);
        service.Cleanup();
        Assert.That(calls.Last(), Is.EqualTo("cleanup"));
    }

    [Test]
    public void Coordinated_service_rejects_a_tick_window_that_overshoots_its_exact_boundary()
    {
        var backend = new RecordingBackend(new List<string>());
        var tick = 10;
        var service = new CoordinatedGatewayPerformanceRunService(backend, () => 0, () => 0, _ => 0);
        var context = new GatewayEndToEndTestContext(() => 0, () => tick, _ => null);
        var descriptor = PerformanceTestContract.Describe(typeof(ShortFixtureBenchmark));

        service.Prepare(descriptor, context);
        using var begin = service.BeginSample(descriptor, context).GetEnumerator();
        Assert.That(begin.MoveNext(), Is.True);
        Assert.That(begin.Current, Is.TypeOf<ScreenshotStep>());
        Assert.That(begin.MoveNext(), Is.True);
        tick = 71;
        using var complete = service.CompleteSample(descriptor, context).GetEnumerator();
        Assert.That(complete.MoveNext(), Is.True);
        Assert.That(((WaitUntilStep)complete.Current).Predicate(context), Is.True);
        Assert.That(complete.MoveNext(), Is.True);

        var error = Assert.Throws<InvalidOperationException>(() => complete.MoveNext());
        Assert.That(error!.Message, Does.Contain("exact tick boundary").And.Contain("70").And.Contain("71"));
        service.Cleanup();
    }

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
            Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Completed));
            Assert.That(calls, Is.EqualTo(new[] { "prepare", "cleanup" }));
        });
    }

    [Test]
    public void Throughput_is_measured_as_sample_delta_and_warmup_counts_cannot_satisfy_the_checkpoint()
    {
        var backend = new RecordingBackend(new List<string>());
        var counter = new MutableCounter { Value = 100 };
        var tick = 10;
        var service = new CoordinatedGatewayPerformanceRunService(
            backend,
            () => 1_000,
            () => 2_000,
            _ => 0);
        var descriptor = PerformanceTestContract.Describe(typeof(ThroughputFixtureBenchmark));

        var contextWithCounter = new GatewayEndToEndTestContext(
            () => 0,
            () => tick,
            type => type == typeof(IPerformanceThroughputCounter) ? counter : null);
        service.Prepare(descriptor, contextWithCounter);
        using var begin = service.BeginSample(descriptor, contextWithCounter).GetEnumerator();
        Assert.That(begin.MoveNext(), Is.True);
        Assert.That(begin.Current, Is.TypeOf<ScreenshotStep>());
        Assert.That(begin.MoveNext(), Is.True);
        tick = 70;
        counter.Value = 104;
        using var complete = service.CompleteSample(descriptor, contextWithCounter).GetEnumerator();
        Assert.That(complete.MoveNext(), Is.True);
        Assert.That(((WaitUntilStep)complete.Current).Predicate(contextWithCounter), Is.True);
        Assert.That(complete.MoveNext(), Is.True);

        var exception = Assert.Throws<InvalidOperationException>(() => complete.MoveNext());
        Assert.That(exception!.Message, Does.Contain("observed 4").And.Contain("at least 5"));
        Assert.That(backend.ThroughputCounts, Is.Null);
        service.Cleanup();
    }

    [Test]
    public void Coordinated_cleanup_retains_a_pending_backend_until_async_ownership_is_confirmed()
    {
        var backend = new PendingCleanupBackend();
        var service = new CoordinatedGatewayPerformanceRunService(backend);
        var descriptor = PerformanceTestContract.Describe(typeof(ShortFixtureBenchmark));
        var context = new GatewayEndToEndTestContext(() => 0, () => 0, _ => null);
        service.Prepare(descriptor, context);

        Assert.Multiple(() =>
        {
            Assert.That(service.TryCleanup(), Is.False);
            Assert.That(service.TryCleanup(), Is.False);
            Assert.That(service.TryCleanup(), Is.True);
            Assert.That(backend.Attempts, Is.EqualTo(3));
            Assert.That(() => service.Prepare(descriptor, context), Throws.Nothing,
                "Confirmed cleanup must release the coordinated service for a later run.");
        });
        Assert.That(service.TryCleanup(), Is.True);
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

    private sealed class PhaseRecordingService : GatewayPerformanceRunService
    {
        private readonly IList<string> calls;

        public PhaseRecordingService(IList<string> calls) => this.calls = calls;

        public override void Prepare(PerformanceTestDescriptor descriptor, IEndToEndContext context) { }

        public override IEnumerable<EndToEndStep> BeginWarmUp(
            PerformanceTestDescriptor descriptor,
            IEndToEndContext context)
        {
            calls.Add("warmup");
            yield break;
        }

        public override IEnumerable<EndToEndStep> BeginSample(
            PerformanceTestDescriptor descriptor,
            IEndToEndContext context)
        {
            calls.Add("begin-sample");
            yield break;
        }

        public override IEnumerable<EndToEndStep> CompleteSample(
            PerformanceTestDescriptor descriptor,
            IEndToEndContext context)
        {
            calls.Add("complete-sample");
            yield break;
        }

        public override void Cleanup() { }
    }

    private sealed class RecordingBackend : IGatewayPerformanceRuntimeBackend
    {
        private readonly IList<string> calls;
        public RecordingBackend(IList<string> calls) => this.calls = calls;
        public GatewayPerformanceControlWindow? Window { get; private set; }
        public IReadOnlyDictionary<string, long>? ThroughputCounts { get; private set; }
        public void Prepare(PerformanceTestDescriptor descriptor) => calls.Add("prepare");
        public void Start(PerformanceTestDescriptor descriptor) => calls.Add("start");
        public void ArmTickBoundary(int gameTick) => calls.Add("arm:" + gameTick);
        public void ConfirmTickBoundary(int gameTick)
        {
            calls.Add("confirm:" + gameTick);
            var arm = calls.Last(item => item.StartsWith("arm:", StringComparison.Ordinal));
            var expected = int.Parse(arm.Substring(4));
            if (gameTick != expected)
                throw new InvalidOperationException(
                    $"Performance window expected exact tick boundary {expected}, observed {gameTick}.");
        }
        public void DisarmTickBoundary() => calls.Add("disarm");
        public IReadOnlyDictionary<string, string> Complete(
            PerformanceTestDescriptor descriptor,
            GatewayPerformanceControlWindow controlWindow,
            IReadOnlyDictionary<string, long> throughputCounts)
        {
            calls.Add("complete");
            Window = controlWindow;
            ThroughputCounts = throughputCounts;
            return new Dictionary<string, string> { ["normalized"] = "normalized.json" };
        }
        public bool TryFinalize(out string reason)
        {
            reason = string.Empty;
            return true;
        }
        public void Cleanup() => calls.Add("cleanup");
    }

    [Test]
    public void Performance_adapter_supplies_the_dynamically_loaded_fixture_counter()
    {
        var service = new CoordinatedGatewayPerformanceRunService(
            new RecordingBackend(new List<string>()),
            () => 1_000,
            () => 2_000,
            _ => 0);
        var context = new GatewayEndToEndTestContext(
            () => 0,
            () => 0,
            type => type == typeof(GatewayPerformanceRunService) ? service : null);
        var adapter = new GatewayPerformanceEndToEndAdapter(
            PerformanceTestContract.Describe(typeof(SelfCountingFixtureBenchmark)));

        adapter.Arrange(context);
        using var execution = adapter.Execute(context);

        Assert.That(execution.MoveNext(), Is.True);
        Assert.That(execution.Current, Is.TypeOf<ScreenshotStep>());
        Assert.That(() => execution.MoveNext(), Throws.Nothing,
            "The adapter must make its dynamically loaded fixture counter available at sample start.");
        Assert.That(execution.Current, Is.TypeOf<TimeControlActionStep>());
    }

    private sealed class MutableCounter : IPerformanceThroughputCounter
    {
        public long Value { get; set; }
        public long Read(string id) => Value;
    }

    private sealed class PendingCleanupBackend :
        IGatewayPerformanceRuntimeBackend,
        IDeferredGatewayPerformanceCleanup
    {
        public int Attempts { get; private set; }
        public void Prepare(PerformanceTestDescriptor descriptor) { }
        public void Start(PerformanceTestDescriptor descriptor) { }
        public void ArmTickBoundary(int gameTick) { }
        public void ConfirmTickBoundary(int gameTick) { }
        public void DisarmTickBoundary() { }
        public IReadOnlyDictionary<string, string> Complete(
            PerformanceTestDescriptor descriptor,
            GatewayPerformanceControlWindow controlWindow,
            IReadOnlyDictionary<string, long> throughputCounts) =>
            new Dictionary<string, string>();
        public bool TryFinalize(out string reason) { reason = string.Empty; return true; }
        public bool TryCleanup(out string reason)
        {
            Attempts++;
            reason = Attempts < 3 ? "pending" : string.Empty;
            return Attempts >= 3;
        }
        public void Cleanup()
        {
            if (!TryCleanup(out var reason)) throw new InvalidOperationException(reason);
        }
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

    [RimWorldPerformanceTest(
        "gateway.phase-adapter-fixture",
        EndToEndTestContract.GatewayPackageId,
        EndToEndTestContract.GatewayPackageId,
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId,
        ComparisonId = "gateway.phase-adapter-fixture")]
    public sealed class PhaseFixtureBenchmark :
        IRimWorldPerformanceTest,
        IPerformanceSamplePreparation,
        IPerformanceSampleValidation,
        IPerformancePostSampleEvidence
    {
        public static List<string> Calls { get; } = new();

        public void Arrange(IEndToEndContext context) => Calls.Add("arrange");

        public IEnumerator<EndToEndStep> PrepareSample(IEndToEndContext context)
        {
            Calls.Add("prepare-sample");
            yield break;
        }

        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            Calls.Add("execute");
            yield break;
        }

        public void ValidateSample(IEndToEndContext context) => Calls.Add("validate-sample");

        public IEnumerator<EndToEndStep> CapturePostSampleEvidence(IEndToEndContext context)
        {
            Calls.Add("post-sample-evidence");
            yield break;
        }
    }

    [RimWorldPerformanceTest(
        "gateway.short-adapter-fixture",
        EndToEndTestContract.GatewayPackageId,
        EndToEndTestContract.GatewayPackageId,
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId,
        ComparisonId = "gateway.short-adapter-fixture",
        WarmUpTicks = 30,
        SampleTicks = 60)]
    public sealed class ShortFixtureBenchmark : IRimWorldPerformanceTest
    {
        public void Arrange(IEndToEndContext context) { }
        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
            Enumerable.Empty<EndToEndStep>().GetEnumerator();
    }

    [RimWorldPerformanceTest(
        "gateway.throughput-adapter-fixture",
        EndToEndTestContract.GatewayPackageId,
        EndToEndTestContract.GatewayPackageId,
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId,
        ComparisonId = "gateway.throughput-adapter-fixture",
        WarmUpTicks = 0,
        SampleTicks = 60)]
    [PerformanceThroughputCheckpoint("native-actions", 5)]
    public sealed class ThroughputFixtureBenchmark :
        IRimWorldPerformanceTest,
        IPerformanceThroughputCounter
    {
        public void Arrange(IEndToEndContext context) { }
        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
            Enumerable.Empty<EndToEndStep>().GetEnumerator();
        public long Read(string id) => 0;
    }

    [RimWorldPerformanceTest(
        "gateway.self-counting-adapter-fixture",
        EndToEndTestContract.GatewayPackageId,
        EndToEndTestContract.GatewayPackageId,
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId,
        ComparisonId = "gateway.self-counting-adapter-fixture",
        WarmUpTicks = 0,
        SampleTicks = 60)]
    [PerformanceThroughputCheckpoint("native-actions", 5)]
    public sealed class SelfCountingFixtureBenchmark :
        IRimWorldPerformanceTest,
        IPerformanceThroughputCounter
    {
        public void Arrange(IEndToEndContext context) { }
        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
            Enumerable.Empty<EndToEndStep>().GetEnumerator();
        public long Read(string id) => 7;
    }
}
