using System;
using System.Collections.Generic;
using System.Reflection;

namespace Megamod.Features.MessagePlacement;

public enum MessageWindowCaptureStatus { Bound, NotRenderedYet, Incompatible }

/// <summary>Only the inspected native fields; never a message/window registry.</summary>
public sealed class MessageWindowBindings
{
    private readonly FieldInfo messageRectangle, windowRectangle, windowCallback, capturedMessage, capturedRectangle;
    private readonly MethodInfo drawingCallback;

    public MessageWindowBindings(FieldInfo messageRectangle, FieldInfo windowRectangle, FieldInfo windowCallback,
        FieldInfo capturedMessage, FieldInfo capturedRectangle, MethodInfo drawingCallback)
    {
        if (messageRectangle.IsStatic || windowRectangle.IsStatic || windowCallback.IsStatic || capturedMessage.IsStatic ||
            capturedRectangle.IsStatic || messageRectangle.IsInitOnly || windowRectangle.IsInitOnly || capturedRectangle.IsInitOnly ||
            !messageRectangle.FieldType.IsValueType || windowRectangle.FieldType != messageRectangle.FieldType ||
            capturedRectangle.FieldType != messageRectangle.FieldType || windowCallback.FieldType != typeof(Action) ||
            !windowRectangle.DeclaringType!.IsAssignableFrom(windowCallback.DeclaringType) ||
            !messageRectangle.DeclaringType!.IsAssignableFrom(capturedMessage.FieldType) ||
            capturedMessage.DeclaringType != capturedRectangle.DeclaringType ||
            drawingCallback.DeclaringType != capturedMessage.DeclaringType || drawingCallback.IsStatic ||
            drawingCallback.ContainsGenericParameters || drawingCallback.ReturnType != typeof(void) || drawingCallback.GetParameters().Length != 0)
            throw new ArgumentException("The native message/window/callback field contract changed.");
        this.messageRectangle = messageRectangle; this.windowRectangle = windowRectangle;
        this.windowCallback = windowCallback; this.capturedMessage = capturedMessage;
        this.capturedRectangle = capturedRectangle; this.drawingCallback = drawingCallback;
    }

    public bool TryCapture(object message, object nativeRectangle, Action suppliedCallback, bool repaint,
        IEnumerable<object> windows, out MessageWindowCapture? capture) =>
        Capture(message, nativeRectangle, suppliedCallback, repaint, windows, out capture) == MessageWindowCaptureStatus.Bound;

    public MessageWindowCaptureStatus Capture(object message, object nativeRectangle, Action suppliedCallback, bool repaint,
        IEnumerable<object> windows, out MessageWindowCapture? capture)
    {
        capture = null;
        if (!Matches(suppliedCallback, message) || !Equals(capturedRectangle.GetValue(suppliedCallback.Target), nativeRectangle) ||
            !Equals(messageRectangle.GetValue(message), nativeRectangle)) return MessageWindowCaptureStatus.Incompatible;
        object? selected = null;
        Action? selectedCallback = null;
        foreach (object window in windows)
        {
            if (!windowCallback.DeclaringType!.IsInstanceOfType(window) || windowCallback.GetValue(window) is not Action callback) continue;
            Delegate[] invocations = callback.GetInvocationList();
            bool containsMessage = false;
            foreach (var invocation in invocations)
                if (HasMessageIdentity(invocation, message)) { containsMessage = true; break; }
            if (!containsMessage) continue;
            // A known message inside a combined callback is present but unsupported, never "not rendered yet".
            if (invocations.Length != 1) return MessageWindowCaptureStatus.Incompatible;
            if (selected != null) return MessageWindowCaptureStatus.Incompatible;
            selected = window;
            selectedCallback = callback;
        }
        if (selected == null) return repaint ? MessageWindowCaptureStatus.Incompatible : MessageWindowCaptureStatus.NotRenderedYet;
        if (repaint && (!ReferenceEquals(selectedCallback, suppliedCallback) ||
            !Equals(windowRectangle.GetValue(selected), nativeRectangle))) return MessageWindowCaptureStatus.Incompatible;
        object windowBefore = windowRectangle.GetValue(selected)!;
        object messageBefore = messageRectangle.GetValue(message)!;
        object callbackBefore = capturedRectangle.GetValue(selectedCallback!.Target)!;
        capture = new MessageWindowCapture(currentWindows =>
        {
            if (!Matches(suppliedCallback, message) || !Matches(selectedCallback, message) ||
                !Equals(capturedRectangle.GetValue(suppliedCallback.Target), nativeRectangle) ||
                !Equals(messageRectangle.GetValue(message), messageBefore) ||
                !Equals(windowRectangle.GetValue(selected), windowBefore) ||
                !Equals(capturedRectangle.GetValue(selectedCallback.Target), callbackBefore) ||
                !ReferenceEquals(windowCallback.GetValue(selected), selectedCallback)) return false;
            int matchingWindows = 0;
            foreach (object window in currentWindows)
            {
                if (!windowCallback.DeclaringType!.IsInstanceOfType(window) || windowCallback.GetValue(window) is not Action callback) continue;
                foreach (Delegate invocation in callback.GetInvocationList())
                    if (HasMessageIdentity(invocation, message))
                    {
                        if (!ReferenceEquals(window, selected) || !ReferenceEquals(callback, selectedCallback)) return false;
                        matchingWindows++;
                    }
            }
            return matchingWindows == 1;
        }, rectangle =>
        {
            if (rectangle.GetType() != messageRectangle.FieldType) throw new ArgumentException("Native rectangle type changed.");
            windowRectangle.SetValue(selected, rectangle);
            messageRectangle.SetValue(message, rectangle);
            capturedRectangle.SetValue(selectedCallback.Target, rectangle);
        }, () =>
        {
            windowRectangle.SetValue(selected, windowBefore);
            messageRectangle.SetValue(message, messageBefore);
            capturedRectangle.SetValue(selectedCallback.Target, callbackBefore);
        });
        return MessageWindowCaptureStatus.Bound;
    }

    private bool Matches(Action callback, object message) => callback.GetInvocationList().Length == 1 && HasMessageIdentity(callback, message);

    private bool HasMessageIdentity(Delegate callback, object message) => callback.Method == drawingCallback &&
        capturedMessage.DeclaringType!.IsInstanceOfType(callback.Target) &&
        ReferenceEquals(capturedMessage.GetValue(callback.Target), message);
}

public sealed class MessageWindowCapture
{
    private readonly Func<IEnumerable<object>, bool> isCurrent;
    private readonly Action<object> apply;
    private readonly Action restore;
    internal MessageWindowCapture(Func<IEnumerable<object>, bool> isCurrent, Action<object> apply, Action restore)
    { this.isCurrent = isCurrent; this.apply = apply; this.restore = restore; }
    public void Apply(object rectangle) => apply(rectangle);
    public void Restore() => restore();
    public bool IsCurrent(IEnumerable<object> windows) => isCurrent(windows);
}
