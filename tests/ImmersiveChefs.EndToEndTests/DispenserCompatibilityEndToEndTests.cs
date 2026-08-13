using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.replimat-native-dining",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "sumghai.Replimat",
    "sumghai.ReplimatMeals",
    "Dubwise.DubsBadHygiene",
    "avilmask.CommonSense",
    "fumblesneeze.immersivechefs",
    MaxFrames = 10_800,
    MaxGameTicks = 36_000,
    MaxWallClockSeconds = 270)]
public sealed class ReplimatNativeDiningTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn diner = null!;
    private ThingWithComps terminal = null!;
    private ThingWithComps tank = null!;
    private ThingWithComps computer = null!;
    private ThingWithComps dishwasher = null!;
    private ThingWithComps waterTower = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private ThingWithComps? dispensedMeal;
    private string expectedMealDefName = string.Empty;
    private float feedstockBefore;
    private float feedstockAfterDispense;
    private bool nativeJobObserved;
    private bool wareCarriedBeforeOutput;
    private bool dirtyReturnObserved;
    private bool commonSenseCleanupJobObserved;
    private bool commonSenseQueuedSecondWareObserved;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        DispenserE2EFixture.SpawnConduitGrid(map, center, 9, 7);
        DispenserE2EFixture.SpawnPowerSources(map, center + new IntVec3(-8, 0, 6), 18);

        terminal = DispenserE2EFixture.SpawnBuilding(map, "ReplimatTerminal", center + new IntVec3(-3, 0, 0));
        tank = DispenserE2EFixture.SpawnBuilding(map, "ReplimatFeedTank", center);
        computer = DispenserE2EFixture.SpawnBuilding(map, "ReplimatComputer", center + new IntVec3(2, 0, 0));
        DispenserE2EFixture.SetReplimatFeedstockPercent(tank, 1f);
        dishwasher = DispenserE2EFixture.SpawnBuilding(
            map,
            "ImmersiveChefs_Dishwasher",
            center + new IntVec3(3, 0, 3));
        waterTower = DispenserE2EFixture.SpawnDubsWaterSupply(map, dishwasher, 10f);
        DispenserE2EFixture.SettlePower(map, new[] { terminal, tank, computer, dishwasher }, 600);

        EndToEndAssert.True(
            DispenserE2EFixture.ReadBooleanProperty(computer, "Working"),
            "The native Replimat computer must recognize the powered shared network.");
        EndToEndAssert.True(
            DispenserE2EFixture.ReadBooleanProperty(terminal, "CanDispenseNow"),
            "The native terminal must be available with a powered computer and stocked tank.");
        feedstockBefore = DispenserE2EFixture.ReadSingleProperty(tank, "StoredFeedstock");
        EndToEndAssert.True(feedstockBefore > 0f, "The native Replimat tank must begin with real feedstock.");
        EndToEndAssert.True(
            DubsWaterAdapter.IsOperationalFixture(dishwasher) &&
            DubsWaterAdapter.CanSupplyCycleWater(dishwasher, 1f),
            "The exact-group dishwasher must have native power and a supplied Dubs plumbing network.");
        EndToEndAssert.True(
            CommonSenseAdapter.Enabled,
            "The exact-group Common Sense adapter must be active before dining begins.");

        diner = DispenserE2EFixture.CreateCleaningCapableColonist("Replimat diner");
        GenSpawn.Spawn(diner, center + new IntVec3(-2, 0, -3), map);
        FoodSearchE2EFixture.SetHunger(diner, 0.10f);

        var expectedMeal = DefDatabase<ThingDef>.GetNamed("ReplimatMeals_F_Ramen");
        var replimatOnlyPolicy = new FoodPolicy(9_810, "Immersive Chefs Replimat E2E");
        replimatOnlyPolicy.filter.SetDisallowAll();
        replimatOnlyPolicy.filter.SetAllow(expectedMeal, true);
        diner.foodRestriction.CurrentFoodPolicy = replimatOnlyPolicy;
        EndToEndAssert.True(
            diner.foodRestriction.CurrentFoodPolicy.Allows(expectedMeal),
            "The native food policy must allow the one registered Replimat Meals fixture product.");
        expectedMealDefName = expectedMeal.defName;
        var complexity = MealComplexityRuntime.Classify(expectedMeal);
        var plateStuff = complexity switch
        {
            MealComplexity.Elaborate => ThingDefOf.Silver,
            MealComplexity.Advanced => ThingDefOf.Steel,
            _ => DefDatabase<ThingDef>.GetNamed("BlocksGranite")
        };
        plate = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Plate", plateStuff);
        cutlery = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Cutlery", ThingDefOf.Steel);
        GenSpawn.Spawn(plate, center + new IntVec3(-1, 0, -2), map);
        GenSpawn.Spawn(cutlery, center + new IntVec3(1, 0, -2), map);

        FoodSearchE2EFixture.UseStrictNonEmergencyDining(context);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var fixture = new[]
        {
            diner.ThingID, terminal.ThingID, tank.ThingID, computer.ThingID,
            dishwasher.ThingID, waterTower.ThingID, plate.ThingID, cutlery.ThingID
        };
        yield return new SelectionActionStep("select the native Replimat fixture", fixture, additive: false);
        yield return new CameraActionStep("frame the native Replimat fixture", fixture, paddingPixels: 220);
        yield return new ScreenshotStep("before native Replimat food search", Array.Empty<string>(), 0);
        yield return new TimeControlActionStep(
            "run ordinary Replimat food search slowly",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "ordinary hunger selects the real Replimat terminal",
            _ => ObserveNativeJob(),
            new EndToEndDeadline(1_800, 5_000, TimeSpan.FromSeconds(60)));
        yield return new WaitUntilStep(
            "the exact setting is carried before Replimat creates a meal",
            _ => ObserveWareBeforeOutput(),
            new EndToEndDeadline(1_800, 5_000, TimeSpan.FromSeconds(60)));
        yield return new SelectionActionStep(
            "select the Replimat diner carrying service ware",
            new[] { diner.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe service ware collected before Replimat output",
            Array.Empty<string>(),
            0);
        yield return new WaitUntilStep(
            "native Replimat dispensing creates the plated product",
            _ => ObserveDispensedMeal(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause on the native Replimat plated meal",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the Replimat diner with the plated meal",
            new[] { diner.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe native Replimat meal with exact embedded plate",
            Array.Empty<string>(),
            0);
        yield return new TimeControlActionStep(
            "complete native Replimat ingestion",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "Replimat meal returns the same dirty setting",
            _ => ObserveDirtyReturn(),
            new EndToEndDeadline(3_600, 16_000, TimeSpan.FromSeconds(105)));
        yield return new TimeControlActionStep(
            "pause after Replimat dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep("Replimat native lifecycle completes once", _ => AssertCompleted());
        yield return new TimeControlActionStep(
            "resume Common Sense returned-setting cleanup",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the diner carries exact returned Replimat ware to the dishwasher",
            _ => ObserveCommonSenseCleanupJob(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(80)));
        yield return new TimeControlActionStep(
            "pause on ordinary Replimat dish hauling",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the Replimat diner doing dishes",
            new[] { diner.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame Replimat dishwasher handoff",
            new[] { diner.ThingID, dishwasher.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "observe Common Sense hauling exact Replimat ware",
            Array.Empty<string>(),
            0);
        yield return new TimeControlActionStep(
            "finish Replimat dishwasher admission",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the dishwasher contains the exact returned Replimat setting",
            _ => DishwasherContainsExactSetting(),
            new EndToEndDeadline(3_600, 12_000, TimeSpan.FromSeconds(105)));
        yield return new TimeControlActionStep(
            "pause on exact Replimat setting in dishwasher",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "Common Sense hands the Replimat setting to the supplied dishwasher",
            _ => AssertDishwasherHandoff());
        yield return new SelectionActionStep(
            "select dishwasher holding returned Replimat setting",
            new[] { dishwasher.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe exact Replimat setting loading in dishwasher",
            Array.Empty<string>(),
            0);
        yield return new CheckpointStep(
            "Replimat native dining result",
            _ => new Dictionary<string, string>
            {
                ["nativeJobObserved"] = nativeJobObserved.ToString(),
                ["wareCarriedBeforeOutput"] = wareCarriedBeforeOutput.ToString(),
                ["expectedMealDef"] = expectedMealDefName,
                ["actualMealDef"] = dispensedMeal?.def.defName ?? "missing",
                ["mealThingId"] = dispensedMeal?.ThingID ?? "missing",
                ["plateThingId"] = plate.ThingID,
                ["cutleryThingId"] = cutlery.ThingID,
                ["dishwasherThingId"] = dishwasher.ThingID,
                ["waterTowerThingId"] = waterTower.ThingID,
                ["dirtyReturnObserved"] = dirtyReturnObserved.ToString(),
                ["commonSenseCleanupJobObserved"] = commonSenseCleanupJobObserved.ToString(),
                ["commonSenseQueuedSecondWareObserved"] = commonSenseQueuedSecondWareObserved.ToString(),
                ["dishwasherUsedCapacity"] = dishwasher.GetComp<CompDishwasher>()?.UsedCapacity.ToString("R") ?? "missing",
                ["feedstockBefore"] = feedstockBefore.ToString("R"),
                ["feedstockAfterDispense"] = feedstockAfterDispense.ToString("R"),
                ["foodPoisoning"] = diner.health.hediffSet.HasHediff(HediffDefOf.FoodPoisoning).ToString()
            });
    }

    private bool ObserveNativeJob()
    {
        if (diner.CurJobDef != JobDefOf.Ingest)
        {
            return false;
        }

        var target = diner.CurJob?.GetTarget(TargetIndex.A).Thing;
        EndToEndAssert.True(
            ReferenceEquals(target, terminal),
            "The ordinary hunger job must target the real Replimat terminal.");
        var session = DiningSessionRegistry.Current(diner);
        EndToEndAssert.NotNull(session, "The native terminal job must create one dining session.");
        EndToEndAssert.True(
            ReferenceEquals(session!.ReservedPlate, plate) && ReferenceEquals(session.Cutlery, cutlery),
            "The native terminal job must reserve the exact admissible plate and cutlery.");
        nativeJobObserved = true;
        return true;
    }

    private bool ObserveWareBeforeOutput()
    {
        var session = DiningSessionRegistry.Current(diner);
        if (!ReferenceEquals(session?.CarriedPlate, plate) ||
            !ReferenceEquals(session.CarriedCutlery, cutlery))
        {
            return false;
        }

        EndToEndAssert.True(
            DispenserE2EFixture.FindMeal(diner, map, IsReplimatMeal) is null,
            "Replimat must not create the meal before the exact service ware is carried.");
        wareCarriedBeforeOutput = true;
        return true;
    }

    private bool ObserveDispensedMeal()
    {
        dispensedMeal ??= DispenserE2EFixture.FindMeal(diner, map, IsReplimatMeal);
        if (dispensedMeal is null)
        {
            return false;
        }

        var embedded = dispensedMeal.GetComp<CompEmbeddedWare>();
        EndToEndAssert.Equal(1, embedded?.EmbeddedPlateCount ?? -1,
            "The native Replimat serving must contain exactly one plate.");
        EndToEndAssert.True(ReferenceEquals(plate, embedded?.PeekPlateThing()),
            "The native Replimat serving must contain the same physical plate carried beforehand.");
        feedstockAfterDispense = DispenserE2EFixture.ReadSingleProperty(tank, "StoredFeedstock");
        EndToEndAssert.True(feedstockAfterDispense < feedstockBefore,
            "Only native Replimat dispensing may debit the connected tank feedstock.");
        return true;
    }

    private void AssertCompleted()
    {
        EndToEndAssert.True(nativeJobObserved && wareCarriedBeforeOutput,
            "The test must observe native job selection and pre-dispense ware carriage.");
        EndToEndAssert.NotNull(dispensedMeal, "The native terminal must produce a real meal Thing.");
        EndToEndAssert.True(dispensedMeal!.Destroyed, "The diner must consume the native Replimat meal.");
        EndToEndAssert.True(dirtyReturnObserved,
            "The test must observe the exact Replimat plate and cutlery spawned dirty after ingestion.");
        EndToEndAssert.True(!diner.health.hediffSet.HasHediff(HediffDefOf.FoodPoisoning),
            "Native Replimat dining must retain its no-food-poisoning outcome.");
    }

    private bool ObserveDirtyReturn()
    {
        if (dispensedMeal?.Destroyed != true ||
            !plate.Spawned || plate.GetComp<CompSanitation>()?.IsDirty != true ||
            !cutlery.Spawned || cutlery.GetComp<CompSanitation>()?.IsDirty != true)
        {
            return false;
        }

        dirtyReturnObserved = true;
        return true;
    }

    private bool ObserveCommonSenseCleanupJob()
    {
        if (diner.CurJobDef != ImmersiveChefsDefOf.ImmersiveChefs_DoDishes ||
            !ReferenceEquals(diner.CurJob?.GetTarget(TargetIndex.B).Thing, dishwasher))
        {
            return false;
        }

        var carried = diner.carryTracker?.CarriedThing;
        if (!ReferenceEquals(carried, plate) && !ReferenceEquals(carried, cutlery))
        {
            return false;
        }

        EndToEndAssert.True(
            carried is ThingWithComps carriedWare && carriedWare.GetComp<CompSanitation>()?.IsDirty == true,
            "Common Sense must carry one exact dirty returned Replimat item.");
        var otherWare = ReferenceEquals(carried, plate) ? cutlery : plate;
        var queuedOtherWare = diner.jobs.jobQueue
            .Where(queued =>
                queued.job.def == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
                ReferenceEquals(queued.job.GetTarget(TargetIndex.A).Thing, otherWare) &&
                ReferenceEquals(queued.job.GetTarget(TargetIndex.B).Thing, dishwasher))
            .ToArray();
        EndToEndAssert.Equal(
            1,
            queuedOtherWare.Length,
            "Common Sense must queue the other exact returned Replimat item for the same dishwasher.");
        commonSenseCleanupJobObserved = true;
        commonSenseQueuedSecondWareObserved = true;
        return true;
    }

    private bool DishwasherContainsExactSetting()
    {
        var contents = dishwasher.GetComp<CompDishwasher>()?.GetDirectlyHeldThings();
        return contents is not null && contents.Contains(plate) && contents.Contains(cutlery);
    }

    private void AssertDishwasherHandoff()
    {
        var dishwasherComp = dishwasher.GetComp<CompDishwasher>();
        EndToEndAssert.True(commonSenseCleanupJobObserved,
            "The test must observe an ordinary Common Sense dish-hauling job.");
        EndToEndAssert.True(commonSenseQueuedSecondWareObserved,
            "The test must distinguish the Common Sense two-item queue from ordinary one-at-a-time Cleaning work.");
        EndToEndAssert.True(DishwasherContainsExactSetting(),
            "The supplied dishwasher must hold the exact returned Replimat plate and cutlery.");
        EndToEndAssert.True(
            Math.Abs((dishwasherComp?.UsedCapacity ?? -1f) - 1.25f) < 0.001f,
            "One plate and one cutlery setting must occupy exactly 1.25 dishwasher capacity.");
        EndToEndAssert.True(
            plate.GetComp<CompSanitation>()?.IsDirty == true &&
            cutlery.GetComp<CompSanitation>()?.IsDirty == true,
            "Both admitted Replimat items must still be visibly dirty during the loading window.");
        EndToEndAssert.True(
            DubsWaterAdapter.IsOperationalFixture(dishwasher) &&
            DubsWaterAdapter.CanSupplyCycleWater(dishwasher, 1f),
            "The dishwasher-first handoff must retain its supplied Dubs water route.");
    }

    private static bool IsReplimatMeal(Thing thing) =>
        MealClassificationCatalog.ReplimatMealDefNames.Contains(thing.def.defName);
}

[RimWorldEndToEndTest(
    "immersive-chefs.replimat-failed-dispense-rollback",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "sumghai.Replimat",
    "sumghai.ReplimatMeals",
    "Dubwise.DubsBadHygiene",
    "avilmask.CommonSense",
    "fumblesneeze.immersivechefs",
    MaxFrames = 5_400,
    MaxGameTicks = 18_000,
    MaxWallClockSeconds = 180)]
public sealed class ReplimatFailedDispenseRollbackTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn diner = null!;
    private Pawn competingDiner = null!;
    private ThingWithComps terminal = null!;
    private ThingWithComps tank = null!;
    private ThingWithComps computer = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private ThingWithComps competingPlate = null!;
    private ThingWithComps competingCutlery = null!;
    private Thing? firstDispensedMeal;
    private float feedstockBefore;
    private float feedstockAfterFirstDispense;
    private bool wareCarriedBeforeFailure;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        DispenserE2EFixture.SpawnConduitGrid(map, center, 9, 7);
        DispenserE2EFixture.SpawnPowerSources(map, center + new IntVec3(-8, 0, 6), 18);

        terminal = DispenserE2EFixture.SpawnBuilding(
            map,
            "ReplimatTerminal",
            center + new IntVec3(-3, 0, 0));
        tank = DispenserE2EFixture.SpawnBuilding(map, "ReplimatFeedTank", center);
        computer = DispenserE2EFixture.SpawnBuilding(
            map,
            "ReplimatComputer",
            center + new IntVec3(2, 0, 0));
        DispenserE2EFixture.SetReplimatFeedstockPercent(tank, 0.002f);
        DispenserE2EFixture.SettlePower(map, new[] { terminal, tank, computer }, 600);
        EndToEndAssert.True(
            DispenserE2EFixture.ReadBooleanProperty(terminal, "CanDispenseNow"),
            "The native terminal must initially be able to dispense.");
        feedstockBefore = DispenserE2EFixture.ReadSingleProperty(tank, "StoredFeedstock");
        EndToEndAssert.True(feedstockBefore > 0.46f && feedstockBefore < 0.51f,
            "The native feed tank must contain exactly one Ramen-scale serving.");

        var expectedMeal = DefDatabase<ThingDef>.GetNamed("ReplimatMeals_F_Ramen");
        var replimatOnlyPolicy = new FoodPolicy(9_811, "Immersive Chefs failed Replimat E2E");
        replimatOnlyPolicy.filter.SetDisallowAll();
        replimatOnlyPolicy.filter.SetAllow(expectedMeal, true);

        competingDiner = FoodSearchE2EFixture.CreateColonist("First Replimat diner");
        GenSpawn.Spawn(competingDiner, center + new IntVec3(-1, 0, -4), map);
        FoodSearchE2EFixture.SetHunger(competingDiner, 0.10f);
        competingDiner.foodRestriction.CurrentFoodPolicy = replimatOnlyPolicy;

        diner = FoodSearchE2EFixture.CreateColonist("Failed Replimat diner");
        GenSpawn.Spawn(diner, center + new IntVec3(4, 0, -4), map);
        FoodSearchE2EFixture.SetHunger(diner, 0.10f);
        diner.foodRestriction.CurrentFoodPolicy = replimatOnlyPolicy;

        plate = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Plate", ThingDefOf.Steel);
        cutlery = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Cutlery", ThingDefOf.Steel);
        competingPlate = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Plate", ThingDefOf.Steel);
        competingCutlery = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Cutlery", ThingDefOf.Steel);
        GenSpawn.Spawn(plate, center + new IntVec3(3, 0, -3), map);
        GenSpawn.Spawn(cutlery, center + new IntVec3(4, 0, -3), map);
        GenSpawn.Spawn(competingPlate, center + new IntVec3(-2, 0, -3), map);
        GenSpawn.Spawn(competingCutlery, center + new IntVec3(-1, 0, -3), map);
        FoodSearchE2EFixture.UseStrictNonEmergencyDining(context);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var fixture = new[]
        {
            diner.ThingID, competingDiner.ThingID, terminal.ThingID, tank.ThingID,
            computer.ThingID, plate.ThingID, cutlery.ThingID,
            competingPlate.ThingID, competingCutlery.ThingID
        };
        yield return new SelectionActionStep("select failed Replimat fixture", fixture, false);
        yield return new CameraActionStep("frame failed Replimat fixture", fixture, 220);
        yield return new ScreenshotStep("before failed Replimat request", Array.Empty<string>(), 0);
        yield return new TimeControlActionStep(
            "run failed Replimat request until ware is carried",
            false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the exact setting is carried before failure",
            _ => ObserveWareCarried(),
            new EndToEndDeadline(1_800, 5_000, TimeSpan.FromSeconds(60)));
        yield return new SelectionActionStep(
            "select diner carrying the clean setting",
            new[] { diner.ThingID },
            false);
        yield return new ScreenshotStep(
            "observe clean setting carried before failed dispense",
            Array.Empty<string>(),
            0);
        yield return new WaitUntilStep(
            "the first diner receives the only Replimat serving",
            _ => ObserveFirstDispense(),
            new EndToEndDeadline(1_800, 5_000, TimeSpan.FromSeconds(60)));
        yield return new SelectionActionStep(
            "select first diner with sole Replimat serving",
            new[] { competingDiner.ThingID },
            false);
        yield return new ScreenshotStep("observe the sole native Replimat serving", Array.Empty<string>(), 0);
        yield return new WaitUntilStep(
            "failed dispense returns the exact setting clean",
            _ => ObserveRollback(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause after failed Replimat rollback",
            true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep("failed Replimat request rolls back once", _ => AssertRolledBack());
        yield return new SelectionActionStep("select rolled-back Replimat plate", new[] { plate.ThingID }, false);
        yield return new CameraActionStep(
            "frame rolled-back Replimat setting",
            new[] { diner.ThingID, terminal.ThingID, plate.ThingID, cutlery.ThingID },
            220);
        yield return new ScreenshotStep("observe clean rolled-back Replimat plate", Array.Empty<string>(), 0);
        yield return new SelectionActionStep(
            "select rolled-back Replimat cutlery",
            new[] { cutlery.ThingID },
            false);
        yield return new ScreenshotStep("observe clean rolled-back Replimat cutlery", Array.Empty<string>(), 0);
        yield return new CheckpointStep(
            "failed Replimat rollback result",
            _ => new Dictionary<string, string>
            {
                ["wareCarriedBeforeFailure"] = wareCarriedBeforeFailure.ToString(),
                ["firstMealThingId"] = firstDispensedMeal?.ThingID ?? string.Empty,
                ["plateThingId"] = plate.ThingID,
                ["cutleryThingId"] = cutlery.ThingID,
                ["plateDirty"] = (plate.GetComp<CompSanitation>()?.IsDirty == true).ToString(),
                ["cutleryDirty"] = (cutlery.GetComp<CompSanitation>()?.IsDirty == true).ToString(),
                ["feedstockBefore"] = feedstockBefore.ToString("R"),
                ["feedstockAfterFirstDispense"] = feedstockAfterFirstDispense.ToString("R"),
                ["feedstockAfter"] = DispenserE2EFixture.ReadSingleProperty(tank, "StoredFeedstock").ToString("R")
            });
    }

    private bool ObserveWareCarried()
    {
        var session = DiningSessionRegistry.Current(diner);
        if (!ReferenceEquals(session?.CarriedPlate, plate) ||
            !ReferenceEquals(session.CarriedCutlery, cutlery))
        {
            return false;
        }

        wareCarriedBeforeFailure = true;
        return true;
    }

    private bool ObserveFirstDispense()
    {
        var meal = FindPawnReplimatMeal(competingDiner);
        var feedstock = DispenserE2EFixture.ReadSingleProperty(tank, "StoredFeedstock");
        if (meal is null || feedstock >= feedstockBefore)
        {
            return false;
        }

        firstDispensedMeal = meal;
        feedstockAfterFirstDispense = feedstock;
        return true;
    }

    private bool ObserveRollback()
    {
        return firstDispensedMeal is not null &&
               DiningSessionRegistry.Current(diner) is null &&
               plate.Spawned && plate.GetComp<CompSanitation>()?.IsDirty == false &&
               cutlery.Spawned && cutlery.GetComp<CompSanitation>()?.IsDirty == false &&
               !HasUnexpectedReplimatMeal();
    }

    private void AssertRolledBack()
    {
        EndToEndAssert.True(wareCarriedBeforeFailure && firstDispensedMeal is not null,
            "The test must observe carried ware and the competing native serving before rollback.");
        EndToEndAssert.True(DiningSessionRegistry.Current(diner) is null,
            "A failed native dispense must remove its dining session.");
        EndToEndAssert.True(plate.Spawned && plate.GetComp<CompSanitation>()?.IsDirty == false,
            "The same failed-dispense plate must return clean.");
        EndToEndAssert.True(cutlery.Spawned && cutlery.GetComp<CompSanitation>()?.IsDirty == false,
            "The same failed-dispense cutlery must return clean.");
        EndToEndAssert.Equal(
            feedstockAfterFirstDispense,
            DispenserE2EFixture.ReadSingleProperty(tank, "StoredFeedstock"),
            "The rejected second dispense must not consume additional Replimat feedstock.");
        EndToEndAssert.True(!HasUnexpectedReplimatMeal(),
            "The failed second dispense must not create another Replimat meal.");
    }

    private bool HasUnexpectedReplimatMeal()
    {
        var candidates = map.listerThings.AllThings.ToList();
        foreach (var pawn in new[] { diner, competingDiner })
        {
            if (pawn.carryTracker?.CarriedThing is { } carried)
            {
                candidates.Add(carried);
            }

            if (pawn.inventory?.innerContainer is { } inventory)
            {
                candidates.AddRange(inventory);
            }
        }

        return candidates
            .Distinct()
            .Any(thing =>
                !thing.Destroyed &&
                !ReferenceEquals(thing, firstDispensedMeal) &&
                MealClassificationCatalog.ReplimatMealDefNames.Contains(thing.def.defName));
    }

    private static Thing? FindPawnReplimatMeal(Pawn pawn)
    {
        var candidates = new List<Thing>();
        if (pawn.carryTracker?.CarriedThing is { } carried)
        {
            candidates.Add(carried);
        }

        if (pawn.inventory?.innerContainer is { } inventory)
        {
            candidates.AddRange(inventory);
        }

        if (pawn.CurJob is { } job)
        {
            foreach (var target in new[]
                     {
                         job.GetTarget(TargetIndex.A),
                         job.GetTarget(TargetIndex.B),
                         job.GetTarget(TargetIndex.C)
                     })
            {
                if (target.Thing is { } thing)
                {
                    candidates.Add(thing);
                }
            }
        }

        return candidates
            .Distinct()
            .FirstOrDefault(thing =>
                !thing.Destroyed &&
                MealClassificationCatalog.ReplimatMealDefNames.Contains(thing.def.defName));
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.meal-printer-fine-dining",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "Orion.Hospitality",
    "Mlie.MealPrinter",
    "Orion.CashRegister",
    "Orion.Gastronomy",
    "fumblesneeze.immersivechefs",
    MaxFrames = 6_000,
    MaxGameTicks = 20_000,
    MaxWallClockSeconds = 180)]
public sealed class MealPrinterFineDiningTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn diner = null!;
    private ThingWithComps printer = null!;
    private ThingWithComps hopper = null!;
    private Thing feedstock = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private ThingWithComps? printedMeal;
    private int feedstockBefore;
    private int feedstockAfter;
    private bool wareCarriedBeforeOutput;

    public void Arrange(IEndToEndContext context)
    {
        var fixture = DispenserE2EFixture.CreateMealPrinterFixture("MealFine", "Fine meal printer diner");
        map = fixture.Map;
        diner = fixture.Diner;
        printer = fixture.Printer;
        hopper = fixture.Hopper;
        feedstock = fixture.Feedstock;
        feedstockBefore = feedstock.stackCount;

        plate = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Plate", ThingDefOf.Steel);
        cutlery = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Cutlery", ThingDefOf.Steel);
        GenSpawn.Spawn(plate, fixture.WareCell + IntVec3.West, map);
        GenSpawn.Spawn(cutlery, fixture.WareCell + IntVec3.East, map);
        FoodSearchE2EFixture.UseStrictNonEmergencyDining(context);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var fixture = new[] { diner.ThingID, printer.ThingID, hopper.ThingID, plate.ThingID, cutlery.ThingID };
        yield return new SelectionActionStep("select the native Meal Printer fixture", fixture, additive: false);
        yield return new CameraActionStep("frame the native Meal Printer fixture", fixture, paddingPixels: 220);
        yield return new ScreenshotStep("before native Fine printer dining", Array.Empty<string>(), 0);
        yield return new TimeControlActionStep(
            "run ordinary Fine printer food search slowly",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "ordinary hunger selects the configured Meal Printer",
            _ => ObservePrinterJob(),
            new EndToEndDeadline(1_800, 5_000, TimeSpan.FromSeconds(60)));
        yield return new WaitUntilStep(
            "the exact Fine setting is carried before printing",
            _ => ObserveWareBeforeOutput(),
            new EndToEndDeadline(1_800, 5_000, TimeSpan.FromSeconds(60)));
        yield return new ScreenshotStep("observe Fine setting collected before printing", Array.Empty<string>(), 0);
        yield return new WaitUntilStep(
            "Meal Printer creates the plated Fine meal",
            _ => ObservePrintedMeal(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause on the printed Fine meal",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select diner carrying printed Fine meal",
            new[] { diner.ThingID },
            additive: false);
        yield return new ScreenshotStep("observe printed Fine meal with exact plate", Array.Empty<string>(), 0);
        yield return new TimeControlActionStep(
            "complete printed Fine meal ingestion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "printed Fine meal returns the same dirty setting",
            _ => printedMeal?.Destroyed == true &&
                 plate.Spawned && plate.GetComp<CompSanitation>()?.IsDirty == true &&
                 cutlery.Spawned && cutlery.GetComp<CompSanitation>()?.IsDirty == true,
            new EndToEndDeadline(3_600, 16_000, TimeSpan.FromSeconds(105)));
        yield return new TimeControlActionStep("pause after printed Fine dining", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep("Meal Printer Fine lifecycle completes once", _ => AssertCompleted());
        yield return new SelectionActionStep("select returned printer plate", new[] { plate.ThingID }, false);
        yield return new CameraActionStep(
            "frame returned printer setting",
            new[] { diner.ThingID, printer.ThingID, plate.ThingID, cutlery.ThingID },
            220);
        yield return new ScreenshotStep("observe returned dirty printer plate", Array.Empty<string>(), 0);
        yield return new SelectionActionStep("select returned printer cutlery", new[] { cutlery.ThingID }, false);
        yield return new ScreenshotStep("observe returned dirty printer cutlery", Array.Empty<string>(), 0);
        yield return new CheckpointStep(
            "Meal Printer Fine dining result",
            _ => new Dictionary<string, string>
            {
                ["wareCarriedBeforeOutput"] = wareCarriedBeforeOutput.ToString(),
                ["mealThingId"] = printedMeal?.ThingID ?? "missing",
                ["plateThingId"] = plate.ThingID,
                ["cutleryThingId"] = cutlery.ThingID,
                ["feedstockBefore"] = feedstockBefore.ToString(),
                ["feedstockAfter"] = feedstockAfter.ToString()
            });
    }

    private bool ObservePrinterJob()
    {
        if (diner.CurJobDef != JobDefOf.Ingest)
        {
            return false;
        }

        EndToEndAssert.True(ReferenceEquals(diner.CurJob?.GetTarget(TargetIndex.A).Thing, printer),
            "The ordinary hunger job must target the configured Meal Printer.");
        var session = DiningSessionRegistry.Current(diner);
        EndToEndAssert.NotNull(session, "A configured Fine printer must create one dining session.");
        EndToEndAssert.True(ReferenceEquals(session!.ReservedPlate, plate) && ReferenceEquals(session.Cutlery, cutlery),
            "The Fine printer job must reserve the exact steel plate and cutlery.");
        return true;
    }

    private bool ObserveWareBeforeOutput()
    {
        var session = DiningSessionRegistry.Current(diner);
        if (!ReferenceEquals(session?.CarriedPlate, plate) || !ReferenceEquals(session.CarriedCutlery, cutlery))
        {
            return false;
        }

        EndToEndAssert.True(
            DispenserE2EFixture.FindMeal(diner, map, thing => thing.def == ThingDefOf.MealFine) is null,
            "Meal Printer must not create the Fine meal before the exact setting is carried.");
        wareCarriedBeforeOutput = true;
        return true;
    }

    private bool ObservePrintedMeal()
    {
        printedMeal ??= DispenserE2EFixture.FindMeal(diner, map, thing => thing.def == ThingDefOf.MealFine);
        if (printedMeal is null)
        {
            return false;
        }

        var embedded = printedMeal.GetComp<CompEmbeddedWare>();
        EndToEndAssert.Equal(1, embedded?.EmbeddedPlateCount ?? -1,
            "The native printed Fine meal must contain exactly one plate.");
        EndToEndAssert.True(ReferenceEquals(plate, embedded?.PeekPlateThing()),
            "The native printed Fine meal must contain the same physical steel plate.");
        feedstockAfter = feedstock.Destroyed ? 0 : feedstock.stackCount;
        EndToEndAssert.True(feedstockAfter < feedstockBefore,
            "Only the native Meal Printer path may consume the hopper feedstock.");
        return true;
    }

    private void AssertCompleted()
    {
        EndToEndAssert.True(wareCarriedBeforeOutput, "The exact setting must be observed before output creation.");
        EndToEndAssert.NotNull(printedMeal, "Meal Printer must create one real Fine meal Thing.");
        EndToEndAssert.True(printedMeal!.Destroyed, "The native printed Fine meal must be consumed.");
        EndToEndAssert.True(plate.Spawned && plate.GetComp<CompSanitation>()?.IsDirty == true,
            "The exact printed-meal plate must return dirty.");
        EndToEndAssert.True(cutlery.Spawned && cutlery.GetComp<CompSanitation>()?.IsDirty == true,
            "The exact printed-meal cutlery must return dirty.");
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.meal-printer-gastronomy-guest-service",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "Orion.Hospitality",
    "Mlie.MealPrinter",
    "Orion.CashRegister",
    "Orion.Gastronomy",
    "fumblesneeze.immersivechefs",
    MaxFrames = 14_400,
    MaxGameTicks = 56_000,
    MaxWallClockSeconds = 360)]
public sealed class MealPrinterGastronomyGuestServiceTest : IRimWorldEndToEndTest
{
    private const string SaveName = "ImmersiveChefs_MealPrinterGastronomyClearing";
    private Map map = null!;
    private Pawn producer = null!;
    private Pawn guest = null!;
    private Pawn waiter = null!;
    private ThingWithComps printer = null!;
    private ThingWithComps hopper = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private ThingWithComps personalCutlery = null!;
    private ThingWithComps dishwasher = null!;
    private ThingWithComps diningTable = null!;
    private ThingWithComps cashRegister = null!;
    private ThingWithComps? printedMeal;
    private Thing? diningSpot;
    private object restaurant = null!;
    private EndToEndGizmoOption draftGizmo = null!;
    private bool printerWareObserved;
    private bool printedMealReleased;
    private bool nativeDineObserved;
    private bool nativeServeObserved;
    private bool serviceSessionObserved;
    private bool dirtyReturnObserved;
    private bool waiterCleanupObserved;
    private bool waiterQueuedSecondWareObserved;
    private bool waiterClearingPersistenceObserved;
    private int nativeServeStartedTick = -1;
    private string producerId = string.Empty;
    private string guestId = string.Empty;
    private string waiterId = string.Empty;
    private string dishwasherId = string.Empty;
    private string plateId = string.Empty;
    private string cutleryId = string.Empty;
    private string personalCutleryId = string.Empty;

    public void Arrange(IEndToEndContext context)
    {
        var savePath = GenFilePaths.FilePathForSavedGame(SaveName);
        context.DeferCleanup(() =>
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
        });

        var printerFixture = DispenserE2EFixture.CreateMealPrinterFixture(
            "MealFine",
            "Printer service cook");
        map = printerFixture.Map;
        producer = printerFixture.Diner;
        printer = printerFixture.Printer;
        hopper = printerFixture.Hopper;
        FoodSearchE2EFixture.SetHunger(producer, 1f);
        producer.jobs.StopAll();

        plate = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Plate", ThingDefOf.Steel);
        cutlery = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Cutlery", ThingDefOf.Steel);
        GenSpawn.Spawn(plate, printerFixture.WareCell + IntVec3.West, map);
        GenSpawn.Spawn(cutlery, printerFixture.WareCell + IntVec3.East, map);

        var center = printer.Position + new IntVec3(1, 0, 2);
        var service = DispenserE2EFixture.CreateGastronomyGuestServiceFixture(map, center);
        guest = service.Guest;
        waiter = service.Waiter;
        diningTable = service.DiningTable;
        diningSpot = service.DiningSpot;
        cashRegister = service.CashRegister;
        dishwasher = service.Dishwasher;
        restaurant = service.Restaurant;

        personalCutlery = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cutlery",
            ThingDefOf.Steel);
        EndToEndAssert.True(
            guest.inventory?.innerContainer.TryAdd(personalCutlery, canMergeWithExistingStacks: false) == true,
            "The arrived Hospitality guest must retain one exact personal cutlery setting.");
        EndToEndAssert.True(
            HospitalityAdapter.IsArrivedGuest(guest),
            "The real Hospitality registry must recognize the service fixture guest as arrived.");

        FoodSearchE2EFixture.SetHunger(guest, 1f);
        FoodSearchE2EFixture.SetHunger(waiter, 1f);
        FoodSearchE2EFixture.SetHunger(producer, 0.10f);
        FoodSearchE2EFixture.UseStrictNonEmergencyDining(context);

        producerId = producer.ThingID;
        guestId = guest.ThingID;
        waiterId = waiter.ThingID;
        dishwasherId = dishwasher.ThingID;
        plateId = plate.ThingID;
        cutleryId = cutlery.ThingID;
        personalCutleryId = personalCutlery.ThingID;

        var gizmos = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { producer.ThingID }, Array.Empty<string>());
        var candidates = gizmos.Where(option =>
            !option.Disabled &&
            option.Interaction == EndToEndGizmoInteraction.Toggle &&
            option.ToggleState == false &&
            string.Equals(option.HotKeyDefName, "Command_ColonistDraft", StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(
            1,
            candidates.Length,
            "The printer cook must expose one enabled native Draft toggle.");
        draftGizmo = candidates[0];
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var fixture = new[]
        {
            producer.ThingID, guest.ThingID, waiter.ThingID, printer.ThingID, hopper.ThingID,
            diningTable.ThingID, cashRegister.ThingID, dishwasher.ThingID,
            plate.ThingID, cutlery.ThingID
        };
        yield return new SelectionActionStep(
            "select the printer service cook",
            new[] { producer.ThingID },
            false);
        yield return new CameraActionStep("frame the printer restaurant fixture", fixture, 240);
        yield return new ScreenshotStep("before native printer restaurant service", Array.Empty<string>(), 0);
        yield return new TimeControlActionStep(
            "run ordinary Fine printer preparation",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the printer cook carries the exact setting before output",
            _ => ObservePrinterWare(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(75)));
        yield return new WaitUntilStep(
            "Meal Printer creates the exact plated Fine meal for service",
            _ => ObservePrintedMeal(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause on the native printed service meal",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select cook carrying the printed service meal",
            new[] { producer.ThingID },
            false);
        yield return new ScreenshotStep(
            "observe exact plated meal before restaurant handoff",
            Array.Empty<string>(),
            0);
        yield return new GizmoActionStep(
            "draft the cook to release the printed restaurant meal",
            new[] { producer.ThingID },
            draftGizmo.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draftGizmo.StableId);
        yield return new TimeControlActionStep(
            "allow the interrupted printer job to release its meal",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the exact printed meal and clean cutlery return to restaurant stock",
            _ => ObserveReleasedPrinterSetting(),
            new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(45)));
        yield return new AssertionStep(
            "place the released meal on a real register field and refresh native stock",
            _ => DispenserE2EFixture.PlaceInNativeRestaurantStock(
                printedMeal!,
                cashRegister,
                restaurant));
        yield return new AssertionStep(
            "arm native arrived-guest dining and exact waiter service",
            _ => AssertRestaurantAvailableAndArmService());
        yield return new WaitUntilStep(
            "the arrived guest selects native Gastronomy dining",
            _ => ObserveNativeDine(),
            new EndToEndDeadline(3_600, 12_000, TimeSpan.FromSeconds(105)));
        yield return new TimeControlActionStep(
            "observe native waiter service at normal speed",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the assigned waiter serves the exact printed meal",
            _ => ObserveNativeServe(),
            new EndToEndDeadline(3_600, 12_000, TimeSpan.FromSeconds(105)));
        yield return new SelectionActionStep(
            "select waiter serving the printed Fine meal",
            new[] { waiter.ThingID },
            false);
        yield return new CameraActionStep(
            "frame native waiter and Hospitality guest",
            new[] { waiter.ThingID, guest.ThingID, diningTable.ThingID },
            200);
        yield return new ScreenshotStep(
            "observe native Gastronomy waiter service",
            Array.Empty<string>(),
            0);
        yield return new WaitUntilStep(
            "the native waiter transfers the exact printed meal to the guest",
            _ => ObserveNativeMealTransfer(),
            new EndToEndDeadline(3_600, 12_000, TimeSpan.FromSeconds(105)));
        yield return new AssertionStep(
            "the waiter delivers exact colony cutlery with the printed meal",
            _ => AssertServedDiningSession());
        yield return new SelectionActionStep(
            "select arrived guest dining with served tableware",
            new[] { guest.ThingID },
            false);
        yield return new ScreenshotStep(
            "observe Hospitality guest dining on the printed meal",
            Array.Empty<string>(),
            0);
        yield return new TimeControlActionStep(
            "allow the served Hospitality guest to finish eating",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "served dining returns the exact dirty setting",
            _ => ObserveDirtyReturn(),
            new EndToEndDeadline(4_800, 18_000, TimeSpan.FromSeconds(120)));
        yield return new WaitUntilStep(
            "the waiter carries one returned item and queues the other",
            _ => ObserveWaiterCleanup(),
            new EndToEndDeadline(3_600, 12_000, TimeSpan.FromSeconds(105)));
        yield return new TimeControlActionStep(
            "pause on waiter-owned dish clearing",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select waiter clearing the guest setting",
            new[] { waiter.ThingID },
            false);
        yield return new CameraActionStep(
            "frame waiter clearing the guest setting",
            new[] { waiter.ThingID, dishwasher.ThingID },
            200);
        yield return new ScreenshotStep(
            "observe waiter carrying and queuing returned ware",
            Array.Empty<string>(),
            0);
        yield return new SaveLoadActionStep(
            "save and load while the waiter carries one returned item and owns the queued second item",
            SaveName);
        yield return new AssertionStep(
            "resolve the loaded waiter setting and restore exact clearing ownership",
            _ =>
            {
                ResolveLoadedClearingFixture();
                AssertLoadedClearingOwnership();
            });
        yield return new SelectionActionStep(
            "select loaded waiter retaining the exact returned setting",
            new[] { waiterId },
            false);
        yield return new CameraActionStep(
            "frame loaded waiter and dishwasher after native persistence",
            new[] { waiterId, dishwasherId },
            200);
        yield return new ScreenshotStep(
            "observe loaded waiter retaining active and queued dish clearing",
            Array.Empty<string>(),
            0);
        yield return new TimeControlActionStep(
            "finish waiter dishwasher admission",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "dishwasher contains the exact served setting",
            _ => DishwasherContainsExactSetting(),
            new EndToEndDeadline(3_600, 12_000, TimeSpan.FromSeconds(105)));
        yield return new TimeControlActionStep(
            "pause on exact served setting in dishwasher",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "printer, Hospitality, Gastronomy, and clearing ownership compose once",
            _ => AssertCompleted());
        yield return new SelectionActionStep(
            "select dishwasher holding the served setting",
            new[] { dishwasher.ThingID },
            false);
        yield return new ScreenshotStep(
            "observe exact served setting loading in dishwasher",
            Array.Empty<string>(),
            0);
        yield return new CheckpointStep(
            "Meal Printer Gastronomy guest service result",
            _ => new Dictionary<string, string>
            {
                ["printerWareObserved"] = printerWareObserved.ToString(),
                ["printedMealReleased"] = printedMealReleased.ToString(),
                ["nativeDineObserved"] = nativeDineObserved.ToString(),
                ["nativeServeObserved"] = nativeServeObserved.ToString(),
                ["serviceSessionObserved"] = serviceSessionObserved.ToString(),
                ["dirtyReturnObserved"] = dirtyReturnObserved.ToString(),
                ["waiterCleanupObserved"] = waiterCleanupObserved.ToString(),
                ["waiterQueuedSecondWareObserved"] = waiterQueuedSecondWareObserved.ToString(),
                ["waiterClearingPersistenceObserved"] = waiterClearingPersistenceObserved.ToString(),
                ["dishwasherUsedCapacity"] =
                    dishwasher.GetComp<CompDishwasher>()?.UsedCapacity.ToString("R") ?? "missing",
                ["guestPersonalCutleryDirty"] =
                    (personalCutlery.GetComp<CompSanitation>()?.IsDirty == true).ToString()
            });
    }

    private bool ObservePrinterWare()
    {
        var session = DiningSessionRegistry.Current(producer);
        if (!ReferenceEquals(session?.CarriedPlate, plate) ||
            !ReferenceEquals(session.CarriedCutlery, cutlery))
        {
            return false;
        }

        EndToEndAssert.True(
            DispenserE2EFixture.FindMeal(producer, map, thing => thing.def == ThingDefOf.MealFine) is null,
            "The printer must not create the service meal before carrying the exact setting.");
        printerWareObserved = true;
        return true;
    }

    private bool ObservePrintedMeal()
    {
        printedMeal ??= DispenserE2EFixture.FindMeal(
            producer,
            map,
            thing => thing.def == ThingDefOf.MealFine);
        if (printedMeal is null)
        {
            return false;
        }

        var embedded = printedMeal.GetComp<CompEmbeddedWare>();
        EndToEndAssert.True(
            ReferenceEquals(embedded?.PeekPlateThing(), plate),
            "The real printed Fine meal must contain the exact steel plate before service.");
        return true;
    }

    private bool ObserveReleasedPrinterSetting()
    {
        if (producer.drafter?.Drafted != true || printedMeal?.Spawned != true ||
            !cutlery.Spawned || cutlery.GetComp<CompSanitation>()?.IsDirty == true ||
            DiningSessionRegistry.Current(producer) is not null)
        {
            return false;
        }

        EndToEndAssert.True(
            ReferenceEquals(printedMeal.GetComp<CompEmbeddedWare>()?.PeekPlateThing(), plate),
            "Cancelling preparation must leave the exact plate embedded in the printed meal.");
        printedMealReleased = true;
        return true;
    }

    private bool ObserveNativeDine()
    {
        if (guest.CurJobDef?.defName != "Gastronomy_Dine")
        {
            return false;
        }

        EndToEndAssert.True(
            ReferenceEquals(guest.CurJob?.GetTarget(TargetIndex.A).Thing, diningSpot),
            "The arrived guest's ordinary hunger job must target the real Gastronomy dining spot.");
        nativeDineObserved = true;
        return true;
    }

    private void AssertRestaurantAvailableAndArmService()
    {
        EndToEndAssert.True(
            guest.WillEat(printedMeal!.def),
            "The arrived guest must be willing to eat the exact native printed Fine meal.");
        FoodSearchE2EFixture.SetHunger(guest, 0.10f);
        EndToEndAssert.True(
            guest.needs?.food?.CurLevelPercentage <= 0.11f,
            "The isolated scenario must arm ordinary guest hunger only after exact stock is available.");
        DispenserE2EFixture.StartNativeGastronomyDining(guest, diningSpot!, restaurant);
        DispenserE2EFixture.StartNativeGastronomyService(
            waiter,
            guest,
            printedMeal!,
            restaurant);
    }

    private bool ObserveNativeServe()
    {
        if (waiter.CurJobDef?.defName != "Gastronomy_Serve")
        {
            return false;
        }

        EndToEndAssert.True(
            ReferenceEquals(waiter.CurJob?.GetTarget(TargetIndex.A).Pawn, guest) &&
            ReferenceEquals(waiter.CurJob?.GetTarget(TargetIndex.B).Thing, printedMeal),
            "The native waiter job must target the arrived guest and exact printed meal.");
        nativeServeObserved = true;
        if (nativeServeStartedTick < 0)
        {
            nativeServeStartedTick = Find.TickManager.TicksGame;
        }
        return true;
    }

    private bool ObserveNativeMealTransfer()
    {
        var guestOwnsMeal = guest.inventory?.innerContainer.Contains(printedMeal) == true ||
                            ReferenceEquals(guest.carryTracker?.CarriedThing, printedMeal);
        if (!guestOwnsMeal)
        {
            if (nativeServeStartedTick >= 0 &&
                Find.TickManager.TicksGame - nativeServeStartedTick >= 1_200)
            {
                throw new EndToEndAssertionException(ServiceStallDiagnostics());
            }

            return false;
        }

        Find.TickManager.Pause();
        return true;
    }

    private string ServiceStallDiagnostics()
    {
        static string Holder(Thing thing) =>
            thing.Spawned
                ? $"map:{thing.Position}"
                : thing.ParentHolder?.GetType().FullName ?? "none";

        return "Native Gastronomy service did not transfer the exact meal after 1,200 ticks. " +
               $"WaiterJob={waiter.CurJobDef?.defName ?? "none"}; " +
               $"WaiterPosition={waiter.Position}; WaiterMoving={waiter.pather?.Moving == true}; " +
               $"WaiterCarry={waiter.carryTracker?.CarriedThing?.ThingID ?? "none"}; " +
               $"WaiterHasCutlery={waiter.inventory?.innerContainer.Contains(cutlery) == true}; " +
               $"GuestJob={guest.CurJobDef?.defName ?? "none"}; " +
               $"GuestPosition={guest.Position}; GuestMoving={guest.pather?.Moving == true}; " +
               $"DiningSpot={diningSpot?.Position.ToString() ?? "none"}; " +
               $"MealHolder={Holder(printedMeal!)}; CutleryHolder={Holder(cutlery)}.";
    }

    private void AssertServedDiningSession()
    {
        var session = DiningSessionRegistry.Current(guest);
        EndToEndAssert.NotNull(
            session,
            "The native Gastronomy transfer must retain the guest's Immersive Chefs dining session.");
        EndToEndAssert.True(
            ReferenceEquals(session!.CarriedCutlery, cutlery),
            "The served dining session must retain the exact colony cutlery Thing. " +
            $"SessionCutlery={session.CarriedCutlery?.ThingID ?? "none"}; " +
            $"ExpectedHolder={(cutlery.Spawned ? $"map:{cutlery.Position}" : cutlery.ParentHolder?.GetType().FullName ?? "none")}; " +
            $"GuestHasExpected={guest.inventory?.innerContainer.Contains(cutlery) == true}; " +
            $"WaiterHasExpected={waiter.inventory?.innerContainer.Contains(cutlery) == true}; " +
            $"SessionServer={session.ServingPawn?.ThingID ?? "none"}; " +
            $"PersonalCutlery={personalCutlery.ThingID}.");
        EndToEndAssert.True(
            ReferenceEquals(
                printedMeal!.GetComp<CompEmbeddedWare>()?.PeekPlateThing(),
                plate),
            "The served printed meal must retain its exact embedded plate until ingestion captures it.");
        EndToEndAssert.True(
            ReferenceEquals(session.ServingPawn, waiter),
            "The served dining session must retain the exact native Gastronomy waiter.");
        var guestInventory = guest.inventory?.innerContainer;
        EndToEndAssert.NotNull(
            guestInventory,
            "The arrived Hospitality guest must retain a native inventory tracker.");
        EndToEndAssert.True(
            (guestInventory!.Contains(printedMeal) ||
             ReferenceEquals(guest.carryTracker?.CarriedThing, printedMeal)) &&
            guestInventory.Contains(cutlery),
            "The guest must carry the exact native meal while its inventory retains delivered colony cutlery.");

        EndToEndAssert.True(
            guestInventory.Contains(personalCutlery) &&
            personalCutlery.GetComp<CompSanitation>()?.IsDirty == false,
            "Waiter service must prefer colony cutlery without taking or dirtying the guest's personal setting.");
        serviceSessionObserved = true;
    }

    private bool ObserveDirtyReturn()
    {
        if (printedMeal?.Destroyed != true || !plate.Spawned || !cutlery.Spawned ||
            plate.GetComp<CompSanitation>()?.IsDirty != true ||
            cutlery.GetComp<CompSanitation>()?.IsDirty != true)
        {
            return false;
        }

        dirtyReturnObserved = true;
        return true;
    }

    private bool ObserveWaiterCleanup()
    {
        if (waiter.CurJobDef != ImmersiveChefsDefOf.ImmersiveChefs_DoDishes ||
            !ReferenceEquals(waiter.CurJob?.GetTarget(TargetIndex.B).Thing, dishwasher))
        {
            return false;
        }

        var carried = waiter.carryTracker?.CarriedThing;
        if (!ReferenceEquals(carried, plate) && !ReferenceEquals(carried, cutlery))
        {
            return false;
        }

        var otherWare = ReferenceEquals(carried, plate) ? cutlery : plate;
        var queuedOtherWare = waiter.jobs.jobQueue
            .Where(queued =>
                queued.job.def == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
                ReferenceEquals(queued.job.GetTarget(TargetIndex.A).Thing, otherWare) &&
                ReferenceEquals(queued.job.GetTarget(TargetIndex.B).Thing, dishwasher))
            .ToArray();
        EndToEndAssert.Equal(
            1,
            queuedOtherWare.Length,
            "Gastronomy clearing must queue the other exact returned item for the same dishwasher.");
        waiterCleanupObserved = true;
        waiterQueuedSecondWareObserved = true;
        return true;
    }

    private bool DishwasherContainsExactSetting()
    {
        var contents = dishwasher.GetComp<CompDishwasher>()?.GetDirectlyHeldThings();
        return contents is not null && contents.Contains(plate) && contents.Contains(cutlery);
    }

    private void ResolveLoadedClearingFixture()
    {
        map = Current.Game.CurrentMap;
        producer = ResolveLoadedPawn(producerId);
        guest = ResolveLoadedPawn(guestId);
        waiter = ResolveLoadedPawn(waiterId);
        dishwasher = ResolveLoadedSpawnedThing<ThingWithComps>(dishwasherId);
        plate = ResolveLoadedWare(plateId);
        cutlery = ResolveLoadedWare(cutleryId);
        personalCutlery = ResolveLoadedWare(personalCutleryId);
    }

    private Pawn ResolveLoadedPawn(string thingId) =>
        map.mapPawns.AllPawns.SingleOrDefault(pawn => pawn.ThingID == thingId) ??
        throw new EndToEndAssertionException(
            "Native save/load lost exact pawn " + thingId + ".");

    private T ResolveLoadedSpawnedThing<T>(string thingId) where T : Thing =>
        map.listerThings.AllThings.OfType<T>()
            .SingleOrDefault(thing => thing.ThingID == thingId) ??
        throw new EndToEndAssertionException(
            "Native save/load lost exact spawned Thing " + thingId + ".");

    private ThingWithComps ResolveLoadedWare(string thingId)
    {
        var spawned = map.listerThings.AllThings.OfType<ThingWithComps>()
            .SingleOrDefault(thing => thing.ThingID == thingId);
        if (spawned is not null)
        {
            return spawned;
        }

        foreach (var pawn in map.mapPawns.AllPawns)
        {
            if (pawn.carryTracker?.CarriedThing is ThingWithComps carried &&
                carried.ThingID == thingId)
            {
                return carried;
            }

            var inventoryThing = pawn.inventory?.innerContainer.OfType<ThingWithComps>()
                .SingleOrDefault(thing => thing.ThingID == thingId);
            if (inventoryThing is not null)
            {
                return inventoryThing;
            }
        }

        var dishwasherThing = dishwasher.GetComp<CompDishwasher>()?.GetDirectlyHeldThings()
            .OfType<ThingWithComps>()
            .SingleOrDefault(thing => thing.ThingID == thingId);
        return dishwasherThing ?? throw new EndToEndAssertionException(
            "Native save/load lost exact returned ware " + thingId + ".");
    }

    private void AssertLoadedClearingOwnership()
    {
        EndToEndAssert.True(
            plate.GetComp<CompSanitation>()?.IsDirty == true &&
            cutlery.GetComp<CompSanitation>()?.IsDirty == true,
            "Native save/load must preserve both returned items as dirty.");
        EndToEndAssert.True(
            waiter.CurJobDef == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
            ReferenceEquals(waiter.CurJob?.targetB.Thing, dishwasher),
            "Native save/load must preserve the waiter's active exact dishwasher job.");

        var currentWare = waiter.CurJob?.targetA.Thing;
        EndToEndAssert.True(
            ReferenceEquals(currentWare, plate) || ReferenceEquals(currentWare, cutlery),
            "The loaded active dish job must retain one exact returned item.");
        var queuedWare = ReferenceEquals(currentWare, plate) ? cutlery : plate;
        EndToEndAssert.Equal(
            1,
            waiter.jobs.jobQueue.Count(queued =>
                queued.job.def == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
                ReferenceEquals(queued.job.targetA.Thing, queuedWare) &&
                ReferenceEquals(queued.job.targetB.Thing, dishwasher)),
            "Native save/load must retain exactly one queued job for the other returned item.");
        EndToEndAssert.True(
            map.reservationManager.ReservedBy(plate, waiter) &&
            map.reservationManager.ReservedBy(cutlery, waiter),
            "Native save/load must retain native waiter reservations for both exact returned items.");
        EndToEndAssert.True(
            map.GetComponent<MapComponent_GastronomyDishClearing>().OwnsExactWare(waiter, plate) &&
            map.GetComponent<MapComponent_GastronomyDishClearing>().OwnsExactWare(waiter, cutlery),
            "Native save/load must restore both Gastronomy clearing claims.");
        waiterClearingPersistenceObserved = true;
    }

    private void AssertCompleted()
    {
        EndToEndAssert.True(
            printerWareObserved && printedMealReleased && nativeDineObserved && nativeServeObserved &&
            serviceSessionObserved && dirtyReturnObserved && waiterCleanupObserved &&
            waiterQueuedSecondWareObserved && waiterClearingPersistenceObserved,
            "The test must observe each native printer, guest, waiter, and clearing boundary.");
        EndToEndAssert.True(
            DishwasherContainsExactSetting() &&
            Math.Abs((dishwasher.GetComp<CompDishwasher>()?.UsedCapacity ?? -1f) - 1.25f) < 0.001f,
            "The exact served plate and cutlery must occupy 1.25 dishwasher capacity.");
        EndToEndAssert.True(
            guest.inventory?.innerContainer.Contains(personalCutlery) == true &&
            personalCutlery.GetComp<CompSanitation>()?.IsDirty == false &&
            personalCutlery.GetComp<CompSanitation>()?.IsPersonalDiningWareFor(guest) == false &&
            personalCutlery.GetComp<CompSanitation>()?.ReturnToMapAfterInterruptedSession == false,
            "The arrived guest must retain its exact clean personal cutlery after colony waiter service.");
        EndToEndAssert.False(
            guest.inventory?.innerContainer.Contains(plate) == true ||
            guest.inventory?.innerContainer.Contains(cutlery) == true,
            "Gastronomy-served colony plate and cutlery must never become the arrived guest's property.");
        EndToEndAssert.False(
            cutlery.GetComp<CompSanitation>()?.ReturnToMapAfterInterruptedSession == true,
            "Completed waiter service must clear the colony cutlery's transient recovery marker.");
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.meal-printer-nutribar-exclusion",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "Orion.Hospitality",
    "Mlie.MealPrinter",
    "Orion.CashRegister",
    "Orion.Gastronomy",
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_800,
    MaxGameTicks = 16_000,
    MaxWallClockSeconds = 150)]
public sealed class MealPrinterNutriBarExclusionTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn diner = null!;
    private ThingWithComps printer = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private ThingWithComps? nutriBar;
    private bool printerJobObserved;

    public void Arrange(IEndToEndContext context)
    {
        var fixture = DispenserE2EFixture.CreateMealPrinterFixture(
            "MealPrinter_NutriBar",
            "NutriBar printer diner");
        map = fixture.Map;
        diner = fixture.Diner;
        printer = fixture.Printer;
        plate = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Plate", ThingDefOf.Gold);
        cutlery = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Cutlery", ThingDefOf.Gold);
        GenSpawn.Spawn(plate, fixture.WareCell + IntVec3.West, map);
        GenSpawn.Spawn(cutlery, fixture.WareCell + IntVec3.East, map);
        FoodSearchE2EFixture.UseStrictNonEmergencyDining(context);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var fixture = new[] { diner.ThingID, printer.ThingID, plate.ThingID, cutlery.ThingID };
        yield return new SelectionActionStep("select the NutriBar exclusion fixture", fixture, false);
        yield return new CameraActionStep("frame the NutriBar exclusion fixture", fixture, 220);
        yield return new ScreenshotStep("before native NutriBar food search", Array.Empty<string>(), 0);
        yield return new TimeControlActionStep("run native NutriBar food search", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "ordinary hunger selects the NutriBar printer without tableware",
            _ => ObserveExcludedJob(),
            new EndToEndDeadline(1_800, 5_000, TimeSpan.FromSeconds(60)));
        yield return new WaitUntilStep(
            "native printer creates a handheld NutriBar",
            _ => ObserveNutriBar(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep("pause on handheld NutriBar", true, EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep("select NutriBar diner", new[] { diner.ThingID }, false);
        yield return new ScreenshotStep("observe handheld NutriBar with untouched setting", Array.Empty<string>(), 0);
        yield return new TimeControlActionStep("complete NutriBar ingestion", false, EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "NutriBar is consumed while decoy ware remains clean",
            _ => nutriBar?.Destroyed == true,
            new EndToEndDeadline(3_000, 12_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep("pause after NutriBar ingestion", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep("NutriBar exclusion remains hand-eaten", _ => AssertExcluded());
        yield return new SelectionActionStep("select untouched NutriBar plate", new[] { plate.ThingID }, false);
        yield return new ScreenshotStep("observe untouched clean NutriBar plate", Array.Empty<string>(), 0);
        yield return new SelectionActionStep("select untouched NutriBar cutlery", new[] { cutlery.ThingID }, false);
        yield return new ScreenshotStep("observe untouched clean NutriBar cutlery", Array.Empty<string>(), 0);
        yield return new CheckpointStep(
            "Meal Printer NutriBar exclusion result",
            _ => new Dictionary<string, string>
            {
                ["printerJobObserved"] = printerJobObserved.ToString(),
                ["nutriBarThingId"] = nutriBar?.ThingID ?? "missing",
                ["diningSession"] = (DiningSessionRegistry.Current(diner) is not null).ToString(),
                ["plateDirty"] = (plate.GetComp<CompSanitation>()?.IsDirty == true).ToString(),
                ["cutleryDirty"] = (cutlery.GetComp<CompSanitation>()?.IsDirty == true).ToString()
            });
    }

    private bool ObserveExcludedJob()
    {
        if (diner.CurJobDef != JobDefOf.Ingest)
        {
            return false;
        }

        EndToEndAssert.True(ReferenceEquals(diner.CurJob?.GetTarget(TargetIndex.A).Thing, printer),
            "The ordinary hunger job must target the configured NutriBar printer.");
        EndToEndAssert.True(DiningSessionRegistry.Current(diner) is null,
            "A NutriBar printer job must not create a plate or cutlery dining session.");
        EndToEndAssert.True(plate.Spawned && cutlery.Spawned,
            "The clean decoy setting must remain on the map when the NutriBar job starts.");
        printerJobObserved = true;
        return true;
    }

    private bool ObserveNutriBar()
    {
        nutriBar ??= DispenserE2EFixture.FindMeal(
            diner,
            map,
            thing => thing.def.defName == "MealPrinter_NutriBar");
        if (nutriBar is null)
        {
            return false;
        }

        EndToEndAssert.True(DiningSessionRegistry.Current(diner) is null,
            "The handheld NutriBar must remain outside the dining lifecycle after printing.");
        EndToEndAssert.True(plate.Spawned && cutlery.Spawned,
            "Printing a NutriBar must not move the available plate or cutlery.");
        return true;
    }

    private void AssertExcluded()
    {
        EndToEndAssert.True(printerJobObserved, "The test must observe the real printer ingestion job.");
        EndToEndAssert.NotNull(nutriBar, "The native printer must create one NutriBar Thing.");
        EndToEndAssert.True(nutriBar!.Destroyed, "The diner must consume the native NutriBar.");
        EndToEndAssert.True(plate.Spawned && plate.GetComp<CompSanitation>()?.IsDirty == false,
            "NutriBar ingestion must leave the available plate clean and untouched.");
        EndToEndAssert.True(cutlery.Spawned && cutlery.GetComp<CompSanitation>()?.IsDirty == false,
            "NutriBar ingestion must leave the available cutlery clean and untouched.");
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.replimat-animal-feeder-exclusion",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "sumghai.Replimat",
    "sumghai.ReplimatMeals",
    "Dubwise.DubsBadHygiene",
    "avilmask.CommonSense",
    "fumblesneeze.immersivechefs",
    MaxFrames = 5_400,
    MaxGameTicks = 18_000,
    MaxWallClockSeconds = 180)]
public sealed class ReplimatAnimalFeederExclusionTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn animal = null!;
    private ThingWithComps feeder = null!;
    private ThingWithComps tank = null!;
    private ThingWithComps computer = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private Thing? replicatedFeed;
    private int feedCountBeforeEating;
    private float hungerBeforeEating;
    private bool nativeIngestJobObserved;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        DispenserE2EFixture.SpawnConduitGrid(map, center, 8, 7);
        DispenserE2EFixture.SpawnPowerSources(map, center + new IntVec3(-7, 0, 4), 14);

        feeder = DispenserE2EFixture.SpawnBuilding(map, "ReplimatAnimalFeeder", center);
        tank = DispenserE2EFixture.SpawnBuilding(map, "ReplimatFeedTank", center + new IntVec3(3, 0, 1));
        computer = DispenserE2EFixture.SpawnBuilding(map, "ReplimatComputer", center + new IntVec3(-3, 0, 1));
        DispenserE2EFixture.SetReplimatFeedstockPercent(tank, 0.5f);
        DispenserE2EFixture.SettlePower(map, new[] { feeder, tank, computer }, 600);
        EndToEndAssert.True(
            DispenserE2EFixture.ReadBooleanProperty(computer, "Working"),
            "The native Replimat computer must recognize the powered animal-feeder network.");

        animal = PawnGenerator.GeneratePawn(
            DefDatabase<PawnKindDef>.GetNamed("Raccoon"),
            Faction.OfPlayer);
        animal.Name = new NameSingle("Replimat animal diner");
        animal.inventory?.innerContainer.ClearAndDestroyContents();
        animal.jobs.StopAll();
        GenSpawn.Spawn(animal, center + new IntVec3(0, 0, -3), map);
        FoodSearchE2EFixture.SetHunger(animal, 0.05f);

        plate = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Plate", ThingDefOf.Steel);
        cutlery = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Cutlery", ThingDefOf.Steel);
        GenSpawn.Spawn(plate, center + new IntVec3(-2, 0, -2), map);
        GenSpawn.Spawn(cutlery, center + new IntVec3(2, 0, -2), map);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var fixture = new[]
        {
            animal.ThingID, feeder.ThingID, tank.ThingID, computer.ThingID,
            plate.ThingID, cutlery.ThingID
        };
        yield return new SelectionActionStep("select the native animal feeder", new[] { feeder.ThingID }, false);
        yield return new CameraActionStep("frame the animal feeder fixture", fixture, 220);
        yield return new TimeControlActionStep(
            "run native animal-feed replication",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the native feeder produces loose kibble",
            _ => ObserveReplicatedFeed(),
            new EndToEndDeadline(1_800, 5_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause on native loose animal feed",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select replicated loose animal feed",
            new[] { replicatedFeed!.ThingID },
            false);
        yield return new ScreenshotStep(
            "observe the native feeder's loose kibble",
            Array.Empty<string>(),
            0);
        yield return new TimeControlActionStep(
            "let the animal choose food normally",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the animal starts the native ingest job",
            _ => ObserveAnimalIngestJob(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(80)));
        yield return new TimeControlActionStep(
            "pause on ordinary animal ingestion",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the animal eating replicated feed",
            new[] { animal.ThingID },
            false);
        yield return new ScreenshotStep(
            "observe the animal eating without tableware",
            Array.Empty<string>(),
            0);
        yield return new TimeControlActionStep(
            "finish ordinary animal ingestion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the animal consumes replicated feed",
            _ => FeedWasConsumed(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(80)));
        yield return new TimeControlActionStep(
            "pause after animal feeding",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "animal feeding remains outside dining service",
            _ => AssertExcluded());
        yield return new SelectionActionStep(
            "select untouched animal-feeder plate",
            new[] { plate.ThingID },
            false);
        yield return new ScreenshotStep(
            "observe untouched clean plate after animal feeding",
            Array.Empty<string>(),
            0);
        yield return new SelectionActionStep(
            "select untouched animal-feeder cutlery",
            new[] { cutlery.ThingID },
            false);
        yield return new ScreenshotStep(
            "observe untouched clean cutlery after animal feeding",
            Array.Empty<string>(),
            0);
        yield return new CheckpointStep(
            "Replimat animal-feeder exclusion result",
            _ => new Dictionary<string, string>
            {
                ["animalThingId"] = animal.ThingID,
                ["feedThingId"] = replicatedFeed?.ThingID ?? "missing",
                ["feedDef"] = replicatedFeed?.def.defName ?? "missing",
                ["feedCountBeforeEating"] = feedCountBeforeEating.ToString(),
                ["feedCountAfterEating"] = replicatedFeed?.Destroyed == true
                    ? "destroyed"
                    : replicatedFeed?.stackCount.ToString() ?? "missing",
                ["nativeIngestJobObserved"] = nativeIngestJobObserved.ToString(),
                ["plateClean"] = (plate.GetComp<CompSanitation>()?.IsDirty == false).ToString(),
                ["cutleryClean"] = (cutlery.GetComp<CompSanitation>()?.IsDirty == false).ToString(),
                ["foodPoisoning"] = animal.health.hediffSet.HasHediff(HediffDefOf.FoodPoisoning).ToString()
            });
    }

    private bool ObserveReplicatedFeed()
    {
        replicatedFeed ??= feeder.Position.GetThingList(map)
            .FirstOrDefault(thing => thing.def == ThingDefOf.Kibble);
        if (replicatedFeed is null)
        {
            return false;
        }

        EndToEndAssert.True(
            replicatedFeed is not ThingWithComps feedWithComps ||
            feedWithComps.GetComp<CompEmbeddedWare>() is null,
            "Native loose animal feed must not acquire embedded tableware.");
        EndToEndAssert.True(
            replicatedFeed is not ThingWithComps culinaryFeed ||
            culinaryFeed.GetComp<CompCulinaryState>() is null,
            "Native loose animal feed must not acquire culinary or temperature state.");
        feedCountBeforeEating = replicatedFeed.stackCount;
        hungerBeforeEating = animal.needs.food.CurLevelPercentage;
        return true;
    }

    private bool ObserveAnimalIngestJob()
    {
        if (animal.CurJobDef != JobDefOf.Ingest || replicatedFeed is null)
        {
            return false;
        }

        EndToEndAssert.True(
            ReferenceEquals(animal.CurJob?.GetTarget(TargetIndex.A).Thing, replicatedFeed),
            "The ordinary animal ingest job must target the feeder's exact loose kibble stack.");
        EndToEndAssert.True(
            DiningSessionRegistry.Current(animal) is null,
            "Animal feeding must not create a plate or cutlery dining session.");
        EndToEndAssert.True(
            plate.Spawned && cutlery.Spawned,
            "The nearby clean tableware controls must remain on the map when animal ingestion starts.");
        nativeIngestJobObserved = true;
        return true;
    }

    private bool FeedWasConsumed() =>
        replicatedFeed is not null &&
        (replicatedFeed.Destroyed || replicatedFeed.stackCount < feedCountBeforeEating) &&
        animal.needs.food.CurLevelPercentage > hungerBeforeEating;

    private void AssertExcluded()
    {
        EndToEndAssert.True(nativeIngestJobObserved, "The test must observe the native animal ingest job.");
        EndToEndAssert.True(FeedWasConsumed(), "The animal must consume part of the replicated feed stack.");
        EndToEndAssert.True(DiningSessionRegistry.Current(animal) is null,
            "No animal dining session may survive ingestion.");
        EndToEndAssert.True(animal.needs.mood is null,
            "The animal must not receive a dining-memory need.");
        EndToEndAssert.True(!animal.health.hediffSet.HasHediff(HediffDefOf.FoodPoisoning),
            "Immersive Chefs must not apply food poisoning to native animal feeding.");
        EndToEndAssert.True(plate.Spawned && plate.GetComp<CompSanitation>()?.IsDirty == false,
            "Animal feeding must leave the nearby plate clean and untouched.");
        EndToEndAssert.True(cutlery.Spawned && cutlery.GetComp<CompSanitation>()?.IsDirty == false,
            "Animal feeding must leave the nearby cutlery clean and untouched.");
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.replimat-survival-batch-exclusion",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "sumghai.Replimat",
    "sumghai.ReplimatMeals",
    "Dubwise.DubsBadHygiene",
    "avilmask.CommonSense",
    "fumblesneeze.immersivechefs",
    MaxFrames = 3_600,
    MaxGameTicks = 12_000,
    MaxWallClockSeconds = 150)]
public sealed class ReplimatSurvivalBatchExclusionTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private ThingWithComps terminal = null!;
    private ThingWithComps tank = null!;
    private ThingWithComps computer = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private EndToEndGizmoOption batchGizmo = null!;
    private ThingWithComps? survivalMeal;
    private float feedstockBefore;
    private float feedstockAfter;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        DispenserE2EFixture.SpawnConduitGrid(map, center, 8, 7);
        DispenserE2EFixture.SpawnPowerSources(map, center + new IntVec3(-7, 0, 4), 14);

        terminal = DispenserE2EFixture.SpawnBuilding(map, "ReplimatTerminal", center);
        tank = DispenserE2EFixture.SpawnBuilding(map, "ReplimatFeedTank", center + new IntVec3(3, 0, 1));
        computer = DispenserE2EFixture.SpawnBuilding(map, "ReplimatComputer", center + new IntVec3(-3, 0, 1));
        DispenserE2EFixture.SetReplimatFeedstockPercent(tank, 0.5f);
        DispenserE2EFixture.SettlePower(map, new[] { terminal, tank, computer }, 600);
        EndToEndAssert.True(
            DispenserE2EFixture.ReadBooleanProperty(terminal, "CanDispenseNow"),
            "The native Replimat terminal must be available before opening its survival-batch dialog.");
        feedstockBefore = DispenserE2EFixture.ReadSingleProperty(tank, "StoredFeedstock");

        plate = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Plate", ThingDefOf.Gold);
        cutlery = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Cutlery", ThingDefOf.Gold);
        GenSpawn.Spawn(plate, center + new IntVec3(-2, 0, -2), map);
        GenSpawn.Spawn(cutlery, center + new IntVec3(2, 0, -2), map);

        var gizmos = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { terminal.ThingID }, Array.Empty<string>());
        var candidates = gizmos.Where(option =>
            !option.Disabled &&
            option.Interaction == EndToEndGizmoInteraction.Invoke &&
            string.Equals(option.Label, "Batch survival meals", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        EndToEndAssert.Equal(
            1,
            candidates.Length,
            "The powered native Replimat terminal must expose one enabled Batch survival meals command.");
        batchGizmo = candidates[0];
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var fixture = new[]
        {
            terminal.ThingID, tank.ThingID, computer.ThingID, plate.ThingID, cutlery.ThingID
        };
        yield return new SelectionActionStep("select the native Replimat terminal", new[] { terminal.ThingID }, false);
        yield return new CameraActionStep("frame the survival-batch fixture", fixture, 220);
        yield return new ScreenshotStep(
            "observe the native terminal before survival batching",
            Array.Empty<string>(),
            0);
        yield return new GizmoActionStep(
            "open the native survival-batch dialog",
            new[] { terminal.ThingID },
            batchGizmo.RuntimeType,
            EndToEndGizmoInteraction.Invoke,
            stableGizmoId: batchGizmo.StableId);
        yield return new ScreenshotStep(
            "observe the native Replimat survival-batch dialog",
            Array.Empty<string>(),
            0);
        yield return new DialogConfirmationActionStep(
            "confirm one packaged survival meal through the open dialog",
            "Replimat.Dialog_BatchMakeSurvivalMeals");
        yield return new WaitUntilStep(
            "native survival batching creates one packaged meal",
            _ => ObserveSurvivalMeal(),
            new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause on the native survival-batch result",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "survival batching remains outside dining service",
            _ => AssertExcluded());
        yield return new SelectionActionStep(
            "select the native packaged survival meal",
            new[] { survivalMeal!.ThingID },
            false);
        yield return new CameraActionStep(
            "frame the packaged survival meal and untouched setting",
            new[] { survivalMeal!.ThingID, terminal.ThingID, plate.ThingID, cutlery.ThingID },
            220);
        yield return new ScreenshotStep(
            "observe unplated packaged survival meal and untouched tableware",
            Array.Empty<string>(),
            0);
        yield return new SelectionActionStep(
            "select untouched survival-batch plate",
            new[] { plate.ThingID },
            false);
        yield return new ScreenshotStep(
            "observe untouched clean plate after survival batching",
            Array.Empty<string>(),
            0);
        yield return new SelectionActionStep(
            "select untouched survival-batch cutlery",
            new[] { cutlery.ThingID },
            false);
        yield return new ScreenshotStep(
            "observe untouched clean cutlery after survival batching",
            Array.Empty<string>(),
            0);
        yield return new CheckpointStep(
            "Replimat survival-batch exclusion result",
            _ => new Dictionary<string, string>
            {
                ["mealThingId"] = survivalMeal?.ThingID ?? "missing",
                ["mealDef"] = survivalMeal?.def.defName ?? "missing",
                ["mealStackCount"] = survivalMeal?.stackCount.ToString() ?? "missing",
                ["feedstockBefore"] = feedstockBefore.ToString("R"),
                ["feedstockAfter"] = feedstockAfter.ToString("R"),
                ["plateClean"] = (plate.GetComp<CompSanitation>()?.IsDirty == false).ToString(),
                ["cutleryClean"] = (cutlery.GetComp<CompSanitation>()?.IsDirty == false).ToString()
            });
    }

    private bool ObserveSurvivalMeal()
    {
        survivalMeal ??= terminal.InteractionCell.GetThingList(map)
            .OfType<ThingWithComps>()
            .SingleOrDefault(thing =>
                thing.def == ThingDefOf.MealSurvivalPack &&
                thing.Spawned &&
                !thing.Destroyed);
        if (survivalMeal is null)
        {
            return false;
        }

        feedstockAfter = DispenserE2EFixture.ReadSingleProperty(tank, "StoredFeedstock");
        return true;
    }

    private void AssertExcluded()
    {
        EndToEndAssert.NotNull(survivalMeal, "The native batch dialog must create a packaged survival meal.");
        EndToEndAssert.Equal(1, survivalMeal!.stackCount,
            "The default native batch-dialog confirmation must create exactly one meal.");
        EndToEndAssert.True(survivalMeal.GetComp<CompEmbeddedWare>() is null,
            "The packaged survival meal must remain unplated.");
        EndToEndAssert.True(survivalMeal.GetComp<CompCulinaryState>() is null,
            "The packaged survival meal must remain outside culinary and temperature state.");
        EndToEndAssert.True(feedstockAfter < feedstockBefore,
            "Only native Replimat survival batching may consume its network feedstock.");
        EndToEndAssert.True(plate.Spawned && plate.GetComp<CompSanitation>()?.IsDirty == false,
            "Survival batching must leave the nearby plate clean and untouched.");
        EndToEndAssert.True(cutlery.Spawned && cutlery.GetComp<CompSanitation>()?.IsDirty == false,
            "Survival batching must leave the nearby cutlery clean and untouched.");
    }
}

internal static class DispenserE2EFixture
{
    internal static GastronomyGuestServiceFixture CreateGastronomyGuestServiceFixture(
        Map map,
        IntVec3 center)
    {
        var stage = "resolve exact Gastronomy and Hospitality runtime shapes";
        try
        {
            var managerType = AccessTools.TypeByName("Gastronomy.Restaurant.RestaurantsManager");
            var restaurantType = AccessTools.TypeByName("Gastronomy.Restaurant.RestaurantController");
            var diningSpotType = AccessTools.TypeByName("Gastronomy.Dining.DiningSpot");
            var compGuestType = AccessTools.TypeByName("Hospitality.CompGuest");
            var hospitalityMapComponentType = AccessTools.TypeByName("Hospitality.Hospitality_MapComponent");
            EndToEndAssert.NotNull(managerType, "Gastronomy must expose its exact RestaurantsManager.");
            EndToEndAssert.NotNull(restaurantType, "Gastronomy must expose its exact RestaurantController.");
            EndToEndAssert.NotNull(diningSpotType, "Gastronomy must expose its exact DiningSpot.");
            EndToEndAssert.NotNull(compGuestType, "Hospitality must expose its exact CompGuest.");
            EndToEndAssert.NotNull(
                hospitalityMapComponentType,
                "Hospitality must expose its exact map-owned guest registry.");

            stage = "spawn the real restaurant furniture and cleaning appliance";
            var table = SpawnBuilding(map, "Table1x2c", center + new IntVec3(2, 0, 0));
            var tableCells = table.OccupiedRect().Cells.ToArray();
            EndToEndAssert.Equal(2, tableCells.Length, "The restaurant fixture requires the real 1x2 table.");
            var spotCell = tableCells[0];
            var diningSpot = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gastronomy_DiningSpot"));
            GenSpawn.Spawn(diningSpot, spotCell, map);
            EndToEndAssert.True(
                diningSpotType!.IsInstanceOfType(diningSpot),
                "The restaurant fixture must spawn the exact native Gastronomy DiningSpot.");

            var chairCell = new[] { IntVec3.South, IntVec3.North, IntVec3.East, IntVec3.West }
                .Select(offset => spotCell + offset)
                .First(cell => cell.InBounds(map) && cell.GetEdifice(map) is null);
            var chairDef = DefDatabase<ThingDef>.GetNamed("DiningChair");
            var chair = ThingMaker.MakeThing(chairDef, ThingDefOf.WoodLog);
            chair.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(chair, chairCell, map, Rot4.FromIntVec3(spotCell - chairCell));

            var registerSupport = SpawnBuilding(
                map,
                "Table1x2c",
                center + new IntVec3(-3, 0, 0));
            var registerCell = registerSupport.OccupiedRect().Cells.First();
            var cashRegister = SpawnBuilding(
                map,
                "CashRegister_CashRegister",
                registerCell);
            EndToEndAssert.True(
                registerCell.GetThingList(map).Contains(registerSupport),
                "The real tabletop cash register must be spawned on its exact native table support.");
            var dishwasher = SpawnBuilding(
                map,
                "ImmersiveChefs_Dishwasher",
                center + new IntVec3(-3, 0, 2));
            SettlePower(map, new[] { dishwasher }, 400);

            stage = "create the assigned Cleaning-capable Gastronomy waiter";
            var waitingWork = DefDatabase<WorkTypeDef>.GetNamed("Gastronomy_Waiting");
            var waiter = CreateCleaningCapableColonist(
                "Printer restaurant waiter",
                waitingWork);
            waiter.workSettings.SetPriority(waitingWork, 1);
            GenSpawn.Spawn(waiter, center + new IntVec3(-2, 0, 3), map);

            stage = "link the real cash register and active waiter shift";
            var manager = map.components.Single(component => managerType!.IsInstanceOfType(component));
            var restaurants = managerType!.GetField("restaurants", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(manager) as IList;
            EndToEndAssert.True(
                restaurants is { Count: > 0 },
                "The finalized Gastronomy map must expose at least one restaurant controller.");
            var restaurant = restaurants![0];
            EndToEndAssert.True(
                restaurant is not null && restaurantType!.IsInstanceOfType(restaurant),
                "The first Gastronomy restaurant must retain its exact runtime type.");
            var linkRegister = restaurantType!.GetMethod(
                "LinkRegister",
                BindingFlags.Public | BindingFlags.Instance);
            var linkRegisterParameters = linkRegister?.GetParameters();
            EndToEndAssert.True(
                linkRegisterParameters?.Length == 1 &&
                linkRegisterParameters[0].ParameterType == cashRegister.GetType(),
                "RestaurantController.LinkRegister must retain the exact native register parameter.");
            linkRegister!.Invoke(restaurant, new object[] { cashRegister });
            restaurantType.GetField("openForBusiness", BindingFlags.Public | BindingFlags.Instance)
                ?.SetValue(restaurant, true);
            restaurantType.GetField("guestPricePercentage", BindingFlags.Public | BindingFlags.Instance)
                ?.SetValue(restaurant, 0f);

            var shifts = cashRegister.GetType()
                .GetField("shifts", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(cashRegister) as IList;
            EndToEndAssert.True(shifts is { Count: > 0 }, "The real cash register must expose one native shift.");
            var shift = shifts![0];
            var assigned = shift!.GetType()
                .GetField("assigned", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(shift) as IList;
            var timetable = shift.GetType()
                .GetField("timetable", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(shift);
            var times = timetable?.GetType()
                .GetField("times", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(timetable) as IList;
            EndToEndAssert.NotNull(assigned, "The exact register shift must expose its assigned pawn list.");
            EndToEndAssert.True(times is { Count: 24 }, "The exact register timetable must expose 24 hours.");
            assigned!.Add(waiter);
            for (var hour = 0; hour < times!.Count; hour++)
            {
                times[hour] = true;
            }

            restaurantType.GetMethod("RescanDiningSpots", BindingFlags.Public | BindingFlags.Instance)
                ?.Invoke(restaurant, Array.Empty<object>());
            var seats = restaurantType.GetProperty("Seats", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(restaurant);
            var open = restaurantType.GetProperty("IsOpenedRightNow", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(restaurant);
            EndToEndAssert.True(seats is int seatCount && seatCount >= 1,
                "The real restaurant must recognize at least one native dining seat.");
            EndToEndAssert.True(open is true,
                "The real restaurant and assigned cash-register shift must be open.");

            stage = "spawn and register the real Hospitality guest";
            var playerFaction = Faction.OfPlayer;
            var guestFaction = Find.FactionManager.AllFactionsListForReading.First(faction =>
                faction != playerFaction && !faction.HostileTo(playerFaction) && !faction.def.hidden);
            var guest = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                guestFaction,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false));
            HumanlikePawnFixture.SetName(guest, "Printer restaurant guest");
            guest.inventory?.innerContainer.ClearAndDestroyContents();
            GenSpawn.Spawn(guest, center + new IntVec3(3, 0, 3), map);
            var compGuest = guest.AllComps.FirstOrDefault(compGuestType!.IsInstanceOfType);
            EndToEndAssert.NotNull(
                compGuest,
                "Hospitality must attach its real CompGuest to the finalized human pawn Def.");
            var hospitalityMapComponent = map.components.Single(component =>
                hospitalityMapComponentType!.IsInstanceOfType(component));
            var joined = hospitalityMapComponentType!.GetMethod(
                "OnGuestJoinedLate",
                BindingFlags.Public | BindingFlags.Instance);
            var arrive = compGuestType!.GetMethod("Arrive", BindingFlags.Public | BindingFlags.Instance);
            var joinedParameters = joined?.GetParameters();
            EndToEndAssert.True(
                joinedParameters?.Length == 1 && joinedParameters[0].ParameterType == typeof(Pawn),
                "Hospitality.OnGuestJoinedLate must retain its exact public Pawn shape.");
            EndToEndAssert.True(
                arrive?.GetParameters().Length == 0,
                "Hospitality.CompGuest.Arrive must retain its exact zero-argument public shape.");
            joined!.Invoke(hospitalityMapComponent, new object[] { guest });
            arrive!.Invoke(compGuest, Array.Empty<object>());

            return new GastronomyGuestServiceFixture(
                guest,
                waiter,
                table,
                diningSpot,
                cashRegister,
                dishwasher,
                restaurant!);
        }
        catch (EndToEndAssertionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new EndToEndAssertionException(
                $"Gastronomy guest-service fixture failed while trying to {stage} " +
                $"({exception.GetType().Name}: {exception.Message}).");
        }
    }

    internal static bool RestaurantStockContains(object restaurant, Thing meal)
    {
        var stock = restaurant.GetType()
            .GetProperty("Stock", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(restaurant);
        var isAvailable = stock?.GetType().GetMethod(
            "IsAvailable",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { typeof(Thing) },
            null);
        EndToEndAssert.NotNull(
            isAvailable,
            "The exact Gastronomy stock must expose IsAvailable(Thing).");
        return isAvailable!.Invoke(stock, new object[] { meal }) is true;
    }

    internal static void PlaceInNativeRestaurantStock(
        Thing meal,
        Thing cashRegister,
        object restaurant)
    {
        var fields = cashRegister.GetType()
            .GetProperty("Fields", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(cashRegister) as IEnumerable;
        EndToEndAssert.NotNull(
            fields,
            "The exact Gastronomy cash register must expose its native stock fields.");
        var stockCell = fields!.Cast<object>()
            .OfType<IntVec3>()
            .Where(cell => cell.InBounds(meal.Map) && cell.GetFirstItem(meal.Map) is null)
            .OrderBy(cell => cell.DistanceToSquared(meal.Position))
            .FirstOrDefault();
        EndToEndAssert.True(
            stockCell.IsValid,
            "The exact Gastronomy cash register must expose one empty native stock field.");

        var map = meal.Map;
        meal.DeSpawn(DestroyMode.Vanish);
        GenSpawn.Spawn(meal, stockCell, map);

        var stock = restaurant.GetType()
            .GetProperty("Stock", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(restaurant);
        var refresh = stock?.GetType().GetMethod(
            "RefreshStock",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);
        EndToEndAssert.NotNull(
            refresh,
            "The exact Gastronomy stock must expose its native zero-argument refresh.");
        refresh!.Invoke(stock, Array.Empty<object>());
        EndToEndAssert.True(
            RestaurantStockContains(restaurant, meal),
            "Gastronomy's real stock cache must discover the exact released printed meal on its native field.");
        Find.TickManager.Pause();
    }

    internal static void StartNativeGastronomyDining(Pawn guest, Thing diningSpot, object restaurant)
    {
        var managerType = AccessTools.TypeByName("Gastronomy.Restaurant.RestaurantsManager");
        var restaurantType = AccessTools.TypeByName("Gastronomy.Restaurant.RestaurantController");
        var manager = guest.Map?.components.Single(component => managerType!.IsInstanceOfType(component));
        var register = managerType?.GetMethod(
            "RegisterDiningAt",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { typeof(Pawn), restaurantType! },
            null);
        EndToEndAssert.NotNull(
            register,
            "Gastronomy RestaurantsManager.RegisterDiningAt must retain its exact public shape.");
        register!.Invoke(manager, new[] { guest, restaurant });

        var dineDef = DefDatabase<JobDef>.GetNamed("Gastronomy_Dine");
        var dineJob = JobMaker.MakeJob(dineDef, diningSpot);
        dineJob.playerForced = true;
        guest.jobs.StartJob(
            dineJob,
            JobCondition.InterruptForced,
            resumeCurJobAfterwards: false,
            cancelBusyStances: true,
            tag: JobTag.Misc);
        EndToEndAssert.True(
            guest.CurJobDef == dineDef && ReferenceEquals(guest.CurJob?.GetTarget(TargetIndex.A).Thing, diningSpot),
            "The explicit scenario setup must arm the exact native Gastronomy dining job.");
    }

    internal static void StartNativeGastronomyService(
        Pawn waiter,
        Pawn guest,
        Thing meal,
        object restaurant)
    {
        EndToEndAssert.True(
            guest.CurJobDef?.defName == "Gastronomy_Dine",
            "The exact guest must still be running the native Gastronomy Dine driver before service.");
        if (waiter.CurJobDef?.defName == "Gastronomy_Serve" &&
            ReferenceEquals(waiter.CurJob?.GetTarget(TargetIndex.A).Pawn, guest) &&
            ReferenceEquals(waiter.CurJob?.GetTarget(TargetIndex.B).Thing, meal))
        {
            return;
        }

        EndToEndAssert.True(
            meal.Spawned && meal.Map == waiter.Map,
            "The exact printed meal must remain spawned in restaurant stock before explicitly arming service.");
        var orders = restaurant.GetType()
            .GetProperty("Orders", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(restaurant);
        var createOrder = orders?.GetType().GetMethod(
            "CreateOrder",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { typeof(Pawn), typeof(Thing) },
            null);
        var getOrder = orders?.GetType().GetMethod(
            "GetOrderFor",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { typeof(Pawn) },
            null);
        EndToEndAssert.NotNull(
            createOrder,
            "Gastronomy RestaurantOrders.CreateOrder must retain its exact native shape.");
        EndToEndAssert.NotNull(
            getOrder,
            "Gastronomy RestaurantOrders.GetOrderFor must retain its exact native shape.");
        createOrder!.Invoke(orders, new object[] { guest, meal });
        var order = getOrder!.Invoke(orders, new object[] { guest });
        EndToEndAssert.NotNull(order, "The real restaurant must own one native guest order.");
        order!.GetType().GetField("consumable", BindingFlags.Public | BindingFlags.Instance)
            ?.SetValue(order, meal);
        order.GetType().GetField("hasToBeMade", BindingFlags.Public | BindingFlags.Instance)
            ?.SetValue(order, false);

        var serveDef = DefDatabase<JobDef>.GetNamed("Gastronomy_Serve");
        var serveJob = JobMaker.MakeJob(serveDef, guest, meal);
        waiter.jobs.StartJob(
            serveJob,
            JobCondition.InterruptForced,
            resumeCurJobAfterwards: false,
            cancelBusyStances: true,
            tag: JobTag.Misc);
        EndToEndAssert.True(
            waiter.CurJobDef == serveDef &&
            ReferenceEquals(waiter.CurJob?.GetTarget(TargetIndex.A).Pawn, guest) &&
            ReferenceEquals(waiter.CurJob?.GetTarget(TargetIndex.B).Thing, meal),
            "The explicit scenario setup must arm the exact native Gastronomy Serve job.");
    }

    internal static Pawn CreateCleaningCapableColonist(
        string name,
        WorkTypeDef? additionallyRequiredWork = null)
    {
        for (var attempt = 0; attempt < 32; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false));
            if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Cleaning) ||
                (additionallyRequiredWork is not null && pawn.WorkTypeIsDisabled(additionallyRequiredWork)) ||
                !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving) ||
                !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                pawn.Destroy(DestroyMode.Vanish);
                continue;
            }

            HumanlikePawnFixture.SetName(pawn, name);
            pawn.inventory?.innerContainer.ClearAndDestroyContents();
            pawn.workSettings.EnableAndInitialize();
            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (!pawn.WorkTypeIsDisabled(workType))
                {
                    pawn.workSettings.SetPriority(workType, 0);
                }
            }

            pawn.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 1);
            pawn.jobs.StopAll();
            return pawn;
        }

        throw new EndToEndAssertionException(
            "Could not generate a healthy Cleaning-capable colonist for the dispenser fixture.");
    }

    internal static ThingWithComps SpawnDubsWaterSupply(
        Map map,
        ThingWithComps appliance,
        float storedWater)
    {
        var pipeDef = DefDatabase<ThingDef>.GetNamed("sewagePipeHidden");
        var towerCell = appliance.Position + new IntVec3(-5, 0, -1);
        for (var x = towerCell.x + 2; x <= appliance.Position.x - 1; x++)
        {
            var pipe = ThingMaker.MakeThing(pipeDef, ThingDefOf.Steel);
            pipe.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(pipe, new IntVec3(x, 0, appliance.Position.z), map);
        }

        var tower = SpawnBuilding(map, "WaterTowerS", towerCell);
        var storage = tower.AllComps.SingleOrDefault(comp =>
            string.Equals(
                comp.GetType().FullName,
                "DubsBadHygiene.CompWaterStorage",
                StringComparison.Ordinal));
        EndToEndAssert.NotNull(
            storage,
            "The exact Dubs water tower must expose one CompWaterStorage.");
        var waterStorageField = storage!.GetType().GetField(
            "WaterStorage",
            BindingFlags.Public | BindingFlags.Instance);
        EndToEndAssert.True(
            waterStorageField?.FieldType == typeof(float),
            "The installed Dubs CompWaterStorage.WaterStorage shape must remain public Single.");
        waterStorageField!.SetValue(storage, storedWater);

        var appliancePipe = appliance.AllComps.SingleOrDefault(comp =>
            string.Equals(comp.GetType().FullName, "DubsBadHygiene.CompPipe", StringComparison.Ordinal));
        var towerPipe = tower.AllComps.SingleOrDefault(comp =>
            string.Equals(comp.GetType().FullName, "DubsBadHygiene.CompPipe", StringComparison.Ordinal));
        EndToEndAssert.NotNull(
            appliancePipe,
            "The finalized Immersive Chefs dishwasher must expose one real Dubs CompPipe.");
        EndToEndAssert.NotNull(
            towerPipe,
            "The real Dubs water tower must expose one CompPipe.");
        var pipeNetProperty = appliancePipe!.GetType().GetProperty(
            "pipeNet",
            BindingFlags.Public | BindingFlags.Instance);
        EndToEndAssert.NotNull(
            pipeNetProperty,
            "The installed Dubs CompPipe.pipeNet property must remain public.");

        object? applianceNet = null;
        object? towerNet = null;
        for (var tick = 0; tick <= 60; tick++)
        {
            applianceNet = pipeNetProperty!.GetValue(appliancePipe);
            towerNet = pipeNetProperty.GetValue(towerPipe);
            if (applianceNet is not null && ReferenceEquals(applianceNet, towerNet))
            {
                break;
            }

            Find.TickManager.DoSingleTick();
        }

        EndToEndAssert.True(
            applianceNet is not null && ReferenceEquals(applianceNet, towerNet),
            "The dishwasher and water tower must join one exact Dubs plumbing network.");
        var networkWaterProperty = applianceNet!.GetType().GetProperty(
            "WaterStorage",
            BindingFlags.Public | BindingFlags.Instance);
        var connectedWater = networkWaterProperty?.GetValue(applianceNet);
        EndToEndAssert.True(
            connectedWater is float water && Math.Abs(water - storedWater) < 0.001f,
            "The connected Dubs plumbing network must expose the exact initialized water supply.");
        return tower;
    }

    internal static MealPrinterFixture CreateMealPrinterFixture(string mealDefName, string pawnName)
    {
        var stage = "resolve current map";
        try
        {
            var map = Current.Game.CurrentMap;
            stage = "build sealed room";
            var center = FoodSearchE2EFixture.FindRoomCenter(map);
            FoodSearchE2EFixture.BuildSealedRoom(map, center);
            stage = "build printer power net";
            SpawnConduitGrid(map, center, 7, 7);
            SpawnPowerSources(map, center + new IntVec3(-4, 0, -4), 3);

            stage = "spawn native printer";
            var printer = SpawnBuilding(map, "MealPrinter", center + new IntVec3(-1, 0, -2));
            stage = "set native printer output";
            SetMealPrinterOutput(printer, DefDatabase<ThingDef>.GetNamed(mealDefName));
            stage = "resolve native adjacent hopper cell";
            var hopperCells = GenAdj.CellsAdjacentCardinal(printer)
                .Where(cell =>
                    cell.InBounds(map) &&
                    cell != printer.InteractionCell &&
                    cell.GetEdifice(map) is null)
                .Take(1)
                .ToArray();
            EndToEndAssert.Equal(
                1,
                hopperCells.Length,
                "The native Meal Printer must expose one free cardinal-adjacent hopper cell.");
            var hopperCell = hopperCells[0];
            stage = "spawn native hopper";
            var hopper = SpawnBuilding(map, ThingDefOf.Hopper.defName, hopperCell);
            stage = "spawn native hopper feedstock";
            var feedstock = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawRice"));
            feedstock.stackCount = Math.Min(60, feedstock.def.stackLimit);
            GenSpawn.Spawn(feedstock, hopperCell, map);

            stage = "settle native power and hopper state";
            SettlePower(map, new[] { printer }, 400);
            EndToEndAssert.True(ReadBooleanProperty(printer, "CanDispenseNow"),
                "The configured native Meal Printer must be powered and supplied by its real hopper.");
            EndToEndAssert.Equal(
                mealDefName,
                ReadMealPrinterOutput(printer)?.defName,
                "Meal Printer's native food resolver must expose the configured output.");

            stage = "spawn hungry diner";
            var diner = FoodSearchE2EFixture.CreateColonist(pawnName);
            GenSpawn.Spawn(diner, center + new IntVec3(2, 0, 3), map);
            FoodSearchE2EFixture.SetHunger(diner, 0.10f);
            return new MealPrinterFixture(
                map,
                diner,
                printer,
                hopper,
                feedstock,
                center + new IntVec3(2, 0, 1));
        }
        catch (EndToEndAssertionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new EndToEndAssertionException(
                $"Meal Printer fixture failed while trying to {stage} ({exception.GetType().Name}).");
        }
    }

    internal static ThingWithComps SpawnBuilding(Map map, string defName, IntVec3 cell)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var building = (ThingWithComps)ThingMaker.MakeThing(
            def,
            def.MadeFromStuff ? ThingDefOf.Steel : null);
        building.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(building, cell, map, Rot4.North);
        if (building.GetComp<CompFlickable>() is { SwitchIsOn: false } flick)
        {
            flick.DoFlick();
        }

        building.GetComp<CompRefuelable>()?.Refuel(999f);
        return building;
    }

    internal static void SpawnConduitGrid(Map map, IntVec3 center, int radiusX, int radiusZ)
    {
        var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
        for (var x = -radiusX; x <= radiusX; x++)
        {
            for (var z = -radiusZ; z <= radiusZ; z++)
            {
                var cell = center + new IntVec3(x, 0, z);
                if (!cell.InBounds(map))
                {
                    continue;
                }

                var conduit = ThingMaker.MakeThing(conduitDef);
                conduit.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(conduit, cell, map);
            }
        }
    }

    internal static void SpawnPowerSources(Map map, IntVec3 firstCell, int count)
    {
        var def = DefDatabase<ThingDef>.GetNamed("VanometricPowerCell");
        for (var index = 0; index < count; index++)
        {
            var source = ThingMaker.MakeThing(def, def.MadeFromStuff ? ThingDefOf.Steel : null);
            source.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(source, firstCell + new IntVec3(index, 0, 0), map, Rot4.North);
        }
    }

    internal static void SettlePower(Map map, IEnumerable<ThingWithComps> poweredThings, int maxTicks)
    {
        var powered = poweredThings.ToArray();
        map.powerNetManager.UpdatePowerNetsAndConnections_First();
        for (var tick = 0; tick <= maxTicks && powered.Any(thing =>
                 thing.GetComp<CompPowerTrader>() is { PowerOn: false }); tick++)
        {
            Find.TickManager.DoSingleTick();
        }

        foreach (var thing in powered)
        {
            if (thing.GetComp<CompPowerTrader>() is { } power)
            {
                EndToEndAssert.True(power.PowerOn, thing.def.defName + " must be powered by the native shared net.");
            }
        }
    }

    internal static ThingWithComps? FindMeal(Pawn pawn, Map map, Func<Thing, bool> predicate)
    {
        var candidates = new List<Thing>();
        if (pawn.carryTracker?.CarriedThing is { } carried)
        {
            candidates.Add(carried);
        }

        if (pawn.inventory?.innerContainer is { } inventory)
        {
            candidates.AddRange(inventory);
        }

        if (pawn.CurJob is { } job)
        {
            AddTarget(job.GetTarget(TargetIndex.A), candidates);
            AddTarget(job.GetTarget(TargetIndex.B), candidates);
            AddTarget(job.GetTarget(TargetIndex.C), candidates);
            AddTargets(job.targetQueueA, candidates);
            AddTargets(job.targetQueueB, candidates);
        }

        candidates.AddRange(map.listerThings.AllThings);
        return candidates
            .Distinct()
            .OfType<ThingWithComps>()
            .FirstOrDefault(thing => !thing.Destroyed && predicate(thing));
    }

    internal static bool ReadBooleanProperty(Thing thing, string propertyName)
    {
        return ReadProperty(thing, propertyName) is true;
    }

    internal static float ReadSingleProperty(Thing thing, string propertyName)
    {
        return ReadProperty(thing, propertyName) is float value
            ? value
            : throw new EndToEndAssertionException(
                thing.GetType().FullName + "." + propertyName + " did not return Single.");
    }

    internal static void SetReplimatFeedstockPercent(Thing tank, float percentage)
    {
        var method = tank.GetType().GetMethod(
            "SetStoredFeedstockPct",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { typeof(float) },
            null);
        EndToEndAssert.NotNull(method, "The validated Replimat tank must expose SetStoredFeedstockPct.");
        method!.Invoke(tank, new object[] { percentage });
    }

    private static object? ReadProperty(Thing thing, string propertyName)
    {
        var property = thing.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance);
        EndToEndAssert.NotNull(property, thing.GetType().FullName + " must expose " + propertyName + ".");
        return property!.GetValue(thing);
    }

    private static void SetMealPrinterOutput(Thing printer, ThingDef mealDef)
    {
        var method = printer.GetType().GetMethod(
            "setMealToPrint",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { typeof(ThingDef) },
            null);
        EndToEndAssert.NotNull(method, "The validated Meal Printer must expose its native configuration setter.");
        method!.Invoke(printer, new object[] { mealDef });
    }

    private static ThingDef? ReadMealPrinterOutput(Thing printer)
    {
        var method = printer.GetType().GetMethod(
            "GetMealThing",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);
        EndToEndAssert.NotNull(method, "The validated Meal Printer must expose its native output getter.");
        return method!.Invoke(printer, Array.Empty<object>()) as ThingDef;
    }

    private static void AddTarget(LocalTargetInfo target, ICollection<Thing> candidates)
    {
        if (target.Thing is { } thing)
        {
            candidates.Add(thing);
        }
    }

    private static void AddTargets(IEnumerable<LocalTargetInfo>? targets, ICollection<Thing> candidates)
    {
        if (targets is null)
        {
            return;
        }

        foreach (var target in targets)
        {
            AddTarget(target, candidates);
        }
    }

    internal readonly struct MealPrinterFixture
    {
        internal MealPrinterFixture(
            Map map,
            Pawn diner,
            ThingWithComps printer,
            ThingWithComps hopper,
            Thing feedstock,
            IntVec3 wareCell)
        {
            Map = map;
            Diner = diner;
            Printer = printer;
            Hopper = hopper;
            Feedstock = feedstock;
            WareCell = wareCell;
        }

        internal Map Map { get; }
        internal Pawn Diner { get; }
        internal ThingWithComps Printer { get; }
        internal ThingWithComps Hopper { get; }
        internal Thing Feedstock { get; }
        internal IntVec3 WareCell { get; }
    }

    internal readonly struct GastronomyGuestServiceFixture
    {
        internal GastronomyGuestServiceFixture(
            Pawn guest,
            Pawn waiter,
            ThingWithComps diningTable,
            Thing diningSpot,
            ThingWithComps cashRegister,
            ThingWithComps dishwasher,
            object restaurant)
        {
            Guest = guest;
            Waiter = waiter;
            DiningTable = diningTable;
            DiningSpot = diningSpot;
            CashRegister = cashRegister;
            Dishwasher = dishwasher;
            Restaurant = restaurant;
        }

        internal Pawn Guest { get; }
        internal Pawn Waiter { get; }
        internal ThingWithComps DiningTable { get; }
        internal Thing DiningSpot { get; }
        internal ThingWithComps CashRegister { get; }
        internal ThingWithComps Dishwasher { get; }
        internal object Restaurant { get; }
    }
}
