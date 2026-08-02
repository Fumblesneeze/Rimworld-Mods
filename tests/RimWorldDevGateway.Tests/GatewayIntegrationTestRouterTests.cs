using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using RimWorldDevGateway.IntegrationTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayIntegrationTestRouterTests
{
    [Test]
    public void Integration_test_status_is_retryable_until_the_initial_artifact_commit_is_durable()
    {
        var router = new GatewayApiRouter(
            new GatewayDispatcher(),
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(integrationTestSnapshot: () => null));

        var response = router.Handle(
            new GatewayHttpRequest(
                "GET",
                "/api/v1/integration-tests",
                "/api/v1/integration-tests",
                string.Empty,
                new Dictionary<string, string>(),
                Array.Empty<byte>()),
            "integration-status-pending");
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(503));
            Assert.That(body, Does.Contain("integration_test_status_pending"));
            Assert.That(body, Does.Contain("\"retryable\":true"));
        });
    }

    [Test]
    public void Integration_test_status_is_available_without_dispatch_or_test_discovery()
    {
        var catalog = new ThrowingCatalog();
        var runner = new GatewayIntegrationTestRunner(
            enabled: false,
            catalog,
            new NoOpArtifactStore(),
            new NeverReady());
        var dispatcher = new GatewayDispatcher();
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(integrationTestSnapshot: () => runner.Snapshot));

        var response = router.Handle(
            new GatewayHttpRequest(
                "GET",
                "/api/v1/integration-tests",
                "/api/v1/integration-tests",
                string.Empty,
                new Dictionary<string, string>(),
                Array.Empty<byte>()),
            "integration-status");
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200), body);
            Assert.That(body, Does.Contain("\"requestId\":\"integration-status\""));
            Assert.That(body, Does.Contain("\"Enabled\":false"));
            Assert.That(body, Does.Contain("\"DiscoveryState\":\"disabled\""));
            Assert.That(body, Does.Contain("\"MainMenuLoaded\""));
            Assert.That(dispatcher.PendingCount, Is.Zero);
            Assert.That(catalog.DiscoveryCount, Is.Zero);
        });
    }

    [Test]
    public void Worst_case_bounded_snapshot_fits_the_same_four_megabyte_endpoint_and_artifact_budget()
    {
        var package = new string('\u0001', 128);
        var identity = new string('\u0002', 256);
        var message = new string('\u0003', 256);
        var stack = new string('\u0004', 1024);
        var now = DateTimeOffset.UtcNow;
        var snapshot = new GatewayIntegrationTestSnapshot(
            enabled: true,
            discoveryState: "completed",
            discoveredAssemblyCount: 64,
            discoveredTestCount: 128,
            discoveredAssemblies: Enumerable.Range(0, 64).Select(_ =>
                new GatewayIntegrationTestAssemblySnapshot(package, identity, identity)),
            discoveredTests: Enumerable.Range(0, 128).Select(_ =>
                new GatewayIntegrationTestDescriptor(package, identity, identity, RunAt.MainMenuLoaded)),
            failures: Enumerable.Range(0, 64).Select(_ =>
                new GatewayIntegrationTestFailure(
                    "bounded_failure",
                    message,
                    package,
                    identity,
                    identity,
                    identity,
                    stack)),
            results: Enumerable.Range(0, 128).Select(_ =>
                new GatewayIntegrationTestResult(
                    package,
                    identity,
                    identity,
                    RunAt.MainMenuLoaded,
                    "failed",
                    now,
                    now,
                    1,
                    identity,
                    message,
                    stack)),
            lifecyclePoints: new[]
            {
                new GatewayIntegrationTestLifecycleSnapshot(
                    RunAt.MainMenuLoaded, "failed", now, now, 128, 0, 128),
                new GatewayIntegrationTestLifecycleSnapshot(
                    RunAt.PlayableMapLoaded, "pending", null, null, 0, 0, 0)
            });
        var router = new GatewayApiRouter(
            new GatewayDispatcher(),
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(integrationTestSnapshot: () => snapshot));

        var response = router.Handle(
            new GatewayHttpRequest(
                "GET",
                "/api/v1/integration-tests",
                "/api/v1/integration-tests",
                string.Empty,
                new Dictionary<string, string>(),
                Array.Empty<byte>()),
            "worst-case-snapshot");

        var saveDataFolder = Path.Combine(
            Path.GetTempPath(),
            "RimWorldDevGateway.BudgetTests",
            Guid.NewGuid().ToString("N"));
        try
        {
            var store = new GatewayIntegrationTestSessionArtifactStore(saveDataFolder);
            var operation = store.BeginAttachSession("run-budget", snapshot);
            Assert.That(SpinWait.SpinUntil(() => operation.IsCompleted, TimeSpan.FromSeconds(10)), Is.True);
            Assert.That(operation.GetOutcome().Succeeded, Is.True);
            var artifactLength = new FileInfo(store.ArtifactPath!).Length;

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(200));
                Assert.That(response.Body.Length, Is.LessThanOrEqualTo(GatewayIntegrationTestSnapshot.MaximumSerializedUtf8Bytes));
                Assert.That(artifactLength, Is.LessThanOrEqualTo(GatewayIntegrationTestSnapshot.MaximumSerializedUtf8Bytes));
            });
        }
        finally
        {
            if (Directory.Exists(saveDataFolder))
            {
                Directory.Delete(saveDataFolder, recursive: true);
            }
        }
    }

    private sealed class ThrowingCatalog : IGatewayIntegrationTestAssemblyCatalog
    {
        public int DiscoveryCount { get; private set; }

        public IGatewayIntegrationTestAssemblyDiscoveryCursor BeginDiscovery()
        {
            DiscoveryCount++;
            throw new AssertionException("Disabled status reads must not scan active mods.");
        }

        public Assembly Load(GatewayIntegrationTestAssemblySource source) =>
            throw new AssertionException("Disabled status reads must not load assemblies.");
    }

    private sealed class NoOpArtifactStore : IGatewayIntegrationTestArtifactStore
    {
        public bool IsAttached => true;

        public GatewayIntegrationTestSnapshot? CommittedSnapshot { get; private set; }

        public IGatewayIntegrationTestPersistenceOperation BeginPersist(
            GatewayIntegrationTestSnapshot snapshot)
        {
            CommittedSnapshot = snapshot;
            return new GatewayIntegrationTestCompletedPersistenceOperation(
                GatewayIntegrationTestPersistenceOutcome.Success(snapshot));
        }
    }

    private sealed class NeverReady : IGatewayIntegrationTestReadiness
    {
        public bool IsMainThread => true;

        public bool IsReady(RunAt lifecyclePoint) => false;
    }

    private sealed class StubStateProvider : IGatewayStateProvider
    {
        public object CaptureStatus() => new { };

        public object CaptureUiState() => new { };
    }
}
