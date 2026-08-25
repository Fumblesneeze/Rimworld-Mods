using NUnit.Framework;
using ThinWalls.Geometry;
using ThinWalls.Rendering;
using UnityEngine;
using Verse;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class ThinWallRenderGeometryTests
{
    [Test]
    public void UnionPlanesMeetWithoutCoplanarOverlap()
    {
        Assert.That(
            ThinWallRenderGeometry.UnionPlaneScale,
            Is.EqualTo(1f).Within(0.000001f));
    }

    [TestCase(ThinWallSide.North, 4.5f, 9f)]
    [TestCase(ThinWallSide.South, 4.5f, 8f)]
    [TestCase(ThinWallSide.East, 5f, 8.5f)]
    [TestCase(ThinWallSide.West, 4f, 8.5f)]
    public void CenterUsesTheCanonicalSharedEdge(
        ThinWallSide side,
        float expectedX,
        float expectedZ)
    {
        var edge = new OwnedEdge(new IntVec3(4, 0, 8), side);

        Assert.That(
            ThinWallRenderGeometry.Center(edge, 3f),
            Is.EqualTo(new Vector3(expectedX, 3f, expectedZ)));
    }

    [TestCase(ThinWallSide.North, 4.5f, 9f)]
    [TestCase(ThinWallSide.South, 4.5f, 8f)]
    [TestCase(ThinWallSide.East, 5f, 8.5f)]
    [TestCase(ThinWallSide.West, 4f, 8.5f)]
    public void StructuralCenterUsesTheSharedBoundaryBalanceTransform(
        ThinWallSide side,
        float expectedX,
        float expectedZ)
    {
        var edge = new OwnedEdge(new IntVec3(4, 0, 8), side);

        Assert.That(
            ThinWallRenderGeometry.StructuralCenter(edge, 3f),
            Is.EqualTo(new Vector3(expectedX, 3f, expectedZ)));
    }

    [Test]
    public void FinalOutlinedStraightBodiesBalanceAroundTheSharedBoundary()
    {
        HybridWallRasterPlan horizontal = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.East | HybridWallRayMask.West);
        HybridWallRasterPlan vertical = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.North | HybridWallRayMask.South);
        (int hMin, int hMaxExclusive) = Bounds(horizontal, useY: true);
        (int vMin, int vMaxExclusive) = Bounds(vertical, useY: false);

        float horizontalMin = hMin;
        float horizontalMax = hMaxExclusive;
        float verticalMin = vMin + ThinWallRenderGeometry.StructuralOffsetPixelsX;
        float verticalMax = vMaxExclusive + ThinWallRenderGeometry.StructuralOffsetPixelsX;

        Assert.Multiple(() =>
        {
            Assert.That((hMin, hMaxExclusive), Is.EqualTo((13, 48)));
            Assert.That((vMin, vMaxExclusive), Is.EqualTo((13, 47)));
            Assert.That(
                System.Math.Abs((30f - horizontalMin) - (horizontalMax - 30f)),
                Is.LessThanOrEqualTo(1f));
            Assert.That(
                System.Math.Abs((30f - verticalMin) - (verticalMax - 30f)),
                Is.LessThanOrEqualTo(1f));
        });
    }

    [TestCase(HybridWallQuadrant.NorthWest, HybridWallRayMask.East, true, 0)]
    [TestCase(HybridWallQuadrant.SouthWest, HybridWallRayMask.East, true, 59)]
    [TestCase(HybridWallQuadrant.NorthEast, HybridWallRayMask.South, false, 0)]
    [TestCase(HybridWallQuadrant.NorthWest, HybridWallRayMask.South, false, 59)]
    public void ExposedHybridGuttersUseTheSameBalancedCrossSection(
        HybridWallQuadrant quadrant,
        HybridWallRayMask ray,
        bool useY,
        int boundary)
    {
        HybridWallRasterWrite[] writes = HybridWallRegularApertureRecipe.Compile(
                quadrant,
                ray,
                HybridWallRayMask.None,
                HybridWallRayMask.None)
            .Where(write => write.Sample.IsStructural)
            .ToArray();
        int min = writes.Min(write => useY ? write.Y : write.X);
        int maxExclusive = writes.Max(write => useY ? write.Y : write.X) + 1;

        Assert.That(
            System.Math.Abs((boundary - min) - (maxExclusive - boundary)),
            Is.LessThanOrEqualTo(1f),
            $"{quadrant}/{ray} bounds [{min},{maxExclusive}) around {boundary}");
    }

    [TestCase(ThinWallSide.North)]
    [TestCase(ThinWallSide.East)]
    [TestCase(ThinWallSide.South)]
    [TestCase(ThinWallSide.West)]
    public void DirtyRegionContainsBothEndpointQuadrantsWithoutDuplicates(ThinWallSide side)
    {
        var edge = new OwnedEdge(new IntVec3(4, 0, 8), side);
        IntVec3[] cells = ThinWallRenderGeometry.IncidentCells(edge).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(cells, Has.Length.EqualTo(6));
            Assert.That(cells.Distinct().ToArray(), Has.Length.EqualTo(6));
            Assert.That(cells, Does.Contain(edge.Shared.AnchorCell));
        });
    }

    private static (int Min, int MaxExclusive) Bounds(HybridWallRasterPlan plan, bool useY)
    {
        int min = HybridWallRasterPlan.Size;
        int max = -1;
        for (int y = 0; y < HybridWallRasterPlan.Size; y++)
        for (int x = 0; x < HybridWallRasterPlan.Size; x++)
        {
            if (plan[x, y].Surface == HybridWallRasterSurface.Transparent)
            {
                continue;
            }

            int value = useY ? y : x;
            min = System.Math.Min(min, value);
            max = System.Math.Max(max, value);
        }

        return (min, max + 1);
    }
}
