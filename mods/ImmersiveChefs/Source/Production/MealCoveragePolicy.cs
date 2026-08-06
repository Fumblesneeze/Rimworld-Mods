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
            thingDef.GetModExtension<MealCoverageExtension>()?.excluded == true ||
            RimCuisineMealExclusionCatalog.IsExcluded(
                thingDef.modContentPack?.PackageId,
                thingDef.defName))
        {
            return false;
        }

        var owningPackageId = thingDef.modContentPack?.PackageId;
        if (MealClassificationCatalog.OwnsExplicitMealRegistry(owningPackageId) &&
            !MealClassificationRuntime.IsMealRegisteredForActivePackage(
                owningPackageId,
                thingDef.defName))
        {
            return false;
        }

        // Def-removal mods can leave static ThingDefOf fields and RecipeDef product references
        // pointing at objects that no longer belong to the finalized database. Those objects are
        // metadata remnants, not spawnable meal definitions.
        if (!ReferenceEquals(DefDatabase<ThingDef>.GetNamedSilentFail(thingDef.defName), thingDef))
        {
            return false;
        }

        var foodType = thingDef.ingestible.foodType;
        return (foodType & FoodTypeFlags.Meal) != 0 &&
               (foodType & (FoodTypeFlags.Fluid | FoodTypeFlags.Liquor)) == 0;
    }

    internal static bool IsBuiltInExcluded(string defName) => BuiltInExclusions.Contains(defName);

    public static int ServingCount(RecipeDef recipe)
    {
        return recipe.products?
                   .Where(product => IsCovered(product.thingDef))
                   .Sum(product => Math.Max(0, product.count)) ?? 0;
    }
}
