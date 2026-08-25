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

    [Test]
    public void Every_approved_visually_sink_bearing_station_declares_the_runtime_sink_capability()
    {
        var root = FindRepositoryRoot();
        var approvals = XDocument.Load(Path.Combine(root, "docs", "DirectionalSpriteApprovals.xml"));
        var sinkFamilies = approvals.Descendants("frame")
            .Where(frame => ((string?)frame.Attribute("equipmentOrder"))?
                .Split('|').Any(part => part == "sink") == true)
            .Select(frame => Path.GetFileNameWithoutExtension((string)frame.Attribute("path")!)!
                .Replace("_Variant01", string.Empty)
                .Replace("_north", string.Empty)
                .Replace("_east", string.Empty)
                .Replace("_south", string.Empty)
                .Replace("_west", string.Empty))
            .Distinct()
            .ToArray();
        var stationDefs = new[]
        {
            XDocument.Load(Path.Combine(root, "mods", "ImmersiveChefs", "Defs", "ThingDefs", "PreparedFoodAndStation.xml")),
            XDocument.Load(Path.Combine(root, "mods", "ImmersiveChefs", "Defs", "ThingDefs", "AssistantStations.xml"))
        }.SelectMany(document => document.Descendants("ThingDef"))
            .Where(def => def.Element("defName") is not null)
            .ToArray();
        var capableDefs = stationDefs
            .Where(def => def.Descendants("li").Any(extension =>
                (string?)extension.Attribute("Class") == "ImmersiveChefs.IntegratedSinkExtension"))
            .Select(def => (string)def.Element("defName")!)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(sinkFamilies, Is.EqualTo(new[] { "PrepStation" }),
                "Only art that visibly contains a sink may drive this contract.");
            Assert.That(capableDefs, Is.EqualTo(new[] { "ImmersiveChefs_PrepStation" }));
        });
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "RimWorldMods.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
