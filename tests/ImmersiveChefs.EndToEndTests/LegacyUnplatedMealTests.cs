using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.legacy-unplated-meal",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "fumblesneeze.immersivechefs",
    MaxFrames = 2_400,
    MaxGameTicks = 8_000,
    MaxWallClockSeconds = 90)]
public sealed class LegacyUnplatedMealTest : IRimWorldEndToEndTest
{
    private Pawn pawn = null!;
    private ThingWithComps meal = null!;
    private ThingDef plateDef = null!;
    private ThingDef cutleryDef = null!;
    private string pawnId = string.Empty;
    private string mealId = string.Empty;

    public void Arrange(IEndToEndContext context)
    {
        var map = Current.Game.CurrentMap;
        var pawnCell = FindClearCell(map);
        var mealCell = GenAdj.CellsAdjacent8Way(pawnCell, Rot4.North, IntVec2.One)
            .First(cell => cell.InBounds(map) && cell.Standable(map) && cell.GetEdifice(map) is null);

        pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false,
            forceNoGear: true));
        pawn.Name = new NameSingle("Legacy Meal Compatibility Diner");
        pawn.inventory?.innerContainer.ClearAndDestroyContents();
        GenSpawn.Spawn(pawn, pawnCell, map);
        if (pawn.needs?.food is { } food)
        {
            food.CurLevelPercentage = 0.05f;
        }

        meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        GenSpawn.Spawn(meal, mealCell, map);
        plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        cutleryDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery");

        var embedded = meal.GetComp<CompEmbeddedWare>();
        EndToEndAssert.NotNull(embedded, "The finalized simple meal must expose embedded-ware compatibility state.");
        EndToEndAssert.Equal(0, embedded.EmbeddedPlateCount, "The debug/mod-spawned fixture must begin unplated.");
        EndToEndAssert.Equal(0, CountServiceWare(map), "The fixture must not arrange loose service ware.");

        pawnId = pawn.ThingID;
        mealId = meal.ThingID;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new SelectionActionStep(
            "select legacy diner and unplated meal",
            new[] { pawnId, mealId },
            additive: false);
        yield return new CameraActionStep(
            "frame legacy diner and unplated meal",
            new[] { pawnId, mealId },
            paddingPixels: 120);
        yield return new ScreenshotStep(
            "before native legacy meal order",
            new[] { pawnId, mealId },
            paddingPixels: 120);

        var catalog = context.GetRequiredService<IEndToEndFloatMenuCatalog>();
        var options = catalog.Query(pawnId, mealId);
        var consumeOptions = options
            .Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(
            1,
            consumeOptions.Length,
            "Expected one enabled native consume option; observed " +
            string.Join(", ", options.Select(option =>
                $"'{option.Label}' (disabled={option.Disabled})")));

        yield return new FloatMenuActionStep(
            "order native consumption of the unplated meal",
            pawnId,
            mealId,
            consumeOptions[0].StableId);
        yield return new TimeControlActionStep(
            "start ordinary ingestion",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "legacy diner reaches the native ingest toil",
            _ => !meal.Destroyed &&
                 pawn.CurJobDef == JobDefOf.Ingest &&
                 pawn.Position.AdjacentTo8WayOrInside(meal.Position),
            new EndToEndDeadline(600, 1_000, TimeSpan.FromSeconds(30)));
        yield return new ScreenshotStep(
            "native legacy meal ingestion in progress",
            new[] { pawnId, mealId },
            paddingPixels: 120);
        yield return new TimeControlActionStep(
            "finish ordinary ingestion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "legacy meal is actually ingested",
            _ => meal.Destroyed,
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause after legacy ingestion",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select legacy diner after ingestion",
            new[] { pawnId },
            additive: false);
        yield return new AssertionStep(
            "no plate or cutlery was fabricated",
            _ =>
            {
                EndToEndAssert.True(meal.Destroyed, "The original unplated meal must have been consumed.");
                EndToEndAssert.Equal(
                    0,
                    CountServiceWare(Current.Game.CurrentMap),
                    "Eating a meal that never contained a plate must not return or fabricate service ware.");
            });
        yield return new ScreenshotStep(
            "after legacy ingestion without fabricated ware",
            new[] { pawnId },
            paddingPixels: 180);
        yield return new CheckpointStep(
            "legacy unplated compatibility result",
            _ => new Dictionary<string, string>
            {
                ["mealDestroyed"] = meal.Destroyed.ToString(),
                ["serviceWareOnMapOrPawn"] = CountServiceWare(Current.Game.CurrentMap).ToString(),
                ["pawnJob"] = pawn.CurJobDef?.defName ?? "none"
            });
    }

    private int CountServiceWare(Map map)
    {
        var mapCount = map.listerThings.AllThings.Count(IsServiceWare);
        var inventoryCount = pawn.inventory?.innerContainer.Count(IsServiceWare) ?? 0;
        var carriedCount = IsServiceWare(pawn.carryTracker?.CarriedThing) ? 1 : 0;
        return mapCount + inventoryCount + carriedCount;
    }

    private bool IsServiceWare(Thing? thing) =>
        thing is not null && (thing.def == plateDef || thing.def == cutleryDef);

    private static IntVec3 FindClearCell(Map map)
    {
        foreach (var cell in GenRadial.RadialCellsAround(map.Center, 45f, useCenter: true))
        {
            if (!cell.InBounds(map) || !cell.Standable(map) || cell.GetEdifice(map) is not null ||
                cell.GetFirstPawn(map) is not null)
            {
                continue;
            }

            if (GenAdj.CellsAdjacent8Way(cell, Rot4.North, IntVec2.One)
                .Any(adjacent => adjacent.InBounds(map) && adjacent.Standable(map) &&
                                 adjacent.GetEdifice(map) is null))
            {
                return cell;
            }
        }

        throw new EndToEndAssertionException("Could not find a clear two-cell legacy meal fixture area.");
    }
}
