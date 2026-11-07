using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using HarmonyLib;
using PathfindingLab.Core;
using RimWorld;
using Unity.Collections;
using Verse;
using Verse.AI;

namespace PathfindingLab;

// Native preparation stays in charge; only the common cell-search worker is replaced.
public static class PathRuntime
{
    private sealed class FollowerState
    {
        internal readonly RequestCursor Cursor = new();
    }
    private sealed class Work
    {
        internal readonly HierarchyProfile Profile;
        internal readonly MapRoutes Routes;
        internal readonly RequestCursor Cursor;
        internal readonly RoutingCounts? Counts;
        internal readonly ChunkStamp Stamp;
        internal readonly int Horizon;
        internal GeometrySnapshot Snapshot = null!;
        internal bool Partial;
        internal Work(HierarchyProfile profile, MapRoutes routes, RequestCursor cursor, RoutingCounts? counts, ChunkStamp stamp, int horizon)
        { Routes = routes; Profile = profile; Cursor = cursor; Counts = counts; Stamp = stamp; Horizon = horizon; }
    }
    private sealed class MapRoutes
    {
        internal readonly ChunkChanges Changes;
        internal readonly HierarchyProfiles Profiles = new();
        internal readonly NativeRequestGrid?[] Grids = new NativeRequestGrid[HierarchyProfiles.WorkspaceCapacity];
        internal GeometrySnapshot? Snapshot;
        internal NativeArray<int> BorrowedCosts;
        internal Building[]? BorrowedBuildings;
        internal readonly Func<int, int> ReadCost;
        internal readonly Func<int, bool> ReadCorner;
        internal MapRoutes(int width, int height)
        {
            Changes = new ChunkChanges(width, height);
            ReadCost = index => BorrowedCosts[index];
            ReadCorner = index => BorrowedBuildings![index] is { } building &&
                (building.def.Fillage == FillCategory.Full || building.def.IsFence);
        }
    }

    private static readonly ConditionalWeakTable<Map, MapRoutes> maps = new();
    private static readonly ConditionalWeakTable<Pawn_PathFollower, FollowerState> followers = new();
    private static readonly ConditionalWeakTable<PathRequest, FollowerState> followerRequests = new();
    private static readonly ConditionalWeakTable<PathRequest, Work> results = new();
    private static readonly ConditionalWeakTable<PawnPath, object> partialPaths = new();
    private static readonly object partialMarker = new();
    private static readonly ConditionalWeakTable<Pawn, RoutingCounts> counts = new();
    private static readonly ConcurrentDictionary<NativeReference<PathFinderJob.ResultData>, Work> scheduled = new();
    private static readonly ConcurrentQueue<string> errors = new();
    private static long handled, fallback, partial, repairs, full, readinessFallback;
    [ThreadStatic] public static bool ReadinessFallback;
    public static long ReadinessFallbackRequests => Interlocked.Read(ref readinessFallback);
    public static bool PreparationPending(Map map) => maps.TryGetValue(map, out var routes) && routes.Profiles.IsRunning;
    public static bool PreparationReady(Map map) => maps.TryGetValue(map, out var routes) &&
        routes.Snapshot is { } snapshot && ReferenceEquals(routes.Changes.Capture(), snapshot.Stamp) && routes.Profiles.IsReady(snapshot);
    public static long HandledRequests => Interlocked.Read(ref handled);
    public static long FallbackRequests => Interlocked.Read(ref fallback);
    public static long PartialRequests => Interlocked.Read(ref partial);
    public static long RepairRequests => Interlocked.Read(ref repairs);
    public static long FullRequests => Interlocked.Read(ref full);
    public static string LastFallback { get; private set; } = "";
    public static RoutingSnapshot Counts(Pawn pawn)
    {
        var value = counts.GetValue(pawn, _ => new RoutingCounts());
        lock (value) return value.Snapshot;
    }
    public static (long Renewals, int MinNodes, int MaxNodes, int MaxSteps) Bounds(Pawn pawn)
    {
        var value = counts.GetValue(pawn, _ => new RoutingCounts());
        lock (value) return (value.Renewals, value.MinRenewalNodes, value.MaxRenewalNodes, value.MaxSegmentSteps);
    }
    public static string Diagnostics(Map map) =>
        $"handled={HandledRequests}; partial={PartialRequests}; full={FullRequests}; repairs={RepairRequests}; readinessFallback={ReadinessFallbackRequests}; fallback={FallbackRequests}; last={LastFallback}";

