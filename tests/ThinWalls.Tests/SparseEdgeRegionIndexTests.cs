using NUnit.Framework;
using ThinWalls.Geometry;
using ThinWalls.Pathing;
using Verse;

namespace ThinWalls.Tests;

public sealed class SparseEdgeRegionIndexTests
{
    [Test]
    public void OnlyFirstAndFinalOwnedEdgesRequireReceiverBoundaryRebuilds()
    {
        var index = new SparseEdgeRegionIndex();
        var first = new OwnedEdge(new IntVec3(15, 0, 15), ThinWallSide.East).Shared;
        var second = new OwnedEdge(new IntVec3(16, 0, 16), ThinWallSide.North).Shared;
        Assert.That(index.Set(first, true), Is.True);
        Assert.That(index.Set(first, true), Is.False);
        Assert.That(index.Set(second, true), Is.False);
        Assert.That(index.Set(first, false), Is.False);
        Assert.That(index.Set(second, false), Is.True);
        Assert.That(index.Set(second, false), Is.False);
    }

    [Test]
    public void OnlyTheTwoNativeRegionChunksTouchingAnEdgeNeedCustomRegionGeneration()
    {
        var index = new SparseEdgeRegionIndex();
        var boundary = new OwnedEdge(new IntVec3(11, 0, 24), ThinWallSide.East).Shared;
        index.Set(boundary, true);
        Assert.That(index.Contains(new IntVec3(0, 0, 24)), Is.True);
        Assert.That(index.Contains(new IntVec3(23, 0, 35)), Is.True);
        Assert.That(index.Contains(new IntVec3(36, 0, 24)), Is.False);
        Assert.That(index.Count, Is.EqualTo(2));
        index.Set(boundary, false);
        Assert.That(index.Count, Is.Zero);
    }

    [Test]
    public void BothSidesOfASplitChunkBoundaryUseMatchingSingleCellLinks()
    {
        var index = new SparseEdgeRegionIndex();
        index.Set(new OwnedEdge(new IntVec3(5, 0, 5), ThinWallSide.East).Shared, true);
        var south = new IntVec3(3, 0, 11);
        var north = new IntVec3(3, 0, 12);
        Assert.That(index.Contains(north), Is.True, "The receiver chunk must participate even without an owned edge.");
        Assert.That(index.UseCellLink(south, north), Is.True);
        Assert.That(index.UseCellLink(north, south), Is.True);
        Assert.That(index.UseCellLink(new IntVec3(3, 0, 23), new IntVec3(3, 0, 24)), Is.False,
            "The outer edge of the receiver ring must keep normal native span merging.");
    }
}
