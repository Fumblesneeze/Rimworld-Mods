using System;

namespace Megamod.Features.MessagePlacement;

public enum MessageBarBoundsState { Visible, Absent, Unavailable }

// One prepared presentation's receipt; validation must never recache or render the provider.
public sealed class MessageBarBoundsReceipt
{
    private readonly Func<bool> isCurrent;
    private MessageBarBoundsReceipt(MessageBarBoundsState state, double? bottom, string? reasonKey, Func<bool> isCurrent)
    {
        if (bottom.HasValue && !double.IsFinite(bottom.Value)) throw new ArgumentOutOfRangeException(nameof(bottom));
        if (state == MessageBarBoundsState.Unavailable && string.IsNullOrWhiteSpace(reasonKey))
            throw new ArgumentException("Unavailable bar bounds require a localized reason key.", nameof(reasonKey));
        State = state; Bottom = bottom; ReasonKey = reasonKey;
        this.isCurrent = isCurrent ?? throw new ArgumentNullException(nameof(isCurrent));
    }
    public MessageBarBoundsState State { get; }
    public double? Bottom { get; }
    public string? ReasonKey { get; }
    public bool IsCurrent() => isCurrent();
    public static MessageBarBoundsReceipt Visible(double bottom, Func<bool> isCurrent) => new(MessageBarBoundsState.Visible, bottom, null, isCurrent);
    public static MessageBarBoundsReceipt Absent(Func<bool> isCurrent) => new(MessageBarBoundsState.Absent, null, null, isCurrent);
    public static MessageBarBoundsReceipt Unavailable(string reasonKey, Func<bool> isCurrent) => new(MessageBarBoundsState.Unavailable, null, reasonKey, isCurrent);
}
