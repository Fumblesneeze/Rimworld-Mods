using RimWorld;
using Verse;

namespace ThinWalls.EndToEndTests;

internal static class ThinWallEndToEndFixtureTerrain
{
    public static bool SupportsEveryFixtureBuild(IntVec3 cell, Map map)
    {
        TerrainDef terrain = cell.GetTerrain(map);
        return terrain.affordances.Contains(TerrainAffordanceDefOf.Light) &&
               terrain.affordances.Contains(TerrainAffordanceDefOf.Medium) &&
               terrain.affordances.Contains(TerrainAffordanceDefOf.Heavy);
    }
}
