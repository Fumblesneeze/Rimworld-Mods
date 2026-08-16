using Verse;

namespace ImmersiveChefs;

public enum WareRequirementMode
{
    Strict,
    Prefer,
    Off
}

public enum DirtyWareFallback
{
    Never,
    UrgentOnly,
    Always
}

public enum OptionalIntegrationMode
{
    Auto,
    Off
}

public sealed class ImmersiveChefsSettings : ModSettings
{
    public WareRequirementMode WareRequirementMode = WareRequirementMode.Strict;
    public DirtyWareFallback DirtyWareFallback = DirtyWareFallback.UrgentOnly;
    public float EmergencyHungerThreshold = 0.15f;
    public float SimpleRecipeTimeMultiplier = 0.75f;
    public float AdvancedRecipeTimeMultiplier = 2f;
    public float ElaborateRecipeTimeMultiplier = 3f;
    public float PreparedWorkReduction = 0.40f;
    public float PreparedRotMultiplier = 4f;
    public int PastePreparationQuality = 20;
    public bool AutoCallAssistants = true;
    public int MaximumAssistants = 4;
    public float AssistantEffectScale = 1f;
    public bool PreferDishwashers = true;
    public bool AllowTerrainHandwashing = true;
    public float DishwashingWorkScale = 1f;
    public float DishwasherCapacityScale = 1f;
    public bool CulinaryQualityEnabled = true;
    public float QualityMoodScale = 1f;
    public float FoodPoisoningEffectScale = 1f;
    public float MaximumCustomPoisonChance = 0.50f;
    public float ToxicKitchenwareExposureScale = 1f;
    public bool MealTemperatureEnabled = true;
    public float ThermalHalfLifeHours = 2f;
    public float AutoMicrowaveBelow = 10f;
    public int MicrowaveQualityLoss = 5;
    public float MicrowaveExtraPoisonChance = 0.5f;
    public bool ColonyDiningStandards = true;
    public bool RoyaltyDiningStandards = true;
    public OptionalIntegrationMode ProcessorFramework = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode ExpandedMaterials = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode CeramicsContinued = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode AbsPolymer = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode DubsBadHygiene = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode Gastronomy = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode CommonSense = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode Hospitality = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode VarietyMatters = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode VanillaFoodVarietyExpanded = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode VanillaExpandedFramework = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode VanillaNutrientPasteExpanded = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode AdaptiveMealBill = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode OvercookedMeals = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode MealsOnWheels = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode PrioritizeMeals = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode Replimat = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode MealPrinter = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode FoodTextureVariety = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode TextureVariationIntegration = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode PickUpAndHaul = OptionalIntegrationMode.Auto;
    public OptionalIntegrationMode CookForYourself = OptionalIntegrationMode.Auto;
    public bool ShowDirtyWareTextures = true;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref WareRequirementMode, "wareRequirementMode", WareRequirementMode.Strict);
        Scribe_Values.Look(ref DirtyWareFallback, "dirtyWareFallback", DirtyWareFallback.UrgentOnly);
        Scribe_Values.Look(ref EmergencyHungerThreshold, "emergencyHungerThreshold", 0.15f);
        Scribe_Values.Look(ref SimpleRecipeTimeMultiplier, "simpleRecipeTimeMultiplier", 0.75f);
        Scribe_Values.Look(ref AdvancedRecipeTimeMultiplier, "advancedRecipeTimeMultiplier", 2f);
        Scribe_Values.Look(ref ElaborateRecipeTimeMultiplier, "elaborateRecipeTimeMultiplier", 3f);
        Scribe_Values.Look(ref PreparedWorkReduction, "preparedWorkReduction", 0.40f);
        Scribe_Values.Look(ref PreparedRotMultiplier, "preparedRotMultiplier", 4f);
        Scribe_Values.Look(ref PastePreparationQuality, "pastePreparationQuality", 20);
        Scribe_Values.Look(ref AutoCallAssistants, "autoCallAssistants", true);
        Scribe_Values.Look(ref MaximumAssistants, "maximumAssistants", 4);
        Scribe_Values.Look(ref AssistantEffectScale, "assistantEffectScale", 1f);
        Scribe_Values.Look(ref PreferDishwashers, "preferDishwashers", true);
        Scribe_Values.Look(ref AllowTerrainHandwashing, "allowTerrainHandwashing", true);
        Scribe_Values.Look(ref DishwashingWorkScale, "dishwashingWorkScale", 1f);
        Scribe_Values.Look(ref DishwasherCapacityScale, "dishwasherCapacityScale", 1f);
        Scribe_Values.Look(ref CulinaryQualityEnabled, "culinaryQualityEnabled", true);
        Scribe_Values.Look(ref QualityMoodScale, "qualityMoodScale", 1f);
        Scribe_Values.Look(ref FoodPoisoningEffectScale, "foodPoisoningEffectScale", 1f);
        Scribe_Values.Look(ref MaximumCustomPoisonChance, "maximumCustomPoisonChance", 0.50f);
        Scribe_Values.Look(
            ref ToxicKitchenwareExposureScale,
            "toxicKitchenwareExposureScale",
            1f);
        Scribe_Values.Look(ref MealTemperatureEnabled, "mealTemperatureEnabled", true);
        Scribe_Values.Look(ref ThermalHalfLifeHours, "thermalHalfLifeHours", 2f);
        Scribe_Values.Look(ref AutoMicrowaveBelow, "autoMicrowaveBelow", 10f);
        Scribe_Values.Look(ref MicrowaveQualityLoss, "microwaveQualityLoss", 5);
        Scribe_Values.Look(ref MicrowaveExtraPoisonChance, "microwaveExtraPoisonChance", 0.5f);
        Scribe_Values.Look(ref ColonyDiningStandards, "colonyDiningStandards", true);
        Scribe_Values.Look(ref RoyaltyDiningStandards, "royaltyDiningStandards", true);
        Scribe_Values.Look(ref ProcessorFramework, "processorFramework", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref ExpandedMaterials, "expandedMaterials", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref CeramicsContinued, "ceramicsContinued", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref AbsPolymer, "absPolymer", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref DubsBadHygiene, "dubsBadHygiene", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref Gastronomy, "gastronomy", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref CommonSense, "commonSense", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref Hospitality, "hospitality", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref VarietyMatters, "varietyMatters", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref VanillaFoodVarietyExpanded, "vanillaFoodVarietyExpanded", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref VanillaExpandedFramework, "vanillaExpandedFramework", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref VanillaNutrientPasteExpanded, "vanillaNutrientPasteExpanded", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref AdaptiveMealBill, "adaptiveMealBill", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref OvercookedMeals, "overcookedMeals", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref MealsOnWheels, "mealsOnWheels", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref PrioritizeMeals, "prioritizeMeals", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref Replimat, "replimat", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref MealPrinter, "mealPrinter", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref FoodTextureVariety, "foodTextureVariety", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(
            ref TextureVariationIntegration,
            "textureVariationIntegration",
            OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref PickUpAndHaul, "pickUpAndHaul", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref CookForYourself, "cookForYourself", OptionalIntegrationMode.Auto);
        Scribe_Values.Look(ref ShowDirtyWareTextures, "showDirtyWareTextures", true);
        ClampToAllowedRanges();
    }

    public void ClampToAllowedRanges()
    {
        EmergencyHungerThreshold = Clamp(EmergencyHungerThreshold, 0.05f, 0.35f);
        SimpleRecipeTimeMultiplier = Clamp(SimpleRecipeTimeMultiplier, 0.25f, 2f);
        AdvancedRecipeTimeMultiplier = Clamp(AdvancedRecipeTimeMultiplier, 1f, 5f);
        ElaborateRecipeTimeMultiplier = Clamp(ElaborateRecipeTimeMultiplier, 1f, 8f);
        PreparedWorkReduction = Clamp(PreparedWorkReduction, 0f, 0.75f);
        PreparedRotMultiplier = Clamp(PreparedRotMultiplier, 1f, 10f);
        PastePreparationQuality = Math.Max(0, Math.Min(50, PastePreparationQuality));
        MaximumAssistants = Math.Max(0, Math.Min(4, MaximumAssistants));
        AssistantEffectScale = Clamp(AssistantEffectScale, 0f, 3f);
        DishwashingWorkScale = Clamp(DishwashingWorkScale, 0.25f, 4f);
        DishwasherCapacityScale = Clamp(DishwasherCapacityScale, 0.5f, 4f);
        QualityMoodScale = Clamp(QualityMoodScale, 0f, 2f);
        FoodPoisoningEffectScale = Clamp(FoodPoisoningEffectScale, 0f, 3f);
        MaximumCustomPoisonChance = Clamp(MaximumCustomPoisonChance, 0.05f, 1f);
        ToxicKitchenwareExposureScale = float.IsNaN(ToxicKitchenwareExposureScale) ||
                                         float.IsInfinity(ToxicKitchenwareExposureScale)
            ? 1f
            : Clamp(ToxicKitchenwareExposureScale, 0f, 3f);
        ThermalHalfLifeHours = Clamp(ThermalHalfLifeHours, 0.25f, 12f);
        AutoMicrowaveBelow = Clamp(AutoMicrowaveBelow, -10f, 30f);
        MicrowaveQualityLoss = Math.Max(0, Math.Min(20, MicrowaveQualityLoss));
        MicrowaveExtraPoisonChance = Clamp(MicrowaveExtraPoisonChance, 0f, 5f);
    }

    private static float Clamp(float value, float minimum, float maximum)
    {
        return Math.Max(minimum, Math.Min(maximum, value));
    }
}
