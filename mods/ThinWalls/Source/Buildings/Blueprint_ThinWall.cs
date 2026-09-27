using RimWorld;
using ThinWalls.Geometry;
using ThinWalls.Rendering;
using UnityEngine;
using Verse;

namespace ThinWalls.Buildings;

public sealed class Blueprint_ThinWall : Blueprint_Build
{
    public override void Print(SectionLayer layer)
    {
        var edge = new OwnedEdge(Position, (ThinWallSide)Rotation.AsInt);
        ThinWallRenderer.PrintBlueprint(layer, edge);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false) { }
}
