using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using RimWorldDevGateway.PerformanceTesting;

namespace RimWorldDevGateway.Performance;

internal sealed class PerformanceNormalizationException : Exception
{
    public PerformanceNormalizationException(string message) : base(message) { }
}

internal sealed class PerformanceNormalizationContext
{
    public PerformanceNormalizationContext(
        string workloadVersion,
        PerformanceEvidenceLens evidenceLens,
        string controlMode,
        long elapsedGameTicks,
        double elapsedWallMilliseconds,
        long managedMemoryStartBytes,
        long managedMemoryEndBytes,
        IReadOnlyList<int> garbageCollectionsStart,
        IReadOnlyList<int> garbageCollectionsEnd,
        IReadOnlyDictionary<string, long> throughputCounts)
    {
        if (string.IsNullOrWhiteSpace(workloadVersion) ||
            workloadVersion.Length > PerformanceTestContract.MaximumIdentityCharacters)
            throw new ArgumentException("Workload version is required.", nameof(workloadVersion));
        if (!Enum.IsDefined(typeof(PerformanceEvidenceLens), evidenceLens))
            throw new ArgumentOutOfRangeException(nameof(evidenceLens));
        var expectedControlMode = evidenceLens switch
        {
            PerformanceEvidenceLens.ProductInstrumented => "instrumented",
            PerformanceEvidenceLens.ArmedDisabledWrapper => "armed-disabled-wrapper",
            PerformanceEvidenceLens.FullyDisarmed => "fully-disarmed",
            PerformanceEvidenceLens.ProductAbsentControl => "product-absent-control",
            _ => throw new ArgumentOutOfRangeException(nameof(evidenceLens))
        };
        if (!string.Equals(controlMode, expectedControlMode, StringComparison.Ordinal))
            throw new ArgumentException(
                $"Evidence lens {evidenceLens} requires exact control mode '{expectedControlMode}'.",
                nameof(controlMode));
        if (elapsedGameTicks < 0 || elapsedWallMilliseconds < 0 || double.IsNaN(elapsedWallMilliseconds) ||
            double.IsInfinity(elapsedWallMilliseconds) ||
            managedMemoryStartBytes < 0 || managedMemoryEndBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(elapsedGameTicks), "Control checkpoints cannot be negative.");
        if (garbageCollectionsStart is null || garbageCollectionsStart.Count != 3 ||
            garbageCollectionsEnd is null || garbageCollectionsEnd.Count != 3)
            throw new ArgumentException("Exactly three GC generation checkpoints are required.");
        if (throughputCounts is null) throw new ArgumentNullException(nameof(throughputCounts));
        if (throughputCounts.Count > PerformanceTestContract.MaximumThroughputCheckpoints)
            throw new ArgumentException(
                $"At most {PerformanceTestContract.MaximumThroughputCheckpoints} throughput counts are supported.",
                nameof(throughputCounts));
        for (var generation = 0; generation < 3; generation++)
        {
            if (garbageCollectionsStart[generation] < 0 ||
                garbageCollectionsEnd[generation] < garbageCollectionsStart[generation])
                throw new ArgumentException("GC checkpoints must be monotonic nonnegative counters.");
        }
        foreach (var checkpoint in throughputCounts)
        {
            if (string.IsNullOrWhiteSpace(checkpoint.Key) ||
                checkpoint.Key.Length > PerformanceTestContract.MaximumIdentityCharacters ||
                checkpoint.Value < 0)
                throw new ArgumentException("Throughput checkpoints require bounded identities and nonnegative counts.");
            if (checkpoint.Key.StartsWith("elapsed-", StringComparison.Ordinal) ||
                checkpoint.Key.StartsWith("managed-memory-", StringComparison.Ordinal) ||
                checkpoint.Key.StartsWith("gc-generation-", StringComparison.Ordinal))
                throw new ArgumentException("Throughput checkpoint collides with a reserved control identity.");
        }

        WorkloadVersion = workloadVersion;
        EvidenceLens = evidenceLens;
        ControlMode = controlMode;
        ElapsedGameTicks = elapsedGameTicks;
        ElapsedWallMilliseconds = elapsedWallMilliseconds;
        ManagedMemoryStartBytes = managedMemoryStartBytes;
        ManagedMemoryEndBytes = managedMemoryEndBytes;
        GarbageCollectionsStart = garbageCollectionsStart.ToArray();
        GarbageCollectionsEnd = garbageCollectionsEnd.ToArray();
        ThroughputCounts = throughputCounts.ToDictionary(
            item => item.Key,
            item => item.Value,
            StringComparer.Ordinal);
    }

