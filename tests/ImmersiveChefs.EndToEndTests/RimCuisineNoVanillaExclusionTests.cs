using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.rimcuisine-no-vanilla-exclusions",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "syrchalis.processor.framework",
    "Mlie.RC2.Core",
    "Mlie.RC2.MaME",
    "Mlie.RC2.BaBE",
    "Mlie.RC2.SaSE",
    "Mlie.NoVanillaMeals",
    "fumblesneeze.immersivechefs",
    MaxFrames = 2_400,
    MaxGameTicks = 4_000,
    MaxWallClockSeconds = 90)]
public sealed class RimCuisineNoVanillaExclusionTest : IRimWorldEndToEndTest
{
    private static readonly (string DefName, string Category)[] ExcludedProducts =
    {
        ("RC2_CannedMeal", "canned meal"),
        ("RC2_Hardtack", "hand-eaten travel food"),
        ("RC2_DriedMeat", "preserved ingredient"),
        ("RC2_Wine", "drink"),
        ("RC2_Cigarette", "drug"),
        ("RC2_RawBarley", "raw ingredient"),
        ("RC2_MealCupcake", "snack")
    };

    private readonly List<ExcludedFixture> fixtures = new();
    private Map map = null!;
    private TradeShip ship = null!;
    private CigaretteProductionFixture cigaretteProduction = null!;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FindFixtureCenter(map);
        var negotiator = CreateNegotiator();
        GenSpawn.Spawn(negotiator, center + new IntVec3(0, 0, -3), map);

        var beacon = CreatePoweredTradeFixture(center);
        PlaceTradeSilver(beacon);
        cigaretteProduction = CreateCigaretteProductionFixture(center);
        var generatedStock = GenerateTraderStock(ExcludedProducts
            .Where(product => product.DefName != "RC2_Cigarette")
            .Select(product => DefDatabase<ThingDef>.GetNamed(product.DefName))
            .ToArray());
        foreach (var product in ExcludedProducts.Where(product => product.DefName != "RC2_Cigarette"))
        {
            fixtures.Add(new ExcludedFixture(
                product.Category,
                generatedStock.Things.Single(thing => thing.def.defName == product.DefName)));
        }

