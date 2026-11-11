using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Megamod.Patching;

namespace Megamod.Features.MessagePlacement;

public sealed record MessagePlacementTargets(MethodInfo Draw, MethodInfo Calculate, MethodInfo Register,
    MethodInfo AddNew, MethodInfo MessagesGui, MethodInfo BaseGui, MethodInfo EntryGui, MethodInfo PlayGui,
    MethodInfo Collides, FieldInfo MessageRectangle, FieldInfo WindowRectangle, FieldInfo WindowCallback);

// One immutable native projection, never another collection of message surfaces.
public sealed record MessagePlacementFrame(object Root, object WindowStack, IEnumerable<object> Windows,
    double Width, double Height, bool Repaint, MessageOffsets Offsets, Func<bool> IsCurrent);

public sealed class MessagePlacementBridge
{
    private readonly MessagePlacementTargets targets;
    private readonly MessageDrawPatch draw;
    private readonly MessageRegistrationGuard queue;
    private readonly MessageGuiEventPatch outer;
    private readonly MessageGuiScopePatch scope;
    private readonly MessageBarPreparedPatch? prepared;
    private readonly MessageWindowStackPatch? windowStack;
    private readonly Func<object, object, MessageBarBoundsReceipt?>? captureWindowFallback;
    private readonly MessageGuiPass pass = new();
    private readonly MessageGuiEvent timing;
    private readonly Func<MessagePlacementFrame?> captureFrame;
    private readonly Func<MessageBounds, object> nativeRectangle;
    private readonly Action<string> warning;
    private readonly Func<bool> placementEnabled;
    private readonly Action? endInvocation;
    private MessageConsumptionPatch? consumption;
    private MessageWindowBindings? bindings;
    private MessageDrawShape? boundShape;
    private Pending? pending;
    private bool enabled, faulted, reported;
    private sealed record Pending(MessageGuiEvent.Collection Token, MessagePlacementFrame Frame,
        MessageDrawShape Shape, object QueueGeneration);

