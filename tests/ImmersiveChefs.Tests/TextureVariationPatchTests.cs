using System.IO;
using System.Reflection;
using System.Xml.Linq;
using NUnit.Framework;
using Verse;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class TextureVariationPatchTests
{
    [Test]
    public void Package_gate_is_a_public_nested_patch_operation()
    {
        var type = typeof(PatchOperationTextureVariations);
        var match = type.GetField("match", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.Multiple(() =>
        {
            Assert.That(type.IsPublic, Is.True);
            Assert.That(type.IsSealed, Is.True);
            Assert.That(type.BaseType, Is.EqualTo(typeof(PatchOperation)));
            Assert.That(match?.FieldType, Is.EqualTo(typeof(PatchOperation)));
        });
    }

    [Test]
    public void Portable_selector_patch_is_bounded_to_the_four_supported_defs()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Patches",
            "TextureVariations.xml"));
        var operation = document.Root!.Element("Operation")!;
        var replacements = operation
            .Descendants("li")
            .Where(element =>
                (string?)element.Attribute("Class") == "PatchOperationReplace")
            .ToList();
        var expectedDefNames = new[]
        {
            "ImmersiveChefs_Cookware",
            "ImmersiveChefs_Plate",
            "ImmersiveChefs_Cutlery",
            "ImmersiveChefs_ChefsKnife"
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                (string?)operation.Attribute("Class"),
                Is.EqualTo("ImmersiveChefs.PatchOperationTextureVariations"));
            Assert.That(replacements, Has.Count.EqualTo(expectedDefNames.Length));
            foreach (var defName in expectedDefNames)
            {
                var replacement = replacements.Single(element =>
                    ((string?)element.Element("xpath"))?.Contains(defName) == true);
                Assert.That(
                    (string?)replacement.Element("value")?.Element("graphicClass"),
                    Is.EqualTo("ImmersiveChefs.Graphic_PortableKitchenwareVariation"));
            }
        });
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ImmersiveChefs.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Immersive Chefs repository root.");
    }
}
