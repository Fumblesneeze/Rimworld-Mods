using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ThinWalls.Rendering;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class NativeWallMeshPlanTests
{
    [TestCase(HybridWallRayMask.North, 0f, 0.5f, false)]
    [TestCase(HybridWallRayMask.East, 0f, 0.5f, true)]
    [TestCase(HybridWallRayMask.South, -0.5f, 0f, false)]
    [TestCase(HybridWallRayMask.West, -0.5f, 0f, true)]
    public void HalfRayCoversExactlyTheVertexToEdgeMidpointWithWorldPhase(
        HybridWallRayMask ray, float expectedMin, float expectedMax, bool horizontal)
    {
        NativeWallMeshQuad[] quads = NativeWallMeshPlan.HalfRay(ray).ToArray();
        Assert.That(quads, Is.Not.Empty);
        float min = quads.Min(q => horizontal ? q.Destination.X : q.Destination.Y);
        float max = quads.Max(q => horizontal ? q.Destination.XMax : q.Destination.YMax);
        float sourceMin = quads.Min(q => horizontal ? q.Source.X : q.Source.Y);
        float sourceMax = quads.Max(q => horizontal ? q.Source.XMax : q.Source.YMax);
        Assert.That(min, Is.EqualTo(expectedMin).Within(0.00001f));
        Assert.That(max, Is.EqualTo(expectedMax).Within(0.00001f));
        Assert.That(sourceMin, Is.EqualTo(expectedMin + 0.5f).Within(0.00001f));
        Assert.That(sourceMax, Is.EqualTo(expectedMax + 0.5f).Within(0.00001f));
    }

    [Test]
    public void VerticalPreservesBothNativeSideDepthsAndBalancesTheCompleteBody()
    {
        var bands = NativeWallMeshPlan.Straight(horizontal: false);
        Assert.That(bands.Count, Is.EqualTo(5));
        Assert.That(bands.Select(b => b.Source.Width * 60),
            Is.EqualTo(new[] { 3f, 11f, 33f, 10f, 3f }).Within(0.00001));
        Assert.That(bands.Select(b => b.Destination.Width * 60),
            Is.EqualTo(new[] { 3f, 11f, 7f, 10f, 3f }).Within(0.00001));
        Assert.That(bands.Min(b => b.Destination.X) * 60, Is.EqualTo(-17f).Within(0.00001));
        Assert.That(bands.Max(b => b.Destination.XMax) * 60, Is.EqualTo(17f).Within(0.00001));
        Assert.That(bands.All(b => b.Source.Y == 0 && b.Source.Height == 1), Is.True);
        Assert.That(bands.All(b => b.Destination.Height == 1), Is.True);
        for (int i = 1; i < bands.Count; i++)
            Assert.That(bands[i].Destination.X, Is.EqualTo(bands[i - 1].Destination.XMax).Within(0.000001));
    }

    [Test]
    public void HorizontalUsesFullNativeFaceAndOutlineButNarrowsOnlyTop()
    {
        var bands = NativeWallMeshPlan.Straight(horizontal: true);
        Assert.That(bands.Count, Is.EqualTo(4));
        Assert.That(bands.Select(b => b.Source.Height * 60),
            Is.EqualTo(new[] { 3f, 22f, 33f, 2f }).Within(0.00001));
        Assert.That(bands.Select(b => b.Destination.Height * 60),
            Is.EqualTo(new[] { 3f, 22f, 7f, 2f }).Within(0.00001));
        Assert.That(bands.Min(b => b.Destination.Y) * 60, Is.EqualTo(-17f).Within(0.00001));
        Assert.That(bands.Max(b => b.Destination.YMax) * 60, Is.EqualTo(17f).Within(0.00001));
        Assert.That(bands.All(b => b.Source.X == 0 && b.Source.Width == 1), Is.True,
            "Native longitudinal brick phase must not be resized or decorated.");
        for (int i = 1; i < bands.Count; i++)
            Assert.That(bands[i].Destination.Y, Is.EqualTo(bands[i - 1].Destination.YMax).Within(0.000001));
    }

    [Test]
    public void EveryTopologyReachesExactlyItsOccupiedHalfCellBoundaries()
    {
        for (int bits = 1; bits < 16; bits++)
        {
            HybridWallRayMask mask = (HybridWallRayMask)bits;
            var cells = NativeWallMeshPlan.Topology(mask);
            float minX = cells.Min(cell => cell.Destination.X);
            float maxX = cells.Max(cell => cell.Destination.XMax);
            float minZ = cells.Min(cell => cell.Destination.Y);
            float maxZ = cells.Max(cell => cell.Destination.YMax);
            bool single = bits is 1 or 2 or 4 or 8;
            Assert.That(minX, Is.EqualTo(mask.HasFlag(HybridWallRayMask.West)
                ? -0.5f
                : single && mask == HybridWallRayMask.East ? 0f : -17f / 60f).Within(0.00001), mask.ToString());
            Assert.That(maxX, Is.EqualTo(mask.HasFlag(HybridWallRayMask.East)
                ? 0.5f
                : single && mask == HybridWallRayMask.West ? 0f : 17f / 60f).Within(0.00001), mask.ToString());
            Assert.That(minZ, Is.EqualTo(mask.HasFlag(HybridWallRayMask.South)
                ? -0.5f
                : single && mask == HybridWallRayMask.North ? 0f : -17f / 60f).Within(0.00001), mask.ToString());
            Assert.That(maxZ, Is.EqualTo(mask.HasFlag(HybridWallRayMask.North)
                ? 0.5f
                : single && mask == HybridWallRayMask.South ? 0f : 17f / 60f).Within(0.00001), mask.ToString());
            Assert.That(cells.All(cell => cell.Source.Width > 0 && cell.Source.Height > 0 &&
                                                  cell.Destination.Width > 0 && cell.Destination.Height > 0), Is.True);
        }
    }

    [TestCase(HybridWallRayMask.North, 0f, 0.5f, false)]
    [TestCase(HybridWallRayMask.East, 0f, 0.5f, true)]
    [TestCase(HybridWallRayMask.South, -0.5f, 0f, false)]
    [TestCase(HybridWallRayMask.West, -0.5f, 0f, true)]
    public void SingleRayTerminalStopsAtVertexAndKeepsFullNormalProjection(
        HybridWallRayMask ray,
        float expectedLongitudinalMin,
        float expectedLongitudinalMax,
        bool horizontal)
    {
        NativeWallMeshQuad[] quads = NativeWallMeshPlan.Topology(ray).ToArray();
        Assert.That(quads, Is.Not.Empty);

        float longitudinalMin = quads.Min(quad => horizontal ? quad.Destination.X : quad.Destination.Y);
        float longitudinalMax = quads.Max(quad => horizontal ? quad.Destination.XMax : quad.Destination.YMax);
        float normalMin = quads.Min(quad => horizontal ? quad.Destination.Y : quad.Destination.X);
        float normalMax = quads.Max(quad => horizontal ? quad.Destination.YMax : quad.Destination.XMax);

        Assert.That(longitudinalMin, Is.EqualTo(expectedLongitudinalMin).Within(0.00001f));
        Assert.That(longitudinalMax, Is.EqualTo(expectedLongitudinalMax).Within(0.00001f));
        Assert.That(normalMin, Is.EqualTo(-17f / 60f).Within(0.00001f));
        Assert.That(normalMax, Is.EqualTo(17f / 60f).Within(0.00001f));
        Assert.That(quads.Length, Is.GreaterThan(NativeWallMeshPlan.HalfRay(ray).Count),
            "The source-derived native endpoint profile must be retained instead of replacing the terminal with a raw straight-strip cut.");
        Assert.That(quads.Any(quad => horizontal
                ? quad.Destination.X <= 0.00001f && quad.Destination.XMax >= -0.00001f
                : quad.Destination.Y <= 0.00001f && quad.Destination.YMax >= -0.00001f),
            Is.True,
            "At least one source-bound endpoint region must terminate exactly on the grid vertex.");
        Assert.That(quads.Sum(Area), Is.EqualTo(17f / 60f).Within(0.00001f),
            "A terminal half-ray must cover its exact half-cell structural rectangle once, without extra end area.");
        Assert.That(quads.All(IsConvexClockwise), Is.True,
            "Every terminal source polygon must remain convex and consistently wound for fan triangulation.");
        Assert.That(quads.All(HasExactFanArea), Is.True,
            "Fan triangles must cover each terminal polygon exactly once.");
    }

    [Test]
    public void JunctionsNeverStretchTheNativeFrontFaceToReachTheirSouthRay()
    {
        foreach (var mask in new[] { HybridWallRayMask.East | HybridWallRayMask.South,
                                    HybridWallRayMask.East | HybridWallRayMask.West | HybridWallRayMask.South })
        {
            var fronts = NativeWallMeshPlan.Topology(mask).Where(quad =>
                quad.SourceLinks == (HybridWallRayMask.East | HybridWallRayMask.West) &&
                quad.Source.YMax <= 25f / 60f + 0.00001f).ToArray();
            Assert.That(fronts, Is.Not.Empty);
            Assert.That(fronts.All(quad => System.Math.Abs(quad.Destination.Height / quad.Source.Height * 22f - 22f) < 0.00001), Is.True,
                "Connected rays require additional straight strips, not stretching the junction's facade.");
        }
    }

    [Test]
    public void EveryJunctionFacadeKeepsTheSameLongitudinalPhaseAsItsIncomingArm()
    {
        for (int mask = 1; mask < 16; mask++)
        {
        if (mask is 1 or 2 or 4 or 8) continue;
        foreach (var quad in NativeWallMeshPlan.Topology((HybridWallRayMask)mask))
        {
            if (quad.SourceLinks == (HybridWallRayMask.East | HybridWallRayMask.West) && quad.Source.YMax <= 25f / 60f + 0.00001f)
            {
                Assert.That(quad.Source.X, Is.EqualTo(quad.Destination.X + 0.5f).Within(0.00001), $"H phase mask {mask}");
                Assert.That(quad.Source.Width, Is.EqualTo(quad.Destination.Width).Within(0.00001));
            }
            if (quad.SourceLinks == (HybridWallRayMask.North | HybridWallRayMask.South) &&
                (quad.Source.XMax <= 14f / 60f + 0.00001f || quad.Source.X >= 47f / 60f - 0.00001f))
            {
                Assert.That(quad.Source.Y, Is.EqualTo(quad.Destination.Y + 0.5f).Within(0.00001), $"V phase mask {mask}");
                Assert.That(quad.Source.Height, Is.EqualTo(quad.Destination.Height).Within(0.00001));
            }
            bool centralTop = System.Math.Abs(quad.Destination.Width - 7f / 60f) < 0.00001 &&
                              System.Math.Abs(quad.Destination.Height - 7f / 60f) < 0.00001;
            Assert.That(centralTop || quad.SourceLinks == (HybridWallRayMask.East | HybridWallRayMask.West) ||
                        quad.SourceLinks == (HybridWallRayMask.North | HybridWallRayMask.South), Is.True,
                "Junction facade regions must expose their actual source plane so a hidden link-state phase reset cannot evade the gate.");
        }
        }
    }

    [Test]
    public void CentralTopUsesItsActualNativeTopologyWithoutAnInternalFrontRim()
    {
        foreach (int mask in new[] { 3, 6, 7, 9, 11, 12, 13, 14, 15 })
        {
            var top = NativeWallMeshPlan.Topology((HybridWallRayMask)mask).Single(quad =>
                System.Math.Abs(quad.Destination.Width - 7f / 60f) < 0.00001 &&
                System.Math.Abs(quad.Destination.Height - 7f / 60f) < 0.00001);
            Assert.That(top.SourceLinks, Is.EqualTo((HybridWallRayMask)mask),
                "Only the actual linked top suppresses all internal rims, including the South-connected front rim.");
            Assert.That(top.Source, Is.EqualTo(new HybridWallUvRect(14f / 60f, 25f / 60f, 33f / 60f, 33f / 60f)));
        }
    }

    private static float Area(NativeWallMeshQuad quad)
    {
        IReadOnlyList<NativeWallMeshVertex> vertices = NativeWallMeshGeometry.Vertices(quad);
        float twiceArea = 0f;
        for (int index = 0; index < vertices.Count; index++)
        {
            NativeWallMeshVertex current = vertices[index];
            NativeWallMeshVertex next = vertices[(index + 1) % vertices.Count];
            twiceArea += current.X * next.Y - next.X * current.Y;
        }
        return Math.Abs(twiceArea) * 0.5f;
    }

    private static bool IsConvexClockwise(NativeWallMeshQuad quad)
    {
        IReadOnlyList<NativeWallMeshVertex> vertices = NativeWallMeshGeometry.Vertices(quad);
        if (vertices.Count < 3) return false;
        for (int index = 0; index < vertices.Count; index++)
        {
            NativeWallMeshVertex a = vertices[index];
            NativeWallMeshVertex b = vertices[(index + 1) % vertices.Count];
            NativeWallMeshVertex c = vertices[(index + 2) % vertices.Count];
            float cross = (b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X);
            if (cross >= -0.000001f) return false;
        }
        return true;
    }

    private static bool HasExactFanArea(NativeWallMeshQuad quad)
    {
        IReadOnlyList<NativeWallMeshVertex> vertices = NativeWallMeshGeometry.Vertices(quad);
        float fanArea = 0f;
        for (int index = 1; index < vertices.Count - 1; index++)
        {
            NativeWallMeshVertex a = vertices[0];
            NativeWallMeshVertex b = vertices[index];
            NativeWallMeshVertex c = vertices[index + 1];
            fanArea += Math.Abs((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)) * 0.5f;
        }
        return Math.Abs(fanArea - Area(quad)) < 0.00001f;
    }
}
