using System;
using System.Collections.Generic;
using HarmonyLib;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using Verse;

namespace ThinWalls.Rooms;

public static class ThinEdgeRegionUtility
{
    private static readonly System.Reflection.MethodInfo DirtyRegionForThing =
        AccessTools.Method(typeof(RegionDirtyer), "DirtyRegionForThing");
    private static readonly System.Reflection.MethodInfo NotifyWalkabilityChanged =
        AccessTools.Method(typeof(RegionDirtyer), "Notify_WalkabilityChanged");

    public static void NotifyEdgeChanged(Building building)
    {
        if (building?.Map != null)
        {
            DirtyRegionForThing.Invoke(building.Map.regionDirtyer, new object[] { building });
            if (building is IThinEdgeStructure edgeStructure)
            {
                OwnedEdge edge = edgeStructure.OwnedEdge;
                NotifyWalkabilityChanged.Invoke(
                    building.Map.regionDirtyer,
                    new object[] { edge.Cell, true });
                if (edge.OppositeCell.InBounds(building.Map))
                {
                    NotifyWalkabilityChanged.Invoke(
                        building.Map.regionDirtyer,
                        new object[] { edge.OppositeCell, true });
                }
            }
        }
    }

    public static bool BlocksCardinalBoundary(Map map, IntVec3 first, IntVec3 second)
    {
        int dx = second.x - first.x;
        int dz = second.z - first.z;
        if (Math.Abs(dx) + Math.Abs(dz) != 1)
        {
            return false;
        }

        ThinWallSide side = dx switch
        {
            1 => ThinWallSide.East,
            -1 => ThinWallSide.West,
            _ => dz > 0 ? ThinWallSide.North : ThinWallSide.South,
        };
        return ThinWallUtility.HasEdgeStructure(map, new OwnedEdge(first, side).Shared);
    }
}

[HarmonyPatch(typeof(RegionMaker), nameof(RegionMaker.TryGenerateRegionFrom))]
public static class ThinEdgeRegionMakerPatch
{
    private static readonly HashSet<Thing> ProcessedThings = new();

    [HarmonyPrefix]
    public static bool Prefix(
        IntVec3 root,
        ref Region __result,
        ref bool ___working,
        Map ___map,
        ref Region ___newReg,
        List<IntVec3> ___newRegCells,
        ref RegionGrid ___regionGrid)
    {
        if (!___map.GetComponent<Pathing.ThinWallMapComponent>().HasCompletedEdgeStructures)
        {
            return true;
        }

        RegionType expected = root.GetExpectedRegionType(___map);
        if (expected == RegionType.None)
        {
            __result = null!;
            return false;
        }

        if (___working)
        {
            Log.Error("Trying to generate a new Thin Walls-aware region while region generation is already active.");
            __result = null!;
            return false;
        }

        ___working = true;
        try
        {
            ___regionGrid = ___map.regionGrid;
            ___newReg = Region.MakeNewUnfilled(root, ___map);
            ___newReg.type = expected;
            if (expected == RegionType.Portal)
            {
                ___newReg.door = root.GetDoor(___map);
            }

            FloodFillAndAddCells(root, ___map, ___regionGrid, ___newReg, ___newRegCells);
            CreateLinks(___map, ___regionGrid, ___newReg, ___newRegCells);
            RegisterThings(___map, ___regionGrid, ___newReg);
            __result = ___newReg;
            return false;
        }
        finally
        {
            ___working = false;
        }
    }

    private static void FloodFillAndAddCells(
        IntVec3 root,
        Map map,
        RegionGrid grid,
        Region region,
        List<IntVec3> cells)
    {
        cells.Clear();
        if (region.type.IsOneCellRegion())
        {
            AddCell(root, map, grid, region, cells);
            return;
        }

        var open = new Queue<IntVec3>();
        var visited = new HashSet<IntVec3> { root };
        open.Enqueue(root);
        while (open.Count != 0)
        {
            IntVec3 current = open.Dequeue();
            AddCell(current, map, grid, region, cells);
            for (int index = 0; index < GenAdj.CardinalDirections.Length; index++)
            {
                IntVec3 adjacent = current + GenAdj.CardinalDirections[index];
                if (!adjacent.InBounds(map) ||
                    !region.extentsLimit.Contains(adjacent) ||
                    adjacent.GetExpectedRegionType(map) != region.type ||
                    ThinEdgeRegionUtility.BlocksCardinalBoundary(map, current, adjacent) ||
                    !visited.Add(adjacent))
                {
                    continue;
                }

                open.Enqueue(adjacent);
            }
        }
    }

    private static void AddCell(
        IntVec3 cell,
        Map map,
        RegionGrid grid,
        Region region,
        List<IntVec3> cells)
    {
        grid.SetRegionAt(cell, region);
        cells.Add(cell);
        region.extentsClose.minX = Math.Min(region.extentsClose.minX, cell.x);
        region.extentsClose.maxX = Math.Max(region.extentsClose.maxX, cell.x);
        region.extentsClose.minZ = Math.Min(region.extentsClose.minZ, cell.z);
        region.extentsClose.maxZ = Math.Max(region.extentsClose.maxZ, cell.z);
        if (cell.x == 0 || cell.x == map.Size.x - 1 || cell.z == 0 || cell.z == map.Size.z - 1)
        {
            region.touchesMapEdge = true;
        }
    }

