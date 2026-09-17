using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest("immersive-chefs.dishwasher-concurrent-local",
    "fumblesneeze.immersivechefs", "brrainz.harmony", EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions", "Dubwise.DubsBadHygiene", "fumblesneeze.immersivechefs",
    MaxFrames = 6000, MaxGameTicks = 12000, MaxWallClockSeconds = 180)]
public sealed class DishwasherConcurrentLocalTest : DishwasherConcurrentAccessTest
{
    protected override bool Processor => false;
}

[RimWorldEndToEndTest("immersive-chefs.dishwasher-concurrent-processor",
    "fumblesneeze.immersivechefs", "brrainz.harmony", EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions", "syrchalis.processor.framework", "Dubwise.DubsBadHygiene",
    "fumblesneeze.immersivechefs", MaxFrames = 9000, MaxGameTicks = 18000, MaxWallClockSeconds = 240)]
public sealed class DishwasherConcurrentProcessorTest : DishwasherConcurrentAccessTest
{
    protected override bool Processor => true;
}

[RimWorldEndToEndTest("immersive-chefs.industrial-concurrent-cleaning-processor",
    "fumblesneeze.immersivechefs", "brrainz.harmony", EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions", "syrchalis.processor.framework", "Dubwise.DubsBadHygiene",
    "fumblesneeze.immersivechefs", MaxFrames = 9000, MaxGameTicks = 18000, MaxWallClockSeconds = 240)]
public sealed class IndustrialConcurrentCleaningProcessorTest : DishwasherConcurrentAccessTest
{
    protected override bool Processor => true;
    protected override bool ProcessorFillWork => false;
    protected override string ApplianceDef => "ImmersiveChefs_IndustrialDishwasher";
}

public abstract class DishwasherConcurrentAccessTest : IRimWorldEndToEndTest
{
    protected abstract bool Processor { get; }
    protected virtual bool ProcessorFillWork => Processor;
    protected virtual string ApplianceDef => "ImmersiveChefs_Dishwasher";
    private PickUpAndHaulDishwasherFixture fixture = null!;
    private Pawn second = null!;
    private ThingWithComps sink = null!;
    private static EndToEndDeadline Deadline => new(1800, 6000, TimeSpan.FromSeconds(60));

