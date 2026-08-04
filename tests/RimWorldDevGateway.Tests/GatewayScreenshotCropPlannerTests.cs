using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayScreenshotCropPlannerTests
{
    [Test]
    public void Plan_unions_projected_targets_then_applies_padding_and_clamps_to_the_frame()
    {
        var targets = new[]
        {
            new GatewayProjectedScreenshotTarget("Pawn_42", 50.2, 40.8, 90.1, 80.2),
            new GatewayProjectedScreenshotTarget("Building_9", 200.4, 150.6, 250.2, 190.5)
        };

        var crop = GatewayScreenshotCropPlanner.Plan(
            frameWidth: 400,
            frameHeight: 300,
            targets,
            paddingPixels: 24);

        Assert.Multiple(() =>
        {
            Assert.That(crop.X, Is.EqualTo(26));
            Assert.That(crop.Y, Is.EqualTo(16));
            Assert.That(crop.Width, Is.EqualTo(249));
            Assert.That(crop.Height, Is.EqualTo(199));
        });
    }

    [Test]
    public void Plan_rejects_the_complete_request_when_one_target_is_wholly_off_screen()
    {
        var targets = new[]
        {
            new GatewayProjectedScreenshotTarget("Pawn_42", 50, 40, 90, 80),
            new GatewayProjectedScreenshotTarget("Building_9", 401, 10, 450, 60)
        };

        var exception = Assert.Throws<GatewayScreenshotException>(() =>
            GatewayScreenshotCropPlanner.Plan(400, 300, targets, paddingPixels: 24));

        Assert.That(exception!.Code, Is.EqualTo("screenshot_target_not_visible"));
    }

    [Test]
    public void Plan_handles_maximum_integer_padding_without_overflow()
    {
        var targets = new[]
        {
            new GatewayProjectedScreenshotTarget("Pawn_42", 50, 40, 90, 80)
        };

        var crop = GatewayScreenshotCropPlanner.Plan(
            400,
            300,
            targets,
            paddingPixels: int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(crop.X, Is.Zero);
            Assert.That(crop.Y, Is.Zero);
            Assert.That(crop.Width, Is.EqualTo(400));
            Assert.That(crop.Height, Is.EqualTo(300));
        });
    }
}
