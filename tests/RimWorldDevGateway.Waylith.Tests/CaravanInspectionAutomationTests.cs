using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class CaravanInspectionAutomationTests
{
    [TestCase("items", false, "health", 0)]
    [TestCase("health", true, "health", 0)]
    [TestCase("items", true, "health", 1)]
    public void NativeTabsPreserveRetainedDetailsAndNeverCloseAnUnrelatedTab(string tab, bool open, string initial, int commands)
    {
        var runtime = new Runtime { Tab = initial };
        var registry = new GatewayAutomationRegistry();
        var worlds = new GatewayWorldViewAutomation(registry, runtime);
        _ = new GatewayCaravanInspectionAutomation(registry, worlds, runtime);
        string view = Handle(Run(registry, "native-world.capture"), "viewHandle");
        string caravan = Handle(Run(registry, "native-caravan.capture", ("viewHandle", view), ("worldObjectLoadId", "Caravan_1")), "caravanHandle");
        var result = Run(registry, "native-caravan.tab", ("caravanHandle", caravan), ("tab", tab), ("open", open));
        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo("succeeded"));
            Assert.That(runtime.Commands, Is.EqualTo(commands));
            Assert.That(runtime.Tab, Is.EqualTo(commands == 0 ? initial : tab));
            Assert.That(((GatewayCaravanState)result.Result!).DetailPawnLoadId, Is.EqualTo("Pawn_1"));
        });
    }

    private static string Handle(GatewayAutomationRun run, string key) => (string)((IReadOnlyDictionary<string, object?>)run.Result!)[key]!;
    private static GatewayAutomationRun Run(GatewayAutomationRegistry registry, string name, params (string, object?)[] values)
    {
        var args = values.ToDictionary(v => v.Item1, v => v.Item2); args["version"] = 1;
        return registry.StartRun(name, Guid.NewGuid().ToString("N"), args);
    }
    private sealed class Runtime : IGatewayWorldViewRuntime, IGatewayWorldViewSession, IGatewayCaravanInspectionRuntime, IGatewayCaravanInspection
    {
        public string? Tab;
        public int Commands;
        public GatewayAutomationAvailability Availability() => GatewayAutomationAvailability.AvailableNow;
        public IGatewayWorldViewSession Capture() => this;
        GatewayWorldViewState IGatewayWorldViewSession.Observe() => new(true, null, true);
        public void Activate(Action admission) => admission();
        public GatewayWorldRestoreState Restore() => new(true, true, "verified");
        public IGatewayCaravanInspection Capture(IGatewayWorldViewSession view, string loadId) => this;
        public GatewayCaravanState Observe() => new("Caravan_1", "A", new[] { "Pawn_1" }, true, true, true, Tab, "Pawn_1");
        public void Select(Action admission) => admission();
        public void ToggleTab(string tab, Action admission) { admission(); Commands++; Tab = Tab == tab ? null : tab; }
    }
}
