using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest("immersive-chefs.mixed-tableware-fire-and-split", "fumblesneeze.immersivechefs",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "imranfish.xmlextensions", "fumblesneeze.immersivechefs",
    MaxFrames = 10000, MaxGameTicks = 26000, MaxWallClockSeconds = 320)]
public sealed class MixedTablewareFireAndSplitTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private IReadOnlyList<IntVec3> centers = null!;
    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap;
        centers = PlateMaterialTierTest.FindRoomCenters(map, 2);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        for (var caseIndex = 0; caseIndex < 2; caseIndex++)
        {
            yield return MixedTablewareAcceptance.Pause("pause before fire case " + caseIndex);
            var center = centers[caseIndex];
            var steel = MixedTablewareAcceptance.Ware("Plate", ThingDefOf.Steel, 1, 60);
            var wood = MixedTablewareAcceptance.Ware("Plate", ThingDefOf.WoodLog, 1, 1);
            var gold = MixedTablewareAcceptance.Ware("Plate", ThingDefOf.Gold, 1, 45);
            var pile = caseIndex == 0 ? steel : wood;
            pile.TryAbsorbStack(caseIndex == 0 ? wood : steel, true);
            pile.TryAbsorbStack(gold, true);
            GenSpawn.Spawn(pile, center, map);
            var originalRoof = map.roofGrid.RoofAt(center);
            map.roofGrid.SetRoof(center, RoofDefOf.RoofConstructed);
            context.DeferCleanup(() => map.roofGrid.SetRoof(center, originalRoof));
            EndToEndAssert.True(FireUtility.TryStartFireIn(center, map, .5f, null),
                "The prepared environment must allow fire even when hidden wood is behind steel.");
            // Fire is a disposable environmental precondition. Only native Fire ticks may apply damage.
            yield return new CameraActionStep("frame mixed pile before environmental fire damage", new[] { pile.ThingID }, 270);
            yield return new SelectionActionStep("inspect mixed pile before fire ticks", new[] { pile.ThingID }, false);
            yield return MixedTablewareAcceptance.Shot("before native fire damage with fragile unit " + (caseIndex == 0 ? "hidden" : "first"));
            yield return MixedTablewareAcceptance.Run("let the native fire damage the mixed pile");
            yield return new WaitUntilStep("fragile wood is destroyed while stronger units survive", _ =>
                pile.Destroyed || pile.stackCount < 3,
                new EndToEndDeadline(1800, 5000, TimeSpan.FromSeconds(55)));
            yield return MixedTablewareAcceptance.Pause("pause after native fire hit");
            var damage = 0;
            yield return new AssertionStep("native fire damages all units without destroying survivors", _ =>
            {
                EndToEndAssert.False(pile.Destroyed, "One broken wooden unit must not destroy the stronger steel and gold.");
                var units = pile.GetComp<CompTablewareStack>().Units;
                EndToEndAssert.Equal(2, units.Count, "Only the one-HP wooden unit should have broken.");
                damage = 60 - units.Single(u => u.MaterialDefName == "Steel").HitPoints;
                EndToEndAssert.True(damage > 0 && damage < 45, "Native fire must have applied a bounded nonzero hit.");
                MixedTablewareAcceptance.AssertUnits(pile, ("Steel", 60 - damage, 1), ("Gold", 45 - damage, 1));
            });
            yield return MixedTablewareAcceptance.Shot("native fire leaves steel and gold with matching lost durability");
            // End the disposable hazard before testing transfer. No survivor HP is changed here.
            foreach (var fire in CellRect.CenteredOn(center, 3).Cells.SelectMany(c => c.GetThingList(map))
                         .Where(t => t.def == ThingDefOf.Fire).Distinct().ToArray())
                fire.Destroy(DestroyMode.Vanish);

            Pawn hauler;
            var attempts = 0;
            do
            {
                hauler = HandwashingE2EFixture.CreateInactiveCleaner("Damaged tableware hauler");
                if (!hauler.WorkTypeIsDisabled(WorkTypeDefOf.Hauling)) break;
                hauler.Destroy(DestroyMode.Vanish);
            } while (++attempts < 32);
            EndToEndAssert.False(hauler.Destroyed, "A hauling-capable pawn is required.");
            hauler.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
            GenSpawn.Spawn(hauler, center + IntVec3.South * 3, map);
            var destination = MixedTablewareAcceptance.Ware("Plate", ThingDefOf.Silver, pile.def.stackLimit - 1, 30);
            GenSpawn.Spawn(destination, center + IntVec3.East * 3, map);
            var stockpile = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
            map.zoneManager.RegisterZone(stockpile);
            stockpile.AddCell(destination.Position);
            stockpile.settings.Priority = StoragePriority.Critical;
            stockpile.settings.filter.SetDisallowAll();
            stockpile.settings.filter.SetAllow(pile.def, true);
            yield return MixedTablewareAcceptance.Order(context, hauler, pile, "haul", "haul one surviving damaged unit into the near-full pile");
            yield return MixedTablewareAcceptance.Run("perform native partial haul after fire");
            yield return new WaitUntilStep("native haul splits the two surviving units", _ =>
                destination.stackCount == destination.def.stackLimit && pile.Spawned && pile.stackCount == 1,
                new EndToEndDeadline(1800, 5000, TimeSpan.FromSeconds(55)));
            yield return MixedTablewareAcceptance.Pause("pause after damaged stack split");
            hauler.workSettings.SetPriority(WorkTypeDefOf.Hauling, 0);
            yield return new AssertionStep("splitting cannot restore damage or resurrect broken wood", _ =>
            {
                MixedTablewareAcceptance.AssertUnits(pile, ("Gold", 45 - damage, 1));
                MixedTablewareAcceptance.AssertUnits(destination, ("Silver", 30, 24), ("Steel", 60 - damage, 1));
            });
            yield return new CameraActionStep("frame both surviving transferred units", new[] { pile.ThingID, destination.ThingID }, 230);
            yield return new SelectionActionStep("inspect the damaged gold remainder", new[] { pile.ThingID }, false);
            yield return MixedTablewareAcceptance.Shot("native partial haul retains damage and removes broken wood");
        }
    }
}
