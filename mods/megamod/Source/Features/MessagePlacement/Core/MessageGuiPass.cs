using System;
using System.Collections.Generic;

namespace Megamod.Features.MessagePlacement;

// Owned by the native main-thread adapter. Tokens contain only one GUI invocation's observations.
public sealed class MessageGuiPass
{
    public sealed class Token
    {
        internal readonly List<MessageSurface> Surfaces = new();
        internal readonly List<object> Messages = new();
        internal readonly Token? Parent;
        internal bool Invalid, Sealed;
        internal Token(Token? parent) { Parent = parent; Invalid = parent != null; }
    }
    private Token? active;
    public Token Begin()
    {
        if (active != null) active.Invalid = true;
        return active = new(active);
    }
    public bool Observe(object messageIdentity, MessageSurface surface)
    {
        if (active == null || active.Invalid) return false;
        if (active.Sealed) { active.Invalid = true; return false; }
        foreach (var message in active.Messages)
            if (ReferenceEquals(message, messageIdentity)) { active.Invalid = true; return false; }
        active.Messages.Add(messageIdentity);
        active.Surfaces.Add(surface);
        return true;
    }
    public void Invalidate() { if (active != null) active.Invalid = true; }
    public bool Seal(Token token)
    {
        if (!ReferenceEquals(active, token) || token.Invalid || token.Sealed) return false;
        token.Sealed = true;
        return true;
    }
    public bool Complete(Token token, bool nativeSucceeded, double width, double height,
        double? visibleBarBottom, MessageOffsets offsets, Func<bool>? canCommit = null)
    {
        if (!ReferenceEquals(active, token)) return false;
        try
        {
            return nativeSucceeded && !token.Invalid && MessagePlacementBatch.TryApply(token.Surfaces, width, height,
                visibleBarBottom, offsets, () => !token.Invalid && (canCommit?.Invoke() ?? true) && !token.Invalid);
        }
        finally { active = token.Parent; token.Surfaces.Clear(); token.Messages.Clear(); }
    }
}
