using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.generated-meal-origins",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_800,
    MaxGameTicks = 16_000,
    MaxWallClockSeconds = 150)]
public sealed class GeneratedMealOriginTest : IRimWorldEndToEndTest
{
    private readonly List<Fixture> fixtures = new();

    public void Arrange(IEndToEndContext context)
    {
        var map = Current.Game.CurrentMap;
        var visitorFaction = Find.FactionManager.AllFactionsVisible
            .First(faction =>
                faction != Faction.OfPlayer &&
                faction.def.humanlikeFaction &&
                !faction.HostileTo(Faction.OfPlayer));
        var raiderFaction = Find.FactionManager.AllFactionsVisible
            .First(faction =>
                faction != Faction.OfPlayer &&
                faction.def.humanlikeFaction &&
                faction.HostileTo(Faction.OfPlayer));
        var cells = FindFixtureCells(map, 3);
        var visitorDiner = CreateDiner("Diner for visitor inventory");
        var raiderDiner = CreateDiner("Diner for raider inventory");

        fixtures.Add(CreateFixture(
            map,
            cells[0],
            "visitor inventory",
            GenerateExternalPawnInventoryMeal(
                visitorDiner,
                visitorFaction,
                ThingDefOf.MealSimple),
            visitorFaction.Name,
            visitorDiner));
        fixtures.Add(CreateFixture(
            map,
            cells[1],
            "raider inventory",
            GenerateExternalPawnInventoryMeal(
                raiderDiner,
                raiderFaction,
                ThingDefOf.MealFine),
            raiderFaction.Name,
            raiderDiner));
        fixtures.Add(CreateFixture(
            map,
            cells[2],
            "trader stock",
            GenerateTraderStockMeal(ThingDefOf.MealFine),
            "ThingSetMaker_TraderStock"));
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var catalog = context.GetRequiredService<IEndToEndFloatMenuCatalog>();
        foreach (var fixture in fixtures)
        {
            if (fixture.Diner.needs?.food is { } activeFood)
            {
                activeFood.CurLevelPercentage = 0.05f;
            }

            yield return new SelectionActionStep(
                "select the " + fixture.Name + " meal",
                new[] { fixture.Meal.ThingID },
                additive: false);
            yield return new CameraActionStep(
                "frame the " + fixture.Name + " serving",
                new[] { fixture.Diner.ThingID, fixture.Meal.ThingID },
                paddingPixels: 160);
            yield return new ScreenshotStep(
                "inspect the generated " + fixture.Name + " plated meal",
                new[] { fixture.Diner.ThingID, fixture.Meal.ThingID },
                paddingPixels: 420);

            var options = catalog.Query(fixture.Diner.ThingID, fixture.Meal.ThingID);
            var consumeOptions = options
                .Where(option =>
                    !option.Disabled &&
                    option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            EndToEndAssert.Equal(
                1,
                consumeOptions.Length,
                fixture.Name + " must expose exactly one native enabled consume action; observed " +
                string.Join(", ", options.Select(option =>
                    $"'{option.Label}' (disabled={option.Disabled})")));

            yield return new FloatMenuActionStep(
                "order native consumption of the " + fixture.Name + " meal",
                fixture.Diner.ThingID,
                fixture.Meal.ThingID,
                consumeOptions[0].StableId);
            yield return new TimeControlActionStep(
                "run the " + fixture.Name + " ingestion",
                paused: false,
                EndToEndGameSpeed.Superfast);
            yield return new WaitUntilStep(
                "the exact " + fixture.Name + " plate returns dirty after eating",
                _ => fixture.Meal.Destroyed &&
                     fixture.Plate.Spawned &&
                     fixture.Plate.TryGetComp<CompSanitation>()?.IsDirty == true,
                new EndToEndDeadline(1_400, 5_000, TimeSpan.FromSeconds(45)));
            yield return new TimeControlActionStep(
                "pause after the " + fixture.Name + " meal",
                paused: true,
                EndToEndGameSpeed.Normal);
            yield return new SelectionActionStep(
                "select the returned " + fixture.Name + " plate",
                new[] { fixture.Plate.ThingID },
                additive: false);
            yield return new AssertionStep(
                "the " + fixture.Name + " meal retained and returned one exact plate",
                _ =>
                {
                    EndToEndAssert.True(
                        fixture.Meal.Destroyed,
                        fixture.Name + " meal must be consumed by the native ingest job.");
                    EndToEndAssert.True(
                        fixture.Plate.Spawned,
                        fixture.Name + " must return its original physical plate to the map.");
                    EndToEndAssert.True(
                        fixture.Plate.TryGetComp<CompSanitation>()?.IsDirty == true,
                        fixture.Name + " must return that exact plate dirty.");
                });
            yield return new ScreenshotStep(
                "observe the returned dirty " + fixture.Name + " plate",
                new[] { fixture.Diner.ThingID, fixture.Plate.ThingID },
                paddingPixels: 420);
        }

        yield return new CheckpointStep(
            "generated meal origin lifecycle",
            _ => fixtures.ToDictionary(
                fixture => fixture.Name,
                fixture =>
                    "source=" + fixture.Source +
                    "; mealDestroyed=" + fixture.Meal.Destroyed +
                    "; samePlateSpawned=" + fixture.Plate.Spawned +
                    "; dirty=" +
                    (fixture.Plate.TryGetComp<CompSanitation>()?.IsDirty == true) +
                    "; stuff=" + (fixture.Plate.Stuff?.defName ?? fixture.Plate.def.defName) +
                    "; quality=" + ReadQuality(fixture.Plate)));
    }

    private static Fixture CreateFixture(
        Map map,
        IntVec3 cell,
        string name,
        ThingWithComps meal,
        string source,
        Pawn? diner = null)
    {
        var embedded = meal.GetComp<CompEmbeddedWare>();
        EndToEndAssert.NotNull(
            embedded,
            name + " must use the finalized embedded-ware component.");
        EndToEndAssert.Equal(
            meal.stackCount,
            embedded!.EmbeddedPlateCount,
            name + " must receive exactly one generated plate per serving.");
        var plate = embedded.PeekPlateThing();
        EndToEndAssert.NotNull(
            plate,
            name + " must retain a real generated plate Thing.");
        EndToEndAssert.True(plate!.TryGetComp<CompQuality>() is null,
            name + " must use an ungraded generated plate.");
        EndToEndAssert.True(
            plate!.TryGetComp<CompSanitation>() is { IsDirty: false },
            name + " must begin with a clean generated plate.");

        GenSpawn.Spawn(meal, cell + IntVec3.East, map);
        diner ??= CreateDiner("Diner for " + name);
        GenSpawn.Spawn(diner, cell + IntVec3.West, map);
        return new Fixture(name, source, diner, meal, plate!);
    }

    private static ThingWithComps GenerateExternalPawnInventoryMeal(
        Pawn pawn,
        Faction faction,
        ThingDef mealDef)
    {
        var kind = PawnKindDefOf.Colonist;
        var priorFixedInventory = kind.fixedInventory;
        var priorInventoryOptions = kind.inventoryOptions;
        pawn.inventory?.innerContainer.ClearAndDestroyContents();
        pawn.SetFactionDirect(faction);
        try
        {
            kind.fixedInventory = new List<ThingDefCountClass>
            {
                new()
                {
                    thingDef = mealDef,
                    count = 1,
                    quality = QualityCategory.Normal
                }
            };
            kind.inventoryOptions = null;
            PawnInventoryGenerator.GenerateInventoryFor(
                pawn,
                new PawnGenerationRequest(
                    kind,
                    faction,
                    canGeneratePawnRelations: false));
        }
        finally
        {
            kind.fixedInventory = priorFixedInventory;
            kind.inventoryOptions = priorInventoryOptions;
            pawn.SetFactionDirect(Faction.OfPlayer);
        }

        var meal = pawn.inventory?.innerContainer.InnerListForReading
            .OfType<ThingWithComps>()
            .Single(thing => thing.def == mealDef);
        pawn.inventory!.innerContainer.Remove(meal);
        pawn.inventory.innerContainer.ClearAndDestroyContents();
        return meal!;
    }

    private static Pawn CreateDiner(string name)
    {
        var diner = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        HumanlikePawnFixture.SetName(diner, name);
        diner.inventory?.innerContainer.ClearAndDestroyContents();
        diner.workSettings.EnableAndInitialize();
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            if (!diner.WorkTypeIsDisabled(workType))
            {
                diner.workSettings.SetPriority(workType, 0);
            }
        }

        if (diner.needs?.food is { } food)
        {
            food.CurLevelPercentage = 1f;
        }

        return diner;
    }

