using System;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace ImmersiveChefs.ExpandedMaterialsMasonry.InGame.IntegrationTests;

public static class ExpandedMaterialsMasonryLocalizationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ConditionalAdobeRecipeUsesTheActiveLocalizedRuntimeFieldsAndLabelCache()
    {
        var requestedLanguage = Environment.GetEnvironmentVariable("RIMWORLD_E2E_EXPECTED_LANGUAGE") ?? string.Empty;
        IntegrationAssert.Equal(
            "German",
            requestedLanguage,
            "This optional localization matrix must be launched explicitly in German so English source text cannot pass vacuously.");
        IntegrationAssert.Equal(
            requestedLanguage,
            LanguageDatabase.activeLanguage.folderName,
            "RimWorld must activate the exact non-English locale requested by the isolated launcher.");
        IntegrationAssert.True(
            LoadedModManager.RunningModsListForReading.Any(mod => string.Equals(
                mod.PackageId,
                "argon.expandedmaterials.masonry",
                StringComparison.OrdinalIgnoreCase)),
            "Expanded Materials - Masonry must be an actually active ModContentPack for this exact group.");

        var recipe = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakeAdobePlates");
        var expectedLabel = "ImmersiveChefs_Recipe_MakeAdobePlates_Label".Translate().CapitalizeFirst();
        IntegrationAssert.Equal(
            expectedLabel.ToString(),
            recipe.LabelCap.ToString(),
            "The finalized conditional recipe LabelCap must use the active locale rather than a cached English label.");
        IntegrationAssert.Equal(
            "ImmersiveChefs_Recipe_MakeAdobePlates_Description".Translate().ToString(),
            recipe.description,
            "The finalized conditional recipe description must use the active locale.");
        IntegrationAssert.Equal(
            "ImmersiveChefs_Recipe_MakeAdobePlates_JobString".Translate().ToString(),
            recipe.jobString,
            "The finalized conditional recipe job string must use the active locale.");
    }
}
