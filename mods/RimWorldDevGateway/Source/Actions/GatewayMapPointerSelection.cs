using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimWorldDevGateway;

internal static class GatewayMapPointerSelection
{
    private const string ActionName = "map.pointer.select";
    private const string Owner = "fumblesneeze.rimworlddevgateway.map-pointer";
    private static Vector2? pointer;

    public static GatewaySemanticActionAvailability Availability()
    {
        if (Current.Game?.CurrentMap is null || !GenScene.InPlayScene ||
            !WorldRendererUtility.DrawingMap || !Current.Game.PlayerHasControl ||
            LongEventHandler.AnyEventNowOrWaiting || Find.WindowStack.AnyWindowAbsorbingAllInput ||
            Find.DesignatorManager.SelectedDesignator is not null || Find.Targeter.IsTargeting ||
            DebugTools.curTool is not null || Find.Selector.dragBox.active ||
            Find.Selector.gotoController.Active || pointer.HasValue)
            return GatewaySemanticActionAvailability.Unavailable("An unblocked, settled map without an active input tool is required.");
        return GatewaySemanticActionAvailability.Available();
    }

    public static GatewaySemanticActionInvocationResult Select(float x, float z)
    {
        var available = Availability();
        if (!available.IsAvailable) return Failure("action_unavailable", available.UnavailableReason!);
        return SelectLive(x, z);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static GatewaySemanticActionInvocationResult SelectLive(float x, float z)
    {
        Map map = Find.CurrentMap;
        if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(z) || float.IsInfinity(z) ||
            x < 0 || z < 0 || x >= map.Size.x || z >= map.Size.z)
            return Failure("pointer_out_of_bounds", "Pointer must be inside the current map.");
        Vector2 gui = new Vector3(x, 0, z).MapToUIPosition();
        // Keep the click inside the map viewport, away from the developer/top and bottom toolbars.
        if (gui.x < 0 || gui.x >= UI.screenWidth || gui.y < 40 || gui.y >= UI.screenHeight - 200 ||
            Find.WindowStack.GetWindowAt(UI.GUIToScreenPoint(gui)) is not null ||
            Find.ColonistBar.ColonistOrCorpseAt(gui) is not null ||
            Find.ColonistBar.CaravanMemberCaravanAt(gui) is not null)
            return Failure("pointer_obscured", "Pointer is outside the unobstructed map viewport.");
        MethodInfo mouse = typeof(UI).GetProperty(nameof(UI.MousePositionOnUI))!.GetGetMethod();
        MethodInfo shift = typeof(Selector).GetProperty(nameof(Selector.ShiftIsHeld))!.GetGetMethod();
        MethodInfo mousePrefix = typeof(GatewayMapPointerSelection).GetMethod(nameof(MousePrefix), BindingFlags.NonPublic | BindingFlags.Static)!;
        MethodInfo shiftPrefix = typeof(GatewayMapPointerSelection).GetMethod(nameof(ShiftPrefix), BindingFlags.NonPublic | BindingFlags.Static)!;
        if (mouse.ReturnType != typeof(Vector2) || !mouse.IsStatic || shift.ReturnType != typeof(bool) || !shift.IsStatic)
            return Failure("pointer_seam_unavailable", "Native pointer input signatures differ.");
        var harmony = new Harmony(Owner);
        Event? priorEvent = Event.current;
        Selector selector = Find.Selector;
        Vector3 oldStart = selector.dragBox.start;
        string[] before = Selection();
        try
        {
            harmony.Patch(mouse, prefix: new HarmonyMethod(mousePrefix));
            harmony.Patch(shift, prefix: new HarmonyMethod(shiftPrefix));
            pointer = new Vector2(gui.x, UI.screenHeight - gui.y);
            foreach (EventType type in new[] { EventType.MouseDown, EventType.MouseUp })
            {
                Event.current = new Event { type = type, button = 0, clickCount = 1, mousePosition = gui };
                selector.SelectorOnGUI();
                if (Event.current.type != EventType.Used)
                    return Failure("pointer_unconsumed", "Native Selector did not consume the click.");
            }
            return GatewaySemanticActionInvocationResult.Success(ActionName, "1",
                new GatewaySemanticActionResult(ActionName,
                    new Dictionary<string, object?> { ["selectedIds"] = before },
                    new Dictionary<string, object?> { ["selectedIds"] = Selection(), ["x"] = x, ["z"] = z,
                        ["desktopInput"] = false }));
        }
        finally
        {
            pointer = null;
            Event.current = priorEvent;
            selector.dragBox.active = false;
            selector.dragBox.start = oldStart;
            try { harmony.Unpatch(mouse, mousePrefix); }
            finally { harmony.Unpatch(shift, shiftPrefix); }
        }
    }

    private static bool MousePrefix(ref Vector2 __result)
    {
        if (!pointer.HasValue) return true;
        __result = pointer.Value;
        return false;
    }

    private static bool ShiftPrefix(ref bool __result)
    {
        if (!pointer.HasValue) return true;
        __result = false;
        return false;
    }

    private static string[] Selection() => Find.Selector.SelectedObjectsListForReading
        .Select(x => x is Thing thing ? thing.ThingID : x.GetType().Name).ToArray();

    private static GatewaySemanticActionInvocationResult Failure(string code, string message) =>
        GatewaySemanticActionInvocationResult.Failure(ActionName, "1", new GatewaySemanticActionError(code, message));
}
