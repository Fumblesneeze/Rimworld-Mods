using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace RimWorldDevGateway.EndToEndHost.Tests;

[TestFixture]
public sealed class PerformanceBaselineTests
{
    [Test]
    public void Compatible_metric_exceeding_either_tracked_threshold_fails_with_causal_context()
    {
        var baseline = Snapshot("accepted", Measurement(100d));
        var current = Snapshot("current", Measurement(112d));
        var policy = new PerformanceThresholdPolicy
        {
            Thresholds =
            [
                new PerformanceMetricThreshold
                {
                    BenchmarkId = "gateway.calibration.instrumented",
                    EvidenceLens = "instrumented-compatible",
                    Scope = "method",
                    Selector = "Example.Tick()",
                    MetricName = "exclusive-time",
                    Unit = "ms",
                    AbsoluteIncrease = 20d,
                    RelativeIncreasePercent = 10d
                }
            ]
        };

        var result = PerformanceBaselineComparer.Compare(current, [baseline], policy, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Passed, Is.False);
            Assert.That(result.Failures, Has.Count.EqualTo(1));
            var failure = result.Failures.Single();
            Assert.That(failure.Code, Is.EqualTo("threshold-exceeded"));
            Assert.That(failure.BaselineValue, Is.EqualTo(100d));
            Assert.That(failure.CurrentValue, Is.EqualTo(112d));
            Assert.That(failure.AbsoluteDelta, Is.EqualTo(12d));
            Assert.That(failure.RelativeDeltaPercent, Is.EqualTo(12d));
            Assert.That(failure.AbsoluteThreshold, Is.EqualTo(20d));
            Assert.That(failure.RelativeThresholdPercent, Is.EqualTo(10d));
            Assert.That(failure.BaselineCalls, Is.EqualTo(1200));
            Assert.That(failure.CurrentCalls, Is.EqualTo(1200));
            Assert.That(failure.BaselineTimedCalls, Is.EqualTo(300));
            Assert.That(failure.CurrentTimedCalls, Is.EqualTo(300));
            Assert.That(failure.BaselineDutyPercent, Is.EqualTo(25d));
            Assert.That(failure.CurrentDutyPercent, Is.EqualTo(25d));
            Assert.That(failure.ExactMethod, Is.EqualTo("Example:Example.Tick()"));
        });
    }

    [Test]
    public void Metric_sets_sampling_context_denominator_and_claim_are_bidirectionally_fail_closed()
    {
        var baseline = Snapshot("accepted", Measurement(100d));
        var extra = Snapshot("current", Measurement(100d), Measurement(5d) with { MetricName = "new-metric" });
        var denominator = Snapshot("current", Measurement(100d) with { Denominator = "other-denominator" });
        var claim = Snapshot("current", Measurement(100d) with { Claim = "other-claim" });
        var adaptive = Snapshot("current", Measurement(100d) with { SampleShift = 9 });
        var window = Snapshot("current", Measurement(100d) with { ProfilerWindowTicks = 2999 });

        Assert.Multiple(() =>
        {
            Assert.That(Codes(PerformanceBaselineComparer.Compare(extra, [baseline], Policy(), false)),
                Does.Contain("metric-set-drift"));
            Assert.That(Codes(PerformanceBaselineComparer.Compare(denominator, [baseline], Policy(), false)),
                Does.Contain("sampling-context-drift"));
            Assert.That(Codes(PerformanceBaselineComparer.Compare(claim, [baseline], Policy(), false)),
                Does.Contain("sampling-context-drift"));
            Assert.That(Codes(PerformanceBaselineComparer.Compare(adaptive, [baseline], Policy(), false)),
                Does.Contain("sampling-context-drift"));
            Assert.That(Codes(PerformanceBaselineComparer.Compare(window, [baseline], Policy(), false)),
                Does.Contain("sampling-context-drift"));
        });
    }

    [Test]
    public void Naturally_varying_valid_sampling_observations_remain_comparable_and_are_reported()
    {
        var baseline = Snapshot("accepted", Measurement(100d));
        var changedContext = Measurement(105d) with
        {
            Calls = 1400,
            TimedCalls = 320,
            DutyPercent = 33d,
            SampleShift = 4,
            RecordedCycles = 120,
            ProfilerWindowMilliseconds = 5000d,
            SamplingContextIdentity = "cycles=120|windowMs=5000|windowTicks=3000|duty=33|shift=4|calls=1400|timed=320"
        };

        var result = PerformanceBaselineComparer.Compare(
            Snapshot("current", changedContext), [baseline], Policy(), false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Passed, Is.True);
            Assert.That(result.Comparisons.Single().ThresholdsApplied, Is.True);
            Assert.That(result.Failures, Is.Empty);
            var evaluation = result.Comparisons.Single().ThresholdEvaluations.Single();
            Assert.That(evaluation.Exceeded, Is.False);
            Assert.That(evaluation.BaselineValue, Is.EqualTo(100d));
            Assert.That(evaluation.CurrentValue, Is.EqualTo(105d));
            Assert.That(evaluation.AbsoluteDelta, Is.EqualTo(5d));
            Assert.That(evaluation.RelativeThresholdPercent, Is.EqualTo(10d));
            Assert.That(evaluation.BaselineCalls, Is.EqualTo(1200d));
            Assert.That(evaluation.CurrentCalls, Is.EqualTo(1400d));
            Assert.That(evaluation.CurrentDutyPercent, Is.EqualTo(33d));
        });
    }

    [Test]
    public void Mod_scope_threshold_uses_complete_window_context_without_method_sidecar_fields()
    {
        var baselineMetric = Measurement(0.10d) with
        {
            Scope = "mod", Selector = "fumblesneeze.product", MetricName = "gross-profiler-window-share",
            Unit = "ratio", Denominator = "profiler-window-ms", Calls = null, TimedCalls = null,
            SampleShift = null, ExactMethod = string.Empty,
            SamplingContextIdentity = "cycles=100|windowMs=4000|windowTicks=3000|duty=25"
        };
        var currentMetric = baselineMetric with { Value = 0.11d, RecordedCycles = 120, ProfilerWindowMilliseconds = 5000d };
        var policy = new PerformanceThresholdPolicy
        {
            Thresholds =
            [
                new PerformanceMetricThreshold
                {
                    BenchmarkId = "gateway.calibration.instrumented", EvidenceLens = "instrumented-compatible",
                    Scope = "mod", Selector = "fumblesneeze.product", MetricName = "gross-profiler-window-share",
                    Unit = "ratio", RelativeIncreasePercent = 20d
                }
            ]
        };

        var result = PerformanceBaselineComparer.Compare(
            Snapshot("current", currentMetric), [Snapshot("accepted", baselineMetric)], policy, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Passed, Is.True);
            Assert.That(result.Comparisons.Single().ThresholdEvaluations.Single().CurrentRecordedCycles, Is.EqualTo(120d));
        });
    }

    [Test]
    public void Averaged_native_sample_shifts_remain_valid_for_multi_repetition_snapshots()
    {
        var baseline = Snapshot("accepted", Measurement(100d) with
        {
            SampleShift = 2.5,
            SamplingContextIdentity = "shift=2||shift=3"
        });
        baseline.Cases[0].Compatibility.RepetitionCount = 2;
        var current = Snapshot("current", Measurement(105d) with
        {
            SampleShift = 3.5,
            SamplingContextIdentity = "shift=3||shift=4"
        });
        current.Cases[0].Compatibility.RepetitionCount = 2;

        var result = PerformanceBaselineComparer.Compare(current, [baseline], Policy(), false);

        Assert.That(result.Passed, Is.True);
    }

    [Test]
    public void Thresholded_non_checkpoint_metric_without_sampling_context_fails_closed()
    {
        var withoutContext = Measurement(100d) with
        {
            Calls = null,
            TimedCalls = null,
            DutyPercent = null,
            SampleShift = null,
            RecordedCycles = null,
            ProfilerWindowMilliseconds = null,
            ProfilerWindowTicks = null,
            SamplingContextIdentity = string.Empty
        };

        var result = PerformanceBaselineComparer.Compare(
            Snapshot("current", withoutContext), [Snapshot("accepted", withoutContext)], Policy(), false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Passed, Is.False);
            Assert.That(Codes(result), Does.Contain("sampling-context-drift"));
            Assert.That(result.Comparisons.Single().ThresholdsApplied, Is.False);
        });
    }

    [Test]
    public void Compatible_case_without_an_applicable_threshold_fails_and_does_not_claim_application()
    {
        var result = PerformanceBaselineComparer.Compare(
            Snapshot("current", Measurement(100d)),
            [Snapshot("accepted", Measurement(100d))],
            new PerformanceThresholdPolicy(),
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Passed, Is.False);
            Assert.That(Codes(result), Does.Contain("threshold-policy-missing"));
            Assert.That(result.Comparisons.Single().ThresholdsApplied, Is.False);
        });
    }

    [Test]
    public void Missing_metric_unit_drift_runtime_error_and_workload_drift_are_fail_closed()
    {
        var baseline = Snapshot("accepted", Measurement(100d));
        var missing = Snapshot("current");
        var unitDrift = Snapshot("current", Measurement(100d) with { Unit = "ticks" });
        var runtimeError = Snapshot("current", Measurement(100d));
        runtimeError.RuntimeErrors.Add("new player-log error");
        var workloadDrift = Snapshot("current", Measurement(100d));
        workloadDrift.Cases[0].Compatibility.WorkloadVersion = "workload/v2";

        Assert.Multiple(() =>
        {
            Assert.That(Codes(PerformanceBaselineComparer.Compare(missing, [baseline], Policy(), false)),
                Does.Contain("missing-metric"));
            Assert.That(Codes(PerformanceBaselineComparer.Compare(unitDrift, [baseline], Policy(), false)),
                Does.Contain("metric-unit-drift"));
            Assert.That(Codes(PerformanceBaselineComparer.Compare(runtimeError, [baseline], Policy(), false)),
                Does.Contain("runtime-error"));
            Assert.That(Codes(PerformanceBaselineComparer.Compare(workloadDrift, [baseline], Policy(), false)),
                Does.Contain("workload-drift"));
        });
    }

    [Test]
    public void Cross_version_identity_drift_is_informational_only_when_explicitly_requested()
    {
        var baseline = Snapshot("accepted", Measurement(100d));
        var current = Snapshot("current", Measurement(500d));
        current.Cases[0].Compatibility.GameVersion = "1.7.1";
        current.Cases[0].Compatibility.ProductAssemblyIdentity = "product-sha-new";
        current.Cases[0].Compatibility.TestAssemblyIdentity = "tests-sha-new";
        current.Cases[0].Compatibility.CircinusAssemblyIdentity = "circinus-sha-new";

        var strict = PerformanceBaselineComparer.Compare(current, [baseline], Policy(), false);
        var informational = PerformanceBaselineComparer.Compare(current, [baseline], Policy(), true);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Passed, Is.False);
            Assert.That(Codes(strict), Does.Contain("workload-drift"));
            Assert.That(informational.Passed, Is.True);
            Assert.That(informational.Comparisons.Single().Status, Is.EqualTo("informational-cross-version"));
            Assert.That(informational.Comparisons.Single().ThresholdsApplied, Is.False,
                "Incompatible cross-version numbers must never be used for a pass/fail threshold claim.");
            var delta = informational.Comparisons.Single().InformationalMetrics.Single();
            Assert.That(delta.BaselineValue, Is.EqualTo(100d));
            Assert.That(delta.CurrentValue, Is.EqualTo(500d));
            Assert.That(delta.AbsoluteDelta, Is.EqualTo(400d));
            Assert.That(delta.RelativeDeltaPercent, Is.EqualTo(400d));
            Assert.That(delta.Unit, Is.EqualTo("ms"));
        });
    }

    [Test]
    public void Cross_version_semantic_metric_drift_is_labeled_without_a_numeric_delta()
    {
        var baseline = Snapshot("accepted", Measurement(100d));
        var current = Snapshot("current", Measurement(500d) with { Denominator = "changed-denominator" });
        current.Cases[0].Compatibility.GameVersion = "1.7.0";

        var result = PerformanceBaselineComparer.Compare(current, [baseline], Policy(), true);
        var delta = result.Comparisons.Single().InformationalMetrics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Passed, Is.True);
            Assert.That(delta.Status, Is.EqualTo("semantic-drift"));
            Assert.That(delta.AbsoluteDelta, Is.Null);
            Assert.That(delta.RelativeDeltaPercent, Is.Null);
        });
    }

    [Test]
    public void Candidate_write_is_explicit_atomic_and_never_overwrites_any_existing_file()
    {
        var root = Path.Combine(Path.GetTempPath(), nameof(PerformanceBaselineTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "gateway.calibration.candidate.json");
            var candidate = Snapshot("current", Measurement(100d));

            PerformanceBaselineStore.WriteCandidate(path, candidate, new DateTimeOffset(2026, 8, 11, 20, 0, 0, TimeSpan.Zero));
            var persisted = JsonSerializer.Deserialize<PerformanceBaselineSnapshot>(File.ReadAllText(path), JsonOptions())!;

            Assert.Multiple(() =>
            {
                Assert.That(persisted.Status, Is.EqualTo("candidate"));
                Assert.That(persisted.CreatedUtc, Is.EqualTo("2026-08-11T20:00:00.0000000+00:00"));
                Assert.That(persisted.Cases.Single().Measurements.Single().Value, Is.EqualTo(100d));
            });
            Assert.That(() => PerformanceBaselineStore.WriteCandidate(path, candidate, DateTimeOffset.UtcNow),
                Throws.TypeOf<IOException>().With.Message.Contains("already exists"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void Candidate_creation_refuses_a_run_with_runtime_errors()
    {
        var root = Path.Combine(Path.GetTempPath(), nameof(PerformanceBaselineTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var current = Snapshot("current", Measurement(100d));
            current.RuntimeErrors.Add("new runtime error");

            Assert.That(
                () => PerformanceBaselineStore.WriteCandidate(
                    Path.Combine(root, "candidate.json"), current, DateTimeOffset.UtcNow),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("runtime errors"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void Every_declared_compatibility_identity_participates_in_exact_selection()
    {
        var baseline = Snapshot("accepted", Measurement(100d));
        var mutations = new Action<PerformanceCompatibilityIdentity>[]
        {
            value => value.GroupId = "other-group",
            value => value.WorkloadVersion = "other-workload",
            value => value.ActivePackageIds = ["brrainz.harmony", "ludeon.rimworld", "astryl.circinus", "optional.mod", "fumblesneeze.rimworlddevgateway"],
            value => value.GameVersion = "1.6.other",
            value => value.ProductAssemblyIdentity = "other-product",
            value => value.TestAssemblyIdentity = "other-tests",
            value => value.CircinusAssemblyIdentity = "other-circinus",
            value => value.CircinusSchemaIdentity = "other-schema",
            value => value.ProfilingPolicyIdentity = "other-profiler-policy",
            value => value.SamplingPolicyIdentity = "other-sampling-policy",
            value => value.HardwareRuntimeFingerprint = "other-hardware-runtime",
            value => value.DeterministicSeed = 7,
            value => value.WarmUpTicks = 301,
            value => value.SampleTicks = 3001,
            value => value.GameSpeed = 2,
            value => value.RepetitionCount = 3,
            value => value.AggregationPolicyIdentity = "median/v1"
        };

        foreach (var mutate in mutations)
        {
            var current = Snapshot("current", Measurement(100d));
            mutate(current.Cases[0].Compatibility);
            var result = PerformanceBaselineComparer.Compare(current, [baseline], Policy(), false);
            Assert.That(Codes(result), Does.Contain("workload-drift"));
        }
    }

    private static string[] Codes(PerformanceBaselineComparison result) =>
        result.Failures.Select(item => item.Code).ToArray();

    private static PerformanceThresholdPolicy Policy() => new()
    {
        Thresholds =
        [
            new PerformanceMetricThreshold
            {
                BenchmarkId = "gateway.calibration.instrumented",
                EvidenceLens = "instrumented-compatible",
                Scope = "method",
                Selector = "Example.Tick()",
                MetricName = "exclusive-time",
                Unit = "ms",
                RelativeIncreasePercent = 10d
            }
        ]
    };

    private static PerformanceBaselineSnapshot Snapshot(
        string status,
        params PerformanceMetricMeasurement[] measurements) => new()
    {
        SchemaVersion = 1,
        Status = status,
        CreatedUtc = "2026-08-11T19:00:00.0000000+00:00",
        RuntimeErrors = [],
        Cases =
        [
            new PerformanceBaselineCase
            {
                Compatibility = new PerformanceCompatibilityIdentity
                {
                    BenchmarkId = "gateway.calibration.instrumented",
                    GroupId = "gateway.calibration",
                    WorkloadVersion = "workload/v1",
                    EvidenceLens = "instrumented-compatible",
                    ActivePackageIds =
                    [
                        "brrainz.harmony", "ludeon.rimworld", "astryl.circinus",
                        "fumblesneeze.rimworlddevgateway"
                    ],
                    GameVersion = "1.6.4871",
                    ProductAssemblyIdentity = "product-sha",
                    TestAssemblyIdentity = "tests-sha",
                    CircinusAssemblyIdentity = "circinus-sha",
                    CircinusSchemaIdentity = "1.15",
                    ProfilingPolicyIdentity = "hand-armed/v1",
                    SamplingPolicyIdentity = "native-adaptive/v1",
                    HardwareRuntimeFingerprint = "hardware-runtime-sha",
                    DeterministicSeed = 60161,
                    WarmUpTicks = 300,
                    SampleTicks = 3000,
                    GameSpeed = 3,
                    RepetitionCount = 1,
                    AggregationPolicyIdentity = "arithmetic-mean/v1"
                },
                Measurements = measurements.ToList()
            }
        ]
    };

    private static PerformanceMetricMeasurement Measurement(double value) => new()
    {
        Scope = "method",
        Selector = "Example.Tick()",
        MetricName = "exclusive-time",
        Value = value,
        Unit = "ms",
        Denominator = "recorded-profiler-cycle",
        Claim = "gross-attribution",
        Calls = 1200,
        TimedCalls = 300,
        DutyPercent = 25d,
        SampleShift = 2,
        RecordedCycles = 100,
        ProfilerWindowMilliseconds = 4000d,
        ProfilerWindowTicks = 3000,
        SamplingContextIdentity = "cycles=100|windowMs=4000|windowTicks=3000|duty=25|shift=2|calls=1200|timed=300",
        ExactMethod = "Example:Example.Tick()"
    };

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
