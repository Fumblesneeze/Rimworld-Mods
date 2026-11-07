using System;
using System.Collections.Generic;
using System.Linq;

namespace PathfindingLab.Core;

public readonly record struct MoveCosts(int Cardinal, int Diagonal);
public sealed class RoutePlan
{
    internal readonly List<int> Waypoints;
    internal readonly List<int> Buffer = new();
    internal readonly MoveCosts Movement;
    internal readonly object Owner;
    internal readonly (int Chunk, int Version)[] Dependencies;
    internal int NextWaypoint;
    internal RoutePlan(List<int> waypoints, int start, MoveCosts movement, object owner, (int, int)[] dependencies)
    {
        Waypoints = waypoints;
        Buffer.Add(start);
        Movement = movement;
        Owner = owner;
        Dependencies = dependencies;
    }
}

public sealed class PortalRouter
{
    public const int ChunkSize = 32;
    private readonly int width, height, chunksX;
    private readonly int[] costs, components;
    private readonly int[] chunkVersions;
    private readonly bool[] cornerBlocks;
    private GeometrySnapshot? geometry;
    private PortalGraph? graph, published;
    private readonly object identity = new();
    private readonly HashSet<int> publicationDirty = new();
    internal object Identity => graph?.Identity ?? identity;
    public PortalGraph? Graph => graph;
    private readonly List<int>[] portals;
    private readonly Dictionary<int, List<int>> crossings = new();
    private readonly HashSet<int> dirty = new();
    private readonly HashSet<int> costDirty = new();
    private readonly record struct CacheKey(int Start, int Target, MoveCosts Movement);
    private readonly Dictionary<CacheKey, (List<int> Path, LinkedListNode<CacheKey> Node, int Version)> cache = new();
    private readonly LinkedList<CacheKey> recency = new();
    private const int CacheCapacity = 512;
    private int generation;
    private readonly int[] flood = new int[ChunkSize * ChunkSize];
    public int Generation => graph?.Generation ?? generation;
    public long LocalSearches { get; private set; }
    public long AbstractSearches { get; private set; }
    public long CacheHits { get; private set; }
    public int LastBoundaryPairsRebuilt { get; private set; }
    public int CachedPaths => cache.Count;
    public void UpdateCell(int cell, int cost, bool cornerBlock)
    {
        if (graph != null || geometry != null) throw new InvalidOperationException("Published readers and snapshot builders do not accept mutable cell writes.");
        if (cell < 0 || cell >= costs.Length) throw new ArgumentOutOfRangeException(nameof(cell));
        if (costs[cell] == cost && cornerBlocks[cell] == cornerBlock) return;
        bool topology = Walkable(cell) != (cost >= 0 && cost < 10000) || cornerBlocks[cell] != cornerBlock;
        costs[cell] = cost;
        cornerBlocks[cell] = cornerBlock;
        MarkChanged(cell, topology);
    }

    private void MarkChanged(int cell, bool topology)
    {
        costDirty.Add(Chunk(cell));
        if (!topology) return;
        int x = cell % width, z = cell / width;
        for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
                if (x + dx >= 0 && x + dx < width && z + dz >= 0 && z + dz < height)
                {
                    int chunk = Chunk(cell + dx + dz * width);
                    dirty.Add(chunk);
                    costDirty.Add(chunk);
                }
    }

    public PortalRouter(GeometrySnapshot snapshot) : this(snapshot.Width, snapshot.Height)
    { geometry = snapshot; }

    public PortalRouter(PortalGraph snapshot) : this(snapshot.Width, snapshot.Height, false)
    { graph = snapshot; }

    private PortalRouter(int width, int height, bool builder = true)
    {
        this.width = width; this.height = height;
        costs = Array.Empty<int>(); cornerBlocks = Array.Empty<bool>();
        chunksX = (width + 31) / 32;
        int count = chunksX * ((height + 31) / 32);
        components = builder ? new int[checked(width * height)] : Array.Empty<int>();
        portals = builder ? new List<int>[count] : Array.Empty<List<int>>();
        chunkVersions = builder ? new int[count] : Array.Empty<int>();
        if (builder) for (int i = 0; i < count; i++) { portals[i] = new List<int>(); dirty.Add(i); }
    }

    public void ApplySnapshot(GeometrySnapshot snapshot)
    {
        if (graph != null || geometry == null) throw new InvalidOperationException("Only a snapshot builder can apply geometry.");
        if (!ReferenceEquals(geometry.Identity, snapshot.Identity)) throw new ArgumentException("Geometry belongs to another map.");
        if (snapshot.Width != width || snapshot.Height != height) throw new ArgumentException("Map dimensions changed.");
        if (snapshot.Version <= geometry.Version) return;
        for (int chunk = 0; chunk < portals.Length; chunk++)
        {
            if (geometry.SameChunk(snapshot, chunk)) continue;
            foreach (int cell in Cells(chunk))
            {
                int before = geometry.Cost(cell), after = snapshot.Cost(cell);
                bool corner = geometry.Corner(cell) != snapshot.Corner(cell);
                if (before != after || corner)
                    MarkChanged(cell, (before >= 0 && before < 10000) != (after >= 0 && after < 10000) || corner);
            }
        }
        geometry = snapshot;
    }

