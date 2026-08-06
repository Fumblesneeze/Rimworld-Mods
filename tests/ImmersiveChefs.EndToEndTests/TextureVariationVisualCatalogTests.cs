using System;
using System.Collections.Generic;
using System.IO;
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
    MaxFrames = 4_800,
    MaxGameTicks = 10_000,
    MaxWallClockSeconds = 180)]
public sealed class TextureVariationVisualCatalogTest : IRimWorldEndToEndTest
{
    private const string SaveName = "ImmersiveChefsE2E_TextureVariations";
    private readonly List<BuildingFixture> buildings = new();
    private readonly List<PortableFixture> portableWare = new();

    public void Arrange(IEndToEndContext context)
    {
        var savePath = GenFilePaths.FilePathForSavedGame(SaveName);
        context.DeferCleanup(() =>
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
        });

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
        var buildingIds = buildings.Select(fixture => fixture.ThingId).ToArray();
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

        foreach (var fixture in buildings)
        {
            fixture.RestoreVariationFamily();
        }
        yield return new AssertionStep(
            "restore every declared two-member VEF family before saving",
            _ =>
            {
                foreach (var fixture in buildings)
                {
                    fixture.AssertVariationFamilyRestored();
                }
            });
        yield return new ScreenshotStep(
            "building graphics after native VEF changes",
            buildingIds,
            paddingPixels: 110);

        var portableIds = portableWare.Select(fixture => fixture.ThingId).ToArray();
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

