using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RimWorldDevGateway.EndToEndHost;

namespace RimWorldDevGateway.EndToEndHost.Tests;

[TestFixture]
public sealed class PerformanceMetadataDiscoveryTests
{
    internal static PerformanceAssemblyCandidate[] ValidCandidatesForPlanning()
    {
        var metadata = PerformanceAssemblyMetadataReader.Read(FixtureAssembly());
        return ValidCandidates(metadata);
    }

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
            Assert.That(declaration.WorkloadVersion, Is.EqualTo("alpha/v2"));
            Assert.That(declaration.WarmUpTicks, Is.EqualTo(900));
            Assert.That(declaration.SampleTicks, Is.EqualTo(3600));
            Assert.That(declaration.GameSpeed, Is.EqualTo(2));
            Assert.That(declaration.Repetitions, Is.EqualTo(2));
            Assert.That(declaration.EvidenceLens, Is.EqualTo(1));
            Assert.That(declaration.ProductAbsentControlId, Is.Null);
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
            Assert.That(declaration.ImplementsThroughputCounter, Is.True,
                "Inherited fixture counters must be discovered without loading the assembly.");
            Assert.That(declaration.IsConcrete, Is.True);
            Assert.That(declaration.HasPublicParameterlessConstructor, Is.True);
        });
    }

    [Test]
    public void Validator_rejects_checkpoint_metadata_without_the_public_counter_contract()
    {
        var metadata = PerformanceAssemblyMetadataReader.Read(FixtureAssembly());
        var original = metadata.Declarations.Single(item => item.Id == "alpha.base");
        var invalid = new PerformanceMetadataDeclaration(
            original.TypeName,
            original.Id,
            original.StagingOwnerPackageId,
            original.MeasuredSubjectPackageId,
            original.ActivePackageIds,
            original.WorkloadVersion,
            original.ComparisonId,
            original.WarmUpTicks,
            original.SampleTicks,
            original.GameSpeed,
            original.Repetitions,
            original.EvidenceLens,
            original.ProductAbsentControlId,
            original.MethodSelectors,
            original.ThroughputCheckpoints,
            original.IsConcrete,
            original.ImplementsContract,
            implementsThroughputCounter: false,
            hasPublicParameterlessConstructor: original.HasPublicParameterlessConstructor);
        var invalidMetadata = new PerformanceAssemblyMetadata(metadata.Assembly, new[] { invalid });

        var error = Assert.Throws<EndToEndDiscoveryException>(() =>
            PerformanceDiscoveryValidator.ValidateAndGroup(
                new[]
                {
                    new PerformanceAssemblyCandidate(
                        FixtureProject(),
                        "alpha.mod",
                        "PerformanceHost.ValidFixtures",
                        invalidMetadata)
                },
                PackageCatalog(),
                requireResolvedControls: false));

        Assert.That(error!.Message,
            Does.Contain("IPerformanceThroughputCounter").And.Contain("alpha.base"));
    }

    [Test]
    public void Validator_groups_only_exact_declared_package_sequences()
    {
        var metadata = PerformanceAssemblyMetadataReader.Read(FixtureAssembly());
        var result = PerformanceDiscoveryValidator.ValidateAndGroup(
            ValidCandidates(metadata),
            PackageCatalog().Append("downloaded.but.inactive"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Groups, Has.Count.EqualTo(3));
            Assert.That(result.Groups.SelectMany(group => group.Benchmarks).Select(item => item.Id),
                Is.EquivalentTo(new[]
                {
                    "alpha.base",
                    "alpha.base-disarmed",
                    "alpha.base-instrumented",
                    "alpha.optional",
                    "alpha.optional-armed-disabled",
                    "alpha.optional-disarmed",
                    "gateway.alpha-present",
                    "gateway.alpha-present-armed-disabled",
                    "gateway.alpha-present-disarmed",
                    "gateway.alpha-absent"
                }));
            Assert.That(result.Groups.SelectMany(group => group.ActivePackageIds),
                Does.Not.Contain("downloaded.but.inactive"));
            Assert.That(result.Groups.Select(group => group.GroupId), Is.Ordered);
        });
    }

    [Test]
    public void Prevalidation_filter_keeps_an_unrelated_selected_group_independent_of_unavailable_products()
    {
        var selected = PerformanceDiscoveryValidator.SelectForFilters(
            ValidCandidatesForPlanning(),
            new[] { "gateway.alpha-absent" },
            Array.Empty<string>());

        var result = PerformanceDiscoveryValidator.ValidateAndGroup(
            selected,
            new[] { "brrainz.harmony", "ludeon.rimworld", "astryl.circinus" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Groups, Has.Count.EqualTo(1));
            Assert.That(result.Groups.Single().Benchmarks.Select(item => item.Id),
                Is.EqualTo(new[] { "gateway.alpha-absent" }));
            Assert.That(selected.SelectMany(candidate => candidate.Metadata.Declarations)
                    .Any(declaration => declaration.ActivePackageIds.Contains("alpha.mod")),
                Is.False);
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
                        metadata),
                    ControlCandidate()
                },
                PackageCatalog().Where(package => package != "optional.mod")));

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
                }));

        Assert.That(error!.Message,
            Does.Contain("total performance declarations").And.Contain("1024"));
    }

    [Test]
    public void Validator_rejects_an_absent_control_that_references_another_control()
    {
        var control = PerformanceAssemblyMetadataReader.Read(ControlAssembly());
        var original = control.Declarations.Single(item => item.Id == "gateway.alpha-absent");
        var invalid = new PerformanceMetadataDeclaration(
            original.TypeName,
            original.Id,
            original.StagingOwnerPackageId,
            original.MeasuredSubjectPackageId,
            original.ActivePackageIds,
            original.WorkloadVersion,
            original.ComparisonId,
            original.WarmUpTicks,
            original.SampleTicks,
            original.GameSpeed,
            original.Repetitions,
            original.EvidenceLens,
            "gateway.another-absent-control",
            original.MethodSelectors,
            original.ThroughputCheckpoints,
            original.IsConcrete,
            original.ImplementsContract,
            original.ImplementsThroughputCounter,
            original.HasPublicParameterlessConstructor);
        var invalidMetadata = new PerformanceAssemblyMetadata(control.Assembly, new[] { invalid });

        var error = Assert.Throws<EndToEndDiscoveryException>(() =>
            PerformanceDiscoveryValidator.ValidateAndGroup(
                new[]
                {
                    new PerformanceAssemblyCandidate(
                        ControlProject(),
                        "fumblesneeze.rimworlddevgateway",
                        "PerformanceHost.ControlFixtures",
                        invalidMetadata)
                },
                PackageCatalog(),
                requireResolvedControls: false));

        Assert.That(error!.Message, Does.Contain("product-absent control").And.Contain("must not reference"));
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

    private static PerformanceAssemblyCandidate[] ValidCandidates(PerformanceAssemblyMetadata metadata) =>
        new[]
        {
            new PerformanceAssemblyCandidate(
                FixtureProject(),
                "alpha.mod",
                "PerformanceHost.ValidFixtures",
                metadata),
            ControlCandidate()
        };

    private static PerformanceAssemblyCandidate ControlCandidate() =>
        new(
            ControlProject(),
            "fumblesneeze.rimworlddevgateway",
            "PerformanceHost.ControlFixtures",
            PerformanceAssemblyMetadataReader.Read(ControlAssembly()));

    private static string[] PackageCatalog() =>
        new[]
        {
            "brrainz.harmony",
            "ludeon.rimworld",
            "astryl.circinus",
            "optional.mod",
            "alpha.mod"
        };

    private static string ControlAssembly() => Path.Combine(
        RepositoryRoot,
        "tests",
        "Fixtures",
        "PerformanceHost.ControlFixtures",
        "bin",
        Configuration,
        "net48",
        "PerformanceHost.ControlFixtures.dll");

    private static string ControlProject() => Path.Combine(
        RepositoryRoot,
        "tests",
        "Fixtures",
        "PerformanceHost.ControlFixtures",
        "PerformanceHost.ControlFixtures.csproj");

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "RimWorldMods.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }
}
