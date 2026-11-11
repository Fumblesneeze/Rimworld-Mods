using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeTextPassGateTests
{
    [TestCase("identity")]
    [TestCase("drawing")]
    [TestCase("type")]
    [TestCase("raw")]
    [TestCase("authority")]
    [TestCase("original")]
    public void AlteredOrUnprovedLayoutCannotAuthorizeTheNextKey(string changed)
    {
        var gate = new GatewayNativeTextPassGate(); var layout = new object(); var key = new object();
        gate.Capture(layout, false, true, false, 0, 0, 0, '\0', 0);
        gate.Before(changed == "identity" ? new object() : layout, changed == "drawing", false,
            changed == "type" ? 2 : 0, changed == "raw" ? 2 : 0, 0, '\0', 0,
            changed != "original", changed != "authority", () => throw new AssertionException("Layout is not a key."));
        gate.After(true);
        gate.Capture(key, false, false, true, 1, 1, 97, 'a', 2);
        Assert.That(gate.Before(key, false, true, 1, 1, 97, 'a', 2, true, true, () => null), Is.False);
        Assert.That(gate.After(true), Is.False);
    }

    [TestCase("identity")]
    [TestCase("drawing")]
    [TestCase("type")]
    [TestCase("raw")]
    [TestCase("key")]
    [TestCase("character")]
    [TestCase("modifiers")]
    [TestCase("missing-capture")]
    [TestCase("missing-layout")]
    [TestCase("skipped-layout")]
    [TestCase("authority")]
    [TestCase("original")]
    public void ChangedOrUnprovedKeyPassNeverReachesTheField(string changed)
    {
        var gate = new GatewayNativeTextPassGate(); var input = new object();
        if (changed != "missing-layout")
        { Layout(gate, changed != "skipped-layout"); }
        if (changed != "missing-capture") gate.Capture(input, false, false, true, 1, 1, 97, 'a', 2);
        int validation = 0;
        bool allowed = gate.Before(changed == "identity" ? new object() : input, changed == "drawing", true,
            changed == "type" ? 2 : 1, changed == "raw" ? 2 : 1, changed == "key" ? 98 : 97,
            changed == "character" ? 'b' : 'a', changed == "modifiers" ? 0 : 2,
            changed != "original", changed != "authority", () => { validation++; return null; });
        Assert.That(allowed, Is.False); Assert.That(gate.After(true), Is.False); Assert.That(validation, Is.Zero);
    }

    [TestCase(null, true)]
    [TestCase("stale_native_text_focus", true)]
    [TestCase(null, false)]
    public void DeliveryRequiresPostLayoutFieldValidationAndActualOriginal(string? rejection, bool original)
    {
        var gate = new GatewayNativeTextPassGate(); var input = new object();
        Layout(gate, true);
        gate.Capture(input, false, false, true, 1, 1, 0, '1', 0);
        Assert.That(gate.Before(input, false, true, 1, 1, 0, '1', 0, true, true, () => rejection), Is.EqualTo(rejection == null));
        Assert.That(gate.After(original), Is.EqualTo(rejection == null && original));
        Assert.That(gate.After(true), Is.False, "A repeated postfix cannot invent another delivery.");
    }

    private static void Layout(GatewayNativeTextPassGate gate, bool original)
    {
        var identity = new object(); gate.Capture(identity, false, true, false, 0, 0, 0, '\0', 0);
        gate.Before(identity, false, false, 0, 0, 0, '\0', 0, original, true, () => throw new AssertionException("Layout is not a key."));
        gate.After(original);
    }
}
