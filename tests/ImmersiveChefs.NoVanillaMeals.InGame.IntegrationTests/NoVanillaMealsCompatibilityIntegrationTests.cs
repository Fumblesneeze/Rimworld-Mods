using System;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace ImmersiveChefs.NoVanillaMeals.InGame.IntegrationTests;

public static class NoVanillaMealsCompatibilityIntegrationTests
{
    private static readonly string[] ExactPackageIds =
    {
        "brrainz.harmony",
        "ludeon.rimworld",
        MealClassificationCatalog.ProcessorFrameworkPackageId,
        MealClassificationCatalog.RimCuisineCorePackageId,
        MealClassificationCatalog.RimCuisineMealsPackageId,
        "Mlie.RC2.BaBE",
        "Mlie.RC2.SaSE",
        MealClassificationCatalog.NoVanillaMealsPackageId,
        ImmersiveChefsMod.PackageId,
        "fumblesneeze.rimworlddevgateway"
    };

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ExactRimCuisineNoVanillaEnvironmentIsActive()
    {
        var active = LoadedModManager.RunningModsListForReading
            .Select(mod => mod.PackageId)
            .ToArray();

        IntegrationAssert.Equal(
            ExactPackageIds.Length,
            active.Length,
            "The compatibility bundle must run only under its declared complete active-mod matrix.");
        for (var index = 0; index < ExactPackageIds.Length; index++)
        {
            IntegrationAssert.True(
                string.Equals(ExactPackageIds[index], active[index], StringComparison.OrdinalIgnoreCase),
                $"Active package {index} must be {ExactPackageIds[index]}, not {active[index]}.");
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void RemovedOfficialMealsAreAbsentWithoutStaleRuntimeClassifications()
    {
        var activeCatalog = MealClassificationCatalog.Create(
            LoadedModManager.RunningModsListForReading.Select(mod => mod.PackageId));
        var removedMealDefs = new[]
        {
            "PackagedSurvivalMeal", "MealNutrientPaste", "MealSimple", "MealFine",
            "MealFine_Meat", "MealFine_Veg", "MealLavish", "MealLavish_Meat",
            "MealLavish_Veg", "Pemmican"
        };
        foreach (var defName in removedMealDefs)
        {
            IntegrationAssert.Null(
                DefDatabase<ThingDef>.GetNamedSilentFail(defName),
                $"No Vanilla Meals must remove finalized official ThingDef {defName}.");
            IntegrationAssert.Null(
                activeCatalog.ClassifyMeal(defName),
                $"Immersive Chefs must not retain stale classification for removed ThingDef {defName}.");
        }

        foreach (var recipeDefName in new[]
                 {
                     "CookMealSimple", "CookMealSimpleBulk", "CookMealFine", "CookMealFine_Meat",
                     "CookMealFine_Veg", "CookMealFineBulk", "CookMealFineBulk_Meat",
                     "CookMealFineBulk_Veg", "CookMealLavish", "CookMealLavish_Meat",
                     "CookMealLavish_Veg", "CookMealLavishBulk", "CookMealLavishBulk_Meat",
                     "CookMealLavishBulk_Veg", "RC2_CookFineMealBulk", "RC2_CookLavishMealBulk"
                 })
        {
            var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(recipeDefName);
            if (recipe is null)
            {
                continue;
            }

            IntegrationAssert.Null(
                MealClassificationRuntime.ClassifyRecipe(recipe),
                $"Removed-product recipe {recipeDefName} must not enter the runtime meal registry.");
            IntegrationAssert.Equal(
                1f,
                RecipeWorkRuntime.MultiplierFor(recipe),
                $"Removed-product recipe {recipeDefName} must not retain an initialization-time work multiplier.");
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void SurvivingRimCuisineMealsReceiveExactRuntimeCoverage()
    {
        AssertRecipe("CookThinPottage", MealComplexity.Simple);
        AssertMeal("RC2_ThinPottage", MealComplexity.Simple);
        AssertRecipe("RC2_CookThickPottage", MealComplexity.Advanced);
        AssertMeal("RC2_ThickPottage", MealComplexity.Advanced);
        AssertRecipe("RC2_CookRubaboo", MealComplexity.Simple);
        AssertMeal("RC2_Rubaboo", MealComplexity.Simple);
        AssertRecipe("RC2_CookExtravagantMeal", MealComplexity.Elaborate);
        AssertRecipe("RC2_CookExtravagantMealBulk", MealComplexity.Elaborate);
        AssertMeal("RC2_ExtravagantMeal", MealComplexity.Elaborate);
        AssertMeal("RC2_Pizza", MealComplexity.Elaborate);
    }

    private static void AssertRecipe(string defName, MealComplexity expected)
    {
        var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(defName);
        IntegrationAssert.NotNull(recipe, $"RimCuisine must finalize recipe {defName}.");
        var actual = MealClassificationRuntime.ClassifyRecipe(recipe);
        IntegrationAssert.True(
            actual.HasValue,
            $"RimCuisine recipe {defName} must remain in the runtime meal registry.");
        IntegrationAssert.Equal(
            expected,
            actual!.Value,
            $"RimCuisine recipe {defName} must use its explicit compatibility tier.");
    }

    private static void AssertMeal(string defName, MealComplexity expected)
    {
        var meal = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
        IntegrationAssert.NotNull(meal, $"RimCuisine must finalize meal {defName}.");
        var actual = MealComplexityRuntime.Classify(meal);
        IntegrationAssert.True(
            actual.HasValue,
            $"RimCuisine meal {defName} must remain in the runtime meal registry.");
        IntegrationAssert.Equal(
            expected,
            actual!.Value,
            $"RimCuisine meal {defName} must use its explicit compatibility tier.");
        IntegrationAssert.True(
            meal!.comps?.Any(properties => properties.compClass == typeof(CompEmbeddedWare)) == true,
            $"Covered RimCuisine meal {defName} must receive embedded plate state.");
        IntegrationAssert.True(
            meal.comps?.Any(properties => properties.compClass == typeof(CompCulinaryState)) == true,
            $"Covered RimCuisine meal {defName} must receive culinary serving state.");
    }
}
