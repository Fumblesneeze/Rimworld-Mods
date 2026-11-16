using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class SteamGameTargetsTests
{
    [Test]
    public void Matrix_selects_each_declared_version_once_and_rejects_unknown_or_ambiguous_targets()
    {
        var catalog = SteamGameTargets.LoadCatalog(TestRepository.FindRoot());
        var ids = catalog.Select(target => target.Id).ToArray();
        Assert.That(SteamGameTargets.SelectMatrix(catalog, ids, ids[1]).Select(target => target.Version), Is.EqualTo(new[] { "1.6", "1.7" }));
        Assert.That(() => SteamGameTargets.SelectMatrix(catalog, [ids[0], ids[0]], ids[0]), Throws.InvalidOperationException);
        Assert.That(() => SteamGameTargets.SelectMatrix(catalog, ["unknown"], "unknown"), Throws.InvalidOperationException);
        Assert.That(() => SteamGameTargets.SelectMatrix(catalog, [ids[0]], ids[1]), Throws.InvalidOperationException);
        Assert.That(() => SteamGameTargets.SelectMatrix([catalog[0], catalog[0] with { Id = "other-16" }], [ids[0], "other-16"], ids[0]), Throws.InvalidOperationException);
    }

    [Test]
    public void Exact_target_reuses_approved_files_and_refuses_corruption()
    {
        var root = TestRepository.FindRoot();
        var cache = Path.Combine(Path.GetTempPath(), "steam-target-" + Guid.NewGuid().ToString("N"));
        var game = Path.Combine(cache, "game");
        var managed = Path.Combine(game, "RimWorldWin64_Data", "Managed");
        Directory.CreateDirectory(managed);
        Directory.CreateDirectory(Path.Combine(game, ".DepotDownloader"));
        try
        {
            File.WriteAllText(Path.Combine(game, ".rimworld-release-cache"), "RimWorldSteamCache/v1\n" + game);
            File.WriteAllText(Path.Combine(game, "Version.txt"), "1.6.4871 rev590");
            File.WriteAllText(Path.Combine(game, "RimWorldWin64.exe"), "fixture only");
            File.WriteAllText(Path.Combine(managed, "Assembly-CSharp.dll"), "fixture only");
            File.WriteAllText(Path.Combine(managed, "UnityEngine.CoreModule.dll"), "fixture only");
            File.WriteAllText(Path.Combine(game, ".DepotDownloader", "294104_123456.manifest"), "exact manifest");
            var target = SteamGameTargets.Snapshot(root, "rw16-fixture", cache, game);
            Assert.That(SteamGameTargets.Validate(root, target, cache).Root, Is.EqualTo(game));
            var injected = Path.Combine(managed, "Undeclared.dll");
            File.WriteAllText(injected, "unapproved compiler/runtime input");
            Assert.That(() => SteamGameTargets.Validate(root, target, cache), Throws.InvalidOperationException.With.Message.Contains("inventory"));
            File.Delete(injected);
            File.WriteAllText(Path.Combine(managed, "Assembly-CSharp.dll"), "corrupted");
            Assert.That(() => SteamGameTargets.Validate(root, target, cache),
                Throws.InvalidOperationException.With.Message.Contains("changed"));
        }
        finally { Directory.Delete(cache, true); }
    }
}
