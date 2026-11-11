using System.Reflection;
using System.Reflection.Emit;
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

internal sealed class GatewayNativeWindowDescriptor
{
    internal GatewayNativeWindowDescriptor(string handle, string runtimeType, int nativeId,
        GatewayNativeWindowGeometry geometry, string? focusedTextControlHandle = null)
    { Handle = handle; RuntimeType = runtimeType; NativeId = nativeId; Geometry = geometry;
      FocusedTextControlHandle = focusedTextControlHandle; }
    public string Handle { get; }
    public string RuntimeType { get; }
    public int NativeId { get; }
    public string? FocusedTextControlHandle { get; }
    internal GatewayNativeWindowGeometry Geometry { get; }
    public double X => Geometry.X;
    public double Y => Geometry.Y;
    public double Width => Geometry.Width;
    public double Height => Geometry.Height;
    public double UiScale => Geometry.UiScale;
    public int PixelWidth => Geometry.PixelWidth;
    public int PixelHeight => Geometry.PixelHeight;
}

// This adapter is created, used and shut down on the actual Unity thread. No input is sent by
// Capture, Begin or Advance: the engine's pre/post-dispatch boundary owns all gesture phases.
internal sealed partial class VerseGatewayNativeWindowInput : IDisposable
{
    public bool IsAvailable => UnavailableReason == null;
    public string? UnavailableReason
    {
        get
        {
#if RIMWORLD_1_7
            if (failure != null) return failure;
            if (active?.IsQuarantined == true || textActive?.IsQuarantined == true) return "native_input_quarantined";
            if (pointerScope?.Result?.Error != null) return "native_pointer_scope_quarantined";
            if (!inputResetObserved) return "native_input_reset_not_observed";
            if (!HasPublishedMouseGuards()) return "native_mouse_guard_not_observed";
            return dispatchInstance == null ? "native_dispatch_not_observed" : null;
#else
            return "native_input_runtime_unsupported";
#endif
        }
    }

    public VerseGatewayNativeWindowInput()
    {
        RequireMainThread();
#if RIMWORLD_1_7
        if (installed != null) { failure = "native_input_provider_already_installed"; return; }
        try
        {
            contract = NativeContract.Resolve();
            if (contract == null) { failure = "native_input_contract_unsupported"; return; }
            harmony = new Harmony("fumblesneeze.rimworlddevgateway");
            // Register ownership before patching so partial patch failures have exact cleanup.
            installed = this;
            harmony.Patch(contract.Dispatch,
                prefix: new HarmonyMethod(Hook(nameof(BeforeNativeDispatch))) { priority = Priority.Last },
                postfix: new HarmonyMethod(Hook(nameof(AfterNativeDispatch))) { priority = Priority.First },
                transpiler: new HarmonyMethod(Hook(nameof(ObserveMouseGuardStream))) { priority = Priority.Last },
                finalizer: new HarmonyMethod(Hook(nameof(FinalizeNativeDispatch))) { priority = Priority.Last });
            harmony.Patch(contract.EventDispatch,
                prefix: new HarmonyMethod(Hook(nameof(BeforeNativeMousePass))) { priority = Priority.Last },
                postfix: new HarmonyMethod(Hook(nameof(AfterNativeMousePass))) { priority = Priority.First },
                transpiler: new HarmonyMethod(Hook(nameof(ObserveMouseGuardStream))) { priority = Priority.Last });
            harmony.Patch(contract.EventDispatch,
                prefix: new HarmonyMethod(Hook(nameof(CaptureNativeMousePass))) { priority = Priority.First });
            harmony.Patch(contract.FrameReset,
                postfix: new HarmonyMethod(Hook(nameof(AfterInputFrameReset))) { priority = Priority.First },
                finalizer: new HarmonyMethod(Hook(nameof(FinalizeInputFrameReset))) { priority = Priority.Last });
            InstallTextHooks();
            harmony.Patch(contract.FrameResetBoundary,
                postfix: new HarmonyMethod(Hook(nameof(AfterInputResetBoundary))) { priority = Priority.First },
                transpiler: new HarmonyMethod(Hook(nameof(ObserveResetBoundaryStream))) { priority = Priority.Last },
                finalizer: new HarmonyMethod(Hook(nameof(FinalizeInputFrameReset))) { priority = Priority.Last });
        }
        catch
        {
            failure = "native_input_hook_unavailable";
            RemoveHooks();
        }
#endif
    }

    public IReadOnlyList<GatewayNativeWindowDescriptor> Capture()
    {
        RequireMainThread();
        RequireAvailable();
#if RIMWORLD_1_7
        WindowStack? stack = Find.WindowStack;
        if (stack == null) throw Error("native_window_stack_unavailable");
        if (stack.Windows.Count > MaximumWindows) throw Error("native_window_inventory_limit");
        if (!ValidExtent()) throw Error("native_render_extent_unavailable");
        captures.Clear();
        textCaptures.Clear();
        var result = new List<GatewayNativeWindowDescriptor>();
        foreach (Window window in stack.Windows)
        {
            // Immediate windows are recreated during drawing and have no durable instance lease.
            if (!GatewayNativeWindowInputContract.SupportsWindowIdentity(window.ID, window is ImmediateWindow)) continue;
            Rect bounds = window.windowRect;
            var geometry = new GatewayNativeWindowGeometry(bounds.x, bounds.y, bounds.width, bounds.height,
                Prefs.UIScale, Screen.width, Screen.height, contract!.MouseOriginOffset);
            string handle = Guid.NewGuid().ToString("N");
            var focus = TextUnavailableReason == null ? CapturableText(window) : null;
            string? controlHandle = focus == null ? null : Guid.NewGuid().ToString("N");
            var descriptor = new GatewayNativeWindowDescriptor(handle, window.GetType().FullName ??
                window.GetType().Name, window.ID, geometry, controlHandle);
            var lease = new CaptureLease(window, stack, descriptor, dispatchInstance!, dispatchHost!);
            captures.Add(handle, lease);
            if (controlHandle != null) textCaptures.Add(controlHandle, new TextCapture(lease, focus!));
            result.Add(descriptor);
        }
        return result;
#else
        return Array.Empty<GatewayNativeWindowDescriptor>();
#endif
    }

    public IGatewayDeferredAutomationOperation Begin(string windowHandle, double pixelX, double pixelY,
        GatewayAutomationContext context, string? pointerScopeHandle = null)
    {
        RequireMainThread();
        RequireAvailable();
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (context.CallerThreadId != Thread.CurrentThread.ManagedThreadId) throw Error("native_input_wrong_thread");
#if RIMWORLD_1_7
        RetireCompleted();
        if (active != null || textActive != null) throw Error("native_input_busy");
        GatewayNativePointerScope? scope = null;
        if (pointerScopeHandle != null)
        {
            scope = RequirePointerScope(pointerScopeHandle);
            string? scopeError = scope.Reject(DateTimeOffset.UtcNow, ReadPointer());
            if (scopeError != null) throw Error(scopeError);
        }
        else if (pointerScope != null && pointerScope.Result == null) throw Error("native_pointer_scope_busy");
        if (windowHandle == null || !captures.TryGetValue(windowHandle, out CaptureLease? capture))
            throw Error("stale_window_handle");
        GatewayNativeInputPoint point = capture.Descriptor.Geometry.ResolvePoint(pixelX, pixelY);
        if (point.Error != null) throw Error(point.Error);
        var backend = new ClickBackend(this, capture, pixelX, pixelY, point, scope);
        string? rejection = backend.Validate(false);
        if (rejection != null) throw Error(rejection);
        context.RunStep("native-click-admission", () => { });
        activeContext = context;
        activeBackend = backend;
        active = new GatewayNativeClickOperation(backend);
        activeResult = new GatewayNativeInputResultOperation(active, () => failure ?? backend.ScopeFailure);
        return activeResult;
#else
        throw Error("native_input_runtime_unsupported");
#endif
    }

