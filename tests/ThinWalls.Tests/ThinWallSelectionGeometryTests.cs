using NUnit.Framework;
using ThinWalls.Geometry;
using ThinWalls.Rendering;
using UnityEngine;
using Verse;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class ThinWallSelectionGeometryTests
{
    [Test]
    public void NorthEdgeCanBeClickedFromBothCellsButNotAtTheOwnerCellCenter()
    {
        var edge = new OwnedEdge(new IntVec3(10, 0, 20), ThinWallSide.North);
        Assert.That(ThinWallSelectionGeometry.Contains(edge, new Vector3(10.5f, 0, 20.9f)), Is.True);
        Assert.That(ThinWallSelectionGeometry.Contains(edge, new Vector3(10.5f, 0, 21.1f)), Is.True);
        Assert.That(ThinWallSelectionGeometry.Contains(edge, new Vector3(10.5f, 0, 20.5f)), Is.False);
    }

    [TestCase(ThinWallSide.North, 10.5f, 21f, 1f, 34f / 60f)]
    [TestCase(ThinWallSide.East, 11f, 20.5f, 34f / 60f, 1f)]
    [TestCase(ThinWallSide.South, 10.5f, 20f, 1f, 34f / 60f)]
    [TestCase(ThinWallSide.West, 10f, 20.5f, 34f / 60f, 1f)]
    public void EveryOrientationUsesTheExistingBracketRectangle(
        ThinWallSide side, float x, float z, float width, float depth)
    {
        var edge = new OwnedEdge(new IntVec3(10, 0, 20), side);
        Rect bounds = ThinWallSelectionGeometry.Bounds(edge);
        Assert.That(bounds.center.x, Is.EqualTo(x).Within(0.00001f));
        Assert.That(bounds.center.y, Is.EqualTo(z).Within(0.00001f));
        Assert.That(bounds.width, Is.EqualTo(width));
        Assert.That(bounds.height, Is.EqualTo(depth));
        Assert.That(ThinWallSelectionGeometry.Size(edge), Is.EqualTo(new Vector2(width, depth)));
        foreach (float px in new[] { bounds.xMin, x, bounds.xMax })
        foreach (float pz in new[] { bounds.yMin, z, bounds.yMax })
            Assert.That(ThinWallSelectionGeometry.Contains(edge, new Vector3(px, 100f, pz)), Is.True);
        foreach (Vector3 outside in new[] {
                     new Vector3(bounds.xMin - 0.001f, 0, z), new Vector3(bounds.xMax + 0.001f, 0, z),
                     new Vector3(x, 0, bounds.yMin - 0.001f), new Vector3(x, 0, bounds.yMax + 0.001f) })
            Assert.That(ThinWallSelectionGeometry.Contains(edge, outside), Is.False);
    }

    [TestCase(ThinWallSide.North, ThinWallSide.South)]
    [TestCase(ThinWallSide.East, ThinWallSide.West)]
    [TestCase(ThinWallSide.South, ThinWallSide.North)]
    [TestCase(ThinWallSide.West, ThinWallSide.East)]
    public void OppositeOwnerHasExactlyTheSameHitArea(ThinWallSide first, ThinWallSide opposite)
    {
        var edge = new OwnedEdge(new IntVec3(10, 0, 20), first);
        Assert.That(ThinWallSelectionGeometry.Bounds(new OwnedEdge(edge.OppositeCell, opposite)),
            Is.EqualTo(ThinWallSelectionGeometry.Bounds(edge)));
    }

    [Test]
    public void OneCellPocketLeavesItsCenterFreeAndEachEdgeIndependentlyHittable()
    {
        var cell = new IntVec3(10, 0, 20);
        foreach (ThinWallSide side in System.Enum.GetValues(typeof(ThinWallSide)))
        {
            var edge = new OwnedEdge(cell, side);
            Assert.That(ThinWallSelectionGeometry.Contains(edge, cell.ToVector3Shifted()), Is.False);
            Vector3 pointer = ThinWallRenderGeometry.StructuralCenter(edge, 0f);
            foreach (ThinWallSide candidate in System.Enum.GetValues(typeof(ThinWallSide)))
                Assert.That(ThinWallSelectionGeometry.Contains(new OwnedEdge(cell, candidate), pointer),
                    Is.EqualTo(side == candidate));
        }
    }
}
