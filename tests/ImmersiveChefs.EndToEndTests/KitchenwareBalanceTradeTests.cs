using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.kitchenware-balance-and-trade",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.immersivechefs",
    MaxFrames = 6_600,
    MaxGameTicks = 22_000,
    MaxWallClockSeconds = 210)]
public sealed class KitchenwareBalanceTradeTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn crafter = null!;
    private Building_WorkTable craftingSpot = null!;
    private RecipeDef primitiveRecipe = null!;
    private Thing granite = null!;
    private Thing wood = null!;
    private ThingWithComps? primitiveCookware;
    private TradeShip ship = null!;
    private Thing tradedCookware = null!;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FindFixtureCenter(map);
        ArrangePrimitiveCrafting(center);
        ArrangeOrbitalTrade(context, center + new IntVec3(14, 0, 0));
        Find.TickManager.Pause();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CheckpointStep(
            "loaded kitchenware recipe and stock identities",
            _ => new Dictionary<string, string>
            {
                ["recipe"] = primitiveRecipe.defName,
                ["recipeLabel"] = primitiveRecipe.LabelCap,
                ["stonyRequirement"] = primitiveRecipe.IngredientValueGetter!
                    .BillRequirementsDescription(primitiveRecipe, primitiveRecipe.ingredients[0]),
                ["stoneUnits"] = primitiveRecipe.ingredients[0].GetBaseCount().ToString(),
                ["woodUnits"] = primitiveRecipe.ingredients[1].GetBaseCount().ToString(),
                ["tradedThing"] = tradedCookware.ThingID + ":" + tradedCookware.LabelCap
            });
        yield return new ScreenshotStep(
            "ordinary orbital trader offers generated cookware",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return TradeDialogActionStep.AdjustTransfer(
            "buy one generated cookware set through the native transfer control",
            tradedCookware.ThingID,
            countDelta: 1);
        yield return new ScreenshotStep(
            "native trade dialog records the cookware-set purchase",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return TradeDialogActionStep.Accept("accept the native cookware-set trade");
        yield return new WaitUntilStep(
            "native cookware trade closes",
            _ => !Find.WindowStack.Windows.Any(window => window is Dialog_Trade),
            new EndToEndDeadline(240, 500, TimeSpan.FromSeconds(15)));
        yield return new TimeControlActionStep(
            "run the orbital cookware delivery",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the purchased cookware set lands on the colony map",
            _ => tradedCookware.Spawned && tradedCookware.Map == map,
            new EndToEndDeadline(1_800, 5_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause after the purchased cookware lands",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the purchased cookware set",
            new[] { tradedCookware.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the purchased cookware set",
            new[] { tradedCookware.ThingID },
            paddingPixels: 260);
        yield return new ScreenshotStep(
            "purchased cookware set after native delivery",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "native trade transfers one sellable generated cookware set",
            _ =>
            {
                EndToEndAssert.True(
                    tradedCookware.Spawned && tradedCookware.Map == map,
                    "The exact trader-owned cookware must become a colony-map Thing after acceptance.");
                EndToEndAssert.Equal(
                    Tradeability.All,
                    tradedCookware.def.tradeability,
                    "Portable cookware must remain buyable and sellable.");
                EndToEndAssert.Equal(
                    1,
                    tradedCookware.stackCount,
                    "The low-stock purchase must transfer exactly one cookware set.");
            });

        var craftingTargets = new[]
        {
            crafter.ThingID,
            craftingSpot.ThingID,
            granite.ThingID,
            wood.ThingID
        };
        yield return new SelectionActionStep(
            "select the primitive cookware crafting fixture",
            craftingTargets,
            additive: false);
        yield return new CameraActionStep(
            "frame the primitive cookware crafting fixture",
            craftingTargets,
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "before the native five-stone one-wood bill",
            craftingTargets,
            paddingPixels: 220);

        var craftingOptions = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(crafter.ThingID, craftingSpot.ThingID);
        var prioritize = craftingOptions.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("prioritize", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(
            1,
            prioritize.Length,
            "Expected one enabled native Prioritize option for the primitive cookware bill; observed " +
            string.Join(", ", craftingOptions.Select(option =>
                $"'{option.Label}' (disabled={option.Disabled})")));
        yield return new FloatMenuActionStep(
            "prioritize the primitive cookware bill through the native float menu",
            crafter.ThingID,
            craftingSpot.ThingID,
            prioritize[0].StableId);
        yield return new TimeControlActionStep(
            "run the player-ordered Crafting work",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the crafter begins the native primitive cookware bill",
            _ => crafter.CurJobDef == JobDefOf.DoBill &&
                 crafter.CurJob?.RecipeDef == primitiveRecipe,
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(40)));
        yield return new ScreenshotStep(
            "primitive cookware bill in progress",
            new[] { crafter.ThingID, craftingSpot.ThingID },
            paddingPixels: 220);
        yield return new WaitUntilStep(
            "the native bill produces primitive stone cookware",
            _ => TryResolvePrimitiveCookware(),
            new EndToEndDeadline(2_100, 7_000, TimeSpan.FromSeconds(65)));
        yield return new TimeControlActionStep(
            "pause after primitive cookware crafting",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the bill consumes one wall-equivalent plus one handle unit",
            _ =>
            {
                EndToEndAssert.NotNull(
                    primitiveCookware,
                    "The ordinary bill must produce one primitive cookware set.");
                EndToEndAssert.True(
                    granite.Destroyed && wood.Destroyed,
                    "The exact five-stone and one-wood input stacks must be consumed.");
                EndToEndAssert.Equal(
                    "BlocksGranite",
                    primitiveCookware!.Stuff?.defName,
                    "The product must retain the selected finalized stony Stuff.");
                EndToEndAssert.Equal(
                    "primitive stone cookware set",
                    primitiveCookware.def.label,
                    "The resulting item must name its complete cookware abstraction.");
                EndToEndAssert.Equal(
                    1,
                    primitiveCookware.stackCount,
                    "One native bill must create one complete set.");
                EndToEndAssert.NotNull(
                    primitiveCookware.GetComp<CompQuality>(),
                    "The completed crafting operation must assign a vanilla crafting quality.");
            });
        yield return new SelectionActionStep(
            "select the crafted primitive cookware set",
            new[] { primitiveCookware!.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the crafted primitive cookware set",
            new[] { primitiveCookware.ThingID, craftingSpot.ThingID, crafter.ThingID },
            paddingPixels: 260);
        yield return new ScreenshotStep(
            "crafted primitive cookware set and inspector",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "native kitchenware balance and trade result",
            _ => new Dictionary<string, string>
            {
                ["purchased"] = tradedCookware.ThingID + ":" + tradedCookware.LabelCap,
                ["crafted"] = primitiveCookware.ThingID + ":" + primitiveCookware.LabelCap,
                ["craftedStuff"] = primitiveCookware.Stuff?.defName ?? "missing",
                ["craftedQuality"] = primitiveCookware.GetComp<CompQuality>()?.Quality.ToString() ?? "missing",
                ["stoneConsumed"] = granite.Destroyed.ToString(),
                ["woodConsumed"] = wood.Destroyed.ToString()
            });
    }

    private void ArrangePrimitiveCrafting(IntVec3 center)
    {
        crafter = GenerateCrafter();
        GenSpawn.Spawn(crafter, center + (IntVec3.South * 3), map);

        var spotDef = DefDatabase<ThingDef>.GetNamed("CraftingSpot");
        craftingSpot = (Building_WorkTable)ThingMaker.MakeThing(spotDef);
        craftingSpot.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(craftingSpot, center, map, Rot4.North);

        primitiveRecipe = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakePrimitiveCookware");
        var bill = new Bill_Production(primitiveRecipe)
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 8f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(crafter);
        craftingSpot.BillStack.AddBill(bill);

        granite = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("BlocksGranite"));
        granite.stackCount = 5;
        GenSpawn.Spawn(granite, center + (IntVec3.West * 2), map);
        wood = ThingMaker.MakeThing(ThingDefOf.WoodLog);
        wood.stackCount = 1;
        GenSpawn.Spawn(wood, center + (IntVec3.East * 2), map);
    }

    private void ArrangeOrbitalTrade(IEndToEndContext context, IntVec3 center)
    {
        var negotiator = CreateNegotiator();
        GenSpawn.Spawn(negotiator, center + new IntVec3(0, 0, -3), map);
        var beacon = CreatePoweredTradeFixture(center);
        PlaceTradeSilver(beacon);

        var bulkTrader = DefDatabase<TraderKindDef>.GetNamed("Orbital_BulkGoods");
        tradedCookware = GenerateNativeKitchenwareStock(bulkTrader);
        ship = new TradeShip(bulkTrader, null);
        ship.GetDirectlyHeldThings().ClearAndDestroyContents();
        EndToEndAssert.True(
            ship.GetDirectlyHeldThings().TryAdd(tradedCookware),
            "The exact native-generated cookware must enter orbital stock.");
        context.DeferCleanup(() =>
        {
            if (map.passingShipManager.passingShips.Contains(ship))
            {
                map.passingShipManager.RemoveShip(ship);
            }
        });
        map.passingShipManager.AddShip(ship);

        var beaconConcept = DefDatabase<ConceptDef>.GetNamed("TradeGoodsMustBeNearBeacon");
        var priorKnowledge = PlayerKnowledgeDatabase.GetKnowledge(beaconConcept);
        context.DeferCleanup(() => PlayerKnowledgeDatabase.SetKnowledge(beaconConcept, priorKnowledge));
        PlayerKnowledgeDatabase.KnowledgeDemonstrated(beaconConcept, KnowledgeAmount.Total);
        ship.TryOpenComms(negotiator);
        EndToEndAssert.True(
            Find.WindowStack.Windows.Any(window => window is Dialog_Trade),
            "RimWorld must open the native orbital trade dialog for kitchenware stock.");
    }

    private Thing GenerateNativeKitchenwareStock(TraderKindDef trader)
    {
        var cookwareDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cookware");
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var generated = ThingSetMakerDefOf.TraderStock.root.Generate(
                new ThingSetMakerParams { traderDef = trader });
            var result = generated.FirstOrDefault(thing => thing.def == cookwareDef);
            foreach (var extra in generated.Where(thing => !ReferenceEquals(thing, result)))
            {
                if (!extra.Destroyed)
                {
                    extra.Destroy(DestroyMode.Vanish);
                }
            }

            if (result is not null)
            {
                result.stackCount = 1;
                return result;
            }
        }

        throw new EndToEndAssertionException(
            "The finalized low-stock Orbital_BulkGoods generator produced no cookware in 64 bounded rolls.");
    }

    private bool TryResolvePrimitiveCookware()
    {
        primitiveCookware ??= map.listerThings
            .ThingsOfDef(DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_PrimitiveCookware"))
            .OfType<ThingWithComps>()
            .FirstOrDefault(thing => thing.Spawned && thing.Stuff?.defName == "BlocksGranite");
        return primitiveCookware is not null;
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

        var powerCell = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("VanometricPowerCell"));
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
        for (var tick = 0; tick <= 200 && !beacon.GetComp<CompPowerTrader>().PowerOn; tick++)
        {
            Find.TickManager.DoSingleTick();
        }

        EndToEndAssert.True(
            beacon.GetComp<CompPowerTrader>().PowerOn,
            "The native orbital beacon must be powered before trade opens.");
        return beacon;
    }

    private void PlaceTradeSilver(Building_OrbitalTradeBeacon beacon)
    {
        var cells = beacon.TradeableCells
            .Where(cell => cell.InBounds(map) && cell.Standable(map) && cell.GetFirstItem(map) is null)
            .Take(4)
            .ToArray();
        EndToEndAssert.Equal(4, cells.Length, "The beacon must expose four clear silver cells.");
        foreach (var cell in cells)
        {
            var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = ThingDefOf.Silver.stackLimit;
            GenSpawn.Spawn(silver, cell, map);
        }
    }

    private static Pawn GenerateCrafter()
    {
        var crafting = WorkTypeDefOf.Crafting;
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            if (pawn.WorkTypeIsDisabled(crafting) ||
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) < 0.9f ||
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) < 0.9f)
            {
                pawn.Destroy(DestroyMode.Vanish);
                continue;
            }

            HumanlikePawnFixture.SetName(pawn, "Primitive Cookware Crafter");
            pawn.inventory?.innerContainer.ClearAndDestroyContents();
            pawn.workSettings.EnableAndInitialize();
            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (!pawn.WorkTypeIsDisabled(workType))
                {
                    pawn.workSettings.SetPriority(workType, 0);
                }
            }

            pawn.workSettings.SetPriority(crafting, 1);
            pawn.skills.GetSkill(SkillDefOf.Crafting).Level = 12;
            if (pawn.needs?.food is { } food)
            {
                food.CurLevelPercentage = 1f;
            }

            return pawn;
        }

        throw new EndToEndAssertionException("Could not generate a capable crafting pawn.");
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
                HumanlikePawnFixture.SetName(pawn, "Kitchenware Buyer");
                pawn.skills.GetSkill(SkillDefOf.Social).Level = 20;
                return pawn;
            }

            pawn.Destroy(DestroyMode.Vanish);
        }

        throw new EndToEndAssertionException("Could not generate a capable trade negotiator.");
    }

    private static IntVec3 FindFixtureCenter(Map map)
    {
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 55f, useCenter: true))
        {
            if (CellRect.CenteredOn(candidate, 24).Cells.All(cell =>
                    cell.InBounds(map) &&
                    cell.Standable(map) &&
                    !map.roofGrid.Roofed(cell) &&
                    cell.GetThingList(map).Count == 0))
            {
                return candidate;
            }
        }

        throw new EndToEndAssertionException("Could not find a clear kitchenware fixture area.");
    }
}