    public IReadOnlyDictionary<string, object?> BeginPointerScope(DateTimeOffset deadlineUtc, GatewayAutomationContext context)
    {
        RequireMainThread();
        RequireAvailable();
#if RIMWORLD_1_7
        context.RunStep("native-pointer-scope-admission", () => { });
        RetireCompleted();
        if (active != null || textActive != null || pointerScope != null && pointerScope.Result == null) throw Error("native_input_busy");
        if (deadlineUtc <= DateTimeOffset.UtcNow) throw Error("native_pointer_scope_expired");
        Mouse mouse = Mouse.current;
        if (new[] { mouse.leftButton, mouse.rightButton, mouse.middleButton, mouse.forwardButton, mouse.backButton }
                .Any(button => button.isPressed || contract!.HasEdges(button)) ||
            Keyboard.current.anyKey.isPressed || contract!.HasEdges(Keyboard.current.anyKey) ||
            mouse.scroll.ReadValue() != Vector2.zero || GUIUtility.hotControl != 0 ||
            (bool)contract.ButtonActive.GetValue(null)!) throw Error("native_input_busy");
        GatewayNativePointerPosition original = ReadPointer();
        if (!double.IsFinite(original.X) || !double.IsFinite(original.Y)) throw Error("native_pointer_unavailable");
        pointerScope = new GatewayNativePointerScope(Guid.NewGuid().ToString("N"), original, deadlineUtc);
        return new Dictionary<string, object?> { ["scopeHandle"] = pointerScope.Handle, ["deadlineUtc"] = deadlineUtc.ToString("O") };
#else
        throw Error("native_input_runtime_unsupported");
#endif
    }

    public IGatewayDeferredAutomationOperation EndPointerScope(string handle, GatewayAutomationContext context)
    {
        RequireMainThread(); // Mandatory cleanup remains available after provider failure/cancellation.
#if RIMWORLD_1_7
        GatewayNativePointerScope scope = RequirePointerScope(handle);
        scope.RequestEnd();
        TickPointerScope();
        return new PointerScopeEndOperation(scope);
#else
        throw Error("native_input_runtime_unsupported");
#endif
    }

    public void TickPointerScope()
    {
        RequireMainThread();
#if RIMWORLD_1_7
        GatewayNativePointerScope? scope = pointerScope;
        if (scope == null || scope.Result != null) return;
        if (scope.IsExpired(DateTimeOffset.UtcNow) || failure != null) scope.RequestEnd();
        if (!scope.Ending) return;
        if (active != null && active.Advance(false) == null && ReferenceEquals(activeBackend?.Scope, scope))
        { active.Advance(true); return; }
        if (textActive != null && textActive.Advance(false) == null)
        { textActive.Advance(true); return; }
        scope.Close(ReadPointer, point =>
        {
            Mouse mouse = Mouse.current;
            if (new[] { mouse.leftButton, mouse.rightButton, mouse.middleButton, mouse.forwardButton, mouse.backButton }
                    .Any(button => button.isPressed) || Keyboard.current.anyKey.isPressed)
                throw Error("native_input_busy");
            InputSystem.QueueMouseMove(new Vector2((float)point.X, (float)point.Y));
        });
#endif
    }

    // Returns the actual partial/cleanup receipt. Shutdown is allowed to clean owned input even
    // when cancellation or the operation deadline has already elapsed.
    public GatewayDeferredAutomationResult? Shutdown()
    {
        RequireMainThread();
#if RIMWORLD_1_7
        failure ??= "native_input_shutdown";
        if (active != null && active.Advance(false) == null)
        {
            active.BeforeDispatch(true);
            active.AfterDispatch(false);
        }
        GatewayDeferredAutomationResult? result = activeResult?.Advance(false);
        if (textActive != null && textActive.Advance(false) == null)
        {
            textActive.BeforeDispatch(true);
            textActive.AfterDispatch(false);
        }
        result ??= textResult?.Advance(false);
        pointerScope?.RequestEnd();
        TickPointerScope();
        if (pointerScope?.SnapshotForShutdown() is GatewayDeferredAutomationResult scopeResult &&
            scopeResult.Result is IReadOnlyDictionary<string, object?> scopeReceipt)
        {
            var combined = scopeReceipt.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (result != null) combined["gestureReceipt"] = result.Result;
            bool gestureClean = result == null || result.Result is IReadOnlyDictionary<string, object?> gesture &&
                gesture.TryGetValue("cleanupComplete", out var clean) && clean is true;
            combined["cleanupComplete"] = scopeResult.Error == null && gestureClean;
            var error = scopeResult.Error ?? result?.Error;
            result = error == null ? GatewayDeferredAutomationResult.Success(combined) :
                GatewayDeferredAutomationResult.Failure(error.Code, error.Message, combined);
        }
        if (!RemoveHooks())
            return GatewayDeferredAutomationResult.Failure("native_input_shutdown_incomplete",
                "Native input hook cleanup was incomplete.", result?.Result);
        captures.Clear();
        textCaptures.Clear();
        lastFocusedText = null;
        return result;
#else
        return null;
#endif
    }

    public void Dispose() => Shutdown();

    private static void RequireMainThread()
    {
        if (!UnityData.IsInMainThread) throw Error("native_input_wrong_thread");
    }
    private void RequireAvailable()
    {
        string? reason = UnavailableReason;
        if (reason != null) throw Error(reason);
    }
    private static GatewayAutomationException Error(string code)
        => new(code, "Native window input is unavailable for the current context.");

#if RIMWORLD_1_7
    // Same inventory policy as VerseGatewaySnapshotSource. A larger stack is rejected rather
    // than partially validated through unbounded native hit testing.
    private const int MaximumWindows = 128;
    private static VerseGatewayNativeWindowInput? installed;
    private readonly Dictionary<string, CaptureLease> captures = new(StringComparer.Ordinal);
    private GatewayNativePointerScope? pointerScope;
    private static GatewayNativePointerPosition ReadPointer()
    {
        Vector2 point = Mouse.current.position.ReadValue();
        return new(point.x, point.y);
    }
    private GatewayNativePointerScope RequirePointerScope(string handle) => pointerScope?.Handle == handle
        ? pointerScope : throw Error("stale_pointer_scope_handle");
    private sealed class PointerScopeEndOperation : IGatewayDeferredAutomationOperation
    {
        private readonly GatewayNativePointerScope scope;
        public PointerScopeEndOperation(GatewayNativePointerScope scope) => this.scope = scope;
        public GatewayDeferredAutomationResult? Advance(bool stopRequested) => scope.Result;
    }
    private NativeContract? contract;
    private Harmony? harmony;
    private volatile string? failure;
    private object? dispatchInstance;
    private object? dispatchHost;
    private GatewayNativeClickOperation? active;
    private GatewayNativeInputResultOperation? activeResult;
    private ClickBackend? activeBackend;
    private GatewayAutomationContext? activeContext;
    private DispatchFrame? currentDispatchFrame;
    private bool dispatchWillRun = true;
    private int inputGeneration;
    private volatile bool inputResetObserved;
    private readonly object resetAdmissionGate = new();
    private GatewayNativeResetAdmission? resetAdmission;
    private long resetObservationVersion;
    private readonly MouseGuardObservation outerMouseGuard = new();
    private readonly MouseGuardObservation eventMouseGuard = new();

    private bool ValidExtent() => contract != null && dispatchHost != null && Screen.width > 0 &&
        Screen.height > 0 && (float)contract.DisplayHeight.GetValue(dispatchHost)! == Screen.height;

    private static MethodInfo Hook(string name) => typeof(VerseGatewayNativeWindowInput)
        .GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;

    private bool RemoveHooks()
    {
        bool complete = true;
        if (harmony != null && contract != null)
        {
            foreach (string callback in new[] { nameof(TextDispatchHooks.BeforeNativeTextDispatch),
                nameof(TextDispatchHooks.AfterNativeTextDispatch), nameof(TextDispatchHooks.FinalizeNativeTextDispatch) })
            {
                try { harmony.Unpatch(contract.Dispatch, TextHook(callback)); }
                catch { complete = false; }
            }
            if (textContract != null)
            {
                foreach (var patch in new[] {
                    (textContract.TextField, nameof(BeforeNativeTextField)),
                    (textContract.TextField, nameof(ObserveNativeTextField)),
                    (textContract.TextField, nameof(FinalizeNativeTextField)),
                    (textContract.TrackControl, nameof(ObserveNativeTextControl)) })
                {
                    try { harmony.Unpatch(patch.Item1, Hook(patch.Item2)); }
                    catch { complete = false; }
                }
            }
            foreach (var patch in new[]
            {
                (contract.Dispatch, nameof(BeforeNativeDispatch)),
                (contract.Dispatch, nameof(AfterNativeDispatch)),
                (contract.Dispatch, nameof(FinalizeNativeDispatch)),
                (contract.Dispatch, nameof(ObserveMouseGuardStream)),
                (contract.EventDispatch, nameof(BeforeNativeMousePass)),
                (contract.EventDispatch, nameof(CaptureNativeMousePass)),
                (contract.EventDispatch, nameof(AfterNativeMousePass)),
                (contract.EventDispatch, nameof(ObserveMouseGuardStream)),
                (contract.FrameReset, nameof(AfterInputFrameReset)),
                (contract.FrameReset, nameof(FinalizeInputFrameReset)),
                (contract.FrameResetBoundary, nameof(AfterInputResetBoundary)),
                (contract.FrameResetBoundary, nameof(ObserveResetBoundaryStream)),
                (contract.FrameResetBoundary, nameof(FinalizeInputFrameReset))
            })
            {
                try { harmony.Unpatch(patch.Item1, Hook(patch.Item2)); }
                catch { complete = false; }
            }
        }
        if (complete && ReferenceEquals(installed, this)) installed = null;
        if (!complete) failure = "native_input_shutdown_incomplete";
        return complete;
    }

