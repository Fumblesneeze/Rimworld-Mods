using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.EndToEndHost.Tests;

[TestFixture]
public sealed class PerformanceBundlePlannerTests
{
    [Test]
    public void Selected_stage_excludes_unrelated_product_bundles_and_package_requirements()
    {
        var root = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "performance-stage-selected",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var selected = PerformanceDiscoveryValidator.SelectForFilters(
                PerformanceMetadataDiscoveryTests.ValidCandidatesForPlanning(),
                new[] { "gateway.alpha-absent" },
                Array.Empty<string>());

            var plan = PerformanceBundlePlanner.Create(
                selected,
                new[] { "brrainz.harmony", "ludeon.rimworld", "astryl.circinus" },
                root,
                "1.6");

            Assert.Multiple(() =>
            {
                Assert.That(plan.OwnerStages.Select(stage => stage.OwnerPackageId),
                    Is.EqualTo(new[] { "fumblesneeze.rimworlddevgateway" }));
                Assert.That(plan.Discovery.Groups.SelectMany(group => group.Benchmarks)
                        .Select(benchmark => benchmark.Id),
                    Is.EqualTo(new[] { "gateway.alpha-absent" }));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Planner_stops_hostile_candidate_enumeration_at_the_published_ceiling()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "performance-stage-bounded", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var candidate = PerformanceMetadataDiscoveryTests.ValidCandidatesForPlanning().First();
        var enumerated = 0;

        IEnumerable<PerformanceAssemblyCandidate> Hostile()
        {
            while (true)
            {
                enumerated++;
                if (enumerated > PerformanceDiscoveryValidator.MaximumBenchmarks + 1)
                    throw new InvalidOperationException("Planner enumerated beyond its published candidate ceiling.");
                yield return candidate;
            }
        }

        try
        {
            var error = Assert.Throws<EndToEndDiscoveryException>(() =>
                PerformanceBundlePlanner.Create(Hostile(), Packages(), root, "1.6"));

            Assert.Multiple(() =>
            {
                Assert.That(error!.Message, Does.Contain("candidate-assembly ceiling"));
                Assert.That(enumerated, Is.EqualTo(PerformanceDiscoveryValidator.MaximumBenchmarks + 1));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Planner_rejects_two_owner_bundles_with_the_same_staged_file_names()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "performance-stage-collision", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var original = PerformanceMetadataDiscoveryTests.ValidCandidatesForPlanning().First();
            var copiedDeclarations = original.Metadata.Declarations
                .Select(item => CopyDeclaration(
                    item,
                    "copy." + item.Id,
                    "copy." + item.ComparisonId,
                    item.WarmUpTicks,
                    item.SampleTicks))
                .ToArray();
            var duplicateOutput = new PerformanceAssemblyCandidate(
                original.ProjectPath + ".copy",
                original.ExpectedOwnerPackageId,
                original.ExpectedAssemblyName,
                new PerformanceAssemblyMetadata(original.Metadata.Assembly, copiedDeclarations));

            var error = Assert.Throws<EndToEndStageException>(() =>
                PerformanceBundlePlanner.Create(
                    new[] { original, duplicateOutput },
                    Packages(),
                    root,
                    "1.6"));

            Assert.That(error!.Message, Does.Contain("duplicate staged file"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Planner_deadline_contains_a_valid_one_hour_normal_speed_sample_plus_overhead()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "performance-stage-deadline", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var candidates = PerformanceMetadataDiscoveryTests.ValidCandidatesForPlanning();
            var source = candidates.First();
            var longMetadata = new PerformanceAssemblyMetadata(
                source.Metadata.Assembly,
                source.Metadata.Declarations.Select(item =>
                    CopyDeclaration(item, item.Id, item.ComparisonId, 0, 216_000)));
            candidates[0] = new PerformanceAssemblyCandidate(
                source.ProjectPath,
                source.ExpectedOwnerPackageId,
                source.ExpectedAssemblyName,
                longMetadata);

            var plan = PerformanceBundlePlanner.Create(candidates, Packages(), root, "1.6");
            var test = plan.OwnerStages
                .Single(item => item.OwnerPackageId == "alpha.mod")
                .Bundles.Single().Manifest.Tests.First();

            Assert.Multiple(() =>
            {
                Assert.That(test.MaxWallClockSeconds, Is.GreaterThan(3_600));
                Assert.That(test.MaxGameTicks, Is.GreaterThan(216_000));
                Assert.That(test.MaxFrames, Is.GreaterThan(test.MaxWallClockSeconds));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Planner_emits_exact_performance_manifests_for_the_shared_owned_stage()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "performance-stage", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var plan = PerformanceBundlePlanner.Create(
                PerformanceMetadataDiscoveryTests.ValidCandidatesForPlanning(),
                Packages(),
                root,
                "1.6");
            var owner = plan.OwnerStages.Single(item => item.OwnerPackageId == "alpha.mod");
            var manifest = owner.Bundles.Single().Manifest;
            var benchmark = manifest.Tests.Single(item => item.Id == "alpha.base-instrumented");

            Assert.Multiple(() =>
            {
                Assert.That(plan.Discovery.Groups, Is.Not.Empty);
                Assert.That(owner.DestinationDirectory,
                    Does.EndWith(Path.Combine("alpha.mod", "1.6", EndToEndStagePaths.ContentDirectoryName)));
                Assert.That(benchmark.Kind, Is.EqualTo(EndToEndBundleTest.PerformanceKind));
                Assert.That(benchmark.OwnerPackageId, Is.EqualTo("alpha.mod"));
                Assert.That(benchmark.MeasuredSubjectPackageId, Is.EqualTo("alpha.mod"));
                Assert.That(benchmark.DeterministicSeed, Is.EqualTo(7123));
                Assert.That(benchmark.WorkloadVersion, Is.EqualTo("alpha/v2"));
                Assert.That(benchmark.ComparisonId, Is.EqualTo("alpha.base"));
                Assert.That(benchmark.WarmUpTicks, Is.EqualTo(900));
                Assert.That(benchmark.SampleTicks, Is.EqualTo(3600));
                Assert.That(benchmark.Repetitions, Is.EqualTo(2));
                Assert.That(benchmark.MethodSelectors, Has.Length.EqualTo(2));
                Assert.That(benchmark.ThroughputCheckpoints, Has.Length.EqualTo(2));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Planner_emits_explicit_Dpa_only_diagnostic_manifests()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "performance-stage-dpa", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var plan = PerformanceBundlePlanner.Create(
                PerformanceMetadataDiscoveryTests.ValidCandidatesForPlanning(),
                Packages().Append(PerformanceDiscoveryValidator.DpaPackageId),
                root,
                "1.6",
                PerformanceProfilerMode.DpaDiagnostic,
                "Verse.Map::MapPreTick()");
            var test = plan.OwnerStages.Single(item => item.OwnerPackageId == "alpha.mod")
                .Bundles.Single().Manifest.Tests.Single(item => item.Id == "alpha.base-instrumented");

            Assert.Multiple(() =>
            {
                Assert.That(test.PerformanceProfiler, Is.EqualTo("dpa"));
                Assert.That(test.DiagnosticSelector, Is.EqualTo("Verse.Map::MapPreTick()"));
                Assert.That(test.ActivePackageIds[2], Is.EqualTo(PerformanceDiscoveryValidator.DpaPackageId));
                Assert.That(test.ActivePackageIds, Does.Not.Contain(PerformanceDiscoveryValidator.CircinusPackageId));
                Assert.That(test.MethodSelectors, Has.Length.EqualTo(1));
                Assert.That(test.MethodSelectors![0].Value, Is.EqualTo("Verse.Map::MapPreTick()"));
                Assert.That(test.Repetitions, Is.EqualTo(1));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Existing_marker_owned_publisher_stages_and_cleans_performance_bundle_exactly()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "performance-publish", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var plan = PerformanceBundlePlanner.Create(
                PerformanceMetadataDiscoveryTests.ValidCandidatesForPlanning(),
                Packages(),
                root,
                "1.6");
            var owner = plan.OwnerStages.Single(item => item.OwnerPackageId == "alpha.mod");
            var publisher = new EndToEndStagePublisher();
            var lease = publisher.Publish(owner);

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(Path.Combine(
                    lease.DestinationDirectory,
                    "PerformanceHost.ValidFixtures.dll")), Is.True);
                Assert.That(Directory.EnumerateFiles(lease.DestinationDirectory, "*.e2etests.json").Count(), Is.EqualTo(1));
                Assert.That(publisher.TryCleanup(lease), Is.True);
                Assert.That(Directory.Exists(lease.DestinationDirectory), Is.False);
            });
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Stage_transaction_persists_each_lease_and_retains_failed_cleanup_for_a_later_owner_failure()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "performance-stage-rollback", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var plan = PerformanceBundlePlanner.Create(
                PerformanceMetadataDiscoveryTests.ValidCandidatesForPlanning(),
                Packages(),
                root,
                "1.6");
            var publisher = new EndToEndStagePublisher();
            var publishes = 0;
            var persisted = new List<string[]>();
            EndToEndStageLease? retainedLease = null;

            var error = Assert.Throws<EndToEndStageException>(() =>
                EndToEndStageTransaction.PublishAll(
                    plan.OwnerStages,
                    (owner, prepared) =>
                    {
                        publishes++;
                        if (publishes == 2) throw new InvalidOperationException("expected later-owner failure");
                        retainedLease = publisher.Publish(owner, prepared);
                        return retainedLease;
                    },
                    _ => false,
                    leases => persisted.Add(leases.Select(item => item.TransactionId).ToArray()),
                    () => persisted.Add(Array.Empty<string>())));

            Assert.Multiple(() =>
            {
                Assert.That(error!.Message, Does.Contain("cleanup was incomplete"));
                Assert.That(persisted, Has.Count.EqualTo(3));
                Assert.That(persisted[0], Has.Length.EqualTo(1));
                Assert.That(persisted[1], Is.EqualTo(persisted[0]));
                Assert.That(persisted[2], Is.EqualTo(persisted[0]));
                Assert.That(Directory.Exists(plan.OwnerStages[0].DestinationDirectory), Is.True);
            });
            Assert.That(publisher.TryCleanup(retainedLease!), Is.True);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Publisher_persists_the_exact_lease_before_creating_the_temporary_stage()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "performance-stage-precommit", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var plan = PerformanceBundlePlanner.Create(
                PerformanceMetadataDiscoveryTests.ValidCandidatesForPlanning(),
                Packages(),
                root,
                "1.6");
            EndToEndStageLease? observedLease = null;
            var publisher = new EndToEndStagePublisher(new CallbackFaultInjector(point =>
            {
                if (point is "after-stage-write" or "after-commit")
                {
                    Assert.That(observedLease, Is.Not.Null);
                }
            }));
            var lease = publisher.Publish(
                plan.OwnerStages[0],
                prepared =>
                {
                    Assert.That(prepared.DestinationDirectory, Is.EqualTo(plan.OwnerStages[0].DestinationDirectory));
                    if (prepared.State == EndToEndStageLeaseState.Prepared)
                    {
                        var parent = Path.GetDirectoryName(prepared.DestinationDirectory)!;
                        Assert.That(Directory.GetDirectories(parent, ".DevEndToEndTests.*"), Is.Empty,
                            "The prepared lease must be durable before any transaction directory exists.");
                    }
                    observedLease = prepared;
                });

            Assert.Multiple(() =>
            {
                Assert.That(observedLease, Is.SameAs(lease));
                Assert.That(Directory.Exists(lease.DestinationDirectory), Is.True);
            });
            Assert.That(publisher.TryCleanup(lease), Is.True);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private sealed class CallbackFaultInjector : IEndToEndStageFaultInjector
    {
        private readonly Action<string> callback;
        public CallbackFaultInjector(Action<string> callback) => this.callback = callback;
        public void OnFaultPoint(string point) => callback(point);
    }

    private static string[] Packages() => new[]
    {
        "brrainz.harmony", "ludeon.rimworld", "astryl.circinus",
        "alpha.mod", "optional.mod", "fumblesneeze.rimworlddevgateway"
    };

    private static PerformanceMetadataDeclaration CopyDeclaration(
        PerformanceMetadataDeclaration source,
        string id,
        string comparisonId,
        int warmUpTicks,
        int sampleTicks) =>
        new(
            source.TypeName,
            id,
            source.StagingOwnerPackageId,
            source.MeasuredSubjectPackageId,
            source.ActivePackageIds,
            source.DeterministicSeed,
            source.WorkloadVersion,
            comparisonId,
            warmUpTicks,
            sampleTicks,
            source.GameSpeed,
            source.Repetitions,
            source.EvidenceLens,
            source.ProductAbsentControlId,
            source.MethodSelectors,
            source.ThroughputCheckpoints,
            source.IsConcrete,
            source.ImplementsContract,
            source.ImplementsThroughputCounter,
            source.HasPublicParameterlessConstructor);
}
