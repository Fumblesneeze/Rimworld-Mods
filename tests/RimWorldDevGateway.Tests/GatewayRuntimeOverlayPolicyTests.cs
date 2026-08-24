using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayRuntimeOverlayPolicyTests
{
    [TestCase(true, false, false, true)]
    [TestCase(true, false, true, false)]
    [TestCase(true, true, false, false)]
    [TestCase(false, false, false, false)]
    public void Gateway_overlay_is_suppressed_by_native_screenshot_mode_without_stopping_the_runtime(
        bool runtimeRunning,
        bool stopping,
        bool screenshotMode,
        bool expected)
    {
        Assert.That(
            GatewayRuntimeOverlayPolicy.ShouldDraw(runtimeRunning, stopping, screenshotMode),
            Is.EqualTo(expected));
    }
}
