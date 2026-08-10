using UnityEngine;
using Verse;

namespace ImmersiveChefs;

internal sealed class ImmersiveChefsSettingsUi
{
    private Vector2 scrollPosition;

    public void Draw(Rect rect, ImmersiveChefsSettings settings)
    {
        var view = new Rect(0f, 0f, rect.width - 20f, 1545f);
        Widgets.BeginScrollView(rect, ref scrollPosition, view);
        var listing = new Listing_Standard();
        listing.Begin(view);

        listing.Label("Restart-required classification and Def settings");
        Cycle(listing, "Ware requirement mode (restart required)", ref settings.WareRequirementMode);
        settings.SimpleRecipeTimeMultiplier = listing.SliderLabeled(
            $"Simple recipe time: {settings.SimpleRecipeTimeMultiplier:0.00}x (restart required)",
            settings.SimpleRecipeTimeMultiplier, 0.25f, 2f, 0.05f, null);
        settings.AdvancedRecipeTimeMultiplier = listing.SliderLabeled(
            $"Fine recipe time: {settings.AdvancedRecipeTimeMultiplier:0.00}x (restart required)",
            settings.AdvancedRecipeTimeMultiplier, 1f, 5f, 0.05f, null);
        settings.ElaborateRecipeTimeMultiplier = listing.SliderLabeled(
            $"Lavish recipe time: {settings.ElaborateRecipeTimeMultiplier:0.00}x (restart required)",
            settings.ElaborateRecipeTimeMultiplier, 1f, 8f, 0.05f, null);
        settings.PreparedRotMultiplier = listing.SliderLabeled(
            $"Prepared-food rot: {settings.PreparedRotMultiplier:0.0}x (restart required)",
            settings.PreparedRotMultiplier, 1f, 10f, 0.25f, null);
        settings.DishwasherCapacityScale = listing.SliderLabeled(
            $"Dishwasher capacity: {settings.DishwasherCapacityScale:0.00}x (restart required)",
            settings.DishwasherCapacityScale, 0.5f, 4f, 0.25f, null);
        Cycle(
            listing,
            "Texture variation integration (restart required)",
            ref settings.TextureVariationIntegration);
        listing.CheckboxLabeled(
            "Show dirty ware textures (restart required)",
            ref settings.ShowDirtyWareTextures);

        listing.GapLine();
        listing.Label("Live simulation settings");
        Cycle(listing, "Dirty-ware fallback", ref settings.DirtyWareFallback);
        settings.EmergencyHungerThreshold = listing.SliderLabeled(
            $"Emergency hunger: {settings.EmergencyHungerThreshold:P0}",
            settings.EmergencyHungerThreshold, 0.05f, 0.35f, 0.01f, null);
        settings.PreparedWorkReduction = listing.SliderLabeled(
            $"Prepared work reduction: {settings.PreparedWorkReduction:P0}",
            settings.PreparedWorkReduction, 0f, 0.75f, 0.01f, null);
        settings.PastePreparationQuality = (int)listing.SliderLabeled(
            $"Paste preparation quality: {settings.PastePreparationQuality}",
            settings.PastePreparationQuality, 0f, 50f, 1f, null);
        listing.CheckboxLabeled("Automatically call cooking assistants", ref settings.AutoCallAssistants);
        settings.MaximumAssistants = (int)listing.SliderLabeled(
            $"Maximum assistants: {settings.MaximumAssistants}", settings.MaximumAssistants, 0f, 4f, 1f, null);
        settings.AssistantEffectScale = listing.SliderLabeled(
            $"Assistant effect: {settings.AssistantEffectScale:0.00}x", settings.AssistantEffectScale, 0f, 3f, 0.05f, null);
        listing.CheckboxLabeled("Prefer dishwashers", ref settings.PreferDishwashers);
        listing.CheckboxLabeled("Allow terrain handwashing", ref settings.AllowTerrainHandwashing);
        settings.DishwashingWorkScale = listing.SliderLabeled(
            $"Dishwashing work: {settings.DishwashingWorkScale:0.00}x", settings.DishwashingWorkScale, 0.25f, 4f, 0.05f, null);
        listing.CheckboxLabeled("Culinary quality", ref settings.CulinaryQualityEnabled);
        settings.QualityMoodScale = listing.SliderLabeled(
            $"Quality mood: {settings.QualityMoodScale:0.00}x", settings.QualityMoodScale, 0f, 2f, 0.05f, null);
        settings.FoodPoisoningEffectScale = listing.SliderLabeled(
            $"Food poisoning effect: {settings.FoodPoisoningEffectScale:0.00}x", settings.FoodPoisoningEffectScale, 0f, 3f, 0.05f, null);
        settings.MaximumCustomPoisonChance = listing.SliderLabeled(
            $"Maximum custom poison chance: {settings.MaximumCustomPoisonChance:P0}", settings.MaximumCustomPoisonChance, 0.05f, 1f, 0.01f, null);
        settings.ToxicKitchenwareExposureScale = listing.SliderLabeled(
            $"Toxic kitchenware exposure: {settings.ToxicKitchenwareExposureScale:0.00}x",
            settings.ToxicKitchenwareExposureScale,
            0f,
            3f,
            0.05f,
            null);
        if (TemperatureOwnership.ImmersiveChefsFeaturesActive)
        {
            listing.CheckboxLabeled("Meal temperature", ref settings.MealTemperatureEnabled);
            settings.ThermalHalfLifeHours = listing.SliderLabeled(
                $"Thermal half-life: {settings.ThermalHalfLifeHours:0.00} h", settings.ThermalHalfLifeHours, 0.25f, 12f, 0.25f, null);
            settings.AutoMicrowaveBelow = listing.SliderLabeled(
                $"Automatically microwave below: {settings.AutoMicrowaveBelow:0} °C", settings.AutoMicrowaveBelow, -10f, 30f, 1f, null);
            settings.MicrowaveQualityLoss = (int)listing.SliderLabeled(
                $"Microwave quality loss: {settings.MicrowaveQualityLoss}", settings.MicrowaveQualityLoss, 0f, 20f, 1f, null);
            settings.MicrowaveExtraPoisonChance = listing.SliderLabeled(
                $"Microwave poison chance: {settings.MicrowaveExtraPoisonChance:0.0} pp", settings.MicrowaveExtraPoisonChance, 0f, 5f, 0.1f, null);
        }
        else
        {
            listing.Label("Meal temperature and microwave: provided by Thermodynamics - Hot Meals");
        }
        listing.CheckboxLabeled("Colony dining standards", ref settings.ColonyDiningStandards);
        listing.CheckboxLabeled("Royalty dining standards", ref settings.RoyaltyDiningStandards);

        listing.GapLine();
        listing.Label("Optional integrations (restart required; Auto or Off)");
        Cycle(listing, "Processor Framework", ref settings.ProcessorFramework);
        Cycle(listing, "Expanded Materials", ref settings.ExpandedMaterials);
        Cycle(listing, "ABS Polymer", ref settings.AbsPolymer);
        Cycle(listing, "Dubs Bad Hygiene", ref settings.DubsBadHygiene);
        Cycle(listing, "Gastronomy", ref settings.Gastronomy);
        Cycle(listing, "Common Sense", ref settings.CommonSense);
        Cycle(listing, "Hospitality", ref settings.Hospitality);
        Cycle(listing, "Variety Matters", ref settings.VarietyMatters);
        Cycle(listing, "Vanilla Food Variety Expanded", ref settings.VanillaFoodVarietyExpanded);
        Cycle(listing, "Vanilla Expanded Framework", ref settings.VanillaExpandedFramework);
        Cycle(listing, "Vanilla Nutrient Paste Expanded", ref settings.VanillaNutrientPasteExpanded);
        Cycle(listing, "Adaptive Meal Bill", ref settings.AdaptiveMealBill);
        Cycle(listing, "Overcooked Meals", ref settings.OvercookedMeals);
        Cycle(listing, "Meals on Wheels", ref settings.MealsOnWheels);
        Cycle(listing, "Prioritize Meals over Preserved Foods", ref settings.PrioritizeMeals);
        Cycle(listing, "Replimat + Replimat Meals", ref settings.Replimat);
        Cycle(listing, "Meal Printer", ref settings.MealPrinter);
        Cycle(listing, "Food Texture Variety", ref settings.FoodTextureVariety);
        Cycle(listing, "Pick Up And Haul", ref settings.PickUpAndHaul);

        listing.End();
        Widgets.EndScrollView();
        settings.ClampToAllowedRanges();
    }

    private static void Cycle<T>(Listing_Standard listing, string label, ref T value) where T : struct, Enum
    {
        if (!listing.ButtonTextLabeled(label, value.ToString()))
        {
            return;
        }

        var values = (T[])Enum.GetValues(typeof(T));
        var index = Array.IndexOf(values, value);
        value = values[(index + 1) % values.Length];
    }
}
