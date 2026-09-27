using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using UnityEngine;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest("thin-walls.supporting-core-damage-source-probe", "fumblesneeze.thinwalls",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "fumblesneeze.thinwalls",
    MaxFrames = 600, MaxGameTicks = 100, MaxWallClockSeconds = 30)]
public sealed class NativeDamageSourceProbeTest : IRimWorldEndToEndTest
{
    private Building wall = null!;
    private Building_ThinWall thin = null!;

    public void Arrange(IEndToEndContext context)
    {
        Map map = Find.CurrentMap;
        wall = (Building)ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
        wall.SetFactionDirect(Faction.OfPlayer);
        wall.HitPoints = wall.MaxHitPoints / 4;
        GenSpawn.Spawn(wall, map.Center, map);
        ThingDef thinDef = DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName);
        thin = (Building_ThinWall)ThingMaker.MakeThing(thinDef, ThingDefOf.Steel);
        thin.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(thin, map.Center + IntVec3.East * 3, map, new Rot4((int)ThinWallSide.South));
        thin.HitPoints = thin.MaxHitPoints / 4;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CheckpointStep("measure installed Core building damage sources", _ =>
        {
            IList<Material> materials = BuildingsDamageSectionLayerUtility.GetScratchMats(wall);
            return new Dictionary<string, string>
            {
                ["count"] = materials.Count.ToString(),
                ["materials"] = string.Join(";", materials.Select(material =>
                    material.name + "|" + material.shader.name + "|" +
                    material.mainTexture.name + "|" + material.mainTexture.width + "x" +
                    material.mainTexture.height + "|scale=" + material.mainTextureScale +
                    "|offset=" + material.mainTextureOffset + "|color=" + material.color +
                    "|" + MeasureAlpha((Texture2D)material.mainTexture))),
            };
        });
        yield return new CameraActionStep("show damaged source probe", new[] { wall.ThingID, thin.ThingID }, 100);
        yield return new ScreenshotStep("damaged Core wall and Thin wall source probe", new[] { wall.ThingID, thin.ThingID }, 100);
        yield return new CheckpointStep("inspect Thin damage section mesh", _ =>
        {
            Section section = thin.Map.mapDrawer.SectionAt(thin.Position);
            var layer = (SectionLayer_BuildingsDamage)section.GetLayer(typeof(SectionLayer_BuildingsDamage));
            LayerSubMesh[] active = layer.subMeshes
                .Where(mesh => !mesh.disabled && mesh.mesh.vertexCount > 0).ToArray();
            return new Dictionary<string, string>
            {
                ["active"] = active.Length.ToString(),
                ["meshes"] = string.Join(";", active.Select(mesh =>
                    mesh.material.name + "|" + mesh.material.mainTexture.name +
                    "|verts=" + mesh.mesh.vertexCount + "|bounds=" + mesh.mesh.bounds +
                    "|positions=" + string.Join(",", mesh.mesh.vertices.Take(32).Select(v => v.ToString())) +
                    "|uvs=" + string.Join(",", mesh.mesh.uv.Take(32).Select(v => v.ToString())))),
            };
        });
    }

    public void Cleanup(IEndToEndContext context)
    {
        if (wall.Spawned) wall.Destroy(DestroyMode.Vanish);
        if (thin.Spawned) thin.Destroy(DestroyMode.Vanish);
    }

    private static string MeasureAlpha(Texture2D texture)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture readableTarget = RenderTexture.GetTemporary(
            texture.width,
            texture.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear);
        var readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, true);
        Color32[] pixels;
        try
        {
            Graphics.Blit(texture, readableTarget);
            RenderTexture.active = readableTarget;
            readable.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0, false);
            readable.Apply(false, false);
            pixels = readable.GetPixels32();
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(readableTarget);
            Object.Destroy(readable);
        }

        int minX = texture.width;
        int minY = texture.height;
        int maxX = -1;
        int maxY = -1;
        int nonzero = 0;
        long red = 0;
        long green = 0;
        long blue = 0;
        long alpha = 0;
        for (int y = 0; y < texture.height; y++)
        for (int x = 0; x < texture.width; x++)
        {
            Color32 pixel = pixels[y * texture.width + x];
            if (pixel.a == 0) continue;
            nonzero++;
            red += pixel.r;
            green += pixel.g;
            blue += pixel.b;
            alpha += pixel.a;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        return "alphaBounds=" + minX + "," + minY + ".." + (maxX + 1) + "," + (maxY + 1) +
               "|nonzeroAlpha=" + nonzero +
               "|meanRgba=" + red / nonzero + "," + green / nonzero + "," + blue / nonzero + "," + alpha / nonzero;
    }
}
