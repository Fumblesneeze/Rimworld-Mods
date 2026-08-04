using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RimWorldDevGateway.EndToEndHost;

namespace RimWorldDevGateway.EndToEndHost.Tests;

[TestFixture]
public sealed class EndToEndMetadataDiscoveryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string Configuration =
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    [Test]
    public void Reader_extracts_compiled_attributes_without_executing_the_test_type()
    {
        var metadata = EndToEndAssemblyMetadataReader.Read(FixtureAssembly("EndToEndHost.ValidFixtures"));
        var declaration = metadata.Declarations.Single(item => item.Id == "alpha.base-a");
        var contractReference = metadata.AssemblyReferences.Single(
            reference => reference.Name == "RimWorldDevGateway.EndToEndTesting");

        Assert.Multiple(() =>
        {
            Assert.That(metadata.AssemblyName, Is.EqualTo("EndToEndHost.ValidFixtures"));
            Assert.That(metadata.AssemblyIdentity, Does.StartWith("EndToEndHost.ValidFixtures, Version="));
            Assert.That(metadata.AssemblyCulture, Is.EqualTo("neutral"));
            Assert.That(metadata.AssemblyPublicKey, Is.EqualTo("null"));
            Assert.That(contractReference.Identity, Does.StartWith("RimWorldDevGateway.EndToEndTesting, Version="));
            Assert.That(contractReference.Culture, Is.EqualTo("neutral"));
            Assert.That(metadata.ModuleVersionId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(metadata.Sha256, Has.Length.EqualTo(64));
            Assert.That(declaration.TypeName, Is.EqualTo("EndToEndHost.ValidFixtures.ExplosiveStaticConstructorTest"));
            Assert.That(declaration.OwnerPackageId, Is.EqualTo("alpha.mod"));
            Assert.That(declaration.ActivePackageIds, Is.EqualTo(new[]
            {
                "ludeon.rimworld",
                "brrainz.harmony",
                "alpha.mod"
            }));
            Assert.That(declaration.ImplementsContract, Is.True);
            Assert.That(declaration.IsConcrete, Is.True);
            Assert.That(declaration.HasPublicParameterlessConstructor, Is.True);
            Assert.That(declaration.MaxFrames, Is.EqualTo(300));
            Assert.That(declaration.MaxGameTicks, Is.EqualTo(1_200));
            Assert.That(declaration.MaxWallClockSeconds, Is.EqualTo(30));
        });
    }

    [Test]
    public void Reader_ignores_lookalike_contract_types_from_the_wrong_assembly()
    {
        var metadata = EndToEndAssemblyMetadataReader.Read(FixtureAssembly("EndToEndHost.LookalikeFixtures"));

        Assert.That(metadata.Declarations, Is.Empty);
    }

    [Test]
    public void Validator_groups_tests_by_the_exact_ordinal_package_sequence()
    {
        var metadata = EndToEndAssemblyMetadataReader.Read(FixtureAssembly("EndToEndHost.ValidFixtures"));
        var candidate = new EndToEndAssemblyCandidate(
            Project("EndToEndHost.ValidFixtures"),
            "alpha.mod",
            "EndToEndHost.ValidFixtures",
            metadata);

        var result = EndToEndDiscoveryValidator.ValidateAndGroup(
            new[] { candidate },
            new[] { "ludeon.rimworld", "brrainz.harmony", "alpha.mod", "optional.mod" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Groups, Has.Count.EqualTo(2));
            Assert.That(result.Groups[0].ActivePackageIds, Is.EqualTo(new[]
            {
                "ludeon.rimworld",
                "brrainz.harmony",
                "alpha.mod"
            }));
            Assert.That(result.Groups[0].Tests.Select(test => test.Id), Is.EqualTo(new[]
            {
                "alpha.base-a",
                "alpha.base-b"
            }));
            Assert.That(result.Groups[1].Tests.Select(test => test.Id), Is.EqualTo(new[] { "alpha.optional" }));
            Assert.That(result.Groups.Select(group => group.GroupId), Is.Ordered);
        });
    }

    [Test]
    public void Validator_rejects_invalid_compiled_declarations_with_actionable_errors()
    {
        var metadata = EndToEndAssemblyMetadataReader.Read(FixtureAssembly("EndToEndHost.InvalidFixtures"));
        var candidate = new EndToEndAssemblyCandidate(
            Project("EndToEndHost.InvalidFixtures"),
            "alpha.mod",
            "EndToEndHost.InvalidFixtures",
            metadata);

        var error = Assert.Throws<EndToEndDiscoveryException>(() =>
            EndToEndDiscoveryValidator.ValidateAndGroup(
                new[] { candidate },
                new[] { "ludeon.rimworld", "alpha.mod", "another.mod" }));

        Assert.Multiple(() =>
        {
            Assert.That(error!.Message, Does.Contain("invalid.abstract").And.Contain("concrete"));
            Assert.That(error.Message, Does.Contain("invalid.interface").And.Contain("IRimWorldEndToEndTest"));
            Assert.That(error.Message, Does.Contain("invalid.owner").And.Contain("owner"));
            Assert.That(error.Message, Does.Contain("invalid.duplicate-package").And.Contain("duplicate"));
            Assert.That(error.Message, Does.Contain("invalid.explicit-gateway").And.Contain("must not list"));
            Assert.That(error.Message, Does.Contain("invalid.deadline").And.Contain("deadline"));
            Assert.That(error.Message, Does.Contain("invalid.duplicate-id").And.Contain("duplicate test ID"));
        });
    }

    [Test]
    public void Validator_rejects_project_owner_output_and_package_resolution_drift()
    {
        var metadata = EndToEndAssemblyMetadataReader.Read(FixtureAssembly("EndToEndHost.ValidFixtures"));

        var ownerError = Assert.Throws<EndToEndDiscoveryException>(() =>
            EndToEndDiscoveryValidator.ValidateAndGroup(
                new[]
                {
                    new EndToEndAssemblyCandidate(
                        Project("EndToEndHost.ValidFixtures"),
                        "wrong.owner",
                        "EndToEndHost.ValidFixtures",
                        metadata)
                },
                new[] { "ludeon.rimworld", "brrainz.harmony", "alpha.mod", "optional.mod" }));
        var outputError = Assert.Throws<EndToEndDiscoveryException>(() =>
            EndToEndDiscoveryValidator.ValidateAndGroup(
                new[]
                {
                    new EndToEndAssemblyCandidate(
                        Project("EndToEndHost.ValidFixtures"),
                        "alpha.mod",
                        "Different.Output",
                        metadata)
                },
                new[] { "ludeon.rimworld", "brrainz.harmony", "alpha.mod", "optional.mod" }));
        var packageError = Assert.Throws<EndToEndDiscoveryException>(() =>
            EndToEndDiscoveryValidator.ValidateAndGroup(
                new[]
                {
                    new EndToEndAssemblyCandidate(
                        Project("EndToEndHost.ValidFixtures"),
                        "alpha.mod",
                        "EndToEndHost.ValidFixtures",
                        metadata)
                },
                new[] { "ludeon.rimworld", "brrainz.harmony", "alpha.mod" }));

        Assert.Multiple(() =>
        {
            Assert.That(ownerError!.Message, Does.Contain("project owner").And.Contain("wrong.owner"));
            Assert.That(outputError!.Message, Does.Contain("assembly output").And.Contain("Different.Output"));
            Assert.That(packageError!.Message, Does.Contain("optional.mod").And.Contain("unresolvable"));
        });
    }

    [Test]
    public void Project_discovery_scans_marked_projects_once_and_returns_deterministic_records()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "project-discovery", Guid.NewGuid().ToString("N"));
        var zeta = Path.Combine(root, "zeta");
        var alpha = Path.Combine(root, "alpha");
        Directory.CreateDirectory(zeta);
        Directory.CreateDirectory(alpha);
        File.WriteAllText(Path.Combine(zeta, "Zeta.EndToEnd.csproj"), MarkedProject("zeta.mod"));
        File.WriteAllText(Path.Combine(alpha, "Alpha.EndToEnd.csproj"), MarkedProject("alpha.mod"));
        Directory.CreateDirectory(Path.Combine(root, "ignored", "obj"));
        File.WriteAllText(Path.Combine(root, "ignored", "obj", "Ignored.csproj"), MarkedProject("ignored.mod"));

        try
        {
            var projects = EndToEndProjectDiscovery.Discover(root);

            Assert.That(projects.Select(project => Path.GetFileNameWithoutExtension(project.ProjectPath)), Is.EqualTo(new[]
            {
                "Alpha.EndToEnd",
                "Zeta.EndToEnd"
            }));
            Assert.That(projects.Select(project => project.OwnerPackageId), Is.EqualTo(new[]
            {
                "alpha.mod",
                "zeta.mod"
            }));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Validator_rejects_zero_selected_assemblies()
    {
        var error = Assert.Throws<EndToEndDiscoveryException>(() =>
            EndToEndDiscoveryValidator.ValidateAndGroup(
                Array.Empty<EndToEndAssemblyCandidate>(),
                new[] { "ludeon.rimworld" }));

        Assert.That(error!.Message, Does.Contain("zero"));
    }

    private static string FixtureAssembly(string projectName) => Path.Combine(
        RepositoryRoot,
        "tests",
        "Fixtures",
        projectName,
        "bin",
        Configuration,
        "net480",
        projectName + ".dll");

    private static string Project(string projectName) => Path.Combine(
        RepositoryRoot,
        "tests",
        "Fixtures",
        projectName,
        projectName + ".csproj");

    private static string MarkedProject(string ownerPackageId) =>
        $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>" +
        "<TargetFramework>net480</TargetFramework>" +
        "<AssemblyName>Fixture.EndToEnd</AssemblyName>" +
        "<RimWorldEndToEndTest>true</RimWorldEndToEndTest>" +
        $"<RimWorldEndToEndTestOwnerPackageId>{ownerPackageId}</RimWorldEndToEndTestOwnerPackageId>" +
        "</PropertyGroup></Project>";

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
