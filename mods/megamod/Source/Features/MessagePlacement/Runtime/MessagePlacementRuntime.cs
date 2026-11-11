using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Megamod.Patching;
using Megamod.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace Megamod.Features.MessagePlacement;

// Root owns construction/admission on the native main thread. No bootstrap registration here.
public sealed class MessagePlacementRuntime
{
    private static MessagePlacementRuntime? route;
    private readonly SettingsRuntime settings;
    private readonly MessagePlacementBridge? bridge;

    public MessagePlacementRuntime(SettingsRuntime settings, IEnumerable<string> activePackageIds,
        Action<string> protectedWarning)
    {
        this.settings = settings;
        try
        {
            if (!UnityData.IsInMainThread) throw new InvalidOperationException("Native message composition requires the main thread.");
            // Registration belongs to root, before the common store is loaded/published. Never silently register here.
            settings.Store.ReadGlobal(MessagePlacementSettings.PositionOffset);
            settings.Store.ReadGlobal(MessagePlacementSettings.Enabled);
            var targets = ResolveTargets();
            bridge = new(targets, typeof(MessagePlacementRuntime).GetMethod(nameof(Forward))!, IsCurrentContext,
                CaptureFrame, ToNativeRectangle, protectedWarning,
                preparedTargets: ResolvePreparedTargets(), capturePrepared: CaptureManualBarHandoff,
                windowStackGui: Method(typeof(WindowStack), nameof(WindowStack.WindowStackOnGUI),
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                captureWindowFallback: CaptureWindowFallback,
                placementEnabled: () => settings.Store.GetEffective(MessagePlacementSettings.Enabled));
            var forwardingRoute = new PatchCandidate("MSG-NATIVE-FORWARDER", "MSG-NATIVE-FRAME", "Verse.Message.Draw native window forwarding",
                "message placement", "native placement", () => UnavailableReason ??
                    (!UnityData.IsInMainThread ? "Native message admission requires the main thread." : null) ??
                    (route != null ? "The native message forwarding route already has an owner." : null),
                () =>
                {
                    if (route != null) throw new InvalidOperationException("The native message forwarding route already has an owner.");
                    route = this;
                }, () => { if (ReferenceEquals(route, this)) route = null; });
            Group = new(bridge.Group.Id, bridge.Group.Phase, new[] { forwardingRoute }.Concat(bridge.Group.Patches).ToArray());
        }
        catch (Exception failure)
        {
            UnavailableReason ??= "Native message placement could not be resolved: " + failure.GetBaseException().Message;
            Group = new("messages.native-placement", AdmissionPhase.Early, new[]
            {
                new PatchCandidate("MSG-NATIVE-RESOLVE", "MSG-NATIVE-FRAME", "native message/window/UIRoot contracts",
                    "message placement", "native placement", () => UnavailableReason, () => {}, () => {})
            });
        }
    }

    public PatchGroup Group { get; }
    public string? UnavailableReason { get; }
    public bool Available => UnavailableReason == null && ReferenceEquals(route, this) && bridge?.Available == true;
    public bool AutomaticClearanceIntegrated => false;
    public string? AutomaticClearanceReasonKey => "Megamod_Messages_BarIntegrationPending";

    // This signature is consumed exactly by MSG-NATIVE-DRAW. The native call is outside owned handling.
    public static void Forward(WindowStack stack, int id, Rect rectangle, WindowLayer layer, Action callback,
        bool background, bool absorb, float shadow, Action outside, bool ignoreScreenFader, Message message)
    {
        stack.ImmediateWindow(id, rectangle, layer, callback, background, absorb, shadow, outside, ignoreScreenFader);
        if (UnityData.IsInMainThread && Event.current != null)
            route?.bridge?.Observe(stack, message, rectangle, callback,
                new(rectangle.x, rectangle.y, rectangle.width, rectangle.height));
    }

    private static bool IsCurrentContext(object actualRoot) => UnityData.IsInMainThread && Event.current != null &&
        actualRoot is UIRoot root && ReferenceEquals(Find.UIRoot, root) && root.windows != null &&
        ReferenceEquals(Find.WindowStack, root.windows);

    private static MessageBarBoundsReceipt? CaptureWindowFallback(object actualRoot, object actualStack)
    {
        if (!IsCurrentContext(actualRoot)) return null;
        var root = (UIRoot)actualRoot;
        var game = Current.Game;
        int width = UI.screenWidth, height = UI.screenHeight;
        if (width <= 0 || height <= 0 || !ReferenceEquals(root.windows, actualStack)) return null;
        bool IsCurrent() => IsCurrentContext(root) && ReferenceEquals(Current.Game, game) &&
            ReferenceEquals(root.windows, actualStack) && UI.screenWidth == width && UI.screenHeight == height;
        return root.GetType() == typeof(UIRoot_Entry) && game == null
            ? MessageBarBoundsReceipt.Absent(IsCurrent)
            : MessageBarBoundsReceipt.Unavailable("Megamod_Messages_BarIntegrationPending", IsCurrent);
    }

    private static MessageBarBoundsReceipt CaptureManualBarHandoff(object actualBar, object entries, bool frames)
    {
        var root = Find.UIRoot;
        var game = Current.Game;
        int width = UI.screenWidth, height = UI.screenHeight;
        // Manual positioning consumes no bar geometry. Unavailable is explicit, never inferred Absent.
        return MessageBarBoundsReceipt.Unavailable("Megamod_Messages_BarIntegrationPending", () =>
            IsCurrentContext(root) && ReferenceEquals(Find.ColonistBar, actualBar) &&
            ReferenceEquals(Current.Game, game) && UI.screenWidth == width && UI.screenHeight == height);
    }

    private static MessageBarPreparedTargets ResolvePreparedTargets()
    {
        const BindingFlags instance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var drawer = typeof(ColonistBarColonistDrawer);
        return new(Method(typeof(ColonistBar), nameof(ColonistBar.ColonistBarOnGUI), instance),
            typeof(ColonistBar).GetProperty(nameof(ColonistBar.Entries), instance)!.GetMethod!,
            typeof(ColonistBar).GetProperty("ShowGroupFrames", BindingFlags.NonPublic | BindingFlags.Instance)!.GetMethod!,
            new[]
            {
                Method(drawer, nameof(ColonistBarColonistDrawer.HandleClicks), instance, typeof(Rect), typeof(Pawn), typeof(int), typeof(bool).MakeByRefType()),
                Method(drawer, nameof(ColonistBarColonistDrawer.DrawColonist), instance, typeof(Rect), typeof(Pawn), typeof(Map), typeof(bool), typeof(bool)),
                Method(drawer, nameof(ColonistBarColonistDrawer.DrawGroupFrame), instance, typeof(int)),
                Method(drawer, nameof(ColonistBarColonistDrawer.HandleGroupFrameClicks), instance, typeof(int))
            });
    }

    private MessagePlacementFrame? CaptureFrame()
    {
        if (!UnityData.IsInMainThread || Event.current == null) return null;
        UIRoot root = Find.UIRoot;
        if (root == null || !IsCurrentContext(root)) return null;
        WindowStack stack = root.windows;
        int width = UI.screenWidth, height = UI.screenHeight;
        if (width <= 0 || height <= 0) return null;
        bool repaint = Event.current.type == EventType.repaint;
        var binding = settings.CurrentBinding; // The common owner reconciles lifecycle once, before capture.
        var lifecycle = settings.LifecycleReceipt;
        var game = Current.Game;
        if (game != null && (!lifecycle.Available || !ReferenceEquals(lifecycle.Owner, game) || binding == null || binding.IsRevoked))
            return null;
        if (game == null && (binding != null || settings.Store.CurrentSaveIdentity != null)) return null;
        var revision = settings.Store.Revision;
        MessageOffsets offsets = settings.Store.GetEffective(MessagePlacementSettings.PositionOffset);
        bool IsCurrent() => IsCurrentContext(root) && ReferenceEquals(root.windows, stack) &&
            UI.screenWidth == width && UI.screenHeight == height && (Event.current.type == EventType.repaint) == repaint &&
            ReferenceEquals(Current.Game, game) && ReferenceEquals(settings.LifecycleReceipt, lifecycle) &&
            (binding == null || !binding.IsRevoked) && settings.Store.Revision == revision;
        return IsCurrent() ? new(root, stack, ExistingWindows(stack), width, height, repaint, offsets, IsCurrent) : null;
    }

    private static IEnumerable<object> ExistingWindows(WindowStack stack)
    {
        // Re-enumerate the current native view for final validation, never an obsolete list wrapper.
        foreach (Window window in stack.Windows) yield return window;
    }

    private static object ToNativeRectangle(MessageBounds bounds)
    {
        var rectangle = new Rect((float)bounds.X, (float)bounds.Y, (float)bounds.Width, (float)bounds.Height);
        if (!float.IsFinite(rectangle.x) || !float.IsFinite(rectangle.y) ||
            !float.IsFinite(rectangle.width) || !float.IsFinite(rectangle.height))
            throw new InvalidOperationException("The translated native rectangle is not finite.");
        return rectangle;
    }

    private static MessagePlacementTargets ResolveTargets()
    {
        const BindingFlags instance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        const BindingFlags statics = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var registration = new[] { typeof(int), typeof(Rect), typeof(WindowLayer), typeof(Action), typeof(bool), typeof(bool),
            typeof(float), typeof(Action), typeof(bool) };
        return new(
            Method(typeof(Message), nameof(Message.Draw), instance, typeof(int), typeof(int)),
            Method(typeof(Message), nameof(Message.CalculateRect), instance, typeof(float), typeof(float)),
            Method(typeof(WindowStack), nameof(WindowStack.ImmediateWindow), instance, registration),
            Method(typeof(WindowStack), "AddNewImmediateWindow", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, registration),
            Method(typeof(Messages), nameof(Messages.MessagesDoGUI), statics),
            Method(typeof(UIRoot), nameof(UIRoot.UIRootOnGUI), instance),
            Method(typeof(UIRoot_Entry), nameof(UIRoot_Entry.UIRootOnGUI), instance),
            Method(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI), instance),
            Method(typeof(Messages), nameof(Messages.CollidesWithAnyMessage), statics, typeof(Rect), typeof(float).MakeByRefType()),
            Field(typeof(Message), nameof(Message.lastDrawRect)), Field(typeof(Window), nameof(Window.windowRect)),
            Field(typeof(ImmediateWindow), nameof(ImmediateWindow.doWindowFunc)));
    }
    private static MethodInfo Method(Type owner, string name, BindingFlags flags, params Type[] arguments) =>
        owner.GetMethod(name, flags, null, arguments, null) ?? throw new MissingMethodException(owner.FullName, name);
    private static FieldInfo Field(Type owner, string name) =>
        owner.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) ??
        throw new MissingFieldException(owner.FullName, name);
}