    // Read on the game thread after native workers have drained.
    public static object[] HierarchyDiagnostics(Map map)
    {
        if (!maps.TryGetValue(map, out var routes)) return Array.Empty<object>();
        return Enum.GetValues<RoutingCohort>().Select(cohort =>
        {
            var profile = routes.Profiles.Get(cohort);
            var status = routes.Profiles.Background(cohort).Status();
            return (object)new { Cohort = cohort.ToString(), profile.Requests, profile.LocalHits, Background = status,
                SharedGraph = true, profile.AbstractSearches, profile.LocalSearches, profile.CacheHits,
                profile.GraphUnavailable, profile.CapacityExhausted };
        }).ToArray();
    }

    public static object? WorkspaceDiagnostics(Map map) => maps.TryGetValue(map, out var routes) ? new
    {
        Capacity = HierarchyProfiles.WorkspaceCapacity, routes.Profiles.Active, routes.Profiles.PeakActive,
        routes.Profiles.GraphUnavailable, routes.Profiles.CapacityExhausted,
        SharedGraph = routes.Profiles.Background(RoutingCohort.Player).Status()
    } : null;

    internal static void ValidateShape()
    {
        var method = AccessTools.DeclaredMethod(typeof(PathFinder), "ParameterizePathJob");
        string[] names = { "PathFinderJob&", "MapGridRequest&", "PathJobOutput&", "PathUniqueState&", "JobBatchState&", "ScheduledGridJob&", "PathRequest" };
        if (method == null || method.IsStatic || method.ReturnType != typeof(void) ||
            !method.GetParameters().Select(p => p.ParameterType.Name).SequenceEqual(names))
            throw new MissingMethodException("PathFinder.ParameterizePathJob shape");
        if (AccessTools.DeclaredMethod(typeof(PathFinderJob), nameof(PathFinderJob.Execute), Type.EmptyTypes)?.ReturnType != typeof(void))
            throw new MissingMethodException("PathFinderJob.Execute shape");
        if (AccessTools.DeclaredMethod(typeof(PathFinderMapData), "ParameterizePathJob", new[] { typeof(PathFinderJob).MakeByRefType() })?.ReturnType != typeof(void))
            throw new MissingMethodException("PathFinderMapData.ParameterizePathJob shape");
        if (AccessTools.DeclaredField(typeof(PathFinderMapData), "map")?.FieldType != typeof(Map))
            throw new MissingFieldException("PathFinderMapData.map");
        foreach (var args in new[] { new[] { typeof(IntVec3) }, new[] { typeof(CellRect) } })
            if (AccessTools.DeclaredMethod(typeof(PathFinderMapData), "Notify_CellDelta", args)?.ReturnType != typeof(void))
                throw new MissingMethodException("PathFinderMapData.Notify_CellDelta shape");
        if (AccessTools.DeclaredMethod(typeof(PathFinderMapData), "Notify_MapDirtied", Type.EmptyTypes)?.ReturnType != typeof(void))
            throw new MissingMethodException("PathFinderMapData.Notify_MapDirtied shape");
    }

    // MapComponentUpdate runs on the game thread even when simulation is paused.
    // No path request, synthetic query or native GatherData is needed to prepare a map.
    internal static void PrepareMap(Map map)
    {
        if (!LabMod.Ready || !LabMod.Settings.Experimental || Current.Game?.PlayerHasControl != true) return;
        var routes = maps.GetValue(map, m => new MapRoutes(m.Size.x, m.Size.z));
        var stamp = routes.Changes.Capture();
        if (!ReferenceEquals(routes.Snapshot?.Stamp, stamp)) PrepareSnapshot(map, routes, stamp);
    }

    private static GeometrySnapshot PrepareSnapshot(Map map, MapRoutes routes, ChunkStamp stamp)
    {
        // Borrow only for this main-thread copy. Owned immutable chunks are the sole input
        // to background builders. Cached delegates avoid per-capture closures.
        routes.BorrowedCosts = map.pathing.Normal.pathGrid.Grid_Unsafe;
        routes.BorrowedBuildings = map.edificeGrid.InnerArray;
        try
        {
            var snapshot = routes.Snapshot = GeometrySnapshot.Capture(routes.Snapshot, stamp, routes.ReadCost, routes.ReadCorner);
            routes.Profiles.Prepare(snapshot);
            return snapshot;
        }
        finally { routes.BorrowedCosts = default; routes.BorrowedBuildings = null; }
    }

