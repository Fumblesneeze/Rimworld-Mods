using RimWorld;
using Verse;

namespace ImmersiveChefs;

public static class MealCoveragePolicy
{
    private static readonly HashSet<string> BuiltInExclusions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pemmican",
        "MealSurvivalPack",
        "BabyFood"
    };

    public static bool IsCovered(RecipeDef? recipe)
    {
        if (recipe is null || recipe.GetModExtension<MealCoverageExtension>()?.excluded == true)
        {
            return false;
        }

        return recipe.products?.Any(product => IsCovered(product.thingDef)) == true;
    }

    public static bool IsCovered(ThingDef? thingDef)
    {
        if (thingDef?.ingestible is null || BuiltInExclusions.Contains(thingDef.defName) ||
            thingDef.GetModExtension<MealCoverageExtension>()?.excluded == true)
        {
            return false;
        }

        var foodType = thingDef.ingestible.foodType;
        return (foodType & FoodTypeFlags.Meal) != 0 &&
               (foodType & (FoodTypeFlags.Fluid | FoodTypeFlags.Liquor)) == 0;
    }

    public static int ServingCount(RecipeDef recipe)
    {
        return recipe.products?
                   .Where(product => IsCovered(product.thingDef))
                   .Sum(product => Math.Max(0, product.count)) ?? 0;
    }
}
