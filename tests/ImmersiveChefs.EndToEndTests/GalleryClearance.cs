using System;
using System.Collections.Generic;

namespace ImmersiveChefs.VisualTesting;

// Test-fixture policy only; no game types or product assembly dependency.
internal static class GalleryClearance
{
    internal const int Margin = 2;

    internal static bool IsClear(
        IEnumerable<(int MinX, int MinZ, int MaxX, int MaxZ)> footprints,
        Func<int, int, bool> cellIsClear,
        IEnumerable<(int X, int Z)>? pendingMoteCells = null)
        => FindBlockedCell(footprints, cellIsClear, pendingMoteCells) is null;

    internal static (int X, int Z)? FindBlockedCell(
        IEnumerable<(int MinX, int MinZ, int MaxX, int MaxZ)> footprints,
        Func<int, int, bool> cellIsClear,
        IEnumerable<(int X, int Z)>? pendingMoteCells = null)
    {
        var pendingCells = new HashSet<(int X, int Z)>(pendingMoteCells ?? Array.Empty<(int, int)>());
        foreach (var footprint in footprints)
        for (var x = footprint.MinX - Margin; x <= footprint.MaxX + Margin; x++)
        for (var z = footprint.MinZ - Margin; z <= footprint.MaxZ + Margin; z++)
            if (pendingCells.Contains((x, z)) || !cellIsClear(x, z))
                return (x, z);
        return null;
    }
}
