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
                "brrainz.harmony",
                "ludeon.rimworld",
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
            new[] { "brrainz.harmony", "ludeon.rimworld", "alpha.mod", "optional.mod" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Groups, Has.Count.EqualTo(3));
            Assert.That(result.Groups[0].ActivePackageIds, Is.EqualTo(new[]
            {
                "brrainz.harmony",
                "ludeon.rimworld",
                "alpha.mod"
            }));
            Assert.That(result.Groups[0].Tests.Select(test => test.Id), Is.EqualTo(new[]
            {
                "alpha.base-a",
                "alpha.base-b"
            }));
            Assert.That(result.Groups[1].Tests.Select(test => test.Id), Is.EqualTo(new[] { "alpha.legacy" }));
            Assert.That(result.Groups[2].Tests.Select(test => test.Id), Is.EqualTo(new[] { "alpha.optional" }));
            Assert.That(result.Groups.Select(group => group.GroupId), Is.Ordered);
        });
    }

    [Test]
    public void Focused_selection_does_not_require_an_unrelated_package_and_keeps_complete_metadata()
    {
        var metadata = EndToEndAssemblyMetadataReader.Read(FixtureAssembly("EndToEndHost.ValidFixtures"));
        var candidate = new EndToEndAssemblyCandidate(Project("EndToEndHost.ValidFixtures"),
            "alpha.mod", "EndToEndHost.ValidFixtures", metadata);

        var result = EndToEndDiscoveryValidator.ValidateAndGroup(new[] { candidate },
            new[] { "brrainz.harmony", "ludeon.rimworld", "alpha.mod" },
            new[] { "alpha.base-a" }, Array.Empty<string>());

        Assert.That(result.Groups.SelectMany(group => group.Tests).Select(test => test.Id),
            Is.EquivalentTo(new[] { "alpha.base-a", "alpha.base-b", "alpha.legacy", "alpha.optional" }));
    }

    [Test]
    public void Focused_group_selection_checks_only_that_groups_packages()
    {
        var result = EndToEndDiscoveryValidator.ValidateAndGroup(new[] { ValidCandidate() },
            BasePackages, Array.Empty<string>(), new[] { BaseGroup });
        Assert.That(result.Groups, Has.Count.EqualTo(3));
    }

    [TestCase("alpha.optional", "", "unresolvable package 'optional.mod'")]
    [TestCase("", "brrainz.harmony|ludeon.rimworld|alpha.mod|optional.mod", "unresolvable package 'optional.mod'")]
    [TestCase("", "", "unresolvable package 'optional.mod'")]
    [TestCase("missing.test", "", "Unknown exact E2E test ID")]
    [TestCase("ALPHA.BASE-A", "", "Unknown exact E2E test ID")]
    [TestCase("alpha.base-a,alpha.base-a", "", "Duplicate exact E2E test ID")]
    [TestCase("", "missing.group", "Unknown exact E2E group ID")]
    [TestCase("", "BRRAINZ.HARMONY|LUDEON.RIMWORLD|ALPHA.MOD", "Unknown exact E2E group ID")]
    [TestCase("", "brrainz.harmony|ludeon.rimworld|alpha.mod,brrainz.harmony|ludeon.rimworld|alpha.mod", "Duplicate exact E2E group ID")]
    [TestCase("alpha.optional", "brrainz.harmony|ludeon.rimworld|alpha.mod", "zero tests")]
    public void Focused_selection_rejects_missing_packages_and_invalid_exact_selections(
        string tests, string groups, string message)
    {
        var error = Assert.Throws<EndToEndDiscoveryException>(() =>
            EndToEndDiscoveryValidator.ValidateAndGroup(new[] { ValidCandidate() }, BasePackages,
                tests.Split(',', StringSplitOptions.RemoveEmptyEntries),
                groups.Split(',', StringSplitOptions.RemoveEmptyEntries)));
        Assert.That(error!.Message, Does.Contain(message));
    }

    [Test]
    public void Focused_selection_still_validates_unselected_declarations()
    {
        var invalid = new EndToEndAssemblyCandidate(Project("EndToEndHost.InvalidFixtures"),
            "alpha.mod", "EndToEndHost.InvalidFixtures",
            EndToEndAssemblyMetadataReader.Read(FixtureAssembly("EndToEndHost.InvalidFixtures")));
        var error = Assert.Throws<EndToEndDiscoveryException>(() =>
            EndToEndDiscoveryValidator.ValidateAndGroup(new[] { ValidCandidate(), invalid }, BasePackages,
                new[] { "alpha.base-a" }, Array.Empty<string>()));
        Assert.That(error!.Message, Does.Contain("invalid.abstract").And.Contain("invalid.interface")
            .And.Contain("invalid.owner").And.Contain("invalid.duplicate-package")
            .And.Contain("invalid.harmony-order").And.Contain("invalid.explicit-gateway")
            .And.Contain("invalid.deadline").And.Contain("duplicate test ID"));
    }

    [Test]
    public void Focused_bundle_plan_preserves_all_declarations_in_the_manifest()
    {
        var plan = EndToEndBundlePlanner.Create(new[] { ValidCandidate() }, BasePackages,
            TestContext.CurrentContext.WorkDirectory, "1.7",
            new[] { "alpha.base-a" }, Array.Empty<string>());
        Assert.That(plan.OwnerStages.Single().Bundles.Single().Manifest.Tests.Select(test => test.Id),
            Is.EquivalentTo(new[] { "alpha.base-a", "alpha.base-b", "alpha.legacy", "alpha.optional" }));
    }

    private const string BaseGroup = "brrainz.harmony|ludeon.rimworld|alpha.mod";

    [TestCase("--test-id", "alpha.base-a", 0)]
    [TestCase("--group-id", BaseGroup, 0)]
    [TestCase("--test-id", "alpha.legacy", 0)]
    [TestCase("--group-id", BaseGroup + "::legacy-xml", 0)]
    [TestCase("--test-id", "alpha.optional", 2)]
    public void Host_cli_routes_exact_selection_into_package_preflight(string option, string id, int expectedExit)
    {
        var directory = Path.Combine(Path.GetTempPath(), "e2e-selected-plan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            // Reuse the real compiled metadata fixture. This build adapter only reports its
            // output properties, keeping the public CLI test independent of game assemblies.
            var document = new System.Xml.Linq.XDocument(new System.Xml.Linq.XElement("Project",
                new System.Xml.Linq.XElement("PropertyGroup",
                    new System.Xml.Linq.XElement("TargetFramework", "net480"),
                    new System.Xml.Linq.XElement("AssemblyName", "EndToEndHost.ValidFixtures"),
                    new System.Xml.Linq.XElement("TargetPath", FixtureAssembly("EndToEndHost.ValidFixtures")),
                    new System.Xml.Linq.XElement("RimWorldEndToEndTest", "true"),
                    new System.Xml.Linq.XElement("RimWorldEndToEndTestOwnerPackageId", "alpha.mod")),
                new System.Xml.Linq.XElement("Target", new System.Xml.Linq.XAttribute("Name", "Restore")),
                new System.Xml.Linq.XElement("Target", new System.Xml.Linq.XAttribute("Name", "Build"))));
            document.Save(Path.Combine(directory, "Fixture.csproj"));
            using var output = new StringWriter();
            using var error = new StringWriter();
            var exitCode = EndToEndHostCli.Invoke(new[]
            {
                "plan", "--repository-root", directory, "--mods-root", directory,
                "--rimworld-version", "1.7", "--package-id", "brrainz.harmony", "ludeon.rimworld", "alpha.mod",
                option, id, "--output", "json"
            }, output, error);
            Assert.That(exitCode, Is.EqualTo(expectedExit), error.ToString());
            if (expectedExit == 0)
            {
                using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
                var groups = json.RootElement.GetProperty("groups").EnumerateArray().ToArray();
                Assert.That(groups.Length, Is.EqualTo(3));
                Assert.That(groups.Single(group => group.GetProperty("groupId").GetString() == BaseGroup)
                    .GetProperty("xmlDeserializer").GetString(), Is.EqualTo("default"));
                Assert.That(groups.Single(group => group.GetProperty("groupId").GetString() == BaseGroup + "::legacy-xml")
                    .GetProperty("xmlDeserializer").GetString(), Is.EqualTo("legacy"));
                Assert.That(error.ToString(), Is.Empty);
            }
            else Assert.That(error.ToString(), Does.Contain("unresolvable package 'optional.mod'"));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static readonly string[] BasePackages = { "brrainz.harmony", "ludeon.rimworld", "alpha.mod" };
    private static EndToEndAssemblyCandidate ValidCandidate() => new(
        Project("EndToEndHost.ValidFixtures"), "alpha.mod", "EndToEndHost.ValidFixtures",
        EndToEndAssemblyMetadataReader.Read(FixtureAssembly("EndToEndHost.ValidFixtures")));

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
            Assert.That(error.Message, Does.Contain("invalid.harmony-order").And.Contain("brrainz.harmony"));
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
    public void Exact_project_selection_excludes_unselected_E2E_projects_and_rejects_unknown_paths()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "project-selection", Guid.NewGuid().ToString("N"));
        var selectedDirectory = Path.Combine(root, "selected");
        var unselectedDirectory = Path.Combine(root, "unselected");
        Directory.CreateDirectory(selectedDirectory);
        Directory.CreateDirectory(unselectedDirectory);
        var selectedPath = Path.Combine(selectedDirectory, "Selected.csproj");
        var unselectedPath = Path.Combine(unselectedDirectory, "BrokenButUnselected.csproj");
        File.WriteAllText(selectedPath, MarkedProject("selected.mod"));
        File.WriteAllText(unselectedPath, MarkedProject("unselected.mod"));
        try
        {
            var discovered = EndToEndProjectDiscovery.Discover(root);
            var selected = EndToEndProjectDiscovery.SelectExactProjects(discovered, new[] { selectedPath });
            var error = Assert.Throws<EndToEndDiscoveryException>(() =>
                EndToEndProjectDiscovery.SelectExactProjects(discovered, new[] { Path.Combine(root, "missing.csproj") }));
            Assert.Multiple(() =>
            {
                Assert.That(selected.Count, Is.EqualTo(1));
                Assert.That(selected[0].ProjectPath, Is.EqualTo(Path.GetFullPath(selectedPath)));
                Assert.That(error!.Message, Does.Contain("could not resolve 1 selected project"));
            });
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
            if (File.Exists(Path.Combine(directory.FullName, "RimWorldMods.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }
}
