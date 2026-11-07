using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using PathfindingLab.Core;
using RimWorld;
using Unity.Collections;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace PathfindingLab;

// Main-thread-only idle steering. Native jobs and the follower still move the animal.
public static class WildlifeWander
{
    private sealed class State
    {
        internal readonly WanderHeading Heading = new() { Direction = Rand.Range(0, 8) };
        internal Walk? Pending;
    }
    private sealed record Walk(Map Map, List<int>? Cells, IntVec3 Root, float Radius,
        bool Herd, Danger Danger, Func<Pawn, IntVec3, IntVec3, bool>? Validator);
    private static readonly ConditionalWeakTable<Pawn, State> states = new();
    private static readonly ConditionalWeakTable<Job, Walk> jobs = new();
    private static Action<PawnPath, NativeList<IntVec3>, int> initialize = null!;
    private static long planned, direct, rejected, regroup;
    public static object Diagnostics() => new { Planned = planned, Direct = direct, Rejected = rejected, Regroup = regroup };

    internal static void Install(Harmony harmony)
    {
        var init = AccessTools.DeclaredMethod(typeof(PawnPath), "Initialize", new[] { typeof(NativeList<IntVec3>), typeof(int) });
        if (init?.ReturnType != typeof(void)) throw new MissingMethodException("PawnPath.Initialize");
        initialize = AccessTools.MethodDelegate<Action<PawnPath, NativeList<IntVec3>, int>>(init);
        Patch(typeof(JobGiver_Wander), "GetExactWanderDest", new[] { typeof(Pawn) }, nameof(DestinationPrefix), false);
        Patch(typeof(JobGiver_Wander), "TryGiveJob", new[] { typeof(Pawn) }, nameof(JobPostfix), true);
        Patch(typeof(PathFinder), nameof(PathFinder.PushRequest), new[] { typeof(PathRequest) }, nameof(PushPrefix), false);
        Patch(typeof(Job), nameof(Job.Clear), Type.EmptyTypes, nameof(JobClearPrefix), false);
        void Patch(Type type, string method, Type[] args, string patch, bool postfix)
        {
            var original = AccessTools.DeclaredMethod(type, method, args) ?? throw new MissingMethodException(type.FullName, method);
            harmony.Patch(original, prefix: postfix ? null : new HarmonyMethod(typeof(WildlifeWander), patch),
                postfix: postfix ? new HarmonyMethod(typeof(WildlifeWander), patch) : null);
        }
    }

    private static bool Ordinary(Pawn pawn, ThinkNode? giver) => LabMod.Settings.Experimental && pawn.Spawned &&
        pawn.RaceProps.Animal && pawn.Faction == null && !pawn.Dead && !pawn.Downed && !pawn.InMentalState &&
        !pawn.Flying && !pawn.Swimming && !pawn.RaceProps.waterSeeker && pawn.mindState.duty == null && pawn.GetLord() == null &&
        (giver?.GetType() == typeof(JobGiver_WanderAnywhere) || giver?.GetType() == typeof(JobGiver_WanderHerd));

    private static bool DestinationPrefix(JobGiver_Wander __instance, Pawn pawn, ref IntVec3 __result,
        float ___wanderRadius, Danger ___maxDanger, bool ___canBashDoors, Func<Pawn, IntVec3, IntVec3, bool>? ___wanderDestValidator)
    {
        if (!Ordinary(pawn, __instance)) return true;
        var state = states.GetValue(pawn, _ => new State());
        state.Pending = null;
        bool herd = __instance.GetType() == typeof(JobGiver_WanderHerd);
        // Preserve native herd-mate selection (including reachability and avoiding humans).
        var root = herd ? (IntVec3)AccessTools.Method(typeof(JobGiver_WanderHerd), "GetWanderRoot").Invoke(__instance, new object[] { pawn })! : pawn.Position;
        if (!root.IsValid) return true;
        var walk = new Walk(pawn.Map, null, root, ___wanderRadius, herd,
            PawnUtility.ResolveMaxDanger(pawn, ___maxDanger), ___wanderDestValidator);
        if (herd && !DirectionalWalk.WithinCircle(pawn.Map.Size.x, pawn.Map.cellIndices.CellToIndex(pawn.Position),
                pawn.Map.cellIndices.CellToIndex(root), ___wanderRadius))
        {
            state.Pending = walk; // Explicit native regroup, not an idle shortcut.
            regroup++;
            __result = RCellFinder.RandomWanderDestFor(pawn, root, ___wanderRadius, ___wanderDestValidator, walk.Danger, ___canBashDoors);
            return false;
        }
        var cells = DirectionalWalk.Build(pawn.Map.Size.x, pawn.Map.Size.z, pawn.Map.cellIndices.CellToIndex(pawn.Position),
            Rand.RangeInclusive(3, 10), state.Heading, (from, to) => CanStep(pawn, walk, from, to), max => Rand.Range(0, max));
        // A trapped animal retains native wait cadence, rather than scheduling a global route.
        if (cells.Count < 4) { __result = pawn.Position; return false; }
        state.Pending = walk with { Cells = cells };
        __result = pawn.Map.cellIndices.IndexToCell(cells[^1]);
        planned++;
        return false;
    }

