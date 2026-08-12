using System.Text;
using System.Text.Json;

namespace RimWorldDevGateway.EndToEndHost;

public sealed class PerformanceBaselineSnapshot
{
    public int SchemaVersion { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CreatedUtc { get; set; } = string.Empty;
    public List<string> RuntimeErrors { get; set; } = [];
    public List<PerformanceBaselineCase> Cases { get; set; } = [];
}

public sealed class PerformanceBaselineCase
{
    public PerformanceCompatibilityIdentity Compatibility { get; set; } = new();
    public List<PerformanceMetricMeasurement> Measurements { get; set; } = [];
}

public sealed class PerformanceCompatibilityIdentity
{
    public string BenchmarkId { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string WorkloadVersion { get; set; } = string.Empty;
    public string EvidenceLens { get; set; } = string.Empty;
    public string[] ActivePackageIds { get; set; } = [];
    public string GameVersion { get; set; } = string.Empty;
    public string ProductAssemblyIdentity { get; set; } = string.Empty;
    public string TestAssemblyIdentity { get; set; } = string.Empty;
    public string CircinusAssemblyIdentity { get; set; } = string.Empty;
    public string CircinusSchemaIdentity { get; set; } = string.Empty;
    public string ProfilingPolicyIdentity { get; set; } = string.Empty;
    public string SamplingPolicyIdentity { get; set; } = string.Empty;
    public string HardwareRuntimeFingerprint { get; set; } = string.Empty;
    public string FixtureManifestSha256 { get; set; } = string.Empty;
    public int DeterministicSeed { get; set; }
    public int WarmUpTicks { get; set; }
    public int SampleTicks { get; set; }
    public int GameSpeed { get; set; }
    public int RepetitionCount { get; set; }
    public string AggregationPolicyIdentity { get; set; } = string.Empty;
}

public sealed record PerformanceMetricMeasurement
{
    public string Scope { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Denominator { get; set; } = string.Empty;
    public string Claim { get; set; } = string.Empty;
    public double? Calls { get; set; }
    public double? TimedCalls { get; set; }
    public double? DutyPercent { get; set; }
    public double? SampleShift { get; set; }
    public double? RecordedCycles { get; set; }
    public double? ProfilerWindowMilliseconds { get; set; }
    public double? ProfilerWindowTicks { get; set; }
    public string SamplingContextIdentity { get; set; } = string.Empty;
    public string ExactMethod { get; set; } = string.Empty;
}

public sealed class PerformanceThresholdPolicy
{
    public int SchemaVersion { get; set; } = 1;
    public List<PerformanceMetricThreshold> Thresholds { get; set; } = [];
}

public sealed class PerformanceMetricThreshold
{
    public string BenchmarkId { get; set; } = string.Empty;
    public string EvidenceLens { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double? AbsoluteIncrease { get; set; }
    public double? RelativeIncreasePercent { get; set; }
}

public sealed class PerformanceBaselineComparison
{
    public bool Passed { get; set; }
    public List<PerformanceCaseComparison> Comparisons { get; set; } = [];
    public List<PerformanceComparisonFailure> Failures { get; set; } = [];
}

public sealed class PerformanceCaseComparison
{
    public string BenchmarkId { get; set; } = string.Empty;
    public string EvidenceLens { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool ThresholdsApplied { get; set; }
    public List<string> IdentityDifferences { get; set; } = [];
    public List<PerformanceMetricDelta> InformationalMetrics { get; set; } = [];
    public List<PerformanceThresholdEvaluation> ThresholdEvaluations { get; set; } = [];
}

public sealed class PerformanceThresholdEvaluation
{
    public string Scope { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public bool Exceeded { get; set; }
    public double BaselineValue { get; set; }
    public double CurrentValue { get; set; }
    public double AbsoluteDelta { get; set; }
    public double? RelativeDeltaPercent { get; set; }
    public double? AbsoluteThreshold { get; set; }
    public double? RelativeThresholdPercent { get; set; }
    public double? BaselineCalls { get; set; }
    public double? CurrentCalls { get; set; }
    public double? BaselineTimedCalls { get; set; }
    public double? CurrentTimedCalls { get; set; }
    public double? BaselineDutyPercent { get; set; }
    public double? CurrentDutyPercent { get; set; }
    public double? BaselineSampleShift { get; set; }
    public double? CurrentSampleShift { get; set; }
    public double? BaselineRecordedCycles { get; set; }
    public double? CurrentRecordedCycles { get; set; }
    public double? BaselineProfilerWindowMilliseconds { get; set; }
    public double? CurrentProfilerWindowMilliseconds { get; set; }
    public double? BaselineProfilerWindowTicks { get; set; }
    public double? CurrentProfilerWindowTicks { get; set; }
    public string ExactMethod { get; set; } = string.Empty;
}

public sealed class PerformanceMetricDelta
{
    public string Scope { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double? BaselineValue { get; set; }
    public double? CurrentValue { get; set; }
    public double? AbsoluteDelta { get; set; }
    public double? RelativeDeltaPercent { get; set; }
}

public sealed class PerformanceComparisonFailure
{
    public string Code { get; set; } = string.Empty;
    public string BenchmarkId { get; set; } = string.Empty;
    public string EvidenceLens { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public double? BaselineValue { get; set; }
    public double? CurrentValue { get; set; }
    public double? AbsoluteDelta { get; set; }
    public double? RelativeDeltaPercent { get; set; }
    public double? AbsoluteThreshold { get; set; }
    public double? RelativeThresholdPercent { get; set; }
    public double? BaselineCalls { get; set; }
    public double? CurrentCalls { get; set; }
    public double? BaselineTimedCalls { get; set; }
    public double? CurrentTimedCalls { get; set; }
    public double? BaselineDutyPercent { get; set; }
    public double? CurrentDutyPercent { get; set; }
    public double? BaselineSampleShift { get; set; }
    public double? CurrentSampleShift { get; set; }
    public double? BaselineRecordedCycles { get; set; }
    public double? CurrentRecordedCycles { get; set; }
    public double? BaselineProfilerWindowMilliseconds { get; set; }
    public double? CurrentProfilerWindowMilliseconds { get; set; }
    public double? BaselineProfilerWindowTicks { get; set; }
    public double? CurrentProfilerWindowTicks { get; set; }
    public string ExactMethod { get; set; } = string.Empty;
}

public static class PerformanceBaselineComparer
{
    public static PerformanceBaselineComparison Compare(
        PerformanceBaselineSnapshot current,
        IEnumerable<PerformanceBaselineSnapshot> acceptedBaselines,
        PerformanceThresholdPolicy policy,
        bool informationalCrossVersion)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(acceptedBaselines);
        ArgumentNullException.ThrowIfNull(policy);
        ValidateSnapshot(current, "current");
        if (!string.Equals(current.Status, "current", StringComparison.Ordinal))
            throw new ArgumentException("The current performance snapshot must have status 'current'.");
        ValidatePolicy(policy);

