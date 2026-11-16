using System.Reflection;
using System.Threading;
using Verse;
#if RIMWORLD_1_7
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Mouse = UnityEngine.InputSystem.Mouse;
#endif

namespace RimWorldDevGateway;

internal sealed partial class VerseGatewayNativeWindowInput
{
    public string? TextUnavailableReason
    {
        get
        {
            if (UnavailableReason is string reason) return reason;
#if RIMWORLD_1_7
            return textContract == null || !HasTextAuthority() ? "native_text_contract_unsupported" : null;
#else
            return "native_input_runtime_unsupported";
#endif
        }
    }

    public IGatewayDeferredAutomationOperation BeginReplaceText(string windowHandle, string controlHandle, string text,
        GatewayAutomationContext context)
    {
        RequireMainThread();
        if (TextUnavailableReason is string reason) throw Error(reason);
        if (context.CallerThreadId != Thread.CurrentThread.ManagedThreadId) throw Error("native_input_wrong_thread");
        if (!GatewayNativeTextOperation.ValidText(text)) throw Error("invalid_native_input_arguments");
#if RIMWORLD_1_7
        RetireCompleted();
        if (active != null || textActive != null) throw Error("native_input_busy");
        if (!textCaptures.TryGetValue(controlHandle, out var capture) || capture.Window.Descriptor.Handle != windowHandle ||
            !captures.TryGetValue(windowHandle, out var window) || !ReferenceEquals(window, capture.Window))
            throw Error("stale_native_text_focus");
        var backend = new TextBackend(this, capture);
        if (backend.Validate() is string rejection) throw Error(rejection);
        context.RunStep("native-text-admission", () => { });
        textContext = context; textBackend = backend;
        textActive = new GatewayNativeTextOperation(backend, text, () => backend.FocusFailure);
        textResult = new GatewayNativeInputResultOperation(textActive, () => failure ?? backend.ScopeFailure);
        return textResult;
#else
        throw Error("native_input_runtime_unsupported");
#endif
    }

#if RIMWORLD_1_7
    private NativeTextContract? textContract;
    private readonly Dictionary<string, TextCapture> textCaptures = new(StringComparer.Ordinal);
    private FocusedText? lastFocusedText;
    private GatewayNativeTextOperation? textActive;
    private GatewayNativeInputResultOperation? textResult;
    private TextBackend? textBackend;
    private GatewayAutomationContext? textContext;
    private TextFrame? textFrame;
    private FieldObservation? fieldObservation;

    private sealed class FieldObservation
    {
        public int? Control, Window;
        public bool Ambiguous;
    }

    private sealed class FocusedText
    {
        public readonly TextEditor Editor;
        public readonly int Control, Window;
        public readonly Rect Rect;
        public readonly bool Writable;
        public FocusedText(TextEditor editor, int control, int window, Rect rect, bool writable)
        { Editor = editor; Control = control; Window = window; Rect = rect; Writable = writable; }
    }
    private sealed class TextCapture
    {
        public readonly CaptureLease Window;
        public readonly FocusedText Focus;
        public readonly GatewayNativeTextControlLease Identity;
        public TextCapture(CaptureLease window, FocusedText focus)
        {
            Window = window; Focus = focus;
            Identity = new(focus.Editor, focus.Control, focus.Window, focus.Rect.x, focus.Rect.y, focus.Rect.width, focus.Rect.height);
        }
    }

    private void InstallTextHooks()
    {
        textContract = NativeTextContract.Resolve(contract!);
        if (textContract == null) return;
        // Patch the small native binding helper before compiling the field wrapper that calls it.
        harmony!.Patch(textContract.TrackControl,
            postfix: new HarmonyMethod(Hook(nameof(ObserveNativeTextControl))) { priority = Priority.First });
        harmony.Patch(textContract.TextField,
            prefix: new HarmonyMethod(Hook(nameof(BeforeNativeTextField))) { priority = Priority.First },
            postfix: new HarmonyMethod(Hook(nameof(ObserveNativeTextField))) { priority = Priority.First },
            finalizer: new HarmonyMethod(Hook(nameof(FinalizeNativeTextField))) { priority = Priority.First });
        harmony.Patch(contract!.Dispatch,
            prefix: new HarmonyMethod(TextHook(nameof(TextDispatchHooks.BeforeNativeTextDispatch))) { priority = int.MinValue },
            postfix: new HarmonyMethod(TextHook(nameof(TextDispatchHooks.AfterNativeTextDispatch))) { priority = Priority.First },
            finalizer: new HarmonyMethod(TextHook(nameof(TextDispatchHooks.FinalizeNativeTextDispatch))) { priority = Priority.Last });
    }

