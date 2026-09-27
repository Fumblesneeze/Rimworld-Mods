using ThinWalls.Geometry;
using UnityEngine;

namespace ThinWalls.Rendering;

/// <summary>The settled native bracket envelope, shared with pointer selection.</summary>
public static class ThinWallSelectionGeometry
{
    public static Vector2 Size(OwnedEdge edge) => edge.Shared.PositiveSide == ThinWallSide.North
        ? new Vector2(1f, 34f / 60f)
        : new Vector2(34f / 60f, 1f);

    public static Rect Bounds(OwnedEdge edge)
    {
        Vector3 center = ThinWallRenderGeometry.StructuralCenter(edge, 0f);
        Vector2 size = Size(edge);
        return new Rect(center.x - size.x / 2f, center.z - size.y / 2f, size.x, size.y);
    }

    public static bool Contains(OwnedEdge edge, Vector3 pointer)
    {
        Rect bounds = Bounds(edge);
        return pointer.x >= bounds.xMin && pointer.x <= bounds.xMax &&
               pointer.z >= bounds.yMin && pointer.z <= bounds.yMax;
    }
}
