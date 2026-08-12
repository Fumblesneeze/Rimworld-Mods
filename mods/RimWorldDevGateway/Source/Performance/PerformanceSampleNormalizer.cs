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

[DataContract]
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
        Metrics = metrics.ToArray();
        Samples = samples.ToArray();
        Checkpoints = checkpoints.ToArray();
        ProfilerSidecars = profilerSidecars.ToArray();
    }

    [DataMember(Name = "runId", Order = 1)] public string RunId { get; private set; }
    public string RawCircinusJson { get; }
    [DataMember(Name = "workloadVersion", Order = 2)] public string WorkloadVersion { get; private set; }
    [DataMember(Name = "evidenceLens", Order = 3)] public PerformanceEvidenceLens EvidenceLens { get; private set; }
    [DataMember(Name = "controlMode", Order = 4)] public string ControlMode { get; private set; }
    [DataMember(Name = "profilerPolicy", Order = 5)] public PerformanceProfilerPolicy ProfilerPolicy { get; private set; }
    [DataMember(Name = "metrics", Order = 6)] public PerformanceNormalizedMetric[] Metrics { get; private set; }
    [DataMember(Name = "samples", Order = 7)] public PerformanceNativeSample[] Samples { get; private set; }
    [DataMember(Name = "checkpoints", Order = 8)] public PerformanceControlCheckpoint[] Checkpoints { get; private set; }
    [DataMember(Name = "profilerSidecars", Order = 9)] public CircinusProfilerSidecar[] ProfilerSidecars { get; private set; }
}

[DataContract]
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

    [DataMember(Name = "recordedCycles", Order = 1)] public int RecordedCycles { get; private set; }
    [DataMember(Name = "windowMilliseconds", Order = 2)] public double WindowMilliseconds { get; private set; }
    [DataMember(Name = "windowTicks", Order = 3)] public long WindowTicks { get; private set; }
    [DataMember(Name = "dutyPercent", Order = 4)] public double DutyPercent { get; private set; }
    [DataMember(Name = "sampled", Order = 5)] public bool Sampled { get; private set; }
    [DataMember(Name = "disarmedBelowFloor", Order = 6)] public int DisarmedBelowFloor { get; private set; }
}

[DataContract]
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

    [DataMember(Name = "scope", Order = 1)] public string Scope { get; private set; }
    [DataMember(Name = "key", Order = 2)] public string Key { get; private set; }
    [DataMember(Name = "name", Order = 3)] public string Name { get; private set; }
    [DataMember(Name = "value", Order = 4)] public double Value { get; private set; }
    [DataMember(Name = "unit", Order = 5)] public string Unit { get; private set; }
    [DataMember(Name = "denominator", Order = 6)] public string Denominator { get; private set; }
    [DataMember(Name = "claim", Order = 7)] public string Claim { get; private set; }
}

[DataContract]
internal sealed class PerformanceNativeSample
{
    [DataMember(Name = "gameTick", Order = 1)] public int GameTick { get; set; }
    [DataMember(Name = "realtimeSeconds", Order = 2)] public double RealtimeSeconds { get; set; }
    [DataMember(Name = "tps", Order = 3)] public int Tps { get; set; }
    [DataMember(Name = "targetTps", Order = 4)] public int TargetTps { get; set; }
    [DataMember(Name = "fps", Order = 5)] public int Fps { get; set; }
    [DataMember(Name = "frameMeanMilliseconds", Order = 6)] public double FrameMeanMilliseconds { get; set; }
    [DataMember(Name = "frameMaximumMilliseconds", Order = 7)] public double FrameMaximumMilliseconds { get; set; }
    [DataMember(Name = "frameP95Milliseconds", Order = 8)] public double FrameP95Milliseconds { get; set; }
    [DataMember(Name = "tickMeanMilliseconds", Order = 9)] public double TickMeanMilliseconds { get; set; }
    [DataMember(Name = "tickMaximumMilliseconds", Order = 10)] public double TickMaximumMilliseconds { get; set; }
    [DataMember(Name = "heapKilobytes", Order = 11)] public int HeapKilobytes { get; set; }
    [DataMember(Name = "pawnCount", Order = 12)] public int PawnCount { get; set; }
}

