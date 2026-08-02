using NUnit.Framework;
using RimWorld;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class PreparedFoodCalculationTests
{
    [Test]
    public void Animal_products_are_not_misclassified_as_plant_food()
    {
        var flags = DietaryClassification.ForFoodType(
            FoodTypeFlags.AnimalProduct,
            "EggChickenUnfertilized");

        Assert.Multiple(() =>
        {
            Assert.That(flags.HasFlag(DietaryFlags.Animal), Is.True);
            Assert.That(flags.HasFlag(DietaryFlags.Plant), Is.False);
            Assert.That(flags.HasFlag(DietaryFlags.VegetarianCompatible), Is.False);
        });
    }

    [Test]
    public void Prepared_stacks_keep_preparer_and_source_count_provenance_distinct()
    {
        var left = new CompPreparedFood();
        left.Initialize(new PreparedFoodState(
            new[] { new IngredientContribution("RawRice", 0.5f, 1) },
            60,
            "Pawn_A",
            DietaryFlags.Plant,
            false,
            0f));
        var differentPreparer = new CompPreparedFood();
        differentPreparer.Initialize(new PreparedFoodState(
            new[] { new IngredientContribution("RawRice", 0.5f, 1) },
            60,
            "Pawn_B",
            DietaryFlags.Plant,
            false,
            0f));
        var differentCount = new CompPreparedFood();
        differentCount.Initialize(new PreparedFoodState(
            new[] { new IngredientContribution("RawRice", 0.5f, 2) },
            60,
            "Pawn_A",
            DietaryFlags.Plant,
            false,
            0f));

        Assert.Multiple(() =>
        {
            Assert.That(left.CompatibleWith(differentPreparer), Is.False);
            Assert.That(left.CompatibleWith(differentCount), Is.False);
        });
    }

    [Test]
    public void Urgent_production_window_is_explicit_bounded_and_consumable()
    {
        var request = new UrgentProductionWindow(durationTicks: 600);

        request.Register(currentTick: 1000);

        Assert.That(request.IsActive(1599), Is.True);
        Assert.That(request.IsActive(1600), Is.False);
        request.Register(currentTick: 2000);
        request.Consume();
        Assert.That(request.IsActive(2001), Is.False);
    }

    [TestCase(0f, 0.4f, 1f)]
    [TestCase(0.5f, 0.4f, 0.8f)]
    [TestCase(1f, 0.4f, 0.6f)]
    [TestCase(1f, 0.75f, 0.25f)]
    public void WorkFactor_IsProportionalAndBounded(float preparedFraction, float maximumReduction, float expected)
    {
        Assert.That(PreparedFoodCalculator.WorkFactor(preparedFraction, maximumReduction), Is.EqualTo(expected).Within(0.0001f));
    }

    [TestCase(-5, 0)]
    [TestCase(0, 0)]
    [TestCase(10, 50)]
    [TestCase(20, 100)]
    [TestCase(50, 100)]
    public void SkillQuality_MapsCookingSkillToZeroThroughOneHundred(int skill, int expected)
    {
        Assert.That(PreparedFoodCalculator.SkillQuality(skill), Is.EqualTo(expected));
    }
}
