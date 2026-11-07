using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PathfindingLab.Core;

public interface IRequestGrid
{
    int Cost(int cell);
    bool CanStep(int from, int to);
}

public sealed class RequestCursor
{
    internal object? Owner;
    internal int[] Goals = Array.Empty<int>();
    internal MoveCosts Movement;
    internal RoutePlan? Plan;
    internal int[] RepairStorage = Array.Empty<int>();
    internal ReadOnlyMemory<int> Repair;
}

// Borrowed storage: consume before the next search on this router or cursor.
// Copy explicitly if a result must survive that boundary. A profile lease owns all calls.
public readonly record struct RequestRoute(ReadOnlyMemory<int> Cells, long Cost, bool Partial, bool Repaired);

public sealed class RequestRouter
{
    private readonly int width, height;
    private object Identity => Guide.Identity;
    private readonly SearchWorkspace local = new(), repair = new();
    private int[] output = Array.Empty<int>();
    private int[] destinations = Array.Empty<int>();
    public PortalRouter Guide { get; }

    // Oversized native destination rectangles use leased storage, never unbounded stackalloc.
    internal Memory<int> DestinationBuffer(int count)
    {
        EnsureCapacity(ref destinations, count);
        return destinations.AsMemory(0, count);
    }

    // A miss means escalate, not unreachable. No graph is touched on this path.
    public RequestRoute? TryLocal(IRequestGrid grid, int start, ReadOnlySpan<int> goals, MoveCosts movement, int maxSteps)
    {
        ValidateArguments(maxSteps, movement);
        bool nearby = false;
        foreach (int goal in goals)
            if (Math.Max(Math.Abs(goal % width - start % width), Math.Abs(goal / width - start / width)) <= 32)
            { nearby = true; break; }
        if (!nearby) return null;
        var complete = local.Search(width, height, grid, start, goals, movement, 256, 32);
        if (complete.IsEmpty) return null;
        var cells = Prefix(complete, maxSteps);
        return Validate(grid, cells, goals, movement, false, false);
    }

    public RequestRouter(int width, int height, int[] costs, bool[] corners)
    {
        this.width = width;
        this.height = height;
        Guide = new PortalRouter(width, height, costs, corners);
    }

    public RequestRouter(PortalGraph graph)
    { width = graph.Width; height = graph.Height; Guide = new PortalRouter(graph); }

    // maxSteps=0 is a full-path query. Only the native follower adapter requests a prefix.
    public RequestRoute? Find(IRequestGrid grid, int start, ReadOnlySpan<int> goals, MoveCosts movement, int maxSteps, RequestCursor cursor)
    {
        ValidateArguments(maxSteps, movement);
        if (goals.IsEmpty) return null;
        if (cursor.Owner != Identity || cursor.Movement != movement || !goals.SequenceEqual(cursor.Goals))
        {
            cursor.Owner = Identity;
            cursor.Movement = movement;
            if (cursor.Goals.Length != goals.Length) cursor.Goals = new int[goals.Length];
            goals.CopyTo(cursor.Goals);
            cursor.Plan = null;
            cursor.Repair = default;
        }
        if (goals.Contains(start))
        {
            EnsureCapacity(ref output, 1);
            output[0] = start;
            return new RequestRoute(output.AsMemory(0, 1), 0, false, false);
        }
        if (!cursor.Repair.IsEmpty)
        {
            int progress = cursor.Repair.Span.IndexOf(start);
            if (progress >= 0)
            {
                // Prove the entire directed tail again before passing the last detour.
                // Both tail and prefix are views: no copy proportional to remaining distance.
                var remaining = cursor.Repair.Slice(progress);
                if (Validate(grid, remaining, goals, movement, true) != null)
                    return Validate(grid, Prefix(remaining, maxSteps), goals, movement, true);
            }
            cursor.Repair = default;
        }
        List<int>? cells = null;
        bool normalMovement = movement.Cardinal > 0 && movement.Diagonal >= movement.Cardinal && movement.Diagonal <= 2L * movement.Cardinal;
        if (normalMovement)
        {
            if (cursor.Plan != null) cells = Guide.Refine(cursor.Plan, start, maxSteps == 0 ? 200 : maxSteps);
            if (cells == null)
            {
                int goal = goals[0];
                long closest = Distance(width, start, goal, movement);
                foreach (int candidate in goals.Slice(1))
                {
                    long distance = Distance(width, start, candidate, movement);
                    if (distance < closest) { closest = distance; goal = candidate; }
                }
                cursor.Plan = Guide.Plan(start, goal, movement);
                if (cursor.Plan != null) cells = Guide.Refine(cursor.Plan, start, maxSteps == 0 ? 200 : maxSteps);
            }
            if (maxSteps == 0 && cells != null)
                while (!goals.Contains(cells[^1]))
                {
                    var next = Guide.Refine(cursor.Plan!, cells[^1], 200);
                    if (next == null || next.Count < 2) { cells = null; break; }
                    for (int i = 1; i < next.Count; i++) cells.Add(next[i]);
                }
            if (cells != null)
            {
                EnsureCapacity(ref output, cells.Count);
                CollectionsMarshal.AsSpan(cells).CopyTo(output);
                var guided = Validate(grid, output.AsMemory(0, cells.Count), goals, movement, false);
                if (guided is { } found && (found.Cells.Length > 1 || !found.Partial)) return found;
            }
        }
        cursor.Plan = null;
        var path = repair.Search(width, height, grid, start, goals, movement);
        if (path.IsEmpty) return null;
        // A cursor must survive searches by other pawns; it owns this one persistent copy.
        EnsureCapacity(ref cursor.RepairStorage, path.Length);
        path.Span.CopyTo(cursor.RepairStorage);
        cursor.Repair = cursor.RepairStorage.AsMemory(0, path.Length);
        return Validate(grid, Prefix(cursor.Repair, maxSteps), goals, movement, true);
    }