    private bool HasTextAuthority()
    {
        try
        {
            if (textContract == null || !HasPublishedMouseGuards()) return false;
            // The current native key producers have no patches. Unknown composed producers cannot
            // bypass the observed layout/event boundary; do not execute them as inspection probes.
            foreach (var method in new[] { textContract.KeyboardDispatch, textContract.KeyPass })
            {
                var bytes = PublishedMetadata(method);
                if (bytes == null) continue;
                var info = PatchInfoSerialization.Deserialize(bytes);
                if (info.prefixes.Length + info.postfixes.Length + info.finalizers.Length + info.transpilers.Length +
                    info.innerprefixes.Length + info.innerpostfixes.Length + info.innerfinalizers.Length != 0) return false;
            }
            var outerBytes = PublishedMetadata(contract!.Dispatch);
            if (outerBytes == null || !ExactObservers(textContract.TextField, nameof(BeforeNativeTextField),
                    nameof(ObserveNativeTextField), nameof(FinalizeNativeTextField)) ||
                !ExactObservers(textContract.TrackControl, null, nameof(ObserveNativeTextControl), null)) return false;
            var outer = PatchInfoSerialization.Deserialize(outerBytes);
            if (outer.prefixes.Any(p => GatewayNativeResetAdmission.IsFactory(p.PatchMethod))) return false;
            var sorted = new PatchSorter(outer.prefixes, debug: false).Sort();
            return sorted.Length != 0 && sorted[sorted.Length - 1].owner == "fumblesneeze.rimworlddevgateway" &&
                sorted[sorted.Length - 1].PatchMethod == TextHook(nameof(TextDispatchHooks.BeforeNativeTextDispatch)) &&
                sorted.Count(p => p.PatchMethod == TextHook(nameof(TextDispatchHooks.BeforeNativeTextDispatch))) == 1;
        }
        catch { return false; }
    }

    private bool ExactObservers(MethodInfo method, string? prefix, string postfix, string? finalizer)
    {
        var bytes = PublishedMetadata(method);
        if (bytes == null) return false;
        var info = PatchInfoSerialization.Deserialize(bytes);
        bool Exact(Patch[] patches, string? name) => name == null ? patches.Length == 0 : patches.Length == 1 &&
            patches[0].owner == "fumblesneeze.rimworlddevgateway" && patches[0].PatchMethod == Hook(name);
        return Exact(info.prefixes, prefix) && Exact(info.postfixes, postfix) && Exact(info.finalizers, finalizer) &&
            info.transpilers.Length + info.innerprefixes.Length + info.innerpostfixes.Length + info.innerfinalizers.Length == 0;
    }

    private FocusedText? CapturableText(Window window) => CapturableText(window, out _);

    private FocusedText? CapturableText(Window window, out string? unavailableReason)
    {
        var focus = lastFocusedText;
        unavailableReason = textContract == null ? "contract-unavailable" : focus == null ? "no-observed-focused-field" :
            focus.Window != window.ID ? "observed-window-changed" : !focus.Writable ? "readonly" : focus.Editor.multiline ? "multiline" :
            !(bool)textContract.CapturesText.GetValue(null)! ? "not-capturing-text" :
            GUIUtility.keyboardControl != focus.Control ? "keyboard-control-changed" :
            GUI.GetFocusedControlWindowId() != window.ID ? "focused-window-changed" :
            !ReferenceEquals(GUIUtility.QueryStateObject(typeof(TextEditor), focus.Control), focus.Editor) ? "editor-state-changed" :
            focus.Editor.controlID != focus.Control ? "editor-control-changed" : focus.Editor.position != focus.Rect ? "editor-position-changed" :
            !Find.WindowStack.GetsInput(window) ? "window-not-input-owner" : null;
        return unavailableReason == null ? focus : null;
    }

    private static void BeforeNativeTextField(out FieldObservation? __state)
    {
        __state = null;
        var service = installed;
        if (service?.textContract == null || !UnityData.IsInMainThread) return;
        if (service.fieldObservation != null) { service.fieldObservation.Ambiguous = true; return; }
        service.fieldObservation = __state = new();
    }

