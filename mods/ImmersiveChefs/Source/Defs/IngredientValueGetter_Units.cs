using RimWorld;
using Verse;

namespace ImmersiveChefs;

public sealed class IngredientValueGetter_Units : IngredientValueGetter
{
    public override float ValuePerUnitOf(ThingDef thingDef)
    {
        return 1f;
    }

    public override string BillRequirementsDescription(RecipeDef recipe, IngredientCount ingredient)
    {
        var index = recipe.ingredients?.IndexOf(ingredient) ?? -1;
        if (index > 0)
        {
            return "ImmersiveChefs_IngredientRequirement".Translate(
                ingredient.GetBaseCount(),
                "ImmersiveChefs_Ingredient_Wood".Translate());
        }

        var extension = recipe.GetModExtension<KitchenwareRecipeExtension>();
        var materialLabel = extension?.product == KitchenwareProduct.ChefsKnife
            ? "ImmersiveChefs_Ingredient_AnyEligibleMetal".Translate()
            : extension?.fabricationTier switch
        {
            FabricationTier.PrimitiveStone => "ImmersiveChefs_Ingredient_AnyStonyMaterial".Translate(),
            FabricationTier.Soft => "ImmersiveChefs_Ingredient_WoodOrSoftMaterial".Translate(),
            FabricationTier.Intermediate => "ImmersiveChefs_Ingredient_AnyIntermediateMetal".Translate(),
            FabricationTier.Modern when extension.product is KitchenwareProduct.Plate or KitchenwareProduct.Cutlery =>
                "ImmersiveChefs_Ingredient_AnyMetalOrPlastic".Translate(),
            FabricationTier.Modern => "ImmersiveChefs_Ingredient_AnyModernMetal".Translate(),
            _ => ingredient.filter.Summary
        };
        return "ImmersiveChefs_IngredientRequirement".Translate(ingredient.GetBaseCount(), materialLabel);
    }
}
