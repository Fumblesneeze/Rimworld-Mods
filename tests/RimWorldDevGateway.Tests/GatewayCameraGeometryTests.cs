using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayCameraGeometryTests
{
    [Test]
    public void View_rectangle_is_recomputed_from_the_applied_transform_in_the_same_frame()
    {
        var rect = GatewayCameraGeometry.CalculateViewRect(
            132.5f,
            30.5f,
            24f,
            2048,
            1152);

        Assert.Multiple(() =>
        {
            Assert.That(rect.MinX, Is.EqualTo(88));
            Assert.That(rect.MaxX, Is.EqualTo(176));
            Assert.That(rect.MinZ, Is.EqualTo(5));
            Assert.That(rect.MaxZ, Is.EqualTo(55));
        });
    }
}
