using RimWorld;
using Verse;

namespace ImmersiveChefs;

internal static class ToxicKitchenwareExposureRuntime
{
    internal static float Apply(
        Pawn ingester,
        KitchenMaterialKind cookwareMaterial,
        KitchenMaterialKind plateMaterial,
        KitchenMaterialKind cutleryMaterial,
        float nutritionIngested)
    {
        if (ingester is null)
        {
            throw new ArgumentNullException(nameof(ingester));
        }

        var dose = ToxicKitchenwareExposurePolicy.Calculate(
            cookwareMaterial,
            plateMaterial,
            cutleryMaterial,
            ImmersiveChefsMod.Settings.ToxicKitchenwareExposureScale,
            ingester.RaceProps.Humanlike,
            nutritionIngested);
        if (dose > 0f)
        {
            HealthUtility.AdjustSeverity(ingester, HediffDefOf.ToxicBuildup, dose);
        }

        return dose;
    }
}
