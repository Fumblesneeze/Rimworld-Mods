using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Rendering;
using UnityEngine;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest("thin-walls.building-appearance-icons", "fumblesneeze.thinwalls",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "fumblesneeze.thinwalls",
    MaxFrames = 6000, MaxGameTicks = 1000, MaxWallClockSeconds = 90)]
public sealed class BuildingAppearanceIconWorkflowTest : IRimWorldEndToEndTest
{
    private Building shelf = null!;

    public void Arrange(IEndToEndContext context)
    {
        Map map = Find.CurrentMap;
        BuildingAppearanceFixture.NormalizeNoonWithCleanup(context, map);
        shelf = (Building)ThingMaker.MakeThing(ThingDef.Named("ShelfSmall"), ThingDefOf.WoodLog);
        shelf.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(shelf, map.Center, map);
        bool shrink = ThinWallsMod.Settings.ShowShrinkGizmo, offset = ThinWallsMod.Settings.ShowOffsetGizmo;
        float scale = Prefs.UIScale;
        bool disableTiny = Prefs.DisableTinyText;
        ThinWallsMod.Settings.ShowShrinkGizmo = ThinWallsMod.Settings.ShowOffsetGizmo = true;
        context.DeferCleanup(() =>
        {
            Prefs.UIScale = scale;
            Prefs.DisableTinyText = disableTiny;
            ThinWallsMod.Settings.ShowShrinkGizmo = shrink;
            ThinWallsMod.Settings.ShowOffsetGizmo = offset;
            if (shelf.Spawned) shelf.Destroy(DestroyMode.Vanish);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause icon workflow", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep("frame shelf", new[] { shelf.ThingID }, 160);
        yield return new SelectionActionStep("select shelf natively", new[] { shelf.ThingID }, false);
        Prefs.UIScale = 1f;
        yield return new ScreenshotStep("native icons 1x before", Array.Empty<string>(), 0);
        foreach (Type type in new[] { typeof(Command_ShrinkBuilding), typeof(Command_OffsetBuilding) })
        {
            var gizmo = context.GetRequiredService<IEndToEndGizmoCatalog>()
                .Query(new[] { shelf.ThingID }, Array.Empty<string>()).Single(x => x.RuntimeType == type.FullName && !x.Disabled);
            yield return new GizmoActionStep("native icon action " + type.Name, new[] { shelf.ThingID },
                gizmo.RuntimeType, EndToEndGizmoInteraction.Invoke, gizmo.StableId);
        }
        yield return new AssertionStep("icon commands retain actions and native textures", _ =>
        {
            EndToEndAssert.Equal(90, BuildingAppearanceControls.Get(shelf).ScalePercent, "Shrink failed.");
            EndToEndAssert.Equal(1, BuildingAppearanceControls.Get(shelf).OffsetStep, "Offset failed.");
            Texture2D arrow = ContentFinder<Texture2D>.Get("UI/Overlays/Arrow");
            EndToEndAssert.True(arrow != null && !ReferenceEquals(arrow, BaseContent.BadTex), "Missing native icon arrow.");
        });
        yield return new ScreenshotStep("native icons 1x after", Array.Empty<string>(), 0);
        Prefs.UIScale = 1.25f;
        yield return new ScreenshotStep("native icons 1.25x after", Array.Empty<string>(), 0);
        Prefs.DisableTinyText = true;
        yield return new ScreenshotStep("native icons larger label font", Array.Empty<string>(), 0);
        Prefs.DisableTinyText = false;
        // Diagnostic observation, not the native action/result proof above.
        Find.WindowStack.Add(new LudeonTK.EditWindow_Log());
        yield return new ScreenshotStep("native developer console", Array.Empty<string>(), 0);
        yield return new WindowCancelActionStep("close developer console", "LudeonTK.EditWindow_Log");
    }
}
