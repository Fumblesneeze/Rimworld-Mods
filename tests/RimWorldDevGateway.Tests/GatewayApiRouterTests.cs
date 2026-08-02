using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayApiRouterTests
{
    [Test]
    public void Status_is_captured_on_the_dispatcher_and_wrapped_in_a_correlated_envelope()
    {
        var dispatcher = new GatewayDispatcher(capacity: 4);
        var state = new StubStateProvider();
        var router = new GatewayApiRouter(
            dispatcher,
            state,
            new GatewayLogBuffer(capacity: 16),
            responseTimeout: TimeSpan.FromSeconds(2));
        var request = Parse("GET /api/v1/status HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n");

        var responseTask = Task.Run(() => router.Handle(request, "router-request"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(state.CaptureThreadId, Is.EqualTo(Thread.CurrentThread.ManagedThreadId));
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(body, Does.Contain("\"apiVersion\":\"1\""));
            Assert.That(body, Does.Contain("\"requestId\":\"router-request\""));
            Assert.That(body, Does.Contain("\"ok\":true"));
            Assert.That(body, Does.Contain("\"programState\":\"Entry\""));
        });
    }

    [Test]
    public void Http_timeout_cancels_queued_snapshot_and_reports_that_it_never_started()
    {
        var dispatcher = new GatewayDispatcher(capacity: 4);
        var state = new StubStateProvider();
        var router = new GatewayApiRouter(
            dispatcher,
            state,
            new GatewayLogBuffer(capacity: 16),
            responseTimeout: TimeSpan.FromMilliseconds(75));
        var request = Parse("GET /api/v1/status HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n");

        var responseTask = Task.Run(() => router.Handle(request, "queued-timeout"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);
        dispatcher.Drain(DispatchPhase.Update);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(504));
            Assert.That(body, Does.Contain("\"code\":\"timed_out_before_start\""));
            Assert.That(state.CaptureCount, Is.Zero);
            Assert.That(dispatcher.PendingCount, Is.Zero);
        });
    }

    [Test]
    public void Http_client_cancellation_prevents_a_queued_snapshot_from_running()
    {
        var dispatcher = new GatewayDispatcher(capacity: 4);
        var state = new StubStateProvider();
        var router = new GatewayApiRouter(
            dispatcher,
            state,
            new GatewayLogBuffer(capacity: 16),
            responseTimeout: TimeSpan.FromSeconds(2));
        using var cancellation = new CancellationTokenSource();
        var request = Parse(
            "GET /api/v1/status HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n",
            cancellation.Token);

        var responseTask = Task.Run(() => router.Handle(request, "client-cancelled"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        cancellation.Cancel();

        Assert.That(
            () => responseTask.GetAwaiter().GetResult(),
            Throws.InstanceOf<OperationCanceledException>());
        dispatcher.Drain(DispatchPhase.Update);
        Assert.Multiple(() =>
        {
            Assert.That(state.CaptureCount, Is.Zero);
            Assert.That(dispatcher.PendingCount, Is.Zero);
        });
    }

    [Test]
    public void Http_timeout_reports_when_snapshot_had_already_started()
    {
        var dispatcher = new GatewayDispatcher(capacity: 4);
        var state = new BlockingStateProvider();
        var router = new GatewayApiRouter(
            dispatcher,
            state,
            new GatewayLogBuffer(capacity: 16),
            responseTimeout: TimeSpan.FromMilliseconds(75));
        var request = Parse("GET /api/v1/status HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n");

        var responseTask = Task.Run(() => router.Handle(request, "started-timeout"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        var drainTask = Task.Run(() => dispatcher.Drain(DispatchPhase.Update));
        Assert.That(state.Started.Wait(1000), Is.True);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);
        state.Release.Set();
        drainTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(504));
            Assert.That(body, Does.Contain("\"code\":\"response_timeout_after_start\""));
            Assert.That(state.CaptureCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Http_timeout_immediately_warns_that_started_work_may_still_be_running()
    {
        var dispatcher = new GatewayDispatcher(capacity: 4);
        var state = new BlockingStateProvider();
        var logs = new GatewayLogBuffer(capacity: 16);
        var router = new GatewayApiRouter(
            dispatcher,
            state,
            logs,
            responseTimeout: TimeSpan.FromMilliseconds(75));
        var request = Parse("GET /api/v1/status HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n");

        var responseTask = Task.Run(() => router.Handle(request, "started-timeout-warning"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        var drainTask = Task.Run(() => dispatcher.Drain(DispatchPhase.Update));
        Assert.That(state.Started.Wait(1000), Is.True);
        var response = responseTask.GetAwaiter().GetResult();
        try
        {
            var entry = logs.Read(0, 16).Entries.Single(
                item => item.RequestId == "started-timeout-warning");
            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(504));
                Assert.That(entry.Severity, Is.EqualTo("Warning"));
                Assert.That(entry.Message, Does.Contain("may still be running"));
                Assert.That(entry.Message, Does.Contain("status"));
            });
        }
        finally
        {
            state.Release.Set();
            drainTask.GetAwaiter().GetResult();
        }
    }

    [Test]
    public void Http_timeout_observes_and_correlates_a_late_main_thread_exception()
    {
        var dispatcher = new GatewayDispatcher(capacity: 4);
        var state = new ThrowingBlockingStateProvider();
        var logs = new GatewayLogBuffer(capacity: 16);
        var router = new GatewayApiRouter(
            dispatcher,
            state,
            logs,
            responseTimeout: TimeSpan.FromMilliseconds(75));
        var request = Parse("GET /api/v1/status HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n");

        var responseTask = Task.Run(() => router.Handle(request, "late-fault-request"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        var drainTask = Task.Run(() => dispatcher.Drain(DispatchPhase.Update));
        Assert.That(state.Started.Wait(1000), Is.True);
        var response = responseTask.GetAwaiter().GetResult();
        state.Release.Set();
        drainTask.GetAwaiter().GetResult();

        Assert.That(SpinWait.SpinUntil(
            () => logs.Read(0, 16).Entries.Any(entry =>
                entry.RequestId == "late-fault-request" && entry.Severity == "Error"),
            1000), Is.True);
        var entry = logs.Read(0, 16).Entries.Single(item =>
            item.RequestId == "late-fault-request" && item.Severity == "Error");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(504));
            Assert.That(entry.Severity, Is.EqualTo("Error"));
            Assert.That(entry.Message, Does.Contain("completed after its HTTP response timed out"));
            Assert.That(entry.Stack, Does.Contain("late failure detail"));
        });
    }

    [Test]
    public void Dispatcher_saturation_is_normalized_to_the_public_gateway_busy_error()
    {
        var dispatcher = new GatewayDispatcher(capacity: 1);
        var occupied = dispatcher.EnqueueOperation(
            "occupied-request",
            "occupied-operation",
            TimeSpan.FromSeconds(5),
            _ => new object());
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(capacity: 16),
            responseTimeout: TimeSpan.FromSeconds(1));

        var response = router.Handle(
            Parse("GET /api/v1/status HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n"),
            "saturated-request");
        var body = Encoding.UTF8.GetString(response.Body);
        occupied.TryCancelBeforeStart();
        dispatcher.Drain(DispatchPhase.Update);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(503));
            Assert.That(body, Does.Contain("\"code\":\"gateway_busy\""));
            Assert.That(body, Does.Not.Contain("queue_full"));
            Assert.That(body, Does.Contain("\"retryable\":true"));
        });
    }

    private static GatewayHttpRequest Parse(
        string request,
        CancellationToken cancellationToken = default)
    {
        var firstLineEnd = request.IndexOf("\r\n", System.StringComparison.Ordinal);
        var firstLine = request.Substring(0, firstLineEnd);
        var parts = firstLine.Split(' ');
        var target = parts[1];
        var queryStart = target.IndexOf('?');
        return new GatewayHttpRequest(
            parts[0],
            target,
            queryStart < 0 ? target : target.Substring(0, queryStart),
            queryStart < 0 ? string.Empty : target.Substring(queryStart + 1),
            new Dictionary<string, string>(),
            new byte[0],
            cancellationToken);
    }

    private sealed class StubStateProvider : IGatewayStateProvider
    {
        public int CaptureThreadId { get; private set; }

        public int CaptureCount { get; private set; }

        public object CaptureStatus()
        {
            CaptureCount++;
            CaptureThreadId = Thread.CurrentThread.ManagedThreadId;
            return new Dictionary<string, object>
            {
                ["programState"] = "Entry",
                ["unrestrictedExecution"] = true
            };
        }

        public object CaptureUiState() => new Dictionary<string, object>();
    }

    private sealed class BlockingStateProvider : IGatewayStateProvider
    {
        public ManualResetEventSlim Started { get; } = new(false);

        public ManualResetEventSlim Release { get; } = new(false);

        public int CaptureCount { get; private set; }

        public object CaptureStatus()
        {
            CaptureCount++;
            Started.Set();
            Release.Wait(TimeSpan.FromSeconds(5));
            return new Dictionary<string, object>();
        }

        public object CaptureUiState() => new Dictionary<string, object>();
    }

    private sealed class ThrowingBlockingStateProvider : IGatewayStateProvider
    {
        public ManualResetEventSlim Started { get; } = new(false);

        public ManualResetEventSlim Release { get; } = new(false);

        public object CaptureStatus()
        {
            Started.Set();
            Release.Wait(TimeSpan.FromSeconds(5));
            throw new InvalidOperationException("late failure detail");
        }

        public object CaptureUiState() => new Dictionary<string, object>();
    }
}
