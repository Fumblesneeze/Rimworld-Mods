using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeWindowAutomationTests
{
    [TestCase("missing-end")]
    [TestCase("extra")]
    [TestCase("version")]
    [TestCase("blank-window")]
    [TestCase("blank-scope")]
    [TestCase("string-end")]
    [TestCase("nan")]
    [TestCase("infinity")]
    public void InvalidDragArgumentsNeverReachTheInputLane(string fault)
    {
        var registry = new GatewayAutomationRegistry(); int calls = 0;
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (_, _, _, _) => throw new AssertionException("Invalid drag must not click."), () => GatewayAutomationAvailability.AvailableNow,
            drag: (_, _, _, _, _, _, _) => { calls++; return new PendingClick(); });
        var args = new Dictionary<string, object?> { ["version"] = 1, ["windowHandle"] = "window-1",
            ["startPixelX"] = 10, ["startPixelY"] = 20, ["endPixelX"] = 30, ["endPixelY"] = 40 };
        switch (fault)
        {
            case "missing-end": args.Remove("endPixelY"); break;
            case "extra": args["extra"] = true; break;
            case "version": args["version"] = 2; break;
            case "blank-window": args["windowHandle"] = " "; break;
            case "blank-scope": args["pointerScopeHandle"] = " "; break;
            case "string-end": args["endPixelX"] = "30"; break;
            case "nan": args["endPixelX"] = double.NaN; break;
            case "infinity": args["startPixelY"] = double.PositiveInfinity; break;
        }
        GatewayAutomationRun? run = null;
        try { run = registry.StartRun(GatewayNativeWindowAutomations.DragName, "invalid-drag", args); }
        catch (GatewayAutomationException error) when (fault is "nan" or "infinity")
        { Assert.That(error.Code, Is.EqualTo("invalid_automation_arguments")); }
        if (run != null) Assert.That(run.Error?.Code, Is.EqualTo("invalid_native_input_arguments"));
        Assert.That(calls, Is.Zero);
    }

    [TestCase(null)]
    [TestCase("owned-scope")]
    public void DragRetainsBothEndpointsAndWaitsForTheExistingDeferredCleanup(string? scope)
    {
        var registry = new GatewayAutomationRegistry(); var operation = new PendingClick(); int calls = 0;
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (_, _, _, _) => throw new AssertionException("Drag must not become a click."), () => GatewayAutomationAvailability.AvailableNow,
            drag: (handle, startX, startY, endX, endY, context, scopeHandle) =>
            {
                calls++;
                Assert.That((handle, startX, startY, endX, endY, scopeHandle), Is.EqualTo(("window-1", 40.5, 120d, 40.5, 121d, scope)));
                return operation;
            });
        var args = new Dictionary<string, object?> { ["version"] = 1, ["windowHandle"] = "window-1",
            ["startPixelX"] = 40.5, ["startPixelY"] = 120, ["endPixelX"] = 40.5, ["endPixelY"] = 121 };
        if (scope != null) args["pointerScopeHandle"] = scope;
        var run = registry.StartRun(GatewayNativeWindowAutomations.DragName, "drag-request", args);
        Assert.That(run.State, Is.EqualTo("running")); Assert.That(calls, Is.EqualTo(1));
        Assert.That(run.Artifacts.Any(a => a.Name == "native-drag-request"), Is.True);
        operation.Complete = true; registry.Advance();
        Assert.That(run.State, Is.EqualTo("succeeded"));
    }

    [Test]
    public void CaptureExposesExactWindowDescriptorsWithoutSendingInput()
    {
        var registry = new GatewayAutomationRegistry();
        var descriptor = new GatewayNativeWindowDescriptor("instance-1", "native.dialog", 14,
            new GatewayNativeWindowGeometry(10, 20, 300, 200, 1.25, 1920, 1080));
        var captures = 0;
        GatewayNativeWindowAutomations.Register(registry, () => { captures++; return new[] { descriptor }; },
            (_, _, _, _) => throw new AssertionException("Capture must not send input."),
            () => GatewayAutomationAvailability.AvailableNow);

        var run = registry.StartRun(GatewayNativeWindowAutomations.CaptureName, "capture-request",
            new Dictionary<string, object?> { ["version"] = 1 });

        Assert.That(run.State, Is.EqualTo("succeeded"));
        Assert.That(run.Result, Is.EqualTo(new[] { descriptor }));
        Assert.That(captures, Is.EqualTo(1));
        Assert.That(registry.Describe().Single(value => value.Name == GatewayNativeWindowAutomations.CaptureName).Mutating, Is.False);
    }
    [Test]
    public void ClickRetainsTheExactRequestAndRemainsPendingUntilNativeCleanupCompletes()
    {
        var registry = new GatewayAutomationRegistry();
        var operation = new PendingClick();
        var calls = 0;
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (handle, x, y, context) =>
            {
                calls++;
                Assert.That(handle, Is.EqualTo("instance-1"));
                Assert.That(x, Is.EqualTo(40.5));
                Assert.That(y, Is.EqualTo(120));
                Assert.That(context.CallerThreadId, Is.EqualTo(Environment.CurrentManagedThreadId));
                return operation;
            }, () => GatewayAutomationAvailability.AvailableNow);

        var run = registry.StartRun(GatewayNativeWindowAutomations.ClickName, "click-request",
            new Dictionary<string, object?> { ["version"] = 1m, ["windowHandle"] = "instance-1", ["pixelX"] = 40.5m, ["pixelY"] = 120m });
        Assert.That(run.State, Is.EqualTo("running"));
        Assert.That(calls, Is.EqualTo(1));
        Assert.That(run.Artifacts.Any(item => item.Kind == "native-input-request"), Is.True);
        operation.Complete = true;
        registry.Advance();
        Assert.That(run.State, Is.EqualTo("succeeded"));
        Assert.That(run.Result, Is.EqualTo("native dispatch complete; no widget outcome claimed"));
        Assert.That(registry.Describe().Single(value => value.Name == GatewayNativeWindowAutomations.ClickName).Mutating, Is.True);
    }

    [TestCase("capture-version")]
    [TestCase("capture-extra")]
    [TestCase("version")]
    [TestCase("missing-handle")]
    [TestCase("blank-handle")]
    [TestCase("string-coordinate")]
    [TestCase("nan")]
    [TestCase("infinity")]
    [TestCase("extra")]
    public void InvalidRequestNeverReachesTheNativeProvider(string invalid)
    {
        var registry = new GatewayAutomationRegistry();
        var calls = 0;
        GatewayNativeWindowAutomations.Register(registry,
            () => { calls++; return Array.Empty<GatewayNativeWindowDescriptor>(); },
            (_, _, _, _) => { calls++; return new PendingClick(); }, () => GatewayAutomationAvailability.AvailableNow);
        bool capture = invalid.StartsWith("capture-");
        var args = capture ? new Dictionary<string, object?> { ["version"] = 1 }
            : new Dictionary<string, object?> { ["version"] = 1, ["windowHandle"] = "instance-1", ["pixelX"] = 10, ["pixelY"] = 20 };
        switch (invalid)
        {
            case "capture-version": case "version": args["version"] = 2; break;
            case "capture-extra": case "extra": args["unexpected"] = true; break;
            case "missing-handle": args.Remove("windowHandle"); break;
            case "blank-handle": args["windowHandle"] = " "; break;
            case "string-coordinate": args["pixelX"] = "10"; break;
            case "nan": args["pixelX"] = double.NaN; break;
            case "infinity": args["pixelY"] = double.PositiveInfinity; break;
        }
        // Non-finite JSON is rejected by registry normalization before the handler can run.
        GatewayAutomationRun? run = null;
        try { run = registry.StartRun(capture ? GatewayNativeWindowAutomations.CaptureName : GatewayNativeWindowAutomations.ClickName, "invalid", args); }
        catch (GatewayAutomationException exception) when (invalid is "nan" or "infinity")
        { Assert.That(exception.Code, Is.EqualTo("invalid_automation_arguments")); }
        if (run != null)
        {
            Assert.That(run.State, Is.EqualTo("failed"));
            Assert.That(run.Error!.Code, Is.EqualTo("invalid_native_input_arguments"));
        }
        Assert.That(calls, Is.Zero);
    }

    [Test]
    public void UnavailableProviderIsReportedByBothRegistrationsAndIsNeverInvoked()
    {
        var registry = new GatewayAutomationRegistry();
        GatewayNativeWindowAutomations.Register(registry,
            () => throw new AssertionException("Unavailable provider invoked."),
            (_, _, _, _) => throw new AssertionException("Unavailable provider invoked."),
            () => new GatewayAutomationAvailability(false, "native_input_runtime_unsupported"));
        Assert.That(registry.Describe().Select(item => item.Availability.Reason),
            Is.EqualTo(new[] { "native_input_runtime_unsupported", "native_input_runtime_unsupported" }));
        Assert.Throws<GatewayAutomationException>(() => registry.StartRun(GatewayNativeWindowAutomations.ClickName,
            "unavailable", new Dictionary<string, object?>()));
    }

    private sealed class PendingClick : IGatewayDeferredAutomationOperation
    {
        public bool Complete { get; set; }
        public GatewayDeferredAutomationResult? Advance(bool stopRequested) => Complete
            ? GatewayDeferredAutomationResult.Success("native dispatch complete; no widget outcome claimed") : null;
    }
}
