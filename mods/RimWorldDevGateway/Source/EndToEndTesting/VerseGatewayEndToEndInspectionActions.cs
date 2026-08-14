using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace RimWorldDevGateway;

internal interface IGatewayEndToEndInspectionRuntime
{
    bool PlayerHasControl { get; }

    bool IsSoleSelectedPawn(string runtimeId);

    bool IsSoleSelectedThing(string runtimeId);

    bool OpenPawnTab(EndToEndPawnInspectTab tab);

    bool IsPawnTabOpen(EndToEndPawnInspectTab tab);

    bool OpenThingInfoCard(string runtimeId);

    bool IsExactThingInfoCardOpen(string runtimeId);

    bool CloseExactThingInfoCard(string runtimeId);

    bool CloseInspectPane(string expectedTabRuntimeType);

    bool IsInspectPaneClosed { get; }

    bool IsExactWindowOpen(string expectedWindowRuntimeType);

    bool CancelExactWindow(string expectedWindowRuntimeType);

    bool AcceptExactWindow(string expectedWindowRuntimeType);

    bool OpenModSettings(string packageId);

    bool IsExactModSettingsOpen(string packageId);
}

internal sealed class GatewayEndToEndInspectionLimitException : Exception
{
}

internal sealed class GatewayEndToEndInspectionIncompleteException : Exception
{
    internal GatewayEndToEndInspectionIncompleteException(Exception innerException)
        : base("Exact Thing resolution could not inspect every supported candidate scope.", innerException)
    {
    }
}

internal static class GatewayEndToEndCancelEventPolicy
{
    internal static bool RequiresSyntheticKeyEvent(
        bool currentEventIsKeyDown,
        bool currentKeyIsEscape) =>
        !currentEventIsKeyDown || !currentKeyIsEscape;
}

