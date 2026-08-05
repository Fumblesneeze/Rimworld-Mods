using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class RecipeClassificationTests
{
    [TestCase("CookMealSimple", MealComplexity.Simple, 75f)]
    [TestCase("CookMealFine_Veg", MealComplexity.Advanced, 200f)]
    [TestCase("CookMealLavishBulk_Meat", MealComplexity.Elaborate, 300f)]
    public void Exact_vanilla_recipe_table_applies_the_configured_multiplier(
        string recipeDefName,
        MealComplexity expectedComplexity,
        float expectedWork)
    {
        var settings = new ImmersiveChefsSettings();
        var classifier = MealClassificationCatalog.Create(Array.Empty<string>());

        var classification = classifier.ClassifyRecipe(recipeDefName);

        Assert.Multiple(() =>
        {
            Assert.That(classification, Is.EqualTo(expectedComplexity));
            Assert.That(classifier.AdjustWorkAmount(recipeDefName, 100f, settings), Is.EqualTo(expectedWork));
            Assert.That(classifier.AdjustWorkAmount("SomeMod_UnclassifiedMeal", 100f, settings), Is.EqualTo(100f));
        });
    }

    [Test]
    public void Optional_recipes_and_products_are_absent_without_their_exact_active_package()
    {
        var classifier = MealClassificationCatalog.Create(Array.Empty<string>());

        Assert.Multiple(() =>
        {
            Assert.That(classifier.ClassifyRecipe("VCE_CookBakeFine"), Is.Null);
            Assert.That(classifier.ClassifyMeal("VCE_FineBake"), Is.Null);
            Assert.That(classifier.ClassifyRecipe("CM_CookFastMealDeluxe"), Is.Null);
            Assert.That(classifier.ClassifyMeal("CM_DeluxeFastMeal"), Is.Null);
            Assert.That(classifier.ClassifyRecipe("RC2_CookThickPottage"), Is.Null);
            Assert.That(classifier.ClassifyMeal("RC2_ThickPottage"), Is.Null);
        });
    }

    [TestCase("VanillaExpanded.VCookE", "VCE_CookBakeSimple", "VCE_SimpleBake", MealComplexity.Simple)]
    [TestCase("VanillaExpanded.VCookE", "VCE_CookGrillFineBulk", "VCE_FineGrill", MealComplexity.Advanced)]
    [TestCase("VanillaExpanded.VCookE", "VCE_CookSoupGourmet", "VCE_CookedSoupGourmet", MealComplexity.Elaborate)]
    [TestCase("VanillaExpanded.VCookEHaute", "VCE_CookMealHaute", "VCE_MealHaute", MealComplexity.Elaborate)]
    [TestCase("VanillaExpanded.VCookEStews", "VCE_CookStewFine", "VCE_CookedStewFine", MealComplexity.Advanced)]
    [TestCase("VanillaExpanded.VCookESushi", "VCE_CookNorimakiSimpleBulk", "VCE_Norimaki", MealComplexity.Simple)]
    [TestCase("VanillaExpanded.VCookESushi", "VCE_CookNigiriFine", "VCE_Nigiri", MealComplexity.Advanced)]
    [TestCase("VanillaExpanded.VCookESushi", "VCE_CookGunkanmakiGourmet", "VCE_Gunkanmaki", MealComplexity.Elaborate)]
    [TestCase("ucp.friedmeals", "CookFritterSimpleBulk", "ucp_SimpleFritter", MealComplexity.Simple)]
    [TestCase("ucp.friedmeals", "CookFritterFine", "ucp_FineFritter", MealComplexity.Advanced)]
    [TestCase("ucp.friedmeals", "VCE_CookFritterGourmet", "ucp_GourmetFritter", MealComplexity.Elaborate)]
    [TestCase("Argon.CheapMeals", "CM_CookFastMeal", "CM_SimpleFastMeal", MealComplexity.Simple)]
    [TestCase("Argon.CheapMeals", "CM_CookFastMealDeluxe_Meat", "CM_DeluxeFastMeal_Meat", MealComplexity.Advanced)]
    [TestCase("Mlie.RC2.Core", "CookThinPottage", "RC2_ThinPottage", MealComplexity.Simple)]
    [TestCase("Mlie.RC2.Core", "RC2_CookThickPottage", "RC2_ThickPottage", MealComplexity.Advanced)]
    [TestCase("Mlie.RC2.MaME", "RC2_CookExtravagantMealBulk", "RC2_ExtravagantMeal", MealComplexity.Elaborate)]
    public void Exact_active_package_registers_recipe_and_final_product(
        string packageId,
        string recipeDefName,
        string productDefName,
        MealComplexity expected)
    {
        var classifier = MealClassificationCatalog.Create(RequiredPackages(packageId));

        Assert.Multiple(() =>
        {
            Assert.That(classifier.ClassifyRecipe(recipeDefName), Is.EqualTo(expected));
            Assert.That(classifier.ClassifyMeal(productDefName), Is.EqualTo(expected));
        });
    }

    [Test]
    public void Similar_names_do_not_activate_packages_or_enter_the_registry()
    {
        var classifier = MealClassificationCatalog.Create(new[]
        {
            "VanillaExpanded.VCookE.Lookalike",
            "Argon.CheapMealsExtra",
            "Mlie.RC2.CoreOld"
        });

        Assert.Multiple(() =>
        {
            Assert.That(classifier.ClassifyRecipe("VCE_CookBakeSimple"), Is.Null);
            Assert.That(classifier.ClassifyRecipe("CM_CookFastMeal"), Is.Null);
            Assert.That(classifier.ClassifyRecipe("CookThinPottage"), Is.Null);
            Assert.That(classifier.ClassifyRecipe("VCE_CookBakeSimpleTranslated"), Is.Null);
        });
    }

    [TestCase("VanillaExpanded.VCookE", "VCE_CookBakeSimple")]
    [TestCase("VanillaExpanded.VCookEHaute", "VCE_CookMealHaute")]
    [TestCase("VanillaExpanded.VCookEStews", "VCE_CookStewSimple")]
    [TestCase("VanillaExpanded.VCookESushi", "VCE_CookNorimakiSimple")]
    [TestCase("ucp.friedmeals", "CookFritterSimple")]
    [TestCase("Mlie.RC2.Core", "CookThinPottage")]
    [TestCase("Mlie.RC2.MaME", "RC2_CookRubaboo")]
    public void Incomplete_dependency_chain_does_not_activate_a_partial_registry(
        string packageId,
        string recipeDefName)
    {
        var classifier = MealClassificationCatalog.Create(new[] { packageId });

        Assert.That(classifier.ClassifyRecipe(recipeDefName), Is.Null);
    }

    [TestCase("CM_CookFastMeal", MealComplexity.Simple)]
    [TestCase("CM_CookFastMealBulk", MealComplexity.Simple)]
    [TestCase("CM_CookFastMealDeluxe", MealComplexity.Advanced)]
    [TestCase("CM_CookFastMealDeluxe_Meat", MealComplexity.Advanced)]
    [TestCase("CM_CookFastMealDeluxe_Veg", MealComplexity.Advanced)]
    [TestCase("CM_CookFastMealDeluxeBulk", MealComplexity.Advanced)]
    [TestCase("CM_CookFastMealDeluxeBulk_Meat", MealComplexity.Advanced)]
    [TestCase("CM_CookFastMealDeluxeBulk_Veg", MealComplexity.Advanced)]
    public void Fast_meals_keep_native_work_but_still_report_service_complexity(
        string recipeDefName,
        MealComplexity expected)
    {
        var settings = new ImmersiveChefsSettings
        {
            SimpleRecipeTimeMultiplier = 0.25f,
            AdvancedRecipeTimeMultiplier = 5f
        };
        var classifier = MealClassificationCatalog.Create(new[] { "Argon.CheapMeals" });

        Assert.Multiple(() =>
        {
            Assert.That(classifier.ClassifyRecipe(recipeDefName), Is.EqualTo(expected));
            Assert.That(classifier.PreservesOriginalWorkAmount(recipeDefName), Is.True);
            Assert.That(classifier.AdjustWorkAmount(recipeDefName, 137f, settings), Is.EqualTo(137f));
        });
    }

    [Test]
    public void Fried_gourmet_entry_requires_vce_even_when_ordinary_fritters_are_available()
    {
        var withoutVce = MealClassificationCatalog.Create(new[]
        {
            "ucp.friedmeals",
            "OskarPotocki.VanillaFactionsExpanded.Core"
        });
        var withVce = MealClassificationCatalog.Create(new[]
        {
            "ucp.friedmeals",
            "OskarPotocki.VanillaFactionsExpanded.Core",
            "VanillaExpanded.VCookE"
        });

        Assert.Multiple(() =>
        {
            Assert.That(withoutVce.ClassifyRecipe("CookFritterSimple"), Is.EqualTo(MealComplexity.Simple));
            Assert.That(withoutVce.ClassifyRecipe("VCE_CookFritterGourmet"), Is.Null);
            Assert.That(withVce.ClassifyRecipe("VCE_CookFritterGourmet"), Is.EqualTo(MealComplexity.Elaborate));
        });
    }

    [TestCase("RC2_CookFineMealBulk", MealComplexity.Advanced)]
    [TestCase("RC2_CookLavishMealBulk", MealComplexity.Elaborate)]
    public void RimCuisine_vanilla_bulk_replacements_receive_explicit_service_tiers(
        string recipeDefName,
        MealComplexity expected)
    {
        var classifier = MealClassificationCatalog.Create(RequiredPackages("Mlie.RC2.MaME"));

        Assert.That(classifier.ClassifyRecipe(recipeDefName), Is.EqualTo(expected));
    }

    [Test]
    public void No_vanilla_meals_removes_vanilla_recipe_and_product_classifications()
    {
        var classifier = MealClassificationCatalog.Create(new[] { "Mlie.NoVanillaMeals" });

        Assert.Multiple(() =>
        {
            Assert.That(classifier.ClassifyRecipe("CookMealSimple"), Is.Null);
            Assert.That(classifier.ClassifyRecipe("CookMealFineBulk"), Is.Null);
            Assert.That(classifier.ClassifyRecipe("CookMealLavish"), Is.Null);
            Assert.That(classifier.ClassifyMeal("MealSimple"), Is.Null);
            Assert.That(classifier.ClassifyMeal("MealFine_Meat"), Is.Null);
            Assert.That(classifier.ClassifyMeal("MealLavish_Veg"), Is.Null);
        });
    }

    [Test]
    public void RimCuisine_custom_meals_survive_No_vanilla_meals_without_stale_bulk_recipes()
    {
        var classifier = MealClassificationCatalog.Create(new[]
        {
            "SYRCHALIS.PROCESSOR.FRAMEWORK",
            "MLIE.RC2.CORE",
            "MLIE.RC2.MAME",
            "MLIE.NOVANILLAMEALS"
        });

        Assert.Multiple(() =>
        {
            Assert.That(classifier.ClassifyRecipe("CookThinPottage"), Is.EqualTo(MealComplexity.Simple));
            Assert.That(classifier.ClassifyMeal("RC2_ThinPottage"), Is.EqualTo(MealComplexity.Simple));
            Assert.That(classifier.ClassifyRecipe("RC2_CookThickPottage"), Is.EqualTo(MealComplexity.Advanced));
            Assert.That(classifier.ClassifyRecipe("RC2_CookRubaboo"), Is.EqualTo(MealComplexity.Simple));
            Assert.That(classifier.ClassifyRecipe("RC2_CookExtravagantMeal"), Is.EqualTo(MealComplexity.Elaborate));
            Assert.That(classifier.ClassifyMeal("RC2_ExtravagantMeal"), Is.EqualTo(MealComplexity.Elaborate));
            Assert.That(classifier.ClassifyMeal("RC2_Pizza"), Is.EqualTo(MealComplexity.Elaborate));
            Assert.That(classifier.ClassifyRecipe("RC2_CookFineMealBulk"), Is.Null);
            Assert.That(classifier.ClassifyRecipe("RC2_CookLavishMealBulk"), Is.Null);
        });
    }

    private static string[] RequiredPackages(string packageId)
    {
        return packageId switch
        {
            "VanillaExpanded.VCookE" => new[]
            {
                packageId.ToUpperInvariant(),
                "OSKARPOTOCKI.VANILLAFACTIONSEXPANDED.CORE"
            },
            "VanillaExpanded.VCookEHaute" or "VanillaExpanded.VCookEStews" => new[]
            {
                packageId.ToUpperInvariant(),
                "VANILLAEXPANDED.VCOOKE",
                "OSKARPOTOCKI.VANILLAFACTIONSEXPANDED.CORE"
            },
            "VanillaExpanded.VCookESushi" => new[]
            {
                packageId.ToUpperInvariant(),
                "VANILLAEXPANDED.VCOOKE",
                "VANILLAEXPANDED.VCEF",
                "OSKARPOTOCKI.VANILLAFACTIONSEXPANDED.CORE"
            },
            "ucp.friedmeals" => new[]
            {
                packageId.ToUpperInvariant(),
                "OSKARPOTOCKI.VANILLAFACTIONSEXPANDED.CORE",
                "VANILLAEXPANDED.VCOOKE"
            },
            "Mlie.RC2.Core" => new[]
            {
                packageId.ToUpperInvariant(),
                "SYRCHALIS.PROCESSOR.FRAMEWORK"
            },
            "Mlie.RC2.MaME" => new[]
            {
                packageId.ToUpperInvariant(),
                "MLIE.RC2.CORE",
                "SYRCHALIS.PROCESSOR.FRAMEWORK"
            },
            _ => new[] { packageId.ToUpperInvariant() }
        };
    }
}
