using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.base-building-visual-catalog",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "fumblesneeze.immersivechefs",
    MaxFrames = 3_600,
    MaxGameTicks = 6_000,
    MaxWallClockSeconds = 150)]
public sealed class BaseBuildingVisualCatalogTest : IRimWorldEndToEndTest
{
    private static readonly string[] ExpectedBuildingDefNames =
    {
        "ImmersiveChefs_Dishwasher",
        "ImmersiveChefs_IndustrialDishwasher",
        "ImmersiveChefs_PrepStation",
        "ImmersiveChefs_SauceStation",
        "ImmersiveChefs_MeatStation",
        "ImmersiveChefs_VegetableStation",
        "ImmersiveChefs_PastryStation",
        "ImmersiveChefs_Microwave"
    };

    private readonly List<BuildingFixture> customBuildings = new();
    private readonly List<Building> supports = new();
    private readonly List<Building> vanillaReferences = new();
    private readonly Dictionary<Rot4, List<string>> rotationRows = new();
    private readonly Dictionary<string, string> inspectorIds = new(StringComparer.Ordinal);
    private Building tableSupport = null!;
    private Building workbenchSupport = null!;
    private Building tableMicrowave = null!;
    private Building workbenchMicrowave = null!;

    public void Arrange(IEndToEndContext context)
    {
        var map = Current.Game.CurrentMap;
        var center = FindCatalogCenter(map);
        var originalGodMode = DebugSettings.godMode;
        context.DeferCleanup(() => DebugSettings.godMode = originalGodMode);
        DebugSettings.godMode = true;
        var columns = new[]
        {
            ("ImmersiveChefs_Dishwasher", -28),
            ("ImmersiveChefs_IndustrialDishwasher", -20),
            ("ImmersiveChefs_PrepStation", -11),
            ("ImmersiveChefs_SauceStation", -2),
            ("ImmersiveChefs_MeatStation", 5),
            ("ImmersiveChefs_VegetableStation", 12),
            ("ImmersiveChefs_PastryStation", 19)
        };
        var rows = new[]
        {
            (Rot4.North, 12),
            (Rot4.East, 4),
            (Rot4.South, -4),
            (Rot4.West, -12)
        };

        foreach (var (rotation, z) in rows)
        {
            rotationRows.Add(rotation, new List<string>());
            foreach (var (defName, x) in columns)
            {
                var fixture = AddBuilding(map, center + new IntVec3(x, 0, z), defName, rotation);
                rotationRows[rotation].Add(fixture.Building.ThingID);
                if (rotation == Rot4.North)
                {
                    inspectorIds.Add(defName, fixture.Building.ThingID);
                }
            }

            var supportDefName = rotation == Rot4.North || rotation == Rot4.South
                ? "Table1x2c"
                : "TableMachining";
            var support = SpawnBuilding(
                map,
                center + new IntVec3(28, 0, z),
                supportDefName,
                rotation,
                supportDefName == "Table1x2c" ? ThingDefOf.Steel : null);
            supports.Add(support);
            var microwave = AddBuilding(
                map,
                support.Position,
                "ImmersiveChefs_Microwave",
                rotation);
            rotationRows[rotation].Add(microwave.Building.ThingID);
            if (rotation == Rot4.North)
            {
                tableSupport = support;
                tableMicrowave = microwave.Building;
                inspectorIds.Add("ImmersiveChefs_Microwave", microwave.Building.ThingID);
            }
            else if (rotation == Rot4.East)
            {
                workbenchSupport = support;
                workbenchMicrowave = microwave.Building;
            }
        }

        foreach (var (defName, x, stuff) in new[]
                 {
                     ("TableButcher", -24, ThingDefOf.Steel),
                     ("ElectricStove", -14, (ThingDef?)null),
                     ("FueledStove", -6, (ThingDef?)null),
                     ("Table1x2c", 4, ThingDefOf.WoodLog),
                     ("TableMachining", 14, (ThingDef?)null)
                 })
        {
            vanillaReferences.Add(SpawnBuilding(
                map,
                center + new IntVec3(x, 0, -22),
                defName,
                Rot4.North,
                stuff));
        }

        var buildables = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(Array.Empty<string>(), new[] { "Production" })
            .Where(option => option.Interaction == EndToEndGizmoInteraction.Place)
            .Select(option => option.BuildableDefName)
            .Where(defName => defName is not null)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var expected in ExpectedBuildingDefNames)
        {
            EndToEndAssert.True(
                buildables.Contains(expected),
                "The native Production architect catalog must expose " + expected + ".");
        }
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var customIds = customBuildings.Select(fixture => fixture.Building.ThingID).ToArray();
        var allIds = customIds
            .Concat(supports.Select(building => building.ThingID))
            .Concat(vanillaReferences.Select(building => building.ThingID))
            .ToArray();

