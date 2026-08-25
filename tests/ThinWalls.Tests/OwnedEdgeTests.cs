using System.Collections.Generic;
using NUnit.Framework;
using ThinWalls.Geometry;
using Verse;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class OwnedEdgeTests
{
    [TestCase(ThinWallSide.North, 10, 21)]
    [TestCase(ThinWallSide.East, 11, 20)]
    [TestCase(ThinWallSide.South, 10, 19)]
    [TestCase(ThinWallSide.West, 9, 20)]
    public void OppositeCellIsTheRoomAndDoorCellAcrossTheOwnedBoundary(
        ThinWallSide side,
        int expectedX,
        int expectedZ)
    {
        var edge = new OwnedEdge(new IntVec3(10, 0, 20), side);

        Assert.That(edge.OppositeCell, Is.EqualTo(new IntVec3(expectedX, 0, expectedZ)));
    }

    [Test]
    public void OppositeOwnersResolveToSameSharedEdgeButRemainDistinctOwners()
    {
        var southOwner = new OwnedEdge(new IntVec3(4, 0, 6), ThinWallSide.North);
        var northOwner = new OwnedEdge(new IntVec3(4, 0, 7), ThinWallSide.South);

        Assert.Multiple(() =>
        {
            Assert.That(southOwner.Shared, Is.EqualTo(northOwner.Shared));
            Assert.That(southOwner, Is.Not.EqualTo(northOwner));
        });
    }

    [Test]
    public void OneCellCanOwnFourEdgesWhileExactDuplicatesCollapse()
    {
        var cell = new IntVec3(2, 0, 3);
        var edges = new HashSet<OwnedEdge>
        {
            new(cell, ThinWallSide.North),
            new(cell, ThinWallSide.East),
            new(cell, ThinWallSide.South),
            new(cell, ThinWallSide.West),
            new(cell, ThinWallSide.East),
        };

        Assert.That(edges, Has.Count.EqualTo(4));
    }
}
