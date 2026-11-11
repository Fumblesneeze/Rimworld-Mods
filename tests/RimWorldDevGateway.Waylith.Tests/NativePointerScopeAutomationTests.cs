using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class NativePointerScopeAutomationTests
{
    [Test]
    public void EndRemainsAvailableForCleanupWhenNewInputAdmissionFails()
    {
        var registry = new GatewayAutomationRegistry();
        int ended = 0;
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (_, _, _, _) => throw new AssertionException("Must not click."),
            () => new GatewayAutomationAvailability(false, "provider_failed"),
            (_, _) => throw new AssertionException("Must not begin."),
            (handle, _) => { Assert.That(handle, Is.EqualTo("owned")); ended++; return new Pending(); });
        var run = registry.StartRun(GatewayNativeWindowAutomations.EndScopeName, "cleanup", new Dictionary<string, object?>
            { ["version"] = 1, ["scopeHandle"] = "owned" });
        Assert.That(run.State, Is.EqualTo("running"));
        Assert.That(ended, Is.EqualTo(1));
    }

    [Test]
    public void VersionTwoClickUsesExactScopeAndVersionOneCannotSilentlyRetain()
    {
        var registry = new GatewayAutomationRegistry();
        int scoped = 0;
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (_, _, _, _) => throw new AssertionException("Scoped request must not use standalone click."),
            () => GatewayAutomationAvailability.AvailableNow, scopedClick: (window, x, y, context, scope) =>
            {
                Assert.That(window, Is.EqualTo("window"));
                Assert.That(scope, Is.EqualTo("workflow"));
                scoped++;
                return new Pending();
            });
        var arguments = new Dictionary<string, object?> { ["version"] = 2, ["windowHandle"] = "window",
            ["pixelX"] = 20, ["pixelY"] = 40, ["pointerScopeHandle"] = "workflow" };
        var run = registry.StartRun(GatewayNativeWindowAutomations.ClickName, "scoped", arguments);
        Assert.That(run.State, Is.EqualTo("running"));
        Assert.That(scoped, Is.EqualTo(1));
        arguments["version"] = 1;
        var invalid = registry.StartRun(GatewayNativeWindowAutomations.ClickName, "v1-extra", arguments);
        Assert.That(invalid.Error!.Code, Is.EqualTo("invalid_native_input_arguments"));
        Assert.That(scoped, Is.EqualTo(1));
    }

    [Test]
    public void BeginRetainsHandleBeforeLateCancellationAndRequestsCleanup()
    {
        using var cancel = new CancellationTokenSource();
        var registry = new GatewayAutomationRegistry();
        int ends = 0;
        GatewayNativeWindowAutomations.Register(registry, () => Array.Empty<GatewayNativeWindowDescriptor>(),
            (_, _, _, _) => throw new AssertionException("Begin must not click."), () => GatewayAutomationAvailability.AvailableNow,
            (deadline, context) =>
            {
                cancel.Cancel();
                return new Dictionary<string, object?> { ["scopeHandle"] = "owned-scope", ["deadlineUtc"] = deadline.ToString("O") };
            }, (handle, context) => { Assert.That(handle, Is.EqualTo("owned-scope")); ends++; return new Pending(); });
        var run = registry.StartRun(GatewayNativeWindowAutomations.BeginScopeName, "begin", new Dictionary<string, object?>
            { ["version"] = 1, ["deadlineUtc"] = DateTimeOffset.UtcNow.AddMinutes(1).ToString("O") }, cancellationToken: cancel.Token);
        Assert.That(run.State, Is.EqualTo("cancelled"));
        var artifact = run.Artifacts.Single(item => item.Name == "native-pointer-scope");
        Assert.That(((IReadOnlyDictionary<string, object?>)artifact.Value!)["scopeHandle"], Is.EqualTo("owned-scope"));
        Assert.That(ends, Is.EqualTo(1));
    }

    private sealed class Pending : IGatewayDeferredAutomationOperation
    { public GatewayDeferredAutomationResult? Advance(bool stopRequested) => null; }
}
