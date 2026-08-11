using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RimWorldDevGateway.EndToEndHost;

namespace RimWorldDevGateway.EndToEndHost.Tests;

[TestFixture]
public sealed class PerformanceMetadataDiscoveryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string Configuration =
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    [Test]
    public void Reader_extracts_performance_metadata_without_loading_the_fixture()
    {
        var metadata = PerformanceAssemblyMetadataReader.Read(FixtureAssembly());
        var declaration = metadata.Declarations.Single(item => item.Id == "alpha.base");

        Assert.Multiple(() =>
        {
            Assert.That(metadata.AssemblyName, Is.EqualTo("PerformanceHost.ValidFixtures"));
            Assert.That(metadata.ModuleVersionId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(metadata.Sha256, Has.Length.EqualTo(64));
            Assert.That(declaration.TypeName, Is.EqualTo("PerformanceHost.ValidFixtures.BaseBenchmark"));
            Assert.That(declaration.StagingOwnerPackageId, Is.EqualTo("alpha.mod"));
            Assert.That(declaration.MeasuredSubjectPackageId, Is.EqualTo("alpha.mod"));
            Assert.That(declaration.ActivePackageIds, Is.EqualTo(new[]
            {
                "brrainz.harmony",
                "ludeon.rimworld",
                "astryl.circinus",
                "alpha.mod"
            }));
            Assert.That(declaration.DeterministicSeed, Is.EqualTo(7123));
            Assert.That(declaration.WorkloadVersion, Is.EqualTo("alpha/v2"));
            Assert.That(declaration.WarmUpTicks, Is.EqualTo(900));
            Assert.That(declaration.SampleTicks, Is.EqualTo(3600));
            Assert.That(declaration.GameSpeed, Is.EqualTo(2));
            Assert.That(declaration.Repetitions, Is.EqualTo(2));
            Assert.That(declaration.EvidenceLens, Is.EqualTo(1));
            Assert.That(declaration.ProductAbsentControlId, Is.EqualTo("gateway.alpha-control"));
            Assert.That(declaration.MethodSelectors.Select(item => item.Value), Is.EqualTo(new[]
            {
                "alpha.harmony",
                "Alpha.Work::Tick"
            }));
            Assert.That(declaration.ThroughputCheckpoints.Select(item => item.Id), Is.EqualTo(new[]
            {
                "meals",
                "washes"
            }));
            Assert.That(declaration.ImplementsContract, Is.True);
            Assert.That(declaration.IsConcrete, Is.True);
            Assert.That(declaration.HasPublicParameterlessConstructor, Is.True);
        });
    }

    [Test]
    public void Validator_groups_only_exact_declared_package_sequences()
    {
        var metadata = PerformanceAssemblyMetadataReader.Read(FixtureAssembly());
        var result = PerformanceDiscoveryValidator.ValidateAndGroup(
            new[]
            {
                new PerformanceAssemblyCandidate(
                    FixtureProject(),
                    "alpha.mod",
                    "PerformanceHost.ValidFixtures",
                    metadata)
            },
            new[]
            {
                "brrainz.harmony",
                "ludeon.rimworld",
                "astryl.circinus",
                "optional.mod",
                "alpha.mod",
                "downloaded.but.inactive"
            },
            requireResolvedControls: false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Groups, Has.Count.EqualTo(2));
            Assert.That(result.Groups.SelectMany(group => group.Benchmarks).Select(item => item.Id),
                Is.EquivalentTo(new[]
                {
                    "alpha.base",
                    "alpha.base-disarmed",
                    "alpha.base-instrumented",
                    "alpha.optional",
                    "alpha.optional-armed-disabled",
                    "alpha.optional-disarmed"
                }));
            Assert.That(result.Groups.SelectMany(group => group.ActivePackageIds),
                Does.Not.Contain("downloaded.but.inactive"));
            Assert.That(result.Groups.Select(group => group.GroupId), Is.Ordered);
        });
    }

    [Test]
    public void Validator_rejects_project_identity_or_unresolvable_package_drift_before_launch()
    {
        var metadata = PerformanceAssemblyMetadataReader.Read(FixtureAssembly());
        var error = Assert.Throws<EndToEndDiscoveryException>(() =>
            PerformanceDiscoveryValidator.ValidateAndGroup(
                new[]
                {
                    new PerformanceAssemblyCandidate(
                        FixtureProject(),
                        "wrong.owner",
                        "Wrong.Output",
                        metadata)
                },
                new[] { "ludeon.rimworld", "astryl.circinus", "alpha.mod" },
                requireResolvedControls: false));

        Assert.That(error!.Message,
            Does.Contain("project owner").And.Contain("assembly output").And.Contain("optional.mod"));
    }

    [Test]
    public void Validator_rejects_more_than_the_published_total_declaration_ceiling()
    {
        var metadata = PerformanceAssemblyMetadataReader.Read(FixtureAssembly());
        var candidates = Enumerable.Range(0, 171).Select(index =>
            new PerformanceAssemblyCandidate(
                $"fixture-{index}.csproj",
                "alpha.mod",
                "PerformanceHost.ValidFixtures",
                metadata));

        var error = Assert.Throws<EndToEndDiscoveryException>(() =>
            PerformanceDiscoveryValidator.ValidateAndGroup(
                candidates,
                new[]
                {
                    "brrainz.harmony",
                    "ludeon.rimworld",
                    "astryl.circinus",
                    "optional.mod",
                    "alpha.mod"
                },
                requireResolvedControls: false));

        Assert.That(error!.Message,
            Does.Contain("total performance declarations").And.Contain("1024"));
    }

    private static string FixtureAssembly() => Path.Combine(
        RepositoryRoot,
        "tests",
        "Fixtures",
        "PerformanceHost.ValidFixtures",
        "bin",
        Configuration,
        "net480",
        "PerformanceHost.ValidFixtures.dll");

    private static string FixtureProject() => Path.Combine(
        RepositoryRoot,
        "tests",
        "Fixtures",
        "PerformanceHost.ValidFixtures",
        "PerformanceHost.ValidFixtures.csproj");

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
}
