using NUnit.Framework;
using RimWorld;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class KitchenwareStatCalculatorTests
{
    [Test]
    public void Glitterworld_cookware_has_the_promised_self_cleaning_tool_profile()
    {
        var stats = KitchenwareStatCalculator.Calculate(
            KitchenMaterialKind.Glitterworld,
            KitchenwareProduct.Cookware,
            QualityCategory.Good);

        Assert.Multiple(() =>
        {
            Assert.That(stats.MaterialCleanliness, Is.EqualTo(100f));
            Assert.That(stats.CookingSpeedFactor, Is.EqualTo(1.35f));
            Assert.That(stats.Comfort, Is.EqualTo(0.90f));
            Assert.That(stats.MaterialCulinaryOffset, Is.EqualTo(15));
        });
    }

    [TestCase(QualityCategory.Awful, QualityCategory.Good)]
    [TestCase(QualityCategory.Normal, QualityCategory.Good)]
    [TestCase(QualityCategory.Excellent, QualityCategory.Excellent)]
    public void Glitterworld_cookware_never_generates_below_good(
        QualityCategory generated,
        QualityCategory expected)
    {
        Assert.That(GlitterworldQualityPolicy.Clamp(generated), Is.EqualTo(expected));
    }

    [Test]
    public void Material_profile_and_craftsmanship_produce_visible_bounded_tool_stats()
    {
        var primitive = KitchenwareStatCalculator.Calculate(
            KitchenMaterialKind.PrimitiveStone,
            KitchenwareProduct.Cookware,
            QualityCategory.Normal);
        var legendarySteel = KitchenwareStatCalculator.Calculate(
            KitchenMaterialKind.Steel,
            KitchenwareProduct.Cookware,
            QualityCategory.Legendary);
        var lead = KitchenwareStatCalculator.Calculate(
            KitchenMaterialKind.Lead,
            KitchenwareProduct.Cookware,
            QualityCategory.Normal);
        var stainless = KitchenwareStatCalculator.Calculate(
            KitchenMaterialKind.StainlessSteel,
            KitchenwareProduct.Cookware,
            QualityCategory.Normal);

        Assert.Multiple(() =>
        {
            Assert.That(primitive.MaterialCleanliness, Is.EqualTo(15));
            Assert.That(primitive.CookingSpeedFactor, Is.EqualTo(0.60f));
            Assert.That(primitive.Comfort, Is.EqualTo(0.10f));
            Assert.That(primitive.CulinaryQualityModifier, Is.EqualTo(-12));
            Assert.That(legendarySteel.CraftsmanshipOffset, Is.EqualTo(15));
            Assert.That(legendarySteel.CulinaryToolScore, Is.EqualTo(80));
            Assert.That(lead.MaterialCleanliness, Is.LessThan(stainless.MaterialCleanliness));
            Assert.That(lead.CulinaryQualityModifier, Is.LessThan(stainless.CulinaryQualityModifier));
        });
    }

    [Test]
    public void Plate_material_changes_eating_speed_but_not_the_cooking_tool_score()
    {
        var plate = KitchenwareStatCalculator.Calculate(
            KitchenMaterialKind.StainlessSteel,
            KitchenwareProduct.Plate,
            QualityCategory.Legendary);

        Assert.Multiple(() =>
        {
            Assert.That(plate.CookingSpeedFactor, Is.EqualTo(1.15f));
            Assert.That(plate.CulinaryQualityModifier, Is.EqualTo(0));
            Assert.That(plate.CulinaryToolScore, Is.EqualTo(50));
        });
    }
}
