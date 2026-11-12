using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeEntryMenuStepTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void FailedOrMalformedCaptureRetainsTheReturnedRunIdentity(bool malformed)
    {
        var registry = new GatewayAutomationRegistry();
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor(GatewayNativeEntryMenuAutomations.CaptureName,
                "1.0", "Entry capture test", new Dictionary<string, object?>(), Array.Empty<string>(), false),
            (_, _) => malformed ? "invalid inventory" : throw new GatewayAutomationException(
                "native_entry_layout_unobserved", "Native Entry layout is unavailable."),
            () => GatewayAutomationAvailability.AvailableNow);
        var outcome = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry).Begin(
            new NativeEntryMenuCaptureStep("capture", _ => Assert.Fail("Invalid capture must not reach the observer.")), Context()).GetOutcome();
        var retained = registry.ListRuns().Single();
        Assert.That(outcome.Passed, Is.False);
        Assert.That(outcome.FailureCode, Is.EqualTo(malformed ? "native_capture_invalid_result" : "native_entry_layout_unobserved"));
        Assert.That(outcome.Artifacts["automationRunId"], Is.EqualTo(retained.RunId));
        Assert.That(outcome.Artifacts["requestId"], Is.EqualTo(retained.RequestId));
        Assert.That(outcome.Artifacts["automationState"], Is.EqualTo(malformed ? "succeeded" : "failed"));
        Assert.That(outcome.Artifacts["automationErrorCode"], Is.EqualTo(malformed ? "" : "native_entry_layout_unobserved"));
        Assert.That(outcome.Artifacts["nativeEntryMenuInventory"], Is.EqualTo(malformed ? "\"invalid inventory\"" : "null"));
    }

    [Test]
    public void UnserializableCaptureAlsoRetainsTheReturnedRunIdentity()
    {
        var registry = new GatewayAutomationRegistry();
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor(GatewayNativeEntryMenuAutomations.CaptureName,
                "1.0", "Entry capture test", new Dictionary<string, object?>(), Array.Empty<string>(), false),
            (_, _) => new UnserializableResult(), () => GatewayAutomationAvailability.AvailableNow);
        var outcome = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry).Begin(
            new NativeEntryMenuCaptureStep("capture", _ => Assert.Fail("Malformed capture cannot reach the observer.")), Context()).GetOutcome();
        Assert.That(outcome.FailureCode, Is.EqualTo("native_capture_invalid_result"));
        Assert.That(outcome.Artifacts["automationRunId"], Is.EqualTo(registry.ListRuns().Single().RunId));
        Assert.That(outcome.Artifacts["requestId"], Is.Not.Empty);
        Assert.That(outcome.Artifacts["automationState"], Is.EqualTo("succeeded"));
        Assert.That(outcome.Artifacts["nativeEntryMenuInventorySerializationError"], Is.Not.Empty);
    }

    private sealed class UnserializableResult
    { public string Value => throw new InvalidOperationException("Malformed inventory getter"); }

    [Test]
    public void CaptureStepProjectsTheExactInventoryAndRetainsFailedObservations()
    {
        var registry = new GatewayAutomationRegistry();
        var controls = new[] { new EndToEndNativeEntryMenuControl("entry-1", "options", "Options", 300, 400, 170, 45, 1, 1600, 900) };
        GatewayNativeEntryMenuAutomations.Register(registry, () => controls,
            (_, _) => throw new AssertionException("Capture sent input."), () => GatewayAutomationAvailability.AvailableNow);
        var driver = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry);
        EndToEndNativeEntryMenuControl? observed = null;
        var first = driver.Begin(new NativeEntryMenuCaptureStep("capture", rows => observed = rows.Single()), Context());
        Assert.That(first.GetOutcome().Passed, Is.True); Assert.That(observed, Is.SameAs(controls[0]));
        var failed = driver.Begin(new NativeEntryMenuCaptureStep("capture fails", _ => throw new InvalidOperationException()), Context()).GetOutcome();
        Assert.That(failed.FailureCode, Is.EqualTo("native_capture_observation_failed"));
        Assert.That(failed.Artifacts["nativeEntryMenuInventory"], Does.Contain("entry-1"));
        Assert.That(failed.Artifacts["requestId"], Is.Not.Empty);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ClickStepUsesOneRetainedRunAndRequiresOwnedCleanup(bool clean)
    {
        var registry = new GatewayAutomationRegistry(); var native = new Pending { Clean = clean }; int calls = 0;
        GatewayNativeEntryMenuAutomations.Register(registry, () => Array.Empty<EndToEndNativeEntryMenuControl>(),
            (handle, _) => { Assert.That(handle, Is.EqualTo("entry-1")); calls++; return native; }, () => GatewayAutomationAvailability.AvailableNow);
        var context = Context();
        var operation = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry).Begin(
            new NativeEntryMenuClickActionStep("click", "entry-1"), context);
        Assert.That(operation.IsCompleted, Is.False);
        Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Pending));
        registry.Advance(); Assert.That(native.StopObserved, Is.True);
        native.Complete = true; registry.Advance();
        Assert.That(operation.GetOutcome().Artifacts["controlHandle"], Is.EqualTo("entry-1"));
        if (clean) Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Completed));
        else Assert.That(context.RunDeferredCleanup(), Is.EqualTo(DeferredCleanupStatus.Failed));
        Assert.That(calls, Is.EqualTo(1)); Assert.That(registry.ListRuns(), Has.Count.EqualTo(1));
    }

    private static GatewayEndToEndTestContext Context() => new(() => 0, () => 0, _ => null);
    private sealed class UnsupportedDriver : IGatewayEndToEndStepDriver
    {
        public IGatewayEndToEndStepOperation Begin(EndToEndStep step, IEndToEndContext context) =>
            GatewayEndToEndCompletedStepOperation.Failed("unsupported", "No Entry action here.");
    }
    private sealed class Pending : IGatewayDeferredAutomationOperation
    {
        public bool Complete, StopObserved, Clean;
        public GatewayDeferredAutomationResult? Advance(bool stopRequested)
        { StopObserved |= stopRequested; return Complete ? GatewayDeferredAutomationResult.Success(
            new Dictionary<string, object?> { ["cleanupComplete"] = Clean }) : null; }
    }
}
