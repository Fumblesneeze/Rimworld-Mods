using System.Collections.Generic;
using System.Linq;
using RimWorld;
using ThinWalls.Buildings;
using Unity.Collections;
using Verse;
using Verse.AI;

namespace ThinWalls.Pathing;

public sealed class ThinWallMapComponent : MapComponent
{
    private NativeArray<CellConnection> firstConnectivity;
    private NativeArray<CellConnection> secondConnectivity;
    private readonly List<NativeArray<CellConnection>> requestConnectivity = new();
    private readonly Dictionary<PathingContext, EdgeReachabilityGraph> reachabilityGraphs = new();
    private readonly Dictionary<DoorGraphKey, EdgeReachabilityGraph> doorReachabilityGraphs = new();
    private bool firstIsActive;
    private bool dirty = true;

    public ThinWallMapComponent(Map map) : base(map)
    {
        map.events.BuildingSpawned += NotifyBuildingChanged;
        map.events.BuildingDespawned += NotifyBuildingChanged;
        map.events.TerrainChanged += NotifyPathingChanged;
        map.events.PathCostRecalculate += NotifyPathingChanged;
        map.events.DoorOpened += NotifyDoorChanged;
        map.events.DoorClosed += NotifyDoorChanged;
    }

    public bool HasCompletedWalls => map.listerThings.ThingsOfDef(ThinWallDef).Count != 0;

    public bool HasCompletedEdgeStructures => HasCompletedWalls ||
        map.listerThings.ThingsOfDef(ThinDoorDef).Count != 0;

    public NativeArray<CellConnection> Connectivity => firstIsActive ? firstConnectivity : secondConnectivity;

    public int Version { get; private set; }

    public int ThinEdgeVisualRevision { get; private set; }

    public int DoorAccessSignature(TraverseParms traverseParms)
    {
        unchecked
        {
            int signature = 17;
            foreach (Building_ThinDoor door in map.listerThings.ThingsOfDef(ThinDoorDef)
                         .OfType<Building_ThinDoor>()
                         .OrderBy(candidate => candidate.thingIDNumber))
            {
                signature = (signature * 31) ^ door.thingIDNumber;
                signature = (signature * 31) ^
                            (ThinDoorAccessPolicy.CanTraverse(door, traverseParms) ? 1 : 0);
                signature = (signature * 31) ^ (door.FreePassage ? 1 : 0);
            }

            return signature;
        }
    }

