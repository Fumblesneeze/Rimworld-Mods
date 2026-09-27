using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using Unity.Collections;
using Verse;
using Verse.AI;

namespace ThinWalls.Pathing;

public sealed class ThinWallMapComponent : MapComponent
{
    private static readonly ConditionalWeakTable<Map, ThinWallMapComponent> Components = new();
    private readonly Dictionary<SharedEdge, Building> edges = new();
    private readonly Dictionary<SharedEdge, Building_ThinDoor> doors = new();
    private readonly SparseEdgeRegionIndex affectedRegions = new();
    private readonly SparseEdgeMasks walls = new();
    private readonly SparseConnectivityOverlay overlay = new();
    // Only permission-specific Burst transport snapshots; disposed after native readers complete.
    private readonly List<NativeArray<CellConnection>> requestConnectivity = new();

    public ThinWallMapComponent(Map map) : base(map)
    {
        map.events.BuildingSpawned += NotifyBuildingChanged;
        map.events.BuildingDespawned += NotifyBuildingChanged;
        Components.Remove(map);
        Components.Add(map, this);
    }

    public static bool TryGet(Map map, out ThinWallMapComponent component) => Components.TryGetValue(map, out component);
    public bool HasCompletedWalls => walls.Cells.Count != 0;
    public bool HasCompletedEdgeStructures => edges.Count != 0;
    public int OwnedEdgeCount => edges.Count;
    public int DoorCount => doors.Count;
    public int MaskedCellCount => walls.Cells.Count;
    public int RemovedNativeCellCount => overlay.Removed.Count;
    public int RequestSnapshotCount => requestConnectivity.Count;
    public int AffectedRegionChunkCount => affectedRegions.Count;
    public bool AffectsRegionAt(IntVec3 cell) => affectedRegions.Contains(cell);
    public bool UseCellRegionLink(IntVec3 first, IntVec3 second) => affectedRegions.UseCellLink(first, second);
    public bool HasEdge(SharedEdge edge) => edges.ContainsKey(edge);
    public bool HasWall(SharedEdge edge) => edges.TryGetValue(edge, out var building) && building is Building_ThinWall;

    public override void MapComponentTick()
    {
        if (!HasCompletedWalls || (Find.TickManager.TicksGame % 120 != 7 && !DebugSettings.fastEcology)) return;
        // Core cannot sample a wall between adjacent air cells. Only visit our canonical edges;
        // native roofs, filled-cell walls, doors and temperature trackers keep their own work.
        foreach (var entry in edges)
            if (entry.Value is Building_ThinWall)
                Rooms.ThinWallHeatTransfer.Equalize(map, entry.Key);
    }

    public void NotifyCompletedEdgeChanged(OwnedEdge edge)
    {
        // Inspect only the two owners. Lifecycle callbacks never write native buffers.
        var owners = ThinWallUtility.ThingsOnSharedEdge(map, edge.Shared, completedOnly: true).OfType<Building>();
        Building? building = owners.FirstOrDefault(t => t is Building_ThinWall) ?? owners.FirstOrDefault();
        if (building == null) edges.Remove(edge.Shared);
        else edges[edge.Shared] = building;
        if (building is Building_ThinDoor door) doors[edge.Shared] = door;
        else doors.Remove(edge.Shared);
        if (affectedRegions.Set(edge.Shared, building != null))
            Rooms.ThinEdgeRegionUtility.NotifyChunkBoundaryChanged(map, edge);
        walls.Set(edge.Shared, building is Building_ThinWall);
        // Every removal cell needs a native delta: vanilla's incremental gather otherwise
        // leaves the endpoint-crossing diagonal bits of neighbor cells stale until some
        // unrelated dirty event recomputes them. Removals repeat cells; notify each once.
        foreach (IntVec3 removalCell in ThinWallConnectivity.Removals(edge.Shared)
                     .Select(removal => removal.Cell).Distinct())
            if (removalCell.InBounds(map)) map.pathFinder.MapData.Notify_CellDelta(removalCell);
        DirtyIncidentThingMeshes(edge);
    }

