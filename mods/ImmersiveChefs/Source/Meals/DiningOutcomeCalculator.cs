namespace ImmersiveChefs;

public readonly struct DiningRiskInputs
{
    public DiningRiskInputs(
        float baseChance,
        int qualityScore,
        ThermalBand thermalBand,
        ContaminationSources contamination,
        float? plateServiceScore,
        float? silverwareServiceScore,
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
        SilverwareServiceScore = silverwareServiceScore;
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
    public float? SilverwareServiceScore { get; }
    public int MicrowaveReheatCount { get; }
    public float MicrowaveExtraPercentagePoints { get; }
    public float EffectScale { get; }
    public float MaximumChance { get; }
}

public static class DiningOutcomeCalculator
{
    private const ContaminationSources DirtyWare =
        ContaminationSources.DirtyCookware |
        ContaminationSources.DirtyPlate |
        ContaminationSources.DirtySilverware;

    public static float FinalPoisonChance(DiningRiskInputs inputs)
    {
        var baseChance = Clamp(inputs.BaseChance, 0f, 1f);
        var cap = Clamp(inputs.MaximumChance, 0f, 1f);
        if (baseChance > cap)
        {
            return baseChance;
        }

        var percentagePoints = (50 - Clamp(inputs.QualityScore, 0, 100)) * 0.20f;
        percentagePoints += inputs.ThermalBand switch
        {
            ThermalBand.Cold => 3f,
            ThermalBand.Frozen => 8f,
            _ => 0f
        };
        percentagePoints += Has(inputs.Contamination, ContaminationSources.DirtyCookware) ? 15f : 0f;
        percentagePoints += Has(inputs.Contamination, ContaminationSources.DirtyPlate) ? 15f : 0f;
        percentagePoints += Has(inputs.Contamination, ContaminationSources.DirtySilverware) ? 10f : 0f;
        percentagePoints += Has(inputs.Contamination, ContaminationSources.WildWaterCookware) ? 5f : 0f;
        percentagePoints += Has(inputs.Contamination, ContaminationSources.WildWaterPlate) ? 4f : 0f;
        percentagePoints += Has(inputs.Contamination, ContaminationSources.WildWaterSilverware) ? 3f : 0f;
        percentagePoints += ServiceDelta(inputs.PlateServiceScore);
        percentagePoints += ServiceDelta(inputs.SilverwareServiceScore);
        percentagePoints += Math.Max(0, inputs.MicrowaveReheatCount) *
                            Math.Max(0f, inputs.MicrowaveExtraPercentagePoints);

        var adjusted = baseChance + ((percentagePoints / 100f) * Math.Max(0f, inputs.EffectScale));
        return Clamp(adjusted, 0f, cap);
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
        return band switch
        {
            ThermalBand.SteamingHot => 2,
            ThermalBand.Warm => 1,
            ThermalBand.Cold => -3,
            ThermalBand.Frozen => -6,
            _ => 0
        };
    }

    public static float ServiceScore(float craftsmanshipScore, float materialCleanliness)
    {
        return Clamp((0.75f * craftsmanshipScore) + (0.25f * materialCleanliness), 0f, 100f);
    }

    public static bool HasDirtyWare(ContaminationSources sources) => (sources & DirtyWare) != 0;

    private static float ServiceDelta(float? serviceScore)
    {
        return serviceScore.HasValue
            ? (50f - Clamp(serviceScore.Value, 0f, 100f)) * 0.03f
            : 0f;
    }

    private static bool Has(ContaminationSources sources, ContaminationSources value) =>
        (sources & value) != 0;

    private static int Clamp(int value, int minimum, int maximum) =>
        Math.Max(minimum, Math.Min(maximum, value));

    private static float Clamp(float value, float minimum, float maximum) =>
        Math.Max(minimum, Math.Min(maximum, value));
}
