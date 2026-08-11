using System;
using System.IO;
using System.Linq;
using RimWorldDevGateway.Contracts;
using NUnit.Framework;

namespace RimWorldDevGateway.EndToEndHost.Tests;

[TestFixture]
public sealed class PerformanceRunPlanTests
{
    [Test]
    public void Plan_filters_one_benchmark_and_expands_fresh_process_repetitions_with_report_paths()
    {
        var discovery = PerformanceDiscoveryValidator.ValidateAndGroup(
            PerformanceMetadataDiscoveryTests.ValidCandidatesForPlanning(),
            new[]
            {
                "brrainz.harmony", "ludeon.rimworld", "astryl.circinus",
                "alpha.mod", "optional.mod", "fumblesneeze.rimworlddevgateway"
            },
            requireResolvedControls: false);
        var artifactRoot = Path.Combine(Path.GetTempPath(), "perf-plan", Guid.NewGuid().ToString("N"));

        var plan = PerformanceRunPlanBuilder.Create(
            discovery,
            benchmarkIds: new[] { "alpha.base-instrumented" },
            groupIds: Array.Empty<string>(),
            warmUpTicksOverride: 120,
            sampleTicksOverride: 600,
            repetitionsOverride: 3,
            artifactRoot);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Processes, Has.Count.EqualTo(3));
            Assert.That(plan.Processes.Select(item => item.Sequence), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(plan.Processes.Select(item => item.Repetition), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(plan.Processes, Has.All.Property(nameof(PerformanceProcessPlan.BenchmarkId))
                .EqualTo("alpha.base-instrumented"));
            Assert.That(plan.Processes, Has.All.Property(nameof(PerformanceProcessPlan.WarmUpTicks)).EqualTo(120));
            Assert.That(plan.Processes, Has.All.Property(nameof(PerformanceProcessPlan.SampleTicks)).EqualTo(600));
            Assert.That(plan.Processes[0].MaxWallClockSeconds,
                Is.EqualTo(PerformanceBundleDeadline.Calculate(120, 600).MaxWallClockSeconds));
            Assert.That(plan.Processes[0].ActivePackageIds.Last(), Is.EqualTo("fumblesneeze.rimworlddevgateway"));
            Assert.That(plan.Processes[0].TypeName, Does.EndWith("BaseInstrumentedBenchmark"));
            Assert.That(plan.Processes[0].AssemblyIdentity, Does.StartWith("PerformanceHost.ValidFixtures,"));
            Assert.That(plan.Processes[0].AssemblySha256, Has.Length.EqualTo(64));
            Assert.That(plan.Processes[0].DeterministicSeed, Is.EqualTo(7123));
            Assert.That(plan.Processes[0].WorkloadVersion, Is.EqualTo("alpha/v2"));
            Assert.That(plan.Processes[0].ComparisonId, Is.EqualTo("alpha.base"));
            Assert.That(plan.Processes[0].MethodSelectors, Has.Count.EqualTo(2));
            Assert.That(plan.Processes[0].ThroughputCheckpoints, Has.Count.EqualTo(2));
            Assert.That(plan.Processes[0].RawCircinusJsonPath, Does.EndWith("circinus.raw.json"));
            Assert.That(plan.Processes[0].NormalizedJsonPath, Does.EndWith("normalized.json"));
            Assert.That(plan.Processes[0].CsvReportPath, Does.EndWith("metrics.csv"));
            Assert.That(plan.Processes[0].MarkdownReportPath, Does.EndWith("summary.md"));
            Assert.That(plan.Processes.Select(item => Path.GetFileName(item.ProcessDirectory)),
                Is.EqualTo(new[] { "p0001-r01", "p0002-r02", "p0003-r03" }));
            Assert.That(plan.AggregateJsonPath, Is.EqualTo(Path.Combine(Path.GetFullPath(artifactRoot), "aggregate.json")));
        });
    }

    [Test]
    public void Plan_group_filter_preserves_every_declared_repetition_as_a_separate_process()
    {
        var discovery = PerformanceDiscoveryValidator.ValidateAndGroup(
            PerformanceMetadataDiscoveryTests.ValidCandidatesForPlanning(),
            new[]
            {
                "brrainz.harmony", "ludeon.rimworld", "astryl.circinus",
                "alpha.mod", "optional.mod", "fumblesneeze.rimworlddevgateway"
            },
            requireResolvedControls: false);
        var group = discovery.Groups.Single(item => item.Benchmarks.Any(test => test.Id == "alpha.base"));

        var plan = PerformanceRunPlanBuilder.Create(
            discovery,
            Array.Empty<string>(),
            new[] { group.GroupId },
            null,
            null,
            null,
            Path.Combine(Path.GetTempPath(), "perf-plan", Guid.NewGuid().ToString("N")));

        Assert.Multiple(() =>
        {
            Assert.That(plan.Groups, Has.Count.EqualTo(1));
            Assert.That(plan.Groups[0].Benchmarks, Has.Count.EqualTo(group.Benchmarks.Count));
            Assert.That(plan.Processes, Has.Count.EqualTo(group.Benchmarks.Sum(item => item.Repetitions)));
            Assert.That(plan.Processes.Select(item => item.BenchmarkId).Distinct().Count(),
                Is.EqualTo(group.Benchmarks.Count));
        });
    }

    [TestCase("unknown-benchmark", null, "benchmark")]
    [TestCase(null, "unknown-group", "group")]
    public void Plan_rejects_unknown_filters(string? benchmark, string? group, string expected)
    {
        var discovery = PerformanceDiscoveryValidator.ValidateAndGroup(
            PerformanceMetadataDiscoveryTests.ValidCandidatesForPlanning(),
            new[]
            {
                "brrainz.harmony", "ludeon.rimworld", "astryl.circinus",
                "alpha.mod", "optional.mod", "fumblesneeze.rimworlddevgateway"
            },
            requireResolvedControls: false);

        var exception = Assert.Throws<ArgumentException>(() => PerformanceRunPlanBuilder.Create(
            discovery,
            benchmark is null ? Array.Empty<string>() : new[] { benchmark },
            group is null ? Array.Empty<string>() : new[] { group },
            null,
            null,
            null,
            Path.GetTempPath()));

        Assert.That(exception!.Message, Does.Contain(expected).IgnoreCase.And.Contain("zero"));
    }

    [TestCase(-1, null, null, "warm-up")]
    [TestCase(null, 0, null, "sample")]
    [TestCase(null, null, 0, "repetitions")]
    public void Plan_rejects_non_positive_overrides(int? warmUp, int? sample, int? repetitions, string expected)
    {
        var discovery = PerformanceDiscoveryValidator.ValidateAndGroup(
            PerformanceMetadataDiscoveryTests.ValidCandidatesForPlanning(),
            new[]
            {
                "brrainz.harmony", "ludeon.rimworld", "astryl.circinus",
                "alpha.mod", "optional.mod", "fumblesneeze.rimworlddevgateway"
            },
            requireResolvedControls: false);

        var exception = Assert.Throws<ArgumentException>(() => PerformanceRunPlanBuilder.Create(
            discovery,
            new[] { "alpha.base-instrumented" },
            Array.Empty<string>(),
            warmUp,
            sample,
            repetitions,
            Path.GetTempPath()));

        Assert.That(exception!.Message, Does.Contain(expected).IgnoreCase);
    }
}
