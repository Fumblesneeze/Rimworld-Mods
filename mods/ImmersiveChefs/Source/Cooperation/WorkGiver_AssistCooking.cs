using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

public sealed class WorkGiver_AssistCooking : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) =>
        pawn.Map.listerThings.AllThings.Where(thing =>
            thing.def.GetModExtension<KitchenAssistantStationExtension>() is not null);

    public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false) =>
        pawn.CanReserveAndReach(thing, PathEndMode.InteractionCell, Danger.Some) &&
        KitchenAssistanceRegistry.CanClaim(pawn, thing, out _);

    public override Job? JobOnThing(Pawn pawn, Thing thing, bool forced = false)
    {
        if (!KitchenAssistanceRegistry.CanClaim(pawn, thing, out var request) || request is null)
        {
            return null;
        }

        return JobMaker.MakeJob(ImmersiveChefsDefOf.ImmersiveChefs_AssistCooking, thing, request.Lead);
    }
}
