using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest("immersive-chefs.mixed-tableware-partial-settlement-trade", "fumblesneeze.immersivechefs",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "imranfish.xmlextensions", "fumblesneeze.immersivechefs",
    MaxFrames = 6000, MaxGameTicks = 10000, MaxWallClockSeconds = 200)]
public sealed class MixedTablewarePartialSettlementTradeTest : IRimWorldEndToEndTest
{
    private Settlement settlement = null!;
    private Caravan caravan = null!;
    private ThingWithComps original = null!;
    private Thing? sold;
    private float quotedMass;
    private int quotedSilver;
    private float negotiatorGearMass;

    public void Arrange(IEndToEndContext context)
    {
        var faction = Find.FactionManager.AllFactionsVisible.Where(f => !f.IsPlayer && !f.HostileTo(Faction.OfPlayer) &&
            f.def.humanlikeFaction && f.def.baseTraderKinds is { Count: > 0 }).OrderBy(f => f.loadID).First();
        settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
        settlement.Tile = TileFinder.RandomSettlementTileFor(faction);
        settlement.SetFaction(faction);
        settlement.Name = "Mixed Tableware Trade Fixture";
        Find.WorldObjects.Add(settlement);
        context.DeferCleanup(() => { if (!settlement.Destroyed) settlement.Destroy(); });
        // Native stock initialization establishes the actual owning container; replace only this
        // disposable settlement's goods to keep the two material trades unambiguous.
        var generated = settlement.Goods.ToArray();
        var owner = settlement.trader.GetDirectlyHeldThings();
        foreach (var thing in generated) thing.Destroy(DestroyMode.Vanish);
        var traderSilver = ThingMaker.MakeThing(ThingDefOf.Silver);
        traderSilver.stackCount = 2000;
        EndToEndAssert.True(owner.TryAdd(traderSilver, false), "The disposable trader must hold sufficient silver.");

        Pawn negotiator = null!;
        for (var attempt = 0; attempt < 32; attempt++)
        {
            negotiator = HandwashingE2EFixture.CreateInactiveCleaner("Mixed tableware negotiator");
            if (negotiator.health.capacities.GetLevel(PawnCapacityDefOf.Talking) >= .95f &&
                negotiator.health.capacities.GetLevel(PawnCapacityDefOf.Hearing) >= .95f &&
                !negotiator.skills.GetSkill(SkillDefOf.Social).TotallyDisabled) break;
            negotiator.Destroy(DestroyMode.Vanish);
        }
        EndToEndAssert.False(negotiator.Destroyed, "Native trading needs a healthy negotiator.");
        negotiator.skills.GetSkill(SkillDefOf.Social).Level = 20;
        negotiatorGearMass = MassUtility.GearMass(negotiator);
        original = MixedTablewareAcceptance.Ware("Plate", ThingDefOf.Steel, 2, 60);
        var gold = MixedTablewareAcceptance.Ware("Plate", ThingDefOf.Gold, 2, 35);
        EndToEndAssert.True(original.TryAbsorbStack(gold, true), "Arrange two steel followed by two gold plates.");
        EndToEndAssert.True(negotiator.inventory.innerContainer.TryAdd(original, false), "Caravan owns the mixed pile before trading.");
        var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
        silver.stackCount = 500;
        negotiator.inventory.innerContainer.TryAdd(silver, false);
        caravan = CaravanMaker.MakeCaravan(new[] { negotiator }, Faction.OfPlayer, settlement.Tile, true);
        context.DeferCleanup(() => { if (!caravan.Destroyed) caravan.Destroy(); });
        caravan.RecacheInventory();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new SettlementTradeActionStep("open native settlement trade for partial mixed sale", settlement.ID, caravan.ID);
        yield return WaitForDialog();
        yield return MixedTablewareAcceptance.Shot("four mixed plates before selecting a partial sale");
        yield return TradeDialogActionStep.AdjustTransfer("sell three actual units through native transfer control", original.ThingID, -3);
        yield return new AssertionStep("partial sale quote uses two steel and one gold", _ =>
        {
            var trade = TradeSession.deal.AllTradeables.Single(t => t.thingsColony.Contains(original));
            EndToEndAssert.Equal(-3, trade.CountToTransfer, "Native UI selected a three-unit sale.");
            var expected = 2 * NativeSinglePrice(ThingDefOf.Steel, 60, TradeAction.PlayerSells) +
                           NativeSinglePrice(ThingDefOf.Gold, 35, TradeAction.PlayerSells);
            EndToEndAssert.True(Math.Abs(trade.GetPriceFor(TradeAction.PlayerSells) * 3 - expected) < .01f,
                "Price must quote the transferred prefix, not three units at the whole-stack average.");
            CaptureQuote();
            var remainingGoldMass = NativeSingleMass(ThingDefOf.Gold, 35);
            var expectedMass = negotiatorGearMass + remainingGoldMass + quotedSilver * ThingDefOf.Silver.GetStatValueAbstract(StatDefOf.Mass);
            EndToEndAssert.True(Math.Abs(quotedMass - expectedMass) < .001f,
                "The native caravan preview must retain the untransferred gold unit and exact currency mass.");
        });
        yield return MixedTablewareAcceptance.Shot("native partial sale price and remaining caravan mass");
        yield return TradeDialogActionStep.Accept("accept the three-unit mixed-material sale");
        yield return WaitForClosedDialog();
        yield return new AssertionStep("native settlement sale conserves both material subsets", _ =>
        {
            caravan.RecacheInventory();
            MixedTablewareAcceptance.AssertUnits(original, ("Gold", 35, 1));
            sold = settlement.Goods.Single(t => t.def == original.def);
            MixedTablewareAcceptance.AssertUnits(sold, ("Steel", 60, 2), ("Gold", 35, 1));
            AssertActualCargoMatchesQuote();
        });

        yield return new SettlementTradeActionStep("reopen trade to inspect transferred units and buy one back", settlement.ID, caravan.ID);
        yield return WaitForDialog();
        yield return MixedTablewareAcceptance.Shot("native settlement owns three sold plates and caravan retains one gold plate");
        yield return TradeDialogActionStep.AdjustTransfer("buy the first steel unit from the mixed trader pile", sold!.ThingID, 1);
        yield return new AssertionStep("partial purchase quote uses the actual first steel unit", _ =>
        {
            var trade = TradeSession.deal.AllTradeables.Single(t => t.thingsTrader.Contains(sold!));
            EndToEndAssert.True(Math.Abs(trade.GetPriceFor(TradeAction.PlayerBuys) -
                NativeSinglePrice(ThingDefOf.Steel, 60, TradeAction.PlayerBuys)) < .01f,
                "Buying one must quote its actual steel value, not the mixed-stack average.");
            CaptureQuote();
            var expectedMass = negotiatorGearMass + NativeSingleMass(ThingDefOf.Gold, 35) + NativeSingleMass(ThingDefOf.Steel, 60) +
                quotedSilver * ThingDefOf.Silver.GetStatValueAbstract(StatDefOf.Mass);
            EndToEndAssert.True(Math.Abs(quotedMass - expectedMass) < .001f,
                "The native caravan preview must include exactly the purchased steel unit.");
        });
        yield return MixedTablewareAcceptance.Shot("native partial purchase price and resulting caravan mass");
        yield return TradeDialogActionStep.Accept("accept purchase of the single steel plate");
        yield return WaitForClosedDialog();
        yield return new AssertionStep("native purchase preserves all four original physical units", _ =>
        {
            caravan.RecacheInventory();
            MixedTablewareAcceptance.AssertUnits(sold!, ("Steel", 60, 1), ("Gold", 35, 1));
            var cargoUnits = CaravanInventoryUtility.AllInventoryItems(caravan).Where(t => t.def == original.def)
                .SelectMany(t => t.TryGetComp<CompTablewareStack>().Units).ToArray();
            EndToEndAssert.True(cargoUnits.Length == 2 &&
                cargoUnits.Count(u => u.MaterialDefName == "Steel" && u.HitPoints == 60) == 1 &&
                cargoUnits.Count(u => u.MaterialDefName == "Gold" && u.HitPoints == 35) == 1,
                "The caravan must own one original steel and one original gold unit after the partial repurchase.");
            AssertActualCargoMatchesQuote();
        });
        yield return new SettlementTradeActionStep("reopen native dialog to observe final split inventories", settlement.ID, caravan.ID);
        yield return WaitForDialog();
        yield return MixedTablewareAcceptance.Shot("final native trader and caravan material counts after two partial transfers");
        yield return new WindowCancelActionStep("close the final trade inspection", typeof(Dialog_Trade).FullName!);
    }