    internal static void CellDirtyPostfix(Map ___map, IntVec3 cell)
    {
        if (maps.TryGetValue(___map, out var routes)) routes.Changes.MarkCell(cell.x, cell.z);
    }
    internal static void RectDirtyPostfix(Map ___map, CellRect rect)
    {
        if (maps.TryGetValue(___map, out var routes)) routes.Changes.MarkRect(rect.minX, rect.minZ, rect.maxX, rect.maxZ);
    }
    internal static void AllDirtyPostfix(Map ___map)
    {
        if (maps.TryGetValue(___map, out var routes)) routes.Changes.MarkAll();
    }

    internal static void GeneratePostfix(Pawn_PathFollower __instance, PathRequest __result)
    {
        if (__result != null)
            followerRequests.Add(__result, followers.GetValue(__instance, _ => new FollowerState()));
    }

    internal static void AssociatePostfix(ref PathFinderJob job, PathFinder.MapGridRequest gridRequest, PathRequest request)
    {
        while (errors.TryDequeue(out var error))
        {
            LastFallback = error;
            Log.Warning("[Pathfinding Lab] Emergency vanilla search fallback: " + error);
        }
        if (!LabMod.Settings.Experimental) return;
        var routes = maps.GetValue(request.map, map => new MapRoutes(map.Size.x, map.Size.z));
        var faction = request.RequesterFaction;
        var cohort = faction == null ? RoutingCohort.Wildlife : faction == Faction.OfPlayer ? RoutingCohort.Player :
            faction.HostileTo(Faction.OfPlayer) ? RoutingCohort.Hostile :
            faction.RelationKindWith(Faction.OfPlayer) == FactionRelationKind.Ally ? RoutingCohort.Allied : RoutingCohort.Neutral;
        bool moving = followerRequests.TryGetValue(request, out var follower);
        var work = new Work(routes.Profiles.Get(cohort), routes, moving ? follower!.Cursor : new RequestCursor(),
            request.pawn == null ? null : counts.GetValue(request.pawn, _ => new RoutingCounts()),
            routes.Changes.Capture(), moving ? LabPolicy.RefinementCells(LabMod.Settings.RefinementCells) : 0);
        results.Add(request, work);
        scheduled[job.result] = work;
    }

    internal static void SnapshotPostfix(ref PathFinderJob job, Map ___map)
    {
        if (!scheduled.TryGetValue(job.result, out var work)) return;
        work.Snapshot = ReferenceEquals(work.Routes.Snapshot?.Stamp, work.Stamp) ? work.Routes.Snapshot! :
            PrepareSnapshot(___map, work.Routes, work.Stamp);
    }

    internal static void DisposePrefix(Map ___map)
    {
        if (maps.TryGetValue(___map, out var routes)) routes.Profiles.Dispose();
        maps.Remove(___map);
    }

