using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

// Fixed comparison pictures, not a simulation of washing or final product acceptance.
[RimWorldEndToEndTest(
    "immersive-chefs.dishwasher-candidate-rendering", "fumblesneeze.immersivechefs",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "imranfish.xmlextensions",
    "fumblesneeze.immersivechefs", MaxFrames = 4800, MaxGameTicks = 8000, MaxWallClockSeconds = 240)]
public sealed class DishwasherCandidateRenderingTest : IRimWorldEndToEndTest
{
    private readonly List<Thing> owned = new();
    private readonly List<Texture2D> textures = new();
    private readonly List<Material> materials = new();
    private readonly List<Designator> designators = new();
    private readonly List<(ThingDef Def, IntVec3 Cell, string Hash)> candidates = new();
    private readonly Dictionary<IntVec3, TerrainDef> floors = new();
    private Map map = null!;
    private DesignationCategoryDef category = null!;
    private ThingWithComps stove = null!;
    private ThingWithComps butcher = null!;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        category = DefDatabase<DesignationCategoryDef>.GetNamed("Production");
        var oldGod = DebugSettings.godMode;
        var oldScreenshot = Find.ScreenshotModeHandler.Active;
        context.DeferCleanup(() =>
        {
            var errors = new List<Exception>();
            void Attempt(Action action) { try { action(); } catch (Exception error) { errors.Add(error); } }
            Attempt(() => Find.Selector.ClearSelection());
            foreach (var thing in owned.Where(thing => !thing.Destroyed).ToArray()) Attempt(() => thing.Destroy(DestroyMode.Vanish));
            // Include a placement whose assertion failed before it could join owned.
            foreach (var candidate in candidates)
                foreach (var thing in candidate.Cell.GetThingList(map).Where(t => ReferenceEquals(t.def, candidate.Def)).ToArray())
                    Attempt(() => thing.Destroy(DestroyMode.Vanish));
            foreach (var designator in designators) Attempt(() => category.AllResolvedDesignators.Remove(designator));
            foreach (var pair in floors) Attempt(() => map.terrainGrid.SetTerrain(pair.Key, pair.Value));
            foreach (var material in materials) Attempt(() => UnityEngine.Object.Destroy(material));
            foreach (var texture in textures) Attempt(() => UnityEngine.Object.Destroy(texture));
            Attempt(() => DebugSettings.godMode = oldGod);
            Attempt(() => Find.ScreenshotModeHandler.Active = oldScreenshot);
            if (errors.Count != 0) throw new AggregateException("Candidate fixture cleanup failed.", errors);
        });
        DebugSettings.godMode = true;
        var origins = GenRadial.RadialCellsAround(map.Center, 60, true).Where(origin =>
            new CellRect(origin.x - 9, origin.z - 7, 19, 15).Cells.All(cell =>
                cell.InBounds(map) && !cell.Fogged(map) && cell.Standable(map) &&
                map.roofGrid.RoofAt(cell) is null && cell.GetThingList(map).Count == 0 &&
                !map.terrainGrid.TopTerrainAt(cell).layerable && map.terrainGrid.UnderTerrainAt(cell) is null &&
                map.terrainGrid.FoundationAt(cell) is null && map.terrainGrid.TempTerrainAt(cell) is null &&
                map.terrainGrid.ColorAt(cell) is null && map.snowGrid.GetDepth(cell) == 0 &&
                (map.sandGrid is null || map.sandGrid.GetDepth(cell) == 0))).Take(1).ToArray();
        EndToEndAssert.Equal(1, origins.Length, "The bounded search needs a clear flat candidate-comparison site.");
        var center = origins[0];
        foreach (var cell in new CellRect(center.x - 9, center.z - 7, 19, 15).Cells)
        {
            floors[cell] = map.terrainGrid.TerrainAt(cell);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
        }
        stove = DispenserE2EFixture.SpawnBuilding(map, "ElectricStove", center + new IntVec3(-5, 0, 2));
        owned.Add(stove);
        butcher = DispenserE2EFixture.SpawnBuilding(map, "TableButcher", center + new IntVec3(-5, 0, -3));
        owned.Add(butcher);
        for (var z = 3; z <= 5; z++)
        {
            var wire = ThingMaker.MakeThing(ThingDefOf.PowerConduit);
            wire.SetFactionDirect(Faction.OfPlayer);
            owned.Add(GenSpawn.Spawn(wire, center + new IntVec3(-5, 0, z), map));
        }
        var supply = DispenserE2EFixture.SpawnBuilding(map, "VanometricPowerCell", center + new IntVec3(-5, 0, 6));
        owned.Add(supply);
        DispenserE2EFixture.SettlePower(map, new[] { stove }, 400);
        // Columns A/B compare handle removal with a broad lifting bar; rows show fixed open/closed endpoints.
        AddCandidate("A-raised", "3912721e2855b6a8a2c390094ec763790fcc5c519e5c9ebc19e8a0ad61e310a1", center + new IntVec3(0, 0, 2));
        AddCandidate("B-raised", "55fec8f1d2cf9370006c005f04aff5d0a5fea3b9ea91d749301fed8042e9beb8", center + new IntVec3(5, 0, 2));
        AddCandidate("A-closed", "65ad1a2ac2d27ff262ef33611d55821edf893b3dffbc5512cf14206f7a2b4ec4", center + new IntVec3(0, 0, -3));
        AddCandidate("B-closed", "01d5a0c11ad5fc9cf1159ba4b7600ee413d42774669d1f21fc31369d0d235624", center + new IntVec3(5, 0, -3));
    }

    private void AddCandidate(string state, string expectedHash, IntVec3 cell)
    {
        using var stream = typeof(DishwasherCandidateRenderingTest).Assembly.GetManifestResourceStream("DishwasherCandidate." + state + ".png");
        EndToEndAssert.NotNull(stream, "The exact ignored candidate art must be present when building this opt-in test.");
        using var bytes = new MemoryStream();
        stream!.CopyTo(bytes);
        var data = bytes.ToArray();
        using var sha = SHA256.Create();
        var hash = BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        EndToEndAssert.Equal(expectedHash, hash, "Candidate resource bytes must match the independently reviewed source.");
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
        textures.Add(texture);
        EndToEndAssert.True(ImageConversion.LoadImage(texture, data, false), "Unity must decode the candidate PNG.");
        EndToEndAssert.Equal(1216, texture.width, "Candidate width must retain the approved256 pixels/cell source.");
        EndToEndAssert.Equal(1024, texture.height, "Candidate height must retain the approved256 pixels/cell source.");
        var reference = (Texture2D)stove.Graphic.MatNorth.mainTexture;
        texture.name = "DishwasherCandidate." + state;
        texture.filterMode = reference.filterMode;
        texture.wrapMode = reference.wrapMode;
        texture.anisoLevel = reference.anisoLevel;
        var graphicData = new GraphicData
        {
            texPath = texture.name, graphicClass = typeof(Graphic_Single), drawSize = new Vector2(4.75f, 4f),
            drawRotated = false, allowAtlasing = false, ignoreThingDrawColor = true
        };
        var material = new Material(ShaderDatabase.Cutout) { mainTexture = texture, color = Color.white, name = texture.name };
        material.SetColor(ShaderPropertyIDs.ColorTwo, Color.white);
        materials.Add(material);
        var graphic = new CandidateGraphic(graphicData, material);
        typeof(GraphicData).GetField("cachedGraphic", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(graphicData, graphic);
        var def = new ThingDef
        {
            defName = "ChefsCandidate_" + state, label = "candidate " + state, thingClass = typeof(Building),
            category = ThingCategory.Building, size = new IntVec2(3, 1), graphicData = graphicData,
            altitudeLayer = AltitudeLayer.Building, drawerType = DrawerType.MapMeshOnly, selectable = true,
            useHitPoints = false, fillPercent = .5f, passability = Traversability.PassThroughOnly,
            building = new BuildingProperties { isEdifice = true }, staticSunShadowHeight = .2f,
            castEdgeShadows = true, defaultPlacingRot = Rot4.North, designationCategory = category
        };
        // The explicit fixture owns a temporary native designator; it does not patch a product Def or global database.
        var designator = new Designator_Build(def);
        designators.Add(designator);
        category.AllResolvedDesignators.Add(designator);
        candidates.Add((def, cell, hash));
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause candidate placement", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep("frame original Core before the clock adjustment", new[] { stove.ThingID, butcher.ThingID }, 100);
        yield return new ScreenshotStep("native clock before noon", Array.Empty<string>(), 0);
        yield return new SupportingSceneTimeActionStep("set comparison local noon", "map-" + map.uniqueID, 720);
        yield return new TimeControlActionStep("allow native lighting to settle at noon", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("observe noon daylight", _ => GenLocalDate.HourOfDay(map) == 12 && map.skyManager.CurSkyGlow > .95f,
            new EndToEndDeadline(900, 1200, TimeSpan.FromSeconds(25)));
        yield return new TimeControlActionStep("hold identical daylight for both candidates", true, EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep("native clock after noon", Array.Empty<string>(), 0);
        foreach (var candidate in candidates)
        {
            yield return new ScreenshotStep("before native placement " + candidate.Def.defName, Array.Empty<string>(), 0);
            var placement = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(Array.Empty<string>(), new[] { "Production" })
                .Single(option => option.BuildableDefName == candidate.Def.defName && !option.Disabled && option.Interaction == EndToEndGizmoInteraction.Place);
            yield return new GizmoActionStep("native Production placement " + candidate.Def.defName, Array.Empty<string>(), placement.RuntimeType,
                EndToEndGizmoInteraction.Place, stableGizmoId: placement.StableId,
                startCell: new EndToEndMapCell(candidate.Cell.x, candidate.Cell.z), architectCategoryDefNames: new[] { "Production" }, rotation: EndToEndCardinalRotation.North);
            yield return new WaitUntilStep("observe placed candidate " + candidate.Def.defName,
                _ => candidate.Cell.GetThingList(map).Any(thing => ReferenceEquals(thing.def, candidate.Def)),
                new EndToEndDeadline(180, 600, TimeSpan.FromSeconds(10)));
            var placed = candidate.Cell.GetThingList(map).Single(thing => ReferenceEquals(thing.def, candidate.Def));
            owned.Add(placed);
            yield return new SelectionActionStep("inspect native placed candidate", new[] { placed.ThingID }, false);
            yield return new CameraActionStep("frame placed candidate with Core", new[] { placed.ThingID, stove.ThingID, butcher.ThingID }, 100);
            yield return new ScreenshotStep("after native placement " + candidate.Def.defName, Array.Empty<string>(), 0);
        }
        var pictured = owned.Where(t => candidates.Any(c => ReferenceEquals(c.Def, t.def))).Concat(new Thing[] { stove, butcher }).ToArray();
        yield return new SelectionActionStep("clear candidate selection", Array.Empty<string>(), false);
        yield return new ScreenshotModeActionStep("hide labels for blind comparison", true);
        var previousRootSize = 0f;
        var previousOrthographicSize = 0f;
        foreach (var (name, rootSize) in new[] { ("close", 11f), ("ordinary", 16f), ("far", 24f) })
        {
            var rects = pictured.Select(t => t.OccupiedRect()).ToArray();
            var height = rects.Max(rect => rect.maxZ) - rects.Min(rect => rect.minZ) + 1;
            var padding = name == "close" ? 100 : (int)Math.Round(Screen.height * (1f - height / (2f * rootSize)) / 2f);
            yield return new CameraActionStep("frame " + name + " candidate comparison", pictured.Select(t => t.ThingID).ToArray(), padding);
            yield return new ScreenshotStep(name + " candidate comparison", Array.Empty<string>(), 0);
            yield return new CheckpointStep(name + " candidate render identity", _ =>
            {
                // Integer pixel padding introduces rounding; 0.5 world units allows that, not a repeated zoom.
                EndToEndAssert.True(Math.Abs(Find.CameraDriver.RootSize - rootSize) <= .5f,
                    "The camera must reach the requested useful zoom within pixel-padding rounding.");
                if (previousRootSize > 0f)
                    EndToEndAssert.True(Find.CameraDriver.RootSize >= previousRootSize * 1.3f &&
                        Find.Camera.orthographicSize >= previousOrthographicSize * 1.3f,
                        "Each real camera zoom must differ materially from its predecessor.");
                previousRootSize = Find.CameraDriver.RootSize;
                previousOrthographicSize = Find.Camera.orthographicSize;
                EndToEndAssert.True(pictured.All(t => t.Rotation == Rot4.North), "All compared objects must use the same North orientation.");
                EndToEndAssert.Equal("Things/Building/Production/TableStoveElectric", stove.Graphic.path, "The stove must use original Core.");
                EndToEndAssert.Equal("Things/Building/Production/TableButcher", butcher.Graphic.path, "The butcher must use original Core.");
                EndToEndAssert.Equal(ThingDefOf.Steel, butcher.Stuff, "The butcher must be actual Core Steel.");
                EndToEndAssert.True(stove.GetComp<CompPowerTrader>().PowerOn, "The stove must retain real power without a needs-power overlay.");
                return new Dictionary<string, string>
                {
                    ["scope"] = "candidate rendering and native placement only; fixed depicted states",
                    ["rootSize"] = Find.CameraDriver.RootSize.ToString(CultureInfo.InvariantCulture),
                    ["orthographicSize"] = Find.Camera.orthographicSize.ToString(CultureInfo.InvariantCulture),
                    ["skyGlow"] = map.skyManager.CurSkyGlow.ToString(CultureInfo.InvariantCulture),
                    ["localHour"] = GenLocalDate.HourOfDay(map).ToString(CultureInfo.InvariantCulture),
                    ["candidateHashes"] = string.Join(",", candidates.Select(c => c.Hash)),
                    ["thingIds"] = string.Join(",", pictured.Select(t => t.ThingID)),
                    ["filterMode"] = textures[0].filterMode.ToString(),
                    ["mipCount"] = textures[0].mipmapCount.ToString(CultureInfo.InvariantCulture)
                };
            });
        }
    }

    private sealed class CandidateGraphic : Graphic_Single
    {
        internal CandidateGraphic(GraphicData graphicData, Material ownedMaterial)
        {
            data = graphicData;
            path = graphicData.texPath;
            drawSize = graphicData.drawSize;
            color = colorTwo = Color.white;
            mat = ownedMaterial;
        }
    }
}
