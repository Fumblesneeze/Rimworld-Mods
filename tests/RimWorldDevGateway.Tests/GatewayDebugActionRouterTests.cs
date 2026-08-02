using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayDebugActionRouterTests
{
    [Test]
    public void Debug_action_routes_query_and_invoke_an_exact_immediate_leaf()
    {
        var invoked = 0;
        var registry = new GatewayDebugActionRegistry(new FakeSource(
            new FakeCandidate(
                new GatewayDebugActionCandidateSnapshot(
                    identity: "native-spawn-weapon",
                    path: "Spawning\\Spawn weapon",
                    label: "Spawn weapon",
                    category: "Spawning",
                    allowedGameStates: "PlayingOnMap",
                    runtimeType: "LudeonTK.DebugActionNode",
                    mode: GatewayDebugActionMode.Immediate,
                    visible: true,
                    active: true,
                    on: false),
                () => invoked++)));
        var dispatcher = new GatewayDispatcher();
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(debugActions: registry),
            responseTimeout: TimeSpan.FromSeconds(2));

        var queryResponse = Dispatch(
            dispatcher,
            router,
            Post(
                "/api/v1/dev-tools/actions/query",
                "{\"search\":\"spawn\",\"categories\":[\"Spawning\"]," +
                "\"modes\":[\"Immediate\"],\"limit\":10}"),
            "query-debug-actions");
        var queryBody = Encoding.UTF8.GetString(queryResponse.Body);
        var handle = registry.Query(new GatewayDebugActionQuery(
            "spawn",
            new[] { "Spawning" },
            new[] { GatewayDebugActionMode.Immediate },
            after: null,
            limit: 10)).Items.Single().Handle;

        var invokeResponse = Dispatch(
            dispatcher,
            router,
            Post(
                "/api/v1/dev-tools/actions/" + Uri.EscapeDataString(handle) + "/invoke",
                "{}"),
            "invoke-debug-action");
        var invokeBody = Encoding.UTF8.GetString(invokeResponse.Body);

        Assert.Multiple(() =>
        {
            Assert.That(queryResponse.StatusCode, Is.EqualTo(200), queryBody);
            Assert.That(queryBody, Does.Contain("Spawning\\\\Spawn weapon"));
            Assert.That(queryBody, Does.Contain(handle));
            Assert.That(invokeResponse.StatusCode, Is.EqualTo(200), invokeBody);
            Assert.That(invokeBody, Does.Contain("\"Completed\":true"));
            Assert.That(invokeBody, Does.Contain("\"PointerRequired\":false"));
            Assert.That(invoked, Is.EqualTo(1));
        });
    }

    [Test]
    public void First_native_debug_tree_generation_uses_its_documented_longer_deadline()
    {
        var registry = new GatewayDebugActionRegistry(new SlowSource(
            TimeSpan.FromMilliseconds(75),
            new FakeCandidate(
                new GatewayDebugActionCandidateSnapshot(
                    "slow-native-tree",
                    "General\\Clear cached materials",
                    "Clear cached materials",
                    "General",
                    "PlayingOnMap",
                    "LudeonTK.DebugActionNode",
                    GatewayDebugActionMode.Immediate,
                    visible: true,
                    active: true,
                    on: false),
                () => { })));
        var dispatcher = new GatewayDispatcher();
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(debugActions: registry),
            responseTimeout: TimeSpan.FromMilliseconds(15));

        var response = Dispatch(
            dispatcher,
            router,
            Post(
                "/api/v1/dev-tools/actions/query",
                "{\"search\":\"Clear cached materials\",\"limit\":10}"),
            "slow-first-debug-tree");
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.That(response.StatusCode, Is.EqualTo(200), body);
    }

    [Test]
    public void Debug_action_invocation_uses_the_normal_route_deadline()
    {
        var started = new ManualResetEventSlim(false);
        var release = new ManualResetEventSlim(false);
        var registry = new GatewayDebugActionRegistry(new FakeSource(
            new FakeCandidate(
                new GatewayDebugActionCandidateSnapshot(
                    "blocking-native-action",
                    "General\\Blocking action",
                    "Blocking action",
                    "General",
                    "PlayingOnMap",
                    "LudeonTK.DebugActionNode",
                    GatewayDebugActionMode.Immediate,
                    visible: true,
                    active: true,
                    on: false),
                () =>
                {
                    started.Set();
                    release.Wait(TimeSpan.FromSeconds(5));
                })));
        var handle = registry.Query(new GatewayDebugActionQuery(
            "Blocking action",
            Array.Empty<string>(),
            Array.Empty<GatewayDebugActionMode>(),
            after: null,
            limit: 10)).Items.Single().Handle;
        var dispatcher = new GatewayDispatcher();
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(debugActions: registry),
            responseTimeout: TimeSpan.FromMilliseconds(250));

        var responseTask = Task.Run(() => router.Handle(
            Post(
                "/api/v1/dev-tools/actions/" + Uri.EscapeDataString(handle) + "/invoke",
                "{}"),
            "bounded-debug-invoke"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        Exception? drainFailure = null;
        var drainThread = new Thread(() =>
        {
            try
            {
                dispatcher.Drain(DispatchPhase.Update);
            }
            catch (Exception exception)
            {
                drainFailure = exception;
            }
        });
        drainThread.Start();
        Assert.That(started.Wait(1000), Is.True);
        try
        {
            Assert.That(
                responseTask.Wait(500),
                Is.True,
                "an already-discovered action must not inherit the discovery warm-up deadline");
            var response = responseTask.GetAwaiter().GetResult();
            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(504));
                Assert.That(
                    Encoding.UTF8.GetString(response.Body),
                    Does.Contain("\"code\":\"response_timeout_after_start\""));
            });
        }
        finally
        {
            release.Set();
            Assert.That(drainThread.Join(1000), Is.True);
            Assert.That(drainFailure, Is.Null);
        }
    }

    [Test]
    public void Native_debug_tree_initialization_failure_returns_a_stable_server_error()
    {
        var registry = new GatewayDebugActionRegistry(new ThrowingSource());
        var dispatcher = new GatewayDispatcher();
        var logs = new GatewayLogBuffer();
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            logs,
            new GatewayApiServices(debugActions: registry),
            responseTimeout: TimeSpan.FromSeconds(2));

        var response = Dispatch(
            dispatcher,
            router,
            Post("/api/v1/dev-tools/actions/query", "{\"limit\":10}"),
            "failed-debug-tree");
        var body = Encoding.UTF8.GetString(response.Body);
        var diagnostic = logs.Read(0, 10).Entries.Single(entry => entry.RequestId == "failed-debug-tree");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(500), body);
            Assert.That(body, Does.Contain("\"code\":\"debug_action_discovery_failed\""));
            Assert.That(body, Does.Not.Contain("InvalidOperationException"));
            Assert.That(diagnostic.Message, Does.Contain("debug-action discovery failed"));
            Assert.That(diagnostic.Stack, Does.Contain("native debug setup detail"));
        });
    }

    private static GatewayHttpResponse Dispatch(
        GatewayDispatcher dispatcher,
        GatewayApiRouter router,
        GatewayHttpRequest request,
        string requestId)
    {
        var responseTask = Task.Run(() => router.Handle(request, requestId));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        return responseTask.GetAwaiter().GetResult();
    }

    private static GatewayHttpRequest Post(string path, string json) => new(
        "POST",
        path,
        path,
        string.Empty,
        new Dictionary<string, string>(),
        Encoding.UTF8.GetBytes(json));

    private sealed class StubStateProvider : IGatewayStateProvider
    {
        public object CaptureStatus() => new { };

        public object CaptureUiState() => new { };
    }

    private sealed class FakeSource : IGatewayDebugActionSource
    {
        private readonly IReadOnlyList<IGatewayDebugActionCandidate> candidates;

        public FakeSource(params IGatewayDebugActionCandidate[] candidates)
        {
            this.candidates = candidates;
        }

        public IReadOnlyList<IGatewayDebugActionCandidate> Discover(int maximumCandidates) =>
            candidates.Take(maximumCandidates).ToArray();
    }

    private sealed class FakeCandidate : IGatewayDebugActionCandidate
    {
        private readonly GatewayDebugActionCandidateSnapshot snapshot;
        private readonly Action invoke;

        public FakeCandidate(GatewayDebugActionCandidateSnapshot snapshot, Action invoke)
        {
            this.snapshot = snapshot;
            this.invoke = invoke;
        }

        public GatewayDebugActionCandidateSnapshot Capture() => snapshot;

        public void Invoke() => invoke();
    }

    private sealed class SlowSource : IGatewayDebugActionSource
    {
        private readonly TimeSpan delay;
        private readonly IReadOnlyList<IGatewayDebugActionCandidate> candidates;

        public SlowSource(TimeSpan delay, params IGatewayDebugActionCandidate[] candidates)
        {
            this.delay = delay;
            this.candidates = candidates;
        }

        public IReadOnlyList<IGatewayDebugActionCandidate> Discover(int maximumCandidates)
        {
            Thread.Sleep(delay);
            return candidates.Take(maximumCandidates).ToArray();
        }
    }

    private sealed class ThrowingSource : IGatewayDebugActionSource
    {
        public IReadOnlyList<IGatewayDebugActionCandidate> Discover(int maximumCandidates) =>
            throw new GatewayDebugActionException(
                "debug_action_discovery_failed",
                "RimWorld could not initialize its native debug-action tree.",
                new InvalidOperationException("native debug setup detail"));
    }
}
