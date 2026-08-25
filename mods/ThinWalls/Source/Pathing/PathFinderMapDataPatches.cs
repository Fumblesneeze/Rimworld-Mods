using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;

namespace ThinWalls.Pathing;

[HarmonyPatch]
public static class PathFinderMapDataPatches
{
    [ThreadStatic]
    private static bool pendingThinWallBashTraversal;

    [ThreadStatic]
    private static PathRequest? pendingRequest;

    [HarmonyPatch(typeof(PathFinder), "ParameterizePathJob")]
    [HarmonyPostfix]
    public static void PathFinderParameterizePostfix(PathRequest request)
    {
        pendingThinWallBashTraversal = MayBashThinWalls(request.TraverseParms);
        pendingRequest = request;
    }

    [HarmonyPatch(typeof(PathFinderMapData), nameof(PathFinderMapData.GatherData))]
    [HarmonyPostfix]
    public static void GatherDataPostfix(PathFinderMapData __instance, bool __result, Map ___map)
    {
        ___map.GetComponent<ThinWallMapComponent>().EnsureConnectivity(__instance, __result);
    }

    [HarmonyPatch(typeof(PathFinderMapData), nameof(PathFinderMapData.ParameterizePathJob))]
    [HarmonyPostfix]
    public static void ParameterizePathJobPostfix(PathFinderMapData __instance, ref PathFinderJob job, Map ___map)
    {
        bool thinWallBashTraversal = pendingThinWallBashTraversal;
        PathRequest? request = pendingRequest;
        pendingThinWallBashTraversal = false;
        pendingRequest = null;
        if (thinWallBashTraversal)
        {
            return;
        }

        ThinWallMapComponent component = ___map.GetComponent<ThinWallMapComponent>();
        if (!component.Connectivity.IsCreated)
        {
            component.EnsureConnectivity(__instance, vanillaChanged: true);
        }
        job.connectivity = request == null
            ? component.Connectivity.AsReadOnly()
            : component.ConnectivityFor(__instance, request);
    }

    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.Dispose))]
    [HarmonyPostfix]
    public static void PathFinderDisposePostfix(Map ___map)
    {
        ___map.GetComponent<ThinWallMapComponent>()?.DisposeConnectivity();
    }

    private static bool MayBashThinWalls(TraverseParms traverseParms)
    {
        return traverseParms.canBashDoors;
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
        ___map.GetComponent<ThinWallMapComponent>()?.DisposeRequestConnectivity();
    }
}
