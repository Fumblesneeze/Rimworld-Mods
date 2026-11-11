using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndDebugActionTests
{
    [Test]
    public void Catalog_handle_invokes_exact_immediate_callback_once_and_retains_receipt()
    {
        int invoked = 0;
        var candidate = new Candidate(Snapshot("reload"), () => invoked++);
        var registry = new GatewayDebugActionRegistry(new Source(candidate));
        var catalog = new GatewayEndToEndDebugActionCatalog(registry);
        var descriptor = catalog.Query("Hot reload", new[] { "General" }).Items.Single();
        var driver = new GatewayEndToEndDebugActionDriver(new RejectOtherDriver(), registry);

        var operation = driver.Begin(new NativeDebugActionStep("native reload", descriptor.Handle), Context());
        var outcome = operation.GetOutcome();
        Assert.That(outcome.Passed, Is.True, outcome.FailureMessage);
        Assert.Multiple(() =>
        {
            Assert.That(invoked, Is.EqualTo(1));
            Assert.That(descriptor.SourceType, Is.EqualTo("Verse.DebugActionsMisc"));
            Assert.That(outcome.Artifacts["debugActionHandle"], Is.EqualTo(descriptor.Handle));
            Assert.That(outcome.Artifacts["debugActionPath"], Is.EqualTo("Actions\\Hot reload Defs"));
            Assert.That(outcome.Artifacts["debugActionDelegateReturned"], Is.EqualTo("True"));
        });
    }

    [TestCase(GatewayDebugActionMode.MapPointer)]
    [TestCase(GatewayDebugActionMode.PawnPointer)]
    [TestCase(GatewayDebugActionMode.WorldPointer)]
    public void Pointer_action_is_rejected_before_its_native_callback(GatewayDebugActionMode mode)
    {
        int invoked = 0;
        var registry = new GatewayDebugActionRegistry(new Source(new Candidate(Snapshot("pointer", mode), () => invoked++)));
        string handle = registry.Query(new GatewayDebugActionQuery(null, null, null, null, 10)).Items.Single().Handle;
        var outcome = new GatewayEndToEndDebugActionDriver(new RejectOtherDriver(), registry)
            .Begin(new NativeDebugActionStep("reject pointer", handle), Context()).GetOutcome();
        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.False);
            Assert.That(outcome.FailureCode, Is.EqualTo("debug_action_requires_immediate"));
            Assert.That(invoked, Is.Zero);
        });
    }

    [TestCase("stale", "stale_debug_action_handle")]
    [TestCase("disabled", "debug_action_unavailable")]
    [TestCase("hidden", "debug_action_unavailable")]
    [TestCase("mode-changed", "stale_debug_action_handle")]
    [TestCase("ambiguous", "stale_debug_action_handle")]
    public void Revalidation_failure_never_invokes_the_discovered_or_neighboring_callback(string change, string expectedCode)
    {
        int invoked = 0;
        var candidate = new Candidate(Snapshot("original"), () => invoked++);
        var source = new Source(candidate);
        var registry = new GatewayDebugActionRegistry(source);
        string handle = new GatewayEndToEndDebugActionCatalog(registry).Query().Items.Single().Handle;
        if (change == "stale") candidate.Snapshot = Snapshot("replacement");
        if (change == "disabled") candidate.Snapshot = Snapshot("original", active: false);
        if (change == "hidden") candidate.Snapshot = Snapshot("original", visible: false);
        if (change == "mode-changed") candidate.Snapshot = Snapshot("original", GatewayDebugActionMode.MapPointer);
        if (change == "ambiguous") source.Candidates = new[] { candidate, candidate };
        var outcome = new GatewayEndToEndDebugActionDriver(new RejectOtherDriver(), registry)
            .Begin(new NativeDebugActionStep("revalidate", handle), Context()).GetOutcome();
        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.False);
            Assert.That(outcome.FailureCode, Is.EqualTo(expectedCode));
            Assert.That(invoked, Is.Zero);
        });
    }

    [Test]
    public void Catalog_preserves_bounded_paging_and_excludes_pointer_actions()
    {
        var registry = new GatewayDebugActionRegistry(new Source(
            new Candidate(Snapshot("first"), () => { }), new Candidate(Snapshot("second"), () => { }),
            new Candidate(Snapshot("pointer", GatewayDebugActionMode.MapPointer), () => { })));
        var catalog = new GatewayEndToEndDebugActionCatalog(registry);
        var first = catalog.Query(limit: 1);
        var second = catalog.Query(after: first.Items.Single().Handle, limit: 1);
        Assert.Multiple(() =>
        {
            Assert.That(first.PageTruncated, Is.True);
            Assert.That(second.PageTruncated, Is.False);
            Assert.That(second.Items.Single().Handle, Is.Not.EqualTo(first.Items[0].Handle));
            Assert.That(catalog.Query().Items, Has.Count.EqualTo(2));
            Assert.Throws<GatewayDebugActionException>(() => catalog.Query(limit: 1001));
        });
    }

    [Test]
    public void Unrelated_step_keeps_its_existing_driver_operation()
    {
        var existing = new RecordingDriver();
        var driver = new GatewayEndToEndDebugActionDriver(existing, new GatewayDebugActionRegistry(new Source()));
        var step = new ScreenshotStep("unrelated", Array.Empty<string>(), 0);
        var context = Context();
        Assert.That(driver.Begin(step, context), Is.SameAs(existing.Operation));
        Assert.That(existing.Step, Is.SameAs(step));
        Assert.That(existing.Context, Is.SameAs(context));
    }

    private static GatewayEndToEndTestContext Context() => new(() => 1, () => 1, _ => null);
    [TestCase(false)]
    [TestCase(true)]
    public void Persisted_scenario_boundary_precedes_callback_and_expiry_prevents_dispatch(bool expire)
    {
        int invoked = 0;
        var registry = new GatewayDebugActionRegistry(new Source(new Candidate(Snapshot("workflow"), () => invoked++)));
        var catalog = new GatewayEndToEndDebugActionCatalog(registry);
        var clock = new Clock();
        var descriptor = new GatewayEndToEndRuntimeTestDescriptor("native-debug-workflow", "gateway.test",
            new[] { "ludeon.rimworld", "gateway.test" }, typeof(NativeWorkflow).FullName!, 100, 100, 30, typeof(NativeWorkflow));
        var machine = new GatewayEndToEndExecutionStateMachine(new[] { descriptor }, clock,
            () => new GatewayEndToEndTestContext(() => clock.FrameCount, () => clock.GameTick,
                type => type == typeof(IEndToEndDebugActionCatalog) ? catalog : null),
            new GatewayEndToEndDebugActionDriver(new RejectOtherDriver(), registry), new Isolation());
        for (int i = 0; i < 20 && machine.Snapshot.CurrentStep == null; i++)
        {
            if (machine.PersistencePending) machine.ConfirmPersisted(machine.Snapshot);
            clock.FrameCount++;
            machine.Advance();
        }
        Assert.That(machine.Snapshot.CurrentStep?.Name, Is.EqualTo("native reload"));
        Assert.That(machine.PersistencePending, Is.True);
        machine.Advance();
        Assert.That(invoked, Is.Zero, "An unpersisted action must never invoke its callback.");
        machine.ConfirmPersisted(machine.Snapshot);
        if (expire) clock.UtcNow = clock.UtcNow.AddMinutes(1);
        clock.FrameCount++;
        machine.Advance();
        Assert.That(invoked, Is.EqualTo(expire ? 0 : 1));
        Assert.That(machine.Snapshot.CurrentStep!.Status, Is.EqualTo(expire ? "timed_out" : "passed"));
        if (!expire)
            Assert.That(machine.Snapshot.CurrentStep.Artifacts["debugActionPath"], Is.EqualTo("Actions\\Hot reload Defs"));
    }

    [Test]
    public void Throwing_native_callback_returns_bounded_failure_without_retry_or_private_text()
    {
        int invoked = 0;
        var registry = new GatewayDebugActionRegistry(new Source(new Candidate(Snapshot("throws"), () =>
        {
            invoked++;
            throw new InvalidOperationException("private-native-message");
        })));
        string handle = new GatewayEndToEndDebugActionCatalog(registry).Query().Items.Single().Handle;
        var operation = new GatewayEndToEndDebugActionDriver(new RejectOtherDriver(), registry)
            .Begin(new NativeDebugActionStep("throws", handle), Context());
        var outcome = operation.GetOutcome();
        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.False);
            Assert.That(outcome.FailureCode, Is.EqualTo("native_debug_action_failed"));
            Assert.That(outcome.FailureMessage, Does.Contain("InvalidOperationException"));
            Assert.That(outcome.FailureMessage, Does.Not.Contain("private-native-message"));
            Assert.That(outcome.Artifacts["debugActionHandle"], Is.EqualTo(handle));
            Assert.That(operation.GetOutcome(), Is.SameAs(outcome));
            Assert.That(invoked, Is.EqualTo(1));
        });
    }

    private static GatewayDebugActionCandidateSnapshot Snapshot(string identity,
        GatewayDebugActionMode mode = GatewayDebugActionMode.Immediate, bool active = true, bool visible = true) =>
        new(identity, "Actions\\Hot reload Defs", "Hot reload Defs", "General", "Playing",
            "LudeonTK.DebugActionNode", mode, visible, active, false, "Verse.DebugActionsMisc");

    private sealed class Source(params Candidate[] candidates) : IGatewayDebugActionSource
    {
        public Candidate[] Candidates = candidates;
        public IReadOnlyList<IGatewayDebugActionCandidate> Discover(int maximumCandidates) => Candidates.Take(maximumCandidates).ToArray();
    }
    private sealed class Candidate(GatewayDebugActionCandidateSnapshot snapshot, Action callback) : IGatewayDebugActionCandidate
    {
        public GatewayDebugActionCandidateSnapshot Snapshot = snapshot;
        public GatewayDebugActionCandidateSnapshot Capture() => Snapshot;
        public void Invoke() => callback();
    }
    private sealed class RejectOtherDriver : IGatewayEndToEndStepDriver
    {
        public IGatewayEndToEndStepOperation Begin(EndToEndStep step, IEndToEndContext context)
            => throw new InvalidOperationException("An unrelated driver was called.");
    }
    private sealed class RecordingDriver : IGatewayEndToEndStepDriver
    {
        public readonly IGatewayEndToEndStepOperation Operation = GatewayEndToEndCompletedStepOperation.Passed();
        public EndToEndStep? Step;
        public IEndToEndContext? Context;
        public IGatewayEndToEndStepOperation Begin(EndToEndStep step, IEndToEndContext context)
        { Step = step; Context = context; return Operation; }
    }
    private sealed class Clock : IGatewayEndToEndClock
    {
        public long FrameCount { get; set; }
        public int GameTick => 0;
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
    }
    private sealed class Isolation : IGatewayEndToEndTestIsolation
    {
        public bool IsReady => true;
        public void Prepare(IEndToEndContext context) { }
        public bool Cleanup(IEndToEndContext context) => true;
    }
    public sealed class NativeWorkflow : IRimWorldEndToEndTest
    {
        public void Arrange(IEndToEndContext context) { }
        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            var catalog = (IEndToEndDebugActionCatalog)context.GetService(typeof(IEndToEndDebugActionCatalog))!;
            yield return new NativeDebugActionStep("native reload", catalog.Query().Items.Single().Handle);
        }
    }
}
