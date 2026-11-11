using System;

namespace Megamod.Features.MessagePlacement;

// One native GUI invocation, not a game frame or a second message/surface collection.
public sealed class MessageGuiEvent
{
    public sealed class Envelope
    {
        internal readonly object Root;
        internal readonly Envelope? Parent;
        internal Collection? Collection;
        internal bool Invalid;
        internal Envelope(object root, Envelope? parent) { Root = root; Parent = parent; }
    }
    public sealed class Collection
    {
        internal readonly Envelope Envelope;
        internal readonly MessageGuiPass.Token Pass;
        internal bool Collected, Completed, Completing;
        internal Collection(Envelope envelope, MessageGuiPass.Token pass) { Envelope = envelope; Pass = pass; }
    }
    private readonly MessageGuiPass pass;
    private readonly Action<string> reportFailure;
    private Envelope? active;
    private bool reported;
    public MessageGuiEvent(MessageGuiPass pass, Action<string> reportFailure) { this.pass = pass; this.reportFailure = reportFailure; }
    public Envelope Enter(object root)
    {
        if (active != null) Reject("Nested native GUI invocations cannot authorize further placement.");
        return active = new(root, active) { Invalid = active != null };
    }
    public void Exit(Envelope envelope)
    {
        if (!ReferenceEquals(active, envelope)) return;
        envelope.Invalid = true;
        try
        {
            if (envelope.Collection is { Completed: false } pending)
            {
                pass.Invalidate();
                if (!pending.Completing) Abort(pending);
            }
        }
        finally { active = envelope.Parent; }
    }
    public Collection? BeginMessages(object root)
    {
        if (active == null || !ReferenceEquals(active.Root, root))
        { Reject("Message collection has no matching native GUI invocation."); return null; }
        if (active.Collection != null)
        { Reject("Repeated message collection cannot authorize further placement in this invocation."); return null; }
        return active.Invalid ? null : active.Collection = new(active, pass.Begin());
    }
    // True means the collection is sealed and pending, not that native surfaces moved.
    public bool CompleteMessages(Collection collection, bool nativeSucceeded)
    {
        if (collection.Collected || collection.Completed || collection.Completing || !IsCurrent(collection)) return false;
        collection.Collected = true;
        if (nativeSucceeded && !collection.Envelope.Invalid && pass.Seal(collection.Pass)) return true;
        Abort(collection);
        return false;
    }
    private void Abort(Collection collection)
    {
        try { pass.Complete(collection.Pass, false, 0, 0, null, default); }
        finally { collection.Completed = true; }
    }
    private bool IsCurrent(Collection collection) => ReferenceEquals(active, collection.Envelope) &&
        ReferenceEquals(active?.Collection, collection);
    // The native caller supplies current prepared values at its admitted later barrier.
    public bool CommitPrepared(Collection collection, double width, double height,
        double? visibleBarBottom, MessageOffsets offsets, Func<bool>? canCommit = null)
    {
        if (!collection.Collected || collection.Completed || collection.Completing || !IsCurrent(collection)) return false;
        collection.Completing = true;
        try
        {
            return pass.Complete(collection.Pass, !collection.Envelope.Invalid, width, height,
                visibleBarBottom, offsets, () => !collection.Envelope.Invalid && (canCommit?.Invoke() ?? true) &&
                    !collection.Envelope.Invalid && IsCurrent(collection));
        }
        finally { collection.Completed = true; collection.Completing = false; }
    }
    public void NotifyConsumption()
    {
        if (active != null && active.Collection?.Completed != true)
            Reject("A native message consumer ran before placement committed.");
    }
    private void Reject(string reason)
    {
        if (active != null) active.Invalid = true;
        pass.Invalidate();
        if (reported) return;
        reported = true;
        try { reportFailure(reason); } catch (Exception) { /* Diagnostics do not own native GUI behavior. */ }
    }
}