        var baselines = acceptedBaselines.ToArray();
        foreach (var baseline in baselines)
        {
            ValidateSnapshot(baseline, "accepted baseline");
            if (!string.Equals(baseline.Status, "accepted", StringComparison.Ordinal))
                throw new ArgumentException("Only snapshots with status 'accepted' may be used as baselines.");
            if (baseline.RuntimeErrors.Count > 0)
                throw new ArgumentException("An accepted baseline may not contain runtime errors.");
            if (baseline.Cases.Any(item => item.Measurements.Count == 0))
                throw new ArgumentException("An accepted baseline may not contain a case without measurements.");
        }

        var result = new PerformanceBaselineComparison();
        foreach (var error in current.RuntimeErrors)
        {
            result.Failures.Add(new PerformanceComparisonFailure
            {
                Code = "runtime-error",
                Message = error
            });
        }

        foreach (var currentCase in current.Cases)
            CompareCase(currentCase, baselines.SelectMany(item => item.Cases).ToArray(), policy, informationalCrossVersion, result);

        result.Passed = result.Failures.Count == 0;
        return result;
    }

    private static void CompareCase(
        PerformanceBaselineCase current,
        IReadOnlyList<PerformanceBaselineCase> baselines,
        PerformanceThresholdPolicy policy,
        bool informationalCrossVersion,
        PerformanceBaselineComparison result)
    {
        var identity = current.Compatibility;
        var primary = baselines.Where(item =>
            string.Equals(item.Compatibility.BenchmarkId, identity.BenchmarkId, StringComparison.Ordinal) &&
            string.Equals(item.Compatibility.EvidenceLens, identity.EvidenceLens, StringComparison.Ordinal)).ToArray();
        if (primary.Length == 0)
        {
            AddIdentityFailure(result, identity, "baseline-missing", "No accepted baseline exists for this benchmark and evidence lens.");
            result.Comparisons.Add(CaseResult(identity, "baseline-missing", false));
            return;
        }

        var exact = primary.Where(item => IdentityDifferences(item.Compatibility, identity).Count == 0).ToArray();
        if (exact.Length > 1)
        {
            AddIdentityFailure(result, identity, "ambiguous-baseline", "More than one exact compatible accepted baseline exists.");
            result.Comparisons.Add(CaseResult(identity, "ambiguous-baseline", false));
            return;
        }

        if (exact.Length == 0)
        {
            var best = primary
                .Select(item => new { Case = item, Differences = IdentityDifferences(item.Compatibility, identity) })
                .OrderBy(item => item.Differences.Count)
                .ThenBy(item => string.Join("\n", item.Differences), StringComparer.Ordinal)
                .First();
            if (informationalCrossVersion && best.Differences.All(IsCrossVersionDifference))
            {
                result.Comparisons.Add(CaseResult(
                    identity,
                    "informational-cross-version",
                    false,
                    best.Differences,
                    InformationalDeltas(best.Case, current)));
                return;
            }

            AddIdentityFailure(
                result,
                identity,
                "workload-drift",
                "No compatible accepted baseline exists. Differences: " + string.Join(", ", best.Differences));
            result.Comparisons.Add(CaseResult(identity, "workload-drift", false, best.Differences));
            return;
        }

        var evaluations = CompareMetrics(exact[0], current, policy, result);
        if (evaluations.Count == 0)
        {
            AddIdentityFailure(
                result,
                identity,
                "threshold-policy-missing",
                "No reviewed threshold could be applied to this compatible performance case.");
        }
        result.Comparisons.Add(CaseResult(
            identity,
            "compared",
            evaluations.Count > 0,
            thresholdEvaluations: evaluations));
    }

