using System.Collections.Generic;
using System.Diagnostics;
using Verse;

namespace RimWorldDevGateway;

public interface IGatewaySnapshotSource
{
    string ProgramState { get; }

    string RootType { get; }

    long? Tick { get; }

    bool? Paused { get; }

    GatewayGameSpeed? Speed { get; }

    GatewayClientAreaSnapshot? ClientArea { get; }

    GatewayMapSnapshot? Map { get; }

    IReadOnlyList<GatewayWindowSnapshot> Windows { get; }

    IReadOnlyList<GatewaySelectionSnapshot> Selection { get; }
}

public sealed class GatewayClientAreaSnapshot
{
    public GatewayClientAreaSnapshot(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    public string CoordinateOrigin => "TopLeft";
}

public sealed class GatewayMapSnapshot
{
    public GatewayMapSnapshot(string handle, string biome, int width, int height)
    {
        Handle = handle ?? string.Empty;
        Biome = biome ?? string.Empty;
        Width = width;
        Height = height;
    }

    public string Handle { get; }

    public string Biome { get; }

    public int Width { get; }

    public int Height { get; }
}

public sealed class GatewayWindowSnapshot
{
    public GatewayWindowSnapshot(string handle, string type, bool modal)
    {
        Handle = handle ?? string.Empty;
        Type = type ?? string.Empty;
        Modal = modal;
    }

    public string Handle { get; }

    public string Type { get; }

    public bool Modal { get; }
}

public sealed class GatewaySelectionSnapshot
{
    public GatewaySelectionSnapshot(string handle, string label)
    {
        Handle = handle ?? string.Empty;
        Label = label ?? string.Empty;
    }

    public string Handle { get; }

    public string Label { get; }
}

public sealed class RimWorldGatewayStateProvider : IGatewayStateProvider
{
    private readonly IGatewaySnapshotSource source;
    private readonly GatewayDispatcher dispatcher;
    private readonly int processId;

    public RimWorldGatewayStateProvider(
        IGatewaySnapshotSource source,
        GatewayDispatcher dispatcher,
        int? processId = null)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        this.processId = processId ?? Process.GetCurrentProcess().Id;
    }

    public object CaptureStatus()
    {
        return new SortedDictionary<string, object?>
        {
            ["developerOnly"] = true,
            ["map"] = source.Map,
            ["pendingDispatches"] = dispatcher.PendingCount,
            ["processId"] = processId,
            ["programState"] = source.ProgramState,
            ["rootType"] = source.RootType,
            ["tick"] = source.Tick,
            ["unrestrictedExecutionEnabled"] = true,
            ["warning"] = RimWorldDevGatewayMod.DangerWarning
        };
    }

    public object CaptureUiState()
    {
        return new SortedDictionary<string, object?>
        {
            ["clientArea"] = source.ClientArea,
            ["paused"] = source.Paused,
            ["programState"] = source.ProgramState,
            ["rootType"] = source.RootType,
            ["selection"] = source.Selection,
            ["speed"] = source.Speed,
            ["windows"] = source.Windows
        };
    }
}

public sealed class VerseGatewaySnapshotSource : IGatewaySnapshotSource
{
    private const int MaximumWindows = 128;
    private const int MaximumSelection = 256;

    public string ProgramState => Current.ProgramState.ToString();

    public string RootType => Current.Root?.GetType().Name ?? string.Empty;

    public long? Tick => Current.Game?.tickManager?.TicksGame;

    public bool? Paused => Current.Game?.tickManager?.Paused;

    public GatewayGameSpeed? Speed
    {
        get
        {
            var tickManager = Current.Game?.tickManager;
            return tickManager is null
                ? null
                : FromVerseSpeed(tickManager.CurTimeSpeed);
        }
    }

    public GatewayClientAreaSnapshot? ClientArea
    {
        get
        {
            var width = UnityEngine.Screen.width;
            var height = UnityEngine.Screen.height;
            return width > 0 && height > 0
                ? new GatewayClientAreaSnapshot(width, height)
                : null;
        }
    }

    public GatewayMapSnapshot? Map
    {
        get
        {
            var map = Current.Game?.CurrentMap;
            if (map is null)
            {
                return null;
            }

            return new GatewayMapSnapshot(
                "map-" + map.uniqueID,
                map.Biome?.LabelCap.ToString() ?? string.Empty,
                map.Size.x,
                map.Size.z);
        }
    }

    public IReadOnlyList<GatewayWindowSnapshot> Windows
    {
        get
        {
            var snapshots = new List<GatewayWindowSnapshot>();
            var windows = Find.WindowStack?.Windows;
            if (windows is null)
            {
                return snapshots;
            }

            for (var index = 0; index < windows.Count && index < MaximumWindows; index++)
            {
                var window = windows[index];
                var type = window.GetType().Name;
                snapshots.Add(new GatewayWindowSnapshot(
                    "window-" + index + "-" + type,
                    type,
                    type.StartsWith("Dialog_", StringComparison.Ordinal)));
            }

            return snapshots;
        }
    }

    public IReadOnlyList<GatewaySelectionSnapshot> Selection
    {
        get
        {
            var snapshots = new List<GatewaySelectionSnapshot>();
            if (Current.Game is null)
            {
                return snapshots;
            }

            var selected = Find.Selector?.SelectedObjects;
            if (selected is null)
            {
                return snapshots;
            }

            for (var index = 0; index < selected.Count && index < MaximumSelection; index++)
            {
                var value = selected[index];
                if (value is Thing thing)
                {
                    snapshots.Add(new GatewaySelectionSnapshot(
                        thing.ThingID,
                        thing.LabelCap.ToString()));
                }
                else
                {
                    snapshots.Add(new GatewaySelectionSnapshot(
                        "selection-" + index + "-" + value.GetType().Name,
                        value.ToString() ?? value.GetType().Name));
                }
            }

            return snapshots;
        }
    }

    private static GatewayGameSpeed FromVerseSpeed(TimeSpeed speed) => speed switch
    {
        TimeSpeed.Paused => GatewayGameSpeed.Paused,
        TimeSpeed.Normal => GatewayGameSpeed.Normal,
        TimeSpeed.Fast => GatewayGameSpeed.Fast,
        TimeSpeed.Superfast => GatewayGameSpeed.Superfast,
        TimeSpeed.Ultrafast => GatewayGameSpeed.Ultrafast,
        _ => throw new ArgumentOutOfRangeException(nameof(speed))
    };
}
