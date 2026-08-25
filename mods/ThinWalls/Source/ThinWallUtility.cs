using System.Collections.Generic;
using System.Linq;
using RimWorld;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using Verse;

namespace ThinWalls;

public static class ThinWallUtility
{
    public const string ThinWallDefName = "TW_ThinWall";
    public const string ThinDoorDefName = "TW_ThinDoor";
    public const string ThinWallBashJobDefName = "TW_AttackThinWallEdge";

    public static bool IsThinWallDef(BuildableDef? def)
    {
        return def?.defName == ThinWallDefName ||
               (def as ThingDef)?.entityDefToBuild?.defName == ThinWallDefName;
    }

    public static bool IsThinDoorDef(BuildableDef? def)
    {
        return def?.defName == ThinDoorDefName ||
               (def as ThingDef)?.entityDefToBuild?.defName == ThinDoorDefName;
    }

    public static bool IsThinEdgeDef(BuildableDef? def)
    {
        return IsThinWallDef(def) || IsThinDoorDef(def);
    }

    public static bool TryGetOwnedEdge(Thing thing, out OwnedEdge edge)
    {
        if (thing != null && IsThinEdgeDef(thing.def))
        {
            edge = new OwnedEdge(thing.Position, (ThinWallSide)thing.Rotation.AsInt);
            return true;
        }

        edge = default;
        return false;
    }

    public static IEnumerable<Thing> ThingsOnSharedEdge(Map map, SharedEdge edge, bool completedOnly)
    {
        foreach (OwnedEdge owner in Owners(edge))
        {
            if (!owner.Cell.InBounds(map))
            {
                continue;
            }

            foreach (Thing thing in owner.Cell.GetThingList(map))
            {
                if (TryGetOwnedEdge(thing, out OwnedEdge actual) && actual.Equals(owner) &&
                    (!completedOnly || thing is IThinEdgeStructure))
                {
                    yield return thing;
                }
            }
        }
    }

    public static bool HasWall(Map map, SharedEdge edge, bool completedOnly = true)
    {
        var first = new OwnedEdge(edge.AnchorCell, edge.PositiveSide);
        if (CellHasOwner(map, first, completedOnly))
        {
            return true;
        }

        var second = edge.PositiveSide == ThinWallSide.North
            ? new OwnedEdge(new IntVec3(edge.AnchorCell.x, edge.AnchorCell.y, edge.AnchorCell.z + 1), ThinWallSide.South)
            : new OwnedEdge(new IntVec3(edge.AnchorCell.x + 1, edge.AnchorCell.y, edge.AnchorCell.z), ThinWallSide.West);
        return CellHasOwner(map, second, completedOnly);
    }

    public static bool HasEdgeStructure(Map map, SharedEdge edge, bool completedOnly = true)
    {
        foreach (Thing thing in ThingsOnSharedEdge(map, edge, completedOnly))
        {
            if (IsThinEdgeDef(thing.def))
            {
                return true;
            }
        }

        return false;
    }

    public static Building_ThinDoor? FirstCompletedDoor(Map map, IntVec3 from, IntVec3 to)
    {
        foreach (SharedEdge edge in SharedEdgesCrossed(from, to))
        {
            Building_ThinDoor? door = ThingsOnSharedEdge(map, edge, completedOnly: true)
                .OfType<Building_ThinDoor>()
                .OrderBy(candidate => candidate.thingIDNumber)
                .FirstOrDefault();
            if (door != null)
            {
                return door;
            }
        }

        return null;
    }

