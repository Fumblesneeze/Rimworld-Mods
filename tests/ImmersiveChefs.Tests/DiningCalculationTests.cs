using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class DiningCalculationTests
{
    private static System.Collections.Generic.IEnumerable<TestCaseData> HalvedCustomRiskCases()
    {
        yield return RiskCase("low culinary quality", 0, ThermalBand.Warm, ContaminationSources.None, null, null, 0, 0.5f, 0.05f, FoodPoisonRiskContributor.LowCulinaryQuality, 5f);
        yield return RiskCase("cold meal", 50, ThermalBand.Cold, ContaminationSources.None, null, null, 0, 0.5f, 0.015f, FoodPoisonRiskContributor.ColdMeal, 1.5f);
        yield return RiskCase("frozen meal", 50, ThermalBand.Frozen, ContaminationSources.None, null, null, 0, 0.5f, 0.04f, FoodPoisonRiskContributor.FrozenMeal, 4f);
        yield return RiskCase("dirty cookware", 50, ThermalBand.Warm, ContaminationSources.DirtyCookware, null, null, 0, 0.5f, 0.075f, FoodPoisonRiskContributor.DirtyCookware, 7.5f);
        yield return RiskCase("dirty plate", 50, ThermalBand.Warm, ContaminationSources.DirtyPlate, null, null, 0, 0.5f, 0.075f, FoodPoisonRiskContributor.DirtyPlate, 7.5f);
        yield return RiskCase("dirty cutlery", 50, ThermalBand.Warm, ContaminationSources.DirtyCutlery, null, null, 0, 0.5f, 0.05f, FoodPoisonRiskContributor.DirtyCutlery, 5f);
        yield return RiskCase("wild-water cookware", 50, ThermalBand.Warm, ContaminationSources.WildWaterCookware, null, null, 0, 0.5f, 0.025f, FoodPoisonRiskContributor.WildWaterCookware, 2.5f);
        yield return RiskCase("wild-water plate", 50, ThermalBand.Warm, ContaminationSources.WildWaterPlate, null, null, 0, 0.5f, 0.02f, FoodPoisonRiskContributor.WildWaterPlate, 2f);
        yield return RiskCase("wild-water cutlery", 50, ThermalBand.Warm, ContaminationSources.WildWaterCutlery, null, null, 0, 0.5f, 0.015f, FoodPoisonRiskContributor.WildWaterCutlery, 1.5f);
        yield return RiskCase("poor plate", 50, ThermalBand.Warm, ContaminationSources.None, 0f, null, 0, 0.5f, 0.0075f, FoodPoisonRiskContributor.PoorPlate, 0.75f);
        yield return RiskCase("poor cutlery", 50, ThermalBand.Warm, ContaminationSources.None, null, 0f, 0, 0.5f, 0.0075f, FoodPoisonRiskContributor.PoorCutlery, 0.75f);
        yield return RiskCase("microwave reheat", 50, ThermalBand.Warm, ContaminationSources.None, null, null, 1, 0.5f, 0.0025f, FoodPoisonRiskContributor.MicrowaveReheating, 0.25f);
    }

    [TestCaseSource(nameof(HalvedCustomRiskCases))]
    public void Every_custom_food_poisoning_contributor_uses_half_the_released_baseline_once(
        DiningRiskInputs inputs,
        float expectedChance,
        FoodPoisonRiskContributor expectedContributor,
        float expectedContributionPercentagePoints)
    {
        var result = DiningOutcomeCalculator.CalculatePoisonRisk(inputs);

        Assert.Multiple(() =>
        {
            Assert.That(result.FinalChance, Is.EqualTo(expectedChance).Within(0.0001f));
            Assert.That(result.LargestPositiveContributor, Is.EqualTo(expectedContributor));
            Assert.That(result.LargestPositiveContributionPercentagePoints,
                Is.EqualTo(expectedContributionPercentagePoints).Within(0.0001f));
        });
    }

    [Test]
    public void Halved_balance_applies_once_to_combined_and_favorable_custom_deltas_but_not_the_base()
    {
        var dirtySetting = DiningOutcomeCalculator.FinalPoisonChance(new DiningRiskInputs(
            0.02f, 50, ThermalBand.Warm,
            ContaminationSources.DirtyCookware | ContaminationSources.DirtyPlate | ContaminationSources.DirtyCutlery,
            null, null, 0, 0.5f, 1f, 0.50f));
        var favorableQuality = DiningOutcomeCalculator.FinalPoisonChance(new DiningRiskInputs(
            0.20f, 80, ThermalBand.Warm, ContaminationSources.None,
            null, null, 0, 0.5f, 1f, 0.50f));
        var favorablePlate = DiningOutcomeCalculator.FinalPoisonChance(new DiningRiskInputs(
            0.20f, 50, ThermalBand.Warm, ContaminationSources.None,
            100f, null, 0, 0.5f, 1f, 0.50f));

        Assert.Multiple(() =>
        {
            Assert.That(dirtySetting, Is.EqualTo(0.22f).Within(0.0001f));
            Assert.That(favorableQuality, Is.EqualTo(0.17f).Within(0.0001f));
            Assert.That(favorablePlate, Is.EqualTo(0.1925f).Within(0.0001f));
        });
    }

    [Test]
    public void Poisoning_combines_one_bounded_roll_without_lowering_an_already_higher_base_risk()
    {
        var dirtySetting = DiningOutcomeCalculator.FinalPoisonChance(new DiningRiskInputs(
            baseChance: 0.02f,
            qualityScore: 50,
            thermalBand: ThermalBand.Warm,
            contamination: ContaminationSources.DirtyCookware | ContaminationSources.DirtyPlate | ContaminationSources.DirtyCutlery,
            plateServiceScore: null,
            cutleryServiceScore: null,
            microwaveReheatCount: 0,
            microwaveExtraPercentagePoints: 0.5f,
            effectScale: 1f,
            maximumChance: 0.50f));
        var capped = DiningOutcomeCalculator.FinalPoisonChance(new DiningRiskInputs(
            0.02f, 0, ThermalBand.Frozen,
            ContaminationSources.DirtyCookware | ContaminationSources.DirtyPlate | ContaminationSources.DirtyCutlery,
            0, 0, 3, 5f, 3f, 0.50f));
        var highCompatibleBase = DiningOutcomeCalculator.FinalPoisonChance(new DiningRiskInputs(
            0.72f, 100, ThermalBand.Warm, ContaminationSources.None,
            100, 100, 0, 0.5f, 1f, 0.50f));

        Assert.Multiple(() =>
        {
            Assert.That(dirtySetting, Is.EqualTo(0.22f).Within(0.0001f));
            Assert.That(capped, Is.EqualTo(0.50f).Within(0.0001f));
            Assert.That(highCompatibleBase, Is.EqualTo(0.72f).Within(0.0001f));
        });
    }

    [Test]
    public void Dining_mood_and_service_scores_follow_the_contract_tables()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DiningOutcomeCalculator.QualityMoodOffset(72), Is.EqualTo(4));
            Assert.That(DiningOutcomeCalculator.TemperatureMoodOffset(ThermalBand.Cold), Is.EqualTo(-3));
            Assert.That(DiningOutcomeCalculator.TemperatureMoodOffset(ThermalBand.Frozen), Is.EqualTo(-10));
            Assert.That(ThermalCalculator.EatingDurationMultiplier(ThermalBand.Cold), Is.EqualTo(1f));
            Assert.That(ThermalCalculator.EatingDurationMultiplier(ThermalBand.Frozen), Is.EqualTo(1.5f));
            Assert.That(DiningOutcomeCalculator.ServiceScore(80, 60), Is.EqualTo(75f));
            Assert.That(DiningOutcomeCalculator.HasDirtyWare(
                ContaminationSources.DirtyPlate | ContaminationSources.DirtyCutlery), Is.True);
        });
    }

    [Test]
    public void Wild_water_plate_and_cutlery_add_the_specified_bounded_risk()
    {
        var chance = DiningOutcomeCalculator.FinalPoisonChance(new DiningRiskInputs(
            baseChance: 0.02f,
            qualityScore: 50,
            thermalBand: ThermalBand.Warm,
            contamination: ContaminationSources.WildWaterPlate | ContaminationSources.WildWaterCutlery,
            plateServiceScore: null,
            cutleryServiceScore: null,
            microwaveReheatCount: 0,
            microwaveExtraPercentagePoints: 0.5f,
            effectScale: 1f,
            maximumChance: 0.50f));

        Assert.That(chance, Is.EqualTo(0.055f).Within(0.0001f));
    }

    [Test]
    public void Sanitation_state_maps_to_both_dirty_and_wild_water_contamination()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                SanitationContamination.ForCookware(isDirty: true, WashProvenance.WildWater),
                Is.EqualTo(ContaminationSources.DirtyCookware | ContaminationSources.WildWaterCookware));
            Assert.That(
                SanitationContamination.ForPlate(isDirty: false, WashProvenance.WildWater),
                Is.EqualTo(ContaminationSources.WildWaterPlate));
            Assert.That(
                SanitationContamination.ForCutlery(isDirty: true, WashProvenance.Safe),
                Is.EqualTo(ContaminationSources.DirtyCutlery));
        });
    }

    [Test]
    public void Poisoning_reports_the_largest_scaled_positive_contributor_without_changing_probability()
    {
        var inputs = new DiningRiskInputs(
            baseChance: 0.02f,
            qualityScore: 0,
            thermalBand: ThermalBand.Frozen,
            contamination: ContaminationSources.DirtyCookware |
                           ContaminationSources.DirtyPlate |
                           ContaminationSources.WildWaterCutlery,
            plateServiceScore: 0f,
            cutleryServiceScore: 100f,
            microwaveReheatCount: 2,
            microwaveExtraPercentagePoints: 5f,
            effectScale: 2f,
            maximumChance: 1f);

        var result = DiningOutcomeCalculator.CalculatePoisonRisk(inputs);

        Assert.Multiple(() =>
        {
            Assert.That(result.FinalChance,
                Is.EqualTo(DiningOutcomeCalculator.FinalPoisonChance(inputs)).Within(0.0001f));
            Assert.That(result.FinalChance, Is.EqualTo(0.63f).Within(0.0001f));
            Assert.That(result.LargestPositiveContributor,
                Is.EqualTo(FoodPoisonRiskContributor.DirtyCookware));
            Assert.That(result.LargestPositiveContributionPercentagePoints,
                Is.EqualTo(15f).Within(0.0001f));
        });
    }

    [Test]
    public void Poisoning_ties_use_physical_risk_priority_and_vanilla_base_remains_eligible()
    {
        var physicalTie = DiningOutcomeCalculator.CalculatePoisonRisk(new DiningRiskInputs(
            0f, 50, ThermalBand.Warm,
            ContaminationSources.DirtyCookware | ContaminationSources.DirtyPlate,
            null, null, 0, 0.5f, 1f, 0.50f));
        var vanillaDominates = DiningOutcomeCalculator.CalculatePoisonRisk(new DiningRiskInputs(
            0.30f, 50, ThermalBand.Warm,
            ContaminationSources.DirtyCookware,
            null, null, 0, 0.5f, 1f, 0.50f));
        var physicalTieBeatsQuality = DiningOutcomeCalculator.CalculatePoisonRisk(new DiningRiskInputs(
            0f, 0, ThermalBand.Warm,
            ContaminationSources.DirtyCutlery,
            null, null, 0, 0.5f, 1f, 0.50f));
        var physicalTieBeatsBase = DiningOutcomeCalculator.CalculatePoisonRisk(new DiningRiskInputs(
            0.075f, 50, ThermalBand.Warm,
            ContaminationSources.DirtyCookware,
            null, null, 0, 0.5f, 1f, 0.50f));

        Assert.Multiple(() =>
        {
            Assert.That(physicalTie.LargestPositiveContributor,
                Is.EqualTo(FoodPoisonRiskContributor.DirtyCookware));
            Assert.That(vanillaDominates.LargestPositiveContributor,
                Is.EqualTo(FoodPoisonRiskContributor.VanillaBase));
            Assert.That(vanillaDominates.FinalChance, Is.EqualTo(0.375f).Within(0.0001f));
            Assert.That(physicalTieBeatsQuality.LargestPositiveContributor,
                Is.EqualTo(FoodPoisonRiskContributor.DirtyCutlery));
            Assert.That(physicalTieBeatsBase.LargestPositiveContributor,
                Is.EqualTo(FoodPoisonRiskContributor.DirtyCookware));
        });
    }

    [Test]
    public void Disabled_custom_risk_does_not_claim_a_custom_cause()
    {
        var result = DiningOutcomeCalculator.CalculatePoisonRisk(new DiningRiskInputs(
            0f, 0, ThermalBand.Frozen,
            ContaminationSources.DirtyCookware | ContaminationSources.DirtyPlate,
            0f, 0f, 4, 5f, 0f, 0.50f));

        Assert.Multiple(() =>
        {
            Assert.That(result.FinalChance, Is.Zero);
            Assert.That(result.LargestPositiveContributor,
                Is.EqualTo(FoodPoisonRiskContributor.None));
            Assert.That(result.LargestPositiveContributionPercentagePoints, Is.Zero);
        });
    }

    private static TestCaseData RiskCase(
        string name,
        int qualityScore,
        ThermalBand thermalBand,
        ContaminationSources contamination,
        float? plateServiceScore,
        float? cutleryServiceScore,
        int microwaveReheatCount,
        float microwaveExtraPercentagePoints,
        float expectedChance,
        FoodPoisonRiskContributor expectedContributor,
        float expectedContributionPercentagePoints)
    {
        return new TestCaseData(
                new DiningRiskInputs(
                    0f,
                    qualityScore,
                    thermalBand,
                    contamination,
                    plateServiceScore,
                    cutleryServiceScore,
                    microwaveReheatCount,
                    microwaveExtraPercentagePoints,
                    1f,
                    1f),
                expectedChance,
                expectedContributor,
                expectedContributionPercentagePoints)
            .SetName($"Custom_food_poisoning_risk_is_halved_for_{name.Replace(' ', '_')}");
    }

}
