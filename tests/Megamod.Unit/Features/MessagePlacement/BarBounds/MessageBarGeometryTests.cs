using Megamod.Features.MessagePlacement;
using NUnit.Framework;

namespace Megamod.Unit.Features.MessagePlacement.BarBounds;

[TestFixture]
public sealed class MessageBarGeometryTests
{
    [Test]
    public void CurrentRowsAndVisibleWeaponDriveClearanceInsteadOfPawnCount()
    {
        var entries = new[]
        {
            new MessageBarEntryGeometry(new(100, 21, 48, 48), true, 14, true, 3, null),
            new MessageBarEntryGeometry(new(100, 101, 48, 48), true, 14, false, 0, null)
        };
        Assert.That(MessageBarGeometry.TryMeasure(entries, 1, false, out var bottom), Is.True);
        Assert.That(bottom, Is.EqualTo(159));
        entries[1] = entries[1] with { WeaponVisible = true };
        Assert.That(MessageBarGeometry.TryMeasure(entries, 1, false, out bottom), Is.True);
        Assert.That(bottom!.Value, Is.EqualTo(193.4).Within(0.00001));
    }

    [Test]
    public void EmptyEntriesReserveOnlyTheirActuallyDisplayedGroupFrame()
    {
        var entries = new[] { new MessageBarEntryGeometry(new(100, 101, 48, 48), false, 14, true, 5, 1) };
        Assert.That(MessageBarGeometry.TryMeasure(entries, 1, false, out var bottom), Is.True);
        Assert.That(bottom, Is.Null, "No pawn means no portrait, label, weapon or selection overlay.");
        Assert.That(MessageBarGeometry.TryMeasure(entries, 1, true, out bottom), Is.True);
        Assert.That(bottom, Is.EqualTo(161));
    }

    [Test]
    public void CurrentSelectionContractsAndOnlyEligibleMoodEdgesExtendThePortrait()
    {
        var entries = new[] { new MessageBarEntryGeometry(new(100, 21, 96, 96), true, 0, false, 0, null) };
        Assert.That(MessageBarGeometry.TryMeasure(entries, 2, false, out var bottom), Is.True);
        Assert.That(bottom, Is.EqualTo(117));
        entries[0] = entries[0] with { RemainingSelectionJump = 0.5 };
        Assert.That(MessageBarGeometry.TryMeasure(entries, 2, false, out bottom), Is.True);
        Assert.That(bottom, Is.EqualTo(125));
        entries[0] = entries[0] with { RemainingSelectionJump = 0 };
        Assert.That(MessageBarGeometry.TryMeasure(entries, 2, false, out bottom), Is.True);
        Assert.That(bottom, Is.EqualTo(121), "Settled selection keeps only its actual outline expansion.");
        entries[0] = entries[0] with { RemainingSelectionJump = null, MoodEdge = 10 };
        Assert.That(MessageBarGeometry.TryMeasure(entries, 2, false, out bottom), Is.True);
        Assert.That(bottom, Is.EqualTo(127));
    }

    [Test]
    public void UnusablePreparedGeometryCannotPublishAPartialReserve()
    {
        var valid = new MessageBarEntryGeometry(new(100, 21, 48, 48), true, 14, true, 0, null);
        foreach (var invalid in new[]
        {
            valid with { Portrait = new(100, double.NaN, 48, 48) },
            valid with { LabelExtent = double.PositiveInfinity },
            valid with { MoodEdge = -1 },
            valid with { RemainingSelectionJump = 1.1 },
            valid with { Portrait = new(100, 21, -1, 48) }
        })
        {
            Assert.That(MessageBarGeometry.TryMeasure(new[] { valid, invalid }, 1, true, out var bottom), Is.False);
            Assert.That(bottom, Is.Null);
        }
        Assert.That(MessageBarGeometry.TryMeasure(new[] { valid }, double.NaN, true, out var invalidScaleBottom), Is.False);
        Assert.That(invalidScaleBottom, Is.Null);
    }
}
