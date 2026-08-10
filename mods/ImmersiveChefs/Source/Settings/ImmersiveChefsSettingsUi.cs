using UnityEngine;
using Verse;

namespace ImmersiveChefs;

internal sealed class ImmersiveChefsSettingsUi
{
    private Vector2 scrollPosition;

    public void Draw(Rect rect, ImmersiveChefsSettings settings)
    {
        var view = new Rect(0f, 0f, rect.width - 20f, 2300f);
        Widgets.BeginScrollView(rect, ref scrollPosition, view);
        var listing = new Listing_Standard();
        listing.Begin(view);

        listing.Label("ImmersiveChefs_Settings_RestartHeading".Translate());
        Cycle(listing, "ImmersiveChefs_Settings_WareRequirement".Translate(), ref settings.WareRequirementMode);
        settings.SimpleRecipeTimeMultiplier = Slider(
            listing,
            "ImmersiveChefs_Settings_SimpleRecipeTime".Translate(settings.SimpleRecipeTimeMultiplier.ToString("0.00")),
            settings.SimpleRecipeTimeMultiplier, 0.25f, 2f, 0.05f);
        settings.AdvancedRecipeTimeMultiplier = Slider(
            listing,
            "ImmersiveChefs_Settings_FineRecipeTime".Translate(settings.AdvancedRecipeTimeMultiplier.ToString("0.00")),
            settings.AdvancedRecipeTimeMultiplier, 1f, 5f, 0.05f);
        settings.ElaborateRecipeTimeMultiplier = Slider(
            listing,
            "ImmersiveChefs_Settings_LavishRecipeTime".Translate(settings.ElaborateRecipeTimeMultiplier.ToString("0.00")),
            settings.ElaborateRecipeTimeMultiplier, 1f, 8f, 0.05f);
        settings.PreparedRotMultiplier = Slider(
            listing,
            "ImmersiveChefs_Settings_PreparedRot".Translate(settings.PreparedRotMultiplier.ToString("0.0")),
            settings.PreparedRotMultiplier, 1f, 10f, 0.25f);
        settings.DishwasherCapacityScale = Slider(
            listing,
            "ImmersiveChefs_Settings_DishwasherCapacity".Translate(settings.DishwasherCapacityScale.ToString("0.00")),
            settings.DishwasherCapacityScale, 0.5f, 4f, 0.25f);
        Cycle(
            listing,
            "ImmersiveChefs_Settings_TextureVariation".Translate(),
            ref settings.TextureVariationIntegration);
        listing.CheckboxLabeled(
            "ImmersiveChefs_Settings_ShowDirtyTextures".Translate(),
            ref settings.ShowDirtyWareTextures);

        listing.GapLine();
        listing.Label("ImmersiveChefs_Settings_LiveHeading".Translate());
        Cycle(listing, "ImmersiveChefs_Settings_DirtyFallback".Translate(), ref settings.DirtyWareFallback);
        settings.EmergencyHungerThreshold = Slider(
            listing,
            "ImmersiveChefs_Settings_EmergencyHunger".Translate(settings.EmergencyHungerThreshold.ToString("P0")),
            settings.EmergencyHungerThreshold, 0.05f, 0.35f, 0.01f);
        settings.PreparedWorkReduction = Slider(
            listing,
            "ImmersiveChefs_Settings_PreparedWorkReduction".Translate(settings.PreparedWorkReduction.ToString("P0")),
            settings.PreparedWorkReduction, 0f, 0.75f, 0.01f);
        settings.PastePreparationQuality = (int)Slider(
            listing,
            "ImmersiveChefs_Settings_PasteQuality".Translate(settings.PastePreparationQuality),
            settings.PastePreparationQuality, 0f, 50f, 1f);
        listing.CheckboxLabeled("ImmersiveChefs_Settings_AutoAssistants".Translate(), ref settings.AutoCallAssistants);
        settings.MaximumAssistants = (int)Slider(
            listing,
            "ImmersiveChefs_Settings_MaxAssistants".Translate(settings.MaximumAssistants),
            settings.MaximumAssistants, 0f, 4f, 1f);
        settings.AssistantEffectScale = Slider(
            listing,
            "ImmersiveChefs_Settings_AssistantEffect".Translate(settings.AssistantEffectScale.ToString("0.00")),
            settings.AssistantEffectScale, 0f, 3f, 0.05f);
        listing.CheckboxLabeled("ImmersiveChefs_Settings_PreferDishwashers".Translate(), ref settings.PreferDishwashers);
        listing.CheckboxLabeled("ImmersiveChefs_Settings_TerrainHandwashing".Translate(), ref settings.AllowTerrainHandwashing);
        settings.DishwashingWorkScale = Slider(
            listing,
            "ImmersiveChefs_Settings_DishwashingWork".Translate(settings.DishwashingWorkScale.ToString("0.00")),
            settings.DishwashingWorkScale, 0.25f, 4f, 0.05f);
        listing.CheckboxLabeled("ImmersiveChefs_Settings_CulinaryQuality".Translate(), ref settings.CulinaryQualityEnabled);
        settings.QualityMoodScale = Slider(
            listing,
            "ImmersiveChefs_Settings_QualityMood".Translate(settings.QualityMoodScale.ToString("0.00")),
            settings.QualityMoodScale, 0f, 2f, 0.05f);
        settings.FoodPoisoningEffectScale = Slider(
            listing,
            "ImmersiveChefs_Settings_FoodPoisoningEffect".Translate(settings.FoodPoisoningEffectScale.ToString("0.00")),
            settings.FoodPoisoningEffectScale, 0f, 3f, 0.05f);
        settings.MaximumCustomPoisonChance = Slider(
            listing,
            "ImmersiveChefs_Settings_MaxPoisonChance".Translate(settings.MaximumCustomPoisonChance.ToString("P0")),
            settings.MaximumCustomPoisonChance, 0.05f, 1f, 0.01f);
        settings.ToxicKitchenwareExposureScale = Slider(
            listing,
            "ImmersiveChefs_Settings_ToxicExposure".Translate(settings.ToxicKitchenwareExposureScale.ToString("0.00")),
            settings.ToxicKitchenwareExposureScale,
            0f,
            3f,
            0.05f);
        if (TemperatureOwnership.ImmersiveChefsFeaturesActive)
        {
            listing.CheckboxLabeled("ImmersiveChefs_Settings_MealTemperature".Translate(), ref settings.MealTemperatureEnabled);
            settings.ThermalHalfLifeHours = Slider(
                listing,
                "ImmersiveChefs_Settings_ThermalHalfLife".Translate(settings.ThermalHalfLifeHours.ToString("0.00")),
                settings.ThermalHalfLifeHours, 0.25f, 12f, 0.25f);
            settings.AutoMicrowaveBelow = Slider(
                listing,
                "ImmersiveChefs_Settings_AutoMicrowaveBelow".Translate(settings.AutoMicrowaveBelow.ToString("0")),
                settings.AutoMicrowaveBelow, -10f, 30f, 1f);
            settings.MicrowaveQualityLoss = (int)Slider(
                listing,
                "ImmersiveChefs_Settings_MicrowaveQualityLoss".Translate(settings.MicrowaveQualityLoss),
                settings.MicrowaveQualityLoss, 0f, 20f, 1f);
            settings.MicrowaveExtraPoisonChance = Slider(
                listing,
                "ImmersiveChefs_Settings_MicrowavePoisonChance".Translate(settings.MicrowaveExtraPoisonChance.ToString("0.0")),
                settings.MicrowaveExtraPoisonChance, 0f, 5f, 0.1f);
        }
        else
        {
            listing.Label("ImmersiveChefs_Settings_ThermodynamicsOwnsTemperature".Translate());
        }
        listing.CheckboxLabeled("ImmersiveChefs_Settings_ColonyStandards".Translate(), ref settings.ColonyDiningStandards);
        listing.CheckboxLabeled("ImmersiveChefs_Settings_RoyaltyStandards".Translate(), ref settings.RoyaltyDiningStandards);

        listing.GapLine();
        listing.Label("ImmersiveChefs_Settings_IntegrationsHeading".Translate());
        Cycle(listing, "ImmersiveChefs_Integration_ProcessorFramework".Translate(), ref settings.ProcessorFramework);
        Cycle(listing, "ImmersiveChefs_Integration_ExpandedMaterials".Translate(), ref settings.ExpandedMaterials);
        Cycle(listing, "ImmersiveChefs_Integration_AbsPolymer".Translate(), ref settings.AbsPolymer);
        Cycle(listing, "ImmersiveChefs_Integration_DubsBadHygiene".Translate(), ref settings.DubsBadHygiene);
        Cycle(listing, "ImmersiveChefs_Integration_Gastronomy".Translate(), ref settings.Gastronomy);
        Cycle(listing, "ImmersiveChefs_Integration_CommonSense".Translate(), ref settings.CommonSense);
        Cycle(listing, "ImmersiveChefs_Integration_Hospitality".Translate(), ref settings.Hospitality);
        Cycle(listing, "ImmersiveChefs_Integration_VarietyMatters".Translate(), ref settings.VarietyMatters);
        Cycle(listing, "ImmersiveChefs_Integration_VanillaFoodVarietyExpanded".Translate(), ref settings.VanillaFoodVarietyExpanded);
        Cycle(listing, "ImmersiveChefs_Integration_VanillaExpandedFramework".Translate(), ref settings.VanillaExpandedFramework);
        Cycle(listing, "ImmersiveChefs_Integration_VanillaNutrientPasteExpanded".Translate(), ref settings.VanillaNutrientPasteExpanded);
        Cycle(listing, "ImmersiveChefs_Integration_AdaptiveMealBill".Translate(), ref settings.AdaptiveMealBill);
        Cycle(listing, "ImmersiveChefs_Integration_OvercookedMeals".Translate(), ref settings.OvercookedMeals);
        Cycle(listing, "ImmersiveChefs_Integration_MealsOnWheels".Translate(), ref settings.MealsOnWheels);
        Cycle(listing, "ImmersiveChefs_Integration_PrioritizeMeals".Translate(), ref settings.PrioritizeMeals);
        Cycle(listing, "ImmersiveChefs_Integration_Replimat".Translate(), ref settings.Replimat);
        Cycle(listing, "ImmersiveChefs_Integration_MealPrinter".Translate(), ref settings.MealPrinter);
        Cycle(listing, "ImmersiveChefs_Integration_FoodTextureVariety".Translate(), ref settings.FoodTextureVariety);
        Cycle(listing, "ImmersiveChefs_Integration_PickUpAndHaul".Translate(), ref settings.PickUpAndHaul);

        listing.End();
        Widgets.EndScrollView();
        settings.ClampToAllowedRanges();
    }

    private static void Cycle<T>(Listing_Standard listing, string label, ref T value) where T : struct, Enum
    {
        var valueLabel = ("ImmersiveChefs_Enum_" + typeof(T).Name + "_" + value).Translate();
        if (!listing.ButtonTextLabeled(label, valueLabel))
        {
            return;
        }

        var values = (T[])Enum.GetValues(typeof(T));
        var index = Array.IndexOf(values, value);
        value = values[(index + 1) % values.Length];
    }

    private static float Slider(
        Listing_Standard listing,
        string label,
        float value,
        float minimum,
        float maximum,
        float roundTo)
    {
        listing.Label(label);
        var changed = listing.Slider(value, minimum, maximum);
        if (roundTo <= 0f)
        {
            return changed;
        }

        return Mathf.Clamp(Mathf.Round(changed / roundTo) * roundTo, minimum, maximum);
    }
}
