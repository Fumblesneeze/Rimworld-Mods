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
    MaxWallClockSeconds = 240)]
public sealed class TextureVariationVisualCatalogTest : IRimWorldEndToEndTest
{
    private const string SaveName = "ImmersiveChefsE2E_TextureVariations";
    private readonly List<BuildingFixture> buildings = new();
    private readonly List<Building> supports = new();
    private readonly List<PortableFixture> portableWare = new();
    private readonly Dictionary<Rot4, List<string>> rotationRows = new();
    private readonly Dictionary<string, VariationFamily> variationFamilies = new(StringComparer.Ordinal);

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
        var columns = new[]
        {
            ("ImmersiveChefs_Dishwasher", -28),
            ("ImmersiveChefs_IndustrialDishwasher", -20),
            ("ImmersiveChefs_MeatStation", -12),
            ("ImmersiveChefs_PastryStation", -4),
            ("ImmersiveChefs_PrepStation", 4),
            ("ImmersiveChefs_SauceStation", 12),
            ("ImmersiveChefs_VegetableStation", 20),
            ("ImmersiveChefs_Microwave", 28)
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
                var cell = center + new IntVec3(x, 0, z);
                if (defName == "ImmersiveChefs_Microwave")
                {
                    var support = (Building)ThingMaker.MakeThing(
                        DefDatabase<ThingDef>.GetNamed("Table1x2c"),
                        ThingDefOf.Steel);
                    support.SetFactionDirect(Faction.OfPlayer);
                    GenSpawn.Spawn(support, cell, map, rotation);
                    supports.Add(support);
                    cell = support.Position;
                }

                AddBuilding(
                    context,
                    map,
                    catalog,
                    DefDatabase<ThingDef>.GetNamed(defName),
                    cell,
                    rotation);
            }
        }

        AddPortableRows(map, center + new IntVec3(-13, 0, -22));
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var catalog = context.GetRequiredService<IEndToEndGizmoCatalog>();
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
            if (string.Equals(
                    fixture.InitialGraphicPath,
                    fixture.ExpectedGraphicPath,
                    StringComparison.Ordinal))
            {
                fixture.PrepareBaseGraphic();
                var baseAction = FindChangeGraphic(catalog, fixture.Building);
                yield return ChangeGraphicStep(fixture, baseAction, "base prerequisite");
                yield return WaitForGraphic(fixture, fixture.BaseGraphicPath, "base prerequisite");
                fixture.RestoreVariationFamily();
            }

            fixture.PrepareVariantGraphic();
            var variantAction = FindChangeGraphic(catalog, fixture.Building);
            yield return ChangeGraphicStep(fixture, variantAction, "alternate family");
            yield return WaitForGraphic(fixture, fixture.ExpectedGraphicPath, "alternate family");
            fixture.RestoreVariationFamily();
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

        yield return new AssertionStep(
            "restore every declared two-member VEF family before saving",
            _ =>
            {
                foreach (var fixture in buildings)
                {
                    fixture.AssertVariationFamilyRestored();
                }
            });
        foreach (var rotation in CardinalRotations())
        {
            var row = rotationRows[rotation];
            for (var groupIndex = 0; groupIndex < 2; groupIndex++)
            {
                var ids = row.Skip(groupIndex * 4).Take(4).ToArray();
                var groupName = groupIndex == 0 ? "appliances and primary stations" : "specialist stations";
                yield return new SelectionActionStep(
                    "select " + RotationName(rotation) + " alternate " + groupName,
                    ids,
                    additive: false);
                yield return new CameraActionStep(
                    "frame " + RotationName(rotation) + " alternate " + groupName,
                    ids,
                    paddingPixels: 120);
                yield return new ScreenshotStep(
                    RotationName(rotation) + " alternate " + groupName,
                    ids,
                    paddingPixels: 120);
            }
        }

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
        foreach (var rotation in CardinalRotations())
        {
            var ids = rotationRows[rotation].ToArray();
            yield return new SelectionActionStep(
                "select the same " + RotationName(rotation) + " optional buildings after loading",
                ids,
                additive: false);
            yield return new CameraActionStep(
                "frame the same " + RotationName(rotation) + " optional buildings after loading",
                ids,
                paddingPixels: 110);
            yield return new ScreenshotStep(
                RotationName(rotation) + " building graphics preserved by native VEF save loading",
                ids,
                paddingPixels: 110);
        }
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
                fixture => fixture.Building.def.defName + ":" + RotationName(fixture.Rotation),
                fixture => fixture.InitialGraphicPath + " -> " + fixture.Building.Graphic.path));
    }

    private void AddBuilding(
        IEndToEndContext context,
        Map map,
        IEndToEndGizmoCatalog catalog,
        ThingDef def,
        IntVec3 cell,
        Rot4 rotation)
    {
        var building = (Building)ThingMaker.MakeThing(def, def.MadeFromStuff ? ThingDefOf.Steel : null);
        building.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(building, cell, map, rotation);
        _ = FindChangeGraphic(catalog, building);
        var initialGraphicPath = building.Graphic.path;
        if (!variationFamilies.TryGetValue(def.defName, out var variationFamily))
        {
            variationFamily = VariationFamily.Capture(building);
            variationFamilies.Add(def.defName, variationFamily);
            context.DeferCleanup(variationFamily.Restore);
        }

        variationFamily.AssertMember(initialGraphicPath);
        buildings.Add(new BuildingFixture(
            building,
            initialGraphicPath,
            rotation,
            variationFamily));
        rotationRows[rotation].Add(building.ThingID);
    }

    private static EndToEndGizmoOption FindChangeGraphic(
        IEndToEndGizmoCatalog catalog,
        Building building)
    {
        var options = catalog
            .Query(new[] { building.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Invoke &&
                option.Label.IndexOf("graphic", StringComparison.OrdinalIgnoreCase) >= 0 &&
                option.Label.IndexOf("choose", StringComparison.OrdinalIgnoreCase) < 0)
            .ToArray();
        EndToEndAssert.Equal(
            1,
            options.Length,
            building.def.defName + " must expose exactly one enabled native VEF random-graphic gizmo.");
        return options[0];
    }

    private static GizmoActionStep ChangeGraphicStep(
        BuildingFixture fixture,
        EndToEndGizmoOption option,
        string target) =>
        new(
            "change " + fixture.Building.def.label + " " + RotationName(fixture.Rotation) +
            " graphic to " + target + " through native VEF gizmo",
            new[] { fixture.Building.ThingID },
            option.RuntimeType,
            EndToEndGizmoInteraction.Invoke,
            stableGizmoId: option.StableId,
            architectCategoryDefNames: Array.Empty<string>());

    private static WaitUntilStep WaitForGraphic(
        BuildingFixture fixture,
        string path,
        string target) =>
        new(
            fixture.Building.def.label + " " + RotationName(fixture.Rotation) +
            " visibly changes to " + target,
            _ => string.Equals(path, fixture.Building.Graphic.path, StringComparison.Ordinal),
            new EndToEndDeadline(120, 120, TimeSpan.FromSeconds(10)));

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
        const int halfWidth = 32;
        const int halfHeight = 24;
        const int candidateStep = 24;
        const int candidateRadius = 48;
        var candidates = new List<IntVec3>();
        for (var z = -candidateRadius; z <= candidateRadius; z += candidateStep)
        {
            for (var x = -candidateRadius; x <= candidateRadius; x += candidateStep)
            {
                candidates.Add(map.Center + new IntVec3(x, 0, z));
            }
        }

        foreach (var cell in candidates.OrderBy(candidate => candidate.DistanceToSquared(map.Center)))
        {
            if (cell.x < halfWidth + 10 ||
                cell.z < halfHeight + 10 ||
                cell.x >= map.Size.x - halfWidth - 10 ||
                cell.z >= map.Size.z - halfHeight - 10)
            {
                continue;
            }

            if (CellRect.FromLimits(
                    cell.x - halfWidth,
                    cell.z - halfHeight,
                    cell.x + halfWidth,
                    cell.z + halfHeight)
                .Cells.All(candidate =>
                    candidate.InBounds(map) &&
                    candidate.Standable(map) &&
                    !candidate.Fogged(map)))
            {
                return cell;
            }
        }

        throw new EndToEndAssertionException(
            "No open catalog rectangle exists within the fixed 25-candidate setup lattice.");
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

    private static IEnumerable<Rot4> CardinalRotations()
    {
        yield return Rot4.North;
        yield return Rot4.East;
        yield return Rot4.South;
        yield return Rot4.West;
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
        internal BuildingFixture(
            Building building,
            string initialGraphicPath,
            Rot4 rotation,
            VariationFamily variationFamily)
        {
            Building = building;
            ThingId = building.ThingID;
            InitialGraphicPath = initialGraphicPath;
            Rotation = rotation;
            VariationFamily = variationFamily;
        }

        internal Building Building { get; private set; }

        internal string ThingId { get; }

        internal string InitialGraphicPath { get; }

        internal Rot4 Rotation { get; }

        internal string BaseGraphicPath => VariationFamily.BaseGraphicPath;

        internal string ExpectedGraphicPath => VariationFamily.VariantGraphicPath;

        private VariationFamily VariationFamily { get; }

        internal void PrepareBaseGraphic() => VariationFamily.ConstrainTo(BaseGraphicPath);

        internal void PrepareVariantGraphic() => VariationFamily.ConstrainTo(ExpectedGraphicPath);

        internal void RestoreVariationFamily() => VariationFamily.Restore();

        internal void AssertVariationFamilyRestored() => VariationFamily.AssertRestored();

        internal void Rebind(Building building) => Building = building;

        internal void AssertLoadedState()
        {
            EndToEndAssert.Equal(
                ExpectedGraphicPath,
                Building.Graphic.path,
                Building.def.defName + " must retain the player-selected VEF graphic after loading.");
            EndToEndAssert.Equal(
                Rotation.AsInt,
                Building.Rotation.AsInt,
                Building.def.defName + " must retain its cardinal direction after loading.");
            VariationFamily.AssertRestored();
        }
    }

    private sealed class VariationFamily
    {
        private readonly List<string> graphics;
        private readonly List<string> names;
        private readonly string[] originalGraphics;
        private readonly string[] originalNames;
        private VariationFamily(
            List<string> graphics,
            List<string> names,
            string[] originalGraphics,
            string[] originalNames)
        {
            this.graphics = graphics;
            this.names = names;
            this.originalGraphics = originalGraphics;
            this.originalNames = originalNames;
            var variants = originalGraphics
                .Where(path => path.EndsWith("_Variant01", StringComparison.Ordinal))
                .ToArray();
            EndToEndAssert.Equal(1, variants.Length, "Each VEF family must contain one alternate path.");
            VariantGraphicPath = variants[0];
            BaseGraphicPath = originalGraphics.Single(path =>
                !string.Equals(path, VariantGraphicPath, StringComparison.Ordinal));
        }

        internal string BaseGraphicPath { get; }

        internal string VariantGraphicPath { get; }

        internal static VariationFamily Capture(Building building)
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
            return new VariationFamily(graphics, names, graphics.ToArray(), names.ToArray());
        }

        internal void AssertMember(string graphicPath) =>
            EndToEndAssert.True(
                originalGraphics.Contains(graphicPath, StringComparer.Ordinal),
                "The rendered graphic must begin with one of its configured family members.");

        internal void ConstrainTo(string graphicPath)
        {
            var index = Array.IndexOf(originalGraphics, graphicPath);
            EndToEndAssert.True(index >= 0, "The requested VEF family member must be declared.");
            graphics.Clear();
            graphics.Add(originalGraphics[index]);
            names.Clear();
            names.Add(originalNames[index]);
        }

        internal void Restore()
        {
            graphics.Clear();
            graphics.AddRange(originalGraphics);
            names.Clear();
            names.AddRange(originalNames);
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
