using System;
using System.Collections.Generic;
using Verse;

namespace ThinWalls.Pathing;

/// <summary>Records only native bits actually cleared by our masks, never a copy of the native grid.</summary>
public sealed class SparseConnectivityOverlay
{
    private readonly Dictionary<int, CellConnection> removed = new();
    public IReadOnlyDictionary<int, CellConnection> Removed => removed;

    public void Apply(int index, CellConnection mask, Func<int, CellConnection> read, Action<int, CellConnection> write)
    {
        CellConnection native = read(index);
        CellConnection cleared = native & mask;
        if (cleared == CellConnection.Self) return;
        removed.TryGetValue(index, out CellConnection previous);
        removed[index] = previous | cleared;
        write(index, native & ~mask);
    }

    public void Restore(Func<int, CellConnection> read, Action<int, CellConnection> write)
    {
        foreach (var entry in removed) write(entry.Key, read(entry.Key) | entry.Value);
        removed.Clear();
    }
}
