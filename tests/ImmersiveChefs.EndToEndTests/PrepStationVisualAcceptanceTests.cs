using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ImmersiveChefs.VisualTesting;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.prep-station-visual-acceptance",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "OskarPotocki.VanillaFactionsExpanded.Core",
    "VanillaExpanded.VTEXVariations",
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_800,
    MaxGameTicks = 8_000,
    MaxWallClockSeconds = 240)]
public sealed class PrepStationVisualAcceptanceTest : WorkstationVisualAcceptanceScenario
{
    public PrepStationVisualAcceptanceTest() : base("PrepStation") { }
}

[RimWorldEndToEndTest(
    "immersive-chefs.sauce-station-visual-acceptance",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "OskarPotocki.VanillaFactionsExpanded.Core",
    "VanillaExpanded.VTEXVariations",
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_800,
    MaxGameTicks = 8_000,
    MaxWallClockSeconds = 240)]
public sealed class SauceStationVisualAcceptanceTest : WorkstationVisualAcceptanceScenario
{
    public SauceStationVisualAcceptanceTest() : base("SauceStation") { }
}

public abstract class WorkstationVisualAcceptanceScenario : IRimWorldEndToEndTest
{
    private readonly string DefName;
    private readonly string BasePath;
    private readonly string? SupportDefName;
    private readonly bool GroundShadowsOnly;

    protected WorkstationVisualAcceptanceScenario(string textureName, string textureCategory = "KitchenStation",
        string? supportDefName = null, bool groundShadowsOnly = false)
    {
        DefName = "ImmersiveChefs_" + textureName;
        BasePath = "ImmersiveChefs/Things/Building/" + textureCategory + "/" + textureName;
        SupportDefName = supportDefName;
        GroundShadowsOnly = groundShadowsOnly;
    }
    private readonly List<(Rot4 Rotation, IntVec3 Cell, Building Reference, Building Stove, Building? Support)> fixtures = new();
    private readonly List<Thing> owned = new();
    private IntVec3 galleryOrigin;
    private CellRect[] galleryFootprints = Array.Empty<CellRect>();
    private string initialMoteSnapshot = "";
    private EndToEndGizmoOption placement = null!;

