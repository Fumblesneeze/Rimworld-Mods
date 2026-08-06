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

    [TestCase("FTV_CookMealSimple", "FTV_MealSimple", MealComplexity.Simple)]
    [TestCase("FTV_CookMealSimpleBulk", "FTV_MealSimple", MealComplexity.Simple)]
    [TestCase("FTV_CookMealFine", "FTV_MealFine", MealComplexity.Advanced)]
    [TestCase("FTV_CookMealFineBulk", "FTV_MealFine", MealComplexity.Advanced)]
    [TestCase("FTV_CookMealLavish", "FTV_MealLavish", MealComplexity.Elaborate)]
    [TestCase("FTV_CookMealLavishBulk", "FTV_MealLavish", MealComplexity.Elaborate)]
    public void Food_texture_variety_registers_its_exact_recipes_and_products(
        string recipeDefName,
        string productDefName,
        MealComplexity expected)
    {
        var classifier = MealClassificationCatalog.Create(new[]
        {
            "GOAT.FOOD.TEXTURE.VARIETY.CORE",
            "GOAT.FOOD.TEXTURE.VARIETY"
        });

        Assert.Multiple(() =>
        {
            Assert.That(classifier.ClassifyRecipe(recipeDefName), Is.EqualTo(expected));
            Assert.That(classifier.ClassifyMeal(productDefName), Is.EqualTo(expected));
        });
    }

    [Test]
    public void Food_texture_variety_registry_requires_core_and_main_packages_together()
    {
        var coreOnly = MealClassificationCatalog.Create(new[]
        {
            "Goat.Food.Texture.Variety.Core"
        });
        var mainOnly = MealClassificationCatalog.Create(new[]
        {
            "Goat.Food.Texture.Variety"
        });

        Assert.Multiple(() =>
        {
            Assert.That(coreOnly.ClassifyRecipe("FTV_CookMealSimple"), Is.Null);
            Assert.That(coreOnly.ClassifyMeal("FTV_MealSimple"), Is.Null);
            Assert.That(mainOnly.ClassifyRecipe("FTV_CookMealSimple"), Is.Null);
            Assert.That(mainOnly.ClassifyMeal("FTV_MealSimple"), Is.Null);
        });
    }

    [Test]
    public void Food_texture_variety_changed_shape_disables_its_whole_registry()
    {
        var validation = MealClassificationCatalog.CreateValidated(
            new[]
            {
                "Goat.Food.Texture.Variety.Core",
                "Goat.Food.Texture.Variety"
            },
            recipeDefName => recipeDefName != "FTV_CookMealFine",
            _ => true);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Failures, Has.Count.EqualTo(1));
            Assert.That(
                validation.Failures[0].PackageId,
                Is.EqualTo("Goat.Food.Texture.Variety"));
            Assert.That(validation.Catalog.ClassifyRecipe("FTV_CookMealSimple"), Is.Null);
            Assert.That(validation.Catalog.ClassifyRecipe("FTV_CookMealFine"), Is.Null);
            Assert.That(validation.Catalog.ClassifyMeal("FTV_MealLavish"), Is.Null);
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

    [TestCase("ReplimatMeals_S_Borscht", MealComplexity.Simple)]
    [TestCase("ReplimatMeals_S_Zongzi", MealComplexity.Simple)]
    [TestCase("ReplimatMeals_F_BeefNoodleSoup", MealComplexity.Advanced)]
    [TestCase("ReplimatMeals_F_TenzaruSoba", MealComplexity.Advanced)]
    [TestCase("ReplimatMeals_L_FullEnglishBreakfast", MealComplexity.Elaborate)]
    [TestCase("ReplimatMeals_L_SundayRoast", MealComplexity.Elaborate)]
    public void Exact_replimat_and_meals_chain_registers_ingredientless_terminal_products(
        string mealDefName,
        MealComplexity expected)
    {
        var catalog = MealClassificationCatalog.Create(new[]
        {
            MealClassificationCatalog.ReplimatPackageId,
            MealClassificationCatalog.ReplimatMealsPackageId
        });

        Assert.Multiple(() =>
        {
            Assert.That(catalog.ClassifyMeal(mealDefName), Is.EqualTo(expected));
            Assert.That(
                MealClassificationCatalog.IsMealRegisteredForPackage(
                    MealClassificationCatalog.ReplimatMealsPackageId,
                    mealDefName),
                Is.True);
        });
    }

    [Test]
    public void Replimat_meal_registry_requires_both_exact_packages_and_fails_as_one_shape()
    {
        var addonOnly = MealClassificationCatalog.Create(new[]
        {
            MealClassificationCatalog.ReplimatMealsPackageId
        });
        var changed = MealClassificationCatalog.CreateValidated(
            new[]
            {
                MealClassificationCatalog.ReplimatPackageId,
                MealClassificationCatalog.ReplimatMealsPackageId
            },
            _ => true,
            defName => defName != "ReplimatMeals_F_Ramen");

        Assert.Multiple(() =>
        {
            Assert.That(addonOnly.ClassifyMeal("ReplimatMeals_F_Ramen"), Is.Null);
            Assert.That(changed.Failures, Has.Count.EqualTo(1));
            Assert.That(changed.Failures[0].PackageId,
                Is.EqualTo(MealClassificationCatalog.ReplimatMealsPackageId));
            Assert.That(changed.Catalog.ClassifyMeal("ReplimatMeals_S_Borscht"), Is.Null);
            Assert.That(changed.Catalog.ClassifyMeal("ReplimatMeals_F_Ramen"), Is.Null);
            Assert.That(changed.Catalog.ClassifyMeal("ReplimatMeals_L_SundayRoast"), Is.Null);
        });
    }

    [Test]
    public void Replimat_registry_is_the_complete_distinct_installed_16_product_set()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MealClassificationCatalog.ReplimatMealDefNames, Has.Count.EqualTo(48));
            Assert.That(
                MealClassificationCatalog.ReplimatMealDefNames.Distinct(StringComparer.Ordinal).ToArray().Length,
                Is.EqualTo(48));
            Assert.That(
                MealClassificationCatalog.ReplimatMealDefNames.Count(name =>
                    name.StartsWith("ReplimatMeals_S_", StringComparison.Ordinal)),
                Is.EqualTo(20));
            Assert.That(
                MealClassificationCatalog.ReplimatMealDefNames.Count(name =>
                    name.StartsWith("ReplimatMeals_F_", StringComparison.Ordinal)),
                Is.EqualTo(22));
            Assert.That(
                MealClassificationCatalog.ReplimatMealDefNames.Count(name =>
                    name.StartsWith("ReplimatMeals_L_", StringComparison.Ordinal)),
                Is.EqualTo(6));
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

    [Test]
    public void Validated_registry_disables_only_the_changed_addon_shape()
    {
        var result = MealClassificationCatalog.CreateValidated(
            new[]
            {
                MealClassificationCatalog.VanillaExpandedFrameworkPackageId,
                MealClassificationCatalog.VanillaCookingExpandedPackageId,
                MealClassificationCatalog.VanillaCookingExpandedHautePackageId,
                MealClassificationCatalog.FriedMealsPackageId
            },
            recipeDefName => recipeDefName != "VCE_CookMealHaute",
            _ => true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failures, Has.Count.EqualTo(1));
            Assert.That(result.Failures[0].PackageId,
                Is.EqualTo(MealClassificationCatalog.VanillaCookingExpandedHautePackageId));
            Assert.That(result.Failures[0].MissingRecipeDefNames,
                Is.EqualTo(new[] { "VCE_CookMealHaute" }));
            Assert.That(result.Failures[0].MissingMealDefNames, Is.Empty);
            Assert.That(result.Catalog.ClassifyRecipe("VCE_CookMealHaute"), Is.Null);
            Assert.That(result.Catalog.ClassifyRecipe("VCE_CookBakeSimple"),
                Is.EqualTo(MealComplexity.Simple));
            Assert.That(result.Catalog.ClassifyRecipe("CookFritterFine"),
                Is.EqualTo(MealComplexity.Advanced));
        });
    }

    [Test]
    public void Changed_vce_base_suppresses_dependent_entries_without_disabling_fried_meals()
    {
        var result = MealClassificationCatalog.CreateValidated(
            new[]
            {
                MealClassificationCatalog.VanillaExpandedFrameworkPackageId,
                MealClassificationCatalog.VanillaCookingExpandedPackageId,
                MealClassificationCatalog.VanillaCookingExpandedHautePackageId,
                MealClassificationCatalog.FriedMealsPackageId
            },
            recipeDefName => recipeDefName != "VCE_CookBakeSimple",
            _ => true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failures.Select(failure => failure.PackageId),
                Is.EqualTo(new[] { MealClassificationCatalog.VanillaCookingExpandedPackageId }));
            Assert.That(result.Catalog.ClassifyRecipe("VCE_CookBakeSimple"), Is.Null);
            Assert.That(result.Catalog.ClassifyRecipe("VCE_CookMealHaute"), Is.Null);
            Assert.That(result.Catalog.ClassifyRecipe("CookFritterSimple"),
                Is.EqualTo(MealComplexity.Simple));
            Assert.That(result.Catalog.ClassifyRecipe("VCE_CookFritterGourmet"), Is.Null);
        });
    }

    [Test]
    public void Changed_registry_is_removed_from_active_meal_coverage_before_components_are_added()
    {
        MealClassificationRuntime.Initialize(
            new[]
            {
                MealClassificationCatalog.VanillaExpandedFrameworkPackageId,
                MealClassificationCatalog.VanillaCookingExpandedPackageId,
                MealClassificationCatalog.VanillaCookingExpandedHautePackageId,
                MealClassificationCatalog.FriedMealsPackageId
            },
            recipeDefName => recipeDefName != "VCE_CookBakeSimple",
            _ => true,
            _ => { });

        Assert.Multiple(() =>
        {
            Assert.That(
                MealClassificationRuntime.IsMealRegisteredForActivePackage(
                    MealClassificationCatalog.VanillaCookingExpandedPackageId,
                    "VCE_FineBake"),
                Is.False,
                "A failed VCE base registry must not leave a partial ware lifecycle active.");
            Assert.That(
                MealClassificationRuntime.IsMealRegisteredForActivePackage(
                    MealClassificationCatalog.VanillaCookingExpandedHautePackageId,
                    "VCE_MealHaute"),
                Is.False,
                "A dependent add-on must not remain active without its validated VCE base.");
            Assert.That(
                MealClassificationRuntime.IsMealRegisteredForActivePackage(
                    MealClassificationCatalog.FriedMealsPackageId,
                    "ucp_FineFritter"),
                Is.True,
                "An independently valid Fried registry must remain active.");
            Assert.That(
                MealClassificationRuntime.IsMealRegisteredForActivePackage(
                    MealClassificationCatalog.FriedMealsPackageId,
                    "ucp_GourmetFritter"),
                Is.False,
                "The VCE-dependent Fried Gourmet entry must fail closed with VCE base.");
        });
    }

    [Test]
    public void Bakery_is_an_explicit_exclusion_only_package()
    {
        var result = MealClassificationCatalog.CreateValidated(
            new[]
            {
                MealClassificationCatalog.VanillaExpandedFrameworkPackageId,
                MealClassificationCatalog.VanillaCookingExpandedPackageId,
                MealClassificationCatalog.VanillaCookingExpandedBakeryPackageId
            },
            _ => true,
            _ => true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failures, Is.Empty);
            Assert.That(result.Catalog.ClassifyRecipe("VCE_CookCakeBatter"), Is.Null);
            Assert.That(result.Catalog.ClassifyMeal("VCE_NormalCakeBatter"), Is.Null);
            Assert.That(result.Catalog.ClassifyMeal("VCE_GourmetConfection"), Is.Null);
            Assert.That(result.Catalog.ClassifyRecipe("VCE_CookDessertGourmet"), Is.Null);
            Assert.That(result.Catalog.ClassifyMeal("VCE_GourmetDessert"), Is.Null);
        });
    }

    [Test]
    public void Runtime_uses_the_validated_catalog_and_reports_one_package_attributed_warning()
    {
        var warnings = new List<string>();

        MealClassificationRuntime.Initialize(
            new[]
            {
                MealClassificationCatalog.VanillaExpandedFrameworkPackageId,
                MealClassificationCatalog.VanillaCookingExpandedPackageId,
                MealClassificationCatalog.VanillaCookingExpandedHautePackageId,
                MealClassificationCatalog.FriedMealsPackageId
            },
            recipeDefName => recipeDefName != "VCE_CookMealHaute",
            _ => true,
            warnings.Add);

        Assert.Multiple(() =>
        {
            Assert.That(MealClassificationRuntime.ValidationFailures, Has.Count.EqualTo(1));
            Assert.That(MealClassificationRuntime.ValidationFailures[0].PackageId,
                Is.EqualTo(MealClassificationCatalog.VanillaCookingExpandedHautePackageId));
            Assert.That(warnings, Is.EqualTo(new[]
            {
                "[ImmersiveChefs] Disabled meal registry for VanillaExpanded.VCookEHaute because its finalized Def shape changed. Missing RecipeDefs: VCE_CookMealHaute. Missing ThingDefs: none."
            }));
        });
    }

    [Test]
    public void Explicit_recipe_complexity_does_not_require_a_direct_meal_product()
    {
        MealClassificationRuntime.Initialize(
            new[]
            {
                MealClassificationCatalog.VanillaExpandedFrameworkPackageId,
                MealClassificationCatalog.VanillaCookingExpandedPackageId
            },
            _ => true,
            _ => true,
            _ => { });
        var twoStageSoupPreparation = new Verse.RecipeDef
        {
            defName = "VCE_CookSoupSimple"
        };

        Assert.That(
            MealClassificationRuntime.ClassifyRecipe(twoStageSoupPreparation),
            Is.EqualTo(MealComplexity.Simple));
    }

    [Test]
    public void Explicit_registry_packages_cover_only_their_registered_final_meals()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                MealClassificationCatalog.OwnsExplicitMealRegistry(
                    MealClassificationCatalog.VanillaCookingExpandedPackageId),
                Is.True);
            Assert.That(
                MealClassificationCatalog.IsMealRegisteredForPackage(
                    MealClassificationCatalog.VanillaCookingExpandedPackageId,
                    "VCE_CookedSoupSimple"),
                Is.True);
            Assert.That(
                MealClassificationCatalog.IsMealRegisteredForPackage(
                    MealClassificationCatalog.VanillaCookingExpandedPackageId,
                    "VCE_CannedMeat"),
                Is.False);
            Assert.That(
                MealClassificationCatalog.OwnsExplicitMealRegistry(
                    MealClassificationCatalog.VanillaCookingExpandedBakeryPackageId),
                Is.True);
            Assert.That(
                MealClassificationCatalog.IsMealRegisteredForPackage(
                    MealClassificationCatalog.VanillaCookingExpandedBakeryPackageId,
                    "VCE_GourmetConfection"),
                Is.False);
            Assert.That(
                MealClassificationCatalog.OwnsExplicitMealRegistry("Some.Other.Mod"),
                Is.False);
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
