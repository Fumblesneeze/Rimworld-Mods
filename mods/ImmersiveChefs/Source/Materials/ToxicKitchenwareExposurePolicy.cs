namespace ImmersiveChefs;

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
        if (!humanlike || nutritionIngested <= 0f ||
            float.IsNaN(nutritionIngested) || float.IsInfinity(nutritionIngested))
        {
            return 0f;
        }

        var scale = float.IsNaN(exposureScale) || float.IsInfinity(exposureScale)
            ? 0f
            : Clamp(exposureScale, 0f, 3f);
        if (scale <= 0f)
        {
            return 0f;
        }

        var dose = (IsToxic(cookwareMaterial) ? CookwareDose : 0f) +
                   (IsToxic(plateMaterial) ? PlateDose : 0f) +
                   (IsToxic(cutleryMaterial) ? CutleryDose : 0f);
        return Clamp(dose * scale, 0f, (CookwareDose + PlateDose + CutleryDose) * 3f);
    }

    public static bool IsToxic(KitchenMaterialKind material) =>
        material is KitchenMaterialKind.Lead or KitchenMaterialKind.Uranium;

    private static float Clamp(float value, float minimum, float maximum) =>
        Math.Max(minimum, Math.Min(maximum, value));
}
