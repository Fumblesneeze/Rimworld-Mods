using Megamod.Features.DrugTolerance.Core;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.DrugTolerance;

[TestFixture]
public sealed class DrugToleranceLayoutTests
{
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