internal static class VerseGatewayEndToEndInspectionActions
{
    internal static GatewayEndToEndStepOutcome Apply(
        PawnInspectTabActionStep step,
        IGatewayEndToEndInspectionRuntime runtime)
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
                "inspect_tab_player_control_required",
                "Native player control is required to open a pawn inspect tab.");
        }

        try
        {
            if (!runtime.IsSoleSelectedPawn(step.PawnRuntimeId))
            {
                return Fail(
                    "inspect_tab_selection_mismatch",
                    "The requested exact pawn is not the current sole selection.");
            }

            return runtime.OpenPawnTab(step.Tab) && runtime.IsPawnTabOpen(step.Tab)
                ? GatewayEndToEndStepOutcome.Pass(
                    new Dictionary<string, string>
                    {
                        ["pawnRuntimeId"] = step.PawnRuntimeId,
                        ["tab"] = step.Tab.ToString()
                    })
                : Fail(
                    "inspect_tab_open_failed",
                    "RimWorld did not open the requested exact pawn inspect tab.");
        }
        catch (Exception exception)
        {
            return Fail(
                "inspect_tab_action_failed",
                "The native pawn inspect-tab action threw " + exception.GetType().Name + ".");
        }
    }

    internal static GatewayEndToEndStepOutcome Apply(
        ThingInfoCardActionStep step,
        IGatewayEndToEndInspectionRuntime runtime)
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
                "info_card_player_control_required",
                "Native player control is required to operate a Thing info card.");
        }

        try
        {
            if (step.IsOpenAction)
            {
                return runtime.OpenThingInfoCard(step.ThingRuntimeId) &&
                       runtime.IsExactThingInfoCardOpen(step.ThingRuntimeId)
                    ? GatewayEndToEndStepOutcome.Pass(
                        new Dictionary<string, string>
                        {
                            ["thingRuntimeId"] = step.ThingRuntimeId,
                            ["open"] = bool.TrueString
                        })
                    : Fail(
                        "info_card_open_failed",
                        "RimWorld did not open one info card bound to the requested exact Thing.");
            }

            if (!runtime.IsExactThingInfoCardOpen(step.ThingRuntimeId))
            {
                return Fail(
                    "info_card_window_mismatch",
                    "No open info card is bound to the requested exact Thing.");
            }

            return runtime.CloseExactThingInfoCard(step.ThingRuntimeId) &&
                   !runtime.IsExactThingInfoCardOpen(step.ThingRuntimeId)
                ? GatewayEndToEndStepOutcome.Pass(
                    new Dictionary<string, string>
                    {
                        ["thingRuntimeId"] = step.ThingRuntimeId,
                        ["open"] = bool.FalseString
                    })
                : Fail(
                    "info_card_close_failed",
                    "RimWorld did not close the requested exact Thing info card.");
        }
        catch (GatewayEndToEndInspectionLimitException)
        {
            return Fail(
                "info_card_resolution_limit",
                "Exact Thing resolution exceeded the bounded interaction candidate budget.");
        }
        catch (GatewayEndToEndInspectionIncompleteException exception)
        {
            return Fail(
                "info_card_resolution_incomplete",
                "Exact Thing resolution could not inspect every supported candidate scope because " +
                exception.InnerException?.GetType().Name + ".");
        }
        catch (Exception exception)
        {
            return Fail(
                "info_card_action_failed",
                "The native Thing info-card action threw " + exception.GetType().Name + ".");
        }
    }

    internal static GatewayEndToEndStepOutcome Apply(
        ModSettingsActionStep step,
        IGatewayEndToEndInspectionRuntime runtime)
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
                "mod_settings_player_control_required",
                "Native player control is required to open mod settings.");
        }

        try
        {
            return runtime.OpenModSettings(step.PackageId) &&
                   runtime.IsExactModSettingsOpen(step.PackageId)
                ? GatewayEndToEndStepOutcome.Pass(
                    new Dictionary<string, string>
                    {
                        ["packageId"] = step.PackageId
                    })
                : Fail(
                    "mod_settings_open_failed",
                    "RimWorld did not open one native settings dialog bound to the requested exact active mod.");
        }
        catch (Exception exception)
        {
            return Fail(
                "mod_settings_action_failed",
                "The native mod-settings action threw " + exception.GetType().Name + ".");
        }
    }

    internal static GatewayEndToEndStepOutcome Apply(
        InspectPaneCloseActionStep step,
        IGatewayEndToEndInspectionRuntime runtime)
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
                "inspect_pane_player_control_required",
                "Native player control is required to close an inspect pane.");
        }

        try
        {
            if (!runtime.IsSoleSelectedThing(step.SelectedThingRuntimeId))
            {
                return Fail(
                    "inspect_pane_selection_mismatch",
                    "The requested exact Thing is not the current sole selection.");
            }

            return runtime.CloseInspectPane(step.ExpectedTabRuntimeType) &&
                   runtime.IsInspectPaneClosed
                ? GatewayEndToEndStepOutcome.Pass(
                    new Dictionary<string, string>
                    {
                        ["selectedThingRuntimeId"] = step.SelectedThingRuntimeId,
                        ["closedTabRuntimeType"] = step.ExpectedTabRuntimeType
                    })
                : Fail(
                    "inspect_pane_close_failed",
                    "RimWorld did not close the requested exact inspect tab.");
        }
        catch (Exception exception)
        {
            return Fail(
                "inspect_pane_action_failed",
                "The native inspect-pane close action threw " + exception.GetType().Name + ".");
        }
    }

    internal static GatewayEndToEndStepOutcome Apply(
        WindowCancelActionStep step,
        IGatewayEndToEndInspectionRuntime runtime)
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
                "window_cancel_player_control_required",
                "Native player control is required to cancel a window.");
        }

        try
        {
            if (!runtime.IsExactWindowOpen(step.ExpectedWindowRuntimeType))
            {
                return Fail(
                    "window_cancel_identity_mismatch",
                    "Exactly one open window did not match the requested runtime type.");
            }

            return runtime.CancelExactWindow(step.ExpectedWindowRuntimeType) &&
                   !runtime.IsExactWindowOpen(step.ExpectedWindowRuntimeType)
                ? GatewayEndToEndStepOutcome.Pass(
                    new Dictionary<string, string>
                    {
                        ["cancelledWindowRuntimeType"] = step.ExpectedWindowRuntimeType
                    })
                : Fail(
                    "window_cancel_failed",
                    "RimWorld did not cancel the requested exact window.");
        }
        catch (Exception exception)
        {
            return Fail(
                "window_cancel_action_failed",
                "The native exact-window cancel action threw " + exception.GetType().Name + ".");
        }
    }

    internal static GatewayEndToEndStepOutcome Apply(
        WindowAcceptActionStep step,
        IGatewayEndToEndInspectionRuntime runtime)
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
                "window_accept_player_control_required",
                "Native player control is required to accept a window.");
        }

        try
        {
            if (!runtime.IsExactWindowOpen(step.ExpectedWindowRuntimeType))
            {
                return Fail(
                    "window_accept_identity_mismatch",
                    "Exactly one open window did not match the requested runtime type.");
            }

            return runtime.AcceptExactWindow(step.ExpectedWindowRuntimeType) &&
                   !runtime.IsExactWindowOpen(step.ExpectedWindowRuntimeType)
                ? GatewayEndToEndStepOutcome.Pass(
                    new Dictionary<string, string>
                    {
                        ["acceptedWindowRuntimeType"] = step.ExpectedWindowRuntimeType
                    })
                : Fail(
                    "window_accept_failed",
                    "RimWorld did not accept the requested exact window.");
        }
        catch (Exception exception)
        {
            return Fail(
                "window_accept_action_failed",
                "The native exact-window accept action threw " + exception.GetType().Name + ".");
        }
    }

    private static GatewayEndToEndStepOutcome Fail(string code, string message) =>
        GatewayEndToEndStepOutcome.Fail(code, message);
}

