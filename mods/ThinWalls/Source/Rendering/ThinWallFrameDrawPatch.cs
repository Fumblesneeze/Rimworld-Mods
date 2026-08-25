using HarmonyLib;
using RimWorld;
using ThinWalls.Geometry;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

[HarmonyPatch(typeof(Frame), "DrawAt")]
public static class ThinWallFrameDrawPatch
{
    [HarmonyPrefix]
    public static bool Prefix(Frame __instance)
    {
        if (!ThinWallUtility.IsThinEdgeDef(__instance.def))
        {
            return true;
        }

        ThingDef buildDef = (ThingDef)__instance.def.entityDefToBuild;
        var edge = new OwnedEdge(__instance.Position, (ThinWallSide)__instance.Rotation.AsInt);
        Material material = ThinWallRenderer.StateEdgeMaterial(
            buildDef,
            __instance.Stuff,
            new Color(0.92f, 0.92f, 0.92f),
            0.48f,
            edge.Side,
            ThinWallUtility.IsThinDoorDef(buildDef));
        ThinWallRenderer.DrawRealtime(
            edge,
            1,
            material,
            AltitudeLayer.Building.AltitudeFor());
        return false;
    }
}
