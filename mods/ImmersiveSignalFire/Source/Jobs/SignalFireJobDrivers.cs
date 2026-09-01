using System.Collections.Generic;
using ImmersiveSignalFire.Buildings;
using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveSignalFire.Jobs;

public sealed class JobDriver_ApproachSignalFire : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(job.GetTarget(TargetIndex.B), job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        AddFinishAction(_ => FireComp()?.NotifyApproachJobEnded(pawn));
        yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
        yield return new Toil
        {
            defaultCompleteMode = ToilCompleteMode.Never,
            handlingFacing = true,
            tickAction = FaceFire,
        };
    }

    private void FaceFire()
    {
        if (job.GetTarget(TargetIndex.A).Thing is { } fire)
        {
            pawn.rotationTracker.FaceTarget(fire);
        }
    }

    private CompSignalFire? FireComp() =>
        (job.GetTarget(TargetIndex.A).Thing as Building_SignalFire)?.SignalComp;
}

public sealed class JobDriver_SignalFire : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(job.GetTarget(TargetIndex.B), job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        AddFinishAction(_ => FireComp()?.NotifySignalJobEnded(pawn));
        Toil signal = new()
        {
            defaultCompleteMode = ToilCompleteMode.Never,
            handlingFacing = true,
            tickAction = TickSignal,
            activeSkill = () => SkillDefOf.Social,
        };
        yield return signal;
    }

    private void TickSignal()
    {
        CompSignalFire? comp = FireComp();
        if (comp is null || !comp.OwnsActiveJob(pawn))
        {
            EndJobWith(JobCondition.Incompletable);
            return;
        }

        pawn.rotationTracker.FaceTarget(job.GetTarget(TargetIndex.A));
        if (Find.TickManager.TicksGame % 30 == 0)
        {
            pawn.skills?.Learn(SkillDefOf.Social, 0.01f);
            pawn.skills?.Learn(SkillDefOf.Melee, 0.01f);
        }
    }

    private CompSignalFire? FireComp() =>
        (job.GetTarget(TargetIndex.A).Thing as Building_SignalFire)?.SignalComp;
}
