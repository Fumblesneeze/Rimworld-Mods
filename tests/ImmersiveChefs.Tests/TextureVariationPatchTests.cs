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
    public void Sanitation_selector_is_base_owned_and_optional_patch_only_adds_variations()
    {
        var root = FindRepositoryRoot();
        var kitchenware = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "Kitchenware.xml"));
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
        var graphicReplacements = replacements
            .Where(element => ((string?)element.Element("xpath"))?.EndsWith(
                "/graphicData/graphicClass",
                StringComparison.Ordinal) == true)
            .ToList();
        var baseDirtyDefNames = new[]
        {
            "ImmersiveChefs_PrimitiveCookware",
            "ImmersiveChefs_Cookware",
            "ImmersiveChefs_Plate",
            "ImmersiveChefs_AdobePlate",
            "ImmersiveChefs_Cutlery"
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                (string?)operation.Attribute("Class"),
                Is.EqualTo("ImmersiveChefs.PatchOperationTextureVariations"));
            Assert.That(graphicReplacements, Is.Empty,
                "Dirty rendering is base behavior and must not depend on the optional VTEX patch.");
            foreach (var defName in baseDirtyDefNames)
            {
                Assert.That(
                    (string?)kitchenware.Descendants("ThingDef")
                        .Single(definition => (string?)definition.Element("defName") == defName)
                        .Element("graphicData")?
                        .Element("graphicClass"),
                    Is.EqualTo("ImmersiveChefs.Graphic_PortableKitchenwareVariation"),
                    defName + " must select its dirty sibling without an optional mod.");
            }
            Assert.That(replacements, Is.Empty,
                "The optional VTEX patch should add only the optional building variation comps.");
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
