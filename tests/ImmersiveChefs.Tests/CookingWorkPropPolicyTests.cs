using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class CookingWorkPropPolicyTests
{
    [Test]
    public void Draws_only_during_active_work_with_the_exact_held_cookware()
    {
        Assert.That(CookingWorkPropPolicy.ShouldDraw(new CookingWorkPropState(
            currentJobMatches: true,
            currentDriverIsDoBill: true,
            workStarted: true,
            productsCompleted: false,
            cookwareExists: true,
            cookwareHeldByCook: true)), Is.True);
    }

    [TestCase(false, true, true, false, true, true)]
    [TestCase(true, false, true, false, true, true)]
    [TestCase(true, true, false, false, true, true)]
    [TestCase(true, true, true, true, true, true)]
    [TestCase(true, true, true, false, false, true)]
    [TestCase(true, true, true, false, true, false)]
    public void Does_not_draw_during_hauling_completion_or_interruption(
        bool currentJobMatches,
        bool currentDriverIsDoBill,
        bool workStarted,
        bool productsCompleted,
        bool cookwareExists,
        bool cookwareHeldByCook)
    {
        Assert.That(CookingWorkPropPolicy.ShouldDraw(new CookingWorkPropState(
            currentJobMatches,
            currentDriverIsDoBill,
            workStarted,
            productsCompleted,
            cookwareExists,
            cookwareHeldByCook)), Is.False);
    }
}