    private static void AfterInputFrameReset(bool __runOriginal)
        => installed?.ObserveInputReset(__runOriginal, advanceGeneration: false);

    private static void AfterInputResetBoundary(bool __runOriginal)
    {
        VerseGatewayNativeWindowInput? service = installed;
        if (service == null) return;
        lock (service.resetAdmissionGate)
            service.ObserveInputReset(__runOriginal && service.HasPublishedTerminalResetObserver(),
                advanceGeneration: true);
    }

    private byte[]? PublishedResetMetadata() => PublishedMetadata(contract!.FrameResetBoundary);

    private byte[]? PublishedMetadata(MethodBase method)
    {
        // Harmony uses this exact dictionary lock for both reads and publication. Each rebuild
        // publishes freshly serialized bytes only after replacement construction and detouring.
        var state = (Dictionary<MethodBase, byte[]>)contract!.PatchState.GetValue(null)!;
        lock (state)
            return state.TryGetValue(method, out byte[]? bytes) ? bytes : null;
    }

    private sealed class MouseGuardObservation
    {
        public long Version;
        public GatewayNativeResetAdmission? Stream;
        public GatewayNativeResetAdmission? Prefix;
        public GatewayNativeResetAdmission? Entry;
    }

    private bool HasPublishedMouseGuards()
    {
        lock (resetAdmissionGate)
        {
            try
            {
                return CheckMouseGuardPublication(contract!.Dispatch, outerMouseGuard, prefix: false) &&
                    CheckMouseGuardPublication(contract.EventDispatch, eventMouseGuard, prefix: true);
            }
            catch { return false; }
        }
    }

    private bool CheckMouseGuardPublication(MethodInfo method, MouseGuardObservation observation, bool prefix)
    {
        byte[]? publication = PublishedMetadata(method);
        if (publication == null || observation.Stream == null || observation.Prefix == null || observation.Entry == null) return false;
        if (observation.Stream.IsAccepted(publication) && (!prefix ||
            observation.Prefix.IsAccepted(publication) && observation.Entry.IsAccepted(publication))) return true;
        PatchInfo info = PatchInfoSerialization.Deserialize(publication);
        if (info.transpilers.Concat(prefix ? info.prefixes : Array.Empty<Patch>())
            .Any(patch => GatewayNativeResetAdmission.IsFactory(patch.PatchMethod))) return false;
        bool Prove(Patch[] patches, string hook, GatewayNativeResetAdmission receipt, bool first = false)
        {
            MethodInfo owned = Hook(hook);
            if (patches.Count(patch => patch.PatchMethod == owned) != 1) return false;
            Patch[] sorted = new PatchSorter(patches, debug: false).Sort();
            if (first) Array.Reverse(sorted);
            return sorted.Length != 0 && sorted[sorted.Length - 1].owner == "fumblesneeze.rimworlddevgateway" &&
                receipt.TryAccept(publication, sorted.Select(patch => patch.PatchMethod).ToArray(), owned,
                    info.innerprefixes.Length, info.innerpostfixes.Length, info.innerfinalizers.Length);
        }
        return Prove(info.transpilers, nameof(ObserveMouseGuardStream), observation.Stream) &&
            (!prefix || Prove(info.prefixes, nameof(CaptureNativeMousePass), observation.Entry, first: true) &&
                Prove(info.prefixes, nameof(BeforeNativeMousePass), observation.Prefix));
    }

    private static IEnumerable<CodeInstruction> ObserveMouseGuardStream(IEnumerable<CodeInstruction> instructions,
        MethodBase __originalMethod)
    {
        VerseGatewayNativeWindowInput? service = installed;
        if (service == null) return instructions;
        MouseGuardObservation observation = __originalMethod == service.contract!.Dispatch
            ? service.outerMouseGuard : service.eventMouseGuard;
        long version;
        byte[]? previous;
        lock (service.resetAdmissionGate)
        {
            version = ++observation.Version;
            observation.Stream = observation.Prefix = observation.Entry = null;
            previous = service.PublishedMetadata(__originalMethod);
            if (service.active != null || service.textActive != null) service.failure ??= "native_mouse_guard_rebuilt_during_gesture";
        }
        var body = instructions.ToList();
        bool supported = true;
        if (__originalMethod == service.contract.Dispatch)
            supported = service.contract.GuardReleaseTail(body, Hook(nameof(ApplyNativeReleaseTail)));
        else
        {
            try { supported = service.contract.SupportsEventBody(body); }
            catch { supported = false; }
        }
        lock (service.resetAdmissionGate)
        {
            if (observation.Version != version) return body;
            observation.Stream = new GatewayNativeResetAdmission(previous, supported);
            observation.Prefix = new GatewayNativeResetAdmission(previous, supported);
            observation.Entry = new GatewayNativeResetAdmission(previous, supported);
            if (!supported) service.failure ??= "native_mouse_body_contract_unsupported";
        }
        return body;
    }

    // Replaces only the inspected caller's release-tail setter, not arbitrary GUI setters.
    private static void ApplyNativeReleaseTail(int value)
    {
        DispatchFrame? frame = installed?.currentDispatchFrame;
        if (frame == null) GUIUtility.hotControl = value;
        else frame.MousePass.ApplyReleaseTail(frame.Backend.HasQueuedRelease, value,
            static control => GUIUtility.hotControl = control);
    }

    // Called under resetAdmissionGate. Sort examines declarations/dependencies only; unlike
    // GetSortedPatchMethods or Patch.GetMethod, it never invokes a third-party patch factory.
    private bool HasPublishedTerminalResetObserver()
    {
        try
        {
            GatewayNativeResetAdmission? admission = resetAdmission;
            byte[]? publication = PublishedResetMetadata();
            if (admission == null || publication == null) return false;
            if (admission.IsAccepted(publication)) return true;
            PatchInfo info = PatchInfoSerialization.Deserialize(publication);
            MethodInfo observer = Hook(nameof(ObserveResetBoundaryStream));
            if (info.transpilers.Any(patch => GatewayNativeResetAdmission.IsFactory(patch.PatchMethod)) ||
                info.transpilers.Count(patch => patch.PatchMethod == observer) != 1) return false;
            Patch[] sorted = new PatchSorter(info.transpilers, debug: false).Sort();
            if (sorted.Length == 0 || sorted[sorted.Length - 1].owner != "fumblesneeze.rimworlddevgateway")
                return false;
            return admission.TryAccept(publication, sorted.Select(patch => patch.PatchMethod).ToArray(), observer,
                info.innerprefixes.Length, info.innerpostfixes.Length, info.innerfinalizers.Length);
        }
        catch { return false; }
    }

    private void ObserveInputReset(bool originalRan, bool advanceGeneration)
    {
        if (failure != null) return;
        try
        {
            // Only the verified caller advances generation; the tiny wrapper may be inlined.
            // When its hook does run, it remains an additional skip/exception guard.
            Mouse mouse = Mouse.current;
            NativeContract shape = contract!;
            if (!originalRan || !UnityData.IsInMainThread ||
                shape.HasEdges(mouse.leftButton) || shape.HasEdges(mouse.rightButton) ||
                shape.HasEdges(mouse.middleButton) || shape.HasEdges(mouse.forwardButton) ||
                shape.HasEdges(mouse.backButton) || shape.HasEdges(Keyboard.current.anyKey) ||
                mouse.scroll.ReadValue() != Vector2.zero)
            {
                RejectInputReset();
                return;
            }
            if (advanceGeneration)
            {
                inputGeneration = unchecked(inputGeneration + 1);
                inputResetObserved = true;
            }
        }
        catch { RejectInputReset(); }
    }

