using System.Collections.Generic;
using ThinWalls.Geometry;
using Verse;

namespace ThinWalls.Pathing;

/// <summary>Only cells incident to owned edges are retained; shared diagonal bits are reference counted.</summary>
public sealed class SparseEdgeMasks
{
    private readonly HashSet<SharedEdge> edges = new();
    private readonly Dictionary<IntVec3, int[]> counts = new();
    private readonly Dictionary<IntVec3, CellConnection> cells = new();

    public IReadOnlyDictionary<IntVec3, CellConnection> Cells => cells;

    public CellConnection At(IntVec3 cell) => cells.TryGetValue(cell, out var mask) ? mask : CellConnection.Self;

    public void Set(SharedEdge edge, bool present)
    {
        if (!(present ? edges.Add(edge) : edges.Remove(edge))) return;
        foreach (ConnectionRemoval removal in ThinWallConnectivity.Removals(edge))
        {
            if (!counts.TryGetValue(removal.Cell, out int[] bits))
            {
                bits = new int[8];
                counts.Add(removal.Cell, bits);
            }
            int mask = 0;
            for (int bit = 0; bit < 8; bit++)
            {
                if (((int)removal.Connection & (1 << bit)) != 0) bits[bit] += present ? 1 : -1;
                if (bits[bit] != 0) mask |= 1 << bit;
            }
            if (mask == 0)
            {
                counts.Remove(removal.Cell);
                cells.Remove(removal.Cell);
            }
            else cells[removal.Cell] = (CellConnection)mask;
        }
    }
}
