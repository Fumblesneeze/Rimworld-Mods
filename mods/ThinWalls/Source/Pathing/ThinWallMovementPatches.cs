using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ThinWalls.Pathing;

[HarmonyPatch]
public static class ThinWallMovementPatches
{
    [HarmonyPatch(typeof(Pawn_PathFollower), "SetupMoveIntoNextCell")]
    [HarmonyPostfix]
    public static void SetupMoveIntoNextCellPostfix(Pawn ___pawn, Pawn_PathFollower __instance)
    {
        if (___pawn?.Spawned == true)
        {
            BlockOrBash(___pawn, __instance);
        }
    }

    [HarmonyPatch(typeof(Pawn_PathFollower), "TryEnterNextPathCell")]
    [HarmonyPrefix]
    public static bool TryEnterNextPathCellPrefix(Pawn ___pawn, Pawn_PathFollower __instance)
    {
        if (___pawn?.Spawned != true)
        {
            return true;
        }

        if (WaitForCrossedThinDoor(___pawn, __instance))
        {
            return false;
        }

        return !BlockOrBash(___pawn, __instance);
    }

    private static bool WaitForCrossedThinDoor(Pawn pawn, Pawn_PathFollower pathFollower)
    {
        Buildings.Building_ThinDoor? door =
            ThinWallUtility.FirstCompletedDoor(pawn.Map, pawn.Position, pathFollower.nextCell);
        if (door == null)
        {
            return false;
        }

        if (!ThinDoorAccessPolicy.CanTraverse(door, pawn))
        {
            if (pawn.CurJob?.canBashDoors ?? false)
            {
                return false;
            }

            pathFollower.ResetToCurrentPosition();
            return true;
        }

        if (door.Open && door.TicksTillFullyOpened <= 0)
        {
            return false;
        }

        if (!door.Open)
        {
            door.StartManualOpenBy(pawn);
        }
        pawn.stances.SetStance(new Stance_Cooldown(door.TicksTillFullyOpened, door, null)
        {
            neverAimWeapon = true,
        });
        door.CheckFriendlyTouched(pawn);
        return true;
    }

    private static bool BlockOrBash(Pawn pawn, Pawn_PathFollower pathFollower)
    {
        IntVec3 attemptedNextCell = pathFollower.nextCell;
        Building? blocker =
            ThinWallUtility.FirstCompletedBlocker(pawn.Map, pawn.Position, attemptedNextCell);
        if (blocker == null)
        {
            Buildings.Building_ThinDoor? door =
                ThinWallUtility.FirstCompletedDoor(pawn.Map, pawn.Position, attemptedNextCell);
            if (door != null && !ThinDoorAccessPolicy.CanTraverse(door, pawn))
            {
                blocker = door;
            }
        }
        if (blocker == null)
        {
            return false;
        }

        pathFollower.ResetToCurrentPosition();
        if (pawn.CurJob?.canBashDoors ?? false)
        {
            JobDef jobDef = DefDatabase<JobDef>.GetNamed(ThinWallUtility.ThinWallBashJobDefName);
            Job job = JobMaker.MakeJob(jobDef, blocker, attemptedNextCell);
            job.expiryInterval = 1_200;
            pawn.jobs.StartJob(job, JobCondition.Incompletable);
        }

        return true;
    }
}

[HarmonyPatch]
public static class ThinDoorMovementPatches
{
    [HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.NextCellDoorToWaitForOrManuallyOpen))]
    [HarmonyPostfix]
    public static void NextCellDoorPostfix(
        Pawn ___pawn,
        IntVec3 ___nextCell,
        ref Building_Door __result)
    {
        if (___pawn?.Spawned != true)
        {
            return;
        }

        if (__result is Buildings.Building_ThinDoor returnedThinDoor)
        {
            if (!ThinWallUtility.StepCrossesEdge(
                    ___pawn.Position,
                    ___nextCell,
                    returnedThinDoor.OwnedEdge.Shared) ||
                !ThinDoorAccessPolicy.CanTraverse(returnedThinDoor, ___pawn))
            {
                __result = null!;
            }
            return;
        }
        if (__result != null)
        {
            return;
        }

        Buildings.Building_ThinDoor? door =
            ThinWallUtility.FirstCompletedDoor(___pawn.Map, ___pawn.Position, ___nextCell);
        if (door != null && ThinDoorAccessPolicy.CanTraverse(door, ___pawn) && door.SlowsPawns &&
            (!door.Open || door.TicksTillFullyOpened > 0) && door.PawnCanOpen(___pawn))
        {
            __result = door;
        }
    }

    [HarmonyPatch(typeof(Building_Door), nameof(Building_Door.BlockedOpenMomentary), MethodType.Getter)]
    [HarmonyPostfix]
    public static void BlockedOpenMomentaryPostfix(Building_Door __instance, ref bool __result)
    {
        if (__instance is Buildings.Building_ThinDoor)
        {
            __result = false;
        }
    }

    [HarmonyPatch(typeof(Building_Door), nameof(Building_Door.CheckFriendlyTouched))]
    [HarmonyPrefix]
    public static bool CheckFriendlyTouchedPrefix(Building_Door __instance, Pawn p)
    {
        if (__instance is not Buildings.Building_ThinDoor door)
        {
            return true;
        }

        bool crossesOwnedEdge = p?.Spawned == true && ThinWallUtility.StepCrossesEdge(
            p.Position,
            p.pather.nextCell,
            door.OwnedEdge.Shared);
        return ThinDoorAccessPolicy.ShouldRefreshFriendlyTouch(p?.Spawned == true, crossesOwnedEdge);
    }

    public readonly struct ThinDoorTickState
    {
        public ThinDoorTickState(int ticksUntilClose, bool crossingPawnOccupiesOwnerCell)
        {
            TicksUntilClose = ticksUntilClose;
            CrossingPawnOccupiesOwnerCell = crossingPawnOccupiesOwnerCell;
        }

        public int TicksUntilClose { get; }

        public bool CrossingPawnOccupiesOwnerCell { get; }
    }

    [HarmonyPatch(typeof(Building_Door), "Tick")]
    [HarmonyPrefix]
    public static void TickPrefix(Building_Door __instance, int ___ticksUntilClose, out ThinDoorTickState __state)
    {
        if (__instance is not Buildings.Building_ThinDoor door || !door.Spawned)
        {
            __state = default;
            return;
        }

        bool crossingPawn = door.Position.GetThingList(door.Map)
            .OfType<Pawn>()
            .Any(pawn => pawn.Spawned && ThinWallUtility.StepCrossesEdge(
                pawn.Position,
                pawn.pather.nextCell,
                door.OwnedEdge.Shared));
        __state = new ThinDoorTickState(___ticksUntilClose, crossingPawn);
    }

    [HarmonyPatch(typeof(Building_Door), "Tick")]
    [HarmonyPostfix]
    public static void TickPostfix(
        Building_Door __instance,
        ref int ___ticksUntilClose,
        ThinDoorTickState __state)
    {
        if (__instance is not Buildings.Building_ThinDoor door ||
            __state.CrossingPawnOccupiesOwnerCell ||
            __state.TicksUntilClose <= 0 ||
            !door.Open)
        {
            return;
        }

        ___ticksUntilClose = ThinDoorAccessPolicy.CorrectOwnerCellCountdown(
            __state.TicksUntilClose,
            door.HoldOpen);
        if (___ticksUntilClose <= 0 && !door.HoldOpen && !door.TryCloseAfterEdgeCountdown())
        {
            ___ticksUntilClose = 1;
        }
    }
}
