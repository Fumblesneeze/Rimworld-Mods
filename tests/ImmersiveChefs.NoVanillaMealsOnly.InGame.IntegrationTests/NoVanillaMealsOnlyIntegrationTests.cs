using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.NoVanillaMealsOnly.InGame.IntegrationTests;

public static class NoVanillaMealsOnlyIntegrationTests
{
    private static readonly string[] ExactPackageIds =
    {
        "ludeon.rimworld",
        "brrainz.harmony",
        MealClassificationCatalog.NoVanillaMealsPackageId,
        ImmersiveChefsMod.PackageId,
        "fumblesneeze.rimworlddevgateway"
    };

    private static readonly string[] RemovedOfficialMealDefs =
    {
        "PackagedSurvivalMeal", "MealNutrientPaste", "MealSimple", "MealFine",
        "MealFine_Meat", "MealFine_Veg", "MealLavish", "MealLavish_Meat",
        "MealLavish_Veg", "Pemmican"
    };

    private static readonly string[] RemovedProductRecipes =
    {
        "CookMealSimple", "CookMealSimpleBulk", "CookMealFine", "CookMealFine_Meat",
        "CookMealFine_Veg", "CookMealFineBulk", "CookMealFineBulk_Meat",
        "CookMealFineBulk_Veg", "CookMealLavish", "CookMealLavish_Meat",
        "CookMealLavish_Veg", "CookMealLavishBulk", "CookMealLavishBulk_Meat",
        "CookMealLavishBulk_Veg"
    };

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ExactNoReplacementEnvironmentHasNoStaleMealRegistry()
    {
        var active = LoadedModManager.RunningModsListForReading
            .Select(mod => mod.PackageId)
            .ToArray();
        IntegrationAssert.Equal(ExactPackageIds.Length, active.Length);
        for (var index = 0; index < ExactPackageIds.Length; index++)
        {
            IntegrationAssert.True(
                string.Equals(ExactPackageIds[index], active[index], StringComparison.OrdinalIgnoreCase),
                $"Active package {index} must be {ExactPackageIds[index]}, not {active[index]}.");
        }

        foreach (var defName in RemovedOfficialMealDefs)
        {
            IntegrationAssert.Null(
                DefDatabase<ThingDef>.GetNamedSilentFail(defName),
                $"No Vanilla Meals must remove finalized official ThingDef {defName}.");
        }

        foreach (var recipeDefName in RemovedProductRecipes)
        {
            var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(recipeDefName);
            if (recipe is null)
            {
                continue;
            }

            IntegrationAssert.Null(
                MealClassificationRuntime.ClassifyRecipe(recipe),
                $"Removed-product recipe {recipeDefName} must not enter the runtime registry.");
            IntegrationAssert.Equal(
                1f,
                RecipeWorkRuntime.MultiplierFor(recipe),
                $"Removed-product recipe {recipeDefName} must not retain a work multiplier.");
        }

        var registeredMeals = DefDatabase<ThingDef>.AllDefsListForReading
            .Where(def => MealClassificationRuntime.ClassifyMeal(def).HasValue)
            .Select(def => def.defName)
            .ToArray();
        IntegrationAssert.Equal(
            0,
            registeredMeals.Length,
            "The no-replacement environment must not invent a fallback meal registry.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void DisabledRemovedProductBillCreatesNoKitchenwareAlert()
    {
        var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("CookMealSimple");
        if (recipe is null)
        {
            IntegrationAssert.True(
                !new Alert_MissingKitchenware().GetReport().AnyCulpritValid,
                "A map with no remaining meal recipe must not report missing kitchenware.");
            return;
        }

        var map = Find.CurrentMap;
        var created = new List<Thing>();
        var previousMode = ImmersiveChefsMod.Settings.WareRequirementMode;
        try
        {
            var center = map.AllCells.First(cell =>
                CellRect.CenteredOn(cell, 2).Cells.All(candidate =>
                    candidate.InBounds(map) &&
                    candidate.Standable(map) &&
                    candidate.GetFirstBuilding(map) is null));
            var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
            Pawn? cook = null;
            for (var attempt = 0; attempt < 64 && cook is null; attempt++)
            {
                var candidate = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    PawnKindDefOf.Colonist,
                    Faction.OfPlayer,
                    forceGenerateNewPawn: true,
                    canGeneratePawnRelations: false));
                if (candidate.WorkTypeIsDisabled(cooking))
                {
                    candidate.Destroy(DestroyMode.Vanish);
                    continue;
                }

                cook = candidate;
            }

            IntegrationAssert.NotNull(cook, "The alert fixture must generate a Cooking-capable colonist.");
            cook!.workSettings.EnableAndInitialize();
            cook.workSettings.SetPriority(cooking, 1);
            GenSpawn.Spawn(cook, center, map);
            created.Add(cook);

            var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
            var stove = ThingMaker.MakeThing(
                stoveDef,
                stoveDef.MadeFromStuff ? DefDatabase<ThingDef>.GetNamed("Steel") : null);
            stove.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(stove, center + IntVec3.East, map, Rot4.North);
            stove.TryGetComp<CompRefuelable>()?.Refuel(999f);
            created.Add(stove);
            ((IBillGiver)stove).BillStack.AddBill(new Bill_Production(recipe)
            {
                repeatMode = BillRepeatModeDefOf.RepeatCount,
                repeatCount = 1
            });

            ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Strict;
            IntegrationAssert.True(
                WorkGiver_PlateMeals.IsOperational((Building_WorkTable)stove),
                "The alert fixture stove must be operational.");
            IntegrationAssert.True(
                cook.CanReach(stove, PathEndMode.InteractionCell, Danger.Some),
                "The alert fixture cook must be able to reach the stove interaction cell.");
            IntegrationAssert.True(
                !MealCoveragePolicy.IsCovered(recipe),
                "The disabled stale-product recipe must not be considered covered on the playable map.");
            IntegrationAssert.True(
                !new Alert_MissingKitchenware().GetReport().AnyCulpritValid,
                "A reachable, fueled stove with only a removed-product bill must not report missing kitchenware.");
        }
        finally
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = previousMode;
            foreach (var thing in created.AsEnumerable().Reverse())
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }
}
