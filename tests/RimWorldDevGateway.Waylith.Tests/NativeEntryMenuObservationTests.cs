using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeEntryMenuObservationTests
{
    [Test]
    public void AnActiveMenuObservationWaitsForItsEntireLayoutToComplete()
    {
        var inventory = new GatewayNativeEntryMenuInventory();
        var input = new object(); var dispatcher = new object(); var host = new object();
        var observer = new GatewayNativeEntryMenuObservation(inventory, 1, input, dispatcher, host, false, 2, 2);
        var control = Snapshot(dispatcher, host);
        observer.Observe(control); observer.MenuReturned(true);
        observer.BeginPendingWindows(); observer.ClearPendingWindows(); observer.EndPendingWindows(true);
        Assert.That(inventory.Find("options"), Is.Null, "A nested menu postfix cannot publish the still-running outer layout.");
        observer.Complete(input, dispatcher, host, false, 2, 2, true, true);
        Assert.That(inventory.Find("options"), Is.SameAs(control));
    }

    [TestCase("input")]
    [TestCase("dispatcher")]
    [TestCase("host")]
    [TestCase("drawing")]
    [TestCase("type")]
    [TestCase("raw")]
    [TestCase("original")]
    [TestCase("authority")]
    public void LateLayoutProvenanceChangesRevokeTheObservedMenu(string changed)
    {
        var inventory = new GatewayNativeEntryMenuInventory();
        var input = new object(); var dispatcher = new object(); var host = new object();
        var observer = new GatewayNativeEntryMenuObservation(inventory, 1, input, dispatcher, host, false, 2, 2);
        observer.Observe(Snapshot(dispatcher, host)); observer.MenuReturned(true);
        observer.BeginPendingWindows(); observer.ClearPendingWindows(); observer.EndPendingWindows(true);
        observer.Complete(changed == "input" ? new() : input, changed == "dispatcher" ? new() : dispatcher,
            changed == "host" ? new() : host, changed == "drawing", changed == "type" ? 1 : 2,
            changed == "raw" ? 1 : 2, changed != "original", changed != "authority");
        Assert.That(inventory.Find("options"), Is.Null);
    }

    [TestCase("late-menu-failure")]
    [TestCase("swallowed-layout-failure")]
    [TestCase("missing-pending-phase")]
    [TestCase("pending-phase-failure")]
    [TestCase("reentered-pending-phase")]
    public void AFailedLayoutCannotPublishEvenWhenItsOuterDispatcherReturnsNormally(string failure)
    {
        var inventory = new GatewayNativeEntryMenuInventory();
        var input = new object(); var dispatcher = new object(); var host = new object();
        var observer = new GatewayNativeEntryMenuObservation(inventory, 1, input, dispatcher, host, false, 2, 2);
        observer.Observe(Snapshot(dispatcher, host)); observer.MenuReturned(true);
        if (failure == "late-menu-failure") observer.Fail();
        if (failure == "swallowed-layout-failure") observer.ClearPendingWindows();
        if (failure != "missing-pending-phase")
        {
            observer.BeginPendingWindows();
            if (failure == "reentered-pending-phase") observer.BeginPendingWindows();
            observer.ClearPendingWindows(); // Native pending-window processing clears normally in its finally.
            observer.EndPendingWindows(failure != "pending-phase-failure");
        }
        observer.Complete(input, dispatcher, host, false, 2, 2, true, true);
        Assert.That(inventory.Find("options"), Is.Null);
    }

    private static GatewayNativeEntryMenuSnapshot Snapshot(object dispatcher, object host) => new(new(), new(), new(), dispatcher,
        host, new(), true, 1600, 900, 1, "options", "Options", (Action)(() => { }), 300, 400, 170, 45, true, 1);
}
