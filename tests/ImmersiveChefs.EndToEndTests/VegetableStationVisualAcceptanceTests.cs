using RimWorldDevGateway.EndToEndTesting;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.vegetable-station-visual-acceptance",
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
public sealed class VegetableStationVisualAcceptanceTest : WorkstationVisualAcceptanceScenario
{
    public VegetableStationVisualAcceptanceTest() : base("VegetableStation") { }
}
