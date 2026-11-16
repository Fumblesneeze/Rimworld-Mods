using System.Text.Json;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class GuestBedPresentationTests
{
    [Test]
    public void ReleaseProfile_IncludesBothGamePayloadsWithoutExternalAssemblies()
    {
        var profile = ReleaseProfileCatalog.Discover(TestRepository.FindRoot())
            .Single(item => item.PackageId == "fumblesneeze.guestbedgizmo");
        Assert.Multiple(() =>
        {
            Assert.That(profile.Targets.Select(target => target.Version), Is.EqualTo(new[] { "1.6", "1.7" }));
            Assert.That(profile.PackageInclude.Where(path => path.EndsWith(".dll", StringComparison.Ordinal)),
                Is.EqualTo(new[] { "1.6/Assemblies/GuestBedGizmo.dll", "1.7/Assemblies/GuestBedGizmo.dll" }));
            Assert.That(profile.Tags, Is.EquivalentTo(new[] { "Mod", "1.6", "1.7" }));
            Assert.That(profile.RequiredWorkshopItems, Is.EquivalentTo(new[] { "2009463077", "3509486825" }));
            Assert.That(profile.RequiredDlcAppIds, Is.EqualTo(new[] { "1392840" }));
        });
    }

    [Test]
    public void AnnotatedWorkshopPresentation_IsCorrectedConciseAndMenuDominant()
    {
        var root = TestRepository.FindRoot();
        var mod = Path.Combine(root, "mods", "GuestBedGizmo");
        var project = File.ReadAllText(Path.Combine(mod, "GuestBedGizmo.csproj"));
        var markdown = File.ReadAllText(Path.Combine(mod, "Release", "workshop", "description.md"));
        var bbcode = File.ReadAllText(Path.Combine(mod, "Release", "workshop", "description.bbcode.txt"));
        using var manifest = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(mod, "Release", "workshop", "presentation.json")));
        var rootElement = manifest.RootElement;
        var layout = rootElement.GetProperty("layout");
        var menu = layout.GetProperty("menuImage");

        Assert.Multiple(() =>
        {
            Assert.That(project, Does.Contain("<RimWorldModName>Hospitality + Ideology Patch</RimWorldModName>"));
            Assert.That(markdown, Does.StartWith("# Hospitality + Ideology Patch"));
            Assert.That(bbcode, Does.StartWith("[h1]Hospitality + Ideology Patch[/h1]"));
            Assert.That(markdown, Does.Contain("Safe to add or remove during a save game."));
            Assert.That(bbcode, Does.Contain("Safe to add or remove during a save game."));
            Assert.That(markdown, Does.Not.Contain("legacy").And.Not.Contain("Package:"));
            Assert.That(bbcode, Does.Not.Contain("legacy").And.Not.Contain("Package:"));
            Assert.That(layout.GetProperty("mode").GetString(), Is.EqualTo("menu-focus"));
            Assert.That(layout.TryGetProperty("image", out _), Is.False);
            Assert.That(rootElement.TryGetProperty("copy", out _), Is.False);
            Assert.That(menu.GetProperty("width").GetInt32(), Is.GreaterThanOrEqualTo(560));
            Assert.That(menu.GetProperty("height").GetInt32(), Is.GreaterThanOrEqualTo(500));
            Assert.That(rootElement.GetProperty("outputs").GetProperty("workshop").GetProperty("alt").GetString(),
                Does.Contain("Hospitality + Ideology Patch").And.Contain("colonists, prisoners, slaves, and guests"));
        });
    }
}
