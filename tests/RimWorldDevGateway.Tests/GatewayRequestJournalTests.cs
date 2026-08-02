using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayRequestJournalTests
{
    [Test]
    public void Older_completion_does_not_obscure_the_newest_admitted_request()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "RimWorldDevGateway.Tests",
            Guid.NewGuid().ToString("N"));
        var olderEntered = new ManualResetEventSlim();
        var newerEntered = new ManualResetEventSlim();
        var releaseOlder = new ManualResetEventSlim();
        var releaseNewer = new ManualResetEventSlim();
        Task<GatewayHttpResponse>? olderTask = null;
        Task<GatewayHttpResponse>? newerTask = null;

        try
        {
            var journal = new GatewayRequestJournal(root);
            olderTask = Task.Run(() => journal.Track(
                Request("/api/v1/older"),
                "older-request",
                (_, _) =>
                {
                    olderEntered.Set();
                    releaseOlder.Wait();
                    return GatewayHttpResponse.Empty(204, "No Content");
                }));
            Assert.That(olderEntered.Wait(3000), Is.True, "older request did not enter its handler");

            newerTask = Task.Run(() => journal.Track(
                Request("/api/v1/newer"),
                "newer-request",
                (_, _) =>
                {
                    newerEntered.Set();
                    releaseNewer.Wait();
                    return GatewayHttpResponse.Empty(204, "No Content");
                }));
            Assert.That(newerEntered.Wait(3000), Is.True, "newer request did not enter its handler");

            releaseOlder.Set();
            Assert.That(olderTask.Wait(3000), Is.True, "older request did not complete");

            var lastWhileNewerIsRunning = File.ReadAllText(Path.Combine(root, "last-request.json"));
            Assert.Multiple(() =>
            {
                Assert.That(lastWhileNewerIsRunning, Does.Contain("\"RequestId\":\"newer-request\""));
                Assert.That(lastWhileNewerIsRunning, Does.Contain("\"State\":\"started\""));
            });

            releaseNewer.Set();
            Assert.That(newerTask.Wait(3000), Is.True, "newer request did not complete");
            var lastAfterNewerCompletes = File.ReadAllText(Path.Combine(root, "last-request.json"));
            Assert.Multiple(() =>
            {
                Assert.That(lastAfterNewerCompletes, Does.Contain("\"RequestId\":\"newer-request\""));
                Assert.That(lastAfterNewerCompletes, Does.Contain("\"State\":\"completed\""));
            });
        }
        finally
        {
            releaseOlder.Set();
            releaseNewer.Set();
            olderTask?.Wait(3000);
            newerTask?.Wait(3000);
            olderEntered.Dispose();
            newerEntered.Dispose();
            releaseOlder.Dispose();
            releaseNewer.Dispose();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Test]
    public void Older_in_flight_request_reappears_after_a_newer_request_completes()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "RimWorldDevGateway.Tests",
            Guid.NewGuid().ToString("N"));
        var olderEntered = new ManualResetEventSlim();
        var releaseOlder = new ManualResetEventSlim();
        Task<GatewayHttpResponse>? olderTask = null;

        try
        {
            var journal = new GatewayRequestJournal(root);
            olderTask = Task.Run(() => journal.Track(
                Request("/api/v1/older-hung"),
                "older-in-flight",
                (_, _) =>
                {
                    olderEntered.Set();
                    releaseOlder.Wait();
                    return GatewayHttpResponse.Empty(204, "No Content");
                }));
            Assert.That(olderEntered.Wait(3000), Is.True, "older request did not enter its handler");

            journal.Track(
                Request("/api/v1/newer-fast"),
                "newer-completed",
                (_, _) => GatewayHttpResponse.Empty(204, "No Content"));

            var lastWhileOlderIsRunning = File.ReadAllText(Path.Combine(root, "last-request.json"));
            Assert.Multiple(() =>
            {
                Assert.That(lastWhileOlderIsRunning, Does.Contain("\"RequestId\":\"older-in-flight\""));
                Assert.That(lastWhileOlderIsRunning, Does.Contain("\"State\":\"started\""));
            });

            releaseOlder.Set();
            Assert.That(olderTask.Wait(3000), Is.True, "older request did not complete");
            var lastAfterAllComplete = File.ReadAllText(Path.Combine(root, "last-request.json"));
            Assert.Multiple(() =>
            {
                Assert.That(lastAfterAllComplete, Does.Contain("\"RequestId\":\"older-in-flight\""));
                Assert.That(lastAfterAllComplete, Does.Contain("\"State\":\"completed\""));
            });
        }
        finally
        {
            releaseOlder.Set();
            olderTask?.Wait(3000);
            olderEntered.Dispose();
            releaseOlder.Dispose();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static GatewayHttpRequest Request(string path) =>
        new(
            "GET",
            path,
            path,
            string.Empty,
            new Dictionary<string, string>(),
            Array.Empty<byte>(),
            CancellationToken.None);
}
