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

    [TestCase(0, false, true)]
    [TestCase(250, false, false)]
    [TestCase(0, true, false)]
    public void Additional_ware_can_join_only_before_cleaning_or_water_debit_starts(
        int progressTicks,
        bool waterDebited,
        bool expected)
    {
        Assert.That(
            DishwasherCyclePolicy.CanAcceptAdditionalWare(progressTicks, waterDebited),
            Is.EqualTo(expected));
    }

    [Test]
    public void Every_admission_reopens_one_bounded_loading_window()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DishwasherCyclePolicy.ResetLoadingWindow(500), Is.EqualTo(500));
            Assert.That(DishwasherCyclePolicy.AdvanceLoadingWindow(500, 250), Is.EqualTo(250));
            Assert.That(DishwasherCyclePolicy.AdvanceLoadingWindow(250, 250), Is.Zero);
            Assert.That(DishwasherCyclePolicy.AdvanceLoadingWindow(0, 250), Is.Zero);
        });
    }
}
