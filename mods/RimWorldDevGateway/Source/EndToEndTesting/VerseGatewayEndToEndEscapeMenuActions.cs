using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace RimWorldDevGateway;

internal interface IGatewayEndToEndEscapeMenuRuntime
{
    bool PlayerControlledPlayableGame { get; }

    bool EscapeMenuOpen { get; }

    void OpenEscapeMenu();

    void CloseEscapeMenu();
}

internal static class VerseGatewayEndToEndEscapeMenuActions
{
    internal static GatewayEndToEndStepOutcome Apply(
        EscapeMenuActionStep step,
        IGatewayEndToEndEscapeMenuRuntime runtime)
    {
        if (step is null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        if (runtime is null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        if (!runtime.PlayerControlledPlayableGame)
        {
            return Fail(
                "escape_menu_player_control_required",
                "A player-controlled playable game is required to operate the Escape menu.");
        }

        try
        {
            if (step.Open)
            {
                if (!runtime.EscapeMenuOpen)
                {
                    runtime.OpenEscapeMenu();
                }

                return runtime.EscapeMenuOpen
                    ? GatewayEndToEndStepOutcome.Pass()
                    : Fail(
                        "escape_menu_open_failed",
                        "RimWorld did not make the native in-play Escape menu current.");
            }

            if (!runtime.EscapeMenuOpen)
            {
                return Fail(
                    "escape_menu_not_open",
                    "The native in-play Escape menu is not current.");
            }

            runtime.CloseEscapeMenu();
            return !runtime.EscapeMenuOpen
                ? GatewayEndToEndStepOutcome.Pass()
                : Fail(
                    "escape_menu_close_failed",
                    "RimWorld did not close the native in-play Escape menu.");
        }
        catch (Exception exception)
        {
            return Fail(
                "escape_menu_action_failed",
                "The native Escape-menu action threw " + exception.GetType().Name + ".");
        }
    }

    private static GatewayEndToEndStepOutcome Fail(string code, string message) =>
        GatewayEndToEndStepOutcome.Fail(code, message);
}

internal sealed class VerseGatewayEndToEndEscapeMenuRuntime :
    IGatewayEndToEndEscapeMenuRuntime
{
    public bool PlayerControlledPlayableGame =>
        Current.ProgramState == ProgramState.Playing &&
        Current.Root is Root_Play &&
        Current.Game?.PlayerHasControl == true;

    public bool EscapeMenuOpen =>
        Find.MainTabsRoot.OpenTab == MainButtonDefOf.Menu;

    public void OpenEscapeMenu() =>
        Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Menu, playSound: false);

    public void CloseEscapeMenu() =>
        Find.MainTabsRoot.EscapeCurrentTab(playSound: false);
}
