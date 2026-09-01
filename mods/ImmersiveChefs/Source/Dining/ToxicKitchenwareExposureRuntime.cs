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

        var dose = ToxicKitchenwareExposurePolicy.CalculateByProvider(
            cookwareMaterial,
            plateMaterial,
            cutleryMaterial,
            ImmersiveChefsMod.Settings.ToxicKitchenwareExposureScale,
            ingester.RaceProps.Humanlike,
            nutritionIngested);
        var coreDose = dose.LeadDose;
        if (dose.UraniumDose > 0f)
        {
            var uraniumApplication = RimatomicsRadiationAdapter.Apply(ingester, dose.UraniumDose);
            if (uraniumApplication == RimatomicsRadiationApplication.UseCoreFallback)
            {
                var crashLandingApplication =
                    CrashLandingRadiationAdapter.Apply(ingester, dose.UraniumDose);
                if (crashLandingApplication == CrashLandingRadiationApplication.UseNextProvider)
                {
                    coreDose += dose.UraniumDose;
                }
            }
        }

        if (coreDose > 0f)
        {
            HealthUtility.AdjustSeverity(ingester, HediffDefOf.ToxicBuildup, coreDose);
        }

        return dose.TotalDose;
    }
}
