using RimWorld;
using Verse;

namespace ImmersiveChefs;

internal readonly struct ServiceWareSnapshot
{
    internal ServiceWareSnapshot(
        KitchenMaterialKind material,
        QualityCategory quality,
        float comfort,
        bool dirty)
    {
        Material = material;
        Quality = quality;
        Comfort = comfort;
        Dirty = dirty;
    }

    internal KitchenMaterialKind Material { get; }
    internal QualityCategory Quality { get; }
    internal float Comfort { get; }
    internal bool Dirty { get; }
}

internal static class DiningStandardsRuntime
{
    internal static int DiningThoughtStage(
        Pawn pawn,
        ThingDef mealDef,
        CulinaryServingRecord serving,
        DiningSession? dining)
    {
        if (ImmersiveChefsMod.Settings.WareRequirementMode == WareRequirementMode.Off)
        {
            return 0;
        }

        var plate = KitchenwareRuntime.Describe(dining?.Plate);
        var silverware = KitchenwareRuntime.Describe(dining?.Silverware);
        // Every covered serving is expected to carry its actual embedded plate. This also
        // catches meals imported from an older save or produced by another mod, which have
        // no EmergencyUnplated marker but are still visibly unplated at the table.
        var missingPlate = plate is null;
        var missingSilverware = silverware is null;
        if (DiningOutcomeCalculator.HasDirtyWare(serving.Contamination))
        {
            return 4;
        }

        if (missingPlate && missingSilverware)
        {
            return 3;
        }

        if (missingPlate)
        {
            return 2;
        }

        if (missingSilverware)
        {
            return 1;
        }

        var requirement = EffectiveRequirement(pawn);
        if (requirement is null || plate is null || silverware is null)
        {
            return 0;
        }

        var missesMaterial = !MeetsMaterial(plate.Value, requirement.Value.Material) ||
                             !MeetsMaterial(silverware.Value, requirement.Value.Material);
        var comfort = Math.Max(0f, Math.Min(1f, (plate.Value.Comfort + silverware.Value.Comfort) / 2f));
        var missesComfort = comfort < requirement.Value.MinimumComfort;
        var complexity = MealComplexityRuntime.Classify(mealDef);
        var missesComplexity = complexity.HasValue && complexity.Value < requirement.Value.Complexity;
        var missesQuality = serving.QualityScore < requirement.Value.MinimumQuality;
        var missCount = new[] { missesMaterial, missesComfort, missesComplexity, missesQuality }.Count(value => value);
        if (missCount > 1)
        {
            return 9;
        }

        if (missesMaterial) return 6;
        if (missesComfort) return 5;
        if (missesComplexity) return 7;
        if (missesQuality) return 8;
        return 0;
    }

    private static DiningRequirement? EffectiveRequirement(Pawn pawn)
    {
        DiningRequirement? result = null;
        if (ImmersiveChefsMod.Settings.ColonyDiningStandards &&
            pawn.IsColonist && !pawn.IsPrisonerOfColony)
        {
            result = DiningStandardPolicy.ForExpectationOrder(
                ExpectationsUtility.CurrentExpectationFor(pawn).order);
        }

        if (ImmersiveChefsMod.Settings.RoyaltyDiningStandards &&
            ImmersiveChefsMod.Integrations?.IsActive(OptionalIntegration.Royalty) == true &&
            pawn.royalty?.MostSeniorTitle?.def is { } title && title.seniority >= 100)
        {
            var titleRequirement = DiningStandardPolicy.ForTitleSeniority(title.seniority / 100);
            result = result.HasValue
                ? DiningStandardPolicy.Combine(result.Value, titleRequirement)
                : titleRequirement;
        }

        return result;
    }

    private static bool MeetsMaterial(ServiceWareSnapshot ware, ServiceMaterialTier required)
    {
        if (ware.Dirty)
        {
            return false;
        }

        var actual = ware.Material switch
        {
            KitchenMaterialKind.Gold => ServiceMaterialTier.Gold,
            KitchenMaterialKind.Silver => ServiceMaterialTier.Silver,
            KitchenMaterialKind.StainlessSteel or KitchenMaterialKind.Ceramic => ServiceMaterialTier.Refined,
            KitchenMaterialKind.Steel or KitchenMaterialKind.Plastic or KitchenMaterialKind.AdvancedSteel or
                KitchenMaterialKind.Titanium or KitchenMaterialKind.Plasteel => ServiceMaterialTier.Durable,
            _ => ServiceMaterialTier.Basic
        };
        if (actual >= required)
        {
            return true;
        }

        return required == ServiceMaterialTier.Refined &&
               ware.Material == KitchenMaterialKind.Steel &&
               ware.Quality >= QualityCategory.Good &&
               !HasRegisteredRefinedMaterial();
    }

    private static bool HasRegisteredRefinedMaterial()
    {
        return (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.ExpandedMaterialsMetals) &&
                DefDatabase<ThingDef>.GetNamedSilentFail("EM_StainlessSteel") is not null) ||
               DefDatabase<ThingDef>.AllDefsListForReading.Any(def =>
                   def.stuffProps is not null &&
                   def.defName.IndexOf("Ceramic", StringComparison.OrdinalIgnoreCase) >= 0);
    }
}

internal static class MealComplexityRuntime
{
    internal static MealComplexity? Classify(ThingDef mealDef)
    {
        if (mealDef.GetModExtension<MealCoverageExtension>()?.complexity is { } extension)
        {
            return extension;
        }

        return mealDef.defName switch
        {
            "MealSimple" => MealComplexity.Simple,
            "MealFine" or "MealFine_Meat" or "MealFine_Veg" => MealComplexity.Advanced,
            "MealLavish" or "MealLavish_Meat" or "MealLavish_Veg" => MealComplexity.Elaborate,
            _ => null
        };
    }
}