    private static void JobPostfix(Pawn pawn, Job? __result)
    {
        if (__result != null) jobs.Remove(__result); // Jobs are pooled, including native regroup jobs.
        if (!states.TryGetValue(pawn, out var state)) return;
        var walk = state.Pending;
        state.Pending = null;
        if (__result?.def != JobDefOf.GotoWander || walk == null) return;
        if (walk.Cells != null && __result.targetA.Cell != walk.Map.cellIndices.IndexToCell(walk.Cells[^1])) return;
        jobs.Add(__result, walk);
    }
    private static void JobClearPrefix(Job __instance) => jobs.Remove(__instance);

    private static bool PushPrefix(PathRequest request)
    {
        var pawn = request.pawn;
        var job = pawn?.CurJob;
        if (pawn == null || job?.def != JobDefOf.GotoWander || !Ordinary(pawn, job.jobGiver) ||
            request.EndMode != PathEndMode.OnCell || request.Target.HasThing || request.customizer != null) return true;
        jobs.TryGetValue(job, out var walk);
        if (walk != null && walk.Cells == null) return true; // Purposeful herd regroup.
        // Save/load, a changed job target or a blocked/replaced short path means rethink,
        // not a destination search. The native follower ends this idle job on not-found.
        if (walk == null || walk.Map != pawn.Map || request.Target.Cell != pawn.Map.cellIndices.IndexToCell(walk.Cells![^1]))
            return Reject(request);
        int progress = walk.Cells!.IndexOf(pawn.Map.cellIndices.CellToIndex(request.Start));
        if (progress < 0) return Reject(request);
        for (int i = progress + 1; i < walk.Cells.Count; i++)
            if (!CanStep(pawn, walk, walk.Cells[i - 1], walk.Cells[i])) return Reject(request);
        var path = pawn.Map.pawnPathPool.GetPath();
        try
        {
            using var nodes = new NativeList<IntVec3>(walk.Cells.Count - progress, Allocator.Temp);
            int cost = 0;
            for (int i = walk.Cells.Count - 1; i >= progress; i--)
            {
                var cell = pawn.Map.cellIndices.IndexToCell(walk.Cells[i]);
                nodes.Add(cell);
                if (i > progress)
                {
                    var previous = pawn.Map.cellIndices.IndexToCell(walk.Cells[i - 1]);
                    cost += (int)(previous.x != cell.x && previous.z != cell.z ? pawn.TicksPerMoveDiagonal : pawn.TicksPerMoveCardinal)
                        + pawn.Map.pathing.For(pawn).pathGrid.Cost(cell);
                }
            }
            initialize(path, nodes, cost);
            request.Resolve(path);
            direct++;
            return false;
        }
        catch { path.ReleaseToPool(); throw; }
    }

    private static bool Reject(PathRequest request) { rejected++; request.Resolve(PawnPath.NotFound); return false; }

    private static bool CanStep(Pawn pawn, Walk walk, int from, int to)
    {
        var map = pawn.Map;
        var a = map.cellIndices.IndexToCell(from); var b = map.cellIndices.IndexToCell(to);
        if (!b.InBounds(map) || Math.Abs(a.x - b.x) > 1 || Math.Abs(a.z - b.z) > 1 || a == b) return false;
        if (walk.Herd && !DirectionalWalk.WithinCircle(map.Size.x, to, map.cellIndices.CellToIndex(walk.Root), walk.Radius)) return false;
        if (!Safe(b) || PawnUtility.PawnBlockingPathAt(b, pawn) != null) return false;
        if (a.x != b.x && a.z != b.z)
        {
            var x = new IntVec3(b.x, 0, a.z); var z = new IntVec3(a.x, 0, b.z);
            if (!Safe(x) || !Safe(z) || x.GetEdifice(map) != null || z.GetEdifice(map) != null) return false;
        }
        return true;

        bool Safe(IntVec3 c)
        {
            if (!c.WalkableBy(map, pawn) || !c.Standable(map) || c.IsForbidden(pawn) || c.GetDoor(map) != null ||
                c.GetEdifice(map)?.def.IsFence == true || c.ContainsStaticFire(map) || PawnUtility.KnownDangerAt(c, map, pawn) ||
                c.GetDangerFor(pawn, map) > walk.Danger || c.GetDangerFor(pawn, map) == Danger.Deadly ||
                !map.pawnDestinationReservationManager.CanReserve(c, pawn)) return false;
            var terrain = c.GetTerrain(map);
            if (terrain.dangerous || terrain.avoidWander && (!terrain.IsWater || !pawn.RaceProps.waterSeeker)) return false;
            if (pawn.genes != null && !pawn.genes.EnjoysSunlight && c.InSunlight(map)) return false;
            if (pawn.GetStatValue(StatDefOf.ToxicEnvironmentResistance) < .5f &&
                (ModsConfig.BiotechActive && c.IsPolluted(map) || c.AnyGas(map, GasType.RotStink))) return false;
            return walk.Validator == null || walk.Validator(pawn, c, walk.Root);
        }
    }
}