    // Observe only the incoming stream of the normal Harmony rebuild. Never invoke foreign
    // transpilers as an inspection probe or change the game's instructions here.
    private static IEnumerable<CodeInstruction> ObserveResetBoundaryStream(IEnumerable<CodeInstruction> instructions)
    {
        VerseGatewayNativeWindowInput? service = installed;
        if (service == null) return instructions;
        long observation;
        byte[]? previousPublication;
        lock (service.resetAdmissionGate)
        {
            observation = ++service.resetObservationVersion;
            service.resetAdmission = null;
            service.inputResetObserved = false;
            service.activeBackend?.InvalidateEdgeOwnership();
            service.textBackend?.InvalidateEdgeOwnership();
            if (service.active != null || service.textActive != null) service.failure ??= "native_input_reset_rebuilt_during_gesture";
            previousPublication = service.PublishedResetMetadata();
        }
        // Invalidate before enumerating any lazily supplied incoming stream. An unsuccessful
        // rebuild must not leave the previous admission active. Nested observations supersede us.
        var body = instructions.ToList();
        bool supported;
        try { supported = service.contract!.SupportsResetBoundary(body); }
        catch { supported = false; }
        lock (service.resetAdmissionGate)
        {
            if (service.resetObservationVersion != observation) return body;
            if (!supported) service.failure ??= "native_input_reset_contract_unsupported";
            service.resetAdmission = new GatewayNativeResetAdmission(previousPublication, supported);
        }
        return body;
    }

    private static Exception? FinalizeInputFrameReset(Exception? __exception)
    {
        if (__exception != null) installed?.RejectInputReset();
        return __exception;
    }

    private void RejectInputReset()
    {
        failure = "native_input_reset_unavailable";
        inputResetObserved = false;
        activeBackend?.InvalidateEdgeOwnership();
        textBackend?.InvalidateEdgeOwnership();
    }

    private void RetireCompleted()
    {
        // Freeze the provider projection at the native completion boundary, before a future
        // shutdown or failed input frame can change the meaning of this finished gesture.
        if (active != null && activeResult!.Advance(false) != null && !active.IsQuarantined)
        {
            active = null; activeResult = null; activeBackend = null; activeContext = null;
        }
        if (textActive != null && textResult!.Advance(false) != null && !textActive.IsQuarantined)
        {
            textActive = null; textResult = null; textBackend = null; textContext = null;
        }
    }

    private static bool BeforeNativeDispatch(object __instance, object __0, bool __runOriginal,
        out DispatchFrame? __state)
    {
        __state = null;
        VerseGatewayNativeWindowInput? service = installed;
        if (service == null || !UnityData.IsInMainThread) return true;
        if (service.currentDispatchFrame != null || service.textFrame != null)
        {
            // A nested dispatch could replay the outer gesture's raw edges.
            service.failure = "native_input_reentrant_dispatch";
            return false;
        }
        service.dispatchInstance = __instance;
        service.dispatchHost = __0;
        service.RetireCompleted();
        service.TickPointerScope();
        if (service.active == null || service.active.IsQuarantined) return true;
        var frame = new DispatchFrame(service, service.active, service.activeBackend!);
        __state = frame;
        service.currentDispatchFrame = frame;
        service.dispatchWillRun = __runOriginal;
        frame.Backend.BeginDispatch();
        try
        {
            if (!service.inputResetObserved) service.failure ??= "native_input_reset_unavailable";
            if (!service.HasPublishedMouseGuards()) service.failure ??= "native_mouse_guard_unavailable";
            bool stop = service.failure != null || service.StopRequested();
            frame.Operation.BeforeDispatch(stop);
            // Either edge may synchronously invoke callbacks. Also suppress a rejected held
            // press: native dispatch could otherwise turn it into a drag on the new context.
            stop |= service.failure != null || !service.inputResetObserved || service.StopRequested();
            if (frame.Backend.MustSuppressDispatch(stop))
            {
                frame.Operation.BeforeDispatch(true);
                return false;
            }
            return true;
        }
        finally { frame.Backend.EndDispatchPreparation(); }
    }

    private bool StopRequested()
    {
        if (activeBackend?.ScopeFailure != null) return true;
        try { activeContext!.RunStep("native-click-dispatch", () => { }); return false; }
        catch (OperationCanceledException) { return true; }
        catch (GatewayAutomationTimeoutException) { return true; }
    }

    private static void AfterNativeDispatch(bool __runOriginal, DispatchFrame? __state)
        => __state?.Finish(__runOriginal);

    // DispatchInput's mouse helper runs layout first. Guard the resulting event here, before
    // GUI.DispatchInputToWindows or any game OnGUI callback sees it. Other events pass unchanged.
    private static void CaptureNativeMousePass(object __instance, Vector2 __0, bool __1,
        out MousePassInvocation? __state)
    {
        __state = null;
        VerseGatewayNativeWindowInput? service = installed;
        DispatchFrame? frame = service?.currentDispatchFrame;
        Event? input = Event.current;
        if (service != null && UnityData.IsInMainThread && input != null && ReferenceEquals(__instance, service.dispatchInstance))
        {
            if (input.rawType is EventType.layout or EventType.repaint) service.lastFocusedText = null;
            service.textFrame?.Capture(input, __1);
        }
        if (frame == null || !UnityData.IsInMainThread || input == null ||
            !ReferenceEquals(__instance, service!.dispatchInstance) ||
            !ReferenceEquals(frame.Operation, service.active)) return;
        frame.Backend.RecordPassCapture(input, __1);
        __state = new MousePassInvocation(service, frame, __instance, new GatewayNativeMouseProvenance(
            !__1 && frame.Backend.OwnsMousePass(input), input, __1, input.button,
            (int)input.type, (int)input.rawType, __0.x, __0.y));
    }

    private static bool BeforeNativeMousePass(object __instance, Vector2 __0, bool __1,
        bool __runOriginal, MousePassInvocation? __state)
    {
        // Rebuilding during layout can put an unsupported prefix ahead of our capture. Its
        // missing/disowned state cannot be trusted until the exact active frame's proof is checked.
        VerseGatewayNativeWindowInput? activeService = installed;
        if (activeService?.textFrame is TextFrame textFrame)
        {
            if (!UnityData.IsInMainThread || !ReferenceEquals(__instance, activeService.dispatchInstance)) return false;
            return textFrame.Before(Event.current, __1, __runOriginal);
        }
        DispatchFrame? activeFrame = activeService?.currentDispatchFrame;
        bool exactActiveFrame = activeFrame != null && UnityData.IsInMainThread &&
            ReferenceEquals(__instance, activeService!.dispatchInstance) &&
            ReferenceEquals(activeFrame.Operation, activeService.active);
        bool? decision = GatewayNativeMousePassGate.BeforeProvenance(exactActiveFrame,
            !exactActiveFrame || activeService!.HasPublishedMouseGuards(),
            !exactActiveFrame || activeService!.failure == null && activeService.inputResetObserved,
            __state?.Provenance);
        if (decision.HasValue)
        {
            if (!decision.Value && exactActiveFrame)
                activeFrame!.MousePass.BeforePass(false, "native_mouse_guard_unavailable", true, false);
            return decision.Value;
        }
        VerseGatewayNativeWindowInput service = __state!.Service;
        DispatchFrame frame = __state.Frame;
        string? rejection;
        bool stop;
        try
        {
            Event? input = Event.current;
            bool intact = ReferenceEquals(installed, service) && ReferenceEquals(service.currentDispatchFrame, frame) &&
                ReferenceEquals(service.active, frame.Operation) && ReferenceEquals(__instance, __state.Dispatcher) &&
                input != null && __state.Provenance.Matches(input, __1, input.button, (int)input.type,
                    (int)input.rawType, __0.x, __0.y);
            rejection = intact ? frame.Backend.ValidateMousePass(input!, __0) : "native_mouse_pass_changed";
            stop = service.failure != null || !service.inputResetObserved || service.StopRequested();
        }
        catch { rejection = "native_mouse_pass_validation_failed"; stop = true; }
        return frame.MousePass.BeforePass(__runOriginal, rejection, stop, service.HasPublishedMouseGuards());
    }

    private static void AfterNativeMousePass(bool __runOriginal, MousePassInvocation? __state)
    {
        installed?.textFrame?.After(__runOriginal);
        if (__state?.Provenance.Owned == true) __state.Frame.MousePass.AfterPass(__runOriginal);
    }

    private sealed class MousePassInvocation
    {
        public MousePassInvocation(VerseGatewayNativeWindowInput service, DispatchFrame frame, object dispatcher,
            GatewayNativeMouseProvenance provenance)
        { Service = service; Frame = frame; Dispatcher = dispatcher; Provenance = provenance; }
        public VerseGatewayNativeWindowInput Service { get; }
        public DispatchFrame Frame { get; }
        public object Dispatcher { get; }
        public GatewayNativeMouseProvenance Provenance { get; }
    }