    public MessagePlacementBridge(MessagePlacementTargets targets, MethodInfo forward,
        Func<object, bool> currentContext, Func<MessagePlacementFrame?> captureFrame,
        Func<MessageBounds, object> nativeRectangle, Action<string> warning,
        MessageBarPreparedTargets? preparedTargets = null,
        Func<object, object, bool, MessageBarBoundsReceipt>? capturePrepared = null,
        MethodInfo? windowStackGui = null, Func<object, object, MessageBarBoundsReceipt?>? captureWindowFallback = null,
        Func<bool>? placementEnabled = null, MessageBarGeometryGuard? barGeometry = null,
        Action<object>? prepareBarGeometry = null, Action<object, int, object>? consumeBarGeometry = null,
        Action? endInvocation = null)
    {
        this.targets = targets; this.captureFrame = captureFrame; this.nativeRectangle = nativeRectangle; this.warning = warning;
        this.captureWindowFallback = captureWindowFallback;
        this.placementEnabled = placementEnabled ?? (() => true);
        this.endInvocation = endInvocation;
        timing = new(pass, Report);
        queue = new(targets.Register, targets.AddNew, targets.WindowRectangle, targets.WindowCallback);
        draw = new(targets.Draw, targets.Calculate, targets.Register, forward);
        outer = new(targets.BaseGui, targets.EntryGui, targets.PlayGui, timing, currentContext, Fault, Exit);
        scope = new(targets.MessagesGui, Begin, Complete, Fault);
        if ((preparedTargets == null) != (capturePrepared == null))
            throw new ArgumentException("Prepared target and geometry capture must be supplied together.");
        if (preparedTargets != null)
            prepared = new(preparedTargets, (bar, entries, frames, authority) =>
            {
                if (!this.placementEnabled()) return;
                var bounds = capturePrepared!(bar, entries, frames);
                var current = pending;
                if (current != null)
                    CommitPrepared(current.Frame.Root, bounds, authority);
                else
                    timing.NotifyConsumption();
            }, Fault, geometry: barGeometry, preparing: prepareBarGeometry, consuming: consumeBarGeometry,
                geometryFailureReporter: failure => Report(failure.GetBaseException().Message));
        if ((windowStackGui == null) != (captureWindowFallback == null))
            throw new ArgumentException("Window entry target and fallback capture must be supplied together.");
        if (windowStackGui != null) windowStack = new(windowStackGui, BeforeWindows, Fault);
        Group = new("messages.native-placement", AdmissionPhase.Early, new[]
        {
            queue.Candidate, draw.Candidate,
            new PatchCandidate("MSG-NATIVE-CONSUMPTION", "MSG-NATIVE-FRAME", targets.Collides.ToString()!,
                "message placement", "native placement", () => null, InstallDependent, RollbackDependent),
            outer.Candidate, scope.Candidate
        }.Concat(prepared == null ? Array.Empty<PatchCandidate>() : new[] { prepared.Candidate })
        .Concat(windowStack == null ? Array.Empty<PatchCandidate>() : new[] { windowStack.Candidate }).Concat(new[]
        {
            new PatchCandidate("MSG-NATIVE-PUBLICATION", "MSG-NATIVE-FRAME", "native message placement group",
                "message placement", "native placement", () => null, Publish, () => { enabled = false; pass.Invalidate(); })
        }).ToArray());
    }
    public PatchGroup Group { get; }
    public bool Available => enabled && Ready;
    public bool GeometryAvailable => Available && prepared?.GeometryAvailable == true;
    public Func<bool> CaptureGeometryAuthority() => prepared?.CaptureGeometryAuthority() ?? (() => false);
    private bool Ready => !faulted && draw.Available && draw.Shape == boundShape && queue.Available &&
        outer.Available && scope.Available && consumption?.Available == true && bindings != null &&
        (prepared == null || prepared.Available) && (windowStack == null || windowStack.Available);
    private void InstallDependent()
    {
        boundShape = draw.Shape ?? throw new InvalidOperationException("The actual native callback was not discovered.");
        bindings = new(targets.MessageRectangle, targets.WindowRectangle, targets.WindowCallback,
            boundShape.MessageField, boundShape.RectangleField, boundShape.Callback);
        consumption = new(boundShape, targets.Collides, timing.NotifyConsumption, Fault);
        string? failure = consumption.Candidate.Validate();
        if (failure != null) throw new InvalidOperationException(failure);
        consumption.Candidate.Install();
    }
    private void RollbackDependent()
    {
        enabled = false; pass.Invalidate(); bindings = null; boundShape = null;
        consumption?.Candidate.Rollback();
    }
    private void Publish()
    {
        if (!Ready) throw new InvalidOperationException("The complete native placement group is unavailable.");
        consumption!.Enable(); outer.Enable(); scope.Enable(); prepared?.Enable(); windowStack?.Enable();
        enabled = true;
    }
    private object? Begin()
    {
        if (!placementEnabled()) return null;
        if (!Available)
        {
            if (enabled) Report("Native message target admission is no longer valid.");
            return null;
        }
        var frame = captureFrame();
        if (frame == null) { Decline("Current native placement context is unavailable."); return null; }
        var token = timing.BeginMessages(frame.Root);
        if (token == null) return null;
        return pending = new(token, frame, draw.Shape!, queue.Generation!);
    }
    public void Observe(object stack, object message, object rectangle, Action callback, MessageBounds bounds)
    {
        var current = pending;
        if (current == null) return;
        try
        {
            if (!ReferenceEquals(current.Frame.WindowStack, stack)) { Decline("Native window stack changed during collection."); return; }
            var status = bindings!.Capture(message, rectangle, callback, current.Frame.Repaint, current.Frame.Windows, out var capture);
            if (status == MessageWindowCaptureStatus.NotRenderedYet) return;
            if (status != MessageWindowCaptureStatus.Bound) { Decline("Native message window binding is incompatible."); return; }
            pass.Observe(message, new(bounds, moved => capture!.Apply(nativeRectangle(moved)), capture!.Restore,
                () => capture!.IsCurrent(current.Frame.Windows)));
        }
        catch (Exception failure) { Fault(failure); }
    }
    private void Complete(object token, bool nativeSucceeded)
    {
        var current = (Pending)token;
        try
        {
            bool Current() => placementEnabled() && Available && ReferenceEquals(draw.Shape, current.Shape) &&
                ReferenceEquals(queue.Generation, current.QueueGeneration) && current.Frame.IsCurrent();
            bool valid = Current();
            if (!valid) Decline("Native message placement context changed before completion.");
            bool collected = timing.CompleteMessages(current.Token, nativeSucceeded && valid);
            if (!collected && ReferenceEquals(pending, current)) pending = null;
            if (nativeSucceeded && valid && !collected) Report("Native message surfaces could not be safely collected.");
        }
        catch { if (ReferenceEquals(pending, current)) pending = null; throw; }
    }
    public bool CommitPrepared(object root, MessageBarBoundsReceipt bar, Func<bool> barrierIsCurrent)
    {
        var current = pending;
        if (current == null || !ReferenceEquals(current.Frame.Root, root)) return false;
        bool Current() => placementEnabled() && Available && ReferenceEquals(draw.Shape, current.Shape) &&
            ReferenceEquals(queue.Generation, current.QueueGeneration) && current.Frame.IsCurrent() &&
            barrierIsCurrent() && bar.IsCurrent();
        bool committed = timing.CommitPrepared(current.Token, current.Frame.Width, current.Frame.Height,
            bar.Bottom, current.Frame.Offsets, Current);
        if (committed && ReferenceEquals(pending, current)) pending = null;
        if (!committed) Decline("Native message surfaces could not be safely translated at the prepared barrier.");
        return committed;
    }
    private void BeforeWindows(object actualStack, Func<bool> barrierIsCurrent)
    {
        var current = pending;
        if (current != null && ReferenceEquals(current.Frame.WindowStack, actualStack))
        {
            var fallback = captureWindowFallback!(current.Frame.Root, actualStack);
            if (fallback != null)
            {
                CommitPrepared(current.Frame.Root, fallback, barrierIsCurrent);
                return;
            }
        }
        timing.NotifyConsumption();
    }
    private void Exit(MessageGuiEvent.Envelope envelope)
    {
        if (pending != null && ReferenceEquals(pending.Token.Envelope, envelope)) pending = null;
        endInvocation?.Invoke();
    }
    private void Decline(string reason) { pass.Invalidate(); Report(reason); }
    private void Fault(Exception failure) { faulted = true; enabled = false; Decline(failure.GetBaseException().Message); }
    private void Report(string reason)
    {
        if (reported || !placementEnabled()) return;
        reported = true;
        char[] bounded = (reason.Length > 256 ? reason.Substring(0, 256) : reason).ToCharArray();
        for (int index = 0; index < bounded.Length; index++) if (char.IsControl(bounded[index])) bounded[index] = ' ';
        try { warning("[fumblesneeze.megamod] MSG-NATIVE-FRAME placement skipped; native messages remain. " + new string(bounded)); }
        catch (Exception) { /* The protected sink owns its durable fallback. */ }
    }
}
