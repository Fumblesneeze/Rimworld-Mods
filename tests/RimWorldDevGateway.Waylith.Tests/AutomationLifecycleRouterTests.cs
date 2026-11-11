using System.Text;
using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class AutomationLifecycleRouterTests
{
    [Test]
    public void StatusReturnsTheRetainedPendingRunWithoutStartingOrAdvancingWork()
    {
        var registry = new GatewayAutomationRegistry();
        var starts = 0;
        var operation = new PendingOperation();
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor("native.click", "1", "fixture",
            new Dictionary<string, object?>(), null, true), (_, _) => { starts++; return operation; });
        var run = registry.StartRun("native.click", "initial-request", new Dictionary<string, object?>());
        var dispatcher = new GatewayDispatcher();
        var router = new GatewayApiRouter(dispatcher, new State(), new GatewayLogBuffer(),
            new GatewayApiServices(automations: registry), responseTimeout: TimeSpan.FromSeconds(3));

        var response = Dispatch(router, dispatcher, "GET", "/api/v1/automation-runs/" + run.RunId);

        Assert.That(response.StatusCode, Is.EqualTo(200), Encoding.UTF8.GetString(response.Body));
        Assert.That(Encoding.UTF8.GetString(response.Body), Does.Contain("\"State\":\"running\""));
        Assert.That(Encoding.UTF8.GetString(response.Body), Does.Contain(run.RunId));
        Assert.That(starts, Is.EqualTo(1));
        Assert.That(operation.Calls, Is.Zero);
    }

    [Test]
    public void CancelAcknowledgesIntentThenStatusTracksCleanupWithoutReplaying()
    {
        var registry = new GatewayAutomationRegistry();
        var operation = new PendingOperation();
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor("native.click", "1", "fixture",
            new Dictionary<string, object?>(), null, true), (_, _) => operation);
        var run = registry.StartRun("native.click", "initial-request", new Dictionary<string, object?>());
        var dispatcher = new GatewayDispatcher();
        var router = new GatewayApiRouter(dispatcher, new State(), new GatewayLogBuffer(),
            new GatewayApiServices(automations: registry), responseTimeout: TimeSpan.FromSeconds(3));

        var response = Dispatch(router, dispatcher, "POST", "/api/v1/automation-runs/" + run.RunId + "/cancel", "{}");

        Assert.That(response.StatusCode, Is.EqualTo(200), Encoding.UTF8.GetString(response.Body));
        Assert.That(Encoding.UTF8.GetString(response.Body), Does.Contain("\"cancellationRequested\":true"));
        Assert.That(run.State, Is.EqualTo("cancel-requested"));
        Assert.That(operation.Calls, Is.Zero);
        registry.Advance();
        Assert.That(operation.StopRequested, Is.True);
        Assert.That(run.CompletedUtc, Is.Null);
        operation.Complete = true;
        registry.Advance();
        var terminal = Dispatch(router, dispatcher, "GET", "/api/v1/automation-runs/" + run.RunId);
        Assert.That(Encoding.UTF8.GetString(terminal.Body), Does.Contain("\"State\":\"cancelled\""));
        Assert.That(Encoding.UTF8.GetString(terminal.Body), Does.Contain("owned input released"));
    }

    [Test]
    public void ReturnedPendingRunKeepsItsDeadlineAfterTheHttpRequestHasFinished()
    {
        var now = DateTimeOffset.UtcNow;
        var registry = new GatewayAutomationRegistry(utcNow: () => now);
        var operation = new PendingOperation();
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor("native.click", "1", "fixture",
            new Dictionary<string, object?>(), null, true), (_, _) => operation);
        var dispatcher = new GatewayDispatcher();
        var router = new GatewayApiRouter(dispatcher, new State(), new GatewayLogBuffer(),
            new GatewayApiServices(automations: registry), responseTimeout: TimeSpan.FromSeconds(3));

        var response = Dispatch(router, dispatcher, "POST", "/api/v1/automations/native.click/runs", "{\"arguments\":{}}");
        Assert.That(response.StatusCode, Is.EqualTo(200), Encoding.UTF8.GetString(response.Body));
        var run = registry.ListRuns().Single();
        Assert.That(run.State, Is.EqualTo("running"));
        Assert.That(run.DeadlineUtc, Is.EqualTo(now.AddSeconds(3)));

        now = now.AddSeconds(3);
        registry.Advance();
        Assert.That(operation.StopRequested, Is.True);
        Assert.That(run.CompletedUtc, Is.Null);
        operation.Complete = true;
        registry.Advance();
        Assert.That(run.State, Is.EqualTo("timed-out"));
        Assert.That(run.Result, Is.EqualTo("owned input released"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void StatusAndCancellationRetainTheStartRoutesLargeAndDeepResultContract(bool deep)
    {
        object result = deep ? "leaf" : new string('x', 5 * 1024 * 1024);
        if (deep)
            for (var depth = 0; depth < 14; depth++) result = new Dictionary<string, object?> { ["child"] = result };
        var registry = new GatewayAutomationRegistry();
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor("fixture.result", "1", "fixture",
            new Dictionary<string, object?>(), null, false), (_, _) => result);
        var dispatcher = new GatewayDispatcher();
        var router = new GatewayApiRouter(dispatcher, new State(), new GatewayLogBuffer(),
            new GatewayApiServices(automations: registry), responseTimeout: TimeSpan.FromSeconds(3));
        var start = Dispatch(router, dispatcher, "POST", "/api/v1/automations/fixture.result/runs", "{\"arguments\":{}}");
        Assert.That(start.StatusCode, Is.EqualTo(200), Encoding.UTF8.GetString(start.Body).Substring(0, Math.Min(300, start.Body.Length)));
        var id = registry.ListRuns().Single().RunId;
        var status = Dispatch(router, dispatcher, "GET", "/api/v1/automation-runs/" + id);
        Assert.That(status.StatusCode, Is.EqualTo(200), Encoding.UTF8.GetString(status.Body).Substring(0, Math.Min(300, status.Body.Length)));
        var cancel = Dispatch(router, dispatcher, "POST", "/api/v1/automation-runs/" + id + "/cancel", "{}");
        Assert.That(cancel.StatusCode, Is.EqualTo(200), Encoding.UTF8.GetString(cancel.Body));
        Assert.That(cancel.Body.Length, Is.LessThan(1024), "Cancellation needs a small identity/intent receipt, not a repeated result.");
        Assert.That(Encoding.UTF8.GetString(cancel.Body), Does.Contain(id));
    }

    private static GatewayHttpResponse Dispatch(GatewayApiRouter router, GatewayDispatcher dispatcher,
        string method, string path, string body = "")
    {
        var request = new GatewayHttpRequest(method, path, path, string.Empty,
            new Dictionary<string, string> { ["Content-Type"] = "application/json" }, Encoding.UTF8.GetBytes(body));
        var task = Task.Run(() => router.Handle(request, "lifecycle-request"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount != 0 || task.IsCompleted, 2000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        return task.GetAwaiter().GetResult();
    }

    private sealed class State : IGatewayStateProvider
    {
        public object CaptureStatus() => new { };
        public object CaptureUiState() => new { };
    }

    private sealed class PendingOperation : IGatewayDeferredAutomationOperation
    {
        public int Calls { get; private set; }
        public bool StopRequested { get; private set; }
        public bool Complete { get; set; }
        public GatewayDeferredAutomationResult? Advance(bool stopRequested)
        {
            Calls++;
            StopRequested = stopRequested;
            return Complete ? GatewayDeferredAutomationResult.Success("owned input released") : null;
        }
    }
}
