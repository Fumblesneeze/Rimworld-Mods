using System;
using ThinWalls.Geometry;
using Verse;

namespace ThinWalls.Rendering;

/// <summary>Choose an existing gizmo preset from footprint perimeter contacts, never from sprite pixels.</summary>
public static class BuildingAutomaticOffset
{
    public static int Resolve(CellRect footprint, Func<SharedEdge, bool> hasThinEdge)
    {
        bool north = false, east = false, south = false, west = false;
        for (int x = footprint.minX; x <= footprint.maxX; x++)
        {
            north |= hasThinEdge(new OwnedEdge(new IntVec3(x, 0, footprint.maxZ), ThinWallSide.North).Shared);
            south |= hasThinEdge(new OwnedEdge(new IntVec3(x, 0, footprint.minZ), ThinWallSide.South).Shared);
        }
        for (int z = footprint.minZ; z <= footprint.maxZ; z++)
        {
            east |= hasThinEdge(new OwnedEdge(new IntVec3(footprint.maxX, 0, z), ThinWallSide.East).Shared);
            west |= hasThinEdge(new OwnedEdge(new IntVec3(footprint.minX, 0, z), ThinWallSide.West).Shared);
        }
        int dx = (west ? 1 : 0) - (east ? 1 : 0);
        int dz = (south ? 1 : 0) - (north ? 1 : 0);
        return dz > 0 ? (dx > 0 ? 2 : dx < 0 ? 8 : 1)
            : dz < 0 ? (dx > 0 ? 4 : dx < 0 ? 6 : 5)
            : dx > 0 ? 3 : dx < 0 ? 7 : 0;
    }
}