        yield return new AssertionStep("validate the complete building direction and support catalog", _ =>
        {
            EndToEndAssert.Equal(32, customBuildings.Count,
                "The base catalog must contain all eight building Defs in all four rotations.");
            EndToEndAssert.Equal(
                string.Join("|", ExpectedBuildingDefNames.OrderBy(value => value, StringComparer.Ordinal)),
                string.Join("|", customBuildings.Select(fixture => fixture.Building.def.defName)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)),
                "The base catalog must render every concrete Immersive Chefs building Def.");
            foreach (var fixture in customBuildings)
            {
                fixture.AssertRendered();
            }

            EndToEndAssert.True(
                ReferenceEquals(tableSupport, MicrowaveSupportRuntime.FindAt(tableSupport.Position, tableSupport.Map, tableMicrowave)),
                "The north microwave must render on its exact real dining-table support.");
            EndToEndAssert.True(
                ReferenceEquals(workbenchSupport, MicrowaveSupportRuntime.FindAt(workbenchSupport.Position, workbenchSupport.Map, workbenchMicrowave)),
                "The east microwave must render on its exact real machining-table support.");
        });

        yield return new SelectionActionStep("select the complete building catalog", customIds, additive: false);
        yield return new CameraActionStep("frame the complete building catalog", allIds, paddingPixels: 70);
        yield return new ScreenshotStep("complete building catalog beside vanilla production and dining fixtures", allIds, 70);

        foreach (var rotation in new[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West })
        {
            var ids = rotationRows[rotation].ToArray();
            var direction = RotationName(rotation);
            yield return new SelectionActionStep("select the " + direction + " building row", ids, additive: false);
            yield return new CameraActionStep("frame the " + direction + " building row", ids, paddingPixels: 130);
            yield return new ScreenshotStep(direction + " rotation footprints and silhouettes", ids, 130);
        }

        yield return new SelectionActionStep(
            "select the dining-table microwave support",
            new[] { tableSupport.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "focus the dining-table microwave support",
            new[] { tableSupport.ThingID, tableMicrowave.ThingID },
            paddingPixels: 240);
        yield return new ScreenshotStep(
            "fallback microwave on a real dining table",
            Array.Empty<string>(),
            0);

        yield return new SelectionActionStep(
            "select the machining-table microwave support",
            new[] { workbenchSupport.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "focus the machining-table microwave support",
            new[] { workbenchSupport.ThingID, workbenchMicrowave.ThingID },
            paddingPixels: 240);
        yield return new ScreenshotStep(
            "fallback microwave on a real production workbench",
            Array.Empty<string>(),
            0);

        yield return new SelectionActionStep("select the building catalog behind the build menu", customIds, false);
        yield return new CameraActionStep("frame the building catalog behind the build menu", allIds, 70);
        yield return new ArchitectCategoryActionStep("open the native Production build menu", "Production", open: true);
        yield return new ScreenshotStep("native Production build menu labels and icons", Array.Empty<string>(), 0);
        yield return new ArchitectCategoryActionStep("close the native Production build menu", "Production", open: false);

        foreach (var defName in ExpectedBuildingDefNames)
        {
            var id = inspectorIds[defName];
            yield return new SelectionActionStep(
                "select " + defName + " for its native inspector",
                new[] { id },
                additive: false);
            yield return new CameraActionStep(
                "focus " + defName + " at final building scale",
                new[] { id },
                paddingPixels: 260);
            yield return new ScreenshotStep("inspect " + defName + " readability", Array.Empty<string>(), 0);
        }

        yield return new CheckpointStep(
            "base building visual catalog",
            _ => customBuildings
                .GroupBy(fixture => fixture.Building.def.defName)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => string.Join(",", group.OrderBy(fixture => fixture.Rotation.AsInt)
                        .Select(fixture => RotationName(fixture.Rotation) + ":" + fixture.Building.Graphic.path)),
                    StringComparer.Ordinal));
    }

    private BuildingFixture AddBuilding(
        Map map,
        IntVec3 cell,
        string defName,
        Rot4 rotation)
    {
        var building = SpawnBuilding(map, cell, defName, rotation, stuff: null);
        var fixture = new BuildingFixture(building, rotation);
        customBuildings.Add(fixture);
        return fixture;
    }

    private static Building SpawnBuilding(
        Map map,
        IntVec3 cell,
        string defName,
        Rot4 rotation,
        ThingDef? stuff)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var building = (Building)ThingMaker.MakeThing(
            def,
            def.MadeFromStuff ? stuff ?? ThingDefOf.Steel : null);
        building.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(building, cell, map, rotation);
        return building;
    }

    private static IntVec3 FindCatalogCenter(Map map)
    {
        const int halfWidth = 36;
        const int halfHeight = 27;
        foreach (var offset in new[]
                 {
                     IntVec3.Zero,
                     new IntVec3(-42, 0, 0),
                     new IntVec3(42, 0, 0),
                     new IntVec3(0, 0, -34),
                     new IntVec3(0, 0, 34),
                     new IntVec3(-42, 0, -34),
                     new IntVec3(42, 0, 34)
                 })
        {
            var center = map.Center + offset;
            var rect = CellRect.FromLimits(
                center.x - halfWidth,
                center.z - halfHeight,
                center.x + halfWidth,
                center.z + halfHeight);
            if (rect.Cells.All(cell =>
                    cell.InBounds(map) &&
                    cell.Standable(map) &&
                    !cell.Fogged(map)))
            {
                return center;
            }
        }

        throw new EndToEndAssertionException("No bounded clear rectangle can host the base building catalog.");
    }

    private static string RotationName(Rot4 rotation)
    {
        if (rotation == Rot4.North)
        {
            return "north";
        }

        if (rotation == Rot4.East)
        {
            return "east";
        }

        if (rotation == Rot4.South)
        {
            return "south";
        }

        if (rotation == Rot4.West)
        {
            return "west";
        }

        throw new ArgumentOutOfRangeException(nameof(rotation), rotation, "Only cardinal rotations are supported.");
    }

    private sealed class BuildingFixture
    {
        internal BuildingFixture(Building building, Rot4 rotation)
        {
            Building = building;
            Rotation = rotation;
        }

        internal Building Building { get; }

        internal Rot4 Rotation { get; }

        internal void AssertRendered()
        {
            EndToEndAssert.True(Building.Spawned, Building.def.defName + " must remain spawned.");
            EndToEndAssert.Equal(Rotation.AsInt, Building.Rotation.AsInt,
                Building.def.defName + " must retain its requested catalog rotation.");
            EndToEndAssert.True(
                Building.Graphic.path.StartsWith("ImmersiveChefs/", StringComparison.Ordinal),
                Building.def.defName + " must resolve its packaged Immersive Chefs building art.");
            EndToEndAssert.True(
                Building.Graphic.MatAt(Building.Rotation, Building).mainTexture is not null,
                Building.def.defName + " must resolve a concrete live texture.");

            var horizontal = Rotation == Rot4.East || Rotation == Rot4.West;
            var expectedWidth = horizontal ? Building.def.size.z : Building.def.size.x;
            var expectedHeight = horizontal ? Building.def.size.x : Building.def.size.z;
            var occupied = Building.OccupiedRect();
            EndToEndAssert.Equal(expectedWidth, occupied.Width,
                Building.def.defName + " must occupy its rotated Def width.");
            EndToEndAssert.Equal(expectedHeight, occupied.Height,
                Building.def.defName + " must occupy its rotated Def height.");
        }
    }
}
