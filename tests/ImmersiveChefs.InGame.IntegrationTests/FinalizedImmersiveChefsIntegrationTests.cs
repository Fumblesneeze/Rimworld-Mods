using System;
using System.Linq;
using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace ImmersiveChefs.InGame.IntegrationTests;

public static class FinalizedImmersiveChefsIntegrationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ImmersiveChefsModInitializedFromTheRealActiveSet()
    {
        IntegrationAssert.True(
            LoadedModManager.RunningModsListForReading.Any(
                mod => string.Equals(
                    mod.PackageId,
                    ImmersiveChefsMod.PackageId,
                    StringComparison.OrdinalIgnoreCase)),
            "Immersive Chefs must be an actually loaded ModContentPack, not merely a referenced assembly.");
        IntegrationAssert.NotNull(
            ImmersiveChefsMod.Integrations,
            "The real Immersive Chefs Mod constructor must initialize its optional-integration snapshot.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ImmersiveChefsXmlProbeContainsItsFinalPatch()
    {
        var steel = DefDatabase<ThingDef>.GetNamedSilentFail("Steel");
        var probe = steel?.GetModExtension<ImmersiveChefsIntegrationProbeExtension>();

        IntegrationAssert.NotNull(
            probe,
            "Finalized Core Steel must contain the Immersive Chefs XML-patched mod extension.");
        IntegrationAssert.Equal(
            "patched-by-immersive-chefs-xml",
            probe!.marker,
            "The finalized probe must contain the exact Immersive Chefs PatchOperation result.");
    }
}
