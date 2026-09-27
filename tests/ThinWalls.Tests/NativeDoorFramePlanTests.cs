using System.Linq;
using NUnit.Framework;
using ThinWalls.Rendering;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class NativeDoorFramePlanTests
{
    [TestCase(HybridWallRayMask.East, 0f, NativeDoorMoverPlan.FrameLength, -17f / 60f, 17f / 60f)]
    [TestCase(HybridWallRayMask.West, -NativeDoorMoverPlan.FrameLength, 0f, -17f / 60f, 17f / 60f)]
    [TestCase(HybridWallRayMask.North, -17f / 60f, 17f / 60f, 0f, NativeDoorMoverPlan.FrameLength)]
    [TestCase(HybridWallRayMask.South, -17f / 60f, 17f / 60f, -NativeDoorMoverPlan.FrameLength, 0f)]
    public void FlushFrameUsesOneSquareStraightSourceStrip(
        HybridWallRayMask ray, float minX, float maxX, float minY, float maxY)
    {
        NativeWallMeshQuad[] plan = NativeDoorFramePlan.ForRay(ray).ToArray();
        Assert.That(plan, Is.Not.Empty);
        Assert.That(plan.Min(q => q.Destination.X), Is.EqualTo(minX).Within(0.00001f));
        Assert.That(plan.Max(q => q.Destination.XMax), Is.EqualTo(maxX).Within(0.00001f));
        Assert.That(plan.Min(q => q.Destination.Y), Is.EqualTo(minY).Within(0.00001f));
        Assert.That(plan.Max(q => q.Destination.YMax), Is.EqualTo(maxY).Within(0.00001f));
        Assert.That(plan.All(q => q.Triangle == 0), Is.True);
        HybridWallRayMask straight = ray == HybridWallRayMask.East || ray == HybridWallRayMask.West
            ? HybridWallRayMask.East | HybridWallRayMask.West
            : HybridWallRayMask.North | HybridWallRayMask.South;
        Assert.That(plan.All(q => q.SourceLinks == straight), Is.True);
    }
}