    private static List<PerformanceThresholdEvaluation> CompareMetrics(
        PerformanceBaselineCase baseline,
        PerformanceBaselineCase current,
        PerformanceThresholdPolicy policy,
        PerformanceBaselineComparison result)
    {
        var identity = current.Compatibility;
        var baselineMetrics = UniqueMetrics(baseline.Measurements, "accepted baseline");
        var currentMetrics = UniqueMetrics(current.Measurements, "current snapshot");
        var invalid = new HashSet<string>(StringComparer.Ordinal);

        foreach (var baselinePair in baselineMetrics)
        {
            if (!currentMetrics.TryGetValue(baselinePair.Key, out var currentMetric))
            {
                AddMetricFailure(result, identity, baselinePair.Value, "missing-metric", "The current run omitted a baseline metric.");
                invalid.Add(baselinePair.Key);
                continue;
            }

            if (!string.Equals(baselinePair.Value.Unit, currentMetric.Unit, StringComparison.Ordinal))
            {
                AddMetricFailure(
                    result,
                    identity,
                    currentMetric,
                    "metric-unit-drift",
                    $"Metric unit changed from '{baselinePair.Value.Unit}' to '{currentMetric.Unit}'.");
                invalid.Add(baselinePair.Key);
                continue;
            }

            var context = SamplingSemanticDifferences(baselinePair.Value, currentMetric);
            if (context.Count > 0)
            {
                AddMetricFailure(
                    result,
                    identity,
                    currentMetric,
                    "sampling-context-drift",
                    "Metric denominator or claim changed: " + string.Join(", ", context),
                    baselinePair.Value);
                invalid.Add(baselinePair.Key);
            }
        }

        foreach (var currentOnly in currentMetrics.Where(item => !baselineMetrics.ContainsKey(item.Key)))
        {
            AddMetricFailure(
                result,
                identity,
                currentOnly.Value,
                "metric-set-drift",
                "The current run introduced a metric absent from the accepted baseline.");
            invalid.Add(currentOnly.Key);
        }

        var evaluations = new List<PerformanceThresholdEvaluation>();
        var thresholds = policy.Thresholds.Where(item =>
            string.Equals(item.BenchmarkId, identity.BenchmarkId, StringComparison.Ordinal) &&
            string.Equals(item.EvidenceLens, identity.EvidenceLens, StringComparison.Ordinal)).ToArray();
        foreach (var threshold in thresholds)
        {
            var key = MetricKey(threshold.Scope, threshold.Selector, threshold.MetricName);
            baselineMetrics.TryGetValue(key, out var baselineMetric);
            currentMetrics.TryGetValue(key, out var currentMetric);
            if (baselineMetric is null || currentMetric is null)
            {
                AddMetricFailure(
                    result,
                    identity,
                    currentMetric ?? baselineMetric ?? new PerformanceMetricMeasurement
                    {
                        Scope = threshold.Scope,
                        Selector = threshold.Selector,
                        MetricName = threshold.MetricName,
                        Unit = threshold.Unit
                    },
                    "missing-metric",
                    "A tracked threshold metric is absent from the baseline or current run.");
                continue;
            }

            if (invalid.Contains(key)) continue;

            if (!string.Equals(threshold.Unit, baselineMetric.Unit, StringComparison.Ordinal) ||
                !string.Equals(threshold.Unit, currentMetric.Unit, StringComparison.Ordinal))
            {
                AddMetricFailure(result, identity, currentMetric, "metric-unit-drift", "Tracked threshold unit does not match the artifacts.");
                continue;
            }

            var baselineContextProblems = SamplingContextProblems(baselineMetric, baseline.Compatibility);
            var currentContextProblems = SamplingContextProblems(currentMetric, current.Compatibility);
            if (baselineContextProblems.Count > 0 || currentContextProblems.Count > 0)
            {
                AddMetricFailure(
                    result,
                    identity,
                    currentMetric,
                    "sampling-context-drift",
                    "A tracked metric lacks a valid bounded Circinus sampling context: " +
                    string.Join(", ", baselineContextProblems.Select(item => "baseline:" + item)
                        .Concat(currentContextProblems.Select(item => "current:" + item))),
                    baselineMetric);
                continue;
            }

            var absoluteDelta = currentMetric.Value - baselineMetric.Value;
            double? relativeDelta = baselineMetric.Value == 0d
                ? absoluteDelta == 0d ? 0d : null
                : absoluteDelta / Math.Abs(baselineMetric.Value) * 100d;
            var exceeded = threshold.AbsoluteIncrease is not null && absoluteDelta > threshold.AbsoluteIncrease.Value ||
                           threshold.RelativeIncreasePercent is not null &&
                           (baselineMetric.Value == 0d ? absoluteDelta > 0d : relativeDelta!.Value > threshold.RelativeIncreasePercent.Value);
            var evaluation = CreateThresholdEvaluation(
                threshold,
                baselineMetric,
                currentMetric,
                absoluteDelta,
                relativeDelta,
                exceeded);
            evaluations.Add(evaluation);
            if (!exceeded) continue;

            result.Failures.Add(new PerformanceComparisonFailure
            {
                Code = "threshold-exceeded",
                BenchmarkId = identity.BenchmarkId,
                EvidenceLens = identity.EvidenceLens,
                Selector = currentMetric.Selector,
                MetricName = currentMetric.MetricName,
                Message = "The compatible current metric exceeded its tracked regression threshold.",
                BaselineValue = baselineMetric.Value,
                CurrentValue = currentMetric.Value,
                AbsoluteDelta = absoluteDelta,
                RelativeDeltaPercent = relativeDelta,
                AbsoluteThreshold = threshold.AbsoluteIncrease,
                RelativeThresholdPercent = threshold.RelativeIncreasePercent,
                BaselineCalls = baselineMetric.Calls,
                CurrentCalls = currentMetric.Calls,
                BaselineTimedCalls = baselineMetric.TimedCalls,
                CurrentTimedCalls = currentMetric.TimedCalls,
                BaselineDutyPercent = baselineMetric.DutyPercent,
                CurrentDutyPercent = currentMetric.DutyPercent,
                BaselineSampleShift = baselineMetric.SampleShift,
                CurrentSampleShift = currentMetric.SampleShift,
                BaselineRecordedCycles = baselineMetric.RecordedCycles,
                CurrentRecordedCycles = currentMetric.RecordedCycles,
                BaselineProfilerWindowMilliseconds = baselineMetric.ProfilerWindowMilliseconds,
                CurrentProfilerWindowMilliseconds = currentMetric.ProfilerWindowMilliseconds,
                BaselineProfilerWindowTicks = baselineMetric.ProfilerWindowTicks,
                CurrentProfilerWindowTicks = currentMetric.ProfilerWindowTicks,
                ExactMethod = currentMetric.ExactMethod
            });
        }

        return evaluations;
    }

