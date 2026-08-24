using System.Linq;
using GuestBedGizmo.Beds;
using NUnit.Framework;

namespace GuestBedGizmo.Tests;

[TestFixture]
public sealed class BedOwnerChoiceCatalogTests
{
    [Test]
    public void UnifiedMenuUsesTheThreeVanillaChoicesThenHospitalityGuests()
    {
        BedOwnerChoice[] choices = BedOwnerChoiceCatalog.All.ToArray();

        Assert.That(choices.Select(choice => new
        {
            choice.Kind,
            choice.LabelKey,
            choice.IconPath,
        }), Is.EqualTo(new[]
        {
            new
            {
                Kind = BedOwnerChoiceKind.Colonist,
                LabelKey = "CommandBedSetForColonistsLabel",
                IconPath = "UI/Commands/ForColonists",
            },
            new
            {
                Kind = BedOwnerChoiceKind.Prisoner,
                LabelKey = "CommandBedSetForPrisonersLabel",
                IconPath = "UI/Commands/ForPrisoners",
            },
            new
            {
                Kind = BedOwnerChoiceKind.Slave,
                LabelKey = "CommandBedSetForSlavesLabel",
                IconPath = "UI/Commands/ForSlaves",
            },
            new
            {
                Kind = BedOwnerChoiceKind.Guest,
                LabelKey = "CommandBedSetAsGuestLabel",
                IconPath = "UI/Commands/AsGuest",
            },
        }));
    }
}
