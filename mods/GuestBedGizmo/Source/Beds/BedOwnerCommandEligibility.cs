using RimWorld;

namespace GuestBedGizmo.Beds;

internal static class BedOwnerCommandEligibility
{
    internal static bool IsEligible(Building_Bed bed) =>
        bed != null &&
        bed.Spawned &&
        bed.Faction == Faction.OfPlayer &&
        bed.def?.building?.bed_humanlike == true &&
        !bed.ForHumanBabies;
}
