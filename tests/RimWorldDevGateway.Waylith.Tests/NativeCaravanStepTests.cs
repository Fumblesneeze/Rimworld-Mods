using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeCaravanStepTests
{
    [Test]
    public void LostDispatchReservesIndependentViewAndFixtureCleanupWithoutClaimingInputRetired()
    {
        var registry = new GatewayAutomationRegistry(); var runtime = new Runtime();
        _ = new GatewayWorldViewAutomation(registry, runtime);
        Register("native-caravan.capture", new Dictionary<string, object?> { ["caravanHandle"] = "caravan" });
        Register("native-caravan-health.capture", new Dictionary<string, object?> { ["controlHandle"] = "row" });
        Register("native-caravan-health.click", new PendingInput(true));
        double now = 0; bool fixtureCleaned = false;
        var context = new GatewayEndToEndTestContext(() => 0, () => 0, _ => null);
        context.DeferCleanup(() => fixtureCleaned = true);
        var driver = new GatewayEndToEndCaravanDriver(new Other(), registry, () => now);
        string view = "";
        Assert.That(driver.Begin(new NativeWorldViewCaptureStep("view", h => view = h), context).GetOutcome().Passed, Is.True);
        Assert.That(driver.Begin(new NativeCaravanCaptureStep("caravan", view, "Caravan_1", _ => { }), context).GetOutcome().Passed, Is.True);
        Assert.That(driver.Begin(new NativeCaravanHealthCaptureStep("row", "caravan", "Pawn_1", _ => { }), context).GetOutcome().Passed, Is.True);
        var click = driver.Begin(new NativeCaravanHealthClickStep("click", "row"), context);
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Pending));
        now = 14.99;
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Pending));
        Assert.That(registry.ListRuns().Any(r => r.Name == "native-world.restore"), Is.False);
        now = 15;
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Pending));
        Assert.That(registry.ListRuns().Any(r => r.Name == "native-world.restore"), Is.True);
        now = 25;
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Failed));
        Assert.Multiple(() =>
        {
            Assert.That(fixtureCleaned, Is.True);
            Assert.That(click.IsCompleted, Is.False);
            Assert.That(runtime.Restores, Is.Zero, "Lost dispatch cannot manufacture camera restoration.");
            var evidence = ((IGatewayEndToEndStepEvidence)click).CaptureArtifacts();
            Assert.That(evidence["nativeInputCleanupFailure"], Is.EqualTo("native_caravan_input_cleanup_deadline"));
            Assert.That(evidence["worldRestorationCleanupFailure"], Is.EqualTo("native_world_cleanup_deadline"));
        });
        void Register(string name, object value) => registry.RegisterBuiltIn(new(name, "1.0", "owned host fixture", new Dictionary<string, object?>(), Array.Empty<string>(), true),
            (_, _) => value, () => GatewayAutomationAvailability.AvailableNow);
    }

    [Test]
    public void CancellationAfterCaptureRetainsTheFallbackRestorationIdentity()
    {
        var registry = new GatewayAutomationRegistry();
        var runtime = new Runtime { AfterCapture = () => registry.ListRuns().Single().RequestCancellation() };
        _ = new GatewayWorldViewAutomation(registry, runtime);
        var context = new GatewayEndToEndTestContext(() => 0, () => 0, _ => null);
        var operation = new GatewayEndToEndCaravanDriver(new Other(), registry).Begin(
            new NativeWorldViewCaptureStep("capture", _ => Assert.Fail("A cancelled capture must not call the receiver.")), context);
        Assert.That(operation.GetOutcome().Passed, Is.False);
        Assert.That(registry.ListRuns().Single().State, Is.EqualTo("cancelled"));
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Pending));
        registry.Advance();
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Completed));
        Assert.That(runtime.Restores, Is.EqualTo(1));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void InputCancellationDrainsBeforeWorldRestorationAndRetainsIncompleteCleanup(bool clean)
    {
        var registry = new GatewayAutomationRegistry(); var runtime = new Runtime();
        _ = new GatewayWorldViewAutomation(registry, runtime);
        var pending = new PendingInput(clean);
        Register("native-caravan.capture", new Dictionary<string, object?> { ["caravanHandle"] = "caravan" });
        Register("native-caravan-health.capture", new Dictionary<string, object?> { ["controlHandle"] = "row" });
        Register("native-caravan-health.click", pending);
        var context = new GatewayEndToEndTestContext(() => 0, () => 0, _ => null);
        var driver = new GatewayEndToEndCaravanDriver(new Other(), registry);
        string view = "";
        Assert.That(driver.Begin(new NativeWorldViewCaptureStep("view", h => view = h), context).GetOutcome().Passed, Is.True);
        Assert.That(driver.Begin(new NativeCaravanCaptureStep("caravan", view, "Caravan_1", _ => { }), context).GetOutcome().Passed, Is.True);
        Assert.That(driver.Begin(new NativeCaravanHealthCaptureStep("row", "caravan", "Pawn_1", _ => { }), context).GetOutcome().Passed, Is.True);
        var click = driver.Begin(new NativeCaravanHealthClickStep("click", "row"), context);
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Pending));
        Assert.That(runtime.Restores, Is.Zero);
        registry.Advance(); Assert.That(pending.Stopped, Is.True);
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Pending));
        Assert.That(runtime.Restores, Is.Zero);
        registry.Advance();
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(clean ? DeferredCleanupStatus.Completed : DeferredCleanupStatus.Failed));
        Assert.That(runtime.Restores, Is.EqualTo(1));
        Assert.That(((IGatewayEndToEndStepEvidence)click).CaptureArtifacts()["nativeInputAdmitted"], Is.EqualTo("true"));

        void Register(string name, object value) => registry.RegisterBuiltIn(new(name, "1.0", "owned host fixture", new Dictionary<string, object?>(), Array.Empty<string>(), true),
            (_, _) => value, () => GatewayAutomationAvailability.AvailableNow);
    }

    [Test]
    public void FailedViewReceiverStillOwnsAndPollsTheOneRestorationRun()
    {
        var registry = new GatewayAutomationRegistry();
        var runtime = new Runtime();
        _ = new GatewayWorldViewAutomation(registry, runtime);
        var context = new GatewayEndToEndTestContext(() => 0, () => 0, _ => null);
        var operation = new GatewayEndToEndCaravanDriver(new Other(), registry).Begin(
            new NativeWorldViewCaptureStep("capture", _ => throw new InvalidOperationException("receiver")), context);
        Assert.That(operation.GetOutcome().FailureCode, Is.EqualTo("native_caravan_observation_failed"));
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Pending));
        registry.Advance();
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Completed));
        Assert.That(runtime.Restores, Is.EqualTo(1));
        Assert.That(((IGatewayEndToEndStepEvidence)operation).CaptureArtifacts()["worldRestorationReceipt"], Does.Contain("true"));
        Assert.That(registry.ListRuns().Count, Is.EqualTo(2));
    }
    private sealed class Other : IGatewayEndToEndStepDriver
    {
        public IGatewayEndToEndStepOperation Begin(EndToEndStep step, IEndToEndContext context) =>
            GatewayEndToEndCompletedStepOperation.Failed("unsupported", "Not a caravan step.");
    }
    private sealed class Runtime : IGatewayWorldViewRuntime, IGatewayWorldViewSession
    {
        public int Restores;
        public Action? AfterCapture;
        public GatewayAutomationAvailability Availability() => GatewayAutomationAvailability.AvailableNow;
        public IGatewayWorldViewSession Capture() { AfterCapture?.Invoke(); return this; }
        public GatewayWorldViewState Observe() => new(false, null, true);
        public void Activate(Action admission) => admission();
        public GatewayWorldRestoreState Restore() { Restores++; return new(true, true, "verified"); }
    }
    private sealed class PendingInput : IGatewayDeferredAutomationOperation
    {
        private readonly bool clean;
        internal bool Stopped;
        internal PendingInput(bool clean) => this.clean = clean;
        public GatewayDeferredAutomationResult Advance(bool stopRequested)
        { Stopped |= stopRequested; return GatewayDeferredAutomationResult.Success(new Dictionary<string, object?> { ["cleanupComplete"] = clean }); }
    }
}