    private static PerformanceThresholdEvaluation CreateThresholdEvaluation(
        PerformanceMetricThreshold threshold,
        PerformanceMetricMeasurement baseline,
        PerformanceMetricMeasurement current,
        double absoluteDelta,
        double? relativeDelta,
        bool exceeded) => new()
    {
        Scope = current.Scope,
        Selector = current.Selector,
        MetricName = current.MetricName,
        Unit = current.Unit,
        Exceeded = exceeded,
        BaselineValue = baseline.Value,
        CurrentValue = current.Value,
        AbsoluteDelta = absoluteDelta,
        RelativeDeltaPercent = relativeDelta,
        AbsoluteThreshold = threshold.AbsoluteIncrease,
        RelativeThresholdPercent = threshold.RelativeIncreasePercent,
        BaselineCalls = baseline.Calls,
        CurrentCalls = current.Calls,
        BaselineTimedCalls = baseline.TimedCalls,
        CurrentTimedCalls = current.TimedCalls,
        BaselineDutyPercent = baseline.DutyPercent,
        CurrentDutyPercent = current.DutyPercent,
        BaselineSampleShift = baseline.SampleShift,
        CurrentSampleShift = current.SampleShift,
        BaselineRecordedCycles = baseline.RecordedCycles,
        CurrentRecordedCycles = current.RecordedCycles,
        BaselineProfilerWindowMilliseconds = baseline.ProfilerWindowMilliseconds,
        CurrentProfilerWindowMilliseconds = current.ProfilerWindowMilliseconds,
        BaselineProfilerWindowTicks = baseline.ProfilerWindowTicks,
        CurrentProfilerWindowTicks = current.ProfilerWindowTicks,
        ExactMethod = current.ExactMethod
    };

