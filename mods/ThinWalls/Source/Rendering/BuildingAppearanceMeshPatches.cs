using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

/// <summary>Transform only vertices appended by this building; never shared graphics or UVs.</summary>
[HarmonyPatch]
public static class BuildingAppearancePrintPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(SectionLayer_ThingsGeneral), "TakePrintFrom");
        yield return AccessTools.Method(typeof(SectionLayer_BuildingsDamage), "PrintDamageVisualsFrom");
    }

    [HarmonyPrefix]
    private static void Prefix(MapDrawLayer __instance, Thing __0, out PrintedGeometry? __state)
    {
        BuildingAppearance appearance = BuildingAppearanceControls.ForRendering(__0);
        __state = appearance.IsDefault ? null : new PrintedGeometry(__instance, __0.TrueCenter(), appearance);
    }

    [HarmonyFinalizer]
    private static Exception? Finalizer(Exception? __exception, PrintedGeometry? __state)
    {
        __state?.Apply();
        return __exception;
    }

    private sealed class PrintedGeometry
    {
        private readonly MapDrawLayer layer;
        private readonly int[] starts;
        private readonly Vector3 pivot;
        private readonly BuildingAppearance appearance;

        public PrintedGeometry(MapDrawLayer layer, Vector3 pivot, BuildingAppearance appearance)
        {
            this.layer = layer;
            this.pivot = pivot;
            this.appearance = appearance;
            starts = new int[layer.subMeshes.Count];
            for (int i = 0; i < starts.Length; i++) starts[i] = layer.subMeshes[i].verts.Count;
        }

        public void Apply()
        {
            for (int i = 0; i < layer.subMeshes.Count; i++)
            {
                List<Vector3> verts = layer.subMeshes[i].verts;
                for (int v = i < starts.Length ? starts[i] : 0; v < verts.Count; v++)
                    verts[v] = appearance.Transform(verts[v], pivot);
            }
        }
    }
}
