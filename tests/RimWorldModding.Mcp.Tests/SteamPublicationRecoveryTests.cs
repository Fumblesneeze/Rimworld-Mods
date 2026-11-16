using System.Text.Json;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

public sealed class SteamPublicationRecoveryTests
{
    [Test]
    public void RepeatedArchivalPreservesBothFailureMessages()
    {
        var root = Path.Combine(Path.GetTempPath(), "release-archive-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "worker-error.txt");
            File.WriteAllText(path, "First failure");
            ReleaseWorkerCoordinator.ArchiveError(path, 9);
            ReleaseWorkerCoordinator.ArchiveError(path, 9);
            File.WriteAllText(path, "Different failure");
            ReleaseWorkerCoordinator.ArchiveError(path, 9);
            Assert.That(Directory.GetFiles(root, "attempt-9-*.txt").Select(File.ReadAllText),
                Is.EquivalentTo(new[] { "First failure", "Different failure" }));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Test]
    public async Task ReadinessWaitsForSignInButRejectsDifferentOwner()
    {
        var now = DateTimeOffset.UtcNow;
        var reads = 0;
        var status = await GatewayWorkshopClient.PollUntilSteamReadyAsync("123", now.AddSeconds(5),
            _ => Task.FromResult(JsonSerializer.SerializeToElement(new { CurrentUserSteamId = ++reads < 3 ? 0UL : 123UL })),
            (duration, _) => { now += duration; return Task.CompletedTask; }, () => now, CancellationToken.None);
        Assert.That(GatewayWorkshopClient.UInt64(status, "CurrentUserSteamId"), Is.EqualTo(123));
        Assert.That(reads, Is.EqualTo(3));
        Assert.ThrowsAsync<InvalidOperationException>(() => GatewayWorkshopClient.PollUntilSteamReadyAsync("123", now.AddSeconds(5),
            _ => Task.FromResult(JsonSerializer.SerializeToElement(new { CurrentUserSteamId = 456UL })),
            (_, _) => throw new AssertionException("Wrong owner must not be retried."), () => now, CancellationToken.None));
    }

    [Test]
    public void ReadinessTimeoutIsBounded()
    {
        var now = DateTimeOffset.UtcNow;
        var start = now;
        Assert.ThrowsAsync<TimeoutException>(() => GatewayWorkshopClient.PollUntilSteamReadyAsync("123", start.AddSeconds(2),
            _ => Task.FromResult(JsonSerializer.SerializeToElement(new { CurrentUserSteamId = 0UL })),
            (duration, _) => { now += duration; return Task.CompletedTask; }, () => now, CancellationToken.None));
        Assert.That(now - start, Is.EqualTo(TimeSpan.FromSeconds(2)));
    }
}