internal interface IGatewayEndToEndThingCandidateSource
{
    IEnumerable<Thing> SpawnedThings();

    IEnumerable<Thing> PawnInventoryThings();

    IEnumerable<Thing> DirectlyHeldThings(IThingHolder holder);
}

internal sealed class VerseGatewayEndToEndThingCandidateSource : IGatewayEndToEndThingCandidateSource
{
    public IEnumerable<Thing> SpawnedThings()
    {
        foreach (var map in Find.Maps ?? new List<Map>())
        {
            foreach (var thing in map.listerThings?.AllThings ?? Enumerable.Empty<Thing>())
            {
                yield return thing;
            }
        }
    }

    public IEnumerable<Thing> PawnInventoryThings()
    {
        foreach (var map in Find.Maps ?? new List<Map>())
        {
            foreach (var pawn in map.mapPawns?.AllPawns ?? Enumerable.Empty<Pawn>())
            {
                foreach (var thing in pawn.inventory?.innerContainer?.OfType<Thing>() ??
                                      Enumerable.Empty<Thing>())
                {
                    yield return thing;
                }
            }
        }
    }

    public IEnumerable<Thing> DirectlyHeldThings(IThingHolder holder) =>
        holder.GetDirectlyHeldThings()?.OfType<Thing>() ?? Enumerable.Empty<Thing>();
}

internal sealed class GatewayEndToEndThingResolver
{
    internal const int MaximumThingCandidates = GatewayGizmoRegistry.MaximumInteractionTargets;
    internal const int MaximumHolderCandidates = GatewayGizmoRegistry.MaximumInteractionTargets;
    private readonly IGatewayEndToEndThingCandidateSource source;

