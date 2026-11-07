using System.Diagnostics;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

public class UserPlaytestTests
{
    [Test]
    public void Playtest_plan_preserves_isolation_and_requires_explicit_opt_in()
    {
        string root = TestRepository.FindRoot();
        string fixture = Path.Combine(root, "artifacts", "PlaytestPlan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(fixture);
        try
        {
            var normal = RepositoryOperationPlanner.GatewayRun(root, fixture, Array.Empty<string>(), Array.Empty<string>(), 60);
            var play = RepositoryOperationPlanner.GatewayRun(root, fixture, Array.Empty<string>(), Array.Empty<string>(), 60,
                prelaunchConfigDirectory: fixture, userPlaytest: true);
            Assert.That(normal.Arguments, Does.Not.Contain("-UserPlaytest"));
            Assert.That(play.Arguments, Does.Contain("-UserPlaytest"));
            Assert.That(play.Arguments, Does.Contain("-PrelaunchConfigDirectory"));
            Assert.That(play.Arguments, Does.Contain(fixture));
            Assert.That(play.Arguments, Does.Not.Contain("-VisibleWindow"));
            Assert.Throws<ArgumentException>(() => RepositoryOperationPlanner.GatewayRun(root, fixture,
                Array.Empty<string>(), Array.Empty<string>(), 60, prelaunchConfigDirectory: root, userPlaytest: true));
        }
        finally { Directory.Delete(fixture, true); }
    }

    [TestCase(false, false, false, "Complete")]
    [TestCase(true, false, false, "Continue")]
    [TestCase(true, true, false, "Complete")]
    [TestCase(false, true, false, "UnexpectedExit")]
    [TestCase(true, false, true, "Complete")]
    public async Task Hold_lifetime_respects_user_ownership(bool user, bool exited, bool cancel, string expected)
    {
        string module = Path.Combine(TestRepository.FindRoot(), "scripts", "RimWorldPlaytest.Support.psm1");
        string Bool(bool value) => value ? "$true" : "$false";
        var start = new ProcessStartInfo("pwsh") { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-Command");
        start.ArgumentList.Add("$ErrorActionPreference='Stop'; Import-Module '" + module.Replace("'", "''") + "'; " +
            "Get-RimWorldHoldDecision -UserPlaytest " + Bool(user) + " -Expired $true -ProcessExited " + Bool(exited) + " -CompletionRequested " + Bool(cancel));
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
        Assert.That(process.ExitCode, Is.Zero, await stderr);
        Assert.That((await stdout).Trim(), Is.EqualTo(expected));
    }
}