[DataContract]
internal sealed class PerformanceControlCheckpoint
{
    public PerformanceControlCheckpoint(string id, double value, string unit)
    {
        Id = id;
        Value = value;
        Unit = unit;
    }

    [DataMember(Name = "id", Order = 1)] public string Id { get; private set; }
    [DataMember(Name = "value", Order = 2)] public double Value { get; private set; }
    [DataMember(Name = "unit", Order = 3)] public string Unit { get; private set; }
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
            if (row.AmbiguousTargets)
            {
                Add(metrics, "patch", key, "ambiguous-targets", 1,
                    "boolean", "native-patch-identity", "sampling-policy");
                AddWindowShare(metrics, "patch", key, "shared-gross-share", row.Total, env);
                AddWindowShare(metrics, "patch", key, "ambiguous-target-gross-share", row.Total, env);
            }
            if (row.Patch?.CanSkip == true)
                AddWindowShare(metrics, "patch", key, "skip-capable-gross-share", row.Total, env);
        }

        foreach (var row in (IEnumerable<NativeModRow>?)document.ModCosts ?? Enumerable.Empty<NativeModRow>())
        {
            var key = RequiredKey(row.PackageId, "mod");
            Add(metrics, "mod", key, "total", row.Total, "ms", "profiler-window-ms", "gross-attribution");
            AddWindowShare(metrics, "mod", key, "gross-profiler-window-share", row.Total, env);
            AddWindowShare(metrics, "mod", key, "shared-profiler-window-share", row.Shared, env);
            AddWindowShare(metrics, "mod", key, "replacement-profiler-window-share", row.Replacement, env);
        }

        var emittedSidecarMetrics = new HashSet<string>(StringComparer.Ordinal);
        var emittedTimingRows = new HashSet<string>(metrics
            .Where(metric => metric.Name == "total" &&
                             (metric.Scope == "method" || metric.Scope == "patch"))
            .Select(metric => metric.Scope + "\0" + metric.Key), StringComparer.Ordinal);
        foreach (var sidecar in capture.Sidecars)
        {
            var scope = sidecar.RowKind == CircinusRowKind.Patch ? "patch" : "method";
            if (!emittedSidecarMetrics.Add(scope + "\0" + sidecar.RowKey))
            {
                // Validation permits a shared key only for distinct empty profilers that
                // Circinus omits from its native rows. Retain every raw sidecar below, but
                // emit the identical zero-valued policy metric set only once.
                continue;
            }
            if (!emittedTimingRows.Contains(scope + "\0" + sidecar.RowKey) &&
                sidecar.TotalCalls == 0 && sidecar.TotalTimedCalls == 0)
            {
                // Circinus legitimately omits an armed profiler row that saw no
                // invocation. Keep a stable explicit-zero gross schema so later
                // repetitions do not confuse zero work with schema drift.
                AddTiming(metrics, scope, sidecar.RowKey, 0, 0, 0, 0, env);
                if (sidecar.RowKind == CircinusRowKind.Patch)
                {
                    Add(metrics, scope, sidecar.RowKey, "native-timed-calls", 0, "calls",
                        "native-adaptive-sampling", "sampling-policy");
                    if (sidecar.AmbiguousTargetCount > 1)
                    {
                        Add(metrics, scope, sidecar.RowKey, "ambiguous-targets", 1,
                            "boolean", "native-patch-identity", "sampling-policy");
                        AddWindowShare(metrics, scope, sidecar.RowKey, "shared-gross-share", 0, env);
                        AddWindowShare(metrics, scope, sidecar.RowKey, "ambiguous-target-gross-share", 0, env);
                    }
                    if (sidecar.CanSkip)
                        AddWindowShare(metrics, scope, sidecar.RowKey, "skip-capable-gross-share", 0, env);
                }
            }
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

        var samples = SamplesInsideGatewayWindow(document, context)
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
        ValidateEnvironment(document.Env, context.EvidenceLens, document);
        ValidateMethodRows(document.Methods, document.Env);
        ValidatePatchRows(document.Patches);
        ValidateModRows(document.ModCosts);
        ValidateSamples(document.Samples);
        _ = SamplesInsideGatewayWindow(document, context);
        ValidateSidecars(capture.Sidecars, document.Env, context.EvidenceLens, document);
    }

    private static void ValidateEnvironment(
        NativeEnvironment env,
        PerformanceEvidenceLens evidenceLens,
        NativeDocument document)
    {
        if (env.ProfilerCycles is null || env.ProfilerWindowMs is null ||
            env.ProfilerWindowTicks is null || env.ProfilerDutyPct is null ||
            env.ProfilerSampled is null || env.ProfilerDisarmedBelowFloor is null)
            throw new PerformanceNormalizationException(
                "Circinus native JSON is missing required profiler environment fields.");
        if (env.ProfilerCycles.Value < 0 || env.ProfilerWindowMs.Value < 0 ||
            env.ProfilerWindowTicks.Value < 0 || !IsFinite(env.ProfilerWindowMs.Value))
            throw new PerformanceNormalizationException("Circinus has an invalid profiler denominator.");
        if (env.ProfilerCycles.Value >= MaximumProfilerCycles)
            throw new PerformanceNormalizationException("Circinus profiler cycles overflowed its retained frame ring.");
        if (!IsFinite(env.ProfilerDutyPct.Value) || env.ProfilerDutyPct.Value < 0 ||
            env.ProfilerDutyPct.Value > 100)
            throw new PerformanceNormalizationException("Circinus profiler duty percentage is outside 0..100.");
        if (env.ProfilerDisarmedBelowFloor.Value < 0)
            throw new PerformanceNormalizationException("Circinus disarmed-profiler count must be nonnegative.");
        var activelyTimed = evidenceLens is PerformanceEvidenceLens.ProductInstrumented or
            PerformanceEvidenceLens.ProductAbsentControl;
        if (activelyTimed &&
            (env.ProfilerWindowMs.Value <= 0 || env.ProfilerCycles.Value <= 0 ||
             env.ProfilerWindowTicks.Value <= 0 || !env.ProfilerSampled.Value))
            throw new PerformanceNormalizationException(
                "Circinus did not retain a sampled profiler denominator for the active-timing lens.");
        if (!activelyTimed &&
            (env.ProfilerWindowMs.Value != 0 || env.ProfilerCycles.Value != 0 ||
             env.ProfilerWindowTicks.Value != 0 || env.ProfilerSampled.Value))
            throw new PerformanceNormalizationException(
                "Circinus recorded active profiler timing in a disabled-control lens.");
        if (env.ProfilerWindowMs.Value == 0)
        {
            var nonzeroMethod = (document.Methods ?? new List<NativeMethodRow>()).Any(row =>
                (row.TotalMs ?? 0) != 0 || (row.MeanMs ?? 0) != 0 || (row.MaxMs ?? 0) != 0 ||
                (row.Calls ?? 0) != 0);
            var nonzeroPatch = (document.Patches ?? new List<NativePatchRow>()).Any(row =>
                (row.TotalMs ?? 0) != 0 || (row.MeanMs ?? 0) != 0 || (row.MaximumMs ?? 0) != 0 ||
                (row.Calls ?? 0) != 0 || (row.TimedCalls ?? 0) != 0);
            var nonzeroMod = (document.ModCosts ?? new List<NativeModRow>()).Any(row =>
                (row.TotalMs ?? 0) != 0 || (row.SharedMs ?? 0) != 0 ||
                (row.ReplacementMs ?? 0) != 0);
            if (nonzeroMethod || nonzeroPatch || nonzeroMod)
                throw new PerformanceNormalizationException(
                    "Circinus retained native timing or call activity against a zero profiler window.");
        }
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
                row.Calls is null || row.TimedCalls is null || row.Patch is null)
                throw new PerformanceNormalizationException(
                    $"Circinus patch '{key}' is missing required measurements or canSkip.");
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
                sample.AchievedTps < 0 || sample.RequestedTps < 0 || sample.AchievedFps < 0 ||
                sample.ManagedHeapKilobytes < 0 || sample.ActivePawnCount < 0)
                throw new PerformanceNormalizationException(
                    "Circinus native sample counters and target TPS must be nonnegative.");
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
        if (env.WindowMilliseconds == 0 && sidecars.Any(sidecar =>
                sidecar.TotalCalls != 0 || sidecar.TotalTimedCalls != 0 || sidecar.CyclesSeen != 0))
            throw new PerformanceNormalizationException(
                "Circinus retained live profiler sidecar activity against a zero profiler window.");
        var nativePatchAmbiguity = (document.Patches ?? new List<NativePatchRow>())
            .Where(row => !string.IsNullOrWhiteSpace(row.Patch?.Key))
            .ToDictionary(row => row.Patch!.Key!, row => row.AmbiguousTargets, StringComparer.Ordinal);
        var nativePatchCanSkip = (document.Patches ?? new List<NativePatchRow>())
            .Where(row => !string.IsNullOrWhiteSpace(row.Patch?.Key))
            .ToDictionary(row => row.Patch!.Key!, row => row.Patch!.CanSkip, StringComparer.Ordinal);
        var identities = new HashSet<string>(StringComparer.Ordinal);
        var rowGroups = new Dictionary<string, List<CircinusProfilerSidecar>>(StringComparer.Ordinal);
        foreach (var sidecar in sidecars)
        {
            if (string.IsNullOrWhiteSpace(sidecar.MethodIdentity) ||
                string.IsNullOrWhiteSpace(sidecar.RowKey) ||
                !identities.Add(sidecar.MethodIdentity))
                throw new PerformanceNormalizationException(
                    "Circinus profiler sidecar method identity is missing or duplicated.");
            var groupKey = sidecar.RowKind + "\0" + sidecar.RowKey;
            if (!rowGroups.TryGetValue(groupKey, out var group))
            {
                group = new List<CircinusProfilerSidecar>();
                rowGroups.Add(groupKey, group);
            }
            group.Add(sidecar);
            if (sidecar.SampleShift is < 0 or > 8 || sidecar.AmbiguousTargetCount < 0 ||
                sidecar.TotalCalls < 0 || sidecar.TotalTimedCalls < 0 || sidecar.CyclesSeen < 0 ||
                sidecar.CyclesSeen > env.RecordedCycles)
                throw new PerformanceNormalizationException(
                    $"Circinus profiler sidecar '{sidecar.RowKey}' has nonnegative/count bounds violations.");
            if (sidecar.TotalTimedCalls > sidecar.TotalCalls)
                throw new PerformanceNormalizationException(
                    $"Circinus profiler sidecar '{sidecar.RowKey}' has timed calls above total calls.");
            if ((evidenceLens == PerformanceEvidenceLens.ProductInstrumented ||
                 evidenceLens == PerformanceEvidenceLens.ArmedDisabledWrapper ||
                 evidenceLens == PerformanceEvidenceLens.ProductAbsentControl) &&
                !sidecar.HandArmed)
                throw new PerformanceNormalizationException(
                    $"Circinus profiler sidecar '{sidecar.RowKey}' was not hand-armed for the instrumented lens.");
            if (sidecar.Empty != string.Equals(
                    sidecar.NoRowReason,
                    "empty-or-uninvoked",
                    StringComparison.Ordinal))
                throw new PerformanceNormalizationException(
                    $"Circinus profiler sidecar '{sidecar.RowKey}' has inconsistent empty-row state.");
            if (!sidecar.Empty && sidecar.RowKind == CircinusRowKind.Patch &&
                nativePatchAmbiguity.TryGetValue(sidecar.RowKey, out var nativeAmbiguous) &&
                nativeAmbiguous != (sidecar.AmbiguousTargetCount > 1))
                throw new PerformanceNormalizationException(
                    $"Circinus patch '{sidecar.RowKey}' native ambiguity disagrees with its live attachment count.");
            if (!sidecar.Empty && sidecar.RowKind == CircinusRowKind.Patch &&
                nativePatchCanSkip.TryGetValue(sidecar.RowKey, out var nativeCanSkip) &&
                nativeCanSkip != sidecar.CanSkip)
                throw new PerformanceNormalizationException(
                    $"Circinus patch '{sidecar.RowKey}' native skip capability disagrees with its live attachment.");
        }

        foreach (var group in rowGroups.Values.Where(group => group.Count > 1))
        {
            if (group.Any(sidecar => !sidecar.Empty))
                throw new PerformanceNormalizationException(
                    "Circinus non-empty profiler sidecar row identity is duplicated for bounded row '" +
                    group[0].RowKey + "'.");
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

    private static IReadOnlyList<NativeSample> SamplesInsideGatewayWindow(
        NativeDocument document,
        PerformanceNormalizationContext context)
    {
        var markers = document.Markers ?? new List<NativeMarker>();
        var starts = markers.Where(item =>
                string.Equals(item.Label, "gateway.sample-start", StringComparison.Ordinal))
            .ToArray();
        var ends = markers.Where(item =>
                string.Equals(item.Label, "gateway.sample-end", StringComparison.Ordinal))
            .ToArray();
        if (starts.Length != 1 || ends.Length != 1)
            throw new PerformanceNormalizationException(
                "Circinus requires exactly one gateway sample-start and one gateway sample-end marker.");
        var start = starts[0];
        var end = ends[0];
        if (start.Tick is null || start.Realtime is null || end.Tick is null || end.Realtime is null ||
            start.Tick.Value < 0 || start.Realtime.Value < 0 || end.Tick.Value < start.Tick.Value ||
            end.Realtime.Value < start.Realtime.Value ||
            !IsFinite(start.Realtime.Value) || !IsFinite(end.Realtime.Value) ||
            !string.Equals(start.Kind, context.WorkloadVersion, StringComparison.Ordinal) ||
            !string.Equals(end.Kind, context.WorkloadVersion, StringComparison.Ordinal))
            throw new PerformanceNormalizationException(
                "Circinus gateway sample boundary markers are invalid or belong to another workload.");
        if ((long)end.Tick.Value - start.Tick.Value != context.ElapsedGameTicks)
            throw new PerformanceNormalizationException(
                "Circinus marker tick span does not match the exact Gateway control window.");

        var samples = (document.Samples ?? new List<NativeSample>())
            .Where(item => item.Tick is not null && item.Realtime is not null &&
                           item.GameTick >= start.Tick.Value && item.GameTick <= end.Tick.Value &&
                           item.RealtimeSeconds > start.Realtime.Value &&
                           item.RealtimeSeconds <= end.Realtime.Value)
            .ToArray();
        if (samples.Length == 0)
            throw new PerformanceNormalizationException(
                "Circinus retained no native samples inside the exact Gateway sample boundaries.");
        return samples;
    }

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
            Add(metrics, scope, key, "gross-ms-per-estimated-call", total / calls,
                "ms/call", "native-estimated-calls", "gross-attribution");
        AddWindowShare(metrics, scope, key, "gross-profiler-window-share", total, env);
    }

    private static void AddWindowShare(
        ICollection<PerformanceNormalizedMetric> metrics,
        string scope,
        string key,
        string name,
        double total,
        NativeEnvironment env)
    {
        if (env.WindowMilliseconds <= 0) return;
        Add(metrics, scope, key, name, total / env.WindowMilliseconds,
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
        string claim)
    {
        if (!IsFinite(value))
            throw new PerformanceNormalizationException(
                $"Normalized metric '{scope}/{key}/{name}' is not finite.");
        metrics.Add(new PerformanceNormalizedMetric(scope, key, name, value, unit, denominator, claim));
    }

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
        [DataMember(Name = "markers")] public List<NativeMarker>? Markers { get; set; }
        [DataMember(Name = "methods")] public List<NativeMethodRow>? Methods { get; set; }
        [DataMember(Name = "patches")] public List<NativePatchRow>? Patches { get; set; }
        [DataMember(Name = "modCosts")] public List<NativeModRow>? ModCosts { get; set; }
    }

    [DataContract]
    private sealed class NativeMarker
    {
        [DataMember(Name = "label")] public string? Label { get; set; }
        [DataMember(Name = "tick")] public int? Tick { get; set; }
        [DataMember(Name = "realtime")] public double? Realtime { get; set; }
        [DataMember(Name = "kind")] public string? Kind { get; set; }
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
        // Circinus omits this default-valued field for an ordinary one-target patch.
        [DataMember(Name = "ambiguousTargets", EmitDefaultValue = false)] public bool AmbiguousTargets { get; set; }

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
        // Circinus omits this default-valued field for an ordinary non-skip-capable patch.
        [DataMember(Name = "canSkip", EmitDefaultValue = false)] public bool CanSkip { get; set; }
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
