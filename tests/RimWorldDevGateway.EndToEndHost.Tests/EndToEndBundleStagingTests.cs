using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RimWorldDevGateway.Contracts;
using RimWorldDevGateway.EndToEndHost;

namespace RimWorldDevGateway.EndToEndHost.Tests;

[TestFixture]
public sealed class EndToEndBundleStagingTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string Configuration =
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    [Test]
    public void Planner_emits_a_deterministic_exact_manifest_and_owner_stage()
    {
        var candidate = ValidCandidate();

        var first = EndToEndBundlePlanner.Create(
            new[] { candidate },
            ResolvablePackages,
            Path.Combine(RepositoryRoot, "artifacts", "E2EPlannerTests"),
            "1.6");
        var second = EndToEndBundlePlanner.Create(
            new[] { candidate },
            ResolvablePackages,
            Path.Combine(RepositoryRoot, "artifacts", "E2EPlannerTests"),
            "1.6");
        var stage = first.OwnerStages.Single();
        var bundle = stage.Bundles.Single();
        var manifest = GatewayContractJson.Read<EndToEndBundleManifest>(bundle.ManifestJson);

        Assert.Multiple(() =>
        {
            Assert.That(stage.OwnerPackageId, Is.EqualTo("alpha.mod"));
            Assert.That(stage.DestinationDirectory, Does.EndWith(
                Path.Combine("alpha.mod", "1.6", "DevEndToEndTests")));
            Assert.That(bundle.ManifestJson, Is.EqualTo(second.OwnerStages.Single().Bundles.Single().ManifestJson));
            Assert.That(manifest.Schema, Is.EqualTo(EndToEndBundleManifest.SchemaValue));
            Assert.That(manifest.OwnerPackageId, Is.EqualTo("alpha.mod"));
            Assert.That(manifest.Assembly, Is.EqualTo("EndToEndHost.ValidFixtures.dll"));
            Assert.That(manifest.AssemblyIdentity, Is.EqualTo(candidate.Metadata.AssemblyIdentity));
            Assert.That(manifest.ModuleVersionId, Is.EqualTo(candidate.Metadata.ModuleVersionId.ToString("D")));
            Assert.That(manifest.AssemblyLength, Is.EqualTo(candidate.Metadata.Length));
            Assert.That(manifest.AssemblySha256, Is.EqualTo(candidate.Metadata.Sha256));
            Assert.That(manifest.Dependencies.Select(reference => reference.Identity),
                Does.Contain(candidate.Metadata.AssemblyReferences.Single(
                    reference => reference.Name == "RimWorldDevGateway.EndToEndTesting").Identity));
            Assert.That(manifest.Tests.Select(test => test.Id), Is.EqualTo(new[]
            {
                "alpha.base-a",
                "alpha.base-b",
                "alpha.optional"
            }));
        });
    }

    [Test]
    public void Publisher_writes_an_owned_atomic_stage_and_matching_cleanup_removes_it()
    {
        using var sandbox = new StageSandbox();
        var stage = Plan(sandbox.Root);
        var publisher = new EndToEndStagePublisher();

        var lease = publisher.Publish(stage);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(stage.DestinationDirectory, "EndToEndHost.ValidFixtures.dll")), Is.True);
            Assert.That(File.Exists(Path.Combine(stage.DestinationDirectory, "EndToEndHost.ValidFixtures.e2etests.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(stage.DestinationDirectory, EndToEndStageMarker.FileName)), Is.True);
            Assert.That(Directory.GetDirectories(Path.GetDirectoryName(stage.DestinationDirectory)!, ".DevEndToEndTests.*"), Is.Empty);
        });

        Assert.That(publisher.TryCleanup(lease), Is.True);
        Assert.That(Directory.Exists(stage.DestinationDirectory), Is.False);
    }

    [Test]
    public void Publisher_retries_a_short_lived_file_lock_during_atomic_commit()
    {
        using var sandbox = new StageSandbox();
        var stage = Plan(sandbox.Root);
        using var transientLock = new ReleaseStageFileLock(
            Path.GetDirectoryName(stage.DestinationDirectory)!,
            LockMoveSource.TemporaryStage);
        var publisher = new EndToEndStagePublisher(transientLock);

        var lease = publisher.Publish(stage);

        Assert.Multiple(() =>
        {
            Assert.That(transientLock.Locked, Is.True,
                "The test must exercise a real open-handle commit interruption.");
            Assert.That(transientLock.RetryCount, Is.GreaterThanOrEqualTo(1),
                "The lock may be released only after Directory.Move actually enters its retry path.");
            Assert.That(Directory.Exists(stage.DestinationDirectory), Is.True);
            Assert.That(Directory.GetDirectories(
                Path.GetDirectoryName(stage.DestinationDirectory)!,
                ".DevEndToEndTests.*"), Is.Empty);
        });
        Assert.That(publisher.TryCleanup(lease), Is.True);
    }

    [Test]
    public void Publisher_retries_short_lived_locks_during_existing_stage_backup()
    {
        using var sandbox = new StageSandbox();
        var stage = Plan(sandbox.Root);
        new EndToEndStagePublisher().Publish(stage);
        using var transientLock = new ReleaseStageFileLock(
            Path.GetDirectoryName(stage.DestinationDirectory)!,
            LockMoveSource.ExistingDestination);
        var publisher = new EndToEndStagePublisher(transientLock);

        var lease = publisher.Publish(stage);

        Assert.Multiple(() =>
        {
            Assert.That(transientLock.Locked, Is.True);
            Assert.That(transientLock.RetryCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(Directory.Exists(stage.DestinationDirectory), Is.True);
            Assert.That(Directory.GetDirectories(
                Path.GetDirectoryName(stage.DestinationDirectory)!,
                ".DevEndToEndTests.*"), Is.Empty);
        });
        Assert.That(publisher.TryCleanup(lease), Is.True);
    }

    [Test]
    public void Publisher_retries_short_lived_locks_while_restoring_a_backup()
    {
        using var sandbox = new StageSandbox();
        var stage = Plan(sandbox.Root);
        new EndToEndStagePublisher().Publish(stage);
        var sentinel = Path.Combine(stage.DestinationDirectory, "previous-stage.txt");
        File.WriteAllText(sentinel, "previous");
        using var transientLock = new LockBackupThenFail(
            Path.GetDirectoryName(stage.DestinationDirectory)!);
        var publisher = new EndToEndStagePublisher(transientLock);

        var error = Assert.Throws<EndToEndStageException>(() => publisher.Publish(stage));

        Assert.Multiple(() =>
        {
            Assert.That(error!.Message, Does.Contain("Injected rollback trigger."));
            Assert.That(transientLock.RetryCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(File.ReadAllText(sentinel), Is.EqualTo("previous"));
            Assert.That(Directory.GetDirectories(
                Path.GetDirectoryName(stage.DestinationDirectory)!,
                ".DevEndToEndTests.*"), Is.Empty);
        });
    }

    [Test]
    public void Publishers_serialize_the_complete_transaction_for_one_destination()
    {
        using var sandbox = new StageSandbox();
        var stage = Plan(sandbox.Root);
        using var blocker = new BlockingFaultPoint("after-stage-write");
        var firstPublisher = new EndToEndStagePublisher(blocker);
        var secondPublisher = new EndToEndStagePublisher();

        var first = Task.Run(() => firstPublisher.Publish(stage));
        Assert.That(blocker.WaitUntilEntered(TimeSpan.FromSeconds(5)), Is.True,
            "The first publisher must hold the destination transaction before the concurrency probe.");
        var second = Task.Run(() => secondPublisher.Publish(stage));

        Assert.That(second.Wait(TimeSpan.FromMilliseconds(200)), Is.False,
            "A second publisher must not enter the same canonical destination transaction.");
        blocker.Release();
        var firstLease = first.GetAwaiter().GetResult();
        var secondLease = second.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(firstPublisher.TryCleanup(firstLease), Is.False,
                "The superseded first lease must not delete the serialized second publication.");
            Assert.That(secondPublisher.TryCleanup(secondLease), Is.True);
        });
    }

    [Test]
    public void Rollback_reports_failure_if_the_destination_becomes_occupied()
    {
        using var sandbox = new StageSandbox();
        var stage = Plan(sandbox.Root);
        new EndToEndStagePublisher().Publish(stage);
        var publisher = new EndToEndStagePublisher(
            new OccupyDestinationAfterBackup(stage.DestinationDirectory));

        var error = Assert.Throws<EndToEndStageException>(() => publisher.Publish(stage));

        Assert.Multiple(() =>
        {
            Assert.That(error!.Message, Does.Contain("rollback was incomplete"));
            Assert.That(error.InnerException!.ToString(), Does.Contain("destination became occupied"));
            Assert.That(File.Exists(Path.Combine(stage.DestinationDirectory, "foreign.txt")), Is.True);
            Assert.That(Directory.GetDirectories(
                Path.GetDirectoryName(stage.DestinationDirectory)!,
                ".DevEndToEndTests.backup.*"), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void Publisher_refuses_to_replace_an_unmarked_foreign_directory()
    {
        using var sandbox = new StageSandbox();
        var stage = Plan(sandbox.Root);
        Directory.CreateDirectory(stage.DestinationDirectory);
        var foreign = Path.Combine(stage.DestinationDirectory, "foreign.txt");
        File.WriteAllText(foreign, "keep");

        var error = Assert.Throws<EndToEndStageException>(() => new EndToEndStagePublisher().Publish(stage));

        Assert.Multiple(() =>
        {
            Assert.That(error!.Message, Does.Contain("marker").And.Contain("refuse"));
            Assert.That(File.ReadAllText(foreign), Is.EqualTo("keep"));
        });
    }

    [TestCase("after-backup")]
    [TestCase("after-commit")]
    public void Publisher_restores_the_previous_owned_stage_when_commit_fails(string faultPoint)
    {
        using var sandbox = new StageSandbox();
        var stage = Plan(sandbox.Root);
        var originalPublisher = new EndToEndStagePublisher();
        originalPublisher.Publish(stage);
        var sentinel = Path.Combine(stage.DestinationDirectory, "previous-stage.txt");
        File.WriteAllText(sentinel, "previous");
        var publisher = new EndToEndStagePublisher(new ThrowAtFaultPoint(faultPoint));

        var error = Assert.Throws<EndToEndStageException>(() => publisher.Publish(stage));

        Assert.Multiple(() =>
        {
            Assert.That(error!.Message, Does.Contain("Injected stage commit failure."),
                "A clean rollback must retain the bounded root cause for operator diagnosis.");
            Assert.That(File.ReadAllText(sentinel), Is.EqualTo("previous"));
            Assert.That(Directory.GetDirectories(Path.GetDirectoryName(stage.DestinationDirectory)!, ".DevEndToEndTests.*"), Is.Empty);
        });
    }

    [Test]
    public void Cleanup_refuses_a_stage_whose_transaction_marker_changed()
    {
        using var sandbox = new StageSandbox();
        var stage = Plan(sandbox.Root);
        var publisher = new EndToEndStagePublisher();
        var lease = publisher.Publish(stage);
        var markerPath = Path.Combine(stage.DestinationDirectory, EndToEndStageMarker.FileName);
        var marker = GatewayContractJson.Read<EndToEndStageMarker>(File.ReadAllText(markerPath));
        marker.TransactionId = Guid.NewGuid().ToString("N");
        File.WriteAllText(markerPath, GatewayContractJson.Write(marker));

        Assert.That(publisher.TryCleanup(lease), Is.False);
        Assert.That(Directory.Exists(stage.DestinationDirectory), Is.True);
    }

    [Test]
    public void Prepared_lease_before_temporary_creation_preserves_an_existing_owned_stage_and_clears()
    {
        using var sandbox = new StageSandbox();
        var stage = Plan(sandbox.Root);
        var publisher = new EndToEndStagePublisher();
        var previous = publisher.Publish(stage, _ => { });
        var sentinel = Path.Combine(stage.DestinationDirectory, "previous-stage.txt");
        File.WriteAllText(sentinel, "previous");
        var interrupted = (EndToEndStageLease)Activator.CreateInstance(
            typeof(EndToEndStageLease),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: new object[]
            {
                stage.DestinationDirectory,
                stage.OwnerPackageId,
                stage.RimWorldVersion,
                Guid.NewGuid().ToString("N"),
                EndToEndStageLeaseState.Prepared
            },
            culture: null)!;

        Assert.Multiple(() =>
        {
            Assert.That(publisher.TryCleanup(interrupted), Is.True);
            Assert.That(File.ReadAllText(sentinel), Is.EqualTo("previous"));
        });
        Assert.That(publisher.TryCleanup(previous), Is.True);
    }

    [TestCase("..")]
    [TestCase("../outside")]
    [TestCase("alpha/mod")]
    [TestCase("alpha\\mod")]
    public void Stage_paths_reject_escaping_or_composite_owner_segments(string ownerPackageId)
    {
        using var sandbox = new StageSandbox();

        Assert.Throws<EndToEndStageException>(() =>
            EndToEndStagePaths.GetDestination(sandbox.Root, ownerPackageId, "1.6"));
    }

    private static readonly string[] ResolvablePackages =
    {
        "brrainz.harmony",
        "ludeon.rimworld",
        "alpha.mod",
        "optional.mod"
    };

    private static EndToEndOwnerStagePlan Plan(string modsRoot) => EndToEndBundlePlanner.Create(
        new[] { ValidCandidate() },
        ResolvablePackages,
        modsRoot,
        "1.6").OwnerStages.Single();

    private static EndToEndAssemblyCandidate ValidCandidate()
    {
        var project = Path.Combine(
            RepositoryRoot,
            "tests",
            "Fixtures",
            "EndToEndHost.ValidFixtures",
            "EndToEndHost.ValidFixtures.csproj");
        var assembly = Path.Combine(
            Path.GetDirectoryName(project)!,
            "bin",
            Configuration,
            "net480",
            "EndToEndHost.ValidFixtures.dll");
        return new EndToEndAssemblyCandidate(
            project,
            "alpha.mod",
            "EndToEndHost.ValidFixtures",
            EndToEndAssemblyMetadataReader.Read(assembly));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ImmersiveChefs.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }

    private sealed class ThrowAtFaultPoint : IEndToEndStageFaultInjector
    {
        private readonly string expectedPoint;

        public ThrowAtFaultPoint(string expectedPoint)
        {
            this.expectedPoint = expectedPoint;
        }

        public void OnFaultPoint(string point)
        {
            if (StringComparer.Ordinal.Equals(point, expectedPoint))
            {
                throw new IOException("Injected stage commit failure.");
            }
        }
    }

    private enum LockMoveSource
    {
        TemporaryStage,
        ExistingDestination
    }

    private sealed class ReleaseStageFileLock : IEndToEndStageFaultInjector, IDisposable
    {
        private readonly string stageParent;
        private readonly LockMoveSource source;
        private FileStream? stream;

        public ReleaseStageFileLock(string stageParent, LockMoveSource source)
        {
            this.stageParent = stageParent;
            this.source = source;
        }

        public bool Locked { get; private set; }

        public int RetryCount { get; private set; }

        public void OnFaultPoint(string point)
        {
            if (StringComparer.Ordinal.Equals(point, "move-retry"))
            {
                RetryCount++;
                Interlocked.Exchange(ref stream, null)?.Dispose();
                return;
            }

            if (!StringComparer.Ordinal.Equals(point, "after-stage-write"))
            {
                return;
            }

            var moveSource = source == LockMoveSource.TemporaryStage
                ? Directory.GetDirectories(stageParent, ".DevEndToEndTests.stage.*").Single()
                : Path.Combine(stageParent, EndToEndStagePaths.ContentDirectoryName);
            var assembly = Directory.GetFiles(moveSource, "*.dll").Single();
            stream = new FileStream(
                assembly,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            Locked = true;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref stream, null)?.Dispose();
        }
    }

    private sealed class LockBackupThenFail : IEndToEndStageFaultInjector, IDisposable
    {
        private readonly string stageParent;
        private FileStream? stream;

        internal LockBackupThenFail(string stageParent)
        {
            this.stageParent = stageParent;
        }

        internal int RetryCount { get; private set; }

        public void OnFaultPoint(string point)
        {
            if (StringComparer.Ordinal.Equals(point, "move-retry"))
            {
                RetryCount++;
                Interlocked.Exchange(ref stream, null)?.Dispose();
                return;
            }

            if (!StringComparer.Ordinal.Equals(point, "after-backup"))
            {
                return;
            }

            var backup = Directory.GetDirectories(
                stageParent,
                ".DevEndToEndTests.backup.*").Single();
            stream = new FileStream(
                Directory.GetFiles(backup, "*.dll").Single(),
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            throw new IOException("Injected rollback trigger.");
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref stream, null)?.Dispose();
        }
    }

    private sealed class BlockingFaultPoint : IEndToEndStageFaultInjector, IDisposable
    {
        private readonly string point;
        private readonly ManualResetEventSlim entered = new(initialState: false);
        private readonly ManualResetEventSlim released = new(initialState: false);

        internal BlockingFaultPoint(string point)
        {
            this.point = point;
        }

        public void OnFaultPoint(string candidate)
        {
            if (!StringComparer.Ordinal.Equals(candidate, point))
            {
                return;
            }

            entered.Set();
            if (!released.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new TimeoutException("The transaction-lock test did not release its first publisher.");
            }
        }

        internal bool WaitUntilEntered(TimeSpan timeout) => entered.Wait(timeout);

        internal void Release() => released.Set();

        public void Dispose()
        {
            released.Set();
            entered.Dispose();
            released.Dispose();
        }
    }

    private sealed class OccupyDestinationAfterBackup : IEndToEndStageFaultInjector
    {
        private readonly string destination;

        internal OccupyDestinationAfterBackup(string destination)
        {
            this.destination = destination;
        }

        public void OnFaultPoint(string point)
        {
            if (!StringComparer.Ordinal.Equals(point, "after-backup"))
            {
                return;
            }

            Directory.CreateDirectory(destination);
            File.WriteAllText(Path.Combine(destination, "foreign.txt"), "foreign");
            throw new IOException("Injected occupied rollback destination.");
        }
    }

    private sealed class StageSandbox : IDisposable
    {
        public StageSandbox()
        {
            Root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "stage-sandbox", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
