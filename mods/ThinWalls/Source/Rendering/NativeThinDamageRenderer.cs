using HarmonyLib;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

internal static class NativeThinDamageRenderer
{
    public static void DrawRealtime(Building_ThinWall building)
    {
        ThinWallDamageGrade grade = ThinWallVisualResolver.DamageGrade(
            building.HitPoints,
            building.MaxHitPoints);
        if (grade == ThinWallDamageGrade.None) return;

        var scratchMaterials = BuildingsDamageSectionLayerUtility.GetScratchMats(building);
        if (scratchMaterials == null || scratchMaterials.Count == 0) return;

        OwnedEdge edge = building.OwnedEdge;
        bool horizontal = edge.Shared.PositiveSide == ThinWallSide.North;
        Vector3 origin = ThinWallRenderGeometry.StructuralCenter(
            edge,
            NativeThinDamagePlan.StaticOverlayAltitude(building.def.Altitude));
        foreach (NativeThinDamageMark mark in NativeThinDamagePlan.Compile(grade, building.thingIDNumber))
        {
            Material material = scratchMaterials[mark.CoreScratchIndex % scratchMaterials.Count];
            Vector3 center = horizontal
                ? origin + new Vector3(mark.LongitudinalCenter, 0f, mark.NormalCenter)
                : origin + new Vector3(mark.NormalCenter, 0f, mark.LongitudinalCenter);
            Graphics.DrawMesh(
                MeshPool.plane10,
                Matrix4x4.TRS(
                    center,
                    Quaternion.Euler(0f, mark.RotationDegrees, 0f),
                    new Vector3(mark.Size, 1f, mark.Size)),
                material,
                0);
        }
    }
}

/// <summary>
/// Vanilla assumes every damaged building fills its cell and would therefore
/// place scratches beside an edge structure. Thin structures print clipped,
/// source-bound Core scratches with their own structural mesh instead.
/// </summary>
[HarmonyPatch(typeof(SectionLayer_BuildingsDamage), "PrintDamageVisualsFrom")]
internal static class NativeThinDamageSuppressionPatch
{
    private static bool Prefix(Building b)
    {
        if (b is not IThinEdgeStructure) return true;
        // Thin Walls and Thin Doors draw Core scratches in their realtime pass;
        // the native cell-centred map-mesh damage would sit beside the edge.
        return false;
    }
}
