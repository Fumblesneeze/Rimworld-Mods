using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class XmlExtensionsPatchContractTests
{
    [Test]
    public void Expanded_masonry_patch_matches_the_canonical_package_id()
    {
        var patch = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "mods",
            "ImmersiveChefs",
            "Patches",
            "ExpandedMasonryPatches.xml"));
        var operation = patch.Root?.Element("Operation");
        var shapeGuard = operation?.Element("caseTrue")?.Element("Operation");

        Assert.Multiple(() =>
        {
            Assert.That(operation?.Attribute("Class")?.Value, Is.EqualTo("XmlExtensions.FindMod"));
            Assert.That(operation?.Element("packageId")?.Value, Is.EqualTo("true"));
            Assert.That(
                operation?.Element("mods")?.Elements("li").Select(element => element.Value),
                Is.EqualTo(new[] { "Argon.ExpandedMaterials.Masonry" }));
            Assert.That(shapeGuard?.Attribute("Class")?.Value, Is.EqualTo("PatchOperationConditional"));
            Assert.That(shapeGuard?.Element("xpath")?.Value, Is.EqualTo("/Defs[ThingDef/defName=\"EM_AdobeBricks\"]"));
            Assert.That(
                shapeGuard?.Element("match")?.Descendants("RecipeDef")
                    .SingleOrDefault(element => element.Element("defName")?.Value == "ImmersiveChefs_MakeAdobePlates"),
                Is.Not.Null);
            Assert.That(shapeGuard?.Element("nomatch")?.Attribute("Class")?.Value, Is.EqualTo("PatchOperationSequence"));
            Assert.That(shapeGuard?.Element("nomatch")?.Element("operations"), Is.Not.Null);
        });
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RimWorldMods.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