    public string WorkloadVersion { get; }
    public PerformanceEvidenceLens EvidenceLens { get; }
    public string ControlMode { get; }
    public long ElapsedGameTicks { get; }
    public double ElapsedWallMilliseconds { get; }
    public long ManagedMemoryStartBytes { get; }
    public long ManagedMemoryEndBytes { get; }
    public IReadOnlyList<int> GarbageCollectionsStart { get; }
    public IReadOnlyList<int> GarbageCollectionsEnd { get; }
    public IReadOnlyDictionary<string, long> ThroughputCounts { get; }
}

internal sealed class PerformanceNormalizedSample
{
    public PerformanceNormalizedSample(
        string runId,
        string rawCircinusJson,
        string workloadVersion,
        PerformanceEvidenceLens evidenceLens,
        string controlMode,
        PerformanceProfilerPolicy profilerPolicy,
        IReadOnlyList<PerformanceNormalizedMetric> metrics,
        IReadOnlyList<PerformanceNativeSample> samples,
        IReadOnlyList<PerformanceControlCheckpoint> checkpoints,
        IReadOnlyList<CircinusProfilerSidecar> profilerSidecars)
    {
        RunId = runId;
        RawCircinusJson = rawCircinusJson;
        WorkloadVersion = workloadVersion;
        EvidenceLens = evidenceLens;
        ControlMode = controlMode;
        ProfilerPolicy = profilerPolicy;
        Metrics = metrics;
        Samples = samples;
        Checkpoints = checkpoints;
        ProfilerSidecars = profilerSidecars;
    }

    public string RunId { get; }
    public string RawCircinusJson { get; }
    public string WorkloadVersion { get; }
    public PerformanceEvidenceLens EvidenceLens { get; }
    public string ControlMode { get; }
    public PerformanceProfilerPolicy ProfilerPolicy { get; }
    public IReadOnlyList<PerformanceNormalizedMetric> Metrics { get; }
    public IReadOnlyList<PerformanceNativeSample> Samples { get; }
    public IReadOnlyList<PerformanceControlCheckpoint> Checkpoints { get; }
    public IReadOnlyList<CircinusProfilerSidecar> ProfilerSidecars { get; }
}

internal sealed class PerformanceProfilerPolicy
{
    public PerformanceProfilerPolicy(
        int recordedCycles,
        double windowMilliseconds,
        long windowTicks,
        double dutyPercent,
        bool sampled,
        int disarmedBelowFloor)
    {
        RecordedCycles = recordedCycles;
        WindowMilliseconds = windowMilliseconds;
        WindowTicks = windowTicks;
        DutyPercent = dutyPercent;
        Sampled = sampled;
        DisarmedBelowFloor = disarmedBelowFloor;
    }

    public int RecordedCycles { get; }
    public double WindowMilliseconds { get; }
    public long WindowTicks { get; }
    public double DutyPercent { get; }
    public bool Sampled { get; }
    public int DisarmedBelowFloor { get; }
}

internal sealed class PerformanceNormalizedMetric
{
    public PerformanceNormalizedMetric(
        string scope,
        string key,
        string name,
        double value,
        string unit,
        string denominator,
        string claim)
    {
        Scope = scope;
        Key = key;
        Name = name;
        Value = value;
        Unit = unit;
        Denominator = denominator;
        Claim = claim;
    }

    public string Scope { get; }
    public string Key { get; }
    public string Name { get; }
    public double Value { get; }
    public string Unit { get; }
    public string Denominator { get; }
    public string Claim { get; }
}

internal sealed class PerformanceNativeSample
{
    public int GameTick { get; set; }
    public double RealtimeSeconds { get; set; }
    public int Tps { get; set; }
    public int TargetTps { get; set; }
    public int Fps { get; set; }
    public double FrameMeanMilliseconds { get; set; }
    public double FrameMaximumMilliseconds { get; set; }
    public double FrameP95Milliseconds { get; set; }
    public double TickMeanMilliseconds { get; set; }
    public double TickMaximumMilliseconds { get; set; }
    public int HeapKilobytes { get; set; }
    public int PawnCount { get; set; }
}

internal sealed class PerformanceControlCheckpoint
{
    public PerformanceControlCheckpoint(string id, double value, string unit)
    {
        Id = id;
        Value = value;
        Unit = unit;
    }

    public string Id { get; }
    public double Value { get; }
    public string Unit { get; }
}