    private static Dictionary<string, PerformanceMetricMeasurement> UniqueMetrics(
        IEnumerable<PerformanceMetricMeasurement> metrics,
        string owner)
    {
        var result = new Dictionary<string, PerformanceMetricMeasurement>(StringComparer.Ordinal);
        foreach (var metric in metrics)
        {
            var key = MetricKey(metric.Scope, metric.Selector, metric.MetricName);
            if (!result.TryAdd(key, metric))
                throw new ArgumentException($"The {owner} contains duplicate metric '{key}'.");
        }

        return result;
    }

    private static List<PerformanceMetricDelta> InformationalDeltas(
        PerformanceBaselineCase baseline,
        PerformanceBaselineCase current)
    {
        var baselineMetrics = UniqueMetrics(baseline.Measurements, "accepted baseline");
        var currentMetrics = UniqueMetrics(current.Measurements, "current snapshot");
        var keys = baselineMetrics.Keys.Concat(currentMetrics.Keys).Distinct(StringComparer.Ordinal).OrderBy(item => item, StringComparer.Ordinal);
        var result = new List<PerformanceMetricDelta>();
        foreach (var key in keys)
        {
            baselineMetrics.TryGetValue(key, out var baselineMetric);
            currentMetrics.TryGetValue(key, out var currentMetric);
            var exemplar = currentMetric ?? baselineMetric!;
            var sameSemantic = baselineMetric is not null && currentMetric is not null &&
                               string.Equals(baselineMetric.Unit, currentMetric.Unit, StringComparison.Ordinal) &&
                               string.Equals(baselineMetric.Denominator, currentMetric.Denominator, StringComparison.Ordinal) &&
                               string.Equals(baselineMetric.Claim, currentMetric.Claim, StringComparison.Ordinal);
            double? absolute = sameSemantic ? currentMetric!.Value - baselineMetric!.Value : null;
            double? relative = sameSemantic
                ? baselineMetric!.Value == 0d
                    ? absolute == 0d ? 0d : null
                    : absolute / Math.Abs(baselineMetric.Value) * 100d
                : null;
            result.Add(new PerformanceMetricDelta
            {
                Scope = exemplar.Scope,
                Selector = exemplar.Selector,
                MetricName = exemplar.MetricName,
                Unit = sameSemantic ? exemplar.Unit : $"{baselineMetric?.Unit ?? "missing"}->{currentMetric?.Unit ?? "missing"}",
                Status = baselineMetric is null ? "current-only" : currentMetric is null ? "baseline-only" :
                    sameSemantic ? "informational" : "semantic-drift",
                BaselineValue = baselineMetric?.Value,
                CurrentValue = currentMetric?.Value,
                AbsoluteDelta = absolute,
                RelativeDeltaPercent = relative
            });
        }
        return result;
    }

    private static List<string> SamplingSemanticDifferences(
        PerformanceMetricMeasurement baseline,
        PerformanceMetricMeasurement current)
    {
        var result = new List<string>();
        Difference(result, "denominator", baseline.Denominator, current.Denominator);
        Difference(result, "claim", baseline.Claim, current.Claim);
        return result;
    }

