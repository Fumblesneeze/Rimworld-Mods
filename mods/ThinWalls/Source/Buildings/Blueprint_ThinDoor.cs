using RimWorld;
using ThinWalls.Geometry;
using ThinWalls.Rendering;
using UnityEngine;
using Verse;

namespace ThinWalls.Buildings;

public sealed class Blueprint_ThinDoor : Blueprint_Build
{
    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        var edge = new OwnedEdge(Position, (ThinWallSide)Rotation.AsInt);
        Material material = ThinWallRenderer.StateEdgeMaterial(
            BuildDef,
            stuffToUse,
            new Color(0.25f, 0.85f, 1f),
            0.72f,
            edge.Side,
            isDoor: true);
        ThinWallRenderer.DrawRealtime(edge, 1, material, AltitudeLayer.Blueprint.AltitudeFor());
    }
}