    internal GatewayEndToEndThingResolver(IGatewayEndToEndThingCandidateSource source)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
    }

    internal Thing? ResolveExactThing(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
        {
            return null;
        }

        var matches = new HashSet<Thing>(ThingReferenceComparer.Instance);
        var seenThings = new HashSet<Thing>(ThingReferenceComparer.Instance);
        var holders = new List<IThingHolder>();
        var seenHolders = new HashSet<IThingHolder>(HolderReferenceComparer.Instance);

        void Consider(Thing? thing, bool collectHolder)
        {
            if (thing is null || !seenThings.Add(thing))
            {
                return;
            }

            if (seenThings.Count > MaximumThingCandidates)
            {
                throw new GatewayEndToEndInspectionLimitException();
            }

            if (collectHolder && thing is IThingHolder holder && seenHolders.Add(holder))
            {
                if (seenHolders.Count > MaximumHolderCandidates)
                {
                    throw new GatewayEndToEndInspectionLimitException();
                }

                holders.Add(holder);
            }

            if (!thing.Destroyed && Matches(thing, runtimeId))
            {
                matches.Add(thing);
            }
        }

        try
        {
            foreach (var thing in source.SpawnedThings())
            {
                Consider(thing, collectHolder: true);
            }

            foreach (var thing in source.PawnInventoryThings())
            {
                Consider(thing, collectHolder: false);
            }

            foreach (var holder in holders)
            {
                foreach (var thing in source.DirectlyHeldThings(holder))
                {
                    Consider(thing, collectHolder: false);
                }
            }
        }
        catch (GatewayEndToEndInspectionLimitException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new GatewayEndToEndInspectionIncompleteException(exception);
        }

        return matches.Count == 1 ? matches.Single() : null;
    }

    private static bool Matches(Thing thing, string runtimeId) =>
        string.Equals(thing.ThingID, runtimeId, StringComparison.Ordinal) ||
        string.Equals(thing.GetUniqueLoadID(), runtimeId, StringComparison.Ordinal);

    private sealed class ThingReferenceComparer : IEqualityComparer<Thing>
    {
        internal static readonly ThingReferenceComparer Instance = new();

        public bool Equals(Thing? x, Thing? y) => ReferenceEquals(x, y);

        public int GetHashCode(Thing obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    private sealed class HolderReferenceComparer : IEqualityComparer<IThingHolder>
    {
        internal static readonly HolderReferenceComparer Instance = new();

        public bool Equals(IThingHolder? x, IThingHolder? y) => ReferenceEquals(x, y);

        public int GetHashCode(IThingHolder obj) =>
            System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}

internal static class GatewayEndToEndThingReferencePolicy
{
    internal static bool CanOpenExactReference(
        Thing exactThing,
        string runtimeId,
        IEnumerable<Thing?> candidates)
    {
        if (exactThing is null)
        {
            throw new ArgumentNullException(nameof(exactThing));
        }

        if (string.IsNullOrWhiteSpace(runtimeId))
        {
            throw new ArgumentException("A Thing runtime ID is required.", nameof(runtimeId));
        }

        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        return candidates.All(candidate =>
            candidate is null ||
            (!IsExactReference(exactThing, candidate) && !MatchesRuntimeIdentity(candidate, runtimeId)));
    }

    internal static bool HasExactlyOneExactReference(Thing exactThing, IEnumerable<Thing?> candidates)
        => CountExactReferences(exactThing, candidates) == 1;

    internal static bool IsExactReference(Thing exactThing, Thing? candidate)
    {
        if (exactThing is null)
        {
            throw new ArgumentNullException(nameof(exactThing));
        }

        return ReferenceEquals(candidate, exactThing);
    }

    private static int CountExactReferences(Thing exactThing, IEnumerable<Thing?> candidates)
    {
        if (exactThing is null)
        {
            throw new ArgumentNullException(nameof(exactThing));
        }

        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        return candidates.Count(candidate => IsExactReference(exactThing, candidate));
    }

    private static bool MatchesRuntimeIdentity(Thing thing, string runtimeId) =>
        string.Equals(thing.ThingID, runtimeId, StringComparison.Ordinal) ||
        string.Equals(thing.GetUniqueLoadID(), runtimeId, StringComparison.Ordinal);
}

internal sealed class VerseGatewayEndToEndInspectionRuntime : IGatewayEndToEndInspectionRuntime
{
    private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly FieldInfo? InfoCardThingField = typeof(Dialog_InfoCard)
        .GetField("thing", InstanceNonPublic);
    private static readonly FieldInfo? ModSettingsModField = typeof(Dialog_ModSettings)
        .GetField("mod", InstanceNonPublic);
    private readonly GatewayEndToEndThingResolver thingResolver = new(
        new VerseGatewayEndToEndThingCandidateSource());

    public bool PlayerHasControl => Current.Game?.PlayerHasControl == true;

    public bool IsSoleSelectedPawn(string runtimeId)
    {
        var selected = Find.Selector?.SelectedObjectsListForReading.OfType<Pawn>().ToArray() ??
                       Array.Empty<Pawn>();
        return selected.Length == 1 && MatchesSelectedThing(selected[0], runtimeId) &&
               Find.Selector!.SelectedObjectsListForReading.Count == 1;
    }

    public bool IsSoleSelectedThing(string runtimeId)
    {
        var selected = Find.Selector?.SelectedObjectsListForReading ?? new List<object>();
        return selected.Count == 1 && selected[0] is Thing thing &&
               MatchesSelectedThing(thing, runtimeId);
    }

    public bool OpenPawnTab(EndToEndPawnInspectTab tab)
    {
        var type = TabType(tab);
        return InspectPaneUtility.OpenTab(type)?.GetType() == type;
    }

    public bool IsPawnTabOpen(EndToEndPawnInspectTab tab)
    {
        var pane = MainButtonDefOf.Inspect.TabWindow as IInspectPane;
        return pane?.OpenTabType == TabType(tab);
    }

    public bool OpenThingInfoCard(string runtimeId)
    {
        if (InfoCardThingField?.FieldType != typeof(Thing) ||
            thingResolver.ResolveExactThing(runtimeId) is not { } thing)
        {
            return false;
        }

        var boundThings = InfoCardBoundThings();
        if (!GatewayEndToEndThingReferencePolicy.CanOpenExactReference(
                thing,
                runtimeId,
                boundThings))
        {
            return false;
        }

        Find.WindowStack.Add(new Dialog_InfoCard(thing));
        return true;
    }

    public bool IsExactThingInfoCardOpen(string runtimeId) =>
        thingResolver.ResolveExactThing(runtimeId) is { } thing && ExactInfoCards(thing).Length == 1;

    public bool CloseExactThingInfoCard(string runtimeId)
    {
        if (thingResolver.ResolveExactThing(runtimeId) is not { } thing)
        {
            return false;
        }

        var matches = ExactInfoCards(thing);
        if (matches.Length != 1)
        {
            return false;
        }

        matches[0].Close(doCloseSound: true);
        return true;
    }

    public bool CloseInspectPane(string expectedTabRuntimeType)
    {
        if (MainButtonDefOf.Inspect.TabWindow is not MainTabWindow_Inspect pane ||
            !string.Equals(
                pane.OpenTabType?.FullName,
                expectedTabRuntimeType,
                StringComparison.Ordinal))
        {
            return false;
        }

        pane.CloseOpenTab();
        if (Find.WindowStack?.IsOpen(pane) == true)
        {
            MainButtonDefOf.Inspect.Worker.Activate();
        }
        return true;
    }

    public bool IsInspectPaneClosed
    {
        get
        {
            if (MainButtonDefOf.Inspect.TabWindow is not MainTabWindow_Inspect pane)
            {
                return false;
            }

            return pane.OpenTabType is null && Find.WindowStack?.IsOpen(pane) != true;
        }
    }

    public bool IsExactWindowOpen(string expectedWindowRuntimeType) =>
        ExactWindows(expectedWindowRuntimeType).Length == 1;

    public bool CancelExactWindow(string expectedWindowRuntimeType)
    {
        var matches = ExactWindows(expectedWindowRuntimeType);
        if (matches.Length != 1)
        {
            return false;
        }

        var currentEvent = Event.current;
        if (!GatewayEndToEndCancelEventPolicy.RequiresSyntheticKeyEvent(
                currentEvent?.type == EventType.KeyDown,
                currentEvent?.keyCode == KeyCode.Escape))
        {
            matches[0].OnCancelKeyPressed();
            return true;
        }

        var syntheticCancelEvent = new Event
        {
            type = EventType.KeyDown,
            keyCode = KeyCode.Escape
        };
        try
        {
            Event.current = syntheticCancelEvent;
            matches[0].OnCancelKeyPressed();
        }
        finally
        {
            Event.current = currentEvent;
        }
        return true;
    }

    public bool AcceptExactWindow(string expectedWindowRuntimeType)
    {
        var matches = ExactWindows(expectedWindowRuntimeType);
        if (matches.Length != 1)
        {
            return false;
        }

        var currentEvent = Event.current;
        if (currentEvent?.type == EventType.KeyDown &&
            (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter))
        {
            matches[0].OnAcceptKeyPressed();
            return true;
        }

        var syntheticAcceptEvent = new Event
        {
            type = EventType.KeyDown,
            keyCode = KeyCode.Return
        };
        InvokeWithTemporaryState(
            matches[0].OnAcceptKeyPressed,
            () => Event.current,
            value => Event.current = value,
            syntheticAcceptEvent);
        return true;
    }

    internal static void InvokeWithTemporaryState<T>(
        Action action,
        Func<T> readCurrent,
        Action<T> writeCurrent,
        T temporaryState)
    {
        var priorState = readCurrent();
        try
        {
            writeCurrent(temporaryState);
            action();
        }
        finally
        {
            writeCurrent(priorState);
        }
    }

    public bool OpenModSettings(string packageId)
    {
        if (ModSettingsModField?.FieldType != typeof(Mod))
        {
            return false;
        }

        var matches = ExactActiveMods(packageId);
        if (matches.Length != 1 || OpenModSettingsDialogs().Length != 0)
        {
            return false;
        }

        Find.WindowStack.Add(new Dialog_ModSettings(matches[0]));
        return true;
    }

    public bool IsExactModSettingsOpen(string packageId)
    {
        if (ModSettingsModField?.FieldType != typeof(Mod))
        {
            return false;
        }

        var mods = ExactActiveMods(packageId);
        var dialogs = OpenModSettingsDialogs();
        return mods.Length == 1 && dialogs.Length == 1 &&
               ReferenceEquals(ModSettingsModField.GetValue(dialogs[0]), mods[0]);
    }

    private static Type TabType(EndToEndPawnInspectTab tab) => tab switch
    {
        EndToEndPawnInspectTab.Gear => typeof(ITab_Pawn_Gear),
        EndToEndPawnInspectTab.Needs => typeof(ITab_Pawn_Needs),
        EndToEndPawnInspectTab.Health => typeof(ITab_Pawn_Health),
        _ => throw new ArgumentOutOfRangeException(nameof(tab))
    };

    private static Dialog_InfoCard[] ExactInfoCards(Thing exactThing)
    {
        if (InfoCardThingField?.FieldType != typeof(Thing))
        {
            return Array.Empty<Dialog_InfoCard>();
        }

        return (Find.WindowStack?.Windows.OfType<Dialog_InfoCard>() ??
                Enumerable.Empty<Dialog_InfoCard>())
            .Where(card => GatewayEndToEndThingReferencePolicy.IsExactReference(
                exactThing,
                InfoCardThingField.GetValue(card) as Thing))
            .ToArray();
    }

    private static Thing?[] InfoCardBoundThings() =>
        InfoCardThingField?.FieldType == typeof(Thing)
            ? (Find.WindowStack?.Windows.OfType<Dialog_InfoCard>() ??
               Enumerable.Empty<Dialog_InfoCard>())
                .Select(card => InfoCardThingField.GetValue(card) as Thing)
                .ToArray()
            : Array.Empty<Thing?>();

    private static Window[] ExactWindows(string expectedWindowRuntimeType) =>
        (Find.WindowStack?.Windows ?? new List<Window>())
        .Where(window => string.Equals(
            window.GetType().FullName,
            expectedWindowRuntimeType,
            StringComparison.Ordinal))
        .Take(2)
        .ToArray();

    private static Mod[] ExactActiveMods(string packageId) =>
        LoadedModManager.ModHandles
            .Where(mod => string.Equals(
                mod.Content?.PackageId,
                packageId,
                StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();

    private static Dialog_ModSettings[] OpenModSettingsDialogs() =>
        (Find.WindowStack?.Windows.OfType<Dialog_ModSettings>() ??
         Enumerable.Empty<Dialog_ModSettings>())
        .Take(2)
        .ToArray();

    private static bool MatchesSelectedThing(Thing thing, string runtimeId) =>
        string.Equals(thing.ThingID, runtimeId, StringComparison.Ordinal) ||
        string.Equals(thing.GetUniqueLoadID(), runtimeId, StringComparison.Ordinal);

}