    public void NotifyPlannedEdgeChanged(OwnedEdge edge)
    {
        DirtyIncidentThingMeshes(edge);
    }

    // GatherData runs only after native jobs finish, including its same-tick early return.
    public void RestoreNativeConnectivity(NativeArray<CellConnection> native) =>
        overlay.Restore(i => native[i], (i, bits) => native[i] = bits);

    public void ApplyNativeConnectivity(NativeArray<CellConnection> native)
    {
        System.Func<int, CellConnection> read = i => native[i];
        System.Action<int, CellConnection> write = (i, bits) => native[i] = bits;
        foreach (var entry in walls.Cells)
            if (entry.Key.InBounds(map))
                overlay.Apply(map.cellIndices.CellToIndex(entry.Key), entry.Value, read, write);
    }

    public NativeArray<CellConnection>.ReadOnly ConnectivityFor(NativeArray<CellConnection> native, PathRequest request)
    {
        NativeArray<CellConnection> specific = default;
        if (request.TraverseParms.canBashDoors && overlay.Removed.Count != 0)
        {
            specific = new NativeArray<CellConnection>(native, Allocator.Persistent);
            requestConnectivity.Add(specific);
            foreach (var entry in overlay.Removed) specific[entry.Key] |= entry.Value;
        }
        if (!request.TraverseParms.canBashDoors)
        foreach (Building_ThinDoor door in doors.Values)
        {
            if (ThinDoorAccessPolicy.CanTraverse(door, request.TraverseParms)) continue;
            if (!specific.IsCreated)
            {
                specific = new NativeArray<CellConnection>(native, Allocator.Persistent);
                requestConnectivity.Add(specific);
            }
            foreach (ConnectionRemoval removal in ThinWallConnectivity.Removals(door.OwnedEdge.Shared))
                if (removal.Cell.InBounds(map))
                {
                    int index = map.cellIndices.CellToIndex(removal.Cell);
                    specific[index] &= ~removal.Connection;
                }
        }
        return specific.IsCreated ? specific.AsReadOnly() : native.AsReadOnly();
    }

    public void DisposeRequestConnectivity()
    {
        foreach (NativeArray<CellConnection> connectivity in requestConnectivity)
            if (connectivity.IsCreated) connectivity.Dispose();
        requestConnectivity.Clear();
    }

    public bool AllowsStep(IntVec3 from, IntVec3 to)
    {
        CellConnection connection = ConnectionFor(to.x - from.x, to.z - from.z);
        return connection != CellConnection.Self && from.InBounds(map) && to.InBounds(map) &&
               (walls.At(from) & connection) == CellConnection.Self;
    }

    public bool AllowsStep(IntVec3 from, IntVec3 to, TraverseParms parms)
    {
        if (ConnectionFor(to.x - from.x, to.z - from.z) == CellConnection.Self || !from.InBounds(map) || !to.InBounds(map)) return false;
        if (parms.canBashDoors) return true;
        if (!AllowsStep(from, to)) return false;
        foreach (SharedEdge edge in ThinWallUtility.SharedEdgesCrossed(from, to))
            if (edges.TryGetValue(edge, out var building) && building is Building_ThinDoor door && !ThinDoorAccessPolicy.CanTraverse(door, parms)) return false;
        return true;
    }

