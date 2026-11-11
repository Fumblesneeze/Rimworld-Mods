using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class DeveloperLogAutomationTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void InterruptedCompletionRetainsBeforeInvocationAndObservedAfter(bool postconditionFailure)
    {
        using var cancellation = new CancellationTokenSource();
        var registry = new GatewayAutomationRegistry();
        int windows = 0;
        GatewayDeveloperLogAutomation.Register(registry, () => new(true, true, windows),
            () => { windows = postconditionFailure ? 2 : 1; if (!postconditionFailure) cancellation.Cancel(); },
            () => GatewayAutomationAvailability.AvailableNow);
        var run = registry.StartRun(GatewayDeveloperLogAutomation.Name, "interrupted-log",
            new Dictionary<string, object?> { ["version"] = 1, ["open"] = true }, cancellationToken: cancellation.Token);
        Assert.That(run.State, Is.EqualTo(postconditionFailure ? "failed" : "cancelled"));
        Assert.That(run.Result, Is.Null);
        Assert.That(((GatewayDeveloperLogState)run.Artifacts.Single(item => item.Name == "native-log-before").Value!).OpenWindows, Is.Zero);
        Assert.That(run.Artifacts.Any(item => item.Name == "native-log-invocation"), Is.True);
        Assert.That(((GatewayDeveloperLogState)run.Artifacts.Single(item => item.Name == "native-log-after").Value!).OpenWindows,
            Is.EqualTo(postconditionFailure ? 2 : 1));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ALogSubclassCannotMasqueradeAsTheExactNativeCommandWindow(bool desired)
    {
        var registry = new GatewayAutomationRegistry();
        int toggles = 0;
        GatewayDeveloperLogAutomation.Register(registry,
            () => GatewayDeveloperLogState.FromOpenWindowTypes(true, true, typeof(LogWindow), new[] { typeof(DerivedLogWindow) }),
            () => toggles++, () => GatewayAutomationAvailability.AvailableNow);
        var run = registry.StartRun(GatewayDeveloperLogAutomation.Name, "derived-log",
            new Dictionary<string, object?> { ["version"] = 1, ["open"] = desired });
        Assert.That(run.State, Is.EqualTo("failed"));
        Assert.That(toggles, Is.Zero);
    }

    private class LogWindow { }
    private sealed class DerivedLogWindow : LogWindow { }

    [Test]
    public void InspectedNativeCommandMetadataIsAvailableWithoutInitializingAGame()
    {
        var registry = new GatewayAutomationRegistry();
        VerseGatewayDeveloperLog.Register(registry);
        var entry = registry.Describe().Single();
        Assert.That(entry.Name, Is.EqualTo(GatewayDeveloperLogAutomation.Name));
        Assert.That(entry.Availability.Available, Is.True, entry.Availability.Reason);
    }

    [Test]
    public void CloseUsesTheSameNativeCommandAndReportsTheObservedClosedState()
    {
        var registry = new GatewayAutomationRegistry();
        int windows = 1;
        GatewayDeveloperLogAutomation.Register(registry, () => new(true, true, windows),
            () => windows = 0, () => GatewayAutomationAvailability.AvailableNow);
        var run = registry.StartRun(GatewayDeveloperLogAutomation.Name, "close-log",
            new Dictionary<string, object?> { ["version"] = 1, ["open"] = false });
        Assert.That(run.State, Is.EqualTo("succeeded"));
        Assert.That(((IReadOnlyDictionary<string, object?>)run.Result!)["open"], Is.False);
    }

    [Test]
    public void ACommandThatDoesNotOpenTheLogCannotReportSuccess()
    {
        var registry = new GatewayAutomationRegistry();
        GatewayDeveloperLogAutomation.Register(registry, () => new(true, true, 0),
            () => { }, () => GatewayAutomationAvailability.AvailableNow);
        var run = registry.StartRun(GatewayDeveloperLogAutomation.Name, "unchanged-command",
            new Dictionary<string, object?> { ["version"] = 1, ["open"] = true });
        Assert.That(run.State, Is.EqualTo("failed"));
        Assert.That(run.Error!.Code, Is.EqualTo("native_log_state_not_observed"));
    }

    [TestCase("version")]
    [TestCase("missing")]
    [TestCase("type")]
    [TestCase("extra")]
    public void InvalidRequestsNeverReachTheNativeCommand(string invalid)
    {
        var registry = new GatewayAutomationRegistry();
        int toggles = 0;
        GatewayDeveloperLogAutomation.Register(registry, () => new(true, true, 0),
            () => toggles++, () => GatewayAutomationAvailability.AvailableNow);
        var args = new Dictionary<string, object?> { ["version"] = 1, ["open"] = true };
        if (invalid == "version") args["version"] = 2;
        if (invalid == "missing") args.Remove("open");
        if (invalid == "type") args["open"] = "true";
        if (invalid == "extra") args["extra"] = false;
        var run = registry.StartRun(GatewayDeveloperLogAutomation.Name, "invalid-log", args);
        Assert.That(run.State, Is.EqualTo("failed"));
        Assert.That(run.Error!.Code, Is.EqualTo("invalid_native_log_arguments"));
        Assert.That(toggles, Is.Zero);
    }

    [TestCase(false, true, 0)]
    [TestCase(true, false, 0)]
    [TestCase(true, true, 2)]
    public void UnavailableContextCannotInvokeTheNativeCommand(bool ready, bool developer, int windows)
    {
        var registry = new GatewayAutomationRegistry();
        int toggles = 0;
        GatewayDeveloperLogAutomation.Register(registry, () => new(ready, developer, windows),
            () => toggles++, () => GatewayAutomationAvailability.AvailableNow);
        var run = registry.StartRun(GatewayDeveloperLogAutomation.Name, "unavailable-log",
            new Dictionary<string, object?> { ["version"] = 1, ["open"] = true });
        Assert.That(run.State, Is.EqualTo("failed"));
        Assert.That(toggles, Is.Zero);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void AlreadyMatchingStateIsPreservedWithoutToggling(bool desired)
    {
        var registry = new GatewayAutomationRegistry();
        int toggles = 0;
        GatewayDeveloperLogAutomation.Register(registry, () => new(true, true, desired ? 1 : 0),
            () => toggles++, () => GatewayAutomationAvailability.AvailableNow);
        var run = registry.StartRun(GatewayDeveloperLogAutomation.Name, "unchanged-log",
            new Dictionary<string, object?> { ["version"] = 1, ["open"] = desired });
        Assert.That(run.State, Is.EqualTo("succeeded"));
        Assert.That(toggles, Is.Zero);
        Assert.That(((IReadOnlyDictionary<string, object?>)run.Result!)["changed"], Is.False);
    }

    [Test]
    public void OpensThroughTheNativeCommandOnceAndObservesTheActualResult()
    {
        var registry = new GatewayAutomationRegistry();
        int open = 0;
        int toggles = 0;
        GatewayDeveloperLogAutomation.Register(registry, () => new(true, true, open),
            () => { toggles++; open = 1; }, () => GatewayAutomationAvailability.AvailableNow);

        var run = registry.StartRun(GatewayDeveloperLogAutomation.Name, "open-log",
            new Dictionary<string, object?> { ["version"] = 1, ["open"] = true });

        Assert.That(run.State, Is.EqualTo("succeeded"));
        Assert.That(toggles, Is.EqualTo(1));
        var receipt = (IReadOnlyDictionary<string, object?>)run.Result!;
        Assert.That(receipt["open"], Is.True);
        Assert.That(receipt["changed"], Is.True);
    }
}
