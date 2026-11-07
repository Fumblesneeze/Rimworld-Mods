using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using System.Threading;
using Gilzoide.ManagedJobs;
using HarmonyLib;
using PathfindingLab.Core;
using Unity.Collections;
using Unity.Jobs;
using Verse;
using Verse.AI;

namespace PathfindingLab.EndToEndTests;

// Loaded only by the isolated E2E runner. Nothing here enters the product package.
internal sealed class SearchTimingProbe : IDisposable
{
    private const string Owner = "fumblesneeze.pathfindinglab.search-timing-test";
    private sealed record Work(SearchSamples Samples, string Source, string? PawnId);
    private static readonly ConcurrentDictionary<NativeReference<PathFinderJob.ResultData>, Work> scheduled = new();
    private static Map? measuredMap;
    private static SearchSamples? current;
    private static MovementSamples? movements;
    private static readonly HashSet<Pawn_PathFollower> starting = new();
    private static readonly Dictionary<Pawn_PathFollower, long> paid = new();
    [ThreadStatic] private static Measurement? experimental;
    [ThreadStatic] private static SearchSamples? gridWindow;
    [ThreadStatic] private static SearchSamples? snapshotWindow;
    private static readonly ConcurrentDictionary<BackgroundHierarchy, long> measuredBackground = new();
    private static readonly Dictionary<SearchSamples, int> windowIds = new();
    private static long nextLane;
    private static BackgroundSamples background = new();
    internal BackgroundSamples Background => background;
    private readonly string probeId = Guid.NewGuid().ToString("N");
    private readonly Harmony harmony = new(Owner);
    private readonly List<(int Id, SearchSamples Searches, MovementSamples Movements)> windows = new();

    internal sealed class Measurement
    {
        internal readonly SearchSamples Samples;
        internal readonly Measurement? Parent;
        internal string Source;
        internal readonly string? PawnId;
        internal int PreparationDepth;
        internal long Started, Preparation, Refresh, Graph, LockWait;
        internal Measurement(SearchSamples samples, string source, string? pawnId = null, Measurement? parent = null)
        { Samples = samples; Source = source; PawnId = pawnId; Parent = parent; Started = Stopwatch.GetTimestamp(); }
        internal void Finish(bool succeeded, bool preparationOnly = false, bool completesWorker = false)
        {
            long elapsed = Stopwatch.GetTimestamp() - Started;
            if (completesWorker) Samples.Complete(elapsed, preparationOnly ? elapsed : Preparation, Stopwatch.Frequency, succeeded, Source, PawnId, Refresh, Graph, LockWait);
            else Samples.Add(elapsed, preparationOnly ? elapsed : Preparation, Stopwatch.Frequency, succeeded, Source, PawnId, Refresh, Graph, LockWait);
        }
    }

