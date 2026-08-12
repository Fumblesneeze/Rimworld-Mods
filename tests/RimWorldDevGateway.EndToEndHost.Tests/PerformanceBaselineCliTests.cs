using System;
using System.IO;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace RimWorldDevGateway.EndToEndHost.Tests;

[TestFixture]
public sealed class PerformanceBaselineCliTests
{
    [Test]
    public void Explicit_candidate_creation_writes_review_file_and_refuses_overwrite()
    {
        using var fixture = new Fixture();
        var candidate = Path.Combine(fixture.Root, "candidate.json");

        var first = Invoke(
            "performance-baseline", "--current-snapshot", fixture.CurrentPath,
            "--candidate-output", candidate, "--output", "json");
        var second = Invoke(
            "performance-baseline", "--current-snapshot", fixture.CurrentPath,
            "--candidate-output", candidate, "--output", "json");

        Assert.Multiple(() =>
        {
            Assert.That(first.ExitCode, Is.Zero, first.Error);
            Assert.That(first.Output, Does.Contain("\"status\": \"candidate-created\""));
            Assert.That(File.ReadAllText(candidate), Does.Contain("\"status\": \"candidate\""));
            Assert.That(second.ExitCode, Is.EqualTo(1));
            Assert.That(second.Error, Does.Contain("already exists"));
        });
    }

    [Test]
    public void Compatible_comparison_persists_machine_readable_failure_and_returns_nonzero()
    {
        using var fixture = new Fixture(currentValue: 112d, baselineValue: 100d);
        var report = Path.Combine(fixture.Root, "comparison.json");

        var run = Invoke(
            "performance-baseline", "--current-snapshot", fixture.CurrentPath,
            "--baseline-directory", fixture.BaselineDirectory,
            "--policy", fixture.PolicyPath,
            "--report", report,
            "--output", "json");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.Output, Does.Contain("\"status\": \"regression\""));
            Assert.That(File.ReadAllText(report),
                Does.Contain("threshold-exceeded").And.Contain("Example:Example.Tick()"));
        });
    }

    [Test]
    public void Explicit_cross_version_comparison_returns_informational_status_and_metric_deltas()
    {
        using var fixture = new Fixture(currentValue: 120d, baselineValue: 100d);
        var current = JsonSerializer.Deserialize<PerformanceBaselineSnapshot>(
            File.ReadAllText(fixture.CurrentPath), Fixture.Options())!;
        current.Cases[0].Compatibility.GameVersion = "1.7.0";
        File.WriteAllText(fixture.CurrentPath, JsonSerializer.Serialize(current, Fixture.Options()), new UTF8Encoding(false));
        var report = Path.Combine(fixture.Root, "informational.json");

        var run = Invoke(
            "performance-baseline", "--current-snapshot", fixture.CurrentPath,
            "--baseline-directory", fixture.BaselineDirectory,
            "--policy", fixture.PolicyPath,
            "--report", report, "--informational-cross-version", "--output", "json");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.Error);
            Assert.That(run.Output, Does.Contain("\"status\": \"informational\""));
            Assert.That(File.ReadAllText(report),
                Does.Contain("informational-cross-version").And.Contain("absoluteDelta").And.Contain("20"));
        });
    }

    private static Invocation Invoke(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = EndToEndHostCli.Invoke(args, output, error);
        return new Invocation(exitCode, output.ToString(), error.ToString());
    }

    private sealed class Fixture : IDisposable
    {
        public Fixture(double currentValue = 100d, double baselineValue = 100d)
        {
            Root = Path.Combine(Path.GetTempPath(), nameof(PerformanceBaselineCliTests), Guid.NewGuid().ToString("N"));
            BaselineDirectory = Path.Combine(Root, "baselines");
            Directory.CreateDirectory(BaselineDirectory);
            CurrentPath = Path.Combine(Root, "current.json");
            PolicyPath = Path.Combine(Root, "policy.json");
            Write(CurrentPath, Snapshot("current", currentValue));
            Write(Path.Combine(BaselineDirectory, "gateway.accepted.json"), Snapshot("accepted", baselineValue));
            Write(PolicyPath, new PerformanceThresholdPolicy
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
            });
        }

        public string Root { get; }
        public string BaselineDirectory { get; }
        public string CurrentPath { get; }
        public string PolicyPath { get; }

        public void Dispose() => Directory.Delete(Root, true);

        private static void Write<T>(string path, T value) =>
            File.WriteAllText(path, JsonSerializer.Serialize(value, Options()), new UTF8Encoding(false));

        private static PerformanceBaselineSnapshot Snapshot(string status, double value) => new()
        {
            SchemaVersion = 1,
            Status = status,
            CreatedUtc = "2026-08-11T20:00:00Z",
            Cases =
            [
                new PerformanceBaselineCase
                {
                    Compatibility = new PerformanceCompatibilityIdentity
                    {
                        BenchmarkId = "gateway.calibration.instrumented", GroupId = "gateway.calibration",
                        WorkloadVersion = "workload/v1", EvidenceLens = "instrumented-compatible",
                        ActivePackageIds = ["brrainz.harmony", "ludeon.rimworld", "astryl.circinus", "fumblesneeze.rimworlddevgateway"],
                        GameVersion = "1.6.4871", ProductAssemblyIdentity = "product-sha",
                        TestAssemblyIdentity = "tests-sha", CircinusAssemblyIdentity = "circinus-sha",
                        CircinusSchemaIdentity = "1.15", ProfilingPolicyIdentity = "hand-armed/v1",
                        SamplingPolicyIdentity = "native-adaptive/v1", HardwareRuntimeFingerprint = "hardware-sha",
                        FixtureManifestSha256 = "fixture-manifest-sha",
                        FixtureTerminalManifestSha256 = "not-applicable",
                        DeterministicSeed = 60161, WarmUpTicks = 300, SampleTicks = 3000, GameSpeed = 3,
                        RepetitionCount = 1, AggregationPolicyIdentity = "arithmetic-mean/v1"
                    },
                    Measurements =
                    [
                        new PerformanceMetricMeasurement
                        {
                            Scope = "method", Selector = "Example.Tick()", MetricName = "exclusive-time",
                            Value = value, Unit = "ms", Denominator = "recorded-profiler-cycle",
                            Claim = "gross-attribution", Calls = 1200, TimedCalls = 300,
                            DutyPercent = 25d, SampleShift = 2, RecordedCycles = 100,
                            ProfilerWindowMilliseconds = 4000, ProfilerWindowTicks = 3000,
                            SamplingContextIdentity = "cycles=100|windowMs=4000|windowTicks=3000|duty=25|shift=2|calls=1200|timed=300",
                            ExactMethod = "Example:Example.Tick()"
                        }
                    ]
                }
            ]
        };

        public static JsonSerializerOptions Options() => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    private sealed record Invocation(int ExitCode, string Output, string Error);
}
