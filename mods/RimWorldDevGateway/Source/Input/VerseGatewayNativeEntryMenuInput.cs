using RimWorldDevGateway.EndToEndTesting;
using System.Reflection;
using System.Threading;
using Verse;
#if RIMWORLD_1_7
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
#endif

namespace RimWorldDevGateway;

internal sealed partial class VerseGatewayNativeWindowInput
{
    public string? EntryUnavailableReason
    {
        get
        {
            if (UnavailableReason is string reason) return reason;
#if RIMWORLD_1_7
            return entryFailure;
#else
            return "native_input_runtime_unsupported";
#endif
        }
    }

    public IReadOnlyList<EndToEndNativeEntryMenuControl> CaptureEntryMenu()
    {
        RequireMainThread();
        if (EntryUnavailableReason is string reason) throw Error(reason);
#if RIMWORLD_1_7
        if (EntryStateError() is string stateError) throw Error(stateError);
        entryCaptures.Clear();
        var result = new List<EndToEndNativeEntryMenuControl>();
        foreach (string control in new[] { "options", "dev-quicktest" })
        {
            var observed = entryInventory.Find(control);
            if (observed == null) continue;
            var lease = new GatewayNativeEntryMenuLease(observed);
            string handle = Guid.NewGuid().ToString("N");
            var target = new EntryClickTarget(this, lease);
            if (target.Validate(observed.X + observed.Width / 2, observed.Y + observed.Height / 2, false) is string rejection)
                throw Error(rejection);
            entryCaptures.Add(handle, target);
            result.Add(new(handle, control, observed.Label, observed.X, observed.Y, observed.Width,
                observed.Height, observed.UiScale, observed.PixelWidth, observed.PixelHeight));
        }
        if (result.Count == 0) throw Error("native_entry_layout_unobserved");
        return result;
#else
        throw Error("native_input_runtime_unsupported");
#endif
    }

    public IGatewayDeferredAutomationOperation BeginEntryClick(string handle, GatewayAutomationContext context)
    {
        RequireMainThread();
        if (EntryUnavailableReason is string reason) throw Error(reason);
        if (context.CallerThreadId != Thread.CurrentThread.ManagedThreadId) throw Error("native_input_wrong_thread");
#if RIMWORLD_1_7
        RetireCompleted();
        if (active != null || textActive != null || pointerScope != null && pointerScope.Result == null)
            throw Error("native_input_busy");
        if (!entryCaptures.TryGetValue(handle, out var target)) throw Error("stale_entry_control_handle");
        var captured = target.Lease.Captured;
        double x = captured.X + captured.Width / 2, y = captured.Y + captured.Height / 2;
        var point = target.Geometry.ResolvePoint(x, y);
        if (point.Error != null) throw Error(point.Error);
        var backend = new ClickBackend(this, target, x, y, point, null);
        if (backend.Validate(false) is string rejection) throw Error(rejection);
        context.RunStep("native-entry-click-admission", () => { });
        activeContext = context; activeBackend = backend;
        active = new GatewayNativeClickOperation(backend);
        activeResult = new GatewayNativeInputResultOperation(active, () => failure ?? backend.ScopeFailure);
        return activeResult;
#else
        throw Error("native_input_runtime_unsupported");
#endif
    }

#if RIMWORLD_1_7
    private readonly Dictionary<string, EntryClickTarget> entryCaptures = new(StringComparer.Ordinal);
    private readonly GatewayNativeEntryMenuInventory entryInventory = new();
    private readonly List<(MethodInfo Target, MethodInfo Patch)> entryHooks = new();
    private string? entryFailure;
    private long entryLayoutVersion;
    private EntryObservationFrame? entryFrame;

