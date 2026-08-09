using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ImmersiveChefs;

public sealed class StockGenerator_Kitchenware : StockGenerator
{
    public ThingDef thingDef = null!;
    public KitchenwareProduct product;
    public List<FabricationTier> fabricationTiers = new();

    public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null!)
    {
        var count = RandomCountOf(thingDef);
        if (count <= 0)
        {
            yield break;
        }

        var eligibleStuffs = EligibleStuffs().ToArray();
        if (eligibleStuffs.Length == 0)
        {
            yield break;
        }

        var stuff = eligibleStuffs.RandomElementByWeight(candidate =>
            Math.Max(0.0001f, candidate.stuffProps?.commonality ?? 0f));
        while (count > 0)
        {
            var item = ThingMaker.MakeThing(thingDef, stuff);
            item.stackCount = Math.Min(count, item.def.stackLimit);
            count -= item.stackCount;
            yield return item;
        }
    }

    public override bool HandlesThingDef(ThingDef candidate)
    {
        return candidate == thingDef;
    }

    public override IEnumerable<string> ConfigErrors(TraderKindDef parentDef)
    {
        foreach (var error in base.ConfigErrors(parentDef))
        {
            yield return error;
        }

        if (thingDef is null)
        {
            yield return "thingDef is required";
        }
        else if (!thingDef.MadeFromStuff)
        {
            yield return $"{thingDef.defName} must be Stuff-aware";
        }

        if (fabricationTiers is null || fabricationTiers.Count == 0)
        {
            yield return "at least one fabrication tier is required";
        }
    }

    internal IEnumerable<ThingDef> EligibleStuffs()
    {
        if (thingDef is null || fabricationTiers is null || fabricationTiers.Count == 0)
        {
            return Enumerable.Empty<ThingDef>();
        }

        var classifier = OptionalMaterialAdapter.CreateClassifier();
        return GenStuff.AllowedStuffsFor(
                thingDef,
                maxTechLevelGenerate,
                checkAllowedInStuffGeneration: true)
            .Where(stuff =>
            {
                var categories = stuff.stuffProps?.categories;
                var classification = classifier.Classify(
                    new KitchenMaterialDescriptor(
                        stuff.defName,
                        categories?.Any(category => category.defName.Equals(
                            "Metallic",
                            StringComparison.OrdinalIgnoreCase)) == true,
                        categories?.Any(category => category.defName.Equals(
                            "Woody",
                            StringComparison.OrdinalIgnoreCase)) == true,
                        categories?.Any(category => category.defName.Equals(
                            "Stony",
                            StringComparison.OrdinalIgnoreCase)) == true),
                    product);
                return classification is not null &&
                       fabricationTiers.Any(tier =>
                           KitchenMaterialFabricationPolicy.Allows(product, tier, classification));
            });
    }
}
