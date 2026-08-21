using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.pick-up-and-haul-local-dishwasher-batch",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "Dubwise.DubsBadHygiene",
    "Mehni.PickUpAndHaul",
    "fumblesneeze.immersivechefs",
    MaxFrames = 3_600,
    MaxGameTicks = 12_000,
    MaxWallClockSeconds = 150)]
public sealed class PickUpAndHaulLocalDishwasherBatchTest : IRimWorldEndToEndTest
{
    private PickUpAndHaulDishwasherFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = PickUpAndHaulDishwasherFixture.Create(context, requireProcessor: false);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before local dishwasher collection",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "enable ordinary Cleaning for the local appliance",
            _ => fixture.ActivateCleaning());
        yield return new TimeControlActionStep(
            "run one native local-dishwasher collection trip",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "one local-dishwasher job carries the full tracked batch",
            _ => fixture.AllDirtyWareTrackedAtDishwasher(),
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(65)));
        yield return new WaitUntilStep(
            "the local dishwasher owns all three exact units",
            _ => fixture.AllWareOwnedByDishwasher(),
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(65)));
        yield return new TimeControlActionStep(
            "pause after local holder admission",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the loaded local dishwasher",
            new[] { fixture.Dishwasher.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the local dishwasher and cleaner",
            new[] { fixture.Dishwasher.ThingID, fixture.Cleaner.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "the non-Processor appliance visibly owns the full one-trip batch",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "local holder admission releases every upstream tracking claim exactly once",
            _ => fixture.AssertLoadedConservation());
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.pick-up-and-haul-processor-dishwasher-batch",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "syrchalis.processor.framework",
    "Dubwise.DubsBadHygiene",
    "Mehni.PickUpAndHaul",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 30_000,
    MaxWallClockSeconds = 300)]
public sealed class PickUpAndHaulProcessorDishwasherBatchTest : IRimWorldEndToEndTest
{
    private PickUpAndHaulDishwasherFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = PickUpAndHaulDishwasherFixture.Create(context, requireProcessor: true);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before one-trip dishwasher collection",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the dirty mixed kitchenware",
            fixture.WareIds,
            additive: false);
        yield return new CameraActionStep(
            "frame dirty ware cleaner dishwasher and clean shelf",
            fixture.VisibleThingIds,
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "dirty mixed kitchenware awaits one cleaner beside the supplied dishwasher",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new AssertionStep(
            "enable the ordinary Cleaning workgiver",
            _ => fixture.ActivateCleaning());
        yield return new TimeControlActionStep(
            "run the ordinary Doing dishes job",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "one Doing dishes job collects all exact units before one dishwasher trip",
            _ => fixture.AllDirtyWareTrackedAtDishwasher(),
            new EndToEndDeadline(3_600, 8_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause while the cleaner carries the full dirty batch",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the cleaner carrying the tracked dirty batch",
            new[] { fixture.Cleaner.ThingID },
            additive: false);
        yield return new PawnInspectTabActionStep(
            "open the cleaner Gear tab before dishwasher admission",
            fixture.Cleaner.ThingID,
            EndToEndPawnInspectTab.Gear);
        yield return new ScreenshotStep(
            "Pick Up And Haul visibly carries the dirty mixed batch on one dishwasher trip",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "admit each tracked unit through Processor Framework",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the exact batch transfers from tracked inventory into one dishwasher",
            _ => fixture.AllWareOwnedByDishwasher(),
            new EndToEndDeadline(1_500, 5_000, TimeSpan.FromSeconds(55)));
        yield return new TimeControlActionStep(
            "pause after exact ownership transfer",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the loaded Processor dishwasher",
            new[] { fixture.Dishwasher.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "the appliance visibly owns all three exact stacks after one collection trip",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "suspend output work while every independent load finishes naturally",
            _ => fixture.SuspendOutputWork());

        yield return new TimeControlActionStep(
            "run the supplied native dishwasher cycle",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "every exact Processor output finishes naturally and remains appliance-owned",
            _ => fixture.AllProcessorOutputsNaturallyComplete(),
            new EndToEndDeadline(2_400, 12_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause before assigning the full-batch output job",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "open clean storage before the native empty-work assignment",
            _ => fixture.EnableCleanStorage());
        yield return new AssertionStep(
            "place the cleaner far enough away to observe the full-batch assignment",
            _ => fixture.RelocateCleanerForOutputTransit());
        yield return new AssertionStep(
            "enable ordinary hauling while cleaning remains available",
            _ => fixture.ActivateHaulingWhileCleaningRemainsEnabled());
        yield return new TimeControlActionStep(
            "let native empty work collect every completed output",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "observe the native Processor empty-work assignment in transit",
            _ => fixture.ObserveProcessorEmptyWorkInTransit(),
            new EndToEndDeadline(600, 2_000, TimeSpan.FromSeconds(30)));
        yield return new WaitUntilStep(
            "the full completed batch leaves the Processor dishwasher",
            _ => fixture.ObserveProcessorOutputExtraction(),
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(55)));
        yield return new AssertionStep(
            "one immediate extraction puts the full clean batch into one native unload",
            _ => fixture.AssertProcessorOutputBatchInSingleUnload());
        yield return new TimeControlActionStep(
            "pause while one native unload owns the full completed batch",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the cleaner carrying all dishwasher outputs",
            new[] { fixture.Cleaner.ThingID },
            additive: false);
        yield return new PawnInspectTabActionStep(
            "open Gear on the completed output batch",
            fixture.Cleaner.ThingID,
            EndToEndPawnInspectTab.Gear);
        yield return new ScreenshotStep(
            "one instant dishwasher extraction visibly tracks every clean output",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new TimeControlActionStep(
            "run the single native Pick Up And Haul unload",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "all exact clean outputs reach the kitchen shelf",
            _ => fixture.AllWareStoredClean(),
            new EndToEndDeadline(1_800, 7_000, TimeSpan.FromSeconds(70)));
        yield return new TimeControlActionStep(
            "pause after native batch unloading",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the neatly stored clean kitchenware",
            fixture.WareIds,
            additive: false);
        yield return new ScreenshotStep(
            "Pick Up And Haul has returned the exact clean batch to its kitchen shelf",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "conserve exact identities sanitation and physical units",
            _ => fixture.AssertFinalConservation());
        yield return new CheckpointStep(
            "instant full-fit dishwasher output timing",
            _ => fixture.OutputBatchCheckpoint());
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.pick-up-and-haul-processor-dishwasher-capacity-boundary",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "syrchalis.processor.framework",
    "Dubwise.DubsBadHygiene",
    "Mehni.PickUpAndHaul",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 30_000,
    MaxWallClockSeconds = 300)]
public sealed class PickUpAndHaulProcessorDishwasherCapacityBoundaryTest : IRimWorldEndToEndTest
{
    private const int InitialPlateCount = 4;
    private const int FirstBatchCount = 2;
    private PickUpAndHaulDishwasherFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = PickUpAndHaulDishwasherFixture.Create(
            context,
            requireProcessor: true,
            plateOnly: true,
            plateCount: InitialPlateCount);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before the stacked dishwasher capacity workflow",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "enable ordinary Cleaning for the stacked load",
            _ => fixture.ActivateCleaning());
        yield return new TimeControlActionStep(
            "let one native dishwashing job admit the stacked load",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the exact stacked load enters the Processor dishwasher",
            _ => fixture.AllWareOwnedByDishwasher(),
            new EndToEndDeadline(3_600, 8_000, TimeSpan.FromSeconds(75)));
        yield return new AssertionStep(
            "suspend output work while the stacked load finishes naturally",
            _ => fixture.SuspendOutputWork());
        yield return new TimeControlActionStep(
            "run the native dishwasher until the stacked output naturally completes",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the complete stack remains appliance-owned before output work",
            _ => fixture.AllProcessorOutputsNaturallyComplete(),
            new EndToEndDeadline(2_400, 12_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause before constraining output inventory capacity",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "leave capacity for exactly two completed plates",
            _ => fixture.ConstrainOutputCapacity(FirstBatchCount));
        yield return new AssertionStep(
            "open clean storage for both capacity-bound unloads",
            _ => fixture.EnableCleanStorage());
        yield return new AssertionStep(
            "place the cleaner far enough away to observe the first bounded assignment",
            _ => fixture.RelocateCleanerForOutputTransit());
        yield return new AssertionStep(
            "enable the native Processor and hauling work paths",
            _ => fixture.ActivateHaulingWhileCleaningRemainsEnabled());
        yield return new TimeControlActionStep(
            "let the first native empty job fill only remaining capacity",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "observe the first native Processor empty assignment in transit",
            _ => fixture.ObserveProcessorEmptyWorkInTransit(),
            new EndToEndDeadline(600, 2_000, TimeSpan.FromSeconds(30)));
        yield return new WaitUntilStep(
            "exactly two split units enter one tracked unload",
            _ => fixture.ObservePartialProcessorOutputBatchInInventory(FirstBatchCount),
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(55)));
        yield return new TimeControlActionStep(
            "pause on the capacity-bound tracked inventory",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the cleaner carrying the partial completed stack",
            new[] { fixture.Cleaner.ThingID },
            additive: false);
        yield return new PawnInspectTabActionStep(
            "open Gear on the capacity-bound output batch",
            fixture.Cleaner.ThingID,
            EndToEndPawnInspectTab.Gear);
        yield return new ScreenshotStep(
            "the first immediate extraction visibly stops at pawn mass capacity",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "prevent a second empty assignment while the first unload runs",
            _ => fixture.DisableHaulingWithoutInterruptingCurrentJob());
        yield return new TimeControlActionStep(
            "let the first native unload store only its capacity-bound batch",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the first batch is stored while the completed remainder stays in the dishwasher",
            _ => fixture.FirstPartialBatchStoredWithRemainderHeld(FirstBatchCount),
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(55)));
        yield return new TimeControlActionStep(
            "pause before admitting the remainder's empty-work assignment",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "place the cleaner far enough away to observe the remainder assignment",
            _ => fixture.RelocateCleanerForOutputTransit());
        yield return new AssertionStep(
            "re-enable native hauling for the completed remainder",
            _ => fixture.ActivateHaulingWhileCleaningRemainsEnabled());
        yield return new TimeControlActionStep(
            "let the second native empty job collect the remainder",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "observe the second native empty assignment for the completed remainder",
            _ => fixture.ObserveProcessorEmptyWorkAssignment(expectedOrdinal: 2),
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(55)));
        yield return new WaitUntilStep(
            "the second immediate extraction owns exactly one new native unload",
            _ => fixture.ObservePartialProcessorRemainderBatchInInventory(FirstBatchCount),
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(55)));
        yield return new WaitUntilStep(
            "both the split batch and completed remainder reach clean storage",
            _ => fixture.AllPartialOutputStoredClean(),
            new EndToEndDeadline(2_400, 10_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause after both capacity-bound unloads",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the conserved split output stacks",
            fixture.PartialOutputStoredIds,
            additive: false);
        yield return new ScreenshotStep(
            "all four completed units are clean and stored after bounded hauling",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "conserve the partial stack boundary without duplication or loss",
            _ => fixture.AssertPartialOutputConservation(FirstBatchCount));
        yield return new CheckpointStep(
            "capacity-bound dishwasher output timing",
            _ => fixture.OutputBatchCheckpoint());
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.pick-up-and-haul-processor-dishwasher-capacity-race-fallback",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "syrchalis.processor.framework",
    "Dubwise.DubsBadHygiene",
    "Mehni.PickUpAndHaul",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 30_000,
    MaxWallClockSeconds = 300)]
