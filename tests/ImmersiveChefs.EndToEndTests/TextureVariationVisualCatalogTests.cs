using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.texture-variation-visual-catalog",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "OskarPotocki.VanillaFactionsExpanded.Core",
    "VanillaExpanded.VTEXVariations",
    "fumblesneeze.immersivechefs",
    MaxFrames = 1_800,
    MaxGameTicks = 2_000,
    MaxWallClockSeconds = 90)]
public sealed class TextureVariationVisualCatalogTest : IRimWorldEndToEndTest
{
    private readonly List<BuildingFixture> buildings = new();
    private readonly List<Thing> portableWare = new();

    public void Arrange(IEndToEndContext context)
    {
        var map = Current.Game.CurrentMap;
        var center = FindCatalogCenter(map);
        var catalog = context.GetRequiredService<IEndToEndGizmoCatalog>();
        var buildingDefs = new[]
        {
            ("ImmersiveChefs_Dishwasher", new IntVec3(-13, 0, 5)),
            ("ImmersiveChefs_IndustrialDishwasher", new IntVec3(-7, 0, 5)),
            ("ImmersiveChefs_PrepStation", new IntVec3(1, 0, 5)),
            ("ImmersiveChefs_SauceStation", new IntVec3(10, 0, 5)),
            ("ImmersiveChefs_MeatStation", new IntVec3(-13, 0, 1)),
            ("ImmersiveChefs_VegetableStation", new IntVec3(-6, 0, 1)),
            ("ImmersiveChefs_PastryStation", new IntVec3(1, 0, 1))
        };

        foreach (var (defName, offset) in buildingDefs)
        {
            AddBuilding(context, map, catalog, DefDatabase<ThingDef>.GetNamed(defName), center + offset);
        }

        var support = (Building)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Table1x2c"),
            ThingDefOf.Steel);
        support.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(support, center + new IntVec3(9, 0, 1), map, Rot4.North);
        AddBuilding(
            context,
            map,
            catalog,
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Microwave"),
            support.Position);

        AddPortableRows(map, center + new IntVec3(-13, 0, -5));
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var buildingIds = buildings.Select(fixture => fixture.Building.ThingID).ToArray();
        yield return new SelectionActionStep(
            "select optional building graphics catalog",
            buildingIds,
            additive: false);
        yield return new CameraActionStep(
            "frame optional building graphics catalog",
            buildingIds,
            paddingPixels: 110);
        yield return new ScreenshotStep(
            "building graphics before native VEF changes",
            buildingIds,
            paddingPixels: 110);

        foreach (var fixture in buildings)
        {
            yield return new GizmoActionStep(
                "change " + fixture.Building.def.label + " graphic through native VEF gizmo",
                new[] { fixture.Building.ThingID },
                fixture.ChangeGraphic.RuntimeType,
                EndToEndGizmoInteraction.Invoke,
                stableGizmoId: fixture.ChangeGraphic.StableId,
                architectCategoryDefNames: Array.Empty<string>());
            yield return new WaitUntilStep(
                fixture.Building.def.label + " visibly changes graphic",
                _ => string.Equals(
                    fixture.ExpectedGraphicPath,
                    fixture.Building.Graphic.path,
                    StringComparison.Ordinal),
                new EndToEndDeadline(120, 120, TimeSpan.FromSeconds(10)));
        }

        yield return new AssertionStep(
            "every native VEF action changed the rendered building path",
            _ =>
            {
                foreach (var fixture in buildings)
                {
                    EndToEndAssert.Equal(
                        fixture.ExpectedGraphicPath,
                        fixture.Building.Graphic.path,
                        fixture.Building.def.defName + " must change to the opposite family member after its native VEF gizmo.");
                }
            });
        yield return new ScreenshotStep(
            "building graphics after native VEF changes",
            buildingIds,
            paddingPixels: 110);

