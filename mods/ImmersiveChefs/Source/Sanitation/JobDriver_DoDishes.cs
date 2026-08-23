using Verse;
using Verse.AI;

namespace ImmersiveChefs;

public sealed class JobDriver_DoDishes : JobDriver
{
    private List<Thing> collectedWare = new();
    private int washIndex;
    private int washTicks;
    private bool waterDebited;
    private int admissionTicks;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (!job.targetQueueA.NullOrEmpty())
        {
            pawn.ReserveAsManyAsPossible(job.targetQueueA, job);
            return pawn.Reserve(job.targetQueueA[0], job, 1, job.countQueue?[0] ?? 1, null, errorOnFailed) &&
                   pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);
        }

        return pawn.Reserve(job.targetA, job, 1, job.count, null, errorOnFailed) &&
               pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        if (DishwashingBatchPolicy.IsPersistedBatch(
                !job.targetQueueA.NullOrEmpty(),
                collectedWare.Count))
        {
            foreach (var toil in MakePickUpAndHaulBatchToils())
            {
                yield return toil;
            }

            yield break;
        }

        this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false);
        yield return Toils_Goto.Goto(TargetIndex.B, PathEndMode.Touch);

        if (TargetThingB is ThingWithComps building && building.GetComp<CompDishwasher>() is { } dishwasher)
        {
            yield return Toils_General.DoAtomic(() =>
            {
                if (!dishwasher.TryAcceptFrom(pawn))
                {
                    TryDropCarriedThingIfPresent(pawn);
                }
            });
            yield break;
        }

        var ware = TargetThingA;
        var perItemPlateEquivalents =
            ware?.def.GetModExtension<KitchenwareExtension>()?.plateEquivalent ?? 1f;
        var count = ware is null
            ? 1
            : Math.Max(1, Math.Min(job.count > 0 ? job.count : ware.stackCount, ware.stackCount));
        var duration = DishwashingWorkPolicy.HandwashingDurationTicks(
            perItemPlateEquivalents,
            count,
            ImmersiveChefsMod.Settings.DishwashingWorkScale);
        yield return Toils_General.DoAtomic(() =>
        {
            if (TargetThingB is { } source &&
                ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.DubsBadHygiene) &&
                !DubsWaterAdapter.TryUseHandwashingSource(pawn, source, out _))
            {
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
            }
        });
        yield return Toils_General.WaitWith(TargetIndex.B, duration, useProgressBar: true);
        yield return Toils_General.DoAtomic(() =>
        {
            var provenance = job.GetTarget(TargetIndex.C).IsValid
                ? WashProvenance.Safe
                : WashProvenance.WildWater;
            (pawn.carryTracker.CarriedThing as ThingWithComps)?.GetComp<CompSanitation>()?.MarkClean(provenance);
            TryDropCarriedThingIfPresent(pawn);
        });
    }

    internal static bool TryDropCarriedThingIfPresent(Pawn pawn)
    {
        if (pawn.carryTracker.CarriedThing is null)
        {
            return false;
        }

        return pawn.carryTracker.TryDropCarriedThing(
            pawn.Position,
            ThingPlaceMode.Near,
            out _);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref collectedWare, "immersiveChefsCollectedWare", LookMode.Reference);
        Scribe_Values.Look(ref washIndex, "immersiveChefsWashIndex");
        Scribe_Values.Look(ref washTicks, "immersiveChefsWashTicks");
        Scribe_Values.Look(ref waterDebited, "immersiveChefsWaterDebited");
        Scribe_Values.Look(ref admissionTicks, "immersiveChefsAdmissionTicks");
        collectedWare ??= new List<Thing>();
    }

    private IEnumerable<Toil> MakePickUpAndHaulBatchToils()
    {
        AddFinishAction(_ => ReturnBatchThroughPickUpAndHaul());

        var extractNext = Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.A);
        yield return extractNext;
        var gotoWare = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        gotoWare.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        yield return gotoWare;
        yield return Toils_General.DoAtomic(CollectCurrentStack);
        yield return Toils_Jump.JumpIf(extractNext, () => !job.targetQueueA.NullOrEmpty());
        yield return Toils_Goto.Goto(TargetIndex.B, PathEndMode.Touch);

        if (TargetThingB is ThingWithComps dishwasher &&
            dishwasher.GetComp<CompDishwasher>() is not null)
        {
            var admit = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Never,
                tickAction = TickBatchDishwasherAdmission
            };
            admit.WithProgressBar(TargetIndex.B, CurrentAdmissionProgress);
            yield return admit;
            yield break;
        }

        var wash = new Toil
        {
            defaultCompleteMode = ToilCompleteMode.Never,
            tickAction = TickBatchWashing
        };
        wash.WithProgressBar(TargetIndex.B, CurrentWashProgress);
        yield return wash;
    }

    private void TickBatchDishwasherAdmission()
    {
        while (washIndex < collectedWare.Count && !IsTrackedInInventory(collectedWare[washIndex]))
        {
            washIndex++;
            admissionTicks = 0;
        }

        if (washIndex >= collectedWare.Count)
        {
            ReadyForNextToil();
            return;
        }

        admissionTicks++;
        if (!DishwashingBatchPolicy.IsDishwasherAdmissionReady(admissionTicks))
        {
            return;
        }

        var ware = collectedWare[washIndex];
        var accepted = TargetThingB is ThingWithComps dishwasher &&
                       (ProcessorFrameworkAdapter.Controls(dishwasher)
                           ? ProcessorFrameworkAdapter.TryAcceptTrackedWare(pawn, dishwasher, ware, out _)
                           : dishwasher.GetComp<CompDishwasher>()?.TryAcceptTrackedWare(pawn, ware) == true);
        if (!accepted)
        {
            EndBatchIncompletable();
            return;
        }

        washIndex++;
        admissionTicks = 0;
    }

    private float CurrentAdmissionProgress() =>
        washIndex >= collectedWare.Count
            ? 1f
            : Math.Min(1f, admissionTicks / (float)DishwashingBatchPolicy.DishwasherAdmissionTicksPerUnit);

    private bool IsTrackedInInventory(Thing thing) =>
        thing is { Destroyed: false } &&
        ReferenceEquals(thing.holdingOwner, pawn.inventory?.innerContainer);

    private void CollectCurrentStack()
    {
        var source = TargetThingA;
        var inventory = pawn.inventory?.innerContainer;
        if (source is null || source.Destroyed || !source.Spawned || inventory is null)
        {
            EndBatchIncompletable();
            return;
        }

        var remaining = Math.Max(0, Math.Min(job.count, source.stackCount));
        while (remaining-- > 0 && !source.Destroyed)
        {
            var unit = source.stackCount > 1 ? source.SplitOff(1) : source;
            if (unit.Spawned)
            {
                unit.DeSpawn(DestroyMode.Vanish);
            }

            if (!inventory.TryAdd(unit, canMergeWithExistingStacks: false))
            {
                GenPlace.TryPlaceThing(unit, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                EndBatchIncompletable();
                return;
            }

            if (!PickUpAndHaulAdapter.TryRegister(pawn, unit, out var reason))
            {
                inventory.TryDrop(unit, pawn.Position, pawn.Map, ThingPlaceMode.Near, out _);
                OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.PickUpAndHaul, reason);
                EndBatchIncompletable();
                return;
            }

            collectedWare.Add(unit);
        }
    }

    private void TickBatchWashing()
    {
        if (!PickUpAndHaulAdapter.CanTrack(pawn))
        {
            EndBatchIncompletable();
            return;
        }

        while (washIndex < collectedWare.Count && !IsWashable(collectedWare[washIndex]))
        {
            washIndex++;
            washTicks = 0;
            waterDebited = false;
        }

        if (washIndex >= collectedWare.Count)
        {
            ReadyForNextToil();
            return;
        }

        var ware = collectedWare[washIndex];
        if (!waterDebited)
        {
            if (!TryDebitWashWater())
            {
                EndBatchIncompletable();
                return;
            }

            waterDebited = true;
        }

        washTicks++;
        if (washTicks < WashDuration(ware))
        {
            return;
        }

        var provenance = job.GetTarget(TargetIndex.C).IsValid
            ? WashProvenance.Safe
            : WashProvenance.WildWater;
        (ware as ThingWithComps)?.GetComp<CompSanitation>()?.MarkClean(provenance);
        washIndex++;
        washTicks = 0;
        waterDebited = false;
    }

    private bool TryDebitWashWater()
    {
        if (TargetThingB is not { } source ||
            !ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.DubsBadHygiene))
        {
            return true;
        }

        return DubsWaterAdapter.TryUseHandwashingSource(pawn, source, out _);
    }

    private int WashDuration(Thing ware)
    {
        var plateEquivalents =
            ware.def.GetModExtension<KitchenwareExtension>()?.plateEquivalent ?? 1f;
        return DishwashingWorkPolicy.HandwashingDurationTicks(
            plateEquivalents,
            1,
            ImmersiveChefsMod.Settings.DishwashingWorkScale);
    }

    private float CurrentWashProgress()
    {
        if (washIndex >= collectedWare.Count || !IsWashable(collectedWare[washIndex]))
        {
            return 1f;
        }

        return Math.Min(1f, washTicks / (float)Math.Max(1, WashDuration(collectedWare[washIndex])));
    }

    private bool IsWashable(Thing thing)
    {
        return thing is { Destroyed: false } &&
               ReferenceEquals(thing.holdingOwner, pawn.inventory?.innerContainer) &&
               (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true;
    }

    private void ReturnBatchThroughPickUpAndHaul()
    {
        var inventory = pawn.inventory?.innerContainer;
        if (collectedWare.Count == 0 || inventory is null ||
            !collectedWare.Any(thing => thing is { Destroyed: false } &&
                                        ReferenceEquals(thing.holdingOwner, inventory)))
        {
            return;
        }

        if (PickUpAndHaulAdapter.TryQueueUnload(pawn, out var reason))
        {
            return;
        }

        OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.PickUpAndHaul, reason);
        if (inventory is null || pawn.MapHeld is null)
        {
            return;
        }

        foreach (var ware in collectedWare.Where(thing =>
                     thing is { Destroyed: false } && ReferenceEquals(thing.holdingOwner, inventory)).ToList())
        {
            if (inventory.TryDrop(ware, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near, out _))
            {
                PickUpAndHaulAdapter.TryRemoveTracked(pawn, ware, out _);
            }
        }
    }

    private void EndBatchIncompletable()
    {
        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
    }
}