    private static List<string> SamplingContextProblems(
        PerformanceMetricMeasurement metric,
        PerformanceCompatibilityIdentity identity)
    {
        var result = new List<string>();
        var hasContext = metric.Calls is not null || metric.TimedCalls is not null || metric.DutyPercent is not null ||
                         metric.SampleShift is not null || metric.RecordedCycles is not null ||
                         metric.ProfilerWindowMilliseconds is not null || metric.ProfilerWindowTicks is not null;
        if (string.Equals(metric.Scope, "checkpoint", StringComparison.Ordinal))
        {
            if (hasContext) result.Add("checkpoint-context-unexpected");
            return result;
        }
        if (string.Equals(metric.Scope, "mod", StringComparison.Ordinal))
        {
            if (metric.Calls is not null || metric.TimedCalls is not null || metric.SampleShift is not null)
                result.Add("mod-method-context-unexpected");
            if (metric.DutyPercent is null || metric.RecordedCycles is null ||
                metric.ProfilerWindowMilliseconds is null || metric.ProfilerWindowTicks is null)
            {
                result.Add("mod-window-context-missing");
                return result;
            }
            ValidateWindowContext(metric, identity, result);
            return result;
        }
        if (!string.Equals(metric.Scope, "method", StringComparison.Ordinal) &&
            !string.Equals(metric.Scope, "patch", StringComparison.Ordinal))
        {
            result.Add("unsupported-threshold-scope");
            return result;
        }
        if (!hasContext || metric.Calls is null || metric.TimedCalls is null || metric.DutyPercent is null ||
            metric.SampleShift is null || metric.RecordedCycles is null ||
            metric.ProfilerWindowMilliseconds is null || metric.ProfilerWindowTicks is null)
        {
            result.Add("profiled-context-missing");
            return result;
        }
        if (metric.Calls <= 0d) result.Add("calls-not-positive");
        if (metric.TimedCalls <= 0d || metric.TimedCalls > metric.Calls) result.Add("timed-calls-invalid");
        if (metric.SampleShift < 0d || metric.SampleShift > 8d) result.Add("sample-shift-outside-native-bounds");
        ValidateWindowContext(metric, identity, result);
        return result;
    }

    private static void ValidateWindowContext(
        PerformanceMetricMeasurement metric,
        PerformanceCompatibilityIdentity identity,
        List<string> result)
    {
        if (metric.DutyPercent <= 0d || metric.DutyPercent > 100d) result.Add("duty-invalid");
        if (metric.RecordedCycles <= 0d) result.Add("recorded-cycles-not-positive");
        if (metric.ProfilerWindowMilliseconds <= 0d) result.Add("profiler-window-not-positive");
        if (metric.ProfilerWindowTicks != identity.SampleTicks) result.Add("profiler-window-ticks-not-sample-window");
    }

    private static void AddIdentityFailure(
        PerformanceBaselineComparison result,
        PerformanceCompatibilityIdentity identity,
        string code,
        string message) => result.Failures.Add(new PerformanceComparisonFailure
    {
        Code = code,
        BenchmarkId = identity.BenchmarkId,
        EvidenceLens = identity.EvidenceLens,
        Message = message
    });

    private static void AddMetricFailure(
        PerformanceBaselineComparison result,
        PerformanceCompatibilityIdentity identity,
        PerformanceMetricMeasurement metric,
        string code,
        string message,
        PerformanceMetricMeasurement? baselineMetric = null) => result.Failures.Add(new PerformanceComparisonFailure
    {
        Code = code,
        BenchmarkId = identity.BenchmarkId,
        EvidenceLens = identity.EvidenceLens,
        Selector = metric.Selector,
        MetricName = metric.MetricName,
        Message = message,
        BaselineValue = baselineMetric?.Value,
        CurrentValue = metric.Value,
        BaselineCalls = baselineMetric?.Calls,
        CurrentCalls = metric.Calls,
        BaselineTimedCalls = baselineMetric?.TimedCalls,
        CurrentTimedCalls = metric.TimedCalls,
        BaselineDutyPercent = baselineMetric?.DutyPercent,
        CurrentDutyPercent = metric.DutyPercent,
        BaselineSampleShift = baselineMetric?.SampleShift,
        CurrentSampleShift = metric.SampleShift,
        BaselineRecordedCycles = baselineMetric?.RecordedCycles,
        CurrentRecordedCycles = metric.RecordedCycles,
        BaselineProfilerWindowMilliseconds = baselineMetric?.ProfilerWindowMilliseconds,
        CurrentProfilerWindowMilliseconds = metric.ProfilerWindowMilliseconds,
        BaselineProfilerWindowTicks = baselineMetric?.ProfilerWindowTicks,
        CurrentProfilerWindowTicks = metric.ProfilerWindowTicks,
        ExactMethod = metric.ExactMethod
    });