    private RequestRoute? Validate(IRequestGrid grid, ReadOnlyMemory<int> memory, ReadOnlySpan<int> goals,
        MoveCosts movement, bool repaired, bool requireReverse = true)
    {
        var path = memory.Span;
        long cost = 0;
        for (int i = 1; i < path.Length; i++)
        {
            if (!grid.CanStep(path[i - 1], path[i])) return null;
            if (!repaired && requireReverse && !grid.CanStep(path[i], path[i - 1])) return null;
            int extra = grid.Cost(path[i]);
            if (extra < 0 || extra >= 10000) return null;
            cost += extra + (long)StepCost(width, path[i - 1], path[i], movement);
        }
        return new RequestRoute(memory, cost, !goals.Contains(path[^1]), repaired);
    }

    private static ReadOnlyMemory<int> Prefix(ReadOnlyMemory<int> path, int maxSteps) =>
        maxSteps == 0 ? path : path.Slice(0, Math.Min(path.Length, maxSteps + 1));
    private static void ValidateArguments(int maxSteps, MoveCosts movement)
    {
        if (maxSteps < 0 || maxSteps > 200 || movement.Cardinal < 0 || movement.Diagonal < 0)
            throw new ArgumentOutOfRangeException(nameof(maxSteps));
    }
    private static void EnsureCapacity(ref int[] buffer, int count)
    {
        if (buffer.Length < count) Array.Resize(ref buffer, Math.Max(count, Math.Max(32, buffer.Length * 2)));
    }
    private static int StepCost(int width, int a, int b, MoveCosts move) =>
        a % width != b % width && a / width != b / width ? move.Diagonal : move.Cardinal;
    private static long Distance(int width, int a, int b, MoveCosts move) =>
        (long)Math.Max(Math.Abs(a % width - b % width), Math.Abs(a / width - b / width)) * Math.Min(move.Cardinal, move.Diagonal);

    // Contiguous AoS nodes keep a cell's relaxation data together. Local and full repair
    // own separate arrays, so a rare million-cell repair cannot enlarge the local working set.
    private sealed class SearchWorkspace
    {
        private struct Node { internal long Cost; internal int Parent, Generation; }
        private Node[] nodes = Array.Empty<Node>();
        private readonly PriorityQueue<(int Cell, long Cost), (long F, long H)> open = new();
        private int[] path = Array.Empty<int>();
        private int generation;

        internal ReadOnlyMemory<int> Search(int width, int height, IRequestGrid grid, int start,
            ReadOnlySpan<int> goals, MoveCosts movement, int budget = int.MaxValue, int radius = int.MaxValue)
        {
            int sx = start % width, sz = start / width;
            int x0 = radius == int.MaxValue ? 0 : Math.Max(0, sx - radius);
            int z0 = radius == int.MaxValue ? 0 : Math.Max(0, sz - radius);
            int x1 = radius == int.MaxValue ? width - 1 : Math.Min(width - 1, sx + radius);
            int z1 = radius == int.MaxValue ? height - 1 : Math.Min(height - 1, sz + radius);
            int stride = x1 - x0 + 1, count = checked(stride * (z1 - z0 + 1));
            if (nodes.Length < count) nodes = new Node[count];
            if (generation == int.MaxValue) { Array.Clear(nodes); generation = 0; }
            generation++;
            open.Clear();
            int Index(int cell) => cell % width - x0 + (cell / width - z0) * stride;
            nodes[Index(start)] = new Node { Cost = 0, Parent = start, Generation = generation };
            open.Enqueue((start, 0), (0, 0));
            int expanded = 0;
            while (open.TryDequeue(out var current, out _))
            {
                if (nodes[Index(current.Cell)].Cost != current.Cost) continue;
                if (goals.Contains(current.Cell))
                {
                    int length = 1;
                    for (int cell = current.Cell; cell != start; cell = nodes[Index(cell)].Parent) length++;
                    EnsureCapacity(ref path, length);
                    int at = current.Cell;
                    for (int i = length - 1; i >= 0; i--)
                    { path[i] = at; at = nodes[Index(at)].Parent; }
                    return path.AsMemory(0, length);
                }
                if (++expanded > budget) return default;
                int x = current.Cell % width, z = current.Cell / width;
                for (int dz = -1; dz <= 1; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0 || x + dx < x0 || x + dx > x1 || z + dz < z0 || z + dz > z1) continue;
                        int next = current.Cell + dx + dz * width;
                        if (!grid.CanStep(current.Cell, next)) continue;
                        int extra = grid.Cost(next);
                        if (extra < 0 || extra >= 10000) continue;
                        long candidate = current.Cost + extra + StepCost(width, current.Cell, next, movement);
                        ref var node = ref nodes[Index(next)];
                        if (node.Generation == generation && node.Cost <= candidate) continue;
                        node = new Node { Cost = candidate, Parent = current.Cell, Generation = generation };
                        long h = long.MaxValue;
                        foreach (int goal in goals) h = Math.Min(h, Distance(width, next, goal, movement));
                        open.Enqueue((next, candidate), (candidate + h, h));
                    }
            }
            return default;
        }
    }
}
