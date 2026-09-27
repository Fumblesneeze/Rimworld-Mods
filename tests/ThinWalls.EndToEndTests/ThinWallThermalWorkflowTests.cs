using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using ThinWalls.Pathing;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest("thin-walls.native-thermal-rooms", "fumblesneeze.thinwalls",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "fumblesneeze.thinwalls",
    MaxFrames = 12_000, MaxGameTicks = 12_000, MaxWallClockSeconds = 260)]
public sealed class ThinWallThermalWorkflowTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private IntVec3 root, inside, control, pocket;
    private CellRect footprint;
    private readonly List<string> fixtures = new();
    private readonly Dictionary<string, string> readings = new();
    private bool originalGod, originalOverlay;
    private string fireId = "";

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap;
        root = map.Center - new IntVec3(9, 0, 4);
        footprint = new CellRect(root.x, root.z, 18, 9);
        inside = root + new IntVec3(2, 0, 2);
        control = root + new IntVec3(10, 0, 3);
        pocket = root + new IntVec3(6, 0, 5);
        originalGod = DebugSettings.godMode;
        originalOverlay = Find.PlaySettings.showTemperatureOverlay;
        DebugSettings.godMode = true;
        foreach (IntVec3 cell in footprint.ExpandedBy(1))
        {
            map.roofGrid.SetRoof(cell, null);
            foreach (Thing thing in cell.GetThingList(map).ToArray())
                if (thing.def.destroyable) thing.Destroy(DestroyMode.Vanish);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
            map.fogGrid.Unfog(cell);
        }
        foreach (IntVec3 cell in footprint.EdgeCells) SpawnWall(cell);
        foreach (IntVec3 cell in new CellRect(control.x - 1, control.z - 1, 4, 4).EdgeCells) SpawnWall(cell);
        // Supports and the control room are disposable preconditions, not construction evidence.
        foreach (IntVec3 cell in footprint) map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
        context.DeferCleanup(() =>
        {
            DebugSettings.godMode = originalGod;
            Find.PlaySettings.showTemperatureOverlay = originalOverlay;
            Map current = Find.CurrentMap;
            foreach (IntVec3 cell in footprint) current.roofGrid.SetRoof(cell, null);
            foreach (Thing thing in current.listerThings.AllThings.Where(t => footprint.Contains(t.Position)).ToArray())
                if (thing.def.destroyable) thing.Destroy(DestroyMode.Vanish);
            string save = GenFilePaths.FilePathForSavedGame("thin-wall-thermal-test");
            if (File.Exists(save)) File.Delete(save);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CameraActionStep("frame unpartitioned room and regular-wall control", fixtures, paddingPixels: 60);
        yield return new ScreenshotStep("before native thin enclosure construction", fixtures, 60);
        for (int n = 0; n < 2; n++)
        {
            yield return Build(context, ThinWallUtility.ThinWallDefName, inside + IntVec3.East * n, EndToEndCardinalRotation.South);
            yield return Build(context, ThinWallUtility.ThinWallDefName, inside + IntVec3.North + IntVec3.East * n, EndToEndCardinalRotation.North);
            yield return Build(context, ThinWallUtility.ThinWallDefName, inside + IntVec3.North * n, EndToEndCardinalRotation.West);
            yield return Build(context, ThinWallUtility.ThinWallDefName, inside + IntVec3.East + IntVec3.North * n, EndToEndCardinalRotation.East);
        }
        // Four co-located orientations also exercise the one-cell U-plus-closing-edge case.
        foreach (EndToEndCardinalRotation rotation in Enum.GetValues(typeof(EndToEndCardinalRotation)))
            yield return Build(context, ThinWallUtility.ThinWallDefName, pocket, rotation);
        yield return Build(context, "ShelfSmall", pocket, EndToEndCardinalRotation.North);
        yield return new AssertionStep("native construction creates four-cell and furnished one-cell rooms", _ =>
        {
            EndToEndAssert.Equal(4, inside.GetRoom(map).CellCount, "Thin enclosure must retain four air cells.");
            EndToEndAssert.Equal(1, pocket.GetRoom(map).CellCount, "Co-located U/closing edge must retain one furnished cell.");
            EndToEndAssert.False(inside.GetRoom(map).UsesOutdoorTemperature, "Supported roofed enclosure is indoor.");
            foreach (Building_ThinWall wall in map.listerThings.AllThings.OfType<Building_ThinWall>())
                EndToEndAssert.False(wall.def.holdsRoof, "Thermal integration adds no roof support.");
        });
        yield return new ScreenshotStep("native thin rooms and co-located shelf after construction", fixtures, 60);
        yield return new AssertionStep("supporting measured Core coefficient and conservative native component wiring", _ => CompareConduction());
        yield return new CheckpointStep("thermal coefficient measurements", _ => new Dictionary<string, string>(readings));
        foreach (EndToEndStep step in ObserveNativeCooling("before reload")) yield return step;

        // Cold initial conditions are setup. The following native build starts actual heat production.
        foreach (Room room in map.regionGrid.AllRooms) if (!room.UsesOutdoorTemperature) room.Temperature = 0f;
        yield return Build(context, "Campfire", inside, null);
        fireId = inside.GetThingList(map).Single(t => t.def.defName == "Campfire").ThingID;
        yield return new SelectionActionStep("select newly built native heat source", new[] { fireId }, false);
        Find.PlaySettings.showTemperatureOverlay = true;
        yield return new ScreenshotStep("before heat evolves in the native thin room", Array.Empty<string>(), 0);
        int startTick = Find.TickManager.TicksGame;
        yield return new TimeControlActionStep("run normal room heat simulation", false, EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep("native campfire warms the enclosed thin room", _ =>
            Find.TickManager.TicksGame - startTick >= 1200 && inside.GetRoom(map).Temperature > 8f,
            new EndToEndDeadline(3000, 4000, TimeSpan.FromSeconds(55)));
        yield return new TimeControlActionStep("pause observable thermal result", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep("native heater and thin-room temperatures remain distinct", _ =>
        {
            EndToEndAssert.True(inside.GetRoom(map).Temperature > Surrounding().Temperature + 3f,
                "Thin walls insulate finitely while native campfire heating remains effective.");
            readings["heatedRoom"] = F(inside.GetRoom(map).Temperature);
            readings["surroundingRoom"] = F(Surrounding().Temperature);
            readings["regularControlRoom"] = F(control.GetRoom(map).Temperature);
        });
        yield return new ScreenshotStep("native temperature overlay after campfire heating", Array.Empty<string>(), 0);
        float savedTemperature = inside.GetRoom(map).Temperature;
        yield return new SaveLoadActionStep("save and reload finite insulated rooms", "thin-wall-thermal-test");
        map = Find.CurrentMap;
        yield return new AssertionStep("reload retains room volume and temperature", _ =>
        {
            EndToEndAssert.Equal(4, inside.GetRoom(map).CellCount, "Reloaded room volume.");
            EndToEndAssert.True(Math.Abs(savedTemperature - inside.GetRoom(map).Temperature) < 0.2f,
                "Native cached temperatures must survive reload.");
            EndToEndAssert.Equal(1, pocket.GetRoom(map).CellCount, "Reloaded furnished pocket.");
        });
        yield return new ScreenshotStep("reloaded heated enclosure", Array.Empty<string>(), 0);
        DebugSettings.godMode = true;
        var removeFire = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { fireId }, Array.Empty<string>())
            .Single(g => !g.Disabled && g.Label.IndexOf("deconstruct", StringComparison.OrdinalIgnoreCase) >= 0);
        yield return new GizmoActionStep("native deconstruct stops the heat source after reload", new[] { fireId },
            removeFire.RuntimeType, removeFire.Interaction!.Value, stableGizmoId: removeFire.StableId);
        foreach (EndToEndStep step in ObserveNativeCooling("after reload")) yield return step;
        Building_ThinWall breach = inside.GetThingList(map).OfType<Building_ThinWall>().Single(t => t.OwnedSide == ThinWallSide.South);
        var deconstruct = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { breach.ThingID }, Array.Empty<string>())
            .Single(g => !g.Disabled && g.Label.IndexOf("deconstruct", StringComparison.OrdinalIgnoreCase) >= 0);
        yield return new GizmoActionStep("native deconstruct breaches the heated room", new[] { breach.ThingID },
            deconstruct.RuntimeType, deconstruct.Interaction!.Value, stableGizmoId: deconstruct.StableId);
        yield return new AssertionStep("breach rejoins surrounding air without invalid temperature", _ =>
        {
            EndToEndAssert.True(ReferenceEquals(inside.GetRoom(map), Surrounding()), "Native room must merge on breach.");
            EndToEndAssert.False(float.IsNaN(inside.GetRoom(map).Temperature), "Merged temperature is finite.");
            readings["breachedTemperature"] = F(inside.GetRoom(map).Temperature);
        });
        yield return new ScreenshotStep("temperature overlay after native breach", Array.Empty<string>(), 0);
        Thing outerWall = (root + IntVec3.East).GetEdifice(map);
        var openOutside = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { outerWall.ThingID }, Array.Empty<string>())
            .Single(g => !g.Disabled && g.Label.IndexOf("deconstruct", StringComparison.OrdinalIgnoreCase) >= 0);
        yield return new GizmoActionStep("native breach exposes the surrounding room to outside", new[] { outerWall.ThingID },
            openOutside.RuntimeType, openOutside.Interaction!.Value, stableGizmoId: openOutside.StableId);
        yield return new AssertionStep("only the furnished pocket remains enclosed against outside air", _ =>
        {
            EndToEndAssert.True(Surrounding().UsesOutdoorTemperature, "Opened outer room uses outdoor air.");
            EndToEndAssert.False(pocket.GetRoom(map).UsesOutdoorTemperature, "Furnished pocket remains roofed and enclosed.");
        });
        float pocketStart = map.mapTemperature.OutdoorTemp + 40f;
        pocket.GetRoom(map).Temperature = pocketStart;
        yield return new ScreenshotStep("hot pocket before native outdoor heat exchange", Array.Empty<string>(), 0);
        int outdoorTick = Find.TickManager.TicksGame / 120 * 120 + 7;
        if (outdoorTick <= Find.TickManager.TicksGame) outdoorTick += 120;
        yield return new TimeControlActionStep("run native outdoor reservoir exchange", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("outdoor exchange interval elapses", _ => Find.TickManager.TicksGame >= outdoorTick + 2,
            new EndToEndDeadline(900, 600, TimeSpan.FromSeconds(25)));
        yield return new TimeControlActionStep("pause pocket after outdoor exchange", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep("four physical edges exchange with fixed outdoors exactly once", _ =>
        {
            EndToEndAssert.True(Find.TickManager.TicksGame < outdoorTick + 120, "One outdoor interval only.");
            float ambient = Surrounding().Temperature;
            double expected = ambient + (pocketStart + (ambient - pocketStart) * 0.006f - ambient) * Math.Pow(1d - 0.0408d, 4);
            readings["outdoor reservoir"] = F(ambient);
            readings["outdoor pocket actual"] = F(pocket.GetRoom(map).Temperature);
            readings["outdoor pocket expected including native roof"] = expected.ToString("R", CultureInfo.InvariantCulture);
            EndToEndAssert.True(Math.Abs(pocket.GetRoom(map).Temperature - expected) < 0.02f,
                "Actual pocket cooling includes one native roof term and four half-resistance edges.");
            EndToEndAssert.True(Math.Abs(ambient - map.mapTemperature.OutdoorTemp) < 0.1f, "Thin walls do not heat the outdoor reservoir.");
        });
        yield return new ScreenshotStep("native outdoor exchange cools the still-enclosed pocket", Array.Empty<string>(), 0);
        yield return new CheckpointStep("final thermal lifecycle measurements", _ => new Dictionary<string, string>(readings));
        // Diagnostic observation only: retain the native console, not just the host log search.
        var console = new LudeonTK.EditWindow_Log();
        Find.WindowStack.Add(console);
        context.DeferCleanup(() => Find.WindowStack.TryRemove(console, false));
        yield return new ScreenshotStep("native developer console after thermal workflow", Array.Empty<string>(), 0);
        yield return new WindowCancelActionStep("close inspected native developer console", "LudeonTK.EditWindow_Log");
    }

    private IEnumerable<EndToEndStep> ObserveNativeCooling(string stage)
    {
        // Identical hot initial conditions are arrangement, never a claimed outcome.
        foreach (Room room in map.regionGrid.AllRooms) if (!room.UsesOutdoorTemperature) room.Temperature = 10f;
        inside.GetRoom(map).Temperature = control.GetRoom(map).Temperature = 40f;
        Find.PlaySettings.showTemperatureOverlay = true;
        yield return new ScreenshotStep(stage + ": equally hot native rooms before ordinary thermal tick", Array.Empty<string>(), 0);
        int now = Find.TickManager.TicksGame;
        int nextInterval = now / 120 * 120 + 7;
        if (nextInterval <= now) nextInterval += 120;
        yield return new TimeControlActionStep(stage + ": resume ordinary native thermal scheduling", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(stage + ": ordinary thermal interval elapses", _ => Find.TickManager.TicksGame >= nextInterval + 2,
            new EndToEndDeadline(900, 600, TimeSpan.FromSeconds(25)));
        yield return new TimeControlActionStep(stage + ": pause after ordinary thermal interval", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep(stage + ": thin room actually loses more heat through native ticks", _ =>
        {
            EndToEndAssert.True(Find.TickManager.TicksGame < nextInterval + 120, "Exactly one ordinary interval is sampled.");
            float thinLoss = 40f - inside.GetRoom(map).Temperature;
            float coreLoss = 40f - control.GetRoom(map).Temperature;
            readings[stage + " native thin loss"] = F(thinLoss);
            readings[stage + " native Core loss"] = F(coreLoss);
            EndToEndAssert.True(thinLoss > coreLoss * 1.5f && thinLoss < coreLoss * 2.3f,
                "Same-volume/roof rooms must visibly cool at different rates under ordinary scheduling, not just fast ecology: thin=" + F(thinLoss) + ", Core=" + F(coreLoss));
        });
        yield return new ScreenshotStep(stage + ": ordinary thermal tick cooled thin room faster", Array.Empty<string>(), 0);
    }

    private void CompareConduction()
    {
        Room hot = inside.GetRoom(map), cold = Surrounding(), normal = control.GetRoom(map), tiny = pocket.GetRoom(map);
        foreach (Room room in map.regionGrid.AllRooms) if (!room.UsesOutdoorTemperature) room.Temperature = 10f;
        hot.Temperature = normal.Temperature = 40f;
        var method = typeof(RoomTempTracker).GetMethod("WallEqualizationTempChangePerInterval", BindingFlags.NonPublic | BindingFlags.Instance)!;
        float coreDelta = (float)method.Invoke(normal.TempTracker, null);
        EndToEndAssert.Equal(8, normal.TempTracker.EqualizeCellsForReading.Count, "Measured native control boundary count.");
        EndToEndAssert.True(Math.Abs(coreDelta - (-30f * 8f * 120f * 0.00017f / 4f)) < 0.0001f,
            "Installed Core must match measured wall coefficient; delta=" + F(coreDelta) +
            "; targets=" + string.Join(",", normal.TempTracker.EqualizeCellsForReading.Select(c => c + "=" + F(c.GetTemperature(map)))));
        float energyBefore = hot.Temperature * hot.CellCount + cold.Temperature * cold.CellCount + tiny.Temperature;
        bool fastEcology = DebugSettings.fastEcology;
        try
        {
            // Diagnostic only: one actual component interval, not the native gameplay evidence below.
            DebugSettings.fastEcology = true;
            map.GetComponent<ThinWallMapComponent>().MapComponentTick();
        }
        finally { DebugSettings.fastEcology = fastEcology; }
        float thinDelta = hot.Temperature - 40f;
        float ratio = thinDelta / coreDelta;
        readings["coreDelta"] = F(coreDelta);
        readings["thinDelta"] = F(thinDelta);
        readings["intervalRatioWithSequentialFeedback"] = F(ratio);
        EndToEndAssert.True(ratio > 1.9f && ratio <= 2.001f,
            "Thin wall interval must use twice Core conductance (small sequential feedback allowed); ratio=" + F(ratio));
        float energyAfter = hot.Temperature * hot.CellCount + cold.Temperature * cold.CellCount + tiny.Temperature;
        EndToEndAssert.True(Math.Abs(energyAfter - energyBefore) < 0.005f, "Finite-room edge transfer conserves temperature-cell energy.");
        EndToEndAssert.True(tiny.Temperature >= 10f && tiny.Temperature <= cold.Temperature,
            "One-cell pocket must remain finite and within endpoint temperatures.");
    }

    private Room Surrounding() => (root + new IntVec3(1, 0, 1)).GetRoom(map);
    private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
    private void SpawnWall(IntVec3 cell)
    {
        Thing wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.BlocksGranite);
        wall.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(wall, cell, map);
        fixtures.Add(wall.ThingID);
    }
    private EndToEndStep Build(IEndToEndContext context, string defName, IntVec3 cell, EndToEndCardinalRotation? rotation)
    {
        string category = defName == "Campfire" ? "Temperature" : defName == "ShelfSmall" ? "Furniture" : "Structure";
        var gizmo = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(Array.Empty<string>(), new[] { category })
            .Single(g => !g.Disabled && g.BuildableDefName == defName);
        if (!rotation.HasValue)
            return new GizmoActionStep("native build " + defName, Array.Empty<string>(), gizmo.RuntimeType, gizmo.Interaction!.Value,
                stableGizmoId: gizmo.StableId, startCell: new EndToEndMapCell(cell.x, cell.z),
                architectCategoryDefNames: new[] { category });
        return new GizmoActionStep("native build " + defName + " " + cell + " " + rotation, Array.Empty<string>(), gizmo.RuntimeType,
            gizmo.Interaction!.Value, rotation.Value,
            new EndToEndBuildMaterial(defName == ThinWallUtility.ThinWallDefName ? new[] { "WoodLog", "Steel", "BlocksGranite" }[(cell.x + cell.z) % 3] : "BlocksGranite"),
            stableGizmoId: gizmo.StableId, startCell: new EndToEndMapCell(cell.x, cell.z), endCell: new EndToEndMapCell(cell.x, cell.z),
            architectCategoryDefNames: new[] { category });
    }
}
