using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RimWorldDevGateway.Contracts;
using RimWorldDevGateway.EndToEndHost;

namespace RimWorldDevGateway.EndToEndHost.Tests;

[TestFixture]
public sealed class EndToEndBundleStagingTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string Configuration =
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    [Test]
    public void Planner_emits_a_deterministic_exact_manifest_and_owner_stage()
    {
        var candidate = ValidCandidate();

        var first = EndToEndBundlePlanner.Create(
            new[] { candidate },
            ResolvablePackages,
            Path.Combine(RepositoryRoot, "artifacts", "E2EPlannerTests"),
            "1.6");
        var second = EndToEndBundlePlanner.Create(
            new[] { candidate },
            ResolvablePackages,
            Path.Combine(RepositoryRoot, "artifacts", "E2EPlannerTests"),
            "1.6");
        var stage = first.OwnerStages.Single();
        var bundle = stage.Bundles.Single();
        var manifest = GatewayContractJson.Read<EndToEndBundleManifest>(bundle.ManifestJson);

        Assert.Multiple(() =>
        {
            Assert.That(stage.OwnerPackageId, Is.EqualTo("alpha.mod"));
            Assert.That(stage.DestinationDirectory, Does.EndWith(
                Path.Combine("alpha.mod", "1.6", "DevEndToEndTests")));
            Assert.That(bundle.ManifestJson, Is.EqualTo(second.OwnerStages.Single().Bundles.Single().ManifestJson));
            Assert.That(manifest.Schema, Is.EqualTo(EndToEndBundleManifest.SchemaValue));
            Assert.That(manifest.OwnerPackageId, Is.EqualTo("alpha.mod"));
            Assert.That(manifest.Assembly, Is.EqualTo("EndToEndHost.ValidFixtures.dll"));
            Assert.That(manifest.AssemblyIdentity, Is.EqualTo(candidate.Metadata.AssemblyIdentity));
            Assert.That(manifest.ModuleVersionId, Is.EqualTo(candidate.Metadata.ModuleVersionId.ToString("D")));
            Assert.That(manifest.AssemblyLength, Is.EqualTo(candidate.Metadata.Length));
            Assert.That(manifest.AssemblySha256, Is.EqualTo(candidate.Metadata.Sha256));
            Assert.That(manifest.Dependencies.Select(reference => reference.Identity),
                Does.Contain(candidate.Metadata.AssemblyReferences.Single(
                    reference => reference.Name == "RimWorldDevGateway.EndToEndTesting").Identity));
            Assert.That(manifest.Tests.Select(test => test.Id), Is.EqualTo(new[]
            {
                "alpha.base-a",
                "alpha.base-b",
                "alpha.optional"
            }));
        });
    }

    [Test]
    public void Publisher_writes_an_owned_atomic_stage_and_matching_cleanup_removes_it()
    {
        using var sandbox = new StageSandbox();
        var stage = Plan(sandbox.Root);
        var publisher = new EndToEndStagePublisher();

        var lease = publisher.Publish(stage);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(stage.DestinationDirectory, "EndToEndHost.ValidFixtures.dll")), Is.True);
            Assert.That(File.Exists(Path.Combine(stage.DestinationDirectory, "EndToEndHost.ValidFixtures.e2etests.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(stage.DestinationDirectory, EndToEndStageMarker.FileName)), Is.True);
            Assert.That(Directory.GetDirectories(Path.GetDirectoryName(stage.DestinationDirectory)!, ".DevEndToEndTests.*"), Is.Empty);
        });

        Assert.That(publisher.TryCleanup(lease), Is.True);
        Assert.That(Directory.Exists(stage.DestinationDirectory), Is.False);
    }

    [Test]
    public void Publisher_refuses_to_replace_an_unmarked_foreign_directory()
    {
        using var sandbox = new StageSandbox();
        var stage = Plan(sandbox.Root);
        Directory.CreateDirectory(stage.DestinationDirectory);
        var foreign = Path.Combine(stage.DestinationDirectory, "foreign.txt");
        File.WriteAllText(foreign, "keep");

        var error = Assert.Throws<EndToEndStageException>(() => new EndToEndStagePublisher().Publish(stage));

        Assert.Multiple(() =>
        {
            Assert.That(error!.Message, Does.Contain("marker").And.Contain("refuse"));
            Assert.That(File.ReadAllText(foreign), Is.EqualTo("keep"));
        });
    }

    [TestCase("after-backup")]
    [TestCase("after-commit")]
    public void Publisher_restores_the_previous_owned_stage_when_commit_fails(string faultPoint)
    {
        using var sandbox = new StageSandbox();
        var stage = Plan(sandbox.Root);
        var originalPublisher = new EndToEndStagePublisher();
        originalPublisher.Publish(stage);
        var sentinel = Path.Combine(stage.DestinationDirectory, "previous-stage.txt");
        File.WriteAllText(sentinel, "previous");
        var publisher = new EndToEndStagePublisher(new ThrowAtFaultPoint(faultPoint));

        Assert.Throws<EndToEndStageException>(() => publisher.Publish(stage));

        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllText(sentinel), Is.EqualTo("previous"));
            Assert.That(Directory.GetDirectories(Path.GetDirectoryName(stage.DestinationDirectory)!, ".DevEndToEndTests.*"), Is.Empty);
        });
    }

    [Test]
    public void Cleanup_refuses_a_stage_whose_transaction_marker_changed()
    {
        using var sandbox = new StageSandbox();
        var stage = Plan(sandbox.Root);
        var publisher = new EndToEndStagePublisher();
        var lease = publisher.Publish(stage);
        var markerPath = Path.Combine(stage.DestinationDirectory, EndToEndStageMarker.FileName);
        var marker = GatewayContractJson.Read<EndToEndStageMarker>(File.ReadAllText(markerPath));
        marker.TransactionId = Guid.NewGuid().ToString("N");
        File.WriteAllText(markerPath, GatewayContractJson.Write(marker));

        Assert.That(publisher.TryCleanup(lease), Is.False);
        Assert.That(Directory.Exists(stage.DestinationDirectory), Is.True);
    }

    [TestCase("..")]
    [TestCase("../outside")]
    [TestCase("alpha/mod")]
    [TestCase("alpha\\mod")]
    public void Stage_paths_reject_escaping_or_composite_owner_segments(string ownerPackageId)
    {
        using var sandbox = new StageSandbox();

        Assert.Throws<EndToEndStageException>(() =>
            EndToEndStagePaths.GetDestination(sandbox.Root, ownerPackageId, "1.6"));
    }

    private static readonly string[] ResolvablePackages =
    {
        "ludeon.rimworld",
        "brrainz.harmony",
        "alpha.mod",
        "optional.mod"
    };

    private static EndToEndOwnerStagePlan Plan(string modsRoot) => EndToEndBundlePlanner.Create(
        new[] { ValidCandidate() },
        ResolvablePackages,
        modsRoot,
        "1.6").OwnerStages.Single();

    private static EndToEndAssemblyCandidate ValidCandidate()
    {
        var project = Path.Combine(
            RepositoryRoot,
            "tests",
            "Fixtures",
            "EndToEndHost.ValidFixtures",
            "EndToEndHost.ValidFixtures.csproj");
        var assembly = Path.Combine(
            Path.GetDirectoryName(project)!,
            "bin",
            Configuration,
            "net480",
            "EndToEndHost.ValidFixtures.dll");
        return new EndToEndAssemblyCandidate(
            project,
            "alpha.mod",
            "EndToEndHost.ValidFixtures",
            EndToEndAssemblyMetadataReader.Read(assembly));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ImmersiveChefs.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }

    private sealed class ThrowAtFaultPoint : IEndToEndStageFaultInjector
    {
        private readonly string expectedPoint;

        public ThrowAtFaultPoint(string expectedPoint)
        {
            this.expectedPoint = expectedPoint;
        }

        public void OnFaultPoint(string point)
        {
            if (StringComparer.Ordinal.Equals(point, expectedPoint))
            {
                throw new IOException("Injected stage commit failure.");
            }
        }
    }

    private sealed class StageSandbox : IDisposable
    {
        public StageSandbox()
        {
            Root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "stage-sandbox", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
