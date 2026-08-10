using HarmonyLib;
using RimWorld;
using Verse;

namespace ImmersiveChefs;

public static class KitchenwareIngredientInfoPolicy
{
    public static FabricationTier? PreferredFabricationTier(
        KitchenwareProduct product,
        KitchenMaterialClassification classification,
        IEnumerable<FabricationTier> availableTiers)
    {
        if (classification is null)
        {
            throw new ArgumentNullException(nameof(classification));
        }

        if (availableTiers is null)
        {
            throw new ArgumentNullException(nameof(availableTiers));
        }

        var tiers = availableTiers.Distinct().ToArray();
        if (tiers.Contains(classification.FabricationTier) &&
            KitchenMaterialFabricationPolicy.Allows(
                product,
                classification.FabricationTier,
                classification))
        {
            return classification.FabricationTier;
        }

        foreach (var tier in tiers)
        {
            if (KitchenMaterialFabricationPolicy.Allows(product, tier, classification))
            {
                return tier;
            }
        }

        return null;
    }
}

[HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
internal static class KitchenwareIngredientInfoPatch
{
    private const int IngredientsDisplayPriority = 1102;

    private static void Postfix(
        ThingDef __instance,
        StatRequest req,
        ref IEnumerable<StatDrawEntry> __result)
    {
        if (!TryCreateReplacement(__instance, req, out var replacement))
        {
            return;
        }

        __result = ReplaceIngredientsEntry(__result, req, replacement);
    }

    private static bool TryCreateReplacement(
        ThingDef definition,
        StatRequest request,
        out ExactIngredientDisplay replacement)
    {
        replacement = default;
        if (!request.HasThing ||
            request.Thing is not { } thing ||
            thing.def != definition ||
            thing.Stuff is not { } stuff ||
            definition.GetModExtension<KitchenwareExtension>() is not { } kitchenware)
        {
            return false;
        }

        var classification = OptionalMaterialAdapter.CreateClassifier().Classify(
            Describe(stuff),
            kitchenware.product);
        if (classification is null)
        {
            return false;
        }

        var recipes = DefDatabase<RecipeDef>.AllDefsListForReading
            .Where(recipe =>
                recipe.products?.Any(product => product.thingDef == definition) == true &&
                recipe.GetModExtension<KitchenwareRecipeExtension>() is not null)
            .ToArray();
        var tier = KitchenwareIngredientInfoPolicy.PreferredFabricationTier(
            kitchenware.product,
            classification,
            recipes.Select(recipe => recipe.GetModExtension<KitchenwareRecipeExtension>()!.fabricationTier));
        if (tier is null)
        {
            return false;
        }

        var selectedRecipe = recipes.FirstOrDefault(recipe =>
            recipe.GetModExtension<KitchenwareRecipeExtension>()!.fabricationTier == tier.Value &&
            recipe.ingredients?.Any(ingredient => ingredient.filter.Allows(stuff)) == true);
        if (selectedRecipe?.IngredientValueGetter is null || selectedRecipe.ingredients is null)
        {
            return false;
        }

        var exactMaterialSlot = selectedRecipe.ingredients.FindIndex(ingredient => ingredient.filter.Allows(stuff));
        if (exactMaterialSlot < 0)
        {
            return false;
        }

        var requirements = selectedRecipe.ingredients
            .Select((ingredient, index) => index == exactMaterialSlot
                ? "ImmersiveChefs_IngredientRequirement".Translate(
                    ingredient.GetBaseCount(),
                    stuff.label).ToString()
                : selectedRecipe.IngredientValueGetter.BillRequirementsDescription(selectedRecipe, ingredient))
            .ToArray();
        var hyperlinks = selectedRecipe.ingredients
            .SelectMany((ingredient, index) => index == exactMaterialSlot
                ? new[] { stuff }
                : ingredient.filter.AllowedThingDefs)
            .Distinct()
            .ToArray();
        replacement = new ExactIngredientDisplay(
            string.Join(", ", requirements),
            hyperlinks);
        return true;
    }

    private static IEnumerable<StatDrawEntry> ReplaceIngredientsEntry(
        IEnumerable<StatDrawEntry> original,
        StatRequest request,
        ExactIngredientDisplay replacement)
    {
        var replaced = false;
        foreach (var entry in original)
        {
            if (!replaced && IsIngredientsEntry(entry))
            {
                replaced = true;
                yield return new StatDrawEntry(
                    entry.category,
                    entry.LabelCap,
                    replacement.Value,
                    entry.GetExplanationText(request),
                    entry.DisplayPriorityWithinCategory,
                    hyperlinks: Dialog_InfoCard.DefsToHyperlinks(replacement.Materials));
                continue;
            }

            yield return entry;
        }
    }

    private static bool IsIngredientsEntry(StatDrawEntry entry) =>
        entry.DisplayPriorityWithinCategory == IngredientsDisplayPriority &&
        string.Equals(
            entry.LabelCap,
            "Ingredients".Translate().CapitalizeFirst().ToString(),
            StringComparison.Ordinal);

    private static KitchenMaterialDescriptor Describe(ThingDef stuff)
    {
        var categories = stuff.stuffProps?.categories;
        return new KitchenMaterialDescriptor(
            stuff.defName,
            categories?.Any(category => category.defName.Equals("Metallic", StringComparison.OrdinalIgnoreCase)) == true,
            categories?.Any(category => category.defName.Equals("Woody", StringComparison.OrdinalIgnoreCase)) == true,
            categories?.Any(category => category.defName.Equals("Stony", StringComparison.OrdinalIgnoreCase)) == true);
    }

    private readonly struct ExactIngredientDisplay
    {
        internal ExactIngredientDisplay(string value, IReadOnlyList<ThingDef> materials)
        {
            Value = value;
            Materials = materials;
        }

        internal string Value { get; }

        internal IReadOnlyList<ThingDef> Materials { get; }
    }
}
