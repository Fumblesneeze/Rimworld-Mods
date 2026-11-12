using Megamod.Features.DrugTolerance.Core;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.DrugTolerance;

[TestFixture]
public sealed class DrugToleranceLayoutTests
{
    [Test]
    public void NativeWindowThatFitsPhysicalResolutionMustStillFitTheScaledViewport()
    {
        Assert.That(DrugToleranceLayout.FitsViewport(0, 0, 1230, 720, 1024, 819), Is.False);
        Assert.That(DrugToleranceLayout.FitsViewport(25, 152, 1230, 720, 1280, 1024), Is.True);
    }

    [TestCase(0, 0, 1280, 720, 1280, 720, true)]
    [TestCase(-1, 0, 1230, 720, 1280, 900, false)]
    [TestCase(0, -1, 1230, 720, 1280, 900, false)]
    [TestCase(51, 0, 1230, 720, 1280, 900, false)]
    [TestCase(50, 0, 1230, 720, 1280, 900, true)]
    [TestCase(0, 181, 1230, 720, 1280, 900, false)]
    [TestCase(0, 180, 1230, 720, 1280, 900, true)]
    [TestCase(0, 0, 1230, 720, 1280, 719, false)]
    [TestCase(0, 0, 0, 720, 1280, 900, false)]
    [TestCase(0, 0, 1230, -1, 1280, 900, false)]
    [TestCase(float.NaN, 0, 1230, 720, 1280, 900, false)]
    [TestCase(0, float.PositiveInfinity, 1230, 720, 1280, 900, false)]
    [TestCase(0, 0, float.NaN, 720, 1280, 900, false)]
    [TestCase(0, 0, 1230, float.PositiveInfinity, 1280, 900, false)]
    [TestCase(0, 0, 1230, 720, float.PositiveInfinity, 900, false)]
    [TestCase(0, 0, 1230, 720, 1280, float.NaN, false)]
    [TestCase(0, 0, 1230, 720, 0, 900, false)]
    public void OpenWindowRequiresCurrentFiniteContainedGeometry(float x, float y, float width, float height,
        float viewportWidth, float viewportHeight, bool fits) =>
        Assert.That(DrugToleranceLayout.FitsViewport(x, y, width, height, viewportWidth, viewportHeight), Is.EqualTo(fits));

    [TestCase(false, 935, 815)]
    [TestCase(true, 935, 799)]
    public void ReservationKeepsTheNativeInsetAndMeasuredColumnSeparate(bool header, float incoming, float columnOffset)
    {
        Assert.That(DrugToleranceLayout.TryReserve(incoming, 100, header, out var result), Is.True);
        Assert.That(result.NativeWidth, Is.EqualTo(815));
        Assert.That(result.ColumnOffset, Is.EqualTo(columnOffset));
        Assert.That(result.ColumnWidth, Is.EqualTo(120));
    }
    [TestCase(false, 719, 120, false)]
    [TestCase(false, 720, 120, true)]
    [TestCase(true, 735, 120, false)]
    [TestCase(true, 736, 120, true)]
    [TestCase(true, 800, 200, false)]
    [TestCase(true, 816, 200, true)]
    [TestCase(false, 1420, 120, true)]
    [TestCase(false, 1421, 120, false)]
    [TestCase(false, float.NaN, 120, false)]
    [TestCase(false, 935, float.PositiveInfinity, false)]
    public void UnsupportedInputsNeverReserveNativeSpace(bool header, float incoming, float measured, bool fits)
    {
        Assert.That(DrugToleranceLayout.TryReserve(incoming, measured, header, out var result), Is.EqualTo(fits));
        if (fits) Assert.That(result.ColumnWidth, Is.GreaterThanOrEqualTo(measured));
        else Assert.That(result, Is.EqualTo(default(DrugToleranceReservation)));
    }
}