    internal SearchTimingProbe(Map map)
    {
        try
        {
            var parameterize = AccessTools.Method(typeof(PathFinder), "ParameterizePathJob")
                ?? throw new MissingMethodException("PathFinder.ParameterizePathJob");
            var nested = new[] { "MapGridRequest", "PathJobOutput", "PathUniqueState", "JobBatchState", "ScheduledGridJob" }
                .Select(name => typeof(PathFinder).GetNestedType(name, BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new MissingMemberException(name));
            var expected = new[] { typeof(PathFinderJob) }.Concat(nested).Select(type => type.MakeByRefType()).Concat(new[] { typeof(PathRequest) });
            if (parameterize.IsStatic || parameterize.ReturnType != typeof(void) ||
                !parameterize.GetParameters().Select(value => value.ParameterType).SequenceEqual(expected))
                throw new MissingMethodException("PathFinder.ParameterizePathJob shape changed");
            harmony.Patch(parameterize, postfix: Patch(nameof(Associate)));
            harmony.Patch(Required(typeof(PathFinderJob), "Execute", Type.EmptyTypes), prefix: Patch(nameof(VanillaBegin)), finalizer: Patch(nameof(VanillaEnd)));
            harmony.Patch(Required(typeof(PathRuntime), "ExecutePrefix", new[] { typeof(PathFinderJob).MakeByRefType() }),
                finalizer: Patch(nameof(ExperimentalReturned)));
            harmony.Patch(Required(typeof(PortalRouter), "ApplySnapshot", new[] { typeof(GeometrySnapshot) }),
                prefix: Patch(nameof(PreparationBegin)), finalizer: Patch(nameof(PreparationEnd)));
            harmony.Patch(Required(typeof(PortalRouter), "Publish", Type.EmptyTypes), prefix: Patch(nameof(PreparationBegin)), finalizer: Patch(nameof(PreparationEnd)));
            harmony.Patch(Required(typeof(HierarchyProfiles), "TryAcquire", new[] { typeof(GeometrySnapshot), typeof(HierarchyProfiles.WorkspaceLease).MakeByRefType(), typeof(WorkspaceFallback).MakeByRefType() }), prefix: Patch(nameof(LockBegin)), finalizer: Patch(nameof(LockEnd)));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(PathRuntime), "SnapshotPostfix"), prefix: Patch(nameof(SnapshotBegin)), finalizer: Patch(nameof(SnapshotEnd)));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(PathRuntime), "PrepareSnapshot"), prefix: Patch(nameof(ProactiveBegin)), finalizer: Patch(nameof(SnapshotEnd)));
            harmony.Patch(Required(typeof(BackgroundHierarchy), "Request", new[] { typeof(GeometrySnapshot) }), prefix: Patch(nameof(BackgroundRequested)), finalizer: Patch(nameof(BackgroundAdmitted)));
            harmony.Patch(Required(typeof(BackgroundHierarchy), "Build", new[] { typeof(GeometrySnapshot) }), prefix: Patch(nameof(BackgroundBegin)), finalizer: Patch(nameof(BackgroundEnd)));
            harmony.Patch(Required(typeof(JobGiver_Wander), "GetExactWanderDest", new[] { typeof(Pawn) }), prefix: Patch(nameof(WanderBegin)), finalizer: Patch(nameof(WanderEnd)));
            harmony.Patch(Required(typeof(WildlifeWander), "PushPrefix", new[] { typeof(PathRequest) }), prefix: Patch(nameof(DirectBegin)), finalizer: Patch(nameof(DirectEnd)));
            harmony.Patch(Required(typeof(PathFinderMapData), "GatherData", new[] { typeof(IEnumerable<PathRequest>) }),
                prefix: Patch(nameof(GatherBegin)), finalizer: Patch(nameof(SharedEnd)));
            harmony.Patch(Required(typeof(PathFinder), "ScheduleGridJob", new[] { typeof(PathRequest), typeof(JobHandle) }),
                prefix: Patch(nameof(GridBegin)), finalizer: Patch(nameof(GridEnd)));
            // Exact 1.7 managed scheduler boundary: one timestamp pair per grid batch, not per cell.
            var scheduler = typeof(JobHandle).Assembly.GetType("Unity.Jobs.UnityJobScheduling", true)!;
            harmony.Patch(Required(scheduler, "ScheduleParallel", new[] { typeof(int), typeof(int), typeof(JobHandle), typeof(Func<int, int, Action>) }),
                prefix: Patch(nameof(WrapGridBatches)));
            // Late-installed helper patches can miss already compiled call sites. Rewrite both
            // native dispatchers so the actual IJob is wrapped BEFORE scheduling/Run, not timed
            // through the private parameterizer. The original dependency/disposal paths remain.
            harmony.Patch(Required(typeof(PathFinder), "ScheduleBatchedPathJobs", new[] { typeof(JobHandle) }),
                transpiler: Patch(nameof(WrapDoorDispatch)));
            harmony.Patch(Required(typeof(PathFinder), "FindPathNow", new[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms),
                    typeof(PathFinderCostTuning?), typeof(PathEndMode), typeof(PathRequest.IPathGridCustomizer) }),
                transpiler: Patch(nameof(WrapDoorDispatch)));
            harmony.Patch(Required(typeof(Pawn_PathFollower), "StartPath", new[] { typeof(LocalTargetInfo), typeof(PathEndMode) }),
                prefix: Patch(nameof(StartMovement)), finalizer: Patch(nameof(StartedMovement)));
            harmony.Patch(Required(typeof(Pawn_PathFollower), "StopDead", Type.EmptyTypes), postfix: Patch(nameof(StoppedMovement)));
            harmony.Patch(Required(typeof(Pawn_PathFollower), "CostToPayThisTick", Type.EmptyTypes), postfix: Patch(nameof(PaidMovement)));
            harmony.Patch(Required(typeof(Pawn_PathFollower), "PatherTick", Type.EmptyTypes),
                prefix: Patch(nameof(MovementTickBegin)), finalizer: Patch(nameof(MovementTickEnd)));
            measuredMap = map;
            background = new BackgroundSamples();
        }
        catch { Dispose(); throw; }
    }

    private static MethodInfo Required(Type type, string name, Type[] args) =>
        AccessTools.DeclaredMethod(type, name, args) ?? throw new MissingMethodException(type.FullName, name);
    private static HarmonyMethod Patch(string name) => new(typeof(SearchTimingProbe), name);
    internal MovementSamples Movements => movements ?? throw new InvalidOperationException("No movement window.");
    internal SearchSamples BeginJourney(int? observationId = null)
    {
        if (current != null) throw new InvalidOperationException("Timing window already open.");
        movements = new MovementSamples();
        current = new SearchSamples();
        int id = observationId ?? windows.Count;
        windowIds[current] = id;
        windows.Add((id, current, movements));
        return current;
    }
    internal void EndJourney()
    {
        if (current != null && windowIds.TryGetValue(current, out int id)) background.CloseWindow(id);
        current?.Close();
        current = null;
        movements?.Close(Stopwatch.GetTimestamp(), Stopwatch.Frequency);
    }

    // Association is made on the game thread before scheduling. Worker callbacks touch only
    // unmanaged job output and the thread-safe sample sink, never live pawn/map state.
    private static void Associate(PathFinder __instance, ref PathFinderJob job, PathRequest request)
    {
        // In the inspected 1.7 UnityStubs, Equals compares buffer/allocator/ownership,
        // not ResultData.Value. Copies in scheduled jobs retain that exact output identity.
        var key = job.result;
        if (ReferenceEquals(__instance, measuredMap?.pathFinder) && current is { } samples)
        {
            samples.Scheduled();
            scheduled[key] = new Work(samples, LabMod.Settings.Experimental ? "experimental-search" : "vanilla-search", request.pawn?.ThingID);
        }
    }
    [HarmonyPriority(Priority.First)]
    private static void VanillaBegin(ref PathFinderJob __instance, out Measurement? __state)
    {
        __state = scheduled.TryRemove(__instance.result, out var work) ? new Measurement(work.Samples, work.Source, work.PawnId, experimental) : null;
        if (__state?.Source == "experimental-search") experimental = __state;
    }
    private static Exception? VanillaEnd(ref PathFinderJob __instance, Measurement? __state, Exception? __exception)
    {
        __state?.Finish(__exception == null && __instance.path.Length > 0, completesWorker: true);
        if (__state?.Source == "experimental-search") experimental = __state.Parent;
        return __exception;
    }
    private static Exception? ExperimentalReturned(bool __result, Exception? __exception)
    {
        if (experimental is not { } measurement || !__result && __exception == null) return __exception;
        // The replacement declined after an exception: retain the failed attempt separately,
        // then time the native body that Execute's prefix is about to allow.
        bool readiness = PathRuntime.ReadinessFallback && __exception == null;
        measurement.Source = readiness ? "experimental-readiness-dispatch" : "experimental-attempt";
        measurement.Finish(readiness);
        measurement.Source = "vanilla-search";
        measurement.Started = Stopwatch.GetTimestamp();
        measurement.Preparation = 0;
        measurement.Refresh = measurement.Graph = measurement.LockWait = 0;
        experimental = measurement.Parent;
        return __exception;
    }
    private static void SnapshotBegin(ref PathFinderJob job, out GridScope __state)
    {
        var measurement = scheduled.TryGetValue(job.result, out var work) && work.Source == "experimental-search" ? new Measurement(work.Samples, "experimental-snapshot") : null;
        __state = new GridScope(snapshotWindow, measurement);
        snapshotWindow = measurement?.Samples;
    }
    private static void ProactiveBegin(Map map, out GridScope __state)
    {
        // Nested request capture is already inside experimental-snapshot; do not double count.
        var measurement = snapshotWindow == null && ReferenceEquals(map, measuredMap) && current is { } samples
            ? new Measurement(samples, "experimental-proactive-snapshot") : null;
        __state = new GridScope(snapshotWindow, measurement);
        snapshotWindow = measurement?.Samples ?? snapshotWindow;
    }
    private static Exception? SnapshotEnd(GridScope __state, Exception? __exception)
    {
        snapshotWindow = __state.Previous;
        __state.Measurement?.Finish(__exception == null, preparationOnly: true);
        return __exception;
    }
    internal sealed record BackgroundAdmission(long Lane, long Version, bool Added);
    private static void BackgroundRequested(BackgroundHierarchy __instance, GeometrySnapshot snapshot, out BackgroundAdmission? __state)
    {
        __state = null;
        if (snapshotWindow != null) measuredBackground.GetOrAdd(__instance, _ => Interlocked.Increment(ref nextLane));
        if (!measuredBackground.TryGetValue(__instance, out long lane)) return;
        int? window = snapshotWindow != null && windowIds.TryGetValue(snapshotWindow, out int id) ? id : null;
        __state = new(lane, snapshot.Version, background.Queue(lane, snapshot.Version, window, Stopwatch.GetTimestamp()));
    }
    private static Exception? BackgroundAdmitted(bool __result, BackgroundAdmission? __state, Exception? __exception)
    {
        if (__state?.Added == true) background.Admit(__state.Lane, __state.Version, __result && __exception == null);
        return __exception;
    }
    internal sealed record BackgroundExecution(BackgroundSamples Owner, long Lane, long Version, Measurement Measurement);
    private static void BackgroundBegin(BackgroundHierarchy __instance, GeometrySnapshot snapshot, out BackgroundExecution? __state)
    {
        __state = null;
        if (!measuredBackground.TryGetValue(__instance, out long lane)) return;
        var measurement = new Measurement(background.Timings, "experimental-background", parent: experimental);
        background.Start(lane, snapshot.Version, measurement.Started);
        background.Timings.Scheduled();
        __state = new(background, lane, snapshot.Version, measurement);
        experimental = measurement;
    }
    private static Exception? BackgroundEnd(BackgroundExecution? __state, Exception? __exception)
    {
        if (__state == null) return __exception;
        long finished = Stopwatch.GetTimestamp();
        __state.Measurement.Finish(__exception == null, completesWorker: true);
        __state.Owner.Finish(__state.Lane, __state.Version, finished,
            (finished - __state.Measurement.Started) * 1000d / Stopwatch.Frequency, __exception == null);
        experimental = __state.Measurement.Parent;
        return __exception;
    }
    internal bool FlushBackground()
    {
        foreach (var pair in measuredBackground)
            if (!pair.Key.IsRunning) background.SettleIdle(pair.Value, pair.Key.Failure);
        return background.Pending == 0 && background.Timings.Pending == 0;
    }
    [HarmonyPriority(Priority.First)]
    [HarmonyBefore("fumblesneeze.pathfindinglab")]
    private static void WanderBegin(Pawn pawn, out Measurement? __state) =>
        __state = ReferenceEquals(pawn.Map, measuredMap) && pawn.RaceProps.Animal && pawn.Faction == null && current is { } samples
            ? new Measurement(samples, "wildlife-destination", pawn.ThingID) : null;
    private static Exception? WanderEnd(Measurement? __state, Exception? __exception)
    { __state?.Finish(__exception == null); return __exception; }
    private static void DirectBegin(PathRequest request, out Measurement? __state) =>
        __state = ReferenceEquals(request.map, measuredMap) && current is { } samples
            ? new Measurement(samples, "wildlife-direct", request.pawn?.ThingID) : null;
    private static Exception? DirectEnd(PathRequest request, bool __result, Measurement? __state, Exception? __exception)
    { if (!__result || __exception != null) __state?.Finish(__exception == null && request.Found == true); return __exception; }
    private static void PreparationBegin(out long __state) =>
        __state = experimental != null && experimental.PreparationDepth++ == 0 ? Stopwatch.GetTimestamp() : 0;
    private static Exception? PreparationEnd(MethodBase __originalMethod, long __state, Exception? __exception)
    {
        if (experimental != null)
        {
            experimental.PreparationDepth--;
            if (__state != 0)
            {
                long elapsed = Stopwatch.GetTimestamp() - __state;
                experimental.Preparation += elapsed;
                if (__originalMethod.Name == "ApplySnapshot") experimental.Refresh += elapsed;
                else experimental.Graph += elapsed;
            }
        }
        return __exception;
    }

    private static void LockBegin(out long __state) => __state = experimental == null ? 0 : Stopwatch.GetTimestamp();
    private static Exception? LockEnd(long __state, Exception? __exception)
    {
        if (__state != 0 && experimental != null) experimental.LockWait += Stopwatch.GetTimestamp() - __state;
        return __exception;
    }

    [HarmonyPriority(Priority.First)]
    [HarmonyBefore("fumblesneeze.pathfindinglab")]
    private static void GatherBegin(Map ___map, out Measurement? __state) =>
        __state = ReferenceEquals(___map, measuredMap) && current is { } samples ? new Measurement(samples, "vanilla-gather") : null;
    private static Exception? SharedEnd(Measurement? __state, Exception? __exception)
    { __state?.Finish(__exception == null, preparationOnly: true); return __exception; }
    internal sealed record GridScope(SearchSamples? Previous, Measurement? Measurement);
    private static void GridBegin(PathFinder __instance, out GridScope __state)
    {
        var measurement = ReferenceEquals(__instance, measuredMap?.pathFinder) && current is { } samples
            ? new Measurement(samples, "vanilla-grid-schedule") : null;
        __state = new GridScope(gridWindow, measurement);
        gridWindow = measurement?.Samples;
    }
    private static Exception? GridEnd(GridScope __state, Exception? __exception)
    {
        gridWindow = __state.Previous;
        __state.Measurement?.Finish(__exception == null, preparationOnly: true);
        return __exception;
    }
    private static void WrapGridBatches(ref Func<int, int, Action> createAction)
    {
        if (gridWindow is not { } samples) return;
        var original = createAction;
        createAction = (start, end) =>
        {
            var action = original(start, end);
            samples.Scheduled();
            return () =>
            {
                var measurement = new Measurement(samples, "vanilla-grid-worker");
                bool success = false;
                try { action(); success = true; }
                finally { measurement.Finish(success, preparationOnly: true, completesWorker: true); }
            };
        };
    }
    private static IEnumerable<CodeInstruction> WrapDoorDispatch(IEnumerable<CodeInstruction> instructions)
    {
        var target = AccessTools.DeclaredMethod(typeof(PathFinder), "GetDoorBlockedJob")
            ?? throw new MissingMethodException("PathFinder.GetDoorBlockedJob");
        var state = typeof(PathFinder).GetNestedType("PathUniqueState", BindingFlags.NonPublic)
            ?? throw new MissingMemberException("PathFinder.PathUniqueState");
        if (target.IsStatic || target.ReturnType != typeof(ManagedJob) ||
            !target.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(PathRequest), state.MakeByRefType() }))
            throw new MissingMethodException("GetDoorBlockedJob return shape");
        var code = instructions.ToList();
        var sites = code.Select((instruction, index) => (instruction, index)).Where(x => x.instruction.Calls(target)).ToArray();
        if (sites.Length != 1) throw new InvalidOperationException("Expected exactly one native door-job dispatch site.");
        code.Insert(sites[0].index + 1, new CodeInstruction(OpCodes.Call,
            Required(typeof(SearchTimingProbe), nameof(InstrumentDoorJob), new[] { typeof(ManagedJob) })));
        return code;
    }
    private static ManagedJob InstrumentDoorJob(ManagedJob original)
    {
        // Main-thread association only. Worker wrapper keeps the native job plus immutable IDs.
        if (original.Job is not PathGridDoorsBlockedJob job)
        {
            original.Dispose();
            throw new InvalidOperationException("Native door-job type changed.");
        }
        if (!ReferenceEquals(job.map, measuredMap) || current is not { } samples) return original;
        var wrapper = new ManagedJob(new MeasuredDoorJob(job, new Work(samples, "vanilla-doors", job.pawn?.ThingID)));
        original.Dispose(); // Free the replaced GCHandle; native Run/Schedule owns the new one.
        samples.Scheduled();
        return wrapper;
    }
    private sealed class MeasuredDoorJob(PathGridDoorsBlockedJob job, Work work) : IJob
    {
        public void Execute()
        {
            var measurement = new Measurement(work.Samples, work.Source, work.PawnId);
            bool succeeded = false;
            try { job.Execute(); succeeded = true; }
            finally { measurement.Finish(succeeded, preparationOnly: true, completesWorker: true); }
        }
    }

    private static void StartMovement(Pawn_PathFollower __instance, Pawn ___pawn, out bool __state)
    {
        __state = current != null && ReferenceEquals(___pawn.Map, measuredMap);
        if (!__state) return;
        movements!.Start(___pawn.ThingID, Stopwatch.GetTimestamp(), __instance.Moving);
        starting.Add(__instance);
    }
    private static Exception? StartedMovement(Pawn_PathFollower __instance, Pawn ___pawn, bool __state, Exception? __exception)
    {
        if (__state)
        {
            starting.Remove(__instance);
            if (__exception != null || !__instance.Moving)
                movements!.Finish(___pawn.ThingID, Stopwatch.GetTimestamp(), Stopwatch.Frequency, __exception != null ? "exception" : "not-started");
        }
        return __exception;
    }
    private static void StoppedMovement(Pawn_PathFollower __instance, Pawn ___pawn)
    {
        if (current != null && !starting.Contains(__instance) && !paid.ContainsKey(__instance))
            movements!.Finish(___pawn.ThingID, Stopwatch.GetTimestamp(), Stopwatch.Frequency, "cancelled");
    }
    private static void PaidMovement(Pawn_PathFollower __instance, Pawn ___pawn, float __result)
    { if (current != null && __result > 0) paid[__instance] = movements!.Generation(___pawn.ThingID); }
    internal sealed record MovementTick(IntVec3 Position, long Generation);
    private static void MovementTickBegin(Pawn_PathFollower __instance, Pawn ___pawn, out MovementTick __state)
    { paid.Remove(__instance); __state = new MovementTick(___pawn.Position, movements?.Generation(___pawn.ThingID) ?? 0); }
    private static Exception? MovementTickEnd(Pawn_PathFollower __instance, Pawn ___pawn, MovementTick __state, Exception? __exception)
    {
        bool didPay = paid.TryGetValue(__instance, out long paidGeneration);
        paid.Remove(__instance);
        bool progressed = didPay || ___pawn.Position != __state.Position;
        if (current != null && (__exception != null || progressed))
            movements!.FinishGeneration(___pawn.ThingID, didPay ? paidGeneration : __state.Generation,
                Stopwatch.GetTimestamp(), Stopwatch.Frequency, __exception != null ? "exception" : "moved");
        return __exception;
    }
    public void Dispose()
    {
        EndJourney();
        FlushBackground();
        measuredMap = null;
        experimental = null;
        gridWindow = null;
        snapshotWindow = null;
        harmony.UnpatchAll(Owner);
        scheduled.Clear();
        measuredBackground.Clear();
        windowIds.Clear();
        starting.Clear();
        paid.Clear();
        // Cleanup runs on success, failure and cancellation. Incomplete workers stay explicit;
        // this diagnostic file never turns a failed native scenario into an accepted benchmark.
        if (windows.Count > 0)
        {
            string directory = Path.Combine(GenFilePaths.SaveDataFolderPath, "PathfindingLabMeasurements");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, probeId + "-background.json"), JsonSerializer.Serialize(background.Export(), new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(Path.Combine(directory, probeId + ".json"),
                JsonSerializer.Serialize(windows.Select(window => new
                {
                    Window = window.Id, Searches = window.Searches.Export(), DoorCoverage = window.Searches.DoorCoverage(), MovementDetailsOverflowed = window.Movements.Overflowed,
                    MovementOutcomes = window.Movements.Outcomes, window.Movements.LatencyTotal,
                    Movements = window.Movements.Snapshot()
                }), new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
