using RimWorldDevGateway.Contracts;
using Verse;

namespace RimWorldDevGateway;

internal enum GatewayEndToEndMapResetPhase
{
    Roofs,
    Designations,
    Zones,
    Things,
    Notifications
}

public sealed class VerseGatewayEndToEndIsolationOperations : IGatewayEndToEndIsolationOperations, IGatewayEndToEndGeneratedMapIsolationOperations, IGatewayEndToEndProcessOwnedGameIsolationOperations
{
    private const int MaximumThingRemovalPasses = 8;
    internal static readonly IReadOnlyList<GatewayEndToEndMapResetPhase> ResetPhaseOrder =
        new[]
        {
            GatewayEndToEndMapResetPhase.Roofs,
            GatewayEndToEndMapResetPhase.Designations,
            GatewayEndToEndMapResetPhase.Zones,
            GatewayEndToEndMapResetPhase.Things,
            GatewayEndToEndMapResetPhase.Notifications
        };
    private readonly GatewayGameControlController gameControl;
    private readonly GatewayCameraController camera;
    private readonly GatewayGizmoRegistry gizmos;
    private readonly GatewayEndToEndNotificationReset notifications =
        new(new VerseGatewayEndToEndNotificationOperations());
    private BaselineState? activeBaseline;

    public VerseGatewayEndToEndIsolationOperations(
        GatewayGameControlController gameControl,
        GatewayCameraController camera,
        GatewayGizmoRegistry gizmos)
    {
        this.gameControl = gameControl ?? throw new ArgumentNullException(nameof(gameControl));
        this.camera = camera ?? throw new ArgumentNullException(nameof(camera));
        this.gizmos = gizmos ?? throw new ArgumentNullException(nameof(gizmos));
    }

    public bool IsReady => Current.Game?.PlayerHasControl == true;

    public GatewayEndToEndIsolationBaseline CaptureBaseline()
        => CaptureBaseline(false);

    public GatewayEndToEndIsolationBaseline CaptureGeneratedMapBaseline()
        => CaptureBaseline(true);

    public GatewayEndToEndIsolationBaseline CaptureProcessOwnedGameBaseline()
        => CaptureBaseline(false, true);

    private GatewayEndToEndIsolationBaseline CaptureBaseline(bool preserveGeneratedMap, bool processOwnedGame = false)
    {
        var map = RequireMap();
        var state = new BaselineState(
            "map-" + map.uniqueID,
            gameControl.Capture(),
            camera.Capture(),
            new HashSet<Window>(Find.WindowStack.Windows));
        activeBaseline = state;
        state.ProcessOwnedGame = processOwnedGame;
        if (preserveGeneratedMap)
        {
            state.Environment = new HashSet<object>(AllMapThings(map).Cast<object>()
                .Concat(map.zoneManager.AllZones.Cast<object>())
                .Concat(map.designationManager.AllDesignations.Cast<object>()));
        }
        return new GatewayEndToEndIsolationBaseline(state);
    }

    public void Pause()
    {
        gameControl.Mutate(CreatePauseRequest());
    }

    internal static GatewayGameStateMutationRequest CreatePauseRequest() => new()
    {
        Paused = true
    };