    private static Exception? FinalizeNativeDispatch(Exception? __exception, DispatchFrame? __state)
    {
        __state?.Finish(__exception == null && __state.OriginalObserved);
        return __exception;
    }

    private sealed class DispatchFrame
    {
        private readonly VerseGatewayNativeWindowInput service;
        private bool finished;
        public DispatchFrame(VerseGatewayNativeWindowInput service, GatewayNativeClickOperation operation, ClickBackend backend)
        { this.service = service; Operation = operation; Backend = backend; }
        public GatewayNativeClickOperation Operation { get; }
        public ClickBackend Backend { get; }
        public GatewayNativeMousePassGate MousePass { get; } = new();
        public bool OriginalObserved { get; private set; }
        public void Finish(bool originalRan)
        {
            if (finished) return;
            finished = true;
            OriginalObserved = originalRan;
            try
            {
                bool delivered = originalRan && MousePass.Dispatched;
                Backend.RecordDispatchResult(originalRan, MousePass);
                Backend.ObserveDispatch(delivered);
                Operation.AfterDispatch(delivered);
                service.RetireCompleted();
                service.TickPointerScope();
            }
            finally
            {
                if (ReferenceEquals(service.currentDispatchFrame, this)) service.currentDispatchFrame = null;
            }
        }
    }

    private sealed class CaptureLease
    {
        public CaptureLease(Window window, WindowStack stack, GatewayNativeWindowDescriptor descriptor,
            object dispatcher, object host)
        { Window = window; Stack = stack; Descriptor = descriptor; Dispatcher = dispatcher; Host = host;
          Identity = new GatewayNativeWindowLease(window, stack); }
        public Window Window { get; }
        public WindowStack Stack { get; }
        public GatewayNativeWindowDescriptor Descriptor { get; }
        public GatewayNativeWindowLease Identity { get; }
        public object Dispatcher { get; }
        public object Host { get; }
    }

    private sealed class ClickBackend : IGatewayNativeClickBackend, IGatewayNativeScopedClickBackend
    {
        private readonly VerseGatewayNativeWindowInput service;
        private readonly CaptureLease capture;
        private readonly double pixelX, pixelY;
        private readonly Vector2 target, previousPointer;
        private bool pointerClaimed;
        private bool buttonClaimed;
        private bool observedPress;
        private bool guiUncertain;
        private bool foreignInput;
        private int ownedHotControl;
        private bool ownedButtonLatch;
        private Rect ownedButtonRect;
        private int ownedButtonNumber;
        public bool PressedThisDispatch { get; private set; }
        public bool HasQueuedRelease => queuedEdgeThisDispatch && !PressedThisDispatch;
        private bool preparingDispatch;
        private bool queuedEdgeThisDispatch;
        private bool rejectedThisDispatch;
        private bool injectionFailed;
        private readonly GatewayNativeInputEdgeReceipt edgeReceipt = new();
        private NativeContract Contract => service.contract!;
        public GatewayNativePointerScope? Scope { get; }
        private string pointerState = "not-owned";
        private int capturedPasses, capturedOwnedPasses;
        private string? lastCapturedPass;
        private bool originalDispatchObserved;
        private bool queuedPressEdge, queuedReleaseEdge;
        private GatewayNativeMousePassGate? observedMousePass;
        public string? ScopeFailure => Scope?.RejectLifetime(DateTimeOffset.UtcNow);
        public IReadOnlyDictionary<string, object?> PointerReceipt
        {
            get
            {
                var receipt = new Dictionary<string, object?>
                {
                    ["dispatchOriginalObserved"] = originalDispatchObserved,
                    ["capturedNativePasses"] = capturedPasses,
                    ["capturedOwnedMousePasses"] = capturedOwnedPasses,
                    ["lastCapturedPass"] = lastCapturedPass,
                    ["queuedPressEdgeObserved"] = queuedPressEdge,
                    ["queuedReleaseEdgeObserved"] = queuedReleaseEdge,
                    ["mouseOriginOffset"] = capture.Descriptor.Geometry.MouseOriginOffset,
                    ["mousePassSeen"] = observedMousePass?.Seen ?? false,
                    ["mousePassAdmitted"] = observedMousePass?.Admitted ?? false,
                    ["mousePassOriginalObserved"] = observedMousePass?.OriginalObserved ?? false,
                    ["mousePassRejection"] = observedMousePass?.Rejection
                };
                if (Scope != null)
                {
                    receipt["pointerScopeHandle"] = Scope.Handle;
                    receipt["pointerScopeState"] = pointerState;
                    receipt["pointerRestored"] = pointerState == "gesture-restored";
                }
                return receipt;
            }
        }

        public void RecordPassCapture(Event input, bool drawing)
        {
            capturedPasses++;
            bool owned = !drawing && OwnsMousePass(input);
            if (owned) capturedOwnedPasses++;
            lastCapturedPass = input.type + "/" + input.rawType + "/drawing=" + drawing;
        }

        public void RecordDispatchResult(bool originalRan, GatewayNativeMousePassGate mousePass)
        {
            originalDispatchObserved = originalRan;
            observedMousePass = mousePass;
        }

        public ClickBackend(VerseGatewayNativeWindowInput service, CaptureLease capture,
            double pixelX, double pixelY, GatewayNativeInputPoint point, GatewayNativePointerScope? scope)
        {
            this.service = service; this.capture = capture; this.pixelX = pixelX; this.pixelY = pixelY;
            target = new Vector2((float)point.InputX, (float)point.InputY);
            previousPointer = Mouse.current.position.ReadValue();
            Scope = scope;
        }

        public string? Validate(bool ownsButton)
        {
            string? rejection = ValidateInput(ownsButton);
            rejectedThisDispatch |= rejection != null;
            return rejection;
        }

        public void BeginDispatch()
        {
            preparingDispatch = true;
            PressedThisDispatch = queuedEdgeThisDispatch = rejectedThisDispatch = injectionFailed = false;
        }

        public void EndDispatchPreparation() => preparingDispatch = false;

        public void InvalidateEdgeOwnership() => edgeReceipt.Invalidate();

        public bool MustSuppressDispatch(bool stopRequested)
            => GatewayNativeWindowInputContract.SuppressDispatch(buttonClaimed, queuedEdgeThisDispatch,
                rejectedThisDispatch, stopRequested, injectionFailed);

        private string? ValidateInput(bool ownsButton)
        {
            if (ScopeFailure != null) return ScopeFailure;
            if (!service.dispatchWillRun) return "native_input_not_dispatched";
            Mouse mouse = Mouse.current;
            bool mouseActivity = ForeignLeftEdges(mouse.leftButton) ||
                new[] { mouse.rightButton, mouse.middleButton, mouse.forwardButton, mouse.backButton }
                    .Any(button => button.isPressed || Contract.HasEdges(button));
            ButtonControl keys = Keyboard.current.anyKey;
            bool keyboardActivity = keys.isPressed || Contract.HasEdges(keys) ||
                ((List<char>)Contract.TypedCharacters.GetValue(service.dispatchInstance)!).Count != 0;
            bool scrolling = mouse.scroll.ReadValue() != Vector2.zero;
            string? inputError = GatewayNativeWindowInputContract.RejectInput(ownsButton, mouse.leftButton.isPressed,
                mouseActivity, keyboardActivity, scrolling, GUIUtility.hotControl, ownedHotControl);
            foreignInput |= ownsButton && (mouseActivity || keyboardActivity || scrolling);
            if (inputError != null) return inputError;
            string? targetError = ValidateTarget();
            if (targetError != null) return targetError;
            if (!float.IsFinite(previousPointer.x) || !float.IsFinite(previousPointer.y))
                return "native_pointer_unavailable";
            if (!ownsButton && (bool)Contract.ButtonActive.GetValue(null)!) return "native_input_busy";
            if (ownsButton && guiUncertain) return "native_gui_ownership_unavailable";
            if (ownsButton && mouse.position.ReadValue() != target)
            { foreignInput = true; return "native_pointer_ownership_lost"; }
            // Begin only admitted the earlier state. Re-read after target checks at the
            // deferred first-press boundary, before this gesture claims or mutates input.
            if (!ownsButton && Scope != null)
                return Scope.RejectBeforePress(DateTimeOffset.UtcNow, ReadPointer);
            return null;
        }

