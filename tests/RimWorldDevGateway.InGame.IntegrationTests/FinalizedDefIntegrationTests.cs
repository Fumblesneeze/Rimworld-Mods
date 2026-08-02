using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace RimWorldDevGateway.InGame.IntegrationTests;

public static class FinalizedDefIntegrationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void CoreSteelDefExistsInFinalizedDatabase()
    {
        IntegrationAssert.NotNull(
            DefDatabase<ThingDef>.GetNamedSilentFail("Steel"),
            "Core Steel must resolve from RimWorld's finalized ThingDef database.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void GatewayProbeContainsItsFinalXmlPatch()
    {
        var steel = DefDatabase<ThingDef>.GetNamedSilentFail("Steel");
        var probe = steel?.GetModExtension<GatewayIntegrationProbeExtension>();

        IntegrationAssert.NotNull(probe, "Core Steel must contain the Gateway's patched mod extension.");
        IntegrationAssert.Equal(
            "patched-by-gateway-xml",
            probe!.marker,
            "The finalized probe must contain the PatchOperation result");
    }
}

public static class PlayableMapIntegrationTests
{
    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void A_DeliberateFailureProbeIsStartupControlled()
    {
        IntegrationAssert.NotNull(Find.CurrentMap, "PlayableMapLoaded must expose a real current map.");
        if (GenCommandLine.CommandLineArgPassed("devGatewayForceIntegrationTestFailure"))
        {
            IntegrationAssert.Fail("Deliberate startup-controlled integration failure probe.");
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void B_LaterTestStillRunsAfterTheFailureProbe()
    {
        IntegrationAssert.Equal(
            ProgramState.Playing,
            Current.ProgramState,
            "A later integration test must still observe the playable game lifecycle");
        IntegrationAssert.NotNull(
            Find.CurrentMap,
            "The Gateway must continue the lifecycle suite with the current map available.");
    }
}
