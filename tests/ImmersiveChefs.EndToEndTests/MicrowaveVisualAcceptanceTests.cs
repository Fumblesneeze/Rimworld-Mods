using RimWorldDevGateway.EndToEndTesting;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.microwave-table-visual-acceptance",
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
public sealed class MicrowaveTableVisualAcceptanceTest : WorkstationVisualAcceptanceScenario
{
    public MicrowaveTableVisualAcceptanceTest() : base("Microwave", "Appliance", "Table1x2c") { }
}

[RimWorldEndToEndTest(
    "immersive-chefs.microwave-workbench-visual-acceptance",
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
public sealed class MicrowaveWorkbenchVisualAcceptanceTest : WorkstationVisualAcceptanceScenario
{
    public MicrowaveWorkbenchVisualAcceptanceTest() : base("Microwave", "Appliance", "TableMachining") { }
}
