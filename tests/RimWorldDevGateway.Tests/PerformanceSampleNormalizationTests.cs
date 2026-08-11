using NUnit.Framework;
using RimWorldDevGateway.Performance;
using RimWorldDevGateway.PerformanceTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class PerformanceSampleNormalizationTests
{
    [Test]
    public void Native_rows_sidecars_time_series_and_control_checkpoints_keep_units_and_denominators()
    {
        var raw = NativeJson().Replace("\"kind\":\"v1\"", "\"kind\":\"kitchen-v3\"");
        var capture = Capture(raw);
        var context = new PerformanceNormalizationContext(
            workloadVersion: "kitchen-v3",
            PerformanceEvidenceLens.ProductInstrumented,
            controlMode: "instrumented",
            elapsedGameTicks: 1_200,
            elapsedWallMilliseconds: 25_000,
            managedMemoryStartBytes: 100_000,
            managedMemoryEndBytes: 125_000,
            new[] { 2, 1, 0 },
            new[] { 5, 2, 1 },
            new Dictionary<string, long> { ["meals-cooked"] = 14 });

        var normalized = PerformanceSampleNormalizer.Normalize(capture, context);

        Assert.Multiple(() =>
        {
            Assert.That(normalized.RawCircinusJson, Is.EqualTo(raw));
            Assert.That(normalized.WorkloadVersion, Is.EqualTo("kitchen-v3"));
            Assert.That(normalized.EvidenceLens, Is.EqualTo(PerformanceEvidenceLens.ProductInstrumented));
            Assert.That(normalized.ControlMode, Is.EqualTo("instrumented"));
            AssertMetric(normalized, "patch", "p1", "total", 12d, "ms", "profiler-window-ms");
            AssertMetric(normalized, "patch", "p1", "mean-per-recorded-cycle", 0.6d, "ms/cycle", "recorded-profiler-cycle");
            AssertMetric(normalized, "patch", "p1", "maximum-recorded-cycle", 2d, "ms", "recorded-profiler-cycle");
            AssertMetric(normalized, "patch", "p1", "gross-profiler-window-share", 0.06d, "ratio", "profiler-window-ms");
            AssertMetric(normalized, "patch", "p1", "gross-ms-per-estimated-call", 0.12d, "ms/call", "native-estimated-calls");
            AssertMetric(normalized, "patch", "p1", "native-timed-calls", 25d, "calls", "native-adaptive-sampling", "sampling-policy");
            AssertMetric(normalized, "patch", "p1", "live-total-timed-calls", 30d, "calls", "native-adaptive-sampling", "sampling-policy");
            AssertMetric(normalized, "patch", "p1", "sample-shift", 2d, "shift", "native-adaptive-sampling", "sampling-policy");
            AssertMetric(normalized, "patch", "p1", "shared-gross-share", 0.06d, "ratio", "profiler-window-ms");
            AssertMetric(normalized, "patch", "p1", "ambiguous-target-gross-share", 0.06d, "ratio", "profiler-window-ms");
            AssertMetric(normalized, "patch", "p1", "skip-capable-gross-share", 0.06d, "ratio", "profiler-window-ms");
            AssertMetric(normalized, "method", "m1", "total", 4d, "ms", "profiler-window-ms");
            AssertMetric(normalized, "mod", "product.mod", "gross-profiler-window-share", 0.08d, "ratio", "profiler-window-ms");
            AssertMetric(normalized, "mod", "product.mod", "shared-profiler-window-share", 0.06d, "ratio", "profiler-window-ms");
            AssertMetric(normalized, "mod", "product.mod", "replacement-profiler-window-share", 0.02d, "ratio", "profiler-window-ms");
            Assert.That(normalized.Samples, Has.Length.EqualTo(1));
            Assert.That(normalized.Samples[0].Tps, Is.EqualTo(58));
            Assert.That(normalized.Samples[0].FrameMeanMilliseconds, Is.EqualTo(8.5d));
            Assert.That(normalized.Samples[0].HeapKilobytes, Is.EqualTo(512_000));
            Assert.That(normalized.Checkpoints.Single(item => item.Id == "elapsed-game-ticks").Value,
                Is.EqualTo(1_200));
            Assert.That(normalized.Checkpoints.Single(item => item.Id == "gc-generation-0").Value,
                Is.EqualTo(3));
            Assert.That(normalized.Checkpoints.Single(item => item.Id == "managed-memory-delta-bytes").Value,
                Is.EqualTo(25_000));
            Assert.That(normalized.Checkpoints.Single(item => item.Id == "meals-cooked").Value,
                Is.EqualTo(14));
            Assert.That(normalized.ProfilerPolicy.DutyPercent, Is.EqualTo(20d));
            Assert.That(normalized.ProfilerPolicy.RecordedCycles, Is.EqualTo(20));
            Assert.That(normalized.ProfilerPolicy.WindowTicks, Is.EqualTo(1_200));
            Assert.That(normalized.ProfilerPolicy.Sampled, Is.True);
            Assert.That(normalized.ProfilerSidecars.Select(item => item.RowKey), Is.EqualTo(new[] { "p1", "m1" }));
            Assert.That(normalized.ProfilerSidecars, Has.All.Property(nameof(CircinusProfilerSidecar.NoRowReason)).Null);
        });
    }

    [TestCase("\"incomplete\":true", "incomplete")]
    [TestCase("\"patchesDropped\":1", "dropped")]
    [TestCase("\"errorsDropped\":1", "errors")]
    [TestCase("\"profilerWindowMs\":0", "denominator")]
    [TestCase("\"profilerCycles\":2000", "ring")]
    public void Incomplete_truncated_or_denominatorless_native_run_is_rejected(
        string replacement,
        string expected)
    {
        var original = expected switch
        {
            "incomplete" => "\"incomplete\":false",
            "dropped" => "\"patchesDropped\":0",
            "errors" => "\"errorsDropped\":0",
            "denominator" => "\"profilerWindowMs\":200",
            "ring" => "\"profilerCycles\":20",
            _ => throw new AssertionException("unknown normalization fixture")
        };
        var raw = NativeJson().Replace(original, replacement);
        var exception = Assert.Throws<PerformanceNormalizationException>(() =>
            PerformanceSampleNormalizer.Normalize(Capture(raw), Context()));
        Assert.That(exception!.Message, Does.Contain(expected).IgnoreCase);
    }

    [Test]
    public void Control_context_rejects_unbounded_nonmonotonic_or_colliding_checkpoints()
    {
        var over = Enumerable.Range(0, PerformanceTestContract.MaximumThroughputCheckpoints + 1)
            .ToDictionary(index => "checkpoint-" + index, _ => 1L);
        Assert.That(() => new PerformanceNormalizationContext(
                "v1", PerformanceEvidenceLens.ProductInstrumented, "instrumented",
                1, 1, 1, 1, new[] { 0, 0, 0 }, new[] { 0, 0, 0 }, over),
            Throws.ArgumentException.With.Message.Contains(
                PerformanceTestContract.MaximumThroughputCheckpoints.ToString()));
        Assert.That(() => new PerformanceNormalizationContext(
                "v1", PerformanceEvidenceLens.ProductInstrumented, "instrumented",
                1, 1, 1, 1, new[] { 2, 0, 0 }, new[] { 1, 0, 0 },
                new Dictionary<string, long>()),
            Throws.ArgumentException.With.Message.Contains("monotonic"));
        Assert.That(() => new PerformanceNormalizationContext(
                "v1", PerformanceEvidenceLens.ProductInstrumented, "instrumented",
                1, 1, 1, 1, new[] { 0, 0, 0 }, new[] { 0, 0, 0 },
                new Dictionary<string, long> { ["elapsed-game-ticks"] = 1 }),
            Throws.ArgumentException.With.Message.Contains("reserved"));
    }

    [TestCase("\"meanMs\":0.6,", "", "required")]
    [TestCase("\"totalMs\":12", "\"totalMs\":-1", "nonnegative")]
    [TestCase("\"timedCalls\":25", "\"timedCalls\":101", "timedCalls")]
    [TestCase("\"samples\":[", "\"removedSamples\":[", "sample")]
    public void Missing_negative_inconsistent_or_absent_native_measurements_fail_closed(
        string original,
        string replacement,
        string expected)
    {
        var raw = NativeJson().Replace(original, replacement);
        var exception = Assert.Throws<PerformanceNormalizationException>(() =>
            PerformanceSampleNormalizer.Normalize(Capture(raw), Context()));
        Assert.That(exception!.Message, Does.Contain(expected).IgnoreCase);
    }

    [Test]
    public void Native_samples_must_be_complete_and_monotonic()
    {
        const string firstTail = "\"heap\":512000,\"pw\":36}";
        const string earlier =
            ", {\"t\":1100,\"rt\":19,\"tps\":58,\"tgt\":60,\"fps\":120," +
            "\"fmn\":8.5,\"fmx\":15,\"f95\":12,\"tmn\":9,\"tmx\":18," +
            "\"heap\":512000,\"pw\":36}";
        var raw = NativeJson().Replace(firstTail, firstTail + earlier);
        var exception = Assert.Throws<PerformanceNormalizationException>(() =>
            PerformanceSampleNormalizer.Normalize(Capture(raw), Context()));
        Assert.That(exception!.Message, Does.Contain("order").IgnoreCase);
    }

    [Test]
    public void Paused_terminal_native_sample_may_report_zero_target_tps()
    {
        var raw = NativeJson().Replace("\"tgt\":60", "\"tgt\":0");

        var normalized = PerformanceSampleNormalizer.Normalize(Capture(raw), Context());

        Assert.That(normalized.Samples.Single().TargetTps, Is.Zero);
    }

    [Test]
    public void Instrumented_evidence_rejects_non_hand_armed_or_inconsistent_sidecars()
    {
        var exception = Assert.Throws<PerformanceNormalizationException>(() =>
            PerformanceSampleNormalizer.Normalize(
                Capture(NativeJson(), patchHandArmed: false),
                Context()));
        Assert.That(exception!.Message, Does.Contain("hand-armed").IgnoreCase);

        exception = Assert.Throws<PerformanceNormalizationException>(() =>
            PerformanceSampleNormalizer.Normalize(
                Capture(NativeJson(), patchTimedCalls: 101),
                Context()));
        Assert.That(exception!.Message, Does.Contain("timed").IgnoreCase);
    }

    [Test]
    public void Product_absent_control_requires_every_owned_profiler_to_remain_hand_armed()
    {
        var context = new PerformanceNormalizationContext(
            "v1", PerformanceEvidenceLens.ProductAbsentControl, "product-absent-control",
            1_200, 1_000, 100, 200, new[] { 0, 0, 0 }, new[] { 0, 0, 0 },
            new Dictionary<string, long>());

        var exception = Assert.Throws<PerformanceNormalizationException>(() =>
            PerformanceSampleNormalizer.Normalize(Capture(NativeJson(), patchHandArmed: false), context));

        Assert.That(exception!.Message, Does.Contain("hand-armed").IgnoreCase);
    }

    [Test]
    public void Only_native_samples_between_the_exact_gateway_boundary_markers_are_normalized()
    {
        const string sample =
            "{\"t\":600,\"rt\":10,\"tps\":30,\"tgt\":60,\"fps\":60," +
            "\"fmn\":10,\"fmx\":20,\"f95\":15,\"tmn\":10,\"tmx\":20," +
            "\"heap\":500000,\"pw\":30},";
        var raw = NativeJson().Replace("\"samples\":[", "\"samples\":[" + sample);

        var normalized = PerformanceSampleNormalizer.Normalize(Capture(raw), Context());

        Assert.That(normalized.Samples.Select(item => item.RealtimeSeconds), Is.EqualTo(new[] { 20d }));
    }

    [TestCase("\"markers\":[", "\"removedMarkers\":[", "marker")]
    [TestCase("gateway.sample-end", "gateway.sample-start", "exactly one")]
    public void Missing_or_ambiguous_gateway_sample_boundaries_fail_closed(
        string original,
        string replacement,
        string expected)
    {
        var exception = Assert.Throws<PerformanceNormalizationException>(() =>
            PerformanceSampleNormalizer.Normalize(Capture(NativeJson().Replace(original, replacement)), Context()));

        Assert.That(exception!.Message, Does.Contain(expected).IgnoreCase);
    }

    [Test]
    public void Gateway_and_Circinus_sample_tick_windows_must_be_identical()
    {
        var mismatched = new PerformanceNormalizationContext(
            "v1", PerformanceEvidenceLens.ProductInstrumented, "instrumented",
            1_199, 1_000, 100, 200, new[] { 0, 0, 0 }, new[] { 0, 0, 0 },
            new Dictionary<string, long>());

        var exception = Assert.Throws<PerformanceNormalizationException>(() =>
            PerformanceSampleNormalizer.Normalize(Capture(NativeJson()), mismatched));

        Assert.That(exception!.Message, Does.Contain("tick span").IgnoreCase);
    }

    [Test]
    public void Evidence_lens_requires_its_exact_control_mode_and_instrumentation_state()
    {
        Assert.That(() => new PerformanceNormalizationContext(
                "v1", PerformanceEvidenceLens.ProductInstrumented, "fully-disarmed",
                1, 1, 1, 1, new[] { 0, 0, 0 }, new[] { 0, 0, 0 },
                new Dictionary<string, long>()),
            Throws.ArgumentException.With.Message.Contains("control mode"));

        var context = new PerformanceNormalizationContext(
            "v1", PerformanceEvidenceLens.FullyDisarmed, "fully-disarmed",
            1_200, 1, 1, 1, new[] { 0, 0, 0 }, new[] { 0, 0, 0 },
            new Dictionary<string, long>());
        var disabledRaw = DisabledNativeJson()
            .ReplaceBetween("\"methods\":[", "],\"patches\"", string.Empty)
            .ReplaceBetween("\"patches\":[", "],\"modCosts\"", string.Empty)
            .ReplaceBetween("\"modCosts\":[", "]}", string.Empty);
        var exception = Assert.Throws<PerformanceNormalizationException>(() =>
            PerformanceSampleNormalizer.Normalize(
                new CircinusCapture(
                    "run-1", 1, 15, disabledRaw, disabledRaw, Capture(NativeJson()).Sidecars),
                context));
        Assert.That(exception!.Message, Does.Contain("fully disarmed").IgnoreCase);

        var unexpectedRowRaw = disabledRaw.Replace(
            "\"methods\":[]",
            "\"methods\":[{\"method\":{\"key\":\"unexpected\"},\"totalMs\":0," +
            "\"meanMs\":0,\"maxMs\":0,\"calls\":0,\"samples\":0}]");
        var unexplainedRows = new CircinusCapture(
            "run-1", 1, 15, unexpectedRowRaw, unexpectedRowRaw,
            Array.Empty<CircinusProfilerSidecar>());
        exception = Assert.Throws<PerformanceNormalizationException>(() =>
            PerformanceSampleNormalizer.Normalize(unexplainedRows, context));
        Assert.That(exception!.Message, Does.Contain("unexplained").IgnoreCase);
    }

    [TestCase(PerformanceEvidenceLens.ArmedDisabledWrapper, "armed-disabled-wrapper")]
    [TestCase(PerformanceEvidenceLens.FullyDisarmed, "fully-disarmed")]
    public void Disabled_control_lenses_accept_native_samples_but_require_zero_profiler_activity(
        PerformanceEvidenceLens lens,
        string controlMode)
    {
        var raw = DisabledNativeJson()
            .ReplaceBetween("\"methods\":[", "],\"patches\"", string.Empty)
            .ReplaceBetween("\"patches\":[", "],\"modCosts\"", string.Empty)
            .ReplaceBetween("\"modCosts\":[", "]}", string.Empty);
        var sidecars = lens == PerformanceEvidenceLens.ArmedDisabledWrapper
            ? Capture(NativeJson()).Sidecars.Select(item => new CircinusProfilerSidecar(
                item.MethodIdentity,
                item.RowKind,
                item.RowKey,
                item.AmbiguousTargetCount,
                true,
                0,
                0,
                0,
                true,
                0,
                "empty-or-uninvoked")).ToArray()
            : Array.Empty<CircinusProfilerSidecar>();
        var capture = new CircinusCapture("run-1", 1, 15, raw, raw, sidecars);
        var context = new PerformanceNormalizationContext(
            "v1", lens, controlMode, 1_200, 1000, 1, 1,
            new[] { 0, 0, 0 }, new[] { 0, 0, 0 }, new Dictionary<string, long>());

        var normalized = PerformanceSampleNormalizer.Normalize(capture, context);

        Assert.Multiple(() =>
        {
            Assert.That(normalized.Samples, Has.Length.EqualTo(1));
            Assert.That(normalized.ProfilerPolicy.RecordedCycles, Is.Zero);
            Assert.That(normalized.ProfilerPolicy.Sampled, Is.False);
        });
    }

    [Test]
    public void Native_rows_must_exactly_match_nonempty_run_owned_sidecars()
    {
        const string firstMethod =
            "{\"method\":{\"key\":\"m1\"},\"totalMs\":4,\"meanMs\":0.2," +
            "\"maxMs\":1,\"calls\":20,\"samples\":20}";
        const string extraMethod =
            ",{\"method\":{\"key\":\"external\"},\"totalMs\":1,\"meanMs\":0.05," +
            "\"maxMs\":0.2,\"calls\":20,\"samples\":20}";
        var raw = NativeJson().Replace(firstMethod, firstMethod + extraMethod);
        var exception = Assert.Throws<PerformanceNormalizationException>(() =>
            PerformanceSampleNormalizer.Normalize(Capture(raw), Context()));
        Assert.That(exception!.Message, Does.Contain("unexplained").IgnoreCase);
    }

    [TestCase(",\"canSkip\":true", "", "canSkip")]
    public void Required_native_patch_flags_cannot_be_omitted(
        string original,
        string replacement,
        string expected)
    {
        var exception = Assert.Throws<PerformanceNormalizationException>(() =>
            PerformanceSampleNormalizer.Normalize(
                Capture(NativeJson().Replace(original, replacement)),
                Context()));
        Assert.That(exception!.Message, Does.Contain(expected).IgnoreCase);
    }

    [Test]
    public void Native_nonambiguous_patch_may_omit_its_false_ambiguity_flag()
    {
        var raw = NativeJson().Replace(",\"ambiguousTargets\":true", string.Empty);

        var normalized = PerformanceSampleNormalizer.Normalize(
            Capture(raw, patchTargetCount: 1),
            Context());

        Assert.That(normalized.Metrics.Any(item =>
            item.Scope == "patch" && item.Key == "p1" && item.Name == "ambiguous-target-gross-share"),
            Is.False);
    }

    [TestCase(1, true)]
    [TestCase(2, false)]
    public void Native_patch_ambiguity_must_match_the_live_attachment_count(
        int targetCount,
        bool nativeAmbiguous)
    {
        var raw = nativeAmbiguous
            ? NativeJson()
            : NativeJson().Replace(",\"ambiguousTargets\":true", string.Empty);

        var exception = Assert.Throws<PerformanceNormalizationException>(() =>
            PerformanceSampleNormalizer.Normalize(
                Capture(raw, patchTargetCount: targetCount),
                Context()));

        Assert.That(exception!.Message, Does.Contain("ambiguity").IgnoreCase);
    }

    [Test]
    public void Frame_mean_may_exceed_p95_when_both_remain_below_the_maximum()
    {
        var raw = NativeJson().Replace("\"fmn\":8.5,\"fmx\":15,\"f95\":12", "\"fmn\":13,\"fmx\":15,\"f95\":12");
        Assert.That(() => PerformanceSampleNormalizer.Normalize(Capture(raw), Context()), Throws.Nothing);
    }

    private static PerformanceNormalizationContext Context() => new(
        "v1",
        PerformanceEvidenceLens.ProductInstrumented,
        "instrumented",
        1_200,
        1_000,
        100,
        200,
        new[] { 0, 0, 0 },
        new[] { 0, 0, 0 },
        new Dictionary<string, long>());

    private static CircinusCapture Capture(
        string raw,
        bool patchHandArmed = true,
        long patchTimedCalls = 30,
        int patchTargetCount = 2) => new(
        "run-1",
        1,
        15,
        raw,
        raw,
        new[]
        {
            new CircinusProfilerSidecar(
                "patch-method",
                CircinusRowKind.Patch,
                "p1",
                patchTargetCount,
                patchHandArmed,
                2,
                100,
                patchTimedCalls,
                false,
                20,
                null),
            new CircinusProfilerSidecar(
                "ordinary-method",
                CircinusRowKind.Method,
                "m1",
                0,
                true,
                0,
                20,
                20,
                false,
                20,
                null)
        });

    private static void AssertMetric(
        PerformanceNormalizedSample sample,
        string scope,
        string key,
        string name,
        double value,
        string unit,
        string denominator,
        string claim = "gross-attribution")
    {
        var metric = sample.Metrics.Single(item => item.Scope == scope && item.Key == key && item.Name == name);
        Assert.Multiple(() =>
        {
            Assert.That(metric.Value, Is.EqualTo(value).Within(0.000001));
            Assert.That(metric.Unit, Is.EqualTo(unit));
            Assert.That(metric.Denominator, Is.EqualTo(denominator));
            Assert.That(metric.Claim, Is.EqualTo(claim));
        });
    }

    private static string NativeJson() =>
        "{" +
        "\"id\":\"run-1\",\"schemaMajor\":1,\"schemaMinor\":15," +
        "\"incomplete\":false,\"errorsDropped\":0,\"patchesDropped\":0," +
        "\"patchesDroppedMs\":0," +
        "\"env\":{\"profilerCycles\":20,\"profilerWindowMs\":200,\"profilerWindowTicks\":1200," +
        "\"profilerDutyPct\":20,\"profilerSampled\":true,\"profilerDisarmedBelowFloor\":0}," +
        "\"samples\":[{\"t\":1200,\"rt\":20,\"tps\":58,\"tgt\":60,\"fps\":120," +
        "\"fmn\":8.5,\"fmx\":15,\"f95\":12,\"tmn\":9,\"tmx\":18,\"heap\":512000,\"pw\":36}]," +
        "\"markers\":[{\"label\":\"gateway.sample-start\",\"tick\":0,\"realtime\":15," +
        "\"kind\":\"v1\"},{\"label\":\"gateway.sample-end\",\"tick\":1200," +
        "\"realtime\":20,\"kind\":\"v1\"}]," +
        "\"methods\":[{\"method\":{\"key\":\"m1\"},\"totalMs\":4,\"meanMs\":0.2," +
        "\"maxMs\":1,\"calls\":20,\"samples\":20}]," +
        "\"patches\":[{\"patch\":{\"key\":\"p1\",\"canSkip\":true},\"totalMs\":12," +
        "\"meanMs\":0.6,\"maxObservedMs\":2,\"calls\":100,\"timedCalls\":25," +
        "\"ambiguousTargets\":true}]," +
        "\"modCosts\":[{\"packageId\":\"product.mod\",\"totalMs\":16,\"sharedMs\":12," +
        "\"replacementMs\":4,\"patchCount\":1,\"methodCount\":1,\"errorCount\":0}]}";

    private static string DisabledNativeJson() => NativeJson()
        .Replace("\"profilerCycles\":20", "\"profilerCycles\":0")
        .Replace("\"profilerWindowMs\":200", "\"profilerWindowMs\":0")
        .Replace("\"profilerWindowTicks\":1200", "\"profilerWindowTicks\":0")
        .Replace("\"profilerDutyPct\":20", "\"profilerDutyPct\":0")
        .Replace("\"profilerSampled\":true", "\"profilerSampled\":false");
}

internal static class PerformanceNormalizationStringExtensions
{
    public static string ReplaceBetween(this string value, string prefix, string suffix, string replacement)
    {
        var start = value.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length;
        var end = value.IndexOf(suffix, start, StringComparison.Ordinal);
        if (start < prefix.Length || end < start) throw new InvalidOperationException("Fixture boundary missing.");
        return value.Substring(0, start) + replacement + value.Substring(end);
    }
}
