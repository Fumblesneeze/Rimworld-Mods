using RimWorldDevGateway.IntegrationTesting;

namespace ImmersiveChefs.MealTexture.InGame.IntegrationTests;

public static class DmtrFtvIntegrationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void EachUpstreamOwnerRetainsItsFinalizedMealFamily()
    {
        MealTextureDefAssertions.RequireActive(
            MealTextureDefAssertions.DmtrPackageId,
            MealTextureDefAssertions.FtvCorePackageId,
            MealTextureDefAssertions.FtvPackageId);
        MealTextureDefAssertions.AssertDmtrOwnsVanillaMeals();
        MealTextureDefAssertions.AssertFtvOwnsVarietyMeals();
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void HiddenPasteAndPhysicalPlateLifecycleRemainHonest()
    {
        MealTextureLifecycleAssertions.AssertHonestFallbackAndExactPlateLifecycle(
            "MealSimple",
            MealTextureDefAssertions.DmtrGraphicType);
        MealTextureLifecycleAssertions.AssertHonestFallbackAndExactPlateLifecycle(
            "FTV_MealSimple",
            MealTextureDefAssertions.FtvGraphicType);
    }
}
