using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class MainTabAutomationTests
{
    [Test]
    public void InspectedNativeMetadataCanRegisterWithoutInitializingGameState()
    {
        var registry = new GatewayAutomationRegistry();
        VerseGatewayMainTab.Register(registry);
        var descriptor = registry.Describe().Single();
        Assert.That(descriptor.Name, Is.EqualTo(GatewayMainTabAutomation.Name));
        Assert.That(descriptor.Availability.Available, Is.True, descriptor.Availability.Reason);
    }

    [Test]
    public void OpensThroughTheNativeCommandAndRetainsBeforeInvocationAndAfter()
    {
        var runtime = new Runtime();
        var registry = Registry(runtime);

        var run = Invoke(registry, true);

        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo("succeeded"));
            Assert.That(runtime.Activations, Is.EqualTo(1));
            Assert.That(runtime.OpenTab, Is.EqualTo("Assign"));
            Assert.That(run.Artifacts.Select(item => item.Name), Is.EqualTo(new[]
                { "native-main-tab-before", "native-main-tab-invocation", "native-main-tab-after" }));
            Assert.That(((IReadOnlyDictionary<string, object?>)run.Result!)["open"], Is.True);
        });
    }

    [TestCase("not-ready")]
    [TestCase("missing")]
    [TestCase("ambiguous")]
    [TestCase("unsupported")]
    [TestCase("hidden")]
    [TestCase("disabled")]
    [TestCase("modal")]
    [TestCase("no-context")]
    public void UnavailableContextIsRefusedEvenIfTheRequestedStateAlreadyMatches(string reason)
    {
        foreach (bool alreadyOpen in new[] { false, true })
        {
            var runtime = new Runtime { OpenTab = alreadyOpen ? "Assign" : "Work" };
            runtime.Transform = state => reason switch
            {
                "not-ready" => state with { Ready = false },
                "missing" => state with { MatchCount = 0 },
                "ambiguous" => state with { MatchCount = 2 },
                "unsupported" => state with { Supported = false },
                "hidden" => state with { Visible = false },
                "disabled" => state with { Disabled = true },
                "modal" => state with { ReceivesInput = false },
                _ => state with { Context = null }
            };
            var run = Invoke(Registry(runtime), true);
            Assert.Multiple(() =>
            {
                Assert.That(run.State, Is.EqualTo("failed"), reason);
                Assert.That(runtime.Activations, Is.Zero);
                Assert.That(runtime.OpenTab, Is.EqualTo(alreadyOpen ? "Assign" : "Work"));
                Assert.That(run.Artifacts.Any(item => item.Name == "native-main-tab-invocation"), Is.False);
                Assert.That(run.Artifacts.Any(item => item.Name == "native-main-tab-before"), Is.True);
            });
        }
    }

    [TestCase(true, "Assign", 0, "Assign")]
    [TestCase(false, "Work", 0, "Work")]
    [TestCase(false, "Assign", 1, null)]
    public void MatchingRequestsDoNotToggleAndCloseNeverTouchesAnotherTab(bool desired,
        string initial, int activations, string? expected)
    {
        var runtime = new Runtime { OpenTab = initial };
        var run = Invoke(Registry(runtime), desired);
        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo("succeeded"));
            Assert.That(runtime.Activations, Is.EqualTo(activations));
            Assert.That(runtime.OpenTab, Is.EqualTo(expected));
        });
    }

    [TestCase("none")]
    [TestCase("other")]
    [TestCase("context")]
    public void AFailedNativePostconditionCannotReportSuccessOrRetry(string effect)
    {
        var runtime = new Runtime();
        runtime.AfterActivate = () =>
        {
            if (effect == "context") runtime.Identity = new object();
            else runtime.OpenTab = effect == "none" ? null : "Work";
        };
        var run = Invoke(Registry(runtime), true);
        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo("failed"));
            Assert.That(runtime.Activations, Is.EqualTo(1));
            Assert.That(run.Artifacts.Any(item => item.Name == "native-main-tab-after"), Is.True);
        });
    }

    [TestCase("version")]
    [TestCase("missing")]
    [TestCase("type")]
    [TestCase("empty")]
    [TestCase("extra")]
    public void InvalidRequestsAreRejectedBeforeRuntimeReads(string invalid)
    {
        var runtime = new Runtime();
        var args = new Dictionary<string, object?> { ["version"] = 1, ["defName"] = "Assign", ["open"] = true };
        if (invalid == "version") args["version"] = 2;
        if (invalid == "missing") args.Remove("open");
        if (invalid == "type") args["open"] = "true";
        if (invalid == "empty") args["defName"] = " ";
        if (invalid == "extra") args["unknown"] = 1;
        var run = Registry(runtime).StartRun(GatewayMainTabAutomation.Name, "invalid-tab", args);
        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo("failed"));
            Assert.That(run.Error?.Code, Is.EqualTo("invalid_native_main_tab_arguments"));
            Assert.That(runtime.Reads, Is.Zero);
            Assert.That(runtime.Activations, Is.Zero);
        });
    }

    [Test]
    public void CancellationAtTheFinalDispatchGatePreventsTheNativeCommand()
    {
        using var cancellation = new CancellationTokenSource();
        var runtime = new Runtime { BeforeNativeGate = cancellation.Cancel };
        var run = Registry(runtime).StartRun(GatewayMainTabAutomation.Name, "cancel-before-tab",
            new Dictionary<string, object?> { ["version"] = 1, ["defName"] = "Assign", ["open"] = true },
            cancellationToken: cancellation.Token);
        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo("cancelled"));
            Assert.That(runtime.Activations, Is.Zero);
            Assert.That(runtime.OpenTab, Is.Null);
            Assert.That(run.Artifacts.Any(item => item.Name == "native-main-tab-invocation"), Is.False);
        });
    }

    [Test]
    public void NativeExceptionRetainsAvailableAfterObservationWithoutRetry()
    {
        var runtime = new Runtime { AfterActivate = () => throw new InvalidOperationException("native-after-change") };
        var run = Invoke(Registry(runtime), true);
        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo("failed"));
            Assert.That(run.Error!.Message, Does.Contain("native-after-change"));
            Assert.That(runtime.Activations, Is.EqualTo(1));
            Assert.That(runtime.OpenTab, Is.EqualTo("Assign"));
            Assert.That(run.Artifacts.Any(item => item.Name == "native-main-tab-after"), Is.True);
        });
    }

    [Test]
    public void CancellationAfterTheCommandRetainsTheActualChangedState()
    {
        using var cancellation = new CancellationTokenSource();
        var runtime = new Runtime { AfterActivate = cancellation.Cancel };
        var run = Registry(runtime).StartRun(GatewayMainTabAutomation.Name, "cancel-after-tab",
            new Dictionary<string, object?> { ["version"] = 1, ["defName"] = "Assign", ["open"] = true },
            cancellationToken: cancellation.Token);
        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo("cancelled"));
            Assert.That(runtime.Activations, Is.EqualTo(1));
            Assert.That(((GatewayMainTabState)run.Artifacts.Single(item => item.Name == "native-main-tab-after").Value!).IsOpen, Is.True);
        });
    }

    [Test]
    public void SerializedObservationDoesNotExposeTheLiveInvocationContext()
    {
        var serialized = System.Text.Json.JsonSerializer.Serialize(new Runtime().Read("Assign"));
        Assert.That(serialized, Does.Contain("\"DefName\":\"Assign\"").And.Not.Contain("Context"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RetainedObservationsDoNotKeepTheLiveGameContext(bool nativeFailure)
    {
        var runtime = new Runtime();
        if (nativeFailure) runtime.AfterActivate = () => throw new InvalidOperationException("native-failure");
        var run = Invoke(Registry(runtime), true);
        var observations = run.Artifacts.Select(item => item.Value).OfType<GatewayMainTabState>().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo(nativeFailure ? "failed" : "succeeded"));
            Assert.That(observations, Has.Length.EqualTo(2));
            Assert.That(observations.All(state => state.Context == null), Is.True,
                "Retained evidence must not keep the live Game, UI or Map object graph alive.");
        });
    }

    private static GatewayAutomationRegistry Registry(Runtime runtime)
    {
        var registry = new GatewayAutomationRegistry();
        GatewayMainTabAutomation.Register(registry, runtime, () => GatewayAutomationAvailability.AvailableNow);
        return registry;
    }

    private static GatewayAutomationRun Invoke(GatewayAutomationRegistry registry, bool open) =>
        registry.StartRun(GatewayMainTabAutomation.Name, "main-tab-test",
            new Dictionary<string, object?> { ["version"] = 1, ["defName"] = "Assign", ["open"] = open });

    private sealed class Runtime : IGatewayMainTabRuntime
    {
        public object Identity = new();
        public string? OpenTab;
        public int Activations;
        public int Reads;
        public Func<GatewayMainTabState, GatewayMainTabState>? Transform;
        public Action? AfterActivate;
        public Action? BeforeNativeGate;
        public GatewayMainTabState Read(string name)
        {
            Reads++;
            var state = new GatewayMainTabState(name, true, 1, true, true, false, true, OpenTab == name, OpenTab)
            {
                Context = new(Identity, Identity, Identity, Identity, Identity, Identity, Identity, Identity, Identity)
            };
            return Transform?.Invoke(state) ?? state;
        }
        public void Activate(GatewayMainTabState expected, Action beforeNativeInvoke)
        {
            BeforeNativeGate?.Invoke();
            beforeNativeInvoke();
            Activations++;
            OpenTab = OpenTab == expected.DefName ? null : expected.DefName;
            AfterActivate?.Invoke();
        }
    }
}
