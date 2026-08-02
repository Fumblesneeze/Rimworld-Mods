using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class InGameIntegrationTestBundleCliTests
{
    [Test]
    public void Dry_run_plans_only_active_owners_in_deterministic_order_without_writes_or_dotnet()
    {
        using var repository = TemporaryRepository.Create();
        repository.AddProject("tests/Zeta.Tests/Zeta.Tests.csproj", "zeta.mod");
        repository.AddProject("tests/Alpha.Tests/Alpha.Tests.csproj", "alpha.mod");
        repository.AddProject("tests/Inactive.Tests/Inactive.Tests.csproj", "inactive.mod");

        var run = repository.Run(
            activePackageIds: new[] { "zeta.mod", "alpha.mod" },
            dryRun: true);
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.Json!.Status, Is.EqualTo("planned"));
            Assert.That(run.Json.ProjectCount, Is.EqualTo(2));
            Assert.That(
                run.Json.Projects.Select(project => project.OwnerPackageId),
                Is.EqualTo(new[] { "alpha.mod", "zeta.mod" }));
            Assert.That(repository.DotNetInvocations, Is.Empty);
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Full_active_mod_set_ignores_packages_without_opted_in_test_projects()
    {
        using var repository = TemporaryRepository.Create();
        repository.AddProject("tests/Alpha.Tests/Alpha.Tests.csproj", "alpha.mod");

        var run = repository.Run(
            activePackageIds: new[] { "ludeon.rimworld", "alpha.mod", "fumblesneeze.rimworlddevgateway" },
            dryRun: true);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.Json!.ProjectCount, Is.EqualTo(1));
            Assert.That(run.Json.Projects.Single().OwnerPackageId, Is.EqualTo("alpha.mod"));
            Assert.That(repository.DotNetInvocations, Is.Empty);
        });
    }

    [Test]
    public void Missing_required_package_excludes_the_project_before_any_build()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.WriteSourceManifest(project, requiredPackageIds: new[] { "needed.mod" });

        var run = repository.Run(activePackageIds: new[] { "alpha.mod" }, dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("complete active-package matrix").IgnoreCase);
            Assert.That(repository.DotNetInvocations, Is.Empty);
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Active_forbidden_package_excludes_the_project_before_any_build()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.WriteSourceManifest(project, forbiddenPackageIds: new[] { "conflict.mod" });

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod", "conflict.mod" },
            dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("complete active-package matrix").IgnoreCase);
            Assert.That(repository.DotNetInvocations, Is.Empty);
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Complete_required_and_forbidden_matrix_is_selected_in_the_dry_run()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.WriteSourceManifest(
            project,
            requiredPackageIds: new[] { "needed.mod" },
            forbiddenPackageIds: new[] { "conflict.mod" });

        var run = repository.Run(
            activePackageIds: new[] { "ludeon.rimworld", "alpha.mod", "needed.mod" },
            dryRun: true);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.Json!.ProjectCount, Is.EqualTo(1));
            Assert.That(run.Json.Projects.Single().OwnerPackageId, Is.EqualTo("alpha.mod"));
            Assert.That(repository.DotNetInvocations, Is.Empty);
        });
    }

    [Test]
    public void Exact_active_package_set_rejects_an_unexpected_extra_before_any_build()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.WriteSourceManifest(
            project,
            activePackageSetMode: "exact",
            activePackageIds: new[] { "ludeon.rimworld", "alpha.mod", "gateway.mod" });

        var run = repository.Run(
            activePackageIds: new[]
            {
                "ludeon.rimworld",
                "alpha.mod",
                "gateway.mod",
                "unexpected.mod"
            },
            dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("complete active-package matrix").IgnoreCase);
            Assert.That(repository.DotNetInvocations, Is.Empty);
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Exact_active_package_set_rejects_the_same_packages_in_the_wrong_order_before_any_build()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.WriteSourceManifest(
            project,
            activePackageSetMode: "exact",
            activePackageIds: new[] { "ludeon.rimworld", "alpha.mod", "gateway.mod" });

        var run = repository.Run(
            activePackageIds: new[] { "ludeon.rimworld", "gateway.mod", "alpha.mod" },
            dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("complete active-package matrix").IgnoreCase);
            Assert.That(repository.DotNetInvocations, Is.Empty);
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Exact_active_package_sequence_is_selected_when_every_package_and_position_match()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.WriteSourceManifest(
            project,
            activePackageSetMode: "exact",
            activePackageIds: new[] { "ludeon.rimworld", "alpha.mod", "gateway.mod" });

        var run = repository.Run(
            activePackageIds: new[] { "LUDEON.RIMWORLD", "ALPHA.MOD", "GATEWAY.MOD" },
            dryRun: true);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.Json!.ProjectCount, Is.EqualTo(1));
            Assert.That(run.Json.Projects.Single().OwnerPackageId, Is.EqualTo("alpha.mod"));
            Assert.That(repository.DotNetInvocations, Is.Empty);
        });
    }

    [Test]
    public void Built_exact_active_package_sequence_must_preserve_the_validated_source_order()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.WriteSourceManifest(
            project,
            activePackageSetMode: "exact",
            activePackageIds: new[] { "ludeon.rimworld", "alpha.mod", "gateway.mod" });
        repository.SeedOutput(
            project,
            "alpha integration assembly",
            manifestJson:
                "{\"ownerPackageId\":\"alpha.mod\",\"assembly\":\"Alpha.IntegrationTests.dll\"," +
                "\"activePackageSetMode\":\"exact\",\"activePackageIds\":[" +
                "\"ludeon.rimworld\",\"gateway.mod\",\"alpha.mod\"]}");

        var run = repository.Run(
            activePackageIds: new[] { "ludeon.rimworld", "alpha.mod", "gateway.mod" },
            dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("matrix").IgnoreCase);
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Source_manifest_rejects_an_unknown_property_before_any_build()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        File.WriteAllText(
            project.SourceManifestPath,
            "{\"ownerPackageId\":\"alpha.mod\",\"assembly\":\"Alpha.IntegrationTests.dll\"," +
            "\"requiredPackageIds\":[],\"forbiddenPackageIds\":[],\"unexpected\":true}",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var run = repository.Run(activePackageIds: new[] { "alpha.mod" }, dryRun: true);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("unknown").And.Contain("unexpected"));
            Assert.That(repository.DotNetInvocations, Is.Empty);
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [TestCase(
        "{\"ownerPackageId\":\"alpha.mod\",\"assembly\":\"Alpha.IntegrationTests.dll\",\"activePackageSetMode\":\"exact\"}",
        "declared together")]
    [TestCase(
        "{\"ownerPackageId\":\"alpha.mod\",\"assembly\":\"Alpha.IntegrationTests.dll\",\"activePackageIds\":[\"alpha.mod\"]}",
        "declared together")]
    [TestCase(
        "{\"ownerPackageId\":\"alpha.mod\",\"assembly\":\"Alpha.IntegrationTests.dll\",\"activePackageSetMode\":\"subset\",\"activePackageIds\":[\"alpha.mod\"]}",
        "literal 'exact'")]
    [TestCase(
        "{\"ownerPackageId\":\"alpha.mod\",\"assembly\":\"Alpha.IntegrationTests.dll\",\"activePackageSetMode\":\"exact\",\"activePackageIds\":[\"alpha.mod\"],\"requiredPackageIds\":[\"alpha.mod\"]}",
        "cannot also declare")]
    public void Source_exact_active_package_schema_requires_one_unambiguous_mode_and_value(
        string manifestJson,
        string expectedMessage)
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        File.WriteAllText(
            project.SourceManifestPath,
            manifestJson,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var run = repository.Run(activePackageIds: new[] { "alpha.mod" }, dryRun: true);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain(expectedMessage).IgnoreCase);
            Assert.That(repository.DotNetInvocations, Is.Empty);
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Built_manifest_matrix_must_equal_the_prebuild_source_matrix()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.WriteSourceManifest(project, requiredPackageIds: new[] { "needed.mod" });
        repository.SeedOutput(
            project,
            "alpha integration assembly",
            manifestJson:
                "{\"ownerPackageId\":\"alpha.mod\",\"assembly\":\"Alpha.IntegrationTests.dll\"," +
                "\"requiredPackageIds\":[\"different.mod\"],\"forbiddenPackageIds\":[]}");

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod", "needed.mod" },
            dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("matrix").IgnoreCase);
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [TestCase("_leading.mod")]
    [TestCase("möd.unicode")]
    public void Unsafe_package_id_path_segments_are_rejected_like_the_runtime_catalog(string packageId)
    {
        using var repository = TemporaryRepository.Create();
        repository.AddProject("tests/RuntimePackage.Tests/RuntimePackage.Tests.csproj", packageId);

        var run = repository.Run(activePackageIds: new[] { packageId }, dryRun: true);

        Assert.That(run.ExitCode, Is.EqualTo(2));
        Assert.That(run.StandardError, Does.Contain("Invalid active package ID"));
    }

    [Test]
    public void Active_owner_project_is_built_and_its_validated_bundle_is_staged()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "alpha integration assembly");

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false);
        var destination = Path.Combine(
            repository.ArtifactsModsRoot,
            "alpha.mod",
            "1.6",
            "DevIntegrationTests");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.Json!.Status, Is.EqualTo("staged"));
            Assert.That(repository.DotNetInvocations.Select(line => line.Split('|')[0]),
                Is.EqualTo(new[] { "build", "msbuild" }));
            Assert.That(repository.DotNetInvocations, Has.All.Contains(
                $"DefaultRimWorldPath={repository.RimWorldPath}"));
            Assert.That(repository.DotNetInvocations, Has.All.Contains(
                $"DefaultSteamModContentFolder={repository.SteamModContentFolder}"));
            Assert.That(repository.DotNetInvocations[0], Does.Contain("BuildProjectReferences=true"));
            Assert.That(repository.DotNetInvocations[0], Does.Not.Contain("BuildProjectReferences=false"));
            Assert.That(
                File.ReadAllText(Path.Combine(destination, "Alpha.IntegrationTests.dll")),
                Is.EqualTo("alpha integration assembly"));
            Assert.That(
                File.Exists(Path.Combine(destination, "Alpha.IntegrationTests.integrationtests.json")),
                Is.True);
            Assert.That(
                File.Exists(Path.Combine(destination, ".rimworld-integration-test-stage.owner")),
                Is.True);
            Assert.That(run.Json.Projects.Single().Assembly, Is.EqualTo("Alpha.IntegrationTests.dll"));
            Assert.That(run.Json.Projects.Single().Manifest, Is.EqualTo("Alpha.IntegrationTests.integrationtests.json"));
        });
    }

    [Test]
    public void Full_stage_accepts_a_non_root_artifacts_path_with_a_trailing_separator()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "alpha integration assembly");

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            artifactsModsRoot: repository.ArtifactsModsRoot + Path.DirectorySeparatorChar);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.Json!.ArtifactsModsRoot, Is.EqualTo(repository.ArtifactsModsRoot));
            Assert.That(
                File.Exists(Path.Combine(
                    repository.ArtifactsModsRoot,
                    "alpha.mod",
                    "1.6",
                    "DevIntegrationTests",
                    "Alpha.IntegrationTests.dll")),
                Is.True);
        });
    }

    [Test]
    public void Explicit_empty_active_package_set_is_rejected_without_side_effects()
    {
        using var repository = TemporaryRepository.Create();
        repository.AddProject("tests/Alpha.Tests/Alpha.Tests.csproj", "alpha.mod");

        var run = repository.Run(activePackageIds: Array.Empty<string>(), dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("At least one active package ID"));
            Assert.That(repository.DotNetInvocations, Is.Empty);
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Complete_active_package_set_is_required_even_when_the_argument_is_omitted()
    {
        using var repository = TemporaryRepository.Create();
        repository.AddProject("tests/Alpha.Tests/Alpha.Tests.csproj", "alpha.mod");

        var run = repository.Run(activePackageIds: null, dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("complete active package set").IgnoreCase);
            Assert.That(repository.DotNetInvocations, Is.Empty);
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Requested_owner_without_an_opt_in_project_is_invalid()
    {
        using var repository = TemporaryRepository.Create();
        repository.AddProject("tests/Alpha.Tests/Alpha.Tests.csproj", "alpha.mod");

        var run = repository.Run(activePackageIds: new[] { "missing.mod" }, dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("missing.mod"));
            Assert.That(repository.DotNetInvocations, Is.Empty);
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Exact_rimworld_and_workshop_paths_are_required_before_dotnet_is_invoked()
    {
        using var repository = TemporaryRepository.Create();
        repository.AddProject("tests/Alpha.Tests/Alpha.Tests.csproj", "alpha.mod");

        var missingRimWorld = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: true,
            rimWorldPath: Path.Combine(repository.RimWorldPath, "missing"));
        var missingWorkshop = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: true,
            steamModContentFolder: Path.Combine(repository.SteamModContentFolder, "missing"));

        Assert.Multiple(() =>
        {
            Assert.That(missingRimWorld.ExitCode, Is.EqualTo(2));
            Assert.That(missingRimWorld.StandardError, Does.Contain("RimWorldPath"));
            Assert.That(missingWorkshop.ExitCode, Is.EqualTo(2));
            Assert.That(missingWorkshop.StandardError, Does.Contain("SteamModContentFolder"));
            Assert.That(repository.DotNetInvocations, Is.Empty);
        });
    }

    [Test]
    public void Default_stage_is_outside_the_normal_artifacts_mod_package_tree()
    {
        using var repository = TemporaryRepository.Create();
        repository.AddProject("tests/Alpha.Tests/Alpha.Tests.csproj", "alpha.mod");

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: true,
            useDefaultArtifactsRoot: true);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.Json!.ArtifactsModsRoot, Does.Contain("InGameIntegrationTests"));
            Assert.That(
                run.Json.ArtifactsModsRoot,
                Does.Not.StartWith(Path.Combine(repository.RepositoryRoot, "artifacts", "Mods") + Path.DirectorySeparatorChar));
        });
    }

    [Test]
    public void Missing_sibling_manifest_is_invalid_and_does_not_stage_the_assembly()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "alpha integration assembly", includeManifest: false);

        var run = repository.Run(activePackageIds: new[] { "alpha.mod" }, dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("manifest does not exist"));
            Assert.That(repository.DotNetInvocations.Select(line => line.Split('|')[0]),
                Is.EqualTo(new[] { "build", "msbuild" }));
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Repository_beneath_steam_workshop_is_rejected_even_when_artifacts_are_elsewhere()
    {
        using var repository = TemporaryRepository.Create(workshopLayout: true);
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "alpha integration assembly");

        var run = repository.Run(activePackageIds: new[] { "alpha.mod" }, dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("Workshop"));
            Assert.That(repository.DotNetInvocations, Is.Empty);
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Artifacts_root_below_a_junction_into_workshop_is_rejected_before_build_or_stage()
    {
        if (Path.DirectorySeparatorChar != '\\')
        {
            Assert.Ignore("Directory-junction safety is a Windows-only staging contract.");
        }

        using var repository = TemporaryRepository.Create(artifactsRootViaWorkshopJunction: true);
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "alpha integration assembly");

        var run = repository.Run(activePackageIds: new[] { "alpha.mod" }, dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("reparse").IgnoreCase);
            Assert.That(run.StandardError, Does.Contain("Workshop").IgnoreCase);
            Assert.That(repository.DotNetInvocations, Is.Empty);
            Assert.That(
                File.Exists(Path.Combine(
                    repository.ArtifactsModsRoot,
                    "alpha.mod",
                    "1.6",
                    "DevIntegrationTests",
                    "Alpha.IntegrationTests.dll")),
                Is.False);
        });
    }

    [Test]
    public void Duplicate_bundle_filenames_for_one_owner_are_rejected_before_staging()
    {
        using var repository = TemporaryRepository.Create();
        var first = repository.AddProject(
            "tests/First.Tests/First.Tests.csproj",
            "alpha.mod",
            "Shared.IntegrationTests");
        var second = repository.AddProject(
            "tests/Second.Tests/Second.Tests.csproj",
            "alpha.mod",
            "Shared.IntegrationTests");
        repository.SeedOutput(first, "first assembly");
        repository.SeedOutput(second, "second assembly");

        var run = repository.Run(activePackageIds: new[] { "alpha.mod" }, dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("Duplicate integration-test bundle destination"));
            Assert.That(repository.DotNetInvocations.Select(line => line.Split('|')[0]),
                Is.EqualTo(new[] { "build", "build", "msbuild", "msbuild" }));
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [TestCase("other.mod", "Alpha.IntegrationTests.dll", "ownerPackageId")]
    [TestCase("alpha.mod", "Other.IntegrationTests.dll", "assembly")]
    public void Manifest_identity_must_match_its_project_and_target(
        string manifestOwner,
        string manifestAssembly,
        string expectedErrorField)
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(
            project,
            "alpha integration assembly",
            manifestOwner: manifestOwner,
            manifestAssembly: manifestAssembly);

        var run = repository.Run(activePackageIds: new[] { "alpha.mod" }, dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain(expectedErrorField));
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Manifest_owner_package_id_matching_is_case_insensitive_like_the_runtime_catalog()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "Alpha.Mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "assembly", manifestOwner: "alpha.mod");

        var run = repository.Run(activePackageIds: new[] { "ALPHA.MOD" }, dryRun: false);

        Assert.That(run.ExitCode, Is.Zero, run.StandardError);
    }

    [Test]
    public void Build_failure_returns_runtime_exit_and_never_queries_or_stages()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "stale assembly that must not be staged");

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            fakeBuildExitCode: 17);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("failed with exit code 17"));
            Assert.That(repository.DotNetInvocations.Select(line => line.Split('|')[0]),
                Is.EqualTo(new[] { "build" }));
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Table_output_is_human_readable_and_contains_the_plan()
    {
        using var repository = TemporaryRepository.Create();
        repository.AddProject("tests/Alpha.Tests/Alpha.Tests.csproj", "alpha.mod");

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: true,
            outputMode: "table");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput, Does.Contain("OwnerPackageId"));
            Assert.That(run.StandardOutput, Does.Contain("alpha.mod"));
            Assert.That(run.StandardOutput, Does.Contain("Status: planned"));
            Assert.That(repository.DotNetInvocations, Is.Empty);
        });
    }

    [Test]
    public void Duplicate_manifest_properties_are_rejected_even_when_values_match()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(
            project,
            "alpha integration assembly",
            manifestJson:
                "{\"ownerPackageId\":\"alpha.mod\",\"ownerPackageId\":\"alpha.mod\"," +
                "\"assembly\":\"Alpha.IntegrationTests.dll\"}");

        var run = repository.Run(activePackageIds: new[] { "alpha.mod" }, dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("duplicate JSON property"));
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [TestCase(
        "{\"ownerPackageId\":\"alpha.mod\",\"assembly\":\"Alpha.IntegrationTests.dll\",\"requiredPackageIds\":\"dep.mod\"}",
        "requiredPackageIds")]
    [TestCase(
        "{\"ownerPackageId\":\"alpha.mod\",\"assembly\":\"Alpha.IntegrationTests.dll\",\"forbiddenPackageIds\":[7]}",
        "forbiddenPackageIds")]
    [TestCase(
        "{\"ownerPackageId\":\"alpha.mod\",\"assembly\":\"Alpha.IntegrationTests.dll\",\"requiredPackageIds\":[\"Dep.Mod\",\"dep.mod\"]}",
        "duplicate")]
    [TestCase(
        "{\"ownerPackageId\":\"alpha.mod\",\"assembly\":\"Alpha.IntegrationTests.dll\",\"requiredPackageIds\":[\"dep.mod\"],\"forbiddenPackageIds\":[\"DEP.MOD\"]}",
        "both required and forbidden")]
    [TestCase(
        "{\"ownerPackageId\":\"alpha.mod\",\"assembly\":\"Alpha.IntegrationTests.dll\",\"requiredPackageIds\":[\"bad/package\"]}",
        "invalid package ID")]
    public void Manifest_package_matrix_mirrors_runtime_validation(string manifestJson, string expectedMessage)
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "assembly", manifestJson: manifestJson);

        var run = repository.Run(activePackageIds: new[] { "alpha.mod" }, dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain(expectedMessage).IgnoreCase);
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Manifest_package_matrix_is_bounded_to_the_runtime_limit()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        var requiredPackages = string.Join(",", Enumerable.Range(0, 65).Select(index => $"\"dep{index}.mod\""));
        repository.SeedOutput(
            project,
            "assembly",
            manifestJson:
                "{\"ownerPackageId\":\"alpha.mod\",\"assembly\":\"Alpha.IntegrationTests.dll\"," +
                $"\"requiredPackageIds\":[{requiredPackages}]}}");

        var run = repository.Run(activePackageIds: new[] { "alpha.mod" }, dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("64"));
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Manifest_and_assembly_must_use_the_runtime_integration_test_suffix()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.Tests/Alpha.Tests.csproj",
            "alpha.mod",
            "Alpha.Tests");
        repository.SeedOutput(project, "assembly");

        var run = repository.Run(activePackageIds: new[] { "alpha.mod" }, dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain(".IntegrationTests.dll"));
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Manifest_file_read_is_bounded_before_json_parsing()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(
            project,
            "assembly",
            manifestJson:
                "{\"ownerPackageId\":\"alpha.mod\",\"assembly\":\"Alpha.IntegrationTests.dll\",\"padding\":\"" +
                new string('x', 70 * 1024) + "\"}");

        var run = repository.Run(activePackageIds: new[] { "alpha.mod" }, dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("byte limit"));
            Assert.That(Directory.Exists(repository.ArtifactsModsRoot), Is.False);
        });
    }

    [Test]
    public void Builds_and_results_are_ordered_by_owner_not_by_requested_order()
    {
        using var repository = TemporaryRepository.Create();
        var zeta = repository.AddProject(
            "tests/Zeta.IntegrationTests/Zeta.IntegrationTests.csproj",
            "zeta.mod",
            "Zeta.IntegrationTests");
        var alpha = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(zeta, "zeta assembly");
        repository.SeedOutput(alpha, "alpha assembly");

        var run = repository.Run(
            activePackageIds: new[] { "zeta.mod", "alpha.mod" },
            dryRun: false);
        var invokedProjects = repository.DotNetInvocations
            .Select(line => Path.GetFileNameWithoutExtension(line.Split('|')[1]))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(invokedProjects, Is.EqualTo(new[]
            {
                "Alpha.IntegrationTests",
                "Zeta.IntegrationTests",
                "Alpha.IntegrationTests",
                "Zeta.IntegrationTests"
            }));
            Assert.That(
                run.Json!.Projects.Select(project => project.OwnerPackageId),
                Is.EqualTo(new[] { "alpha.mod", "zeta.mod" }));
            Assert.That(
                File.Exists(Path.Combine(
                    repository.ArtifactsModsRoot,
                    "alpha.mod",
                    "1.6",
                    "DevIntegrationTests",
                    "Alpha.IntegrationTests.dll")),
                Is.True);
            Assert.That(
                File.Exists(Path.Combine(
                    repository.ArtifactsModsRoot,
                    "zeta.mod",
                    "1.6",
                    "DevIntegrationTests",
                    "Zeta.IntegrationTests.integrationtests.json")),
                Is.True);
        });
    }

    [Test]
    public void Successful_stage_removes_stale_files_only_from_the_exact_owned_test_directory()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "fresh assembly");
        var testDirectory = Path.Combine(
            repository.ArtifactsModsRoot,
            "alpha.mod",
            "1.6",
            "DevIntegrationTests");
        var siblingDirectory = Path.Combine(
            repository.ArtifactsModsRoot,
            "alpha.mod",
            "1.6",
            "Assemblies");
        Directory.CreateDirectory(testDirectory);
        Directory.CreateDirectory(siblingDirectory);
        File.WriteAllText(
            Path.Combine(testDirectory, ".rimworld-integration-test-stage.owner"),
            "RimWorldInGameIntegrationTests/v1|alpha.mod|1.6");
        File.WriteAllText(Path.Combine(testDirectory, "Stale.IntegrationTests.dll"), "stale");
        File.WriteAllText(Path.Combine(siblingDirectory, "Product.dll"), "must survive");

        var run = repository.Run(activePackageIds: new[] { "alpha.mod" }, dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(File.Exists(Path.Combine(testDirectory, "Stale.IntegrationTests.dll")), Is.False);
            Assert.That(File.Exists(Path.Combine(testDirectory, "Alpha.IntegrationTests.dll")), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(siblingDirectory, "Product.dll")), Is.EqualTo("must survive"));
        });
    }

    [Test]
    public void Existing_unowned_stage_is_never_recursively_deleted()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "fresh assembly");
        var testDirectory = Path.Combine(
            repository.ArtifactsModsRoot,
            "alpha.mod",
            "1.6",
            "DevIntegrationTests");
        Directory.CreateDirectory(testDirectory);
        var foreignFile = Path.Combine(testDirectory, "Foreign.dll");
        File.WriteAllText(foreignFile, "must survive");

        var run = repository.Run(activePackageIds: new[] { "alpha.mod" }, dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("ownership marker"));
            Assert.That(File.ReadAllText(foreignFile), Is.EqualTo("must survive"));
        });
    }

    [Test]
    public void Every_stage_is_preflighted_before_any_owned_stage_is_recursively_replaced()
    {
        using var repository = TemporaryRepository.Create();
        var alpha = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        var zeta = repository.AddProject(
            "tests/Zeta.IntegrationTests/Zeta.IntegrationTests.csproj",
            "zeta.mod",
            "Zeta.IntegrationTests");
        repository.SeedOutput(alpha, "fresh alpha");
        repository.SeedOutput(zeta, "fresh zeta");
        var alphaStage = Path.Combine(
            repository.ArtifactsModsRoot,
            "alpha.mod",
            "1.6",
            "DevIntegrationTests");
        var zetaStage = Path.Combine(
            repository.ArtifactsModsRoot,
            "zeta.mod",
            "1.6",
            "DevIntegrationTests");
        Directory.CreateDirectory(alphaStage);
        Directory.CreateDirectory(zetaStage);
        File.WriteAllText(
            Path.Combine(alphaStage, ".rimworld-integration-test-stage.owner"),
            "RimWorldInGameIntegrationTests/v1|alpha.mod|1.6");
        var alphaSentinel = Path.Combine(alphaStage, "AlphaSentinel.dll");
        var zetaSentinel = Path.Combine(zetaStage, "ZetaSentinel.dll");
        File.WriteAllText(alphaSentinel, "must survive failed preflight");
        File.WriteAllText(zetaSentinel, "must survive failed preflight");

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod", "zeta.mod" },
            dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("ownership marker"));
            Assert.That(File.ReadAllText(alphaSentinel), Is.EqualTo("must survive failed preflight"));
            Assert.That(File.ReadAllText(zetaSentinel), Is.EqualTo("must survive failed preflight"));
        });
    }

    [Test]
    public void Copy_failure_never_publishes_a_partial_live_stage()
    {
        using var repository = TemporaryRepository.Create();
        var alpha = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "owner.mod",
            "Alpha.IntegrationTests");
        var zeta = repository.AddProject(
            "tests/Zeta.IntegrationTests/Zeta.IntegrationTests.csproj",
            "owner.mod",
            "Zeta.IntegrationTests");
        repository.SeedOutput(alpha, "alpha");
        repository.SeedOutput(zeta, "zeta");
        var lockedAssembly = Path.Combine(
            Path.GetDirectoryName(zeta.ProjectPath)!,
            "bin",
            "Release",
            "Zeta.IntegrationTests.dll");
        using var sourceLock = new FileStream(lockedAssembly, FileMode.Open, FileAccess.Read, FileShare.None);

        var run = repository.Run(activePackageIds: new[] { "owner.mod" }, dryRun: false);
        var versionDirectory = Path.Combine(repository.ArtifactsModsRoot, "owner.mod", "1.6");
        var liveStage = Path.Combine(versionDirectory, "DevIntegrationTests");
        var stagingDirectories = Directory.Exists(versionDirectory)
            ? Directory.GetDirectories(versionDirectory, ".DevIntegrationTests.stage.*")
            : Array.Empty<string>();

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(Directory.Exists(liveStage), Is.False);
            Assert.That(stagingDirectories, Is.Empty);
        });
    }

    [Test]
    public void Copy_failure_preserves_the_previous_complete_owned_stage()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "new assembly");
        var liveStage = Path.Combine(
            repository.ArtifactsModsRoot,
            "alpha.mod",
            "1.6",
            "DevIntegrationTests");
        Directory.CreateDirectory(liveStage);
        File.WriteAllText(
            Path.Combine(liveStage, ".rimworld-integration-test-stage.owner"),
            "RimWorldInGameIntegrationTests/v1|alpha.mod|1.6");
        var sentinel = Path.Combine(liveStage, "PreviouslyComplete.IntegrationTests.dll");
        File.WriteAllText(sentinel, "previous complete stage");
        var lockedAssembly = Path.Combine(
            Path.GetDirectoryName(project.ProjectPath)!,
            "bin",
            "Release",
            "Alpha.IntegrationTests.dll");
        using var sourceLock = new FileStream(lockedAssembly, FileMode.Open, FileAccess.Read, FileShare.None);

        var run = repository.Run(activePackageIds: new[] { "alpha.mod" }, dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(File.ReadAllText(sentinel), Is.EqualTo("previous complete stage"));
            Assert.That(
                File.Exists(Path.Combine(liveStage, "Alpha.IntegrationTests.dll")),
                Is.False);
        });
    }

    [Test]
    public void Commit_failure_after_a_live_to_backup_rename_restores_every_owner_and_cleans_transactions()
    {
        using var repository = TemporaryRepository.Create();
        var alpha = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        var zeta = repository.AddProject(
            "tests/Zeta.IntegrationTests/Zeta.IntegrationTests.csproj",
            "zeta.mod",
            "Zeta.IntegrationTests");
        repository.SeedOutput(alpha, "new alpha assembly");
        repository.SeedOutput(zeta, "new zeta assembly");
        var alphaStage = repository.SeedOwnedStage("alpha.mod", "AlphaPrevious.dll", "old alpha stage");
        var zetaStage = repository.SeedOwnedStage("zeta.mod", "ZetaPrevious.dll", "old zeta stage");

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod", "zeta.mod" },
            dryRun: false,
            testFailCommitAfterBackupOwner: "zeta.mod");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("Injected integration-test stage commit failure"));
            Assert.That(File.ReadAllText(Path.Combine(alphaStage, "AlphaPrevious.dll")), Is.EqualTo("old alpha stage"));
            Assert.That(File.ReadAllText(Path.Combine(zetaStage, "ZetaPrevious.dll")), Is.EqualTo("old zeta stage"));
            Assert.That(File.Exists(Path.Combine(alphaStage, "Alpha.IntegrationTests.dll")), Is.False);
            Assert.That(File.Exists(Path.Combine(zetaStage, "Zeta.IntegrationTests.dll")), Is.False);
            Assert.That(repository.TransactionDirectories("alpha.mod"), Is.Empty);
            Assert.That(repository.TransactionDirectories("zeta.mod"), Is.Empty);
        });
    }

    [Test]
    public void Abrupt_process_interruption_after_backup_is_recovered_from_the_scripts_own_durable_marker()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "new assembly");
        var liveStage = repository.SeedOwnedStage(
            "alpha.mod",
            "PreviouslyComplete.dll",
            "recover the script-owned transaction");

        var interrupted = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            testCrashCommitAfterBackupOwner: "alpha.mod");
        var interruptedEntries = repository.TransactionDirectories("alpha.mod");

        var recovered = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            fakeBuildExitCode: 17);

        Assert.Multiple(() =>
        {
            Assert.That(interrupted.ExitCode, Is.Not.Zero);
            Assert.That(
                interruptedEntries.Any(path => Path.GetFileName(path).StartsWith(
                    ".DevIntegrationTests.transaction.",
                    StringComparison.Ordinal)),
                Is.True,
                "The interrupted production path must leave its own durable recovery marker.");
            Assert.That(recovered.ExitCode, Is.EqualTo(1));
            Assert.That(recovered.StandardError, Does.Contain("failed with exit code 17"));
            Assert.That(
                File.ReadAllText(Path.Combine(liveStage, "PreviouslyComplete.dll")),
                Is.EqualTo("recover the script-owned transaction"));
            Assert.That(File.Exists(Path.Combine(liveStage, "Alpha.IntegrationTests.dll")), Is.False);
            Assert.That(repository.TransactionDirectories("alpha.mod"), Is.Empty);
        });
    }

    [Test]
    public void Rollback_refuses_an_inserted_live_destination_and_retains_recoverable_transaction_state()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "new assembly");
        var liveStage = repository.SeedOwnedStage(
            "alpha.mod",
            "PreviouslyComplete.dll",
            "restore only after the destination is safe");

        var failed = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            testFailCommitAfterBackupOwner: "alpha.mod",
            testCreateRollbackDestinationOwner: "alpha.mod");
        var retainedEntries = repository.TransactionDirectories("alpha.mod");

        Assert.Multiple(() =>
        {
            Assert.That(failed.ExitCode, Is.EqualTo(1));
            Assert.That(failed.StandardError, Does.Contain("Rollback also failed"));
            Assert.That(failed.StandardError, Does.Contain("live destination appeared"));
            Assert.That(Directory.Exists(liveStage), Is.True);
            Assert.That(File.Exists(Path.Combine(liveStage, "PreviouslyComplete.dll")), Is.False);
            Assert.That(
                retainedEntries.Any(path => Path.GetFileName(path).StartsWith(
                    ".DevIntegrationTests.backup.",
                    StringComparison.Ordinal)),
                Is.True);
            Assert.That(
                retainedEntries.Any(path => Path.GetFileName(path).StartsWith(
                    ".DevIntegrationTests.transaction.",
                    StringComparison.Ordinal)),
                Is.True);
        });

        Directory.Delete(liveStage, recursive: true);
        var recovered = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            fakeBuildExitCode: 17);

        Assert.Multiple(() =>
        {
            Assert.That(recovered.ExitCode, Is.EqualTo(1));
            Assert.That(
                File.ReadAllText(Path.Combine(liveStage, "PreviouslyComplete.dll")),
                Is.EqualTo("restore only after the destination is safe"));
            Assert.That(repository.TransactionDirectories("alpha.mod"), Is.Empty);
        });
    }

    [Test]
    public void Rollback_retains_the_durable_marker_when_an_expected_backup_disappears()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "new assembly");
        var liveStage = repository.SeedOwnedStage(
            "alpha.mod",
            "PreviouslyComplete.dll",
            "the missing backup must be reported");

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            testFailCommitAfterBackupOwner: "alpha.mod",
            testDeleteBackupBeforeRollbackOwner: "alpha.mod");
        var retainedEntries = repository.TransactionDirectories("alpha.mod");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("Rollback also failed"));
            Assert.That(run.StandardError, Does.Contain("expected backup is missing"));
            Assert.That(Directory.Exists(liveStage), Is.False);
            Assert.That(
                retainedEntries.Any(path => Path.GetFileName(path).StartsWith(
                    ".DevIntegrationTests.transaction.",
                    StringComparison.Ordinal)),
                Is.True,
                "Uncertain rollback must retain its durable recovery marker.");
        });

        var recoveryAttempt = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            fakeBuildExitCode: 17);

        Assert.Multiple(() =>
        {
            Assert.That(recoveryAttempt.ExitCode, Is.EqualTo(2));
            Assert.That(recoveryAttempt.StandardError, Does.Contain("no recoverable live, temporary, or backup stage"));
            Assert.That(
                File.Exists(retainedEntries.Single(path => Path.GetFileName(path).StartsWith(
                    ".DevIntegrationTests.transaction.",
                    StringComparison.Ordinal))),
                Is.True,
                "Startup must preserve the only evidence for an unrecoverable transaction.");
        });
    }

    [Test]
    public void Commit_refuses_an_inserted_backup_destination_without_nesting_the_live_stage()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "new assembly");
        var liveStage = repository.SeedOwnedStage(
            "alpha.mod",
            "PreviouslyComplete.dll",
            "old live remains exact");

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            testCreateCommitBackupDestinationOwner: "alpha.mod");
        var entries = repository.TransactionDirectories("alpha.mod");
        var injectedBackup = entries.Single(path => Path.GetFileName(path).StartsWith(
            ".DevIntegrationTests.backup.",
            StringComparison.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("backup destination appeared"));
            Assert.That(
                File.ReadAllText(Path.Combine(liveStage, "PreviouslyComplete.dll")),
                Is.EqualTo("old live remains exact"));
            Assert.That(File.Exists(Path.Combine(injectedBackup, "foreign-destination.txt")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(injectedBackup, "DevIntegrationTests")), Is.False);
            Assert.That(
                entries.Any(path => Path.GetFileName(path).StartsWith(
                    ".DevIntegrationTests.transaction.",
                    StringComparison.Ordinal)),
                Is.False);
        });
    }

    [Test]
    public void Publish_refuses_an_inserted_live_destination_and_leaves_recoverable_state()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "new assembly");
        var liveStage = repository.SeedOwnedStage(
            "alpha.mod",
            "PreviouslyComplete.dll",
            "recover old stage after publish race");

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            testCreatePublishLiveDestinationOwner: "alpha.mod");
        var entries = repository.TransactionDirectories("alpha.mod");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("live destination appeared before publication"));
            Assert.That(File.Exists(Path.Combine(liveStage, "foreign-destination.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(liveStage, "Alpha.IntegrationTests.dll")), Is.False);
            Assert.That(
                entries.Any(path => Directory.Exists(path) &&
                                    File.Exists(Path.Combine(path, "PreviouslyComplete.dll"))),
                Is.True);
            Assert.That(
                entries.Any(path => Path.GetFileName(path).StartsWith(
                    ".DevIntegrationTests.transaction.",
                    StringComparison.Ordinal)),
                Is.True);
        });

        Directory.Delete(liveStage, recursive: true);
        var recovered = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            fakeBuildExitCode: 17);
        Assert.Multiple(() =>
        {
            Assert.That(recovered.ExitCode, Is.EqualTo(1));
            Assert.That(
                File.ReadAllText(Path.Combine(liveStage, "PreviouslyComplete.dll")),
                Is.EqualTo("recover old stage after publish race"));
            Assert.That(repository.TransactionDirectories("alpha.mod"), Is.Empty);
        });
    }

    [Test]
    public void Recovery_refuses_an_inserted_live_destination_without_nesting_the_backup()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "new assembly");
        var liveStage = repository.SeedInterruptedStageTransaction(
            "alpha.mod",
            "PreviouslyComplete.dll",
            "recover only to the exact live path");

        var failed = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            fakeBuildExitCode: 17,
            testCreateRecoveryLiveDestinationOwner: "alpha.mod");
        var retainedEntries = repository.TransactionDirectories("alpha.mod");

        Assert.Multiple(() =>
        {
            Assert.That(failed.ExitCode, Is.EqualTo(1));
            Assert.That(failed.StandardError, Does.Contain("live destination appeared before interrupted transaction recovery"));
            Assert.That(File.Exists(Path.Combine(liveStage, "foreign-destination.txt")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(liveStage, Path.GetFileName(
                retainedEntries.Single(path => Path.GetFileName(path).StartsWith(
                    ".DevIntegrationTests.backup.",
                    StringComparison.Ordinal))))), Is.False);
            Assert.That(
                retainedEntries.Any(path => Path.GetFileName(path).StartsWith(
                    ".DevIntegrationTests.transaction.",
                    StringComparison.Ordinal)),
                Is.True);
        });

        Directory.Delete(liveStage, recursive: true);
        var recovered = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            fakeBuildExitCode: 17);
        Assert.Multiple(() =>
        {
            Assert.That(recovered.ExitCode, Is.EqualTo(1));
            Assert.That(
                File.ReadAllText(Path.Combine(liveStage, "PreviouslyComplete.dll")),
                Is.EqualTo("recover only to the exact live path"));
            Assert.That(repository.TransactionDirectories("alpha.mod"), Is.Empty);
        });
    }

    [Test]
    public void Cleanup_refuses_an_unowned_replacement_of_the_current_transaction_temporary_stage()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "new assembly");

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            testReplaceTemporaryWithUnownedOwner: "alpha.mod");
        var retainedEntries = repository.TransactionDirectories("alpha.mod");
        var foreignTemporary = retainedEntries.Single(path =>
            Path.GetFileName(path).StartsWith(
                ".DevIntegrationTests.stage.",
                StringComparison.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("rollback cleanup failed"));
            Assert.That(run.StandardError, Does.Contain("ownership marker"));
            Assert.That(
                File.ReadAllText(Path.Combine(foreignTemporary, "foreign-destination.txt")),
                Is.EqualTo("foreign destination"));
            Assert.That(
                retainedEntries.Any(path => Path.GetFileName(path).StartsWith(
                    ".DevIntegrationTests.transaction.",
                    StringComparison.Ordinal)),
                Is.True);
        });
    }

    [Test]
    public void Startup_recovery_restores_a_valid_backup_when_live_is_absent_after_interruption()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "new assembly that the failing build must not publish");
        var liveStage = repository.SeedInterruptedStageTransaction(
            "alpha.mod",
            "PreviouslyComplete.dll",
            "recover this complete stage");

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            fakeBuildExitCode: 17);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("failed with exit code 17"));
            Assert.That(
                File.ReadAllText(Path.Combine(liveStage, "PreviouslyComplete.dll")),
                Is.EqualTo("recover this complete stage"));
            Assert.That(File.Exists(Path.Combine(liveStage, "InterruptedNew.dll")), Is.False);
            Assert.That(repository.TransactionDirectories("alpha.mod"), Is.Empty);
        });
    }

    [TestCase("owner")]
    [TestCase("version")]
    [TestCase("transaction-id")]
    public void Startup_recovery_rejects_mismatched_transaction_ownership_without_mutating_stages(
        string mismatch)
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "new assembly");
        repository.SeedInterruptedStageTransaction(
            "alpha.mod",
            "PreviouslyComplete.dll",
            "must remain in backup");
        var entries = repository.TransactionDirectories("alpha.mod");
        var marker = entries.Single(path => Path.GetFileName(path).StartsWith(
            ".DevIntegrationTests.transaction.",
            StringComparison.Ordinal));
        const string transactionId = "0123456789abcdef0123456789abcdef";
        var markerText = mismatch switch
        {
            "owner" => $"RimWorldInGameIntegrationTests/transaction-v1|foreign.mod|1.6|{transactionId}",
            "version" => $"RimWorldInGameIntegrationTests/transaction-v1|alpha.mod|1.5|{transactionId}",
            "transaction-id" => "RimWorldInGameIntegrationTests/transaction-v1|alpha.mod|1.6|abcdef0123456789abcdef0123456789",
            _ => throw new AssertionException($"Unknown mismatch fixture: {mismatch}")
        };
        File.WriteAllText(marker, markerText);

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            fakeBuildExitCode: 17);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("transaction marker ownership"));
            Assert.That(
                entries.Any(path => Directory.Exists(path) &&
                                    File.Exists(Path.Combine(path, "PreviouslyComplete.dll"))),
                Is.True);
            Assert.That(
                entries.Any(path => Directory.Exists(path) &&
                                    File.Exists(Path.Combine(path, "InterruptedNew.dll"))),
                Is.True);
            Assert.That(File.Exists(marker), Is.True);
        });
    }

    [Test]
    public void Startup_recovery_rejects_multiple_transaction_markers_without_mutating_stages()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "new assembly");
        repository.SeedInterruptedStageTransaction(
            "alpha.mod",
            "PreviouslyComplete.dll",
            "must remain in backup");
        var versionDirectory = Path.Combine(repository.ArtifactsModsRoot, "alpha.mod", "1.6");
        const string secondId = "abcdef0123456789abcdef0123456789";
        var secondMarker = Path.Combine(
            versionDirectory,
            $".DevIntegrationTests.transaction.{secondId}.owner");
        File.WriteAllText(
            secondMarker,
            $"RimWorldInGameIntegrationTests/transaction-v1|alpha.mod|1.6|{secondId}");
        var before = Directory.GetFileSystemEntries(versionDirectory).OrderBy(path => path).ToArray();

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            fakeBuildExitCode: 17);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("Multiple interrupted integration-test transactions"));
            Assert.That(
                Directory.GetFileSystemEntries(versionDirectory).OrderBy(path => path),
                Is.EqualTo(before));
        });
    }

    [Test]
    public void Startup_recovery_rejects_a_linked_transaction_marker_without_following_it()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "new assembly");
        repository.SeedInterruptedStageTransaction(
            "alpha.mod",
            "PreviouslyComplete.dll",
            "must remain in backup");
        var entries = repository.TransactionDirectories("alpha.mod");
        var marker = entries.Single(path => Path.GetFileName(path).StartsWith(
            ".DevIntegrationTests.transaction.",
            StringComparison.Ordinal));
        File.Delete(marker);
        var foreignTarget = Path.Combine(repository.RepositoryRoot, "foreign-marker-target");
        Directory.CreateDirectory(foreignTarget);
        var foreignSentinel = Path.Combine(foreignTarget, "must-survive.txt");
        File.WriteAllText(foreignSentinel, "foreign");
        TemporaryRepository.CreateJunction(marker, foreignTarget);

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            fakeBuildExitCode: 17);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("reparse point"));
            Assert.That(File.ReadAllText(foreignSentinel), Is.EqualTo("foreign"));
            Assert.That(
                entries.Where(Directory.Exists).Any(path =>
                    File.Exists(Path.Combine(path, "PreviouslyComplete.dll"))),
                Is.True);
        });
        Directory.Delete(marker);
    }

    [TestCase("backup")]
    [TestCase("stage")]
    public void Startup_recovery_rejects_a_transaction_whose_referenced_stage_is_not_owned(
        string stageKind)
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "new assembly");
        repository.SeedInterruptedStageTransaction(
            "alpha.mod",
            "PreviouslyComplete.dll",
            "must remain untouched");
        var entries = repository.TransactionDirectories("alpha.mod");
        var stage = entries.Single(path => Path.GetFileName(path).StartsWith(
            $".DevIntegrationTests.{stageKind}.",
            StringComparison.Ordinal));
        var ownershipMarker = Path.Combine(stage, ".rimworld-integration-test-stage.owner");
        File.WriteAllText(
            ownershipMarker,
            "RimWorldInGameIntegrationTests/v1|foreign.mod|1.6");
        var before = entries.ToDictionary(
            path => path,
            path => Directory.Exists(path)
                ? Directory.GetFileSystemEntries(path).OrderBy(value => value).ToArray()
                : new[] { File.ReadAllText(path) });

        var run = repository.Run(
            activePackageIds: new[] { "alpha.mod" },
            dryRun: false,
            fakeBuildExitCode: 17);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("ownership marker does not match"));
            foreach (var pair in before)
            {
                Assert.That(File.Exists(pair.Key) || Directory.Exists(pair.Key), Is.True, pair.Key);
                if (Directory.Exists(pair.Key))
                {
                    Assert.That(
                        Directory.GetFileSystemEntries(pair.Key).OrderBy(value => value),
                        Is.EqualTo(pair.Value));
                }
                else
                {
                    Assert.That(File.ReadAllText(pair.Key), Is.EqualTo(pair.Value.Single()));
                }
            }
        });
    }

    [Test]
    public void Unmarked_transaction_siblings_are_never_adopted_or_deleted()
    {
        using var repository = TemporaryRepository.Create();
        var project = repository.AddProject(
            "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj",
            "alpha.mod",
            "Alpha.IntegrationTests");
        repository.SeedOutput(project, "new assembly");
        var versionDirectory = Path.Combine(repository.ArtifactsModsRoot, "alpha.mod", "1.6");
        const string foreignId = "fedcba9876543210fedcba9876543210";
        var foreignBackup = Path.Combine(versionDirectory, $".DevIntegrationTests.backup.{foreignId}");
        var foreignTemporary = Path.Combine(versionDirectory, $".DevIntegrationTests.stage.{foreignId}");
        Directory.CreateDirectory(foreignBackup);
        Directory.CreateDirectory(foreignTemporary);
        var backupSentinel = Path.Combine(foreignBackup, "foreign-backup.txt");
        var temporarySentinel = Path.Combine(foreignTemporary, "foreign-stage.txt");
        File.WriteAllText(backupSentinel, "foreign backup");
        File.WriteAllText(temporarySentinel, "foreign stage");

        var run = repository.Run(activePackageIds: new[] { "alpha.mod" }, dryRun: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(File.ReadAllText(backupSentinel), Is.EqualTo("foreign backup"));
            Assert.That(File.ReadAllText(temporarySentinel), Is.EqualTo("foreign stage"));
            Assert.That(
                File.Exists(Path.Combine(versionDirectory, "DevIntegrationTests", "Alpha.IntegrationTests.dll")),
                Is.True);
        });
    }

    private sealed class TemporaryRepository : IDisposable
    {
        private readonly string cleanupRoot;
        private readonly string root;
        private readonly string scriptPath;
        private readonly string dotNetJournalPath;
        private readonly string fakeDotNetDirectory;
        private readonly string? artifactsRootJunction;

        private TemporaryRepository(
            string cleanupRoot,
            string root,
            string scriptPath,
            string dotNetJournalPath,
            string fakeDotNetDirectory,
            string artifactsModsRoot,
            string rimWorldPath,
            string steamModContentFolder,
            string? artifactsRootJunction)
        {
            this.cleanupRoot = cleanupRoot;
            this.root = root;
            this.scriptPath = scriptPath;
            this.dotNetJournalPath = dotNetJournalPath;
            this.fakeDotNetDirectory = fakeDotNetDirectory;
            ArtifactsModsRoot = artifactsModsRoot;
            RimWorldPath = rimWorldPath;
            SteamModContentFolder = steamModContentFolder;
            this.artifactsRootJunction = artifactsRootJunction;
        }

        public string ArtifactsModsRoot { get; }

        public string RepositoryRoot => root;

        public string RimWorldPath { get; }

        public string SteamModContentFolder { get; }

        public IReadOnlyList<string> DotNetInvocations => File.Exists(dotNetJournalPath)
            ? File.ReadAllLines(dotNetJournalPath).Where(line => line.Length > 0).ToArray()
            : Array.Empty<string>();

        public static TemporaryRepository Create(
            bool workshopLayout = false,
            bool artifactsRootViaWorkshopJunction = false)
        {
            var sourceRepositoryRoot = FindSourceRepositoryRoot();
            var sourceScriptPath = Path.Combine(sourceRepositoryRoot, "scripts", "Build-InGameIntegrationTests.ps1");
            if (!File.Exists(sourceScriptPath))
            {
                throw new AssertionException($"Integration-test bundle CLI does not exist: {sourceScriptPath}");
            }

            var cleanupRoot = Path.Combine(Path.GetTempPath(), "RimWorldIntegrationTestBundleCli", Guid.NewGuid().ToString("N"));
            var root = workshopLayout
                ? Path.Combine(cleanupRoot, "steamapps", "workshop", "content", "294100", "repository")
                : cleanupRoot;
            var scriptsDirectory = Path.Combine(root, "scripts");
            var fakeDotNetDirectory = Path.Combine(root, "fake-dotnet");
            Directory.CreateDirectory(scriptsDirectory);
            Directory.CreateDirectory(fakeDotNetDirectory);

            var scriptPath = Path.Combine(scriptsDirectory, "Build-InGameIntegrationTests.ps1");
            File.Copy(sourceScriptPath, scriptPath);
            var dotNetJournalPath = Path.Combine(root, "dotnet-invocations.txt");
            File.WriteAllText(
                Path.Combine(fakeDotNetDirectory, "dotnet.cmd"),
                "@echo off\r\npwsh -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"%~dp0fake-dotnet.ps1\" %*\r\nexit /b %ERRORLEVEL%\r\n",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.WriteAllText(
                Path.Combine(fakeDotNetDirectory, "fake-dotnet.ps1"),
                FakeDotNetScript,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            var artifactsModsRoot = workshopLayout
                ? Path.Combine(cleanupRoot, "staged-mods")
                : Path.Combine(root, "staged-mods");
            var rimWorldPath = Path.Combine(cleanupRoot, "installed-game", "RimWorld");
            var steamModContentFolder = Path.Combine(
                cleanupRoot,
                "installed-workshop",
                "steamapps",
                "workshop",
                "content",
                "294100");
            var managedDirectory = Path.Combine(rimWorldPath, "RimWorldWin64_Data", "Managed");
            Directory.CreateDirectory(managedDirectory);
            Directory.CreateDirectory(steamModContentFolder);
            File.WriteAllText(Path.Combine(managedDirectory, "Assembly-CSharp.dll"), "fixture");
            string? artifactsRootJunction = null;
            if (artifactsRootViaWorkshopJunction)
            {
                artifactsRootJunction = Path.Combine(root, "artifacts-root-link");
                CreateDirectoryJunction(artifactsRootJunction, steamModContentFolder);
                artifactsModsRoot = Path.Combine(artifactsRootJunction, "gateway-staging");
            }

            return new TemporaryRepository(
                cleanupRoot,
                root,
                scriptPath,
                dotNetJournalPath,
                fakeDotNetDirectory,
                artifactsModsRoot,
                rimWorldPath,
                steamModContentFolder,
                artifactsRootJunction);
        }

        public ProjectFixture AddProject(string relativePath, string ownerPackageId, string? assemblyName = null)
        {
            var projectPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var resolvedAssemblyName = assemblyName ?? Path.GetFileNameWithoutExtension(projectPath);
            Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);
            File.WriteAllText(
                projectPath,
                $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>" +
                "<RimWorldInGameIntegrationTest>true</RimWorldInGameIntegrationTest>" +
                $"<RimWorldIntegrationTestOwnerPackageId>{ownerPackageId}</RimWorldIntegrationTestOwnerPackageId>" +
                $"<AssemblyName>{resolvedAssemblyName}</AssemblyName>" +
                "</PropertyGroup></Project>",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            var project = new ProjectFixture(projectPath, ownerPackageId, resolvedAssemblyName);
            WriteSourceManifest(project);
            return project;
        }

        public void WriteSourceManifest(
            ProjectFixture project,
            IEnumerable<string>? requiredPackageIds = null,
            IEnumerable<string>? forbiddenPackageIds = null,
            string? activePackageSetMode = null,
            IEnumerable<string>? activePackageIds = null)
        {
            static string Array(IEnumerable<string>? values) =>
                "[" + string.Join(",", (values ?? System.Array.Empty<string>()).Select(value => "\"" + value + "\"")) + "]";
            var exactSet = activePackageSetMode is null
                ? string.Empty
                : ",\"activePackageSetMode\":\"" + activePackageSetMode +
                  "\",\"activePackageIds\":" + Array(activePackageIds);
            File.WriteAllText(
                project.SourceManifestPath,
                "{\"ownerPackageId\":\"" + project.OwnerPackageId +
                "\",\"assembly\":\"" + project.AssemblyName +
                ".dll\",\"requiredPackageIds\":" + Array(requiredPackageIds) +
                ",\"forbiddenPackageIds\":" + Array(forbiddenPackageIds) + exactSet + "}",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        public void SeedOutput(
            ProjectFixture project,
            string assemblyContents,
            bool includeManifest = true,
            string? manifestOwner = null,
            string? manifestAssembly = null,
            string? manifestJson = null)
        {
            var outputDirectory = Path.Combine(Path.GetDirectoryName(project.ProjectPath)!, "bin", "Release");
            Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(
                Path.Combine(outputDirectory, project.AssemblyName + ".dll"),
                assemblyContents,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            if (includeManifest)
            {
                File.WriteAllText(
                    Path.Combine(outputDirectory, project.AssemblyName + ".integrationtests.json"),
                    manifestJson ??
                    ("{\"ownerPackageId\":\"" + (manifestOwner ?? project.OwnerPackageId) +
                     "\",\"assembly\":\"" + (manifestAssembly ?? project.AssemblyName + ".dll") + "\"}"),
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
        }

        public string SeedOwnedStage(
            string ownerPackageId,
            string sentinelName,
            string sentinelContents)
        {
            var stage = Path.Combine(
                ArtifactsModsRoot,
                ownerPackageId,
                "1.6",
                "DevIntegrationTests");
            Directory.CreateDirectory(stage);
            File.WriteAllText(
                Path.Combine(stage, ".rimworld-integration-test-stage.owner"),
                $"RimWorldInGameIntegrationTests/v1|{ownerPackageId}|1.6");
            File.WriteAllText(Path.Combine(stage, sentinelName), sentinelContents);
            return stage;
        }

        public string SeedInterruptedStageTransaction(
            string ownerPackageId,
            string sentinelName,
            string sentinelContents)
        {
            const string transactionId = "0123456789abcdef0123456789abcdef";
            var versionDirectory = Path.Combine(ArtifactsModsRoot, ownerPackageId, "1.6");
            var liveStage = Path.Combine(versionDirectory, "DevIntegrationTests");
            var backupStage = Path.Combine(versionDirectory, $".DevIntegrationTests.backup.{transactionId}");
            var temporaryStage = Path.Combine(versionDirectory, $".DevIntegrationTests.stage.{transactionId}");
            Directory.CreateDirectory(backupStage);
            Directory.CreateDirectory(temporaryStage);
            var ownership = $"RimWorldInGameIntegrationTests/v1|{ownerPackageId}|1.6";
            File.WriteAllText(
                Path.Combine(backupStage, ".rimworld-integration-test-stage.owner"),
                ownership);
            File.WriteAllText(
                Path.Combine(temporaryStage, ".rimworld-integration-test-stage.owner"),
                ownership);
            File.WriteAllText(Path.Combine(backupStage, sentinelName), sentinelContents);
            File.WriteAllText(Path.Combine(temporaryStage, "InterruptedNew.dll"), "discard this incomplete transaction");
            File.WriteAllText(
                Path.Combine(versionDirectory, $".DevIntegrationTests.transaction.{transactionId}.owner"),
                $"RimWorldInGameIntegrationTests/transaction-v1|{ownerPackageId}|1.6|{transactionId}");
            return liveStage;
        }

        public IReadOnlyList<string> TransactionDirectories(string ownerPackageId)
        {
            var versionDirectory = Path.Combine(ArtifactsModsRoot, ownerPackageId, "1.6");
            return Directory.Exists(versionDirectory)
                ? Directory.GetFileSystemEntries(versionDirectory, ".DevIntegrationTests.*.*")
                : Array.Empty<string>();
        }

        public static void CreateJunction(string junctionPath, string targetPath)
        {
            CreateDirectoryJunction(junctionPath, targetPath);
        }

        public CliRun Run(
            IReadOnlyList<string>? activePackageIds,
            bool dryRun,
            int fakeBuildExitCode = 0,
            string outputMode = "json",
            string? rimWorldPath = null,
            string? steamModContentFolder = null,
            bool useDefaultArtifactsRoot = false,
            string? artifactsModsRoot = null,
            string? testFailCommitAfterBackupOwner = null,
            string? testCrashCommitAfterBackupOwner = null,
            string? testCreateRollbackDestinationOwner = null,
            string? testDeleteBackupBeforeRollbackOwner = null,
            string? testCreateCommitBackupDestinationOwner = null,
            string? testCreatePublishLiveDestinationOwner = null,
            string? testCreateRecoveryLiveDestinationOwner = null,
            string? testReplaceTemporaryWithUnownedOwner = null)
        {
            var invocationPath = Path.Combine(root, $"invoke-{Guid.NewGuid():N}.ps1");
            var activeArgument = activePackageIds is null
                ? string.Empty
                : $"    ActivePackageIds = @({string.Join(", ", activePackageIds.Select(PowerShellLiteral))}){Environment.NewLine}";
            var invocation =
                "$parameters = @{" + Environment.NewLine +
                activeArgument +
                "    Configuration = 'Release'" + Environment.NewLine +
                (useDefaultArtifactsRoot
                    ? string.Empty
                    : $"    ArtifactsModsRoot = {PowerShellLiteral(artifactsModsRoot ?? ArtifactsModsRoot)}{Environment.NewLine}") +
                $"    RimWorldPath = {PowerShellLiteral(rimWorldPath ?? RimWorldPath)}" + Environment.NewLine +
                $"    SteamModContentFolder = {PowerShellLiteral(steamModContentFolder ?? SteamModContentFolder)}" + Environment.NewLine +
                "    RimWorldVersion = '1.6'" + Environment.NewLine +
                $"    DryRun = ${dryRun.ToString().ToLowerInvariant()}" + Environment.NewLine +
                $"    Output = {PowerShellLiteral(outputMode)}" + Environment.NewLine +
                (testFailCommitAfterBackupOwner is null
                    ? string.Empty
                    : $"    TestFailCommitAfterBackupOwner = {PowerShellLiteral(testFailCommitAfterBackupOwner)}{Environment.NewLine}") +
                (testCrashCommitAfterBackupOwner is null
                    ? string.Empty
                    : $"    TestCrashCommitAfterBackupOwner = {PowerShellLiteral(testCrashCommitAfterBackupOwner)}{Environment.NewLine}") +
                (testCreateRollbackDestinationOwner is null
                    ? string.Empty
                    : $"    TestCreateRollbackDestinationOwner = {PowerShellLiteral(testCreateRollbackDestinationOwner)}{Environment.NewLine}") +
                (testDeleteBackupBeforeRollbackOwner is null
                    ? string.Empty
                    : $"    TestDeleteBackupBeforeRollbackOwner = {PowerShellLiteral(testDeleteBackupBeforeRollbackOwner)}{Environment.NewLine}") +
                (testCreateCommitBackupDestinationOwner is null
                    ? string.Empty
                    : $"    TestCreateCommitBackupDestinationOwner = {PowerShellLiteral(testCreateCommitBackupDestinationOwner)}{Environment.NewLine}") +
                (testCreatePublishLiveDestinationOwner is null
                    ? string.Empty
                    : $"    TestCreatePublishLiveDestinationOwner = {PowerShellLiteral(testCreatePublishLiveDestinationOwner)}{Environment.NewLine}") +
                (testCreateRecoveryLiveDestinationOwner is null
                    ? string.Empty
                    : $"    TestCreateRecoveryLiveDestinationOwner = {PowerShellLiteral(testCreateRecoveryLiveDestinationOwner)}{Environment.NewLine}") +
                (testReplaceTemporaryWithUnownedOwner is null
                    ? string.Empty
                    : $"    TestReplaceTemporaryWithUnownedOwner = {PowerShellLiteral(testReplaceTemporaryWithUnownedOwner)}{Environment.NewLine}") +
                "}" + Environment.NewLine +
                $"& {PowerShellLiteral(scriptPath)} @parameters" + Environment.NewLine +
                "exit $LASTEXITCODE" + Environment.NewLine;
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            var startInfo = new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File {Quote(invocationPath)}",
                WorkingDirectory = root,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.EnvironmentVariables["PATH"] =
                fakeDotNetDirectory + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
            startInfo.EnvironmentVariables["RWIG_TEST_DOTNET_JOURNAL"] = dotNetJournalPath;
            startInfo.EnvironmentVariables["RWIG_TEST_DOTNET_BUILD_EXIT_CODE"] = fakeBuildExitCode.ToString();

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start bundle CLI.");
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(60000))
            {
                process.Kill();
                throw new TimeoutException("The integration-test bundle CLI did not exit within 60 seconds.");
            }

            Task.WaitAll(standardOutput, standardError);
            var output = standardOutput.Result;
            var error = standardError.Result;
            var jsonLine = output
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .SingleOrDefault(line => line.TrimStart().StartsWith("{", StringComparison.Ordinal));
            CliJson? json = null;
            if (jsonLine is not null)
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonLine));
                var serializer = new DataContractJsonSerializer(typeof(CliJson));
                json = (CliJson)(serializer.ReadObject(stream) ?? throw new SerializationException("CLI JSON was null."));
            }

            return new CliRun(process.ExitCode, output, error, json);
        }

        public void Dispose()
        {
            if (artifactsRootJunction is not null && Directory.Exists(artifactsRootJunction))
            {
                Directory.Delete(artifactsRootJunction);
            }

            if (Directory.Exists(cleanupRoot))
            {
                Directory.Delete(cleanupRoot, recursive: true);
            }
        }

        private static void CreateDirectoryJunction(string junctionPath, string targetPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/d /c mklink /J {Quote(junctionPath)} {Quote(targetPath)}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo) ??
                throw new InvalidOperationException("Failed to start mklink for the staging fixture.");
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new AssertionException(
                    $"Could not create directory junction '{junctionPath}' -> '{targetPath}'. {output} {error}");
            }

            Assert.That(
                new DirectoryInfo(junctionPath).Attributes.HasFlag(FileAttributes.ReparsePoint),
                Is.True,
                "The staging fixture must use an actual reparse point.");
        }

        private static string FindSourceRepositoryRoot()
        {
            for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
                 directory is not null;
                 directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "scripts", "Invoke-Tests.ps1")))
                {
                    return directory.FullName;
                }
            }

            throw new DirectoryNotFoundException("Could not find the source repository root.");
        }

        private static string PowerShellLiteral(string value)
        {
            return $"'{value.Replace("'", "''")}'";
        }

        private static string Quote(string value)
        {
            return $"\"{value.Replace("\"", "\\\"")}\"";
        }

        private const string FakeDotNetScript = @"Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$dotnetArguments = @($args)