    public PortalGraph Publish()
    {
        if (graph != null || geometry == null) throw new InvalidOperationException("Only a snapshot builder publishes graphs.");
        EnsureGraph();
        if (published?.Version == geometry.Version) return published;
        var chunks = published?.CopyChunkTable() ?? new PortalGraph.Chunk[portals.Length];
        foreach (int chunk in publicationDirty)
        {
            int[] members = new int[1024];
            foreach (int cell in Cells(chunk)) members[cell % width % 32 + cell / width % 32 * 32] = components[cell];
            int[] nodes = portals[chunk].ToArray();
            var links = new int[nodes.Length][];
            for (int i = 0; i < nodes.Length; i++) links[i] = crossings[nodes[i]].ToArray();
            chunks[chunk] = new PortalGraph.Chunk(members, nodes, links);
        }
        published = new PortalGraph(geometry, identity, chunks, (int[])chunkVersions.Clone(), generation, publicationDirty.Count);
        publicationDirty.Clear();
        return published;
    }

    internal void Bind(PortalGraph snapshot)
    {
        if (graph == null || snapshot.Width != width || snapshot.Height != height)
            throw new InvalidOperationException("Reader dimensions changed.");
        if (!ReferenceEquals(graph.Identity, snapshot.Identity)) { cache.Clear(); recency.Clear(); }
        graph = snapshot;
    }

    public PortalRouter(int width, int height, int[] costs, bool[] cornerBlocks)
    {
        if (width <= 0 || height <= 0 || costs.Length != checked(width * height) || cornerBlocks.Length != costs.Length)
            throw new ArgumentException("Invalid grid dimensions.");
        this.width = width;
        this.height = height;
        this.costs = (int[])costs.Clone();
        this.cornerBlocks = (bool[])cornerBlocks.Clone();
        components = new int[costs.Length];
        chunksX = (width + ChunkSize - 1) / ChunkSize;
        portals = new List<int>[chunksX * ((height + ChunkSize - 1) / ChunkSize)];
        chunkVersions = new int[portals.Length];
        for (int i = 0; i < portals.Length; i++) { portals[i] = new List<int>(); dirty.Add(i); }
    }

    private int Cost(int cell) => graph?.Geometry.Cost(cell) ?? geometry?.Cost(cell) ?? costs[cell];
    private bool Corner(int cell) => graph?.Geometry.Corner(cell) ?? geometry?.Corner(cell) ?? cornerBlocks[cell];
    private int Component(int cell) => graph?.Component(cell) ?? components[cell];
    private int Version(int chunk) => graph?.ChunkVersion(chunk) ?? chunkVersions[chunk];
    private bool Walkable(int cell) => cell >= 0 && cell < width * height && Cost(cell) >= 0 && Cost(cell) < 10000;
    private int Chunk(int cell) => cell % width / ChunkSize + cell / width / ChunkSize * chunksX;
    private long Distance(int a, int b, MoveCosts movement)
    {
        int dx = Math.Abs(a % width - b % width), dz = Math.Abs(a / width - b / width);
        return (long)Math.Min(dx, dz) * movement.Diagonal + (long)Math.Abs(dx - dz) * movement.Cardinal;
    }

    public bool CanStep(int from, int to)
    {
        if (!Walkable(from) || !Walkable(to)) return false;
        int dx = to % width - from % width, dz = to / width - from / width;
        if (Math.Abs(dx) > 1 || Math.Abs(dz) > 1 || (dx == 0 && dz == 0)) return false;
        if (dx == 0 || dz == 0) return true;
        int sideX = from + dx, sideZ = from + dz * width;
        return Walkable(sideX) && Walkable(sideZ) && !Corner(sideX) && !Corner(sideZ);
    }

    private IEnumerable<int> Adjacent(int cell)
    {
        int x = cell % width, z = cell / width;
        for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
                if ((dx != 0 || dz != 0) && x + dx >= 0 && x + dx < width && z + dz >= 0 && z + dz < height)
                {
                    int next = cell + dx + dz * width;
                    if (CanStep(cell, next)) yield return next;
                }
    }

    private IEnumerable<int> Cells(int chunk)
    {
        int x0 = chunk % chunksX * ChunkSize, z0 = chunk / chunksX * ChunkSize;
        for (int z = z0; z < Math.Min(z0 + ChunkSize, height); z++)
            for (int x = x0; x < Math.Min(x0 + ChunkSize, width); x++) yield return x + z * width;
    }