    private static PerformanceCaseComparison CaseResult(
        PerformanceCompatibilityIdentity identity,
        string status,
        bool thresholdsApplied,
        IEnumerable<string>? differences = null,
        IEnumerable<PerformanceMetricDelta>? informationalMetrics = null,
        IEnumerable<PerformanceThresholdEvaluation>? thresholdEvaluations = null) => new()
    {
        BenchmarkId = identity.BenchmarkId,
        EvidenceLens = identity.EvidenceLens,
        Status = status,
        ThresholdsApplied = thresholdsApplied,
        IdentityDifferences = differences?.ToList() ?? [],
        InformationalMetrics = informationalMetrics?.ToList() ?? [],
        ThresholdEvaluations = thresholdEvaluations?.ToList() ?? []
    };

    private static List<string> IdentityDifferences(
        PerformanceCompatibilityIdentity baseline,
        PerformanceCompatibilityIdentity current)
    {
        var result = new List<string>();
        Difference(result, "groupId", baseline.GroupId, current.GroupId);
        Difference(result, "workloadVersion", baseline.WorkloadVersion, current.WorkloadVersion);
        if (!baseline.ActivePackageIds.SequenceEqual(current.ActivePackageIds, StringComparer.Ordinal)) result.Add("activePackageIds");
        Difference(result, "gameVersion", baseline.GameVersion, current.GameVersion);
        Difference(result, "productAssemblyIdentity", baseline.ProductAssemblyIdentity, current.ProductAssemblyIdentity);
        Difference(result, "testAssemblyIdentity", baseline.TestAssemblyIdentity, current.TestAssemblyIdentity);
        Difference(result, "circinusAssemblyIdentity", baseline.CircinusAssemblyIdentity, current.CircinusAssemblyIdentity);
        Difference(result, "circinusSchemaIdentity", baseline.CircinusSchemaIdentity, current.CircinusSchemaIdentity);
        Difference(result, "profilingPolicyIdentity", baseline.ProfilingPolicyIdentity, current.ProfilingPolicyIdentity);
        Difference(result, "samplingPolicyIdentity", baseline.SamplingPolicyIdentity, current.SamplingPolicyIdentity);
        Difference(result, "hardwareRuntimeFingerprint", baseline.HardwareRuntimeFingerprint, current.HardwareRuntimeFingerprint);
        Difference(result, "fixtureManifestSha256", baseline.FixtureManifestSha256, current.FixtureManifestSha256);
        if (baseline.DeterministicSeed != current.DeterministicSeed) result.Add("deterministicSeed");
        if (baseline.WarmUpTicks != current.WarmUpTicks) result.Add("warmUpTicks");
        if (baseline.SampleTicks != current.SampleTicks) result.Add("sampleTicks");
        if (baseline.GameSpeed != current.GameSpeed) result.Add("gameSpeed");
        if (baseline.RepetitionCount != current.RepetitionCount) result.Add("repetitionCount");
        Difference(result, "aggregationPolicyIdentity", baseline.AggregationPolicyIdentity, current.AggregationPolicyIdentity);
        return result;
    }

    private static bool IsCrossVersionDifference(string value) => value is
        "gameVersion" or
        "productAssemblyIdentity" or
        "testAssemblyIdentity" or
        "circinusAssemblyIdentity" or
        "circinusSchemaIdentity" or
        "hardwareRuntimeFingerprint";

    private static void Difference(List<string> result, string name, string baseline, string current)
    {
        if (!string.Equals(baseline, current, StringComparison.Ordinal)) result.Add(name);
    }

    private static string MetricKey(string scope, string selector, string metricName) =>
        scope + "\n" + selector + "\n" + metricName;

