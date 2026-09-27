using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Rendering;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest("thin-walls.building-appearance-localization", "fumblesneeze.thinwalls",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "fumblesneeze.thinwalls",
    MaxFrames = 6000, MaxGameTicks = 1000, MaxWallClockSeconds = 90)]
public sealed class BuildingAppearanceLocalizationWorkflowTest : IRimWorldEndToEndTest
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
        ThinWallsMod.Settings.ShowShrinkGizmo = ThinWallsMod.Settings.ShowOffsetGizmo = true;
        context.DeferCleanup(() =>
        {
            ThinWallsMod.Settings.ShowShrinkGizmo = shrink;
            ThinWallsMod.Settings.ShowOffsetGizmo = offset;
            if (shelf.Spawned) shelf.Destroy(DestroyMode.Vanish);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause localized controls", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep("frame selected comparator", new[] { shelf.ThingID }, 160);
        yield return new SelectionActionStep("select shelf through native UI", new[] { shelf.ThingID }, false);
        yield return new ScreenshotStep("localized default gizmo labels", Array.Empty<string>(), 0);
        foreach (Type type in new[] { typeof(Command_ShrinkBuilding), typeof(Command_OffsetBuilding) })
        {
            var gizmo = context.GetRequiredService<IEndToEndGizmoCatalog>()
                .Query(new[] { shelf.ThingID }, Array.Empty<string>()).Single(x => x.RuntimeType == type.FullName);
            yield return new GizmoActionStep("native localized " + type.Name, new[] { shelf.ThingID },
                gizmo.RuntimeType, EndToEndGizmoInteraction.Invoke, gizmo.StableId);
        }
        yield return new AssertionStep("localized controls change appearance", _ =>
        {
            EndToEndAssert.Equal(90, BuildingAppearanceControls.Get(shelf).ScalePercent, "Scale action failed.");
            EndToEndAssert.Equal(1, BuildingAppearanceControls.Get(shelf).OffsetStep, "Offset action failed.");
        });
        yield return new ScreenshotStep("localized controls after actions", Array.Empty<string>(), 0);
        yield return new ModSettingsActionStep("open localized mod settings", ThinWallsMod.PackageId);
        yield return new ScreenshotStep("localized settings labels", Array.Empty<string>(), 0);
        yield return new WindowCancelActionStep("close localized settings", "RimWorld.Dialog_ModSettings");
    }
}

internal static class BuildingAppearanceFixture
{
    public static void NormalizeNoonWithCleanup(IEndToEndContext context, Map map)
    {
        int originalTicks = Find.TickManager.TicksGame;
        int originalStart = Find.TickManager.gameStartAbsTick;
        WeatherDef weather = map.weatherManager.curWeather, lastWeather = map.weatherManager.lastWeather;
        int age = map.weatherManager.curWeatherAge;
        float previousLerp = map.weatherManager.prevSkyTargetLerp, currentLerp = map.weatherManager.currSkyTargetLerp;
        int mapId = map.uniqueID;
        context.DeferCleanup(() =>
        {
            Find.TickManager.DebugSetTicksGame(originalTicks);
            Find.TickManager.gameStartAbsTick = originalStart;
            // Save/load replaces the Map object; restore the corresponding live map, not its old instance.
            Map live = Find.Maps.Single(x => x.uniqueID == mapId);
            live.weatherManager.curWeather = weather;
            live.weatherManager.lastWeather = lastWeather;
            live.weatherManager.curWeatherAge = age;
            live.weatherManager.prevSkyTargetLerp = previousLerp;
            live.weatherManager.currSkyTargetLerp = currentLerp;
            live.weatherManager.ResetSkyTargetLerpCache();
        });
        ThinWallConstructionLifecycleTest.NormalizeToClearNoon(map);
    }
}