    public bool TryBridgeEdges(IntVec3 start, LocalTargetInfo destination, PathEndMode endMode, TraverseParms parms)
    {
        bool Native(IntVec3 from, LocalTargetInfo to, PathEndMode mode) =>
            ThinWallReachabilityPatch.NativeCanReach(map, from, to, mode, parms);
        if (!parms.canBashDoors && doors.Count == 0) return false;
        var crossings = new List<SharedEdge>();
        if (parms.canBashDoors) crossings.AddRange(edges.Keys);
        else foreach (var entry in doors)
            if (ThinDoorAccessPolicy.CanTraverse(entry.Value, parms)) crossings.Add(entry.Key);
        if (crossings.Count == 0) return false;
        // Re-run Core's destination admission, not its search. A bridge changes the native start,
        // so immediate and same-district shortcuts must not erase the original destination policy.
        if (parms.mode != TraverseMode.PassAllDestroyableThings &&
            parms.mode != TraverseMode.PassAllDestroyablePlayerOwnedThings &&
            parms.mode != TraverseMode.PassAllDestroyableThingsNotWater)
        {
            PathEndMode resolvedMode = endMode;
            LocalTargetInfo resolved = (LocalTargetInfo)GenPath.ResolvePathMode(parms.pawn, destination.ToTargetInfo(map), ref resolvedMode);
            if (resolvedMode == PathEndMode.OnCell)
            {
                if (resolved.Cell.GetRegion(map)?.Allows(parms, isDestination: true) != true) return false;
            }
            else if (resolvedMode == PathEndMode.Touch)
            {
                var destinations = SimplePool<List<Region>>.Get();
                try
                {
                    TouchPathEndModeUtility.AddAllowedAdjacentRegions(resolved, parms, map, destinations);
                    if (destinations.Count == 0) return false;
                }
                finally { destinations.Clear(); SimplePool<List<Region>>.Return(destinations); }
            }
            else return false;
        }
        bool Usable(IntVec3 cell)
        {
            if (!cell.InBounds(map) || !map.pathing.For(parms).pathGrid.Walkable(cell)) return false;
            if ((parms.mode == TraverseMode.NoPassClosedDoorsOrWater ||
                 parms.mode == TraverseMode.PassAllDestroyableThingsNotWater) && cell.GetTerrain(map).IsWater) return false;
            Region? region = cell.GetRegion(map);
            return region != null && region.Allows(parms, isDestination: false);
        }
        return ThinEdgeBridgeReachability.CanReach(start, crossings,
            (from, to) => ThinWallReachabilityPatch.NativeCanReach(map, from, to, PathEndMode.OnCell, parms, intermediateEndpoint: true),
            from => Native(from, destination, endMode), Usable);
    }

    public override void MapRemoved()
    {
        if (TryGet(map, out var current) && ReferenceEquals(current, this)) Components.Remove(map);
        map.events.BuildingSpawned -= NotifyBuildingChanged;
        map.events.BuildingDespawned -= NotifyBuildingChanged;
    }

    private void NotifyBuildingChanged(Building building)
    {
        Rendering.BuildingAppearanceControls.RefreshAutomaticOffset(building);
        if (building is Building_ThinWall || building.def.building?.isWall != true) return;
        foreach (IntVec3 cell in building.OccupiedRect().ExpandedBy(1).Cells)
            if (cell.InBounds(map)) map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Things);
    }

    private static CellConnection ConnectionFor(int x, int z)
    {
        if (x == -1 && z == -1) return CellConnection.SouthWest;
        if (x == 0 && z == -1) return CellConnection.South;
        if (x == 1 && z == -1) return CellConnection.SouthEast;
        if (x == -1 && z == 0) return CellConnection.West;
        if (x == 1 && z == 0) return CellConnection.East;
        if (x == -1 && z == 1) return CellConnection.NorthWest;
        if (x == 0 && z == 1) return CellConnection.North;
        if (x == 1 && z == 1) return CellConnection.NorthEast;
        return CellConnection.Self;
    }

    private void DirtyIncidentThingMeshes(Geometry.OwnedEdge edge)
    {
        foreach (IntVec3 cell in Rendering.ThinWallRenderGeometry.MeshDependencyCells(edge))
        {
            if (cell.InBounds(map))
            {
                map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Things);
            }
        }

        foreach (Thing thing in ThinWallUtility.Owners(edge.Shared)
                     .Where(owner => owner.Cell.InBounds(map))
                     .SelectMany(owner => owner.Cell.GetThingList(map))
                     .Distinct())
        {
            if (thing is Building building)
                Rendering.BuildingAppearanceControls.RefreshAutomaticOffset(building);
        }
    }
}
