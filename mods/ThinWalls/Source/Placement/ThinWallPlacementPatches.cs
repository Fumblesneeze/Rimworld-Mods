using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ThinWalls.Placement;

[HarmonyPatch(typeof(GenConstruct), "CanPlaceBlueprintAt_NewTemp")]
public static class ThinWallPlacementPatch
{
    [HarmonyPrefix]
    public static void Prefix(
        BuildableDef entDef,
        IntVec3 center,
        Rot4 rot,
        ref Predicate<Thing> skipBlockingThing)
    {
        Predicate<Thing> inherited = skipBlockingThing;
        skipBlockingThing = thing => (inherited?.Invoke(thing) ?? false) ||
                                     ThinWallPlacementRules.MayIgnoreBlockingThing(
                                         entDef,
                                         center,
                                         rot,
                                         thing);
    }

    [HarmonyPostfix]
    public static void Postfix(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, ref AcceptanceReport __result)
    {
        if (__result.Accepted)
        {
            __result = ThinWallPlacementRules.Validate(entDef, center, rot, map);
        }
    }
}

[HarmonyPatch(typeof(GenSpawn), nameof(GenSpawn.SpawningWipes))]
public static class ThinWallSpawningWipesPatch
{
    [HarmonyPrefix]
    public static bool Prefix(BuildableDef newEntDef, BuildableDef oldEntDef, ref bool __result)
    {
        if (ThinWallUtility.IsThinEdgeDef(newEntDef) || ThinWallUtility.IsThinEdgeDef(oldEntDef))
        {
            __result = false;
            return false;
        }

        return true;
    }
}
