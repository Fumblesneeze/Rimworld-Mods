using NUnit.Framework;
using ThinWalls.Geometry;
using ThinWalls.Pathing;
using Verse;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class SparseEdgeMaskTests
{
    [Test]
    public void OneWallRetainsOnlyItsSixIncidentCellsAndRemovalReleasesThem()
    {
        var masks = new SparseEdgeMasks();
        var edge = new OwnedEdge(new IntVec3(150, 0, 150), ThinWallSide.North).Shared;
        masks.Set(edge, true);
        Assert.That(masks.Cells.Count, Is.EqualTo(6));
        Assert.That(masks.At(edge.AnchorCell) & CellConnection.North, Is.Not.EqualTo(CellConnection.Self));
        Assert.That(masks.At(new IntVec3(1, 0, 1)), Is.EqualTo(CellConnection.Self));
        masks.Set(edge, false);
        Assert.That(masks.Cells, Is.Empty);
    }

    [Test]
    public void RemovingAnIntersectingWallPreservesTheOtherWallsDiagonalBits()
    {
        var masks = new SparseEdgeMasks();
        var cell = new IntVec3(150, 0, 150);
        var north = new OwnedEdge(cell, ThinWallSide.North).Shared;
        var east = new OwnedEdge(cell, ThinWallSide.East).Shared;
        masks.Set(north, true);
        masks.Set(east, true);
        masks.Set(east, true);
        masks.Set(north, false);
        Assert.That(masks.At(cell), Is.EqualTo(CellConnection.East | CellConnection.NorthEast | CellConnection.SouthEast));
        masks.Set(east, false);
        Assert.That(masks.Cells, Is.Empty);
    }

    [Test]
    public void NativeDisabledBitsStayDisabledWhenTheLastOverlayIsRestored()
    {
        var overlay = new SparseConnectivityOverlay();
        var cells = new[] { CellConnection.East | CellConnection.NorthEast, CellConnection.South };
        overlay.Apply(0, CellConnection.North | CellConnection.East, i => cells[i], (i, bits) => cells[i] = bits);
        Assert.That(cells[0], Is.EqualTo(CellConnection.NorthEast));
        Assert.That(overlay.Removed.Count, Is.EqualTo(1));
        overlay.Restore(i => cells[i], (i, bits) => cells[i] = bits);
        Assert.That(cells[0], Is.EqualTo(CellConnection.East | CellConnection.NorthEast));
        Assert.That(cells[1], Is.EqualTo(CellConnection.South));
        Assert.That(overlay.Removed, Is.Empty);
    }

    [Test]
    public void OverlayRoundTripPreservesEveryNativeBitCombinationIncludingOverlaps()
    {
        for (int native = 0; native <= 255; native++)
        {
            var overlay = new SparseConnectivityOverlay();
            CellConnection value = (CellConnection)native;
            overlay.Apply(5, CellConnection.North | CellConnection.NorthEast, _ => value, (_, bits) => value = bits);
            overlay.Apply(5, CellConnection.East | CellConnection.NorthEast, _ => value, (_, bits) => value = bits);
            Assert.That(value, Is.EqualTo((CellConnection)native & ~(CellConnection.North | CellConnection.East | CellConnection.NorthEast)));
            overlay.Restore(_ => value, (_, bits) => value = bits);
            Assert.That(value, Is.EqualTo((CellConnection)native), "Native bits " + native);
            // A later native update takes precedence over the previous overlay's removed bits.
            value = CellConnection.South;
            overlay.Apply(5, CellConnection.NorthEast, _ => value, (_, bits) => value = bits);
            overlay.Restore(_ => value, (_, bits) => value = bits);
            Assert.That(value, Is.EqualTo(CellConnection.South));
        }
    }
}