    public static Building_ThinWall? FirstCompletedBlocker(Map map, IntVec3 from, IntVec3 to)
    {
        int deltaX = to.x - from.x;
        int deltaZ = to.z - from.z;
        if (System.Math.Abs(deltaX) > 1 || System.Math.Abs(deltaZ) > 1 || (deltaX == 0 && deltaZ == 0))
        {
            return null;
        }

        Building_ThinWall? blocker;
        if (deltaX != 0 && (blocker = FirstCompletedOnEdge(
                map,
                new OwnedEdge(from, deltaX > 0 ? ThinWallSide.East : ThinWallSide.West).Shared)) != null)
        {
            return blocker;
        }

        if (deltaZ != 0 && (blocker = FirstCompletedOnEdge(
                map,
                new OwnedEdge(from, deltaZ > 0 ? ThinWallSide.North : ThinWallSide.South).Shared)) != null)
        {
            return blocker;
        }

        if (deltaX == 0 || deltaZ == 0)
        {
            return null;
        }

        var verticalOwner = new IntVec3(from.x, from.y, from.z + deltaZ);
        blocker = FirstCompletedOnEdge(
            map,
            new OwnedEdge(verticalOwner, deltaX > 0 ? ThinWallSide.East : ThinWallSide.West).Shared);
        if (blocker != null)
        {
            return blocker;
        }

        var horizontalOwner = new IntVec3(from.x + deltaX, from.y, from.z);
        return FirstCompletedOnEdge(
            map,
            new OwnedEdge(horizontalOwner, deltaZ > 0 ? ThinWallSide.North : ThinWallSide.South).Shared);
    }

    public static bool BlocksStep(Map map, IntVec3 from, IntVec3 to)
    {
        int deltaX = to.x - from.x;
        int deltaZ = to.z - from.z;
        if (System.Math.Abs(deltaX) > 1 || System.Math.Abs(deltaZ) > 1 || (deltaX == 0 && deltaZ == 0))
        {
            return false;
        }

        if (deltaX != 0 && HasWall(
                map,
                new OwnedEdge(from, deltaX > 0 ? ThinWallSide.East : ThinWallSide.West).Shared))
        {
            return true;
        }

        if (deltaZ != 0 && HasWall(
                map,
                new OwnedEdge(from, deltaZ > 0 ? ThinWallSide.North : ThinWallSide.South).Shared))
        {
            return true;
        }

        if (deltaX == 0 || deltaZ == 0)
        {
            return false;
        }

        var verticalOwner = new IntVec3(from.x, from.y, from.z + deltaZ);
        if (HasWall(
                map,
                new OwnedEdge(verticalOwner, deltaX > 0 ? ThinWallSide.East : ThinWallSide.West).Shared))
        {
            return true;
        }

        var horizontalOwner = new IntVec3(from.x + deltaX, from.y, from.z);
        return HasWall(
            map,
            new OwnedEdge(horizontalOwner, deltaZ > 0 ? ThinWallSide.North : ThinWallSide.South).Shared);
    }

    public static IEnumerable<SharedEdge> SharedEdgesCrossed(IntVec3 from, IntVec3 to)
    {
        int deltaX = to.x - from.x;
        int deltaZ = to.z - from.z;
        if (System.Math.Abs(deltaX) > 1 || System.Math.Abs(deltaZ) > 1 || (deltaX == 0 && deltaZ == 0))
        {
            yield break;
        }

        if (deltaX > 0)
        {
            yield return new OwnedEdge(from, ThinWallSide.East).Shared;
        }
        else if (deltaX < 0)
        {
            yield return new OwnedEdge(from, ThinWallSide.West).Shared;
        }

        if (deltaZ > 0)
        {
            yield return new OwnedEdge(from, ThinWallSide.North).Shared;
        }
        else if (deltaZ < 0)
        {
            yield return new OwnedEdge(from, ThinWallSide.South).Shared;
        }

        if (deltaX != 0 && deltaZ != 0)
        {
            // A diagonal cell-center move crosses one grid vertex. Every wall edge
            // incident to that vertex owns the endpoint and must prevent corner slip.
            IntVec3 verticalOwner = new(from.x, from.y, from.z + deltaZ);
            yield return new OwnedEdge(
                verticalOwner,
                deltaX > 0 ? ThinWallSide.East : ThinWallSide.West).Shared;

            IntVec3 horizontalOwner = new(from.x + deltaX, from.y, from.z);
            yield return new OwnedEdge(
                horizontalOwner,
                deltaZ > 0 ? ThinWallSide.North : ThinWallSide.South).Shared;
        }
    }

