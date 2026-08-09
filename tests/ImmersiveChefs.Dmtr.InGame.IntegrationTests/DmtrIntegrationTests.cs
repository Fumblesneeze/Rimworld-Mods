using RimWorldDevGateway.IntegrationTesting;

namespace ImmersiveChefs.MealTexture.InGame.IntegrationTests;

public static class DmtrIntegrationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void DmtrOwnsVanillaGraphicsWhileImmersiveStateRemainsAttached()
    {
        MealTextureDefAssertions.RequireActive(MealTextureDefAssertions.DmtrPackageId);
        MealTextureDefAssertions.RequireAbsent(
            MealTextureDefAssertions.FtvCorePackageId,
            MealTextureDefAssertions.FtvPackageId);
        MealTextureDefAssertions.AssertDmtrOwnsVanillaMeals();
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void HiddenPasteAndPhysicalPlateLifecycleRemainHonest()
    {
        MealTextureLifecycleAssertions.AssertHonestFallbackAndExactPlateLifecycle(
            "MealSimple",
            MealTextureDefAssertions.DmtrGraphicType);
    }
}