    private int Adjacent(int cell, Span<int> destination)
    {
        int x = cell % width, z = cell / width, count = 0;
        for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
                if ((dx != 0 || dz != 0) && x + dx >= 0 && x + dx < width && z + dz >= 0 && z + dz < height)
                {
                    int next = cell + dx + dz * width;
                    if (CanStep(cell, next)) destination[count++] = next;
                }
        return count;
    }

    public void Prepare() => EnsureGraph();
    private void EnsureGraph()
    {
        if (graph != null) return;
        if (dirty.Count == 0 && costDirty.Count == 0) return;
        LastBoundaryPairsRebuilt = 0;
        foreach (int chunk in dirty) costDirty.Add(chunk);
        foreach (int chunk in costDirty) chunkVersions[chunk]++;
        foreach (var key in cache.Keys.Where(key => costDirty.Contains(Chunk(key.Start))).ToArray())
        {
            recency.Remove(cache[key].Node);
            cache.Remove(key);
        }
        // One small stack buffer for this invocation, not one allocation per expanded cell.
        Span<int> neighbors = stackalloc int[8];
        foreach (int chunk in dirty)
        {
            publicationDirty.Add(chunk);
            foreach (int cell in Cells(chunk)) components[cell] = -1;
            foreach (int cell in Cells(chunk))
            {
                if (!Walkable(cell) || components[cell] >= 0) continue;
                components[cell] = cell;
                int head = 0, tail = 1;
                flood[0] = cell;
                while (head < tail)
                {
                    int count = Adjacent(flood[head++], neighbors);
                    foreach (int next in neighbors.Slice(0, count))
                        if (Chunk(next) == chunk && components[next] < 0)
                        {
                            components[next] = cell;
                            flood[tail++] = next;
                        }
                }
            }
        }
        var boundaries = new HashSet<(int Chunk, bool Vertical)>();
        foreach (int chunk in dirty)
        {
            int x0 = chunk % chunksX * ChunkSize, z0 = chunk / chunksX * ChunkSize;
            if (x0 + ChunkSize < width) boundaries.Add((chunk, true));
            if (z0 + ChunkSize < height) boundaries.Add((chunk, false));
            if (x0 > 0) boundaries.Add((chunk - 1, true));
            if (z0 > 0) boundaries.Add((chunk - chunksX, false));
        }
        // Preserve unrelated boundary nodes and their local caches on an interior edit.
        foreach (var boundary in boundaries.OrderBy(value => value.Chunk).ThenBy(value => value.Vertical))
        {
            publicationDirty.Add(boundary.Chunk);
            publicationDirty.Add(boundary.Chunk + (boundary.Vertical ? 1 : chunksX));
            int x0 = boundary.Chunk % chunksX * ChunkSize, z0 = boundary.Chunk / chunksX * ChunkSize;
            int offset = boundary.Vertical ? 1 : width;
            var cells = (boundary.Vertical
                ? Enumerable.Range(z0, Math.Min(ChunkSize, height - z0)).Select(z => z * width + x0 + ChunkSize - 1)
                : Enumerable.Range(x0, Math.Min(ChunkSize, width - x0)).Select(x => (z0 + ChunkSize - 1) * width + x)).ToArray();
            foreach (int cell in cells) { Unlink(cell, cell + offset); Unlink(cell + offset, cell); }
            AddBoundary(cells, offset);
        }
        dirty.Clear();
        costDirty.Clear();
        generation++;
    }

    private void AddBoundary(IEnumerable<int> cells, int offset)
    {
        var run = new List<int>();
        void Flush()
        {
            if (run.Count == 0) return;
            foreach (int cell in new[] { run[0], run[run.Count / 2], run[^1] }.Distinct())
            {
                Link(cell, cell + offset);
                Link(cell + offset, cell);
            }
            run.Clear();
        }
        foreach (int cell in cells)
        {
            LastBoundaryPairsRebuilt++;
            if (!CanStep(cell, cell + offset)) { Flush(); continue; }
            if (run.Count > 0 && (components[cell] != components[run[0]] || components[cell + offset] != components[run[0] + offset])) Flush();
            run.Add(cell);
        }
        Flush();
    }

    private void Link(int a, int b)
    {
        if (!crossings.TryGetValue(a, out var list))
        {
            crossings[a] = list = new List<int>();
            portals[Chunk(a)].Add(a);
        }
        list.Add(b);
    }

    private void Unlink(int a, int b)
    {
        if (!crossings.TryGetValue(a, out var links)) return;
        links.Remove(b);
        if (links.Count != 0) return;
        crossings.Remove(a);
        portals[Chunk(a)].Remove(a);
    }