        private string? ValidateTarget()
        {
            WindowStack? stack = Find.WindowStack;
            if (stack == null || !ReferenceEquals(stack, capture.Stack)) return "stale_window_stack";
            if (stack.Windows.Count > MaximumWindows) return "native_window_inventory_limit";
            Window? current = stack.Windows.FirstOrDefault(window => ReferenceEquals(window, capture.Window));
            if (current == null) return "stale_window_instance";
            string? leaseError = capture.Identity.Validate(current, stack);
            if (leaseError != null) return leaseError;
            if (current.ID != capture.Descriptor.NativeId ||
                !GatewayNativeWindowInputContract.SupportsWindowIdentity(current.ID, current is ImmediateWindow))
                return "stale_window_instance";
            if (!ReferenceEquals(service.dispatchInstance, capture.Dispatcher) ||
                !ReferenceEquals(service.dispatchHost, capture.Host)) return "stale_native_dispatcher";
            GatewayNativeWindowGeometry geometry = capture.Descriptor.Geometry;
            Rect bounds = current.windowRect;
            if (bounds.x != geometry.X || bounds.y != geometry.Y || bounds.width != geometry.Width ||
                bounds.height != geometry.Height || Prefs.UIScale != geometry.UiScale ||
                Screen.width != geometry.PixelWidth || Screen.height != geometry.PixelHeight || !service.ValidExtent())
                return "stale_window_geometry";
            string? pointError = geometry.ResolvePoint(pixelX, pixelY).Error;
            if (pointError != null) return pointError;
            if (!stack.GetsInput(current) || !ReferenceEquals(stack.GetWindowAt(
                new Vector2((float)(pixelX / geometry.UiScale), (float)(pixelY / geometry.UiScale))), current))
                return "native_window_obscured";
            return null;
        }

        public void Press()
        {
            string? rejection = Scope?.RejectBeforePress(DateTimeOffset.UtcNow, ReadPointer);
            if (rejection != null) throw Error(rejection);
            pointerClaimed = buttonClaimed = PressedThisDispatch = true;
            try
            {
                queuedEdgeThisDispatch = true;
                InputSystem.QueueMouseButton(0, true, target);
                queuedPressEdge = (bool)Contract.PressEdge.GetValue(Mouse.current.leftButton)!;
                edgeReceipt.Record(service.inputGeneration, pressed: true);
                ValidateQueuedEdge();
            }
            catch { injectionFailed = true; throw; }
        }

        private void ValidateQueuedEdge()
        {
            // Cleanup release is mandatory after dispatch and must not be rejected merely
            // because the target vanished or the context was cancelled.
            if (!preparingDispatch) return;
            if (ValidateTarget() != null)
            {
                rejectedThisDispatch = true;
                throw Error("native_window_changed_during_injection");
            }
        }

        public void Release()
        {
            if (!buttonClaimed) return;
            Mouse mouse = Mouse.current;
            foreignInput |= observedPress && (Keyboard.current.anyKey.isPressed ||
                Contract.HasEdges(Keyboard.current.anyKey) || mouse.scroll.ReadValue() != Vector2.zero ||
                new[] { mouse.rightButton, mouse.middleButton, mouse.forwardButton, mouse.backButton }
                    .Any(button => button.isPressed || Contract.HasEdges(button)));
            // A replacement physical press is not ours to release.
            if (observedPress && ForeignLeftEdges(mouse.leftButton))
            {
                foreignInput = true;
                throw Error("native_input_ownership_lost");
            }
            try
            {
                queuedEdgeThisDispatch |= preparingDispatch;
                InputSystem.QueueMouseButton(0, false, mouse.position.ReadValue());
                queuedReleaseEdge = (bool)Contract.ReleaseEdge.GetValue(mouse.leftButton)!;
                edgeReceipt.Record(service.inputGeneration, pressed: false);
                buttonClaimed = false;
                ValidateQueuedEdge();
            }
            catch { injectionFailed |= preparingDispatch; throw; }
        }

        private bool ForeignLeftEdges(ButtonControl button)
            => edgeReceipt.HasForeignEdges(service.inputGeneration, (bool)Contract.PressEdge.GetValue(button)!,
                (bool)Contract.ReleaseEdge.GetValue(button)!, (bool)Contract.RepeatEdge.GetValue(button)!);

        public bool OwnsMousePass(Event input)
            => input.button == 0 && (queuedEdgeThisDispatch || buttonClaimed) &&
               input.rawType is EventType.mouseDown or EventType.mouseUp or EventType.mouseDrag;

        public string? ValidateMousePass(Event input, Vector2 physicalPoint)
        {
            EventType expected = PressedThisDispatch ? EventType.mouseDown : EventType.mouseUp;
            if (!queuedEdgeThisDispatch || input.rawType != expected || input.type != expected ||
                physicalPoint.x != (float)pixelX || physicalPoint.y != (float)pixelY)
                return "native_mouse_pass_changed";
            if (ForeignLeftEdges(Mouse.current.leftButton))
            {
                foreignInput = true;
                return "native_input_ownership_lost";
            }
            return ValidateTarget();
        }

        public void ObserveDispatch(bool originalRan)
        {
            if (!PressedThisDispatch || observedPress || !originalRan) return;
            observedPress = true;
            try
            {
                ownedHotControl = GUIUtility.hotControl;
                if (ownedHotControl != 0 && !Contract.ControlBelongsTo(ownedHotControl, capture.Descriptor.NativeId))
                    guiUncertain = true;
                ownedButtonLatch = (bool)Contract.ButtonActive.GetValue(null)!;
                if (ownedButtonLatch)
                {
                    ownedButtonRect = (Rect)Contract.ButtonRect.GetValue(null)!;
                    ownedButtonNumber = (int)Contract.ButtonNumber.GetValue(null)!;
                    if (ownedButtonNumber != 0) guiUncertain = true;
                }
            }
            catch { guiUncertain = true; }
        }

        public void RestorePointer()
            => CompletePointer(false);

        public void CompletePointer(bool successfulGesture)
        {
            Exception? cleanupFailure = null;
            try { RestoreGuiOwnership(); }
            catch (Exception exception) { cleanupFailure = exception; }
            try
            {
                if (pointerClaimed)
                {
                    if (Mouse.current.position.ReadValue() != target) throw Error("native_pointer_ownership_lost");
                    DateTimeOffset completedAt = DateTimeOffset.UtcNow;
                    if (successfulGesture && cleanupFailure == null && Scope != null &&
                        Scope.RejectLifetime(completedAt) == null && service.failure == null &&
                        service.inputResetObserved && !service.StopRequested())
                    {
                        Scope.Retain(completedAt, ReadPointer(), new(target.x, target.y));
                        pointerState = "retained";
                    }
                    else
                    {
                        InputSystem.QueueMouseMove(previousPointer);
                        if (Mouse.current.position.ReadValue() != previousPointer) throw Error("native_pointer_restore_unconfirmed");
                        pointerState = "gesture-restored";
                    }
                    pointerClaimed = false;
                }
            }
            catch (Exception exception)
            { cleanupFailure = cleanupFailure == null ? exception : new AggregateException(cleanupFailure, exception); }
            if (cleanupFailure != null) throw cleanupFailure;
        }

        private void RestoreGuiOwnership()
        {
            if (!observedPress) return;
            if (guiUncertain || foreignInput) throw Error("native_gui_ownership_unavailable");
            bool sameStack = ReferenceEquals(Find.WindowStack, capture.Stack);
            int currentHot = GUIUtility.hotControl;
            if (currentHot != 0)
            {
                bool owned = currentHot == ownedHotControl && sameStack &&
                    Contract.ControlBelongsTo(currentHot, capture.Descriptor.NativeId);
                DispatchFrame? frame = service.currentDispatchFrame;
                bool rejectedRelease = frame != null && ReferenceEquals(frame.Backend, this) &&
                    HasQueuedRelease && !frame.MousePass.Dispatched;
                bool otherOwner = sameStack && Contract.ControlWindows.GetValue(null) is Dictionary<int, int> owners &&
                    owners.TryGetValue(currentHot, out int owner) && owner != capture.Descriptor.NativeId &&
                    capture.Stack.Windows.Count(window => window.ID == owner &&
                        GatewayNativeWindowInputContract.SupportsWindowIdentity(window.ID, window is ImmediateWindow)) == 1;
                if (owned) GUIUtility.hotControl = 0;
                else if (!GatewayNativeWindowInputContract.PreserveReplacementHotControl(rejectedRelease,
                    capture.Stack.Windows.Any(window => ReferenceEquals(window, capture.Window)), otherOwner))
                    throw Error("native_gui_ownership_unavailable");
            }
            if (ownedButtonLatch && (bool)Contract.ButtonActive.GetValue(null)!)
            {
                if (!sameStack || (Rect)Contract.ButtonRect.GetValue(null)! != ownedButtonRect ||
                    (int)Contract.ButtonNumber.GetValue(null)! != ownedButtonNumber)
                    throw Error("native_gui_ownership_unavailable");
                Contract.ButtonActive.SetValue(null, false);
            }
        }
    }

