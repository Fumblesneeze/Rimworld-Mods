using System;
using System.Collections.Generic;

namespace Megamod.Features.MessagePlacement;

// All inputs describe the current prepared native presentation, never another pawn census.
public readonly record struct MessageBarEntryGeometry(MessageBounds Portrait, bool HasPawn,
    double LabelExtent, bool WeaponVisible, double MoodEdge, double? RemainingSelectionJump);

public static class MessageBarGeometry
{
    public static bool TryMeasure(IReadOnlyList<MessageBarEntryGeometry> entries, double scale,
        bool showGroupFrames, out double? bottom)
    {
        bottom = null;
        if (entries == null || !double.IsFinite(scale) || scale <= 0) return false;
        foreach (var entry in entries)
        {
            var rect = entry.Portrait;
            if (!double.IsFinite(rect.X) || !double.IsFinite(rect.Y) || !double.IsFinite(rect.Width) ||
                !double.IsFinite(rect.Height) || rect.Width <= 0 || rect.Height <= 0 ||
                !double.IsFinite(entry.LabelExtent) || entry.LabelExtent < 0 ||
                !double.IsFinite(entry.MoodEdge) || entry.MoodEdge < 0 ||
                entry.RemainingSelectionJump is double remaining && (!double.IsFinite(remaining) || remaining < 0 || remaining > 1))
                return false;
        }
        foreach (var entry in entries)
        {
            if (showGroupFrames)
            {
                double frameBottom = entry.Portrait.Y + entry.Portrait.Height + 12 * scale;
                bottom = bottom.HasValue ? Math.Max(bottom.Value, frameBottom) : frameBottom;
            }
            if (!entry.HasPawn) continue;
            double edge = entry.Portrait.Y + entry.Portrait.Height;
            edge = Math.Max(edge, edge - 4 * scale + entry.LabelExtent);
            if (entry.WeaponVisible)
                edge = Math.Max(edge, entry.Portrait.Y + 1.925 * entry.Portrait.Height);
            edge = Math.Max(edge, entry.Portrait.Y + entry.Portrait.Height + entry.MoodEdge);
            if (entry.RemainingSelectionJump is double remaining)
                edge = Math.Max(edge, entry.Portrait.Y + entry.Portrait.Height + (2 + 4 * remaining) * scale);
            bottom = bottom.HasValue ? Math.Max(bottom.Value, edge) : edge;
        }
        if (bottom.HasValue && !double.IsFinite(bottom.Value)) { bottom = null; return false; }
        return true;
    }
}
