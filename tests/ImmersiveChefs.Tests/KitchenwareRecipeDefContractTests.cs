using System.IO;
using System.Xml.Linq;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class KitchenwareRecipeDefContractTests
{
    [Test]
    public void Existing_kitchenware_selects_the_recipe_tier_matching_its_actual_material()
    {
        var allPlateTiersInCoreEnumerationOrder = new[]
        {
            FabricationTier.PrimitiveStone,
            FabricationTier.Soft,
            FabricationTier.Intermediate,
            FabricationTier.Modern
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                KitchenwareIngredientInfoPolicy.PreferredFabricationTier(
                    KitchenwareProduct.Plate,
                    new KitchenMaterialClassification(KitchenMaterialKind.Steel, FabricationTier.Modern),
                    allPlateTiersInCoreEnumerationOrder),
                Is.EqualTo(FabricationTier.Modern),
                "A steel Thing must not inherit the first primitive producing recipe.");
            Assert.That(
                KitchenwareIngredientInfoPolicy.PreferredFabricationTier(
                    KitchenwareProduct.Plate,
                    new KitchenMaterialClassification(KitchenMaterialKind.Bronze, FabricationTier.Intermediate),
                    allPlateTiersInCoreEnumerationOrder),
                Is.EqualTo(FabricationTier.Intermediate),
                "An exact-era recipe should win over the universal machining fallback.");
        });
    }

    [Test]
    public void Kitchenware_unit_costs_honor_vanilla_small_volume_scaling()
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
            Assert.That(getter.ValuePerUnitOf(smallVolume), Is.EqualTo(0.1f));
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
            ("ImmersiveChefs_MakePrimitivePlates", "Crafting"),
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
    public void Stone_block_plates_are_a_distinct_primitive_crafting_path()
    {
        var root = FindRepositoryRoot();
        var things = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "Kitchenware.xml"));
        var recipes = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "RecipeDefs",
            "KitchenwareRecipes.xml"));
        var plate = things.Root?.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_Plate");
        var primitiveRecipe = recipes.Root?.Elements("RecipeDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_MakePrimitivePlates");

        Assert.Multiple(() =>
        {
            Assert.That(
                plate?.Element("stuffCategories")?.Elements("li").Select(element => element.Value),
                Does.Contain("Stony"));
            Assert.That(
                (string?)primitiveRecipe?.Element("products")?.Element("ImmersiveChefs_Plate"),
                Is.EqualTo("4"));
            Assert.That(
                primitiveRecipe?.Element("recipeUsers")?.Elements("li").Select(element => element.Value),
                Is.EqualTo(new[] { "CraftingSpot" }));
            Assert.That(
                (string?)primitiveRecipe?.Element("modExtensions")?.Elements("li").Single()
                    .Element("fabricationTier"),
                Is.EqualTo("PrimitiveStone"));
        });
    }

    [Test]
    public void Kitchenware_costs_are_core_benchmarked_and_filters_do_not_expose_root()
    {
        var document = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "mods",
            "ImmersiveChefs",
            "Defs",
            "RecipeDefs",
            "KitchenwareRecipes.xml"));
        var expected = new Dictionary<string, float[]>
        {
            ["ImmersiveChefs_MakePrimitiveCookware"] = new[] { 5f, 1f },
            ["ImmersiveChefs_MakePrimitivePlates"] = new[] { 4f },
            ["ImmersiveChefs_MakeMedievalCookware"] = new[] { 6f, 1f },
            ["ImmersiveChefs_MakeModernCookware"] = new[] { 6f, 1f },
            ["ImmersiveChefs_MakeChefsKnife"] = new[] { 6f },
            ["ImmersiveChefs_MakeSoftPlates"] = new[] { 4f },
            ["ImmersiveChefs_MakeSoftCutlery"] = new[] { 2f },
            ["ImmersiveChefs_SmithPlates"] = new[] { 4f },
            ["ImmersiveChefs_SmithCutlery"] = new[] { 2f },
            ["ImmersiveChefs_MachinePlates"] = new[] { 4f },
            ["ImmersiveChefs_MachineCutlery"] = new[] { 2f }
        };

        Assert.Multiple(() =>
        {
            foreach (var pair in expected)
            {
                var recipe = document.Root!.Elements("RecipeDef")
                    .Single(element => (string?)element.Element("defName") == pair.Key);
                var actual = recipe.Element("ingredients")!.Elements("li")
                    .Select(element => (float)element.Element("count")!)
                    .ToArray();
                Assert.That(actual, Is.EqualTo(pair.Value), pair.Key);
            }

            Assert.That(
                document.Descendants("categories").Elements("li").Select(element => element.Value),
                Does.Not.Contain("Root"));
        });
    }

    [Test]
    public void Cookware_recipes_name_the_complete_set_consistently()
    {
        var document = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "mods",
            "ImmersiveChefs",
            "Defs",
            "RecipeDefs",
            "KitchenwareRecipes.xml"));
        var cookwareRecipes = new[]
        {
            "ImmersiveChefs_MakePrimitiveCookware",
            "ImmersiveChefs_MakeMedievalCookware",
            "ImmersiveChefs_MakeModernCookware"
        };

        Assert.Multiple(() =>
        {
            foreach (var defName in cookwareRecipes)
            {
                var recipe = document.Root!.Elements("RecipeDef")
                    .Single(element => (string?)element.Element("defName") == defName);
                Assert.That(
                    (string?)recipe.Element("label"),
                    Does.Contain("cookware set"),
                    $"{defName} label");
                Assert.That(
                    (string?)recipe.Element("jobString"),
                    Does.Contain("cookware set"),
                    $"{defName} job string");
            }
        });
    }

    [Test]
    public void Player_facing_catalog_and_alert_text_names_cookware_sets()
    {
        var filters = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "mods",
            "ImmersiveChefs",
            "Defs",
            "SpecialThingFilterDefs",
            "KitchenwareSanitationFilters.xml"));
        var descriptions = filters.Descendants("description")
            .Select(element => element.Value)
            .ToArray();

        Assert.Multiple(() =>
        {
            var english = XDocument.Load(Path.Combine(
                FindRepositoryRoot(),
                "mods",
                "ImmersiveChefs",
                "Languages",
                "English",
                "Keyed",
                "ImmersiveChefs.xml"));
            Assert.That(
                english.Root!.Element(KitchenwareAlertRuntime.ProductTranslationKey(KitchenwareProduct.Cookware))?.Value,
                Is.EqualTo("cookware sets"));
            Assert.That(descriptions, Is.All.Contains("cookware sets"));
        });
    }

    [Test]
    public void Primitive_cookware_is_a_distinct_stony_product_with_its_own_texture()
    {
        var root = FindRepositoryRoot();
        var things = XDocument.Load(Path.Combine(
            root, "mods", "ImmersiveChefs", "Defs", "ThingDefs", "Kitchenware.xml"));
        var recipes = XDocument.Load(Path.Combine(
            root, "mods", "ImmersiveChefs", "Defs", "RecipeDefs", "KitchenwareRecipes.xml"));
        var primitive = things.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_PrimitiveCookware");
        var modern = things.Root!.Elements("ThingDef")
            .Single(element => (string?)element.Element("defName") == "ImmersiveChefs_Cookware");
        var recipe = recipes.Root!.Elements("RecipeDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_MakePrimitiveCookware");

        Assert.Multiple(() =>
        {
            Assert.That(
                primitive.Element("stuffCategories")!.Elements("li").Select(element => element.Value),
                Is.EqualTo(new[] { "Stony" }));
            Assert.That(
                modern.Element("stuffCategories")!.Elements("li").Select(element => element.Value),
                Is.EqualTo(new[] { "Metallic" }));
            Assert.That(
                (string?)primitive.Element("graphicData")?.Element("texPath"),
                Is.Not.EqualTo((string?)modern.Element("graphicData")?.Element("texPath")));
            Assert.That(
                (string?)primitive.Element("modExtensions")?
                    .Elements("li")
                    .Single(element =>
                        (string?)element.Attribute("Class") == "ImmersiveChefs.KitchenwareExtension")
                    .Element("baseGraphicMaterialKind"),
                Is.EqualTo("PrimitiveStone"));
            Assert.That(
                (string?)recipe.Element("products")?.Element("ImmersiveChefs_PrimitiveCookware"),
                Is.EqualTo("1"));
        });
    }

    [Test]
    public void Portable_ware_is_sellable_and_trader_stock_is_low_and_thematic()
    {
        var root = FindRepositoryRoot();
        var things = XDocument.Load(Path.Combine(
            root, "mods", "ImmersiveChefs", "Defs", "ThingDefs", "Kitchenware.xml"));
        var patches = XDocument.Load(Path.Combine(
            root, "mods", "ImmersiveChefs", "Patches", "KitchenwareTraderStock.xml"));
        var portableDefs = new[]
        {
            "ImmersiveChefs_PrimitiveCookware",
            "ImmersiveChefs_Cookware",
            "ImmersiveChefs_Plate",
            "ImmersiveChefs_AdobePlate",
            "ImmersiveChefs_Cutlery",
            "ImmersiveChefs_GlitterworldCookware",
            "ImmersiveChefs_ChefsKnife"
        };
        var kitchenwareBaseTradeability = (string?)things.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Attribute("Name") == "ImmersiveChefs_KitchenwareBase")
            .Element("tradeability");
        var expectedTraders = new[]
        {
            "Caravan_Neolithic_BulkGoods",
            "Base_Neolithic_Standard",
            "Caravan_Outlander_BulkGoods",
            "Base_Outlander_Standard",
            "Orbital_BulkGoods",
            "Caravan_Outlander_Exotic",
            "Orbital_Exotic"
        };

        var stockXml = patches.ToString(SaveOptions.DisableFormatting);
        var stuffableStockGenerators = patches
            .Descendants("li")
            .Where(element =>
                ((string?)element.Element("thingDef")) is
                    "ImmersiveChefs_PrimitiveCookware" or
                    "ImmersiveChefs_Cookware" or
                    "ImmersiveChefs_Plate" or
                    "ImmersiveChefs_Cutlery" or
                    "ImmersiveChefs_ChefsKnife")
            .ToArray();
        Assert.Multiple(() =>
        {
            foreach (var defName in portableDefs)
            {
                var def = things.Root!.Elements("ThingDef")
                    .Single(element => (string?)element.Element("defName") == defName);
                var tradeability = (string?)def.Element("tradeability") ??
                                   ((string?)def.Attribute("ParentName") == "ImmersiveChefs_KitchenwareBase"
                                       ? kitchenwareBaseTradeability
                                       : null);
                Assert.That(tradeability, Is.EqualTo("All"), defName);
            }

            foreach (var trader in expectedTraders)
            {
                Assert.That(stockXml, Does.Contain($"defName=\"{trader}\""), trader);
            }

            Assert.That(stockXml, Does.Not.Contain("ImmersiveChefs_PreparedFood"));
            Assert.That(stockXml, Does.Not.Contain("ImmersiveChefs_Dishwasher"));
            Assert.That(stockXml, Does.Contain("ImmersiveChefs_GlitterworldCookware"));
            Assert.That(stockXml, Does.Contain("-3~1"));
            Assert.That(stuffableStockGenerators, Is.Not.Empty);
            Assert.That(
                stuffableStockGenerators.Select(element => (string?)element.Attribute("Class")),
                Is.All.EqualTo("ImmersiveChefs.StockGenerator_Kitchenware"));
            Assert.That(
                stuffableStockGenerators.All(element =>
                    element.Element("product") is not null &&
                    element.Element("fabricationTiers")?.Elements("li").Any() == true),
                Is.True,
                "Stuffable trader stock must declare the same product/tier classifier boundary as recipes.");
        });
    }

    [Test]
    public void Chefs_knife_requirement_says_any_eligible_metal()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "mods",
            "ImmersiveChefs",
            "Source",
            "Defs",
            "IngredientValueGetter_Units.cs"));

        Assert.That(source, Does.Contain("KitchenwareProduct.ChefsKnife"));
        Assert.That(source, Does.Contain("ImmersiveChefs_Ingredient_AnyEligibleMetal"));
        var english = XDocument.Load(Path.Combine(
            FindRepositoryRoot(), "mods", "ImmersiveChefs", "Languages", "English", "Keyed", "ImmersiveChefs.xml"));
        Assert.That(
            english.Root!.Element("ImmersiveChefs_Ingredient_AnyEligibleMetal")?.Value,
            Is.EqualTo("any eligible metal"));
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

    [Test]
    public void Every_plate_acquisition_path_applies_meal_complexity_eligibility()
    {
        var root = FindRepositoryRoot();
        var cooking = File.ReadAllText(Path.Combine(
            root, "mods", "ImmersiveChefs", "Source", "Production", "CookingSessionRegistry.cs"));
        var imported = File.ReadAllText(Path.Combine(
            root, "mods", "ImmersiveChefs", "Source", "Production", "ImportedMealPlating.cs"));
        var dining = File.ReadAllText(Path.Combine(
            root, "mods", "ImmersiveChefs", "Source", "Dining", "DiningSessionRegistry.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(cooking, Does.Contain("PlateMaterialEligibilityRuntime.Allows"));
            Assert.That(imported, Does.Contain("PlateMaterialEligibilityRuntime.Allows"));
            Assert.That(dining, Does.Contain("PlateMaterialEligibilityRuntime.Allows"));
            Assert.That(dining, Does.Contain("FoodUtility.GetFinalIngestibleDef"));
            Assert.That(dining, Does.Contain("MealComplexityRuntime.Classify(diningMealDef)"));
        });
    }

    [Test]
    public void Prep_station_has_a_dedicated_cooking_bill_workgiver()
    {
        var root = FindRepositoryRoot();
        var workGivers = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "WorkGiverDefs",
            "PreparedFoodWorkGiver.xml"));
        var workGiver = workGivers.Root?.Elements("WorkGiverDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_PrepareIngredients");

        Assert.Multiple(() =>
        {
            Assert.That((string?)workGiver?.Element("giverClass"), Is.EqualTo("WorkGiver_DoBill"));
            Assert.That((string?)workGiver?.Element("workType"), Is.EqualTo("Cooking"));
            Assert.That((string?)workGiver?.Element("scanThings"), Is.EqualTo("true"));
            Assert.That(
                workGiver?.Element("fixedBillGiverDefs")?.Elements("li")
                    .Select(element => element.Value),
                Is.EqualTo(new[] { "ImmersiveChefs_PrepStation" }));
            Assert.That(
                workGiver?.Element("requiredCapacities")?.Elements("li")
                    .Select(element => element.Value),
                Does.Contain("Manipulation"));
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
            if (File.Exists(Path.Combine(current.FullName, "RimWorldMods.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Immersive Chefs repository root.");
    }
}