    public RoutePlan? Plan(int start, int target, MoveCosts movement)
    {
        if (!Walkable(start) || !Walkable(target)) return null;
        if (movement.Cardinal <= 0 || movement.Diagonal < movement.Cardinal || movement.Diagonal > 2L * movement.Cardinal)
            throw new ArgumentOutOfRangeException(nameof(movement));
        EnsureGraph();
        AbstractSearches++;
        IEnumerable<(int, long)> Edges(int cell)
        {
            int chunk = Chunk(cell);
            int count = graph == null ? portals[chunk].Count : graph.Portals(chunk).Length;
            for (int i = 0; i < count; i++)
            {
                int next = graph == null ? portals[chunk][i] : graph.Portals(chunk).Span[i];
                if (next != cell && Component(next) == Component(cell)) yield return (next, Distance(cell, next, movement));
            }
            if (Chunk(target) == chunk && target != cell && Component(cell) == Component(target))
                yield return (target, Distance(cell, target, movement));
            if (graph != null)
            {
                int length = graph.Crossings(cell).Length;
                for (int i = 0; i < length; i++)
                { int next = graph.Crossings(cell).Span[i]; yield return (next, movement.Cardinal + (long)Cost(next)); }
            }
            else if (crossings.TryGetValue(cell, out var links))
                foreach (int next in links) yield return (next, movement.Cardinal + (long)Cost(next));
        }
        var route = Search(start, target, movement, Edges);
        return route == null ? null : new RoutePlan(route.Skip(1).ToList(), start, movement, Identity,
            route.Select(Chunk).Distinct().Select(chunk => (chunk, Version(chunk))).ToArray());
    }

    private List<int>? Search(int start, int target, MoveCosts movement, Func<int, IEnumerable<(int Cell, long Cost)>> edges)
    {
        var open = new PriorityQueue<(int Cell, long Cost), (long F, long H)>();
        var best = new Dictionary<int, long> { [start] = 0 };
        var parent = new Dictionary<int, int>();
        open.Enqueue((start, 0), (Distance(start, target, movement), Distance(start, target, movement)));
        while (open.TryDequeue(out var entry, out _))
        {
            if (best[entry.Cell] != entry.Cost) continue;
            if (entry.Cell == target)
            {
                var result = new List<int> { target };
                while (result[^1] != start) result.Add(parent[result[^1]]);
                result.Reverse();
                return result;
            }
            foreach (var edge in edges(entry.Cell))
            {
                long candidate = entry.Cost + edge.Cost;
                if (best.TryGetValue(edge.Cell, out long old) && old <= candidate) continue;
                best[edge.Cell] = candidate;
                parent[edge.Cell] = entry.Cell;
                long h = Distance(edge.Cell, target, movement);
                open.Enqueue((edge.Cell, candidate), (candidate + h, h));
            }
        }
        return null;
    }

    private List<int>? LocalPath(int start, int target, MoveCosts movement)
    {
        if (Chunk(start) != Chunk(target)) return CanStep(start, target) ? new List<int> { start, target } : null;
        var key = new CacheKey(start, target, movement);
        if (cache.TryGetValue(key, out var cached) && cached.Version == Version(Chunk(start)))
        {
            CacheHits++;
            recency.Remove(cached.Node);
            recency.AddLast(cached.Node);
            return cached.Path;
        }
        if (cached.Path != null) { recency.Remove(cached.Node); cache.Remove(key); }
        LocalSearches++;
        var path = Search(start, target, movement, cell => Adjacent(cell)
            .Where(next => Chunk(next) == Chunk(start))
            .Select(next => (next, Distance(cell, next, movement) + Cost(next))));
        if (path != null)
        {
            if (cache.Count == CacheCapacity)
            {
                cache.Remove(recency.First!.Value);
                recency.RemoveFirst();
            }
            cache.Add(key, (path, recency.AddLast(key), Version(Chunk(start))));
        }
        return path;
    }

    public List<int>? Refine(RoutePlan plan, int start, int maxSteps)
    {
        if (maxSteps < 1 || maxSteps > 200) throw new ArgumentOutOfRangeException(nameof(maxSteps));
        EnsureGraph();
        if (!ReferenceEquals(plan.Owner, Identity) || plan.Dependencies.Any(d => Version(d.Chunk) != d.Version)) return null;
        int progress = plan.Buffer.IndexOf(start);
        if (progress < 0) return null;
        plan.Buffer.RemoveRange(0, progress);
        while (plan.Buffer.Count <= maxSteps && plan.NextWaypoint < plan.Waypoints.Count)
        {
            var leg = LocalPath(plan.Buffer[^1], plan.Waypoints[plan.NextWaypoint], plan.Movement);
            if (leg == null) return null;
            plan.Buffer.AddRange(leg.Skip(1));
            plan.NextWaypoint++;
        }
        return plan.Buffer.Take(maxSteps + 1).ToList();
    }
}
