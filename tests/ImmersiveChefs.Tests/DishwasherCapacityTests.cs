using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class DishwasherCapacityTests
{
    [TestCase(16f, 0f, 1f, 50, 16)]
    [TestCase(16f, 15.5f, 0.5f, 10, 1)]
    [TestCase(16f, 16f, 1f, 1, 0)]
    public void Oversized_stack_can_be_partially_loaded(
        float capacity,
        float used,
        float perItem,
        int stackCount,
        int expected)
    {
        Assert.That(
            DishwasherCapacityPolicy.CountAccepted(capacity, used, perItem, stackCount),
            Is.EqualTo(expected));
    }
}
