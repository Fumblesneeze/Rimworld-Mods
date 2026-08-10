using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class DishwashingWorkPolicyTests
{
    [TestCase(1f, 1, 1f, 250)]
    [TestCase(4f, 1, 1f, 1000)]
    [TestCase(0.25f, 1, 1f, 125)]
    [TestCase(1f, 1, 0.25f, 63)]
    [TestCase(0.25f, 4, 1f, 500)]
    public void Handwashing_duration_scales_from_one_plate_equivalent(
        float plateEquivalentsPerItem,
        int itemCount,
        float workScale,
        int expectedTicks)
    {
        Assert.That(
            DishwashingWorkPolicy.HandwashingDurationTicks(
                plateEquivalentsPerItem,
                itemCount,
                workScale),
            Is.EqualTo(expectedTicks));
    }

    [TestCase(0f, 0, 1f)]
    [TestCase(-1f, 1, 1f)]
    [TestCase(1f, 1, 0f)]
    [TestCase(1f, 1, -1f)]
    public void Handwashing_duration_never_collapses_below_one_tick(
        float plateEquivalentsPerItem,
        int itemCount,
        float workScale)
    {
        Assert.That(
            DishwashingWorkPolicy.HandwashingDurationTicks(
                plateEquivalentsPerItem,
                itemCount,
                workScale),
            Is.EqualTo(1));
    }
}
