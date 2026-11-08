using System.Reflection;
using RimWorld;
using Verse;

namespace RimWorldDevGateway;

internal sealed class VerseGatewayMainTab : IGatewayMainTabRuntime
{
    private static readonly FieldInfo? Buttons = typeof(MainButtonsRoot).GetField("allButtonsInOrder",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
    private static readonly MethodInfo? Command = typeof(MainButtonWorker).GetMethod("InterfaceTryActivate",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);

    public static void Register(GatewayAutomationRegistry registry) => GatewayMainTabAutomation.Register(
        registry, new VerseGatewayMainTab(), Availability);

    private static GatewayAutomationAvailability Availability()
    {
#if RIMWORLD_1_7
        return new GatewayAutomationAvailability(Buttons?.FieldType == typeof(List<MainButtonDef>) &&
            Command is { ReturnType: var result, IsGenericMethod: false, ContainsGenericParameters: false } &&
            result == typeof(void) && Command.GetMethodBody() != null, "native_main_tab_command_unavailable");
#else
        return new GatewayAutomationAvailability(false, "native_main_tab_runtime_unverified");
#endif
    }

    public GatewayMainTabState Read(string defName)
    {
        RequireMainThread();
        var unavailable = new GatewayMainTabState(defName, false, 0, false, false, true, false, false, null);
        if (!Availability().Available || !PlayDataLoader.Loaded || Current.ProgramState != ProgramState.Playing ||
            Current.Game is not { } game || Current.Root is not Root_Play root ||
            Find.UIRoot is not UIRoot_Play ui || ui.mainButtonsRoot is not { } bar ||
            Find.WindowStack is not { } stack || Find.MainTabsRoot is not { } tabs ||
            LongEventHandler.AnyEventNowOrWaiting || !game.PlayerHasControl || ui.screenshotMode.Active)
            return unavailable;

        if (Buttons!.GetValue(bar) is not List<MainButtonDef> buttons) return unavailable;
        // Resolve the actual rendered bar's list, not a potentially newer Def database.
        var matches = buttons.Where(button => button != null &&
            string.Equals(button.defName, defName, StringComparison.Ordinal)).ToArray();
        var open = tabs.OpenTab;
        var state = unavailable with { Ready = true, MatchCount = matches.Length,
            OpenTabDefName = open?.defName, ReceivesInput = stack.GetsInput(null) };
        if (matches.Length != 1) return state;
        var target = matches[0];
        state = state with { IsOpen = ReferenceEquals(open, target) };
        var windowType = target.tabWindowClass;
        if (target.workerClass != typeof(MainButtonWorker_ToggleTab) || windowType == null ||
            !typeof(MainTabWindow).IsAssignableFrom(windowType) || windowType.IsAbstract ||
            windowType.ContainsGenericParameters || windowType.GetConstructor(Type.EmptyTypes) == null)
            return state;

        var worker = target.Worker;
        var openWindows = stack.Windows.OfType<MainTabWindow>().ToArray();
        bool supported = worker.GetType() == typeof(MainButtonWorker_ToggleTab) && ReferenceEquals(worker.def, target) &&
            openWindows.Length <= 1 && (!state.IsOpen ||
                openWindows.Length == 1 && openWindows[0].GetType() == windowType && openWindows[0].IsOpen);
        // Do not access target.TabWindow here: its getter constructs the window before the command.
        return state with { Supported = supported, Visible = worker.Visible, Disabled = worker.Disabled,
            Context = new(game, root, ui, bar, stack, tabs, target, worker, Find.CurrentMap) };
    }

    public void Activate(GatewayMainTabState expected, Action beforeNativeInvoke)
    {
        var current = Read(expected.DefName);
        GatewayMainTabAutomation.RequireAvailable(current);
        if (expected.Context == null || !expected.Context.Matches(current.Context) ||
            current.IsOpen != expected.IsOpen || current.OpenTabDefName != expected.OpenTabDefName)
            throw new GatewayAutomationException("native_main_tab_context_changed",
                "The native tab or game/UI context changed before its command could run.");
        beforeNativeInvoke();
        ((MainButtonWorker)current.Context!.Worker).InterfaceTryActivate();
    }

    private static void RequireMainThread()
    {
        if (!UnityData.IsInMainThread)
            throw new GatewayAutomationException("native_main_tab_wrong_thread", "Native tab commands require the game thread.");
    }
}
