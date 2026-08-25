using ThinWalls.Buildings;
using ThinWalls.Geometry;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

public static class ThinWallRenderer
{
    public static void PrintCompleted(SectionLayer layer, Building_ThinWall wall)
    {
        HybridWallRenderer.PrintCompleted(layer, wall);
    }

    public static void PrintCompleted(SectionLayer layer, Building_ThinDoor door)
    {
        HybridWallRenderer.PrintCompleted(layer, door);
    }

    public static void Dirty(Map map, OwnedEdge edge)
    {
        foreach (IntVec3 cell in ThinWallRenderGeometry.IncidentCells(edge))
        {
            if (cell.InBounds(map))
            {
                map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Things);
            }
        }
    }

    public static void DrawRealtime(OwnedEdge edge, int ownerCount, Material material, float altitude)
    {
        HybridWallRenderer.DrawStateEdge(edge, ownerCount, material, altitude);
    }

    public static Material StateMaterial(
        ThingDef buildDef,
        ThingDef? stuff,
        Color stateColor,
        float stateWeight,
        ThinWallSide side)
    {
        return StateEdgeMaterial(buildDef, stuff, stateColor, stateWeight, side, isDoor: false);
    }

    public static Material StateDoorMaterial(
        ThingDef buildDef,
        ThingDef? stuff,
        Color stateColor,
        float stateWeight,
        ThinWallSide side)
    {
        return StateEdgeMaterial(buildDef, stuff, stateColor, stateWeight, side, isDoor: true);
    }

    public static Material StateEdgeMaterial(
        ThingDef buildDef,
        ThingDef? stuff,
        Color stateColor,
        float stateWeight,
        ThinWallSide side,
        bool isDoor)
    {
        Color stuffColor = stuff == null ? Color.white : buildDef.GetColorForStuff(stuff);
        Color finalColor = Color.Lerp(stuffColor, stateColor, Mathf.Clamp01(stateWeight));
        return SolidColorMaterials.SimpleSolidColorMaterial(finalColor);
    }

    public static void DrawDoor(Building_ThinDoor door, float openPct)
    {
        HybridWallRenderer.DrawDoor(door, openPct);
    }
}
