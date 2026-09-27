using ThinWalls.Buildings;
using Verse;
using Verse.AI;

namespace ThinWalls.Pathing;

public static class ThinDoorAccessPolicy
{
    public static bool PermitsPassage(bool canPhysicallyPass, bool forbidden) =>
        canPhysicallyPass && !forbidden;

    public static bool ShouldRefreshFriendlyTouch(bool pawnSpawned, bool crossesOwnedEdge) =>
        pawnSpawned && crossesOwnedEdge;

    public static int CorrectOwnerCellCountdown(int previousTicks, bool holdOpen)
    {
        if (previousTicks <= 0)
        {
            return 0;
        }

        return holdOpen ? previousTicks : previousTicks - 1;
    }

    public static bool CanTraverse(Building_ThinDoor door, Pawn pawn) =>
        PermitsPassage(door.CanPhysicallyPass(pawn), door.IsForbiddenToPass(pawn));

    public static bool CanTraverse(Building_ThinDoor door, TraverseParms traverseParms)
    {
        if (traverseParms.mode == TraverseMode.NoPassClosedDoors ||
            traverseParms.mode == TraverseMode.NoPassClosedDoorsOrWater) return door.FreePassage;
        if (traverseParms.pawn != null)
        {
            return CanTraverse(door, traverseParms.pawn);
        }

        return true;
    }
}
