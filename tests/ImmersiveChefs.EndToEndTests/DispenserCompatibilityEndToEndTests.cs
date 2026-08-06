using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.replimat-native-dining",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "sumghai.Replimat",
    "sumghai.ReplimatMeals",
    "Dubwise.DubsBadHygiene",
    "avilmask.CommonSense",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 24_000,
    MaxWallClockSeconds = 210)]
public sealed class ReplimatNativeDiningTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn diner = null!;
    private ThingWithComps terminal = null!;
    private ThingWithComps tank = null!;
    private ThingWithComps computer = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private ThingWithComps? dispensedMeal;
    private string expectedMealDefName = string.Empty;
    private float feedstockBefore;
    private float feedstockAfterDispense;
    private bool nativeJobObserved;
    private bool wareCarriedBeforeOutput;

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
        DispenserE2EFixture.SettlePower(map, new[] { terminal, tank, computer }, 600);

        EndToEndAssert.True(
            DispenserE2EFixture.ReadBooleanProperty(computer, "Working"),
            "The native Replimat computer must recognize the powered shared network.");
        EndToEndAssert.True(
            DispenserE2EFixture.ReadBooleanProperty(terminal, "CanDispenseNow"),
            "The native terminal must be available with a powered computer and stocked tank.");
        feedstockBefore = DispenserE2EFixture.ReadSingleProperty(tank, "StoredFeedstock");
        EndToEndAssert.True(feedstockBefore > 0f, "The native Replimat tank must begin with real feedstock.");

        diner = FoodSearchE2EFixture.CreateColonist("Replimat diner");
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
            plate.ThingID, cutlery.ThingID
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
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "Replimat meal returns the same dirty setting",
            _ => dispensedMeal?.Destroyed == true &&
                 plate.Spawned && plate.GetComp<CompSanitation>()?.IsDirty == true &&
                 cutlery.Spawned && cutlery.GetComp<CompSanitation>()?.IsDirty == true,
            new EndToEndDeadline(3_600, 16_000, TimeSpan.FromSeconds(105)));
        yield return new TimeControlActionStep(
            "pause after Replimat dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep("Replimat native lifecycle completes once", _ => AssertCompleted());
        yield return new SelectionActionStep(
            "select returned Replimat plate",
            new[] { plate.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame returned Replimat setting",
            new[] { diner.ThingID, terminal.ThingID, plate.ThingID, cutlery.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep("observe returned dirty Replimat plate", Array.Empty<string>(), 0);
        yield return new SelectionActionStep(
            "select returned Replimat cutlery",
            new[] { cutlery.ThingID },
            additive: false);
        yield return new ScreenshotStep("observe returned dirty Replimat cutlery", Array.Empty<string>(), 0);
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
        EndToEndAssert.True(plate.Spawned && plate.GetComp<CompSanitation>()?.IsDirty == true,
            "The exact embedded Replimat plate must return dirty after ingestion.");
        EndToEndAssert.True(cutlery.Spawned && cutlery.GetComp<CompSanitation>()?.IsDirty == true,
            "The exact carried Replimat cutlery must return dirty after ingestion.");
        EndToEndAssert.True(!diner.health.hediffSet.HasHediff(HediffDefOf.FoodPoisoning),
            "Native Replimat dining must retain its no-food-poisoning outcome.");
    }

    private static bool IsReplimatMeal(Thing thing) =>
        MealClassificationCatalog.ReplimatMealDefNames.Contains(thing.def.defName);
}

[RimWorldEndToEndTest(
    "immersive-chefs.replimat-failed-dispense-rollback",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
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
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
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
    "immersive-chefs.meal-printer-nutribar-exclusion",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
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
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
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
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
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
            feedstock.stackCount = 100;
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
}
