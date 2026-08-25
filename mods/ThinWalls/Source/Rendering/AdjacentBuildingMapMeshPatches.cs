using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

internal static class AdjacentBuildingMapMeshContext
{
    [ThreadStatic]
    private static Thing? printingThing;

    [ThreadStatic]
    private static Vector3 printingOffset;

    public static Scope Begin(Thing thing)
    {
        var scope = new Scope(printingThing, printingOffset);
        Vector3 offset = AdjacentBuildingDrawPosPatch.ResolveOffset(thing);
        printingThing = offset == Vector3.zero ? null : thing;
        printingOffset = offset;
        return scope;
    }

    public static bool IsPrinting(Thing thing) => ReferenceEquals(printingThing, thing);

    public static bool TryGetOffset(Thing thing, out Vector3 offset)
    {
        if (ReferenceEquals(printingThing, thing))
        {
            offset = printingOffset;
            return true;
        }

        offset = Vector3.zero;
        return false;
    }

    internal readonly struct Scope : IDisposable
    {
        private readonly Thing? previousThing;
        private readonly Vector3 previousOffset;

        public Scope(Thing? previousThing, Vector3 previousOffset)
        {
            this.previousThing = previousThing;
            this.previousOffset = previousOffset;
        }

        public void Dispose()
        {
            printingThing = previousThing;
            printingOffset = previousOffset;
        }
    }
}

[HarmonyPatch(typeof(SectionLayer_ThingsGeneral), "TakePrintFrom", new[] { typeof(Thing) })]
public static class AdjacentBuildingMapMeshPrintPatch
{
    [HarmonyPrefix]
    private static void Prefix(Thing t, out AdjacentBuildingMapMeshContext.Scope __state)
    {
        __state = AdjacentBuildingMapMeshContext.Begin(t);
    }

    [HarmonyFinalizer]
    private static Exception? Finalizer(Exception? __exception, AdjacentBuildingMapMeshContext.Scope __state)
    {
        __state.Dispose();
        return __exception;
    }
}

[HarmonyPatch(typeof(GenThing), nameof(GenThing.TrueCenter), new[] { typeof(Thing) })]
public static class AdjacentBuildingMapMeshCenterPatch
{
    [HarmonyPostfix]
    public static void Postfix(Thing t, ref Vector3 __result)
    {
        if (AdjacentBuildingMapMeshContext.TryGetOffset(t, out Vector3 offset))
        {
            __result += offset;
        }
    }
}
