using Megamod.Features.MessagePlacement;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.MessagePlacement;

[TestFixture]
public sealed class MessageBarBoundsReceiptTests
{
    [Test]
    public void DistinctStatesRetainOnePreparedReceiptWithReadOnlyRevalidation()
    {
        bool current = true;
        var visible = MessageBarBoundsReceipt.Visible(78.5, () => current);
        var absent = MessageBarBoundsReceipt.Absent(() => current);
        var unavailable = MessageBarBoundsReceipt.Unavailable("Megamod_Messages_BarUnavailable", () => current);
        Assert.That(visible.State, Is.EqualTo(MessageBarBoundsState.Visible));
        Assert.That(visible.Bottom, Is.EqualTo(78.5));
        Assert.That(visible.ReasonKey, Is.Null);
        Assert.That(absent.State, Is.EqualTo(MessageBarBoundsState.Absent));
        Assert.That(absent.Bottom, Is.Null);
        Assert.That(absent.ReasonKey, Is.Null);
        Assert.That(unavailable.State, Is.EqualTo(MessageBarBoundsState.Unavailable));
        Assert.That(unavailable.Bottom, Is.Null);
        Assert.That(unavailable.ReasonKey, Is.EqualTo("Megamod_Messages_BarUnavailable"));
        Assert.That(visible.IsCurrent() && absent.IsCurrent() && unavailable.IsCurrent(), Is.True);
        current = false;
        Assert.That(visible.IsCurrent() || absent.IsCurrent() || unavailable.IsCurrent(), Is.False);
        Assert.That(visible.Bottom, Is.EqualTo(78.5));
    }

    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void VisibleBoundsMustBeFinite(double bottom) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => MessageBarBoundsReceipt.Visible(bottom, () => true));
    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public void UnavailableBoundsMustExplainTheirState(string? reason) =>
        Assert.Throws<ArgumentException>(() => MessageBarBoundsReceipt.Unavailable(reason!, () => true));
}
