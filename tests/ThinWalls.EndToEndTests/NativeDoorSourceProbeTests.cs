using System.Collections;
using System.Collections.Generic;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest("thin-walls.supporting-core-door-source-probe", "fumblesneeze.thinwalls",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "fumblesneeze.thinwalls",
    MaxFrames = 600, MaxGameTicks = 100, MaxWallClockSeconds = 30)]
public sealed class NativeDoorSourceProbeTest : IRimWorldEndToEndTest
{
    public void Arrange(IEndToEndContext context)
    {
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CheckpointStep("measure installed Core simple-door mover source", _ =>
        {
            Graphic graphic = ThingDefOf.Door.graphicData.Graphic;
            Material material = graphic.MatSingle;
            var texture = (Texture2D)material.mainTexture;
            RenderTexture previous = RenderTexture.active;
            RenderTexture readableTarget = RenderTexture.GetTemporary(
                texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
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
            int minX = texture.width, minY = texture.height, maxX = -1, maxY = -1, nonzero = 0;
            for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
            {
                if (pixels[y * texture.width + x].a == 0) continue;
                nonzero++;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            return new Dictionary<string, string>
            {
                ["def"] = ThingDefOf.Door.defName,
                ["path"] = ThingDefOf.Door.graphicData.texPath,
                ["material"] = material.name,
                ["texture"] = texture.name,
                ["dimensions"] = texture.width + "x" + texture.height,
                ["alphaBounds"] = minX + "," + minY + ".." + (maxX + 1) + "," + (maxY + 1),
                ["nonzeroAlpha"] = nonzero.ToString(),
            };
        });
    }
}
