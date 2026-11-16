using System;
using System.Collections.Generic;

namespace Megamod.Settings;

public enum SettingsRowRevealStatus { Idle, Pending, Highlighting, Expired, Unavailable, Cancelled }
public readonly record struct SettingsRowRevealPlacement(string Id, float Top, float Height);
public readonly record struct SettingsRowRevealFrame(float ScrollY, string? HighlightedRowId);

// The renderer supplies its current measured layout and monotonic time; no editor is invoked here.
public sealed class SettingsRowReveal
{
    private string? rowId;
    private long requestedActivation;
    private double? highlightedAt;
    public SettingsRowRevealStatus Status { get; private set; }

    public SettingsRowRevealStatus Request(string rowId, long activation)
    {
        this.rowId = rowId;
        requestedActivation = activation;
        highlightedAt = null;
        Status = SettingsRowRevealStatus.Pending;
        return Status;
    }

    public SettingsRowRevealFrame Advance(long activation, IReadOnlyList<SettingsRowRevealPlacement> layout,
        float scrollY, float viewportHeight, float contentHeight, double nowSeconds, bool isRepaint)
    {
        if (Status is not (SettingsRowRevealStatus.Pending or SettingsRowRevealStatus.Highlighting))
            return new(scrollY, null);
        if (activation != requestedActivation)
        {
            Cancel();
            return new(scrollY, null);
        }
        if (!isRepaint) return new(scrollY, null);
        SettingsRowRevealFrame Refuse()
        {
            this.Refuse();
            return new(scrollY, null);
        }
        if (!float.IsFinite(scrollY) || scrollY < 0 || !float.IsFinite(viewportHeight) || viewportHeight <= 0 ||
            !float.IsFinite(contentHeight) || contentHeight < 0 || !float.IsFinite(scrollY + viewportHeight) ||
            !double.IsFinite(nowSeconds) || nowSeconds < 0)
            return Refuse();
        if (highlightedAt.HasValue && nowSeconds - highlightedAt.Value >= 3)
        {
            Status = SettingsRowRevealStatus.Expired;
            return new(scrollY, null);
        }
        SettingsRowRevealPlacement? match = null;
        foreach (var placement in layout)
        {
            if (placement.Id != rowId) continue;
            if (match.HasValue) return Refuse();
            match = placement;
        }
        if (!match.HasValue) return Refuse();
        var row = match.Value;
        float bottom = row.Top + row.Height;
        if (!float.IsFinite(row.Top) || row.Top < 0 || !float.IsFinite(row.Height) || row.Height <= 0 ||
            !float.IsFinite(bottom) || bottom <= row.Top || bottom > contentHeight)
            return Refuse();
        float nextScroll = scrollY;
        if (row.Height > viewportHeight || row.Top < scrollY) nextScroll = row.Top;
        else if (bottom > scrollY + viewportHeight) nextScroll = bottom - viewportHeight;
        nextScroll = Math.Clamp(nextScroll, 0, Math.Max(0, contentHeight - viewportHeight));
        highlightedAt ??= nowSeconds;
        Status = SettingsRowRevealStatus.Highlighting;
        return new(nextScroll, rowId);
    }

    public void Refuse()
    {
        rowId = null;
        highlightedAt = null;
        Status = SettingsRowRevealStatus.Unavailable;
    }

    public void Cancel()
    {
        rowId = null;
        highlightedAt = null;
        Status = SettingsRowRevealStatus.Cancelled;
    }
}