        yield return new SaveLoadActionStep(
            "save and load the selected building and portable graphics through RimWorld",
            SaveName);
        yield return new WaitUntilStep(
            "the same visual catalog is available after loading",
            _ => TryRebindLoadedCatalog(),
            new EndToEndDeadline(2_400, 10_000, TimeSpan.FromSeconds(120)));
        yield return new AssertionStep(
            "native VEF selections and portable cosmetic state survive loading",
            _ =>
            {
                foreach (var fixture in buildings)
                {
                    fixture.AssertLoadedState();
                }

                foreach (var fixture in portableWare)
                {
                    fixture.AssertLoadedState();
                }
            });
        yield return new SelectionActionStep(
            "select the same optional buildings after loading",
            buildingIds,
            additive: false);
        yield return new CameraActionStep(
            "frame the same optional buildings after loading",
            buildingIds,
            paddingPixels: 110);
        yield return new ScreenshotStep(
            "building graphics preserved by native VEF save loading",
            buildingIds,
            paddingPixels: 110);
        yield return new SelectionActionStep(
            "select the same portable material and sanitation catalog after loading",
            portableIds,
            additive: false);
        yield return new CameraActionStep(
            "frame the same portable catalog after loading",
            portableIds,
            paddingPixels: 140);
        yield return new ScreenshotStep(
            "portable material and sanitation graphics preserved after loading",
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
        var variationFamily = ConstrainNativeRandomToOpposite(building);
        context.DeferCleanup(variationFamily.Restore);
        buildings.Add(new BuildingFixture(
            building,
            initialGraphicPath,
            variationFamily,
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
        portableWare.Add(new PortableFixture(knife));
    }

    private void AddPair(Map map, IntVec3 cell, string defName, ThingDef stuff)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var clean = ThingMaker.MakeThing(def, stuff);
        var dirty = ThingMaker.MakeThing(def, stuff);
        dirty.TryGetComp<CompSanitation>()?.MarkDirty();
        GenSpawn.Spawn(clean, cell, map);
        GenSpawn.Spawn(dirty, cell + (IntVec3.East * 2), map);
        portableWare.Add(new PortableFixture(clean));
        portableWare.Add(new PortableFixture(dirty));
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

    private bool TryRebindLoadedCatalog()
    {
        var allThings = Current.Game?.CurrentMap?.listerThings?.AllThings;
        if (allThings is null)
        {
            return false;
        }

        var byId = allThings.ToDictionary(thing => thing.ThingID, StringComparer.Ordinal);
        if (buildings.Any(fixture =>
                !byId.TryGetValue(fixture.ThingId, out var thing) || thing is not Building) ||
            portableWare.Any(fixture => !byId.ContainsKey(fixture.ThingId)))
        {
            return false;
        }

        foreach (var fixture in buildings)
        {
            fixture.Rebind((Building)byId[fixture.ThingId]);
        }

        foreach (var fixture in portableWare)
        {
            fixture.Rebind(byId[fixture.ThingId]);
        }

        return true;
    }

    private static VariationFamilyLease ConstrainNativeRandomToOpposite(Building building)
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
        return new VariationFamilyLease(
            graphics,
            names,
            originalGraphics,
            originalNames,
            originalGraphics[oppositeIndex]);
    }

    private sealed class BuildingFixture
    {
        internal BuildingFixture(
            Building building,
            string initialGraphicPath,
            VariationFamilyLease variationFamily,
            EndToEndGizmoOption changeGraphic)
        {
            Building = building;
            ThingId = building.ThingID;
            InitialGraphicPath = initialGraphicPath;
            VariationFamily = variationFamily;
            ChangeGraphic = changeGraphic;
        }

        internal Building Building { get; private set; }

        internal string ThingId { get; }

        internal string InitialGraphicPath { get; }

        internal string ExpectedGraphicPath => VariationFamily.ExpectedGraphicPath;

        internal EndToEndGizmoOption ChangeGraphic { get; }

        private VariationFamilyLease VariationFamily { get; }

        internal void RestoreVariationFamily() => VariationFamily.Restore();

        internal void AssertVariationFamilyRestored() => VariationFamily.AssertRestored();

        internal void Rebind(Building building) => Building = building;

        internal void AssertLoadedState()
        {
            EndToEndAssert.Equal(
                ExpectedGraphicPath,
                Building.Graphic.path,
                Building.def.defName + " must retain the player-selected VEF graphic after loading.");
            VariationFamily.AssertRestored();
        }
    }

    private sealed class VariationFamilyLease
    {
        private readonly List<string> graphics;
        private readonly List<string> names;
        private readonly string[] originalGraphics;
        private readonly string[] originalNames;
        private bool restored;

        internal VariationFamilyLease(
            List<string> graphics,
            List<string> names,
            string[] originalGraphics,
            string[] originalNames,
            string expectedGraphicPath)
        {
            this.graphics = graphics;
            this.names = names;
            this.originalGraphics = originalGraphics;
            this.originalNames = originalNames;
            ExpectedGraphicPath = expectedGraphicPath;
        }

        internal string ExpectedGraphicPath { get; }

        internal void Restore()
        {
            if (restored)
            {
                return;
            }

            graphics.Clear();
            graphics.AddRange(originalGraphics);
            names.Clear();
            names.AddRange(originalNames);
            restored = true;
        }

        internal void AssertRestored()
        {
            EndToEndAssert.Equal(
                string.Join("|", originalGraphics),
                string.Join("|", graphics),
                "The exact two-member VEF graphics family must be restored before persistence.");
            EndToEndAssert.Equal(
                string.Join("|", originalNames),
                string.Join("|", names),
                "The exact two-member VEF player-name family must be restored before persistence.");
        }
    }

    private sealed class PortableFixture
    {
        private readonly string expectedTextureName;
        private readonly string expectedStuffDefName;
        private readonly bool expectedDirty;

        internal PortableFixture(Thing thing)
        {
            Thing = thing;
            ThingId = thing.ThingID;
            expectedTextureName = RenderedTextureName(thing);
            expectedStuffDefName = thing.Stuff?.defName ?? string.Empty;
            expectedDirty = thing.TryGetComp<CompSanitation>()?.IsDirty == true;
        }

        internal Thing Thing { get; private set; }

        internal string ThingId { get; }

        internal void Rebind(Thing thing) => Thing = thing;

        internal void AssertLoadedState()
        {
            EndToEndAssert.Equal(
                expectedTextureName,
                RenderedTextureName(Thing),
                Thing.def.defName + " must retain its material/sanitation cosmetic after loading.");
            EndToEndAssert.Equal(
                expectedStuffDefName,
                Thing.Stuff?.defName ?? string.Empty,
                Thing.def.defName + " must retain exact Stuff through the cosmetic save/load.");
            EndToEndAssert.Equal(
                expectedDirty,
                Thing.TryGetComp<CompSanitation>()?.IsDirty == true,
                Thing.def.defName + " must retain sanitation state through the cosmetic save/load.");
        }

        private static string RenderedTextureName(Thing thing)
        {
            return thing.Graphic.MatSingleFor(thing).mainTexture?.name ??
                throw new EndToEndAssertionException(
                    thing.def.defName + " must resolve one concrete rendered texture.");
        }
    }
}
