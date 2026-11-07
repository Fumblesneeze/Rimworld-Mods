using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Waylith.Tests;

public sealed class GeneratedMapIsolationTests
{
    [Test]
    public void Cleanup_refuses_to_destroy_a_new_holder_of_baseline_contents()
    {
        var weapon = new object();
        var tracker = new object();
        var pawn = new object();
        var parents = new Dictionary<object, object> { [weapon] = tracker, [tracker] = pawn };
        object? Parent(object value) => parents.GetValueOrDefault(value);
        Assert.Throws<InvalidOperationException>(() => VerseGatewayEndToEndIsolationOperations.EnsureSafeRemoval(
            new[] { pawn }, new[] { weapon }, Parent));
        Assert.DoesNotThrow(() => VerseGatewayEndToEndIsolationOperations.EnsureSafeRemoval(
            new[] { weapon }, new[] { pawn }, Parent));
    }

    private sealed class NaturalFixture : EmptyFixture, IEndToEndGeneratedMapFixture { }
    private sealed class ProcessFixture : EmptyFixture, IEndToEndProcessOwnedGameFixture { }

    [TestCase("none")]
    [TestCase("verification")]
    [TestCase("deferred")]
    public void Process_owned_dispatch_and_cleanup_failures_are_observed_by_the_execution_machine(string failure)
    {
        var operations = new RecordingOperations { FailCleanupVerification = failure == "verification" };
        var fixture = new ProcessFixture { FailDeferredCleanup = failure == "deferred" };
        var machine = Machine(new GatewayEndToEndTestIsolation(operations), Descriptor("one", fixture));
        for (int i = 0; i < 100 && !machine.Snapshot.IsTerminal; i++)
        {
            if (machine.PersistencePending) machine.ConfirmPersisted(machine.Snapshot);
            machine.Advance();
        }
        Assert.That(machine.Snapshot.IsTerminal, Is.True);
        Assert.That(operations.Calls[0], Is.EqualTo("process"));
        Assert.That(fixture.Arranged, Is.True);
        Assert.That(machine.Snapshot.Results.Single().Status, Is.EqualTo(failure == "none" ? "passed" : "infrastructure_failed"));
    }

    [Test]
    public void Mixed_process_owned_selection_fails_before_any_preparation()
    {
        var operations = new RecordingOperations();
        var fixture = new ProcessFixture();
        Assert.Throws<EndToEndContractException>(() => Machine(new GatewayEndToEndTestIsolation(operations),
            Descriptor("one", fixture), Descriptor("two", new EmptyFixture())));
        Assert.That(operations.Calls, Is.Empty);
        Assert.That(fixture.Arranged, Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Execution_machine_selects_the_declared_isolation_before_arrangement(bool generated)
    {
        var operations = new RecordingOperations();
        var isolation = new GatewayEndToEndTestIsolation(operations);
        var fixture = generated ? new NaturalFixture() : new EmptyFixture();
        var machine = Machine(isolation, Descriptor("one", fixture));
        machine.Advance();
        Assert.That(operations.Calls, Is.Empty);
        machine.ConfirmPersisted(machine.Snapshot);
        machine.Advance();
        Assert.That(operations.Calls[0], Is.EqualTo(generated ? "generated" : "empty"));
        Assert.That(fixture.Arranged, Is.True);
    }

    [Test]
    public void Execution_machine_rejects_mixed_generated_groups_before_preparation_or_arrangement()
    {
        var operations = new RecordingOperations();
        var fixture = new NaturalFixture();
        Assert.Throws<EndToEndContractException>(() => Machine(new GatewayEndToEndTestIsolation(operations),
            Descriptor("one", fixture), Descriptor("two", new EmptyFixture())));
        Assert.That(operations.Calls, Is.Empty);
        Assert.That(fixture.Arranged, Is.False);
    }

    private static GatewayEndToEndRuntimeTestDescriptor Descriptor(string id, EmptyFixture fixture) =>
        new(id, "alpha.mod", new[] { "ludeon.rimworld", "alpha.mod" }, fixture.GetType().FullName!,
            100, 100, 30, fixture.GetType(), () => fixture, null);
    private static GatewayEndToEndExecutionStateMachine Machine(IGatewayEndToEndTestIsolation isolation,
        params GatewayEndToEndRuntimeTestDescriptor[] descriptors) => new(descriptors, new Clock(),
            () => new GatewayEndToEndTestContext(() => 0, () => 0, _ => null), new Driver(), isolation);
    private class EmptyFixture : IRimWorldEndToEndTest
    {
        internal bool Arranged;
        internal bool FailDeferredCleanup;
        public void Arrange(IEndToEndContext context)
        {
            Arranged = true;
            if (FailDeferredCleanup) context.DeferCleanup(() => throw new InvalidOperationException("cleanup test"));
        }
        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) { yield break; }
    }
    private sealed class Clock : IGatewayEndToEndClock
    {
        public long FrameCount => 0;
        public int GameTick => 0;
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch;
    }
    private sealed class Driver : IGatewayEndToEndStepDriver
    {
        public IGatewayEndToEndStepOperation Begin(EndToEndStep step, IEndToEndContext context) =>
            GatewayEndToEndCompletedStepOperation.Passed();
    }

    [Test]
    public void Generated_map_prepare_and_cleanup_keep_the_normal_control_lifecycle()
    {
        var operations = new RecordingOperations();
        var isolation = new GatewayEndToEndTestIsolation(operations);
        var context = new GatewayEndToEndTestContext(() => 0, () => 0, _ => null);
        isolation.PrepareGeneratedMap(context);
        Assert.That(isolation.Cleanup(context), Is.True);
        Assert.That(operations.Calls, Is.EqualTo(new[] { "generated", "pause", "reset", "verify", "pause", "reset", "verify", "restore", "verify" }));
        operations.Calls.Clear();
        isolation.Prepare(context);
        Assert.That(operations.Calls[0], Is.EqualTo("empty"));
        Assert.That(isolation.Cleanup(context), Is.True);
    }

    private sealed class RecordingOperations : IGatewayEndToEndIsolationOperations, IGatewayEndToEndGeneratedMapIsolationOperations, IGatewayEndToEndProcessOwnedGameIsolationOperations
    {
        public readonly List<string> Calls = new();
        public bool FailCleanupVerification;
        private int verifications;
        public bool IsReady => true;
        public GatewayEndToEndIsolationBaseline CaptureBaseline() { Calls.Add("empty"); return new(new object()); }
        public GatewayEndToEndIsolationBaseline CaptureGeneratedMapBaseline() { Calls.Add("generated"); return new(new object()); }
        public GatewayEndToEndIsolationBaseline CaptureProcessOwnedGameBaseline() { Calls.Add("process"); return new(new object()); }
        public void Pause() => Calls.Add("pause");
        public void ResetTransientState() => Calls.Add("reset");
        public bool VerifyEmpty() { Calls.Add("verify"); return !FailCleanupVerification || ++verifications == 1; }
        public bool RestoreBaseline(GatewayEndToEndIsolationBaseline value) { Calls.Add("restore"); return true; }
    }

    [Test]
    public void Generated_map_policy_is_explicit_and_refuses_shared_processes()
    {
        Assert.That(EndToEndTestContract.UsesGeneratedMap(typeof(object), 2), Is.False);
        Assert.That(EndToEndTestContract.UsesGeneratedMap(typeof(NaturalFixture), 1), Is.True);
        Assert.Throws<EndToEndContractException>(() => EndToEndTestContract.UsesGeneratedMap(typeof(NaturalFixture), 2));
    }
}
