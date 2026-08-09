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
            return ingredient.GetBaseCount() + "x wood";
        }

        var extension = recipe.GetModExtension<KitchenwareRecipeExtension>();
        var materialLabel = extension?.product == KitchenwareProduct.ChefsKnife
            ? "any eligible metal"
            : extension?.fabricationTier switch
        {
            FabricationTier.PrimitiveStone => "any stony material",
            FabricationTier.Soft => "wood or soft material",
            FabricationTier.Intermediate => "any intermediate metal",
            FabricationTier.Modern when extension.product is KitchenwareProduct.Plate or KitchenwareProduct.Cutlery =>
                "any eligible metal or plastic",
            FabricationTier.Modern => "any modern metal",
            _ => ingredient.filter.Summary
        };
        return ingredient.GetBaseCount() + "x " + materialLabel;
    }
}