    public void Arrange(IEndToEndContext context)
    {
        var originalGodMode = DebugSettings.godMode;
        context.DeferCleanup(() => DebugSettings.godMode = originalGodMode);
        var originalScreenshotMode = Find.ScreenshotModeHandler.Active;
        context.DeferCleanup(() => Find.ScreenshotModeHandler.Active = originalScreenshotMode);
        context.DeferCleanup(() =>
        {
            foreach (var thing in owned.Where(thing => !thing.Destroyed).ToArray())
                thing.Destroy(DestroyMode.Vanish);
        });
        DebugSettings.godMode = true;
        var map = Current.Game.CurrentMap;
        var def = DefDatabase<ThingDef>.GetNamed(DefName);
        var referenceDef = DefDatabase<ThingDef>.GetNamed("TableButcher");
        var stoveDef = DefDatabase<ThingDef>.GetNamed("ElectricStove");
        var supportDef = SupportDefName is null ? null : DefDatabase<ThingDef>.GetNamed(SupportDefName);
        var orientations = new[]
                 {
                     (Rot4.North, new IntVec3(-12, 0, 10)),
                     (Rot4.East, new IntVec3(12, 0, 10)),
                     (Rot4.South, new IntVec3(-12, 0, -10)),
                     (Rot4.West, new IntVec3(12, 0, -10))
                 };
        if (GroundShadowsOnly)
            orientations = orientations.Take(2).ToArray();
        CellRect[] FootprintsAt(IntVec3 origin) => orientations.SelectMany(entry => new[]
        {
            GenAdj.OccupiedRect(origin + entry.Item2, entry.Item1, def.size),
            GenAdj.OccupiedRect(origin + entry.Item2 + new IntVec3(-5, 0, 0), entry.Item1, referenceDef.size),
            GenAdj.OccupiedRect(origin + entry.Item2 + new IntVec3(5, 0, 0), entry.Item1, stoveDef.size)
        }.Concat(supportDef is null ? Array.Empty<CellRect>() : new[]
        {
            GenAdj.OccupiedRect(origin + entry.Item2, entry.Item1, supportDef.size)
        })).ToArray();
        // God-mode placement permits occupied cells. Check actual visual clearance as well,
        // including motes omitted from ListerThings, before creating any owned fixtures.
        var origins = GenRadial.RadialCellsAround(map.Center, 60f, true)
            .Where(origin => GalleryIsClear(map, FootprintsAt(origin), out _))
            .Where(origin => orientations.All(entry =>
            {
                var cell = origin + entry.Item2;
                return GenConstruct.CanPlaceBlueprintAt(supportDef ?? def, cell, entry.Item1, map,
                           godMode: true, stuffDef: supportDef?.MadeFromStuff == true ? ThingDefOf.Steel : null).Accepted &&
                       GenConstruct.CanPlaceBlueprintAt(referenceDef, cell + new IntVec3(-5, 0, 0),
                           entry.Item1, map, godMode: true, stuffDef: ThingDefOf.Steel).Accepted &&
                       GenConstruct.CanPlaceBlueprintAt(stoveDef, cell + new IntVec3(5, 0, 0),
                           entry.Item1, map, godMode: true).Accepted;
            }))
            .Take(1).ToArray();
        EndToEndAssert.True(origins.Length == 1,
            "The bounded gallery search must find native placement and Core comparison cells.");
        var origin = origins[0];
        galleryOrigin = origin;
        galleryFootprints = FootprintsAt(origin);
        foreach (var (rotation, offset) in orientations)
        {
            var cell = origin + offset;
            var referenceCell = cell + new IntVec3(-5, 0, 0);
            var stoveCell = cell + new IntVec3(5, 0, 0);
            Building? support = null;
            if (supportDef is not null)
            {
                support = (Building)ThingMaker.MakeThing(supportDef,
                    supportDef.MadeFromStuff ? ThingDefOf.Steel : null);
                support.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(support, cell, map, rotation);
                owned.Add(support);
                // A powered workbench's own interaction cell must stay clear. Let the
                // unchanged native placement workers choose a valid cell on this support.
                var supportedCells = support.OccupiedRect().Cells
                    .Where(candidate => GenConstruct.CanPlaceBlueprintAt(def, candidate, rotation, map,
                        godMode: true).Accepted)
                    .OrderBy(candidate => candidate.DistanceToSquared(support.Position))
                    .Take(1).ToArray();
                EndToEndAssert.True(supportedCells.Length == 1,
                    "The owned support must offer a native-valid microwave position with a distinct interaction cell.");
                cell = supportedCells[0];
            }
            EndToEndAssert.True(
                GenConstruct.CanPlaceBlueprintAt(def, cell, rotation, map, godMode: true).Accepted &&
                GenConstruct.CanPlaceBlueprintAt(referenceDef, referenceCell, rotation, map,
                    godMode: true, stuffDef: ThingDefOf.Steel).Accepted &&
                GenConstruct.CanPlaceBlueprintAt(stoveDef, stoveCell, rotation, map, godMode: true).Accepted,
                "The isolated map must provide clear native placement and Core comparison cells.");
            var reference = (Building)ThingMaker.MakeThing(referenceDef, ThingDefOf.Steel);
            reference.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(reference, referenceCell, map, rotation);
            owned.Add(reference);
            var stove = (Building)ThingMaker.MakeThing(stoveDef);
            stove.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(stove, stoveCell, map, rotation);
            owned.Add(stove);
            fixtures.Add((rotation, cell, reference, stove, support));
        }

        placement = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(Array.Empty<string>(), new[] { "Production" })
            .Single(option => option.BuildableDefName == DefName &&
                              option.Interaction == EndToEndGizmoInteraction.Place && !option.Disabled);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var catalog = context.GetRequiredService<IEndToEndGizmoCatalog>();
        yield return new TimeControlActionStep("pause for texture inspection", paused: true,
            EndToEndGameSpeed.Normal);
        foreach (var fixture in fixtures)
        foreach (var (comparator, corePath) in new[]
                 {
                     (fixture.Reference, "Things/Building/Production/TableButcher"),
                     (fixture.Stove, "Things/Building/Production/TableStoveElectric")
                 })
        {
            for (var attempt = 0; attempt < 64 && comparator.Graphic.path != corePath; attempt++)
            {
                var change = catalog.Query(new[] { comparator.ThingID }, Array.Empty<string>())
                    .Single(option => !option.Disabled &&
                                      option.Interaction == EndToEndGizmoInteraction.Invoke &&
                                      option.Label == "VFE_ChangeGraphic".Translate().ToString());
                yield return new GizmoActionStep("select original Core graphic " + comparator.ThingID +
                    " attempt " + attempt, new[] { comparator.ThingID }, change.RuntimeType,
                    EndToEndGizmoInteraction.Invoke, stableGizmoId: change.StableId,
                    architectCategoryDefNames: Array.Empty<string>());
            }
            EndToEndAssert.Equal(corePath, comparator.Graphic.path,
                "The comparison furniture must render original Core artwork.");
        }
        yield return new CameraActionStep("frame vanilla workstations before clock setup",
            new[] { fixtures[0].Reference.ThingID, fixtures[0].Stove.ThingID }, 100);
        yield return new ScreenshotStep("native clock and lighting before noon setup", Array.Empty<string>(), 0);
        yield return new SupportingSceneTimeActionStep("set local noon through Gateway scene clock",
            "map-" + Current.Game.CurrentMap.uniqueID, 720);
        yield return new WaitUntilStep("observe local noon and settled daylight",
            _ => GenLocalDate.HourOfDay(Current.Game.CurrentMap) == 12 &&
                 Current.Game.CurrentMap.skyManager.CurSkyGlow > 0.95f,
            new EndToEndDeadline(180, 600, TimeSpan.FromSeconds(10)));
        yield return new ScreenshotStep("native clock and lighting after noon setup", Array.Empty<string>(), 0);
        foreach (var fixture in fixtures)
        {
            var direction = fixture.Rotation.AsInt.ToString(CultureInfo.InvariantCulture);
            yield return new CameraActionStep("frame empty placement " + direction,
                new[] { fixture.Reference.ThingID, fixture.Stove.ThingID }, 100);
            yield return new ScreenshotStep("before native workstation placement " + direction,
                Array.Empty<string>(), 0);
            yield return new GizmoActionStep("place workstation through native Production " + direction,
                Array.Empty<string>(), placement.RuntimeType, EndToEndGizmoInteraction.Place,
                stableGizmoId: placement.StableId,
                startCell: new EndToEndMapCell(fixture.Cell.x, fixture.Cell.z),
                architectCategoryDefNames: new[] { "Production" },
                rotation: (EndToEndCardinalRotation)fixture.Rotation.AsInt);
            yield return new WaitUntilStep("observe placed workstation " + direction,
                _ => FindPlaced(fixture.Cell) is { } building && building.Rotation == fixture.Rotation,
                new EndToEndDeadline(180, 600, TimeSpan.FromSeconds(10)));
            var placed = FindPlaced(fixture.Cell)!;
            owned.Add(placed);
            if (fixture.Support is not null)
                EndToEndAssert.True(ReferenceEquals(fixture.Support,
                        MicrowaveSupportRuntime.FindAt(fixture.Cell, placed.Map, placed)),
                    "The native microwave placement must retain its exact real table or workbench support.");
            yield return new SelectionActionStep("inspect player-placed workstation " + direction,
                new[] { placed.ThingID }, additive: false);
            yield return new ScreenshotStep("after native workstation placement " + direction,
                Array.Empty<string>(), 0);

            var firstPath = placed.Graphic.path;
            EndToEndAssert.True(firstPath == BasePath || firstPath == BasePath + "_Variant01",
                "Native construction must render one of the declared workstation families.");
            // Supporting visual setup after native placement has been observed. These are
            // owned disposable objects in a paused map; power behavior is not under test.
            foreach (var building in new[] { placed, fixture.Stove, fixture.Support })
            {
                var power = building?.TryGetComp<CompPowerTrader>();
                if (power is not null)
                    power.PowerOn = true;
            }
            foreach (var step in CaptureZooms(placed, fixture.Reference, fixture.Stove, fixture.Support, direction + " initial"))
                yield return step;

            if (GroundShadowsOnly)
            {
                yield return new CheckpointStep("loaded native ground-shadow settings " + direction, _ =>
                {
                    foreach (var building in new[] { placed, fixture.Reference, fixture.Stove })
                    {
                        EndToEndAssert.True(building.def.castEdgeShadows,
                            "Placed appliances and Core comparators must use native edge shadows.");
                        EndToEndAssert.True(Math.Abs(building.def.staticSunShadowHeight - 0.20f) < 0.0001f,
                            "Placed appliances must use the Core workstation sun-shadow height.");
                    }
                    return new Dictionary<string, string>
                    {
                        ["placedThing"] = placed.ThingID,
                        ["rotation"] = direction,
                        ["purpose"] = "Ground-shadow comparison only; texture identity and style remain unaccepted."
                    };
                });
                yield return new SupportingSceneTimeActionStep("set morning for ground shadows " + direction,
                    "map-" + Current.Game.CurrentMap.uniqueID, 540);
                yield return new WaitUntilStep("observe settled morning shadows " + direction,
                    _ => GenLocalDate.HourOfDay(Current.Game.CurrentMap) == 9,
                    new EndToEndDeadline(180, 600, TimeSpan.FromSeconds(10)));
                foreach (var step in CaptureZooms(placed, fixture.Reference, fixture.Stove, fixture.Support,
                             direction + " morning ground shadows"))
                    yield return step;
                yield return new SupportingSceneTimeActionStep("restore comparison noon " + direction,
                    "map-" + Current.Game.CurrentMap.uniqueID, 720);
                yield return new WaitUntilStep("observe restored noon " + direction,
                    _ => GenLocalDate.HourOfDay(Current.Game.CurrentMap) == 12 &&
                         Current.Game.CurrentMap.skyManager.CurSkyGlow > 0.95f,
                    new EndToEndDeadline(180, 600, TimeSpan.FromSeconds(10)));
                continue;
            }

            for (var attempt = 0; attempt < 16 && placed.Graphic.path == firstPath; attempt++)
            {
                var change = catalog.Query(new[] { placed.ThingID }, Array.Empty<string>())
                    .Single(option => !option.Disabled &&
                                      option.Interaction == EndToEndGizmoInteraction.Invoke &&
                                      option.Label == "VFE_ChangeGraphic".Translate().ToString());
                yield return new GizmoActionStep("native random graphic " + direction + " attempt " + attempt,
                    new[] { placed.ThingID }, change.RuntimeType, EndToEndGizmoInteraction.Invoke,
                    stableGizmoId: change.StableId, architectCategoryDefNames: Array.Empty<string>());
            }
            EndToEndAssert.True(placed.Graphic.path != firstPath &&
                                  (placed.Graphic.path == BasePath || placed.Graphic.path == BasePath + "_Variant01"),
                "The unchanged native two-member variation family must expose the other rendering.");
            yield return new SelectionActionStep("inspect native graphic change " + direction,
                new[] { placed.ThingID }, additive: false);
            yield return new ScreenshotStep("after native graphic change " + direction,
                Array.Empty<string>(), 0);
            foreach (var step in CaptureZooms(placed, fixture.Reference, fixture.Stove, fixture.Support, direction + " alternate"))
                yield return step;

            foreach (var ratio in new[] { 0.55f, 0.15f })
            {
                yield return new SupportingHitPointFixtureActionStep(
                    "arrange supporting damage grade " + direction + " " + ratio,
                    new[] { new EndToEndHitPointFixture(placed.ThingID, ratio) });
                yield return new SelectionActionStep("inspect damage grade " + direction,
                    new[] { placed.ThingID }, additive: false);
                yield return new CameraActionStep("frame damage grade " + direction,
                    new[] { placed.ThingID, fixture.Reference.ThingID, fixture.Stove.ThingID }, 100);
                yield return new ScreenshotStep("supporting damage rendering " + direction + " " + ratio,
                    Array.Empty<string>(), 0);
            }
        }
    }

