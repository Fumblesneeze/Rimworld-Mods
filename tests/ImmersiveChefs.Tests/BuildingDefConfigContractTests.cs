using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class BuildingDefConfigContractTests
{
    [Test]
    public void Minifiable_kitchen_support_buildings_author_mass_and_a_haul_category()
    {
        var root = FindRepositoryRoot();
        var stations = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "AssistantStations.xml"));
        var stationBase = stations.Descendants("ThingDef")
            .Single(element => (string?)element.Attribute("Name") == "ImmersiveChefs_AssistantStationBase");

        var thermodynamicsPatch = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Patches",
            "Compatibility",
            "ThermodynamicsHotMeals.xml"));
        var microwave = thermodynamicsPatch.Descendants("ThingDef")
            .Single(element => (string?)element.Element("defName") == "ImmersiveChefs_Microwave");

        Assert.Multiple(() =>
        {
            Assert.That((string?)stationBase.Element("statBases")?.Element("Mass"), Is.EqualTo("20"),
                "A 2x1 station is anchored to Core's 20 kg minifiable tool cabinet.");
            Assert.That(
                stationBase.Element("thingCategories")?.Elements("li").Select(element => element.Value),
                Does.Contain("BuildingsMisc"));
            Assert.That((string?)microwave.Element("statBases")?.Element("Mass"), Is.EqualTo("10"),
                "A 1x1 countertop appliance is anchored between Core furniture and the tool cabinet.");
            Assert.That(
                microwave.Element("thingCategories")?.Elements("li").Select(element => element.Value),
                Does.Contain("BuildingsMisc"));
        });
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ImmersiveChefs.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