        var portableIds = portableWare.Select(thing => thing.ThingID).ToArray();
        yield return new SelectionActionStep(
            "select portable material and sanitation catalog",
            portableIds,
            additive: false);
        yield return new CameraActionStep(
            "frame portable material and sanitation catalog",
            portableIds,
            paddingPixels: 140);
        yield return new ScreenshotStep(
            "portable clean dirty wood and stone graphics",
            portableIds,
            paddingPixels: 140);
        yield return new CheckpointStep(
            "texture variation visual catalog",
            _ => buildings.ToDictionary(
                fixture => fixture.Building.def.defName,
                fixture => fixture.InitialGraphicPath + " -> " + fixture.Building.Graphic.path));
    }

    private void AddBuilding(
        IEndToEndContext context,
        Map map,
        IEndToEndGizmoCatalog catalog,
        ThingDef def,
        IntVec3 cell)
    {
        var building = (Building)ThingMaker.MakeThing(def, def.MadeFromStuff ? ThingDefOf.Steel : null);
        building.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(building, cell, map, Rot4.North);
        var changeGraphic = catalog
            .Query(new[] { building.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Invoke &&
                option.Label.IndexOf("graphic", StringComparison.OrdinalIgnoreCase) >= 0 &&
                option.Label.IndexOf("choose", StringComparison.OrdinalIgnoreCase) < 0)
            .ToArray();
        EndToEndAssert.Equal(
            1,
            changeGraphic.Length,
            def.defName + " must expose exactly one enabled native VEF random-graphic gizmo.");
        var initialGraphicPath = building.Graphic.path;
        var expectedGraphicPath = ConstrainNativeRandomToOpposite(context, building);
        buildings.Add(new BuildingFixture(
            building,
            initialGraphicPath,
            expectedGraphicPath,
            changeGraphic[0]));
    }

    private void AddPortableRows(Map map, IntVec3 origin)
    {
        var steel = ThingDefOf.Steel;
        var wood = ThingDefOf.WoodLog;
        var stone = DefDatabase<ThingDef>.GetNamed("BlocksGranite");
        AddPair(map, origin + new IntVec3(0, 0, 0), "ImmersiveChefs_Cookware", steel);
        AddPair(map, origin + new IntVec3(4, 0, 0), "ImmersiveChefs_Cookware", stone);
        AddPair(map, origin + new IntVec3(8, 0, 0), "ImmersiveChefs_Plate", steel);
        AddPair(map, origin + new IntVec3(12, 0, 0), "ImmersiveChefs_Plate", wood);
        AddPair(map, origin + new IntVec3(16, 0, 0), "ImmersiveChefs_Plate", stone);
        AddPair(map, origin + new IntVec3(20, 0, 0), "ImmersiveChefs_Cutlery", steel);
        AddPair(map, origin + new IntVec3(24, 0, 0), "ImmersiveChefs_Cutlery", wood);

        var knife = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_ChefsKnife"),
            steel);
        GenSpawn.Spawn(knife, origin + new IntVec3(28, 0, 0), map);
        portableWare.Add(knife);
    }

    private void AddPair(Map map, IntVec3 cell, string defName, ThingDef stuff)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var clean = ThingMaker.MakeThing(def, stuff);
        var dirty = ThingMaker.MakeThing(def, stuff);
        dirty.TryGetComp<CompSanitation>()?.MarkDirty();
        GenSpawn.Spawn(clean, cell, map);
        GenSpawn.Spawn(dirty, cell + (IntVec3.East * 2), map);
        portableWare.Add(clean);
        portableWare.Add(dirty);
    }

    private static IntVec3 FindCatalogCenter(Map map)
    {
        const int halfWidth = 18;
        const int halfHeight = 8;
        return map.AllCells
            .Where(cell =>
                cell.x >= halfWidth + 10 &&
                cell.z >= halfHeight + 10 &&
                cell.x < map.Size.x - halfWidth - 10 &&
                cell.z < map.Size.z - halfHeight - 10)
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First(cell => CellRect.FromLimits(
                    cell.x - halfWidth,
                    cell.z - halfHeight,
                    cell.x + halfWidth,
                    cell.z + halfHeight)
                .Cells.All(candidate =>
                    candidate.InBounds(map) &&
                    candidate.Standable(map) &&
                    !candidate.Fogged(map)));
    }

    private static string ConstrainNativeRandomToOpposite(IEndToEndContext context, Building building)
    {
        var def = building.def;
        var properties = def.comps.Single(value =>
            value.GetType().FullName == "VEF.Buildings.CompProperties_RandomBuildingGraphic");
        var propertiesType = properties.GetType();
        var graphics = (List<string>)(propertiesType.GetField(
            "randomGraphics",
            BindingFlags.Instance | BindingFlags.Public)?.GetValue(properties) ??
            throw new EndToEndAssertionException(def.defName + " has no VEF randomGraphics list."));
        var names = (List<string>)(propertiesType.GetField(
            "optionalNames",
            BindingFlags.Instance | BindingFlags.Public)?.GetValue(properties) ??
            throw new EndToEndAssertionException(def.defName + " has no VEF optionalNames list."));
        EndToEndAssert.Equal(2, graphics.Count, def.defName + " must begin with standard and alternate paths.");
        EndToEndAssert.Equal(2, names.Count, def.defName + " must begin with standard and alternate names.");
        var originalGraphics = graphics.ToArray();
        var originalNames = names.ToArray();
        var initialIndex = Array.IndexOf(originalGraphics, building.Graphic.path);
        EndToEndAssert.True(
            initialIndex >= 0,
            def.defName + " must visibly start with one of its configured family members.");
        var oppositeIndex = initialIndex == 0 ? 1 : 0;
        graphics.Clear();
        graphics.Add(originalGraphics[oppositeIndex]);
        names.Clear();
        names.Add(originalNames[oppositeIndex]);
        context.DeferCleanup(() =>
        {
            graphics.Clear();
            graphics.AddRange(originalGraphics);
            names.Clear();
            names.AddRange(originalNames);
        });
        return originalGraphics[oppositeIndex];
    }

    private sealed class BuildingFixture
    {
        internal BuildingFixture(
            Building building,
            string initialGraphicPath,
            string expectedGraphicPath,
            EndToEndGizmoOption changeGraphic)
        {
            Building = building;
            InitialGraphicPath = initialGraphicPath;
            ExpectedGraphicPath = expectedGraphicPath;
            ChangeGraphic = changeGraphic;
        }

        internal Building Building { get; }

        internal string InitialGraphicPath { get; }

        internal string ExpectedGraphicPath { get; }

        internal EndToEndGizmoOption ChangeGraphic { get; }
    }
}
