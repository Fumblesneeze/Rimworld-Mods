using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeTextControlLeaseTests
{
    [TestCase("same", true)]
    [TestCase("editor", false)]
    [TestCase("control", false)]
    [TestCase("window", false)]
    [TestCase("readonly", false)]
    [TestCase("multiline", false)]
    [TestCase("x", false)]
    [TestCase("y", false)]
    [TestCase("width", true)]
    [TestCase("width-too-far", false)]
    [TestCase("height", false)]
    [TestCase("nonfinite", false)]
    public void OnlyTheExactCurrentWritableNativeEditorCanReceiveText(string change, bool accepted)
    {
        var editor = new object();
        var lease = new GatewayNativeTextControlLease(editor, -12, 0, 10, 20, 200, 30);
        var error = lease.Validate(change == "editor" ? new object() : editor,
            change == "control" ? 41 : -12, change == "window" ? 2 : 0,
            change != "readonly", change == "multiline", change == "x" ? 11 : 10,
            change == "y" ? 21 : 20, change == "width-too-far" ? 253 : change == "width" ? 201 : 200,
            change == "nonfinite" ? double.NaN : change == "height" ? 31 : 30);
        Assert.That(error == null, Is.EqualTo(accepted));
    }

    [TestCase(411, 385, 359)]
    [TestCase(359, 385, 411)]
    public void NativeQuickSearchCanResizeForItsClearButtonAndResultsScrollbar(double captured, double intermediate, double active)
    {
        var editor = new object();
        var lease = new GatewayNativeTextControlLease(editor, -12, 0, 10, 20, captured, 30);
        Assert.That(lease.Validate(editor, -12, 0, true, false, 10, 20, intermediate, 30), Is.Null);
        Assert.That(lease.Validate(editor, -12, 0, true, false, 10, 20, active, 30), Is.Null);
        // Admission never rebinds the original width, even after an accepted resize.
        double drifted = active + (active > captured ? 26 : -26);
        Assert.That(lease.Validate(editor, -12, 0, true, false, 10, 20, drifted, 30), Is.EqualTo("stale_native_text_focus"));
        Assert.That(lease.Validate(editor, -12, 0, true, false, 10, 20, captured, 30), Is.Null);
    }

    [TestCase("editor")]
    [TestCase("control")]
    [TestCase("window")]
    [TestCase("x")]
    [TestCase("y")]
    [TestCase("height")]
    public void AValidWidthChangeCannotAuthorizeAReplacedOrMovedField(string change)
    {
        var editor = new object();
        var lease = new GatewayNativeTextControlLease(editor, -12, 0, 10, 20, 200, 30);
        Assert.That(lease.Validate(change == "editor" ? new object() : editor,
            change == "control" ? 41 : -12, change == "window" ? 2 : 0, true, false,
            change == "x" ? 11 : 10, change == "y" ? 21 : 20, 148, change == "height" ? 31 : 30),
            Is.EqualTo("stale_native_text_focus"));
    }

    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(147.99)]
    [TestCase(252.01)]
    public void InvalidOrOutOfBoundWidthsRemainStale(double width)
    {
        var editor = new object();
        var lease = new GatewayNativeTextControlLease(editor, -12, 0, 10, 20, 200, 30);
        Assert.That(lease.Validate(editor, -12, 0, true, false, 10, 20, width, 30), Is.EqualTo("stale_native_text_focus"));
    }
}