    internal static bool ExecutePrefix(ref PathFinderJob __instance)
    {
        ReadinessFallback = false;
        if (!scheduled.TryRemove(__instance.result, out var work)) return true;
        HierarchyProfiles.WorkspaceLease lease = default;
        NativeRequestGrid? grid = null;
        long abstractBefore = 0, localBefore = 0, hitsBefore = 0;
        try
        {
            if (!work.Routes.Profiles.TryAcquire(work.Snapshot, out lease, out var reason))
            {
                work.Profile.RecordRequest(false);
                work.Profile.RecordFallback(reason);
                ReadinessFallback = true;
                Interlocked.Increment(ref readinessFallback);
                return true;
            }
            // The adapter and scratch are private to a slot, even for concurrent same-cohort pawns.
            grid = work.Routes.Grids[lease.Index] ??= new NativeRequestGrid();
            grid.Job = __instance;
            var router = lease.Router;
            abstractBefore = router.Guide.AbstractSearches; localBefore = router.Guide.LocalSearches; hitsBefore = router.Guide.CacheHits;
            int capacity = checked(__instance.destRect.Width * __instance.destRect.Height);
            Span<int> goals = capacity <= 64 ? stackalloc int[capacity] : router.DestinationBuffer(capacity).Span;
            int goalCount = 0;
            foreach (var cell in __instance.destRect)
            {
                int index = __instance.indices.CellToIndex(cell);
                if (!__instance.excludedRectIndices.Contains(index)) goals[goalCount++] = index;
            }
            var targets = goals.Slice(0, goalCount);
            int start = __instance.indices.CellToIndex(__instance.start);
            var movement = new MoveCosts(__instance.moveTicksCardinal, __instance.moveTicksDiagonal);
            var route = router.TryLocal(grid, start, targets, movement, work.Horizon);
            work.Profile.RecordRequest(route != null);
            if (route == null)
            {
                route = router.Find(grid, start, targets, movement, work.Horizon, work.Cursor);
            }
            __instance.path.Clear();
            __instance.result.Value = default;
            if (route is { } found)
            {
                var cells = found.Cells.Span;
                for (int i = cells.Length - 1; i >= 0; i--) __instance.path.Add(__instance.indices.IndexToCell(cells[i]));
                __instance.result.Value = new PathFinderJob.ResultData { pathCost = (int)Math.Min(int.MaxValue, found.Cost) };
                work.Partial = found.Partial;
                if (found.Partial) Interlocked.Increment(ref partial);
                if (found.Repaired) Interlocked.Increment(ref repairs);
            }
            if (work.Horizon == 0) Interlocked.Increment(ref full);
            Interlocked.Increment(ref handled);
            if (work.Counts != null)
                lock (work.Counts) work.Counts.Handle(work.Partial, work.Horizon == 0 ? 0 : Math.Max(0, (route?.Cells.Length ?? 1) - 1));
            return false;
        }
        catch (Exception exception)
        {
            work.Partial = false;
            Interlocked.Increment(ref fallback);
            if (work.Counts != null) lock (work.Counts) work.Counts.Fallback();
            // Worker callbacks must not read live game objects or call the game logger.
            if (errors.Count < 4) errors.Enqueue(exception.GetType().Name + ": " + exception.Message);
            return true;
        }
        finally
        {
            if (grid != null)
            {
                grid.Job = default;
                var guide = lease.Router.Guide;
                work.Profile.RecordSearches(guide.AbstractSearches - abstractBefore, guide.LocalSearches - localBefore, guide.CacheHits - hitsBefore);
            }
            lease.Dispose();
        }
    }

    internal static void ResolvePostfix(PathRequest __instance, PawnPath p)
    {
        if (p != null && results.TryGetValue(__instance, out var work) && work.Partial)
        {
            partialPaths.Remove(p); // PawnPath instances are pooled.
            partialPaths.Add(p, partialMarker);
        }
        else if (p != null) partialPaths.Remove(p);
    }

    internal static void NeedNewPathPostfix(Pawn_PathFollower __instance, Pawn ___pawn, ref bool __result)
    {
        if (__instance.curPath != null && partialPaths.TryGetValue(__instance.curPath, out _) &&
            __instance.curPath.NodesLeftCount <= LabPolicy.RenewalNodes(LabMod.Settings.RefinementCells))
        {
            var value = counts.GetValue(___pawn, _ => new RoutingCounts());
            lock (value) value.Renew(__instance.curPath.NodesLeftCount);
            __result = true;
        }
    }
    internal static void StopPostfix(Pawn_PathFollower __instance) => followers.Remove(__instance);
    internal static void ReleasePostfix(PawnPath __instance) => partialPaths.Remove(__instance);

    // Mirrors the inspected worker's edge/cost contract, using only job-owned native snapshots.
    private sealed class NativeRequestGrid : IRequestGrid
    {
        internal PathFinderJob Job;
        private ref PathFinderJob job => ref Job;
        public int Cost(int index) => job.providerCost[index] == ushort.MaxValue ? 10000 :
            Math.Max(job.grid[index], job.providerCost[index]) + (job.blocked[index] ? 175 : 0);
        public bool CanStep(int from, int to)
        {
            if (to < 0 || to >= job.indices.NumGridCells || Cost(to) >= 10000) return false;
            IntVec3 a = job.indices.IndexToCell(from), b = job.indices.IndexToCell(to);
            var connection = CellConnectionExtensions.ConnectionFromCells(a, b);
            if (!connection.HasValue || !job.connectivity[from].HasBit(connection.Value)) return false;
            if (!connection.Value.Diagonal()) return true;
            int sideX = job.indices.CellToIndex(new IntVec3(b.x, 0, a.z));
            int sideZ = job.indices.CellToIndex(new IntVec3(a.x, 0, b.z));
            return Cost(sideX) < 10000 && Cost(sideZ) < 10000 &&
                !job.buildings.IsSet(sideX) && !job.buildings.IsSet(sideZ) &&
                (job.traverseParams.canBashFences || !job.fences.IsSet(sideX) && !job.fences.IsSet(sideZ));
        }
    }
}

public sealed class PathfindingMapComponent(Map map) : MapComponent(map)
{
    public override void MapComponentUpdate() => PathRuntime.PrepareMap(map);
}
