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

    [Test]
    public void End_to_end_artifact_commits_when_only_a_destination_derived_guid_temporary_path_would_exceed_legacy_windows_limit()
    {
        var baseRoot = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "e2e-deep-artifact",
            Guid.NewGuid().ToString("N"));
        var runId = "deep-run";
        var root = MakeLegacyPathBoundaryRoot(baseRoot, runId);
        try
        {
            var artifactPath = Path.Combine(
                root,
                "DevGateway",
                "Sessions",
                runId,
                "end-to-end-tests.json");
            Assert.Multiple(() =>
            {
                Assert.That(artifactPath.Length, Is.LessThan(260));
                Assert.That((artifactPath + "." + new string('0', 32) + ".tmp").Length,
                    Is.GreaterThanOrEqualTo(260));
            });

            var snapshot = new GatewayEndToEndSnapshot(enabled: true, discoveryState: "discovering");
            var store = new GatewayEndToEndSessionArtifactStore(root);
            var operation = store.BeginAttachSession(runId, snapshot);
            Assert.That(
                SpinWait.SpinUntil(() => operation.IsCompleted, TimeSpan.FromSeconds(10)),
                Is.True);
            var outcome = operation.GetOutcome();

            Assert.Multiple(() =>
            {
                Assert.That(outcome.Succeeded, Is.True, outcome.Failure?.ToString());
                Assert.That(store.IsAttached, Is.True);
                Assert.That(File.Exists(artifactPath), Is.True);
                Assert.That(Directory.GetFiles(Path.GetDirectoryName(artifactPath)!, "*.tmp"), Is.Empty);
                Assert.That(
                    Directory.GetFiles(Path.GetDirectoryName(artifactPath)!),
                    Is.EqualTo(new[] { artifactPath }));
            });
        }
        finally
        {
            if (Directory.Exists(baseRoot))
            {
                Directory.Delete(baseRoot, recursive: true);
            }
        }
    }

    [Test]
    public void Atomic_temporary_sibling_retries_a_collision_without_deleting_the_unowned_file()
    {
        var root = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "e2e-temp-collision",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var destination = Path.Combine(root, "end-to-end-tests.json");
            var collision = Path.Combine(root, ".collision.tmp");
            var owned = Path.Combine(root, ".owned.tmp");
            File.WriteAllText(collision, "unowned");
            var leaves = new Queue<string>(new[] { ".collision.tmp", ".owned.tmp" });

            using (var stream = GatewayTemporaryFile.CreateSibling(
                       destination,
                       leafFactory: () => leaves.Dequeue()))
            {
                Assert.That(stream.Name, Is.EqualTo(owned));
            }

            Assert.Multiple(() =>
            {
                Assert.That(File.ReadAllText(collision), Is.EqualTo("unowned"));
                Assert.That(File.Exists(owned), Is.True);
            });
            File.Delete(owned);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Test]
    public void Atomic_temporary_sibling_recovers_when_its_session_directory_is_not_yet_present()
    {
        var root = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "e2e-temp-missing-parent",
            Guid.NewGuid().ToString("N"));
        var destination = Path.Combine(root, "DevGateway", "Sessions", "run", "session.json");
        try
        {
            using (var stream = GatewayTemporaryFile.CreateSibling(
                       destination,
                       leafFactory: () => ".owned.tmp"))
            {
                Assert.That(Path.GetDirectoryName(stream.Name),
                    Is.EqualTo(Path.GetDirectoryName(destination)).IgnoreCase);
            }

            Assert.That(File.Exists(Path.Combine(Path.GetDirectoryName(destination)!, ".owned.tmp")), Is.True);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static string MakeLegacyPathBoundaryRoot(string baseRoot, string runId)
    {
        var fixedSuffixLength = Path.Combine(
            "DevGateway",
            "Sessions",
            runId,
            "end-to-end-tests.json").Length + 1;
        var desiredArtifactLength = 245;
        var remaining = desiredArtifactLength - baseRoot.Length - fixedSuffixLength;
        if (remaining < 4)
        {
            throw new AssertionException("The test work directory is too deep for the legacy path-boundary fixture.");
        }

        var segments = new List<string>();
        while (remaining > 0)
        {
            var length = Math.Min(remaining - 1, 40);
            if (length <= 0)
            {
                throw new AssertionException("The requested legacy path length cannot be represented safely.");
            }
            segments.Add(new string('d', length));
            remaining -= length + 1;
        }

        return segments.Aggregate(baseRoot, Path.Combine);
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