    internal static void ValidateSnapshot(PerformanceBaselineSnapshot snapshot, string owner)
    {
        if (snapshot.SchemaVersion != 1) throw new ArgumentException($"The {owner} schemaVersion must be 1.");
        if (snapshot.Cases.Count == 0) throw new ArgumentException($"The {owner} contains no performance cases.");
        if (snapshot.RuntimeErrors.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException($"The {owner} contains an empty runtime error.");
        foreach (var item in snapshot.Cases)
        {
            var identity = item.Compatibility;
            foreach (var value in new[]
                     {
                         identity.BenchmarkId, identity.GroupId, identity.WorkloadVersion, identity.EvidenceLens,
                         identity.GameVersion, identity.ProductAssemblyIdentity, identity.TestAssemblyIdentity,
                         identity.CircinusAssemblyIdentity, identity.CircinusSchemaIdentity,
                         identity.ProfilingPolicyIdentity, identity.SamplingPolicyIdentity,
                         identity.HardwareRuntimeFingerprint, identity.FixtureManifestSha256,
                         identity.AggregationPolicyIdentity
                     })
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"The {owner} contains an empty compatibility identity.");
            if (identity.ActivePackageIds.Length == 0 || identity.ActivePackageIds.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException($"The {owner} contains an invalid active package order.");
            if (identity.RepetitionCount <= 0)
                throw new ArgumentException($"The {owner} contains an invalid repetition count.");
            foreach (var metric in item.Measurements)
            {
                if (string.IsNullOrWhiteSpace(metric.Selector) || string.IsNullOrWhiteSpace(metric.MetricName) ||
                    string.IsNullOrWhiteSpace(metric.Unit) || string.IsNullOrWhiteSpace(metric.Denominator) ||
                    string.IsNullOrWhiteSpace(metric.Claim) || !Finite(metric.Value) ||
                    metric.Calls is < 0 || metric.TimedCalls is < 0 ||
                    metric.Calls is not null && metric.TimedCalls > metric.Calls ||
                    metric.DutyPercent is < 0d or > 100d ||
                    !Finite(metric.Calls) || !Finite(metric.TimedCalls) || !Finite(metric.DutyPercent) ||
                    !Finite(metric.SampleShift) || !Finite(metric.RecordedCycles) ||
                    !Finite(metric.ProfilerWindowMilliseconds) || !Finite(metric.ProfilerWindowTicks))
                    throw new ArgumentException($"The {owner} contains an invalid measurement.");
            }
        }
    }

    private static void ValidatePolicy(PerformanceThresholdPolicy policy)
    {
        if (policy.SchemaVersion != 1) throw new ArgumentException("The performance threshold policy schemaVersion must be 1.");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var threshold in policy.Thresholds)
        {
            if (string.IsNullOrWhiteSpace(threshold.BenchmarkId) || string.IsNullOrWhiteSpace(threshold.EvidenceLens) ||
                string.IsNullOrWhiteSpace(threshold.Scope) ||
                string.IsNullOrWhiteSpace(threshold.Selector) || string.IsNullOrWhiteSpace(threshold.MetricName) ||
                string.IsNullOrWhiteSpace(threshold.Unit) ||
                threshold.AbsoluteIncrease is null && threshold.RelativeIncreasePercent is null ||
                threshold.AbsoluteIncrease is < 0d || threshold.RelativeIncreasePercent is < 0d)
                throw new ArgumentException("The performance threshold policy contains an invalid threshold.");
            var key = threshold.BenchmarkId + "\n" + threshold.EvidenceLens + "\n" +
                      threshold.Scope + "\n" + threshold.Selector + "\n" + threshold.MetricName;
            if (!keys.Add(key)) throw new ArgumentException($"The performance threshold policy contains duplicate threshold '{key}'.");
        }
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    private static bool Finite(double? value) => value is null || Finite(value.Value);
}

public static class PerformanceBaselineStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static void WriteCandidate(
        string path,
        PerformanceBaselineSnapshot snapshot,
        DateTimeOffset createdUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(snapshot);
        PerformanceBaselineComparer.ValidateSnapshot(snapshot, "current snapshot");
        if (!string.Equals(snapshot.Status, "current", StringComparison.Ordinal))
            throw new InvalidOperationException("Only a snapshot with status 'current' can become a candidate.");
        if (snapshot.RuntimeErrors.Count > 0)
            throw new InvalidOperationException("A performance run with runtime errors cannot become a baseline candidate.");
        if (snapshot.Cases.Any(item => item.Measurements.Count == 0))
            throw new InvalidOperationException("A performance case without measurements cannot become a baseline candidate.");
        if (path.EndsWith(".accepted.json", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A baseline candidate cannot be written with the reserved .accepted.json suffix.");

        var candidate = Clone(snapshot);
        candidate.Status = "candidate";
        candidate.CreatedUtc = createdUtc.ToUniversalTime().ToString("O");
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(candidate, JsonOptions) + Environment.NewLine);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        var temporary = Path.Combine(
            directory ?? Directory.GetCurrentDirectory(),
            "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            File.Move(temporary, path);
        }
        catch (IOException exception) when (File.Exists(path))
        {
            throw new IOException($"Baseline candidate already exists and was not overwritten: {path}", exception);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static PerformanceBaselineSnapshot Clone(PerformanceBaselineSnapshot snapshot)
    {
        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        return JsonSerializer.Deserialize<PerformanceBaselineSnapshot>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Could not clone the performance baseline candidate.");
    }
}
