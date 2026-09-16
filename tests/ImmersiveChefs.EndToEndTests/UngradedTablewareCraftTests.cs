using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest("immersive-chefs.ungraded-tableware-crafting", "fumblesneeze.immersivechefs",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "imranfish.xmlextensions", "fumblesneeze.immersivechefs",
    MaxFrames = 12000, MaxGameTicks = 24000, MaxWallClockSeconds = 300)]
public sealed class UngradedTablewareCraftTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn crafter = null!;
    private Building_WorkTable table = null!;
    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap;
        var center = PreciousMetalKitchenwareCostTest.FindFixtureCenter(map);
        crafter = PreciousMetalKitchenwareCostTest.GenerateCrafter();
        GenSpawn.Spawn(crafter, center + IntVec3.South * 3, map);
        table = (Building_WorkTable)DispenserE2EFixture.SpawnBuilding(map, "TableMachining", center);
        DispenserE2EFixture.SpawnBuilding(map, "VanometricPowerCell", center + IntVec3.North * 3);
        DispenserE2EFixture.SettlePower(map, new[] { table }, 1000);
        var steel = ThingMaker.MakeThing(ThingDefOf.Steel);
        steel.stackCount = 6;
        GenSpawn.Spawn(steel, center + IntVec3.East * 3, map);
    }
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var product in new[] { ("Plate", "ImmersiveChefs_MachinePlates"), ("Cutlery", "ImmersiveChefs_MachineCutlery") })
        {
            var recipe = DefDatabase<RecipeDef>.GetNamed(product.Item2);
            var productDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_" + product.Item1);
            yield return new AssertionStep("arrange one " + product.Item1 + " bill", _ =>
            {
                table.BillStack.Clear();
                var bill = new Bill_Production(recipe) { repeatMode = BillRepeatModeDefOf.RepeatCount, repeatCount = 1 };
                bill.ingredientFilter.SetDisallowAll();
                bill.ingredientFilter.SetAllow(ThingDefOf.Steel, true);
                bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
                bill.SetPawnRestriction(crafter);
                table.BillStack.AddBill(bill);
            });
            yield return new CameraActionStep("frame native " + product.Item1 + " crafting", new[] { table.ThingID, crafter.ThingID }, 230);
            var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(crafter.ThingID, table.ThingID);
            var order = options.Single(o => !o.Disabled && o.Label.IndexOf("prioritize", StringComparison.OrdinalIgnoreCase) >= 0);
            yield return new FloatMenuActionStep("prioritize native " + product.Item1 + " bill", crafter.ThingID, table.ThingID, order.StableId);
            yield return new TimeControlActionStep("perform crafting", false, EndToEndGameSpeed.Superfast);
            yield return new WaitUntilStep("four ungraded " + product.Item1 + " produced", _ =>
                map.listerThings.ThingsOfDef(productDef).Any(t => t.Spawned && t.stackCount == 4),
                new EndToEndDeadline(3000, 9000, TimeSpan.FromSeconds(95)));
            yield return new TimeControlActionStep("pause after crafting", true, EndToEndGameSpeed.Normal);
            var output = map.listerThings.ThingsOfDef(productDef).Single(t => t.Spawned && t.stackCount == 4);
            yield return new AssertionStep("native output is ungraded", _ =>
                EndToEndAssert.True(output.Stuff == ThingDefOf.Steel && output.TryGetComp<CompQuality>() is null,
                    "Crafting must create the selected material with no quality grade."));
            yield return new SelectionActionStep("inspect crafted " + product.Item1, new[] { output.ThingID }, false);
            yield return new ScreenshotStep("crafted ungraded " + product.Item1, Array.Empty<string>(), 0);
        }
        // Reproduce the old save schema using the original native component. This
        // is a legacy-input fixture; only loading current Defs removes the grade.
        yield return new AssertionStep("arrange obsolete quality components for native save migration", _ =>
        {
            foreach (var name in new[] { "ImmersiveChefs_Plate", "ImmersiveChefs_Cutlery" })
            {
                var ware = (ThingWithComps)map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed(name))
                    .Single(t => t.Spawned && t.stackCount == 4);
                var legacy = new CompQuality { parent = ware };
                legacy.Initialize(new CompProperties { compClass = typeof(CompQuality) });
                ware.AllComps.Add(legacy);
                legacy.SetQuality(QualityCategory.Excellent, ArtGenerationContext.Colony);
                ware.HitPoints = 20;
            }
        });
        yield return new ScreenshotStep("legacy quality component before native reload", Array.Empty<string>(), 0);
        yield return new SaveLoadActionStep("save legacy quality fields and load current tableware Defs", "ImmersiveChefs-UngradedCrafted");
        yield return new AssertionStep("native load retains ungraded crafted batches", _ =>
        {
            foreach (var name in new[] { "ImmersiveChefs_Plate", "ImmersiveChefs_Cutlery" })
                EndToEndAssert.True(Find.CurrentMap.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed(name))
                    .Any(t => t.Spawned && t.stackCount == 4 && t.HitPoints == 20 && t.Stuff == ThingDefOf.Steel &&
                        t.TryGetComp<CompQuality>() is null), "Loading drops obsolete quality while retaining material, count and HP.");
        });
        var loadedCutlery = Find.CurrentMap.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"))
            .Single(t => t.Spawned && t.stackCount == 4);
        yield return new SelectionActionStep("inspect ungraded loaded cutlery", new[] { loadedCutlery.ThingID }, false);
        yield return new ScreenshotStep("obsolete quality removed by native loading", Array.Empty<string>(), 0);
    }
}
