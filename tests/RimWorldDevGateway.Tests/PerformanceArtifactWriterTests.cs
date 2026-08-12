using System.IO;
using System.Runtime.Serialization.Json;
using NUnit.Framework;
using RimWorldDevGateway.Performance;
using RimWorldDevGateway.PerformanceTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class PerformanceArtifactWriterTests
{
    [Test]
    public void Writer_keeps_transaction_sibling_short_enough_for_a_long_isolated_root()
    {
        var prefix = Path.Combine(Path.GetTempPath(), "rdg-performance-long-root");
        // Matches the deeply nested isolated SavedData/DevGateway/Sessions root used by
        // the real performance launcher closely enough to cross legacy MAX_PATH when a
        // full GUID transaction sibling is appended.
        var padding = new string('a', 198 - prefix.Length - 1);
        var root = Path.Combine(prefix, padding);
        Directory.CreateDirectory(root);
        try
        {
            var capture = Capture();

            var artifacts = PerformanceArtifactWriter.Write(
                root, "gateway.long-path.fixture", capture, Normalized(capture));

            Assert.That(new[]
            {
                artifacts["performance.raw.in_memory"],
                artifacts["performance.raw.persisted"],
                artifacts["performance.normalized"]
            }, Has.All.Matches<string>(File.Exists));
        }
        finally
        {
            if (Directory.Exists(prefix)) Directory.Delete(prefix, recursive: true);
        }
    }

    [Test]
    public void Writer_preserves_both_raw_documents_and_publishes_one_complete_normalized_directory()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "performance-artifacts", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var capture = Capture();
            var normalized = Normalized(capture);

            var artifacts = PerformanceArtifactWriter.Write(root, "gateway.fixture", capture, normalized);

            Assert.Multiple(() =>
            {
                Assert.That(File.ReadAllText(artifacts["performance.raw.in_memory"]),
                    Is.EqualTo(capture.InMemoryJson));
                Assert.That(File.ReadAllText(artifacts["performance.raw.persisted"]),
                    Is.EqualTo(capture.PersistedJson));
                Assert.That(Directory.EnumerateDirectories(root), Has.Exactly(1).Items);
                Assert.That(Directory.EnumerateDirectories(root, "*.tmp.*"), Is.Empty);
                Assert.That(Directory.EnumerateDirectories(root, "t-*"), Is.Empty);
            });
            using var stream = File.OpenRead(artifacts["performance.normalized"]);
            var roundTrip = (PerformanceNormalizedSample)new DataContractJsonSerializer(
                typeof(PerformanceNormalizedSample)).ReadObject(stream)!;
            Assert.Multiple(() =>
            {
                Assert.That(roundTrip.RunId, Is.EqualTo("run-exact"));
                Assert.That(roundTrip.WorkloadVersion, Is.EqualTo("workload/v1"));
                Assert.That(roundTrip.RawCircinusJson, Is.Null);
                Assert.That(roundTrip.Samples.Single().TargetTps, Is.EqualTo(60));
                Assert.That(roundTrip.Checkpoints.Single().Value, Is.EqualTo(60));
            });
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Dpa_writer_publishes_only_one_raw_nonbaseline_diagnostic_document()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "dpa-diagnostic-artifacts", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var capture = new DpaDiagnosticCapture(
                new DpaAssemblyIdentity("PerformanceAnalyzer, Version=1.0.0.0", Guid.NewGuid(), 42, "ABC", "fixture"),
                "workload/v1",
                "Tick",
                new[] { "Verse.Map::MapPreTick()" },
                new[] { "Assembly:Verse.Map::MapPreTick()@mvid:1" },
                new[] { "sample-start-tick=10", "sample-end-tick=70" },
                new[]
                {
                    new DpaDiagnosticEntry(
                        "inner", "inner", "Verse.Map", "MapPreTick", 1, false,
                        new[] { new DpaDiagnosticSample(0, 60, 1.25) })
                });

            var artifacts = DpaDiagnosticArtifactWriter.Write(root, "gateway.fixture", capture);

            Assert.Multiple(() =>
            {
                Assert.That(artifacts, Has.Count.EqualTo(3));
                Assert.That(artifacts["performance.diagnostic.profiler"], Is.EqualTo("dpa"));
                Assert.That(artifacts["performance.diagnostic.baseline_eligible"], Is.EqualTo("false"));
                Assert.That(File.Exists(artifacts["performance.diagnostic.raw"]), Is.True);
                Assert.That(Directory.EnumerateFiles(root, "performance.normalized.json", SearchOption.AllDirectories), Is.Empty);
                Assert.That(Directory.EnumerateFiles(root, "circinus.*.json", SearchOption.AllDirectories), Is.Empty);
                Assert.That(Directory.EnumerateDirectories(root, ".tmp-*", SearchOption.TopDirectoryOnly), Is.Empty);
                Assert.That(Directory.EnumerateDirectories(root, "t-*", SearchOption.TopDirectoryOnly), Is.Empty);
            });
            using var stream = File.OpenRead(artifacts["performance.diagnostic.raw"]);
            var roundTrip = (DpaDiagnosticCapture)new DataContractJsonSerializer(
                typeof(DpaDiagnosticCapture)).ReadObject(stream)!;
            Assert.Multiple(() =>
            {
                Assert.That(roundTrip.Kind, Is.EqualTo("diagnostic"));
                Assert.That(roundTrip.Profiler, Is.EqualTo("dpa"));
                Assert.That(roundTrip.BaselineEligible, Is.False);
                Assert.That(roundTrip.RequestedSelectors, Is.EqualTo(new[] { "Verse.Map::MapPreTick()" }));
                Assert.That(roundTrip.Selectors, Is.EqualTo(new[] { "Assembly:Verse.Map::MapPreTick()@mvid:1" }));
                Assert.That(roundTrip.Entries, Has.Length.EqualTo(1));
            });
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static CircinusCapture Capture() => new(
        "run-exact", 1, 15, "{\"source\":\"memory\"}", "{\"source\":\"persisted\"}",
        Array.Empty<CircinusProfilerSidecar>());

    private static PerformanceNormalizedSample Normalized(CircinusCapture capture) => new(
        capture.RunId,
        capture.InMemoryJson,
        "workload/v1",
        PerformanceEvidenceLens.FullyDisarmed,
        "fully-disarmed",
        new PerformanceProfilerPolicy(0, 0, 0, 0, false, 0),
        Array.Empty<PerformanceNormalizedMetric>(),
        new[] { new PerformanceNativeSample { GameTick = 1, TargetTps = 60 } },
        new[] { new PerformanceControlCheckpoint("elapsed-game-ticks", 60, "ticks") },
        Array.Empty<CircinusProfilerSidecar>());
}
