using System;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace ThinWalls.Pathing;

[HarmonyPatch(typeof(Reachability), nameof(Reachability.CanReach),
    typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms))]
public static class ThinWallReachabilityPatch
{
    [ThreadStatic] private static int nativeLegDepth;
    [ThreadStatic] internal static Region? TransitDestination;

    public static bool NativeCanReach(Map map, IntVec3 start, LocalTargetInfo dest, PathEndMode mode, TraverseParms parms,
        bool intermediateEndpoint = false)
    {
        Region? previous = TransitDestination;
        TransitDestination = intermediateEndpoint ? dest.Cell.GetRegion(map) : null;
        nativeLegDepth++;
        try { return map.reachability.CanReach(start, dest, mode, parms); }
        finally { nativeLegDepth--; TransitDestination = previous; }
    }

    [HarmonyPostfix]
    public static void Postfix(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode,
        TraverseParms traverseParams, ref bool __result, Map ___map)
    {
        // Native positive answers retain all native RegionLinks, including nonlocal mod links.
        if (__result || nativeLegDepth != 0 || !dest.IsValid || !start.InBounds(___map) ||
            !dest.Cell.InBounds(___map) || (dest.HasThing && dest.Thing.Map != ___map)) return;
        if (ThinWallMapComponent.TryGet(___map, out var component) && component.HasCompletedEdgeStructures)
            __result = component.TryBridgeEdges(start, dest, peMode, traverseParams);
    }
}

[HarmonyPatch(typeof(Region), nameof(Region.Allows))]
public static class ThinEdgeTransitRegionPatch
{
    [HarmonyPrefix]
    public static void Prefix(Region __instance, ref bool isDestination)
    {
        // Only the artificial endpoint of a scoped connecting leg is transit. The player's real
        // destination was admitted separately; danger limits and every other TraverseParm stay intact.
        if (isDestination && ReferenceEquals(__instance, ThinWallReachabilityPatch.TransitDestination))
            isDestination = false;
    }
}

[HarmonyPatch(typeof(ReachabilityImmediate), nameof(ReachabilityImmediate.CanReachImmediate),
    typeof(IntVec3), typeof(LocalTargetInfo), typeof(Map), typeof(PathEndMode), typeof(Pawn))]
public static class ThinWallReachabilityImmediatePatch
{
    [HarmonyPostfix]
    public static void Postfix(
        IntVec3 start,
        LocalTargetInfo target,
        Map map,
        PathEndMode peMode,
        Pawn pawn,
        ref bool __result)
    {
        if (!__result || peMode != PathEndMode.Touch || !target.IsValid)
        {
            return;
        }

        if (!ThinWallMapComponent.TryGet(map, out ThinWallMapComponent component) ||
            !component.HasCompletedEdgeStructures)
        {
            return;
        }

        // The segment itself must remain reachable from either adjacent side for
        // melee bashing, repair, and deconstruction. Only targets beyond an edge
        // are protected by the Touch barrier below.
        if (target.HasThing && ThinWallUtility.IsThinEdgeDef(target.Thing.def))
        {
            return;
        }

        bool touchesTarget = false;
        bool hasOpenTouch = false;
        if (target.HasThing)
        {
            foreach (IntVec3 cell in target.Thing.OccupiedRect().Cells)
            {
                if (!start.AdjacentTo8WayOrInside(cell))
                {
                    continue;
                }

                touchesTarget = true;
                TraverseParms parms = pawn != null
                    ? TraverseParms.For(pawn)
                    : TraverseParms.For(TraverseMode.NoPassClosedDoors);
                if (cell == start || component.AllowsStep(start, cell, parms))
                {
                    hasOpenTouch = true;
                    break;
                }
            }
        }
        else if (start.AdjacentTo8WayOrInside(target.Cell))
        {
            touchesTarget = true;
            TraverseParms parms = pawn != null
                ? TraverseParms.For(pawn)
                : TraverseParms.For(TraverseMode.NoPassClosedDoors);
            hasOpenTouch = target.Cell == start ||
                           component.AllowsStep(start, target.Cell, parms);
        }

        if (touchesTarget && !hasOpenTouch)
        {
            __result = false;
        }
    }
}
