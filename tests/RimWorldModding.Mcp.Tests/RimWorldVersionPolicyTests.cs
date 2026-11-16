using NUnit.Framework;
using System.Text.Json;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class RimWorldVersionPolicyTests
{
    [Test]
    public void Release_contract_has_no_game_revision_build_or_hash_pins()
    {
        foreach (var forbidden in new[] { "RimWorldBuild", "RimWorldRuntimeBuild", "SteamBuildId", "ManagedAssemblySha256" })
            Assert.That(typeof(ReleaseProfile).GetProperty(forbidden), Is.Null, forbidden);
    }

    [Test]
    public void Target_ids_are_only_major_minor_versions()
    {
        foreach (var target in SteamGameTargets.LoadCatalog(TestRepository.FindRoot()))
            Assert.That(target.Id, Is.EqualTo(target.Version));
    }

    [Test]
    public void Major_minor_target_schema_is_accepted()
    {
        var target = JsonSerializer.Deserialize<SteamGameTarget>(
            """{"id":"1.6","version":"1.6","cacheDirectory":"1.6","branch":"version-1.6"}""", SteamGameTargets.Json)!;
        Assert.DoesNotThrow(() => SteamGameTargets.ValidateDeclaration(target));
    }

    [TestCase("manifestId")]
    [TestCase("depotId")]
    [TestCase("fullVersion")]
    [TestCase("runtimeVersion")]
    [TestCase("steamBuildId")]
    public void Downloader_rejects_game_pin_arguments_before_running(string forbidden)
    {
        var args = JsonSerializer.SerializeToElement(new Dictionary<string, object> { ["targetId"] = "1.6", [forbidden] = "123" });
        Assert.That(async () => await OperationRegistry.CreateDefault(TestRepository.FindRoot())
            .InvokeAsync("game_depot_download", args, CancellationToken.None), Throws.ArgumentException.With.Message.Contains("forbidden"));
    }

    [TestCase("game_target_validate")]
    [TestCase("release_matrix_build")]
    [TestCase("release_target_test")]
    [TestCase("game_installation_inspect")]
    public void Game_operations_reject_obsolete_pin_arguments(string operation)
    {
        var args = JsonSerializer.SerializeToElement(new { targetId = "1.6", fullVersion = "1.6.4871 rev590" });
        Assert.That(async () => await OperationRegistry.CreateDefault(TestRepository.FindRoot())
            .InvokeAsync(operation, args, CancellationToken.None), Throws.ArgumentException.With.Message.Contains("forbidden"));
    }

    [TestCase("rimWorldBuild")]
    [TestCase("rimWorldRuntimeBuild")]
    [TestCase("steamBuildId")]
    [TestCase("managedAssemblySha256")]
    public void Release_profile_rejects_game_pins(string forbidden)
    {
        var root = TestRepository.FindRoot();
        var source = Path.Combine(root, "mods", "ImmersiveChefs", "Release", "release.json");
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(source))!;
        data[forbidden] = JsonSerializer.SerializeToElement("forbidden");
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try {
            File.WriteAllText(path, JsonSerializer.Serialize(data));
            Assert.That(() => ReleaseProfileCatalog.Load(root, path, false),
                Throws.TypeOf<ReleaseProfileException>().With.Message.Contains("unknown"));
        } finally { File.Delete(path); }
    }

    [TestCase("fullVersion")]
    [TestCase("runtimeVersion")]
    [TestCase("inventorySha256")]
    [TestCase("manifestId")]
    public void Target_schema_rejects_attempted_game_pins(string forbidden)
    {
        var json = JsonSerializer.Serialize(new Dictionary<string, object> {
            ["id"] = "1.6", ["version"] = "1.6", ["cacheDirectory"] = "1.6", ["branch"] = "version-1.6", [forbidden] = "forbidden"
        });
        Assert.That(() => JsonSerializer.Deserialize<SteamGameTarget>(json, SteamGameTargets.Json), Throws.TypeOf<JsonException>());
    }
}
