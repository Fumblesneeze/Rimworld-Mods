using NUnit.Framework;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class ReleaseMatrixBuildTests
{
    [Test]
    public void Native_release_tests_reject_a_different_product_owner()
    {
        var root = TestRepository.FindRoot();
        Assert.That(() => ReleaseMatrixBuild.ValidateTestProject(root,
            "tests/RimWorldDevGateway.EndToEndTests/RimWorldDevGateway.EndToEndTests.csproj", "fumblesneeze.immersivechefs"),
            Throws.ArgumentException);
    }

    [Test]
    public void Publication_requires_a_clean_release_receipt_matching_source_targets_and_content()
    {
        var root = TestRepository.FindRoot();
        var profile = ReleaseProfileCatalog.Discover(root).Single(item => item.PackageId == "fumblesneeze.immersivechefs");
        var receipt = JsonSerializer.SerializeToNode(new { schema = "RimWorldMatrixBuild/v1", configuration = "Release",
            dirty = false, sourceRevision = "revision", profileSha256 = ReleaseCandidateBuilder.Hash(profile.Path),
            catalogSha256 = ReleaseCandidateBuilder.Hash(Path.Combine(root, "release", "rimworld-targets.json")),
            candidate = new { contentDigest = "digest" }, targets = profile.Targets.Select(target => new { target }) }, SteamGameTargets.Json)!;
        void Validate(JsonNode value) => ReleaseMatrixBuild.ValidateReceipt(JsonSerializer.SerializeToElement(value), profile, root, "digest", "revision");
        Assert.That(() => Validate(receipt), Throws.Nothing);
        foreach (var field in new[] { "sourceRevision", "profileSha256", "catalogSha256", "configuration" })
        {
            var changed = receipt.DeepClone(); changed[field] = "changed";
            Assert.That(() => Validate(changed), Throws.InvalidOperationException, field);
        }
        var dirty = receipt.DeepClone(); dirty["dirty"] = true;
        Assert.That(() => Validate(dirty), Throws.InvalidOperationException);
        var digest = receipt.DeepClone(); digest["candidate"]!["contentDigest"] = "changed";
        Assert.That(() => Validate(digest), Throws.InvalidOperationException);
        var targets = receipt.DeepClone(); targets["targets"]!.AsArray().RemoveAt(0);
        Assert.That(() => Validate(targets), Throws.InvalidOperationException);
    }

    [Test]
    public void Exact_target_native_verification_is_registered_and_preserves_prepared_product()
    {
        var root = TestRepository.FindRoot();
        Assert.That(OperationRegistry.CreateDefault(root).Descriptors.Select(item => item.Name), Does.Contain("release_target_test"));
        var command = RepositoryOperationPlanner.EndToEnd(root, null, "immersive-chefs.dirty-cookware-cooking-workflow", "English", 900, false,
            gameRoot: "F:/cache/1.6", preparedProductPackageId: "fumblesneeze.immersivechefs");
        Assert.That(command.Arguments, Does.Contain("-PreparedProductPackageId"));
        Assert.That(command.Arguments, Does.Contain(Path.GetFullPath("F:/cache/1.6")));
    }

    [Test]
    public void Bundle_has_one_own_assembly_in_every_declared_version_and_no_extra_payload()
    {
        var files = new[] { new CandidateFile("1.6/Assemblies/Product.dll", 1, "hash"), new CandidateFile("1.7/Assemblies/Product.dll", 1, "hash") };
        Assert.That(() => ReleasePackageValidator.ValidateAssemblyLayout("Product", ["1.6", "1.7"], files), Throws.Nothing);
        Assert.That(() => ReleasePackageValidator.ValidateAssemblyLayout("Product", ["1.6", "1.7"], files.Take(1).ToArray()), Throws.InvalidOperationException);
        Assert.That(() => ReleasePackageValidator.ValidateAssemblyLayout("Product", ["1.6"], files), Throws.InvalidOperationException);
        Assert.That(() => ReleasePackageValidator.ValidateAssemblyLayout("Product", ["1.6", "1.7"], [files[0], files[1] with { Path = "1.7/Assemblies/0Harmony.dll" }]), Throws.InvalidOperationException);
    }

    [Test]
    public void Target_builds_use_distinct_intermediates_outputs_and_exact_game_references()
    {
        var root = TestRepository.FindRoot();
        var profile = ReleaseProfileCatalog.Discover(root).Single(profile => profile.PackageId == "fumblesneeze.immersivechefs");
        var targets = SteamGameTargets.LoadCatalog(root);
        var run = Path.Combine(root, "artifacts", "matrix-command-fixture");
        var commands = targets.Select(target => ReleaseMatrixBuild.PlanTarget(root, profile, target,
            new GameInstallation(Path.Combine("F:/cache", target.Id), target.FullVersion, target.Version,
                "unused.exe", Path.Combine("F:/cache", target.Id, "refs"), target.Version == "1.6" ? "net480" : "net10.0"), run, "Release")).ToArray();
        foreach (var property in new[] { "BaseIntermediateOutputPath", "MSBuildProjectExtensionsPath", "OutputFolder", "RimWorldPath", "RimWorldManagedPath" })
        {
            var arguments = commands.Select(command => command.Arguments.Single(argument => argument.StartsWith("-p:" + property + "=", StringComparison.Ordinal))).ToArray();
            Assert.That(arguments[0], Is.Not.EqualTo(arguments[1]), property);
        }
        Assert.That(commands[0].Arguments, Does.Contain("-p:RimWorldVersion=1.6"));
        Assert.That(commands[1].Arguments, Does.Contain("-p:RimWorldVersion=1.7"));
    }
}
