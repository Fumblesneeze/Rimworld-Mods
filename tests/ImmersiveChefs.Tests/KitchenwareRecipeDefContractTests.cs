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

    [Test]
    public void Chefs_knife_is_a_belt_apparel_without_mutable_sanitation()
    {
        var document = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "Kitchenware.xml"));
        var knife = document.Root?.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_ChefsKnife");

        Assert.Multiple(() =>
        {
            Assert.That((string?)knife?.Element("thingClass"), Is.EqualTo("Apparel"));
            Assert.That(
                knife?.Element("apparel")?.Element("bodyPartGroups")?.Elements("li")
                    .Select(element => element.Value),
                Does.Contain("Waist"));
            Assert.That(
                knife?.Element("apparel")?.Element("layers")?.Elements("li")
                    .Select(element => element.Value),
                Does.Contain("Belt"));
            Assert.That(
                knife?.Element("comps")?.Elements("li")
                    .Any(element =>
                        (string?)element.Attribute("Class") ==
                        "ImmersiveChefs.CompProperties_Sanitation"),
                Is.False);
        });
    }

    [Test]
    public void Imported_meals_have_a_dedicated_cooking_work_queue_and_dining_gate()
    {
        var root = FindRepositoryRoot();
        var jobs = XDocument.Load(Path.Combine(
            root, "mods", "ImmersiveChefs", "Defs", "JobDefs", "PlatingJobs.xml"));
        var workGivers = XDocument.Load(Path.Combine(
            root, "mods", "ImmersiveChefs", "Defs", "WorkGiverDefs", "PlatingWorkGivers.xml"));
        var source = File.ReadAllText(Path.Combine(
            root, "mods", "ImmersiveChefs", "Source", "Production", "ImportedMealPlating.cs"));

        var job = jobs.Root?.Elements("JobDef")
            .Single(element => (string?)element.Element("defName") == "ImmersiveChefs_PlateMeals");
        var workGiver = workGivers.Root?.Elements("WorkGiverDef")
            .Single(element => (string?)element.Element("defName") == "ImmersiveChefs_PlateMeals");

        Assert.Multiple(() =>
        {
            Assert.That((string?)job?.Element("driverClass"),
                Is.EqualTo("ImmersiveChefs.JobDriver_PlateMeals"));
            Assert.That((string?)workGiver?.Element("giverClass"),
                Is.EqualTo("ImmersiveChefs.WorkGiver_PlateMeals"));
            Assert.That((string?)workGiver?.Element("workType"), Is.EqualTo("Cooking"));
            Assert.That(source, Does.Contain("IsFoodSourceOnMapSociallyProper"));
            Assert.That(source, Does.Contain("RecordFailedPlatingOpportunity"));
        });
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
