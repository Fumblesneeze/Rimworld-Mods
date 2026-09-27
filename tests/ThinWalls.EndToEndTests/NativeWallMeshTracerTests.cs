using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest("thin-walls.native-material-straight-tracer", "fumblesneeze.thinwalls",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "fumblesneeze.thinwalls",
    MaxFrames = 5000, MaxGameTicks = 3000, MaxWallClockSeconds = 180)]
public sealed class NativeWallMeshTracerTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private IntVec3 center;
    private EndToEndGizmoOption build = null!;
    private EndToEndGizmoOption coreBuild = null!;
    private readonly List<Thing> displaced = new();
    private readonly Dictionary<IntVec3, TerrainDef> terrain = new();
    private readonly string[] materials = { "BlocksGranite", "WoodLog", "Steel" };

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        center = map.Center;
        var originalBuildings = new HashSet<Building>(map.listerThings.AllThings.OfType<Building>());
        bool god = DebugSettings.godMode;
        bool screenshot = Find.ScreenshotModeHandler.Active;
        int ticks = Find.TickManager.TicksGame;
        int start = Find.TickManager.gameStartAbsTick;
        WeatherDef weather = map.weatherManager.curWeather, lastWeather = map.weatherManager.lastWeather;
        int age = map.weatherManager.curWeatherAge;
        float prevLerp = map.weatherManager.prevSkyTargetLerp, currLerp = map.weatherManager.currSkyTargetLerp;
        context.DeferCleanup(() =>
        {
            foreach (var wall in Samples().Where(wall => !originalBuildings.Contains(wall))) wall.Destroy(DestroyMode.Vanish);
            foreach (var entry in terrain) map.terrainGrid.SetTerrain(entry.Key, entry.Value);
            foreach (var thing in displaced) if (!thing.Destroyed && !thing.Spawned) GenSpawn.Spawn(thing, thing.Position, map, thing.Rotation);
            DebugSettings.godMode = god;
            Find.ScreenshotModeHandler.Active = screenshot;
            Find.TickManager.DebugSetTicksGame(ticks);
            Find.TickManager.gameStartAbsTick = start;
            map.weatherManager.curWeather = weather;
            map.weatherManager.lastWeather = lastWeather;
            map.weatherManager.curWeatherAge = age;
            map.weatherManager.prevSkyTargetLerp = prevLerp;
            map.weatherManager.currSkyTargetLerp = currLerp;
            map.weatherManager.ResetSkyTargetLerpCache();
        });
        foreach (var cell in CellRect.CenteredOn(center, 20).Cells)
        {
            foreach (var thing in cell.GetThingList(map).ToArray())
                if (thing.Spawned) { displaced.Add(thing); thing.DeSpawn(); }
            terrain[cell] = map.terrainGrid.TerrainAt(cell);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
        }
        DebugSettings.godMode = true;
        Find.TickManager.DebugSetTicksGame(0);
        Find.TickManager.gameStartAbsTick = GenDate.TicksPerYear + 30_000;
        for (int pass = 0; pass < 2; pass++)
            Find.TickManager.gameStartAbsTick += 30_000 -
                (GenLocalDate.DayOfYear(map) * GenDate.TicksPerDay + GenLocalDate.DayTick(map));
        map.weatherManager.curWeather = map.weatherManager.lastWeather = WeatherDefOf.Clear;
        map.weatherManager.curWeatherAge = 0;
        map.weatherManager.prevSkyTargetLerp = map.weatherManager.currSkyTargetLerp = 1f;
        map.weatherManager.ResetSkyTargetLerpCache();
        build = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(Array.Empty<string>(), new[] { "Structure" })
            .Single(option => !option.Disabled && option.BuildableDefName == ThinWallUtility.ThinWallDefName &&
                              option.Interaction == EndToEndGizmoInteraction.Drag);
        coreBuild = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(Array.Empty<string>(), new[] { "Structure" })
            .Single(option => !option.Disabled && option.BuildableDefName == "Wall" &&
                              option.Interaction == EndToEndGizmoInteraction.Drag);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        if (Find.WindowStack.Windows.Any(window => window.GetType().FullName == "LudeonTK.EditWindow_Log"))
            yield return new WindowCancelActionStep("close startup log", "LudeonTK.EditWindow_Log");
        yield return new ScreenshotStep("before native wall designations", Array.Empty<string>(), 0);
        for (int i = 0; i < materials.Length; i++)
        {
            yield return Drag(materials[i] + " horizontal native material run", -10, 8 - i * 6, -4, 8 - i * 6,
                EndToEndCardinalRotation.South, materials[i]);
            yield return Drag(materials[i] + " vertical native material run", 1 + i * 5, 8, 1 + i * 5, 2,
                EndToEndCardinalRotation.West, materials[i]);
            yield return Drag(materials[i] + " horizontal Core comparator", -10, 6 - i * 6, -4, 6 - i * 6,
                null, materials[i], coreBuild);
            yield return Drag(materials[i] + " vertical Core comparator", 3 + i * 5, 8, 3 + i * 5, 2,
                null, materials[i], coreBuild);
        }
        yield return new WaitUntilStep("native designator creates all 42 walls", _ => Walls().Length == 42,
            new EndToEndDeadline(300, 600, TimeSpan.FromSeconds(20)));
        yield return new AssertionStep("native Core comparators complete", _ =>
            EndToEndAssert.Equal(42, Samples().Count(wall => wall.def == ThingDefOf.Wall), "All 42 Core reference walls must be built."));
        yield return new ScreenshotModeActionStep("clean technical render evidence", true);
        yield return new CheckpointStep("native action result", _ => new Dictionary<string, string>
        {
            ["wallCount"] = Walls().Length.ToString(),
            ["materials"] = string.Join(",", Walls().Select(wall => wall.Stuff.defName).Distinct()),
            ["scope"] = "native-source straight runs and endpoints beside unchanged Core walls; three materials, both orientations"
        });
        var ids = Samples().Select(wall => wall.ThingID).ToArray();
        yield return new CameraActionStep("overview of all three materials", ids, paddingPixels: 100);
        yield return new ScreenshotStep("all native material straight runs", ids, 100);
        foreach (string material in materials)
        {
            var sample = Samples().Where(wall => wall.Stuff.defName == material && wall.Position.x < center.x)
                .Select(wall => wall.ThingID).ToArray();
            yield return new CameraActionStep("close horizontal " + material, sample, paddingPixels: 60);
            yield return new ScreenshotStep("close horizontal " + material, sample, 60);
            sample = Samples().Where(wall => wall.Stuff.defName == material && wall.Position.x > center.x)
                .Select(wall => wall.ThingID).ToArray();
            yield return new CameraActionStep("close vertical " + material, sample, paddingPixels: 60);
            yield return new ScreenshotStep("close vertical " + material, sample, 60);
        }
        for (int i = 0; i < materials.Length; i++)
        {
            yield return Drag(materials[i] + " South stem into regular row", -7, 5 - i * 6, -7, 3 - i * 6,
                EndToEndCardinalRotation.West, materials[i]);
            var sample = Samples().Where(wall => wall.Stuff.defName == materials[i] && wall.Position.x < center.x &&
                wall.Position.z <= center.z + 6 - i * 6).Select(wall => wall.ThingID).ToArray();
            yield return new CameraActionStep("South mixed-width contact " + materials[i], sample, paddingPixels: 80);
            yield return new ScreenshotStep("South mixed-width contact " + materials[i], sample, 80);
        }
        yield return new AssertionStep("all source-bound receiving contacts use native material textures", _ =>
        {
            EndToEndAssert.Equal(51, Walls().Length, "Native input must create all nine contact-stem segments.");
            var rendered = Samples().Select(wall => map.mapDrawer.SectionAt(wall.Position)).Distinct()
                .Select(section => section.GetLayer(typeof(SectionLayer_ThingsGeneral))).SelectMany(layer => layer.subMeshes)
                .Where(mesh => !mesh.disabled && mesh.mesh.vertexCount > 0).ToArray();
            EndToEndAssert.True(rendered.All(mesh => mesh.material.mainTexture?.name.StartsWith("TW_", StringComparison.Ordinal) != true),
                "Regular contact rendering must preserve native texture bindings, not the painted legacy contact atlas.");
        });
        for (int i = 0; i < materials.Length; i++)
        {
            int rowX = -12 + i * 8;
            yield return Drag(materials[i] + " isolated Core row for North contact", rowX, 11, rowX + 4, 11,
                null, materials[i], coreBuild);
            yield return Drag(materials[i] + " North stem into regular row", rowX + 1, 12, rowX + 1, 14,
                EndToEndCardinalRotation.East, materials[i]);
            var sample = Samples().Where(wall => wall.Stuff.defName == materials[i] &&
                wall.Position.x >= center.x + rowX && wall.Position.x <= center.x + rowX + 4 &&
                wall.Position.z >= center.z + 11).Select(wall => wall.ThingID).ToArray();
            yield return new CameraActionStep("North mixed-width contact " + materials[i], sample, paddingPixels: 80);
            yield return new ScreenshotStep("North mixed-width contact " + materials[i], sample, 30);
        }
        yield return new AssertionStep("north receiving contacts use only native material textures", _ =>
        {
            EndToEndAssert.Equal(60, Walls().Length, "All nine North contact-stem segments must exist.");
            EndToEndAssert.Equal(57, Samples().Count(wall => wall.def == ThingDefOf.Wall), "All fifteen North comparator cells must exist.");
            var rendered = Samples().Select(wall => map.mapDrawer.SectionAt(wall.Position)).Distinct()
                .Select(section => section.GetLayer(typeof(SectionLayer_ThingsGeneral))).SelectMany(layer => layer.subMeshes)
                .Where(mesh => !mesh.disabled && mesh.mesh.vertexCount > 0).ToArray();
            EndToEndAssert.True(rendered.All(mesh => mesh.material.mainTexture?.name.StartsWith("TW_", StringComparison.Ordinal) != true),
                "North contact rendering must not fall back to a painted Thin Walls atlas.");
        });
        for (int i = 0; i < materials.Length; i++)
        {
            int rowZ = -12 + i * 8;
            yield return Drag(materials[i] + " isolated Core column for East contact", 16, rowZ, 16, rowZ + 4,
                null, materials[i], coreBuild);
            yield return Drag(materials[i] + " East stem into regular column", 17, rowZ + 2, 19, rowZ + 2,
                EndToEndCardinalRotation.South, materials[i]);
            var east = Samples().Where(wall => wall.Stuff.defName == materials[i] && wall.Position.x >= center.x + 16 &&
                wall.Position.z >= center.z + rowZ && wall.Position.z <= center.z + rowZ + 4)
                .Select(wall => wall.ThingID).ToArray();
            yield return new CameraActionStep("East mixed-width contact " + materials[i], east, paddingPixels: 80);
            yield return new ScreenshotStep("East mixed-width contact " + materials[i], east, 30);

            yield return Drag(materials[i] + " isolated Core column for West contact", -15, rowZ, -15, rowZ + 4,
                null, materials[i], coreBuild);
            yield return Drag(materials[i] + " West stem into regular column", -16, rowZ + 1, -18, rowZ + 1,
                EndToEndCardinalRotation.North, materials[i]);
            var west = Samples().Where(wall => wall.Stuff.defName == materials[i] && wall.Position.x <= center.x - 15 &&
                wall.Position.z >= center.z + rowZ && wall.Position.z <= center.z + rowZ + 4)
                .Select(wall => wall.ThingID).ToArray();
            yield return new CameraActionStep("West mixed-width contact " + materials[i], west, paddingPixels: 80);
            yield return new ScreenshotStep("West mixed-width contact " + materials[i], west, 30);
        }
        yield return new AssertionStep("east and west receiving contacts use only native material textures", _ =>
        {
            EndToEndAssert.Equal(78, Walls().Length, "All eighteen east/west contact-stem segments must exist.");
            EndToEndAssert.Equal(87, Samples().Count(wall => wall.def == ThingDefOf.Wall), "All thirty east/west comparator cells must exist.");
            string[] painted = PaintedSampleMaterials();
            EndToEndAssert.Equal(0, painted.Length,
                "East/west contacts must not use a painted Thin Walls atlas. Found: " + string.Join("; ", painted));
        });
        yield return Drag("granite Core row for contrasting south contact", -4, -16, 0, -16,
            null, "BlocksGranite", coreBuild);
        yield return Drag("wood Thin stem into contrasting granite row", -2, -17, -2, -19,
            EndToEndCardinalRotation.West, "WoodLog");
        var contrasting = Samples().Where(wall =>
                wall.Position.x >= center.x - 4 && wall.Position.x <= center.x &&
                wall.Position.z >= center.z - 19 && wall.Position.z <= center.z - 16)
            .Select(wall => wall.ThingID).ToArray();
        yield return new AssertionStep("contrasting contact keeps both independently sourced materials", _ =>
        {
            Building[] sample = Samples().Where(wall => contrasting.Contains(wall.ThingID)).ToArray();
            EndToEndAssert.True(sample.Any(wall => wall.def == ThingDefOf.Wall &&
                                                  wall.Stuff?.defName == "BlocksGranite"),
                "The receiving row must remain granite Core walls.");
            EndToEndAssert.True(sample.Any(wall => wall is Building_ThinWall &&
                                                  wall.Stuff?.defName == "WoodLog"),
                "The joining edge arm must remain a wood Thin Wall.");
            EndToEndAssert.Equal(0, PaintedSampleMaterials().Length,
                "Contrasting Stuff must still use installed Core source textures only.");
        });
        yield return new CameraActionStep("close contrasting Core and Thin material contact", contrasting, paddingPixels: 80);
        yield return new ScreenshotStep("wood Thin diagonal joins granite Core wall without a butt line", contrasting, 80);
        string[] PaintedSampleMaterials()
        {
            return Samples().Select(wall => map.mapDrawer.SectionAt(wall.Position)).Distinct()
                .SelectMany(section => section.GetLayer(typeof(SectionLayer_ThingsGeneral)).subMeshes
                    .Where(mesh => !mesh.disabled && mesh.mesh.vertexCount > 0 &&
                                   mesh.material.mainTexture?.name.StartsWith("TW_", StringComparison.Ordinal) == true)
                    .Select(mesh => section.botLeft + ":" + mesh.material.name + "/" + mesh.material.mainTexture.name))
                .Distinct().OrderBy(value => value).ToArray();
        }
    }

    private Building_ThinWall[] Walls() => map.listerThings.AllThings.OfType<Building_ThinWall>()
        .Where(wall => CellRect.CenteredOn(center, 20).Contains(wall.Position)).ToArray();

    private Building[] Samples() => map.listerThings.AllThings.OfType<Building>()
        .Where(wall => (wall is Building_ThinWall || wall.def == ThingDefOf.Wall) && CellRect.CenteredOn(center, 20).Contains(wall.Position)).ToArray();

    private GizmoActionStep Drag(string name, int x, int z, int endX, int endZ,
        EndToEndCardinalRotation? rotation, string material, EndToEndGizmoOption? option = null)
    {
        option ??= build;
        var start = new EndToEndMapCell(center.x + x, center.z + z);
        var end = new EndToEndMapCell(center.x + endX, center.z + endZ);
        return rotation.HasValue
            ? new GizmoActionStep(name, Array.Empty<string>(), option.RuntimeType, EndToEndGizmoInteraction.Drag,
                rotation.Value, new EndToEndBuildMaterial(material), option.StableId, start, end, new[] { "Structure" })
            : new GizmoActionStep(name, Array.Empty<string>(), option.RuntimeType, EndToEndGizmoInteraction.Drag,
                new EndToEndBuildMaterial(material), option.StableId, start, end, new[] { "Structure" });
    }
}
