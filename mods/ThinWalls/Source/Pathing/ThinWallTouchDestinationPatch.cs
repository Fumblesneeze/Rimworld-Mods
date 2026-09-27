using System;
using System.Collections.Generic;
using HarmonyLib;
using Unity.Collections;
using Verse;
using Verse.AI;

namespace ThinWalls.Pathing;

/// <summary>Public seam deciding which cells of a Touch destination rect may finalize a path.</summary>
public static class ThinWallTouchDestination
{
    private static readonly IntVec3[] EightWay =
    {
        new IntVec3(-1, 0, -1), new IntVec3(0, 0, -1), new IntVec3(1, 0, -1),
        new IntVec3(-1, 0, 0), new IntVec3(1, 0, 0),
        new IntVec3(-1, 0, 1), new IntVec3(0, 0, 1), new IntVec3(1, 0, 1),
    };

    /// <summary>Reports every cell that can only touch the target across a completed thin edge.</summary>
    public static void ExcludeBlockedTouchCells(CellRect touchRect, CellRect targetRect,
        Func<IntVec3, IntVec3, bool> stepBlocked, Action<IntVec3> exclude)
    {
        foreach (IntVec3 cell in touchRect.Cells)
        {
            if (targetRect.Contains(cell))
            {
                continue;
            }

            bool hasOpenAdjacency = false;
            for (int index = 0; index < EightWay.Length; index++)
            {
                IntVec3 neighbor = cell + EightWay[index];
                if (!targetRect.Contains(neighbor))
                {
                    continue;
                }

                if (!stepBlocked(cell, neighbor))
                {
                    hasOpenAdjacency = true;
                    break;
                }
            }

            if (!hasOpenAdjacency)
            {
                exclude(cell);
            }
        }
    }
}

/// <summary>Keeps the native Touch destination rect consistent with Thin Walls edge blocking:
/// cells only reachable as touch positions across a completed edge are excluded, so the
/// pathfinder routes around through the door instead of finalizing a trivial path the
/// immediate-touch contract then rejects.</summary>
[HarmonyPatch]
public static class ThinWallTouchDestinationPatch
{
    [HarmonyPatch(typeof(PathFinder), "MakeDestination")]
    [HarmonyPostfix]
    public static void MakeDestinationPostfix(LocalTargetInfo target, PathEndMode peMode,
        PathingContext context, CellRect rect, ref NativeList<int> excluded)
    {
        if (peMode != PathEndMode.Touch || context?.map == null || target.Cell.IsValid == false ||
            !Pathing.ThinWallMapComponent.TryGet(context.map, out var component) ||
            !component.HasCompletedEdgeStructures)
        {
            return;
        }

        Map map = context.map;
        CellIndices indices = map.cellIndices;
        CellRect targetRect = (target.HasThing ? target.Thing.OccupiedRect() : CellRect.SingleCell(target.Cell))
            .ClipInsideMap(map);
        CellRect touchRect = rect.ClipInsideMap(map);
        var pending = new List<IntVec3>();

        ThinWallTouchDestination.ExcludeBlockedTouchCells(touchRect, targetRect,
            (cell, neighbor) => ThinWallUtility.BlocksStep(map, cell, neighbor),
            cell => pending.Add(cell));

        foreach (IntVec3 cell in pending)
        {
            if (cell.InBounds(map))
            {
                int index = indices.CellToIndex(cell);
                if (!excluded.Contains(index))
                {
                    excluded.Add(index);
                }
            }
        }
    }
}