    private void CaptureQuote()
    {
        quotedSilver = TradeSession.deal.CurrencyTradeable.CountPostDealFor(Transactor.Colony);
        quotedMass = CollectionsMassCalculator.MassUsageLeftAfterTradeableTransfer(
            caravan.AllThings.ToList(), TradeSession.deal.AllTradeables.ToList(), IgnorePawnsInventoryMode.Ignore);
    }
    private void AssertActualCargoMatchesQuote()
    {
        EndToEndAssert.Equal(quotedSilver, CaravanInventoryUtility.AllInventoryItems(caravan)
            .Where(t => t.def == ThingDefOf.Silver).Sum(t => t.stackCount), "Native currency transfer must equal the dialog quote.");
        EndToEndAssert.True(Math.Abs(quotedMass - CollectionsMassCalculator.MassUsage(caravan.AllThings.ToList(),
            IgnorePawnsInventoryMode.Ignore)) < .001f, "Actual caravan cargo mass must equal its pre-transfer native preview.");
    }
    private static float NativeSinglePrice(ThingDef material, int hp, TradeAction action)
    {
        var unit = MixedTablewareAcceptance.Ware("Plate", material, 1, hp);
        try
        {
            var quote = new Tradeable();
            quote.AddThing(unit, action == TradeAction.PlayerBuys ? Transactor.Trader : Transactor.Colony);
            return quote.GetPriceFor(action);
        }
        finally { unit.Destroy(DestroyMode.Vanish); }
    }
    private static float NativeSingleMass(ThingDef material, int hp)
    {
        var unit = MixedTablewareAcceptance.Ware("Plate", material, 1, hp);
        try { return unit.GetStatValue(StatDefOf.Mass); }
        finally { unit.Destroy(DestroyMode.Vanish); }
    }
    private static WaitUntilStep WaitForDialog() => new("native settlement trade dialog opens",
        _ => Find.WindowStack.Windows.Any(w => w is Dialog_Trade), new EndToEndDeadline(180, 300, TimeSpan.FromSeconds(10)));
    private static WaitUntilStep WaitForClosedDialog() => new("native settlement trade closes after acceptance",
        _ => !Find.WindowStack.Windows.Any(w => w is Dialog_Trade), new EndToEndDeadline(240, 500, TimeSpan.FromSeconds(15)));
}
