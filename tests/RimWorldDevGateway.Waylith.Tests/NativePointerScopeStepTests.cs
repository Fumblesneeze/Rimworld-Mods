using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativePointerScopeStepTests
{
    [TestCase("scopeHandle", "foreign-scope")]
    [TestCase("cleanupComplete", false)]
    [TestCase("pointerRestored", false)]
    [TestCase("pointerScopeState", "cleanup-incomplete")]
    public void IncompleteRestorationFailsBothExplicitEndAndDeferredCleanup(string field, object value)
    {
        var registry = new GatewayAutomationRegistry();
        RegisterBegin(registry);
        Register(registry, "native-pointer-scope.end", (_, _) =>
        {
            var receipt = Restored().ToDictionary(pair => pair.Key, pair => pair.Value);
            receipt[field] = value;
            return receipt;
        });
        var context = Context();
        var driver = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry);
        driver.Begin(new NativePointerScopeBeginStep("begin", _ => { }), context);
        var result = driver.Begin(new NativePointerScopeEndStep("end", "scope-1"), context).GetOutcome();
        Assert.That(result.Passed, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("native_pointer_scope_cleanup_failed"));
        Assert.That(result.Artifacts["pointerScopeReceipt"], Does.Contain(field));
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Failed));
    }

    [Test]
    public void FailedBeginStillRestoresTheScopeIdentifiedByItsRetainedAdmissionArtifact()
    {
        var registry = new GatewayAutomationRegistry();
        Register(registry, "native-pointer-scope.begin", (run, args) =>
        {
            run.AddArtifact("native-pointer-scope", "native-input-scope", new Dictionary<string, object?>
                { ["scopeHandle"] = "scope-1", ["deadlineUtc"] = args["deadlineUtc"] });
            throw new GatewayAutomationException("late_admission_failure", "Failure after admission.");
        });
        int ends = 0;
        Register(registry, "native-pointer-scope.end", (_, _) => { ends++; return Restored(); });
        var context = Context();
        var driver = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry);
        var result = driver.Begin(new NativePointerScopeBeginStep("begin", _ =>
            throw new AssertionException("A failed admission reached the scenario.")), context).GetOutcome();
        Assert.That(result.FailureCode, Is.EqualTo("late_admission_failure"));
        Assert.That(result.Artifacts["pointerScopeHandle"], Is.EqualTo("scope-1"));
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Completed));
        Assert.That(ends, Is.EqualTo(1));
    }

    [Test]
    public void LaterScenarioFailureRetainsScopeRestorationEvidenceOnTheOriginalBeginStep()
    {
        var clock = new TestClock();
        var registry = new GatewayAutomationRegistry();
        RegisterBegin(registry);
        Register(registry, "native-pointer-scope.end", (_, _) => Restored());
        var descriptor = new GatewayEndToEndRuntimeTestDescriptor("pointer-failure", "test.owner",
            new[] { "ludeon.rimworld", "test.owner" }, typeof(FailedScopeScenario).FullName!, 100, 100, 30,
            typeof(FailedScopeScenario));
        var machine = new GatewayEndToEndExecutionStateMachine(new[] { descriptor }, clock,
            () => new GatewayEndToEndTestContext(() => clock.FrameCount, () => clock.GameTick, _ => null),
            new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry), new TestIsolation { Ready = true });
        for (int i = 0; i < 40 && !machine.Snapshot.IsTerminal; i++)
        {
            if (machine.PersistencePending) machine.ConfirmPersisted(machine.Snapshot);
            clock.Frame++;
            registry.Advance();
            machine.Advance();
        }
        var result = machine.Snapshot.Results.Single();
        Assert.That(result.Status, Is.EqualTo("failed"));
        Assert.That(result.CleanupState, Is.EqualTo("passed"));
        Assert.That(result.Steps[0].Artifacts!.TryGetValue("pointerScopeCleanupReceipt", out var receipt), Is.True);
        Assert.That(receipt, Does.Contain("\"pointerRestored\":true"));
    }

    [Test]
    public void AnotherWorkflowStartsOnlyAfterConfirmedRestorationWithoutRepeatingOldCleanup()
    {
        var context = Context();
        var registry = new GatewayAutomationRegistry();
        int begins = 0, ends = 0;
        Register(registry, "native-pointer-scope.begin", (_, args) => new Dictionary<string, object?>
            { ["scopeHandle"] = "scope-" + ++begins, ["deadlineUtc"] = args["deadlineUtc"] });
        Register(registry, "native-pointer-scope.end", (_, args) =>
        {
            ends++;
            return new Dictionary<string, object?> { ["scopeHandle"] = args["scopeHandle"],
                ["cleanupComplete"] = true, ["pointerRestored"] = true, ["pointerScopeState"] = "closed" };
        });
        var driver = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry);
        var begin = new NativePointerScopeBeginStep("begin", _ => { });
        Assert.That(driver.Begin(begin, context).GetOutcome().Passed, Is.True);
        Assert.That(driver.Begin(begin, context).GetOutcome().FailureCode, Is.EqualTo("native_pointer_scope_already_owned"));
        Assert.That(begins, Is.EqualTo(1));
        Assert.That(driver.Begin(new NativePointerScopeEndStep("end", "scope-1"), context).GetOutcome().Passed, Is.True);
        Assert.That(driver.Begin(begin, context).GetOutcome().Passed, Is.True);
        Assert.That(begins, Is.EqualTo(2));
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Completed));
        Assert.That(ends, Is.EqualTo(2));
    }

    [Test]
    public void PointerWorkflowKeepsTheOriginalDeadlineAfterWaitingForPlayerControl()
    {
        var clock = new TestClock();
        var isolation = new TestIsolation();
        var registry = new GatewayAutomationRegistry();
        string? observedDeadline = null;
        Register(registry, "native-pointer-scope.begin", (_, args) =>
        {
            observedDeadline = (string)args["deadlineUtc"]!;
            return new Dictionary<string, object?> { ["scopeHandle"] = "scope-1", ["deadlineUtc"] = observedDeadline };
        });
        Register(registry, "native-pointer-scope.end", (_, _) => Restored());
        var expectedDeadline = clock.UtcNow.AddSeconds(30);
        var descriptor = new GatewayEndToEndRuntimeTestDescriptor("pointer-deadline", "test.owner",
            new[] { "ludeon.rimworld", "test.owner" }, typeof(ScopeScenario).FullName!, 100, 100, 30, typeof(ScopeScenario));
        var machine = new GatewayEndToEndExecutionStateMachine(new[] { descriptor }, clock,
            () => new GatewayEndToEndTestContext(() => clock.FrameCount, () => clock.GameTick, _ => null),
            new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry), isolation);
        machine.Advance();
        machine.ConfirmPersisted(machine.Snapshot);
        clock.Seconds = 10;
        machine.Advance();
        Assert.That(observedDeadline, Is.Null);
        isolation.Ready = true;
        for (int i = 0; i < 40 && !machine.Snapshot.IsTerminal; i++)
        {
            if (machine.PersistencePending) machine.ConfirmPersisted(machine.Snapshot);
            clock.Frame++;
            registry.Advance();
            machine.Advance();
        }
        Assert.That(observedDeadline, Is.EqualTo(expectedDeadline.ToString("O")));
        Assert.That(machine.Snapshot.Results.Single().Status, Is.EqualTo("passed"));
    }

    [Test]
    public void ScopedClickUsesVersionTwoAndOnlyTheOwningScenarioMayUseTheHandle()
    {
        var registry = new GatewayAutomationRegistry();
        RegisterBegin(registry);
        int clicks = 0;
        IReadOnlyDictionary<string, object?>? arguments = null;
        Register(registry, "native-window.click", (_, args) =>
        {
            clicks++;
            arguments = args;
            return new Dictionary<string, object?> { ["cleanupComplete"] = true,
                ["pointerScopeHandle"] = "scope-1", ["pointerScopeState"] = "retained", ["pointerRestored"] = false };
        });
        var context = Context();
        var driver = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry);
        driver.Begin(new NativePointerScopeBeginStep("begin", _ => { }), context);
        var click = new NativeWindowClickActionStep("choose", "window-1", 90, 80, null, "scope-1");
        Assert.That(driver.Begin(click, Context()).GetOutcome().FailureCode, Is.EqualTo("native_pointer_scope_not_owned"));
        Assert.That(clicks, Is.Zero);
        Assert.That(driver.Begin(click, context).GetOutcome().Passed, Is.True);
        Assert.That(clicks, Is.EqualTo(1));
        Assert.That(arguments!["version"], Is.EqualTo(2));
        Assert.That(arguments["pointerScopeHandle"], Is.EqualTo("scope-1"));
    }

    [Test]
    public void ExplicitEndWaitsForRestorationAndDeferredCleanupDoesNotEndTwice()
    {
        var context = Context();
        var registry = new GatewayAutomationRegistry();
        var pending = new PendingRestoration();
        int ends = 0;
        RegisterBegin(registry);
        Register(registry, "native-pointer-scope.end", (_, args) =>
        {
            ends++;
            Assert.That(args["scopeHandle"], Is.EqualTo("scope-1"));
            return pending;
        });
        var driver = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry);
        Assert.That(driver.Begin(new NativePointerScopeBeginStep("begin", _ => { }), context).GetOutcome().Passed, Is.True);
        var end = driver.Begin(new NativePointerScopeEndStep("restore", "scope-1"), context);
        Assert.That(end.IsCompleted, Is.False);
        registry.Advance();
        Assert.That(end.IsCompleted, Is.False);
        pending.Complete = true;
        registry.Advance();
        Assert.That(end.IsCompleted, Is.True);
        Assert.That(end.GetOutcome().Passed, Is.True);
        Assert.That(end.GetOutcome().Artifacts["pointerScopeReceipt"], Does.Contain("\"pointerRestored\":true"));
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Completed));
        Assert.That(ends, Is.EqualTo(1));
    }

    [Test]
    public void BeginRetainsTheScenarioDeadlineAndCleansUpWhenTheReceiverThrows()
    {
        var deadline = DateTimeOffset.UtcNow.AddMinutes(1);
        var context = new GatewayEndToEndTestContext(() => 0, () => 0, _ => null)
            { WorkflowDeadlineUtc = deadline };
        var registry = new GatewayAutomationRegistry();
        int begins = 0, ends = 0;
        string? receivedDeadline = null;
        Register(registry, "native-pointer-scope.begin", (run, args) =>
        {
            begins++;
            receivedDeadline = (string)args["deadlineUtc"]!;
            var receipt = new Dictionary<string, object?>
                { ["scopeHandle"] = "scope-1", ["deadlineUtc"] = receivedDeadline };
            run.AddArtifact("native-pointer-scope", "native-input-scope", receipt);
            return receipt;
        });
        Register(registry, "native-pointer-scope.end", (_, args) =>
        {
            ends++;
            Assert.That(args["scopeHandle"], Is.EqualTo("scope-1"));
            return new Dictionary<string, object?> { ["scopeHandle"] = "scope-1", ["cleanupComplete"] = true,
                ["pointerRestored"] = true, ["pointerScopeState"] = "closed", ["deadlineUtc"] = receivedDeadline };
        });
        var driver = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry);
        var outcome = driver.Begin(new NativePointerScopeBeginStep("begin pointer workflow", _ =>
            throw new InvalidOperationException("scenario receiver failed")), context).GetOutcome();

        Assert.Multiple(() =>
        {
            Assert.That(begins, Is.EqualTo(1));
            Assert.That(receivedDeadline, Is.EqualTo(deadline.ToString("O")));
            Assert.That(outcome.Passed, Is.False);
            Assert.That(outcome.FailureCode, Is.EqualTo("native_pointer_scope_observation_failed"));
            Assert.That(outcome.Artifacts["pointerScopeHandle"], Is.EqualTo("scope-1"));
        });
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Completed));
        Assert.That(ends, Is.EqualTo(1));
    }

    private static void Register(GatewayAutomationRegistry registry, string name, GatewayAutomationHandler handler) =>
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor(name, "1.0", "Owned native boundary fixture",
            new Dictionary<string, object?>(), Array.Empty<string>(), true), handler);

    private static GatewayEndToEndTestContext Context() => new(() => 0, () => 0, _ => null)
        { WorkflowDeadlineUtc = DateTimeOffset.UtcNow.AddMinutes(1) };

    private static void RegisterBegin(GatewayAutomationRegistry registry) =>
        Register(registry, "native-pointer-scope.begin", (run, args) =>
        {
            var receipt = new Dictionary<string, object?>
                { ["scopeHandle"] = "scope-1", ["deadlineUtc"] = args["deadlineUtc"] };
            run.AddArtifact("native-pointer-scope", "native-input-scope", receipt);
            return receipt;
        });

    private sealed class PendingRestoration : IGatewayDeferredAutomationOperation
    {
        public bool Complete;
        public GatewayDeferredAutomationResult? Advance(bool stopRequested) => !Complete ? null
            : GatewayDeferredAutomationResult.Success(new Dictionary<string, object?>
                { ["scopeHandle"] = "scope-1", ["cleanupComplete"] = true,
                    ["pointerRestored"] = true, ["pointerScopeState"] = "closed" });
    }

    private static IReadOnlyDictionary<string, object?> Restored() => new Dictionary<string, object?>
        { ["scopeHandle"] = "scope-1", ["cleanupComplete"] = true,
            ["pointerRestored"] = true, ["pointerScopeState"] = "closed" };

    private sealed class ScopeScenario : IRimWorldEndToEndTest
    {
        public void Arrange(IEndToEndContext context) { }
        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        { yield return new NativePointerScopeBeginStep("begin", _ => { }); }
    }
    private sealed class FailedScopeScenario : IRimWorldEndToEndTest
    {
        public void Arrange(IEndToEndContext context) { }
        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            yield return new NativePointerScopeBeginStep("begin", _ => { });
            yield return new AssertionStep("later assertion", _ => throw new InvalidOperationException("deliberate fixture failure"));
        }
    }
    private sealed class TestClock : IGatewayEndToEndClock
    {
        public long Frame;
        public int Seconds;
        public long FrameCount => Frame;
        public int GameTick => (int)Frame;
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch.AddSeconds(Seconds).AddMilliseconds(Frame * 16);
    }
    private sealed class TestIsolation : IGatewayEndToEndTestIsolation
    {
        public bool Ready;
        public bool IsReady => Ready;
        public void Prepare(IEndToEndContext context) { }
        public bool Cleanup(IEndToEndContext context) => true;
    }

    private sealed class UnsupportedDriver : IGatewayEndToEndStepDriver
    {
        public IGatewayEndToEndStepOperation Begin(EndToEndStep step, IEndToEndContext context) =>
            GatewayEndToEndCompletedStepOperation.Failed("unsupported_e2e_step", "Not a legacy step.");
    }
}
