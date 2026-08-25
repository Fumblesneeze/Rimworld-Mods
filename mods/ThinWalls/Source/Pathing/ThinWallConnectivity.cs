using System.Collections.Generic;
using ThinWalls.Geometry;
using Verse;

namespace ThinWalls.Pathing;

public static class ThinWallConnectivity
{
    public static IReadOnlyList<ConnectionRemoval> Removals(SharedEdge edge)
    {
        return edge.PositiveSide switch
        {
            ThinWallSide.North => HorizontalRemovals(edge.AnchorCell),
            ThinWallSide.East => VerticalRemovals(edge.AnchorCell),
            _ => throw new System.ArgumentException("A shared edge must use its canonical north or east side.", nameof(edge)),
        };
    }

    private static IReadOnlyList<ConnectionRemoval> HorizontalRemovals(IntVec3 cell)
    {
        IntVec3 north = Offset(cell, 0, 1);
        return new[]
        {
            new ConnectionRemoval(cell, CellConnection.North),
            new ConnectionRemoval(north, CellConnection.South),
            new ConnectionRemoval(cell, CellConnection.NorthEast),
            new ConnectionRemoval(north, CellConnection.SouthWest),
            new ConnectionRemoval(cell, CellConnection.NorthWest),
            new ConnectionRemoval(north, CellConnection.SouthEast),
            new ConnectionRemoval(Offset(cell, -1, 0), CellConnection.NorthEast),
            new ConnectionRemoval(Offset(cell, -1, 1), CellConnection.SouthEast),
            new ConnectionRemoval(Offset(cell, 1, 0), CellConnection.NorthWest),
            new ConnectionRemoval(Offset(cell, 1, 1), CellConnection.SouthWest),
        };
    }

    private static IReadOnlyList<ConnectionRemoval> VerticalRemovals(IntVec3 cell)
    {
        IntVec3 east = Offset(cell, 1, 0);
        return new[]
        {
            new ConnectionRemoval(cell, CellConnection.East),
            new ConnectionRemoval(east, CellConnection.West),
            new ConnectionRemoval(cell, CellConnection.NorthEast),
            new ConnectionRemoval(east, CellConnection.SouthWest),
            new ConnectionRemoval(cell, CellConnection.SouthEast),
            new ConnectionRemoval(east, CellConnection.NorthWest),
            new ConnectionRemoval(Offset(cell, 0, -1), CellConnection.NorthEast),
            new ConnectionRemoval(Offset(cell, 1, -1), CellConnection.NorthWest),
            new ConnectionRemoval(Offset(cell, 0, 1), CellConnection.SouthEast),
            new ConnectionRemoval(Offset(cell, 1, 1), CellConnection.SouthWest),
        };
    }

    private static IntVec3 Offset(IntVec3 cell, int x, int z) => new(cell.x + x, cell.y, cell.z + z);
}
