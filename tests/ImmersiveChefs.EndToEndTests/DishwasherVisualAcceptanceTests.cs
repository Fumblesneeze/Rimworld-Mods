using RimWorldDevGateway.EndToEndTesting;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.dishwasher-visual-acceptance",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "OskarPotocki.VanillaFactionsExpanded.Core",
    "VanillaExpanded.VTEXVariations",
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_800,
    MaxGameTicks = 8_000,
    MaxWallClockSeconds = 240)]
public sealed class DishwasherVisualAcceptanceTest : WorkstationVisualAcceptanceScenario
{
    public DishwasherVisualAcceptanceTest() : base("Dishwasher", "Dishwasher") { }
}

[RimWorldEndToEndTest(
    "immersive-chefs.industrial-dishwasher-visual-acceptance",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "OskarPotocki.VanillaFactionsExpanded.Core",
    "VanillaExpanded.VTEXVariations",
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_800,
    MaxGameTicks = 8_000,
    MaxWallClockSeconds = 240)]
public sealed class IndustrialDishwasherVisualAcceptanceTest : WorkstationVisualAcceptanceScenario
{
    public IndustrialDishwasherVisualAcceptanceTest() : base("IndustrialDishwasher", "Dishwasher") { }
}
