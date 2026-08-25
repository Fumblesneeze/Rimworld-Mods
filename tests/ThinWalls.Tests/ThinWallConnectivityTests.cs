using System.Linq;
using NUnit.Framework;
using ThinWalls.Geometry;
using ThinWalls.Pathing;
using Verse;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class ThinWallConnectivityTests
{
    [Test]
    public void NorthSharedEdgeRemovesBothCardinalDirections()
    {
        var anchor = new IntVec3(10, 0, 20);
        var removals = ThinWallConnectivity.Removals(new SharedEdge(anchor, ThinWallSide.North));

        Assert.Multiple(() =>
        {
            Assert.That(removals.Any(item => item.Cell == anchor && item.Connection == CellConnection.North), Is.True);
            Assert.That(removals.Any(item => item.Cell == new IntVec3(10, 0, 21) && item.Connection == CellConnection.South), Is.True);
        });
    }

    [Test]
    public void NorthSharedEdgeRemovesEveryDiagonalThatTouchesEitherEndpoint()
    {
        var anchor = new IntVec3(10, 0, 20);
        var removals = ThinWallConnectivity.Removals(new SharedEdge(anchor, ThinWallSide.North));

        Assert.Multiple(() =>
        {
            Assert.That(removals, Has.Count.EqualTo(10));
            Assert.That(removals.Any(item => item.Cell == anchor && item.Connection == CellConnection.NorthEast), Is.True);
            Assert.That(removals.Any(item => item.Cell == anchor && item.Connection == CellConnection.NorthWest), Is.True);
            Assert.That(removals.Any(item => item.Cell == new IntVec3(9, 0, 20) && item.Connection == CellConnection.NorthEast), Is.True);
            Assert.That(removals.Any(item => item.Cell == new IntVec3(11, 0, 20) && item.Connection == CellConnection.NorthWest), Is.True);
        });
    }

    [Test]
    public void DiagonalStepReportsAllFourEdgesIncidentToTheCrossedVertex()
    {
        var from = new IntVec3(10, 0, 20);
        SharedEdge[] crossed = ThinWallUtility
            .SharedEdgesCrossed(from, new IntVec3(11, 0, 21))
            .ToArray();

        Assert.That(crossed, Is.EquivalentTo(new[]
        {
            new OwnedEdge(from, ThinWallSide.North).Shared,
            new OwnedEdge(from, ThinWallSide.East).Shared,
            new OwnedEdge(new IntVec3(11, 0, 20), ThinWallSide.North).Shared,
            new OwnedEdge(new IntVec3(10, 0, 21), ThinWallSide.East).Shared,
        }));
    }

    [Test]
    public void AllocationFreeCrossingCheckRecognizesNearAndFarEndpointEdges()
    {
        var from = new IntVec3(10, 0, 20);
        var to = new IntVec3(11, 0, 21);
        SharedEdge[] crossed = ThinWallUtility.SharedEdgesCrossed(from, to).ToArray();

        Assert.Multiple(() =>
        {
            foreach (SharedEdge edge in crossed)
            {
                Assert.That(ThinWallUtility.StepCrossesEdge(from, to, edge), Is.True, edge.ToString());
            }

            Assert.That(ThinWallUtility.StepCrossesEdge(
                from,
                to,
                new OwnedEdge(from + IntVec3.West, ThinWallSide.West).Shared), Is.False);
        });
    }
}