    private static void ObserveNativeTextControl(int __0, bool __runOriginal)
    {
        var service = installed;
        if (service?.fieldObservation is not FieldObservation observation || !UnityData.IsInMainThread || !__runOriginal) return;
        int window = (int)service.textContract!.CurrentWindow.GetValue(null)!;
        if (observation.Control.HasValue && (observation.Control != __0 || observation.Window != window)) observation.Ambiguous = true;
        observation.Control = __0; observation.Window = window;
    }

    private static Exception? FinalizeNativeTextField(Exception? __exception, FieldObservation? __state)
    {
        var service = installed;
        if (service != null && __state != null && ReferenceEquals(service.fieldObservation, __state))
        {
            service.fieldObservation = null;
            if (__exception != null) service.lastFocusedText = null;
        }
        return __exception;
    }

    private static void ObserveNativeTextField(Rect __0, bool __3, bool __runOriginal, FieldObservation? __state)
    {
        var service = installed;
        if (service?.textContract == null || !UnityData.IsInMainThread || !__runOriginal || __state == null || __state.Ambiguous ||
            !ReferenceEquals(service.fieldObservation, __state)) return;
        int control = GUIUtility.keyboardControl;
        if (control == 0 || __state.Control != control || GUIUtility.QueryStateObject(typeof(TextEditor), control) is not TextEditor editor ||
            editor.controlID != control || editor.position != __0) return;
        int window = (int)service.textContract.CurrentWindow.GetValue(null)!;
        if (window < 0 || __state.Window != window || GUI.GetFocusedControlWindowId() != window) return;
        service.lastFocusedText = new(editor, control, window, __0,
            GUI.enabled && !__3 && (bool)service.textContract.CapturesText.GetValue(null)!);
    }

    private bool TextStopRequested()
    {
        if (textBackend?.ScopeFailure != null) return true;
        try { textContext!.RunStep("native-text-dispatch", () => { }); return false; }
        catch (OperationCanceledException) { return true; }
        catch (GatewayAutomationTimeoutException) { return true; }
    }

    private static MethodInfo TextHook(string name) => typeof(TextDispatchHooks).GetMethod(name, BindingFlags.Public | BindingFlags.Static)!;

    // Harmony keys __state by the patch declaring type within one original method. These
    // callbacks must not share the click family's declaring type and incompatible DispatchFrame.
    private static class TextDispatchHooks
    {
        public static bool BeforeNativeTextDispatch(object __instance, object __0, bool __runOriginal, out TextFrame? __state)
        {
            __state = null;
            var service = installed;
            if (service?.textActive == null || service.textActive.IsQuarantined || !UnityData.IsInMainThread) return true;
            if (service.textFrame != null || service.currentDispatchFrame != null)
            { service.failure = "native_input_reentrant_dispatch"; return false; }
            var frame = new TextFrame(service, service.textActive, service.textBackend!);
            service.textFrame = __state = frame;
            service.dispatchWillRun = __runOriginal;
            frame.Backend.BeginDispatch();
            if (!ReferenceEquals(__instance, service.dispatchInstance) || !ReferenceEquals(__0, service.dispatchHost))
                service.failure ??= "native_dispatch_changed";
            if (!service.inputResetObserved) service.failure ??= "native_input_reset_unavailable";
            if (!service.HasTextAuthority()) service.failure ??= "native_text_guard_unavailable";
            bool stop = service.failure != null || service.TextStopRequested();
            frame.Operation.BeforeDispatch(stop);
            return __runOriginal && !stop && !frame.Backend.InjectionFailed;
        }
        public static void AfterNativeTextDispatch(bool __runOriginal, TextFrame? __state) => __state?.Finish(__runOriginal);
        public static Exception? FinalizeNativeTextDispatch(Exception? __exception, TextFrame? __state)
        { __state?.Finish(false); return __exception; }
    }

