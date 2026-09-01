using System.Reflection;
using System.Linq;
using RimWorld;
using Verse;

namespace ImmersiveChefs;

internal static class OptionalMaterialAdapter
{
    private static readonly System.Reflection.FieldInfo CachedLabelCapField =
        typeof(Def).GetField("cachedLabelCap", BindingFlags.Instance | BindingFlags.NonPublic) ??
        throw new MissingFieldException(typeof(Def).FullName, "cachedLabelCap");

    private static readonly FieldInfo? AllRecipesCachedField =
        typeof(ThingDef).GetField("allRecipesCached", BindingFlags.Instance | BindingFlags.NonPublic);

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

    private const string PorcelainDefName = "N7_Porcelain";
    private const string PorcelainRecipeDefName = "ImmersiveChefs_MakePorcelainPlates";
    private const string PlateDefName = "ImmersiveChefs_Plate";
    private const string BasicCeramicsBenchDefName = "CeramicsBench_Basic";
    private const string ElectricCeramicsBenchDefName = "CeramicsBench_Electric";
    private const string BasicCeramicsResearchDefName = "BasicCeramics";

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

        if (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.CeramicsContinued) &&
            HasSupportedCeramicsShape(out _))
        {
            classifier.Register(PorcelainDefName, KitchenMaterialKind.Ceramic);
        }
        else
        {
            // Ceramics (Continued) deliberately tags porcelain as Stony. Exclusion is required
            // when the adapter is absent, disabled, or incompatible so that broad stone fallback
            // cannot leak porcelain into primitive cookware or plate recipes.
            classifier.Exclude(PorcelainDefName);
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

        var porcelainRecipe = DefDatabase<RecipeDef>.GetNamedSilentFail(PorcelainRecipeDefName);
        if (porcelainRecipe is not null)
        {
            porcelainRecipe.label = "ImmersiveChefs_Recipe_MakePorcelainPlates_Label".Translate();
            CachedLabelCapField.SetValue(porcelainRecipe, default(TaggedString));
            porcelainRecipe.description = "ImmersiveChefs_Recipe_MakePorcelainPlates_Description".Translate();
            porcelainRecipe.jobString = "ImmersiveChefs_Recipe_MakePorcelainPlates_JobString".Translate();
        }
        if (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.CeramicsContinued))
        {
            if (!HasSupportedCeramicsShape(out var ceramicsReason))
            {
                OptionalIntegrationDiagnostics.WarnOnce(
                    OptionalIntegration.CeramicsContinued,
                    ceramicsReason);
                DisableRecipe(porcelainRecipe);
            }
        }
        else
        {
            DisableRecipe(porcelainRecipe);
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

        }
        else
        {
            DisableRecipe(adobeRecipe);
        }
    }

    private static void DisableRecipe(RecipeDef? recipe)
    {
        if (recipe is null)
        {
            return;
        }

        recipe.recipeUsers?.Clear();
        foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            def.recipes?.Remove(recipe);
            AllRecipesCachedField?.SetValue(def, null);
        }
    }

    private static bool HasSupportedCeramicsShape(out string reason)
    {
        var porcelain = DefDatabase<ThingDef>.GetNamedSilentFail(PorcelainDefName);
        if (porcelain?.stuffProps is null)
        {
            reason = $"expected porcelain Stuff Def {PorcelainDefName} is missing or is not Stuff";
            return false;
        }

        var categories = porcelain.stuffProps.categories;
        if (categories is null ||
            categories.All(category => !string.Equals(category.defName, "Stony", StringComparison.OrdinalIgnoreCase)))
        {
            reason = $"expected porcelain Stuff Def {PorcelainDefName} no longer carries its supported Stony category";
            return false;
        }

        var basicBench = DefDatabase<ThingDef>.GetNamedSilentFail(BasicCeramicsBenchDefName);
        var electricBench = DefDatabase<ThingDef>.GetNamedSilentFail(ElectricCeramicsBenchDefName);
        if (!IsWorkTable(basicBench) || !IsWorkTable(electricBench))
        {
            reason = "expected ceramics benches CeramicsBench_Basic and CeramicsBench_Electric are missing or are not worktables";
            return false;
        }

        var research = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(BasicCeramicsResearchDefName);
        var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(PorcelainRecipeDefName);
        if (research is null || recipe is null)
        {
            reason = "expected BasicCeramics research or the package-gated porcelain-plate recipe is missing";
            return false;
        }

        var recipeUsers = recipe.recipeUsers;
        if (recipeUsers is null ||
            recipeUsers.Count != 2 ||
            !recipeUsers.Contains(basicBench!) ||
            !recipeUsers.Contains(electricBench!) ||
            recipe.researchPrerequisite != research)
        {
            reason = "porcelain-plate recipe no longer uses both supported ceramics benches and BasicCeramics";
            return false;
        }

        var extension = recipe.GetModExtension<KitchenwareRecipeExtension>();
        var ingredient = recipe.ingredients is { Count: 1 } ? recipe.ingredients[0] : null;
        var product = recipe.products is { Count: 1 } ? recipe.products[0] : null;
        if (extension is null ||
            extension.product != KitchenwareProduct.Plate ||
            extension.fabricationTier != FabricationTier.Ceramic ||
            ingredient is null ||
            ingredient.GetBaseCount() != 4f ||
            !ingredient.filter.Allows(porcelain) ||
            ingredient.filter.AllowedThingDefs.Any(def => def != porcelain) ||
            recipe.fixedIngredientFilter is null ||
            !recipe.fixedIngredientFilter.Allows(porcelain) ||
            recipe.fixedIngredientFilter.AllowedThingDefs.Any(def => def != porcelain) ||
            !recipe.productHasIngredientStuff ||
            product is null ||
            product.thingDef?.defName != PlateDefName ||
            product.count != 4)
        {
            reason = "porcelain-plate recipe no longer has the supported plate-only four-output shape";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool IsWorkTable(ThingDef? def)
    {
        return def?.thingClass is not null && typeof(Building_WorkTable).IsAssignableFrom(def.thingClass);
    }
}
