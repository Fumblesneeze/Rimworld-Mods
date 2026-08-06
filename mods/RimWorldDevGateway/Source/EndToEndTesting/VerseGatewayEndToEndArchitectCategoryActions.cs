using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace RimWorldDevGateway;

internal interface IGatewayEndToEndArchitectCategoryRuntime
{
    bool PlayerHasControl { get; }

    bool IsArchitectOpen { get; }

    bool HasCategory(string categoryDefName);

    void OpenArchitect();

    bool SelectCategory(string categoryDefName);

    void CloseArchitect();
}

internal static class VerseGatewayEndToEndArchitectCategoryActions
{
    internal static GatewayEndToEndStepOutcome Apply(
        ArchitectCategoryActionStep step,
        IGatewayEndToEndArchitectCategoryRuntime runtime)
    {
        if (step is null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        if (runtime is null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        if (!runtime.PlayerHasControl)
        {
            return Fail(
                "architect_player_control_required",
                "Native player control is required to change the Architect tab.");
        }

        try
        {
            if (!runtime.HasCategory(step.CategoryDefName))
            {
                return Fail(
                    "architect_category_missing",
                    "The requested exact Architect category tab is not loaded.");
            }

            if (!step.Open)
            {
                if (runtime.IsArchitectOpen)
                {
                    runtime.CloseArchitect();
                }

                return runtime.IsArchitectOpen
                    ? Fail("architect_tab_close_failed", "RimWorld did not close the Architect tab.")
                    : GatewayEndToEndStepOutcome.Pass();
            }

            if (!runtime.IsArchitectOpen)
            {
                runtime.OpenArchitect();
            }

            if (!runtime.IsArchitectOpen)
            {
                return Fail(
                    "architect_tab_open_failed",
                    "RimWorld did not open the Architect tab.");
            }

            return runtime.SelectCategory(step.CategoryDefName) && runtime.IsArchitectOpen
                ? GatewayEndToEndStepOutcome.Pass(
                    new Dictionary<string, string>
                    {
                        ["categoryDefName"] = step.CategoryDefName,
                        ["open"] = bool.TrueString
                    })
                : Fail(
                    "architect_category_selection_failed",
                    "RimWorld did not select exactly one matching Architect category tab.");
        }
        catch (Exception exception)
        {
            return Fail(
                "architect_action_failed",
                "The native Architect action threw " + exception.GetType().Name + ".");
        }
    }

    private static GatewayEndToEndStepOutcome Fail(string code, string message) =>
        GatewayEndToEndStepOutcome.Fail(code, message);
}

internal sealed class VerseGatewayEndToEndArchitectCategoryRuntime :
    IGatewayEndToEndArchitectCategoryRuntime
{
    private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly FieldInfo? CachedTabsField = typeof(MainTabWindow_Architect)
        .GetField("desPanelsCached", InstanceNonPublic);
    private static readonly MethodInfo? ClickedCategoryMethod = typeof(MainTabWindow_Architect)
        .GetMethod("ClickedCategory", InstanceNonPublic, null, new[] { typeof(ArchitectCategoryTab) }, null);

    public bool PlayerHasControl => Current.Game?.PlayerHasControl == true;

    public bool IsArchitectOpen => ReferenceEquals(Find.MainTabsRoot?.OpenTab, MainButtonDefOf.Architect);

    public bool HasCategory(string categoryDefName) =>
        TryGetExactCategoryTab(categoryDefName, out _, out _);

    public void OpenArchitect()
    {
        if (!IsArchitectOpen)
        {
            MainButtonDefOf.Architect.Worker.InterfaceTryActivate();
        }
    }

    public bool SelectCategory(string categoryDefName)
    {
        if (!TryGetExactCategoryTab(categoryDefName, out var window, out var selected))
        {
            return false;
        }

        if (!ReferenceEquals(window.selectedDesPanel, selected))
        {
            ClickedCategoryMethod!.Invoke(window, new object[] { selected });
        }

        return ReferenceEquals(window.selectedDesPanel, selected);
    }

    public void CloseArchitect()
    {
        if (IsArchitectOpen)
        {
            Find.MainTabsRoot.EscapeCurrentTab(playSound: false);
        }
    }

    private static bool TryGetExactCategoryTab(
        string categoryDefName,
        out MainTabWindow_Architect window,
        out ArchitectCategoryTab selected)
    {
        window = null!;
        selected = null!;
        var category = DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(categoryDefName);
        var candidateWindow = MainButtonDefOf.Architect.TabWindow as MainTabWindow_Architect;
        if (category is null || candidateWindow is null || CachedTabsField is null || ClickedCategoryMethod is null)
        {
            return false;
        }

        if (CachedTabsField.GetValue(candidateWindow) is not List<ArchitectCategoryTab> cachedTabs)
        {
            return false;
        }

        var matches = cachedTabs.Where(tab => ReferenceEquals(tab.def, category)).ToArray();
        if (matches.Length != 1)
        {
            return false;
        }

        window = candidateWindow;
        selected = matches[0];
        return true;
    }
}
