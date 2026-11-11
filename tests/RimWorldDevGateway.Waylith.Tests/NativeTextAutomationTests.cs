using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeTextAutomationTests
{
    [TestCase("")]
    [TestCase("-12.5")]
    [TestCase("Médicine")]
    public void ExactCapturedRequestIsRetainedAndOnlyDeferredCleanupCanFinishIt(string text)
    {
        var registry = new GatewayAutomationRegistry();
        var pending = new Pending();
        int calls = 0;
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (_, _, _, _) => throw new AssertionException("No click fallback."), () => GatewayAutomationAvailability.AvailableNow,
            replaceText: (window, control, value, context) =>
            {
                calls++;
                Assert.That((window, control, value), Is.EqualTo(("window", "control", text)));
                Assert.That(context.CallerThreadId, Is.EqualTo(Environment.CurrentManagedThreadId));
                return pending;
            });
        var run = registry.StartRun(GatewayNativeWindowAutomations.ReplaceTextName, "text-request", Request(text));
        Assert.That(run.State, Is.EqualTo("running"));
        Assert.That(calls, Is.EqualTo(1));
        Assert.That(run.Artifacts.Any(item => item.Kind == "native-input-request"), Is.True);
        pending.Complete = true; registry.Advance();
        Assert.That(run.State, Is.EqualTo("succeeded"));
    }

    [TestCase("oversized")]
    [TestCase("newline")]
    [TestCase("line-separator")]
    [TestCase("paragraph-separator")]
    [TestCase("format")]
    [TestCase("surrogate")]
    [TestCase("version")]
    [TestCase("missing-control")]
    [TestCase("extra")]
    public void InvalidReplacementNeverReachesNativeInput(string invalid)
    {
        var registry = new GatewayAutomationRegistry();
        int calls = 0;
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (_, _, _, _) => throw new AssertionException("No click fallback."), () => GatewayAutomationAvailability.AvailableNow,
            replaceText: (_, _, _, _) => { calls++; return new Pending(); });
        var arguments = Request("123");
        if (invalid == "oversized") arguments["text"] = new string('1', 129);
        if (invalid == "newline") arguments["text"] = "12\n3";
        if (invalid == "line-separator") arguments["text"] = "12\u20283";
        if (invalid == "paragraph-separator") arguments["text"] = "12\u20293";
        if (invalid == "format") arguments["text"] = "12\u200b3";
        if (invalid == "surrogate") arguments["text"] = "😀";
        if (invalid == "version") arguments["version"] = 2;
        if (invalid == "missing-control") arguments.Remove("controlHandle");
        if (invalid == "extra") arguments["key"] = "Enter";
        var run = registry.StartRun(GatewayNativeWindowAutomations.ReplaceTextName, "invalid-text", arguments);
        Assert.That(run.State, Is.EqualTo("failed"));
        Assert.That(run.Error!.Code, Is.EqualTo("invalid_native_input_arguments"));
        Assert.That(calls, Is.Zero);
    }

    private static Dictionary<string, object?> Request(string text) => new()
        { ["version"] = 1, ["windowHandle"] = "window", ["controlHandle"] = "control", ["text"] = text };
    private sealed class Pending : IGatewayDeferredAutomationOperation
    {
        public bool Complete;
        public GatewayDeferredAutomationResult? Advance(bool stopRequested) => Complete
            ? GatewayDeferredAutomationResult.Success(new Dictionary<string, object?> { ["cleanupComplete"] = true }) : null;
    }
}
