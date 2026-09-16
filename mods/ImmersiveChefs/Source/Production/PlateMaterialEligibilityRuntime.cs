using Verse;

namespace ImmersiveChefs;

internal static class PlateMaterialEligibilityRuntime
{
    internal static bool Allows(Thing thing, MealComplexity? complexity) => CountEligible(thing, complexity) > 0;

    internal static int CountEligible(Thing thing, MealComplexity? complexity)
    {
        if (thing.def.GetModExtension<KitchenwareExtension>() is not { product: KitchenwareProduct.Plate } extension)
        {
            return 0;
        }

        if (CompTablewareStack.For(thing) is { } pile)
            return pile.CountWhere(unit => AllowsMaterial(extension, pile.ResolveMaterial(unit), complexity));
        return AllowsMaterial(extension, thing.Stuff, complexity) ? thing.stackCount : 0;
    }

    internal static bool PreparePickup(Thing thing, MealComplexity? complexity, int count)
    {
        if (count <= 0 || CountEligible(thing, complexity) < count) return false;
        if (CompTablewareStack.For(thing) is not { } pile) return true;
        var extension = thing.def.GetModExtension<KitchenwareExtension>();
        return pile.Prioritize(unit => AllowsMaterial(extension, pile.ResolveMaterial(unit), complexity), count);
    }

    private static bool AllowsMaterial(KitchenwareExtension extension, ThingDef? stuff, MealComplexity? complexity)
    {
        var material = extension.fixedMaterialKind ?? ResolveStuff(stuff);
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
