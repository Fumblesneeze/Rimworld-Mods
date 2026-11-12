using System.Reflection;
using Verse;
using RimWorldDevGateway.EndToEndTesting;
#if RIMWORLD_1_7
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using UnityStubs.Gui;
#endif

namespace RimWorldDevGateway;

internal sealed partial class VerseGatewayNativeWindowInput
{
    internal Func<IGatewayCaravanInspection?>? CaravanSource { get; set; }
    internal string? CaravanHealthUnavailableReason => UnavailableReason ?? caravanHealthFailure;
    private string? caravanHealthFailure;
    internal long CompletedWorldFrame { get; private set; }
    internal EndToEndCaravanHealthObservation ObserveCaravanHealth(IGatewayCaravanInspection selected, string pawnLoadId)
    {
        RequireMainThread();
#if RIMWORLD_1_7
        if (selected is not VerseGatewayCaravanInspection.Captured caravan || !caravan.Selected ||
            !caravan.View.Pane.IsOpen || caravan.View.Pane.OpenTabType != typeof(WITab_Caravan_Health)) return new(false, false, caravanLayoutVersion);
        var pawn = caravan.Caravan.PawnsListForReading.SingleOrDefault(p => p.GetUniqueLoadID() == pawnLoadId);
        bool row = pawn != null && healthRows.TryGetValue(pawn, out var observed) && ReferenceEquals(observed.Caravan, caravan.Caravan);
        bool detail = pawn != null && healthDetailDrawn is { } drawn && ReferenceEquals(drawn.Pawn, pawn) &&
            ReferenceEquals(drawn.Caravan.Caravan, caravan.Caravan) && drawn.Current;
        return new(row, detail, caravanLayoutVersion);
#else
        return new(false, false, 0);
#endif
    }
    internal bool WorldInputIdle
    {
        get
        {
#if RIMWORLD_1_7
            RequireMainThread(); RetireCompleted();
            if (!IsAvailable || active != null || textActive != null || pointerScope != null && pointerScope.Result == null) return false;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return mouse != null && keyboard != null &&
                !new[] { mouse.leftButton, mouse.rightButton, mouse.middleButton, mouse.forwardButton, mouse.backButton }
                    .Any(button => button.isPressed || contract!.HasEdges(button)) &&
                !keyboard.anyKey.isPressed && !contract!.HasEdges(keyboard.anyKey) &&
                ((List<char>)contract.TypedCharacters.GetValue(dispatchInstance)!).Count == 0 &&
                mouse.scroll.ReadValue() == Vector2.zero && GUIUtility.hotControl == 0 && !(bool)contract.ButtonActive.GetValue(null)!;
#else
            return false;
#endif
        }
    }

    internal object CaptureCaravanHealth(IGatewayCaravanInspection selected, string pawnLoadId, Func<IGatewayCaravanInspection> resolve)
    {
        RequireMainThread();
        if (CaravanHealthUnavailableReason is string reason) throw Error(reason);
#if RIMWORLD_1_7
        healthCaptures.Clear();
        if (selected is not VerseGatewayCaravanInspection.Captured caravan || !caravan.Selected ||
            caravan.View.Pane.OpenTabType != typeof(WITab_Caravan_Health) || !caravan.View.Pane.IsOpen)
            throw Error("native_caravan_health_not_open");
        var matches = caravan.Caravan.PawnsListForReading.Where(p => p.GetUniqueLoadID() == pawnLoadId).ToArray();
        if (matches.Length != 1 || matches[0].Destroyed || !healthRows.TryGetValue(matches[0], out var observed) ||
            !ReferenceEquals(observed.Caravan, caravan.Caravan)) throw Error("native_caravan_health_layout_unobserved");
        string handle = Guid.NewGuid().ToString("N");
        var target = new CaravanHealthTarget(this, new(observed), resolve);
        if (target.Validate(observed.X + observed.Width / 2, observed.Y + observed.Height / 2, false) is string rejection) throw Error(rejection);
        healthCaptures.Add(handle, target);
        return new Dictionary<string, object?> { ["controlHandle"] = handle, ["pawnLoadId"] = pawnLoadId,
            ["x"] = observed.X, ["y"] = observed.Y, ["width"] = observed.Width, ["height"] = observed.Height,
            ["uiScale"] = observed.UiScale, ["pixelWidth"] = observed.PixelWidth, ["pixelHeight"] = observed.PixelHeight,
            ["layoutVersion"] = observed.LayoutVersion, ["detailPawnLoadId"] = selected.Observe().DetailPawnLoadId };
#else
        throw Error("native_input_runtime_unsupported");
#endif
    }

