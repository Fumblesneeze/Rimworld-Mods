using System;
using System.Collections.Generic;
using ThinWalls.Geometry;
using UnityEngine;

namespace ThinWalls.Rendering;

/// <summary>Reconciles native candidates without depending on game state or changing ordinary targets.</summary>
public static class ThinWallPointerCandidates
{
    public static void Reconcile<T>(List<T> native, IEnumerable<T> nearby, Vector3 pointer,
        Func<T, OwnedEdge?> edgeOf, Func<T, bool> canTarget)
    {
        for (int i = native.Count - 1; i >= 0; i--)
            if (edgeOf(native[i]) is OwnedEdge edge && !ThinWallSelectionGeometry.Contains(edge, pointer))
                native.RemoveAt(i);

        foreach (T candidate in nearby)
            if (edgeOf(candidate) is OwnedEdge edge && ThinWallSelectionGeometry.Contains(edge, pointer) &&
                !native.Contains(candidate) && canTarget(candidate))
                native.Add(candidate);
    }
}
