using NUnit.Framework;
using PathfindingLab.Core;

namespace PathfindingLab.Unit;

public class RoutingCountsTests
{
    [Test]
    public void SnapshotsRemainStableAndSeparateRequestOwners()
    {
        var first = new RoutingCounts();
        var second = new RoutingCounts();
        var before = first.Snapshot;
        first.Handle(true);
        first.Handle(false);
        first.Fallback();
        Assert.That(before, Is.EqualTo(new RoutingSnapshot(0, 0, 0)));
        Assert.That(first.Snapshot, Is.EqualTo(new RoutingSnapshot(2, 1, 1)));
        Assert.That(second.Snapshot, Is.EqualTo(before));
    }

    [Test]
    public void RenewalAndSegmentDiagnosticsRetainActualBounds()
    {
        var counts = new RoutingCounts();
        counts.Handle(true, 50);
        counts.Renew(26);
        counts.Renew(24);
        counts.Handle(false, 30);
        Assert.That(counts.MaxSegmentSteps, Is.EqualTo(50));
        Assert.That(counts.Renewals, Is.EqualTo(2));
        Assert.That(counts.MinRenewalNodes, Is.EqualTo(24));
        Assert.That(counts.MaxRenewalNodes, Is.EqualTo(26));
    }
}