    private Building? FindPlaced(IntVec3 cell) =>
        cell.GetThingList(Current.Game.CurrentMap).OfType<Building>()
            .SingleOrDefault(building => building.def.defName == DefName && building.Faction == Faction.OfPlayer);

    private bool GalleryIsClear(Map map, IEnumerable<CellRect> footprints, out string obstruction)
    {
        var moteCells = new Dictionary<IntVec3, string>();
        var pendingCells = new Dictionary<IntVec3, string>();
        foreach (var mote in map.dynamicDrawManager.DrawThings.OfType<Mote>()
                     .Where(mote => mote.Spawned && !owned.Contains(mote)))
        {
            var drawn = mote.DrawPos;
            var description = $"{mote.ThingID} def={mote.def.defName} stored={mote.Position} drawn={drawn}";
            // Core mood thoughts use one attachment. Their first DrawAt refreshes exactPosition
            // even while paused; refresh only a copy and preserve a destroyed target's cache.
            if (mote.GetType() == typeof(MoteBubble) && mote.link1.Linked &&
                (mote.def == ThingDefOf.Mote_ThoughtBad || mote.def == ThingDefOf.Mote_ThoughtGood))
            {
                var link = mote.link1;
                if (!link.Target.ThingDestroyed)
                    link.UpdateDrawPos();
                var predicted = link.LastDrawPos + mote.def.mote.attachedDrawOffset + drawn - mote.exactPosition;
                description +=
                    $" predicted={predicted} targetDestroyed={link.Target.ThingDestroyed}";
                pendingCells[predicted.ToIntVec3()] = description;
            }
            moteCells[drawn.ToIntVec3()] = description;
        }
        if (owned.Count == 0)
            initialMoteSnapshot = string.Join(";", pendingCells.Values.Take(8));
        var blocked = GalleryClearance.FindBlockedCell(footprints.Select(rect =>
            (rect.minX, rect.minZ, rect.maxX, rect.maxZ)), (x, z) =>
        {
            var cell = new IntVec3(x, 0, z);
            return cell.InBounds(map) && !cell.Fogged(map) && !cell.Roofed(map) &&
                   !moteCells.ContainsKey(cell) && cell.GetThingList(map).All(owned.Contains);
        }, pendingCells.Keys.Select(cell => (cell.x, cell.z)));
        obstruction = "";
        if (blocked is null)
            return true;
        var blockedCell = new IntVec3(blocked.Value.X, 0, blocked.Value.Z);
        var reason = !blockedCell.InBounds(map) ? "out-of-bounds" :
            blockedCell.Fogged(map) ? "fog" : blockedCell.Roofed(map) ? "roof" :
            pendingCells.TryGetValue(blockedCell, out var pending) ? "pending mote " + pending :
            moteCells.TryGetValue(blockedCell, out var current) ? "drawn mote " + current :
            string.Join(",", blockedCell.GetThingList(map).Where(thing => !owned.Contains(thing))
                .Take(8).Select(thing => thing.ThingID + " def=" + thing.def.defName));
        obstruction = $"cell={blockedCell} reason={reason}";
        return false;
    }

