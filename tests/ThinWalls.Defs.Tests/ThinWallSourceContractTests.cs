using System.IO;
using System.Reflection;
using System.Xml.Linq;
using NUnit.Framework;

namespace ThinWalls.Defs.Tests;

[TestFixture]
public sealed class ThinWallSourceContractTests
{
    [Test]
    public void ThinWallDefIsHalfStrengthStandableNonEdificeAndNeverSupportsRoofs()
    {
        XDocument document = XDocument.Load(SourcePath("Defs", "ThingDefs", "ThinWalls.xml"));
        XElement def = document.Root!.Elements("ThingDef")
            .Single(candidate => (string?)candidate.Element("defName") == "TW_ThinWall");

        Assert.Multiple(() =>
        {
            Assert.That((string?)def.Element("thingClass"), Is.EqualTo("ThinWalls.Buildings.Building_ThinWall"));
            Assert.That((string?)def.Element("canGenerateDefaultDesignator"), Is.EqualTo("false"));
            Assert.That((string?)def.Element("costStuffCount"), Is.EqualTo("3"));
            Assert.That((string?)def.Element("statBases")?.Element("MaxHitPoints"), Is.EqualTo("150"));
            Assert.That((string?)def.Element("passability"), Is.EqualTo("Standable"));
            Assert.That((string?)def.Element("holdsRoof"), Is.EqualTo("false"));
            Assert.That((string?)def.Element("building")?.Element("isEdifice"), Is.EqualTo("false"));
            Assert.That((string?)def.Element("building")?.Element("allowAutoroof"), Is.EqualTo("false"));
            Assert.That(def.Element("stuffCategories")?.Elements("li").Select(item => item.Value),
                Is.EquivalentTo(new[] { "Metallic", "Woody", "Stony" }));
        });
    }

    [Test]
    public void ThinDoorIsOneEdgeOwnedHalfSimpleDoorAndLeavesItsCellUsable()
    {
        XDocument document = XDocument.Load(SourcePath("Defs", "ThingDefs", "ThinWalls.xml"));
        XElement def = document.Root!.Elements("ThingDef")
            .Single(candidate => (string?)candidate.Element("defName") == "TW_ThinDoor");

        Assert.Multiple(() =>
        {
            Assert.That((string?)def.Attribute("ParentName"), Is.EqualTo("DoorBase"));
            Assert.That((string?)def.Element("thingClass"), Is.EqualTo("ThinWalls.Buildings.Building_ThinDoor"));
            Assert.That((string?)def.Element("drawerType"), Is.EqualTo("MapMeshAndRealTime"),
                "the realtime door must also print its canonical fixed endpoint frames into the cached section mesh");
            Assert.That((string?)def.Element("canGenerateDefaultDesignator"), Is.EqualTo("false"));
            Assert.That((string?)def.Element("costStuffCount"), Is.EqualTo("13"));
            Assert.That((string?)def.Element("statBases")?.Element("MaxHitPoints"), Is.EqualTo("80"));
            Assert.That((string?)def.Element("passability"), Is.EqualTo("Standable"));
            Assert.That((string?)def.Element("fillPercent"), Is.EqualTo("0"));
            Assert.That((string?)def.Element("holdsRoof"), Is.EqualTo("false"));
            Assert.That((string?)def.Element("blockWind"), Is.EqualTo("false"));
            Assert.That((string?)def.Element("building")?.Element("isEdifice"), Is.EqualTo("false"));
            Assert.That((string?)def.Element("building")?.Element("allowAutoroof"), Is.EqualTo("false"));
        });
    }

    [Test]
    public void StructureReceivesOneSpecialDesignatorAndLineOnlyDrawStyle()
    {
        XDocument definitions = XDocument.Load(SourcePath("Defs", "MiscDefs", "ThinWallDrawStyle.xml"));
        XElement style = definitions.Root!.Element("DrawStyleCategoryDef")!;
        XDocument patch = XDocument.Load(SourcePath("Patches", "StructureDesignator.xml"));

        Assert.Multiple(() =>
        {
            Assert.That((string?)style.Element("defName"), Is.EqualTo("TW_ThinWallLine"));
            Assert.That(style.Element("styles")?.Elements("li").Select(item => item.Value), Is.EqualTo(new[] { "Line" }));
            Assert.That(patch.Descendants("xpath").Single().Value,
                Is.EqualTo("Defs/DesignationCategoryDef[defName=\"Structure\"]/specialDesignatorClasses"));
            Assert.That(patch.Descendants("li").Select(item => item.Value), Is.EqualTo(new[]
            {
                "ThinWalls.Designation.Designator_ThinWall",
                "ThinWalls.Designation.Designator_ThinDoor",
            }));
        });
    }

    [Test]
    public void EdgeBashingUsesAnOwnedJobDriverInsteadOfVanillaTouchPathing()
    {
        XDocument document = XDocument.Load(SourcePath("Defs", "JobDefs", "ThinWallJobs.xml"));
        XElement def = document.Root!.Elements("JobDef")
            .Single(candidate => (string?)candidate.Element("defName") == "TW_AttackThinWallEdge");

        Assert.Multiple(() =>
        {
            Assert.That((string?)def.Element("driverClass"),
                Is.EqualTo("ThinWalls.Pathing.JobDriver_AttackThinWallEdge"));
            Assert.That((string?)def.Element("alwaysShowWeapon"), Is.EqualTo("true"));
            Assert.That((string?)def.Element("casualInterruptible"), Is.EqualTo("false"));
        });
    }

    private static string SourcePath(params string[] parts)
    {
        string repositoryRoot = typeof(ThinWallSourceContractTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RepositoryRoot")
            .Value;
        return Path.Combine(new[] { repositoryRoot, "mods", "ThinWalls" }.Concat(parts).ToArray());
    }
}