        ship = new TradeShip(generatedStock.Trader, null);
        ship.GetDirectlyHeldThings().ClearAndDestroyContents();
        foreach (var fixture in fixtures)
        {
            EndToEndAssert.True(ship.GetDirectlyHeldThings().TryAdd(fixture.Thing),
                "The upstream-generated " + fixture.Thing.def.defName +
                " must enter the orbital trader's native stock container.");
        }

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
            "RimWorld must open its native trade dialog for the retained RimCuisine products.");
        Find.TickManager.Pause();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CheckpointStep(
            "upstream tradeable RimCuisine exclusion stock identity",
            _ => fixtures.ToDictionary(
                fixture => fixture.Thing.def.defName,
                fixture => fixture.Thing.ThingID,
                StringComparer.Ordinal));
        yield return new ScreenshotStep(
            "observe tradeable retained RimCuisine products in native orbital stock",
            Array.Empty<string>(),
            paddingPixels: 0);
        foreach (var fixture in fixtures)
        {
            yield return TradeDialogActionStep.AdjustTransfer(
                "buy upstream-generated " + fixture.Category + " through the native transfer action",
                fixture.Thing.ThingID,
                countDelta: 1);
        }

        yield return new WaitUntilStep(
            "native trade dialog records all tradeable retained RimCuisine purchases",
            _ => fixtures.All(fixture => TradeSession.deal.AllTradeables.Any(tradeable =>
                tradeable.thingsTrader.Contains(fixture.Thing) &&
                tradeable.CountToTransfer == 1)),
            new EndToEndDeadline(300, 600, TimeSpan.FromSeconds(15)));
        yield return new ScreenshotStep(
            "observe tradeable retained RimCuisine products selected for purchase",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return TradeDialogActionStep.Accept(
            "accept the native retained RimCuisine product trade");
        yield return new WaitUntilStep(
            "native retained-product trade closes after acceptance",
            _ => !Find.WindowStack.Windows.Any(window => window is Dialog_Trade),
            new EndToEndDeadline(240, 500, TimeSpan.FromSeconds(15)));
        yield return new TimeControlActionStep(
            "run the native retained-product delivery",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the RimCuisine drug-lab worker enters the native cigarette bill",
            _ => cigaretteProduction.ObserveNativeBill(),
            new EndToEndDeadline(1_200, 3_000, TimeSpan.FromSeconds(30)));
        var cigaretteTargets = new[]
        {
            cigaretteProduction.Worker.ThingID,
            cigaretteProduction.Lab.ThingID
        };
        yield return new SelectionActionStep(
            "select the native RimCuisine cigarette production fixture",
            cigaretteTargets,
            additive: false);
        yield return new CameraActionStep(
            "frame the native RimCuisine cigarette production fixture",
            cigaretteTargets,
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "observe native RimCuisine cigarette production",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new WaitUntilStep(
            "all retained RimCuisine products arrive through their native workflows",
            _ => fixtures.All(fixture => fixture.Thing.Spawned && fixture.Thing.Map == map) &&
                 cigaretteProduction.TryResolveCompletedProduct(),
            new EndToEndDeadline(1_800, 5_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause after all retained RimCuisine products arrive",
            paused: true,
            EndToEndGameSpeed.Normal);
        fixtures.Add(cigaretteProduction.AsExcludedFixture());

        var handles = fixtures.Select(fixture => fixture.Thing.ThingID).ToArray();
        yield return new SelectionActionStep(
            "select the retained RimCuisine exclusion catalog",
            handles,
            additive: false);
        yield return new CameraActionStep(
            "frame the retained RimCuisine exclusion catalog",
            handles,
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "observe all retained RimCuisine exclusion categories",
            handles,
            paddingPixels: 220);

        foreach (var fixture in fixtures)
        {
            yield return new SelectionActionStep(
                "select excluded RimCuisine " + fixture.Category,
                new[] { fixture.Thing.ThingID },
                additive: false);
            yield return new AssertionStep(
                "RimCuisine " + fixture.Category + " remains outside Immersive Chefs",
                _ => fixture.AssertExcluded());
            yield return new ScreenshotStep(
                "inspect excluded RimCuisine " + fixture.Category,
                Array.Empty<string>(),
                paddingPixels: 0);
        }

        yield return new AssertionStep(
            "No Vanilla Meals leaves no classified vanilla output or stale RimCuisine bulk path",
            _ => AssertRemovedVanillaProductsAndBulkRecipes());
        yield return new CheckpointStep(
            "RimCuisine exclusion and removed-recipe result",
            _ => BuildCheckpoint());
    }

    private static void AssertRemovedVanillaProductsAndBulkRecipes()
    {
        foreach (var defName in new[] { "MealSimple", "MealFine", "MealLavish" })
        {
            EndToEndAssert.True(
                DefDatabase<ThingDef>.GetNamedSilentFail(defName) is null,
                "No Vanilla Meals must remove finalized official meal " + defName + ".");
        }

        foreach (var defName in new[]
                 {
                     "CookSimpleMealBulk",
                     "RC2_CookFineMealBulk",
                     "RC2_CookLavishMealBulk"
                 })
        {
            var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(defName);
            if (recipe is null)
            {
                continue;
            }

            EndToEndAssert.False(
                MealCoveragePolicy.IsCovered(recipe),
                "Removed-product bulk recipe " + defName + " must remain outside Immersive Chefs.");
            EndToEndAssert.Equal(
                1f,
                RecipeWorkRuntime.MultiplierFor(recipe),
                "Removed-product bulk recipe " + defName + " must retain its native work amount.");
        }
    }

    private Dictionary<string, string> BuildCheckpoint()
    {
        var checkpoint = fixtures.ToDictionary(
            fixture => fixture.Thing.def.defName,
            fixture =>
                "category=" + fixture.Category +
                "; covered=" + MealCoveragePolicy.IsCovered(fixture.Thing.def) +
                "; embeddedWare=" +
                ((fixture.Thing as ThingWithComps)?.GetComp<CompEmbeddedWare>() is not null) +
                "; culinaryState=" +
                ((fixture.Thing as ThingWithComps)?.GetComp<CompCulinaryState>() is not null),
            StringComparer.Ordinal);
        checkpoint["removedVanillaMeals"] = string.Join(",",
            new[] { "MealSimple", "MealFine", "MealLavish" }
                .Where(defName => DefDatabase<ThingDef>.GetNamedSilentFail(defName) is null));
        checkpoint["inactiveBulkRecipes"] = string.Join(",",
            new[] { "CookSimpleMealBulk", "RC2_CookFineMealBulk", "RC2_CookLavishMealBulk" }
                .Where(defName =>
                {
                    var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(defName);
                    return recipe is null || RecipeWorkRuntime.MultiplierFor(recipe) == 1f;
                }));
        checkpoint["RC2_CigaretteNativeBillObserved"] =
            cigaretteProduction.NativeBillObserved.ToString();
        return checkpoint;
    }

    private CigaretteProductionFixture CreateCigaretteProductionFixture(IntVec3 center)
    {
        var crafting = DefDatabase<WorkTypeDef>.GetNamed("Crafting");
        Pawn? worker = null;
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var candidate = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                forceNoGear: true));
            if (candidate.WorkTypeIsDisabled(crafting) ||
                !candidate.health.capacities.CapableOf(PawnCapacityDefOf.Moving) ||
                !candidate.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                candidate.Destroy(DestroyMode.Vanish);
                continue;
            }

            worker = candidate;
            break;
        }

        if (worker is null)
        {
            throw new EndToEndAssertionException(
                "Could not generate a capable native RimCuisine cigarette worker.");
        }

        HumanlikePawnFixture.SetName(worker, "RimCuisine Cigarette Maker");
        worker.inventory?.innerContainer.ClearAndDestroyContents();
        worker.workSettings.EnableAndInitialize();
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            if (!worker.WorkTypeIsDisabled(workType))
            {
                worker.workSettings.SetPriority(workType, 0);
            }
        }

        worker.workSettings.SetPriority(crafting, 1);
        worker.skills.GetSkill(SkillDefOf.Intellectual).Level = 20;
        for (var hour = 0; hour < 24; hour++)
        {
            worker.timetable?.SetAssignment(hour, TimeAssignmentDefOf.Work);
        }

        if (worker.needs?.food is { } food)
        {
            food.CurLevelPercentage = 1f;
        }

        if (worker.needs?.rest is { } rest)
        {
            rest.CurLevelPercentage = 1f;
        }

        var labDef = DefDatabase<ThingDef>.GetNamed("DrugLab");
        var lab = ThingMaker.MakeThing(
            labDef,
            labDef.MadeFromStuff ? ThingDefOf.Steel : null);
        lab.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(lab, center + new IntVec3(-4, 0, 3), map, Rot4.North);
        GenSpawn.Spawn(worker, center + new IntVec3(-4, 0, 0), map);

        var recipe = DefDatabase<RecipeDef>.GetNamed("RC2_MakeCigarettes");
        var productDef = DefDatabase<ThingDef>.GetNamed("RC2_Cigarette");
        var expectedCount = recipe.products
            .Where(product => product.thingDef == productDef)
            .Sum(product => product.count);
        EndToEndAssert.Equal(20, expectedCount,
            "The retained RimCuisine cigarette recipe must keep its native twenty-item output.");
        var bill = new Bill_Production(recipe)
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 9f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(worker);
        ((IBillGiver)lab).BillStack.AddBill(bill);

        var tobacco = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RC2_RawTobacco"));
        tobacco.stackCount = 4;
        GenSpawn.Spawn(tobacco, center + new IntVec3(-6, 0, 1), map);
        return new CigaretteProductionFixture(center, worker, lab, recipe, productDef, expectedCount);
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
            "The native orbital beacon must be powered before opening the exclusion trade.");
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
                HumanlikePawnFixture.SetName(pawn, "RimCuisine Catalog Buyer");
                pawn.skills.GetSkill(SkillDefOf.Social).Level = 20;
                return pawn;
            }

            pawn.Destroy(DestroyMode.Vanish);
        }

        throw new EndToEndAssertionException("Could not generate a capable RimCuisine catalog negotiator.");
    }

    private static GeneratedTraderStock GenerateTraderStock(IReadOnlyList<ThingDef> productDefs)
    {
        var thingDefField = typeof(StockGenerator_SingleDef).GetField(
            "thingDef",
            BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new EndToEndAssertionException(
                "RimWorld 1.6 StockGenerator_SingleDef must retain its XML thingDef field.");
        var selected = new List<Thing>();
        foreach (var productDef in productDefs)
        {
            var generator = CreateStockGenerator(productDef, thingDefField);
            var trader = new TraderKindDef
            {
                defName = "ImmersiveChefs_E2ERimCuisineCatalogTrader_" + productDef.defName,
                stockGenerators = new List<StockGenerator> { generator }
            };
            generator.ResolveReferences(trader);
            var generated = ThingSetMakerDefOf.TraderStock.root.Generate(
                new ThingSetMakerParams { traderDef = trader });
            var product = generated.SingleOrDefault(thing => thing.def == productDef);
            EndToEndAssert.NotNull(product,
                "The upstream stock generator must create " + productDef.defName +
                "; generated: " + string.Join(",", generated.Select(thing => thing.def.defName)));
            selected.Add(product!);
            foreach (var extra in generated.Where(thing => !ReferenceEquals(thing, product)))
            {
                if (!extra.Destroyed)
                {
                    extra.Destroy(DestroyMode.Vanish);
                }
            }
        }

        var saleGenerators = productDefs
            .Select(productDef => (StockGenerator)CreateStockGenerator(productDef, thingDefField))
            .ToList();
        var saleTrader = new TraderKindDef
        {
            defName = "ImmersiveChefs_E2ERimCuisineCatalogOrbitalTrader",
            label = "RimCuisine catalog trader",
            orbital = true,
            stockGenerators = saleGenerators
        };
        foreach (var generator in saleGenerators)
        {
            generator.ResolveReferences(saleTrader);
        }

        return new GeneratedTraderStock(saleTrader, selected);
    }

    private static StockGenerator_SingleDef CreateStockGenerator(
        ThingDef productDef,
        FieldInfo thingDefField)
    {
        var generator = new StockGenerator_SingleDef { countRange = new IntRange(1, 1) };
        thingDefField.SetValue(generator, productDef);
        return generator;
    }

    private static IntVec3 FindFixtureCenter(Map map)
    {
        for (var x = -72; x <= 72; x += 18)
        {
            for (var z = -72; z <= 72; z += 18)
            {
                var candidate = map.Center + new IntVec3(x, 0, z);
                if (LineIsClear(map, candidate))
                {
                    return candidate;
                }
            }
        }

        throw new EndToEndAssertionException("Could not find a clear RimCuisine exclusion display area.");
    }

    private static bool LineIsClear(Map map, IntVec3 center)
    {
        for (var x = -8; x <= 8; x++)
        {
            for (var z = -3; z <= 3; z++)
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

    private sealed class ExcludedFixture
    {
        public ExcludedFixture(string category, Thing thing)
        {
            Category = category;
            Thing = thing;
        }

        public string Category { get; }
        public Thing Thing { get; }

        public void AssertExcluded()
        {
            EndToEndAssert.False(MealCoveragePolicy.IsCovered(Thing.def),
                Thing.def.defName + " must remain outside covered meal production and dining.");
            var withComps = Thing as ThingWithComps;
            EndToEndAssert.True(withComps?.GetComp<CompEmbeddedWare>() is null,
                Thing.def.defName + " must not receive an embedded plate component.");
            EndToEndAssert.True(withComps?.GetComp<CompCulinaryState>() is null,
                Thing.def.defName + " must not receive culinary or temperature state.");
        }
    }

    private sealed class GeneratedTraderStock
    {
        public GeneratedTraderStock(TraderKindDef trader, IReadOnlyList<Thing> things)
        {
            Trader = trader;
            Things = things;
        }

        public TraderKindDef Trader { get; }
        public IReadOnlyList<Thing> Things { get; }
    }

    private sealed class CigaretteProductionFixture
    {
        private readonly IntVec3 center;
        private readonly RecipeDef recipe;
        private readonly ThingDef productDef;
        private readonly int expectedCount;
        private Thing? product;

        public CigaretteProductionFixture(
            IntVec3 center,
            Pawn worker,
            Thing lab,
            RecipeDef recipe,
            ThingDef productDef,
            int expectedCount)
        {
            this.center = center;
            Worker = worker;
            Lab = lab;
            this.recipe = recipe;
            this.productDef = productDef;
            this.expectedCount = expectedCount;
        }

        public Pawn Worker { get; }
        public Thing Lab { get; }
        public bool NativeBillObserved { get; private set; }

        public bool ObserveNativeBill()
        {
            NativeBillObserved |= Worker.CurJobDef == JobDefOf.DoBill &&
                                  Worker.CurJob?.RecipeDef == recipe;
            return NativeBillObserved;
        }

        public bool TryResolveCompletedProduct()
        {
            ObserveNativeBill();
            product ??= Worker.MapHeld?.listerThings.ThingsOfDef(productDef)
                .FirstOrDefault(candidate =>
                    candidate.Spawned &&
                    candidate.Position.DistanceToSquared(center) <= 100 &&
                    candidate.stackCount == expectedCount);
            return NativeBillObserved && product is not null;
        }

        public ExcludedFixture AsExcludedFixture()
        {
            EndToEndAssert.True(NativeBillObserved,
                "RC2_Cigarette must be produced by RimWorld's ordinary DoBill job.");
            EndToEndAssert.NotNull(product,
                "The native RC2_MakeCigarettes bill must produce RC2_Cigarette.");
            return new ExcludedFixture("drug", product!);
        }
    }
}
