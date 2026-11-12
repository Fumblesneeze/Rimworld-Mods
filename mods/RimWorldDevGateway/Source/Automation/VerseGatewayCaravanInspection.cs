using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimWorldDevGateway;

internal sealed class VerseGatewayCaravanInspection : IGatewayCaravanInspectionRuntime
{
    public GatewayAutomationAvailability Availability()
    {
#if RIMWORLD_1_7
        return new(TabCommand is { IsStatic: true, IsGenericMethod: false, ContainsGenericParameters: false, ReturnType: var result } &&
            result == typeof(void) && TabCommand.GetMethodBody() != null && DetailPawn?.FieldType == typeof(Pawn), "native_caravan_contract_unavailable");
#else
        return new(false, "native_caravan_runtime_unverified");
#endif
    }
    public IGatewayCaravanInspection Capture(IGatewayWorldViewSession view, string loadId)
    {
#if RIMWORLD_1_7
        if (!Availability().Available || view is not VerseGatewayWorldViewSnapshot snapshot || !snapshot.CurrentOwner)
            throw Error("native_caravan_context_unavailable");
        var list = snapshot.World.worldObjects.Caravans;
        // Native maintained caravan lister, bounded before any traversal; never report a partial scan as absence.
        if (list.Count > 1024) throw Error("native_caravan_inventory_limit");
        var matching = list.Where(c => c != null && c.GetUniqueLoadID() == loadId).ToArray();
        if (matching.Length != 1) throw Error("native_caravan_identity_unavailable");
        var result = new Captured(snapshot, matching[0], list);
        result.RequireCurrent();
        foreach (var tab in result.InspectionTabs().Where(t => t.GetType() == typeof(WITab_Caravan_Health)).Cast<WITab_Caravan_Health>()) snapshot.OwnHealthTab(tab);
        return result;
#else
        throw Error("native_caravan_runtime_unverified");
#endif
    }

#if RIMWORLD_1_7
    internal static readonly MethodInfo? TabCommand = typeof(InspectPaneUtility).GetMethod("InterfaceToggleTab",
        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, new[] { typeof(InspectTabBase), typeof(IInspectPane) }, null);
    internal static readonly FieldInfo? DetailPawn = typeof(WITab_Caravan_Health).GetField("specificHealthTabForPawn",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

    internal sealed class Captured : IGatewayCaravanInspection
    {
        internal readonly VerseGatewayWorldViewSnapshot View;
        internal readonly Caravan Caravan;
        private readonly List<Caravan> list;
        private readonly string loadId;
        internal Captured(VerseGatewayWorldViewSnapshot view, Caravan caravan, List<Caravan> list)
        { View = view; Caravan = caravan; this.list = list; loadId = caravan.GetUniqueLoadID(); }
        internal bool Current => View.CurrentOwner && ReferenceEquals(View.World.worldObjects.Caravans, list) && list.Count <= 1024 &&
            list.Contains(Caravan) && Caravan.Spawned && Caravan.SelectableNow && Caravan.Faction == Faction.OfPlayer &&
            Caravan.GetUniqueLoadID() == loadId && Caravan.Tile.Valid && Caravan.Tile.Layer == View.World.grid.Surface &&
            View.World.grid.InBounds(Caravan.Tile) && Caravan.PawnsListForReading.Count <= 64;
        internal void RequireCurrent() { if (!Current) throw Error("stale_caravan_handle"); }
        internal bool Selected => Current && PlayDataLoader.Loaded && !LongEventHandler.AnyEventNowOrWaiting &&
            View.Game.PlayerHasControl && !View.Ui.screenshotMode.Active && WorldRendererUtility.WorldSelected &&
            !View.WorldUi.targeter.IsTargeting && !View.WorldUi.routePlanner.Active && !View.WorldUi.tilePicker.Active &&
            View.Selector.SelectedObjects.Count == 1 && ReferenceEquals(View.Selector.SingleSelectedObject, Caravan);
        internal WITab_Caravan_Health? HealthTab
        {
            get
            {
                if (!Selected) return null;
                var tabs = InspectionTabs().Where(t => t.GetType() == typeof(WITab_Caravan_Health)).ToArray();
                if (tabs.Length > 1) throw Error("native_caravan_health_tab_ambiguous");
                return tabs.Length == 1 ? (WITab_Caravan_Health)tabs[0] : null;
            }
        }
        internal InspectTabBase[] InspectionTabs()
        {
            var tabs = Caravan.GetInspectTabs().Take(17).ToArray();
            if (tabs.Length > 16) throw Error("native_caravan_tab_limit");
            return tabs;
        }
        public GatewayCaravanState Observe()
        {
            if (!Current) return new(loadId, string.Empty, Array.Empty<string>(), false, false, false, null, null);
            bool selected = Selected;
            string? tab = View.Pane.OpenTabType == typeof(WITab_Caravan_Items) ? "items" :
                View.Pane.OpenTabType == typeof(WITab_Caravan_Health) ? "health" : null;
            var health = selected ? HealthTab : null;
            return new(loadId, Caravan.LabelCap.ToString(), Caravan.PawnsListForReading.Select(p => p.GetUniqueLoadID()).ToArray(),
                true, selected, selected && View.Pane.IsOpen, selected ? tab : null,
                health == null ? null : (DetailPawn!.GetValue(health) as Pawn)?.GetUniqueLoadID(), View.CanCommand);
        }
        public void Select(Action admission)
        {
            RequireCurrent(); View.RequireCommands();
            if (!WorldRendererUtility.WorldSelected) throw Error("native_caravan_world_closed");
            admission(); View.OwnSelection(Caravan);
            CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(Caravan));
        }
        public void ToggleTab(string tab, Action admission)
        {
            RequireCurrent(); View.RequireCommands();
            if (!Selected || !WorldRendererUtility.WorldSelected || !View.Pane.IsOpen || !View.Stack.GetsInput(View.Pane))
                throw Error("native_caravan_pane_unavailable");
            Type type = tab == "items" ? typeof(WITab_Caravan_Items) : typeof(WITab_Caravan_Health);
            var matches = InspectionTabs().Where(t => t.GetType() == type && t.IsVisible && !t.Hidden).ToArray();
            if (matches.Length != 1 || !View.Pane.CurTabs.Any(t => ReferenceEquals(t, matches[0]))) throw Error("native_caravan_tab_unavailable");
            admission();
            try { TabCommand!.Invoke(null, new object[] { matches[0], View.Pane }); }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw(); throw; }
        }
    }
#endif
    private static GatewayAutomationException Error(string code) => new(code, "The native caravan identity, membership and inspection command must remain current.");
}
