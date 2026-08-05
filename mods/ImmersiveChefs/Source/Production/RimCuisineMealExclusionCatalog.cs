namespace ImmersiveChefs;

public static class RimCuisineMealExclusionCatalog
{
    private static readonly IReadOnlyDictionary<string, HashSet<string>> ExcludedByPackage =
        new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [MealClassificationCatalog.RimCuisineCorePackageId] = new(
                new[]
                {
                    "RC2_Hardtack",
                    "RC2_MealCandy",
                    "RC2_MealPastry",
                    "RC2_MealCrustyPeanuts"
                },
                StringComparer.OrdinalIgnoreCase),
            [MealClassificationCatalog.RimCuisineMealsPackageId] = new(
                new[]
                {
                    "RC2_CannedMeal",
                    "RC2_CannedMeat",
                    "RC2_CannedFruit",
                    "RC2_CannedVegetables",
                    "RC2_MealIceCream",
                    "RC2_MealChocolateIceCream",
                    "RC2_MealCrisps",
                    "RC2_MealCupcake"
                },
                StringComparer.OrdinalIgnoreCase)
        };

    public static bool IsExcluded(string? packageId, string? defName)
    {
        if (packageId is null || packageId.Trim().Length == 0 ||
            defName is null || defName.Trim().Length == 0)
        {
            return false;
        }

        return ExcludedByPackage.TryGetValue(packageId, out var excluded) &&
               excluded.Contains(defName);
    }
}