public sealed class PickUpAndHaulProcessorDishwasherCapacityRaceFallbackTest : IRimWorldEndToEndTest
{
    private PickUpAndHaulDishwasherFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = PickUpAndHaulDishwasherFixture.Create(
            context,
            requireProcessor: true,
            plateOnly: true,
            plateCount: 1);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before the output-capacity race workflow",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "enable ordinary Cleaning for the single output",
            _ => fixture.ActivateCleaning());
        yield return new TimeControlActionStep(
            "admit the single plate through ordinary dishwashing work",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the exact plate enters the Processor dishwasher",
            _ => fixture.AllWareOwnedByDishwasher(),
            new EndToEndDeadline(3_600, 8_000, TimeSpan.FromSeconds(75)));
        yield return new AssertionStep(
            "suspend output work while the single load finishes naturally",
            _ => fixture.SuspendOutputWork());
        yield return new TimeControlActionStep(
            "run the dishwasher until the output naturally completes",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the completed plate remains appliance-owned",
            _ => fixture.AllProcessorOutputsNaturallyComplete(),
            new EndToEndDeadline(2_400, 12_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause before assigning native output work",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "open clean storage before Processor output work",
            _ => fixture.EnableCleanStorage());
        yield return new AssertionStep(
            "place the cleaner far enough away to observe admitted transit",
            _ => fixture.RelocateCleanerForOutputTransit());
        yield return new AssertionStep(
            "enable ordinary Processor and hauling work",
            _ => fixture.ActivateHaulingWhileCleaningRemainsEnabled());
        yield return new TimeControlActionStep(
            "admit the native empty job while capacity is available",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the native empty job is in transit before capacity changes",
            _ => fixture.ObserveProcessorEmptyWorkInTransit(),
            new EndToEndDeadline(600, 2_000, TimeSpan.FromSeconds(30)));
        yield return new TimeControlActionStep(
            "pause the admitted empty job during transit",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "fill remaining inventory capacity after job admission",
            _ => fixture.ConstrainOutputCapacity(fittingUnitCount: 0));
        yield return new TimeControlActionStep(
            "resume the already-admitted empty job",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the appliance-arrival branch retains Processor Framework's stock output path",
            _ => fixture.ObserveStockCapacityRaceFallback(),
            new EndToEndDeadline(1_800, 7_000, TimeSpan.FromSeconds(70)));
        yield return new TimeControlActionStep(
            "pause after the stock one-output haul",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the clean stock-hauled output",
            fixture.PartialOutputStoredIds,
            additive: false);
        yield return new ScreenshotStep(
            "the capacity race falls back to Processor Framework storage without tracked inventory",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "retain one stock empty job without PUAH tracking or over-encumbrance",
            _ => fixture.AssertStockCapacityRaceFallback());
        yield return new CheckpointStep(
            "runtime capacity-race stock fallback",
            _ => fixture.StockFallbackCheckpoint());
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.processor-dishwasher-continuous-admission",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "syrchalis.processor.framework",
    "Dubwise.DubsBadHygiene",
    "Mehni.PickUpAndHaul",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 24_000,
    MaxWallClockSeconds = 240)]
