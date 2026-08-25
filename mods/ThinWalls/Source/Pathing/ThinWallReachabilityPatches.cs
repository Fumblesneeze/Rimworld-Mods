using System.Collections.Generic;
using System;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace ThinWalls.Pathing;

[HarmonyPatch(typeof(Reachability), nameof(Reachability.CanReach),
    typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms))]
public static class ThinWallReachabilityPatch
{
    [ThreadStatic]
    private static DoorRecoveryRequest? activeDoorRecovery;

    [ThreadStatic]
    private static Dictionary<DoorRecoveryRequest, bool>? verifiedDoorRecoveries;

    [ThreadStatic]
    private static Map? recoveryCacheMap;

    [ThreadStatic]
    private static int recoveryCacheTick;

    [ThreadStatic]
    private static int recoveryCacheVersion;

    public static void NotifyMapRemoved(Map map)
    {
        if (!ReferenceEquals(recoveryCacheMap, map))
        {
            return;
        }

        verifiedDoorRecoveries?.Clear();
        recoveryCacheMap = null;
        recoveryCacheTick = 0;
        recoveryCacheVersion = 0;
    }

    [HarmonyPostfix]
    public static void Postfix(
        IntVec3 start,
        LocalTargetInfo dest,
        PathEndMode peMode,
        TraverseParms traverseParams,
        ref bool __result,
        Map ___map)
    {
        ThinWallMapComponent component = ___map.GetComponent<ThinWallMapComponent>();
        if (!component.HasCompletedEdgeStructures)
        {
            return;
        }

        bool vanillaAccepted = __result;
        DoorRecoveryRequest recovery = default;
        if (!vanillaAccepted)
        {
            recovery = new DoorRecoveryRequest(
                start,
                dest,
                peMode,
                traverseParams,
                component.DoorAccessSignature(traverseParams));
            if (activeDoorRecovery.HasValue && activeDoorRecovery.Value.Equals(recovery))
            {
                // FindPathNow validates its PathRequest through Reachability.CanReach.
                // Only the exact request whose edge graph was already accepted receives
                // this provisional answer; the outer call still inspects the real path.
                __result = true;
                return;
            }

            PrepareRecoveryCache(___map, component.Version);
            if (verifiedDoorRecoveries!.TryGetValue(recovery, out bool cached))
            {
                __result = cached;
                return;
            }
        }

        bool edgeGraphAccepted = component.CanReachThroughEdges(start, dest, peMode, traverseParams);
        if (vanillaAccepted)
        {
            __result = ThinDoorAccessPolicy.RefineReachability(vanillaAccepted, edgeGraphAccepted);
            return;
        }

        if (!edgeGraphAccepted)
        {
            return;
        }

        PawnPath path;
        DoorRecoveryRequest? previousRecovery = activeDoorRecovery;
        try
        {
            activeDoorRecovery = recovery;
            path = ___map.pathFinder.FindPathNow(
                start,
                dest,
                traverseParams,
                peMode: peMode);
        }
        finally
        {
            activeDoorRecovery = previousRecovery;
        }

        try
        {
            __result = ThinDoorAccessPolicy.MayRecoverThinDoorRegionSplit(
                vanillaAccepted,
                edgeGraphAccepted,
                path.Found,
                PathCrossesThinDoor(___map, path));
            verifiedDoorRecoveries![recovery] = __result;
        }
        finally
        {
            path.Dispose();
        }
    }

    private static void PrepareRecoveryCache(Map map, int version)
    {
        int tick = Find.TickManager.TicksGame;
        if (!ReferenceEquals(recoveryCacheMap, map) ||
            recoveryCacheTick != tick ||
            recoveryCacheVersion != version)
        {
            verifiedDoorRecoveries ??= new Dictionary<DoorRecoveryRequest, bool>();
            verifiedDoorRecoveries.Clear();
            recoveryCacheMap = map;
            recoveryCacheTick = tick;
            recoveryCacheVersion = version;
        }
    }

    private static bool PathCrossesThinDoor(Map map, PawnPath path)
    {
        if (!path.Found)
        {
            return false;
        }

        List<IntVec3> nodes = path.NodesReversed;
        for (int index = 1; index < nodes.Count; index++)
        {
            if (ThinWallUtility.FirstCompletedDoor(map, nodes[index - 1], nodes[index]) != null)
            {
                return true;
            }
        }

        return false;
    }

