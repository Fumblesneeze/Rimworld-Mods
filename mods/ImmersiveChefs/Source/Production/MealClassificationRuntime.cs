using Verse;

namespace ImmersiveChefs;

internal static class MealClassificationRuntime
{
    private static MealClassificationCatalog catalog =
        MealClassificationCatalog.Create(Array.Empty<string>());
    private static bool noVanillaMealsActive;

    internal static void Initialize(IEnumerable<string> loadedPackageIds)
    {
        var packages = loadedPackageIds?.ToArray() ?? Array.Empty<string>();
        noVanillaMealsActive = packages.Contains(
            MealClassificationCatalog.NoVanillaMealsPackageId,
            StringComparer.OrdinalIgnoreCase);
        catalog = MealClassificationCatalog.Create(packages);
    }

    internal static MealComplexity? ClassifyRecipe(RecipeDef? recipe)
    {
        if (noVanillaMealsActive &&
            recipe?.products?.Any(product => IsOfficialMeal(product.thingDef)) == true)
        {
            return null;
        }

        if (!MealCoveragePolicy.IsCovered(recipe))
        {
            return null;
        }

        if (recipe?.GetModExtension<MealCoverageExtension>()?.complexity is { } extension)
        {
            return extension;
        }

        return recipe is null ? null : catalog.ClassifyRecipe(recipe.defName);
    }

    internal static MealComplexity? ClassifyMeal(ThingDef? meal)
    {
        if (noVanillaMealsActive && IsOfficialMeal(meal))
        {
            return null;
        }

        if (!MealCoveragePolicy.IsCovered(meal))
        {
            return null;
        }

        if (meal?.GetModExtension<MealCoverageExtension>()?.complexity is { } extension)
        {
            return extension;
        }

        return meal is null ? null : catalog.ClassifyMeal(meal.defName);
    }

    internal static bool PreservesOriginalWorkAmount(RecipeDef? recipe)
    {
        return recipe is not null && catalog.PreservesOriginalWorkAmount(recipe.defName);
    }

    private static bool IsOfficialMeal(ThingDef? thingDef)
    {
        var foodType = thingDef?.ingestible?.foodType ?? FoodTypeFlags.None;
        return thingDef?.modContentPack?.IsOfficialMod == true &&
               (foodType & FoodTypeFlags.Meal) != 0;
    }
}