    private void InstallEntryHooks()
    {
        try
        {
            // Inner observer first: recompiling callers must see the already patched boundary.
            var button = typeof(Widgets).GetMethod(nameof(Widgets.ButtonText), new[] {
                typeof(Rect), typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(TextAnchor?) });
            var option = typeof(ListableOption).GetMethod(nameof(ListableOption.DrawOption), new[] { typeof(Vector2), typeof(float) });
            var menu = typeof(MainMenuDrawer).GetMethod(nameof(MainMenuDrawer.DoMainMenuControls), new[] { typeof(Rect), typeof(bool) });
            var pending = typeof(GUI).GetMethod("ProcessPendingWindows", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var clear = typeof(GUI).GetMethod("ClearPendingWindows", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (button == null || option == null || menu == null || pending == null || clear == null ||
                pending.ReturnType != typeof(void) || clear.ReturnType != typeof(void) ||
                pending.GetParameters().Length != 0 || clear.GetParameters().Length != 0)
                throw Error("native_entry_contract_unsupported");
            PatchEntry(clear, nameof(BeforeEntryClearPending));
            PatchEntry(pending, nameof(BeforeEntryPending), nameof(AfterEntryPending), nameof(FinalizeEntryPending));
            PatchEntry(button, postfix: nameof(ObserveEntryButton));
            PatchEntry(option, nameof(BeforeEntryOption), nameof(AfterEntryOption), nameof(FinalizeEntryOption));
            PatchEntry(menu, nameof(BeforeEntryMenu), nameof(AfterEntryMenu), nameof(FinalizeEntryMenu));
            // A separate declaring type owns Dispatch __state; the existing mouse state remains independent.
            foreach (string name in new[] { "Before", "After", "Finish" })
                entryHooks.Add((contract!.EventDispatch, EntryLayoutHook(name)));
            harmony!.Patch(contract!.EventDispatch,
                prefix: new HarmonyMethod(EntryLayoutHook("Before")) { priority = Priority.Normal },
                postfix: new HarmonyMethod(EntryLayoutHook("After")) { priority = Priority.Last },
                finalizer: new HarmonyMethod(EntryLayoutHook("Finish")) { priority = int.MinValue });
        }
        catch
        {
            entryFailure = "native_entry_contract_unsupported";
            if (!RemoveEntryHooks()) failure = "native_input_hook_cleanup_incomplete";
        }
    }

    private void PatchEntry(MethodInfo target, string? prefix = null, string? postfix = null, string? finalizer = null)
    {
        foreach (string name in new[] { prefix, postfix, finalizer }.Where(name => name != null)!)
            entryHooks.Add((target, Hook(name)));
        harmony!.Patch(target,
            prefix: prefix == null ? null : new HarmonyMethod(Hook(prefix)) { priority = Priority.First },
            postfix: postfix == null ? null : new HarmonyMethod(Hook(postfix)) { priority = Priority.Last },
            finalizer: finalizer == null ? null : new HarmonyMethod(Hook(finalizer)) { priority = Priority.Last });
    }

    private bool RemoveEntryHooks()
    {
        bool complete = true;
        foreach (var hook in entryHooks)
            try { harmony!.Unpatch(hook.Target, hook.Patch); } catch { complete = false; }
        if (complete) entryHooks.Clear();
        return complete;
    }

    private static string? EntryStateError()
    {
        if (!PlayDataLoader.Loaded || Current.ProgramState != ProgramState.Entry || Current.Game != null ||
            Current.Root is not Root_Entry || Find.UIRoot is not UIRoot_Entry || Find.WindowStack == null ||
            LongEventHandler.AnyEventNowOrWaiting || WorldRendererUtility.WorldSelected)
            return "native_entry_not_settled";
        if (Find.WindowStack.Windows.Count > MaximumWindows) return "native_window_inventory_limit";
        if (Find.WindowStack.Windows.Any(window => window.layer == WindowLayer.Dialog && !window.IsDebug))
            return "native_entry_obscured";
        return null;
    }

    private GatewayNativeEntryMenuSnapshot CurrentEntryContext(GatewayNativeEntryMenuSnapshot value) => value with
    {
        Root = Current.Root, UiRoot = Find.UIRoot, Stack = Find.WindowStack,
        Dispatcher = dispatchInstance!, Host = dispatchHost!, Language = LanguageDatabase.activeLanguage,
        DevMode = Prefs.DevMode, PixelWidth = Screen.width, PixelHeight = Screen.height, UiScale = Prefs.UIScale
    };

    private void BeginEntryLayout(Event input, object dispatcher, bool drawing)
    {
        if (input.rawType != EventType.layout) return;
        entryFrame?.Observation.Fail();
        entryFrame = new EntryObservationFrame(this, input, dispatcher, drawing, ++entryLayoutVersion);
    }

    private static void BeforeEntryMenu(bool __runOriginal, out EntryObservationFrame? __state)
    {
        __state = null;
        var service = installed; var input = Event.current;
        if (service == null || !UnityData.IsInMainThread || input == null || input.type != EventType.layout ||
            input.rawType != EventType.layout || !__runOriginal || EntryStateError() != null) return;
        var frame = service.entryFrame;
        if (frame == null || !frame.IsIntact) return;
        if (frame.MenuEntered) { frame.Observation.Fail(); return; }
        frame.MenuEntered = true;
        __state = frame;
    }

    private static void AfterEntryMenu(bool __runOriginal, EntryObservationFrame? __state)
    { if (__state != null) __state.Observation.MenuReturned(__runOriginal && __state.IsIntact); }
    private static Exception? FinalizeEntryMenu(Exception? __exception, EntryObservationFrame? __state)
    { if (__exception != null) __state?.Observation.Fail(); return __exception; }

    private static void BeforeEntryPending(out EntryObservationFrame? __state)
    { __state = installed?.entryFrame; __state?.Observation.BeginPendingWindows(); }
    private static void AfterEntryPending(bool __runOriginal, EntryObservationFrame? __state)
    { if (__state != null) __state.PendingReturned = __runOriginal && __state.IsIntact; }
    private static Exception? FinalizeEntryPending(Exception? __exception, EntryObservationFrame? __state)
    {
        if (__state != null) __state.Observation.EndPendingWindows(__exception == null && __state.PendingReturned);
        return __exception;
    }
    private static void BeforeEntryClearPending() => installed?.entryFrame?.Observation.ClearPendingWindows();

    private static void BeforeEntryOption(ListableOption __instance, out ListableOption? __state)
    {
        __state = null;
        var frame = installed?.entryFrame;
        if (frame == null) return;
        __state = frame.Option;
        frame.Option = __instance.GetType() == typeof(ListableOption) ? __instance : null;
    }
    private static void AfterEntryOption(ListableOption? __state)
    { if (installed?.entryFrame is { } frame) frame.Option = __state; }
    private static Exception? FinalizeEntryOption(Exception? __exception, ListableOption? __state)
    {
        if (__exception != null) installed?.entryFrame?.Observation.Fail();
        AfterEntryOption(__state); return __exception;
    }

    private static void ObserveEntryButton(Rect __0, string __1, bool __4, bool __runOriginal)
    {
        var service = installed; var frame = service?.entryFrame; var option = frame?.Option;
        if (frame == null || option == null || !__runOriginal || !frame.IsIntact || option.label != __1 || option.action == null) return;
        string? control = __1 == "Options".Translate().ToString() ? "options" :
            __1 == "DevQuickTest".Translate().ToString() && Prefs.DevMode ? "dev-quicktest" : null;
        if (control == null) return;
        Vector2 a = GUIUtility.GUIToScreenPoint(new(__0.x, __0.y));
        Vector2 b = GUIUtility.GUIToScreenPoint(new(__0.xMax, __0.yMax));
        Vector2 c = GUIUtility.GUIToScreenPoint(new(__0.xMax, __0.y));
        Vector2 d = GUIUtility.GUIToScreenPoint(new(__0.x, __0.yMax));
        if (a.y != c.y || a.x != d.x || b.x != c.x || b.y != d.y) { frame.Observation.Fail(); return; }
        var value = new GatewayNativeEntryMenuSnapshot(Current.Root, Find.UIRoot, Find.WindowStack,
            service!.dispatchInstance!, service.dispatchHost!, LanguageDatabase.activeLanguage, Prefs.DevMode,
            Screen.width, Screen.height, Prefs.UIScale, control, __1, option.action,
            a.x, a.y, b.x - a.x, b.y - a.y, GUI.enabled && __4, frame.Version);
        frame.Observation.Observe(value);
    }

    private sealed class EntryObservationFrame
    {
        private readonly VerseGatewayNativeWindowInput service;
        private readonly Event input;
        private readonly object dispatcher;
        private readonly bool drawing;
        public readonly long Version;
        public readonly GatewayNativeEntryMenuObservation Observation;
        public ListableOption? Option;
        public bool MenuEntered, PendingReturned, LayoutReturned;
        public EntryObservationFrame(VerseGatewayNativeWindowInput service, Event input, object dispatcher, bool drawing, long version)
        {
            this.service = service; this.input = input; this.dispatcher = dispatcher; this.drawing = drawing; Version = version;
            Observation = new(service.entryInventory, version, input, dispatcher, service.dispatchHost!, drawing,
                (int)input.type, (int)input.rawType);
        }
        public bool IsIntact => ReferenceEquals(service.entryFrame, this) &&
            ReferenceEquals(Event.current, input) && input.type == EventType.layout && input.rawType == EventType.layout;
        public void Finish(object currentDispatcher, bool currentDrawing, Exception? exception)
        {
            var current = Event.current;
            bool success = exception == null && LayoutReturned && IsIntact && EntryStateError() == null &&
                ReferenceEquals(dispatcher, service.dispatchInstance) && drawing == currentDrawing;
            Observation.Complete(current!, currentDispatcher, service.dispatchHost!, currentDrawing,
                (int)(current?.type ?? EventType.ignore), (int)(current?.rawType ?? EventType.ignore),
                success, success && service.HasEntryLayoutAuthority());
            if (ReferenceEquals(service.entryFrame, this)) service.entryFrame = null;
        }
    }

    private static MethodInfo EntryLayoutHook(string name) => typeof(EntryLayoutHooks)
        .GetMethod(name, BindingFlags.Static | BindingFlags.Public)!;

    private bool HasEntryLayoutAuthority()
    {
        try
        {
            byte[]? metadata = PublishedMetadata(contract!.EventDispatch);
            if (metadata == null || !HasPublishedMouseGuards()) return false;
            var info = PatchInfoSerialization.Deserialize(metadata);
            // Another finalizer could suppress a late exception before our publication. Fail closed.
            return info.innerfinalizers.Length == 0 && info.finalizers.Count(patch => patch.PatchMethod == EntryLayoutHook("Finish")) == 1 &&
                info.finalizers.All(patch => KnownLayoutFinalizer(contract.EventDispatch, patch.PatchMethod)) &&
                entryHooks.All(hook => {
                    byte[]? bytes = PublishedMetadata(hook.Target);
                    if (bytes == null) return false;
                    var published = PatchInfoSerialization.Deserialize(bytes);
                    return published.innerfinalizers.Length == 0 && published.finalizers.All(patch =>
                        KnownLayoutFinalizer(hook.Target, patch.PatchMethod)) &&
                        published.prefixes.Concat(published.postfixes).Concat(published.finalizers)
                        .Count(patch => patch.PatchMethod == hook.Patch) == 1;
                });
        }
        catch { return false; }
    }

    private static class EntryLayoutHooks
    {
        public static void Before(object __instance, bool __1, out EntryObservationFrame? __state)
        {
            var service = installed;
            __state = service != null && UnityData.IsInMainThread && ReferenceEquals(__instance, service.dispatchInstance) &&
                service.entryFrame?.IsIntact == true ? service.entryFrame : null;
        }
        public static void After(bool __runOriginal, EntryObservationFrame? __state)
        { if (__state != null) __state.LayoutReturned = __runOriginal && __state.IsIntact; }
        public static Exception? Finish(object __instance, bool __1, Exception? __exception, EntryObservationFrame? __state)
        { __state?.Finish(__instance, __1, __exception); return __exception; }
    }

    private sealed class EntryClickTarget : IClickTarget
    {
        private readonly VerseGatewayNativeWindowInput service;
        private long requiredLayout;
        public GatewayNativeEntryMenuLease Lease { get; }
        public EntryClickTarget(VerseGatewayNativeWindowInput service, GatewayNativeEntryMenuLease lease)
        {
            this.service = service; Lease = lease;
            var value = lease.Captured;
            Geometry = new(value.X / value.UiScale, value.Y / value.UiScale, value.Width / value.UiScale,
                value.Height / value.UiScale, value.UiScale, value.PixelWidth, value.PixelHeight, service.contract!.MouseOriginOffset);
        }
        public GatewayNativeWindowGeometry Geometry { get; }
        public bool ContextCurrent => ReferenceEquals(Current.Root, Lease.Captured.Root) &&
            ReferenceEquals(Find.UIRoot, Lease.Captured.UiRoot) && ReferenceEquals(Find.WindowStack, Lease.Captured.Stack);
        public bool TargetPresent => ContextCurrent && EntryStateError() == null;
        public bool OwnsHotControl(int control) => false; // Ordinary Entry buttons own only the native rectangle latch.
        public bool HasOtherWindowOwner(int control) => ContextCurrent && service.contract!.ControlWindows.GetValue(null) is Dictionary<int, int> owners &&
            owners.TryGetValue(control, out int owner) && Find.WindowStack.Windows.Count(window => window.ID == owner &&
                GatewayNativeWindowInputContract.SupportsWindowIdentity(window.ID, window is ImmediateWindow)) == 1;
        public void BeginDispatch() => requiredLayout = service.entryLayoutVersion + 1;
        public string? Validate(double pixelX, double pixelY, bool afterLayout)
        {
            if (EntryStateError() is string stateError) return stateError;
            var current = service.entryInventory.Find(Lease.Captured.Control);
            if (current == null) return "native_entry_layout_unobserved";
            string? error = Lease.Validate(service.CurrentEntryContext(current), afterLayout ? requiredLayout : 0);
            if (error != null) return error;
            if (!service.ValidExtent()) return "stale_entry_geometry";
            if (Find.WindowStack.GetWindowAt(new Vector2((float)(pixelX / Geometry.UiScale),
                (float)(pixelY / Geometry.UiScale))) != null) return "native_entry_obscured";
            return Geometry.ResolvePoint(pixelX, pixelY).Error;
        }
    }
#endif
}