    internal IGatewayDeferredAutomationOperation BeginCaravanHealthClick(string handle, GatewayAutomationContext context)
    {
        RequireMainThread();
        if (CaravanHealthUnavailableReason is string reason) throw Error(reason);
#if RIMWORLD_1_7
        if (!WorldInputIdle) throw Error("native_input_busy");
        if (!healthCaptures.TryGetValue(handle, out var target)) throw Error("stale_caravan_health_control_handle");
        var value = target.Lease.Captured;
        double x = value.X + value.Width / 2, y = value.Y + value.Height / 2;
        var point = target.Geometry.ResolvePoint(x, y);
        if (point.Error != null) throw Error(point.Error);
        var backend = new ClickBackend(this, target, x, y, point, null);
        if (backend.Validate(false) is string rejection) throw Error(rejection);
        context.RunStep("native-caravan-health-click-admission", () => { });
        activeContext = context; activeBackend = backend;
        active = new GatewayNativeClickOperation(backend);
        activeResult = new GatewayNativeInputResultOperation(active, () => failure ?? backend.ScopeFailure);
        return activeResult;
#else
        throw Error("native_input_runtime_unsupported");
#endif
    }

#if RIMWORLD_1_7
    private readonly Dictionary<string, CaravanHealthTarget> healthCaptures = new(StringComparer.Ordinal);
    private readonly Dictionary<Pawn, GatewayNativeCaravanHealthSnapshot> healthRows = new();
    private readonly List<(MethodInfo Target, MethodInfo Patch)> caravanHooks = new();
    private CaravanObservationFrame? caravanFrame;
    private long caravanLayoutVersion;
    private HealthDetailRequest? healthDetailRequest, healthDetailDrawn;
    private GatewayNativeCaravanDetailObservation? healthDetailObservation;
    private static readonly FieldInfo? SpecificIcon = typeof(CaravanThingsTabUtility).GetField("SpecificTabButtonTex",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

    private void InstallCaravanHooks()
    {
        try
        {
            var fill = typeof(WITab_Caravan_Health).GetMethod("FillTab", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            var specific = typeof(CaravanThingsTabUtility).GetMethod("DoOpenSpecificTabButton", new[] { typeof(Rect), typeof(Pawn), typeof(Pawn).MakeByRefType() });
            var button = typeof(Widgets).GetMethod(nameof(Widgets.ButtonImage), new[] { typeof(Rect), typeof(Texture2D), typeof(Color), typeof(Color), typeof(bool), typeof(string) });
            var pending = typeof(GUI).GetMethod("ProcessPendingWindows", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var clear = typeof(GUI).GetMethod("ClearPendingWindows", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var extra = typeof(WITab_Caravan_Health).GetMethod("ExtraOnGUI", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            var immediate = typeof(WindowStack).GetMethod(nameof(WindowStack.ImmediateWindow), new[] { typeof(int), typeof(Rect), typeof(WindowLayer), typeof(Action),
                typeof(bool), typeof(bool), typeof(float), typeof(Action), typeof(bool) });
            var card = typeof(HealthCardUtility).GetMethod(nameof(HealthCardUtility.DrawPawnHealthCard), new[] { typeof(Rect), typeof(Pawn), typeof(bool), typeof(bool), typeof(Thing) });
            var contents = typeof(ImmediateWindow).GetMethod(nameof(ImmediateWindow.DoWindowContents), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
                null, new[] { typeof(Rect) }, null);
            if (fill == null || specific == null || button == null || pending == null || clear == null || extra == null || immediate == null || card == null || contents == null ||
                new[] { fill, specific, button, pending, clear, extra, immediate, card, contents }.Any(m => m.IsGenericMethod || m.ContainsGenericParameters || m.GetMethodBody() == null) ||
                fill.IsStatic || fill.ReturnType != typeof(void) || fill.GetParameters().Length != 0 || !specific.IsStatic || specific.ReturnType != typeof(void) ||
                extra.IsStatic || extra.ReturnType != typeof(void) || extra.GetParameters().Length != 0 ||
                immediate.IsStatic || immediate.ReturnType != typeof(void) || !card.IsStatic || card.ReturnType != typeof(void) || contents.ReturnType != typeof(void) ||
                button.ReturnType != typeof(bool) || pending.ReturnType != typeof(void) || pending.GetParameters().Length != 0 ||
                clear.ReturnType != typeof(void) || clear.GetParameters().Length != 0 || SpecificIcon?.FieldType != typeof(Texture2D))
                throw Error("native_caravan_health_contract_unsupported");
            PatchCaravan(contents, nameof(BeforeCaravanDetailContents), finalizer: nameof(FinalizeCaravanDetailContents));
            PatchCaravan(card, postfix: nameof(ObserveCaravanHealthCard));
            PatchCaravan(immediate, postfix: nameof(ObserveCaravanImmediateRequest));
            PatchCaravan(extra, nameof(BeforeCaravanExtra), nameof(AfterCaravanExtra), nameof(FinalizeCaravanExtra));
            PatchCaravan(clear, nameof(BeforeCaravanClearPending));
            PatchCaravan(pending, nameof(CaravanPendingHooks.BeforeCaravanPending), nameof(CaravanPendingHooks.AfterCaravanPending), nameof(CaravanPendingHooks.FinalizeCaravanPending));
            PatchCaravan(button, postfix: nameof(ObserveCaravanButton));
            PatchCaravan(specific, nameof(BeforeCaravanSpecific), nameof(AfterCaravanSpecific), nameof(FinalizeCaravanSpecific));
            PatchCaravan(fill, nameof(BeforeCaravanFill), nameof(AfterCaravanFill), nameof(FinalizeCaravanFill));
            foreach (string name in new[] { "Before", "After", "Finish" }) caravanHooks.Add((contract!.EventDispatch, CaravanLayoutHook(name)));
            harmony!.Patch(contract!.EventDispatch,
                prefix: new HarmonyMethod(CaravanLayoutHook("Before")) { priority = Priority.Normal },
                postfix: new HarmonyMethod(CaravanLayoutHook("After")) { priority = Priority.Last },
                finalizer: new HarmonyMethod(CaravanLayoutHook("Finish")) { priority = int.MinValue });
        }
        catch
        {
            caravanHealthFailure = "native_caravan_health_contract_unsupported";
            if (!RemoveCaravanHooks()) failure = "native_input_hook_cleanup_incomplete";
        }
    }

    private void PatchCaravan(MethodInfo target, string? prefix = null, string? postfix = null, string? finalizer = null)
    {
        foreach (string name in new[] { prefix, postfix, finalizer }.Where(n => n != null)!) caravanHooks.Add((target, CaravanHook(name)));
        harmony!.Patch(target,
            prefix: prefix == null ? null : new HarmonyMethod(CaravanHook(prefix)) { priority = Priority.First },
            postfix: postfix == null ? null : new HarmonyMethod(CaravanHook(postfix)) { priority = Priority.Last },
            finalizer: finalizer == null ? null : new HarmonyMethod(CaravanHook(finalizer)) { priority = Priority.Last });
    }
    private bool RemoveCaravanHooks()
    {
        bool complete = true;
        foreach (var hook in caravanHooks) try { harmony!.Unpatch(hook.Target, hook.Patch); } catch { complete = false; }
        if (complete) { caravanHooks.Clear(); healthCaptures.Clear(); healthRows.Clear(); caravanFrame = null; healthDetailRequest = null; healthDetailDrawn = null; healthDetailObservation = null; }
        return complete;
    }

    private void BeginCaravanLayout(Event input, object dispatcher, bool drawing)
    {
        if (input.rawType is not (EventType.layout or EventType.repaint)) return;
        healthRows.Clear(); healthDetailDrawn = null; healthDetailObservation = null; caravanFrame = null;
        if (Current.ProgramState != ProgramState.Playing || Current.Game?.World == null || Current.Root is not Root_Play ||
            Find.UIRoot is not UIRoot_Play || LongEventHandler.AnyEventNowOrWaiting) return;
        VerseGatewayCaravanInspection.Captured? selected = null;
        try { selected = CaravanSource?.Invoke() as VerseGatewayCaravanInspection.Captured; } catch { }
        caravanFrame = new(this, input, dispatcher, drawing, ++caravanLayoutVersion, selected);
    }

    private static void BeforeCaravanFill(WITab_Caravan_Health __instance, bool __runOriginal, out CaravanObservationFrame? __state)
    {
        __state = null;
        var frame = installed?.caravanFrame;
        if (frame?.Caravan is not { } caravan || !frame.Intact || !__runOriginal || !caravan.Selected ||
            !caravan.View.Pane.IsOpen || caravan.View.Pane.OpenTabType != typeof(WITab_Caravan_Health) ||
            !ReferenceEquals(caravan.HealthTab, __instance) || caravan.View.Stack.currentlyDrawnWindow is not ImmediateWindow window || window.ID != -235086) return;
        if (frame.FillActive || frame.FillReturned) { frame.Failed = true; return; }
        frame.FillActive = true; frame.Tab = __instance; frame.Window = window; __state = frame;
    }
    private static void AfterCaravanFill(bool __runOriginal, CaravanObservationFrame? __state)
    {
        if (__state == null) return;
        __state.Proof.BodyReturned(__runOriginal && __state.Intact); __state.FillActive = false;
    }
    private static Exception? FinalizeCaravanFill(Exception? __exception, CaravanObservationFrame? __state)
    { if (__state != null && __exception != null) { __state.Failed = true; __state.FillActive = false; } return __exception; }
    private static void BeforeCaravanSpecific(Pawn __1, bool __runOriginal, out CaravanObservationFrame? __state)
    {
        __state = null;
        var frame = installed?.caravanFrame;
        if (frame == null || !frame.Intact || !frame.FillActive || !__runOriginal || frame.Pawn != null ||
            frame.Caravan?.Caravan.PawnsListForReading.Contains(__1) != true) return;
        frame.Pawn = __1; __state = frame;
    }
    private static void AfterCaravanSpecific(bool __runOriginal, CaravanObservationFrame? __state)
    { if (__state != null) { if (!__runOriginal || !__state.Intact) __state.Failed = true; __state.Pawn = null; } }
    private static Exception? FinalizeCaravanSpecific(Exception? __exception, CaravanObservationFrame? __state)
    { if (__state != null) { if (__exception != null) __state.Failed = true; __state.Pawn = null; } return __exception; }
    // Pending processing is also shared with Entry; its __state needs its own declaring type.
    private static class CaravanPendingHooks
    {
        public static void BeforeCaravanPending(out CaravanObservationFrame? __state)
        { __state = installed?.caravanFrame; __state?.Proof.BeginPending(); }
        public static void AfterCaravanPending(bool __runOriginal, CaravanObservationFrame? __state)
        { if (__state != null) __state.PendingReturned = __runOriginal && __state.Intact; }
        public static Exception? FinalizeCaravanPending(Exception? __exception, CaravanObservationFrame? __state)
        {
            __state?.Proof.EndPending(__exception == null && __state.PendingReturned);
            return __exception;
        }
    }
    private static void BeforeCaravanClearPending()
    { installed?.caravanFrame?.Proof.ClearPending(); }

    private static void BeforeCaravanExtra(WITab_Caravan_Health __instance, bool __runOriginal, out CaravanObservationFrame? __state)
    {
        __state = null;
        var frame = installed?.caravanFrame;
        if (frame?.Caravan is not { } caravan || !frame.Intact || !__runOriginal || !caravan.Selected ||
            !ReferenceEquals(caravan.HealthTab, __instance) || caravan.View.Pane.OpenTabType != typeof(WITab_Caravan_Health)) return;
        frame.ExtraTab = __instance; __state = frame;
    }
    private static void AfterCaravanExtra(bool __runOriginal, CaravanObservationFrame? __state)
    { if (__state != null) { if (!__runOriginal || !__state.Intact) __state.Failed = true; __state.ExtraTab = null; } }
    private static Exception? FinalizeCaravanExtra(Exception? __exception, CaravanObservationFrame? __state)
    { if (__state != null) { if (__exception != null) __state.Failed = true; __state.ExtraTab = null; } return __exception; }
    private static void ObserveCaravanImmediateRequest(WindowStack __instance, int __0, Action __3, bool __runOriginal)
    {
        var service = installed; var frame = service?.caravanFrame;
        if (frame?.ExtraTab is not { } tab || frame.Caravan is not { } caravan || !frame.Intact || !__runOriginal ||
            frame.Input.rawType != EventType.repaint || __0 != 1439870015 || !ReferenceEquals(caravan.View.Stack, __instance) ||
            VerseGatewayCaravanInspection.DetailPawn!.GetValue(tab) is not Pawn pawn) return;
        var matches = __instance.Windows.OfType<ImmediateWindow>().Where(w => w.ID == -1439870015 && ReferenceEquals(w.doWindowFunc, __3)).Take(2).ToArray();
        if (matches.Length != 1) { frame.Failed = true; return; }
        var request = service!.healthDetailRequest = new(caravan, tab, pawn, matches[0], __3);
        service.healthDetailObservation = new(request.Window, () => frame.Intact && request.Current);
    }
    private static void ObserveCaravanHealthCard(Pawn __1, bool __2, bool __3, Thing __4, bool __runOriginal)
    {
        var service = installed; var frame = service?.caravanFrame; var request = service?.healthDetailRequest;
        if (frame == null || !frame.Intact || !__runOriginal || __2 || !__3 || !ReferenceEquals(__1, __4) || request == null ||
            !request.Current || !ReferenceEquals(request.Pawn, __1) || !ReferenceEquals(request.Window, Find.WindowStack.currentlyDrawnWindow) ||
            !ReferenceEquals(frame.Caravan?.Caravan, request.Caravan.Caravan)) return;
        frame.DetailDrawn = request;
        service!.healthDetailObservation?.CardReturned();
    }
    private static void BeforeCaravanDetailContents(object __instance, bool __runOriginal, out GatewayNativeCaravanDetailObservation.Invocation? __state)
    { __state = installed?.healthDetailObservation?.Enter(__instance, __runOriginal); }
    private static Exception? FinalizeCaravanDetailContents(bool __runOriginal, Exception? __exception, GatewayNativeCaravanDetailObservation.Invocation? __state)
    { __state?.Complete(__runOriginal, __exception); return __exception; }
    private sealed record HealthDetailRequest(VerseGatewayCaravanInspection.Captured Caravan, WITab_Caravan_Health Tab,
        Pawn Pawn, ImmediateWindow Window, Action Callback)
    {
        internal bool Current => Caravan.Selected && Caravan.View.Pane.IsOpen && Caravan.View.Pane.OpenTabType == typeof(WITab_Caravan_Health) &&
            ReferenceEquals(Caravan.HealthTab, Tab) && ReferenceEquals(VerseGatewayCaravanInspection.DetailPawn!.GetValue(Tab), Pawn) &&
            !Pawn.Destroyed && Caravan.Caravan.PawnsListForReading.Contains(Pawn) && Caravan.View.Stack.Windows.Any(w => ReferenceEquals(w, Window)) &&
            Window.ID == -1439870015 && ReferenceEquals(Window.doWindowFunc, Callback);
    }

    private static void ObserveCaravanButton(Rect __0, Texture2D __1, bool __runOriginal)
    {
        var service = installed; var frame = service?.caravanFrame;
        if (frame?.Pawn is not { } pawn || frame.Caravan is not { } caravan || !frame.Intact || !frame.FillActive ||
            !__runOriginal || !ReferenceEquals(SpecificIcon!.GetValue(null), __1) ||
            !ReferenceEquals(caravan.View.Stack.currentlyDrawnWindow, frame.Window)) return;
        var backend = GuiBackend.Current;
        if (backend == null) { frame.Failed = true; return; }
        if (!frame.Proof.ObserveRow(CaravanGeometry(backend, __0, out var bounds))) return;
        var view = caravan.View;
        var value = new GatewayNativeCaravanHealthSnapshot(view.Game, view.World, view.Root, view.Ui, view.Stack, view.Pane,
            caravan.Caravan, frame.Tab!, pawn, frame.Window!, service!.dispatchInstance!, service.dispatchHost!, LanguageDatabase.activeLanguage,
            frame.Window!.ID, bounds.x, bounds.y, bounds.width, bounds.height, Prefs.UIScale, Screen.width, Screen.height, GUI.enabled, frame.Version);
        if (frame.Rows.Count >= 64 || !frame.Rows.TryAdd(pawn, value)) frame.Failed = true;
    }

    // Read the actual backend's intersected physical hit clip. GUIClip's topmost group alone
    // does not prove visibility inside all enclosing scroll views.
    private static GatewayNativeCaravanRowGeometry CaravanGeometry(IGuiBackend backend, Rect rect, out Rect bounds)
    {
        bounds = default;
        var type = backend.GetType();
        if (type.FullName != "RimWorldWaylith.Imgui.ImGuiGuiBackend" || type.Assembly.GetName().Name != "RimWorld") return GatewayNativeCaravanRowGeometry.Unsupported;
        Vector2 a = backend.GuiToDevicePoint(rect.min), b = backend.GuiToDevicePoint(rect.max),
            c = backend.GuiToDevicePoint(new(rect.xMax, rect.yMin)), d = backend.GuiToDevicePoint(new(rect.xMin, rect.yMax));
        if (a.x != d.x || a.y != c.y || b.x != c.x || b.y != d.y || b.x <= a.x || b.y <= a.y) return GatewayNativeCaravanRowGeometry.Unsupported;
        var field = type.GetField("hitClipStack", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        var stack = field?.GetValue(backend);
        if (stack == null || !field!.FieldType.IsGenericType || field.FieldType.GetGenericTypeDefinition() != typeof(Stack<>) ||
            field.FieldType.GetGenericArguments()[0].FullName != "Brutal.Numerics.float4" ||
            field.FieldType.GetProperty("Count")?.GetValue(stack) is not int count || count < 1 || count > 32) return GatewayNativeCaravanRowGeometry.Unsupported;
        var clip = field.FieldType.GetMethod("Peek", Type.EmptyTypes)?.Invoke(stack, null);
        if (clip == null) return GatewayNativeCaravanRowGeometry.Unsupported;
        float Read(string name) => clip.GetType().GetField(name)?.GetValue(clip) is float value ? value : float.NaN;
        var geometry = GatewayNativeCaravanGeometry.Classify(a.x, a.y, b.x, b.y, Read("X"), Read("Y"), Read("Z"), Read("W"));
        bounds = new(a.x, a.y, b.x - a.x, b.y - a.y); return geometry;
    }

    private sealed class CaravanObservationFrame
    {
        internal readonly VerseGatewayNativeWindowInput Service;
        internal readonly Event Input;
        internal readonly object Dispatcher, Host;
        internal readonly bool Drawing;
        internal readonly long Version;
        internal readonly VerseGatewayCaravanInspection.Captured? Caravan;
        internal readonly Dictionary<Pawn, GatewayNativeCaravanHealthSnapshot> Rows = new();
        internal WITab_Caravan_Health? Tab;
        internal WITab_Caravan_Health? ExtraTab;
        internal HealthDetailRequest? DetailDrawn;
        internal ImmediateWindow? Window;
        internal Pawn? Pawn;
        internal readonly GatewayNativeCaravanLayoutProof Proof;
        internal bool Failed { get => Proof.Failed; set { if (value) Proof.Fail(); } }
        internal bool FillReturned => Proof.BodyCompleted;
        internal bool FillActive, PendingReturned, Returned;
        internal CaravanObservationFrame(VerseGatewayNativeWindowInput service, Event input, object dispatcher, bool drawing,
            long version, VerseGatewayCaravanInspection.Captured? caravan)
        { Service = service; Input = input; Dispatcher = dispatcher; Host = service.dispatchHost!; Drawing = drawing; Version = version; Caravan = caravan;
            Proof = new(input, dispatcher, Host, drawing, (int)input.type, (int)input.rawType); }
        internal bool Intact => ReferenceEquals(Service.caravanFrame, this) && ReferenceEquals(Event.current, Input) &&
            Input.type == Input.rawType && Input.rawType is EventType.layout or EventType.repaint &&
            ReferenceEquals(Service.dispatchInstance, Dispatcher) && ReferenceEquals(Service.dispatchHost, Host);
        internal void Finish(object dispatcher, bool drawing, Exception? exception)
        {
            var current = Event.current;
            bool success = Proof.Complete(current!, dispatcher, Service.dispatchHost!, drawing,
                (int)(current?.type ?? EventType.ignore), (int)(current?.rawType ?? EventType.ignore),
                exception == null && Returned && Intact, Service.HasCaravanLayoutAuthority());
            Service.healthRows.Clear();
            if (success)
            {
                if (Input.rawType == EventType.repaint) Service.CompletedWorldFrame++;
                if (FillReturned && Caravan?.Selected == true)
                    foreach (var pair in Rows) Service.healthRows.Add(pair.Key, pair.Value);
                if (DetailDrawn?.Current == true && Service.healthDetailObservation?.Visible == true) Service.healthDetailDrawn = DetailDrawn;
            }
            if (ReferenceEquals(Service.caravanFrame, this)) Service.caravanFrame = null;
        }
    }

    private static MethodInfo CaravanLayoutHook(string name) => typeof(CaravanLayoutHooks).GetMethod(name, BindingFlags.Static | BindingFlags.Public)!;
    private static MethodInfo CaravanHook(string name) => typeof(CaravanPendingHooks).GetMethod(name, BindingFlags.Static | BindingFlags.Public) ?? Hook(name);
    private bool HasCaravanLayoutAuthority()
    {
        try
        {
            if (!HasPublishedMouseGuards()) return false;
            return caravanHooks.All(hook =>
            {
                var bytes = PublishedMetadata(hook.Target); if (bytes == null) return false;
                var info = PatchInfoSerialization.Deserialize(bytes);
                return info.innerfinalizers.Length == 0 && info.finalizers.All(p => KnownLayoutFinalizer(hook.Target, p.PatchMethod)) &&
                    info.prefixes.Concat(info.postfixes).Concat(info.finalizers).Count(p => p.PatchMethod == hook.Patch) == 1;
            });
        }
        catch { return false; }
    }
    private bool KnownLayoutFinalizer(MethodInfo target, MethodInfo patch) =>
        (patch.Name.StartsWith("Finalize", StringComparison.Ordinal) || patch.Name == "Finish") &&
        (entryHooks.Any(h => h.Target == target && h.Patch == patch) || caravanHooks.Any(h => h.Target == target && h.Patch == patch));
    private static class CaravanLayoutHooks
    {
        public static void Before(object __instance, bool __1, out CaravanObservationFrame? __state)
        { var service = installed; __state = service != null && UnityData.IsInMainThread && ReferenceEquals(__instance, service.dispatchInstance) &&
            service.caravanFrame?.Intact == true ? service.caravanFrame : null; }
        public static void After(bool __runOriginal, CaravanObservationFrame? __state)
        { if (__state != null) __state.Returned = __runOriginal && __state.Intact; }
        public static Exception? Finish(object __instance, bool __1, Exception? __exception, CaravanObservationFrame? __state)
        { __state?.Finish(__instance, __1, __exception); return __exception; }
    }

    private sealed class CaravanHealthTarget : IClickTarget
    {
        private readonly VerseGatewayNativeWindowInput service;
        private readonly Func<IGatewayCaravanInspection> resolve;
        private readonly VerseGatewayCaravanInspection.Captured owner;
        private long requiredLayout;
        internal GatewayNativeCaravanHealthLease Lease { get; }
        internal CaravanHealthTarget(VerseGatewayNativeWindowInput service, GatewayNativeCaravanHealthLease lease, Func<IGatewayCaravanInspection> resolve)
        {
            this.service = service; this.resolve = resolve; owner = (VerseGatewayCaravanInspection.Captured)resolve();
            Lease = lease; var value = lease.Captured;
            Geometry = new(value.X / value.UiScale, value.Y / value.UiScale, value.Width / value.UiScale,
                value.Height / value.UiScale, value.UiScale, value.PixelWidth, value.PixelHeight, service.contract!.MouseOriginOffset);
        }
        public GatewayNativeWindowGeometry Geometry { get; }
        public bool ContextCurrent => owner.Selected && !((Pawn)Lease.Captured.Pawn).Destroyed &&
            owner.Caravan.PawnsListForReading.Contains((Pawn)Lease.Captured.Pawn) && owner.View.Pane.IsOpen &&
            owner.View.Pane.OpenTabType == typeof(WITab_Caravan_Health) && ReferenceEquals(owner.HealthTab, Lease.Captured.Tab) &&
            ReferenceEquals(Current.Game, Lease.Captured.Game) &&
            ReferenceEquals(Current.Game?.World, Lease.Captured.World) && ReferenceEquals(Current.Root, Lease.Captured.Root) &&
            ReferenceEquals(Find.UIRoot, Lease.Captured.Ui) && ReferenceEquals(Find.WindowStack, Lease.Captured.Stack);
        public bool TargetPresent => ContextCurrent && Find.WindowStack.Windows.Any(w => ReferenceEquals(w, Lease.Captured.Window));
        public bool OwnsHotControl(int control) => TargetPresent && service.contract!.ControlBelongsTo(control, Lease.Captured.WindowId);
        public bool HasOtherWindowOwner(int control) => ContextCurrent && service.contract!.ControlWindows.GetValue(null) is Dictionary<int, int> owners &&
            owners.TryGetValue(control, out int owner) && Find.WindowStack.Windows.Any(w => w.ID == owner && !ReferenceEquals(w, Lease.Captured.Window));
        public void BeginDispatch() => requiredLayout = service.caravanLayoutVersion + 1;
        public string? Validate(double pixelX, double pixelY, bool afterLayout)
        {
            try
            {
                if (!ContextCurrent || resolve() is not VerseGatewayCaravanInspection.Captured caravan || !caravan.Selected ||
                    !ReferenceEquals(caravan.Caravan, Lease.Captured.Caravan) || !caravan.View.Pane.IsOpen ||
                    caravan.View.Pane.OpenTabType != typeof(WITab_Caravan_Health)) return "stale_caravan_health_context";
                if (!service.healthRows.TryGetValue((Pawn)Lease.Captured.Pawn, out var current)) return "native_caravan_health_layout_unobserved";
                current = current with { Game = Current.Game, World = Current.Game.World, Root = Current.Root, Ui = Find.UIRoot,
                    Stack = Find.WindowStack, Dispatcher = service.dispatchInstance!, Host = service.dispatchHost!, Language = LanguageDatabase.activeLanguage,
                    UiScale = Prefs.UIScale, PixelWidth = Screen.width, PixelHeight = Screen.height };
                string? error = Lease.Validate(current, afterLayout ? requiredLayout : 0);
                if (error != null) return error;
                var window = (Window)Lease.Captured.Window;
                if (!TargetPresent || Find.WindowStack.Windows.Count > MaximumWindows || !Find.WindowStack.GetsInput(window) ||
                    !ReferenceEquals(Find.WindowStack.GetWindowAt(new Vector2((float)(pixelX / Geometry.UiScale), (float)(pixelY / Geometry.UiScale))), window))
                    return "native_caravan_health_obscured";
                return service.ValidExtent() ? Geometry.ResolvePoint(pixelX, pixelY).Error : "stale_caravan_health_geometry";
            }
            catch (GatewayAutomationException) { return "stale_caravan_health_context"; }
        }
    }
#endif
}
