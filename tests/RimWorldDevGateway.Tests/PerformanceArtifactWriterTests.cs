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
        var padding = new string('a', 180 - prefix.Length - 1);
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
