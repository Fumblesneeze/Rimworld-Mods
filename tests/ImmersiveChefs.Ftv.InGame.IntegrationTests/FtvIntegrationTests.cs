using RimWorldDevGateway.IntegrationTesting;

namespace ImmersiveChefs.MealTexture.InGame.IntegrationTests;

public static class FtvIntegrationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FtvOwnsVarietyGraphicsWhileImmersiveStateRemainsAttached()
    {
        var phase = "active package identity";
        try
        {
            MealTextureDefAssertions.RequireActive(
                MealTextureDefAssertions.FtvCorePackageId,
                MealTextureDefAssertions.FtvPackageId);
            MealTextureDefAssertions.RequireAbsent(MealTextureDefAssertions.DmtrPackageId);
            phase = "vanilla meal graphic ownership";
            MealTextureDefAssertions.AssertVanillaMealOwner();
            phase = "FTV meal graphic and component ownership";
            MealTextureDefAssertions.AssertFtvOwnsVarietyMeals();
        }
        catch (IntegrationTestAssertionException)
        {
            throw;
        }
        catch (System.Exception exception)
        {
            throw new IntegrationTestAssertionException(
                $"FTV loaded-Def phase '{phase}' failed with " +
                $"{exception.GetType().FullName}: {exception.Message}");
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void SelectedTextureGroupSurvivesTheRealScribePipeline()
    {
        FtvPersistenceAssertions.AssertSelectedTextureGroupSurvivesRealScribePipeline(
            "FTV_MealSimple");
    }
}
