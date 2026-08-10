using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace ImmersiveChefs.FinalProductMods.InGame.IntegrationTests;

public static class FinalProductModsIntegrationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ExactOptionalAssembliesAndRuntimeAdaptersAreActive()
    {
        var active = LoadedModManager.RunningModsListForReading
            .Select(mod => mod.PackageId)
            .ToArray();
        var expected = new[]
        {
            "brrainz.harmony",
            "ludeon.rimworld",
            "rabiosus.AdaptiveMealBill",
            "binchcannon.overcookedmeals",
            "fumblesneeze.immersivechefs",
            "fumblesneeze.rimworlddevgateway"
        };
        IntegrationAssert.Equal(
            string.Join("|", expected).ToLowerInvariant(),
            string.Join("|", active).ToLowerInvariant(),
            "The integration group must use the complete exact active package sequence.");

        var adaptiveType = AccessTools.TypeByName(AdaptiveMealBillCompatibility.AdaptiveRecipeTypeName);
        var overcookedType = AccessTools.TypeByName(OvercookedMealsCompatibility.PrefixTypeName);
        IntegrationAssert.Equal(
            "AdaptiveMealBill, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            adaptiveType?.Assembly.FullName,
            "Adaptive Meal Bill must retain the inspected assembly identity.");
        IntegrationAssert.Equal(
            "OvercookedMeals, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            overcookedType?.Assembly.FullName,
            "Overcooked Meals must retain the inspected assembly identity.");
        IntegrationAssert.True(
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.AdaptiveMealBill) &&
            AdaptiveMealBillAdapter.Enabled,
            "Adaptive Meal Bill must pass its exact package/shape gate.");
        IntegrationAssert.True(
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.OvercookedMeals) &&
            OvercookedMealsAdapter.Enabled,
            "Overcooked Meals must pass its exact package/shape gate.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void UpstreamFinalProductPatchesAndFinalizedDefsComposeOnce()
    {
        var makeProducts = AccessTools.Method(
            typeof(GenRecipe),
            nameof(GenRecipe.MakeRecipeProducts),
            new[]
            {
                typeof(RecipeDef), typeof(Pawn), typeof(System.Collections.Generic.List<Thing>),
                typeof(Thing), typeof(IBillGiver), typeof(Precept_ThingStyle),
                typeof(ThingStyleDef), typeof(int?)
            });
        var postProcess = AccessTools.Method(
            typeof(GenRecipe),
            "PostProcessProduct",
            new[]
            {
                typeof(Thing), typeof(RecipeDef), typeof(Pawn),
                typeof(Precept_ThingStyle), typeof(ThingStyleDef), typeof(int?)
            });
        var makeInfo = Harmony.GetPatchInfo(makeProducts!);
        var postInfo = Harmony.GetPatchInfo(postProcess!);
        IntegrationAssert.Equal(
            1,
            makeInfo?.Prefixes.Count(patch =>
                patch.owner == AdaptiveMealBillCompatibility.MakeProductsPatchOwner &&
                patch.PatchMethod?.DeclaringType?.FullName ==
                AdaptiveMealBillCompatibility.MakeProductsPrefixTypeName) ?? 0,
            "Adaptive Meal Bill must remain the sole validated concrete-recipe prefix owner.");
        IntegrationAssert.Equal(
            1,
            postInfo?.Prefixes.Count(patch =>
                patch.owner == OvercookedMealsCompatibility.PatchOwner &&
                patch.PatchMethod?.DeclaringType?.FullName ==
                OvercookedMealsCompatibility.PrefixTypeName) ?? 0,
            "Overcooked Meals must remain the sole validated final replacement prefix owner.");
        IntegrationAssert.Equal(
            1,
            makeInfo?.Postfixes.Count(patch =>
                patch.owner == ImmersiveChefsMod.PackageId &&
                patch.PatchMethod?.DeclaringType?.Name == "GenRecipeKitchenwarePatch") ?? 0,
            "Immersive Chefs must wrap final MakeRecipeProducts enumeration exactly once.");

        var wrapper = DefDatabase<RecipeDef>.GetNamed("CookMealFine_Adaptive");
        var concrete = DefDatabase<RecipeDef>.GetNamed("CookMealFine_Meat");
        IntegrationAssert.Null(
            MealClassificationRuntime.ClassifyRecipe(wrapper),
            "The adaptive wrapper must never be classified as a concrete meal recipe.");
        IntegrationAssert.Equal(
            MealComplexity.Advanced,
            MealClassificationRuntime.ClassifyRecipe(concrete),
            "The selected concrete Fine recipe must retain Advanced classification.");

        var finalMeal = DefDatabase<ThingDef>.GetNamed(
            OvercookedMealsCompatibility.FinalMealDefName);
        IntegrationAssert.Equal(
            "binchcannon.overcookedmeals",
            finalMeal.modContentPack?.PackageId,
            "The surviving replacement Def must remain owned by Overcooked Meals.");
        IntegrationAssert.Equal(
            1,
            finalMeal.comps.Count(properties => properties.compClass == typeof(CompEmbeddedWare)),
            "The final survivor must receive exactly one embedded-ware component.");
        IntegrationAssert.Equal(
            1,
            finalMeal.comps.Count(properties => properties.compClass == typeof(CompCulinaryState)),
            "The final survivor must receive exactly one culinary-state component.");
    }
}
