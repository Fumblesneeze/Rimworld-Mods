using System.Reflection;
using System.IO;
using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndDiscoveryCoordinatorTests
{
    [Test]
    public void Disabled_coordinator_never_constructs_enabled_discovery_dependencies()
    {
        var factoryCalls = 0;
        using var coordinator = GatewayEndToEndCoordinator.Create(
            enabled: false,
            () =>
            {
                factoryCalls++;
                throw new AssertionException("Disabled E2E discovery must stay inert.");
            });

        coordinator.AttachSession("disabled-run", "secret-token");
        coordinator.Tick();

        Assert.Multiple(() =>
        {
            Assert.That(factoryCalls, Is.Zero);
            Assert.That(coordinator.Snapshot.Enabled, Is.False);
            Assert.That(coordinator.Snapshot.DiscoveryState, Is.EqualTo("disabled"));
            Assert.That(coordinator.PublishedSnapshot, Is.SameAs(coordinator.Snapshot));
        });
    }

    [Test]
    public void Enabled_discovery_waits_for_initial_durability_and_advances_one_operation_per_tick()
    {
        var cursor = new RecordingCursor(new[]
        {
            GatewayEndToEndManifestDiscoveryStep.ActivePackage("ludeon.rimworld"),
            GatewayEndToEndManifestDiscoveryStep.ActivePackage("alpha.mod"),
            GatewayEndToEndManifestDiscoveryStep.ActivePackage(EndToEndTestContract.GatewayPackageId),
            GatewayEndToEndManifestDiscoveryStep.Candidate(
                new GatewayEndToEndManifestCandidate("alpha.mod", TestManifestPath())),
            GatewayEndToEndManifestDiscoveryStep.Complete()
        });
        var source = new RecordingSource(cursor);
        var store = new ControlledStore();
        var inspector = new RecordingInspector(LoadedInspection());
        var inspections = new ControlledInspectionFactory(inspector);
        using var coordinator = GatewayEndToEndCoordinator.CreateEnabledWithStore(
            source,
            inspector,
            inspections,
            store);

        coordinator.AttachSession("e2e-run");
        coordinator.Tick();
        Assert.Multiple(() =>
        {
            Assert.That(source.BeginCount, Is.Zero, "Discovery started before the initial artifact commit.");
            Assert.That(cursor.AdvanceCount, Is.Zero);
            Assert.That(coordinator.PublishedSnapshot, Is.Null);
        });

        store.CompletePending(success: true);
        coordinator.Tick();
        Assert.That(source.BeginCount, Is.Zero, "The persistence completion is the one transition for this frame.");

        coordinator.Tick();
        Assert.Multiple(() =>
        {
            Assert.That(source.BeginCount, Is.EqualTo(1));
            Assert.That(cursor.AdvanceCount, Is.Zero);
        });

        coordinator.Tick();
        Assert.Multiple(() =>
        {
            Assert.That(cursor.AdvanceCount, Is.EqualTo(1));
            Assert.That(coordinator.Snapshot.ActivePackageIds, Is.EqualTo(new[] { "ludeon.rimworld" }));
        });

        store.CompletePending(success: true);
        coordinator.Tick();
        coordinator.Tick();
        Assert.That(cursor.AdvanceCount, Is.EqualTo(2));

        store.CompletePending(success: true);
        coordinator.Tick();
        coordinator.Tick();
        Assert.That(cursor.AdvanceCount, Is.EqualTo(3));

        store.CompletePending(success: true);
        coordinator.Tick();
        coordinator.Tick();
        Assert.Multiple(() =>
        {
            Assert.That(cursor.AdvanceCount, Is.EqualTo(4));
            Assert.That(inspections.BeginCount, Is.EqualTo(1));
            Assert.That(inspector.InspectCount, Is.Zero, "The controlled background operation has not completed.");
        });

        inspections.CompletePending();
        coordinator.Tick();
        Assert.Multiple(() =>
        {
            Assert.That(inspector.InspectCount, Is.EqualTo(1));
            Assert.That(inspector.LastActivePackages, Is.EqualTo(new[]
            {
                "ludeon.rimworld",
                "alpha.mod",
                EndToEndTestContract.GatewayPackageId
            }));
            Assert.That(coordinator.Snapshot.Tests.Select(test => test.Id), Is.EqualTo(new[] { "alpha.e2e" }));
            Assert.That(cursor.AdvanceCount, Is.EqualTo(4));
        });

        store.CompletePending(success: true);
        coordinator.Tick();
        coordinator.Tick();
        Assert.That(cursor.AdvanceCount, Is.EqualTo(5));
        Assert.That(coordinator.Snapshot.DiscoveryState, Is.EqualTo("completed"));
    }

    [Test]
    public void Failed_bundle_is_persisted_without_hiding_a_later_bundle()
    {
        var candidates = new[]
        {
            new GatewayEndToEndManifestCandidate("alpha.mod", TestManifestPath("bad")),
            new GatewayEndToEndManifestCandidate("alpha.mod", TestManifestPath("good"))
        };
        var cursor = new RecordingCursor(new[]
        {
            GatewayEndToEndManifestDiscoveryStep.ActivePackage("ludeon.rimworld"),
            GatewayEndToEndManifestDiscoveryStep.ActivePackage("alpha.mod"),
            GatewayEndToEndManifestDiscoveryStep.ActivePackage(EndToEndTestContract.GatewayPackageId),
            GatewayEndToEndManifestDiscoveryStep.Candidate(candidates[0]),
            GatewayEndToEndManifestDiscoveryStep.Candidate(candidates[1]),
            GatewayEndToEndManifestDiscoveryStep.Complete()
        });
        var store = new ImmediateStore();
        var inspector = new SequencedInspector(
            GatewayEndToEndBundleInspectionResult.Failed("bad_bundle", "fixed failure"),
            LoadedInspection());
        using var coordinator = GatewayEndToEndCoordinator.CreateEnabledWithStore(
            new RecordingSource(cursor),
            inspector,
            new ImmediateInspectionFactory(inspector),
            store);

        coordinator.AttachSession("e2e-isolation");
        for (var tick = 0;
             tick < 32 && coordinator.PublishedSnapshot?.DiscoveryState != "completed_with_failures";
             tick++)
        {
            coordinator.Tick();
        }

        Assert.Multiple(() =>
        {
            Assert.That(inspector.InspectCount, Is.EqualTo(2));
            Assert.That(coordinator.Snapshot.DiscoveryState, Is.EqualTo("completed_with_failures"));
            Assert.That(coordinator.Snapshot.Failures.Select(failure => failure.Code), Is.EqualTo(new[] { "bad_bundle" }));
            Assert.That(coordinator.Snapshot.Tests.Select(test => test.Id), Is.EqualTo(new[] { "alpha.e2e" }));
            Assert.That(coordinator.PublishedSnapshot, Is.SameAs(store.CommittedSnapshot));
        });
    }

    [Test]
    public void Failed_persistence_retries_the_exact_snapshot_before_discovery_continues()
    {
        var cursor = new RecordingCursor(new[]
        {
            GatewayEndToEndManifestDiscoveryStep.ActivePackage("ludeon.rimworld"),
            GatewayEndToEndManifestDiscoveryStep.Complete()
        });
        var source = new RecordingSource(cursor);
        var store = new ControlledStore();
        using var coordinator = GatewayEndToEndCoordinator.CreateEnabledWithStore(
            source,
            new RecordingInspector(GatewayEndToEndBundleInspectionResult.Skipped()),
            new ImmediateInspectionFactory(
                new RecordingInspector(GatewayEndToEndBundleInspectionResult.Skipped())),
            store);

        coordinator.AttachSession("retry-run", "credential-that-must-not-leak");
        var exactInitialCandidate = store.PendingSnapshot;
        store.CompletePending(success: false);
        coordinator.Tick();
        coordinator.Tick();

        Assert.Multiple(() =>
        {
            Assert.That(store.BeginCount, Is.EqualTo(2));
            Assert.That(store.PendingSnapshot, Is.SameAs(exactInitialCandidate));
            Assert.That(source.BeginCount, Is.Zero);
            Assert.That(cursor.AdvanceCount, Is.Zero);
            Assert.That(coordinator.PublishedSnapshot, Is.Null);
        });

        store.CompletePending(success: true);
        coordinator.Tick();
        coordinator.Tick();
        Assert.That(source.BeginCount, Is.EqualTo(1));
    }

    [Test]
    public void Incomplete_active_mod_identity_never_reaches_the_bundle_loader()
    {
        var cursor = new RecordingCursor(new[]
        {
            GatewayEndToEndManifestDiscoveryStep.Failure(
                "active_mod_read_failed",
                "fixed active mod failure"),
            GatewayEndToEndManifestDiscoveryStep.ActivePackage("ludeon.rimworld"),
            GatewayEndToEndManifestDiscoveryStep.ActivePackage("alpha.mod"),
            GatewayEndToEndManifestDiscoveryStep.ActivePackage(EndToEndTestContract.GatewayPackageId),
            GatewayEndToEndManifestDiscoveryStep.Candidate(
                new GatewayEndToEndManifestCandidate("alpha.mod", TestManifestPath())),
            GatewayEndToEndManifestDiscoveryStep.Complete()
        });
        var inspector = new RecordingInspector(LoadedInspection());
        var store = new ImmediateStore();
        using var coordinator = GatewayEndToEndCoordinator.CreateEnabledWithStore(
            new RecordingSource(cursor),
            inspector,
            new ImmediateInspectionFactory(inspector),
            store);

        coordinator.AttachSession("untrusted-active-list");
        for (var tick = 0;
             tick < 32 && coordinator.PublishedSnapshot?.DiscoveryState != "completed_with_failures";
             tick++)
        {
            coordinator.Tick();
        }

        Assert.Multiple(() =>
        {
            Assert.That(inspector.InspectCount, Is.Zero);
            Assert.That(coordinator.PublishedSnapshot!.Failures.Select(failure => failure.Code),
                Does.Contain("active_package_list_untrusted"));
            Assert.That(coordinator.PublishedSnapshot.Tests, Is.Empty);
        });
    }

    private static GatewayEndToEndBundleInspectionResult LoadedInspection()
    {
        var descriptor = new GatewayEndToEndRuntimeTestDescriptor(
            "alpha.e2e",
            "alpha.mod",
            new[] { "ludeon.rimworld", "alpha.mod" },
            typeof(FakeEndToEndTest).FullName!,
            100,
            200,
            30,
            typeof(FakeEndToEndTest));
        return GatewayEndToEndBundleInspectionResult.Loaded(
            new GatewayEndToEndAssemblySource(
                "alpha.mod",
                TestManifestPath(),
                "Fixture, Version=1.0.0.0, Culture=neutral, PublicKey=null",
                new string('a', 64),
                typeof(FakeEndToEndTest).Assembly,
                new[] { descriptor }));
    }

    private static string TestManifestPath(string name = "Fixture") => Path.Combine(
        TestContext.CurrentContext.WorkDirectory,
        "alpha.mod",
        "1.6",
        "DevEndToEndTests",
        name + ".e2etests.json");

    private sealed class FakeEndToEndTest : IRimWorldEndToEndTest
    {
        public void Arrange(IEndToEndContext context)
        {
        }

        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            yield break;
        }
    }

    private sealed class RecordingSource : IGatewayEndToEndManifestSource
    {
        private readonly IGatewayEndToEndManifestDiscoveryCursor cursor;

        public RecordingSource(IGatewayEndToEndManifestDiscoveryCursor cursor) => this.cursor = cursor;

        public int BeginCount { get; private set; }

        public IGatewayEndToEndManifestDiscoveryCursor BeginDiscovery()
        {
            BeginCount++;
            return cursor;
        }
    }

    private sealed class RecordingCursor : IGatewayEndToEndManifestDiscoveryCursor
    {
        private readonly Queue<GatewayEndToEndManifestDiscoveryStep> steps;

        public RecordingCursor(IEnumerable<GatewayEndToEndManifestDiscoveryStep> steps) =>
            this.steps = new Queue<GatewayEndToEndManifestDiscoveryStep>(steps);

        public int AdvanceCount { get; private set; }

        public GatewayEndToEndManifestDiscoveryStep Advance()
        {
            AdvanceCount++;
            return steps.Dequeue();
        }

        public void Dispose()
        {
        }
    }

    private class RecordingInspector : IGatewayEndToEndBundleInspector
    {
        private readonly GatewayEndToEndBundleInspectionResult result;

        public RecordingInspector(GatewayEndToEndBundleInspectionResult result) => this.result = result;

        public int InspectCount { get; protected set; }

        public IReadOnlyList<string>? LastActivePackages { get; private set; }

        public virtual GatewayEndToEndBundleInspectionResult Inspect(
            GatewayEndToEndManifestCandidate candidate,
            IReadOnlyList<string> activePackageIds)
        {
            InspectCount++;
            LastActivePackages = activePackageIds.ToArray();
            return result;
        }
    }

    private sealed class SequencedInspector : RecordingInspector
    {
        private readonly Queue<GatewayEndToEndBundleInspectionResult> results;

        public SequencedInspector(params GatewayEndToEndBundleInspectionResult[] results)
            : base(GatewayEndToEndBundleInspectionResult.Skipped()) =>
            this.results = new Queue<GatewayEndToEndBundleInspectionResult>(results);

        public override GatewayEndToEndBundleInspectionResult Inspect(
            GatewayEndToEndManifestCandidate candidate,
            IReadOnlyList<string> activePackageIds)
        {
            InspectCount++;
            return results.Dequeue();
        }
    }

    private sealed class ControlledInspectionFactory : IGatewayEndToEndInspectionOperationFactory
    {
        private readonly IGatewayEndToEndBundleInspector inspector;
        private ControlledInspectionOperation? pending;

        public ControlledInspectionFactory(IGatewayEndToEndBundleInspector inspector) => this.inspector = inspector;

        public int BeginCount { get; private set; }

        public IGatewayEndToEndInspectionOperation Begin(
            GatewayEndToEndManifestCandidate candidate,
            IReadOnlyList<string> activePackageIds)
        {
            BeginCount++;
            pending = new ControlledInspectionOperation(inspector, candidate, activePackageIds);
            return pending;
        }

        public void CompletePending() => pending!.Complete();
    }

    private sealed class ImmediateInspectionFactory : IGatewayEndToEndInspectionOperationFactory
    {
        private readonly IGatewayEndToEndBundleInspector inspector;

        public ImmediateInspectionFactory(IGatewayEndToEndBundleInspector inspector) => this.inspector = inspector;

        public IGatewayEndToEndInspectionOperation Begin(
            GatewayEndToEndManifestCandidate candidate,
            IReadOnlyList<string> activePackageIds) =>
            new ImmediateInspectionOperation(inspector.Inspect(candidate, activePackageIds));
    }

    private sealed class ControlledInspectionOperation : IGatewayEndToEndInspectionOperation
    {
        private readonly IGatewayEndToEndBundleInspector inspector;
        private readonly GatewayEndToEndManifestCandidate candidate;
        private readonly IReadOnlyList<string> activePackageIds;
        private GatewayEndToEndBundleInspectionResult? outcome;

        public ControlledInspectionOperation(
            IGatewayEndToEndBundleInspector inspector,
            GatewayEndToEndManifestCandidate candidate,
            IReadOnlyList<string> activePackageIds)
        {
            this.inspector = inspector;
            this.candidate = candidate;
            this.activePackageIds = activePackageIds;
        }

        public bool IsCompleted => outcome is not null;

        public void Complete() => outcome = inspector.Inspect(candidate, activePackageIds);

        public GatewayEndToEndBundleInspectionResult GetOutcome() => outcome!;
    }

    private sealed class ImmediateInspectionOperation : IGatewayEndToEndInspectionOperation
    {
        private readonly GatewayEndToEndBundleInspectionResult outcome;

        public ImmediateInspectionOperation(GatewayEndToEndBundleInspectionResult outcome) => this.outcome = outcome;

        public bool IsCompleted => true;

        public GatewayEndToEndBundleInspectionResult GetOutcome() => outcome;
    }

    private sealed class ControlledStore : IGatewayEndToEndSessionArtifactStore
    {
        private ControlledPersistenceOperation? pending;

        public string? ArtifactPath => IsAttached ? "controlled/end-to-end-tests.json" : null;

        public bool IsAttached { get; private set; }

        public GatewayEndToEndSnapshot? CommittedSnapshot { get; private set; }

        public GatewayEndToEndSnapshot? PendingSnapshot => pending?.Snapshot;

        public int BeginCount { get; private set; }

        public IGatewayEndToEndPersistenceOperation BeginAttachSession(
            string runId,
            GatewayEndToEndSnapshot initialSnapshot) => Begin(initialSnapshot, attach: true);

        public IGatewayEndToEndPersistenceOperation BeginPersist(GatewayEndToEndSnapshot snapshot) =>
            Begin(snapshot, attach: false);

        public void CompletePending(bool success)
        {
            pending!.Complete(success);
            if (success)
            {
                IsAttached |= pending.Attach;
                CommittedSnapshot = pending.Snapshot;
            }
        }

        private IGatewayEndToEndPersistenceOperation Begin(GatewayEndToEndSnapshot snapshot, bool attach)
        {
            BeginCount++;
            pending = new ControlledPersistenceOperation(snapshot, attach);
            return pending;
        }
    }

    private sealed class ImmediateStore : IGatewayEndToEndSessionArtifactStore
    {
        public string? ArtifactPath { get; private set; }

        public bool IsAttached => ArtifactPath is not null;

        public GatewayEndToEndSnapshot? CommittedSnapshot { get; private set; }

        public IGatewayEndToEndPersistenceOperation BeginAttachSession(
            string runId,
            GatewayEndToEndSnapshot initialSnapshot)
        {
            ArtifactPath = "immediate/end-to-end-tests.json";
            CommittedSnapshot = initialSnapshot;
            return new ImmediatePersistenceOperation(initialSnapshot);
        }

        public IGatewayEndToEndPersistenceOperation BeginPersist(GatewayEndToEndSnapshot snapshot)
        {
            CommittedSnapshot = snapshot;
            return new ImmediatePersistenceOperation(snapshot);
        }
    }

    private sealed class ControlledPersistenceOperation : IGatewayEndToEndPersistenceOperation
    {
        private GatewayEndToEndPersistenceOutcome? outcome;

        public ControlledPersistenceOperation(GatewayEndToEndSnapshot snapshot, bool attach)
        {
            Snapshot = snapshot;
            Attach = attach;
        }

        public GatewayEndToEndSnapshot Snapshot { get; }

        public bool Attach { get; }

        public bool IsCompleted => outcome is not null;

        public void Complete(bool success) => outcome = success
            ? GatewayEndToEndPersistenceOutcome.Success(Snapshot)
            : GatewayEndToEndPersistenceOutcome.Failed(Snapshot, new IOException("write failed"));

        public GatewayEndToEndPersistenceOutcome GetOutcome() => outcome!;
    }

    private sealed class ImmediatePersistenceOperation : IGatewayEndToEndPersistenceOperation
    {
        private readonly GatewayEndToEndPersistenceOutcome outcome;

        public ImmediatePersistenceOperation(GatewayEndToEndSnapshot snapshot) =>
            outcome = GatewayEndToEndPersistenceOutcome.Success(snapshot);

        public bool IsCompleted => true;

        public GatewayEndToEndPersistenceOutcome GetOutcome() => outcome;
    }
}