if ($dotnetArguments.Count -lt 2) { exit 98 }
$command = $dotnetArguments[0]
$projectPath = [System.IO.Path]::GetFullPath($dotnetArguments[1])
[System.IO.File]::AppendAllText(
    $env:RWIG_TEST_DOTNET_JOURNAL,
    ($command + '|' + $projectPath + '|' + ($dotnetArguments -join ' ')) + [Environment]::NewLine)
if ($command -eq 'build') {
    $buildExitCode = [int]$env:RWIG_TEST_DOTNET_BUILD_EXIT_CODE
    if ($buildExitCode -ne 0) {
        [Console]::Error.WriteLine('Synthetic build failure.')
    }
    exit $buildExitCode
}
if ($command -ne 'msbuild') { exit 98 }
[xml]$project = [System.IO.File]::ReadAllText($projectPath)
$assemblyName = @($project.SelectNodes(""//*[local-name()='AssemblyName']""))[0].InnerText.Trim()
$configuration = 'Release'
foreach ($argument in $dotnetArguments) {
    if ($argument -match '^(?:-property|-p):Configuration=(.+)$') {
        $configuration = $Matches[1]
    }
}
$targetPath = Join-Path (Split-Path -Parent $projectPath) (""bin\$configuration\$assemblyName.dll"")
Write-Output $targetPath
exit 0
";
    }

    private sealed class ProjectFixture
    {
        public ProjectFixture(string projectPath, string ownerPackageId, string assemblyName)
        {
            ProjectPath = projectPath;
            OwnerPackageId = ownerPackageId;
            AssemblyName = assemblyName;
        }

        public string ProjectPath { get; }

        public string OwnerPackageId { get; }

        public string AssemblyName { get; }

        public string SourceManifestPath => Path.Combine(
            Path.GetDirectoryName(ProjectPath)!,
            AssemblyName + ".integrationtests.json");
    }

    private sealed class CliRun
    {
        public CliRun(int exitCode, string standardOutput, string standardError, CliJson? json)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
            Json = json;
        }

        public int ExitCode { get; }

        public string StandardOutput { get; }

        public string StandardError { get; }

        public CliJson? Json { get; }
    }

    [DataContract]
    private sealed class CliJson
    {
        [DataMember]
        public string Status { get; set; } = string.Empty;

        [DataMember]
        public int ProjectCount { get; set; }

        [DataMember]
        public string ArtifactsModsRoot { get; set; } = string.Empty;

        [DataMember]
        public List<CliProjectJson> Projects { get; set; } = new();
    }

    [DataContract]
    private sealed class CliProjectJson
    {
        [DataMember]
        public string OwnerPackageId { get; set; } = string.Empty;

        [DataMember]
        public string? Assembly { get; set; }

        [DataMember]
        public string? Manifest { get; set; }
    }
}
