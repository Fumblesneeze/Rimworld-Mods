using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeMainTabStepTests
{
    [TestCase(null, "native_main_tab_disabled")]
    [TestCase("native_main_tab_hidden", "native_main_tab_disabled")]
    [TestCase("native_main_tab_disabled", null)]
    public void SuccessDifferentFailureOrUnexpectedFailureCannotPass(string? actualFailure, string? expectedFailure)
    {
        var registry = new GatewayAutomationRegistry();
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor("native-main-tab.set-open", "1.0", "Tab outcome fixture",
            new Dictionary<string, object?>(), Array.Empty<string>(), true), (context, _) =>
        {
            context.AddArtifact("native-main-tab-before", "native-main-tab-state", new { open = false });
            if (actualFailure != null) throw new GatewayAutomationException(actualFailure, "Native refusal.");
            return new Dictionary<string, object?> { ["defName"] = "Assign", ["open"] = true };
        });
        var outcome = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry).Begin(
            new NativeMainTabActionStep("observe Assign", "Assign", true, expectedFailure), Context()).GetOutcome();
        Assert.That(outcome.Passed, Is.False);
        Assert.That(outcome.FailureCode, Is.EqualTo(expectedFailure == null ? actualFailure : "native_expected_rejection_missing"));
        Assert.That(outcome.Artifacts["automationErrorCode"], Is.EqualTo(actualFailure ?? string.Empty));
        Assert.That(outcome.Artifacts["nativeMainTabObservations"], Does.Contain("native-main-tab-before"));
    }

    [Test]
    public void ExpectedNativeRefusalRetainsObservedStateAndRequestIdentity()
    {
        var registry = new GatewayAutomationRegistry();
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor("native-main-tab.set-open", "1.0", "Tab refusal fixture",
            new Dictionary<string, object?>(), Array.Empty<string>(), true), (context, _) =>
        {
            context.AddArtifact("native-main-tab-before", "native-main-tab-state", new { disabled = true });
            throw new GatewayAutomationException("native_main_tab_disabled", "The native button is disabled.");
        });
        var outcome = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry).Begin(
            new NativeMainTabActionStep("disabled Assign", "Assign", true, "native_main_tab_disabled"), Context()).GetOutcome();
        Assert.That(outcome.Passed, Is.True);
        Assert.That(outcome.Artifacts["automationErrorCode"], Is.EqualTo("native_main_tab_disabled"));
        Assert.That(outcome.Artifacts["automationRunId"], Is.Not.Empty);
        Assert.That(outcome.Artifacts["nativeMainTabObservations"], Does.Contain("native-main-tab-before"));
    }

    [Test]
    public void StepRoutesExactDesiredStateAndRetainsNativeEvidence()
    {
        var registry = new GatewayAutomationRegistry();
        int calls = 0;
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor("native-main-tab.set-open", "1.0", "Tab step boundary fixture",
            new Dictionary<string, object?>(), Array.Empty<string>(), true), (context, arguments) =>
        {
            calls++;
            Assert.That(arguments["version"], Is.EqualTo(1));
            Assert.That(arguments["defName"], Is.EqualTo("Assign"));
            Assert.That(arguments["open"], Is.EqualTo(false));
            context.AddArtifact("native-main-tab-before", "native-main-tab-state", new { open = true });
            context.AddArtifact("native-main-tab-invocation", "native-main-tab-command", new { attempted = true });
            context.AddArtifact("native-main-tab-after", "native-main-tab-state", new { open = false });
            return new Dictionary<string, object?> { ["defName"] = "Assign", ["open"] = false, ["changed"] = true };
        }, () => GatewayAutomationAvailability.AvailableNow);

        var outcome = new GatewayEndToEndNativeWindowDriver(new UnsupportedDriver(), registry).Begin(
            new NativeMainTabActionStep("close Assign", "Assign", false), Context()).GetOutcome();

        Assert.That(outcome.Passed, Is.True);
        Assert.That(calls, Is.EqualTo(1));
        Assert.That(outcome.Artifacts["requestId"], Is.Not.Empty);
        Assert.That(outcome.Artifacts["automationRunId"], Is.Not.Empty);
        Assert.That(outcome.Artifacts["nativeMainTabReceipt"], Does.Contain("\"open\":false"));
        Assert.That(outcome.Artifacts["nativeMainTabObservations"], Does.Contain("native-main-tab-invocation"));
    }

    private static GatewayEndToEndTestContext Context() => new(() => 0, () => 0, _ => null);
    private sealed class UnsupportedDriver : IGatewayEndToEndStepDriver
    {
        public IGatewayEndToEndStepOperation Begin(EndToEndStep step, IEndToEndContext context) =>
            GatewayEndToEndCompletedStepOperation.Failed("unsupported_e2e_step", "Not an existing step.");
    }
}
