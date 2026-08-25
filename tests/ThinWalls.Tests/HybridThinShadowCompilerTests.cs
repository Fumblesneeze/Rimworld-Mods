using System.Linq;
using NUnit.Framework;
using ThinWalls.Rendering;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class HybridThinShadowCompilerTests
{
    [Test]
    public void EveryThinMaskAndDoubledVariantUsesOneFinalUnionWithoutInternalOrHandoffCastingEdges()
    {
        for (int bits = 0; bits < 16; bits++)
        {
            var rays = (HybridWallRayMask)bits;
            foreach (HybridWallRayMask doubledRays in new[] { HybridWallRayMask.None, rays })
            {
                HybridRegularShadowPlan plan = HybridThinShadowCompiler.Compile(rays, doubledRays);
                int width = doubledRays == HybridWallRayMask.None ? 7 : 14;
                int min = 30 - width / 2;

                Assert.Multiple(() =>
                {
                    Assert.That(plan.OccupiedCount,
                        bits == 0 ? Is.EqualTo(0) : Is.GreaterThan(0),
                        $"{rays}/{doubledRays}");
                    Assert.That(plan.CastingEdges.Any(edge =>
                            (edge.MinX == edge.MaxX && (edge.MinX == 0 || edge.MinX == 60)) ||
                            (edge.MinY == edge.MaxY && (edge.MinY == 0 || edge.MinY == 60))),
                        Is.False,
                        $"{rays}/{doubledRays}: paired raster boundaries are continuations");
                    if (PopCount(bits) > 1)
                    {
                        Assert.That(plan.CastingEdges.Any(edge =>
                                edge.MinX == 30 && edge.MaxX == 30 && edge.MaxY - edge.MinY >= width),
                            Is.False,
                            $"{rays}/{doubledRays}: no vertical ray-local cap may survive at the occupied vertex");
                        Assert.That(plan.CastingEdges.Any(edge =>
                                edge.MinY == 30 && edge.MaxY == 30 && edge.MaxX - edge.MinX >= width),
                            Is.False,
                            $"{rays}/{doubledRays}: no horizontal ray-local cap may survive at the occupied vertex");
                    }

                    if (rays.HasFlag(HybridWallRayMask.North))
                        Assert.That(Enumerable.Range(min, width).All(x => plan.Contains(x, 59)), Is.True, $"{rays}/{doubledRays} North");
                    if (rays.HasFlag(HybridWallRayMask.East))
                        Assert.That(Enumerable.Range(min, width).All(y => plan.Contains(59, y)), Is.True, $"{rays}/{doubledRays} East");
                    if (rays.HasFlag(HybridWallRayMask.South))
                        Assert.That(Enumerable.Range(min, width).All(x => plan.Contains(x, 0)), Is.True, $"{rays}/{doubledRays} South");
                    if (rays.HasFlag(HybridWallRayMask.West))
                        Assert.That(Enumerable.Range(min, width).All(y => plan.Contains(0, y)), Is.True, $"{rays}/{doubledRays} West");
                });
            }
        }
    }

    [Test]
    public void StraightShadowRetainsExactCenteredSevenPixelFootprint()
    {
        HybridRegularShadowPlan horizontal = HybridThinShadowCompiler.Compile(
            HybridWallRayMask.East | HybridWallRayMask.West,
            HybridWallRayMask.None);
        HybridRegularShadowPlan vertical = HybridThinShadowCompiler.Compile(
            HybridWallRayMask.North | HybridWallRayMask.South,
            HybridWallRayMask.None);

        Assert.Multiple(() =>
        {
            Assert.That(horizontal.OccupiedCount, Is.EqualTo(60 * 7));
            Assert.That(vertical.OccupiedCount, Is.EqualTo(60 * 7));
            Assert.That(Enumerable.Range(27, 7).All(y => horizontal.Contains(30, y)), Is.True);
            Assert.That(Enumerable.Range(27, 7).All(x => vertical.Contains(x, 30)), Is.True);
        });
    }

    [Test]
    public void CastingEdgesUseTheNativePrinterShadowSidesAndTriangleWinding()
    {
        HybridRegularShadowPlan horizontal = HybridThinShadowCompiler.Compile(
            HybridWallRayMask.East | HybridWallRayMask.West,
            HybridWallRayMask.None);
        HybridRegularShadowPlan vertical = HybridThinShadowCompiler.Compile(
            HybridWallRayMask.North | HybridWallRayMask.South,
            HybridWallRayMask.None);

        HybridWallShadowEdge south = horizontal.CastingEdges.Single(edge =>
            edge.MinY == edge.MaxY);
        HybridWallShadowEdge west = vertical.CastingEdges.Single(edge =>
            edge.MinX == edge.MaxX && edge.MinX < 30);
        HybridWallShadowEdge east = vertical.CastingEdges.Single(edge =>
            edge.MinX == edge.MaxX && edge.MinX > 30);

        Assert.Multiple(() =>
        {
            Assert.That(south.CastingSide, Is.EqualTo(HybridWallShadowCastingSide.South));
            Assert.That(west.CastingSide, Is.EqualTo(HybridWallShadowCastingSide.West));
            Assert.That(east.CastingSide, Is.EqualTo(HybridWallShadowCastingSide.East));
            Assert.That(HybridWallShadowMeshTopology.TriangleIndices(south.CastingSide),
                Is.EqualTo(new[] { 0, 1, 2, 1, 3, 2 }),
                "south-facing walls must use Printer_Shadow's non-culled south-edge winding");
            Assert.That(HybridWallShadowMeshTopology.TriangleIndices(west.CastingSide),
                Is.EqualTo(new[] { 0, 2, 3, 0, 3, 1 }));
            Assert.That(HybridWallShadowMeshTopology.TriangleIndices(east.CastingSide),
                Is.EqualTo(new[] { 0, 2, 3, 0, 3, 1 }));
        });
    }

    private static int PopCount(int value)
    {
        int count = 0;
        while (value != 0)
        {
            count += value & 1;
            value >>= 1;
        }
        return count;
    }
}
