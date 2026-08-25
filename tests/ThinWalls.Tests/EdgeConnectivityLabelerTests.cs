using NUnit.Framework;
using ThinWalls.Pathing;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class EdgeConnectivityLabelerTests
{
    [Test]
    public void OneBlockedEdgeSplitsReusableComponentLabels()
    {
        int connectionChecks = 0;
        int[] labels = EdgeConnectivityLabeler.Build(
            width: 3,
            height: 1,
            walkable: _ => true,
            connected: (from, to) =>
            {
                connectionChecks++;
                return !((from == 1 && to == 2) || (from == 2 && to == 1));
            });

        Assert.Multiple(() =>
        {
            Assert.That(labels[0], Is.EqualTo(labels[1]));
            Assert.That(labels[2], Is.Not.EqualTo(labels[1]));
            Assert.That(labels, Has.All.GreaterThan(0));
            Assert.That(connectionChecks, Is.GreaterThan(0));
        });
    }

    [Test]
    public void UnwalkableCellsRemainOutsideEveryComponent()
    {
        int[] labels = EdgeConnectivityLabeler.Build(
            width: 2,
            height: 1,
            walkable: index => index == 0,
            connected: (_, _) => true);

        Assert.That(labels, Is.EqualTo(new[] { 1, 0 }));
    }
}
