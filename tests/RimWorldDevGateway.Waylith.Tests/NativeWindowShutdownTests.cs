using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeWindowShutdownTests
{
    [Test]
    public void IncompleteHookRemovalRetainsShutdownOwnerWhileOtherResourcesAreClosedAndRetryCanRecover()
    {
        var lifecycle = new GatewayShutdownLifecycle();
        var closed = 0;
        var incomplete = true;
        var now = DateTimeOffset.UtcNow;
        void Cleanup() => GatewayNativeWindowShutdown.Cleanup(null,
            () => incomplete ? GatewayDeferredAutomationResult.Failure("native_input_shutdown_incomplete", "hook removal failed") : null,
            () => closed++);

        var first = lifecycle.TryShutdown(Cleanup, true, now);
        Assert.That(closed, Is.EqualTo(1), "A native-input failure must not prevent best-effort transport teardown.");
        Assert.That(first.RetainsOwnership, Is.True);
        Assert.That(first.IsTerminal, Is.False);
        incomplete = false;
        var second = lifecycle.TryShutdown(Cleanup, true, now.AddSeconds(2));
        Assert.That(second.IsTerminal, Is.True);
        Assert.That(second.RecoveredAfterRetainedFailure, Is.True);
        Assert.That(closed, Is.EqualTo(2));
    }
    [TestCase("native_input_shutdown")]
    [TestCase("native_input_reset_unavailable")]
    public void AnAbortedGestureWithCompletedCleanupDoesNotBlockShutdown(string abort)
    {
        var closed = false;
        Assert.DoesNotThrow(() => GatewayNativeWindowShutdown.Cleanup(null,
            () => GatewayDeferredAutomationResult.Failure(abort, "gesture aborted",
                new Dictionary<string, object?> { ["cleanupComplete"] = true, ["pressDispatched"] = false }),
            () => closed = true));
        Assert.That(closed, Is.True);
    }

    [Test]
    public void CleanupExceptionsDoNotSkipOtherResourcesAndRemainVisibleTogether()
    {
        var lastClosed = false;
        var failure = Assert.Throws<AggregateException>(() => GatewayNativeWindowShutdown.Cleanup(null,
            () => throw new InvalidOperationException("input"),
            () => throw new InvalidOperationException("transport"),
            () => lastClosed = true));
        Assert.That(lastClosed, Is.True);
        Assert.That(failure!.InnerExceptions.Select(error => error.Message), Is.EqualTo(new[] { "input", "transport" }));
    }

    [Test]
    public void PendingDeferredCleanupRetainsOwnershipUntilAnActualTerminalReceipt()
    {
        var registry = new GatewayAutomationRegistry();
        var operation = new Pending();
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor("fixture.pending", "1", "fixture",
            new Dictionary<string, object?>(), null, true), (_, _) => operation);
        var run = registry.StartRun("fixture.pending", "pending-request", new Dictionary<string, object?>());
        var lifecycle = new GatewayShutdownLifecycle();
        var now = DateTimeOffset.UtcNow;
        var first = lifecycle.TryShutdown(() => GatewayNativeWindowShutdown.Cleanup(registry, () => null), true, now);
        Assert.That(first.RetainsOwnership, Is.True);
        Assert.That(operation.StopRequested, Is.True);
        Assert.That(run.CompletedUtc, Is.Null);
        operation.Complete = true;
        var last = lifecycle.TryShutdown(() => GatewayNativeWindowShutdown.Cleanup(registry, () => null), true, now.AddSeconds(2));
        Assert.That(last.IsTerminal, Is.True);
        Assert.That(run.State, Is.EqualTo("cancelled"));
    }

    private sealed class Pending : IGatewayDeferredAutomationOperation
    {
        public bool Complete { get; set; }
        public bool StopRequested { get; private set; }
        public GatewayDeferredAutomationResult? Advance(bool stopRequested)
        {
            StopRequested |= stopRequested;
            return Complete ? GatewayDeferredAutomationResult.Success("released") : null;
        }
    }
}