    public static bool StepCrossesEdge(IntVec3 from, IntVec3 to, SharedEdge edge)
    {
        int deltaX = to.x - from.x;
        int deltaZ = to.z - from.z;
        if (System.Math.Abs(deltaX) > 1 || System.Math.Abs(deltaZ) > 1 || (deltaX == 0 && deltaZ == 0))
        {
            return false;
        }

        if (deltaX != 0 && edge.Equals(
                new OwnedEdge(from, deltaX > 0 ? ThinWallSide.East : ThinWallSide.West).Shared))
        {
            return true;
        }

        if (deltaZ != 0 && edge.Equals(
                new OwnedEdge(from, deltaZ > 0 ? ThinWallSide.North : ThinWallSide.South).Shared))
        {
            return true;
        }

        if (deltaX == 0 || deltaZ == 0)
        {
            return false;
        }

        var verticalOwner = new IntVec3(from.x, from.y, from.z + deltaZ);
        if (edge.Equals(new OwnedEdge(
                verticalOwner,
                deltaX > 0 ? ThinWallSide.East : ThinWallSide.West).Shared))
        {
            return true;
        }

        var horizontalOwner = new IntVec3(from.x + deltaX, from.y, from.z);
        return edge.Equals(new OwnedEdge(
            horizontalOwner,
            deltaZ > 0 ? ThinWallSide.North : ThinWallSide.South).Shared);
    }

    public static IEnumerable<OwnedEdge> Owners(SharedEdge edge)
    {
        yield return new OwnedEdge(edge.AnchorCell, edge.PositiveSide);
        if (edge.PositiveSide == ThinWallSide.North)
        {
            yield return new OwnedEdge(
                new IntVec3(edge.AnchorCell.x, edge.AnchorCell.y, edge.AnchorCell.z + 1),
                ThinWallSide.South);
        }
        else
        {
            yield return new OwnedEdge(
                new IntVec3(edge.AnchorCell.x + 1, edge.AnchorCell.y, edge.AnchorCell.z),
                ThinWallSide.West);
        }
    }

    private static bool CellHasOwner(Map map, OwnedEdge owner, bool completedOnly)
    {
        if (!owner.Cell.InBounds(map))
        {
            return false;
        }

        List<Thing> things = owner.Cell.GetThingList(map);
        for (int index = 0; index < things.Count; index++)
        {
            Thing thing = things[index];
            if (TryGetOwnedEdge(thing, out OwnedEdge actual) && actual.Equals(owner) &&
                IsThinWallDef(thing.def) && (!completedOnly || thing is Building_ThinWall))
            {
                return true;
            }
        }

        return false;
    }

    private static Building_ThinWall? FirstCompletedOnEdge(Map map, SharedEdge edge)
    {
        Building_ThinWall? first = FirstCompletedOwner(map, new OwnedEdge(edge.AnchorCell, edge.PositiveSide));
        var opposite = edge.PositiveSide == ThinWallSide.North
            ? new OwnedEdge(new IntVec3(edge.AnchorCell.x, edge.AnchorCell.y, edge.AnchorCell.z + 1), ThinWallSide.South)
            : new OwnedEdge(new IntVec3(edge.AnchorCell.x + 1, edge.AnchorCell.y, edge.AnchorCell.z), ThinWallSide.West);
        Building_ThinWall? second = FirstCompletedOwner(map, opposite);
        if (first == null || second?.thingIDNumber < first.thingIDNumber)
        {
            return second;
        }

        return first;
    }

    private static Building_ThinWall? FirstCompletedOwner(Map map, OwnedEdge owner)
    {
        if (!owner.Cell.InBounds(map))
        {
            return null;
        }

        Building_ThinWall? first = null;
        List<Thing> things = owner.Cell.GetThingList(map);
        for (int index = 0; index < things.Count; index++)
        {
            if (things[index] is Building_ThinWall wall && wall.OwnedEdge.Equals(owner) &&
                (first == null || wall.thingIDNumber < first.thingIDNumber))
            {
                first = wall;
            }
        }

        return first;
    }
}
