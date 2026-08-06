using Verse;

namespace ImmersiveChefs;

internal static class MealClassificationRuntime
{
    private static MealClassificationCatalog catalog =
        MealClassificationCatalog.Create(Array.Empty<string>());
    private static bool noVanillaMealsActive;
    private static bool initialized;

    internal static IReadOnlyList<MealClassificationShapeFailure> ValidationFailures { get; private set; } =
        Array.Empty<MealClassificationShapeFailure>();

    internal static void Initialize(IEnumerable<string> loadedPackageIds)
    {
        Initialize(
            loadedPackageIds,
            defName => DefDatabase<RecipeDef>.GetNamedSilentFail(defName) is not null,
            defName => DefDatabase<ThingDef>.GetNamedSilentFail(defName) is not null,
            Log.Warning);
    }

    internal static void Initialize(
        IEnumerable<string> loadedPackageIds,
        Func<string, bool> recipeDefExists,
        Func<string, bool> mealDefExists,
        Action<string> warningSink)
    {
        var packages = loadedPackageIds?.ToArray() ?? Array.Empty<string>();
        noVanillaMealsActive = packages.Contains(
            MealClassificationCatalog.NoVanillaMealsPackageId,
            StringComparer.OrdinalIgnoreCase);
        var result = MealClassificationCatalog.CreateValidated(
            packages,
            recipeDefExists,
            mealDefExists);
        catalog = result.Catalog;
        initialized = true;
        ValidationFailures = result.Failures;
        foreach (var failure in result.Failures)
        {
            warningSink(
                $"[ImmersiveChefs] Disabled meal registry for {failure.PackageId} because its finalized Def shape changed. " +
                $"Missing RecipeDefs: {DescribeMissing(failure.MissingRecipeDefNames)}. " +
                $"Missing ThingDefs: {DescribeMissing(failure.MissingMealDefNames)}.");
        }
    }

    internal static MealComplexity? ClassifyRecipe(RecipeDef? recipe)
    {
        if (noVanillaMealsActive &&
            recipe?.products?.Any(product => IsOfficialMeal(product.thingDef)) == true)
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

    internal static bool IsMealRegisteredForActivePackage(
        string? packageId,
        string? mealDefName)
    {
        return initialized
            ? catalog.ContainsRegisteredMeal(packageId, mealDefName)
            : MealClassificationCatalog.IsMealRegisteredForPackage(packageId, mealDefName);
    }

    private static bool IsOfficialMeal(ThingDef? thingDef)
    {
        var foodType = thingDef?.ingestible?.foodType ?? FoodTypeFlags.None;
        return thingDef?.modContentPack?.IsOfficialMod == true &&
               (foodType & FoodTypeFlags.Meal) != 0;
    }

    private static string DescribeMissing(IReadOnlyList<string> defNames)
    {
        return defNames.Count == 0 ? "none" : string.Join(", ", defNames);
    }
}
