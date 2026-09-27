using HarmonyLib;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Verse;

namespace ThinWalls.Pathing;

[HarmonyPatch]
public static class PathFinderMapDataPatches
{
    private static readonly AccessTools.FieldRef<SimplePathFinderDataSource<CellConnection>, NativeArray<CellConnection>> NativeData =
        AccessTools.FieldRefAccess<SimplePathFinderDataSource<CellConnection>, NativeArray<CellConnection>>("data");

    [ThreadStatic]
    private static PathRequest? pendingRequest;

    [HarmonyPatch(typeof(PathFinder), "ParameterizePathJob")]
    [HarmonyPostfix]
    public static void PathFinderParameterizePostfix(PathRequest request)
    {
        pendingRequest = request;
    }

    [HarmonyPatch(typeof(PathFinderMapData), nameof(PathFinderMapData.GatherData))]
    [HarmonyPrefix]
    public static void GatherDataPrefix(ConnectivitySource ___connectivity, Map ___map)
    {
        if (ThinWallMapComponent.TryGet(___map, out var component))
            component.RestoreNativeConnectivity(NativeData(___connectivity));
    }

    [HarmonyPatch(typeof(PathFinderMapData), nameof(PathFinderMapData.GatherData))]
    [HarmonyPostfix]
    public static void GatherDataPostfix(ConnectivitySource ___connectivity, Map ___map)
    {
        if (ThinWallMapComponent.TryGet(___map, out ThinWallMapComponent component))
        {
            component.ApplyNativeConnectivity(NativeData(___connectivity));
        }
    }

    [HarmonyPatch(typeof(PathFinderMapData), nameof(PathFinderMapData.ParameterizePathJob))]
    [HarmonyPostfix]
    public static void ParameterizePathJobPostfix(ConnectivitySource ___connectivity, ref PathFinderJob job, Map ___map)
    {
        PathRequest? request = pendingRequest;
        pendingRequest = null;
        if (!ThinWallMapComponent.TryGet(___map, out ThinWallMapComponent component) ||
            !component.HasCompletedEdgeStructures || request == null)
        {
            return;
        }
        job.connectivity = component.ConnectivityFor(NativeData(___connectivity), request);
    }

    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.Dispose))]
    [HarmonyPostfix]
    public static void PathFinderDisposePostfix(Map ___map)
    {
        // Core calls MapRemoved (which unregisters the hot-path lookup) before PathFinder.Dispose.
        // The map's component list still exists here, after native scheduled readers completed.
        ___map.GetComponent<ThinWallMapComponent>()?.DisposeRequestConnectivity();
    }
}

[HarmonyPatch]
public static class ThinDoorPathFinderPatches
{
    [HarmonyPatch(typeof(PathFinder), "EnsureDoorsPawnsCached")]
    [HarmonyPostfix]
    public static void EnsureDoorsPawnsCachedPostfix(List<Thing> ___cachedDoors)
    {
        ___cachedDoors.RemoveAll(thing => ThinWallUtility.IsThinDoorDef(thing.def));
    }

    [HarmonyPatch(typeof(PathFinder), "ForceCompleteScheduledJobs")]
    [HarmonyPostfix]
    public static void ForceCompleteScheduledJobsPostfix(Map ___map)
    {
        if (ThinWallMapComponent.TryGet(___map, out ThinWallMapComponent component))
        {
            component.DisposeRequestConnectivity();
        }
    }
}
