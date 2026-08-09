using RimWorldDevGateway.IntegrationTesting;

namespace ImmersiveChefs.MealTexture.InGame.IntegrationTests;

public static class VisualProvenanceIntegrationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FtvVceAddonsPreserveFinalGraphicOwnersAndImmersiveState()
    {
        MealTextureDefAssertions.AssertFtvOwnsVceAddonMeals();
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void FtvVceStewPreservesItsSelectedTextureAndPhysicalStateThroughScribe()
    {
        FtvPersistenceAssertions.AssertSelectedTextureGroupSurvivesRealScribePipeline(
            "VCE_CookedStewSimple");
    }
}
