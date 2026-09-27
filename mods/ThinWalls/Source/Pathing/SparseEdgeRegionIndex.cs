using System.Collections.Generic;
using ThinWalls.Geometry;
using Verse;

namespace ThinWalls.Pathing;

public sealed class SparseEdgeRegionIndex
{
    private readonly HashSet<SharedEdge> edges = new();
    private readonly Dictionary<IntVec3, int> chunks = new();
    public int Count => chunks.Count;
    public bool Contains(IntVec3 cell)
    {
        IntVec3 chunk = Key(cell);
        return chunks.ContainsKey(chunk) || chunks.ContainsKey(chunk + IntVec3.North) ||
               chunks.ContainsKey(chunk + IntVec3.East) || chunks.ContainsKey(chunk + IntVec3.South) ||
               chunks.ContainsKey(chunk + IntVec3.West);
    }
    public bool UseCellLink(IntVec3 first, IntVec3 second) => chunks.ContainsKey(Key(first)) || chunks.ContainsKey(Key(second));

    public bool Set(SharedEdge edge, bool present)
    {
        if (!(present ? edges.Add(edge) : edges.Remove(edge))) return false;
        int previousCount = chunks.Count;
        Change(Key(edge.AnchorCell), present ? 1 : -1);
        IntVec3 opposite = new OwnedEdge(edge.AnchorCell, edge.PositiveSide).OppositeCell;
        if (Key(opposite) != Key(edge.AnchorCell)) Change(Key(opposite), present ? 1 : -1);
        return previousCount != chunks.Count;
    }

    private void Change(IntVec3 key, int delta)
    {
        chunks.TryGetValue(key, out int count);
        if (count + delta == 0) chunks.Remove(key);
        else chunks[key] = count + delta;
    }

    private static IntVec3 Key(IntVec3 cell) => new(cell.x / Region.GridSize, 0, cell.z / Region.GridSize);
}
