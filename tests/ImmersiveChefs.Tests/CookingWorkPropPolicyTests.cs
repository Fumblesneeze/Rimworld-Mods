using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class CookingWorkPropPolicyTests
{
    [Test]
    public void Draw_depth_follows_the_north_south_work_direction()
    {
        var towardNorth = CookingWorkPropPolicy.AltitudeOffsetFor(
            normalizedWorkDirectionZ: 1f);
        var towardSouth = CookingWorkPropPolicy.AltitudeOffsetFor(
            normalizedWorkDirectionZ: -1f);
        var lateral = CookingWorkPropPolicy.AltitudeOffsetFor(
            normalizedWorkDirectionZ: 0f);

        Assert.Multiple(() =>
        {
            Assert.That(towardNorth, Is.LessThan(0f),
                "Cookware toward a north-side work surface must depth-sort behind the pawn.");
            Assert.That(towardSouth, Is.GreaterThan(0f),
                "Cookware toward a south-side work surface must depth-sort in front of the pawn.");
            Assert.That(towardSouth, Is.EqualTo(-towardNorth).Within(0.0001f));
            Assert.That(lateral, Is.Zero);
        });
    }

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
