using Verse;

namespace ImmersiveChefs;

internal static class PlateMaterialEligibilityRuntime
{
    internal static bool Allows(Thing thing, MealComplexity? complexity)
    {
        if (thing.def.GetModExtension<KitchenwareExtension>() is not { product: KitchenwareProduct.Plate } extension)
        {
            return false;
        }

        var material = extension.fixedMaterialKind ?? ResolveStuff(thing.Stuff);
        return material.HasValue && PlateMaterialEligibilityPolicy.Allows(complexity, material.Value);
    }

    private static KitchenMaterialKind? ResolveStuff(ThingDef? stuff)
    {
        if (stuff?.stuffProps is null)
        {
            return null;
        }

        var categories = stuff.stuffProps.categories;
        var descriptor = new KitchenMaterialDescriptor(
            stuff.defName,
            categories?.Any(category => category.defName.Equals("Metallic", StringComparison.OrdinalIgnoreCase)) == true,
            categories?.Any(category => category.defName.Equals("Woody", StringComparison.OrdinalIgnoreCase)) == true,
            categories?.Any(category => category.defName.Equals("Stony", StringComparison.OrdinalIgnoreCase)) == true);
        return OptionalMaterialAdapter.CreateClassifier()
            .Classify(descriptor, KitchenwareProduct.Plate)?.Kind;
    }
}
