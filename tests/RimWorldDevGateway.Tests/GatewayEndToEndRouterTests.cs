using System.Text;
using System.IO;
using System.Threading;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndRouterTests
{
    [Test]
    public void End_to_end_status_is_retryable_until_initial_artifact_is_durable()
    {
        var router = CreateRouter(() => null);

        var response = router.Handle(Request(), "e2e-pending");
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(503));
            Assert.That(body, Does.Contain("end_to_end_test_status_pending"));
            Assert.That(body, Does.Contain("\"retryable\":true"));
        });
    }

    [Test]
    public void Disabled_end_to_end_status_is_available_without_main_thread_dispatch()
    {
        var dispatcher = new GatewayDispatcher();
        using var coordinator = GatewayEndToEndCoordinator.Create(false, () =>
            throw new AssertionException("Disabled endpoint must not create discovery dependencies."));
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(endToEndTestSnapshot: () => coordinator.PublishedSnapshot));

        var response = router.Handle(Request(), "e2e-disabled");
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200), body);
            Assert.That(body, Does.Contain("\"Enabled\":false"));
            Assert.That(body, Does.Contain("\"DiscoveryState\":\"disabled\""));
            Assert.That(dispatcher.PendingCount, Is.Zero);
        });
    }

    [Test]
    public void End_to_end_status_is_not_subject_to_the_legacy_four_megabyte_response_ceiling()
    {
        var largeFixedDiagnostic = new string('x', 5 * 1024 * 1024);
        var snapshot = new GatewayEndToEndSnapshot(
            enabled: true,
            discoveryState: "completed_with_failures",
            failures: new[]
            {
                new GatewayEndToEndFailureSnapshot("fixture", largeFixedDiagnostic, "alpha.mod", "fixture.json")
            });
        var router = CreateRouter(() => snapshot);

        var response = router.Handle(Request(), "e2e-large-status");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(response.Body.Length, Is.GreaterThan(4 * 1024 * 1024));
        });
    }

    [Test]
    public void End_to_end_artifact_is_not_subject_to_the_legacy_four_megabyte_policy_ceiling()
    {
        var root = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "e2e-artifact",
            Guid.NewGuid().ToString("N"));
        try
        {
            var snapshot = new GatewayEndToEndSnapshot(
                true,
                "completed_with_failures",
                failures: new[]
                {
                    new GatewayEndToEndFailureSnapshot(
                        "fixture",
                        new string('x', 5 * 1024 * 1024))
                });
            var store = new GatewayEndToEndSessionArtifactStore(root);

            var operation = store.BeginAttachSession("large-artifact", snapshot);
            Assert.That(
                SpinWait.SpinUntil(() => operation.IsCompleted, TimeSpan.FromSeconds(10)),
                Is.True);
            var outcome = operation.GetOutcome();

            Assert.Multiple(() =>
            {
                Assert.That(outcome.Succeeded, Is.True, outcome.Failure?.ToString());
                Assert.That(outcome.Snapshot, Is.SameAs(snapshot));
                Assert.That(store.CommittedSnapshot, Is.SameAs(snapshot));
                Assert.That(new FileInfo(store.ArtifactPath!).Length, Is.GreaterThan(4 * 1024 * 1024));
            });
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static GatewayApiRouter CreateRouter(Func<GatewayEndToEndSnapshot?> snapshot) => new(
        new GatewayDispatcher(),
        new StubStateProvider(),
        new GatewayLogBuffer(),
        new GatewayApiServices(endToEndTestSnapshot: snapshot));

    private static GatewayHttpRequest Request() => new(
        "GET",
        "/api/v1/end-to-end-tests",
        "/api/v1/end-to-end-tests",
        string.Empty,
        new Dictionary<string, string>(),
        Array.Empty<byte>());

    private sealed class StubStateProvider : IGatewayStateProvider
    {
        public object CaptureStatus() => new { };

        public object CaptureUiState() => new { };
    }
}