    private sealed class TextFrame
    {
        private readonly VerseGatewayNativeWindowInput service;
        private bool finished;
        private readonly GatewayNativeTextPassGate pass = new();
        private EventType raw;
        private KeyCode key;
        private char character;
        public readonly GatewayNativeTextOperation Operation;
        public readonly TextBackend Backend;
        public TextFrame(VerseGatewayNativeWindowInput service, GatewayNativeTextOperation operation, TextBackend backend)
        { this.service = service; Operation = operation; Backend = backend; }
        public void Capture(Event current, bool drawing)
        {
            raw = current.rawType; key = current.keyCode; character = current.character;
            pass.Capture(current, drawing, raw == EventType.layout, raw is EventType.keyDown or EventType.keyUp,
                (int)current.type, (int)raw, (int)key, character, (int)current.modifiers);
        }
        public bool Before(Event? current, bool drawing, bool original)
        {
            bool admitted = pass.Before(current, drawing, current?.rawType is EventType.keyDown or EventType.keyUp,
                (int)(current?.type ?? EventType.ignore), (int)(current?.rawType ?? EventType.ignore),
                (int)(current?.keyCode ?? KeyCode.None), current?.character ?? '\0', (int)(current?.modifiers ?? EventModifiers.None),
                original, service.failure == null && service.inputResetObserved && service.HasTextAuthority() && !service.TextStopRequested(),
                () => Backend.ValidatePass(raw, key, character, current!.modifiers));
            if (!admitted) Backend.Rejection ??= pass.Rejection ?? "native_input_not_dispatched";
            return admitted;
        }
        public void After(bool original)
        {
            if (pass.After(original)) Backend.Delivered(raw, key, character);
        }
        public void Finish(bool original)
        {
            if (finished) return;
            finished = true;
            try { Operation.AfterDispatch(original && Backend.AllDelivered, Backend.Rejection); }
            finally
            {
                if (ReferenceEquals(service.textFrame, this)) service.textFrame = null;
                service.RetireCompleted(); service.TickPointerScope();
            }
        }
    }