    public void Arrange(IEndToEndContext context)
    {
        fixture = PickUpAndHaulDishwasherFixture.Create(context, requireProcessor: Processor,
            requirePickUpAndHaul: false, hiddenConduits: true, dishwasherDefName: ApplianceDef);
        fixture.PhysicalWare[2].SetForbidden(true, false);
        second = PickUpAndHaulDishwasherFixture.CreateInactiveDishwasherWorker("Ivo Dishrunner");
        GenSpawn.Spawn(second, fixture.Cleaner.Position + IntVec3.North, fixture.Dishwasher.Map);
        sink = DispenserE2EFixture.SpawnBuilding(fixture.Dishwasher.Map, "KitchenSink",
            fixture.Dishwasher.Position + new IntVec3(-3, 0, 2));
        HandwashingE2EFixture.SpawnDubsSinkWaterSupply(fixture.Dishwasher.Map, sink, 10f);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause before concurrent deliveries", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep("frame workers dishwasher and sink",
            new[] { fixture.Cleaner.ThingID, second.ThingID, fixture.Dishwasher.ThingID, sink.ThingID }, 160);
        yield return new ScreenshotStep("before two workers deliver", Array.Empty<string>(), 0);
        yield return new AssertionStep("enable first worker work", _ =>
        {
            if (ProcessorFillWork) fixture.ActivateProcessorHaulingOnly();
            else fixture.ActivateCleaning();
        });
        yield return new TimeControlActionStep("run ordinary cleaning search", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("first worker schedules a dishwasher delivery",
            _ => IsDelivery(fixture.Cleaner), Deadline);
        yield return new TimeControlActionStep("retain first worker en route", true, EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep("first native delivery en route", Array.Empty<string>(), 0);
        yield return new AssertionStep("appliance remains unreserved and sink is eligible", _ =>
        {
            AssertNoApplianceReservations();
            EndToEndAssert.True(HandwashingE2EFixture.DubsFixtureAllowsAndWorks(second, sink, 0.1f),
                "The nearby sink must be a real usable fallback.");
            second.workSettings.SetPriority(ProcessorFillWork ? WorkTypeDefOf.Hauling : WorkTypeDefOf.Cleaning, 1);
            second.jobs.EndCurrentJob(JobCondition.InterruptForced);
        });
        yield return new TimeControlActionStep("let second worker seek Cleaning", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("both workers concurrently target one dishwasher",
            _ => IsDelivery(fixture.Cleaner) && IsDelivery(second), Deadline);
        yield return new TimeControlActionStep("retain concurrent jobs", true, EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep("show both scheduled deliveries",
            new[] { fixture.Cleaner.ThingID, second.ThingID }, false);
        yield return new ScreenshotStep("both workers target dishwasher instead of sink", Array.Empty<string>(), 0);
        yield return new AssertionStep("distinct ware stays protected", _ =>
        {
            AssertNoApplianceReservations();
            var input = ProcessorFillWork ? TargetIndex.B : TargetIndex.A;
            EndToEndAssert.False(ReferenceEquals(fixture.Cleaner.CurJob.GetTarget(input).Thing, second.CurJob.GetTarget(input).Thing),
                "Concurrent workers must own distinct input ware.");
        });
        yield return new TimeControlActionStep("perform both deliveries", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("both actual items enter the dishwasher",
            _ => fixture.PhysicalWare.Take(2).All(item => fixture.HeldDishwasherWare.Contains(item)), Deadline);
        yield return new TimeControlActionStep("pause after native admission", true, EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep("inspect two admitted loads", new[] { fixture.Dishwasher.ThingID }, false);
        yield return new ScreenshotStep("two deliveries admitted with spare capacity", Array.Empty<string>(), 0);
        yield return new AssertionStep("concurrent transfers conserve capacity and physical units", _ =>
        {
            AssertNoApplianceReservations();
            EndToEndAssert.Equal(2, fixture.HeldDishwasherWare.Sum(item => item.stackCount), "Both items enter exactly once.");
            var comp = fixture.Dishwasher.GetComp<CompDishwasher>();
            var used = Processor ? ProcessorFrameworkAdapter.UsedPlateEquivalentCapacity(fixture.Dishwasher) : comp.UsedCapacity;
            EndToEndAssert.True(used > 0f && used <= comp.Capacity, "Concurrent arrivals cannot overfill.");
        });
        if (!Processor) yield break;

        yield return new AssertionStep("stop work while admitted loads finish naturally", _ =>
        {
            fixture.DisableCleanerWork();
            second.workSettings.SetPriority(WorkTypeDefOf.Hauling, 0);
            second.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 0);
            second.jobs.EndCurrentJob(JobCondition.InterruptForced);
        });
        yield return new TimeControlActionStep("let both native cycles finish", false, EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep("two retained outputs complete naturally",
            _ => ProcessorFrameworkAdapter.AllProcessesNaturallyComplete(fixture.Dishwasher), Deadline);
        yield return new TimeControlActionStep("pause before output work", true, EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep("inspect completed outputs", new[] { fixture.Dishwasher.ThingID }, false);
        yield return new ScreenshotStep("two naturally completed outputs before unloading", Array.Empty<string>(), 0);
        yield return new AssertionStep("prepare ordinary output storage and distant workers", _ =>
        {
            fixture.EnableCleanStorage();
            var origin = fixture.Dishwasher.Position + new IntVec3(-6, 0, -3);
            fixture.Cleaner.Position = origin;
            second.Position = origin + IntVec3.North;
            fixture.Cleaner.Notify_Teleported();
            second.Notify_Teleported();
            fixture.ActivateProcessorHaulingOnly();
        });
        yield return new TimeControlActionStep("first worker seeks output work", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("first native EmptyProcessor job starts", _ => IsEmptying(fixture.Cleaner), Deadline);
        yield return new TimeControlActionStep("retain first unloader en route", true, EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep("first native unloader en route", Array.Empty<string>(), 0);
        yield return new AssertionStep("unloading leaves appliance available to another worker", _ =>
        {
            AssertNoApplianceReservations();
            second.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
            second.jobs.EndCurrentJob(JobCondition.InterruptForced);
        });
        yield return new TimeControlActionStep("second worker seeks output work", false, EndToEndGameSpeed.Normal);
        var outputIds = new Dictionary<Pawn, int>();
        var extracted = new Dictionary<Pawn, Thing>();
        yield return new WaitUntilStep("both native unloading jobs target one appliance",
            _ =>
            {
                if (!IsEmptying(fixture.Cleaner) || !IsEmptying(second)) return false;
                foreach (var pawn in new[] { fixture.Cleaner, second })
                {
                    outputIds[pawn] = pawn.CurJob.loadID;
                    if (pawn.CurJob.targetB.Thing is { } output) extracted[pawn] = output;
                }
                return true;
            }, Deadline);
        yield return new TimeControlActionStep("retain two native unloading jobs", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep("retain exact output job identities", _ =>
        {
            AssertNoApplianceReservations();
        });
        yield return new SelectionActionStep("inspect second native unloader", new[] { second.ThingID }, false);
        yield return new ScreenshotStep("concurrent native unloading assignments", Array.Empty<string>(), 0);
        yield return new TimeControlActionStep("perform concurrent output trips", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("both exact output jobs complete their storage trips", _ =>
        {
            foreach (var pawn in new[] { fixture.Cleaner, second })
            {
                if (pawn.CurJob is { } current && current.loadID == outputIds[pawn] &&
                    current.targetB.Thing is { } output) extracted[pawn] = output;
                if (!extracted.TryGetValue(pawn, out var item)) continue;
                EndToEndAssert.True(IsStored(item) || pawn.CurJob?.loadID == outputIds[pawn],
                    "An extracted output must finish the same haul even when the appliance becomes empty.");
            }
            return fixture.PhysicalWare.Take(2).All(IsStored);
        }, Deadline);
        yield return new TimeControlActionStep("retain both outputs in storage", true, EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep("inspect the now empty appliance", new[] { fixture.Dishwasher.ThingID }, false);
        yield return new ScreenshotStep("two exact clean outputs stored after concurrent unloading", Array.Empty<string>(), 0);
        yield return new AssertionStep("output identities and units are conserved", _ =>
        {
            AssertNoApplianceReservations();
            EndToEndAssert.Equal(2, extracted.Count, "Both native jobs must actually extract an output.");
            EndToEndAssert.False(ReferenceEquals(extracted[fixture.Cleaner], extracted[second]),
                "Two workers extract distinct physical outputs.");
            EndToEndAssert.Equal(0, fixture.HeldDishwasherWare.Count, "No completed output remains stranded.");
            EndToEndAssert.True(fixture.PhysicalWare.Take(2).All(item =>
                item.stackCount == 1 && item.GetComp<CompSanitation>().IsDirty == false),
                "Both exact units remain clean and unduplicated.");
        });
    }

    private bool IsEmptying(Pawn pawn) => pawn.CurJobDef?.defName == "EmptyProcessor" &&
        ReferenceEquals(pawn.CurJob.targetA.Thing, fixture.Dishwasher);

    private static bool IsStored(Thing item) => item.Spawned && item.GetSlotGroup() is not null;

    private bool IsDelivery(Pawn pawn) =>
        pawn.CurJobDef?.defName == (ProcessorFillWork ? "FillProcessor" : "ImmersiveChefs_DoDishes") &&
        ReferenceEquals(pawn.CurJob.GetTarget(ProcessorFillWork ? TargetIndex.A : TargetIndex.B).Thing, fixture.Dishwasher);

    private void AssertNoApplianceReservations() => EndToEndAssert.False(
        fixture.Dishwasher.Map.reservationManager.ReservationsReadOnly.Any(reservation =>
            ReferenceEquals(reservation.Target.Thing, fixture.Dishwasher)),
        "Loading must not reserve the whole appliance while a worker is en route.");
}
