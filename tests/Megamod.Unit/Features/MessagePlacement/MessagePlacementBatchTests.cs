using Megamod.Features.MessagePlacement;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.MessagePlacement;

[TestFixture]
public sealed class MessagePlacementBatchTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void EverySurfaceMustStillBeCurrentBeforeAnySurfaceIsWritten(bool throws)
    {
        int writes = 0, restores = 0, validations = 0;
        MessageSurface[] surfaces = [
            new(new(10, 10, 20, 20), _ => writes++, () => restores++, () => { validations++; return true; }),
            new(new(10, 40, 20, 20), _ => writes++, () => restores++, () =>
            { validations++; if (throws) throw new InvalidOperationException("stale window"); return false; })];
        Assert.That(MessagePlacementBatch.TryApply(surfaces, 1000, 800, null, new(10, 10)), Is.False);
        Assert.That(validations, Is.EqualTo(2));
        Assert.That(writes, Is.Zero);
        Assert.That(restores, Is.Zero, "No owned write occurred; do not overwrite the foreign mutation.");
    }

    [Test]
    public void ObservedNativeIrregularPositionsTranslateTogetherWithoutChangingSizesOrOrder()
    {
        var applied = new List<(int Id, MessageBounds Bounds)>();
        MessageSurface[] surfaces = [new(new(134, 34, 210, 25), value => applied.Add((7, value)), () => {}),
            new(new(139, 77, 160, 39), value => applied.Add((3, value)), () => {})];
        Assert.That(MessagePlacementBatch.TryApply(surfaces, 1000, 800, 100, new(10, 0)), Is.True);
        Assert.That(applied, Is.EqualTo(new[] { (7, new MessageBounds(234, 104, 210, 25)),
            (3, new MessageBounds(239, 147, 160, 39)) }));
    }

    [Test]
    public void FailedNativeSurfaceWriteRestoresEveryAttemptedSurfaceAndReportsFailure()
    {
        var first = new MessageBounds(10, 10, 20, 20);
        var second = new MessageBounds(10, 40, 20, 20);
        var observedFirst = first;
        var observedSecond = second;
        MessageSurface[] surfaces = [new(first, value => observedFirst = value, () => observedFirst = first), new(second, value =>
        {
            observedSecond = value;
            if (value != second) throw new InvalidOperationException("native setter failed after write");
        }, () => observedSecond = second)];
        Assert.That(MessagePlacementBatch.TryApply(surfaces, 1000, 800, null, new(10, 10)), Is.False);
        Assert.That(observedFirst, Is.EqualTo(first));
        Assert.That(observedSecond, Is.EqualTo(second));
    }

    [Test]
    public void InvalidGeometryDoesNotWriteAnySurface()
    {
        int writes = 0;
        MessageSurface[] surfaces = [new(new(10, 10, 20, 20), _ => writes++, () => {}),
            new(new(double.NaN, 40, 20, 20), _ => writes++, () => {})];
        Assert.That(MessagePlacementBatch.TryApply(surfaces, 1000, 800, null, default), Is.False);
        Assert.That(writes, Is.Zero);
    }

    [Test]
    public void RestorationFailureStillAttemptsEarlierSurfacesAndReportsBothFailures()
    {
        bool firstRestored = false;
        MessageSurface[] surfaces = [new(new(10, 10, 20, 20), _ => {}, () => firstRestored = true),
            new(new(10, 40, 20, 20), _ => throw new InvalidOperationException("write"),
                () => throw new InvalidOperationException("restore"))];
        var failure = Assert.Throws<AggregateException>(() => MessagePlacementBatch.TryApply(surfaces, 1000, 800, null, new(10, 0)));
        Assert.That(firstRestored, Is.True);
        Assert.That(failure!.InnerExceptions.Select(error => error.Message), Is.EqualTo(new[] { "write", "restore" }));
    }
}
