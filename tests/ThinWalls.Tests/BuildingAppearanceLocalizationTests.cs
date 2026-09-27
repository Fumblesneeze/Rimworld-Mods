using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NUnit.Framework;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class BuildingAppearanceLocalizationTests
{
    [TestCase("English")]
    [TestCase("German")]
    [TestCase("Spanish")]
    [TestCase("French")]
    [TestCase("ChineseSimplified")]
    [TestCase("Russian")]
    [TestCase("Japanese")]
    public void AppearanceControlsHaveCompleteLocalizedLabelsAndDescriptions(string language)
    {
        DirectoryInfo? root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "RimWorldMods.sln"))) root = root.Parent;
        Assert.That(root, Is.Not.Null);
        string folder = Path.Combine(root!.FullName, "mods", "ThinWalls", "Languages");
        string[] expected = { "TW_ShrinkBuilding", "TW_ShrinkBuildingDesc", "TW_OffsetBuilding", "TW_OffsetBuildingDesc",
            "TW_ShowShrinkGizmo", "TW_ShowOffsetGizmo", "TW_AppearanceSettingsHint", "TW_OffsetCenter",
            "TW_OffsetN", "TW_OffsetNE", "TW_OffsetE", "TW_OffsetSE", "TW_OffsetS", "TW_OffsetSW", "TW_OffsetW", "TW_OffsetNW" };
        var elements = XDocument.Load(Path.Combine(folder, language, "Keyed", "BuildingAppearance.xml")).Root!.Elements().ToArray();
        Assert.That(elements.Select(x => x.Name.LocalName), Is.EquivalentTo(expected));
        Assert.That(elements.All(x => !string.IsNullOrWhiteSpace(x.Value)), Is.True);
        Assert.That(elements.All(x => !Regex.IsMatch(x.Value, @"\{\d+\}")), Is.True);
    }
}
