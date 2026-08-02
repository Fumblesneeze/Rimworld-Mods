namespace ImmersiveChefs;

public readonly struct CulinaryQualityInputs
{
    public CulinaryQualityInputs(
        float leadSkill,
        float ingredientDiversity,
        float ingredientCraftsmanship,
        float preparationQuality,
        float cookware,
        float knife,
        float assistants)
    {
        LeadSkill = leadSkill;
        IngredientDiversity = ingredientDiversity;
        IngredientCraftsmanship = ingredientCraftsmanship;
        PreparationQuality = preparationQuality;
        Cookware = cookware;
        Knife = knife;
        Assistants = assistants;
    }

    public float LeadSkill { get; }
    public float IngredientDiversity { get; }
    public float IngredientCraftsmanship { get; }
    public float PreparationQuality { get; }
    public float Cookware { get; }
    public float Knife { get; }
    public float Assistants { get; }
}

public static class CulinaryQualityCalculator
{
    public static int Calculate(CulinaryQualityInputs inputs)
    {
        var score = (0.30f * Clamp(inputs.LeadSkill)) +
                    (0.15f * Clamp(inputs.IngredientDiversity)) +
                    (0.10f * Clamp(inputs.IngredientCraftsmanship)) +
                    (0.15f * Clamp(inputs.PreparationQuality)) +
                    (0.10f * Clamp(inputs.Cookware)) +
                    (0.05f * Clamp(inputs.Knife)) +
                    (0.15f * Clamp(inputs.Assistants));
        return Math.Max(0, Math.Min(100, (int)Math.Round(score, MidpointRounding.AwayFromZero)));
    }

    public static string LabelFor(int score)
    {
        var bounded = Math.Max(0, Math.Min(100, score));
        if (bounded <= 19) return "Awful";
        if (bounded <= 34) return "Poor";
        if (bounded <= 49) return "Normal";
        if (bounded <= 64) return "Good";
        if (bounded <= 79) return "Excellent";
        if (bounded <= 89) return "Masterwork";
        return "Legendary";
    }

    public static int MoodOffsetFor(int score)
    {
        return LabelFor(score) switch
        {
            "Awful" => -6,
            "Poor" => -3,
            "Good" => 2,
            "Excellent" => 4,
            "Masterwork" => 6,
            "Legendary" => 8,
            _ => 0
        };
    }

    private static float Clamp(float value) => Math.Max(0f, Math.Min(100f, value));
}
