using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

[HarmonyPatch(typeof(Graphic_Linked), nameof(Graphic_Linked.Print))]
internal static class HybridRegularWallPrintPatch
{
    private static readonly MethodInfo LinkedDrawMatFrom = AccessTools.Method(
        typeof(Graphic_Linked),
        "LinkedDrawMatFrom",
        new[] { typeof(Thing), typeof(IntVec3) });

    [ThreadStatic]
    private static bool printingReplacement;

    private static bool Prefix(Graphic_Linked __instance, SectionLayer layer, Thing thing)
    {
        if (printingReplacement || thing is not Building wall || wall.def.building?.isWall != true)
        {
            return true;
        }

        try
        {
            var nativeMaterial = (Material)LinkedDrawMatFrom.Invoke(
                __instance,
                new object[] { thing, thing.Position });
            printingReplacement = true;
            return !HybridWallRenderer.TryPrintHybridRegularWall(
                layer,
                wall,
                nativeMaterial);
        }
        catch (Exception exception)
        {
            Log.ErrorOnce(
                $"[Thin Walls] Hybrid regular-wall tile compilation failed; retaining the native wall print. {exception}",
                0x54485250);
            return true;
        }
        finally
        {
            printingReplacement = false;
        }
    }
}
