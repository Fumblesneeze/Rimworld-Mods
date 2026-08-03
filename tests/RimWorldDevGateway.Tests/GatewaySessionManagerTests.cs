using System.IO;
using RimWorldDevGateway.Contracts;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewaySessionManagerTests
{
    [Test]
    public void Prepare_keeps_credentials_private_until_publish_then_stop_leaves_a_credential_free_tombstone()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "gateway-session-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var now = new DateTimeOffset(2026, 8, 1, 10, 30, 0, TimeSpan.Zero);
            var processStart = now.AddMinutes(-1);
            var manager = new GatewaySessionManager(
                root,
                () => Enumerable.Range(0, 32).Select(value => (byte)value).ToArray(),
                () => now,
                () => "run-fixed");

            var lease = manager.Prepare(
                processId: 1234,
                processStartUtc: processStart,
                gameVersion: "1.6.4871 rev590",
                modVersion: "0.1.0");

            var currentPath = Path.Combine(root, "DevGateway", "current.json");
            var runPath = Path.Combine(root, "DevGateway", "Sessions", "run-fixed", "session.json");
            Assert.Multiple(() =>
            {
                Assert.That(lease.Token, Has.Length.EqualTo(43));
                Assert.That(File.Exists(currentPath), Is.False);
                Assert.That(File.Exists(runPath), Is.False);
            });

            var active = lease.Publish(port: 46555);
            var published = GatewayContractJson.ReadFile<GatewaySessionManifest>(currentPath);

            Assert.Multiple(() =>
            {
                Assert.That(active.Token, Has.Length.EqualTo(43));
                Assert.That(active.BaseUrl, Is.EqualTo("http://127.0.0.1:46555/api/v1"));
                Assert.That(published.Token, Is.EqualTo(active.Token));
                Assert.That(published.ProcessId, Is.EqualTo(1234));
                Assert.That(File.Exists(runPath + ".tmp"), Is.False);
            });

            lease.Stop();

            var tombstone = GatewayContractJson.ReadFile<GatewaySessionManifest>(runPath);
            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(currentPath), Is.False);
                Assert.That(tombstone.State, Is.EqualTo("stopped"));
                Assert.That(tombstone.Token, Is.Null.Or.Empty);
                Assert.That(File.ReadAllText(runPath), Does.Not.Contain(active.Token));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Stop_classifies_a_locked_run_tombstone_and_retains_it_for_a_later_retry()
    {
        var root = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "gateway-transient-tombstone-lock-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        GatewaySessionLease? lease = null;
        FileStream? runLock = null;
        try
        {
            var now = new DateTimeOffset(2026, 8, 3, 8, 30, 0, TimeSpan.Zero);
            var manager = new GatewaySessionManager(
                root,
                () => Enumerable.Repeat((byte)7, 32).ToArray(),
                () => now,
                () => "run-transient-lock");
            lease = manager.Prepare(4321, now.AddMinutes(-1), "1.6", "1.0");
            var active = lease.Publish(40123);
            var currentPath = Path.Combine(root, "DevGateway", "current.json");
            var runPath = Path.Combine(
                root,
                "DevGateway",
                "Sessions",
                "run-transient-lock",
                "session.json");
            runLock = new FileStream(runPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var transient = Assert.Throws<GatewayTransientSessionCleanupException>(() => lease.Stop());
            var retained = GatewayContractJson.ReadFile<GatewaySessionManifest>(runPath);
            Assert.Multiple(() =>
            {
                Assert.That(transient!.InnerException, Is.TypeOf<IOException>());
                Assert.That(lease.IsStopped, Is.False);
                Assert.That(File.Exists(currentPath), Is.False);
                Assert.That(retained.State, Is.EqualTo("active"));
                Assert.That(retained.Token, Is.EqualTo(active.Token));
                Assert.That(Directory.GetFiles(Path.GetDirectoryName(runPath)!, "*.tmp"), Is.Empty);
            });

            runLock.Dispose();
            runLock = null;
            lease.Stop();

            var tombstone = GatewayContractJson.ReadFile<GatewaySessionManifest>(runPath);
            Assert.Multiple(() =>
            {
                Assert.That(lease.IsStopped, Is.True);
                Assert.That(tombstone.State, Is.EqualTo("stopped"));
                Assert.That(tombstone.Token, Is.Null.Or.Empty);
            });
        }
        finally
        {
            runLock?.Dispose();
            lease?.Stop();
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Transient_cleanup_classification_is_limited_to_windows_sharing_and_lock_violations()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                GatewaySessionManager.IsTransientSharingViolation(
                    new HResultIOException(unchecked((int)0x80070020))),
                Is.True);
            Assert.That(
                GatewaySessionManager.IsTransientSharingViolation(
                    new HResultIOException(unchecked((int)0x80070021))),
                Is.True);
            Assert.That(
                GatewaySessionManager.IsTransientSharingViolation(
                    new HResultIOException(unchecked((int)0x80070070))),
                Is.False);
        });
    }

    [Test]
    public void Prepare_scrubs_a_stale_current_credential_before_creating_the_next_lease()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "gateway-stale-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var oldStart = new DateTimeOffset(2026, 8, 1, 8, 0, 0, TimeSpan.Zero);
            var currentPath = Path.Combine(root, "DevGateway", "current.json");
            var crashedManifest = new GatewaySessionManifest
            {
                ApiVersion = "1",
                RunId = "run-crashed",
                State = "active",
                BaseUrl = "http://127.0.0.1:40101/api/v1",
                Token = "stale-secret",
                ProcessId = 555,
                ProcessStartUtc = oldStart.ToString("O"),
                StartedUtc = oldStart.AddMinutes(1).ToString("O"),
                GameVersion = "1.6",
                ModVersion = "0.1",
                UnrestrictedExecution = true
            };
            Directory.CreateDirectory(Path.GetDirectoryName(currentPath)!);
            File.WriteAllText(currentPath, GatewayContractJson.Write(crashedManifest));

            var replacement = new GatewaySessionManager(
                root,
                () => Enumerable.Repeat((byte)8, 32).ToArray(),
                () => oldStart.AddHours(1),
                () => "run-replacement",
                _ => null);
            var lease = replacement.Prepare(777, oldStart.AddHours(1), "1.6", "0.1");

            var stalePath = Path.Combine(root, "DevGateway", "Sessions", "run-crashed", "session.json");
            var stale = GatewayContractJson.ReadFile<GatewaySessionManifest>(stalePath);
            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(currentPath), Is.False);
                Assert.That(stale.State, Is.EqualTo("stale"));
                Assert.That(stale.Token, Is.Null.Or.Empty);
                Assert.That(File.ReadAllText(stalePath), Does.Not.Contain("stale-secret"));
                Assert.That(lease.Token, Has.Length.EqualTo(43));
            });

            lease.Stop();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void A_second_manager_cannot_prepare_the_same_save_data_root_until_the_owner_stops()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "gateway-claim-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        GatewaySessionLease? firstLease = null;
        GatewaySessionLease? secondLease = null;
        try
        {
            var processStart = DateTimeOffset.UtcNow.AddMinutes(-1);
            var first = new GatewaySessionManager(root, runIdFactory: () => "run-first");
            var second = new GatewaySessionManager(root, runIdFactory: () => "run-second");
            firstLease = first.Prepare(101, processStart, "1.6", "0.1");

            var conflict = Assert.Throws<InvalidOperationException>(() =>
                second.Prepare(202, processStart, "1.6", "0.1"));

            Assert.That(conflict!.Message, Does.Contain("claimed"));
            firstLease.Stop();
            firstLease = null;

            secondLease = second.Prepare(202, processStart, "1.6", "0.1");
            Assert.That(secondLease.RunId, Is.EqualTo("run-second"));
        }
        finally
        {
            secondLease?.Stop();
            firstLease?.Stop();
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Stop_does_not_delete_a_current_locator_owned_by_another_run()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "gateway-foreign-current-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var manager = new GatewaySessionManager(root, runIdFactory: () => "run-owner");
            var lease = manager.Prepare(303, now.AddMinutes(-1), "1.6", "0.1");
            lease.Publish(40103);
            var currentPath = Path.Combine(root, "DevGateway", "current.json");
            var foreign = new GatewaySessionManifest
            {
                ApiVersion = "1",
                RunId = "run-foreign",
                State = "active",
                BaseUrl = "http://127.0.0.1:40104/api/v1",
                Token = "foreign-token",
                ProcessId = 404,
                ProcessStartUtc = now.ToString("O"),
                StartedUtc = now.ToString("O"),
                GameVersion = "1.6",
                ModVersion = "0.1",
                UnrestrictedExecution = true
            };
            File.WriteAllText(currentPath, GatewayContractJson.Write(foreign));

            lease.Stop();

            var retained = GatewayContractJson.ReadFile<GatewaySessionManifest>(currentPath);
            Assert.Multiple(() =>
            {
                Assert.That(retained.RunId, Is.EqualTo("run-foreign"));
                Assert.That(retained.Token, Is.EqualTo("foreign-token"));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Failed_current_locator_cleanup_retains_the_lease_and_can_be_retried_before_restarting()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "gateway-cleanup-retry-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        GatewaySessionLease? firstLease = null;
        GatewaySessionLease? replacementLease = null;
        FileStream? locatorLock = null;
        try
        {
            var runIds = new Queue<string>(new[] { "run-cleanup-retry", "run-after-cleanup" });
            var now = new DateTimeOffset(2026, 8, 1, 16, 0, 0, TimeSpan.Zero);
            var manager = new GatewaySessionManager(
                root,
                () => Enumerable.Repeat((byte)9, 32).ToArray(),
                () => now,
                () => runIds.Dequeue(),
                _ => null);
            firstLease = manager.Prepare(505, now.AddMinutes(-1), "1.6", "0.1");
            firstLease.Publish(40105);
            var currentPath = Path.Combine(root, "DevGateway", "current.json");
            locatorLock = new FileStream(currentPath, FileMode.Open, FileAccess.Read, FileShare.None);

            Assert.That(
                () => firstLease.Stop(),
                Throws.TypeOf<GatewayTransientSessionCleanupException>()
                    .With.Message.Contains("transient file lock"));
            Assert.Multiple(() =>
            {
                Assert.That(firstLease.IsStopped, Is.False);
                Assert.That(File.Exists(currentPath), Is.True);
                Assert.That(
                    () => manager.Prepare(506, now, "1.6", "0.1"),
                    Throws.TypeOf<InvalidOperationException>()
                        .With.Message.Contains("already prepared or active"));
            });

            locatorLock.Dispose();
            locatorLock = null;
            firstLease.Stop();
            Assert.That(File.Exists(currentPath), Is.False);

            replacementLease = manager.Prepare(506, now, "1.6", "0.1");
            Assert.Multiple(() =>
            {
                Assert.That(firstLease.IsStopped, Is.True);
                Assert.That(replacementLease.RunId, Is.EqualTo("run-after-cleanup"));
            });
        }
        finally
        {
            locatorLock?.Dispose();
            replacementLease?.Stop();
            firstLease?.Stop();
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class HResultIOException : IOException
    {
        internal HResultIOException(int hresult)
        {
            HResult = hresult;
        }
    }
}
