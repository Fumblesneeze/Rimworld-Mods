using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeEntryMenuAutomationTests
{
    [Test]
    public void CaptureRetainsTheNativeControlInventoryWithoutInput()
    {
        var registry = new GatewayAutomationRegistry();
        var controls = new[] { new EndToEndNativeEntryMenuControl("entry-1", "options", "Options", 300, 400, 170, 45, 1, 1600, 900) };
        GatewayNativeEntryMenuAutomations.Register(registry, () => controls,
            (_, _) => throw new AssertionException("Capture cannot inject input."), () => GatewayAutomationAvailability.AvailableNow);
        var run = registry.StartRun(GatewayNativeEntryMenuAutomations.CaptureName, "capture",
            new Dictionary<string, object?> { ["version"] = 1 });
        Assert.That(run.State, Is.EqualTo("succeeded"));
        Assert.That(run.Result, Is.SameAs(controls));
        Assert.That(registry.Describe().Single(x => x.Name == GatewayNativeEntryMenuAutomations.CaptureName).Mutating, Is.False);
    }

    [Test]
    public void ClickRetainsItsExactHandleAndCancellationUntilOwnedCleanup()
    {
        var registry = new GatewayAutomationRegistry();
        var pending = new PendingClick(); int calls = 0;
        GatewayNativeEntryMenuAutomations.Register(registry, () => Array.Empty<EndToEndNativeEntryMenuControl>(),
            (handle, context) => { calls++; Assert.That(handle, Is.EqualTo("entry-1"));
                Assert.That(context.CallerThreadId, Is.EqualTo(Environment.CurrentManagedThreadId)); return pending; },
            () => GatewayAutomationAvailability.AvailableNow);
        var run = registry.StartRun(GatewayNativeEntryMenuAutomations.ClickName, "click",
            new Dictionary<string, object?> { ["version"] = 1, ["controlHandle"] = "entry-1" });
        Assert.That(run.State, Is.EqualTo("running")); Assert.That(calls, Is.EqualTo(1));
        Assert.That(run.Artifacts.Any(x => x.Kind == "native-input-request"), Is.True);
        run.RequestCancellation(); registry.Advance();
        Assert.That(pending.StopObserved, Is.True); Assert.That(run.IsTerminal, Is.False);
        pending.Complete = true; registry.Advance();
        Assert.That(run.IsTerminal, Is.True); Assert.That(calls, Is.EqualTo(1));
    }

    [TestCase(false, "version")]
    [TestCase(false, "extra")]
    [TestCase(true, "version")]
    [TestCase(true, "extra")]
    [TestCase(true, "missing")]
    [TestCase(true, "blank")]
    [TestCase(true, "long")]
    [TestCase(true, "number")]
    public void InvalidRequestsNeverReachTheNativeProvider(bool click, string invalid)
    {
        var registry = new GatewayAutomationRegistry(); int calls = 0;
        GatewayNativeEntryMenuAutomations.Register(registry, () => { calls++; return Array.Empty<EndToEndNativeEntryMenuControl>(); },
            (_, _) => { calls++; return new PendingClick(); }, () => GatewayAutomationAvailability.AvailableNow);
        var arguments = new Dictionary<string, object?> { ["version"] = 1 };
        if (click) arguments["controlHandle"] = "entry-1";
        switch (invalid)
        {
            case "version": arguments["version"] = 2; break;
            case "extra": arguments["pixelX"] = 55; break;
            case "missing": arguments.Remove("controlHandle"); break;
            case "blank": arguments["controlHandle"] = " "; break;
            case "long": arguments["controlHandle"] = new string('x', 201); break;
            case "number": arguments["controlHandle"] = 5; break;
        }
        var run = registry.StartRun(click ? GatewayNativeEntryMenuAutomations.ClickName : GatewayNativeEntryMenuAutomations.CaptureName,
            "invalid", arguments);
        Assert.That(run.State, Is.EqualTo("failed"));
        Assert.That(run.Error!.Code, Is.EqualTo("invalid_native_input_arguments"));
        Assert.That(calls, Is.Zero);
    }

    private sealed class PendingClick : IGatewayDeferredAutomationOperation
    {
        public bool Complete, StopObserved;
        public GatewayDeferredAutomationResult? Advance(bool stopRequested)
        { StopObserved |= stopRequested; return Complete ? GatewayDeferredAutomationResult.Success(
            new Dictionary<string, object?> { ["cleanupComplete"] = true }) : null; }
    }
}
