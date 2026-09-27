using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

/// <summary>The native sun shader extrudes high-alpha vertices; keep its footprint and cast height together.</summary>
public static class BuildingAppearanceShadowGeometry
{
    public static void Append(List<Vector3> vertices, List<Color32> colors, List<int> triangles,
        CellRect footprint, Vector3 pivot, BuildingAppearance appearance, float altitude, float height,
        CellRect? sectionRect = null)
    {
        CellRect part = footprint;
        if (sectionRect.HasValue) part.ClipInsideRect(sectionRect.Value);
        if (part.IsEmpty) return;
        int start = vertices.Count;
        var low = new Color32(0, 0, 0, 0);
        var high = new Color32(0, 0, 0, (byte)Math.Min(255, Math.Max(0, Math.Round(255 * height * appearance.Scale))));
        Add(part.minX, part.minZ, low);
        Add(part.minX, part.maxZ + 1, low);
        Add(part.maxX + 1, part.maxZ + 1, low);
        Add(part.maxX + 1, part.minZ, low);
        Tri(0, 1, 2); Tri(0, 2, 3);
        // West, east and south casting faces use the native winding and duplicated height vertices.
        if (part.minX == footprint.minX)
        {
            int v = vertices.Count - start;
            Add(part.minX, part.minZ, high);
            Add(part.minX, part.maxZ + 1, high);
            Tri(1, 0, v); Tri(v, v + 1, 1);
        }
        if (part.maxX == footprint.maxX)
        {
            int v = vertices.Count - start;
            Add(part.maxX + 1, part.maxZ + 1, high);
            Add(part.maxX + 1, part.minZ, high);
            Tri(2, v, v + 1); Tri(v + 1, 3, 2);
        }
        if (part.minZ == footprint.minZ)
        {
            int v = vertices.Count - start;
            Add(part.minX, part.minZ, high);
            Add(part.maxX + 1, part.minZ, high);
            Tri(0, 3, v); Tri(3, v + 1, v);
        }

        void Add(float x, float z, Color32 color)
        {
            vertices.Add(appearance.Transform(new Vector3(x, altitude, z), pivot));
            colors.Add(color);
        }
        void Tri(int a, int b, int c)
        { triangles.Add(start + a); triangles.Add(start + b); triangles.Add(start + c); }
    }
}

[HarmonyPatch]
public static class BuildingAppearanceSunShadowPatch
{
    private static MethodBase TargetMethod() => AccessTools.Method(
        typeof(Thing).Assembly.GetType("Verse.SectionLayer_SunShadows", throwOnError: true), "Regenerate");

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo filter = AccessTools.Method(typeof(BuildingAppearanceSunShadowPatch), nameof(NativeCaster));
        int count = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            yield return instruction;
            if (instruction.opcode == OpCodes.Ldelem_Ref)
            {
                yield return new CodeInstruction(OpCodes.Call, filter);
                count++;
            }
        }
        if (count != 4) throw new InvalidOperationException("Thin Walls: native sun-shadow edifice seam changed.");
    }

    public static Building? NativeCaster(Building? building) =>
        BuildingAppearanceControls.ForRendering(building).IsDefault ? building : null;

    [HarmonyPostfix]
    private static void Postfix(MapDrawLayer __instance, Section ___section)
    {
        if (!MatBases.SunShadow.shader.isSupported) return;
        LayerSubMesh? mesh = null;
        foreach (IntVec3 cell in ___section.CellRect)
        {
            Building building = cell.GetEdifice(___section.map);
            if (building == null || building.def.staticSunShadowHeight <= 0f) continue;
            BuildingAppearance appearance = BuildingAppearanceControls.ForRendering(building);
            if (appearance.IsDefault) continue;
            CellRect footprint = building.OccupiedRect();
            CellRect part = footprint;
            part.ClipInsideRect(___section.CellRect);
            if (cell.x != part.minX || cell.z != part.minZ) continue;
            mesh ??= __instance.GetSubMesh(MatBases.SunShadow);
            BuildingAppearanceShadowGeometry.Append(mesh.verts, mesh.colors, mesh.tris, footprint,
                building.TrueCenter(), appearance, AltitudeLayer.Shadows.AltitudeFor(), building.def.staticSunShadowHeight,
                ___section.CellRect);
        }
        if (mesh != null)
        {
            // Reopen the native buffer without clearing any owner's geometry. Regenerate may have
            // uploaded its unadjusted casters already; this second upload is only on mesh rebuilds.
            mesh.Clear(MeshParts.None);
            mesh.FinalizeMesh(MeshParts.Verts | MeshParts.Tris | MeshParts.Colors);
        }
    }
}
