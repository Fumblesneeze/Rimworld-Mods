using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.visible-cookware-station-lifecycle",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_600,
    MaxGameTicks = 34_000,
    MaxWallClockSeconds = 300)]
public sealed class VisibleCookwareStationLifecycleTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn cook = null!;
    private Thing stove = null!;
    private ThingWithComps firstCookware = null!;
    private ThingWithComps secondCookware = null!;
    private ThingWithComps firstPlate = null!;
    private ThingWithComps secondPlate = null!;
    private readonly List<ThingWithComps> additionalCookware = new();
    private readonly List<ThingWithComps> additionalPlates = new();
    private ThingWithComps? firstMeal;
    private ThingWithComps? secondMeal;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);

        var settings = ImmersiveChefsMod.Settings;
        var priorMode = settings.WareRequirementMode;
        var priorFallback = settings.DirtyWareFallback;
        var priorTemperature = settings.MealTemperatureEnabled;
        var priorAssistants = settings.AutoCallAssistants;
        context.DeferCleanup(() =>
        {
            settings.WareRequirementMode = priorMode;
            settings.DirtyWareFallback = priorFallback;
            settings.MealTemperatureEnabled = priorTemperature;
            settings.AutoCallAssistants = priorAssistants;
        });
        settings.WareRequirementMode = WareRequirementMode.Strict;
        settings.DirtyWareFallback = DirtyWareFallback.Never;
        settings.MealTemperatureEnabled = false;
        settings.AutoCallAssistants = false;

        stove = SpawnFurniture("FueledStove", center, Rot4.North, ThingDefOf.Steel);
        stove.TryGetComp<CompRefuelable>()?.Refuel(999f);

        cook = CookForYourselfFixture.CreateCapableCook(
            "Elena Ruiz",
            DefDatabase<WorkTypeDef>.GetNamed("Cooking"));
        FoodSearchE2EFixture.SetHunger(cook, 1f);
        GenSpawn.Spawn(cook, stove.InteractionCell + IntVec3.South, map);
        SetOnlyCooking(cook);

        firstCookware = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cookware",
            ThingDefOf.Steel);
        secondCookware = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cookware",
            ThingDefOf.Steel);
        firstPlate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.Steel);
        secondPlate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.Steel);
        var stagingCells = CellRect.CenteredOn(center, 4)
            .Cells
            .Where(cell => cell.InBounds(map) &&
                           cell.Walkable(map) &&
                           cell.DistanceToSquared(center) >= 9f &&
                           cell != cook.Position &&
                           cell != stove.InteractionCell &&
                           !stove.OccupiedRect().Contains(cell) &&
                           cell.GetThingList(map).All(thing =>
                               thing.def.category is not ThingCategory.Item and
                               not ThingCategory.Pawn and
                               not ThingCategory.Building))
            .OrderBy(cell => cell.DistanceToSquared(center))
            .ThenBy(cell => cell.x)
            .ThenBy(cell => cell.z)
            .Take(11)
            .ToArray();
        EndToEndAssert.Equal(11, stagingCells.Length,
            "The sealed fixture must expose eleven reachable interior staging cells.");

        GenSpawn.Spawn(firstCookware, stagingCells[0], map);
        GenSpawn.Spawn(secondCookware, stagingCells[1], map);
        GenSpawn.Spawn(firstPlate, stagingCells[5], map);
        GenSpawn.Spawn(secondPlate, stagingCells[6], map);
        secondCookware.SetForbidden(true, warnOnFail: false);
        secondPlate.SetForbidden(true, warnOnFail: false);
        for (var index = 0; index < 3; index++)
        {
            var cookware = FoodSearchE2EFixture.MakeCleanWare(
                "ImmersiveChefs_Cookware",
                ThingDefOf.Steel);
            var plate = FoodSearchE2EFixture.MakeCleanWare(
                "ImmersiveChefs_Plate",
                ThingDefOf.Steel);
            additionalCookware.Add(cookware);
            additionalPlates.Add(plate);
            GenSpawn.Spawn(cookware, stagingCells[index + 2], map);
            GenSpawn.Spawn(plate, stagingCells[index + 7], map);
            cookware.SetForbidden(true, warnOnFail: false);
            plate.SetForbidden(true, warnOnFail: false);
        }

        var rice = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawRice"));
        rice.stackCount = rice.def.stackLimit;
        GenSpawn.Spawn(rice, stagingCells[10], map);
        AddOneSimpleMealBill();
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
            "pause before ordering the first cooking bill",
            paused: true,
            EndToEndGameSpeed.Normal);
        var firstOptions = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(cook.ThingID, stove.ThingID);
        var firstPrioritize = FindPrioritizeCooking(firstOptions);
        yield return new FloatMenuActionStep(
            "prioritize the first ordinary cooking bill",
            cook.ThingID,
            stove.ThingID,
            firstPrioritize.StableId);
        yield return new TimeControlActionStep(
            "run the first ordinary cooking bill",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the first meal and dirty cookware return through native bill work",
            _ => TryResolveMeal(firstPlate, out firstMeal) &&
                 firstCookware.Spawned &&
                 firstCookware.GetComp<CompSanitation>()!.IsDirty &&
                 stove.OccupiedRect().Contains(firstCookware.Position),
            new EndToEndDeadline(1_600, 7_000, TimeSpan.FromSeconds(65)));
        yield return new TimeControlActionStep(
            "pause with used cookware on the stove",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the first bill retains the exact cookware on the station",
            _ =>
            {
                EndToEndAssert.True(firstCookware.stackCount == 1 &&
                                    stove.OccupiedRect().Contains(firstCookware.Position),
                    "The first exact cookware set must remain once on the stove surface.");
                EndToEndAssert.True(firstCookware.GetComp<CompSanitation>()!.IsDirty,
                    "The first exact cookware set must be dirty after real cooking work.");
            });
        yield return new CameraActionStep(
            "frame the first returned cookware and stove tightly",
            new[] { firstCookware.ThingID, stove.ThingID },
            paddingPixels: 55);
        yield return new SelectionActionStep(
            "keep the stove surface unobscured",
            Array.Empty<string>(),
            additive: false);
        yield return new ScreenshotStep(
            "used cookware visibly remains on the stove after cooking",
            new[] { firstCookware.ThingID, stove.ThingID },
            paddingPixels: 58);

        yield return new AssertionStep(
            "add the second ordinary bill while paused",
            _ =>
            {
                secondCookware.SetForbidden(false, warnOnFail: false);
                secondPlate.SetForbidden(false, warnOnFail: false);
                AddOneSimpleMealBill();
            });
        var secondOptions = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(cook.ThingID, stove.ThingID);
        var secondPrioritize = FindPrioritizeCooking(secondOptions);
        yield return new FloatMenuActionStep(
            "prioritize the second ordinary cooking bill",
            cook.ThingID,
            stove.ThingID,
            secondPrioritize.StableId);
        yield return new TimeControlActionStep(
            "run the second ordinary cooking bill",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the second bill moves the first dirty cookware aside before active work",
            _ => CookingSessionRegistry.TryGetActiveWorkProp(
                     cook,
                     out var prop,
                     out var giver) &&
                 ReferenceEquals(prop, secondCookware) &&
                 ReferenceEquals(giver, stove) &&
                 firstCookware.Spawned &&
                 !stove.OccupiedRect().Contains(firstCookware.Position),
            new EndToEndDeadline(1_200, 4_500, TimeSpan.FromSeconds(50)));
        yield return new TimeControlActionStep(
            "pause while the replacement cookware is in active use",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the prior cookware moved aside without replacement or cleaning",
            _ =>
            {
                EndToEndAssert.True(firstCookware.Spawned && firstCookware.stackCount == 1,
                    "The first exact cookware set must remain one physical map unit.");
                EndToEndAssert.True(firstCookware.GetComp<CompSanitation>()!.IsDirty,
                    "Moving the first cookware aside must not clean it.");
                EndToEndAssert.True(!stove.OccupiedRect().Contains(firstCookware.Position),
                    "The later admitted bill must clear the first cookware from the stove footprint.");
            });
        yield return new CameraActionStep(
            "frame the active cook stove and moved-aside cookware tightly",
            new[] { cook.ThingID, stove.ThingID, firstCookware.ThingID },
            paddingPixels: 60);
        yield return new SelectionActionStep(
            "keep the second cooking action unobscured",
            Array.Empty<string>(),
            additive: false);
        yield return new ScreenshotStep(
            "a later cook pushes prior dirty cookware aside and uses a clean set",
            new[] { cook.ThingID, stove.ThingID, firstCookware.ThingID },
            paddingPixels: 62);

        yield return new TimeControlActionStep(
            "finish the second ordinary bill",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the second meal and second dirty cookware return",
            _ => TryResolveMeal(secondPlate, out secondMeal) &&
                 secondCookware.Spawned &&
                 secondCookware.GetComp<CompSanitation>()!.IsDirty &&
                 stove.OccupiedRect().Contains(secondCookware.Position),
            new EndToEndDeadline(1_600, 7_000, TimeSpan.FromSeconds(65)));
        yield return new TimeControlActionStep(
            "pause after both native bills",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "conserve both exact cookware sets and both plated meals",
            _ =>
            {
                EndToEndAssert.NotNull(firstMeal,
                    "The first ordinary bill must retain its plated meal.");
                EndToEndAssert.NotNull(secondMeal,
                    "The second ordinary bill must retain its plated meal.");
                EndToEndAssert.True(firstCookware.Spawned && secondCookware.Spawned &&
                                    firstCookware.stackCount == 1 && secondCookware.stackCount == 1,
                    "Both exact cookware sets must survive once without duplication or loss.");
                EndToEndAssert.True(!stove.OccupiedRect().Contains(firstCookware.Position) &&
                                    stove.OccupiedRect().Contains(secondCookware.Position),
                    "Only the latest used cookware must occupy the stove while the prior set remains aside.");
            });

        var usedCookware = new List<ThingWithComps> { firstCookware, secondCookware };
        for (var index = 0; index < additionalCookware.Count; index++)
        {
            var completedServingCount = index + 3;
            ThingWithComps? activeCookware = null;
            yield return new TimeControlActionStep(
                $"pause before ordinary cooking bill {completedServingCount}",
                paused: true,
                EndToEndGameSpeed.Normal);
            yield return new AssertionStep(
                $"add ordinary cooking bill {completedServingCount}",
                _ =>
                {
                    additionalCookware[index].SetForbidden(false, warnOnFail: false);
                    additionalPlates[index].SetForbidden(false, warnOnFail: false);
                    AddOneSimpleMealBill();
                });
            if (IsCookingAtStove())
            {
                yield return new AssertionStep(
                    $"ordinary work assignment starts cooking bill {completedServingCount}",
                    _ => EndToEndAssert.True(
                        IsCookingAtStove(),
                        "RimWorld's native work assignment must keep the exact bill active."));
            }
            else
            {
                var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
                    .Query(cook.ThingID, stove.ThingID);
                var prioritize = options.SingleOrDefault(option =>
                    !option.Disabled &&
                    option.Label.IndexOf("prioritize", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    option.Label.IndexOf("cook", StringComparison.OrdinalIgnoreCase) >= 0);
                if (prioritize is not null)
                {
                    yield return new FloatMenuActionStep(
                        $"prioritize ordinary cooking bill {completedServingCount}",
                        cook.ThingID,
                        stove.ThingID,
                        prioritize.StableId);
                }
                else
                {
                    EndToEndAssert.True(
                        options.Any(option => option.Disabled &&
                            option.Label.IndexOf("already cooking", StringComparison.OrdinalIgnoreCase) >= 0),
                        "The native menu must offer Prioritize cooking or report the same bill already active; observed " +
                        string.Join(", ", options.Select(option =>
                            $"'{option.Label}' (disabled={option.Disabled})")));
                }
            }
            yield return new TimeControlActionStep(
                $"run ordinary cooking bill {completedServingCount}",
                paused: false,
                EndToEndGameSpeed.Superfast);
            yield return new WaitUntilStep(
                $"bill {completedServingCount} uses another exact clean cookware set",
                _ => CookingSessionRegistry.TryGetActiveWorkProp(
                         cook,
                         out var prop,
                         out var giver) &&
                     prop is ThingWithComps candidate &&
                     ReferenceEquals(giver, stove) &&
                     !usedCookware.Contains(candidate) &&
                     CaptureActiveCookware(candidate, out activeCookware),
                new EndToEndDeadline(400, 2_000, TimeSpan.FromSeconds(20)));
            yield return new WaitUntilStep(
                $"bill {completedServingCount} completes and returns its dirty cookware",
                _ => activeCookware is { Spawned: true } &&
                     activeCookware.GetComp<CompSanitation>()!.IsDirty &&
                     CompletedMealServings() >= completedServingCount,
                new EndToEndDeadline(400, 2_000, TimeSpan.FromSeconds(20)));
            yield return new AssertionStep(
                $"retain exact cookware set {completedServingCount}",
                _ =>
                {
                    EndToEndAssert.NotNull(activeCookware,
                        $"Bill {completedServingCount} must retain its active cookware identity.");
                    usedCookware.Add(activeCookware!);
                });
        }

        yield return new TimeControlActionStep(
            "pause after five ordinary bills",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "five exact dirty cookware sets accumulate at and around the stove",
            _ =>
            {
                EndToEndAssert.Equal(5, usedCookware.Distinct().Count(),
                    "Five bills must use five distinct physical cookware sets.");
                EndToEndAssert.True(usedCookware.All(ware =>
                        ware.Spawned &&
                        ware.stackCount == 1 &&
                        ware.GetComp<CompSanitation>()!.IsDirty &&
                        ware.Position.DistanceToSquared(stove.Position) <= 64f),
                    "All five exact dirty sets must remain visibly accumulated at or around the stove.");
                EndToEndAssert.Equal(1,
                    usedCookware.Count(ware => stove.OccupiedRect().Contains(ware.Position)),
                    "Only the latest completed set should occupy the stove surface.");
            });
        yield return new CameraActionStep(
            "frame all five accumulated cookware sets and the stove",
            usedCookware.Select(ware => ware.ThingID).Append(stove.ThingID).ToArray(),
            paddingPixels: 62);
        yield return new SelectionActionStep(
            "keep the five-set accumulation unobscured",
            Array.Empty<string>(),
            additive: false);
        yield return new ScreenshotStep(
            "five native bills leave five dirty cookware sets at and around the stove",
            usedCookware.Select(ware => ware.ThingID).Append(stove.ThingID).ToArray(),
            paddingPixels: 64);
        yield return new CheckpointStep(
            "visible cookware station lifecycle result",
            _ => new Dictionary<string, string>
            {
                ["cook"] = cook.ThingID,
                ["stove"] = stove.ThingID,
                ["firstCookware"] = firstCookware.ThingID,
                ["firstCookwareCell"] = firstCookware.Position.ToString(),
                ["secondCookware"] = secondCookware.ThingID,
                ["secondCookwareCell"] = secondCookware.Position.ToString(),
                ["firstMeal"] = firstMeal!.ThingID,
                ["secondMeal"] = secondMeal!.ThingID,
                ["cookwareCount"] = usedCookware.Count.ToString(),
                ["cookwareIds"] = string.Join(",", usedCookware.Select(ware => ware.ThingID))
            });
    }

    private static bool CaptureActiveCookware(
        ThingWithComps candidate,
        out ThingWithComps? activeCookware)
    {
        activeCookware = candidate;
        return true;
    }

    private int CompletedMealServings() => map.listerThings
        .ThingsOfDef(ThingDefOf.MealSimple)
        .Sum(meal => meal.stackCount);

    private bool IsCookingAtStove() => cook.CurJobDef == JobDefOf.DoBill &&
                                       ReferenceEquals(cook.CurJob?.targetA.Thing, stove);

    private void AddOneSimpleMealBill()
    {
        var bill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("CookMealSimple"))
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 8f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(cook);
        ((IBillGiver)stove).BillStack.AddBill(bill);
        cook.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    private bool TryResolveMeal(Thing plate, out ThingWithComps? meal)
    {
        meal = map.listerThings.ThingsOfDef(ThingDefOf.MealSimple)
            .OfType<ThingWithComps>()
            .FirstOrDefault(candidate => ReferenceEquals(
                candidate.GetComp<CompEmbeddedWare>()?.PeekPlateThing(),
                plate));
        return meal is not null;
    }

    private static EndToEndFloatMenuOption FindPrioritizeCooking(
        IReadOnlyList<EndToEndFloatMenuOption> options)
    {
        var matches = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("prioritize", StringComparison.OrdinalIgnoreCase) >= 0 &&
                option.Label.IndexOf("cook", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(
            1,
            matches.Length,
            "Expected one enabled native Prioritize cooking command; observed " +
            string.Join(", ", options.Select(option =>
                $"'{option.Label}' (disabled={option.Disabled})")));
        return matches[0];
    }

    private static void SetOnlyCooking(Pawn pawn)
    {
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            if (!pawn.WorkTypeIsDisabled(workType))
            {
                pawn.workSettings.SetPriority(workType, 0);
            }
        }

        pawn.workSettings.SetPriority(DefDatabase<WorkTypeDef>.GetNamed("Cooking"), 1);
        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
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
