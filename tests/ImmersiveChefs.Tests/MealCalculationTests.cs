using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class MealCalculationTests
{
    [Test]
    public void Weighted_quality_is_bounded_and_uses_the_contract_bands()
    {
        var maximum = CulinaryQualityCalculator.Calculate(new CulinaryQualityInputs(
            leadSkill: 100, ingredientDiversity: 100, ingredientCraftsmanship: 100,
            preparationQuality: 100, cookware: 100, knife: 100, assistants: 100));
        var ordinary = CulinaryQualityCalculator.Calculate(new CulinaryQualityInputs(
            leadSkill: 50, ingredientDiversity: 40, ingredientCraftsmanship: 50,
            preparationQuality: 50, cookware: 50, knife: 0, assistants: 0));

        Assert.Multiple(() =>
        {
            Assert.That(maximum, Is.EqualTo(100));
            Assert.That(ordinary, Is.EqualTo(39));
            Assert.That(CulinaryQualityCalculator.LabelFor(19), Is.EqualTo("Awful"));
            Assert.That(CulinaryQualityCalculator.LabelFor(72), Is.EqualTo("Excellent"));
            Assert.That(CulinaryQualityCalculator.LabelFor(100), Is.EqualTo("Legendary"));
        });
    }

    [Test]
    public void Thermal_progression_uses_refrigeration_rates_and_exact_bands()
    {
        const int oneHour = 2500;
        var room = ThermalCalculator.TemperatureAfter(70f, 21f, oneHour, 2f);
        var refrigerated = ThermalCalculator.TemperatureAfter(70f, 5f, oneHour, 2f);
        var frozen = ThermalCalculator.TemperatureAfter(70f, -5f, oneHour, 2f);

        Assert.Multiple(() =>
        {
            Assert.That(room, Is.EqualTo(55.648f).Within(0.01f));
            Assert.That(refrigerated, Is.EqualTo(37.5f).Within(0.01f));
            Assert.That(frozen, Is.EqualTo(13.75f).Within(0.01f));
            Assert.That(ThermalCalculator.BandFor(55f), Is.EqualTo(ThermalBand.SteamingHot));
            Assert.That(ThermalCalculator.BandFor(14.9f), Is.EqualTo(ThermalBand.Cold));
            Assert.That(ThermalCalculator.BandFor(0f), Is.EqualTo(ThermalBand.Frozen));
        });
    }
}
