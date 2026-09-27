using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

/// <summary>
/// Replaces only a Core wall cell participating in one admitted two-receiver
/// Thin side-T. Every other linked wall print remains entirely native.
/// </summary>
[HarmonyPatch(typeof(Graphic_Linked), nameof(Graphic_Linked.Print))]
internal static class HybridRegularWallPrintPatch
{
    private static readonly MethodInfo LinkedDrawMatFrom = AccessTools.Method(
        typeof(Graphic_Linked),
        "LinkedDrawMatFrom",
        new[] { typeof(Thing), typeof(IntVec3) });

    [ThreadStatic]
    private static bool printingReplacement;

    private static readonly Dictionary<Type, bool> NativePrintContracts = new();

    internal static bool UsesNativePrint(Graphic graphic)
    {
        Type type = graphic.GetType();
        if (NativePrintContracts.TryGetValue(type, out bool supported)) return supported;
        Type? printOwner = type.GetMethod(nameof(Graphic.Print), new[]
            { typeof(SectionLayer), typeof(Thing), typeof(float) })?.DeclaringType;
        Type? materialOwner = AccessTools.Method(type, "LinkedDrawMatFrom",
            new[] { typeof(Thing), typeof(IntVec3) })?.DeclaringType;
        // CornerFiller's inspected native override delegates to the patched base Print.
        // Unknown overrides remain native rather than losing the Thin stem preemptively.
        supported = (printOwner == typeof(Graphic_Linked) || printOwner == typeof(Graphic_LinkedCornerFiller)) &&
            materialOwner == typeof(Graphic_Linked);
        NativePrintContracts.Add(type, supported);
        return supported;
    }

    private static bool Prefix(Graphic_Linked __instance, SectionLayer layer, Thing thing)
    {
        if (printingReplacement || thing is not Building wall || wall.def.building?.isWall != true)
            return true;

        try
        {
            var nativeMaterial = (Material)LinkedDrawMatFrom.Invoke(
                __instance,
                new object[] { thing, thing.Position });
            printingReplacement = true;
            return !HybridWallRenderer.TryPrintHybridRegularWall(layer, wall, nativeMaterial);
        }
        catch (Exception exception)
        {
            Log.ErrorOnce(
                $"[Thin Walls] Mixed side-T compilation failed; retaining the native wall print. {exception}",
                0x54485250);
            return true;
        }
        finally
        {
            printingReplacement = false;
        }
    }
}
