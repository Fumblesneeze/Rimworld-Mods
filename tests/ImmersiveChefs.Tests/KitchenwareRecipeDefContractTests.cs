using System.IO;
using System.Xml.Linq;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class KitchenwareRecipeDefContractTests
{
    [Test]
    public void Kitchenware_unit_costs_ignore_vanilla_small_volume_scaling()
    {
        var getter = new IngredientValueGetter_Units();
        var normalVolume = (ThingDef)System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(typeof(ThingDef));
        var smallVolume = (ThingDef)System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(typeof(ThingDef));
        normalVolume.smallVolume = false;
        smallVolume.smallVolume = true;

        Assert.Multiple(() =>
        {
            Assert.That(getter.ValuePerUnitOf(normalVolume), Is.EqualTo(1f));
            Assert.That(getter.ValuePerUnitOf(smallVolume), Is.EqualTo(1f));
        });
    }

    [Test]
    public void Kitchenware_recipes_require_the_work_type_of_their_vanilla_bill_giver()
    {
        var document = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "mods",
            "ImmersiveChefs",
            "Defs",
            "RecipeDefs",
            "KitchenwareRecipes.xml"));

        var expected = new[]
        {
            ("ImmersiveChefs_MakePrimitiveCookware", "Crafting"),
            ("ImmersiveChefs_MakeMedievalCookware", "Smithing"),
            ("ImmersiveChefs_MakeModernCookware", "Smithing"),
            ("ImmersiveChefs_MakeChefsKnife", "Smithing"),
            ("ImmersiveChefs_MakeSoftPlates", "Crafting"),
            ("ImmersiveChefs_MakeSoftCutlery", "Crafting"),
            ("ImmersiveChefs_SmithPlates", "Smithing"),
            ("ImmersiveChefs_SmithCutlery", "Smithing"),
            ("ImmersiveChefs_MachinePlates", "Smithing"),
            ("ImmersiveChefs_MachineCutlery", "Smithing")
        };

        Assert.That(
            expected.Select(pair => (pair.Item1, WorkType: RequiredWorkType(document, pair.Item1))),
            Is.EqualTo(expected.Select(pair => (pair.Item1, WorkType: (string?)pair.Item2))));
        Assert.That(
            (string?)document.Root?.Elements("RecipeDef")
                .Single(element =>
                    (string?)element.Attribute("Name") == "ImmersiveChefs_KitchenwareRecipeBase")
                .Element("ingredientValueGetterClass"),
            Is.EqualTo("ImmersiveChefs.IngredientValueGetter_Units"));
    }

    private static string? RequiredWorkType(XDocument document, string defName)
    {
        var recipe = document.Root?.Elements("RecipeDef")
            .Single(element => (string?)element.Element("defName") == defName);
        var directValue = (string?)recipe?.Element("requiredGiverWorkType");
        if (!string.IsNullOrWhiteSpace(directValue))
        {
            return directValue;
        }

        var parentName = (string?)recipe?.Attribute("ParentName");
        return (string?)document.Root?.Elements("RecipeDef")
            .Single(element => (string?)element.Attribute("Name") == parentName)
            .Element("requiredGiverWorkType");
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
