using System;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace ImmersiveChefs.CeramicsContinued.InGame.IntegrationTests;

public static class CeramicsContinuedIntegrationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedPorcelainRecipeHonorsRestartSettingWithoutFrameworkDependencies()
    {
        try
        {
            AssertFinalizedPorcelainRecipe();
        }
        catch (IntegrationTestAssertionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            IntegrationAssert.Fail(
                "Unexpected Ceramics integration exception: " + exception.GetType().FullName + "; " +
                exception.Message + "; " + exception.StackTrace);
        }
    }

    private static void AssertFinalizedPorcelainRecipe()
    {
        var activePackageIds = LoadedModManager.RunningModsListForReading
            .Select(mod => mod.PackageId)
            .ToArray();
        IntegrationAssert.True(
            activePackageIds.Any(packageId => string.Equals(packageId, "zal.ceramics", StringComparison.OrdinalIgnoreCase)),
            "Ceramics (Continued) must be an actually active ModContentPack for this exact group.");
        IntegrationAssert.False(
            activePackageIds.Any(packageId => string.Equals(packageId, "syrchalis.processor.framework", StringComparison.OrdinalIgnoreCase)),
            "Processor Framework must remain absent from the minimum supported Ceramics group.");
        IntegrationAssert.False(
            activePackageIds.Any(packageId => string.Equals(packageId, "oskarpotocki.vanillafactionsexpanded.core", StringComparison.OrdinalIgnoreCase)),
            "Vanilla Expanded Framework must remain absent from the minimum supported Ceramics group.");

        var porcelain = DefDatabase<ThingDef>.GetNamed("N7_Porcelain");
        var plate = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var recipe = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakePorcelainPlates");
        var primitiveRecipe = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakePrimitivePlates");
        var basicBench = DefDatabase<ThingDef>.GetNamed("CeramicsBench_Basic");
        var electricBench = DefDatabase<ThingDef>.GetNamed("CeramicsBench_Electric");
        var research = DefDatabase<ResearchProjectDef>.GetNamed("BasicCeramics");
        IntegrationAssert.NotNull(recipe.ingredients, "The finalized porcelain recipe must retain its ingredient list.");
        IntegrationAssert.Equal(1, recipe.ingredients.Count, "The finalized porcelain recipe must have exactly one ingredient.");
        IntegrationAssert.NotNull(recipe.ingredients[0].filter, "The finalized porcelain ingredient must retain its filter.");
        IntegrationAssert.NotNull(primitiveRecipe.ingredients, "The primitive recipe must retain its ingredient list.");
        IntegrationAssert.Equal(1, primitiveRecipe.ingredients.Count, "The primitive plate recipe must have exactly one ingredient.");
        IntegrationAssert.NotNull(primitiveRecipe.ingredients[0].filter, "The primitive plate ingredient must retain its filter.");
        IntegrationAssert.NotNull(recipe.products, "The finalized porcelain recipe must retain its products.");
        IntegrationAssert.Equal(1, recipe.products.Count, "The finalized porcelain recipe must have exactly one product.");
        IntegrationAssert.NotNull(recipe.recipeUsers, "The finalized porcelain recipe must retain a recipe-user list.");
        var ingredient = recipe.ingredients[0];
        var primitiveIngredient = primitiveRecipe.ingredients[0];
        var product = recipe.products[0];

        IntegrationAssert.True(porcelain.IsStuff, "N7_Porcelain must remain a real Stuff Def.");
        IntegrationAssert.False(
            primitiveIngredient.filter.Allows(porcelain),
            "The Stony tag on porcelain must never leak into the primitive plate recipe.");
        var descriptor = new KitchenMaterialDescriptor(
            porcelain.defName,
            isMetallic: porcelain.stuffProps.categories.Any(category => category.defName == "Metallic"),
            isWoody: porcelain.stuffProps.categories.Any(category => category.defName == "Woody"),
            isStony: porcelain.stuffProps.categories.Any(category => category.defName == "Stony"));
        var classification = OptionalMaterialAdapter.CreateClassifier().Classify(descriptor, KitchenwareProduct.Plate);

        if (ImmersiveChefsMod.Settings.CeramicsContinued == OptionalIntegrationMode.Off)
        {
            IntegrationAssert.Equal(0, recipe.recipeUsers.Count, "Off must clear the recipe's users after restart.");
            IntegrationAssert.False(basicBench.AllRecipes.Contains(recipe), "Off must remove the bill from the basic bench.");
            IntegrationAssert.False(electricBench.AllRecipes.Contains(recipe), "Off must remove the bill from the electric bench.");
            IntegrationAssert.Null(classification, "Off must remove porcelain material admission.");

            return;
        }

        IntegrationAssert.Equal(
            OptionalIntegrationMode.Auto,
            ImmersiveChefsMod.Settings.CeramicsContinued,
            "The minimum enabled group must use the default Auto setting.");
        IntegrationAssert.True(
            ingredient.filter.Allows(porcelain),
            "The finalized optional recipe must accept N7_Porcelain.");
        IntegrationAssert.False(
            ingredient.filter.Allows(ThingDefOf.Steel),
            "The porcelain bill must not become a generic metal plate recipe.");
        IntegrationAssert.Equal(4f, ingredient.GetBaseCount(), "One bill must consume four porcelain units.");
        IntegrationAssert.Equal(plate, product.thingDef, "The optional bill must produce the shared plate ThingDef.");
        IntegrationAssert.Equal(4, product.count, "One bill must produce four plates.");
        IntegrationAssert.Equal(research, recipe.researchPrerequisite, "BasicCeramics must own progression.");
        IntegrationAssert.True(recipe.recipeUsers.Contains(basicBench), "The basic ceramics bench must host the bill.");
        IntegrationAssert.True(recipe.recipeUsers.Contains(electricBench), "The electric ceramics bench must host the bill.");
        IntegrationAssert.NotNull(classification, "Auto must explicitly admit N7_Porcelain for plates.");
        var activeClassification = classification!;
        IntegrationAssert.Equal(KitchenMaterialKind.Ceramic, activeClassification.Kind, "Porcelain must classify as ceramic.");
        IntegrationAssert.Equal(FabricationTier.Ceramic, activeClassification.FabricationTier, "Porcelain must use the ceramic tier.");
        var stats = KitchenwareStatCalculator.Calculate(activeClassification.Kind, KitchenwareProduct.Plate, QualityCategory.Normal);
        IntegrationAssert.Equal(75f, stats.MaterialCleanliness, "Porcelain plates must use the ceramic profile, not primitive stone.");
        IntegrationAssert.Equal(0.95f, stats.CookingSpeedFactor, "Porcelain plates must use the ceramic eating-speed profile.");
    }
}
