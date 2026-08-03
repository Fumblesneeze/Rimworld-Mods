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
        return ingredient.GetBaseCount() + "x " + ingredient.filter.Summary;
    }
}
