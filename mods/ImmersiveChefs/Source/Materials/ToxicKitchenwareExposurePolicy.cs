namespace ImmersiveChefs;

public readonly struct UnsafeKitchenwareExposureDose
{
    public UnsafeKitchenwareExposureDose(float leadDose, float uraniumDose)
    {
        LeadDose = leadDose;
        UraniumDose = uraniumDose;
    }

    public float LeadDose { get; }

    public float UraniumDose { get; }

    public float TotalDose => LeadDose + UraniumDose;
}

public static class ToxicKitchenwareExposurePolicy
{
    public const float CookwareDose = 0.020f;
    public const float PlateDose = 0.015f;
    public const float CutleryDose = 0.010f;

    public static float Calculate(
        KitchenMaterialKind cookwareMaterial,
        KitchenMaterialKind plateMaterial,
        KitchenMaterialKind cutleryMaterial,
        float exposureScale,
        bool humanlike,
        float nutritionIngested)
    {
        return CalculateByProvider(
            cookwareMaterial,
            plateMaterial,
            cutleryMaterial,
            exposureScale,
            humanlike,
            nutritionIngested).TotalDose;
    }

    public static UnsafeKitchenwareExposureDose CalculateByProvider(
        KitchenMaterialKind cookwareMaterial,
        KitchenMaterialKind plateMaterial,
        KitchenMaterialKind cutleryMaterial,
        float exposureScale,
        bool humanlike,
        float nutritionIngested)
    {
        if (!humanlike || nutritionIngested <= 0f ||
            float.IsNaN(nutritionIngested) || float.IsInfinity(nutritionIngested))
        {
            return default;
        }

        var scale = float.IsNaN(exposureScale) || float.IsInfinity(exposureScale)
            ? 0f
            : Clamp(exposureScale, 0f, 3f);
        if (scale <= 0f)
        {
            return default;
        }

        var leadDose = DoseFor(KitchenMaterialKind.Lead, cookwareMaterial, plateMaterial, cutleryMaterial);
        var uraniumDose = DoseFor(KitchenMaterialKind.Uranium, cookwareMaterial, plateMaterial, cutleryMaterial);
        var maximum = (CookwareDose + PlateDose + CutleryDose) * 3f;
        return new UnsafeKitchenwareExposureDose(
            Clamp(leadDose * scale, 0f, maximum),
            Clamp(uraniumDose * scale, 0f, maximum));
    }

    public static bool IsToxic(KitchenMaterialKind material) =>
        material is KitchenMaterialKind.Lead or KitchenMaterialKind.Uranium;

    private static float DoseFor(
        KitchenMaterialKind expected,
        KitchenMaterialKind cookwareMaterial,
        KitchenMaterialKind plateMaterial,
        KitchenMaterialKind cutleryMaterial) =>
        (cookwareMaterial == expected ? CookwareDose : 0f) +
        (plateMaterial == expected ? PlateDose : 0f) +
        (cutleryMaterial == expected ? CutleryDose : 0f);

    private static float Clamp(float value, float minimum, float maximum) =>
        Math.Max(minimum, Math.Min(maximum, value));
}
