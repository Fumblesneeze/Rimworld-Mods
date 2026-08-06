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

    [Test]
    public void Building_variation_patch_adds_one_exact_VEF_comp_to_each_owned_building()
    {
        var root = FindRepositoryRoot();
        var texturePatch = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Patches",
            "TextureVariations.xml"));
        var microwavePatch = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Patches",
            "Compatibility",
            "ThermodynamicsHotMeals.xml"));
        var expected = new Dictionary<string, (string BasePath, string VariantPath, string BaseName, string VariantName)>
        {
            ["ImmersiveChefs_Dishwasher"] =
                ("ImmersiveChefs/Things/Building/Dishwasher/Dishwasher", "ImmersiveChefs/Things/Building/Dishwasher/Dishwasher_Variant01", "standard dishwasher", "alternate dishwasher"),
            ["ImmersiveChefs_IndustrialDishwasher"] =
                ("ImmersiveChefs/Things/Building/Dishwasher/IndustrialDishwasher", "ImmersiveChefs/Things/Building/Dishwasher/IndustrialDishwasher_Variant01", "standard industrial dishwasher", "alternate industrial dishwasher"),
            ["ImmersiveChefs_PrepStation"] =
                ("ImmersiveChefs/Things/Building/KitchenStation/PrepStation", "ImmersiveChefs/Things/Building/KitchenStation/PrepStation_Variant01", "standard prep station", "alternate prep station"),
            ["ImmersiveChefs_SauceStation"] =
                ("ImmersiveChefs/Things/Building/KitchenStation/SauceStation", "ImmersiveChefs/Things/Building/KitchenStation/SauceStation_Variant01", "standard sauce station", "alternate sauce station"),
            ["ImmersiveChefs_MeatStation"] =
                ("ImmersiveChefs/Things/Building/KitchenStation/MeatStation", "ImmersiveChefs/Things/Building/KitchenStation/MeatStation_Variant01", "standard meat station", "alternate meat station"),
            ["ImmersiveChefs_VegetableStation"] =
                ("ImmersiveChefs/Things/Building/KitchenStation/VegetableStation", "ImmersiveChefs/Things/Building/KitchenStation/VegetableStation_Variant01", "standard vegetable station", "alternate vegetable station"),
            ["ImmersiveChefs_PastryStation"] =
                ("ImmersiveChefs/Things/Building/KitchenStation/PastryStation", "ImmersiveChefs/Things/Building/KitchenStation/PastryStation_Variant01", "standard pastry station", "alternate pastry station"),
            ["ImmersiveChefs_Microwave"] =
                ("ImmersiveChefs/Things/Building/Appliance/Microwave", "ImmersiveChefs/Things/Building/Appliance/Microwave_Variant01", "standard microwave", "alternate microwave")
        };
        var documents = new[] { texturePatch, microwavePatch };

        foreach (var (defName, variant) in expected)
        {
            var additions = documents
                .SelectMany(document => document.Descendants("li"))
                .Where(element =>
                    (string?)element.Attribute("Class") == "PatchOperationAdd" &&
                    ((string?)element.Element("xpath"))?.IndexOf(
                        $"defName=\"{defName}\"",
                        StringComparison.Ordinal) >= 0)
                .ToList();
            Assert.That(additions, Has.Count.EqualTo(1), defName);

            var comp = additions[0]
                .Element("value")!
                .Descendants("li")
                .Single(element =>
                    (string?)element.Attribute("Class") ==
                    "VEF.Buildings.CompProperties_RandomBuildingGraphic");
            Assert.Multiple(() =>
            {
                Assert.That(
                    comp.Element("randomGraphics")?.Elements("li").Select(value => value.Value),
                    Is.EqualTo(new[] { variant.BasePath, variant.VariantPath }),
                    defName + " randomGraphics");
                Assert.That(
                    comp.Element("optionalNames")?.Elements("li").Select(value => value.Value),
                    Is.EqualTo(new[] { variant.BaseName, variant.VariantName }),
                    defName + " optionalNames");
            });
        }
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
