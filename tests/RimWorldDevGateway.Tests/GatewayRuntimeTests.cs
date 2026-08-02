using System.IO;
using System.Threading.Tasks;
using RimWorldDevGateway.Contracts;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayRuntimeTests
{
    [Test]
    public void Start_binds_before_publishing_and_is_idempotent()
    {
        var root = NewRoot();
        try
        {
            var currentPath = Path.Combine(root, "DevGateway", "current.json");
            var events = new List<string>();
            var manager = Manager(root);
            FakeTransport? transport = null;
            string? transportToken = null;
            using var runtime = new GatewayRuntime(
                new GatewayRuntimeIdentity(
                    4321,
                    new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
                    "1.6.4871 rev590",
                    "0.1.0"),
                manager,
                new GatewayDispatcher(capacity: 8),
                dispatcher => new StubStateProvider(),
                (token, _) =>
                {
                    transportToken = token;
                    transport = new FakeTransport(41234, () =>
                    {
                        Assert.That(File.Exists(currentPath), Is.False);
                        events.Add("bound");
                    });
                    return transport;
                });

            var first = runtime.Start();
            events.Add("published");
            var second = runtime.Start();
            var published = GatewayContractJson.ReadFile<GatewaySessionManifest>(currentPath);

            Assert.Multiple(() =>
            {
                Assert.That(events, Is.EqualTo(new[] { "bound", "published" }));
                Assert.That(first, Is.SameAs(second));
                Assert.That(runtime.IsRunning, Is.True);
                Assert.That(runtime.Port, Is.EqualTo(41234));
                Assert.That(transport, Is.Not.Null);
                Assert.That(transport!.StartCount, Is.EqualTo(1));
                Assert.That(published.Token, Is.EqualTo(transportToken));
                Assert.That(published.BaseUrl, Is.EqualTo("http://127.0.0.1:41234/api/v1"));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Stop_is_idempotent_and_cleans_transport_dispatch_manifest_and_log_subscription()
    {
        var root = NewRoot();
        try
        {
            var dispatcher = new GatewayDispatcher(capacity: 8);
            var subscription = new TrackingDisposable();
            var transport = new FakeTransport(41235, () => { });
            var runtime = new GatewayRuntime(
                new GatewayRuntimeIdentity(
                    4322,
                    new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
                    "1.6.4871 rev590",
                    "0.1.0"),
                Manager(root),
                dispatcher,
                _ => new StubStateProvider(),
                (_, _) => transport,
                _ => subscription);
            var active = runtime.Start();
            var queued = dispatcher.Enqueue(
                "queued-on-stop",
                "queued",
                TimeSpan.FromSeconds(5),
                _ => "unexpected");

            runtime.Stop();
            runtime.Stop();

            var currentPath = Path.Combine(root, "DevGateway", "current.json");
            var runPath = Path.Combine(root, "DevGateway", "Sessions", "runtime-run", "session.json");
            var stopped = GatewayContractJson.ReadFile<GatewaySessionManifest>(runPath);
            Assert.Multiple(() =>
            {
                Assert.That(runtime.IsStopped, Is.True);
                Assert.That(transport.StopCount, Is.EqualTo(1));
                Assert.That(transport.DisposeCount, Is.EqualTo(1));
                Assert.That(subscription.DisposeCount, Is.EqualTo(1));
                Assert.That(dispatcher.IsStopped, Is.True);
                Assert.That(File.Exists(currentPath), Is.False);
                Assert.That(stopped.State, Is.EqualTo("stopped"));
                Assert.That(stopped.Token, Is.Null.Or.Empty);
                Assert.That(File.ReadAllText(runPath), Does.Not.Contain(active.Token));
                AssertStopping(queued);
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Stop_closes_dispatch_admission_before_stopping_the_transport()
    {
        var root = NewRoot();
        try
        {
            var dispatcher = new GatewayDispatcher(capacity: 8);
            var dispatcherWasStopped = false;
            var transport = new FakeTransport(
                41237,
                () => { },
                () => dispatcherWasStopped = dispatcher.IsStopped);
            using var runtime = new GatewayRuntime(
                new GatewayRuntimeIdentity(
                    4325,
                    new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
                    "1.6.4871 rev590",
                    "0.1.0"),
                Manager(root),
                dispatcher,
                _ => new StubStateProvider(),
                (_, _) => transport);

            runtime.Start();
            runtime.Stop();

            Assert.That(
                dispatcherWasStopped,
                Is.True,
                "transport shutdown must not join workers that can still queue Unity work");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Failed_transport_stop_retains_runtime_ownership_until_a_retry_succeeds()
    {
        var root = NewRoot();
        GatewayRuntime? runtime = null;
        try
        {
            var stopAttempts = 0;
            var transport = new FakeTransport(
                41238,
                () => { },
                () =>
                {
                    stopAttempts++;
                    if (stopAttempts == 1)
                    {
                        throw new IOException("listener still stopping");
                    }
                });
            runtime = new GatewayRuntime(
                new GatewayRuntimeIdentity(
                    4326,
                    new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
                    "1.6.4871 rev590",
                    "0.1.0"),
                Manager(root),
                new GatewayDispatcher(capacity: 8),
                _ => new StubStateProvider(),
                (_, _) => transport);
            runtime.Start();
            var currentPath = Path.Combine(root, "DevGateway", "current.json");

            Assert.That(
                () => runtime.Stop(),
                Throws.TypeOf<AggregateException>()
                    .With.Message.Contains("failed to stop cleanly"));
            Assert.Multiple(() =>
            {
                Assert.That(runtime.IsRunning, Is.False);
                Assert.That(runtime.IsStopped, Is.False);
                Assert.That(transport.StopCount, Is.EqualTo(1));
                Assert.That(transport.DisposeCount, Is.Zero);
                Assert.That(runtime.Port, Is.EqualTo(41238));
                Assert.That(File.Exists(currentPath), Is.True);
            });

            runtime.Stop();
            Assert.Multiple(() =>
            {
                Assert.That(runtime.IsStopped, Is.True);
                Assert.That(transport.StopCount, Is.EqualTo(2));
                Assert.That(transport.DisposeCount, Is.EqualTo(1));
                Assert.That(runtime.Port, Is.Zero);
                Assert.That(File.Exists(currentPath), Is.False);
            });
        }
        finally
        {
            runtime?.Dispose();
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Routed_requests_leave_a_secret_free_started_and_terminal_journal()
    {
        var root = NewRoot();
        try
        {
            Func<GatewayHttpRequest, string, GatewayHttpResponse>? routedHandler = null;
            using var runtime = new GatewayRuntime(
                new GatewayRuntimeIdentity(
                    4324,
                    new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
                    "1.6.4871 rev590",
                    "0.1.0"),
                Manager(root),
                new GatewayDispatcher(capacity: 8),
                _ => new StubStateProvider(),
                (_, handler) =>
                {
                    routedHandler = handler;
                    return new FakeTransport(41236, () => { });
                });

            var active = runtime.Start();
            var response = routedHandler!(
                new GatewayHttpRequest(
                    "GET",
                    "/api/v1/not-found?ignored=true",
                    "/api/v1/not-found",
                    "ignored=true",
                    new Dictionary<string, string>(),
                    Array.Empty<byte>()),
                "journal-request");

            var runDirectory = Path.Combine(root, "DevGateway", "Sessions", "runtime-run");
            var journalPath = Path.Combine(runDirectory, "requests.jsonl");
            var lastPath = Path.Combine(runDirectory, "last-request.json");
            var journal = File.ReadAllText(journalPath);
            var last = File.ReadAllText(lastPath);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(404));
                Assert.That(journal, Does.Contain("\"State\":\"started\""));
                Assert.That(journal, Does.Contain("\"State\":\"completed\""));
                Assert.That(journal, Does.Contain("\"RequestId\":\"journal-request\""));
                Assert.That(journal, Does.Contain("\"Path\":\"/api/v1/not-found\""));
                Assert.That(last, Does.Contain("\"StatusCode\":404"));
                Assert.That(journal, Does.Not.Contain(active.Token));
                Assert.That(last, Does.Not.Contain(active.Token));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Start_failure_rolls_back_every_prepared_resource_without_publishing_credentials()
    {
        var root = NewRoot();
        try
        {
            var dispatcher = new GatewayDispatcher(capacity: 8);
            var subscription = new TrackingDisposable();
            var transport = new FakeTransport(
                0,
                () => throw new InvalidOperationException("bind failed"));
            var runtime = new GatewayRuntime(
                new GatewayRuntimeIdentity(
                    4323,
                    new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
                    "1.6.4871 rev590",
                    "0.1.0"),
                Manager(root),
                dispatcher,
                _ => new StubStateProvider(),
                (_, _) => transport,
                _ => subscription);

            Assert.That(
                () => runtime.Start(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("bind failed"));

            Assert.Multiple(() =>
            {
                Assert.That(runtime.IsStopped, Is.True);
                Assert.That(transport.StopCount, Is.EqualTo(1));
                Assert.That(transport.DisposeCount, Is.EqualTo(1));
                Assert.That(subscription.DisposeCount, Is.EqualTo(1));
                Assert.That(dispatcher.IsStopped, Is.True);
                Assert.That(File.Exists(Path.Combine(root, "DevGateway", "current.json")), Is.False);
                Assert.That(Directory.Exists(Path.Combine(root, "DevGateway", "Sessions")), Is.False);
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static GatewaySessionManager Manager(string root)
    {
        return new GatewaySessionManager(
            root,
            () => Enumerable.Range(0, 32).Select(value => (byte)value).ToArray(),
            () => new DateTimeOffset(2026, 8, 1, 10, 1, 0, TimeSpan.Zero),
            () => "runtime-run",
            _ => null);
    }

    private static string NewRoot()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "gateway-runtime-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void AssertStopping(Task<string> result)
    {
        Assert.That(
            () => result.GetAwaiter().GetResult(),
            Throws.TypeOf<GatewayDispatchException>()
                .With.Property(nameof(GatewayDispatchException.Code)).EqualTo("gateway_stopping"));
    }

    private sealed class StubStateProvider : IGatewayStateProvider
    {
        public object CaptureStatus() => new { State = "Entry" };

        public object CaptureUiState() => new { };
    }

    private sealed class FakeTransport : IGatewayTransport
    {
        private readonly int port;
        private readonly Action onStart;
        private readonly Action onStop;

        public FakeTransport(int port, Action onStart, Action? onStop = null)
        {
            this.port = port;
            this.onStart = onStart;
            this.onStop = onStop ?? (() => { });
        }

        public int StartCount { get; private set; }

        public int StopCount { get; private set; }

        public int DisposeCount { get; private set; }

        public bool IsRunning { get; private set; }

        public int Port { get; private set; }

        public int Start(int preferredPort = 0)
        {
            StartCount++;
            onStart();
            Port = port;
            IsRunning = true;
            return Port;
        }

        public void Stop()
        {
            StopCount++;
            onStop();
            IsRunning = false;
        }

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    private sealed class TrackingDisposable : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
        }
    }
}
