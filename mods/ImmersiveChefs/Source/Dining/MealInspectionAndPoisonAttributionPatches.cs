using HarmonyLib;
using RimWorld;
using Verse;

namespace ImmersiveChefs;

[HarmonyPatch(typeof(ThingComp), nameof(ThingComp.CompInspectStringExtra))]
[HarmonyPriority(Priority.Last)]
internal static class FoodPoisonInspectPrivacyPatch
{
    private static void Postfix(ThingComp __instance, ref string __result)
    {
        if (__instance is CompFoodPoisonable &&
            MealCoveragePolicy.IsCovered(__instance.parent?.def))
        {
            __result = string.Empty;
        }
    }
}

[HarmonyPatch(typeof(FoodPoisonCauseExtension), nameof(FoodPoisonCauseExtension.ToStringHuman))]
[HarmonyPriority(Priority.First)]
internal static class FoodPoisonCauseAttributionPatch
{
    private static bool Prefix(FoodPoisonCause cause, ref string __result)
    {
        var contributor = FoodPoisonAttributionContext.Current;
        if (contributor == FoodPoisonRiskContributor.None)
        {
            return true;
        }

        if (contributor == FoodPoisonRiskContributor.VanillaBase &&
            cause != FoodPoisonCause.Unknown)
        {
            return true;
        }

        __result = FoodPoisonAttributionLabels.TranslationKey(contributor).Translate();
        return false;
    }
}

internal static class FoodPoisonAttributionContext
{
    [ThreadStatic]
    private static FoodPoisonRiskContributor current;

    internal static FoodPoisonRiskContributor Current => current;

    internal static FoodPoisonRiskContributor Replace(FoodPoisonRiskContributor contributor)
    {
        var previous = current;
        current = contributor;
        return previous;
    }
}

public static class FoodPoisonAttributionLabels
{
    public static string TranslationKey(FoodPoisonRiskContributor contributor)
    {
        return contributor switch
        {
            FoodPoisonRiskContributor.LowCulinaryQuality => "ImmersiveChefs_PoisonCause_LowQuality",
            FoodPoisonRiskContributor.ColdMeal => "ImmersiveChefs_PoisonCause_ColdMeal",
            FoodPoisonRiskContributor.FrozenMeal => "ImmersiveChefs_PoisonCause_FrozenMeal",
            FoodPoisonRiskContributor.DirtyCookware => "ImmersiveChefs_PoisonCause_DirtyCookware",
            FoodPoisonRiskContributor.DirtyPlate => "ImmersiveChefs_PoisonCause_DirtyPlate",
            FoodPoisonRiskContributor.DirtyCutlery => "ImmersiveChefs_PoisonCause_DirtyCutlery",
            FoodPoisonRiskContributor.WildWaterCookware => "ImmersiveChefs_PoisonCause_UnsafeWashedCookware",
            FoodPoisonRiskContributor.WildWaterPlate => "ImmersiveChefs_PoisonCause_UnsafeWashedPlate",
            FoodPoisonRiskContributor.WildWaterCutlery => "ImmersiveChefs_PoisonCause_UnsafeWashedCutlery",
            FoodPoisonRiskContributor.PoorPlate => "ImmersiveChefs_PoisonCause_PoorPlate",
            FoodPoisonRiskContributor.PoorCutlery => "ImmersiveChefs_PoisonCause_PoorCutlery",
            FoodPoisonRiskContributor.MicrowaveReheating => "ImmersiveChefs_PoisonCause_ReheatedMeal",
            FoodPoisonRiskContributor.VanillaBase => "ImmersiveChefs_PoisonCause_PoisonedMeal",
            _ => "UnknownLower"
        };
    }
}
