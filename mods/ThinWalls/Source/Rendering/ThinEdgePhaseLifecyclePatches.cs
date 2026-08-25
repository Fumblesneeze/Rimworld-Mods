using HarmonyLib;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using ThinWalls.Pathing;
using Verse;

namespace ThinWalls.Rendering;

[HarmonyPatch(typeof(Thing), nameof(Thing.SpawnSetup))]
public static class ThinEdgePhaseSpawnPatch
{
    [HarmonyPostfix]
    public static void Postfix(Thing __instance, Map map)
    {
        if (__instance is IThinEdgeStructure ||
            !ThinWallUtility.TryGetOwnedEdge(__instance, out OwnedEdge edge))
        {
            return;
        }

        map.GetComponent<ThinWallMapComponent>().NotifyPlannedEdgeChanged(edge);
    }
}

[HarmonyPatch(typeof(Thing), nameof(Thing.DeSpawn))]
public static class ThinEdgePhaseDeSpawnPatch
{
    [HarmonyPrefix]
    public static void Prefix(Thing __instance)
    {
        Map? map = __instance.Map;
        if (map == null || __instance is IThinEdgeStructure ||
            !ThinWallUtility.TryGetOwnedEdge(__instance, out OwnedEdge edge))
        {
            return;
        }

        map.GetComponent<ThinWallMapComponent>().NotifyPlannedEdgeChanged(edge);
    }
}
