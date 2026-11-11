using System;

namespace Megamod.Features.MessagePlacement;

public sealed record MessageBarLayoutContext(object Root, object Bar, double Width, double Height,
    object AdmissionGeneration);

public sealed class MessageBarLayoutReceipt
{
    private readonly Func<bool> isCurrent;
    internal MessageBarLayoutReceipt(Func<bool> isCurrent) => this.isCurrent = isCurrent;
    public bool IsCurrent() => isCurrent();
}

public sealed class MessageBarLayoutPreparation
{
    // Main-thread-owned. This is only a successful dimension/authority identity, never native layout.
    private MessageBarLayoutContext? lastPrepared;
    private object? currentPreparation;
    private bool preparing;
    public MessageBarLayoutReceipt? Prepare(MessageBarLayoutContext? context, Action markNativeDirty,
        Action prepareNative, Func<bool> nativeIsCurrent)
    {
        var token = currentPreparation = new object();
        var previous = lastPrepared;
        lastPrepared = null;
        if (preparing || context == null || context.Root == null || context.Bar == null ||
            context.AdmissionGeneration == null || !double.IsFinite(context.Width) || context.Width <= 0 ||
            !double.IsFinite(context.Height) || context.Height <= 0) return null;
        bool invalidate = previous == null || !SameContext(previous, context);
        preparing = true;
        try
        {
            if (invalidate) markNativeDirty();
            if (!ReferenceEquals(currentPreparation, token)) return null;
            prepareNative();
            if (!CheckCurrent(token, nativeIsCurrent)) return null;
            lastPrepared = context;
            return new(() => CheckCurrent(token, nativeIsCurrent));
        }
        finally { preparing = false; }
    }

    private bool CheckCurrent(object token, Func<bool> nativeIsCurrent)
    {
        if (!ReferenceEquals(currentPreparation, token)) return false;
        try
        {
            bool valid = nativeIsCurrent();
            if (!ReferenceEquals(currentPreparation, token)) return false;
            if (valid) return true;
        }
        catch
        {
            Revoke(token);
            throw;
        }
        Revoke(token);
        return false;
    }

    private void Revoke(object token)
    {
        if (!ReferenceEquals(currentPreparation, token)) return;
        currentPreparation = null;
        lastPrepared = null;
    }

    private static bool SameContext(MessageBarLayoutContext a, MessageBarLayoutContext b) =>
        ReferenceEquals(a.Root, b.Root) && ReferenceEquals(a.Bar, b.Bar) &&
        ReferenceEquals(a.AdmissionGeneration, b.AdmissionGeneration) && a.Width == b.Width && a.Height == b.Height;
}
