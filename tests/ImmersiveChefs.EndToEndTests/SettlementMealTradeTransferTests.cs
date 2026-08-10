using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.settlement-meal-trade-transfer",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_800,
    MaxGameTicks = 16_000,
    MaxWallClockSeconds = 150)]
public sealed class SettlementMealTradeTransferTest : IRimWorldEndToEndTest
{
    private Pawn negotiator = null!;
    private Caravan caravan = null!;
    private Settlement settlement = null!;
    private ThingWithComps meal = null!;
    private Thing plate = null!;

    public void Arrange(IEndToEndContext context)
    {
        var arrangeStage = "select a neutral settlement trader faction";
        try
        {
            var faction = Find.FactionManager.AllFactionsVisible
                .Where(candidate =>
                    !candidate.IsPlayer &&
                    !candidate.HostileTo(Faction.OfPlayer) &&
                    candidate.def.humanlikeFaction &&
                    candidate.def.baseTraderKinds is { Count: > 0 })
                .OrderBy(candidate => candidate.loadID)
                .FirstOrDefault() ??
                throw new EndToEndAssertionException(
                    "The base game must provide one neutral humanlike settlement trader faction.");

            arrangeStage = "configure one native settlement stock generator";
            var originalBaseTraderKinds = faction.def.baseTraderKinds;
            var traderKind = originalBaseTraderKinds.First();
            var originalStockGenerators = traderKind.stockGenerators;
            var mealGenerator = CreateSingleDefStockGenerator(ThingDefOf.MealFine);
            var retainedStockGenerator = CreateSingleDefStockGenerator(ThingDefOf.Steel);
            context.DeferCleanup(() =>
            {
                traderKind.stockGenerators = originalStockGenerators;
                faction.def.baseTraderKinds = originalBaseTraderKinds;
            });
            faction.def.baseTraderKinds = new List<TraderKindDef> { traderKind };
            traderKind.stockGenerators = new List<StockGenerator>
            {
                mealGenerator,
                retainedStockGenerator
            };
            mealGenerator.ResolveReferences(traderKind);
            retainedStockGenerator.ResolveReferences(traderKind);

            arrangeStage = "create the native settlement world object";
            settlement = CreateSettlement(
                faction,
                TileFinder.RandomSettlementTileFor(faction),
                "E2E Plated Meal Settlement");
            context.DeferCleanup(() => DestroyWorldObject(settlement));

            arrangeStage = "generate upstream plated settlement stock";
            var generatedGoods = settlement.Goods.ToList();
            meal = generatedGoods
                .OfType<ThingWithComps>()
                .Single(thing => thing.def == ThingDefOf.MealFine);
            var embedded = meal.GetComp<CompEmbeddedWare>();
            EndToEndAssert.NotNull(embedded,
                "Generated settlement stock must use the finalized embedded-ware component.");
            EndToEndAssert.Equal(1, embedded!.EmbeddedPlateCount,
                "The one-serving settlement stock meal must contain exactly one generated plate.");
            plate = embedded.PeekPlateThing() ??
                throw new EndToEndAssertionException(
                    "Generated settlement stock did not retain a physical plate Thing.");

            arrangeStage = "create the player trade caravan";
            negotiator = CreateNegotiator();
            var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = ThingDefOf.Silver.stackLimit;
            EndToEndAssert.True(
                negotiator.inventory.innerContainer.TryAdd(silver, canMergeWithExistingStacks: false),
                "The settlement-trade caravan must carry spendable silver.");
            caravan = CaravanMaker.MakeCaravan(
                new[] { negotiator },
                Faction.OfPlayer,
                settlement.Tile,
                addToWorldPawnsIfNotAlready: true);
            context.DeferCleanup(() => DestroyWorldObject(caravan));
            caravan.RecacheInventory();

            arrangeStage = "validate the live caravan and settlement ownership";
            EndToEndAssert.True(settlement.CanTradeNow,
                "The arranged settlement must expose its native trader after receiving stock.");
            EndToEndAssert.True(settlement.Goods.Contains(meal),
                "The settlement must own the exact plated meal before trade begins.");
            EndToEndAssert.True(caravan.AllThings.Contains(silver),
                "The player caravan must own its trade silver before the native dialog opens.");
            Find.TickManager.Pause();
        }
        catch (EndToEndAssertionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new EndToEndAssertionException(
                $"Settlement trade arrangement failed while trying to {arrangeStage} " +
                $"({exception.GetType().Name}).");
        }
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new SettlementTradeActionStep(
            "open native caravan settlement trade",
            settlement.ID,
            caravan.ID);
        yield return new WaitUntilStep(
            "native settlement trade dialog opens",
            _ => Find.WindowStack.Windows.Any(window => window is Dialog_Trade),
            new EndToEndDeadline(180, 300, TimeSpan.FromSeconds(10)));
        yield return new ScreenshotStep(
            "observe exact plated meal in native settlement stock",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return TradeDialogActionStep.AdjustTransfer(
            "buy one generated meal through the native settlement transfer control",
            meal.ThingID,
            countDelta: 1);
        yield return new WaitUntilStep(
            "native settlement deal records one meal purchase",
            _ => TradeSession.deal.AllTradeables.Any(tradeable =>
                tradeable.thingsTrader.Contains(meal) &&
                tradeable.CountToTransfer == 1),
            new EndToEndDeadline(180, 300, TimeSpan.FromSeconds(10)));
        yield return new ScreenshotStep(
            "observe generated meal selected in native settlement trade",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return TradeDialogActionStep.Accept("accept the native settlement meal purchase");
        yield return new WaitUntilStep(
            "native settlement trade closes after acceptance",
            _ => !Find.WindowStack.Windows.Any(window => window is Dialog_Trade),
            new EndToEndDeadline(240, 500, TimeSpan.FromSeconds(15)));
        yield return new AssertionStep(
            "native settlement purchase transfers the exact embedded plate without duplication",
            _ => AssertTransferredMeal());
        yield return new ScreenshotStep(
            "observe native settlement trade closed after the purchase",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "settlement meal transfer result",
            _ => new Dictionary<string, string>
            {
                ["settlementWorldObjectId"] = settlement.ID.ToString(),
                ["caravanWorldObjectId"] = caravan.ID.ToString(),
                ["mealThingId"] = meal.ThingID,
                ["plateThingId"] = plate.ThingID,
                ["caravanOwnsMeal"] = caravan.AllThings.Contains(meal).ToString(),
                ["embeddedPlateCount"] = meal.GetComp<CompEmbeddedWare>()!.EmbeddedPlateCount.ToString(),
                ["samePlate"] = ReferenceEquals(
                    plate,
                    meal.GetComp<CompEmbeddedWare>()!.PeekPlateThing()).ToString()
            });
    }

    private void AssertTransferredMeal()
    {
        caravan.RecacheInventory();
        var embedded = meal.GetComp<CompEmbeddedWare>();
        EndToEndAssert.True(!settlement.Goods.Contains(meal),
            "The settlement must release the exact meal after native acceptance.");
        EndToEndAssert.True(caravan.AllThings.Contains(meal),
            "The player caravan must own the exact purchased meal after native acceptance.");
        EndToEndAssert.NotNull(embedded,
            "The purchased meal must retain its finalized embedded-ware component.");
        EndToEndAssert.Equal(1, embedded!.EmbeddedPlateCount,
            "The settlement transfer must retain exactly one embedded plate.");
        EndToEndAssert.True(ReferenceEquals(plate, embedded.PeekPlateThing()),
            "The purchased meal must retain the original physical plate rather than generate a replacement.");
        EndToEndAssert.True(!plate.Spawned && plate.holdingOwner is not null,
            "The exact plate must remain embedded rather than duplicate during settlement transfer.");
    }

    private static Pawn CreateNegotiator()
    {
        for (var attempt = 0; attempt < 32; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                forceNoGear: true));
            if (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Talking) >= 0.95f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing) >= 0.95f)
            {
                pawn.Name = new NameSingle("Settlement Meal Buyer");
                pawn.skills.GetSkill(SkillDefOf.Social).Level = 20;
                return pawn;
            }

            pawn.Destroy(DestroyMode.Vanish);
        }

        throw new EndToEndAssertionException(
            "Could not generate a healthy settlement trade negotiator.");
    }

    private static StockGenerator_SingleDef CreateSingleDefStockGenerator(ThingDef mealDef)
    {
        var generator = new StockGenerator_SingleDef { countRange = new IntRange(1, 1) };
        var thingDefField = typeof(StockGenerator_SingleDef).GetField(
            "thingDef",
            BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new EndToEndAssertionException(
                "RimWorld 1.6 StockGenerator_SingleDef must retain its XML thingDef field.");
        thingDefField.SetValue(generator, mealDef);
        return generator;
    }

    private static Settlement CreateSettlement(Faction faction, int tile, string name)
    {
        var created = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
        created.Tile = tile;
        created.SetFaction(faction);
        created.Name = name;
        Find.WorldObjects.Add(created);
        return created;
    }

    private static void DestroyWorldObject(WorldObject worldObject)
    {
        if (!worldObject.Destroyed)
        {
            worldObject.Destroy();
        }
    }
}