internal static class PerformanceSampleNormalizer
{
    public const int MaximumProfilerCycles = 2_000;
    public const int MaximumNativeSamples = 7_200;
    public static PerformanceNormalizedSample Normalize(
        CircinusCapture capture,
        PerformanceNormalizationContext context)
    {
        if (capture is null) throw new ArgumentNullException(nameof(capture));
        if (context is null) throw new ArgumentNullException(nameof(context));
        var document = Read(capture.InMemoryJson);
        Validate(document, capture, context);
        var env = document.Env!;
        var metrics = new List<PerformanceNormalizedMetric>();

        foreach (var row in (IEnumerable<NativeMethodRow>?)document.Methods ?? Enumerable.Empty<NativeMethodRow>())
        {
            var key = RequiredKey(row.Method?.Key, "method");
            AddTiming(metrics, "method", key, row.Total, row.Mean, row.Maximum, row.CallCount, env);
        }

        foreach (var row in (IEnumerable<NativePatchRow>?)document.Patches ?? Enumerable.Empty<NativePatchRow>())
        {
            var key = RequiredKey(row.Patch?.Key, "patch");
            AddTiming(metrics, "patch", key, row.Total, row.Mean, row.Maximum, row.CallCount, env);
            Add(metrics, "patch", key, "native-timed-calls", row.TimedCallCount, "calls",
                "native-adaptive-sampling", "sampling-policy");
            if (row.AmbiguousTargets == true)
            {
                Add(metrics, "patch", key, "ambiguous-targets", 1,
                    "boolean", "native-patch-identity", "sampling-policy");
                Add(metrics, "patch", key, "shared-gross-share", row.Total / env.WindowMilliseconds,
                    "ratio", "profiler-window-ms", "gross-attribution");
                Add(metrics, "patch", key, "ambiguous-target-gross-share",
                    row.Total / env.WindowMilliseconds,
                    "ratio", "profiler-window-ms", "gross-attribution");
            }
            if (row.Patch?.CanSkip == true)
                Add(metrics, "patch", key, "skip-capable-gross-share", row.Total / env.WindowMilliseconds,
                    "ratio", "profiler-window-ms", "gross-attribution");
        }

        foreach (var row in (IEnumerable<NativeModRow>?)document.ModCosts ?? Enumerable.Empty<NativeModRow>())
        {
            var key = RequiredKey(row.PackageId, "mod");
            Add(metrics, "mod", key, "total", row.Total, "ms", "profiler-window-ms", "gross-attribution");
            Add(metrics, "mod", key, "gross-profiler-window-share", row.Total / env.WindowMilliseconds,
                "ratio", "profiler-window-ms", "gross-attribution");
            Add(metrics, "mod", key, "shared-profiler-window-share", row.Shared / env.WindowMilliseconds,
                "ratio", "profiler-window-ms", "gross-attribution");
            Add(metrics, "mod", key, "replacement-profiler-window-share",
                row.Replacement / env.WindowMilliseconds,
                "ratio", "profiler-window-ms", "gross-attribution");
        }

        foreach (var sidecar in capture.Sidecars)
        {
            var scope = sidecar.RowKind == CircinusRowKind.Patch ? "patch" : "method";
            Add(metrics, scope, sidecar.RowKey, "live-total-calls", sidecar.TotalCalls, "calls",
                "native-adaptive-sampling", "sampling-policy");
            Add(metrics, scope, sidecar.RowKey, "live-total-timed-calls", sidecar.TotalTimedCalls, "calls",
                "native-adaptive-sampling", "sampling-policy");
            Add(metrics, scope, sidecar.RowKey, "sample-shift", sidecar.SampleShift, "shift",
                "native-adaptive-sampling", "sampling-policy");
            Add(metrics, scope, sidecar.RowKey, "cycles-seen", sidecar.CyclesSeen, "cycles",
                "recorded-profiler-cycle", "sampling-policy");
            Add(metrics, scope, sidecar.RowKey, "hand-armed", sidecar.HandArmed ? 1 : 0, "boolean",
                "run-owned-profiler", "sampling-policy");
        }

        var samples = ((IEnumerable<NativeSample>?)document.Samples ?? Enumerable.Empty<NativeSample>())
            .Select(item => new PerformanceNativeSample
            {
                GameTick = item.GameTick,
                RealtimeSeconds = item.RealtimeSeconds,
                Tps = item.AchievedTps,
                TargetTps = item.RequestedTps,
                Fps = item.AchievedFps,
                FrameMeanMilliseconds = item.FrameMeanMilliseconds,
                FrameMaximumMilliseconds = item.FrameMaximumMilliseconds,
                FrameP95Milliseconds = item.FrameP95Milliseconds,
                TickMeanMilliseconds = item.TickMeanMilliseconds,
                TickMaximumMilliseconds = item.TickMaximumMilliseconds,
                HeapKilobytes = item.ManagedHeapKilobytes,
                PawnCount = item.ActivePawnCount
            })
            .ToArray();
        var checkpoints = Checkpoints(context);
        return new PerformanceNormalizedSample(
            capture.RunId,
            capture.InMemoryJson,
            context.WorkloadVersion,
            context.EvidenceLens,
            context.ControlMode,
            new PerformanceProfilerPolicy(
                env.RecordedCycles,
                env.WindowMilliseconds,
                env.WindowTicks,
                env.DutyPercent,
                env.WasSampled,
                env.DisarmedBelowFloor),
            metrics.OrderBy(item => item.Scope, StringComparer.Ordinal)
                .ThenBy(item => item.Key, StringComparer.Ordinal)
                .ThenBy(item => item.Name, StringComparer.Ordinal)
                .ToArray(),
            samples,
            checkpoints,
            capture.Sidecars.ToArray());
    }

