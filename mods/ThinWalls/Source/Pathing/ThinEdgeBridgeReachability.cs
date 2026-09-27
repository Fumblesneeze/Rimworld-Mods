using System;
using System.Collections.Generic;
using ThinWalls.Geometry;
using Verse;

namespace ThinWalls.Pathing;

/// <summary>Searches only supplied, permitted edge crossings. Native reachability owns every connecting leg.</summary>
public static class ThinEdgeBridgeReachability
{
    public static bool CanReach(IntVec3 start, IEnumerable<SharedEdge> permittedCrossings,
        Func<IntVec3, IntVec3, bool> nativeLeg, Func<IntVec3, bool> nativeFinish, Predicate<IntVec3> usableEndpoint)
    {
        var crossings = new List<SharedEdge>(permittedCrossings);
        var open = new List<IntVec3> { start };
        var visited = new HashSet<IntVec3> { start };
        for (int next = 0; next < open.Count; next++)
        {
            IntVec3 from = open[next];
            // Crossing order is irrelevant; `visited` alone bounds the search.
            foreach (SharedEdge edge in crossings)
            {
                IntVec3 a = edge.AnchorCell;
                IntVec3 b = new OwnedEdge(a, edge.PositiveSide).OppositeCell;
                if (!usableEndpoint(a) || !usableEndpoint(b)) continue;
                bool reachA = nativeLeg(from, a);
                if (!reachA && !nativeLeg(from, b)) continue;
                IntVec3 exit = reachA ? b : a;
                if (!visited.Add(exit)) continue;
                if (nativeFinish(exit)) return true;
                open.Add(exit);
            }
        }
        return false;
    }
}
