using RimWorldDevGateway.IntegrationTesting;

namespace ImmersiveChefs.MealTexture.InGame.IntegrationTests;

public static class MealTextureAbsentIntegrationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void VanillaGraphicAndImmersiveStateCoexistWithoutOptionalOwners()
    {
        MealTextureDefAssertions.RequireAbsent(
            MealTextureDefAssertions.DmtrPackageId,
            MealTextureDefAssertions.FtvCorePackageId,
            MealTextureDefAssertions.FtvPackageId);
        MealTextureDefAssertions.AssertVanillaMealOwner();
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void HiddenPasteAndPhysicalPlateLifecycleRemainHonest()
    {
        MealTextureLifecycleAssertions.AssertHonestFallbackAndExactPlateLifecycle(
            "MealSimple",
            typeof(Verse.Graphic_MealVariants).FullName!);
    }
}
