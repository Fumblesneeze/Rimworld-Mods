using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

/// <summary>Prints source-bound geometry without a diffuse/mask readback or raster cache.</summary>
internal static class NativeWallMeshPrinter
{
    public static bool CanRemap(Material atlas) =>
        atlas != null && atlas.shader != null &&
        HybridWallAtlasSupport.IsSupported(atlas.mainTexture);

    public static void Print(SectionLayer layer, Vector3 center, Material atlas, HybridWallRayMask rays) =>
        Print(layer, center, atlas, NativeWallMeshPlan.Topology(rays));

    public static void Print(SectionLayer layer, Vector3 center, Material atlas,
        IReadOnlyList<NativeWallMeshQuad> plan)
    {
        foreach (NativeWallMeshQuad quad in plan)
        {
            Material linked = NativeWallMaterial.ForLinks(atlas, (int)quad.SourceLinks);
            PrintQuad(layer, center, linked, quad);
        }
    }

    public static void PrintLinked(
        SectionLayer layer,
        Vector3 center,
        Material linked,
        float receiverCellMinY,
        IReadOnlyList<NativeWallMeshQuad> plan)
    {
        foreach (NativeWallMeshQuad quad in plan)
            PrintQuad(layer, center, linked,
                NativeRegularContactPlan.WithNativeReceiverAltitude(quad, receiverCellMinY));
    }

    private static void PrintQuad(
        SectionLayer layer,
        Vector3 center,
        Material linked,
        NativeWallMeshQuad quad)
    {
            if (quad.Vertices is { Count: >= 3 } polygon)
            {
                LayerSubMesh clippedMesh = layer.GetSubMesh(linked);
                int clippedStart = clippedMesh.verts.Count;
                foreach (NativeWallMeshVertex vertex in polygon)
                {
                    clippedMesh.verts.Add(center + new Vector3(
                        vertex.X,
                        quad.AltitudeAt(vertex.X, vertex.Y),
                        vertex.Y));
                    clippedMesh.uvs.Add(new Vector3(vertex.U, vertex.V, 0f));
                    clippedMesh.colors.Add(new Color32(255, 255, 255, 255));
                }
                for (int i = 1; i < polygon.Count - 1; i++)
                {
                    clippedMesh.tris.Add(clippedStart);
                    clippedMesh.tris.Add(clippedStart + i);
                    clippedMesh.tris.Add(clippedStart + i + 1);
                }
                return;
            }
            HybridWallUvRect d = quad.Destination;
            HybridWallUvRect uv = quad.Source;
            bool shapedAltitude = quad.AltitudeOffset != 0f || quad.AltitudeSlopeX != 0f || quad.AltitudeSlopeY != 0f;
            if (quad.Triangle != 0 || shapedAltitude)
            {
                LayerSubMesh mesh = layer.GetSubMesh(linked);
                int start = mesh.verts.Count;
                mesh.verts.Add(center + new Vector3(d.X, quad.AltitudeAt(d.X, d.Y), d.Y));
                mesh.verts.Add(center + new Vector3(d.X, quad.AltitudeAt(d.X, d.YMax), d.YMax));
                mesh.verts.Add(center + new Vector3(d.XMax, quad.AltitudeAt(d.XMax, d.YMax), d.YMax));
                mesh.verts.Add(center + new Vector3(d.XMax, quad.AltitudeAt(d.XMax, d.Y), d.Y));
                mesh.uvs.Add(new Vector3(uv.X, uv.Y, 0f));
                mesh.uvs.Add(new Vector3(uv.X, uv.YMax, 0f));
                mesh.uvs.Add(new Vector3(uv.XMax, uv.YMax, 0f));
                mesh.uvs.Add(new Vector3(uv.XMax, uv.Y, 0f));
                for (int i = 0; i < 4; i++) mesh.colors.Add(new Color32(255, 255, 255, 255));
                mesh.tris.Add(start + (quad.Triangle == 4 ? 1 : 0));
                mesh.tris.Add(start + (quad.Triangle == 2 || quad.Triangle == 4 ? 2 : 1));
                mesh.tris.Add(start + (quad.Triangle == 0 || quad.Triangle == 1 ? 2 : 3));
                if (quad.Triangle == 0)
                {
                    mesh.tris.Add(start);
                    mesh.tris.Add(start + 2);
                    mesh.tris.Add(start + 3);
                }
                return;
            }
            Printer_Plane.PrintPlane(layer,
                center + new Vector3(d.X + d.Width / 2f, 0f, d.Y + d.Height / 2f),
                new Vector2(d.Width, d.Height), linked,
                uvs: new[]
                {
                    new Vector2(uv.X, uv.Y), new Vector2(uv.X, uv.YMax),
                    new Vector2(uv.XMax, uv.YMax), new Vector2(uv.XMax, uv.Y)
                },
                topVerticesAltitudeBias: 0f);
    }
}
