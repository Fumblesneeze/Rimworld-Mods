using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeEntryMenuLeaseTests
{
    [Test]
    public void ANewEntryRootCannotReceiveAnOlderCapturedControl()
    {
        var current = Snapshot();
        var lease = new GatewayNativeEntryMenuLease(current);
        Assert.That(lease.Validate(current, 1), Is.Null);
        Assert.That(lease.Validate(current with { Root = new object() }, 1), Is.EqualTo("stale_entry_context"));
    }

    [TestCase("ui-root")]
    [TestCase("stack")]
    [TestCase("dispatcher")]
    [TestCase("host")]
    [TestCase("language")]
    [TestCase("developer-mode")]
    public void ChangedNativeContextRefusesBeforeDelivery(string change)
    {
        var captured = Snapshot();
        var changed = change switch
        {
            "ui-root" => captured with { UiRoot = new object() },
            "stack" => captured with { Stack = new object() },
            "dispatcher" => captured with { Dispatcher = new object() },
            "host" => captured with { Host = new object() },
            "language" => captured with { Language = new object() },
            _ => captured with { DevMode = false }
        };
        Assert.That(new GatewayNativeEntryMenuLease(captured).Validate(changed, 1), Is.EqualTo("stale_entry_context"));
    }

    [Test]
    public void EveryMousePassRequiresItsNewCompletedLayout()
    {
        var captured = Snapshot();
        var lease = new GatewayNativeEntryMenuLease(captured);
        Assert.That(lease.Validate(captured, 2), Is.EqualTo("native_entry_layout_unobserved"));
        Assert.That(lease.Validate(captured with { LayoutVersion = 2 }, 2), Is.Null);
        Assert.That(lease.Validate(captured with { LayoutVersion = 2 }, 3), Is.EqualTo("native_entry_layout_unobserved"));
    }

    [TestCase("x")]
    [TestCase("y")]
    [TestCase("width")]
    [TestCase("height")]
    [TestCase("scale")]
    [TestCase("pixel-width")]
    [TestCase("pixel-height")]
    public void ChangedRenderedGeometryCannotRedirectInput(string change)
    {
        var captured = Snapshot();
        var changed = change switch
        {
            "x" => captured with { X = captured.X + 1 },
            "y" => captured with { Y = captured.Y + 1 },
            "width" => captured with { Width = captured.Width + 1 },
            "height" => captured with { Height = captured.Height + 1 },
            "scale" => captured with { UiScale = 1.25 },
            "pixel-width" => captured with { PixelWidth = 1700 },
            _ => captured with { PixelHeight = 1000 }
        };
        Assert.That(new GatewayNativeEntryMenuLease(captured).Validate(changed, 1), Is.EqualTo("stale_entry_geometry"));
    }

    [TestCase("label")]
    [TestCase("control")]
    [TestCase("callback")]
    [TestCase("disabled")]
    public void AChangedOrDisabledNativeControlCannotReceiveTheGesture(string change)
    {
        var captured = Snapshot();
        var changed = change switch
        {
            "label" => captured with { Label = "Other" },
            "control" => captured with { Control = "dev-quicktest" },
            "callback" => captured with { Callback = (Action)OtherCallback },
            _ => captured with { Enabled = false }
        };
        Assert.That(new GatewayNativeEntryMenuLease(captured).Validate(changed, 1), Is.EqualTo("stale_entry_control"));
    }

    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(-2)]
    [TestCase(0)]
    public void InvalidCapturedGeometryIsNotAnInputLease(double width)
    {
        var captured = Snapshot() with { Width = width };
        Assert.That(new GatewayNativeEntryMenuLease(captured).Validate(captured, 1), Is.EqualTo("invalid_entry_geometry"));
    }

    private static void OtherCallback() { }

    [Test]
    public void OnlyACompletedUnambiguousCurrentLayoutCanSupplyControls()
    {
        var inventory = new GatewayNativeEntryMenuInventory();
        var control = Snapshot();
        inventory.Begin(1); inventory.Observe(control);
        Assert.That(inventory.Find("options"), Is.Null);
        inventory.Complete(1, true);
        Assert.That(inventory.Find("options"), Is.SameAs(control));
        inventory.Begin(2);
        Assert.That(inventory.Find("options"), Is.Null, "Beginning the next layout must revoke the prior observation.");
        inventory.Observe(control with { LayoutVersion = 2 });
        inventory.Observe(control with { LayoutVersion = 2 });
        inventory.Complete(2, true);
        Assert.That(inventory.Find("options"), Is.Null, "Duplicate labels must not select a guessed control.");
        inventory.Begin(3); inventory.Observe(control with { LayoutVersion = 3 });
        inventory.Complete(3, false);
        Assert.That(inventory.Find("options"), Is.Null);
    }

    [Test]
    public void AMismatchedCompletionCannotLaterPublishItsPendingLayout()
    {
        var inventory = new GatewayNativeEntryMenuInventory();
        inventory.Begin(1); inventory.Observe(Snapshot());
        inventory.Complete(0, true);
        inventory.Complete(1, true);
        Assert.That(inventory.Find("options"), Is.Null);
    }

    private static GatewayNativeEntryMenuSnapshot Snapshot() => new(new(), new(), new(), new(), new(), new(),
        true, 1600, 900, 1, "options", "Options", (Action)(() => { }), 300, 400, 170, 45, true, 1);
}
