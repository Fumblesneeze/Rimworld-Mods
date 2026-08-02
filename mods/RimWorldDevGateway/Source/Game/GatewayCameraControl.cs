using RimWorldDevGateway.Contracts;
using UnityEngine;
using Verse;

namespace RimWorldDevGateway;

public sealed class GatewayCameraException : Exception
{
    public GatewayCameraException(string code, string message)
        : base(message)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    public string Code { get; }
}

public sealed class GatewayMapCell
{
    public GatewayMapCell(int x, int z)
    {
        X = x;
        Z = z;
    }

    public int X { get; }

    public int Z { get; }
}

public sealed class GatewayMapRect
{
    public GatewayMapRect(int minX, int minZ, int maxX, int maxZ)
    {
        MinX = minX;
        MinZ = minZ;
        MaxX = maxX;
        MaxZ = maxZ;
    }

    public int MinX { get; }

    public int MinZ { get; }

    public int MaxX { get; }

    public int MaxZ { get; }
}

public sealed class GatewayCameraSnapshot
{
    public GatewayCameraSnapshot(
        string mapHandle,
        GatewayMapCell center,
        float rootSize,
        string zoom,
        GatewayMapRect viewRect,
        int mapWidth,
        int mapHeight,
        float minimumRootSize,
        float maximumRootSize)
    {
        MapHandle = mapHandle ?? throw new ArgumentNullException(nameof(mapHandle));
        Center = center ?? throw new ArgumentNullException(nameof(center));
        RootSize = rootSize;
        Zoom = zoom ?? throw new ArgumentNullException(nameof(zoom));
        ViewRect = viewRect ?? throw new ArgumentNullException(nameof(viewRect));
        MapWidth = mapWidth;
        MapHeight = mapHeight;
        MinimumRootSize = minimumRootSize;
        MaximumRootSize = maximumRootSize;
    }

    public string MapHandle { get; }

    public GatewayMapCell Center { get; }

    public float RootSize { get; }

    public string Zoom { get; }

    public GatewayMapRect ViewRect { get; }

    public int MapWidth { get; }

    public int MapHeight { get; }

    public float MinimumRootSize { get; }

    public float MaximumRootSize { get; }
}

public sealed class GatewayCameraMutationResult
{
    public GatewayCameraMutationResult(GatewayCameraSnapshot before, GatewayCameraSnapshot after)
    {
        Before = before ?? throw new ArgumentNullException(nameof(before));
        After = after ?? throw new ArgumentNullException(nameof(after));
    }

    public GatewayCameraSnapshot Before { get; }

    public GatewayCameraSnapshot After { get; }
}

internal static class GatewayCameraGeometry
{
    public static GatewayMapRect CalculateViewRect(
        float cameraX,
        float cameraZ,
        float rootSize,
        int screenWidth,
        int screenHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            throw new GatewayCameraException(
                "camera_view_unavailable",
                "Camera view reporting requires a positive rendered screen size.");
        }

        var aspectRatio = (float)screenWidth / screenHeight;
        return new GatewayMapRect(
            Mathf.FloorToInt(cameraX - (rootSize * aspectRatio) - 1f),
            Mathf.FloorToInt(cameraZ - rootSize - 1f),
            Mathf.CeilToInt(cameraX + (rootSize * aspectRatio)),
            Mathf.CeilToInt(cameraZ + rootSize));
    }
}

public interface IGatewayCameraOperations
{
    GatewayCameraSnapshot Capture();

    void Set(GatewayMapCell? center, float? rootSize);
}

/// <summary>
/// Validates absolute camera changes before delegating to RimWorld's camera driver.
/// Callers must invoke this controller through the main-thread dispatcher.
/// </summary>
public sealed class GatewayCameraController
{
    private readonly IGatewayCameraOperations operations;

    public GatewayCameraController(IGatewayCameraOperations operations)
    {
        this.operations = operations ?? throw new ArgumentNullException(nameof(operations));
    }

    public GatewayCameraSnapshot Capture() => operations.Capture();

