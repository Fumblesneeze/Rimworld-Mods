using System.Collections.Generic;
using System.Linq;
using GuestBedGizmo.Compatibility.Hospitality;
using Verse;

namespace GuestBedGizmo.Beds;

internal enum GizmoProjectionDecision
{
    Preserve,
    RemoveLegacyToggle,
    ReplaceOwnerCommand,
}

internal static class GuestBedGizmoGizmoProjection
{
    internal static GizmoProjectionDecision Decide(
        Gizmo gizmo,
        HospitalityRuntimeAdapter adapter)
    {
        if (adapter.IsLegacyGuestToggle(gizmo))
        {
            return GizmoProjectionDecision.RemoveLegacyToggle;
        }

        return gizmo is Command_SetBedOwnerType
            ? GizmoProjectionDecision.ReplaceOwnerCommand
            : GizmoProjectionDecision.Preserve;
    }

    internal static IEnumerable<Gizmo> Project(
        RimWorld.Building_Bed bed,
        IEnumerable<Gizmo> source,
        HospitalityRuntimeAdapter adapter)
    {
        Gizmo[] gizmos = source.ToArray();
        bool hasOwnerCommand = gizmos.Any(gizmo => gizmo is Command_SetBedOwnerType);
        foreach (Gizmo gizmo in gizmos)
        {
            GizmoProjectionDecision decision = Decide(gizmo, adapter);
            switch (decision)
            {
                case GizmoProjectionDecision.Preserve:
                    yield return gizmo;
                    break;
                case GizmoProjectionDecision.RemoveLegacyToggle when !hasOwnerCommand:
                    yield return gizmo;
                    break;
                case GizmoProjectionDecision.ReplaceOwnerCommand:
                    yield return new Command_GuestAwareBedOwnerType(bed, adapter);
                    break;
            }
        }
    }
}
