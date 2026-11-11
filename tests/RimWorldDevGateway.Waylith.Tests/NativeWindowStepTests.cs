using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeWindowStepTests
{
    [Test]
    public void DeveloperLogStepUsesTheRegisteredNativeCommandAndRetainsItsIdentity()
    {
        var registry = new GatewayAutomationRegistry();
        int windows = 0;
        GatewayDeveloperLogAutomation.Register(registry, () => new(true, true, windows),
            () => windows = 1, () => GatewayAutomationAvailability.AvailableNow);
        var result = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry).Begin(
            new NativeDeveloperLogActionStep("open log", true), Context());
        var outcome = result.GetOutcome();
        Assert.That(outcome.Passed, Is.True);
        Assert.That(outcome.Artifacts["requestId"], Is.Not.Empty);
        Assert.That(outcome.Artifacts["nativeLogReceipt"], Does.Contain("\"open\":true"));
        Assert.That(registry.ListRuns(), Has.Count.EqualTo(1));
    }

    [Test]
    public void ExpectedNativeRejectionRetainsItsReceiptAndAllowsFurtherObservation()
    {
        var registry = new GatewayAutomationRegistry();
        var native = new PendingClick { Complete = true, FailureCode = "native_input_not_dispatched" };
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (_, _, _, _) => native, () => GatewayAutomationAvailability.AvailableNow);
        var context = Context();
        var result = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry).Begin(
            new NativeWindowClickActionStep("expected stale rejection", "window-1", 55, 90,
                "native_input_not_dispatched"), context);
        registry.Advance();

        var outcome = result.GetOutcome();
        Assert.That(outcome.Passed, Is.True);
        Assert.That(outcome.Artifacts["automationErrorCode"], Is.EqualTo("native_input_not_dispatched"));
        Assert.That(outcome.Artifacts["nativeInputReceipt"], Does.Contain("\"cleanupComplete\":true"));
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Completed));
    }

    [Test]
    public void CaptureCallbackFailureRetainsTheActualWindowInventoryAndRequestIdentity()
    {
        var registry = new GatewayAutomationRegistry();
        GatewayNativeWindowAutomations.Register(registry, () => new[]
            { new GatewayNativeWindowDescriptor("window-1", "native.actual", 0,
                new GatewayNativeWindowGeometry(10, 20, 300, 200, 1.25, 1600, 900)) },
            (_, _, _, _) => throw new AssertionException("Capture sent input."),
            () => GatewayAutomationAvailability.AvailableNow);
        var driver = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry);
        var result = driver.Begin(new NativeWindowCaptureStep("capture", _ =>
            throw new InvalidOperationException("The expected window is absent.")), Context());
        var outcome = result.GetOutcome();
        Assert.That(outcome.Passed, Is.False);
        Assert.That(outcome.FailureCode, Is.EqualTo("native_capture_observation_failed"));
        Assert.That(outcome.Artifacts["automationRunId"], Is.Not.Empty);
        Assert.That(outcome.Artifacts["requestId"], Is.Not.Empty);
        Assert.That(outcome.Artifacts["nativeWindowInventory"], Does.Contain("native.actual"));
        Assert.That(outcome.Artifacts["nativeWindowInventory"], Does.Contain("window-1"));
        Assert.That(registry.ListRuns(), Has.Count.EqualTo(1));
    }

    [TestCase(null, true)]
    [TestCase("different_rejection", true)]
    [TestCase("native_input_not_dispatched", false)]
    public void ExpectedRejectionCannotHideSuccessAnotherFailureOrUnconfirmedCleanup(string? failureCode, bool clean)
    {
        var registry = new GatewayAutomationRegistry();
        var native = new PendingClick { Complete = true, FailureCode = failureCode, Clean = clean };
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (_, _, _, _) => native, () => GatewayAutomationAvailability.AvailableNow);
        var result = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry).Begin(
            new NativeWindowClickActionStep("expected stale rejection", "window-1", 55, 90,
                "native_input_not_dispatched"), Context());
        registry.Advance();
        Assert.That(result.GetOutcome().Passed, Is.False);
    }

    [Test]
    public void ExpectedRejectionWithoutAnInputReceiptStillFailsAndTaintsCleanup()
    {
        var registry = new GatewayAutomationRegistry();
        var native = new PendingClick { Complete = true, FailureCode = "native_input_not_dispatched", OmitReceipt = true };
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (_, _, _, _) => native, () => GatewayAutomationAvailability.AvailableNow);
        var context = Context();
        var result = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry).Begin(
            new NativeWindowClickActionStep("expected stale rejection", "window-1", 55, 90,
                "native_input_not_dispatched"), context);
        registry.Advance();
        Assert.That(result.GetOutcome().Passed, Is.False);
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Failed));
    }

    [Test]
    public void CaptureUsesTheRegisteredProviderAndReturnsExactIdentityAndGeometry()
    {
        var registry = new GatewayAutomationRegistry();
        int captures = 0;
        GatewayNativeWindowAutomations.Register(registry, () =>
        {
            captures++;
            return new[] { new GatewayNativeWindowDescriptor("window-1", "native.test", 7,
                new GatewayNativeWindowGeometry(10, 20, 300, 200, 1.25, 1600, 900)) };
        }, (_, _, _, _) => throw new AssertionException("Capture sent input."),
            () => GatewayAutomationAvailability.AvailableNow);
        var driver = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry);
        IReadOnlyList<EndToEndNativeWindow>? captured = null;
        var result = driver.Begin(new NativeWindowCaptureStep("capture", values => captured = values), Context());
        Assert.That(result.IsCompleted, Is.True);
        Assert.That(result.GetOutcome().Passed, Is.True);
        Assert.That(captures, Is.EqualTo(1));
        var window = captured!.Single();
        Assert.That(window.Handle, Is.EqualTo("window-1"));
        Assert.That(window.UiScale, Is.EqualTo(1.25));
        Assert.That(window.PixelHeight, Is.EqualTo(900));
        Assert.That(result.GetOutcome().Artifacts.ContainsKey("automationRunId"), Is.True);
    }

    [Test]
    public void ClickUsesOneRetainedRunAndWaitsForItsActualCompletion()
    {
        var registry = new GatewayAutomationRegistry();
        var native = new PendingClick();
        int calls = 0;
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (handle, x, y, _) =>
            {
                calls++;
                Assert.That(handle, Is.EqualTo("window-1"));
                Assert.That(x, Is.EqualTo(55.5));
                Assert.That(y, Is.EqualTo(90));
                return native;
            }, () => GatewayAutomationAvailability.AvailableNow);
        var driver = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry);
        var result = driver.Begin(new NativeWindowClickActionStep("click", "window-1", 55.5, 90), Context());
        Assert.That(result.IsCompleted, Is.False);
        Assert.That(calls, Is.EqualTo(1));
        Assert.Throws<InvalidOperationException>(() => result.GetOutcome());
        registry.Advance();
        Assert.That(result.IsCompleted, Is.False);
        native.Complete = true;
        registry.Advance();
        Assert.That(result.IsCompleted, Is.True);
        Assert.That(result.GetOutcome().Passed, Is.True);
        Assert.That(result.GetOutcome().Artifacts["windowHandle"], Is.EqualTo("window-1"));
        Assert.That(registry.ListRuns(), Has.Count.EqualTo(1));
    }

    [Test]
    public void ScenarioCleanupCancelsAndWaitsForNativeCleanupBeforeIsolation()
    {
        var registry = new GatewayAutomationRegistry();
        var native = new PendingClick();
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (_, _, _, _) => native, () => GatewayAutomationAvailability.AvailableNow);
        var context = Context();
        var driver = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry);
        var result = driver.Begin(new NativeWindowClickActionStep("click", "window-1", 55, 90), context);

        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Pending));
        registry.Advance();
        Assert.That(native.StopObserved, Is.True);
        Assert.That(result.IsCompleted, Is.False);
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Pending));
        native.Complete = true;
        registry.Advance();
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Completed));
        Assert.That(result.IsCompleted, Is.True);
        Assert.That(result.GetOutcome().Passed, Is.False);
    }

    [Test]
    public void FailedClickPreservesIdentityAndPartialDispatchReceipt()
    {
        var registry = new GatewayAutomationRegistry();
        var native = new PendingClick { Complete = true, FailureCode = "stale_window_instance" };
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (_, _, _, _) => native, () => GatewayAutomationAvailability.AvailableNow);
        var driver = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry);
        var result = driver.Begin(new NativeWindowClickActionStep("click", "window-1", 55, 90), Context());
        registry.Advance();
        var outcome = result.GetOutcome();
        Assert.That(outcome.Passed, Is.False);
        Assert.That(outcome.FailureCode, Is.EqualTo("stale_window_instance"));
        Assert.That(outcome.Artifacts["windowHandle"], Is.EqualTo("window-1"));
        Assert.That(outcome.Artifacts["pixelX"], Is.EqualTo("55"));
        Assert.That(outcome.Artifacts["nativeInputReceipt"], Does.Contain("\"cleanupComplete\":true"));
    }

    [Test]
    public void IncompleteNativeCleanupTaintsTheScenarioInsteadOfAllowingReuse()
    {
        var registry = new GatewayAutomationRegistry();
        var native = new PendingClick { Complete = true, Clean = false, FailureCode = "native_input_cleanup_incomplete" };
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (_, _, _, _) => native, () => GatewayAutomationAvailability.AvailableNow);
        var context = Context();
        new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry)
            .Begin(new NativeWindowClickActionStep("click", "window-1", 55, 90), context);
        registry.Advance();
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Failed));
    }

    [Test]
    public void ThrowingDeferredCleanupWithoutAReceiptTaintsTheScenario()
    {
        var registry = new GatewayAutomationRegistry();
        var native = new PendingClick { ThrowOnStop = true };
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (_, _, _, _) => native, () => GatewayAutomationAvailability.AvailableNow);
        var context = Context();
        var operation = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry)
            .Begin(new NativeWindowClickActionStep("click", "window-1", 55, 90), context);
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Pending));
        registry.Advance();
        Assert.That(operation.GetOutcome().FailureCode, Is.EqualTo("automation_cleanup_incomplete"));
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Failed));
    }

    [Test]
    public void NativeRejectionBeforeDeferredAdmissionNeedsNoInputCleanup()
    {
        var registry = new GatewayAutomationRegistry();
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (_, _, _, _) => throw new GatewayAutomationException("native_input_busy", "Input is already held."),
            () => GatewayAutomationAvailability.AvailableNow);
        var context = Context();
        var outcome = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry)
            .Begin(new NativeWindowClickActionStep("click", "window-1", 1, 1), context).GetOutcome();
        Assert.That(outcome.FailureCode, Is.EqualTo("native_input_busy"));
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Completed));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void UnavailableCapabilityNeverInvokesAProvider(bool capture)
    {
        var registry = new GatewayAutomationRegistry();
        GatewayNativeWindowAutomations.Register(registry,
            () => throw new AssertionException("Unavailable capture invoked."),
            (_, _, _, _) => throw new AssertionException("Unavailable input invoked."),
            () => new GatewayAutomationAvailability(false, "native_input_reset_unavailable"));
        var context = Context();
        EndToEndStep step = capture ? new NativeWindowCaptureStep("capture", _ => Assert.Fail("Unexpected capture."))
            : new NativeWindowClickActionStep("click", "window-1", 1, 1);
        var outcome = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry).Begin(step, context).GetOutcome();
        Assert.That(outcome.Passed, Is.False);
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Completed));
    }

    [Test]
    public void ScenarioTimeoutRetainsAdmissionAndFinalCleanupReceiptWithoutReplacingTimeout()
    {
        var registry = new GatewayAutomationRegistry();
        var native = new PendingClick();
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (_, _, _, _) => native, () => GatewayAutomationAvailability.AvailableNow);
        var clock = new TestClock();
        var descriptor = new GatewayEndToEndRuntimeTestDescriptor("native-timeout", "test.owner",
            new[] { "ludeon.rimworld", "test.owner" }, typeof(ClickScenario).FullName!, 12, 100, 30, typeof(ClickScenario));
        var machine = new GatewayEndToEndExecutionStateMachine(new[] { descriptor }, clock,
            () => new GatewayEndToEndTestContext(() => clock.FrameCount, () => clock.GameTick, _ => null),
            new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry), new TestIsolation());
        bool admissionRetained = false;
        for (int attempt = 0; attempt < 80 && !machine.Snapshot.IsTerminal; attempt++)
        {
            if (machine.PersistencePending) machine.ConfirmPersisted(machine.Snapshot);
            clock.Frame++;
            registry.Advance();
            machine.Advance();
            if (machine.Snapshot.CurrentStep is { Status: "running" } pending)
                admissionRetained |= pending.Artifacts?.ContainsKey("automationRunId") == true;
            if (native.StopObserved) native.Complete = true;
        }
        Assert.That(machine.Snapshot.IsTerminal, Is.True);
        Assert.That(admissionRetained, Is.True, "The accepted input identity must be durable while input is pending.");
        var step = machine.Snapshot.Results.Single().Steps.Single();
        Assert.That(step.Status, Is.EqualTo("timed_out"));
        Assert.That(step.Artifacts!["windowHandle"], Is.EqualTo("window-1"));
        Assert.That(step.Artifacts["nativeInputReceipt"], Does.Contain("\"cleanupComplete\":true"));
    }

    private sealed class ClickScenario : IRimWorldEndToEndTest
    {
        public void Arrange(IEndToEndContext context) { }
        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        { yield return new NativeWindowClickActionStep("click", "window-1", 55, 90); }
    }
    private sealed class TestClock : IGatewayEndToEndClock
    {
        public long Frame;
        public long FrameCount => Frame;
        public int GameTick => (int)Frame;
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch.AddMilliseconds(Frame * 16);
    }
    private sealed class TestIsolation : IGatewayEndToEndTestIsolation
    {
        public bool IsReady => true;
        public void Prepare(IEndToEndContext context) { }
        public bool Cleanup(IEndToEndContext context) => true;
    }

    private sealed class PendingClick : IGatewayDeferredAutomationOperation
    {
        public bool Complete;
        public bool StopObserved;
        public string? FailureCode;
        public bool Clean = true;
        public bool ThrowOnStop;
        public bool OmitReceipt;
        public GatewayDeferredAutomationResult? Advance(bool stopRequested)
        {
            StopObserved |= stopRequested;
            if (stopRequested && ThrowOnStop) throw new InvalidOperationException("Owned release failed.");
            if (!Complete) return null;
            var receipt = OmitReceipt ? null : new Dictionary<string, object?> { ["cleanupComplete"] = Clean, ["dispatched"] = true };
            return FailureCode == null ? GatewayDeferredAutomationResult.Success(receipt)
                : GatewayDeferredAutomationResult.Failure(FailureCode, "Native context changed.", receipt);
        }
    }

    private static GatewayEndToEndTestContext Context() => new(() => 0, () => 0, _ => null);
    private sealed class UnsupportedDriver : IGatewayEndToEndStepDriver
    {
        public IGatewayEndToEndStepOperation Begin(EndToEndStep step, IEndToEndContext context) =>
            GatewayEndToEndCompletedStepOperation.Failed("unsupported_e2e_step", "Not a legacy step.");
    }
}