    private static ThingDef ThinWallDef => DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName);

    private static ThingDef ThinDoorDef => DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinDoorDefName);

    public void NotifyCompletedEdgeChanged(Geometry.OwnedEdge edge)
    {
        dirty = true;
        Version++;
        IncrementThinEdgeVisualRevision();
        reachabilityGraphs.Clear();
        doorReachabilityGraphs.Clear();
        foreach (Geometry.OwnedEdge owner in ThinWallUtility.Owners(edge.Shared))
        {
            if (owner.Cell.InBounds(map))
            {
                map.pathFinder.MapData.Notify_CellDelta(owner.Cell);
            }
        }

        DirtyIncidentThingMeshes(edge);
    }

    public void NotifyPlannedEdgeChanged(Geometry.OwnedEdge edge)
    {
        IncrementThinEdgeVisualRevision();
        DirtyIncidentThingMeshes(edge);
    }

    private void IncrementThinEdgeVisualRevision()
    {
        ThinEdgeVisualRevision++;
    }

    public NativeArray<CellConnection>.ReadOnly ConnectivityFor(
        PathFinderMapData source,
        PathRequest request)
    {
        EnsureConnectivity(source, vanillaChanged: false);
        List<Building_ThinDoor> blockedDoors = map.listerThings.ThingsOfDef(ThinDoorDef)
            .OfType<Building_ThinDoor>()
            .Where(door => !ThinDoorAccessPolicy.CanTraverse(door, request.TraverseParms))
            .ToList();
        if (blockedDoors.Count == 0)
        {
            return Connectivity.AsReadOnly();
        }

        var specific = new NativeArray<CellConnection>(
            Connectivity,
            Allocator.Persistent);
        foreach (Building_ThinDoor door in blockedDoors)
        {
            RemoveEdge(specific, door.OwnedEdge.Shared);
        }

        requestConnectivity.Add(specific);
        return specific.AsReadOnly();
    }

    public void DisposeRequestConnectivity()
    {
        foreach (NativeArray<CellConnection> connectivity in requestConnectivity)
        {
            if (connectivity.IsCreated)
            {
                connectivity.Dispose();
            }
        }
        requestConnectivity.Clear();
    }

    public void EnsureConnectivity(PathFinderMapData source, bool vanillaChanged)
    {
        if (!dirty && !vanillaChanged && Connectivity.IsCreated)
        {
            return;
        }

        int count = map.cellIndices.NumGridCells;
        EnsureBuffers(count);
        NativeArray<CellConnection> rebuilt = firstIsActive ? secondConnectivity : firstConnectivity;
        for (int index = 0; index < count; index++)
        {
            rebuilt[index] = source.CellConnectionsAt(index);
        }

        foreach (Building_ThinWall wall in map.listerThings.ThingsOfDef(ThinWallDef).OfType<Building_ThinWall>())
        {
            foreach (ConnectionRemoval removal in ThinWallConnectivity.Removals(wall.OwnedEdge.Shared))
            {
                if (!removal.Cell.InBounds(map))
                {
                    continue;
                }

                int index = map.cellIndices.CellToIndex(removal.Cell);
                rebuilt[index] = (CellConnection)((byte)rebuilt[index] & ~(byte)removal.Connection);
            }
        }

        firstIsActive = rebuilt.Equals(firstConnectivity);
        dirty = false;
        Version++;
        reachabilityGraphs.Clear();
        doorReachabilityGraphs.Clear();
    }

    public bool AllowsStep(IntVec3 from, IntVec3 to)
    {
        int deltaX = to.x - from.x;
        int deltaZ = to.z - from.z;
        CellConnection connection = ConnectionFor(deltaX, deltaZ);
        if (connection == CellConnection.Self || !from.InBounds(map) || !to.InBounds(map))
        {
            return false;
        }

        if (Connectivity.IsCreated)
        {
            int index = map.cellIndices.CellToIndex(from);
            if (((byte)Connectivity[index] & (byte)connection) == 0)
            {
                return false;
            }
        }

        return !ThinWallUtility.BlocksStep(map, from, to);
    }

    public bool AllowsStep(IntVec3 from, IntVec3 to, TraverseParms traverseParms)
    {
        if (!traverseParms.canBashDoors && !AllowsStep(from, to))
        {
            return false;
        }

        foreach (Geometry.SharedEdge edge in ThinWallUtility.SharedEdgesCrossed(from, to))
        {
            foreach (Building_ThinDoor door in ThinWallUtility
                         .ThingsOnSharedEdge(map, edge, completedOnly: true)
                         .OfType<Building_ThinDoor>())
            {
                if (!traverseParms.canBashDoors && !ThinDoorAccessPolicy.CanTraverse(door, traverseParms))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool CanReachThroughEdges(
        IntVec3 start,
        LocalTargetInfo destination,
        PathEndMode endMode,
        TraverseParms traverseParms)
    {
        PathingContext context = map.pathing.For(traverseParms);
        bool hasThinDoors = map.listerThings.ThingsOfDef(ThinDoorDef).Count != 0;
        EdgeReachabilityGraph graph;
        if (hasThinDoors)
        {
            var doorGraphKey = new DoorGraphKey(
                context,
                DoorAccessSignature(traverseParms),
                traverseParms.canBashDoors);
            if (!doorReachabilityGraphs.TryGetValue(doorGraphKey, out graph))
            {
                graph = BuildReachabilityGraph(context, traverseParms);
                doorReachabilityGraphs.Add(doorGraphKey, graph);
            }
        }
        else if (!reachabilityGraphs.TryGetValue(context, out graph))
        {
            graph = BuildReachabilityGraph(context, traverseParms);
            reachabilityGraphs.Add(context, graph);
        }

        int startNode = graph.NodeAt(map, start);
        if (startNode == 0)
        {
            return false;
        }

        HashSet<int> destinationNodes = SimplePool<HashSet<int>>.Get();
        Queue<int> open = SimplePool<Queue<int>>.Get();
        HashSet<int> visited = SimplePool<HashSet<int>>.Get();
        try
        {
        CellRect targetRect = destination.HasThing
            ? destination.Thing.OccupiedRect()
            : new CellRect(destination.Cell.x, destination.Cell.z, 1, 1);
        foreach (IntVec3 candidate in targetRect.ExpandedBy(1).Cells)
        {
            int candidateNode = graph.NodeAt(map, candidate);
            if (candidateNode == 0)
            {
                continue;
            }

            if (ReachabilityImmediate.CanReachImmediate(
                    candidate,
                    destination,
                    map,
                    endMode,
                    traverseParms.pawn))
            {
                destinationNodes.Add(candidateNode);
            }
        }

            if (destinationNodes.Contains(startNode))
            {
                return true;
            }

            open.Enqueue(startNode);
            visited.Add(startNode);
            while (open.Count != 0)
            {
                int current = open.Dequeue();
                foreach (int adjacent in graph.Adjacent[current])
                {
                    if (!visited.Add(adjacent))
                    {
                        continue;
                    }

                    bool isDestination = destinationNodes.Contains(adjacent);
                    Region region = graph.Regions[adjacent];
                    if (region == null || !region.Allows(traverseParms, isDestination))
                    {
                        continue;
                    }

                    if (isDestination)
                    {
                        return true;
                    }

                    open.Enqueue(adjacent);
                }
            }

            return false;
        }
        finally
        {
            destinationNodes.Clear();
            open.Clear();
            visited.Clear();
            SimplePool<HashSet<int>>.Return(destinationNodes);
            SimplePool<Queue<int>>.Return(open);
            SimplePool<HashSet<int>>.Return(visited);
        }
    }

    public override void FinalizeInit()
    {
        dirty = true;
    }

    public override void MapRemoved()
    {
        map.events.BuildingSpawned -= NotifyBuildingChanged;
        map.events.BuildingDespawned -= NotifyBuildingChanged;
        map.events.TerrainChanged -= NotifyPathingChanged;
        map.events.PathCostRecalculate -= NotifyPathingChanged;
        map.events.DoorOpened -= NotifyDoorChanged;
        map.events.DoorClosed -= NotifyDoorChanged;
        reachabilityGraphs.Clear();
        doorReachabilityGraphs.Clear();
        ThinWallReachabilityPatch.NotifyMapRemoved(map);
    }

    public void DisposeConnectivity()
    {
        if (firstConnectivity.IsCreated)
        {
            firstConnectivity.Dispose();
        }

        if (secondConnectivity.IsCreated)
        {
            secondConnectivity.Dispose();
        }

        DisposeRequestConnectivity();
    }

    private void EnsureBuffers(int count)
    {
        if (firstConnectivity.IsCreated && firstConnectivity.Length == count &&
            secondConnectivity.IsCreated && secondConnectivity.Length == count)
        {
            return;
        }

        if (firstConnectivity.IsCreated)
        {
            firstConnectivity.Dispose();
        }

        if (secondConnectivity.IsCreated)
        {
            secondConnectivity.Dispose();
        }

        firstConnectivity = new NativeArray<CellConnection>(
            count,
            Allocator.Persistent,
            NativeArrayOptions.UninitializedMemory);
        secondConnectivity = new NativeArray<CellConnection>(
            count,
            Allocator.Persistent,
            NativeArrayOptions.UninitializedMemory);
        firstIsActive = false;
    }

    private void NotifyBuildingChanged(Building building)
    {
        Version++;
        reachabilityGraphs.Clear();
        doorReachabilityGraphs.Clear();
        if (building is Building_ThinWall || building.def.building?.isWall != true)
        {
            return;
        }

        foreach (IntVec3 cell in building.OccupiedRect().ExpandedBy(1).Cells)
        {
            if (cell.InBounds(map))
            {
                map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Things);
            }
        }
    }

    private void NotifyPathingChanged(IntVec3 _)
    {
        Version++;
        reachabilityGraphs.Clear();
        doorReachabilityGraphs.Clear();
    }

    private void NotifyDoorChanged(Building_Door _)
    {
        Version++;
        reachabilityGraphs.Clear();
        doorReachabilityGraphs.Clear();
    }

    private static CellConnection ConnectionFor(int deltaX, int deltaZ)
    {
        if (deltaX == -1 && deltaZ == -1) return CellConnection.SouthWest;
        if (deltaX == 0 && deltaZ == -1) return CellConnection.South;
        if (deltaX == 1 && deltaZ == -1) return CellConnection.SouthEast;
        if (deltaX == -1 && deltaZ == 0) return CellConnection.West;
        if (deltaX == 1 && deltaZ == 0) return CellConnection.East;
        if (deltaX == -1 && deltaZ == 1) return CellConnection.NorthWest;
        if (deltaX == 0 && deltaZ == 1) return CellConnection.North;
        if (deltaX == 1 && deltaZ == 1) return CellConnection.NorthEast;
        return CellConnection.Self;
    }

    private EdgeReachabilityGraph BuildReachabilityGraph(PathingContext context, TraverseParms traverseParms)
    {
        int cellCount = map.cellIndices.NumGridCells;
        var cellRegions = new Region[cellCount];
        for (int index = 0; index < cellCount; index++)
        {
            IntVec3 cell = map.cellIndices.IndexToCell(index);
            cellRegions[index] = cell.GetRegion(map, RegionType.Set_Passable);
        }

        int[] nodes = EdgeConnectivityLabeler.Build(
            map.cellIndices.SizeX,
            map.cellIndices.SizeZ,
            index => cellRegions[index] != null &&
                     context.pathGrid.WalkableFast(map.cellIndices.IndexToCell(index)),
            (from, to) => ReferenceEquals(cellRegions[from], cellRegions[to]) &&
                          AllowsStep(
                              map.cellIndices.IndexToCell(from),
                              map.cellIndices.IndexToCell(to),
                              traverseParms));
        int nodeCount = nodes.Length == 0 ? 0 : nodes.Max();
        var regions = new Region[nodeCount + 1];
        var adjacencyLists = new List<int>[nodeCount + 1];
        for (int node = 1; node <= nodeCount; node++)
        {
            adjacencyLists[node] = new List<int>();
        }

        for (int index = 0; index < nodes.Length; index++)
        {
            int node = nodes[index];
            if (node != 0 && regions[node] == null)
            {
                regions[node] = cellRegions[index];
            }
        }

        var edges = new HashSet<long>();
        for (int index = 0; index < nodes.Length; index++)
        {
            int fromNode = nodes[index];
            if (fromNode == 0)
            {
                continue;
            }

            IntVec3 fromCell = map.cellIndices.IndexToCell(index);
            for (int adjacentIndex = 0; adjacentIndex < GenAdj.AdjacentCells.Length; adjacentIndex++)
            {
                IntVec3 toCell = fromCell + GenAdj.AdjacentCells[adjacentIndex];
                if (!toCell.InBounds(map))
                {
                    continue;
                }

                int toNode = nodes[map.cellIndices.CellToIndex(toCell)];
                if (toNode == 0 || toNode == fromNode || !AllowsStep(fromCell, toCell, traverseParms))
                {
                    continue;
                }

                int lower = fromNode < toNode ? fromNode : toNode;
                int upper = fromNode < toNode ? toNode : fromNode;
                long edge = ((long)lower << 32) | (uint)upper;
                if (!edges.Add(edge))
                {
                    continue;
                }

                adjacencyLists[lower].Add(upper);
                adjacencyLists[upper].Add(lower);
            }
        }

        var adjacent = new int[nodeCount + 1][];
        adjacent[0] = System.Array.Empty<int>();
        for (int node = 1; node <= nodeCount; node++)
        {
            adjacent[node] = adjacencyLists[node].ToArray();
        }

        return new EdgeReachabilityGraph(nodes, regions, adjacent);
    }

    private void RemoveEdge(NativeArray<CellConnection> connectivity, Geometry.SharedEdge edge)
    {
        foreach (ConnectionRemoval removal in ThinWallConnectivity.Removals(edge))
        {
            if (!removal.Cell.InBounds(map))
            {
                continue;
            }

            int index = map.cellIndices.CellToIndex(removal.Cell);
            connectivity[index] = (CellConnection)((byte)connectivity[index] & ~(byte)removal.Connection);
        }
    }

    private sealed class EdgeReachabilityGraph
    {
        public EdgeReachabilityGraph(int[] nodes, Region[] regions, int[][] adjacent)
        {
            Nodes = nodes;
            Regions = regions;
            Adjacent = adjacent;
        }

        public int[] Nodes { get; }

        public Region[] Regions { get; }

        public int[][] Adjacent { get; }

        public int NodeAt(Map map, IntVec3 cell)
        {
            return cell.InBounds(map) ? Nodes[map.cellIndices.CellToIndex(cell)] : 0;
        }
    }

    private readonly struct DoorGraphKey : System.IEquatable<DoorGraphKey>
    {
        public DoorGraphKey(PathingContext context, int doorAccessSignature, bool canBashDoors)
        {
            Context = context;
            DoorAccessSignature = doorAccessSignature;
            CanBashDoors = canBashDoors;
        }

        private PathingContext Context { get; }

        private int DoorAccessSignature { get; }

        private bool CanBashDoors { get; }

        public bool Equals(DoorGraphKey other) =>
            Equals(Context, other.Context) &&
            DoorAccessSignature == other.DoorAccessSignature &&
            CanBashDoors == other.CanBashDoors;

        public override bool Equals(object? obj) => obj is DoorGraphKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Context?.GetHashCode() ?? 0;
                hash = (hash * 397) ^ DoorAccessSignature;
                return (hash * 397) ^ CanBashDoors.GetHashCode();
            }
        }
    }

    private void DirtyIncidentThingMeshes(Geometry.OwnedEdge edge)
    {
        foreach (IntVec3 cell in Rendering.ThinWallRenderGeometry.IncidentCells(edge))
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
            if (Rendering.AdjacentBuildingVisualOffset.IsEligible(thing, out _))
            {
                map.mapDrawer.MapMeshDirty(thing.Position, MapMeshFlagDefOf.Things);
            }
        }
    }
}
