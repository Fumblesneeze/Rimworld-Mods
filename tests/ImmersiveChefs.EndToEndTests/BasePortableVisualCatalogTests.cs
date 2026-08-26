using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.base-portable-visual-catalog",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "fumblesneeze.immersivechefs",
    MaxFrames = 2_400,
    MaxGameTicks = 5_000,
    MaxWallClockSeconds = 120)]
public sealed class BasePortableVisualCatalogTest : IRimWorldEndToEndTest
{
    private static readonly string[] ExpectedPortableDefNames =
    {
        "ImmersiveChefs_PrimitiveCookware",
        "ImmersiveChefs_Cookware",
        "ImmersiveChefs_Plate",
        "ImmersiveChefs_AdobePlate",
        "ImmersiveChefs_Cutlery",
        "ImmersiveChefs_GlitterworldCookware",
        "ImmersiveChefs_ChefsKnife",
        "ImmersiveChefs_PreparedFood"
    };

    private readonly List<PortableFixture> customItems = new();
    private readonly List<Thing> vanillaReferences = new();
    private readonly Dictionary<ThingDef, Thing> materialReferences = new();
    private readonly Dictionary<string, List<string>> rowIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> inspectorIds = new(StringComparer.Ordinal);

    public void Arrange(IEndToEndContext context)
    {
        var map = Current.Game.CurrentMap;
        var center = FindCatalogCenter(map);
        var granite = DefDatabase<ThingDef>.GetNamed("BlocksGranite");
        var materials = new[]
        {
            ("wood", ThingDefOf.WoodLog),
            ("granite", granite),
            ("steel", ThingDefOf.Steel),
            ("silver", ThingDefOf.Silver),
            ("gold", ThingDefOf.Gold)
        };

        AddStuffPairs(
            map,
            "cookware",
            center + new IntVec3(-16, 0, 8),
            "ImmersiveChefs_PrimitiveCookware",
            materials.Skip(1).Take(1));
        inspectorIds["primitive cookware"] = customItems.First(item =>
            item.Thing.def.defName == "ImmersiveChefs_PrimitiveCookware" &&
            item.Thing.Stuff == granite &&
            item.ExpectedDirty).Thing.ThingID;

        AddStuffPairs(
            map,
            "cookware",
            center + new IntVec3(-16, 0, 8),
            "ImmersiveChefs_Cookware",
            materials.Skip(2));
        inspectorIds["cookware"] = customItems.First(item =>
            item.Thing.def.defName == "ImmersiveChefs_Cookware" &&
            item.Thing.Stuff == ThingDefOf.Steel &&
            item.ExpectedDirty).Thing.ThingID;

        AddStuffPairs(
            map,
            "plates",
            center + new IntVec3(-16, 0, 4),
            "ImmersiveChefs_Plate",
            materials);
        inspectorIds["plate"] = customItems.First(item =>
            item.Thing.def.defName == "ImmersiveChefs_Plate" &&
            item.Thing.Stuff == ThingDefOf.Gold &&
            item.ExpectedDirty).Thing.ThingID;

        AddStuffPairs(
            map,
            "cutlery",
            center + new IntVec3(-16, 0, 0),
            "ImmersiveChefs_Cutlery",
            new[] { materials[0], materials[2], materials[3], materials[4] });
        inspectorIds["cutlery"] = customItems.First(item =>
            item.Thing.def.defName == "ImmersiveChefs_Cutlery" &&
            item.Thing.Stuff == ThingDefOf.WoodLog &&
            item.ExpectedDirty).Thing.ThingID;

        AddFixedPair(
            map,
            "special",
            center + new IntVec3(-16, 0, -4),
            "ImmersiveChefs_AdobePlate");
        inspectorIds["adobe plate"] = customItems.First(item =>
            item.Thing.def.defName == "ImmersiveChefs_AdobePlate" && item.ExpectedDirty).Thing.ThingID;

        var glitter = AddCustom(
            map,
            "special",
            center + new IntVec3(-12, 0, -4),
            "ImmersiveChefs_GlitterworldCookware",
            stuff: null,
            dirty: false,
            stackCount: 1);
        inspectorIds["glitterworld cookware"] = glitter.Thing.ThingID;

        foreach (var (offset, stuff) in new[]
                 {
                     (-8, ThingDefOf.Steel),
                     (-4, ThingDefOf.Silver),
                     (0, ThingDefOf.Gold)
                 })
        {
            var knife = AddCustom(
                map,
                "special",
                center + new IntVec3(offset, 0, -4),
                "ImmersiveChefs_ChefsKnife",
                stuff,
                dirty: false,
                stackCount: 1);
            if (stuff == ThingDefOf.Gold)
            {
                inspectorIds["chef's knife"] = knife.Thing.ThingID;
            }
        }

        var prepared = AddCustom(
            map,
            "special",
            center + new IntVec3(4, 0, -4),
            "ImmersiveChefs_PreparedFood",
            stuff: null,
            dirty: false,
            stackCount: 12);
        inspectorIds["prepared ingredients"] = prepared.Thing.ThingID;

        foreach (var (offset, def, stackCount) in new[]
                 {
                     (-16, ThingDefOf.WoodLog, 20),
                     (-12, granite, 20),
                     (-8, ThingDefOf.Steel, 20),
                     (-4, ThingDefOf.Silver, 20),
                     (0, ThingDefOf.Gold, 20),
                     (8, DefDatabase<ThingDef>.GetNamed("MealSimple"), 4)
                 })
        {
            var reference = AddVanillaReference(map, center + new IntVec3(offset, 0, -8), def, stackCount);
            if (def.IsStuff)
            {
                materialReferences.Add(def, reference);
            }
        }
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var allIds = customItems.Select(item => item.Thing.ThingID)
            .Concat(vanillaReferences.Select(thing => thing.ThingID))
            .ToArray();
        yield return new AssertionStep("validate the complete portable Def and material catalog", _ =>
        {
            EndToEndAssert.Equal(33, customItems.Count,
                "The base visual catalog must contain every expected material and sanitation fixture.");
            EndToEndAssert.Equal(
                string.Join("|", ExpectedPortableDefNames.OrderBy(value => value, StringComparer.Ordinal)),
                string.Join("|", customItems.Select(item => item.Thing.def.defName)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)),
                "The base visual catalog must render every Immersive Chefs portable Def exactly from its real ThingDef.");
            foreach (var item in customItems)
            {
                try
                {
                    item.AssertRendered();
                }
                catch (EndToEndAssertionException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw new EndToEndAssertionException(
                        "Portable rendering threw for " + item.Thing.def.defName + "/" + item.Summary +
                        " (" + exception.GetType().FullName + "): " + exception.Message);
                }
            }
            foreach (var item in customItems.Where(item => item.Thing.Stuff is not null))
            {
                var column = materialReferences[item.Thing.Stuff!].Position.x;
                EndToEndAssert.True(
                    item.Thing.Position.x == column || item.Thing.Position.x == column + 1,
                    item.Thing.def.defName + "/" + item.Thing.Stuff!.defName +
                    " must be visibly aligned above its exact vanilla material reference.");
            }
        });

