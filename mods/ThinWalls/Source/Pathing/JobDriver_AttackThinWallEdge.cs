using System.Collections.Generic;
using RimWorld;
using ThinWalls.Geometry;
using Verse;
using Verse.AI;

namespace ThinWalls.Pathing;

/// <summary>
/// Performs an ordinary melee attack without asking the standable target building for a
/// vanilla Touch path. A segment may be owned by the pawn's current cell, so that path
/// would otherwise try to move the pawn through the very edge it needs to destroy.
/// </summary>
public sealed class JobDriver_AttackThinWallEdge : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

        Toil attack = ToilMaker.MakeToil("AttackThinWallEdge");
        attack.initAction = () => InitializeAttack(attack.actor);
        attack.tickAction = () => AttackCurrentBoundary(attack.actor);
        attack.defaultCompleteMode = ToilCompleteMode.Never;
        attack.activeSkill = () => SkillDefOf.Melee;
        yield return attack;
    }

    private void InitializeAttack(Pawn actor)
    {
        actor.pather.StopDead();
        Thing? target = job.GetTarget(TargetIndex.A).Thing;
        if (target == null || actor.meleeVerbs.TryGetMeleeVerb(target) == null)
        {
            EndJobWith(JobCondition.Incompletable);
        }
    }

    private void AttackCurrentBoundary(Pawn actor)
    {
        Thing? target = job.GetTarget(TargetIndex.A).Thing;
        if (target == null || target.Destroyed)
        {
            ReadyForNextToil();
            return;
        }

        LocalTargetInfo attemptedStep = job.GetTarget(TargetIndex.B);
        if (!attemptedStep.IsValid || !ThinWallUtility.TryGetOwnedEdge(target, out OwnedEdge edge) ||
            !ThinWallUtility.StepCrossesEdge(actor.Position, attemptedStep.Cell, edge.Shared))
        {
            EndJobWith(JobCondition.Incompletable);
            return;
        }

        actor.meleeVerbs.TryMeleeAttack(target, null, false);
    }
}
