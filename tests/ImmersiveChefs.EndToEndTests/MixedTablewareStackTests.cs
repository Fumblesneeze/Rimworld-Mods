using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest("immersive-chefs.mixed-tableware-hauling", "fumblesneeze.immersivechefs",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "imranfish.xmlextensions", "fumblesneeze.immersivechefs",
    MaxFrames = 18000, MaxGameTicks = 32000, MaxWallClockSeconds = 360)]
public sealed class MixedTablewareStackTest : IRimWorldEndToEndTest
{
    private readonly MixedTablewareScenario plates = new("ImmersiveChefs_Plate");
    private readonly MixedTablewareScenario cutlery = new("ImmersiveChefs_Cutlery");
    public void Arrange(IEndToEndContext context) => plates.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var step in plates.Execute(context)) yield return step;
        cutlery.Arrange(context);
        foreach (var step in cutlery.Execute(context)) yield return step;
    }
}

internal sealed class MixedTablewareScenario
{
    private readonly string defName;
    internal MixedTablewareScenario(string defName) => this.defName = defName;
    private Map map = null!;
    private Pawn actor = null!;
    private Thing steel = null!;
    private Thing gold = null!;
    private Zone_Stockpile pile = null!;
    private Zone_Stockpile secondPile = null!;
    private Thing partialDestination = null!;
    private float expectedValue;
    private float expectedMass;
    private IntVec3 center;
    private readonly List<Thing> owned = new();

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap;
        center = GenRadial.RadialCellsAround(map.Center, 65, true).First(c =>
            CellRect.CenteredOn(c, 6).All(p => p.InBounds(map) && p.Walkable(map) &&
                p.GetEdifice(map) is null && p.GetZone(map) is null && !p.GetThingList(map).Any(t => t is Pawn)));
        var cells = CellRect.CenteredOn(center, 6).ToArray();
        var terrain = cells.ToDictionary(p => p, p => p.GetTerrain(map));
        var home = cells.ToDictionary(p => p, p => map.areaManager.Home[p]);
        context.DeferCleanup(() =>
        {
            map = Find.CurrentMap;
            foreach (var zone in map.zoneManager.AllZones.Where(z => z.ID == pile?.ID || z.ID == secondPile?.ID).ToArray())
                map.zoneManager.DeregisterZone(zone);
            var ids = new HashSet<string>(owned.Select(t => t.ThingID));
            foreach (var t in map.listerThings.AllThings.Where(t => ids.Contains(t.ThingID)).ToArray().Reverse())
                t.Destroy(DestroyMode.Vanish);
            foreach (var p in cells) { map.terrainGrid.SetTerrain(p, terrain[p]); map.areaManager.Home[p] = home[p]; }
        });
        foreach (var p in cells) { map.terrainGrid.SetTerrain(p, TerrainDefOf.Concrete); map.areaManager.Home[p] = true; }
        actor = HandwashingE2EFixture.CreateInactiveCleaner("Tableware hauler");
        for (var attempt = 0; actor.WorkTypeIsDisabled(WorkTypeDefOf.Hauling) && attempt < 64; attempt++)
        {
            actor.Destroy(DestroyMode.Vanish);
            actor = HandwashingE2EFixture.CreateInactiveCleaner("Tableware hauler");
        }
        EndToEndAssert.False(actor.WorkTypeIsDisabled(WorkTypeDefOf.Hauling), "The fixture requires a pawn capable of hauling.");
        Spawn(actor, center + new IntVec3(-4, 0, -2));
        actor.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
        actor.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 0);
        steel = Ware(ThingDefOf.Steel, 2, center);
        gold = Ware(ThingDefOf.Gold, 4, center + new IntVec3(3, 0, 0));
        steel.HitPoints = 70;
        gold.HitPoints = 40;
        expectedValue = steel.MarketValue * 2 + gold.MarketValue * 4;
        expectedMass = steel.GetStatValue(StatDefOf.Mass) * 2 + gold.GetStatValue(StatDefOf.Mass) * 4;
        pile = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        map.zoneManager.RegisterZone(pile);
        pile.AddCell(center);
        pile.settings.filter.SetDisallowAll();
        pile.settings.filter.SetAllow(steel.def, true);
        pile.settings.Priority = StoragePriority.Critical;
    }

    public IEnumerable<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause before hauling", true, EndToEndGameSpeed.Normal);
        yield return new SupportingSceneTimeActionStep("set noon for stack comparison", "map-" + map.uniqueID, 720);
        yield return new CameraActionStep("frame two materials before native haul", new[] { steel.ThingID, gold.ThingID, actor.ThingID }, 230);
        yield return new ScreenshotStep("two steel and four gold plates before hauling", Array.Empty<string>(), 0);
        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(actor.ThingID, gold.ThingID);
        var haul = options.SingleOrDefault(o => !o.Disabled && o.Label.IndexOf("haul", StringComparison.OrdinalIgnoreCase) >= 0);
        EndToEndAssert.True(haul is not null, "The native haul menu must accept the other material into the plate pile: " +
            string.Join("; ", options.Select(o => o.Label)));
        yield return new FloatMenuActionStep("prioritize hauling gold plates", actor.ThingID, gold.ThingID, haul!.StableId);
        yield return new TimeControlActionStep("perform native hauling job", false, EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep("hauling merges all six physical plates", _ => steel.Spawned && steel.stackCount == 6 && gold.Destroyed,
            new EndToEndDeadline(2400, 6500, TimeSpan.FromSeconds(70)));
        yield return new WaitUntilStep("the completed haul releases its reservation", _ =>
            actor.CurJobDef != JobDefOf.HaulToCell && actor.carryTracker.CarriedThing is null,
            new EndToEndDeadline(600, 1500, TimeSpan.FromSeconds(25)));
        yield return new TimeControlActionStep("pause after merge", true, EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep("inspect mixed plate pile", new[] { steel.ThingID }, false);
        yield return new ScreenshotStep("six plates merged by the native hauling job", Array.Empty<string>(), 0);
        yield return new AssertionStep("native merge preserves each material and durability", _ =>
        {
            var units = steel.TryGetComp<CompTablewareStack>().Units;
            EndToEndAssert.Equal(2, units.Count(u => u.MaterialDefName == "Steel"), "Both steel plates remain steel.");
            EndToEndAssert.Equal(4, units.Count(u => u.MaterialDefName == "Gold"), "All four gold plates remain gold.");
            EndToEndAssert.True(units.All(u => u.HitPoints == (u.MaterialDefName == "Steel" ? 70 : 40)),
                "Native merge must retain exact per-unit HP rather than average it.");
        });
        yield return new CheckpointStep("retained native stack identity", _ => new Dictionary<string, string>
        {
            ["destination"] = steel.ThingID,
            ["count"] = steel.stackCount.ToString(),
            ["sourceDestroyed"] = gold.Destroyed.ToString()
        });
        yield return new AssertionStep("mixed stack has the original total value and mass", _ =>
        {
            EndToEndAssert.True(Math.Abs(steel.MarketValue * steel.stackCount - expectedValue) < .01f,
                "Stack wealth must equal the sum of actual steel and gold units.");
            EndToEndAssert.True(Math.Abs(steel.GetStatValue(StatDefOf.Mass) * steel.stackCount - expectedMass) < .001f,
                "Stack mass must equal the sum of actual units.");
        });
        yield return new AssertionStep("prepare a higher-priority pile with room for three", _ =>
        {
            pile.settings.Priority = StoragePriority.Normal;
            partialDestination = Ware(ThingDefOf.Silver, steel.def.stackLimit - 3, center + new IntVec3(-3, 0, 3));
            secondPile = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
            map.zoneManager.RegisterZone(secondPile);
            secondPile.AddCell(partialDestination.Position);
            secondPile.settings.filter.SetDisallowAll();
            secondPile.settings.filter.SetAllow(steel.def, true);
            secondPile.settings.Priority = StoragePriority.Critical;
            pile.settings.filter.SetDisallowAll();
        });
        var partialOptions = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(actor.ThingID, steel.ThingID);
        var partialHaul = partialOptions.SingleOrDefault(o => !o.Disabled && o.Label.IndexOf("haul", StringComparison.OrdinalIgnoreCase) >= 0);
        EndToEndAssert.True(partialHaul is not null, "Native partial haul must be offered: " + string.Join("; ", partialOptions.Select(o => o.Label)));
        yield return new FloatMenuActionStep("haul three units into the nearly full silver pile", actor.ThingID, steel.ThingID, partialHaul!.StableId);
        yield return new TimeControlActionStep("perform partial native transfer", false, EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep("three units transferred and three remain", _ =>
            partialDestination.stackCount == partialDestination.def.stackLimit && steel.Spawned && steel.stackCount == 3,
            new EndToEndDeadline(2400, 6500, TimeSpan.FromSeconds(70)));
        yield return new TimeControlActionStep("pause after split", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep("frame both resulting piles", new[] { steel.ThingID, partialDestination.ThingID }, 230);
        yield return new SelectionActionStep("inspect remaining gold plates", new[] { steel.ThingID }, false);
        yield return new ScreenshotStep("native partial haul leaves the exact gold remainder", Array.Empty<string>(), 0);
        yield return new AssertionStep("partial transfer conserves ordered material units", _ =>
        {
            var remainder = steel.TryGetComp<CompTablewareStack>().Units;
            var destination = partialDestination.TryGetComp<CompTablewareStack>().Units;
            EndToEndAssert.True(remainder.All(u => u.MaterialDefName == "Gold"), "The three remaining plates are gold.");
            EndToEndAssert.Equal(2, destination.Count(u => u.MaterialDefName == "Steel"), "Both steel units transferred.");
            EndToEndAssert.Equal(1, destination.Count(u => u.MaterialDefName == "Gold"), "One gold unit transferred.");
            EndToEndAssert.True(remainder.All(u => u.HitPoints == 40) &&
                destination.Where(u => u.MaterialDefName == "Steel").All(u => u.HitPoints == 70) &&
                destination.Where(u => u.MaterialDefName == "Gold").All(u => u.HitPoints == 40),
                "Partial hauling preserves every unit's original durability.");
        });
        var actorId = actor.ThingID;
        var steelId = steel.ThingID;
        var destinationId = partialDestination.ThingID;
        var expectedUnits = partialDestination.TryGetComp<CompTablewareStack>().Units
            .Select(u => u.MaterialDefName + ":" + u.HitPoints).ToArray();
        yield return new SaveLoadActionStep("save and reload the mixed piles", "ImmersiveChefs-MixedTableware-" + defName);
        yield return new AssertionStep("all individual materials and HP survive native loading", _ =>
        {
            map = Find.CurrentMap;
            actor = (Pawn)map.listerThings.AllThings.Single(t => t.ThingID == actorId);
            steel = map.listerThings.AllThings.Single(t => t.ThingID == steelId);
            partialDestination = map.listerThings.AllThings.Single(t => t.ThingID == destinationId);
            EndToEndAssert.True(expectedUnits.SequenceEqual(partialDestination.TryGetComp<CompTablewareStack>().Units
                .Select(u => u.MaterialDefName + ":" + u.HitPoints)), "The loaded ledger matches every pre-save unit.");
            EndToEndAssert.True(steel.stackCount == 3 && steel.Stuff == ThingDefOf.Gold, "The remainder stays three gold plates.");
        });
        yield return new SelectionActionStep("inspect the loaded mixed pile", new[] { partialDestination.ThingID }, false);
        yield return new ScreenshotStep("same mixed materials after save and reload", Array.Empty<string>(), 0);
        yield return new AssertionStep("loaded tableware has no quality grade", _ =>
            EndToEndAssert.True(partialDestination.TryGetComp<CompQuality>() is null &&
                !partialDestination.TryGetQuality(out var obsoleteQuality), "Loading must not recreate obsolete crafting quality."));
        yield return ToggleDraft(context, false);
        var catalog = new List<Thing>();
        yield return new AssertionStep("arrange count comparison using the shipped sprites", _ =>
        {
            for (var i = 0; i < 4; i++)
            {
                var t = Ware(i % 2 == 0 ? ThingDefOf.Steel : ThingDefOf.Gold, new[] { 1, 2, 5, 6 }[i],
                    center + new IntVec3(-5 + i * 3, 0, -4));
                catalog.Add(t);
            }
        });
        yield return new SelectionActionStep("clear selection for stack comparison", Array.Empty<string>(), false);
        yield return new ScreenshotModeActionStep("hide UI for count comparison", true);
        foreach (var zoom in new[] { 11f, 16f, 24f })
        {
            var padding = (int)Math.Round(UnityEngine.Screen.height * (1f - 1f / (2f * zoom)) / 2f);
            yield return new CameraActionStep("frame " + defName + " at " + zoom, catalog.Select(t => t.ThingID), padding);
            yield return new CheckpointStep("actual camera for " + defName + " zoom " + zoom,
                _ => new Dictionary<string, string> { ["orthographicSize"] = Find.Camera.orthographicSize.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            yield return new ScreenshotStep(defName + " counts 1 2 5 6 at zoom " + zoom, Array.Empty<string>(), 0);
        }
        yield return new ScreenshotModeActionStep("restore UI after count comparison", false);

    }

    private GizmoActionStep ToggleDraft(IEndToEndContext context, bool expected)
    {
        var option = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { actor.ThingID }, Array.Empty<string>())
            .Single(item => !item.Disabled && item.ToggleState == expected && item.HotKeyDefName == "Command_ColonistDraft");
        return new GizmoActionStep("toggle native Draft", new[] { actor.ThingID }, option.RuntimeType,
            EndToEndGizmoInteraction.Toggle, stableGizmoId: option.StableId);
    }

    private Thing Ware(ThingDef material, int count, IntVec3 at)
    {
        var t = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(defName), material);
        t.stackCount = count;
        EndToEndAssert.True(t.TryGetComp<CompQuality>() is null, "Tableware must not have a quality grade.");
        t.SetForbidden(false, false);
        Spawn(t, at);
        return t;
    }

    private void Spawn(Thing thing, IntVec3 at)
    {
        owned.Add(thing);
        GenSpawn.Spawn(thing, at, map);
    }
}
