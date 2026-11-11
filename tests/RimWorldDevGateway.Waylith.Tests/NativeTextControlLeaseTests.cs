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
    [TestCase("width", false)]
    [TestCase("height", false)]
    [TestCase("nonfinite", false)]
    public void OnlyTheExactCurrentWritableNativeEditorCanReceiveText(string change, bool accepted)
    {
        var editor = new object();
        var lease = new GatewayNativeTextControlLease(editor, -12, 0, 10, 20, 200, 30);
        var error = lease.Validate(change == "editor" ? new object() : editor,
            change == "control" ? 41 : -12, change == "window" ? 2 : 0,
            change != "readonly", change == "multiline", change == "x" ? 11 : 10,
            change == "y" ? 21 : 20, change == "width" ? 201 : 200,
            change == "nonfinite" ? double.NaN : change == "height" ? 31 : 30);
        Assert.That(error == null, Is.EqualTo(accepted));
    }
}
