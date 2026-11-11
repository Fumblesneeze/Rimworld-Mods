using System.Diagnostics;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class RunLeaseAndGatewayPlannerTests
{
    [Test]
    public void ExactProcessIdentity_DetectsRunningAndReusedPid()
    {
        using var current = Process.GetCurrentProcess();
        var start = new DateTimeOffset(current.StartTime.ToUniversalTime(), TimeSpan.Zero);
        var exact = RunProcessIdentity.Inspect(current.Id, start);
        var mismatched = RunProcessIdentity.Inspect(current.Id, start.AddMinutes(-1));

        Assert.That(exact.State, Is.EqualTo("running"));
        Assert.That(mismatched.State, Is.EqualTo("pid-reused"));
    }

    [Test]
    public void ReusedLauncherPid_DoesNotBlockGracefulExactGameCleanup()
    {
        var decision = RunCancellationPolicy.Decide("pid-reused", "running");

        Assert.That(decision.WaitForLauncher, Is.False);
        Assert.That(decision.SignalCompletion, Is.True);
        Assert.That(decision.GracefullyCloseGame, Is.True);
        Assert.That(decision.AllowExactPidFallback, Is.True);
    }

    [Test]
    public void GatewayPlanner_SeparatesDiagnosticsFromMutations()
    {
        var root = TestRoot();
        try
        {
            var manifest = Path.Combine(root, "artifacts", "run", "current.json");
            Directory.CreateDirectory(Path.GetDirectoryName(manifest)!);
            File.WriteAllText(manifest, "{}");
            var read = GatewayClientPlanner.Diagnostic(root, manifest, 123, "ui-state", null);
            var write = GatewayClientPlanner.Mutation(root, manifest, 123, "action", "game.pause", "{}");

            Assert.That(read.Kind, Is.EqualTo("gateway_diagnostic"));
            Assert.That(read.Arguments, Does.Contain("ui-state"));
            Assert.That(write.Kind, Is.EqualTo("gateway_mutation"));
            Assert.That(write.Arguments, Does.Contain("action"));
            Assert.That(write.Arguments, Does.Contain("game.pause"));
            Assert.Throws<ArgumentException>(() =>
                GatewayClientPlanner.Diagnostic(root, manifest, 123, "execute-source", null));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void AutomationLifecyclePlannerBindsTheExactRunAndSeparatesReadFromCancellation()
    {
        var root = TestRoot();
        try
        {
            var manifest = Path.Combine(root, "artifacts", "run", "current.json");
            Directory.CreateDirectory(Path.GetDirectoryName(manifest)!);
            File.WriteAllText(manifest, "{}");
            var read = GatewayClientPlanner.AutomationRun(root, manifest, 123, "gesture-1", false);
            var cancel = GatewayClientPlanner.AutomationRun(root, manifest, 123, "gesture-1", true);
            Assert.That(read.Kind, Is.EqualTo("gateway_automation_status"));
            Assert.That(cancel.Kind, Is.EqualTo("gateway_automation_cancel"));
            Assert.That(read.Arguments.SkipWhile(value => value != "automations").Take(3),
                Is.EqualTo(new[] { "automations", "status", "gesture-1" }));
            Assert.That(cancel.Arguments.SkipWhile(value => value != "automations").Take(3),
                Is.EqualTo(new[] { "automations", "cancel", "gesture-1" }));
            Assert.That(read.Arguments, Does.Contain(manifest));
            Assert.That(cancel.Arguments, Does.Contain("123"));
            foreach (var invalid in new[] { "", "../other", "gesture-1\n", new string('x', 201) })
                Assert.Throws<ArgumentException>(() => GatewayClientPlanner.AutomationRun(root, manifest, 123, invalid, false));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    internal static string TestRoot()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"gateway-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "mods"));
        Directory.CreateDirectory(Path.Combine(root, "openspec"));
        Directory.CreateDirectory(Path.Combine(root, "tools", "RimWorldDevGateway.Client"));
        File.WriteAllText(Path.Combine(root, "AGENTS.md"), "fixture");
        File.WriteAllText(Path.Combine(root, "tools", "RimWorldDevGateway.Client", "RimWorldDevGateway.Client.csproj"), "<Project />");
        return root;
    }
}
