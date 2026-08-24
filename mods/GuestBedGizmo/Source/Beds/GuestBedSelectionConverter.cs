using System.Collections.Generic;
using System.Linq;
using GuestBedGizmo.Compatibility.Hospitality;
using RimWorld;
using Verse;

namespace GuestBedGizmo.Beds;

internal static class GuestBedSelectionConverter
{
    internal static void Apply(
        Building_Bed commandBed,
        HospitalityRuntimeAdapter adapter,
        BedOwnerChoiceKind choice)
    {
        List<object> selectedObjects = Find.Selector.SelectedObjects.ToList();
        if (!selectedObjects.Contains(commandBed))
        {
            selectedObjects.Add(commandBed);
        }

        List<Building_Bed> eligibleBeds = selectedObjects
            .OfType<Building_Bed>()
            .Where(BedOwnerCommandEligibility.IsEligible)
            .Distinct()
            .ToList();
        if (eligibleBeds.Count == 0)
        {
            return;
        }

        if (choice == BedOwnerChoiceKind.Guest)
        {
            bool[] included = Enumerable.Repeat(true, eligibleBeds.Count).ToArray();
            if (!GuestBedConversionTransaction.TryConvert(
                    eligibleBeds,
                    included,
                    adapter,
                    expectedGuestState: true,
                    out string? failure))
            {
                Log.ErrorOnce(
                    $"[Guest Bed Gizmo] Guest-bed conversion rejected before ownership changed: {failure}",
                    1748243121);
            }
            return;
        }

        if (choice == BedOwnerChoiceKind.Prisoner &&
            !Building_Bed.RoomCanBePrisonCell(
                RegionAndRoomQuery.GetRoom(commandBed, RegionType.Set_Passable)) &&
            !commandBed.ForPrisoners)
        {
            Messages.Message(
                "CommandBedSetForPrisonersFailOutdoors".Translate(),
                commandBed,
                MessageTypeDefOf.RejectInput,
                historical: false);
            return;
        }

        commandBed.SetBedOwnerTypeByInterface(ToVanillaOwnerType(choice));
    }

    private static BedOwnerType ToVanillaOwnerType(BedOwnerChoiceKind choice)
    {
        return choice switch
        {
            BedOwnerChoiceKind.Colonist => BedOwnerType.Colonist,
            BedOwnerChoiceKind.Prisoner => BedOwnerType.Prisoner,
            BedOwnerChoiceKind.Slave => BedOwnerType.Slave,
            _ => throw new ArgumentOutOfRangeException(nameof(choice), choice, "Guest is not a vanilla owner type."),
        };
    }
}
