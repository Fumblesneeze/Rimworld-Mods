using System.Collections.Generic;
using ThinWalls.Geometry;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

public static class ThinWallRenderGeometry
{
    public const int ProjectionRecipeVersion = 4;
    public const float UnionPlaneScale = 1f;
    public const float StructuralOffsetPixelsX = 0f;
    public const float StructuralOffsetPixelsZ = 0f;
    public const int StructuralRasterOffsetPixelsZ = 8;
    public const float SourcePixelsPerCell = 60f;

    public static Vector3 StructuralWorldOffset => new(
        StructuralOffsetPixelsX / SourcePixelsPerCell,
        0f,
        StructuralOffsetPixelsZ / SourcePixelsPerCell);

    public static Vector3 Center(OwnedEdge edge, float altitude)
    {
        SharedEdge shared = edge.Shared;
        return shared.PositiveSide == ThinWallSide.North
            ? new Vector3(shared.AnchorCell.x + 0.5f, altitude, shared.AnchorCell.z + 1f)
            : new Vector3(shared.AnchorCell.x + 1f, altitude, shared.AnchorCell.z + 0.5f);
    }

    public static Vector3 StructuralCenter(OwnedEdge edge, float altitude) =>
        Center(edge, altitude);

    public static Vector3 StructuralVertexCenter(IntVec3 vertex, float altitude) =>
        new(vertex.x, altitude, vertex.z);

    public static IEnumerable<IntVec3> IncidentCells(OwnedEdge edge)
    {
        SharedEdge shared = edge.Shared;
        IntVec3 firstCorner = shared.PositiveSide == ThinWallSide.North
            ? new IntVec3(shared.AnchorCell.x, 0, shared.AnchorCell.z + 1)
            : new IntVec3(shared.AnchorCell.x + 1, 0, shared.AnchorCell.z);
        IntVec3 secondCorner = new(
            firstCorner.x + (shared.PositiveSide == ThinWallSide.North ? 1 : 0),
            0,
            firstCorner.z + (shared.PositiveSide == ThinWallSide.East ? 1 : 0));
        var seen = new HashSet<IntVec3>();
        foreach (IntVec3 corner in new[] { firstCorner, secondCorner })
        {
            for (int x = corner.x - 1; x <= corner.x; x++)
            {
                for (int z = corner.z - 1; z <= corner.z; z++)
                {
                    IntVec3 cell = new(x, 0, z);
                    if (seen.Add(cell))
                    {
                        yield return cell;
                    }
                }
            }
        }
    }
}
