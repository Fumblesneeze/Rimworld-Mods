using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.dubs-processor-dishwasher-water-interruption",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "syrchalis.processor.framework",
    "Dubwise.DubsBadHygiene",
    "fumblesneeze.immersivechefs",
    MaxFrames = 12_000,
    MaxGameTicks = 40_000,
    MaxWallClockSeconds = 480)]
public sealed class DubsProcessorDishwasherWaterInterruptionTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn hauler = null!;
    private ThingWithComps dishwasher = null!;
    private ThingWithComps tower = null!;
    private ThingWithComps plate = null!;
    private string plateId = string.Empty;
    private string dishwasherId = string.Empty;
    private string towerId = string.Empty;
    private string haulerId = string.Empty;
    private float waterAfterDebit;
    private float progressBeforeLoss;
    private int lossTick;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        HandwashingE2EFixture.PreserveSettings(context);
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        DispenserE2EFixture.SpawnConduitGrid(map, center, 7, 5);
        DispenserE2EFixture.SpawnPowerSources(map, center + new IntVec3(5, 0, 3), 1);

        dishwasher = DispenserE2EFixture.SpawnBuilding(
            map,
            "ImmersiveChefs_Dishwasher",
            center + new IntVec3(1, 0, 2));
        tower = DispenserE2EFixture.SpawnDubsWaterSupply(map, dishwasher, 2f);
        DispenserE2EFixture.SettlePower(map, new[] { dishwasher }, 400);
        EndToEndAssert.True(ProcessorFrameworkAdapter.Controls(dishwasher),
            "The exact group must exercise Processor Framework's native dishwasher path.");

        plate = HandwashingE2EFixture.MakeDirtyPlate(ThingDefOf.Steel);
        GenSpawn.Spawn(plate, center + new IntVec3(-1, 0, -2), map);
        plateId = plate.ThingID;

        hauler = CreateInactiveHauler("Processor dishwasher hauler");
        GenSpawn.Spawn(hauler, center + new IntVec3(1, 0, -2), map);
        dishwasherId = dishwasher.ThingID;
        towerId = tower.ThingID;
        haulerId = hauler.ThingID;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before native Processor dishwasher hauling",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the dirty plate before Processor hauling",
            new[] { plate.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the supplied Processor dishwasher fixture",
            new[] { plate.ThingID, hauler.ThingID, dishwasher.ThingID, tower.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "dirty plate beside the supplied Processor dishwasher before native hauling",
            Array.Empty<string>(),
            paddingPixels: 0);

        ActivateHauler(hauler);
        yield return new TimeControlActionStep(
            "run ordinary hauling into the Processor dishwasher",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the native Processor work giver admits the exact dirty plate",
            _ => ProcessorFrameworkAdapter.HasContents(dishwasher) &&
                 !plate.Spawned &&
                 HandwashingE2EFixture.Nearly(
                     HandwashingE2EFixture.ReadDubsNetworkWater(dishwasher),
                     2f),
            new EndToEndDeadline(1_500, 6_000, TimeSpan.FromSeconds(60)));
        yield return new WaitUntilStep(
            "the captured Processor cycle starts after one load-scaled water debit",
            _ => ProcessorFrameworkAdapter.ProgressPercent(dishwasher) >= 8f &&
                 HandwashingE2EFixture.ReadDubsNetworkWater(dishwasher) < 2f,
            new EndToEndDeadline(1_500, 7_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause after the one Processor water debit",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "capture the debited batch before supply loss",
            _ =>
            {
                waterAfterDebit = HandwashingE2EFixture.ReadDubsNetworkWater(dishwasher);
                progressBeforeLoss = ProcessorFrameworkAdapter.ProgressPercent(dishwasher);
                EndToEndAssert.True(HandwashingE2EFixture.Nearly(waterAfterDebit, 1.9f),
                    "One plate must debit exactly 0.1 L from the two-liter Dubs supply.");
                EndToEndAssert.True(progressBeforeLoss >= 8f,
                    "The active Processor batch must establish visible progress before water loss.");
            });

        yield return new SaveLoadActionStep(
            "save and load the active residual-water Processor batch through RimWorld",
            "ImmersiveChefs_DubsProcessorWaterInterruption");
        yield return new AssertionStep(
            "resolve the same residual-water batch after native loading",
            _ =>
            {
                ResolveLoadedFixtures();
                EndToEndAssert.True(
                    HandwashingE2EFixture.Nearly(
                        ProcessorFrameworkAdapter.ProgressPercent(dishwasher),
                        progressBeforeLoss),
                    "Native save/load must retain the active Processor progress unchanged.");
                EndToEndAssert.True(
                    HandwashingE2EFixture.Nearly(
                        HandwashingE2EFixture.ReadDubsNetworkWater(dishwasher),
                        waterAfterDebit),
                    "Native save/load must retain the once-debited residual water supply.");
            });

        HandwashingE2EFixture.SetDubsStoredWater(tower, dishwasher, 0f);
        lossTick = Find.TickManager.TicksGame;
        yield return new TimeControlActionStep(
            "advance the active batch after its Dubs supply is lost",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the dishwasher remains paused at unchanged progress for one thousand ticks",
            _ => Find.TickManager.TicksGame >= lossTick + 1_000 &&
                 HandwashingE2EFixture.Nearly(
                     ProcessorFrameworkAdapter.ProgressPercent(dishwasher),
                     progressBeforeLoss),
            new EndToEndDeadline(1_200, 3_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause on the water-loss interruption",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the active batch reports its unavailable water supply",
            _ => EndToEndAssert.True(
                dishwasher.GetComp<CompDishwasher>()!.CompInspectStringExtra()
                    .IndexOf("paused: water supply unavailable", StringComparison.Ordinal) >= 0,
                "The ordinary dishwasher inspector must report the water-loss pause."));
        yield return new SelectionActionStep(
            "select the dishwasher paused by water loss",
            new[] { dishwasher.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the paused dishwasher and empty Dubs tower",
            new[] { dishwasher.ThingID, tower.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "Processor dishwasher visibly paused after its Dubs supply is lost",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select the empty Dubs tower during the pause",
            new[] { tower.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "connected Dubs tower visibly empty during the unchanged-progress pause",
            Array.Empty<string>(),
            paddingPixels: 0);

        HandwashingE2EFixture.SetDubsStoredWater(tower, dishwasher, waterAfterDebit);
        yield return new TimeControlActionStep(
            "resume the same batch after restoring Dubs supply",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the unchanged Processor batch resumes without a second water debit",
            _ => ProcessorFrameworkAdapter.ProgressPercent(dishwasher) >= progressBeforeLoss + 8f &&
                 HandwashingE2EFixture.Nearly(
                     HandwashingE2EFixture.ReadDubsNetworkWater(dishwasher),
                     waterAfterDebit),
            new EndToEndDeadline(1_500, 6_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause on resumed Processor progress",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the resumed dishwasher",
            new[] { dishwasher.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "same Processor dishwasher visibly washing again after water restoration",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish the resumed Processor cycle",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the same physical plate returns clean from the resumed batch",
            _ => TryResolveReturnedPlate(),
            new EndToEndDeadline(1_800, 8_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause after resumed Processor completion",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the resumed cycle conserves the exact plate and debits only once",
            _ =>
            {
                EndToEndAssert.Equal(plateId, plate.ThingID,
                    "Processor interruption must return the exact admitted plate.");
                EndToEndAssert.Equal(1, plate.stackCount,
                    "Processor interruption must conserve one physical plate.");
                HandwashingE2EFixture.AssertTotalPlateUnits(map, 1);
                EndToEndAssert.True(HandwashingE2EFixture.Nearly(
                        HandwashingE2EFixture.ReadDubsNetworkWater(dishwasher),
                        waterAfterDebit),
                    "Resuming and completing the captured batch must not debit Dubs water again.");
            });
        yield return new SelectionActionStep(
            "select the exact clean plate after resumed completion",
            new[] { plate.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the returned plate dishwasher and supplied tower",
            new[] { plate.ThingID, dishwasher.ThingID, tower.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "exact plate visibly clean after the same water-interrupted Processor batch resumes",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "Dubs Processor water interruption result",
            _ => new Dictionary<string, string>
            {
                ["plate"] = plateId,
                ["dishwasher"] = dishwasher.ThingID,
                ["waterAfterDebit"] = waterAfterDebit.ToString("R"),
                ["pausedProgress"] = progressBeforeLoss.ToString("R"),
                ["finalWater"] = HandwashingE2EFixture.ReadDubsNetworkWater(dishwasher).ToString("R")
            });
    }

    private void ResolveLoadedFixtures()
    {
        map = Current.Game.CurrentMap;
        dishwasher = ResolveSpawned<ThingWithComps>(dishwasherId);
        tower = ResolveSpawned<ThingWithComps>(towerId);
        hauler = map.mapPawns.AllPawnsSpawned.SingleOrDefault(pawn => pawn.ThingID == haulerId) ??
                 throw new EndToEndAssertionException(
                     "Native save/load lost exact Processor dishwasher hauler " + haulerId + ".");
    }

    private T ResolveSpawned<T>(string thingId) where T : Thing =>
        map.listerThings.AllThings.OfType<T>().SingleOrDefault(thing => thing.ThingID == thingId) ??
        throw new EndToEndAssertionException("Native save/load lost exact Thing " + thingId + ".");

    private bool TryResolveReturnedPlate()
    {
        var returned = map.listerThings.AllThings
            .OfType<ThingWithComps>()
            .SingleOrDefault(thing => thing.ThingID == plateId);
        if (returned?.GetComp<CompSanitation>() is not
            { IsDirty: false, WashProvenance: WashProvenance.Safe })
        {
            return false;
        }

        plate = returned;
        return true;
    }

    internal static Pawn CreateInactiveHauler(string name)
    {
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var pawn = FoodSearchE2EFixture.CreateColonist(name);
            if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling))
            {
                pawn.Destroy(DestroyMode.Vanish);
                continue;
            }

            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (!pawn.WorkTypeIsDisabled(workType))
                {
                    pawn.workSettings.SetPriority(workType, 0);
                }
            }

            return pawn;
        }

        throw new EndToEndAssertionException("Could not generate a capable Processor dishwasher hauler.");
    }

    internal static void ActivateHauler(Pawn pawn)
    {
        pawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.dubs-processor-dishwasher-exact-water-charge",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "syrchalis.processor.framework",
    "Dubwise.DubsBadHygiene",
    "fumblesneeze.immersivechefs",
    MaxFrames = 5_400,
    MaxGameTicks = 22_000,
    MaxWallClockSeconds = 240)]
public sealed class DubsProcessorDishwasherExactWaterChargeTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn hauler = null!;
    private ThingWithComps dishwasher = null!;
    private ThingWithComps tower = null!;
    private ThingWithComps plate = null!;
    private string plateId = string.Empty;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        HandwashingE2EFixture.PreserveSettings(context);
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        DispenserE2EFixture.SpawnConduitGrid(map, center, 7, 5);
        DispenserE2EFixture.SpawnPowerSources(map, center + new IntVec3(5, 0, 3), 1);

        dishwasher = DispenserE2EFixture.SpawnBuilding(
            map,
            "ImmersiveChefs_Dishwasher",
            center + new IntVec3(1, 0, 2));
        tower = DispenserE2EFixture.SpawnDubsWaterSupply(map, dishwasher, 0.1f);
        DispenserE2EFixture.SettlePower(map, new[] { dishwasher }, 400);
        EndToEndAssert.True(ProcessorFrameworkAdapter.Controls(dishwasher),
            "The exact group must exercise Processor Framework's native dishwasher path.");

        plate = HandwashingE2EFixture.MakeDirtyPlate(ThingDefOf.Steel);
        GenSpawn.Spawn(plate, center + new IntVec3(-1, 0, -2), map);
        plateId = plate.ThingID;
        hauler = DubsProcessorDishwasherWaterInterruptionTest.CreateInactiveHauler(
            "Exact-charge dishwasher hauler");
        GenSpawn.Spawn(hauler, center + new IntVec3(1, 0, -2), map);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before exact-charge native Processor hauling",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the exact dirty plate before the exact-charge cycle",
            new[] { plate.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the exact-charge dishwasher fixture",
            new[] { plate.ThingID, hauler.ThingID, dishwasher.ThingID, tower.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "one dirty plate and its exact 0.1-liter supplied batch before native hauling",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select the Dubs tower before the exact water debit",
            new[] { tower.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "Dubs tower inspector visibly starts with the exact 0.1-liter charge",
            Array.Empty<string>(),
            paddingPixels: 0);

        DubsProcessorDishwasherWaterInterruptionTest.ActivateHauler(hauler);
        yield return new TimeControlActionStep(
            "run ordinary hauling into the exact-charge Processor dishwasher",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the exact 0.1-liter charge starts the captured Processor cycle",
            _ => ProcessorFrameworkAdapter.ProgressPercent(dishwasher) >= 8f &&
                 HandwashingE2EFixture.Nearly(
                     HandwashingE2EFixture.ReadDubsNetworkWater(dishwasher),
                     0f),
            new EndToEndDeadline(1_800, 8_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause on visible exact-charge progress",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the exact-charge dishwasher while washing",
            new[] { dishwasher.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the washing dishwasher and now-empty supplied tower",
            new[] { dishwasher.ThingID, tower.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "exactly sufficient water is reserved once and the native Processor cycle visibly progresses",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select the Dubs tower after the exact water debit",
            new[] { tower.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "Dubs tower inspector visibly reaches zero after the one exact captured debit",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish the exact-charge Processor cycle",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the exact physical plate returns clean without more water",
            _ => plate.Spawned &&
                 plate.GetComp<CompSanitation>() is
                     { IsDirty: false, WashProvenance: WashProvenance.Safe },
            new EndToEndDeadline(1_800, 8_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause after exact-charge completion",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the exact-charge cycle conserves the plate and debits only its captured charge",
            _ =>
            {
                EndToEndAssert.Equal(plateId, plate.ThingID,
                    "The exact-charge cycle must return the exact admitted plate.");
                EndToEndAssert.Equal(1, plate.stackCount,
                    "The exact-charge cycle must conserve one physical plate.");
                HandwashingE2EFixture.AssertTotalPlateUnits(map, 1);
                EndToEndAssert.True(
                    HandwashingE2EFixture.Nearly(
                        HandwashingE2EFixture.ReadDubsNetworkWater(dishwasher),
                        0f),
                    "The exact-charge cycle must debit only its one captured 0.1-liter charge.");
            });
        yield return new SelectionActionStep(
            "select the exact plate after exact-charge completion",
            new[] { plate.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the exact clean plate and completed dishwasher",
            new[] { plate.ThingID, dishwasher.ThingID, tower.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "same exact plate visibly returns clean after the exact-charge cycle",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "Dubs Processor exact water charge result",
            _ => new Dictionary<string, string>
            {
                ["plate"] = plateId,
                ["dishwasher"] = dishwasher.ThingID,
                ["tower"] = tower.ThingID,
                ["finalWater"] = HandwashingE2EFixture.ReadDubsNetworkWater(dishwasher).ToString("R")
            });
    }
}
