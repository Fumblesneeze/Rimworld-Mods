using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace ImmersiveChefs.FoodSearchMods.InGame.IntegrationTests;

public static class FoodSearchModsIntegrationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ExactOptionalAssembliesAndRuntimeAdaptersAreActive()
    {
        var active = LoadedModManager.RunningModsListForReading
            .Select(mod => mod.PackageId)
            .ToArray();
        var expected = new[]
        {
            "ludeon.rimworld",
            "brrainz.harmony",
            "Argon.CheapMeals",
            "Memegoddess.MealsOnWheels",
            "seekiworksmod.no10",
            "fumblesneeze.immersivechefs",
            "fumblesneeze.rimworlddevgateway"
        };
        IntegrationAssert.Equal(
            string.Join("|", expected).ToLowerInvariant(),
            string.Join("|", active).ToLowerInvariant(),
            "The integration group must use the complete exact active package sequence.");

        var mealsOnWheelsType = AccessTools.TypeByName(MealsOnWheelsCompatibility.PatchTypeName);
        var prioritizeType = AccessTools.TypeByName(PrioritizeMealsCompatibility.StartupTypeName);
        IntegrationAssert.Equal(
            "Meals_On_Wheels, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            mealsOnWheelsType?.Assembly.FullName,
            "Meals on Wheels must retain the inspected assembly identity.");
        IntegrationAssert.Equal(
            "Prioritize Meals over Preserved Foods, Version=2.3.0.0, Culture=neutral, PublicKeyToken=null",
            prioritizeType?.Assembly.FullName,
            "Prioritize Meals must retain the inspected assembly identity.");
        IntegrationAssert.True(
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.MealsOnWheels) &&
            MealsOnWheelsAdapter.Enabled,
            "Meals on Wheels must pass its exact package and runtime-shape gate.");
        IntegrationAssert.True(
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.PrioritizeMeals) &&
            PrioritizeMealsAdapter.Enabled,
            "Prioritize Meals must pass its exact package and runtime-shape gate.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void UpstreamFoodSearchAndTraderPatchOwnersRemainAuthoritative()
    {
        var mealsOnWheelsPatch = AccessTools.TypeByName(MealsOnWheelsCompatibility.PatchTypeName)
            ?.GetMethod(
                MealsOnWheelsCompatibility.PostfixMethodName,
                BindingFlags.Public | BindingFlags.Static);
        var mealsOnWheelsTarget = AccessTools.Method(
            typeof(FoodUtility),
            nameof(FoodUtility.TryFindBestFoodSourceFor),
            new[]
            {
                typeof(Pawn), typeof(Pawn), typeof(bool),
                typeof(Thing).MakeByRefType(), typeof(ThingDef).MakeByRefType(),
                typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool),
                typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool),
                typeof(bool), typeof(FoodPreferability)
            });
        IntegrationAssert.NotNull(
            mealsOnWheelsTarget,
            "The exact 17-parameter RimWorld food-search overload must remain available.");
        var mealsOnWheelsPatches = (Harmony.GetPatchInfo(mealsOnWheelsTarget)?.Postfixes ?? Enumerable.Empty<Patch>())
            .Where(patch =>
                patch.owner == MealsOnWheelsCompatibility.PatchOwner &&
                patch.PatchMethod == mealsOnWheelsPatch &&
                patch.priority == Priority.Low)
            .ToArray();
        IntegrationAssert.Equal(
            1,
            mealsOnWheelsPatches.Length,
            "Meals on Wheels must retain exactly one upstream mobile food-source postfix.");

        var prioritizePatch = AccessTools.TypeByName(PrioritizeMealsCompatibility.CaravanPatchTypeName)
            ?.GetMethod(
                PrioritizeMealsCompatibility.CaravanPostfixMethodName,
                BindingFlags.NonPublic | BindingFlags.Static);
        var prioritizePatches = Harmony.GetAllPatchedMethods()
            .Where(method =>
                method.DeclaringType == typeof(IncidentWorker_TraderCaravanArrival) &&
                method.Name == "SendLetter")
            .SelectMany(method => Harmony.GetPatchInfo(method)?.Postfixes ?? Enumerable.Empty<Patch>())
            .Where(patch =>
                patch.owner == PrioritizeMealsCompatibility.PatchOwner &&
                patch.PatchMethod == prioritizePatch)
            .ToArray();
        IntegrationAssert.Equal(
            1,
            prioritizePatches.Length,
            "Prioritize Meals must retain exactly one upstream trader-caravan compensation postfix.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedFoodOrderingAndImmersiveMealComponentsCompose()
    {
        var foodsType = AccessTools.TypeByName(PrioritizeMealsCompatibility.FoodsTypeName);
        var preservedFoodsField = foodsType?.GetField(
            PrioritizeMealsCompatibility.PreservedFoodsFieldName,
            BindingFlags.NonPublic | BindingFlags.Static);
        var preservedFoods = preservedFoodsField?.GetValue(null) as HashSet<ThingDef>;
        IntegrationAssert.NotNull(
            preservedFoods,
            "Prioritize Meals must finalize its exact preserved-food ledger before main-menu integration tests run.");

        var simpleMeal = DefDatabase<ThingDef>.GetNamed("MealSimple");
        var pemmican = DefDatabase<ThingDef>.GetNamed("Pemmican");
        var survivalMeal = DefDatabase<ThingDef>.GetNamed("MealSurvivalPack");
        var fastSimple = DefDatabase<ThingDef>.GetNamed("CM_SimpleFastMeal");
        var fastDeluxe = DefDatabase<ThingDef>.GetNamed("CM_DeluxeFastMeal");

        IntegrationAssert.True(
            preservedFoods!.Contains(pemmican) && preservedFoods.Contains(survivalMeal),
            "Pemmican and packaged survival meals must remain upstream-classified preserved foods.");
        IntegrationAssert.True(
            !preservedFoods.Contains(simpleMeal) &&
            !preservedFoods.Contains(fastSimple) &&
            !preservedFoods.Contains(fastDeluxe),
            "Perishable vanilla and Fast Meals servings must remain outside the preserved-food ledger.");
        IntegrationAssert.True(
            simpleMeal.ingestible.optimalityOffsetHumanlikes > pemmican.ingestible.optimalityOffsetHumanlikes &&
            simpleMeal.ingestible.optimalityOffsetHumanlikes > survivalMeal.ingestible.optimalityOffsetHumanlikes &&
            fastSimple.ingestible.optimalityOffsetHumanlikes > survivalMeal.ingestible.optimalityOffsetHumanlikes,
            "Upstream finalized optimality must prefer perishable meals over preserved controls.");
        IntegrationAssert.Equal(
            FoodPreferability.MealAwful,
            survivalMeal.ingestible.preferability,
            "Prioritize Meals must demote the packaged survival meal to its validated preserved-food preferability.");
        IntegrationAssert.Equal(
            FoodPreferability.MealSimple,
            simpleMeal.ingestible.preferability,
            "Prioritize Meals must leave the vanilla perishable simple meal at MealSimple preferability.");
        IntegrationAssert.Equal(
            FoodPreferability.MealSimple,
            fastSimple.ingestible.preferability,
            "Prioritize Meals must leave the perishable Fast Meals serving at MealSimple preferability.");

        foreach (var meal in new[] { fastSimple, fastDeluxe })
        {
            IntegrationAssert.Equal(
                1,
                meal.comps.Count(properties => properties.compClass == typeof(CompEmbeddedWare)),
                meal.defName + " must receive exactly one embedded-ware component.");
            IntegrationAssert.Equal(
                1,
                meal.comps.Count(properties => properties.compClass == typeof(CompCulinaryState)),
                meal.defName + " must receive exactly one culinary-state component.");
        }
    }
}