    private static void CreateLinks(Map map, RegionGrid grid, Region region, List<IntVec3> cells)
    {
        var processed = new[]
        {
            new HashSet<IntVec3>(),
            new HashSet<IntVec3>(),
            new HashSet<IntVec3>(),
            new HashSet<IntVec3>(),
        };
        foreach (IntVec3 cell in cells)
        {
            Sweep(Rot4.North, cell, map, grid, region, processed);
            Sweep(Rot4.South, cell, map, grid, region, processed);
            Sweep(Rot4.East, cell, map, grid, region, processed);
            Sweep(Rot4.West, cell, map, grid, region, processed);
        }
    }

    private static void Sweep(
        Rot4 direction,
        IntVec3 cell,
        Map map,
        RegionGrid grid,
        Region region,
        HashSet<IntVec3>[] processed)
    {
        HashSet<IntVec3> seen = processed[direction.AsInt];
        if (seen.Contains(cell))
        {
            return;
        }

        IntVec3 other = cell + direction.FacingCell;
        if ((other.InBounds(map) && grid.GetRegionAt_NoRebuild_InvalidAllowed(other) == region) ||
            ThinEdgeRegionUtility.BlocksCardinalBoundary(map, cell, other))
        {
            seen.Add(cell);
            return;
        }

        RegionType otherType = other.GetExpectedRegionType(map);
        if (otherType == RegionType.None)
        {
            seen.Add(cell);
            return;
        }

        Rot4 along = direction;
        along.Rotate(RotationDirection.Clockwise);
        int forward = 0;
        int backward = 0;
        seen.Add(cell);
        if (!otherType.IsOneCellRegion())
        {
            while (CanExtend(
                       cell + along.FacingCell * (forward + 1),
                       direction,
                       otherType,
                       map,
                       grid,
                       region,
                       seen))
            {
                IntVec3 candidate = cell + along.FacingCell * (forward + 1);
                if (!seen.Add(candidate))
                {
                    Log.Error("Thin Walls region link sweep processed the same cell twice.");
                }
                forward++;
            }

            while (CanExtend(
                       cell - along.FacingCell * (backward + 1),
                       direction,
                       otherType,
                       map,
                       grid,
                       region,
                       seen))
            {
                IntVec3 candidate = cell - along.FacingCell * (backward + 1);
                if (!seen.Add(candidate))
                {
                    Log.Error("Thin Walls region link sweep processed the same cell twice.");
                }
                backward++;
            }
        }

        int length = forward + backward + 1;
        SpanDirection spanDirection;
        IntVec3 spanRoot;
        if (direction == Rot4.North)
        {
            spanDirection = SpanDirection.East;
            spanRoot = cell - along.FacingCell * backward;
            spanRoot.z++;
        }
        else if (direction == Rot4.South)
        {
            spanDirection = SpanDirection.East;
            spanRoot = cell + along.FacingCell * forward;
        }
        else if (direction == Rot4.East)
        {
            spanDirection = SpanDirection.North;
            spanRoot = cell + along.FacingCell * forward;
            spanRoot.x++;
        }
        else
        {
            spanDirection = SpanDirection.North;
            spanRoot = cell - along.FacingCell * backward;
        }

        RegionLink link = map.regionLinkDatabase.LinkFrom(new EdgeSpan(spanRoot, spanDirection, length));
        link.Register(region);
        region.links.Add(link);
    }

    private static bool CanExtend(
        IntVec3 candidate,
        Rot4 direction,
        RegionType otherType,
        Map map,
        RegionGrid grid,
        Region region,
        HashSet<IntVec3> seen)
    {
        return !seen.Contains(candidate) &&
               candidate.InBounds(map) &&
               grid.GetRegionAt_NoRebuild_InvalidAllowed(candidate) == region &&
               (candidate + direction.FacingCell).GetExpectedRegionType(map) == otherType &&
               !ThinEdgeRegionUtility.BlocksCardinalBoundary(map, candidate, candidate + direction.FacingCell);
    }

    private static void RegisterThings(Map map, RegionGrid grid, Region region)
    {
        CellRect extents = region.extentsClose.ExpandedBy(1);
        extents.ClipInsideMap(map);
        ProcessedThings.Clear();
        foreach (IntVec3 item in extents)
        {
            bool adjacent = false;
            for (int index = 0; index < 9; index++)
            {
                IntVec3 candidate = item + GenAdj.AdjacentCellsAndInside[index];
                if (candidate.InBounds(map) && grid.GetValidRegionAt(candidate) == region)
                {
                    adjacent = true;
                    break;
                }
            }

            if (adjacent)
            {
                RegionListersUpdater.RegisterAllAt(item, map, ProcessedThings);
            }
        }
        ProcessedThings.Clear();
    }
}
