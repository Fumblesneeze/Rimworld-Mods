using RimWorldDevGateway.EndToEndTesting;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.dishwasher-ground-shadows",
    "fumblesneeze.immersivechefs", "brrainz.harmony", EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions", "fumblesneeze.immersivechefs",
    MaxFrames = 4800, MaxGameTicks = 8000, MaxWallClockSeconds = 240)]
public sealed class DishwasherGroundShadowTest : WorkstationVisualAcceptanceScenario
{
    public DishwasherGroundShadowTest() : base("Dishwasher", "Dishwasher", groundShadowsOnly: true) { }
}

[RimWorldEndToEndTest(
    "immersive-chefs.industrial-dishwasher-ground-shadows",
    "fumblesneeze.immersivechefs", "brrainz.harmony", EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions", "fumblesneeze.immersivechefs",
    MaxFrames = 4800, MaxGameTicks = 8000, MaxWallClockSeconds = 240)]
public sealed class IndustrialDishwasherGroundShadowTest : WorkstationVisualAcceptanceScenario
{
    public IndustrialDishwasherGroundShadowTest()
        : base("IndustrialDishwasher", "Dishwasher", groundShadowsOnly: true) { }
}
