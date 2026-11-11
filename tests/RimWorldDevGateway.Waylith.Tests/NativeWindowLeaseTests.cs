using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeWindowLeaseTests
{
    [Test]
    public void ReplacementWithTheSameDisplayIdentityCannotReceiveTheOriginalGesture()
    {
        var original = new DisplayIdentity(0, "Dialog_ModSettings");
        var replacement = new DisplayIdentity(0, "Dialog_ModSettings");
        var stack = new object();
        var lease = new GatewayNativeWindowLease(original, stack);

        Assert.That(lease.Validate(original, stack), Is.Null);
        Assert.That(lease.Validate(replacement, stack), Is.EqualTo("stale_window_instance"));
    }

    [Test]
    public void MovingTheSameWindowToAnotherStackInvalidatesTheGesture()
    {
        var window = new object();
        var originalStack = new object();
        var lease = new GatewayNativeWindowLease(window, originalStack);

        Assert.That(lease.Validate(window, new object()), Is.EqualTo("stale_window_stack"));
        Assert.That(lease.Validate(window, originalStack), Is.Null);
    }

    [Test]
    public void AScaledDialogUsesRenderedPixelsWithoutApplyingUiScaleTwice()
    {
        var view = new GatewayNativeWindowGeometry(100, 50, 300, 200, 1.5, 1200, 900);

        var point = view.ResolvePoint(300, 180);

        Assert.That(point.Error, Is.Null);
        Assert.That(point.InputX, Is.EqualTo(300));
        Assert.That(point.InputY, Is.EqualTo(720));
    }

    [TestCase(0)]
    [TestCase(1)]
    public void NativeProjectionRoundTripsBothInspectedPixelOrigins(double originOffset)
    {
        var view = new GatewayNativeWindowGeometry(0, 0, 1600, 900, 1, 1600, 900, originOffset);
        foreach (double requestedY in new[] { 0d, 413d, 899d })
        {
            var point = view.ResolvePoint(946, requestedY);
            Assert.That(point.Error, Is.Null);
            double nativeGuiY = 900 - originOffset - point.InputY;
            Assert.That(nativeGuiY, Is.EqualTo(requestedY));
        }
    }

    [TestCase(149, 180)]
    [TestCase(300, 74)]
    [TestCase(600, 180)]
    [TestCase(300, 375)]
    [TestCase(double.NaN, 180)]
    [TestCase(300, double.PositiveInfinity)]
    public void APointOutsideTheDialogCannotBeRedirectedToAnotherControl(double x, double y)
    {
        var view = new GatewayNativeWindowGeometry(100, 50, 300, 200, 1.5, 1200, 900);

        var point = view.ResolvePoint(x, y);

        Assert.That(point.Error, Is.EqualTo("point_outside_window"));
    }

    [TestCase(0, 1200, 900, 300)]
    [TestCase(-1, 1200, 900, 300)]
    [TestCase(double.NaN, 1200, 900, 300)]
    [TestCase(double.PositiveInfinity, 1200, 900, 300)]
    [TestCase(1.5, 0, 900, 300)]
    [TestCase(1.5, 1200, 0, 300)]
    [TestCase(1.5, 1200, 900, double.PositiveInfinity)]
    public void AnInvalidRenderedViewCannotSupplyInputCoordinates(
        double scale, int pixelWidth, int pixelHeight, double width)
    {
        var view = new GatewayNativeWindowGeometry(100, 50, width, 200, scale, pixelWidth, pixelHeight);

        Assert.That(view.ResolvePoint(300, 180).Error, Is.EqualTo("invalid_window_geometry"));
    }

    [TestCase(-1, 20)]
    [TestCase(20, -1)]
    [TestCase(800, 20)]
    [TestCase(20, 600)]
    public void AClippedDialogOnlyAcceptsItsActuallyRenderedPortion(double x, double y)
    {
        var view = new GatewayNativeWindowGeometry(-100, -100, 2000, 2000, 1, 800, 600);

        Assert.That(view.ResolvePoint(20, 20).Error, Is.Null);
        Assert.That(view.ResolvePoint(x, y).Error, Is.EqualTo("point_outside_viewport"));
    }

    private sealed record DisplayIdentity(int Index, string RuntimeType);
}