    public void ResetTransientState()
    {
        var map = RequireMap();
        FloatMenuAutomationLease.Clear();
        CancelSemanticInteractions(gizmos);
        Find.DesignatorManager?.Deselect();
        Find.Selector.ClearSelection();
        RemoveTestWindows();

        if (activeBaseline?.ProcessOwnedGame == true)
        {
            notifications.Clear();
            return; // Exact-process shutdown releases this world; no later fixture can reuse it.
        }

        foreach (var phase in ResetPhaseOrder)
        {
            switch (phase)
            {
                case GatewayEndToEndMapResetPhase.Roofs:
                    if (activeBaseline?.Environment is null) RemoveRoofs(map);
                    break;
                case GatewayEndToEndMapResetPhase.Designations:
                    RemoveDesignations(map);
                    break;
                case GatewayEndToEndMapResetPhase.Zones:
                    RemoveZones(map);
                    break;
                case GatewayEndToEndMapResetPhase.Things:
                    RemoveThings(map);
                    break;
                case GatewayEndToEndMapResetPhase.Notifications:
                    notifications.Clear();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(phase));
            }
        }
    }

    private static void RemoveRoofs(Map map)
    {
        foreach (var cell in map.AllCells)
        {
            if (ShouldRemoveRoof(map.roofGrid.RoofAt(cell)))
            {
                map.roofGrid.SetRoof(cell, null);
            }
        }
    }

    private void RemoveDesignations(Map map)
    {
        foreach (var designation in map.designationManager.AllDesignations.ToArray())
        {
            if (!IsEnvironment(designation)) designation.Delete();
        }
    }

    private void RemoveZones(Map map)
    {
        foreach (var zone in map.zoneManager.AllZones.ToArray())
        {
            if (!IsEnvironment(zone)) zone.Delete();
        }
    }

    private bool IsEnvironment(object value) => activeBaseline?.Environment?.Contains(value) == true;

    private static List<Thing> AllMapThings(Map map)
    {
        var result = new List<Thing>();
        ThingOwnerUtility.GetAllThingsRecursively(map, ThingRequest.ForGroup(ThingRequestGroup.Everything), result);
        return result;
    }

    private IEnumerable<Thing> ResetCandidates(Map map) => activeBaseline?.Environment is null
        ? map.listerThings.AllThings.Where(thing => thing.Spawned && thing.Map == map)
        : AllMapThings(map);

    private void RemoveThings(Map map)
    {
        for (var pass = 0; pass < MaximumThingRemovalPasses; pass++)
        {
            var spawned = ResetCandidates(map)
                .ToArray()
                .Where(thing => !IsEnvironment(thing) && IsDisposable(thing) && !thing.Destroyed)
                .ToArray();
            if (spawned.Length == 0)
            {
                break;
            }

            if (activeBaseline?.Environment is { } environment)
                EnsureSafeRemoval(spawned.Cast<object>(), environment.OfType<Thing>().Where(thing => !thing.Destroyed),
                    value => value is Thing thing ? thing.ParentHolder : (value as IThingHolder)?.ParentHolder);
            foreach (var thing in spawned)
            {
                if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
            }
        }
    }

    internal static void EnsureSafeRemoval(IEnumerable<object> removal, IEnumerable<object> preserved,
        Func<object, object?> parent)
    {
        var targets = new HashSet<object>(removal);
        foreach (var value in preserved)
        {
            var seen = new HashSet<object>();
            for (var holder = parent(value); holder is not null && seen.Add(holder); holder = parent(holder))
                if (targets.Contains(holder))
                    throw new InvalidOperationException("Cleanup refused to destroy a new holder containing baseline objects; restore its contents first.");
        }
    }

    public bool VerifyEmpty()
    {
        var state = activeBaseline;
        var map = Current.Game?.CurrentMap;
        if (state is null || map is null ||
            !state.ProcessOwnedGame && !string.Equals(state.MapHandle, "map-" + map.uniqueID, StringComparison.Ordinal))
        {
            return false;
        }

        return (state.ProcessOwnedGame || (!ResetCandidates(map).Any(thing =>
                   !IsEnvironment(thing) && IsDisposable(thing) && !thing.Destroyed) &&
               (state.Environment is not null || !map.AllCells.Any(cell => ShouldRemoveRoof(map.roofGrid.RoofAt(cell)))) &&
               map.designationManager.AllDesignations.All(value => IsEnvironment(value)) &&
               map.zoneManager.AllZones.All(value => IsEnvironment(value)))) &&
               notifications.IsEmpty() &&
               Find.Selector.SelectedObjectsListForReading.Count == 0 &&
               FloatMenuAutomationLease.CapturedForAutomation is null &&
               SemanticInteractionsEmpty(gizmos) &&
               Find.DesignatorManager?.SelectedDesignator is null &&
               Find.WindowStack.Windows.All(state.Windows.Contains);
    }

    internal static bool IsDisposable(Thing thing) =>
        thing is not null && thing.def is not null && thing.def.destroyable;

    internal static bool ShouldRemoveRoof(RoofDef? roof) => roof is not null;

    internal static GatewayCameraMutationRequest CameraRestoreRequest(GatewayCameraSnapshot original,
        GatewayCameraSnapshot current, bool processOwnedGame) => new()
    {
        MapHandle = processOwnedGame ? current.MapHandle : original.MapHandle,
        Center = new GatewayMapCellRequest
        {
            X = processOwnedGame ? Math.Max(0, Math.Min(original.Center.X, current.MapWidth - 1)) : original.Center.X,
            Z = processOwnedGame ? Math.Max(0, Math.Min(original.Center.Z, current.MapHeight - 1)) : original.Center.Z
        },
        RootSize = processOwnedGame ? Math.Max(current.MinimumRootSize, Math.Min(original.RootSize, current.MaximumRootSize)) : original.RootSize
    };

    public bool RestoreBaseline(GatewayEndToEndIsolationBaseline baseline)
    {
        if (baseline?.State is not BaselineState state || !ReferenceEquals(state, activeBaseline))
        {
            return false;
        }

        var speed = state.Game.Speed ?? GatewayGameSpeed.Paused;
        gameControl.Mutate(new GatewayGameStateMutationRequest
        {
            DevMode = state.Game.DevMode,
            GodMode = state.Game.GodMode,
            Speed = speed.ToString()
        });
        var cameraRestore = CameraRestoreRequest(state.Camera, camera.Capture(), state.ProcessOwnedGame);
        camera.Mutate(cameraRestore);

        var restoredGame = gameControl.Capture();
        var restoredCamera = camera.Capture();
        return restoredGame.DevMode == state.Game.DevMode &&
               restoredGame.GodMode == state.Game.GodMode &&
               restoredGame.Speed == speed &&
               string.Equals(restoredCamera.MapHandle, cameraRestore.MapHandle, StringComparison.Ordinal) &&
               restoredCamera.Center.X == cameraRestore.Center!.X &&
               restoredCamera.Center.Z == cameraRestore.Center.Z &&
               Math.Abs(restoredCamera.RootSize - cameraRestore.RootSize!.Value) < 0.001f;
    }

    internal static void CancelSemanticInteractions(GatewayGizmoRegistry gizmos)
    {
        if (gizmos is null)
        {
            throw new ArgumentNullException(nameof(gizmos));
        }

        if (gizmos.HasActiveDesignatorPreview)
        {
            gizmos.CancelDesignatorPreview();
        }

        var interaction = gizmos.CurrentInteraction;
        if (interaction is not null)
        {
            gizmos.Cancel(interaction.Handle);
        }
    }

    internal static bool SemanticInteractionsEmpty(GatewayGizmoRegistry gizmos) =>
        gizmos is not null &&
        !gizmos.HasActiveDesignatorPreview &&
        gizmos.CurrentInteraction is null;

    private void RemoveTestWindows()
    {
        var baselineWindows = activeBaseline?.Windows ?? new HashSet<Window>();
        foreach (var window in Find.WindowStack.Windows.ToArray())
        {
            if (!baselineWindows.Contains(window))
            {
                Find.WindowStack.TryRemove(window, doCloseSound: false);
            }
        }
    }

    private static Map RequireMap() =>
        Current.Game?.CurrentMap ?? throw new InvalidOperationException(
            "E2E isolation requires a current playable map.");

    private sealed class BaselineState
    {
        public BaselineState(
            string mapHandle,
            GatewayGameStateSnapshot game,
            GatewayCameraSnapshot camera,
            HashSet<Window> windows)
        {
            MapHandle = mapHandle;
            Game = game;
            Camera = camera;
            Windows = windows;
        }

        public string MapHandle { get; }

        public GatewayGameStateSnapshot Game { get; }

        public GatewayCameraSnapshot Camera { get; }

        public HashSet<Window> Windows { get; }

        public HashSet<object>? Environment { get; set; }

        public bool ProcessOwnedGame { get; set; }
    }
}