        yield return new SelectionActionStep("select every portable catalog fixture", allIds, additive: false);
        yield return new CameraActionStep("frame the complete base portable catalog", allIds, paddingPixels: 90);
        yield return new ScreenshotStep("complete portable catalog beside vanilla references", allIds, paddingPixels: 90);

        foreach (var rowName in new[] { "cookware", "plates", "cutlery", "special" })
        {
            var ids = rowIds[rowName].ToArray();
            yield return new SelectionActionStep("select " + rowName + " visual row", ids, additive: false);
            yield return new CameraActionStep("frame " + rowName + " visual row", ids, paddingPixels: 180);
            yield return new ScreenshotStep(rowName + " material sanitation and stack visuals", ids, paddingPixels: 180);
        }

        foreach (var inspectorName in new[]
                 {
                     "primitive cookware",
                     "cookware",
                     "plate",
                     "adobe plate",
                     "cutlery",
                     "glitterworld cookware",
                     "chef's knife",
                     "prepared ingredients"
                 })
        {
            var id = inspectorIds[inspectorName];
            yield return new SelectionActionStep("select " + inspectorName + " for its native inspector", new[] { id }, false);
            yield return new CameraActionStep("focus " + inspectorName + " at final item scale", new[] { id }, paddingPixels: 260);
            yield return new ScreenshotStep("inspect " + inspectorName + " readability", Array.Empty<string>(), 0);
        }

