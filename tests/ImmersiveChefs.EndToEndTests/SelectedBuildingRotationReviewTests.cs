using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

// Final art gallery. Native designators place the packaged buildings; utility flags and
// loaded industrial endpoints are explicit picture preconditions, not washing acceptance.
[RimWorldEndToEndTest("immersive-chefs.selected-building-rotation-review", "fumblesneeze.immersivechefs",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "imranfish.xmlextensions",
    "fumblesneeze.immersivechefs", MaxFrames = 16000, MaxGameTicks = 8000, MaxWallClockSeconds = 360)]
public sealed class SelectedBuildingRotationReviewTest : IRimWorldEndToEndTest
{
    private readonly List<Thing> owned = new();
    private readonly List<(ThingDef Def, Rot4 Rotation)> fixtures = new();
    private readonly Dictionary<IntVec3, TerrainDef> floors = new();
    private readonly Dictionary<IntVec3, RoofDef> roofs = new();
    private Map map = null!;
    private IntVec3 placementCell;

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap;
        var origins = GenRadial.RadialCellsAround(map.Center, 60, true).Where(origin =>
            new CellRect(origin.x - 11, origin.z - 8, 23, 17).Cells.All(cell =>
                cell.InBounds(map) && !cell.Fogged(map) && cell.Standable(map) &&
                map.roofGrid.RoofAt(cell) is null && cell.GetThingList(map).Count == 0 &&
                !map.terrainGrid.TopTerrainAt(cell).layerable && map.terrainGrid.UnderTerrainAt(cell) is null &&
                map.terrainGrid.FoundationAt(cell) is null && map.terrainGrid.TempTerrainAt(cell) is null &&
                map.terrainGrid.ColorAt(cell) is null && map.snowGrid.GetDepth(cell) == 0 &&
                (map.sandGrid is null || map.sandGrid.GetDepth(cell) == 0))).Take(1).ToArray();
        EndToEndAssert.Equal(1, origins.Length, "The bounded search needs one clear flat 23 by 17 gallery pad.");
        placementCell = origins[0];
        var oldGod = DebugSettings.godMode;
        var oldScreenshot = Find.ScreenshotModeHandler.Active;
        context.DeferCleanup(() =>
        {
            var errors = new List<Exception>();
            void Attempt(Action action) { try { action(); } catch (Exception error) { errors.Add(error); } }
            Attempt(() => Find.Selector.ClearSelection());
            Attempt(() => DestroyOwnedSince(0));
            // Also clean a native placement if a following assertion failed before registration.
            foreach (var def in fixtures.Select(f => f.Def).Distinct())
                foreach (var thing in placementCell.GetThingList(map).Where(t => t.def == def).ToArray())
                    Attempt(() => thing.Destroy(DestroyMode.Vanish));
            foreach (var pair in floors) Attempt(() => map.terrainGrid.SetTerrain(pair.Key, pair.Value));
            foreach (var pair in roofs) Attempt(() => map.roofGrid.SetRoof(pair.Key, pair.Value));
            Attempt(() => DebugSettings.godMode = oldGod);
            Attempt(() => Find.ScreenshotModeHandler.Active = oldScreenshot);
            if (errors.Count != 0) throw new AggregateException("Gallery fixture cleanup failed.", errors);
        });
        DebugSettings.godMode = true;
        foreach (var cell in new CellRect(placementCell.x - 11, placementCell.z - 8, 23, 17).Cells)
        {
            floors[cell] = map.terrainGrid.TerrainAt(cell);
            roofs[cell] = map.roofGrid.RoofAt(cell);
            map.roofGrid.SetRoof(cell, null);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
        }
        var names = new[] { "Dishwasher", "IndustrialDishwasher", "PrepStation", "SauceStation",
            "MeatStation", "VegetableStation", "PastryStation", "Microwave" };
        foreach (var name in names)
        for (var direction = 0; direction < 4; direction++)
        {
            fixtures.Add((DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_" + name), new Rot4(direction)));
        }
        AddIndustrialPowerFixture();
    }

    private void AddIndustrialPowerFixture()
    {
        // One permanent support net powers each sequential pair, outside its cropped view.
        for (var x = -9; x <= 0; x++)
            owned.Add(BaseBuildingVisualCatalogTest.SpawnBuilding(map, placementCell + new IntVec3(x, 0, 0),
                "HiddenConduit", Rot4.North, null));
        for (var x = -9; x <= -8; x++)
            owned.Add(BaseBuildingVisualCatalogTest.SpawnBuilding(map, placementCell + new IntVec3(x, 0, 0),
                "VanometricPowerCell", Rot4.North, null));
    }

    private void DestroyOwnedSince(int firstIndex)
    {
        var errors = new List<Exception>();
        foreach (var thing in owned.Skip(firstIndex).Reverse().Where(t => !t.Destroyed).ToArray())
        {
            try { thing.Destroy(DestroyMode.Vanish); }
            catch (Exception error) { errors.Add(error); }
        }
        if (errors.Count != 0) throw new AggregateException("Gallery object cleanup failed.", errors);
        owned.RemoveRange(firstIndex, owned.Count - firstIndex);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new SupportingSceneTimeActionStep("set final gallery noon", "map-" + map.uniqueID, 720);
        yield return new TimeControlActionStep("settle native noon lighting", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("observe final gallery daylight", _ =>
            GenLocalDate.HourOfDay(map) == 12 && map.skyManager.CurSkyGlow > .95f,
            new EndToEndDeadline(900, 1200, TimeSpan.FromSeconds(25)));
        yield return new TimeControlActionStep("hold gallery daylight", true, EndToEndGameSpeed.Normal);
        foreach (var fixture in fixtures)
        {
            var firstOwned = owned.Count;
            var butcher = fixture.Def.defName is "ImmersiveChefs_PrepStation" or
                "ImmersiveChefs_MeatStation" or "ImmersiveChefs_VegetableStation";
            var reference = BaseBuildingVisualCatalogTest.SpawnBuilding(map,
                placementCell + new IntVec3(0, 0, 4), butcher ? "TableButcher" : "ElectricStove",
                fixture.Rotation, butcher ? ThingDefOf.Steel : null);
            owned.Add(reference);
            if (reference.TryGetComp<CompPowerTrader>() is { } beforePower) beforePower.PowerOn = true;
            if (fixture.Def.defName == "ImmersiveChefs_Microwave")
                owned.Add(BaseBuildingVisualCatalogTest.SpawnBuilding(map, placementCell,
                    "Table1x2c", fixture.Rotation, ThingDefOf.Steel));
            var key = fixture.Def.defName + "/" + fixture.Rotation.ToStringWord().ToLowerInvariant();
            yield return new CameraActionStep("frame native placement " + key, new[] { reference.ThingID }, 100);
            yield return new ScreenshotStep("before placement " + key, Array.Empty<string>(), 0);
            var option = context.GetRequiredService<IEndToEndGizmoCatalog>()
                .Query(Array.Empty<string>(), new[] { "Production" })
                .Single(g => g.BuildableDefName == fixture.Def.defName && !g.Disabled &&
                    g.Interaction == EndToEndGizmoInteraction.Place);
            yield return new GizmoActionStep("place selected building " + key, Array.Empty<string>(), option.RuntimeType,
                EndToEndGizmoInteraction.Place, stableGizmoId: option.StableId,
                startCell: new EndToEndMapCell(placementCell.x, placementCell.z),
                architectCategoryDefNames: new[] { "Production" },
                rotation: BaseBuildingVisualCatalogTest.EndToEndRotation(fixture.Rotation));
            yield return new WaitUntilStep("observe placed building " + key, _ => placementCell.GetThingList(map)
                .Any(t => t.def == fixture.Def && t.Rotation == fixture.Rotation),
                new EndToEndDeadline(180, 600, TimeSpan.FromSeconds(10)));
            var building = (Building)placementCell.GetThingList(map).Single(t => t.def == fixture.Def);
            owned.Add(building);
            if (building.def.defName == "ImmersiveChefs_IndustrialDishwasher")
            {
                DispenserE2EFixture.SettlePower(map, new[] { building }, 400);
                EndToEndAssert.NotNull(building.GetComp<CompPowerTrader>().PowerNet,
                    "The industrial picture precondition requires a real connected power network.");
            }
            else if (building.TryGetComp<CompPowerTrader>() is { } power) power.PowerOn = true;
            if (reference.TryGetComp<CompPowerTrader>() is { } referencePower) referencePower.PowerOn = true;
            yield return new SelectionActionStep("inspect placed building " + key, new[] { building.ThingID }, false);
            yield return new ScreenshotStep("after placement " + key, Array.Empty<string>(), 0);
            yield return new SelectionActionStep("clear gallery selection " + key, Array.Empty<string>(), false);
            yield return new ScreenshotModeActionStep("hide gallery UI " + key, true);
            foreach (var step in Capture(building, reference, key + "/open")) yield return step;
            if (building.def.defName == "ImmersiveChefs_IndustrialDishwasher")
            {
                // Supporting pose setup: real comp ownership drives the closed renderer, with ticks paused.
                var loader = HandwashingE2EFixture.CreateInactiveCleaner("Gallery loader");
                owned.Add(loader);
                var ware = (ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"), ThingDefOf.Steel);
                owned.Add(ware);
                ware.GetComp<CompSanitation>().MarkDirty();
                loader.carryTracker.innerContainer.TryAdd(ware);
                EndToEndAssert.True(building.GetComp<CompDishwasher>().TryAcceptFrom(loader),
                    "The explicit closed-endpoint precondition must enter actual component ownership.");
                yield return new WaitUntilStep("observe closed endpoint " + key, _ =>
                    building.GetComp<CompIndustrialDishwasherPresentation>().State == DishwasherPresentationState.Washing,
                    new EndToEndDeadline(240, 600, TimeSpan.FromSeconds(10)));
                foreach (var step in Capture(building, reference, key + "/closed")) yield return step;
            }
            yield return new ScreenshotModeActionStep("restore native UI " + key, false);
            DestroyOwnedSince(firstOwned);
        }
    }

    private IEnumerable<EndToEndStep> Capture(Building building, Building reference, string key)
    {
        var ids = new[] { building.ThingID, reference.ThingID };
        var rects = new[] { building.OccupiedRect(), reference.OccupiedRect() };
        var height = rects.Max(r => r.maxZ) - rects.Min(r => r.minZ) + 1;
        foreach (var (zoom, size) in new[] { ("close", 11f), ("ordinary", 16f), ("far", 24f) })
        {
            var padding = (int)Math.Round(Screen.height * (1f - height / (2f * size)) / 2f);
            yield return new CameraActionStep("frame " + key + "/" + zoom, ids, padding);
            yield return new ScreenshotStep("gallery/" + key + "/" + zoom, ids, 70);
            yield return new CheckpointStep("identity/" + key + "/" + zoom, _ =>
            {
                EndToEndAssert.True(Math.Abs(Find.CameraDriver.RootSize - size) <= .5f,
                    "Each image must use the stated distinct native camera zoom.");
                return new Dictionary<string, string>
                {
                    ["rootSize"] = Find.CameraDriver.RootSize.ToString(CultureInfo.InvariantCulture),
                    ["rotation"] = building.Rotation.AsInt.ToString(),
                    ["graphic"] = building.Graphic.path,
                    ["reference"] = reference.Graphic.path,
                    ["scope"] = "native placement and packaged art; industrial load is a supporting pose fixture"
                };
            });
        }
    }
}
