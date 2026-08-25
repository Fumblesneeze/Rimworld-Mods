using System.IO;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class PreGameFactionSafetyTests
{
    [Test]
    public void Kitchenware_alert_scan_does_not_log_when_the_player_faction_is_not_initialized()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Source",
            "Dining",
            "KitchenwareAlerts.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(
                source,
                Does.Contain("station.Faction == Faction.OfPlayerSilentFail"));
            Assert.That(
                source,
                Does.Not.Contain("station.Faction == Faction.OfPlayer;"));
        });
    }

    [Test]
    public void Generated_pawn_inventory_patch_does_not_log_before_the_player_faction_exists()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Source",
            "Production",
            "GeneratedMealPlatingHarmonyPatches.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(
                source,
                Does.Contain("__0.Faction == Faction.OfPlayerSilentFail"));
            Assert.That(
                source,
                Does.Not.Contain("__0.Faction == Faction.OfPlayer)"));
        });
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "RimWorldMods.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Immersive Chefs repository root.");
    }
}