    private sealed class TextBackend : IGatewayNativeTextBackend
    {
        private readonly VerseGatewayNativeWindowInput service;
        private readonly TextCapture capture;
        private readonly Keyboard keyboard = Keyboard.current;
        private readonly GatewayNativePointerScope? pointer;
        private readonly GatewayNativeTextKeys keys = new();
        private readonly HashSet<(EventType Type, KeyCode Key, char Character)> expected = new(), delivered = new();
        private char? queuedCharacter;
        private EventModifiers expectedModifiers;
        public bool InjectionFailed { get; private set; }
        public string? Rejection { get; set; }
        public IReadOnlyDictionary<string, object?>? FocusFailure { get; private set; }
        public bool AllDelivered => expected.Count != 0 && expected.SetEquals(delivered) && Rejection == null;
        public string? ScopeFailure => pointer?.Reject(DateTimeOffset.UtcNow, ReadPointer());
        private List<char> Characters => (List<char>)service.contract!.TypedCharacters.GetValue(service.dispatchInstance)!;
        public TextBackend(VerseGatewayNativeWindowInput service, TextCapture capture)
        {
            this.service = service; this.capture = capture;
            pointer = service.pointerScope?.Result == null ? service.pointerScope : null;
        }
        public void BeginDispatch() { expected.Clear(); delivered.Clear(); InjectionFailed = false; Rejection = null; }
        public void InvalidateEdgeOwnership() => keys.Invalidate();
        public string? Validate()
        {
            if (!service.dispatchWillRun) return "native_input_not_dispatched";
            if (!(bool)service.textContract!.CharacterHooked.GetValue(service.dispatchInstance)!) return "native_text_contract_unsupported";
            return ScopeFailure ?? ValidateFocus() ?? ValidateInput();
        }
        private string? ValidateFocus()
        {
            var window = capture.Window;
            var stack = Find.WindowStack;
            if (!ReferenceEquals(stack, window.Stack) || !stack.Windows.Any(item => ReferenceEquals(item, window.Window))) return RejectFocus("stale_window_instance");
            var geometry = window.Descriptor.Geometry; var rect = window.Window.windowRect;
            if (window.Identity.Validate(window.Window, stack) is string rejection) return RejectFocus(rejection);
            if (!ReferenceEquals(service.dispatchInstance, window.Dispatcher) || !ReferenceEquals(service.dispatchHost, window.Host) ||
                rect.x != geometry.X || rect.y != geometry.Y || rect.width != geometry.Width || rect.height != geometry.Height ||
                Prefs.UIScale != geometry.UiScale || Screen.width != geometry.PixelWidth || Screen.height != geometry.PixelHeight || !service.ValidExtent())
                return RejectFocus("stale_window_geometry");
            if (!stack.GetsInput(window.Window)) return RejectFocus("native_window_obscured");
            var current = service.CapturableText(window.Window, out var unavailableReason);
            if (current == null) return RejectFocus("stale_native_text_focus", unavailableReason);
            var failure = capture.Identity.Validate(current.Editor, current.Control, current.Window, current.Writable, current.Editor.multiline,
                current.Rect.x, current.Rect.y, current.Rect.width, current.Rect.height);
            return failure == null ? null : RejectFocus(failure);
        }
        private string RejectFocus(string failure, string? unavailableReason = null)
        {
            if (FocusFailure != null) return failure;
            try
            {
                var current = service.lastFocusedText;
                var captured = capture.Focus;
                object? Number(float value) => float.IsNaN(value) || float.IsInfinity(value) ? null : (double)value;
                IReadOnlyDictionary<string, object?> Rectangle(Rect value) => new Dictionary<string, object?>
                { ["x"] = Number(value.x), ["y"] = Number(value.y), ["width"] = Number(value.width), ["height"] = Number(value.height) };
                FocusFailure = new Dictionary<string, object?>
                {
                    ["rejection"] = failure, ["capturableNullReason"] = unavailableReason,
                    ["sameEditor"] = current != null && ReferenceEquals(captured.Editor, current.Editor),
                    ["sameControl"] = current != null && captured.Control == current.Control,
                    ["sameWindow"] = current != null && captured.Window == current.Window,
                    ["capturedControl"] = captured.Control, ["observedControl"] = current?.Control,
                    ["keyboardControl"] = GUIUtility.keyboardControl, ["editorControl"] = current?.Editor.controlID,
                    ["capturedWindow"] = captured.Window, ["observedWindow"] = current?.Window,
                    ["focusedWindow"] = GUI.GetFocusedControlWindowId(),
                    ["capturedRect"] = Rectangle(captured.Rect),
                    ["observedRect"] = current == null ? null : Rectangle(current.Rect),
                    ["currentRect"] = current == null ? null : Rectangle(current.Editor.position),
                    ["writable"] = current?.Writable, ["multiline"] = current?.Editor.multiline
                };
            }
            catch { FocusFailure = new Dictionary<string, object?> { ["diagnosticUnavailable"] = true }; }
            return failure;
        }
        private string? ValidateInput()
        {
            if (!ReferenceEquals(Keyboard.current, keyboard)) return "native_input_ownership_lost";
            var mouse = Mouse.current;
            if (new[] { mouse.leftButton, mouse.rightButton, mouse.middleButton, mouse.forwardButton, mouse.backButton }
                    .Any(button => button.isPressed || service.contract!.HasEdges(button)) || mouse.scroll.ReadValue() != Vector2.zero ||
                GUIUtility.hotControl != 0 || (bool)service.contract!.ButtonActive.GetValue(null)!) return "native_input_busy";
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                var button = keyboard[key]; if (button == null) continue;
                if (button.isPressed != keys.IsHeld((int)key)) return keys.IsHeld((int)key) ? "native_input_ownership_lost" : "native_input_busy";
                if (Foreign(key, button))
                    return "native_input_ownership_lost";
            }
            return Characters.Count == 0 ? null : "native_input_busy";
        }
        private bool Foreign(Key key, ButtonControl button) => keys.HasForeignEdges((int)key, service.inputGeneration,
            (bool)service.contract!.PressEdge.GetValue(button)!, (bool)service.contract.ReleaseEdge.GetValue(button)!,
            (bool)service.contract.RepeatEdge.GetValue(button)!);
        public void Queue(GatewayNativeTextPhase phase, char character)
        {
            try
            {
                expectedModifiers = phase == GatewayNativeTextPhase.SelectAllPress ? EventModifiers.Control : EventModifiers.None;
                switch (phase)
                {
                    case GatewayNativeTextPhase.SelectAllPress:
                        Expect(EventType.keyDown, KeyCode.LeftControl); Expect(EventType.keyDown, KeyCode.A);
                        KeyChange(Key.LeftCtrl, true); KeyChange(Key.A, true); break;
                    case GatewayNativeTextPhase.SelectAllRelease:
                        Expect(EventType.keyUp, KeyCode.A); Expect(EventType.keyUp, KeyCode.LeftControl);
                        KeyChange(Key.A, false); KeyChange(Key.LeftCtrl, false); break;
                    case GatewayNativeTextPhase.ClearPress:
                        Expect(EventType.keyDown, KeyCode.Backspace); KeyChange(Key.Backspace, true); break;
                    case GatewayNativeTextPhase.ClearRelease:
                        Expect(EventType.keyUp, KeyCode.Backspace); KeyChange(Key.Backspace, false); break;
                    case GatewayNativeTextPhase.Character:
                        if (Characters.Count != 0) throw Error("native_input_busy");
                        expected.Add((EventType.keyDown, KeyCode.None, character)); queuedCharacter = character;
                        InputSystem.QueueTextInput(character);
                        if (Characters.Count != 1 || Characters[0] != character) throw Error("native_text_queue_ownership_lost");
                        break;
                }
                if (ValidateFocus() != null) throw Error("stale_native_text_focus");
            }
            catch { InjectionFailed = true; throw; }
        }
        private void Expect(EventType type, KeyCode key) => expected.Add((type, key, '\0'));
        private void KeyChange(Key key, bool pressed)
        {
            if (pressed) keys.BeforePress((int)key);
            InputSystem.QueueKeyChange(key, pressed);
            keys.Record((int)key, service.inputGeneration, pressed);
            if (keyboard[key].isPressed != pressed || Foreign(key, keyboard[key])) throw Error("native_input_ownership_lost");
            if (!pressed) keys.Released((int)key);
        }
        public string? ValidatePass(EventType type, KeyCode key, char character, EventModifiers modifiers)
        {
            if (!expected.Contains((type, key, character)) || delivered.Contains((type, key, character)) || modifiers != expectedModifiers)
                return "native_text_pass_changed";
            return ScopeFailure ?? ValidateFocus() ?? ValidateInput();
        }
        public void Delivered(EventType type, KeyCode key, char character)
        {
            delivered.Add((type, key, character));
            if (key == KeyCode.None && queuedCharacter == character) queuedCharacter = null;
        }
        public void Cleanup()
        {
            keys.Cleanup(rawKey =>
            {
                var key = (Key)rawKey;
                if (!ReferenceEquals(Keyboard.current, keyboard) || !keyboard[key].isPressed || Foreign(key, keyboard[key]))
                    throw Error("native_input_ownership_lost");
                KeyChange(key, false);
            }, () =>
            {
                if (queuedCharacter is not char character) return;
                if (Characters.Count == 1 && Characters[0] == character) Characters.RemoveAt(0);
                else if (Characters.Count != 0) throw Error("native_text_queue_ownership_lost");
                queuedCharacter = null;
            });
        }
    }

    private sealed class NativeTextContract
    {
        public required MethodInfo TextField, TrackControl, KeyboardDispatch, KeyPass;
        public required FieldInfo CurrentWindow, CharacterHooked;
        public required PropertyInfo CapturesText;
        public static NativeTextContract? Resolve(NativeContract native)
        {
            var dispatcher = native.Dispatch.DeclaringType!;
            const BindingFlags hiddenStatic = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
            const BindingFlags hiddenInstance = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            var textField = typeof(GUI).GetMethod("TextFieldImpl", hiddenStatic, null, new[] { typeof(Rect), typeof(string), typeof(GUIStyle), typeof(bool) }, null);
            var trackControl = typeof(GUI).GetMethod("TrackControlWindow", hiddenStatic, null, new[] { typeof(int) }, null);
            var keyboard = dispatcher.GetMethod("DispatchKeyboard", hiddenInstance, null, new[] { typeof(Vector2), typeof(EventModifiers) }, null);
            var keyPass = dispatcher.GetMethod("RunKeyPass", hiddenInstance, null,
                new[] { typeof(KeyCode), typeof(char), typeof(Vector2), typeof(EventModifiers), typeof(EventType) }, null);
            var window = typeof(GUI).GetField("_currentWindowId", hiddenStatic);
            var characterHooked = dispatcher.GetField("_charHooked", hiddenInstance);
            var textFocus = typeof(GUI).Assembly.GetType("UnityStubs.Internal.TextFocus", false);
            var captures = textFocus?.GetProperty("CapturesText", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (textField?.ReturnType != typeof(string) || trackControl?.ReturnType != typeof(void) || keyboard?.ReturnType != typeof(void) || keyPass?.ReturnType != typeof(void) ||
                window?.FieldType != typeof(int) || characterHooked?.FieldType != typeof(bool) || captures?.PropertyType != typeof(bool) ||
                !GatewayNativeWindowInputContract.Matches(typeof(InputSystem).GetMethod("QueueTextInput", new[] { typeof(char) }),
                    typeof(InputSystem), true, typeof(void), new[] { typeof(char) }) ||
                !GatewayNativeWindowInputContract.Matches(typeof(InputSystem).GetMethod("QueueKeyChange", new[] { typeof(Key), typeof(bool) }),
                    typeof(InputSystem), true, typeof(void), new[] { typeof(Key), typeof(bool) })) return null;
            return new() { TextField = textField, TrackControl = trackControl, KeyboardDispatch = keyboard, KeyPass = keyPass,
                CurrentWindow = window, CharacterHooked = characterHooked, CapturesText = captures };
        }
    }
#endif
}
