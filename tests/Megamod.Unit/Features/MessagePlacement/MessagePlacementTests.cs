using Megamod.Features.MessagePlacement;
using NUnit.Framework;
using Placement = Megamod.Features.MessagePlacement.MessagePlacement;

namespace Megamod.Unit.Features.MessagePlacement;

[TestFixture]
public sealed class MessagePlacementTests
{
    [TestCase(double.NaN, 0)]
    [TestCase(10, 101)]
    public void InvalidOffsetsCannotProduceAnOwnedTranslation(double horizontal, double vertical)
    {
        Assert.That(Placement.TryCalculate(new[] { new MessageBounds(20, 30, 200, 40) }, 1000, 600,
            null, new(horizontal, vertical), out var shift), Is.False);
        Assert.That(shift, Is.EqualTo(default(MessageTranslation)));
    }
    [Test]
    public void EmptyStackNeedsNoTranslation()
    {
        Assert.That(Placement.TryCalculate(Array.Empty<MessageBounds>(), 1000, 600, 100, new(20, 30), out var shift), Is.True);
        Assert.That(shift, Is.EqualTo(default(MessageTranslation)));
    }

    [TestCase(double.NaN, 100)]
    [TestCase(500, 0)]
    [TestCase(-500, 100)]
    [TestCase(double.PositiveInfinity, 100)]
    public void UnusableViewportDoesNotProduceAnOwnedTranslation(double width, double height)
    {
        Assert.That(Placement.TryCalculate(new[] { new MessageBounds(50, 40, 200, 30) }, width, height,
            null, default, out var shift), Is.False);
        Assert.That(shift, Is.EqualTo(default(MessageTranslation)));
    }

    [Test]
    public void UnusableMessageExtentDoesNotProduceAnOwnedTranslation()
    {
        Assert.That(Placement.TryCalculate(new[] { new MessageBounds(50, 40, -200, 30) }, 1000, 600,
            null, default, out var shift), Is.False);
        Assert.That(shift, Is.EqualTo(default(MessageTranslation)));
    }
    [TestCase(30, 170)]
    [TestCase(260, -40)]
    public void OversizedStackKeepsNewestVisibleOrTopAlignedAndRetainsOlderSpacing(double newestHeight, double expectedY)
    {
        var newest = new MessageBounds(80, 40, 100, newestHeight);
        var older = new MessageBounds(60, 80, 900, 500);
        Assert.That(Placement.TryCalculate(new[] { newest, older }, 500, 240, 100, new(100, 100), out var shift), Is.True);
        Assert.That(older.X + shift.X, Is.EqualTo(0));
        Assert.That(shift.Y, Is.EqualTo(expectedY));
        Assert.That((older.Y + shift.Y) - (newest.Y + shift.Y), Is.EqualTo(40));
    }
    [TestCase(100, 100, 360, 150)]
    [TestCase(-100, -100, -20, -250)]
    public void FittingMovesTheEntireStackJustEnoughWithoutChangingRelativePositions(
        double xPercent, double yPercent, double expectedX, double expectedY)
    {
        var newest = new MessageBounds(50, 250, 250, 40);
        var older = new MessageBounds(20, 290, 620, 60);
        Assert.That(Placement.TryCalculate(new[] { newest, older }, 1000, 500, 245,
            new(xPercent, yPercent), out var shift), Is.True);
        Assert.That(shift, Is.EqualTo(new MessageTranslation(expectedX, expectedY)));
        Assert.That((older.Y + shift.Y) - (newest.Y + shift.Y), Is.EqualTo(40));
        Assert.That((older.X + shift.X) - (newest.X + shift.X), Is.EqualTo(-30));
    }

    [Test]
    public void FittingOverridesClearanceWhenTheStackOnlyFitsAcrossTheWholeViewport()
    {
        var messages = new[] { new MessageBounds(20, 20, 300, 200), new MessageBounds(20, 220, 300, 200) };
        Assert.That(Placement.TryCalculate(messages, 1000, 500, 300, default, out var shift), Is.True);
        Assert.That(messages[0].Y + shift.Y, Is.EqualTo(100));
        Assert.That(messages[1].Y + messages[1].Height + shift.Y, Is.EqualTo(500));
    }
    [TestCase(null)]
    [TestCase(20d)]
    public void NativeLessonPositionIsKeptWhenTheBarIsHiddenOrAlreadyAboveIt(double? barBottom)
    {
        var messages = new[] { new MessageBounds(134, 74, 280, 26) };
        Assert.That(Placement.TryCalculate(messages, 1200, 800, barBottom, default, out var movement), Is.True);
        Assert.That(movement, Is.EqualTo(new MessageTranslation(0, 0)));
    }

    [TestCase(1000, 600, 100, -60)]
    [TestCase(1600, 900, 160, -90)]
    public void ManualPairUsesCurrentLogicalViewportAfterClearanceAndCanOverlapTheBar(
        double width, double height, double expectedX, double expectedY)
    {
        var offsets = new MessageOffsets(10, -10);
        var messages = new[] { new MessageBounds(134, 34, 280, 26) };
        Assert.That(Placement.TryCalculate(messages, width, height, 150, offsets, out var movement), Is.True);
        Assert.That(movement.X, Is.EqualTo(expectedX));
        Assert.That(messages[0].Y + movement.Y, Is.EqualTo(154 + expectedY));
        Assert.That(offsets, Is.EqualTo(new MessageOffsets(10, -10)));
    }
    [Test]
    public void ClearanceTranslatesThePaddedStackWithoutAddingItsNativeOriginAgain()
    {
        var messages = new[] { new MessageBounds(134, 34, 280, 26), new MessageBounds(134, 60, 440, 26) };
        Assert.That(Placement.TryCalculate(messages, 1200, 800, 150, default, out var movement), Is.True);
        Assert.That(messages[0].Y + movement.Y, Is.EqualTo(154));
        Assert.That(messages[0].X + movement.X, Is.EqualTo(134));
        Assert.That(messages[1].Y + movement.Y, Is.EqualTo(180));
    }
}
