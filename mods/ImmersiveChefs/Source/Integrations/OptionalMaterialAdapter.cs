using HarmonyLib;
using Verse;

namespace ImmersiveChefs;

internal static class OptionalMaterialAdapter
{
    private static readonly System.Reflection.FieldInfo CachedLabelCapField =
        AccessTools.Field(typeof(Def), "cachedLabelCap") ??
        throw new MissingFieldException(typeof(Def).FullName, "cachedLabelCap");

    private static readonly string[] ExpandedMetalDefs =
    {
        "EM_Iron",
        "EM_MildSteel",
        "EM_TemperedSteel",
        "EM_Lead",
        "EM_Bronze",
        "EM_Copper",
        "EM_StainlessSteel",
        "EM_Titanium"
    };

    internal static KitchenMaterialClassifier CreateClassifier()
    {
        var classifier = KitchenMaterialClassifier.CreateDefault();
        if (!ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.ExpandedMaterialsMetals))
        {
            foreach (var defName in ExpandedMetalDefs)
            {
                classifier.Exclude(defName);
            }
        }

        if (!ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.AbsPolymer))
        {
            classifier.Exclude("ABSPolymer");
        }

        return classifier;
    }

    internal static void ValidateAndConfigure()
    {
        if (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.ExpandedMaterialsMetals))
        {
            var missing = ExpandedMetalDefs
                .Where(defName => DefDatabase<ThingDef>.GetNamedSilentFail(defName) is null)
                .ToList();
            if (missing.Count > 0)
            {
                OptionalIntegrationDiagnostics.WarnOnce(
                    OptionalIntegration.ExpandedMaterialsMetals,
                    "required Stuff Defs are missing: " + string.Join(", ", missing));
            }
        }

        if (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.AbsPolymer) &&
            DefDatabase<ThingDef>.GetNamedSilentFail("ABSPolymer") is null)
        {
            OptionalIntegrationDiagnostics.WarnOnce(
                OptionalIntegration.AbsPolymer,
                "expected Stuff Def ABSPolymer is missing");
        }

        var adobeRecipe = DefDatabase<RecipeDef>.GetNamedSilentFail("ImmersiveChefs_MakeAdobePlates");
        if (adobeRecipe is not null)
        {
            adobeRecipe.label = "ImmersiveChefs_Recipe_MakeAdobePlates_Label".Translate();
            CachedLabelCapField.SetValue(adobeRecipe, default(TaggedString));
            adobeRecipe.description = "ImmersiveChefs_Recipe_MakeAdobePlates_Description".Translate();
            adobeRecipe.jobString = "ImmersiveChefs_Recipe_MakeAdobePlates_JobString".Translate();
        }
        if (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.ExpandedMaterialsMasonry))
        {
            if (DefDatabase<ThingDef>.GetNamedSilentFail("EM_AdobeBricks") is null || adobeRecipe is null)
            {
                OptionalIntegrationDiagnostics.WarnOnce(
                    OptionalIntegration.ExpandedMaterialsMasonry,
                    "expected EM_AdobeBricks or its fixed adobe-plate recipe is missing");
            }

            return;
        }

        if (adobeRecipe is null)
        {
            return;
        }

        adobeRecipe.recipeUsers?.Clear();
        foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            def.recipes?.Remove(adobeRecipe);
            AccessTools.Field(typeof(ThingDef), "allRecipesCached")?.SetValue(def, null);
        }
    }
}
