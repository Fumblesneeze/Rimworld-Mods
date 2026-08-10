using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace ImmersiveChefs.VceFriedMods.InGame.IntegrationTests;

public static class VceFriedModsIntegrationTests
{
    private static readonly string[] ExpectedPackages =
    {
        "brrainz.harmony",
        "ludeon.rimworld",
        "OskarPotocki.VanillaFactionsExpanded.Core",
        "VanillaExpanded.VCookE",
        "VanillaExpanded.VCookEBakery",
        "VanillaExpanded.VCookEHaute",
        "VanillaExpanded.VCookEStews",
        "VanillaExpanded.VCEF",
        "VanillaExpanded.VCookESushi",
        "ucp.friedmeals",
        "rabiosus.AdaptiveMealBill",
        "binchcannon.overcookedmeals",
        "fumblesneeze.immersivechefs",
        "fumblesneeze.rimworlddevgateway"
    };

    private static readonly ExpectedDef[] Recipes =
    {
        Simple("VCE_CookBakeSimple"), Simple("VCE_CookBakeSimpleBulk"),
        Simple("VCE_CookGrillSimple"), Simple("VCE_CookGrillSimpleBulk"),
        Simple("VCE_CookSoupSimple"),
        Advanced("VCE_CookBakeFine"), Advanced("VCE_CookBakeFineBulk"),
        Advanced("VCE_CookGrillFine"), Advanced("VCE_CookGrillFineBulk"),
        Advanced("VCE_CookSoupFine"),
        Elaborate("VCE_CookBakeLavish"), Elaborate("VCE_CookBakeLavishBulk"),
        Elaborate("VCE_CookBakeGourmet"), Elaborate("VCE_CookGrillLavish"),
        Elaborate("VCE_CookGrillLavishhBulk"), Elaborate("VCE_CookGrillGourmet"),
        Elaborate("VCE_CookMealGourmet"), Elaborate("VCE_CookSoupLavish"),
        Elaborate("VCE_CookSoupGourmet"),
        Elaborate("VCE_CookMealHaute"),
        Simple("VCE_CookStewSimple"), Advanced("VCE_CookStewFine"),
        Elaborate("VCE_CookStewLavish"),
        Simple("VCE_CookChirashizushiSimple"), Simple("VCE_CookChirashizushiSimpleBulk"),
        Simple("VCE_CookNorimakiSimple"), Simple("VCE_CookNorimakiSimpleBulk"),
        Advanced("VCE_CookUramakiFine"), Advanced("VCE_CookUramakiFineBulk"),
        Advanced("VCE_CookNigiriFine"), Advanced("VCE_CookNigiriFineBulk"),
        Elaborate("VCE_CookTemakiLavish"), Elaborate("VCE_CookTemakiLavishBulk"),
        Elaborate("VCE_CookFutomakiLavish"), Elaborate("VCE_CookFutomakiLavishBulk"),
        Elaborate("VCE_CookGunkanmakiGourmet"), Elaborate("VCE_CookOshizushiiGourmet"),
        Simple("CookFritterSimple"), Simple("CookFritterSimpleBulk"),
        Advanced("CookFritterFine"), Advanced("CookFritterFineBulk"),
        Elaborate("CookFritterLavish"), Elaborate("CookFritterLavishBulk"),
        Elaborate("VCE_CookFritterGourmet")
    };

