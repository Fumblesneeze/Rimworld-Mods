using System.IO;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndManifestSourceTests
{
    [Test]
    public void Cursor_emits_complete_active_order_before_scanning_one_entry_at_a_time()
    {
        using var stage = new ManifestStage();
        var mods = new RecordingActiveModSource(
            new GatewayEndToEndActiveMod(
                "ludeon.rimworld",
                stage.CoreRoot,
                new[] { stage.CoreVersion }),
            new GatewayEndToEndActiveMod(
                "alpha.mod",
                stage.AlphaRoot,
                new[] { stage.AlphaVersion, stage.AlphaFallback }),
            new GatewayEndToEndActiveMod(
                "fumblesneeze.rimworlddevgateway",
                stage.GatewayRoot,
                new[] { stage.GatewayVersion }));
        using var cursor = new GatewayEndToEndResolvedManifestCursor(mods);
        var steps = new List<GatewayEndToEndManifestDiscoveryStep>();

        for (var index = 0; index < 64; index++)
        {
            var beforeReads = mods.ReadCount;
            var step = cursor.Advance();
            steps.Add(step);
            Assert.That(mods.ReadCount - beforeReads, Is.LessThanOrEqualTo(1));
            if (step.Kind == GatewayEndToEndManifestDiscoveryStepKind.Complete)
            {
                break;
            }
        }

        var active = steps
            .Where(step => step.Kind == GatewayEndToEndManifestDiscoveryStepKind.ActivePackage)
            .Select(step => step.ActivePackageId)
            .ToArray();
        var candidates = steps
            .Where(step => step.Kind == GatewayEndToEndManifestDiscoveryStepKind.Candidate)
            .Select(step => step.ManifestCandidate!)
            .ToArray();
        var firstCandidate = steps.FindIndex(
            step => step.Kind == GatewayEndToEndManifestDiscoveryStepKind.Candidate);
        var lastActive = steps.FindLastIndex(
            step => step.Kind == GatewayEndToEndManifestDiscoveryStepKind.ActivePackage);

        Assert.Multiple(() =>
        {
            Assert.That(steps.Last().Kind, Is.EqualTo(GatewayEndToEndManifestDiscoveryStepKind.Complete));
            Assert.That(active, Is.EqualTo(new[]
            {
                "ludeon.rimworld",
                "alpha.mod",
                "fumblesneeze.rimworlddevgateway"
            }));
            Assert.That(firstCandidate, Is.GreaterThan(lastActive));
            Assert.That(candidates.Select(candidate => Path.GetFileName(candidate.ManifestPath)),
                Is.EqualTo(new[] { "Alpha.e2etests.json", "Beta.e2etests.json" }));
            Assert.That(candidates[0].ManifestPath, Does.StartWith(stage.AlphaVersion));
        });
    }

    [Test]
    public void Active_mod_reader_failure_is_fixed_text_and_does_not_hide_later_mods()
    {
        using var stage = new ManifestStage();
        var mods = new ThrowingActiveModSource(
            new InvalidOperationException("SECRET_FROM_HOSTILE_MOD"),
            new GatewayEndToEndActiveMod(
                "alpha.mod",
                stage.AlphaRoot,
                new[] { stage.AlphaVersion }));
        using var cursor = new GatewayEndToEndResolvedManifestCursor(mods);
        var steps = new List<GatewayEndToEndManifestDiscoveryStep>();

        for (var index = 0; index < 32; index++)
        {
            var step = cursor.Advance();
            steps.Add(step);
            if (step.Kind == GatewayEndToEndManifestDiscoveryStepKind.Complete)
            {
                break;
            }
        }

        var failure = steps.Single(step => step.Kind == GatewayEndToEndManifestDiscoveryStepKind.Failure)
            .FailureSnapshot!;
        Assert.Multiple(() =>
        {
            Assert.That(failure.Code, Is.EqualTo("active_mod_read_failed"));
            Assert.That(failure.Message, Does.Not.Contain("SECRET_FROM_HOSTILE_MOD"));
            Assert.That(steps.Any(step => step.ActivePackageId == "alpha.mod"), Is.True);
            Assert.That(steps.Any(step => step.ManifestCandidate is not null), Is.True);
        });
    }

    private sealed class RecordingActiveModSource : IGatewayEndToEndActiveModSource
    {
        private readonly GatewayEndToEndActiveMod[] mods;

        public RecordingActiveModSource(params GatewayEndToEndActiveMod[] mods) => this.mods = mods;

        public int Count => mods.Length;

        public int ReadCount { get; private set; }

        public GatewayEndToEndActiveMod Read(int index)
        {
            ReadCount++;
            return mods[index];
        }
    }

    private sealed class ThrowingActiveModSource : IGatewayEndToEndActiveModSource
    {
        private readonly Exception exception;
        private readonly GatewayEndToEndActiveMod healthy;

        public ThrowingActiveModSource(Exception exception, GatewayEndToEndActiveMod healthy)
        {
            this.exception = exception;
            this.healthy = healthy;
        }

        public int Count => 2;

        public GatewayEndToEndActiveMod Read(int index) => index == 0 ? throw exception : healthy;
    }

    private sealed class ManifestStage : IDisposable
    {
        public ManifestStage()
        {
            Root = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                "e2e-source",
                Guid.NewGuid().ToString("N"));
            CoreRoot = CreateMod("ludeon.rimworld", out var coreVersion, out _);
            CoreVersion = coreVersion;
            AlphaRoot = CreateMod("alpha.mod", out var alphaVersion, out var alphaFallback);
            AlphaVersion = alphaVersion;
            AlphaFallback = alphaFallback;
            GatewayRoot = CreateMod("fumblesneeze.rimworlddevgateway", out var gatewayVersion, out _);
            GatewayVersion = gatewayVersion;

            WriteManifest(AlphaVersion, "Alpha.e2etests.json");
            WriteManifest(AlphaFallback, "Alpha.e2etests.json");
            WriteManifest(AlphaFallback, "Beta.e2etests.json");
            Directory.CreateDirectory(Path.Combine(AlphaVersion, "DevEndToEndTests", "ignored-directory"));
            File.WriteAllText(Path.Combine(AlphaVersion, "DevEndToEndTests", "ignored.txt"), "ignored");
        }

        public string Root { get; }
        public string CoreRoot { get; }
        public string CoreVersion { get; }
        public string AlphaRoot { get; }
        public string AlphaVersion { get; }
        public string AlphaFallback { get; }
        public string GatewayRoot { get; }
        public string GatewayVersion { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }

        private string CreateMod(string packageId, out string version, out string fallback)
        {
            var root = Path.Combine(Root, packageId);
            version = Path.Combine(root, "1.6");
            fallback = Path.Combine(root, "Common");
            Directory.CreateDirectory(version);
            Directory.CreateDirectory(fallback);
            return root;
        }

        private static void WriteManifest(string version, string fileName)
        {
            var directory = Path.Combine(version, "DevEndToEndTests");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, fileName), "{}");
        }
    }
}
