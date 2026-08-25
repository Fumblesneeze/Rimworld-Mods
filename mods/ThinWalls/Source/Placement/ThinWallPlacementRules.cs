using System.Collections.Generic;
using System.Linq;
using RimWorld;
using ThinWalls.Geometry;
using Verse;

namespace ThinWalls.Placement;

public static class ThinWallPlacementRules
{
    public static AcceptanceReport Validate(BuildableDef placingDef, IntVec3 location, Rot4 rotation, Map map)
    {
        if (ThinWallUtility.IsThinEdgeDef(placingDef))
        {
            var owned = new OwnedEdge(location, (ThinWallSide)rotation.AsInt);
            if (ThinWallUtility.Owners(owned.Shared).Any(owner => !owner.Cell.InBounds(map)))
            {
                return "OutOfBounds".Translate();
            }

            foreach (Thing thing in ThinWallUtility.Owners(owned.Shared)
                         .SelectMany(owner => owner.Cell.GetThingList(map))
                         .Distinct())
            {
                if (ThinWallUtility.TryGetOwnedEdge(thing, out OwnedEdge existing) &&
                    ConflictsOnSharedEdge(existing, owned))
                {
                    return "TW_AlreadyOnEdge".Translate();
                }

                if (BlocksThinWallPlacement(thing, owned))
                {
                    return "TW_WallCrossesBuilding".Translate();
                }
            }

            return AcceptanceReport.WasAccepted;
        }

        CellRect occupied = GenAdj.OccupiedRect(location, rotation, placingDef.Size);
        foreach (SharedEdge edge in InternalEdges(occupied))
        {
            if (ThinWallUtility.HasEdgeStructure(map, edge, completedOnly: false))
            {
                return "TW_BuildingCrossesWall".Translate();
            }
        }

        return AcceptanceReport.WasAccepted;
    }

    public static IEnumerable<SharedEdge> InternalEdges(CellRect occupied)
    {
        for (int x = occupied.minX; x < occupied.maxX; x++)
        {
            for (int z = occupied.minZ; z <= occupied.maxZ; z++)
            {
                yield return new SharedEdge(new IntVec3(x, 0, z), ThinWallSide.East);
            }
        }

        for (int z = occupied.minZ; z < occupied.maxZ; z++)
        {
            for (int x = occupied.minX; x <= occupied.maxX; x++)
            {
                yield return new SharedEdge(new IntVec3(x, 0, z), ThinWallSide.North);
            }
        }
    }

    public static bool FootprintCrossesEdge(CellRect footprint, SharedEdge edge)
    {
        IntVec3 first = edge.AnchorCell;
        IntVec3 second = edge.PositiveSide == ThinWallSide.North
            ? new IntVec3(first.x, first.y, first.z + 1)
            : new IntVec3(first.x + 1, first.y, first.z);
        return footprint.Contains(first) && footprint.Contains(second);
    }

    public static bool ConflictsOnSharedEdge(OwnedEdge candidate, OwnedEdge existing) =>
        candidate.Shared.Equals(existing.Shared);

    public static bool MayIgnoreBlockingThing(
        BuildableDef placingDef,
        IntVec3 location,
        Rot4 rotation,
        Thing thing)
    {
        if (ThinWallUtility.IsThinEdgeDef(thing?.def))
        {
            return true;
        }

        if (!ThinWallUtility.IsThinEdgeDef(placingDef) || thing == null)
        {
            return false;
        }

        var owned = new OwnedEdge(location, (ThinWallSide)rotation.AsInt);
        return !BlocksThinWallPlacement(thing, owned);
    }

    private static bool BlocksThinWallPlacement(Thing thing, OwnedEdge owned)
    {
        if (ThinWallUtility.IsThinEdgeDef(thing.def))
        {
            return false;
        }

        BuildableDef builtDef = thing.def.entityDefToBuild ?? thing.def;
        CellRect footprint = GenAdj.OccupiedRect(thing.Position, thing.Rotation, builtDef.Size);
        if (FootprintCrossesEdge(footprint, owned.Shared))
        {
            return true;
        }

        return false;
    }
}
