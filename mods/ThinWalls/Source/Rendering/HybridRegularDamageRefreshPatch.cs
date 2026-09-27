using HarmonyLib;
using Verse;

namespace ThinWalls.Rendering;

[HarmonyPatch(typeof(BuildingsDamageSectionLayerUtility), nameof(BuildingsDamageSectionLayerUtility.Notify_BuildingHitPointsChanged))]
internal static class HybridRegularDamageRefreshPatch
{
    private static void Postfix(Building b, int oldHitPoints)
    {
        if (!b.Spawned || b.def.building?.isWall != true ||
            !ThinWallRenderGeometry.RegularWallDamageGradeChanged(oldHitPoints, b.HitPoints, b.MaxHitPoints)) return;
        foreach (IntVec3 cell in ThinWallRenderGeometry.RegularWallDependencyCells(b.Position))
            if (cell.InBounds(b.Map)) b.Map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Things);
    }
}
