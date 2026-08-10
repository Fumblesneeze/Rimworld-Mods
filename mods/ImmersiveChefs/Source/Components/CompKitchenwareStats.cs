using HarmonyLib;
using RimWorld;
using Verse;

namespace ImmersiveChefs;

public static class GlitterworldQualityPolicy
{
    public static QualityCategory Clamp(QualityCategory quality) =>
        quality < QualityCategory.Good ? QualityCategory.Good : quality;
}

[HarmonyPatch(typeof(CompQuality), nameof(CompQuality.SetQuality))]
internal static class GlitterworldMinimumQualityPatch
{
    private static void Prefix(CompQuality __instance, ref QualityCategory q)
    {
        if (__instance.parent.def.GetModExtension<KitchenwareExtension>()?.fixedMaterialKind ==
            KitchenMaterialKind.Glitterworld)
        {
            q = GlitterworldQualityPolicy.Clamp(q);
        }
    }
}

public sealed class CompProperties_KitchenwareStats : CompProperties
{
    public CompProperties_KitchenwareStats()
    {
        compClass = typeof(CompKitchenwareStats);
    }
}

public sealed class CompKitchenwareStats : ThingComp
{
    public KitchenwareStats CurrentStats => Calculate();

    public override string CompInspectStringExtra()
    {
        var stats = Calculate();
        var product = parent.def.GetModExtension<KitchenwareExtension>()?.product ?? KitchenwareProduct.Plate;
        var lines = new List<string>
        {
            "ImmersiveChefs_Stat_KitchenCleanlinessValue".Translate(stats.MaterialCleanliness.ToString("0")),
            (product is KitchenwareProduct.Cookware or KitchenwareProduct.ChefsKnife
                ? "ImmersiveChefs_Stat_KitchenComfortValue"
                : "ImmersiveChefs_Stat_DiningComfortValue").Translate(stats.Comfort.ToString("P0"))
        };
        if (product is KitchenwareProduct.Cookware or KitchenwareProduct.ChefsKnife)
        {
            lines.Add("ImmersiveChefs_Stat_CookingSpeedValue".Translate(stats.CookingSpeedFactor.ToString("P0")));
            lines.Add("ImmersiveChefs_Stat_CulinaryModifierValue".Translate(stats.CulinaryQualityModifier.ToString("+0;-0;0")));
        }
        else if (product == KitchenwareProduct.Plate)
        {
            lines.Add("ImmersiveChefs_Stat_EatingSpeedValue".Translate(stats.CookingSpeedFactor.ToString("P0")));
        }

        return string.Join("\n", lines);
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        var stats = Calculate();
        var product = parent.def.GetModExtension<KitchenwareExtension>()?.product ?? KitchenwareProduct.Plate;
        yield return Entry("ImmersiveChefs_Stat_KitchenCleanliness".Translate(), stats.MaterialCleanliness.ToString("0"), 3100);
        yield return Entry(
            (product is KitchenwareProduct.Cookware or KitchenwareProduct.ChefsKnife
                ? "ImmersiveChefs_Stat_KitchenComfort"
                : "ImmersiveChefs_Stat_DiningComfort").Translate(),
            stats.Comfort.ToString("P0"),
            3090);
        if (product is KitchenwareProduct.Cookware or KitchenwareProduct.ChefsKnife)
        {
            yield return Entry("ImmersiveChefs_Stat_CookingSpeed".Translate(), stats.CookingSpeedFactor.ToString("P0"), 3080);
            yield return Entry("ImmersiveChefs_Stat_CulinaryModifier".Translate(), stats.CulinaryQualityModifier.ToString("+0;-0;0"), 3070);
        }
        else if (product == KitchenwareProduct.Plate)
        {
            yield return Entry("ImmersiveChefs_Stat_EatingSpeed".Translate(), stats.CookingSpeedFactor.ToString("P0"), 3080);
        }
    }

    private KitchenwareStats Calculate()
    {
        var extension = parent.def.GetModExtension<KitchenwareExtension>();
        var product = extension?.product ?? KitchenwareProduct.Plate;
        var material = extension?.fixedMaterialKind ?? ResolveMaterial(parent.Stuff, product);
        var quality = QualityUtility.TryGetQuality(parent, out var foundQuality)
            ? foundQuality
            : QualityCategory.Normal;
        return KitchenwareStatCalculator.Calculate(material, product, quality);
    }

    private static KitchenMaterialKind ResolveMaterial(ThingDef? stuff, KitchenwareProduct product)
    {
        if (stuff is null)
        {
            return KitchenMaterialKind.Steel;
        }

        var categories = stuff.stuffProps?.categories;
        var descriptor = new KitchenMaterialDescriptor(
            stuff.defName,
            categories?.Any(category => category.defName.Equals("Metallic", StringComparison.OrdinalIgnoreCase)) == true,
            categories?.Any(category => category.defName.Equals("Woody", StringComparison.OrdinalIgnoreCase)) == true,
            categories?.Any(category => category.defName.Equals("Stony", StringComparison.OrdinalIgnoreCase)) == true);
        return OptionalMaterialAdapter.CreateClassifier().Classify(descriptor, product)?.Kind
               ?? KitchenMaterialKind.OtherMetal;
    }

    private static StatDrawEntry Entry(string label, string value, int priority)
    {
        return new StatDrawEntry(
            StatCategoryDefOf.BasicsNonPawn,
            label,
            value,
            "ImmersiveChefs_Stat_Explanation".Translate(),
            priority);
    }
}
