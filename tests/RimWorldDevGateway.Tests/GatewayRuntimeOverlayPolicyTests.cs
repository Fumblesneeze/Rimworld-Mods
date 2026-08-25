using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayRuntimeOverlayPolicyTests
{
    [TestCase(true, false, false, true, true, true)]
    [TestCase(true, false, false, true, false, false)]
    [TestCase(true, false, false, false, true, false)]
    [TestCase(true, false, true, true, true, false)]
    [TestCase(true, true, false, true, true, false)]
    [TestCase(false, false, false, true, true, false)]
    public void Gateway_overlay_is_visible_only_while_the_native_escape_menu_is_open(
        bool runtimeRunning,
        bool stopping,
        bool screenshotMode,
        bool inPlayUi,
        bool probedEscapeMenuOpen,
        bool expected)
    {
        Assert.That(
            GatewayRuntimeOverlayPolicy.ShouldDraw(
                runtimeRunning,
                stopping,
                screenshotMode,
                inPlayUi,
                () => probedEscapeMenuOpen),
            Is.EqualTo(expected));
    }

    [Test]
    public void Gateway_overlay_does_not_read_the_play_only_menu_seam_at_the_title_screen()
    {
        var probeCalls = 0;

        var result = GatewayRuntimeOverlayPolicy.ShouldDraw(
            runtimeRunning: true,
            stopping: false,
            screenshotMode: false,
            inPlayUi: false,
            escapeMenuOpen: () =>
            {
                probeCalls++;
                throw new InvalidOperationException("The play-only seam must not be read.");
            });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(probeCalls, Is.Zero);
        });
    }
}
