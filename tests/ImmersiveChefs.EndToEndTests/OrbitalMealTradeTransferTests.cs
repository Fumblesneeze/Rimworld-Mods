using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.orbital-meal-trade-transfer",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_800,
    MaxGameTicks = 16_000,
    MaxWallClockSeconds = 150)]
public sealed class OrbitalMealTradeTransferTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn negotiator = null!;
    private TradeShip ship = null!;
    private ThingWithComps meal = null!;
    private Thing plate = null!;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FindFixtureCenter(map);
        negotiator = CreateNegotiator();
        GenSpawn.Spawn(negotiator, center + new IntVec3(0, 0, -3), map);

        var beacon = CreatePoweredTradeFixture(center);
        PlaceTradeSilver(beacon);
        meal = GenerateTraderStockMeal(ThingDefOf.MealFine);
        var embedded = meal.GetComp<CompEmbeddedWare>();
        EndToEndAssert.NotNull(embedded, "Generated trade stock must use the finalized embedded-ware component.");
        EndToEndAssert.Equal(1, embedded!.EmbeddedPlateCount,
            "The one-serving orbital stock meal must contain exactly one generated plate.");
        plate = embedded.PeekPlateThing() ??
            throw new EndToEndAssertionException("Generated orbital stock did not retain a physical plate Thing.");

        ship = new TradeShip(DefDatabase<TraderKindDef>.GetNamed("Orbital_Exotic"), null);
        ship.GetDirectlyHeldThings().ClearAndDestroyContents();
        EndToEndAssert.True(ship.GetDirectlyHeldThings().TryAdd(meal),
            "The exact generated meal must enter the orbital trader's native stock container.");
        context.DeferCleanup(() =>
        {
            if (map.passingShipManager.passingShips.Contains(ship))
            {
                map.passingShipManager.RemoveShip(ship);
            }
        });
        map.passingShipManager.AddShip(ship);
        EndToEndAssert.True(ship.Goods.Contains(meal),
            "The orbital trader must own the generated plated meal before trade begins.");

        var beaconTradeConcept = DefDatabase<ConceptDef>.GetNamed("TradeGoodsMustBeNearBeacon");
        var priorBeaconTradeKnowledge = PlayerKnowledgeDatabase.GetKnowledge(beaconTradeConcept);
        context.DeferCleanup(() =>
            PlayerKnowledgeDatabase.SetKnowledge(beaconTradeConcept, priorBeaconTradeKnowledge));
        PlayerKnowledgeDatabase.KnowledgeDemonstrated(beaconTradeConcept, KnowledgeAmount.Total);
        ship.TryOpenComms(negotiator);
        EndToEndAssert.True(Find.WindowStack.Windows.Any(window => window is Dialog_Trade),
            "RimWorld must open its native trade dialog for the arranged orbital trader.");
        Find.TickManager.Pause();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CheckpointStep(
            "native trade dialog identity",
            _ => new Dictionary<string, string>
            {
                ["dialogType"] = Find.WindowStack.Windows.OfType<Dialog_Trade>().Single().GetType().FullName,
                ["mealThingId"] = meal.ThingID
            });
        yield return new ScreenshotStep(
            "observe exact plated meal in native orbital stock",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return TradeDialogActionStep.AdjustTransfer(
            "buy one generated meal through the native transfer arrow",
            meal.ThingID,
            countDelta: 1);
        yield return new ScreenshotStep(
            "observe native meal transfer after the typed quantity action",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new WaitUntilStep(
            "native trade dialog records one meal purchase",
            _ => TradeSession.deal.AllTradeables.Any(tradeable =>
                tradeable.AnyThing?.def == ThingDefOf.MealFine &&
                tradeable.CountToTransfer != 0),
            new EndToEndDeadline(180, 300, TimeSpan.FromSeconds(10)));
        yield return new ScreenshotStep(
            "observe exact plated meal selected for purchase",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return TradeDialogActionStep.Accept("accept the native orbital trade");
        yield return new WaitUntilStep(
            "native trade closes after acceptance",
            _ => !Find.WindowStack.Windows.Any(window => window is Dialog_Trade),
            new EndToEndDeadline(240, 500, TimeSpan.FromSeconds(15)));
        yield return new TimeControlActionStep(
            "run the orbital delivery",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the purchased plated meal lands on the colony map",
            _ => meal.Spawned && meal.Map == map,
            new EndToEndDeadline(1_800, 5_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause after the exact meal lands",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the purchased plated meal",
            new[] { meal.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the purchased plated meal",
            new[] { meal.ThingID },
            paddingPixels: 260);
        yield return new AssertionStep(
            "native orbital purchase transfers the exact embedded plate without duplication",
            _ =>
            {
                var embedded = meal.GetComp<CompEmbeddedWare>();
                EndToEndAssert.True(meal.Spawned && meal.Map == map,
                    "The exact trader-owned meal must become a colony-map Thing after native acceptance.");
                EndToEndAssert.NotNull(embedded,
                    "The purchased meal must retain its finalized embedded-ware component.");
                EndToEndAssert.Equal(1, embedded!.EmbeddedPlateCount,
                    "The native transfer must retain exactly one embedded plate.");
                EndToEndAssert.True(ReferenceEquals(plate, embedded.PeekPlateThing()),
                    "The purchased meal must retain the original physical plate rather than generate a replacement.");
                EndToEndAssert.True(!plate.Spawned && plate.holdingOwner is not null,
                    "The exact plate must remain embedded rather than duplicate onto the map during transfer.");
            });
        yield return new ScreenshotStep(
            "observe purchased meal with one bound plate",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "orbital meal transfer result",
            _ => new Dictionary<string, string>
            {
                ["mealThingId"] = meal.ThingID,
                ["plateThingId"] = plate.ThingID,
                ["mealSpawned"] = meal.Spawned.ToString(),
                ["embeddedPlateCount"] = meal.GetComp<CompEmbeddedWare>()!.EmbeddedPlateCount.ToString(),
                ["samePlate"] = ReferenceEquals(plate, meal.GetComp<CompEmbeddedWare>()!.PeekPlateThing()).ToString()
            });
    }

    private Building_OrbitalTradeBeacon CreatePoweredTradeFixture(IntVec3 center)
    {
        var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
        for (var x = center.x - 6; x <= center.x + 6; x++)
        {
            var conduit = ThingMaker.MakeThing(conduitDef);
            conduit.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(conduit, new IntVec3(x, 0, center.z + 3), map);
        }

        var powerCellDef = DefDatabase<ThingDef>.GetNamed("VanometricPowerCell");
        var powerCell = ThingMaker.MakeThing(powerCellDef);
        powerCell.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(powerCell, center + new IntVec3(6, 0, 3), map, Rot4.North);

        var beaconDef = DefDatabase<ThingDef>.GetNamed("OrbitalTradeBeacon");
        var beacon = (Building_OrbitalTradeBeacon)ThingMaker.MakeThing(
            beaconDef,
            beaconDef.MadeFromStuff ? ThingDefOf.Steel : null);
        beacon.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(beacon, center + new IntVec3(2, 0, 3), map, Rot4.North);

        foreach (var powered in new ThingWithComps[] { beacon })
        {
            var flick = powered.GetComp<CompFlickable>();
            if (flick is { SwitchIsOn: false })
            {
                flick.DoFlick();
            }
        }

        map.powerNetManager.UpdatePowerNetsAndConnections_First();
        for (var tick = 0; tick <= 200 &&
             !beacon.TryGetComp<CompPowerTrader>().PowerOn; tick++)
        {
            Find.TickManager.DoSingleTick();
        }

        EndToEndAssert.True(beacon.TryGetComp<CompPowerTrader>().PowerOn,
            "The native orbital beacon must be powered by the connected vanometric source before opening trade.");
        return beacon;
    }

    private void PlaceTradeSilver(Building_OrbitalTradeBeacon beacon)
    {
        var cells = beacon.TradeableCells
            .Where(cell => cell.InBounds(map) && cell.Standable(map) && cell.GetFirstItem(map) is null)
            .Take(4)
            .ToArray();
        EndToEndAssert.Equal(4, cells.Length,
            "The orbital beacon must expose four clear cells for colony trade silver.");
        foreach (var cell in cells)
        {
            var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = ThingDefOf.Silver.stackLimit;
            GenSpawn.Spawn(silver, cell, map);
        }
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
            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking) &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing))
            {
                HumanlikePawnFixture.SetName(pawn, "Orbital Meal Buyer");
                pawn.skills.GetSkill(SkillDefOf.Social).Level = 20;
                return pawn;
            }

            pawn.Destroy(DestroyMode.Vanish);
        }

        throw new EndToEndAssertionException("Could not generate a capable orbital trade negotiator.");
    }

    private static ThingWithComps GenerateTraderStockMeal(ThingDef mealDef)
    {
        var generator = new StockGenerator_SingleDef { countRange = new IntRange(1, 1) };
        var thingDefField = typeof(StockGenerator_SingleDef).GetField(
            "thingDef",
            BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new EndToEndAssertionException(
                "RimWorld 1.6 StockGenerator_SingleDef must retain its XML thingDef field.");
        thingDefField.SetValue(generator, mealDef);
        var trader = new TraderKindDef
        {
            defName = "ImmersiveChefs_E2EOrbitalMealTrader",
            stockGenerators = new List<StockGenerator> { generator }
        };
        generator.ResolveReferences(trader);
        var generated = ThingSetMakerDefOf.TraderStock.root.Generate(
            new ThingSetMakerParams { traderDef = trader });
        var result = generated.OfType<ThingWithComps>().Single(thing => thing.def == mealDef);
        foreach (var extra in generated.Where(thing => !ReferenceEquals(thing, result)))
        {
            if (!extra.Destroyed)
            {
                extra.Destroy(DestroyMode.Vanish);
            }
        }

        return result;
    }

    private static IntVec3 FindFixtureCenter(Map map)
    {
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 55f, useCenter: true))
        {
            if (CellRect.CenteredOn(candidate, 10).Cells.All(cell =>
                    cell.InBounds(map) &&
                    cell.Standable(map) &&
                    !map.roofGrid.Roofed(cell) &&
                    cell.GetThingList(map).Count == 0))
            {
                return candidate;
            }
        }

        throw new EndToEndAssertionException("Could not find a clear orbital meal trade fixture area.");
    }

}
