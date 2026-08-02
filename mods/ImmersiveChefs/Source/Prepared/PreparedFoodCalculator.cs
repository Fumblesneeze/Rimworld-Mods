namespace ImmersiveChefs;

public static class PreparedFoodCalculator
{
    public static float WorkFactor(float preparedNutritionFraction, float maximumReduction)
    {
        var fraction = Clamp(preparedNutritionFraction, 0f, 1f);
        var reduction = Clamp(maximumReduction, 0f, 0.75f);
        return 1f - (fraction * reduction);
    }

    public static int SkillQuality(int cookingSkill) =>
        Math.Max(0, Math.Min(100, cookingSkill * 5));

    private static float Clamp(float value, float minimum, float maximum) =>
        Math.Max(minimum, Math.Min(maximum, value));
}