    private static readonly ExpectedDef[] Meals =
    {
        Simple("VCE_SimpleBake"), Simple("VCE_SimpleGrill"),
        Simple("VCE_RuinedSimpleGrill"), Simple("VCE_CookedSoupSimple"),
        Advanced("VCE_FineBake"), Advanced("VCE_FineGrill"),
        Advanced("VCE_RuinedFineGrill"), Advanced("VCE_CookedSoupFine"),
        Elaborate("VCE_LavishBake"), Elaborate("VCE_GourmetBake"),
        Elaborate("VCE_LavishGrill"), Elaborate("VCE_GourmetGrill"),
        Elaborate("VCE_RuinedLavishGrill"), Elaborate("VCE_RuinedGourmetGrill"),
        Elaborate("VCE_MealGourmet"), Elaborate("VCE_CookedSoupLavish"),
        Elaborate("VCE_CookedSoupGourmet"), Elaborate("VCE_MealHaute"),
        Simple("VCE_CookedStewSimple"), Advanced("VCE_CookedStewFine"),
        Elaborate("VCE_CookedStewLavish"),
        Simple("VCE_Chirashizushi"), Simple("VCE_Norimaki"),
        Advanced("VCE_Uramaki"), Advanced("VCE_Nigiri"),
        Elaborate("VCE_Temaki"), Elaborate("VCE_Futomaki"),
        Elaborate("VCE_Gunkanmaki"), Elaborate("VCE_Oshizushi"),
        Simple("ucp_SimpleFritter"), Advanced("ucp_FineFritter"),
        Elaborate("ucp_LavishFritter"), Elaborate("ucp_GourmetFritter")
    };

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ExactGroupAndFinalizedRegistryShapeAreActive()
    {
        var active = LoadedModManager.RunningModsListForReading.Select(mod => mod.PackageId).ToArray();
        IntegrationAssert.Equal(
            string.Join("|", ExpectedPackages).ToLowerInvariant(),
            string.Join("|", active).ToLowerInvariant(),
            "The VCE/Fried integration group must use the complete exact active package sequence.");
        IntegrationAssert.Equal(
            0,
            MealClassificationRuntime.ValidationFailures.Count,
            "Every active optional meal registry must match its inspected finalized Def shape.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ExactRecipesReceiveDeclaredComplexityAndFinalizedWorkMultipliers()
    {
        foreach (var expected in Recipes)
        {
            var recipe = DefDatabase<RecipeDef>.GetNamed(expected.DefName);
            IntegrationAssert.Equal(
                expected.Complexity,
                MealClassificationRuntime.ClassifyRecipe(recipe),
                expected.DefName + " must retain its exact package-gated complexity.");
            IntegrationAssert.Equal(
                Multiplier(expected.Complexity),
                RecipeWorkRuntime.MultiplierFor(recipe),
                expected.DefName + " must receive its finalized configured work multiplier.");
            var baseWorkAmount = recipe.workAmount >= 0f
                ? recipe.workAmount
                : recipe.products[0].thingDef.GetStatValueAbstract(RimWorld.StatDefOf.WorkToMake, null);
            var expectedWorkAmount = baseWorkAmount * Multiplier(expected.Complexity);
            var actualWorkAmount = recipe.WorkAmountForStuff(null);
            IntegrationAssert.True(
                Math.Abs(actualWorkAmount - expectedWorkAmount) < 0.01f,
                expected.DefName + " must expose its Harmony-adjusted finalized work amount; " +
                "expected " + expectedWorkAmount + " from base " + baseWorkAmount +
                ", actual " + actualWorkAmount + ".");
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ExactFinalMealsReceiveOneImmersiveLifecycle()
    {
        foreach (var expected in Meals)
        {
            var meal = DefDatabase<ThingDef>.GetNamed(expected.DefName);
            IntegrationAssert.Equal(
                expected.Complexity,
                MealClassificationRuntime.ClassifyMeal(meal),
                expected.DefName + " must retain its exact package-gated service tier.");
            IntegrationAssert.Equal(
                1,
                meal.comps.Count(properties => properties.compClass == typeof(CompEmbeddedWare)),
                expected.DefName + " must receive exactly one embedded-ware component.");
            IntegrationAssert.Equal(
                1,
                meal.comps.Count(properties => properties.compClass == typeof(CompCulinaryState)),
                expected.DefName + " must receive exactly one culinary-state component.");
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void BakeryAndNonMealProductsRemainExcludedAndUpstreamOwned()
    {
        var excludedRecipes = new[]
        {
            "VCE_CookCakeBatter", "VCE_CookDessertGourmet", "VCE_CanMeats",
            "VCE_DeepFryMeats", "VCE_ChopCondiments"
        };
        foreach (var defName in excludedRecipes)
        {
            var recipe = DefDatabase<RecipeDef>.GetNamed(defName);
            IntegrationAssert.Null(
                MealClassificationRuntime.ClassifyRecipe(recipe),
                defName + " must remain outside the explicit full-meal registry.");
            IntegrationAssert.Equal(
                1f,
                RecipeWorkRuntime.MultiplierFor(recipe),
                defName + " must retain its upstream work amount.");
        }

        var excludedProducts = new[]
        {
            "VCE_NormalCakeBatter", "VCE_GourmetConfection", "VCE_GourmetDessert",
            "VCE_CannedMeat", "VCE_DeepFriedBigMeat", "VCE_Mayo"
        };
        foreach (var defName in excludedProducts)
        {
            var product = DefDatabase<ThingDef>.GetNamed(defName);
            IntegrationAssert.Null(
                MealClassificationRuntime.ClassifyMeal(product),
                defName + " must remain outside the explicit full-meal registry.");
            IntegrationAssert.Equal(
                0,
                product.comps.Count(properties =>
                    properties.compClass == typeof(CompEmbeddedWare) ||
                    properties.compClass == typeof(CompCulinaryState)),
                defName + " must not receive an Immersive Chefs meal lifecycle.");
        }

        AssertProcessorOwner("VCE_ElectricPot");
        AssertProcessorOwner("VCE_StewPot");
        AssertProcessorOwner("VCE_BakeryOven_Electric");
    }

    private static void AssertProcessorOwner(string buildingDefName)
    {
        var building = DefDatabase<ThingDef>.GetNamed(buildingDefName);
        IntegrationAssert.Equal(
            1,
            building.comps.Count(properties =>
                properties.GetType().FullName == "PipeSystem.CompProperties_AdvancedResourceProcessor"),
            buildingDefName + " must retain exactly one upstream VEF processor owner.");
    }

    private static float Multiplier(MealComplexity complexity)
    {
        return complexity switch
        {
            MealComplexity.Simple => ImmersiveChefsMod.Settings.SimpleRecipeTimeMultiplier,
            MealComplexity.Advanced => ImmersiveChefsMod.Settings.AdvancedRecipeTimeMultiplier,
            MealComplexity.Elaborate => ImmersiveChefsMod.Settings.ElaborateRecipeTimeMultiplier,
            _ => 1f
        };
    }

    private static ExpectedDef Simple(string defName) =>
        new(defName, MealComplexity.Simple);

    private static ExpectedDef Advanced(string defName) =>
        new(defName, MealComplexity.Advanced);

    private static ExpectedDef Elaborate(string defName) =>
        new(defName, MealComplexity.Elaborate);

    private readonly struct ExpectedDef
    {
        internal ExpectedDef(string defName, MealComplexity complexity)
        {
            DefName = defName;
            Complexity = complexity;
        }

        internal string DefName { get; }

        internal MealComplexity Complexity { get; }
    }
}