    private IEnumerable<EndToEndStep> CaptureZooms(Building placed, Building reference, Building stove,
        Building? support, string label)
    {
        yield return new SelectionActionStep("clear selection for visual comparison " + label,
            Array.Empty<string>(), additive: false);
        yield return new ScreenshotModeActionStep("hide interface for visual comparison " + label, true);
        foreach (var (zoom, desiredRootSize) in new[] { ("close", 11f), ("ordinary", 16f), ("far", 24f) })
        {
            var visualThings = new[] { placed, reference, stove, support }
                .Where(thing => thing is not null).Cast<Building>().ToArray();
            var rectangles = visualThings.Select(thing => thing.OccupiedRect()).ToArray();
            var height = rectangles.Max(rectangle => rectangle.maxZ) -
                         rectangles.Min(rectangle => rectangle.minZ) + 1;
            // Convert the requested framing size to the existing pixel-padding camera contract.
            // An appliance may occupy an edge cell while its support stays centered.
            var padding = zoom == "close" ? 100 :
                (int)Math.Round(UI.screenHeight * (1f - height / (2f * desiredRootSize)) / 2f);
            yield return new CameraActionStep("set " + zoom + " camera " + label,
                visualThings.Select(thing => thing.ThingID).ToArray(), padding);
            yield return new CheckpointStep(zoom + " gallery clearance " + label, _ =>
            {
                var clear = GalleryIsClear(Current.Game.CurrentMap, galleryFootprints, out var obstruction);
                EndToEndAssert.True(clear,
                    $"Gallery {galleryOrigin} footprints={string.Join(";", galleryFootprints)} margin={GalleryClearance.Margin}: {obstruction}");
                return new Dictionary<string, string>
                {
                    ["galleryOrigin"] = galleryOrigin.ToString(),
                    ["footprints"] = string.Join(";", galleryFootprints.Select(rect => rect.ToString())),
                    ["marginCells"] = GalleryClearance.Margin.ToString(CultureInfo.InvariantCulture),
                    ["unrelatedObstructions"] = "0",
                    ["initialAttachedThoughts"] = initialMoteSnapshot
                };
            });
            yield return new ScreenshotStep(zoom + " visual " + label, Array.Empty<string>(), 0);
            yield return new CheckpointStep(zoom + " rendering identity " + label, _ =>
            {
                EndToEndAssert.Equal("Things/Building/Production/TableButcher", reference.Graphic.path,
                    "The butcher comparator must retain its original Core artwork.");
                EndToEndAssert.Equal("Things/Building/Production/TableStoveElectric", stove.Graphic.path,
                    "The stove comparator must retain its original Core artwork.");
                var stationPower = placed.TryGetComp<CompPowerTrader>();
                EndToEndAssert.True(stationPower is null || stationPower.PowerOn,
                    "Supporting power setup must keep needs-power icons off the workstation artwork.");
                EndToEndAssert.True(stove.TryGetComp<CompPowerTrader>().PowerOn,
                    "Supporting power setup must keep needs-power icons off the Core stove artwork.");
                if (support is not null)
                    EndToEndAssert.True(ReferenceEquals(support,
                            MicrowaveSupportRuntime.FindAt(placed.Position, placed.Map, placed)),
                        "The rendered microwave must retain its exact owned support after native variation.");
                var supportPower = support?.TryGetComp<CompPowerTrader>();
                EndToEndAssert.True(supportPower is null || supportPower.PowerOn,
                    "Supporting power setup must keep needs-power icons off the owned support artwork.");
                return new Dictionary<string, string>
                {
                    ["thingId"] = placed.ThingID,
                    ["rotation"] = placed.Rotation.AsInt.ToString(CultureInfo.InvariantCulture),
                    ["graphicPath"] = placed.Graphic.path,
                    ["orthographicSize"] = Find.Camera.orthographicSize.ToString(CultureInfo.InvariantCulture),
                    ["hitPoints"] = placed.HitPoints.ToString(CultureInfo.InvariantCulture),
                    ["referenceId"] = reference.ThingID,
                    ["referenceGraphicPath"] = reference.Graphic.path,
                    ["stoveId"] = stove.ThingID,
                    ["stoveGraphicPath"] = stove.Graphic.path,
                    ["stationPowerOn"] = stationPower?.PowerOn.ToString() ?? "not-applicable",
                    ["stovePowerOn"] = stove.TryGetComp<CompPowerTrader>().PowerOn.ToString(),
                    ["supportId"] = support?.ThingID ?? "not-applicable",
                    ["supportDefName"] = support?.def.defName ?? "not-applicable",
                    ["supportGraphicPath"] = support?.Graphic.path ?? "not-applicable",
                    ["supportMatches"] = support is null ? "not-applicable" : "True",
                    ["supportPowerOn"] = supportPower?.PowerOn.ToString() ?? "not-applicable",
                    ["localHour"] = GenLocalDate.HourOfDay(Current.Game.CurrentMap).ToString(CultureInfo.InvariantCulture)
                };
            });
        }
        yield return new ScreenshotModeActionStep("restore interface after visual comparison " + label, false);
    }
}