    private static void Validate(
        NativeDocument document,
        CircinusCapture capture,
        PerformanceNormalizationContext context)
    {
        if (document.SchemaMajor is null || document.SchemaMinor is null ||
            document.Incomplete is null || document.ErrorsDropped is null ||
            document.PatchesDropped is null || document.PatchesDroppedMs is null)
            throw new PerformanceNormalizationException(
                "Circinus native JSON is missing required run metadata.");
        if (!string.Equals(document.Id, capture.RunId, StringComparison.Ordinal) ||
            document.SchemaMajor != capture.SchemaMajor || document.SchemaMinor != capture.SchemaMinor)
            throw new PerformanceNormalizationException("Native Circinus identity/schema drifted before normalization.");
        if (document.Incomplete.Value)
            throw new PerformanceNormalizationException("Circinus marked the native run incomplete.");
        RequireNonnegativeFinite(document.PatchesDroppedMs.Value, "patchesDroppedMs");
        if (document.PatchesDropped.Value < 0 || document.PatchesDroppedMs.Value > 0)
            throw new PerformanceNormalizationException("Circinus has invalid dropped-patch counters.");
        if (document.PatchesDropped.Value > 0)
            throw new PerformanceNormalizationException("Circinus dropped required patch details.");
        if (document.ErrorsDropped.Value < 0)
            throw new PerformanceNormalizationException("Circinus errorsDropped must be nonnegative.");
        if (document.ErrorsDropped.Value > 0)
            throw new PerformanceNormalizationException("Circinus dropped native errors.");
        if ((document.Methods?.Count ?? 0) > CircinusRuntimeRun.MaximumMethodRows ||
            (document.Patches?.Count ?? 0) > CircinusRuntimeRun.MaximumPatchRows)
            throw new PerformanceNormalizationException("Circinus native method/patch detail exceeds its output cap.");
        if (document.Samples is null || document.Samples.Count == 0)
            throw new PerformanceNormalizationException("Circinus native JSON has no required samples.");
        if (document.Samples.Count > MaximumNativeSamples)
            throw new PerformanceNormalizationException("Circinus native sample count exceeds its run ceiling.");
        if (document.Env is null)
            throw new PerformanceNormalizationException("Circinus has no valid profiler denominator.");
        ValidateEnvironment(document.Env);
        ValidateMethodRows(document.Methods, document.Env);
        ValidatePatchRows(document.Patches);
        ValidateModRows(document.ModCosts);
        ValidateSamples(document.Samples);
        ValidateSidecars(capture.Sidecars, document.Env, context.EvidenceLens, document);
    }

    private static void ValidateEnvironment(NativeEnvironment env)
    {
        if (env.ProfilerCycles is null || env.ProfilerWindowMs is null ||
            env.ProfilerWindowTicks is null || env.ProfilerDutyPct is null ||
            env.ProfilerSampled is null || env.ProfilerDisarmedBelowFloor is null)
            throw new PerformanceNormalizationException(
                "Circinus native JSON is missing required profiler environment fields.");
        if (env.ProfilerWindowMs.Value <= 0 || env.ProfilerCycles.Value <= 0 ||
            env.ProfilerWindowTicks.Value <= 0 || !IsFinite(env.ProfilerWindowMs.Value))
            throw new PerformanceNormalizationException("Circinus has no valid profiler denominator.");
        if (env.ProfilerCycles.Value > MaximumProfilerCycles)
            throw new PerformanceNormalizationException("Circinus profiler cycles overflowed its retained frame ring.");
        if (!IsFinite(env.ProfilerDutyPct.Value) || env.ProfilerDutyPct.Value < 0 ||
            env.ProfilerDutyPct.Value > 100)
            throw new PerformanceNormalizationException("Circinus profiler duty percentage is outside 0..100.");
        if (env.ProfilerDisarmedBelowFloor.Value < 0)
            throw new PerformanceNormalizationException("Circinus disarmed-profiler count must be nonnegative.");
        if (!env.ProfilerSampled.Value)
            throw new PerformanceNormalizationException("Circinus did not mark the profiler window sampled.");
    }

