using System;
using System.Linq;
using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace ImmersiveChefs.TextureVariations.InGame.IntegrationTests;

public static class TextureVariationFinalizedDefTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ExactVtexVefGroupInstallsOnlyThePortableSelectors()
    {
        var requiredPackages = new[]
        {
            "OskarPotocki.VanillaFactionsExpanded.Core",
            "VanillaExpanded.VTEXVariations"
        };
        foreach (var packageId in requiredPackages)
        {
            IntegrationAssert.True(
                LoadedModManager.RunningModsListForReading.Any(mod => string.Equals(
                    mod.PackageId,
                    packageId,
                    StringComparison.OrdinalIgnoreCase)),
                packageId + " must be an actually active ModContentPack for this exact group.");
        }

        var expectedDefNames = new[]
        {
            "ImmersiveChefs_Cookware",
            "ImmersiveChefs_Plate",
            "ImmersiveChefs_Cutlery",
            "ImmersiveChefs_ChefsKnife"
        };
        var selected = DefDatabase<ThingDef>.AllDefsListForReading
            .Where(def =>
                string.Equals(
                    def.modContentPack?.PackageId,
                    ImmersiveChefsMod.PackageId,
                    StringComparison.OrdinalIgnoreCase) &&
                def.graphicData?.graphicClass == typeof(Graphic_PortableKitchenwareVariation))
            .Select(def => def.defName)
            .OrderBy(defName => defName, StringComparer.Ordinal)
            .ToArray();

        IntegrationAssert.Equal(
            string.Join("|", expectedDefNames.OrderBy(value => value, StringComparer.Ordinal)),
            string.Join("|", selected),
            "The exact VTEX/VEF group must install the selector on exactly the four supported portable Defs.");
        foreach (var defName in expectedDefNames)
        {
            IntegrationAssert.Equal(
                typeof(Graphic_PortableKitchenwareVariation),
                DefDatabase<ThingDef>.GetNamed(defName).graphicData.graphicClass,
                defName + " must retain the finalized optional selector.");
        }

        IntegrationAssert.Equal(
            typeof(Graphic_Single),
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_AdobePlate").graphicData.graphicClass,
            "The fixed adobe plate must not receive a Stuff/material selector.");
        IntegrationAssert.Equal(
            typeof(Graphic_Single),
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_GlitterworldCookware").graphicData.graphicClass,
            "Fixed-color glitterworld cookware must not receive the portable selector.");
    }
}
