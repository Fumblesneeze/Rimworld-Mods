namespace ImmersiveChefs;

public readonly struct DiningRiskInputs
{
    public DiningRiskInputs(
        float baseChance,
        int qualityScore,
        ThermalBand thermalBand,
        ContaminationSources contamination,
        float? plateServiceScore,
        float? cutleryServiceScore,
        int microwaveReheatCount,
        float microwaveExtraPercentagePoints,
        float effectScale,
        float maximumChance)
    {
        BaseChance = baseChance;
        QualityScore = qualityScore;
        ThermalBand = thermalBand;
        Contamination = contamination;
        PlateServiceScore = plateServiceScore;
        CutleryServiceScore = cutleryServiceScore;
        MicrowaveReheatCount = microwaveReheatCount;
        MicrowaveExtraPercentagePoints = microwaveExtraPercentagePoints;
        EffectScale = effectScale;
        MaximumChance = maximumChance;
    }

    public float BaseChance { get; }
    public int QualityScore { get; }
    public ThermalBand ThermalBand { get; }
    public ContaminationSources Contamination { get; }
    public float? PlateServiceScore { get; }
    public float? CutleryServiceScore { get; }
    public int MicrowaveReheatCount { get; }
    public float MicrowaveExtraPercentagePoints { get; }
    public float EffectScale { get; }
    public float MaximumChance { get; }
}

public enum FoodPoisonRiskContributor
{
    None,
    LowCulinaryQuality,
    ColdMeal,
    FrozenMeal,
    DirtyCookware,
    DirtyPlate,
    DirtyCutlery,
    WildWaterCookware,
    WildWaterPlate,
    WildWaterCutlery,
    PoorPlate,
    PoorCutlery,
    MicrowaveReheating,
    VanillaBase
}

public readonly struct DiningPoisonRiskResult
{
    public DiningPoisonRiskResult(
        float finalChance,
        FoodPoisonRiskContributor largestPositiveContributor,
        float largestPositiveContributionPercentagePoints)
    {
        FinalChance = finalChance;
        LargestPositiveContributor = largestPositiveContributor;
        LargestPositiveContributionPercentagePoints = largestPositiveContributionPercentagePoints;
    }

    public float FinalChance { get; }
    public FoodPoisonRiskContributor LargestPositiveContributor { get; }
    public float LargestPositiveContributionPercentagePoints { get; }
}

public static class DiningOutcomeCalculator
{
    public const float CustomRiskBalanceFactor = 0.5f;

    private const ContaminationSources DirtyWare =
        ContaminationSources.DirtyCookware |
        ContaminationSources.DirtyPlate |
        ContaminationSources.DirtyCutlery;

    public static float FinalPoisonChance(DiningRiskInputs inputs)
    {
        return CalculatePoisonRisk(inputs).FinalChance;
    }

    public static DiningPoisonRiskResult CalculatePoisonRisk(DiningRiskInputs inputs)
    {
        var baseChance = Clamp(inputs.BaseChance, 0f, 1f);
        var cap = Clamp(inputs.MaximumChance, 0f, 1f);
        if (baseChance > cap)
        {
            return new DiningPoisonRiskResult(
                baseChance,
                baseChance > 0f ? FoodPoisonRiskContributor.VanillaBase : FoodPoisonRiskContributor.None,
                baseChance * 100f);
        }

        var scale = Math.Max(0f, inputs.EffectScale);
        var largestContributor = FoodPoisonRiskContributor.None;
        var largestContribution = 0f;

        var qualityPoints = (50 - Clamp(inputs.QualityScore, 0, 100)) *
                            0.20f *
                            CustomRiskBalanceFactor;
        var percentagePoints = qualityPoints;
        Consider(
            FoodPoisonRiskContributor.LowCulinaryQuality,
            qualityPoints * scale,
            ref largestContributor,
            ref largestContribution);

        var thermalPoints = inputs.ThermalBand switch
        {
            ThermalBand.Cold => 3f * CustomRiskBalanceFactor,
            ThermalBand.Frozen => 8f * CustomRiskBalanceFactor,
            _ => 0f
        };
        percentagePoints += thermalPoints;
        Consider(
            inputs.ThermalBand == ThermalBand.Frozen
                ? FoodPoisonRiskContributor.FrozenMeal
                : FoodPoisonRiskContributor.ColdMeal,
            thermalPoints * scale,
            ref largestContributor,
            ref largestContribution);

        AddContaminationCandidate(
            inputs.Contamination,
            ContaminationSources.DirtyCookware,
            FoodPoisonRiskContributor.DirtyCookware,
            15f * CustomRiskBalanceFactor,
            scale,
            ref percentagePoints,
            ref largestContributor,
            ref largestContribution);
        AddContaminationCandidate(
            inputs.Contamination,
            ContaminationSources.DirtyPlate,
            FoodPoisonRiskContributor.DirtyPlate,
            15f * CustomRiskBalanceFactor,
            scale,
            ref percentagePoints,
            ref largestContributor,
            ref largestContribution);
        AddContaminationCandidate(
            inputs.Contamination,
            ContaminationSources.DirtyCutlery,
            FoodPoisonRiskContributor.DirtyCutlery,
            10f * CustomRiskBalanceFactor,
            scale,
            ref percentagePoints,
            ref largestContributor,
            ref largestContribution);
        AddContaminationCandidate(
            inputs.Contamination,
            ContaminationSources.WildWaterCookware,
            FoodPoisonRiskContributor.WildWaterCookware,
            5f * CustomRiskBalanceFactor,
            scale,
            ref percentagePoints,
            ref largestContributor,
            ref largestContribution);
        AddContaminationCandidate(
            inputs.Contamination,
            ContaminationSources.WildWaterPlate,
            FoodPoisonRiskContributor.WildWaterPlate,
            4f * CustomRiskBalanceFactor,
            scale,
            ref percentagePoints,
            ref largestContributor,
            ref largestContribution);
        AddContaminationCandidate(
            inputs.Contamination,
            ContaminationSources.WildWaterCutlery,
            FoodPoisonRiskContributor.WildWaterCutlery,
            3f * CustomRiskBalanceFactor,
            scale,
            ref percentagePoints,
            ref largestContributor,
            ref largestContribution);

        var plateServicePoints = ServiceDelta(inputs.PlateServiceScore);
        percentagePoints += plateServicePoints;
        Consider(
            FoodPoisonRiskContributor.PoorPlate,
            plateServicePoints * scale,
            ref largestContributor,
            ref largestContribution);

        var cutleryServicePoints = ServiceDelta(inputs.CutleryServiceScore);
        percentagePoints += cutleryServicePoints;
        Consider(
            FoodPoisonRiskContributor.PoorCutlery,
            cutleryServicePoints * scale,
            ref largestContributor,
            ref largestContribution);

        var microwavePoints = Math.Max(0, inputs.MicrowaveReheatCount) *
                              Math.Max(0f, inputs.MicrowaveExtraPercentagePoints) *
                              CustomRiskBalanceFactor;
        percentagePoints += microwavePoints;
        Consider(
            FoodPoisonRiskContributor.MicrowaveReheating,
            microwavePoints * scale,
            ref largestContributor,
            ref largestContribution);

        // The compatible vanilla probability participates in cause attribution, but is deliberately
        // considered after the physical/custom table so exact ties retain the table's severity order.
        Consider(
            FoodPoisonRiskContributor.VanillaBase,
            baseChance * 100f,
            ref largestContributor,
            ref largestContribution);

        var adjusted = baseChance + ((percentagePoints / 100f) * scale);
        var finalChance = Clamp(adjusted, 0f, cap);
        if (finalChance <= 0f)
        {
            largestContributor = FoodPoisonRiskContributor.None;
            largestContribution = 0f;
        }

        return new DiningPoisonRiskResult(finalChance, largestContributor, largestContribution);
    }

