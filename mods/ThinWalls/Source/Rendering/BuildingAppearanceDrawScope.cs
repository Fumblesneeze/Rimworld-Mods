using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace ThinWalls.Rendering;

/// <summary>One owner for the entire dynamic building draw, including direct component submissions.</summary>
public static class BuildingAppearanceDrawScope
{
    [ThreadStatic] private static State current;

    public readonly struct State
    {
        internal readonly Thing? Owner;
        internal readonly BuildingAppearance Appearance;
        internal readonly Vector3 Pivot;
        internal readonly bool Artwork;
        internal State(Thing? owner)
        {
            Owner = owner;
            Appearance = BuildingAppearanceControls.ForRendering(owner);
            Pivot = Appearance.IsDefault ? default : owner!.TrueCenter();
            Artwork = false;
        }
        internal State(State enclosing)
        { Owner = enclosing.Owner; Appearance = enclosing.Appearance; Pivot = enclosing.Pivot; Artwork = true; }
    }

    public static State EnterThing(Thing thing)
    {
        State previous = current;
        current = new State(thing);
        return previous;
    }

    public static State EnterGraphic(Thing? thing)
    {
        State previous = current;
        // Components frequently omit the Thing argument. They still belong to the enclosing draw.
        // An explicit different Thing is a separate item/pawn and must not inherit its parent's scale.
        if (!current.Artwork && thing != null && !ReferenceEquals(thing, current.Owner)) current = new State(thing);
        return previous;
    }

    public static bool IsIndependentPawn(bool statueArtwork) => !statueArtwork;

    public static State EnterPawn(Pawn pawn, bool statueArtwork)
    {
        State previous = current;
        current = IsIndependentPawn(statueArtwork) ? new State(pawn) : new State(current);
        return previous;
    }

    public static void Restore(State previous) => current = previous;

    public static void TransformSubmission(ref Matrix4x4 matrix)
    {
        if (!current.Appearance.IsDefault) matrix = current.Appearance.TransformMatrix(matrix, current.Pivot);
    }
}

// Containers can call PawnRenderer directly, without Thing.DrawNowAt. Actual occupants remain
// independent; the native renderer explicitly marks fake statue pawns as decorative building art.
[HarmonyPatch]
public static class BuildingAppearancePawnScopePatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.DynamicDrawPhaseAt));
        yield return AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt));
    }
    [HarmonyPrefix]
    private static void Prefix(PawnRenderer __instance, Pawn ___pawn, out BuildingAppearanceDrawScope.State __state) =>
        __state = BuildingAppearanceDrawScope.EnterPawn(___pawn, __instance.StatueColor.HasValue);
    [HarmonyFinalizer]
    private static Exception? Finalizer(Exception? __exception, BuildingAppearanceDrawScope.State __state)
    { BuildingAppearanceDrawScope.Restore(__state); return __exception; }
}

[HarmonyPatch]
public static class BuildingAppearanceThingDrawPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        // Both nonvirtual entry points cover overrides of DynamicDrawPhaseAt/DrawAt and every comp.
        yield return AccessTools.Method(typeof(Thing), nameof(Thing.DynamicDrawPhase));
        yield return AccessTools.Method(typeof(Thing), nameof(Thing.DrawNowAt));
    }
    [HarmonyPrefix]
    private static void Prefix(Thing __instance, out BuildingAppearanceDrawScope.State __state) =>
        __state = BuildingAppearanceDrawScope.EnterThing(__instance);
    [HarmonyFinalizer]
    private static Exception? Finalizer(Exception? __exception, BuildingAppearanceDrawScope.State __state)
    { BuildingAppearanceDrawScope.Restore(__state); return __exception; }
}

[HarmonyPatch]
public static class BuildingAppearanceGraphicScopePatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Graphic), nameof(Graphic.Draw));
        yield return AccessTools.Method(typeof(Graphic), nameof(Graphic.DrawWorker));
    }
    [HarmonyPrefix]
    private static void Prefix(Thing? thing, out BuildingAppearanceDrawScope.State __state) =>
        __state = BuildingAppearanceDrawScope.EnterGraphic(thing);
    [HarmonyFinalizer]
    private static Exception? Finalizer(Exception? __exception, BuildingAppearanceDrawScope.State __state)
    { BuildingAppearanceDrawScope.Restore(__state); return __exception; }
}

[HarmonyPatch]
public static class BuildingAppearanceMeshSubmissionPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        // These are the two managed terminal overloads in the installed Unity 2022 CoreModule.
        // All other DrawMesh overloads funnel here; neither calls the other, so transform only once.
        Type[] common = { typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int), typeof(Camera), typeof(int),
            typeof(MaterialPropertyBlock), typeof(ShadowCastingMode), typeof(bool), typeof(Transform), typeof(LightProbeUsage) };
        yield return AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh), common);
        var extended = new Type[common.Length + 1];
        Array.Copy(common, extended, common.Length);
        extended[common.Length] = typeof(LightProbeProxyVolume);
        yield return AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh), extended);
    }
    [HarmonyPrefix]
    private static void Prefix(ref Matrix4x4 matrix) => BuildingAppearanceDrawScope.TransformSubmission(ref matrix);
}