    private sealed class NativeContract
    {
        public required MethodInfo Dispatch { get; init; }
        public required MethodInfo EventDispatch { get; init; }
        public required MethodInfo MouseInputPass { get; init; }
        public required MethodInfo HotControlSetter { get; init; }
        public required IReadOnlyList<GatewayNativeInstructionShape> OriginalEventBody { get; init; }
        public required MethodInfo FrameReset { get; init; }
        public required MethodInfo FrameResetBoundary { get; init; }
        public required MethodInfo PostResetPhase { get; init; }
        public required double MouseOriginOffset { get; init; }
        public required PropertyInfo DisplayHeight { get; init; }
        public required PropertyInfo PressEdge { get; init; }
        public required PropertyInfo ReleaseEdge { get; init; }
        public required PropertyInfo RepeatEdge { get; init; }
        public required FieldInfo TypedCharacters { get; init; }
        public required FieldInfo ButtonActive { get; init; }
        public required FieldInfo ButtonRect { get; init; }
        public required FieldInfo ButtonNumber { get; init; }
        public required FieldInfo ControlWindows { get; init; }
        public required FieldInfo PatchState { get; init; }

        public bool HasEdges(ButtonControl button) => (bool)PressEdge.GetValue(button)! ||
            (bool)ReleaseEdge.GetValue(button)! || (bool)RepeatEdge.GetValue(button)!;
        public bool ControlBelongsTo(int control, int window)
            => ControlWindows.GetValue(null) is Dictionary<int, int> owners &&
               owners.TryGetValue(control, out int owner) && owner == window;

        public bool GuardReleaseTail(IReadOnlyList<CodeInstruction> body, MethodInfo guardedSetter)
        {
            int[] calls = Enumerable.Range(0, body.Count).Where(index => body[index].Calls(HotControlSetter)).ToArray();
            if (calls.Length != 1) return false;
            int index = calls[0];
            // RunInputPass(mouseUp, button, point, mods); if (button == 0) hotControl = 0.
            // Bind the button load and branch target without depending on numeric local slots.
            if (index < 8 || index + 1 >= body.Count || body[index - 1].opcode != OpCodes.Ldc_I4_0 ||
                !body[index - 4].Calls(MouseInputPass) || body[index - 8].opcode != OpCodes.Ldc_I4_1 ||
                body[index - 3].opcode != body[index - 7].opcode ||
                !Equals(body[index - 3].operand, body[index - 7].operand) ||
                body[index - 2].opcode != OpCodes.Brtrue && body[index - 2].opcode != OpCodes.Brtrue_S ||
                body[index - 2].operand is not Label end || !body[index + 1].labels.Contains(end)) return false;
            body[index].operand = guardedSetter; // Identical static void(int) stack effect; retain labels/EH markers.
            return true;
        }

        public bool SupportsEventBody(IReadOnlyList<CodeInstruction> body)
            => GatewayNativeInstructionShape.SameBody(OriginalEventBody, NormalizeEventBody(body, EventDispatch));

        private static IReadOnlyList<GatewayNativeInstructionShape> ReadOriginalEventBody(MethodInfo method)
        {
            // Do not use PatchProcessor.GetOriginalInstructions: this Harmony version resolves
            // patch factories even with zero transpilers requested. This copier has an empty list.
            var generator = new DynamicMethod("GatewayEventBodyMetadata", typeof(void), Type.EmptyTypes).GetILGenerator();
            var copier = new MethodCopier(method, generator);
            return NormalizeEventBody(copier.Finalize(false, out _, out _, null!), method);
        }

        private static IReadOnlyList<GatewayNativeInstructionShape> NormalizeEventBody(
            IReadOnlyList<CodeInstruction> body, MethodInfo method)
        {
            var labels = new Dictionary<Label, int>();
            int count = 0;
            bool Keep(CodeInstruction instruction) => instruction.opcode != OpCodes.Nop || instruction.blocks.Count != 0;
            foreach (CodeInstruction instruction in body)
            {
                foreach (Label label in instruction.labels)
                {
                    if (labels.TryGetValue(label, out int prior) && prior != count) throw Error("native_event_body_unsupported");
                    labels[label] = count;
                }
                if (Keep(instruction)) count++;
            }
            var result = new List<GatewayNativeInstructionShape>();
            IList<LocalVariableInfo> locals = method.GetMethodBody()!.LocalVariables;
            foreach (CodeInstruction instruction in body.Where(Keep))
            {
                OpCode code = instruction.opcode;
                object? operand = instruction.operand;
                int[] targets = Array.Empty<int>();
                if (operand is Label label) { targets = new[] { labels[label] }; operand = null; }
                else if (operand is Label[] branches) { targets = branches.Select(branch => labels[branch]).ToArray(); operand = null; }
                string name = code.Name!;
                if (code.OperandType == OperandType.ShortInlineBrTarget)
                    code = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                        .Select(field => (OpCode)field.GetValue(null)!).Single(value => value.Name == name.Substring(0, name.Length - 2));
                else if (name.StartsWith("ldloc", StringComparison.Ordinal) || name.StartsWith("stloc", StringComparison.Ordinal))
                {
                    bool address = name.StartsWith("ldloca", StringComparison.Ordinal);
                    bool store = name.StartsWith("stloc", StringComparison.Ordinal);
                    int index = operand is LocalBuilder local ? local.LocalIndex :
                        name.Length == 7 && char.IsDigit(name[6]) ? name[6] - '0' : Convert.ToInt32(operand);
                    if (index < 0 || index >= locals.Count) throw Error("native_event_body_unsupported");
                    operand = (index, locals[index].LocalType, locals[index].IsPinned);
                    code = address ? OpCodes.Ldloca : store ? OpCodes.Stloc : OpCodes.Ldloc;
                }
                else if (name.StartsWith("ldarg", StringComparison.Ordinal) || name.StartsWith("starg", StringComparison.Ordinal))
                {
                    operand = operand is ParameterInfo parameter ? parameter.Position + (method.IsStatic ? 0 : 1) :
                        name.Length == 7 && char.IsDigit(name[6]) ? name[6] - '0' : Convert.ToInt32(operand);
                    code = name.StartsWith("ldarga", StringComparison.Ordinal) ? OpCodes.Ldarga :
                        name.StartsWith("starg", StringComparison.Ordinal) ? OpCodes.Starg : OpCodes.Ldarg;
                }
                else if (name.StartsWith("ldc.i4", StringComparison.Ordinal))
                {
                    operand = code == OpCodes.Ldc_I4_M1 ? -1 : name.Length == 8 && char.IsDigit(name[7])
                        ? name[7] - '0' : Convert.ToInt32(operand);
                    code = OpCodes.Ldc_I4;
                }
                else if (code.OperandType == OperandType.InlineNone) operand = null;
                else if (operand != null && operand is not (MemberInfo or string or int or long or float or double or byte or sbyte or short or ushort))
                    throw Error("native_event_body_unsupported");
                result.Add(new GatewayNativeInstructionShape(code.Value, operand, targets,
                    instruction.blocks.Select(block => ((int)block.blockType, (Type?)block.catchType)).ToArray()));
            }
            return result;
        }

        public bool SupportsResetBoundary(IReadOnlyList<CodeInstruction> body)
        {
            var labels = new Dictionary<Label, int>();
            for (int index = 0; index < body.Count; index++)
            {
                if (body[index].blocks.Count != 0 || body[index].opcode == OpCodes.Jmp) return false;
                foreach (Label label in body[index].labels)
                    if (!labels.TryAdd(label, index)) return false;
            }
            var nodes = new List<GatewayNativeResetNode>(body.Count);
            for (int index = 0; index < body.Count; index++)
            {
                CodeInstruction instruction = body[index];
                FlowControl flow = instruction.opcode.FlowControl;
                var next = new List<int>();
                if (flow is FlowControl.Branch or FlowControl.Cond_Branch)
                {
                    Label[]? targets = instruction.operand is Label target ? new[] { target }
                        : instruction.operand as Label[];
                    if (targets == null) return false;
                    foreach (Label label in targets)
                    {
                        if (!labels.TryGetValue(label, out int destination)) return false;
                        next.Add(destination);
                    }
                }
                if (flow is not (FlowControl.Branch or FlowControl.Return or FlowControl.Throw))
                {
                    if (index + 1 >= body.Count) return false;
                    next.Add(index + 1);
                }
                bool call = instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt;
                nodes.Add(new GatewayNativeResetNode(next, call && Equals(instruction.operand, FrameReset),
                    call && Equals(instruction.operand, PostResetPhase), flow == FlowControl.Return));
            }
            return GatewayNativeWindowInputContract.HasRequiredResetFlow(nodes);
        }

