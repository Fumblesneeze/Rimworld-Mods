using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NUnit.Framework;

namespace ThinWalls.Defs.Tests;

[TestFixture]
public sealed class ThinWallLocalizationContractTests
{
    private static readonly string[] RequiredKeyed =
    {
        "TW_AlreadyOnEdge",
        "TW_BuildingCrossesWall",
        "TW_DesignatorDescription",
        "TW_DesignatorLabel",
        "TW_DoorNeedsExclusiveEdge",
        "TW_InspectEdge",
        "TW_ModName",
        "TW_ThinDoorDesignatorDescription",
        "TW_ThinDoorDesignatorLabel",
        "TW_WallCrossesBuilding",
    };

    private static readonly string[] RequiredDefInjected =
    {
        "TW_ThinDoor.description",
        "TW_ThinDoor.label",
        "TW_ThinWall.description",
        "TW_ThinWall.label",
    };

    private static readonly string[] RequiredJobDefInjected =
    {
        "TW_AttackThinWallEdge.reportString",
    };

    [Test]
    public void EnglishAndGermanCatalogsMatchTheExactPlayerFacingInventory()
    {
        Dictionary<string, string> english = Load("English", "Keyed", "ThinWalls.xml");
        Dictionary<string, string> german = Load("German", "Keyed", "ThinWalls.xml");

        Assert.Multiple(() =>
        {
            Assert.That(english.Keys, Is.EqualTo(RequiredKeyed));
            Assert.That(german.Keys, Is.EqualTo(RequiredKeyed));
            foreach (string key in RequiredKeyed)
            {
                Assert.That(Placeholders(german[key]), Is.EqualTo(Placeholders(english[key])),
                    key + " must preserve format placeholders.");
                Assert.That(german[key], Is.Not.Empty, key + " must have a contextual German translation.");
            }
        });
    }

    [Test]
    public void EnglishAndGermanDefInjectedCatalogsCoverTheThinWallDefExactly()
    {
        Dictionary<string, string> english = Load("English", "DefInjected", "ThingDef", "ThinWall.xml");
        Dictionary<string, string> german = Load("German", "DefInjected", "ThingDef", "ThinWall.xml");

        Assert.Multiple(() =>
        {
            Assert.That(english.Keys, Is.EqualTo(RequiredDefInjected));
            Assert.That(german.Keys, Is.EqualTo(RequiredDefInjected));
            Assert.That(english.Values.All(value => !string.IsNullOrWhiteSpace(value)), Is.True);
            Assert.That(german.Values.All(value => !string.IsNullOrWhiteSpace(value)), Is.True);
        });
    }

    [Test]
    public void EnglishAndGermanDefInjectedCatalogsCoverTheEdgeMeleeJobExactly()
    {
        Dictionary<string, string> english = Load("English", "DefInjected", "JobDef", "ThinWallJobs.xml");
        Dictionary<string, string> german = Load("German", "DefInjected", "JobDef", "ThinWallJobs.xml");

        Assert.Multiple(() =>
        {
            Assert.That(english.Keys, Is.EqualTo(RequiredJobDefInjected));
            Assert.That(german.Keys, Is.EqualTo(RequiredJobDefInjected));
            Assert.That(english.Values.All(value => !string.IsNullOrWhiteSpace(value)), Is.True);
            Assert.That(german.Values.All(value => !string.IsNullOrWhiteSpace(value)), Is.True);
            Assert.That(Placeholders(german.Values.Single()), Is.EqualTo(Placeholders(english.Values.Single())));
        });
    }

    private static Dictionary<string, string> Load(string language, params string[] parts)
    {
        string root = typeof(ThinWallLocalizationContractTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RepositoryRoot")
            .Value;
        string path = Path.Combine(new[] { root, "mods", "ThinWalls", "Languages", language }
            .Concat(parts).ToArray());
        return XDocument.Load(path).Root!.Elements()
            .OrderBy(element => element.Name.LocalName)
            .ToDictionary(element => element.Name.LocalName, element => element.Value);
    }

    private static string[] Placeholders(string value) => Regex.Matches(value, @"\{\d+\}")
        .Cast<Match>()
        .Select(match => match.Value)
        .OrderBy(value => value)
        .ToArray();
}
