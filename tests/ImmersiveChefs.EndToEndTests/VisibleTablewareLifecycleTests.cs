using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.visible-tableware-lifecycle",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.immersivechefs",
    MaxFrames = 3_600,
    MaxGameTicks = 14_000,
    MaxWallClockSeconds = 150)]
public sealed class VisibleTablewareLifecycleTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn diner = null!;
    private Thing table = null!;
    private Thing chair = null!;
    private ThingWithComps meal = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private IntVec3 selectedTableCell = IntVec3.Invalid;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);

        var settings = ImmersiveChefsMod.Settings;
        var priorMode = settings.WareRequirementMode;
        var priorFallback = settings.DirtyWareFallback;
        var priorTemperature = settings.MealTemperatureEnabled;
        context.DeferCleanup(() =>
        {
            settings.WareRequirementMode = priorMode;
            settings.DirtyWareFallback = priorFallback;
            settings.MealTemperatureEnabled = priorTemperature;
        });
        settings.WareRequirementMode = WareRequirementMode.Strict;
        settings.DirtyWareFallback = DirtyWareFallback.Never;
        settings.MealTemperatureEnabled = false;

        table = SpawnFurniture("Table1x2c", center, Rot4.North, ThingDefOf.WoodLog);
        var chairCell = GenAdj.CellsAdjacentCardinal(table)
            .Where(cell => cell.InBounds(map) && cell.Standable(map))
            .OrderBy(cell => cell.z)
            .First();
        chair = SpawnFurniture("DiningChair", chairCell, Rot4.North, ThingDefOf.WoodLog);

        diner = FoodSearchE2EFixture.CreateColonist("Elena Ruiz");
        FoodSearchE2EFixture.SetHunger(diner, 0.95f);
        GenSpawn.Spawn(diner, center + (IntVec3.West * 3), map);
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            if (!diner.WorkTypeIsDisabled(workType))
            {
                diner.workSettings.SetPriority(workType, 0);
            }
        }

        meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        meal.GetComp<CompCulinaryState>()!.ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                qualityScore: 55,
                temperatureCelsius: 21f,
                contamination: ContaminationSources.None,
                microwaveReheatCount: 0,
                lastThermalTick: Find.TickManager.TicksGame,
                cookwareMaterial: KitchenMaterialKind.Steel)
        });
        plate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.Steel);
        cutlery = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cutlery",
            ThingDefOf.Steel);
        EndToEndAssert.True(
            meal.GetComp<CompEmbeddedWare>()!.TryEmbedPlate(plate),
            "The table-lifecycle fixture must embed its one exact clean plate.");
        GenSpawn.Spawn(cutlery, center + (IntVec3.West * 2), map);
        GenSpawn.Spawn(meal, center + (IntVec3.East * 3), map);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        if (Find.WindowStack.Windows.Any(window =>
                string.Equals(
                    window.GetType().FullName,
                    "LudeonTK.EditWindow_Log",
                    StringComparison.Ordinal)))
        {
            yield return new WindowCancelActionStep(
                "close the startup developer log before observing gameplay",
                "LudeonTK.EditWindow_Log");
        }

        yield return new TimeControlActionStep(
            "pause before table dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new CameraActionStep(
            "frame the diner meal cutlery and table closely",
            new[] { diner.ThingID, meal.ThingID, cutlery.ThingID, table.ThingID },
            paddingPixels: 90);
        yield return new SelectionActionStep(
            "clear selection before the table-dining preview",
            Array.Empty<string>(),
            additive: false);
        yield return new ScreenshotStep(
            "clean setting awaits native table dining",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "activate hunger only for the native dining order",
            _ => FoodSearchE2EFixture.SetHunger(diner, 0.20f));

        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(diner.ThingID, meal.ThingID);
        var consume = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(
            1,
            consume.Length,
            "Expected one enabled native Consume action for the plated meal.");
        yield return new FloatMenuActionStep(
            "order native table dining",
            diner.ThingID,
            meal.ThingID,
            consume[0].StableId);
        yield return new TimeControlActionStep(
            "run native table dining",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "vanilla selects the actual eat-surface target",
            _ => ObserveNativeTableTarget(),
            new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(40)));
        yield return new CameraActionStep(
            "tighten on the active diner and used table",
            new[] { diner.ThingID, table.ThingID },
            paddingPixels: 70);
        yield return new SelectionActionStep(
            "keep native dining unobscured",
            Array.Empty<string>(),
            additive: false);
        yield return new ScreenshotStep(
            "diner uses the vanilla-selected table",
            new[] { diner.ThingID, table.ThingID },
            paddingPixels: 72);
        yield return new WaitUntilStep(
            "the exact dirty setting remains on the used table",
            _ => meal.Destroyed &&
                 plate.Spawned &&
                 cutlery.Spawned &&
                 plate.Position == selectedTableCell &&
                 cutlery.Position == selectedTableCell,
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause after the returned setting appears",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "conserve the exact dirty table setting",
            _ => AssertReturnedSetting());
        yield return new CameraActionStep(
            "frame the returned plate cutlery and table tightly",
            new[] { plate.ThingID, cutlery.ThingID, table.ThingID },
            paddingPixels: 65);
        yield return new SelectionActionStep(
            "show the returned setting without brackets",
            Array.Empty<string>(),
            additive: false);
        yield return new ScreenshotStep(
            "used plate and cutlery visibly remain on the dining table",
            new[] { plate.ThingID, cutlery.ThingID, table.ThingID },
            paddingPixels: 72);
        yield return new CheckpointStep(
            "visible tableware lifecycle result",
            _ => new Dictionary<string, string>
            {
                ["diner"] = diner.ThingID,
                ["table"] = table.ThingID,
                ["selectedTableCell"] = selectedTableCell.ToString(),
                ["plate"] = plate.ThingID,
                ["plateCell"] = plate.Position.ToString(),
                ["cutlery"] = cutlery.ThingID,
                ["cutleryCell"] = cutlery.Position.ToString()
            });
    }

    private bool ObserveNativeTableTarget()
    {
        if (diner.CurJobDef != JobDefOf.Ingest || diner.CurJob is null)
        {
            return false;
        }

        var target = diner.CurJob.GetTarget(TargetIndex.B).Cell;
        if (!target.IsValid || !target.InBounds(map) || !target.HasEatSurface(map))
        {
            return false;
        }

        selectedTableCell = target;
        return table.OccupiedRect().Contains(target);
    }

    private void AssertReturnedSetting()
    {
        EndToEndAssert.True(selectedTableCell.IsValid,
            "The workflow must retain vanilla's actual table-cell choice.");
        EndToEndAssert.True(plate.Spawned && plate.stackCount == 1,
            "The exact physical plate must return once.");
        EndToEndAssert.True(cutlery.Spawned && cutlery.stackCount == 1,
            "The exact physical cutlery must return once.");
        EndToEndAssert.True(plate.Position == selectedTableCell,
            "The returned plate must occupy vanilla's selected table cell.");
        EndToEndAssert.True(cutlery.Position == selectedTableCell,
            "The returned cutlery must occupy the same selected table cell as the plate.");
        EndToEndAssert.True(
            table is Building { MaxItemsInCell: 2 } && selectedTableCell.GetItemCount(map) == 2,
            "The dining surface must retain exactly the two physical setting Things in one cell.");
        EndToEndAssert.True(plate.DrawPos != cutlery.DrawPos,
            "RimWorld must render the same-cell plate and cutlery with distinct native offsets.");
        EndToEndAssert.True(
            plate.GetComp<CompSanitation>()!.IsDirty &&
            cutlery.GetComp<CompSanitation>()!.IsDirty,
            "Both exact returned Things must be dirty after native ingestion.");
    }

    private Thing SpawnFurniture(
        string defName,
        IntVec3 cell,
        Rot4 rotation,
        ThingDef? stuff)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? stuff : null);
        thing.SetFactionDirect(Faction.OfPlayer);
        return GenSpawn.Spawn(thing, cell, map, rotation);
    }
}
