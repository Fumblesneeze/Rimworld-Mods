using Verse;
using Verse.AI;

namespace ImmersiveChefs;

public sealed class JobDriver_DoDishes : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, job.count, null, errorOnFailed) &&
               pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
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
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
                }
            });
            yield break;
        }

        var duration = Math.Max(1, (int)Math.Round(1000f * ImmersiveChefsMod.Settings.DishwashingWorkScale));
        yield return Toils_General.DoAtomic(() =>
        {
            if (TargetThingB is { } source &&
                ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.DubsBadHygiene) &&
                DubsWaterAdapter.IsPlumbedDubsFixture(source) &&
                !DubsWaterAdapter.TryConsumeCycleWater(source, 1f, out _))
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
            pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
        });
    }
}
