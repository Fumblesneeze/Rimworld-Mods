using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class DeferredAutomationTests
{
    [Test]
    public void DeferredRunStaysRunningUntilNativeCompletionAndReturnsItsResult()
    {
        var operation = new ControlledOperation();
        var registry = Registry(operation);

        var run = registry.StartRun("native.click", "request", Arguments());

        Assert.That(run.State, Is.EqualTo("running"));
        Assert.That(run.Result, Is.Null);
        Assert.That(operation.Calls, Is.Zero);
        registry.Advance();
        Assert.That(operation.Calls, Is.EqualTo(1));
        Assert.That(run.State, Is.EqualTo("running"));
        operation.Outcome = GatewayDeferredAutomationResult.Success("input dispatched; no widget claim");
        registry.Advance();
        Assert.That(run.State, Is.EqualTo("succeeded"));
        Assert.That(run.Result, Is.EqualTo("input dispatched; no widget claim"));
        registry.Advance();
        Assert.That(operation.Calls, Is.EqualTo(2));
    }

    [Test]
    public void CancellationWaitsForCleanupAndRetainsAlreadyDispatchedResult()
    {
        var operation = new ControlledOperation();
        var registry = Registry(operation);
        var run = registry.StartRun("native.click", "request", Arguments());
        registry.Advance();
        Assert.That(registry.Cancel(run.RunId), Is.True);
        Assert.That(operation.Calls, Is.EqualTo(1), "Cancellation must not run native cleanup on its caller.");

        registry.Advance();
        Assert.That(operation.StopRequests, Is.EqualTo(new[] { false, true }));
        Assert.That(run.State, Is.EqualTo("cancel-requested"));
        Assert.That(run.CompletedUtc, Is.Null);
        operation.Outcome = GatewayDeferredAutomationResult.Success("press dispatched; release cleaned");
        registry.Advance();

        Assert.That(run.State, Is.EqualTo("cancelled"));
        Assert.That(run.Error!.Code, Is.EqualTo("automation_cancelled"));
        Assert.That(run.Result, Is.EqualTo("press dispatched; release cleaned"));
        Assert.That(registry.Cancel(run.RunId), Is.False);
    }

    [Test]
    public void DeadlineStopsWorkButDoesNotFinishUntilCleanupCompletes()
    {
        var now = DateTimeOffset.UtcNow;
        var operation = new ControlledOperation();
        var registry = new GatewayAutomationRegistry(utcNow: () => now);
        registry.RegisterBuiltIn(Descriptor(), (_, _) => operation);
        var run = registry.StartRun("native.click", "request", Arguments(), timeout: TimeSpan.FromSeconds(1));
        now += TimeSpan.FromSeconds(1);

        registry.Advance();
        Assert.That(operation.StopRequests, Is.EqualTo(new[] { true }));
        Assert.That(run.CompletedUtc, Is.Null);
        operation.Outcome = GatewayDeferredAutomationResult.Success("no input remains held");
        registry.Advance();

        Assert.That(run.State, Is.EqualTo("timed-out"));
        Assert.That(run.Error!.Code, Is.EqualTo("automation_timed_out"));
        Assert.That(run.Result, Is.EqualTo("no input remains held"));
    }

    [Test]
    public void RequestCancellationRemainsLinkedAfterStartReturns()
    {
        using var cancellation = new CancellationTokenSource();
        var operation = new ControlledOperation();
        var registry = Registry(operation);
        var run = registry.StartRun("native.click", "request", Arguments(), cancellationToken: cancellation.Token);

        cancellation.Cancel();
        Assert.That(run.State, Is.EqualTo("cancel-requested"));
        registry.Advance();
        Assert.That(operation.StopRequests, Is.EqualTo(new[] { true }));
        operation.Outcome = GatewayDeferredAutomationResult.Success();
        registry.Advance();
        Assert.That(run.State, Is.EqualTo("cancelled"));
    }

    [Test]
    public void UnexpectedFailureRequestsCleanupBeforePublishingFailure()
    {
        var operation = new ControlledOperation { NextFailure = new InvalidOperationException("dispatch failed") };
        var registry = Registry(operation);
        var run = registry.StartRun("native.click", "request", Arguments());

        Assert.DoesNotThrow(registry.Advance);
        Assert.That(run.CompletedUtc, Is.Null);
        registry.Advance();
        Assert.That(operation.StopRequests, Is.EqualTo(new[] { false, true }));
        operation.Outcome = GatewayDeferredAutomationResult.Success("owned button released");
        registry.Advance();

        Assert.That(run.State, Is.EqualTo("failed"));
        Assert.That(run.Error!.Code, Is.EqualTo("automation_failed"));
        Assert.That(run.Error.Message, Is.EqualTo("dispatch failed"));
        Assert.That(run.Result, Is.EqualTo("owned button released"));
    }

    [Test]
    public void CleanupExceptionTerminatesExplicitlyWithoutRetryingOrClaimingCleanInput()
    {
        var operation = new ControlledOperation { NextFailure = new InvalidOperationException("dispatch failed") };
        var registry = Registry(operation);
        var run = registry.StartRun("native.click", "request", Arguments());
        registry.Advance();
        operation.NextFailure = new InvalidOperationException("release failed");

        registry.Advance();

        Assert.That(run.State, Is.EqualTo("failed"));
        Assert.That(run.Error!.Code, Is.EqualTo("automation_cleanup_incomplete"));
        Assert.That(run.Error.Message, Does.Contain("release failed"));
        Assert.That(run.Result, Is.Null);
        registry.Advance();
        Assert.That(operation.Calls, Is.EqualTo(2));
    }

    [Test]
    public void PendingRunDoesNotPreventBoundedEvictionOfLaterTerminalRuns()
    {
        var operation = new ControlledOperation();
        var registry = new GatewayAutomationRegistry(historyCapacity: 2);
        int calls = 0;
        registry.RegisterBuiltIn(Descriptor(), (_, _) => ++calls == 1 ? operation : "done");
        var pending = registry.StartRun("native.click", "first", Arguments(), "same-gesture");

        for (int index = 0; index < 6; index++)
        {
            registry.StartRun("native.click", "terminal-" + index, Arguments());
            Assert.That(registry.ListRuns().Count, Is.LessThanOrEqualTo(2));
            Assert.That(registry.GetRun(pending.RunId), Is.SameAs(pending));
        }
        Assert.That(registry.StartRun("native.click", "replay", Arguments(), "same-gesture"), Is.SameAs(pending));
        Assert.That(calls, Is.EqualTo(7));
    }

    [Test]
    public void AllActiveCapacityRejectsBeforeInvokingAnotherHandler()
    {
        var registry = new GatewayAutomationRegistry(historyCapacity: 1);
        int calls = 0;
        registry.RegisterBuiltIn(Descriptor(), (_, _) => { calls++; return new ControlledOperation(); });
        var first = registry.StartRun("native.click", "first", Arguments(), "retry");

        var failure = Assert.Throws<GatewayAutomationException>(() =>
            registry.StartRun("native.click", "second", Arguments()));

        Assert.That(failure!.Code, Is.EqualTo("automation_capacity_reached"));
        Assert.That(calls, Is.EqualTo(1));
        Assert.That(registry.ListRuns(), Has.Count.EqualTo(1));
        Assert.That(registry.StartRun("native.click", "replay", Arguments(), "retry"), Is.SameAs(first));
    }

    [Test]
    public void DeferredWorkCannotAdvanceOnAnotherThread()
    {
        var operation = new ControlledOperation();
        var registry = Registry(operation);
        var run = registry.StartRun("native.click", "request", Arguments());
        var failure = Task.Run(() => Assert.Throws<GatewayAutomationException>(registry.Advance)).GetAwaiter().GetResult();

        Assert.That(failure!.Code, Is.EqualTo("automation_thread_mismatch"));
        Assert.That(operation.Calls, Is.Zero);
        Assert.That(run.State, Is.EqualTo("running"));
        operation.Outcome = GatewayDeferredAutomationResult.Success();
        registry.Advance();
        Assert.That(run.State, Is.EqualTo("succeeded"));
    }

    [Test]
    public void DifferentOriginalThreadsCanCompleteTheirOwnWorkAndCleanupIndependently()
    {
        var callerOperation = new ControlledOperation { Outcome = GatewayDeferredAutomationResult.Success("caller complete") };
        var workerOperation = new ControlledOperation();
        var registry = new GatewayAutomationRegistry();
        registry.RegisterBuiltIn(Descriptor(), (context, _) => context.RequestId == "worker" ? workerOperation : callerOperation);
        var callerRun = registry.StartRun("native.click", "caller", Arguments());
        GatewayAutomationRun? workerRun = null;
        Exception? workerFailure = null;
        using var ready = new ManualResetEventSlim();
        using var continueWorker = new ManualResetEventSlim();
        var worker = new Thread(() =>
        {
            try
            {
                workerRun = registry.StartRun("native.click", "worker", Arguments());
                ready.Set();
                if (!continueWorker.Wait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("Test did not release the worker.");
                registry.Advance();
                Assert.That(workerRun.CompletedUtc, Is.Null, "Cancellation must wait for worker-owned cleanup.");
                workerOperation.Outcome = GatewayDeferredAutomationResult.Success("worker cleanup complete");
                registry.Advance();
            }
            catch (Exception exception) { workerFailure = exception; }
            finally { ready.Set(); }
        }) { IsBackground = true };
        worker.Start();
        try
        {
            Assert.That(ready.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(workerFailure, Is.Null);
            Assert.That(workerRun, Is.Not.Null);

            registry.Advance();

            Assert.That(callerRun.State, Is.EqualTo("succeeded"));
            Assert.That(callerRun.Result, Is.EqualTo("caller complete"));
            Assert.That(callerOperation.Calls, Is.EqualTo(1));
            Assert.That(workerOperation.Calls, Is.Zero, "Caller must not dispatch another thread's work.");
            Assert.That(registry.Cancel(workerRun!.RunId), Is.True);
        }
        finally
        {
            continueWorker.Set();
            Assert.That(worker.Join(TimeSpan.FromSeconds(5)), Is.True, "Test worker must exit.");
        }
        Assert.That(workerFailure, Is.Null);
        Assert.That(workerOperation.StopRequests, Is.EqualTo(new[] { true, true }));
        Assert.That(workerRun!.State, Is.EqualTo("cancelled"));
        Assert.That(workerRun.Result, Is.EqualTo("worker cleanup complete"));
        Assert.That(callerOperation.Calls, Is.EqualTo(1));
    }

    [Test]
    public void NestedAdvanceCannotDispatchTheSamePhaseTwice()
    {
        var operation = new ControlledOperation { Outcome = GatewayDeferredAutomationResult.Success() };
        var registry = Registry(operation);
        operation.OnAdvance = () =>
        {
            operation.OnAdvance = null;
            var failure = Assert.Throws<GatewayAutomationException>(registry.Advance);
            Assert.That(failure!.Code, Is.EqualTo("automation_advance_reentrant"));
        };
        var run = registry.StartRun("native.click", "request", Arguments());

        registry.Advance();

        Assert.That(operation.Calls, Is.EqualTo(1));
        Assert.That(run.State, Is.EqualTo("succeeded"));
    }

    [Test]
    public void CancellationDuringNativeStepStillWaitsForCleanupAsCancellation()
    {
        var operation = new ControlledOperation();
        var registry = new GatewayAutomationRegistry();
        registry.RegisterBuiltIn(Descriptor(), (context, _) =>
        {
            operation.OnAdvance = () =>
            {
                operation.OnAdvance = null;
                registry.Cancel(context.RunId);
                context.RunStep("before-input", () => Assert.Fail("Cancelled input must not be injected."));
            };
            return operation;
        });
        var run = registry.StartRun("native.click", "request", Arguments());
        registry.Advance();
        Assert.That(run.CompletedUtc, Is.Null);
        operation.Outcome = GatewayDeferredAutomationResult.Success("cleanup finished");
        registry.Advance();
        Assert.That(run.State, Is.EqualTo("cancelled"));
        Assert.That(run.Result, Is.EqualTo("cleanup finished"));
    }

    [Test]
    public void NativeRejectionIsFailedWithItsPartialResult()
    {
        var operation = new ControlledOperation
        {
            Outcome = GatewayDeferredAutomationResult.Failure("stale_window_instance", "Window replaced", "press occurred")
        };
        var registry = Registry(operation);
        var run = registry.StartRun("native.click", "request", Arguments());
        registry.Advance();
        Assert.That(run.State, Is.EqualTo("failed"));
        Assert.That(run.Error!.Code, Is.EqualTo("stale_window_instance"));
        Assert.That(run.Result, Is.EqualTo("press occurred"));
    }

    private static GatewayAutomationRegistry Registry(ControlledOperation operation)
    {
        var registry = new GatewayAutomationRegistry();
        registry.RegisterBuiltIn(Descriptor(), (_, _) => operation);
        return registry;
    }

    private static GatewayAutomationDescriptor Descriptor() => new("native.click", "1.0",
        "Native click lifecycle fixture", new Dictionary<string, object?>(), null, true);

    private static IReadOnlyDictionary<string, object?> Arguments() => new Dictionary<string, object?>();

    private sealed class ControlledOperation : IGatewayDeferredAutomationOperation
    {
        public int Calls { get; private set; }
        public GatewayDeferredAutomationResult? Outcome { get; set; }
        public List<bool> StopRequests { get; } = new();
        public Exception? NextFailure { get; set; }
        public Action? OnAdvance { get; set; }
        public GatewayDeferredAutomationResult? Advance(bool stopRequested)
        {
            Calls++;
            OnAdvance?.Invoke();
            StopRequests.Add(stopRequested);
            var failure = NextFailure;
            NextFailure = null;
            if (failure != null) throw failure;
            return Outcome;
        }
    }
}
