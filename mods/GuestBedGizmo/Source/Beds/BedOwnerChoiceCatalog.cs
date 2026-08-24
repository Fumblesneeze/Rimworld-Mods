using System.Collections.Generic;
using GuestBedGizmo.Compatibility.Hospitality;

namespace GuestBedGizmo.Beds;

internal enum BedOwnerChoiceKind
{
    Colonist,
    Prisoner,
    Slave,
    Guest,
}

internal readonly struct BedOwnerChoice
{
    internal BedOwnerChoice(BedOwnerChoiceKind kind, string labelKey, string iconPath)
    {
        Kind = kind;
        LabelKey = labelKey;
        IconPath = iconPath;
    }

    internal BedOwnerChoiceKind Kind { get; }

    internal string LabelKey { get; }

    internal string IconPath { get; }
}

internal static class BedOwnerChoiceCatalog
{
    internal static IReadOnlyList<BedOwnerChoice> All { get; } = new[]
    {
        new BedOwnerChoice(
            BedOwnerChoiceKind.Colonist,
            "CommandBedSetForColonistsLabel",
            "UI/Commands/ForColonists"),
        new BedOwnerChoice(
            BedOwnerChoiceKind.Prisoner,
            "CommandBedSetForPrisonersLabel",
            "UI/Commands/ForPrisoners"),
        new BedOwnerChoice(
            BedOwnerChoiceKind.Slave,
            "CommandBedSetForSlavesLabel",
            "UI/Commands/ForSlaves"),
        new BedOwnerChoice(
            BedOwnerChoiceKind.Guest,
            HospitalityRuntimeContract.GuestLabelKey,
            HospitalityRuntimeContract.GuestIconPath),
    };
}
