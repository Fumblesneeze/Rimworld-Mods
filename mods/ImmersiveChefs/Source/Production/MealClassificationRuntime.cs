using Verse;

namespace ImmersiveChefs;

internal static class MealClassificationRuntime
{
    private static MealClassificationCatalog catalog =
        MealClassificationCatalog.Create(Array.Empty<string>());

    internal static void Initialize(IEnumerable<string> loadedPackageIds)
    {
        catalog = MealClassificationCatalog.Create(loadedPackageIds);
    }

    internal static MealComplexity? ClassifyRecipe(RecipeDef? recipe)
    {
        if (recipe?.GetModExtension<MealCoverageExtension>()?.complexity is { } extension)
        {
            return extension;
        }

        return recipe is null ? null : catalog.ClassifyRecipe(recipe.defName);
    }

    internal static MealComplexity? ClassifyMeal(ThingDef? meal)
    {
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
}