    public static int QualityMoodOffset(int qualityScore)
    {
        return Clamp(qualityScore, 0, 100) switch
        {
            < 20 => -6,
            < 35 => -3,
            < 50 => 0,
            < 65 => 2,
            < 80 => 4,
            < 90 => 6,
            _ => 8
        };
    }

    public static int TemperatureMoodOffset(ThermalBand band)
    {
        return ThermalCalculator.MoodOffsetFor(band);
    }

    public static float ServiceScore(float craftsmanshipScore, float materialCleanliness)
    {
        return Clamp((0.75f * craftsmanshipScore) + (0.25f * materialCleanliness), 0f, 100f);
    }

    public static bool HasDirtyWare(ContaminationSources sources) => (sources & DirtyWare) != 0;

    private static float ServiceDelta(float? serviceScore)
    {
        return serviceScore.HasValue
            ? (50f - Clamp(serviceScore.Value, 0f, 100f)) *
              0.03f *
              CustomRiskBalanceFactor
            : 0f;
    }

    private static bool Has(ContaminationSources sources, ContaminationSources value) =>
        (sources & value) != 0;

    private static void AddContaminationCandidate(
        ContaminationSources sources,
        ContaminationSources source,
        FoodPoisonRiskContributor contributor,
        float points,
        float scale,
        ref float totalPoints,
        ref FoodPoisonRiskContributor largestContributor,
        ref float largestContribution)
    {
        if (!Has(sources, source))
        {
            return;
        }

        totalPoints += points;
        Consider(contributor, points * scale, ref largestContributor, ref largestContribution);
    }

    private static void Consider(
        FoodPoisonRiskContributor contributor,
        float contribution,
        ref FoodPoisonRiskContributor largestContributor,
        ref float largestContribution)
    {
        if (contribution <= 0f ||
            contribution < largestContribution ||
            (Math.Abs(contribution - largestContribution) < 0.0001f &&
             AttributionPriority(contributor) <= AttributionPriority(largestContributor)))
        {
            return;
        }

        largestContributor = contributor;
        largestContribution = contribution;
    }

    private static int AttributionPriority(FoodPoisonRiskContributor contributor)
    {
        return contributor switch
        {
            FoodPoisonRiskContributor.DirtyCookware => 140,
            FoodPoisonRiskContributor.DirtyPlate => 130,
            FoodPoisonRiskContributor.DirtyCutlery => 120,
            FoodPoisonRiskContributor.FrozenMeal => 110,
            FoodPoisonRiskContributor.LowCulinaryQuality => 100,
            FoodPoisonRiskContributor.MicrowaveReheating => 90,
            FoodPoisonRiskContributor.WildWaterCookware => 80,
            FoodPoisonRiskContributor.WildWaterPlate => 70,
            FoodPoisonRiskContributor.WildWaterCutlery => 60,
            FoodPoisonRiskContributor.ColdMeal => 50,
            FoodPoisonRiskContributor.PoorPlate => 40,
            FoodPoisonRiskContributor.PoorCutlery => 30,
            FoodPoisonRiskContributor.VanillaBase => 20,
            _ => 0
        };
    }

    private static int Clamp(int value, int minimum, int maximum) =>
        Math.Max(minimum, Math.Min(maximum, value));

    private static float Clamp(float value, float minimum, float maximum) =>
        Math.Max(minimum, Math.Min(maximum, value));
}
