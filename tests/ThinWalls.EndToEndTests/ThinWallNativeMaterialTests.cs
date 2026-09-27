using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using ThinWalls.Rendering;
using UnityEngine;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest("thin-walls.native-material-bindings",
    "fumblesneeze.thinwalls", "brrainz.harmony", EndToEndTestContract.CorePackageId,
    "fumblesneeze.thinwalls", MaxFrames = 5000, MaxGameTicks = 5000, MaxWallClockSeconds = 140)]
public sealed class ThinWallNativeMaterialTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private IntVec3 center;
    private readonly List<Building_ThinWall> walls = new();
    private EndToEndGizmoOption build = null!;

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap;
        center = map.Center;
        bool originalGod = DebugSettings.godMode;
        DebugSettings.godMode = true;
        context.DeferCleanup(() => DebugSettings.godMode = originalGod);
        context.DeferCleanup(() =>
        {
            foreach (var wall in walls) if (wall.Spawned) wall.Destroy(DestroyMode.Vanish);
        });
        build = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(Array.Empty<string>(), new[] { "Structure" })
            .Single(x => x.BuildableDefName == "TW_ThinWall" && !x.Disabled);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause native material placement", true, EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep("before material-family wall placements", Array.Empty<string>(), 0);
        string[] stuffs = { "WoodLog", "BlocksGranite", "Steel", "Plasteel", "Uranium", "Gold", "Jade" };
        for (int i = 0; i < stuffs.Length; i++)
        {
            IntVec3 cell = center + new IntVec3(i * 2 - 6, 0, 0);
            yield return new GizmoActionStep("native place " + stuffs[i] + " wall", Array.Empty<string>(), build.RuntimeType,
                EndToEndGizmoInteraction.Drag, EndToEndCardinalRotation.South, new EndToEndBuildMaterial(stuffs[i]),
                stableGizmoId: build.StableId, startCell: new EndToEndMapCell(cell.x, cell.z),
                endCell: new EndToEndMapCell(cell.x, cell.z), architectCategoryDefNames: new[] { "Structure" });
            walls.Add(cell.GetThingList(map).OfType<Building_ThinWall>().Single());
        }
        yield return new CameraActionStep("frame native Stuff catalog", walls.Select(x => x.ThingID), 70);
        yield return new ScreenshotStep("native material families render from source textures", Array.Empty<string>(), 0);
        yield return new SelectionActionStep("select placed wall and display native Build copy icon", new[] { walls[0].ThingID }, false);
        yield return new ScreenshotStep("selected wall uses native menu icon not full atlas", Array.Empty<string>(), 0);
        yield return new AssertionStep("native icon resolution respects menu icons for both thin definitions", _ =>
        {
            foreach (string defName in new[] { "TW_ThinWall", "TW_ThinDoor" })
            foreach (string stuffName in stuffs)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamed(defName);
                ThingDef stuff = DefDatabase<ThingDef>.GetNamed(stuffName);
                EndToEndAssert.True(Widgets.GetIconFor(def, stuff) == def.GetUIIconForStuff(stuff),
                    $"{defName}/{stuffName}: raw appearance atlas replaced the native UI icon.");
            }
        });
        yield return new AssertionStep("resolved material retains replacement texture mask transforms and shader bindings", _ =>
        {
            Material original = ThingDefOf.Wall.graphicData.Graphic.MatSingle;
            var replacement = new Material(ShaderDatabase.CutoutComplex)
            {
                mainTexture = original.mainTexture,
                mainTextureScale = new Vector2(.8f, .9f),
                mainTextureOffset = new Vector2(.1f, .05f),
                renderQueue = 2991,
            };
            replacement.SetTexture("_MaskTex", BaseContent.WhiteTex);
            Material colored = NativeWallMaterial.WithColors(replacement, Color.cyan, Color.magenta);
            EndToEndAssert.True(colored.mainTexture == replacement.mainTexture &&
                colored.GetTexture("_MaskTex") == replacement.GetTexture("_MaskTex") &&
                colored.mainTextureScale == replacement.mainTextureScale &&
                colored.mainTextureOffset == replacement.mainTextureOffset && colored.renderQueue == 2991 &&
                colored.shader == replacement.shader && colored.shaderKeywords.SequenceEqual(replacement.shaderKeywords),
                "Recoloring discarded native material bindings.");
            EndToEndAssert.True(colored.color == Color.cyan && colored.GetColor("_ColorTwo") == Color.magenta,
                "Primary/secondary color must reach the resolved material, not Graphic_Appearances.");
            Material linked = NativeWallMaterial.ForLinks(colored, 5);
            Vector2 expectedOffset = new Vector2(.1f, .05f) + Vector2.Scale(new Vector2(.28125f, .28125f), new Vector2(.8f, .9f));
            EndToEndAssert.True((linked.mainTextureOffset - expectedOffset).sqrMagnitude < .000001f &&
                linked.mainTextureScale == Vector2.Scale(new Vector2(3f / 16f, 3f / 16f), new Vector2(.8f, .9f)),
                "Linked sampling must compose normalized native slot UVs with the source transform.");
        });
    }
}
