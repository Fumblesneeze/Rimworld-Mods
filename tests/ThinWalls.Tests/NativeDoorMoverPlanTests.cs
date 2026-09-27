using NUnit.Framework;
using ThinWalls.Rendering;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class NativeDoorMoverPlanTests
{
    [Test]
    public void ClosedLeavesFillTheOpeningBetweenFlushFramesUsingMeasuredCoreAlpha()
    {
        NativeDoorMoverPlan plan = NativeDoorMoverPlan.Compile(0f);
        Assert.That(plan.Left.LongitudinalMin, Is.EqualTo(-0.45f).Within(0.00001f));
        Assert.That(plan.Left.LongitudinalMax, Is.EqualTo(0f).Within(0.00001f));
        Assert.That(plan.Right.LongitudinalMin, Is.EqualTo(0f).Within(0.00001f));
        Assert.That(plan.Right.LongitudinalMax, Is.EqualTo(0.45f).Within(0.00001f));
        Assert.That(plan.Left.UAtMin, Is.EqualTo(0f).Within(0.00001f));
        Assert.That(plan.Left.UAtMax, Is.EqualTo(34f / 64f).Within(0.00001f));
        Assert.That(plan.Right.UAtMin, Is.EqualTo(34f / 64f).Within(0.00001f));
        Assert.That(plan.Right.UAtMax, Is.EqualTo(0f).Within(0.00001f));
        Assert.That(plan.Left.VMin, Is.EqualTo(10f / 64f).Within(0.00001f));
        Assert.That(plan.Left.VMax, Is.EqualTo(54f / 64f).Within(0.00001f));
        Assert.That(plan.NormalMin, Is.EqualTo(-17f / 60f).Within(0.00001f));
        Assert.That(plan.NormalMax, Is.EqualTo(17f / 60f).Within(0.00001f));
    }

    [Test]
    public void FullyOpenLeavesAreClippedInsideTheFixedOpening()
    {
        NativeDoorMoverPlan plan = NativeDoorMoverPlan.Compile(1f);
        Assert.That(plan.Left.LongitudinalMin, Is.EqualTo(-0.45f).Within(0.00001f));
        Assert.That(plan.Left.LongitudinalMax, Is.EqualTo(-0.30f).Within(0.00001f));
        Assert.That(plan.Right.LongitudinalMin, Is.EqualTo(0.30f).Within(0.00001f));
        Assert.That(plan.Right.LongitudinalMax, Is.EqualTo(0.45f).Within(0.00001f));
        Assert.That(plan.Left.UAtMin, Is.EqualTo((2f / 3f) * (34f / 64f)).Within(0.00001f));
        Assert.That(plan.Right.UAtMax, Is.EqualTo((2f / 3f) * (34f / 64f)).Within(0.00001f));
        Assert.That(plan.OpeningWidth, Is.EqualTo(0.60f).Within(0.00001f));
    }

    [TestCase(-1f)]
    [TestCase(2f)]
    public void OpenFractionIsClampedAndNeverMovesPixelsOutsideTheDoorEdge(float open)
    {
        NativeDoorMoverPlan plan = NativeDoorMoverPlan.Compile(open);
        Assert.That(plan.Left.LongitudinalMin, Is.GreaterThanOrEqualTo(-0.45001f));
        Assert.That(plan.Right.LongitudinalMax, Is.LessThanOrEqualTo(0.45001f));
        Assert.That(plan.Left.LongitudinalMax, Is.LessThanOrEqualTo(plan.Right.LongitudinalMin));
    }
}