    private static ThingWithComps GenerateTraderStockMeal(ThingDef mealDef)
    {
        var generator = new StockGenerator_SingleDef
        {
            countRange = new IntRange(1, 1)
        };
        var thingDefField = typeof(StockGenerator_SingleDef).GetField(
            "thingDef",
            BindingFlags.Instance | BindingFlags.NonPublic);
        EndToEndAssert.NotNull(
            thingDefField,
            "RimWorld 1.6 StockGenerator_SingleDef must retain its XML thingDef field.");
        thingDefField!.SetValue(generator, mealDef);
        var trader = new TraderKindDef
        {
            defName = "ImmersiveChefs_E2EGeneratedMealTrader",
            stockGenerators = new List<StockGenerator> { generator }
        };
        generator.ResolveReferences(trader);
        var generated = ThingSetMakerDefOf.TraderStock.root.Generate(
            new ThingSetMakerParams { traderDef = trader });
        var meal = generated.OfType<ThingWithComps>().Single(thing => thing.def == mealDef);
        foreach (var extra in generated.Where(thing => !ReferenceEquals(thing, meal)))
        {
            if (!extra.Destroyed)
            {
                extra.Destroy(DestroyMode.Vanish);
            }
        }

        return meal;
    }

    private static string ReadQuality(Thing thing) =>
        thing.TryGetComp<CompQuality>()?.Quality.ToString() ?? "ungraded";

    private static IReadOnlyList<IntVec3> FindFixtureCells(Map map, int count)
    {
        var cells = new List<IntVec3>();
        foreach (var cell in GenRadial.RadialCellsAround(map.Center, 55f, useCenter: true))
        {
            if (!cell.InBounds(map) ||
                !cell.Standable(map) ||
                cell.GetEdifice(map) is not null ||
                cells.Any(existing => existing.DistanceToSquared(cell) < 144) ||
                !new[] { cell + IntVec3.East, cell + IntVec3.West }.All(candidate =>
                    candidate.InBounds(map) &&
                    candidate.Standable(map) &&
                    candidate.GetEdifice(map) is null))
            {
                continue;
            }

            cells.Add(cell);
            if (cells.Count == count)
            {
                return cells;
            }
        }

        throw new EndToEndAssertionException("Could not find three separated generated-meal fixtures.");
    }

    private sealed class Fixture
    {
        public Fixture(
            string name,
            string source,
            Pawn diner,
            ThingWithComps meal,
            Thing plate)
        {
            Name = name;
            Source = source;
            Diner = diner;
            Meal = meal;
            Plate = plate;
        }

        public string Name { get; }
        public string Source { get; }
        public Pawn Diner { get; }
        public ThingWithComps Meal { get; }
        public Thing Plate { get; }
    }
}
