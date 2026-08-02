using Verse;
using Verse.AI;

namespace ImmersiveChefs;

public sealed class JobDriver_AssistCooking : JobDriver
{
    private Thing Station => job.GetTarget(TargetIndex.A).Thing;
    private Pawn Lead => (Pawn)job.GetTarget(TargetIndex.B).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(Station, job, 1, -1, null, errorOnFailed) &&
        KitchenAssistanceRegistry.Claim(pawn, Station, Lead);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOn(() => !KitchenAssistanceRegistry.ClaimStillActive(pawn));
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
        var work = new Toil
        {
            initAction = () => KitchenAssistanceRegistry.BeginWorking(pawn),
            defaultCompleteMode = ToilCompleteMode.Never,
            tickAction = () => pawn.skills?.Learn(RimWorld.SkillDefOf.Cooking, 0.03f)
        };
        work.AddFinishAction(() => KitchenAssistanceRegistry.StopWorking(pawn));
        yield return work;
    }

}