    public GatewayCameraMutationResult Mutate(GatewayCameraMutationRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Center is null && !request.RootSize.HasValue)
        {
            throw new GatewayCameraException(
                "empty_camera_mutation",
                "A camera center and/or rootSize is required.");
        }

        var before = operations.Capture();
        if (request.MapHandle is not null &&
            !string.Equals(request.MapHandle, before.MapHandle, StringComparison.Ordinal))
        {
            throw new GatewayCameraException(
                "stale_map_handle",
                $"Camera request map '{request.MapHandle}' is not the current map '{before.MapHandle}'.");
        }

        GatewayMapCell? center = null;
        if (request.Center is not null)
        {
            if (request.Center.X < 0 || request.Center.X >= before.MapWidth ||
                request.Center.Z < 0 || request.Center.Z >= before.MapHeight)
            {
                throw new GatewayCameraException(
                    "camera_center_out_of_bounds",
                    "Camera center must lie within the current map.");
            }

            center = new GatewayMapCell(request.Center.X, request.Center.Z);
        }

        if (request.RootSize.HasValue &&
            (float.IsNaN(request.RootSize.Value) ||
             float.IsInfinity(request.RootSize.Value) ||
             request.RootSize.Value < before.MinimumRootSize ||
             request.RootSize.Value > before.MaximumRootSize))
        {
            throw new GatewayCameraException(
                "camera_zoom_out_of_bounds",
                $"rootSize must be between {before.MinimumRootSize} and {before.MaximumRootSize}.");
        }

        // RimWorld's JumpToCurrentMapLoc queues a camera transition, so a capture in the same
        // dispatched operation can still report the old position. Supplying the existing root
        // size makes center-only mutations use SetRootPosAndSize and therefore take effect
        // synchronously without changing zoom.
        var rootSize = request.RootSize ?? (center is not null ? before.RootSize : null);
        operations.Set(center, rootSize);
        return new GatewayCameraMutationResult(before, operations.Capture());
    }
}

public sealed class VerseGatewayCameraOperations : IGatewayCameraOperations
{
    public GatewayCameraSnapshot Capture()
    {
        var map = Current.Game?.CurrentMap ?? throw new GatewayCameraException(
            "map_unavailable",
            "Camera control requires a current playable map.");
        var driver = Find.CameraDriver ?? throw new GatewayCameraException(
            "camera_unavailable",
            "RimWorld's map camera is unavailable.");
        var center = driver.MapPosition;
        var appliedPosition = driver.transform.position;
        // CameraDriver.CurrentViewRect is a static once-per-frame cache. A mutation captures
        // both Before and After in one dispatched frame, so asking that property twice returns
        // the pre-mutation rectangle. Recompute RimWorld's native projection from the applied
        // transform and root size so every snapshot is internally consistent.
        var view = GatewayCameraGeometry.CalculateViewRect(
            appliedPosition.x,
            appliedPosition.z,
            driver.RootSize,
            UI.screenWidth,
            UI.screenHeight);
        var range = driver.config.sizeRange;
        return new GatewayCameraSnapshot(
            "map-" + map.uniqueID,
            new GatewayMapCell(center.x, center.z),
            driver.RootSize,
            driver.CurrentZoom.ToString(),
            view,
            map.Size.x,
            map.Size.z,
            range.min,
            range.max);
    }

    public void Set(GatewayMapCell? center, float? rootSize)
    {
        var driver = Find.CameraDriver ?? throw new GatewayCameraException(
            "camera_unavailable",
            "RimWorld's map camera is unavailable.");
        if (center is not null && rootSize.HasValue)
        {
            driver.SetRootPosAndSize(new Vector3(center.X + 0.5f, 0f, center.Z + 0.5f), rootSize.Value);
        }
        else if (center is not null)
        {
            driver.JumpToCurrentMapLoc(new IntVec3(center.X, 0, center.Z));
        }
        else if (rootSize.HasValue)
        {
            driver.SetRootSize(rootSize.Value);
        }
    }
}
