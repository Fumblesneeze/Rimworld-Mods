using System.Collections.Generic;

namespace RimWorldDevGateway;

public sealed class GatewayProjectedScreenshotTarget
{
    public GatewayProjectedScreenshotTarget(
        string handle,
        double minimumX,
        double minimumY,
        double maximumX,
        double maximumY)
    {
        Handle = handle ?? throw new ArgumentNullException(nameof(handle));
        MinimumX = minimumX;
        MinimumY = minimumY;
        MaximumX = maximumX;
        MaximumY = maximumY;
    }

    public string Handle { get; }

    public double MinimumX { get; }

    public double MinimumY { get; }

    public double MaximumX { get; }

    public double MaximumY { get; }
}

public sealed class GatewayScreenshotCrop
{
    public GatewayScreenshotCrop(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int X { get; }

    public int Y { get; }

    public int Width { get; }

    public int Height { get; }
}

public static class GatewayScreenshotCropPlanner
{
    public static GatewayScreenshotCrop Plan(
        int frameWidth,
        int frameHeight,
        IReadOnlyList<GatewayProjectedScreenshotTarget> targets,
        int paddingPixels)
    {
        if (frameWidth <= 0 || frameHeight <= 0)
        {
            throw new GatewayScreenshotException(
                "capture_failed",
                "The captured screenshot has invalid dimensions.");
        }

        if (targets is null)
        {
            throw new ArgumentNullException(nameof(targets));
        }

        if (targets.Count == 0)
        {
            throw new GatewayScreenshotException(
                "invalid_screenshot_request",
                "A targeted screenshot requires at least one projected target.");
        }

        if (paddingPixels < 0)
        {
            throw new GatewayScreenshotException(
                "invalid_screenshot_request",
                "paddingPixels must be non-negative.");
        }

        var unionMinimumX = double.PositiveInfinity;
        var unionMinimumY = double.PositiveInfinity;
        var unionMaximumX = double.NegativeInfinity;
        var unionMaximumY = double.NegativeInfinity;
        foreach (var target in targets)
        {
            Validate(target);
            if (target.MaximumX <= 0d ||
                target.MaximumY <= 0d ||
                target.MinimumX >= frameWidth ||
                target.MinimumY >= frameHeight)
            {
                throw new GatewayScreenshotException(
                    "screenshot_target_not_visible",
                    $"Thing '{target.Handle}' is wholly outside the rendered frame.");
            }

            unionMinimumX = Math.Min(unionMinimumX, target.MinimumX);
            unionMinimumY = Math.Min(unionMinimumY, target.MinimumY);
            unionMaximumX = Math.Max(unionMaximumX, target.MaximumX);
            unionMaximumY = Math.Max(unionMaximumY, target.MaximumY);
        }

        var left = Clamp(Math.Floor(unionMinimumX) - paddingPixels, 0d, frameWidth);
        var bottom = Clamp(Math.Floor(unionMinimumY) - paddingPixels, 0d, frameHeight);
        var right = Clamp(Math.Ceiling(unionMaximumX) + paddingPixels, 0d, frameWidth);
        var top = Clamp(Math.Ceiling(unionMaximumY) + paddingPixels, 0d, frameHeight);
        var x = (int)left;
        var y = (int)bottom;
        var width = (int)right - x;
        var height = (int)top - y;
        if (width <= 0 || height <= 0)
        {
            throw new GatewayScreenshotException(
                "screenshot_target_not_visible",
                "The requested Things do not produce a visible crop.");
        }

        return new GatewayScreenshotCrop(x, y, width, height);
    }

    private static void Validate(GatewayProjectedScreenshotTarget target)
    {
        if (target is null)
        {
            throw new GatewayScreenshotException(
                "capture_failed",
                "The screenshot projector returned a null target.");
        }

        if (!IsFinite(target.MinimumX) ||
            !IsFinite(target.MinimumY) ||
            !IsFinite(target.MaximumX) ||
            !IsFinite(target.MaximumY) ||
            target.MaximumX <= target.MinimumX ||
            target.MaximumY <= target.MinimumY)
        {
            throw new GatewayScreenshotException(
                "capture_failed",
                $"Thing '{target.Handle}' produced invalid projected bounds.");
        }
    }

    private static bool IsFinite(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value);

    private static double Clamp(double value, double minimum, double maximum) =>
        Math.Min(maximum, Math.Max(minimum, value));
}
