using ImageMagick;
using VerifyNUnit;

namespace RimWorldDevGateway.Snapshots.Tests;

[TestFixture]
public sealed class GatewayScreenshotSnapshotCompatibilityTests
{
    static GatewayScreenshotSnapshotCompatibilityTests()
    {
        VerifyImageMagick.Initialize();
        VerifyImageMagick.RegisterComparers(threshold: 0.001);
    }

    [Test]
    public Task Verify_and_ImageMagick_run_against_the_Gateway_crop_contract()
    {
        var crop = GatewayScreenshotCropPlanner.Plan(
            frameWidth: 400,
            frameHeight: 300,
            new[]
            {
                new GatewayProjectedScreenshotTarget("Pawn_42", 50.2, 40.8, 90.1, 80.2),
                new GatewayProjectedScreenshotTarget("Building_9", 200.4, 150.6, 250.2, 190.5)
            },
            paddingPixels: 24);
        using var image = new MagickImage(
            MagickColors.Crimson,
            width: (uint)crop.Width,
            height: (uint)crop.Height)
        {
            Format = MagickFormat.Png
        };

        return Verifier.Verify(new
            {
                Runtime = ".NET Framework 4.8 host snapshot test",
                Crop = new { crop.X, crop.Y, crop.Width, crop.Height },
                Image = new
                {
                    image.Width,
                    image.Height,
                    Format = image.Format.ToString()
                },
                ImageMagickComparerThreshold = 0.001
            });
    }

    [Test]
    public Task Verify_ImageMagick_compares_a_deterministic_png()
    {
        using var image = new MagickImage(MagickColors.Crimson, width: 3, height: 2)
        {
            Format = MagickFormat.Png
        };
        using var png = new MemoryStream(image.ToByteArray());

        return Verifier.Verify(png, "png");
    }
}
