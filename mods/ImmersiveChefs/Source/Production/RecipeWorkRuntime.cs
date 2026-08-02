using HarmonyLib;
using Verse;

namespace ImmersiveChefs;

public static class RecipeWorkRuntime
{
    private static readonly Dictionary<string, float> Multipliers = new(StringComparer.OrdinalIgnoreCase);

    public static void Initialize(ImmersiveChefsSettings settings)
    {
        Multipliers.Clear();
        var catalog = RecipeClassificationCatalog.CreateVanilla();
        foreach (var recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
        {
            var explicitComplexity = recipe.GetModExtension<MealCoverageExtension>()?.complexity;
            var complexity = explicitComplexity ?? catalog.Classify(recipe.defName);
            if (!complexity.HasValue)
            {
                continue;
            }

            Multipliers[recipe.defName] = complexity.Value switch
            {
                MealComplexity.Simple => settings.SimpleRecipeTimeMultiplier,
                MealComplexity.Advanced => settings.AdvancedRecipeTimeMultiplier,
                MealComplexity.Elaborate => settings.ElaborateRecipeTimeMultiplier,
                _ => 1f
            };
        }
    }

    public static float MultiplierFor(RecipeDef recipe)
    {
        return recipe is not null && Multipliers.TryGetValue(recipe.defName, out var value) ? value : 1f;
    }
}

[HarmonyPatch(typeof(RecipeDef), nameof(RecipeDef.WorkAmountForStuff))]
internal static class RecipeDef_WorkAmountForStuff_ImmersiveChefsPatch
{
    private static void Postfix(RecipeDef __instance, ref float __result)
    {
        __result *= RecipeWorkRuntime.MultiplierFor(__instance);
    }
}