    private readonly struct DoorRecoveryRequest : IEquatable<DoorRecoveryRequest>
    {
        public DoorRecoveryRequest(
            IntVec3 start,
            LocalTargetInfo destination,
            PathEndMode endMode,
            TraverseParms traverseParms,
            int doorAccessSignature)
        {
            Start = start;
            Destination = destination;
            EndMode = endMode;
            Pawn = traverseParms.pawn;
            Mode = traverseParms.mode;
            MaxDanger = traverseParms.maxDanger;
            CanBashDoors = traverseParms.canBashDoors;
            CanBashFences = traverseParms.canBashFences;
            AlwaysUseAvoidGrid = traverseParms.alwaysUseAvoidGrid;
            FenceBlocked = traverseParms.fenceBlocked;
            AvoidPersistentDanger = traverseParms.avoidPersistentDanger;
            AvoidDarknessDanger = traverseParms.avoidDarknessDanger;
            AvoidFog = traverseParms.avoidFog;
            TargetBuildable = traverseParms.targetBuildable;
            DoorAccessSignature = doorAccessSignature;
        }

        private IntVec3 Start { get; }

        private LocalTargetInfo Destination { get; }

        private PathEndMode EndMode { get; }

        private Pawn? Pawn { get; }

        private TraverseMode Mode { get; }

        private Danger MaxDanger { get; }

        private bool CanBashDoors { get; }

        private bool CanBashFences { get; }

        private bool AlwaysUseAvoidGrid { get; }

        private bool FenceBlocked { get; }

        private bool AvoidPersistentDanger { get; }

        private bool AvoidDarknessDanger { get; }

        private bool AvoidFog { get; }

        private CellRect TargetBuildable { get; }

        private int DoorAccessSignature { get; }

        public bool Equals(DoorRecoveryRequest other) =>
            Start == other.Start &&
            Destination == other.Destination &&
            EndMode == other.EndMode &&
            ReferenceEquals(Pawn, other.Pawn) &&
            Mode == other.Mode &&
            MaxDanger == other.MaxDanger &&
            CanBashDoors == other.CanBashDoors &&
            CanBashFences == other.CanBashFences &&
            AlwaysUseAvoidGrid == other.AlwaysUseAvoidGrid &&
            FenceBlocked == other.FenceBlocked &&
            AvoidPersistentDanger == other.AvoidPersistentDanger &&
            AvoidDarknessDanger == other.AvoidDarknessDanger &&
            AvoidFog == other.AvoidFog &&
            TargetBuildable.Equals(other.TargetBuildable) &&
            DoorAccessSignature == other.DoorAccessSignature;

        public override bool Equals(object? obj) => obj is DoorRecoveryRequest other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Start.GetHashCode();
                hash = (hash * 397) ^ Destination.GetHashCode();
                hash = (hash * 397) ^ (int)EndMode;
                hash = (hash * 397) ^ (Pawn?.thingIDNumber ?? 0);
                hash = (hash * 397) ^ (int)Mode;
                hash = (hash * 397) ^ (int)MaxDanger;
                hash = (hash * 397) ^ CanBashDoors.GetHashCode();
                hash = (hash * 397) ^ CanBashFences.GetHashCode();
                hash = (hash * 397) ^ AlwaysUseAvoidGrid.GetHashCode();
                hash = (hash * 397) ^ FenceBlocked.GetHashCode();
                hash = (hash * 397) ^ AvoidPersistentDanger.GetHashCode();
                hash = (hash * 397) ^ AvoidDarknessDanger.GetHashCode();
                hash = (hash * 397) ^ AvoidFog.GetHashCode();
                hash = (hash * 397) ^ TargetBuildable.GetHashCode();
                return (hash * 397) ^ DoorAccessSignature;
            }
        }
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

        if (!map.GetComponent<ThinWallMapComponent>().HasCompletedEdgeStructures)
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
                if (cell == start || map.GetComponent<ThinWallMapComponent>().AllowsStep(start, cell, parms))
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
                           map.GetComponent<ThinWallMapComponent>().AllowsStep(start, target.Cell, parms);
        }

        if (touchesTarget && !hasOpenTouch)
        {
            __result = false;
        }
    }
}
