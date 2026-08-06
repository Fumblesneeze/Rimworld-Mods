using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.legacy-unplated-meal-expiry",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "fumblesneeze.immersivechefs",
    MaxFrames = 1_200,
    MaxGameTicks = 2_000,
    MaxWallClockSeconds = 60)]
public sealed class LegacyUnplatedMealExpiryTest : IRimWorldEndToEndTest
{
    private Pawn observer = null!;
    private ThingWithComps meal = null!;
    private CompRottable rottable = null!;
    private ThingDef plateDef = null!;
    private ThingDef cutleryDef = null!;
    private int arrangedAtTick;
    private float arrangedRotProgress;

    public void Arrange(IEndToEndContext context)
    {
        var map = Current.Game.CurrentMap;
        var observerCell = FindClearCell(map);
        var mealCell = GenAdj.CellsAdjacent8Way(observerCell, Rot4.North, IntVec2.One)
            .First(cell => cell.InBounds(map) && cell.Standable(map) && cell.GetEdifice(map) is null);

        observer = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        observer.Name = new NameSingle("Legacy Meal Expiry Observer");
        observer.inventory?.innerContainer.ClearAndDestroyContents();
        GenSpawn.Spawn(observer, observerCell, map);
        observer.drafter.Drafted = true;
        if (observer.needs?.food is { } food)
        {
            food.CurLevelPercentage = 1f;
        }

        meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        GenSpawn.Spawn(meal, mealCell, map);
        rottable = meal.GetComp<CompRottable>();
        EndToEndAssert.NotNull(rottable, "The finalized simple meal must retain vanilla rotting behavior.");
        EndToEndAssert.True(
            rottable.PropsRot.rotDestroys,
            "The vanilla simple meal must still be destroyed when it reaches the rotten stage.");
        EndToEndAssert.True(
            meal.AmbientTemperature > -10f,
            $"The expiry fixture needs a naturally rotting cell, but ambient temperature was {meal.AmbientTemperature:0.##} C.");

        var embedded = meal.GetComp<CompEmbeddedWare>();
        EndToEndAssert.NotNull(embedded, "The finalized simple meal must expose embedded-ware compatibility state.");
        EndToEndAssert.Equal(0, embedded.EmbeddedPlateCount, "The legacy/debug-style fixture must begin unplated.");

        plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        cutleryDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery");
        EndToEndAssert.Equal(0, CountServiceWare(map), "The expiry fixture must not arrange loose service ware.");

        arrangedRotProgress = Math.Max(0f, rottable.PropsRot.TicksToRotStart - 1f);
        rottable.RotProgress = arrangedRotProgress;
        arrangedAtTick = Find.TickManager.TicksGame;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new SelectionActionStep(
            "select legacy expiry fixture",
            new[] { observer.ThingID, meal.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame legacy meal before native expiry",
            new[] { observer.ThingID, meal.ThingID },
            paddingPixels: 160);
        yield return new ScreenshotStep(
            "legacy unplated meal immediately before expiry",
            new[] { observer.ThingID, meal.ThingID },
            paddingPixels: 140);
        yield return new TimeControlActionStep(
            "let ordinary game time expire the legacy meal",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "vanilla rotting destroys the legacy meal",
            _ => meal.Destroyed,
            new EndToEndDeadline(600, 1_200, TimeSpan.FromSeconds(40)));
        yield return new TimeControlActionStep(
            "pause after native legacy meal expiry",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select observer after legacy meal expiry",
            new[] { observer.ThingID },
            additive: false);
        yield return new AssertionStep(
            "expiry does not fabricate service ware",
            _ =>
            {
                EndToEndAssert.True(meal.Destroyed, "The original legacy-style unplated meal must be gone.");
                EndToEndAssert.True(
                    Find.TickManager.TicksGame > arrangedAtTick,
                    "The meal must disappear only after player-controlled game time advances.");
                EndToEndAssert.Equal(
                    0,
                    CountServiceWare(Current.Game.CurrentMap),
                    "Rot-destroying an unplated meal must not return or fabricate plate or cutlery.");
            });
        yield return new ScreenshotStep(
            "after native legacy meal expiry without fabricated ware",
            new[] { observer.ThingID },
            paddingPixels: 180);
        yield return new CheckpointStep(
            "legacy unplated expiry result",
            _ => new Dictionary<string, string>
            {
                ["mealDestroyed"] = meal.Destroyed.ToString(),
                ["initialRotProgress"] = arrangedRotProgress.ToString("0.###"),
                ["elapsedGameTicks"] = (Find.TickManager.TicksGame - arrangedAtTick).ToString(),
                ["serviceWareOnMapOrPawn"] = CountServiceWare(Current.Game.CurrentMap).ToString()
            });
    }

    private int CountServiceWare(Map map)
    {
        var mapCount = map.listerThings.AllThings.Count(IsServiceWare);
        var inventoryCount = observer.inventory?.innerContainer.Count(IsServiceWare) ?? 0;
        var carriedCount = IsServiceWare(observer.carryTracker?.CarriedThing) ? 1 : 0;
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

        throw new EndToEndAssertionException("Could not find a clear two-cell legacy expiry fixture area.");
    }
}
