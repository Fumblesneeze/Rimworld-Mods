using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.rimcuisine-no-vanilla-pizza-trade",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "syrchalis.processor.framework",
    "Mlie.RC2.Core",
    "Mlie.RC2.MaME",
    "Mlie.RC2.BaBE",
    "Mlie.RC2.SaSE",
    "Mlie.NoVanillaMeals",
    "fumblesneeze.immersivechefs",
    MaxFrames = 3_600,
    MaxGameTicks = 12_000,
    MaxWallClockSeconds = 120)]
public sealed class RimCuisineNoVanillaPizzaTradeTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn negotiator = null!;
    private TradeShip ship = null!;
    private ThingWithComps pizza = null!;
    private Thing plate = null!;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FindFixtureCenter(map);
        negotiator = CreateNegotiator();
        GenSpawn.Spawn(negotiator, center + new IntVec3(0, 0, -3), map);

        var beacon = CreatePoweredTradeFixture(center);
        PlaceTradeSilver(beacon);
        var pizzaDef = DefDatabase<ThingDef>.GetNamed("RC2_Pizza");
        pizza = GenerateTraderStockMeal(pizzaDef);

        var embedded = pizza.GetComp<CompEmbeddedWare>();
        EndToEndAssert.NotNull(embedded,
            "An upstream-generated RimCuisine pizza must use the finalized embedded-ware component.");
        EndToEndAssert.Equal(1, embedded!.EmbeddedPlateCount,
            "One upstream-generated pizza must contain exactly one physical plate.");
        plate = embedded.PeekPlateThing() ??
            throw new EndToEndAssertionException(
                "The upstream-generated pizza did not retain a physical plate Thing.");
        EndToEndAssert.Equal(ThingDefOf.Silver.defName, plate.Stuff?.defName,
            "An Elaborate pizza must receive the least-cost supported luxury plate material.");
        EndToEndAssert.True(plate.TryGetComp<CompQuality>() is null,
            "Externally generated trader meals receive ungraded service ware.");

        ship = new TradeShip(DefDatabase<TraderKindDef>.GetNamed("Orbital_Exotic"), null);
        ship.GetDirectlyHeldThings().ClearAndDestroyContents();
        EndToEndAssert.True(ship.GetDirectlyHeldThings().TryAdd(pizza),
            "The exact generated pizza must enter the orbital trader's native stock container.");
        context.DeferCleanup(() =>
        {
            if (map.passingShipManager.passingShips.Contains(ship))
            {
                map.passingShipManager.RemoveShip(ship);
            }
        });
        map.passingShipManager.AddShip(ship);

        var beaconTradeConcept = DefDatabase<ConceptDef>.GetNamed("TradeGoodsMustBeNearBeacon");
        var priorBeaconTradeKnowledge = PlayerKnowledgeDatabase.GetKnowledge(beaconTradeConcept);
        context.DeferCleanup(() =>
            PlayerKnowledgeDatabase.SetKnowledge(beaconTradeConcept, priorBeaconTradeKnowledge));
        PlayerKnowledgeDatabase.KnowledgeDemonstrated(beaconTradeConcept, KnowledgeAmount.Total);
        ship.TryOpenComms(negotiator);
        EndToEndAssert.True(Find.WindowStack.Windows.Any(window => window is Dialog_Trade),
            "RimWorld must open its native trade dialog for the RimCuisine pizza trader.");
        Find.TickManager.Pause();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CheckpointStep(
            "upstream RimCuisine pizza stock identity",
            _ => new Dictionary<string, string>
            {
                ["pizzaThingId"] = pizza.ThingID,
                ["plateThingId"] = plate.ThingID,
                ["plateStuff"] = plate.Stuff?.defName ?? "none",
                ["plateQuality"] =
                    plate.TryGetComp<CompQuality>()?.Quality.ToString() ?? "ungraded"
            });
        yield return new ScreenshotStep(
            "observe upstream RimCuisine pizza in native orbital stock",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return TradeDialogActionStep.AdjustTransfer(
            "buy the upstream-generated pizza through the native transfer action",
            pizza.ThingID,
            countDelta: 1);
        yield return new WaitUntilStep(
            "native trade dialog records one pizza purchase",
            _ => TradeSession.deal.AllTradeables.Any(tradeable =>
                tradeable.thingsTrader.Contains(pizza) &&
                tradeable.CountToTransfer == 1),
            new EndToEndDeadline(180, 300, TimeSpan.FromSeconds(10)));
        yield return new ScreenshotStep(
            "observe the exact RimCuisine pizza selected for purchase",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return TradeDialogActionStep.Accept("accept the native RimCuisine pizza trade");
        yield return new WaitUntilStep(
            "native pizza trade closes after acceptance",
            _ => !Find.WindowStack.Windows.Any(window => window is Dialog_Trade),
            new EndToEndDeadline(240, 500, TimeSpan.FromSeconds(15)));
        yield return new TimeControlActionStep(
            "run the native pizza delivery",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the purchased RimCuisine pizza lands on the colony map",
            _ => pizza.Spawned && pizza.Map == map,
            new EndToEndDeadline(1_800, 5_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause after the exact pizza lands",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the purchased RimCuisine pizza",
            new[] { pizza.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the purchased RimCuisine pizza",
            new[] { pizza.ThingID },
            paddingPixels: 260);
        yield return new AssertionStep(
            "native trade preserves the pizza's exact luxury plate and culinary state once",
            _ =>
            {
                var embedded = pizza.GetComp<CompEmbeddedWare>();
                EndToEndAssert.True(pizza.Spawned && pizza.Map == map,
                    "The exact trader-owned pizza must become a colony-map Thing after native acceptance.");
                EndToEndAssert.NotNull(embedded,
                    "The purchased pizza must retain its finalized embedded-ware component.");
                EndToEndAssert.Equal(1, embedded!.EmbeddedPlateCount,
                    "The native transfer must retain exactly one embedded pizza plate.");
                EndToEndAssert.True(ReferenceEquals(plate, embedded.PeekPlateThing()),
                    "The purchased pizza must retain the original physical plate rather than generate a replacement.");
                EndToEndAssert.True(!plate.Spawned && plate.holdingOwner is not null,
                    "The exact pizza plate must remain embedded rather than duplicate onto the map.");
                EndToEndAssert.Equal(1, pizza.GetComp<CompCulinaryState>()?.Servings.Count ?? -1,
                    "The upstream-generated pizza must retain one culinary serving record.");
            });
        yield return new ScreenshotStep(
            "observe purchased RimCuisine pizza with one bound plate",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "RimCuisine pizza trade result",
            _ => new Dictionary<string, string>
            {
                ["pizzaThingId"] = pizza.ThingID,
                ["plateThingId"] = plate.ThingID,
                ["pizzaSpawned"] = pizza.Spawned.ToString(),
                ["embeddedPlateCount"] =
                    pizza.GetComp<CompEmbeddedWare>()!.EmbeddedPlateCount.ToString(),
                ["samePlate"] =
                    ReferenceEquals(plate, pizza.GetComp<CompEmbeddedWare>()!.PeekPlateThing()).ToString(),
                ["culinaryServingCount"] =
                    (pizza.GetComp<CompCulinaryState>()?.Servings.Count ?? -1).ToString()
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
        var flick = beacon.GetComp<CompFlickable>();
        if (flick is { SwitchIsOn: false })
        {
            flick.DoFlick();
        }

        map.powerNetManager.UpdatePowerNetsAndConnections_First();
        for (var tick = 0; tick <= 200 &&
             !beacon.TryGetComp<CompPowerTrader>().PowerOn; tick++)
        {
            Find.TickManager.DoSingleTick();
        }

        EndToEndAssert.True(beacon.TryGetComp<CompPowerTrader>().PowerOn,
            "The native orbital beacon must be powered before opening the pizza trade.");
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
            if (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Talking) >= 0.95f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing) >= 0.95f)
            {
                HumanlikePawnFixture.SetName(pawn, "RimCuisine Pizza Buyer");
                pawn.skills.GetSkill(SkillDefOf.Social).Level = 20;
                return pawn;
            }

            pawn.Destroy(DestroyMode.Vanish);
        }

        throw new EndToEndAssertionException("Could not generate a capable RimCuisine pizza negotiator.");
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
            defName = "ImmersiveChefs_E2ERimCuisinePizzaTrader",
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
        for (var x = -72; x <= 72; x += 18)
        {
            for (var z = -72; z <= 72; z += 18)
            {
                var candidate = map.Center + new IntVec3(x, 0, z);
                if (SquareIsClear(map, candidate, 10))
                {
                    return candidate;
                }
            }
        }

        throw new EndToEndAssertionException("Could not find a clear RimCuisine pizza trade fixture area.");
    }

    private static bool SquareIsClear(Map map, IntVec3 center, int radius)
    {
        for (var x = -radius; x <= radius; x++)
        {
            for (var z = -radius; z <= radius; z++)
            {
                var cell = center + new IntVec3(x, 0, z);
                if (!cell.InBounds(map) ||
                    !cell.Standable(map) ||
                    map.roofGrid.Roofed(cell) ||
                    cell.GetThingList(map).Count != 0)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
