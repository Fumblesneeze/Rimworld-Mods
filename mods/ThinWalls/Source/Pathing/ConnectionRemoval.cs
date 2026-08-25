using Verse;

namespace ThinWalls.Pathing;

public readonly struct ConnectionRemoval
{
    public ConnectionRemoval(IntVec3 cell, CellConnection connection)
    {
        Cell = cell;
        Connection = connection;
    }

    public IntVec3 Cell { get; }

    public CellConnection Connection { get; }
}
