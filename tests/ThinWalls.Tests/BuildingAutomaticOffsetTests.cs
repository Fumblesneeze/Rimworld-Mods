using System.Collections.Generic;
using NUnit.Framework;
using ThinWalls.Geometry;
using ThinWalls.Rendering;
using Verse;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class BuildingAutomaticOffsetTests
{
    [TestCase(0, 0)] [TestCase(1, 5)] [TestCase(2, 7)] [TestCase(3, 6)]
    [TestCase(4, 1)] [TestCase(5, 0)] [TestCase(6, 8)] [TestCase(7, 7)]
    [TestCase(8, 3)] [TestCase(9, 4)] [TestCase(10, 0)] [TestCase(11, 5)]
    [TestCase(12, 2)] [TestCase(13, 3)] [TestCase(14, 1)] [TestCase(15, 0)]
    public void AllPerimeterCombinationsCancelOpposingSides(int mask, int expected)
    {
        var cell = new IntVec3(10, 0, 20);
        var edges = new HashSet<SharedEdge>();
        for (int side = 0; side < 4; side++)
            if ((mask & (1 << side)) != 0) edges.Add(new OwnedEdge(cell, (ThinWallSide)side).Shared);
        Assert.That(BuildingAutomaticOffset.Resolve(new CellRect(10, 20, 1, 1), edges.Contains), Is.EqualTo(expected));
    }

    [Test]
    public void RectangularFootprintsUseTheWholePerimeterButIgnoreInternalAndRemoteEdges()
    {
        var footprint = new CellRect(10, 20, 3, 2);
        var edges = new HashSet<SharedEdge>
        {
            new OwnedEdge(new IntVec3(12, 0, 21), ThinWallSide.North).Shared,
            new OwnedEdge(new IntVec3(12, 0, 20), ThinWallSide.East).Shared,
            new OwnedEdge(new IntVec3(11, 0, 20), ThinWallSide.North).Shared, // internal
            new OwnedEdge(new IntVec3(10, 0, 18), ThinWallSide.South).Shared, // remote
        };
        int visits = 0;
        Assert.That(BuildingAutomaticOffset.Resolve(footprint, edge => { visits++; return edges.Contains(edge); }), Is.EqualTo(6));
        Assert.That(visits, Is.EqualTo(2 * (footprint.Width + footprint.Height)));
        var oppositeOwners = new HashSet<SharedEdge>();
        foreach (SharedEdge edge in edges)
        {
            var owner = new OwnedEdge(edge.AnchorCell, edge.PositiveSide);
            oppositeOwners.Add(new OwnedEdge(owner.OppositeCell, (ThinWallSide)(((int)owner.Side + 2) % 4)).Shared);
        }
        Assert.That(BuildingAutomaticOffset.Resolve(footprint, oppositeOwners.Contains), Is.EqualTo(6));
    }

    [Test]
    public void NorthAndEastWallsChooseTheSouthwestGizmoPreset()
    {
        var cell = new IntVec3(10, 0, 20);
        var edges = new HashSet<SharedEdge>
        {
            new OwnedEdge(cell, ThinWallSide.North).Shared,
            new OwnedEdge(cell, ThinWallSide.East).Shared,
        };
        int step = BuildingAutomaticOffset.Resolve(new CellRect(10, 20, 1, 1), edges.Contains);
        var appearance = new BuildingAppearance(0, step);
        Assert.That(step, Is.EqualTo(6));
        Assert.That((appearance.OffsetX, appearance.OffsetZ), Is.EqualTo((-.2f, -.2f)));
    }
}