public sealed class ProcessorDishwasherContinuousAdmissionTest : IRimWorldEndToEndTest
{
    private PickUpAndHaulDishwasherFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = PickUpAndHaulDishwasherFixture.Create(context, requireProcessor: true);
        fixture.PrepareContinuousAdmission();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before the first independent dishwasher load",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "enable ordinary Cleaning for only the first dirty item",
            _ => fixture.ActivateCleaning());
        yield return new TimeControlActionStep(
            "let native Cleaning admit and start the first load",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the first exact load is visibly partway through washing",
            _ => fixture.FirstContinuousLoadHasProgress(),
            new EndToEndDeadline(2_400, 7_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause on the first load's established progress",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "capture the first load progress and its separate water debit",
            _ => fixture.CaptureFirstContinuousLoad());
        yield return new SelectionActionStep(
            "select the dishwasher washing only the first load",
            new[] { fixture.Dishwasher.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the active dishwasher and later dirty item",
            fixture.ContinuousVisibleThingIds,
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "one exact load is already washing while later ware remains outside",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new AssertionStep(
            "make the later dirty item available to ordinary Cleaning",
            _ => fixture.ReleaseLaterContinuousLoad());
        yield return new TimeControlActionStep(
            "let native Cleaning add the later load without stopping the first",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "both exact loads wash in parallel on different progress clocks",
            _ => fixture.ObserveParallelContinuousLoads(),
            new EndToEndDeadline(2_400, 7_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause after the later parallel admission",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the continuously loaded dishwasher",
            new[] { fixture.Dishwasher.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "later ware joins active washing without resetting the older load",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new AssertionStep(
            "enable native output hauling while Cleaning remains active",
            _ => fixture.ActivateHaulingWhileCleaningRemainsEnabled());
        yield return new TimeControlActionStep(
            "run both independent loads toward their own completion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the older load exits clean while the later load is still washing",
            _ => fixture.FirstContinuousLoadCompletesBeforeLaterLoad(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(85)));
        yield return new TimeControlActionStep(
            "pause between the two independent completions",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the older clean output and still-active dishwasher",
            new[] { fixture.FirstContinuousWareId, fixture.Dishwasher.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "older ware is clean outside while the later load remains in progress",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish the later independent load",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the later exact load also returns clean",
            _ => fixture.BothContinuousLoadsAreCleanOutputs(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(85)));
        yield return new TimeControlActionStep(
            "pause after both independent completions",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select both exact clean outputs",
            fixture.ContinuousWareIds,
            additive: false);
        yield return new ScreenshotStep(
            "both separately admitted identities return clean",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "conserve both identities progress order and per-admission water",
            _ => fixture.AssertContinuousAdmission());
        yield return new CheckpointStep(
            "continuous dishwasher admission result",
            _ => fixture.ContinuousAdmissionCheckpoint());
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.pick-up-and-haul-processor-dishwasher-interruption",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "syrchalis.processor.framework",
    "Dubwise.DubsBadHygiene",
    "Mehni.PickUpAndHaul",
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_800,
    MaxGameTicks = 16_000,
    MaxWallClockSeconds = 180)]
public sealed class PickUpAndHaulProcessorDishwasherInterruptionTest : IRimWorldEndToEndTest
{
    private PickUpAndHaulDishwasherFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = PickUpAndHaulDishwasherFixture.Create(context, requireProcessor: true);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before partial Processor admission",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the interruption cleaner",
            new[] { fixture.Cleaner.ThingID },
            additive: false);
        yield return new AssertionStep(
            "enable ordinary Cleaning before partial admission",
            _ => fixture.ActivateCleaning());
        yield return new TimeControlActionStep(
            "run the one-trip Doing dishes job",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "one exact unit enters the dishwasher while two remain tracked",
            _ => fixture.ObservePartialDishwasherAdmission(),
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(65)));
        yield return new TimeControlActionStep(
            "pause before the player interrupts admission",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new PawnInspectTabActionStep(
            "open Gear on the two still-carried dirty units",
            fixture.Cleaner.ThingID,
            EndToEndPawnInspectTab.Gear);
        yield return new ScreenshotStep(
            "one unit belongs to the dishwasher while two remain in tracked inventory",
            Array.Empty<string>(),
            paddingPixels: 0);
        var draft = fixture.RequiredDraftToggle(context);
        yield return new GizmoActionStep(
            "draft through the native pawn toggle to interrupt admission",
            new[] { fixture.Cleaner.ThingID },
            draft.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draft.StableId);
        yield return new WaitUntilStep(
            "interruption leaves admitted and carried ownership disjoint",
            _ => fixture.PartialOwnershipRemainsDisjoint(),
            new EndToEndDeadline(300, 500, TimeSpan.FromSeconds(20)));
        yield return new AssertionStep(
            "disable new Cleaning work before upstream unloading",
            _ => fixture.DisableCleaning());
        draft = fixture.RequiredDraftToggle(context);
        yield return new GizmoActionStep(
            "undraft through the native pawn toggle",
            new[] { fixture.Cleaner.ThingID },
            draft.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draft.StableId);
        yield return new TimeControlActionStep(
            "run only the queued upstream return",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "unadmitted units leave personal inventory through native unloading",
            _ => fixture.UnadmittedWareReturnedAfterInterruption(),
            new EndToEndDeadline(1_500, 5_000, TimeSpan.FromSeconds(55)));
        yield return new TimeControlActionStep(
            "pause on the disjoint interrupted ownership",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the still-dirty returned units",
            fixture.ReturnedUnadmittedWareIds,
            additive: false);
        yield return new CameraActionStep(
            "frame the admitted dishwasher and returned unadmitted units",
            fixture.InterruptedVisibleThingIds,
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "admitted ware remains appliance-owned while unadmitted ware returns dirty",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "partial admission conserves all exact physical units",
            _ => fixture.AssertInterruptedConservation());
    }
}

internal sealed class PickUpAndHaulDishwasherFixture
{
    private const int MaxObservedImmediateTransferTicks = 30;
    private const string TrackerTypeName = "PickUpAndHaul.CompHauledToInventory";
    private const string EmptyProcessorDriverTypeName = "ProcessorFramework.JobDriver_EmptyProcessor";
    private const string UnloadDriverTypeName = "PickUpAndHaul.JobDriver_UnloadYourHauledInventory";
    private readonly Map map;
    private readonly ThingWithComps waterTower;
    private readonly IReadOnlyList<ThingWithComps> ware;
    private readonly int initialWareUnitCount;
    private readonly Zone_Stockpile cleanStorage;
    private readonly HashSet<IntVec3> cleanStorageCells;
    private bool observedCleanBatchHaul;
    private ThingWithComps? admittedBeforeInterruption;
    private float firstContinuousProgressBeforeLaterAdmission;
    private float firstContinuousProgressAfterLaterAdmission;
    private float laterContinuousProgressAfterAdmission;
    private float waterAfterFirstContinuousAdmission;
    private float waterAfterLaterContinuousAdmission;
    private int partialOutputInitialCount;
    private int partialOutputFirstBatchCount;
    private ThingDef? partialOutputDef;
    private ThingDef? partialOutputStuff;
    private readonly HashSet<string> partialOutputFirstBatchIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> partialOutputRemainderIds = new(StringComparer.Ordinal);
    private readonly HashSet<int> observedOutputEmptyJobIds = new();
    private readonly HashSet<int> observedOutputUnloadJobIds = new();
    private readonly Dictionary<int, int> outputJobStartTicks = new();
    private readonly Dictionary<int, int> outputArrivalTicks = new();
    private readonly Dictionary<int, int> outputTransferTicks = new();
    private readonly HashSet<int> outputJobsWithStockDelay = new();
    private int lastOutputEmptyJobId = -1;
    private int naturalCompletionObservationStartedTick = -1;
    private int stockFallbackCompletedTick = -1;

    private PickUpAndHaulDishwasherFixture(
        Map map,
        Pawn cleaner,
        ThingWithComps dishwasher,
        ThingWithComps waterTower,
        IReadOnlyList<ThingWithComps> ware,
        Zone_Stockpile cleanStorage,
        HashSet<IntVec3> cleanStorageCells)
    {
        this.map = map;
        Cleaner = cleaner;
        Dishwasher = dishwasher;
        this.waterTower = waterTower;
        this.ware = ware;
        initialWareUnitCount = ware.Sum(item => item.stackCount);
        this.cleanStorage = cleanStorage;
        this.cleanStorageCells = cleanStorageCells;
    }

    internal Pawn Cleaner { get; }
    internal ThingWithComps Dishwasher { get; }
    internal string[] WareIds => ware.Select(item => item.ThingID).ToArray();
    internal string[] VisibleThingIds => WareIds.Concat(new[]
    {
        Cleaner.ThingID,
        Dishwasher.ThingID,
        waterTower.ThingID
    }).ToArray();
    internal string[] ReturnedUnadmittedWareIds => ware
        .Where(item => !ReferenceEquals(item, admittedBeforeInterruption))
        .Select(item => item.ThingID)
        .ToArray();
    internal string[] InterruptedVisibleThingIds => ReturnedUnadmittedWareIds.Concat(new[]
    {
        Cleaner.ThingID,
        Dishwasher.ThingID,
        waterTower.ThingID
    }).ToArray();
    internal string FirstContinuousWareId => ware[0].ThingID;
    internal string[] ContinuousWareIds => ware.Take(2).Select(item => item.ThingID).ToArray();
    internal string[] ContinuousVisibleThingIds => new[]
    {
        ware[1].ThingID,
        Cleaner.ThingID,
        Dishwasher.ThingID,
        waterTower.ThingID
    };

    internal static PickUpAndHaulDishwasherFixture Create(
        IEndToEndContext context,
        bool requireProcessor,
        bool plateOnly = false,
        int plateCount = 1)
    {
        var map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        HandwashingE2EFixture.PreserveSettings(context);
        var settings = ImmersiveChefsMod.Settings;
        var priorPickUpAndHaul = settings.PickUpAndHaul;
        context.DeferCleanup(() => settings.PickUpAndHaul = priorPickUpAndHaul);
        settings.PickUpAndHaul = OptionalIntegrationMode.Auto;
        settings.DubsBadHygiene = OptionalIntegrationMode.Auto;
        settings.PreferDishwashers = true;
        settings.DishwashingWorkScale = requireProcessor ? 0.25f : 4f;

        var powerCell = SpawnPowerSource(map, center + new IntVec3(5, 0, 3));
        SpawnConduitsOutside(map, center, 7, 5, powerCell.OccupiedRect().Cells.ToHashSet());
        var dishwasher = DispenserE2EFixture.SpawnBuilding(
            map,
            "ImmersiveChefs_Dishwasher",
            center + new IntVec3(2, 0, 1));
        var tower = DispenserE2EFixture.SpawnDubsWaterSupply(map, dishwasher, 10f);
        DispenserE2EFixture.SettlePower(map, new[] { dishwasher }, 400);
        EndToEndAssert.Equal(requireProcessor, ProcessorFrameworkAdapter.Controls(dishwasher),
            requireProcessor
                ? "The exact Processor group must use the supported Processor Framework dishwasher."
                : "The exact local group must exercise the Immersive Chefs holder without Processor Framework.");

        var cleaner = HandwashingE2EFixture.CreateInactiveCleaner("Mara Dishrunner");
        GenSpawn.Spawn(cleaner, center + new IntVec3(-4, 0, -2), map);
        EndToEndAssert.Equal(1,
            cleaner.AllComps.Count(comp => comp.GetType().FullName == TrackerTypeName),
            "The supported Pick Up And Haul tracker must attach exactly once.");

        var plate = MakeDirtyWare("ImmersiveChefs_Plate", ThingDefOf.Steel);
        plate.stackCount = plateCount;
        var ware = plateOnly
            ? new[] { plate }
            : new[]
            {
                plate,
                MakeDirtyWare("ImmersiveChefs_Cutlery", ThingDefOf.WoodLog),
                MakeDirtyWare("ImmersiveChefs_Cookware", ThingDefOf.Steel)
            };
        for (var index = 0; index < ware.Length; index++)
        {
            GenSpawn.Spawn(ware[index], center + new IntVec3(-3 + index, 0, -1), map);
        }

        var zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        map.zoneManager.RegisterZone(zone);
        var cells = new HashSet<IntVec3>();
        for (var x = 2; x <= 4; x++)
        {
            for (var z = -3; z <= -2; z++)
            {
                var cell = center + new IntVec3(x, 0, z);
                zone.AddCell(cell);
                cells.Add(cell);
            }
        }

        zone.settings.Priority = StoragePriority.Critical;
        zone.settings.filter.SetDisallowAll();
        map.resourceCounter.UpdateResourceCounts();
        return new PickUpAndHaulDishwasherFixture(
            map,
            cleaner,
            dishwasher,
            tower,
            ware,
            zone,
            cells);
    }

    internal void ActivateCleaning()
    {
        Cleaner.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 1);
        Cleaner.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    internal void PrepareContinuousAdmission()
    {
        ware[1].SetForbidden(true, warnOnFail: false);
        ware[2].SetForbidden(true, warnOnFail: false);
    }

    internal bool FirstContinuousLoadHasProgress()
    {
        var held = DishwasherHeldWare();
        var progress = ProcessorFrameworkAdapter.ProgressPercent(Dishwasher, ware[0]);
        return held.Count == 1 &&
               held.Any(item => ReferenceEquals(item, ware[0])) &&
               progress >= 12f && progress < 70f &&
               ware[1].Spawned && ware[1].IsForbidden(Faction.OfPlayer);
    }

    internal void CaptureFirstContinuousLoad()
    {
        firstContinuousProgressBeforeLaterAdmission =
            ProcessorFrameworkAdapter.ProgressPercent(Dishwasher, ware[0]);
        waterAfterFirstContinuousAdmission =
            HandwashingE2EFixture.ReadDubsNetworkWater(Dishwasher);
        EndToEndAssert.True(firstContinuousProgressBeforeLaterAdmission >= 12f,
            "The first load must establish independent progress before later admission.");
        EndToEndAssert.True(HandwashingE2EFixture.Nearly(waterAfterFirstContinuousAdmission, 9.9f),
            "The first plate admission must debit only its own 0.1-liter charge.");
    }

    internal void ReleaseLaterContinuousLoad()
    {
        ware[1].SetForbidden(false, warnOnFail: false);
        Cleaner.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    internal bool ObserveParallelContinuousLoads()
    {
        var held = DishwasherHeldWare();
        if (!held.Any(item => ReferenceEquals(item, ware[0])) ||
            !held.Any(item => ReferenceEquals(item, ware[1])))
        {
            return false;
        }

        var firstProgress = ProcessorFrameworkAdapter.ProgressPercent(Dishwasher, ware[0]);
        var laterProgress = ProcessorFrameworkAdapter.ProgressPercent(Dishwasher, ware[1]);
        if (firstProgress + 0.01f < firstContinuousProgressBeforeLaterAdmission ||
            laterProgress >= firstProgress - 5f)
        {
            return false;
        }

        firstContinuousProgressAfterLaterAdmission = firstProgress;
        laterContinuousProgressAfterAdmission = laterProgress;
        waterAfterLaterContinuousAdmission =
            HandwashingE2EFixture.ReadDubsNetworkWater(Dishwasher);
        return HandwashingE2EFixture.Nearly(waterAfterLaterContinuousAdmission, 9.875f);
    }

    internal bool FirstContinuousLoadCompletesBeforeLaterLoad()
    {
        return ware[0].Spawned &&
               ware[0].GetComp<CompSanitation>() is
                   { IsDirty: false, WashProvenance: WashProvenance.Safe } &&
               DishwasherHeldWare().Any(item => ReferenceEquals(item, ware[1])) &&
               ware[1].GetComp<CompSanitation>()?.IsDirty == true &&
               ProcessorFrameworkAdapter.ProgressPercent(Dishwasher, ware[1]) is > 0f and < 100f;
    }

    internal bool BothContinuousLoadsAreCleanOutputs()
    {
        return ware.Take(2).All(item =>
            item.Spawned && item.Map == map &&
            item.GetComp<CompSanitation>() is
                { IsDirty: false, WashProvenance: WashProvenance.Safe });
    }

    internal void AssertContinuousAdmission()
    {
        EndToEndAssert.True(BothContinuousLoadsAreCleanOutputs(),
            "Both separately admitted exact loads must return clean.");
        EndToEndAssert.True(
            firstContinuousProgressAfterLaterAdmission + 0.01f >=
            firstContinuousProgressBeforeLaterAdmission,
            "Later admission must not reset the first load's established progress.");
        EndToEndAssert.True(
            firstContinuousProgressAfterLaterAdmission > laterContinuousProgressAfterAdmission + 5f,
            "The later load must begin on its own younger progress clock.");
        EndToEndAssert.True(
            HandwashingE2EFixture.Nearly(waterAfterFirstContinuousAdmission, 9.9f) &&
            HandwashingE2EFixture.Nearly(waterAfterLaterContinuousAdmission, 9.875f) &&
            HandwashingE2EFixture.Nearly(
                HandwashingE2EFixture.ReadDubsNetworkWater(Dishwasher),
                9.875f),
            "Plate and cutlery admissions must debit separate 0.1- and 0.025-liter charges exactly once.");
        EndToEndAssert.Equal(2, ContinuousWareIds.Distinct().Count(),
            "Continuous admission must preserve both exact identities.");
        EndToEndAssert.True(ware.Take(2).All(item => item.stackCount == 1),
            "Continuous admission must conserve one physical unit for each exact load.");
    }

    internal Dictionary<string, string> ContinuousAdmissionCheckpoint()
    {
        return new Dictionary<string, string>
        {
            ["firstWare"] = ware[0].ThingID,
            ["laterWare"] = ware[1].ThingID,
            ["firstProgressBeforeLater"] = firstContinuousProgressBeforeLaterAdmission.ToString("R"),
            ["firstProgressAfterLater"] = firstContinuousProgressAfterLaterAdmission.ToString("R"),
            ["laterProgressAfterAdmission"] = laterContinuousProgressAfterAdmission.ToString("R"),
            ["waterAfterFirstAdmission"] = waterAfterFirstContinuousAdmission.ToString("R"),
            ["waterAfterLaterAdmission"] = waterAfterLaterContinuousAdmission.ToString("R")
        };
    }

    internal Dictionary<string, string> OutputBatchCheckpoint()
    {
        var checkpoint = new Dictionary<string, string>
        {
            ["emptyJobCount"] = observedOutputEmptyJobIds.Count.ToString(),
            ["unloadJobCount"] = observedOutputUnloadJobIds.Count.ToString(),
            ["stockDelayCount"] = outputJobsWithStockDelay.Count.ToString()
        };
        var index = 0;
        foreach (var jobId in observedOutputEmptyJobIds.OrderBy(id => outputJobStartTicks[id]))
        {
            checkpoint[$"emptyJob{index}Id"] = jobId.ToString();
            checkpoint[$"emptyJob{index}StartTick"] = outputJobStartTicks[jobId].ToString();
            checkpoint[$"emptyJob{index}TransferTick"] = outputTransferTicks[jobId].ToString();
            if (outputArrivalTicks.TryGetValue(jobId, out var arrivalTick))
            {
                checkpoint[$"emptyJob{index}ArrivalTick"] = arrivalTick.ToString();
                checkpoint[$"emptyJob{index}ArrivalToTransferTicks"] =
                    (outputTransferTicks[jobId] - arrivalTick).ToString();
            }
            else
            {
                checkpoint[$"emptyJob{index}ArrivalTick"] = "not-sampled-two-tick-transition";
                checkpoint[$"emptyJob{index}ArrivalToTransferTicks"] = "within-frame-poll-gap";
            }
            index++;
        }

        return checkpoint;
    }

    internal void ActivateHaulingWhileCleaningRemainsEnabled()
    {
        Cleaner.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 1);
        Cleaner.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
        Cleaner.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    internal bool ObserveProcessorEmptyWorkAssignment(int expectedOrdinal = 1)
    {
        ObserveOutputJobTiming();
        return observedOutputEmptyJobIds.Count >= expectedOrdinal;
    }

    internal void DisableCleaning()
    {
        Cleaner.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 0);
    }

    internal void SuspendOutputWork()
    {
        Cleaner.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 0);
        Cleaner.workSettings.SetPriority(WorkTypeDefOf.Hauling, 0);
        Cleaner.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    internal void DisableHaulingWithoutInterruptingCurrentJob()
    {
        Cleaner.workSettings.SetPriority(WorkTypeDefOf.Hauling, 0);
    }

    internal bool AllDirtyWareTrackedAtDishwasher()
    {
        return Cleaner.CurJobDef == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
               Cleaner.Position.DistanceToSquared(Dishwasher.InteractionCell) <= 2 &&
               ware.All(item => ReferenceEquals(item.holdingOwner, Cleaner.inventory?.innerContainer)) &&
               ware.All(IsTracked) &&
               ware.All(item => item.GetComp<CompSanitation>()!.IsDirty);
    }

    internal bool AllWareOwnedByDishwasher()
    {
        var held = DishwasherHeldWare();
        return ware.All(item => held.Any(candidate => ReferenceEquals(candidate, item))) &&
               ware.All(item => !IsTracked(item)) &&
               ware.All(item => !item.Spawned) &&
               Cleaner.CurJobDef != ImmersiveChefsDefOf.ImmersiveChefs_DoDishes;
    }

    internal bool ObservePartialDishwasherAdmission()
    {
        var held = DishwasherHeldWare();
        var admitted = ware.Where(item => held.Any(candidate => ReferenceEquals(candidate, item))).ToArray();
        var carried = ware.Where(item => ReferenceEquals(item.holdingOwner, Cleaner.inventory?.innerContainer)).ToArray();
        if (admitted.Length != 1 || carried.Length != 2 || !carried.All(IsTracked))
        {
            return false;
        }

        admittedBeforeInterruption = admitted[0];
        return !IsTracked(admittedBeforeInterruption);
    }

    internal bool PartialOwnershipRemainsDisjoint()
    {
        if (admittedBeforeInterruption is null)
        {
            return false;
        }

        var held = DishwasherHeldWare();
        return held.Any(item => ReferenceEquals(item, admittedBeforeInterruption)) &&
               !IsTracked(admittedBeforeInterruption) &&
               ware.Where(item => !ReferenceEquals(item, admittedBeforeInterruption)).All(item =>
                   ReferenceEquals(item.holdingOwner, Cleaner.inventory?.innerContainer) && IsTracked(item));
    }

    internal bool UnadmittedWareReturnedAfterInterruption()
    {
        if (admittedBeforeInterruption is null)
        {
            return false;
        }

        var held = DishwasherHeldWare();
        return held.Any(item => ReferenceEquals(item, admittedBeforeInterruption)) &&
               ware.Where(item => !ReferenceEquals(item, admittedBeforeInterruption)).All(item =>
                   item.Spawned && item.Map == map && item.GetComp<CompSanitation>()!.IsDirty && !IsTracked(item));
    }

    internal bool ObserveCycleProgress()
    {
        return ProcessorFrameworkAdapter.ProgressPercent(Dishwasher) >= 5f &&
               HandwashingE2EFixture.ReadDubsNetworkWater(Dishwasher) < 10f;
    }

    internal bool AllProcessorOutputsNaturallyComplete()
    {
        if (naturalCompletionObservationStartedTick < 0)
        {
            naturalCompletionObservationStartedTick = Find.TickManager.TicksGame;
        }

        var heldCount = DishwasherHeldWare().Sum(item => item.stackCount);
        var expectedCount = initialWareUnitCount;
        var allNatural = ProcessorFrameworkAdapter.AllProcessesNaturallyComplete(Dishwasher);
        var complete = ProcessorFrameworkAdapter.Controls(Dishwasher) &&
                       allNatural &&
                       heldCount == expectedCount;
        if (!complete &&
            Find.TickManager.TicksGame - naturalCompletionObservationStartedTick > 12_000)
        {
            throw new EndToEndAssertionException(
                $"Processor completion stalled or was observed incorrectly: maxProgress={ProcessorFrameworkAdapter.ProgressPercent(Dishwasher):R}; allNatural={allNatural}; held={heldCount}/{expectedCount}; hasContents={ProcessorFrameworkAdapter.HasContents(Dishwasher)}; water={HandwashingE2EFixture.ReadDubsNetworkWater(Dishwasher):R}; current={Cleaner.CurJobDef?.defName ?? "none"}.");
        }

        return complete;
    }

    internal void ConstrainOutputCapacity(int fittingUnitCount)
    {
        EndToEndAssert.Equal(1, ware.Count,
            "The constrained output fixture must contain one completed stack.");
        EndToEndAssert.True(AllProcessorOutputsNaturallyComplete(),
            "Output capacity may be constrained only after the exact stack naturally completes.");

        var output = ware[0];
        var inventory = Cleaner.inventory?.innerContainer;
        EndToEndAssert.NotNull(inventory,
            "The output cleaner must expose ordinary pawn inventory.");
        var unitMass = output.GetStatValue(StatDefOf.Mass);
        var targetRemaining = unitMass * (fittingUnitCount + 0.5f);
        var availableBefore = RemainingMassCapacity(Cleaner);
        var ballastDef = ThingDefOf.Silver;
        var ballastUnitMass = ballastDef.GetStatValueAbstract(StatDefOf.Mass);
        var ballastCount = Math.Max(
            0,
            (int)Math.Round((availableBefore - targetRemaining) / ballastUnitMass));
        EndToEndAssert.True(ballastCount > 0,
            "The fixture must need positive ballast to exercise a partial output stack.");

        while (ballastCount > 0)
        {
            var stack = ThingMaker.MakeThing(ballastDef);
            stack.stackCount = Math.Min(ballastCount, ballastDef.stackLimit);
            ballastCount -= stack.stackCount;
            EndToEndAssert.True(inventory!.TryAdd(stack, canMergeWithExistingStacks: false),
                "The fixture ballast must enter ordinary pawn inventory.");
        }

        var remaining = RemainingMassCapacity(Cleaner);
        EndToEndAssert.True(
            remaining + 0.0001f >= fittingUnitCount * unitMass &&
            remaining + 0.0001f < (fittingUnitCount + 1) * unitMass,
            "The exact remaining pawn capacity must admit only the requested whole-unit count.");
        partialOutputInitialCount = initialWareUnitCount;
        partialOutputDef = output.def;
        partialOutputStuff = output.Stuff;
    }

    internal void RelocateCleanerForOutputTransit()
    {
        Cleaner.jobs.EndCurrentJob(JobCondition.InterruptForced);
        var dishwasherRoom = Dishwasher.Position.GetRoom(map);
        var destinations = GenRadial.RadialCellsAround(Dishwasher.Position, 12f, useCenter: false)
            .Where(cell => cell.InBounds(map) &&
                           cell.Standable(map) &&
                           cell.GetRoom(map) == dishwasherRoom &&
                           cell.GetFirstPawn(map) is null &&
                           cell.DistanceToSquared(Dishwasher.InteractionCell) >= 36)
            .OrderByDescending(cell => cell.DistanceToSquared(Dishwasher.InteractionCell))
            .ToArray();
        EndToEndAssert.True(destinations.Length > 0,
            "The sealed fixture must expose a distant standable output-work start cell.");
        var destination = destinations[0];
        Cleaner.DeSpawn();
        GenSpawn.Spawn(Cleaner, destination, map);
    }

    internal bool ObserveProcessorEmptyWorkInTransit()
    {
        ObserveOutputJobTiming();
        return Cleaner.jobs.curDriver?.GetType().FullName == EmptyProcessorDriverTypeName &&
               observedOutputEmptyJobIds.Count == 1 &&
               Cleaner.Position.DistanceToSquared(Dishwasher.InteractionCell) > 2 &&
               !outputArrivalTicks.ContainsKey(lastOutputEmptyJobId);
    }

    internal void EnableCleanStorage()
    {
        foreach (var item in ware)
        {
            cleanStorage.settings.filter.SetAllow(item.def, allow: true);
        }
        cleanStorage.settings.filter.SetAllow(
            DefDatabase<SpecialThingFilterDef>.GetNamed("ImmersiveChefs_AllowCleanKitchenware"),
            allow: true);
        cleanStorage.settings.filter.SetAllow(
            DefDatabase<SpecialThingFilterDef>.GetNamed("ImmersiveChefs_AllowDirtyKitchenware"),
            allow: false);
        map.resourceCounter.UpdateResourceCounts();
        Cleaner.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    internal bool ObserveProcessorOutputExtraction()
    {
        ObserveOutputJobTiming();
        return !ProcessorFrameworkAdapter.HasContents(Dishwasher);
    }

    internal void AssertProcessorOutputBatchInSingleUnload()
    {
        ObserveOutputJobTiming();
        var pawnOwnedClean = ware.Count(item => PawnOwnsOutput(item) &&
                                                item.GetComp<CompSanitation>() is
                                                    { IsDirty: false, WashProvenance: WashProvenance.Safe });
        var inventoryOutputs = ware
            .Where(item => ReferenceEquals(item.holdingOwner, Cleaner.inventory?.innerContainer))
            .ToArray();
        var unloadJobs = CurrentAndQueuedUnloadJobs();
        var immediate = OutputTransferWasImmediate();
        var trackedInventoryCount = inventoryOutputs.Count(IsTracked);
        var arrival = outputArrivalTicks.TryGetValue(lastOutputEmptyJobId, out var arrivalTick)
            ? arrivalTick.ToString()
            : "none";
        var transfer = outputTransferTicks.TryGetValue(lastOutputEmptyJobId, out var transferTick)
            ? transferTick.ToString()
            : "none";
        EndToEndAssert.True(
            !ProcessorFrameworkAdapter.HasContents(Dishwasher) &&
            pawnOwnedClean == ware.Count &&
            inventoryOutputs.All(IsTracked) &&
            unloadJobs.Length == 1 &&
            immediate,
            $"The completed batch must enter one immediate native unload. ownedClean={pawnOwnedClean}/{ware.Count}; inventoryTracked={trackedInventoryCount}/{inventoryOutputs.Length}; unloadJobs={unloadJobs.Length}; current={Cleaner.CurJobDef?.defName ?? "none"}; immediate={immediate}; arrival={arrival}; transfer={transfer}; stockDelay={outputJobsWithStockDelay.Contains(lastOutputEmptyJobId)}.");
        observedOutputUnloadJobIds.Add(unloadJobs[0].loadID);
        observedCleanBatchHaul = true;
    }

    internal bool ObservePartialProcessorOutputBatchInInventory(int expectedCount)
    {
        ObserveOutputJobTiming();
        if (partialOutputDef is null)
        {
            return false;
        }

        var pawnOwnedOutput = Cleaner.inventory?.innerContainer
            .Where(MatchesPartialOutput)
            .Concat(Cleaner.carryTracker?.innerContainer.Where(MatchesPartialOutput) ?? Enumerable.Empty<Thing>())
            .ToArray() ?? Array.Empty<Thing>();
        var inventoryOutput = pawnOwnedOutput
            .Where(item => ReferenceEquals(item.holdingOwner, Cleaner.inventory?.innerContainer))
            .ToArray();
        var heldOutput = DishwasherHeldWare()
            .Where(MatchesPartialOutput)
            .ToArray();
        var heldCount = heldOutput.Sum(item => item.stackCount);
        var unloadJobs = CurrentAndQueuedUnloadJobs();
        if (pawnOwnedOutput.Sum(item => item.stackCount) == expectedCount &&
            inventoryOutput.All(IsTracked) &&
            pawnOwnedOutput.All(item => (item as ThingWithComps)?.GetComp<CompSanitation>() is
                { IsDirty: false, WashProvenance: WashProvenance.Safe }) &&
            heldCount == partialOutputInitialCount - expectedCount &&
            unloadJobs.Length == 1 &&
            pawnOwnedOutput.Length > 0 &&
            heldOutput.Length > 0 &&
            pawnOwnedOutput.Select(item => item.ThingID)
                .Intersect(heldOutput.Select(item => item.ThingID), StringComparer.Ordinal)
                .Any() == false &&
            OutputTransferWasImmediate())
        {
            partialOutputFirstBatchCount = expectedCount;
            partialOutputFirstBatchIds.UnionWith(pawnOwnedOutput.Select(item => item.ThingID));
            partialOutputRemainderIds.UnionWith(heldOutput.Select(item => item.ThingID));
            observedOutputUnloadJobIds.Add(unloadJobs[0].loadID);
            return true;
        }

        return false;
    }

    internal bool ObservePartialProcessorRemainderBatchInInventory(int expectedCount)
    {
        ObserveOutputJobTiming();
        if (partialOutputDef is null)
        {
            return false;
        }

        var pawnOwnedOutput = Cleaner.inventory?.innerContainer
            .Where(MatchesPartialOutput)
            .Concat(Cleaner.carryTracker?.innerContainer.Where(MatchesPartialOutput) ?? Enumerable.Empty<Thing>())
            .ToArray() ?? Array.Empty<Thing>();
        var inventoryOutput = pawnOwnedOutput
            .Where(item => ReferenceEquals(item.holdingOwner, Cleaner.inventory?.innerContainer))
            .ToArray();
        var unloadJobs = CurrentAndQueuedUnloadJobs();
        if (!ProcessorFrameworkAdapter.HasContents(Dishwasher) &&
            pawnOwnedOutput.Sum(item => item.stackCount) == expectedCount &&
            inventoryOutput.All(IsTracked) &&
            pawnOwnedOutput.All(item => (item as ThingWithComps)?.GetComp<CompSanitation>() is
                { IsDirty: false, WashProvenance: WashProvenance.Safe }) &&
            unloadJobs.Length == 1 &&
            OutputTransferWasImmediate())
        {
            observedOutputUnloadJobIds.Add(unloadJobs[0].loadID);
            return observedOutputEmptyJobIds.Count == 2 &&
                   observedOutputUnloadJobIds.Count == 2;
        }

        return false;
    }

    internal bool FirstPartialBatchStoredWithRemainderHeld(int expectedCount)
    {
        return partialOutputDef is not null &&
               DishwasherHeldWare().Where(MatchesPartialOutput).Sum(item => item.stackCount) == expectedCount &&
               map.listerThings.AllThings
                   .Where(MatchesPartialOutput)
                   .Where(item => cleanStorageCells.Contains(item.Position))
                   .Sum(item => item.stackCount) == expectedCount &&
               Cleaner.inventory?.innerContainer.Where(MatchesPartialOutput).Sum(item => item.stackCount) == 0;
    }

    internal bool AllPartialOutputStoredClean()
    {
        return partialOutputDef is not null &&
               !ProcessorFrameworkAdapter.HasContents(Dishwasher) &&
               map.listerThings.AllThings
                   .Where(MatchesPartialOutput)
                   .Where(item => cleanStorageCells.Contains(item.Position))
                   .Sum(item => item.stackCount) == partialOutputInitialCount &&
               map.listerThings.AllThings
                   .Where(MatchesPartialOutput)
                   .All(item => (item as ThingWithComps)?.GetComp<CompSanitation>() is
                       { IsDirty: false, WashProvenance: WashProvenance.Safe } && !IsTracked(item));
    }

    internal bool ObserveStockCapacityRaceFallback()
    {
        ObserveOutputJobTiming();
        var stored = partialOutputDef is not null &&
                     !ProcessorFrameworkAdapter.HasContents(Dishwasher) &&
                     map.listerThings.AllThings
                         .Where(MatchesPartialOutput)
                         .Where(item => cleanStorageCells.Contains(item.Position))
                         .Sum(item => item.stackCount) == partialOutputInitialCount;
        if (stored &&
            observedOutputEmptyJobIds.Count == 1 &&
            outputJobsWithStockDelay.SetEquals(observedOutputEmptyJobIds) &&
            CurrentAndQueuedUnloadJobs().Length == 0 &&
            map.listerThings.AllThings.Where(MatchesPartialOutput).All(item => !IsTracked(item)))
        {
            if (stockFallbackCompletedTick < 0)
            {
                stockFallbackCompletedTick = Find.TickManager.TicksGame;
            }
            return true;
        }

        return false;
    }

    internal string[] PartialOutputStoredIds => map.listerThings.AllThings
        .Where(MatchesPartialOutput)
        .Where(item => cleanStorageCells.Contains(item.Position))
        .Select(item => item.ThingID)
        .ToArray();

    internal void AssertPartialOutputConservation(int expectedFirstBatchCount)
    {
        EndToEndAssert.Equal(expectedFirstBatchCount, partialOutputFirstBatchCount,
            "The first native extraction must stop at the exact whole-unit mass boundary.");
        EndToEndAssert.True(AllPartialOutputStoredClean(),
            "Both the initial partial batch and completed remainder must reach clean storage.");
        EndToEndAssert.Equal(partialOutputInitialCount,
            map.listerThings.AllThings.Where(MatchesPartialOutput).Sum(item => item.stackCount),
            "Partial output extraction must conserve every physical unit.");
        EndToEndAssert.True(
            partialOutputFirstBatchIds.Count > 0 &&
            partialOutputRemainderIds.Count > 0 &&
            !partialOutputFirstBatchIds.Overlaps(partialOutputRemainderIds),
            "The capacity-bound extraction must partition disjoint tracked output Things from the processor-held remainder.");
        EndToEndAssert.Equal(2, observedOutputEmptyJobIds.Count,
            "The split batch and its remainder must use exactly two native Processor empty assignments.");
        EndToEndAssert.Equal(2, observedOutputUnloadJobIds.Count,
            "Each capacity-bound output batch must own exactly one distinct native Pick Up And Haul unload.");
        EndToEndAssert.Equal(0, outputJobsWithStockDelay.Count,
            "Neither naturally completed fitting output batch may enter Processor Framework's 200-tick stock wait.");
    }

    internal void AssertStockCapacityRaceFallback()
    {
        EndToEndAssert.True(ObserveStockCapacityRaceFallback(),
            "The post-admission capacity race must complete through Processor Framework's stock one-output path.");
        EndToEndAssert.Equal(0, observedOutputUnloadJobIds.Count,
            "The stock fallback must not fabricate a Pick Up And Haul tracked unload.");
        EndToEndAssert.Equal(0, outputTransferTicks.Count,
            "The stock fallback output must never enter tracked pawn inventory.");
        EndToEndAssert.True(
            partialOutputDef is not null &&
            RemainingMassCapacity(Cleaner) + 0.0001f <
            partialOutputDef.GetStatValueAbstract(StatDefOf.Mass, partialOutputStuff),
            "The stock fallback must complete while ordinary inventory remains unable to fit the output.");
    }

    internal Dictionary<string, string> StockFallbackCheckpoint()
    {
        return new Dictionary<string, string>
        {
            ["emptyJobId"] = lastOutputEmptyJobId.ToString(),
            ["emptyJobStartTick"] = outputJobStartTicks[lastOutputEmptyJobId].ToString(),
            ["emptyJobArrivalTick"] = outputArrivalTicks[lastOutputEmptyJobId].ToString(),
            ["stockFallbackCompletedTick"] = stockFallbackCompletedTick.ToString(),
            ["stockDelayObserved"] = outputJobsWithStockDelay.Contains(lastOutputEmptyJobId).ToString(),
            ["trackedUnloadCount"] = observedOutputUnloadJobIds.Count.ToString()
        };
    }

    internal bool AllWareStoredClean()
    {
        return observedCleanBatchHaul &&
               ware.All(item => item.Spawned && item.Map == map && cleanStorageCells.Contains(item.Position)) &&
               ware.All(item => item.GetComp<CompSanitation>()!.IsDirty == false) &&
               ware.All(item => !IsTracked(item));
    }

    internal void AssertFinalConservation()
    {
        EndToEndAssert.True(AllWareStoredClean(),
            "The exact clean mixed batch must reach the declared kitchen storage through native hauling.");
        EndToEndAssert.Equal(3, ware.Select(item => item.ThingID).Distinct().Count(),
            "Dishwasher batching must preserve all three exact identities.");
        EndToEndAssert.True(ware.All(item => item.stackCount == 1),
            "Each exact kitchenware fixture must remain one physical unit.");
        EndToEndAssert.Equal(3, map.listerThings.AllThings
            .Where(item => item.def.GetModExtension<KitchenwareExtension>()?.product is
                KitchenwareProduct.Plate or KitchenwareProduct.Cutlery or KitchenwareProduct.Cookware)
            .Sum(item => item.stackCount),
            "The one-trip Processor workflow must neither duplicate nor lose kitchenware.");
        EndToEndAssert.Equal(1, observedOutputEmptyJobIds.Count,
            "The full fitting output must use exactly one native Processor empty assignment.");
        EndToEndAssert.Equal(1, observedOutputUnloadJobIds.Count,
            "The full fitting output must request exactly one native Pick Up And Haul unload.");
        EndToEndAssert.Equal(0, outputJobsWithStockDelay.Count,
            "The fitting output must not enter Processor Framework's 200-tick stock wait.");
    }

    internal void AssertLoadedConservation()
    {
        EndToEndAssert.True(AllWareOwnedByDishwasher(),
            "The exact local dishwasher must own all three one-trip units.");
        EndToEndAssert.True(ware.All(item => !IsTracked(item)),
            "Local appliance admission must release every Pick Up And Haul tracking claim.");
        EndToEndAssert.True(ware.All(item => item.stackCount == 1),
            "Local appliance admission must preserve one physical unit per exact fixture.");
        EndToEndAssert.Equal(3, TotalPhysicalUnitsAcrossHolders(),
            "Local appliance admission must neither duplicate nor lose kitchenware.");
    }

    internal void AssertInterruptedConservation()
    {
        EndToEndAssert.True(UnadmittedWareReturnedAfterInterruption(),
            "Only unadmitted units must return through upstream unloading after interruption.");
        EndToEndAssert.Equal(3, ware.Select(item => item.ThingID).Distinct().Count(),
            "Partial Processor admission must retain all three exact identities.");
        EndToEndAssert.True(ware.All(item => item.stackCount == 1),
            "Partial Processor admission must retain one physical unit per fixture.");
        EndToEndAssert.Equal(3, TotalPhysicalUnitsAcrossHolders(),
            "Partial Processor admission must neither duplicate nor lose a physical unit.");
    }

    internal EndToEndGizmoOption RequiredDraftToggle(IEndToEndContext context)
    {
        var candidates = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { Cleaner.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Toggle &&
                string.Equals(option.HotKeyDefName, "Command_ColonistDraft", StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(1, candidates.Length,
            "The selected cleaner must expose exactly one current native Draft toggle.");
        return candidates[0];
    }

    private int TotalPhysicalUnitsAcrossHolders()
    {
        return map.listerThings.AllThings
            .Concat(map.mapPawns.AllPawnsSpawned.SelectMany(pawn =>
                pawn.inventory?.innerContainer ?? Enumerable.Empty<Thing>()))
            .Concat(DishwasherHeldWare())
            .Distinct()
            .Where(item => item.def.GetModExtension<KitchenwareExtension>()?.product is
                KitchenwareProduct.Plate or KitchenwareProduct.Cutlery or KitchenwareProduct.Cookware)
            .Sum(item => item.stackCount);
    }

    private IReadOnlyList<Thing> DishwasherHeldWare()
    {
        return ProcessorFrameworkAdapter.Controls(Dishwasher)
            ? ProcessorFrameworkAdapter.HeldWare(Dishwasher)
            : Dishwasher.GetComp<CompDishwasher>()?.GetDirectlyHeldThings().Cast<Thing>().ToArray()
              ?? Array.Empty<Thing>();
    }

    private bool MatchesPartialOutput(Thing item)
    {
        return partialOutputDef is not null &&
               item.def == partialOutputDef &&
               item.Stuff == partialOutputStuff;
    }

    private bool PawnOwnsOutput(Thing item)
    {
        return ReferenceEquals(item.holdingOwner, Cleaner.inventory?.innerContainer) ||
               ReferenceEquals(item.holdingOwner, Cleaner.carryTracker?.innerContainer);
    }

    private void ObserveOutputJobTiming()
    {
        var driver = Cleaner.jobs.curDriver;
        if (driver?.GetType().FullName != EmptyProcessorDriverTypeName)
        {
            return;
        }

        var jobId = driver.job.loadID;
        observedOutputEmptyJobIds.Add(jobId);
        if (!outputJobStartTicks.ContainsKey(jobId))
        {
            outputJobStartTicks[jobId] = Find.TickManager.TicksGame;
        }
        lastOutputEmptyJobId = jobId;
        if (Cleaner.Position.DistanceToSquared(Dishwasher.InteractionCell) <= 2)
        {
            if (!outputArrivalTicks.ContainsKey(jobId))
            {
                outputArrivalTicks[jobId] = Find.TickManager.TicksGame;
            }
            if (driver.ticksLeftThisToil > 5)
            {
                outputJobsWithStockDelay.Add(jobId);
            }
        }
    }

    private bool OutputTransferWasImmediate()
    {
        if (lastOutputEmptyJobId < 0)
        {
            return false;
        }

        if (!outputTransferTicks.ContainsKey(lastOutputEmptyJobId))
        {
            outputTransferTicks[lastOutputEmptyJobId] = Find.TickManager.TicksGame;
        }

        return !outputJobsWithStockDelay.Contains(lastOutputEmptyJobId) &&
               (!outputArrivalTicks.TryGetValue(lastOutputEmptyJobId, out var arrivalTick) ||
                outputTransferTicks[lastOutputEmptyJobId] - arrivalTick <= MaxObservedImmediateTransferTicks);
    }

    private Job[] CurrentAndQueuedUnloadJobs()
    {
        var jobs = new List<Job>();
        if (Cleaner.jobs.curDriver?.GetType().FullName == UnloadDriverTypeName &&
            Cleaner.CurJob is { } current)
        {
            jobs.Add(current);
        }

        jobs.AddRange(Cleaner.jobs.jobQueue
            .Where(queued => queued.job?.def?.driverClass?.FullName == UnloadDriverTypeName)
            .Select(queued => queued.job));
        return jobs.Distinct().ToArray();
    }

    private static float RemainingMassCapacity(Pawn pawn)
    {
        return Math.Max(
            0f,
            MassUtility.Capacity(pawn) * (1f - MassUtility.EncumbrancePercent(pawn)));
    }

    private bool IsTracked(Thing thing)
    {
        var tracker = Cleaner.AllComps.Single(comp => comp.GetType().FullName == TrackerTypeName);
        var method = tracker.GetType().GetMethod(
            "GetHashSet",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        return method?.Invoke(tracker, null) is IEnumerable tracked &&
               tracked.Cast<object>().Any(candidate => ReferenceEquals(candidate, thing));
    }

    private static ThingWithComps MakeDirtyWare(string defName, ThingDef stuff)
    {
        var item = FoodSearchE2EFixture.MakeCleanWare(defName, stuff);
        item.GetComp<CompSanitation>()!.MarkDirty();
        return item;
    }

    private static ThingWithComps SpawnPowerSource(Map map, IntVec3 cell)
    {
        var def = DefDatabase<ThingDef>.GetNamed("VanometricPowerCell");
        var source = (ThingWithComps)ThingMaker.MakeThing(def, def.MadeFromStuff ? ThingDefOf.Steel : null);
        source.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(source, cell, map, Rot4.North);
        return source;
    }

    private static void SpawnConduitsOutside(
        Map map,
        IntVec3 center,
        int radiusX,
        int radiusZ,
        ISet<IntVec3> excluded)
    {
        var def = DefDatabase<ThingDef>.GetNamed("PowerConduit");
        for (var x = -radiusX; x <= radiusX; x++)
        {
            for (var z = -radiusZ; z <= radiusZ; z++)
            {
                var cell = center + new IntVec3(x, 0, z);
                if (!cell.InBounds(map) || excluded.Contains(cell))
                {
                    continue;
                }

                var conduit = ThingMaker.MakeThing(def);
                conduit.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(conduit, cell, map);
            }
        }
    }
}
