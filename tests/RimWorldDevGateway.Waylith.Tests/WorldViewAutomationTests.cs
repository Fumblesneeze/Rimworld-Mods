using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class WorldViewAutomationTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void ForeignWorldActivityPreventsTheClosureThatWouldCancelIt(int activity)
    {
        bool foreignStateIntact = true;
        bool closed = GatewayWorldViewRestorePolicy.TryClose(activity == 0, activity == 1, activity == 2, activity == 3,
            () => foreignStateIntact = false);
        Assert.Multiple(() => { Assert.That(closed, Is.False); Assert.That(foreignStateIntact, Is.True); });
        Assert.That(GatewayWorldViewRestorePolicy.TryClose(false, false, false, false, () => foreignStateIntact = false), Is.True);
        Assert.That(foreignStateIntact, Is.False);
    }

    [Test]
    public void NormalWorldHistoryBeyond1024DrainsInBoundedAdvancesWithoutReplacingTheList()
    {
        System.Collections.IList history = new List<int>(Enumerable.Range(0, 8193));
        int calls = 0;
        do
        {
            int before = history.Count;
            bool complete = GatewayWorldViewRestorePolicy.DrainHistory(history);
            Assert.That(before - history.Count, Is.InRange(1, 256));
            Assert.That(complete, Is.EqualTo(history.Count == 0));
            calls++;
        } while (history.Count > 0);
        Assert.That(calls, Is.GreaterThan(32));
        Assert.That(GatewayWorldViewRestorePolicy.DrainHistory(history), Is.True);
    }

    [Test]
    public void ExactWorldAndCaravanContractsAreAvailableWithoutInitializingAGame()
    {
        Assert.That(new VerseGatewayWorldView(() => true, () => 1).Availability().Available, Is.True);
        Assert.That(new VerseGatewayCaravanInspection().Availability().Available, Is.True);
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    public void StaleOrBlockedViewsCannotDispatchEvenANoOp(bool current, bool ready)
    {
        var registry = new GatewayAutomationRegistry(); var runtime = new Runtime();
        _ = new GatewayWorldViewAutomation(registry, runtime);
        string handle = (string)((IReadOnlyDictionary<string, object?>)Run(registry, "native-world.capture").Result!)["viewHandle"]!;
        runtime.Current = current; runtime.Ready = ready;
        var run = Run(registry, "native-world.set-open", ("viewHandle", handle), ("open", false));
        Assert.Multiple(() => { Assert.That(run.State, Is.EqualTo("failed")); Assert.That(runtime.Commands, Is.Zero); });
    }

    [Test]
    public void PartialRestorationHistoryPreservesFailureWithoutAdditionalWrites()
    {
        var registry = new GatewayAutomationRegistry(); var runtime = new Runtime { RestoreFails = true };
        _ = new GatewayWorldViewAutomation(registry, runtime);
        string handle = (string)((IReadOnlyDictionary<string, object?>)Run(registry, "native-world.capture").Result!)["viewHandle"]!;
        var first = Run(registry, "native-world.restore", ("viewHandle", handle)); registry.Advance();
        var repeat = Run(registry, "native-world.restore", ("viewHandle", handle)); registry.Advance();
        Assert.Multiple(() =>
        {
            Assert.That(first.State, Is.EqualTo("failed")); Assert.That(repeat.State, Is.EqualTo("failed"));
            Assert.That(repeat.Error?.Code, Is.EqualTo("native_world_context_changed"));
            Assert.That(runtime.Restores, Is.EqualTo(1));
        });
    }
    [Test]
    public void CaptureRetainsOneOwnedViewAndRejectsReplacementUntilRestored()
    {
        var registry = new GatewayAutomationRegistry();
        var runtime = new Runtime();
        _ = new GatewayWorldViewAutomation(registry, runtime);
        var first = Run(registry, "native-world.capture");
        var second = Run(registry, "native-world.capture");
        Assert.Multiple(() =>
        {
            Assert.That(first.State, Is.EqualTo("succeeded"));
            Assert.That(((IReadOnlyDictionary<string, object?>)first.Result!)["viewHandle"], Is.Not.Empty);
            Assert.That(first.Artifacts.Any(a => a.Name == "native-world-lease"), Is.True);
            Assert.That(second.Error?.Code, Is.EqualTo("native_world_view_busy"));
            Assert.That(runtime.Captures, Is.EqualTo(1));
        });
    }

    [TestCase(false, true, 1)]
    [TestCase(true, true, 0)]
    [TestCase(true, false, 1)]
    public void DesiredWorldStateUsesOneNativeCommandOrNoOp(bool initial, bool desired, int calls)
    {
        var registry = new GatewayAutomationRegistry();
        var runtime = new Runtime { Open = initial };
        _ = new GatewayWorldViewAutomation(registry, runtime);
        var capture = Run(registry, "native-world.capture");
        string handle = (string)((IReadOnlyDictionary<string, object?>)capture.Result!)["viewHandle"]!;
        var run = Run(registry, "native-world.set-open", ("viewHandle", handle), ("open", desired));
        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo("succeeded"));
            Assert.That(runtime.Open, Is.EqualTo(desired));
            Assert.That(runtime.Commands, Is.EqualTo(calls));
            Assert.That(run.Artifacts.Select(a => a.Name), Does.Contain("native-world-before").And.Contain("native-world-after"));
        });
    }

    private static GatewayAutomationRun Run(GatewayAutomationRegistry registry, string name,
        params (string Key, object? Value)[] values)
    {
        var args = values.ToDictionary(v => v.Key, v => v.Value);
        args["version"] = 1;
        return registry.StartRun(name, "world-test-" + Guid.NewGuid().ToString("N"), args);
    }

    [Test]
    public void RestoreSurvivesCancellationAndRetiresTheLeaseOnlyAfterLaterVerification()
    {
        var registry = new GatewayAutomationRegistry();
        var runtime = new Runtime { RestoreFrames = 2 };
        _ = new GatewayWorldViewAutomation(registry, runtime);
        string handle = (string)((IReadOnlyDictionary<string, object?>)Run(registry, "native-world.capture").Result!)["viewHandle"]!;
        var restore = Run(registry, "native-world.restore", ("viewHandle", handle));
        restore.RequestCancellation();
        registry.Advance();
        Assert.That(restore.IsTerminal, Is.False);
        var command = Run(registry, "native-world.set-open", ("viewHandle", handle), ("open", true));
        Assert.That(command.Error?.Code, Is.EqualTo("stale_world_view_handle"));
        registry.Advance();
        Assert.That(((GatewayWorldRestoreState)restore.Result!).Restored, Is.True);
        int writes = runtime.Restores;
        var repeat = Run(registry, "native-world.restore", ("viewHandle", handle));
        Assert.Multiple(() =>
        {
            Assert.That(repeat.State, Is.EqualTo("succeeded"));
            Assert.That(runtime.Restores, Is.EqualTo(writes));
            Assert.That(Run(registry, "native-world.capture").State, Is.EqualTo("succeeded"));
        });
    }

    private sealed class Runtime : IGatewayWorldViewRuntime, IGatewayWorldViewSession
    {
        public int Captures;
        public bool Open;
        public int Commands;
        public int RestoreFrames = 1;
        public int Restores;
        public bool Current = true, Ready = true, RestoreFails;
        public GatewayAutomationAvailability Availability() => GatewayAutomationAvailability.AvailableNow;
        public IGatewayWorldViewSession Capture() { Captures++; return this; }
        public GatewayWorldViewState Observe() => new(Open, null, Current, Ready);
        public void Activate(Action beforeNativeCommand) { beforeNativeCommand(); Commands++; Open = !Open; }
        public GatewayWorldRestoreState Restore() => ++Restores >= RestoreFrames
            ? new(true, !RestoreFails, "verified", RestoreFails ? "native_world_context_changed" : null) : new(false, false, "waiting-native-frame");
    }
}
