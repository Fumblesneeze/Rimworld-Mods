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

public sealed class VerseGatewayEndToEndIsolationOperations : IGatewayEndToEndIsolationOperations
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
    {
        var map = RequireMap();
        var state = new BaselineState(
            "map-" + map.uniqueID,
            gameControl.Capture(),
            camera.Capture(),
            new HashSet<Window>(Find.WindowStack.Windows));
        activeBaseline = state;
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

        foreach (var phase in ResetPhaseOrder)
        {
            switch (phase)
            {
                case GatewayEndToEndMapResetPhase.Roofs:
                    RemoveRoofs(map);
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

    private static void RemoveDesignations(Map map)
    {
        foreach (var designation in map.designationManager.AllDesignations.ToArray())
        {
            designation.Delete();
        }
    }

    private static void RemoveZones(Map map)
    {
        foreach (var zone in map.zoneManager.AllZones.ToArray())
        {
            zone.Delete();
        }
    }

    private static void RemoveThings(Map map)
    {
        for (var pass = 0; pass < MaximumThingRemovalPasses; pass++)
        {
            var spawned = map.listerThings.AllThings
                .ToArray()
                .Where(thing => IsDisposable(thing) && thing.Spawned && !thing.Destroyed && thing.Map == map)
                .ToArray();
            if (spawned.Length == 0)
            {
                break;
            }

            foreach (var thing in spawned)
            {
                thing.Destroy(DestroyMode.Vanish);
            }
        }
    }

    public bool VerifyEmpty()
    {
        var state = activeBaseline;
        var map = Current.Game?.CurrentMap;
        if (state is null || map is null ||
            !string.Equals(state.MapHandle, "map-" + map.uniqueID, StringComparison.Ordinal))
        {
            return false;
        }

        return !map.listerThings.AllThings.Any(thing =>
                   IsDisposable(thing) && thing.Spawned && !thing.Destroyed && thing.Map == map) &&
               !map.AllCells.Any(cell => ShouldRemoveRoof(map.roofGrid.RoofAt(cell))) &&
               map.designationManager.AllDesignations.Count == 0 &&
               map.zoneManager.AllZones.Count == 0 &&
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
        camera.Mutate(new GatewayCameraMutationRequest
        {
            MapHandle = state.Camera.MapHandle,
            Center = new GatewayMapCellRequest
            {
                X = state.Camera.Center.X,
                Z = state.Camera.Center.Z
            },
            RootSize = state.Camera.RootSize
        });

        var restoredGame = gameControl.Capture();
        var restoredCamera = camera.Capture();
        return restoredGame.DevMode == state.Game.DevMode &&
               restoredGame.GodMode == state.Game.GodMode &&
               restoredGame.Speed == speed &&
               string.Equals(restoredCamera.MapHandle, state.Camera.MapHandle, StringComparison.Ordinal) &&
               restoredCamera.Center.X == state.Camera.Center.X &&
               restoredCamera.Center.Z == state.Camera.Center.Z &&
               Math.Abs(restoredCamera.RootSize - state.Camera.RootSize) < 0.001f;
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
    }
}
