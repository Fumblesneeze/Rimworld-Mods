using System;
using System.Collections.Generic;

namespace Megamod.Features.MessagePlacement;

public readonly record struct MessageBounds(double X, double Y, double Width, double Height);
public readonly record struct MessageTranslation(double X, double Y);
public readonly record struct MessageOffsets(double HorizontalPercent, double VerticalPercent)
{
    public static string? Validate(MessageOffsets value) =>
        double.IsFinite(value.HorizontalPercent) && double.IsFinite(value.VerticalPercent) &&
        value.HorizontalPercent is >= -100 and <= 100 && value.VerticalPercent is >= -100 and <= 100
            ? null : "Megamod_Messages_InvalidOffsetPair";
}

// Bounds already contain native padding, baseline, lesson adjustment and relative spacing.
public static class MessagePlacement
{
    public const double BarClearance = 4;

    public static bool TryCalculate(IReadOnlyList<MessageBounds> newestFirst, double viewportWidth,
        double viewportHeight, double? visibleBarBottom, MessageOffsets offsets, out MessageTranslation translation)
    {
        translation = default;
        if (newestFirst == null || MessageOffsets.Validate(offsets) != null ||
            !double.IsFinite(viewportWidth) || viewportWidth <= 0 ||
            !double.IsFinite(viewportHeight) || viewportHeight <= 0 ||
            (visibleBarBottom.HasValue && !double.IsFinite(visibleBarBottom.Value))) return false;
        if (newestFirst.Count == 0) return true;
        foreach (var bounds in newestFirst)
            if (!double.IsFinite(bounds.X) || !double.IsFinite(bounds.Y) ||
                !double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height) ||
                bounds.Width < 0 || bounds.Height < 0 ||
                !double.IsFinite(bounds.X + bounds.Width) || !double.IsFinite(bounds.Y + bounds.Height)) return false;
        double automaticY = visibleBarBottom.HasValue
            ? Math.Max(0, visibleBarBottom.Value + BarClearance - newestFirst[0].Y) : 0;
        double left = newestFirst[0].X, top = newestFirst[0].Y;
        double right = left + newestFirst[0].Width, bottom = top + newestFirst[0].Height;
        foreach (var bounds in newestFirst)
        {
            left = Math.Min(left, bounds.X); top = Math.Min(top, bounds.Y);
            right = Math.Max(right, bounds.X + bounds.Width); bottom = Math.Max(bottom, bounds.Y + bounds.Height);
        }
        double x = viewportWidth * (offsets.HorizontalPercent / 100);
        double y = automaticY + viewportHeight * (offsets.VerticalPercent / 100);
        x = right - left <= viewportWidth ? Math.Clamp(x, -left, viewportWidth - right) : -left;
        if (bottom - top <= viewportHeight) y = Math.Clamp(y, -top, viewportHeight - bottom);
        else
        {
            var newest = newestFirst[0];
            y = newest.Height <= viewportHeight
                ? Math.Clamp(y, -newest.Y, viewportHeight - newest.Y - newest.Height) : -newest.Y;
        }
        if (!double.IsFinite(x) || !double.IsFinite(y)) return false;
        translation = new(x, y);
        return true;
    }
}