        yield return new CameraActionStep("frame the aligned material-reference columns", allIds, paddingPixels: 90);
        foreach (var (materialName, material) in new[]
                 {
                     ("wood", ThingDefOf.WoodLog),
                     ("granite", DefDatabase<ThingDef>.GetNamed("BlocksGranite")),
                     ("steel", ThingDefOf.Steel),
                     ("silver", ThingDefOf.Silver),
                     ("gold", ThingDefOf.Gold)
                 })
        {
            yield return new SelectionActionStep(
                "select the vanilla " + materialName + " column reference",
                new[] { materialReferences[material].ThingID },
                additive: false);
            yield return new ScreenshotStep(
                "identify the aligned " + materialName + " material column",
                Array.Empty<string>(),
                0);
        }

        yield return new CheckpointStep(
            "base portable visual catalog",
            _ => customItems
                .GroupBy(item => item.Thing.def.defName)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => string.Join(",", group.Select(item => item.Summary)),
                    StringComparer.Ordinal));
    }

    private void AddStuffPairs(
        Map map,
        string rowName,
        IntVec3 origin,
        string defName,
        IEnumerable<(string Name, ThingDef Stuff)> materials)
    {
        foreach (var (name, stuff) in materials)
        {
            var cell = origin + new IntVec3(MaterialColumn(name), 0, 0);
            var def = DefDatabase<ThingDef>.GetNamed(defName);
            var cleanCount = def.stackLimit > 1 ? Math.Min(5, def.stackLimit) : 1;
            var dirtyCount = def.stackLimit > 1 ? Math.Min(3, def.stackLimit) : 1;
            AddCustom(map, rowName, cell, defName, stuff, dirty: false, cleanCount);
            AddCustom(map, rowName, cell + IntVec3.East, defName, stuff, dirty: true, dirtyCount);
        }
    }

    private void AddFixedPair(Map map, string rowName, IntVec3 cell, string defName)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        AddCustom(map, rowName, cell, defName, stuff: null, dirty: false, Math.Min(5, def.stackLimit));
        AddCustom(map, rowName, cell + IntVec3.East, defName, stuff: null, dirty: true, Math.Min(3, def.stackLimit));
    }

    private PortableFixture AddCustom(
        Map map,
        string rowName,
        IntVec3 cell,
        string defName,
        ThingDef? stuff,
        bool dirty,
        int stackCount)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var thing = ThingMaker.MakeThing(def, stuff);
        thing.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
        if (thing.TryGetComp<CompSanitation>() is { } sanitation)
        {
            if (dirty)
            {
                sanitation.MarkDirty();
            }
            else
            {
                sanitation.MarkClean();
            }
        }
        else
        {
            EndToEndAssert.True(!dirty, defName + " cannot represent a requested dirty visual fixture.");
        }

        thing.stackCount = stackCount;
        GenSpawn.Spawn(thing, cell, map);
        var fixture = new PortableFixture(thing, dirty, stackCount);
        customItems.Add(fixture);
        if (!rowIds.TryGetValue(rowName, out var ids))
        {
            ids = new List<string>();
            rowIds.Add(rowName, ids);
        }
        ids.Add(thing.ThingID);
        return fixture;
    }

    private Thing AddVanillaReference(Map map, IntVec3 cell, ThingDef def, int stackCount)
    {
        var thing = ThingMaker.MakeThing(def);
        thing.stackCount = Math.Min(stackCount, def.stackLimit);
        GenSpawn.Spawn(thing, cell, map);
        vanillaReferences.Add(thing);
        return thing;
    }

    private static int MaterialColumn(string materialName) => materialName switch
    {
        "wood" => 0,
        "granite" => 4,
        "steel" => 8,
        "silver" => 12,
        "gold" => 16,
        _ => throw new EndToEndAssertionException("Unknown visual-catalog material column: " + materialName)
    };

    private static IntVec3 FindCatalogCenter(Map map)
    {
        const int halfWidth = 21;
        const int halfHeight = 13;
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

    private sealed class PortableFixture
    {
        internal PortableFixture(Thing thing, bool expectedDirty, int expectedStackCount)
        {
            Thing = thing;
            ExpectedDirty = expectedDirty;
            ExpectedStackCount = expectedStackCount;
        }

        internal Thing Thing { get; }

        internal bool ExpectedDirty { get; }

        private int ExpectedStackCount { get; }

        internal string Summary
        {
            get
            {
                var summary = (Thing.Stuff?.defName ?? "fixed") + ":" +
                              (ExpectedDirty ? "dirty" : "clean") + ":" +
                              ExpectedStackCount;
                if (Thing is Apparel)
                {
                    return summary + ":apparel-ground";
                }

                var material = Thing.Graphic.MatSingleFor(Thing);
                var secondary = material.HasProperty("_ColorTwo")
                    ? material.GetColor("_ColorTwo").ToString()
                    : "none";
                var mask = material.HasProperty("_MaskTex")
                    ? material.GetTexture("_MaskTex")?.name ?? "missing"
                    : "none";
                return summary + ":shader=" + material.shader.name +
                       ":primary=" + material.color +
                       ":secondary=" + secondary +
                       ":mask=" + mask;
            }
        }

        internal void AssertRendered()
        {
            EndToEndAssert.Equal(ExpectedStackCount, Thing.stackCount,
                Thing.def.defName + " must render the requested real stack overlay count.");
            EndToEndAssert.Equal(
                ExpectedDirty,
                Thing.TryGetComp<CompSanitation>()?.IsDirty == true,
                Thing.def.defName + " must render the requested sanitation state.");
            // An unworn Apparel does not expose Thing.Graphic safely in RimWorld 1.6. Its finalized
            // graphicData path plus native map screenshot are the concrete ground-rendering oracle.
            if (Thing is Apparel)
            {
                EndToEndAssert.True(
                    Thing.def.graphicData?.texPath.StartsWith("ImmersiveChefs/", StringComparison.Ordinal) == true,
                    Thing.def.defName + " must resolve its packaged apparel ground art rather than a vanilla placeholder.");
            }
            else
            {
                EndToEndAssert.True(
                    Thing.Graphic.path.StartsWith("ImmersiveChefs/", StringComparison.Ordinal),
                    Thing.def.defName + " must resolve its packaged Immersive Chefs art rather than a vanilla placeholder.");
                var material = Thing.Graphic.MatSingleFor(Thing);
                EndToEndAssert.True(
                    material is not null && material.mainTexture is not null,
                    Thing.def.defName + " must resolve a concrete live texture.");
            }

            if (Thing.Stuff is not { } stuff)
            {
                return;
            }

            var stuffProperties = stuff.stuffProps;
            EndToEndAssert.True(
                Thing.def.stuffCategories is not null &&
                stuffProperties?.categories.Any(Thing.def.stuffCategories.Contains) == true,
                Thing.def.defName + " must only be shown in a material allowed by its finalized Def.");
            if (Thing is Apparel)
            {
                return;
            }
            EndToEndAssert.True(
                stuffProperties is not null && Thing.DrawColor == stuffProperties.color,
                Thing.def.defName + " must pass the exact Stuff color into its live graphic.");
            var renderedMaterialColor = Thing.Graphic.MatSingleFor(Thing).color;
            EndToEndAssert.True(
                stuffProperties is not null && renderedMaterialColor == stuffProperties.color,
                Thing.def.defName + "/" + stuff.defName +
                " must build its live material with the exact Stuff color; actual=" + renderedMaterialColor +
                ", expected=" + stuffProperties?.color + ".");
        }
    }
}
