using System.Reflection;
using HarmonyLib;
using RimWorld;
using ThinWalls.Geometry;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

/// <summary>Keep native brackets/animation, changing only their visual envelope like wall attachments do.</summary>
[HarmonyPatch]
public static class ThinWallSelectionPatch
{
    public static MethodBase TargetMethod() => AccessTools.Method(typeof(SelectionDrawerUtility),
        nameof(SelectionDrawerUtility.CalculateSelectionBracketPositionsWorld)).MakeGenericMethod(typeof(object));

    [HarmonyPrefix]
    public static void Prefix(object obj, ref Vector3 worldPos, ref Vector2 worldSize)
    {
        if (obj is not Thing thing || !ThinWallUtility.TryGetOwnedEdge(thing, out OwnedEdge edge)) return;
        worldPos = ThinWallRenderGeometry.StructuralCenter(edge, worldPos.y);
        worldSize = ThinWallSelectionGeometry.Size(edge);
    }
}
