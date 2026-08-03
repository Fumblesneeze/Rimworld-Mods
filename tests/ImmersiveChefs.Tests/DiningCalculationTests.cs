using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class DiningCalculationTests
{
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
            Assert.That(dirtySetting, Is.EqualTo(0.42f).Within(0.0001f));
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
            Assert.That(DiningOutcomeCalculator.TemperatureMoodOffset(ThermalBand.Frozen), Is.EqualTo(-6));
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

        Assert.That(chance, Is.EqualTo(0.09f).Within(0.0001f));
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
}