        public static NativeContract? Resolve()
        {
            Assembly[] candidates = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.GetName().Name == "RimWorld").ToArray();
            if (candidates.Length != 1) return null;
            Type? dispatcher = candidates[0].GetType("RimWorldWaylith.Imgui.ImguiDispatch", false);
            Type? host = candidates[0].GetType("RimWorldWaylith.Imgui.ImGuiHost", false);
            Type? bootstrap = candidates[0].GetType("RimWorldWaylith.Bootstrap", false);
            Type? ime = candidates[0].GetType("RimWorldWaylith.WindowsImeBridge", false);
            if (dispatcher == null || host == null || bootstrap == null || ime == null) return null;
            MethodInfo? dispatch = GatewayNativeWindowInputContract.ResolveDispatch(dispatcher, host);
            MethodInfo? eventDispatch = GatewayNativeWindowInputContract.ResolveEventDispatch(dispatcher, typeof(Vector2));
            MethodInfo? mousePass = dispatcher.GetMethod("RunInputPass", BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.DeclaredOnly, null, new[] { typeof(EventType), typeof(int), typeof(Vector2), typeof(EventModifiers) }, null);
            MethodInfo? hotSetter = typeof(GUIUtility).GetProperty("hotControl", BindingFlags.Public |
                BindingFlags.Static | BindingFlags.DeclaredOnly)?.GetSetMethod();
            MethodInfo? reset = GatewayNativeWindowInputContract.ResolveFrameReset(typeof(InputSystem));
            MethodInfo? boundary = GatewayNativeWindowInputContract.ResolveFrameResetBoundary(bootstrap,
                typeof(Waylith.EngineApplication));
            MethodInfo? postReset = ime.GetMethod("BeginFrame", BindingFlags.Public | BindingFlags.Instance |
                BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);
            MethodInfo? readMouse = dispatcher.GetMethod("ReadPhysMouse", BindingFlags.NonPublic | BindingFlags.Static |
                BindingFlags.DeclaredOnly, null, new[] { host }, null);
            MethodInfo? displayHeight = host.GetProperty("DisplayHeight", BindingFlags.Public | BindingFlags.Instance |
                BindingFlags.DeclaredOnly)?.GetMethod;
            double? mouseOriginOffset = readMouse is { IsPrivate: true, IsGenericMethod: false } &&
                readMouse.ReturnType == typeof(Vector2) && displayHeight != null
                ? GatewayNativeWindowInputContract.MouseOriginOffset(ReadOriginalEventBody(readMouse),
                    typeof(Input).GetProperty(nameof(Input.mousePosition))!.GetMethod!, displayHeight,
                    typeof(Vector3).GetField(nameof(Vector3.x))!, typeof(Vector3).GetField(nameof(Vector3.y))!,
                    typeof(Vector2).GetConstructor(new[] { typeof(float), typeof(float) })!) : null;
            if (dispatch == null || eventDispatch == null || reset == null || boundary == null ||
                mousePass == null || !mousePass.IsPrivate || mousePass.IsGenericMethod || mousePass.ReturnType != typeof(void) ||
                !GatewayNativeWindowInputContract.Matches(hotSetter, typeof(GUIUtility), true, typeof(void), new[] { typeof(int) }) ||
                !GatewayNativeWindowInputContract.Matches(postReset, ime, false, typeof(void), Type.EmptyTypes) ||
                !GatewayNativeWindowInputContract.HasQueues(typeof(InputSystem), typeof(Vector2)) ||
                mouseOriginOffset == null || !SupportsMetadataOnlyPatchOrdering()) return null;
            return new NativeContract
            {
                Dispatch = dispatch, EventDispatch = eventDispatch, MouseInputPass = mousePass,
                MouseOriginOffset = mouseOriginOffset.Value,
                HotControlSetter = hotSetter!, FrameReset = reset,
                OriginalEventBody = ReadOriginalEventBody(eventDispatch),
                FrameResetBoundary = boundary, PostResetPhase = postReset!,
                DisplayHeight = host.GetProperty("DisplayHeight")!,
                PressEdge = Edge("WasPressedThisFrame"), ReleaseEdge = Edge("WasReleasedThisFrame"),
                RepeatEdge = Edge("WasRepeatedThisFrame"),
                TypedCharacters = Field(dispatcher, "_typedChars", typeof(List<char>), false),
                ButtonActive = Field(typeof(GUI), "_buttonDownActive", typeof(bool), true),
                ButtonRect = Field(typeof(GUI), "_buttonDownRect", typeof(Rect), true),
                ButtonNumber = Field(typeof(GUI), "_buttonDownButton", typeof(int), true),
                ControlWindows = Field(typeof(GUI), "_idToWindowId", typeof(Dictionary<int, int>), true),
                PatchState = Field(typeof(HarmonySharedState), "state", typeof(Dictionary<MethodBase, byte[]>), true)
            };
        }

        private static bool SupportsMetadataOnlyPatchOrdering()
        {
            // These are the inspected Harmony 3 metadata APIs, not factory-evaluating helpers.
            // The provider is 1.7-only; other runtimes never resolve or call these internals.
            const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            ConstructorInfo? constructor = typeof(PatchSorter).GetConstructor(instance, null,
                new[] { typeof(Patch[]), typeof(bool), typeof(bool) }, null);
            MethodInfo? sort = typeof(PatchSorter).GetMethod("Sort", instance, null, Type.EmptyTypes, null);
            MethodInfo? deserialize = typeof(PatchInfoSerialization).GetMethod("Deserialize",
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly,
                null, new[] { typeof(byte[]) }, null);
            MethodInfo? method = typeof(Patch).GetProperty("PatchMethod", BindingFlags.Public |
                BindingFlags.Instance | BindingFlags.DeclaredOnly)?.GetGetMethod();
            FieldInfo state = Field(typeof(HarmonySharedState), "state", typeof(Dictionary<MethodBase, byte[]>), true);
            ConstructorInfo? copierConstructor = typeof(MethodCopier).GetConstructor(instance, null,
                new[] { typeof(MethodBase), typeof(ILGenerator), typeof(LocalBuilder[]) }, null);
            MethodInfo? copierFinish = typeof(MethodCopier).GetMethod("Finalize", instance, null,
                new[] { typeof(bool), typeof(bool).MakeByRefType(), typeof(bool).MakeByRefType(), typeof(List<Label>) }, null);
            bool patchArrays = new[] { "prefixes", "transpilers", "innerprefixes", "innerpostfixes", "innerfinalizers" }
                .All(name => typeof(PatchInfo).GetField(name, BindingFlags.Public | BindingFlags.Instance |
                    BindingFlags.DeclaredOnly)?.FieldType == typeof(Patch[]));
            return copierConstructor != null && copierFinish?.ReturnType == typeof(List<CodeInstruction>) &&
                !copierFinish.IsGenericMethod && state.IsInitOnly && constructor != null && sort != null && !sort.IsGenericMethod &&
                sort.ReturnType == typeof(Patch[]) && deserialize != null && !deserialize.IsGenericMethod &&
                deserialize.ReturnType == typeof(PatchInfo) && patchArrays &&
                GatewayNativeWindowInputContract.Matches(method, typeof(Patch), false, typeof(MethodInfo), Type.EmptyTypes);
        }
        private static PropertyInfo Edge(string name)
        {
            PropertyInfo? property = typeof(ButtonControl).GetProperty(name,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            MethodInfo? getter = property?.GetGetMethod(true);
            if (getter == null || getter.IsStatic || getter.IsGenericMethod || getter.ReturnType != typeof(bool) ||
                getter.GetParameters().Length != 0) throw Error("native_input_contract_unsupported");
            return property!;
        }
        private static FieldInfo Field(Type owner, string name, Type type, bool isStatic)
        {
            FieldInfo? field = owner.GetField(name, BindingFlags.NonPublic | BindingFlags.DeclaredOnly |
                (isStatic ? BindingFlags.Static : BindingFlags.Instance));
            if (field == null || field.FieldType != type || field.IsStatic != isStatic)
                throw Error("native_input_contract_unsupported");
            return field;
        }
    }
#endif
}