    private static void ValidateMethodRows(
        IEnumerable<NativeMethodRow>? rows,
        NativeEnvironment env)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows ?? Enumerable.Empty<NativeMethodRow>())
        {
            var key = RequiredKey(row.Method?.Key, "method");
            if (!keys.Add(key))
                throw new PerformanceNormalizationException("Circinus emitted a duplicate method row.");
            if (row.TotalMs is null || row.MeanMs is null || row.MaxMs is null ||
                row.Calls is null || row.Samples is null)
                throw new PerformanceNormalizationException(
                    $"Circinus method '{key}' is missing required measurements.");
            ValidateTiming(key, "method", row.Total, row.Mean, row.Maximum, row.CallCount);
            if (row.Samples.Value < 0 || row.Samples.Value > env.RecordedCycles)
                throw new PerformanceNormalizationException(
                    $"Circinus method '{key}' has an invalid sample count.");
        }
    }

    private static void ValidatePatchRows(IEnumerable<NativePatchRow>? rows)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows ?? Enumerable.Empty<NativePatchRow>())
        {
            var key = RequiredKey(row.Patch?.Key, "patch");
            if (!keys.Add(key))
                throw new PerformanceNormalizationException("Circinus emitted a duplicate patch row.");
            if (row.TotalMs is null || row.MeanMs is null || row.MaximumMs is null ||
                row.Calls is null || row.TimedCalls is null || row.AmbiguousTargets is null ||
                row.Patch?.CanSkip is null)
                throw new PerformanceNormalizationException(
                    $"Circinus patch '{key}' is missing required measurements, canSkip, or ambiguousTargets.");
            ValidateTiming(key, "patch", row.Total, row.Mean, row.Maximum, row.CallCount);
            if (row.TimedCallCount < 0 || row.TimedCallCount > row.CallCount)
                throw new PerformanceNormalizationException(
                    $"Circinus patch '{key}' has timedCalls outside its call count.");
        }
    }

    private static void ValidateModRows(IEnumerable<NativeModRow>? rows)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows ?? Enumerable.Empty<NativeModRow>())
        {
            var key = RequiredKey(row.PackageId, "mod");
            if (!keys.Add(key))
                throw new PerformanceNormalizationException("Circinus emitted a duplicate mod row.");
            if (row.TotalMs is null || row.SharedMs is null || row.ReplacementMs is null ||
                row.PatchCount is null || row.MethodCount is null || row.ErrorCount is null)
                throw new PerformanceNormalizationException(
                    $"Circinus mod '{key}' is missing required measurements.");
            RequireNonnegativeFinite(row.Total, $"mod '{key}' total");
            RequireNonnegativeFinite(row.Shared, $"mod '{key}' shared");
            RequireNonnegativeFinite(row.Replacement, $"mod '{key}' replacement");
            if (row.Shared > row.Total || row.Replacement > row.Total)
                throw new PerformanceNormalizationException(
                    $"Circinus mod '{key}' has a component greater than its total.");
            if (row.PatchCount.Value < 0 || row.MethodCount.Value < 0 || row.ErrorCount.Value < 0)
                throw new PerformanceNormalizationException(
                    $"Circinus mod '{key}' counts must be nonnegative.");
        }
    }

    private static void ValidateSamples(IReadOnlyList<NativeSample> samples)
    {
        int? previousTick = null;
        double? previousRealtime = null;
        foreach (var sample in samples)
        {
            if (sample.Tick is null || sample.Realtime is null || sample.Tps is null ||
                sample.TargetTps is null || sample.Fps is null || sample.FrameMean is null ||
                sample.FrameMaximum is null || sample.FrameP95 is null || sample.TickMean is null ||
                sample.TickMaximum is null || sample.HeapKilobytes is null || sample.PawnCount is null)
                throw new PerformanceNormalizationException(
                    "Circinus native sample is missing required measurements.");
            if (sample.GameTick < 0 || sample.RealtimeSeconds < 0 ||
                sample.AchievedTps < 0 || sample.RequestedTps <= 0 || sample.AchievedFps < 0 ||
                sample.ManagedHeapKilobytes < 0 || sample.ActivePawnCount < 0)
                throw new PerformanceNormalizationException(
                    "Circinus native sample counters must be nonnegative and target TPS must be positive.");
            RequireNonnegativeFinite(sample.RealtimeSeconds, "sample realtime");
            RequireNonnegativeFinite(sample.FrameMeanMilliseconds, "sample frame mean");
            RequireNonnegativeFinite(sample.FrameMaximumMilliseconds, "sample frame maximum");
            RequireNonnegativeFinite(sample.FrameP95Milliseconds, "sample frame p95");
            RequireNonnegativeFinite(sample.TickMeanMilliseconds, "sample tick mean");
            RequireNonnegativeFinite(sample.TickMaximumMilliseconds, "sample tick maximum");
            if (sample.FrameMeanMilliseconds > sample.FrameMaximumMilliseconds ||
                sample.FrameP95Milliseconds > sample.FrameMaximumMilliseconds ||
                sample.TickMeanMilliseconds > sample.TickMaximumMilliseconds)
                throw new PerformanceNormalizationException(
                    "Circinus native sample timing summaries are inconsistent.");
            if (previousTick is not null && (sample.GameTick < previousTick.Value ||
                sample.RealtimeSeconds < previousRealtime!.Value))
                throw new PerformanceNormalizationException(
                    "Circinus native samples are outside monotonic capture order.");
            previousTick = sample.GameTick;
            previousRealtime = sample.RealtimeSeconds;
        }
    }

    private static void ValidateSidecars(
        IReadOnlyList<CircinusProfilerSidecar> sidecars,
        NativeEnvironment env,
        PerformanceEvidenceLens evidenceLens,
        NativeDocument document)
    {
        if (sidecars is null)
            throw new PerformanceNormalizationException("Circinus profiler sidecars are required.");
        if (evidenceLens == PerformanceEvidenceLens.FullyDisarmed && sidecars.Count != 0)
            throw new PerformanceNormalizationException(
                "A fully disarmed Circinus lens cannot retain any run-owned profiler sidecars.");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sidecar in sidecars)
        {
            if (string.IsNullOrWhiteSpace(sidecar.MethodIdentity) ||
                string.IsNullOrWhiteSpace(sidecar.RowKey) ||
                !keys.Add(sidecar.RowKind + "\0" + sidecar.RowKey))
                throw new PerformanceNormalizationException(
                    "Circinus profiler sidecar identity is missing or duplicated.");
            if (sidecar.SampleShift is < 0 or > 8 || sidecar.AmbiguousTargetCount < 0 ||
                sidecar.TotalCalls < 0 || sidecar.TotalTimedCalls < 0 || sidecar.CyclesSeen < 0 ||
                sidecar.CyclesSeen > env.RecordedCycles)
                throw new PerformanceNormalizationException(
                    $"Circinus profiler sidecar '{sidecar.RowKey}' has nonnegative/count bounds violations.");
            if (sidecar.TotalTimedCalls > sidecar.TotalCalls)
                throw new PerformanceNormalizationException(
                    $"Circinus profiler sidecar '{sidecar.RowKey}' has timed calls above total calls.");
            if ((evidenceLens == PerformanceEvidenceLens.ProductInstrumented ||
                 evidenceLens == PerformanceEvidenceLens.ArmedDisabledWrapper) &&
                !sidecar.HandArmed)
                throw new PerformanceNormalizationException(
                    $"Circinus profiler sidecar '{sidecar.RowKey}' was not hand-armed for the instrumented lens.");
            if (sidecar.Empty != string.Equals(
                    sidecar.NoRowReason,
                    "empty-or-uninvoked",
                    StringComparison.Ordinal))
                throw new PerformanceNormalizationException(
                    $"Circinus profiler sidecar '{sidecar.RowKey}' has inconsistent empty-row state.");
        }

        var nativeMethods = new HashSet<string>(
            (document.Methods ?? new List<NativeMethodRow>()).Select(row => row.Method!.Key!),
            StringComparer.Ordinal);
        var nativePatches = new HashSet<string>(
            (document.Patches ?? new List<NativePatchRow>()).Select(row => row.Patch!.Key!),
            StringComparer.Ordinal);
        var ownedMethods = new HashSet<string>(
            sidecars.Where(item => !item.Empty && item.RowKind == CircinusRowKind.Method)
                .Select(item => item.RowKey),
            StringComparer.Ordinal);
        var ownedPatches = new HashSet<string>(
            sidecars.Where(item => !item.Empty && item.RowKind == CircinusRowKind.Patch)
                .Select(item => item.RowKey),
            StringComparer.Ordinal);
        if (!nativeMethods.SetEquals(ownedMethods) || !nativePatches.SetEquals(ownedPatches))
            throw new PerformanceNormalizationException(
                "Circinus native method/patch rows contain unexplained or missing run-owned profiler evidence.");
    }

    private static void ValidateTiming(
        string key,
        string scope,
        double total,
        double mean,
        double maximum,
        long calls)
    {
        RequireNonnegativeFinite(total, $"{scope} '{key}' total");
        RequireNonnegativeFinite(mean, $"{scope} '{key}' mean");
        RequireNonnegativeFinite(maximum, $"{scope} '{key}' maximum");
        if (calls < 0)
            throw new PerformanceNormalizationException(
                $"Circinus {scope} '{key}' calls must be nonnegative.");
        if (mean > maximum || maximum > total || (calls == 0 && total > 0))
            throw new PerformanceNormalizationException(
                $"Circinus {scope} '{key}' timings and calls are inconsistent.");
    }

    private static void RequireNonnegativeFinite(double value, string field)
    {
        if (value < 0 || !IsFinite(value))
            throw new PerformanceNormalizationException(
                $"Circinus {field} must be finite and nonnegative.");
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static NativeDocument Read(string json)
    {
        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json), writable: false);
            return (NativeDocument?)new DataContractJsonSerializer(typeof(NativeDocument)).ReadObject(stream) ??
                   throw new PerformanceNormalizationException("Circinus native JSON is empty.");
        }
        catch (PerformanceNormalizationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new PerformanceNormalizationException(
                "Circinus native JSON could not be normalized: " + CircinusRuntimeAdapter.Describe(exception));
        }
    }

    private static void AddTiming(
        ICollection<PerformanceNormalizedMetric> metrics,
        string scope,
        string key,
        double total,
        double mean,
        double maximum,
        long calls,
        NativeEnvironment env)
    {
        Add(metrics, scope, key, "total", total, "ms", "profiler-window-ms", "gross-attribution");
        Add(metrics, scope, key, "mean-per-recorded-cycle", mean, "ms/cycle",
            "recorded-profiler-cycle", "gross-attribution");
        Add(metrics, scope, key, "maximum-recorded-cycle", maximum, "ms",
            "recorded-profiler-cycle", "gross-attribution");
        Add(metrics, scope, key, "calls", calls, "calls", "native-estimated-calls", "gross-attribution");
        if (calls > 0)
            Add(metrics, scope, key, "gross-ms-per-estimated-call", total / calls, "ms/call",
                "native-estimated-calls", "gross-attribution");
        Add(metrics, scope, key, "gross-profiler-window-share", total / env.WindowMilliseconds,
            "ratio", "profiler-window-ms", "gross-attribution");
    }

    private static void Add(
        ICollection<PerformanceNormalizedMetric> metrics,
        string scope,
        string key,
        string name,
        double value,
        string unit,
        string denominator,
        string claim) =>
        metrics.Add(new PerformanceNormalizedMetric(scope, key, name, value, unit, denominator, claim));

    private static IReadOnlyList<PerformanceControlCheckpoint> Checkpoints(
        PerformanceNormalizationContext context)
    {
        var result = new List<PerformanceControlCheckpoint>
        {
            new("elapsed-game-ticks", context.ElapsedGameTicks, "ticks"),
            new("elapsed-wall-milliseconds", context.ElapsedWallMilliseconds, "ms"),
            new("managed-memory-start-bytes", context.ManagedMemoryStartBytes, "bytes"),
            new("managed-memory-end-bytes", context.ManagedMemoryEndBytes, "bytes"),
            new("managed-memory-delta-bytes",
                context.ManagedMemoryEndBytes - context.ManagedMemoryStartBytes,
                "bytes")
        };
        for (var generation = 0; generation < 3; generation++)
        {
            result.Add(new PerformanceControlCheckpoint(
                "gc-generation-" + generation,
                context.GarbageCollectionsEnd[generation] - context.GarbageCollectionsStart[generation],
                "collections"));
        }
        foreach (var checkpoint in context.ThroughputCounts.OrderBy(item => item.Key, StringComparer.Ordinal))
            result.Add(new PerformanceControlCheckpoint(checkpoint.Key, checkpoint.Value, "count"));
        return result.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
    }

    private static string RequiredKey(string? value, string scope)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new PerformanceNormalizationException("Circinus " + scope + " row has no identity key.");
        return value!;
    }

    [DataContract]
    private sealed class NativeDocument
    {
        [DataMember(Name = "id")] public string? Id { get; set; }
        [DataMember(Name = "schemaMajor")] public int? SchemaMajor { get; set; }
        [DataMember(Name = "schemaMinor")] public int? SchemaMinor { get; set; }
        [DataMember(Name = "incomplete")] public bool? Incomplete { get; set; }
        [DataMember(Name = "errorsDropped")] public int? ErrorsDropped { get; set; }
        [DataMember(Name = "patchesDropped")] public int? PatchesDropped { get; set; }
        [DataMember(Name = "patchesDroppedMs")] public double? PatchesDroppedMs { get; set; }
        [DataMember(Name = "env")] public NativeEnvironment? Env { get; set; }
        [DataMember(Name = "samples")] public List<NativeSample>? Samples { get; set; }
        [DataMember(Name = "methods")] public List<NativeMethodRow>? Methods { get; set; }
        [DataMember(Name = "patches")] public List<NativePatchRow>? Patches { get; set; }
        [DataMember(Name = "modCosts")] public List<NativeModRow>? ModCosts { get; set; }
    }

    [DataContract]
    private sealed class NativeEnvironment
    {
        [DataMember(Name = "profilerCycles")] public int? ProfilerCycles { get; set; }
        [DataMember(Name = "profilerWindowMs")] public double? ProfilerWindowMs { get; set; }
        [DataMember(Name = "profilerWindowTicks")] public long? ProfilerWindowTicks { get; set; }
        [DataMember(Name = "profilerDutyPct")] public double? ProfilerDutyPct { get; set; }
        [DataMember(Name = "profilerSampled")] public bool? ProfilerSampled { get; set; }
        [DataMember(Name = "profilerDisarmedBelowFloor")] public int? ProfilerDisarmedBelowFloor { get; set; }

        public int RecordedCycles => ProfilerCycles!.Value;
        public double WindowMilliseconds => ProfilerWindowMs!.Value;
        public long WindowTicks => ProfilerWindowTicks!.Value;
        public double DutyPercent => ProfilerDutyPct!.Value;
        public bool WasSampled => ProfilerSampled!.Value;
        public int DisarmedBelowFloor => ProfilerDisarmedBelowFloor!.Value;
    }

    [DataContract]
    private sealed class NativeMethodRow
    {
        [DataMember(Name = "method")] public NativeReference? Method { get; set; }
        [DataMember(Name = "totalMs")] public double? TotalMs { get; set; }
        [DataMember(Name = "meanMs")] public double? MeanMs { get; set; }
        [DataMember(Name = "maxMs")] public double? MaxMs { get; set; }
        [DataMember(Name = "calls")] public long? Calls { get; set; }
        [DataMember(Name = "samples")] public int? Samples { get; set; }

        public double Total => TotalMs!.Value;
        public double Mean => MeanMs!.Value;
        public double Maximum => MaxMs!.Value;
        public long CallCount => Calls!.Value;
    }

    [DataContract]
    private sealed class NativePatchRow
    {
        [DataMember(Name = "patch")] public NativePatchReference? Patch { get; set; }
        [DataMember(Name = "totalMs")] public double? TotalMs { get; set; }
        [DataMember(Name = "meanMs")] public double? MeanMs { get; set; }
        [DataMember(Name = "maxObservedMs")] public double? MaximumMs { get; set; }
        [DataMember(Name = "calls")] public long? Calls { get; set; }
        [DataMember(Name = "timedCalls")] public long? TimedCalls { get; set; }
        [DataMember(Name = "ambiguousTargets")] public bool? AmbiguousTargets { get; set; }

        public double Total => TotalMs!.Value;
        public double Mean => MeanMs!.Value;
        public double Maximum => MaximumMs!.Value;
        public long CallCount => Calls!.Value;
        public long TimedCallCount => TimedCalls!.Value;
    }

    [DataContract]
    private class NativeReference
    {
        [DataMember(Name = "key")] public string? Key { get; set; }
    }

    [DataContract]
    private sealed class NativePatchReference : NativeReference
    {
        [DataMember(Name = "canSkip")] public bool? CanSkip { get; set; }
    }

    [DataContract]
    private sealed class NativeModRow
    {
        [DataMember(Name = "packageId")] public string? PackageId { get; set; }
        [DataMember(Name = "totalMs")] public double? TotalMs { get; set; }
        [DataMember(Name = "sharedMs")] public double? SharedMs { get; set; }
        [DataMember(Name = "replacementMs")] public double? ReplacementMs { get; set; }
        [DataMember(Name = "patchCount")] public int? PatchCount { get; set; }
        [DataMember(Name = "methodCount")] public int? MethodCount { get; set; }
        [DataMember(Name = "errorCount")] public int? ErrorCount { get; set; }

        public double Total => TotalMs!.Value;
        public double Shared => SharedMs!.Value;
        public double Replacement => ReplacementMs!.Value;
    }

    [DataContract]
    private sealed class NativeSample
    {
        [DataMember(Name = "t")] public int? Tick { get; set; }
        [DataMember(Name = "rt")] public double? Realtime { get; set; }
        [DataMember(Name = "tps")] public int? Tps { get; set; }
        [DataMember(Name = "tgt")] public int? TargetTps { get; set; }
        [DataMember(Name = "fps")] public int? Fps { get; set; }
        [DataMember(Name = "fmn")] public double? FrameMean { get; set; }
        [DataMember(Name = "fmx")] public double? FrameMaximum { get; set; }
        [DataMember(Name = "f95")] public double? FrameP95 { get; set; }
        [DataMember(Name = "tmn")] public double? TickMean { get; set; }
        [DataMember(Name = "tmx")] public double? TickMaximum { get; set; }
        [DataMember(Name = "heap")] public int? HeapKilobytes { get; set; }
        [DataMember(Name = "pw")] public int? PawnCount { get; set; }

        public int GameTick => Tick!.Value;
        public double RealtimeSeconds => Realtime!.Value;
        public int AchievedTps => Tps!.Value;
        public int RequestedTps => TargetTps!.Value;
        public int AchievedFps => Fps!.Value;
        public double FrameMeanMilliseconds => FrameMean!.Value;
        public double FrameMaximumMilliseconds => FrameMaximum!.Value;
        public double FrameP95Milliseconds => FrameP95!.Value;
        public double TickMeanMilliseconds => TickMean!.Value;
        public double TickMaximumMilliseconds => TickMaximum!.Value;
        public int ManagedHeapKilobytes => HeapKilobytes!.Value;
        public int ActivePawnCount => PawnCount!.Value;
    }
}
