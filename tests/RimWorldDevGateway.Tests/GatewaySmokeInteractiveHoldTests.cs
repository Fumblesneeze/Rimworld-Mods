using System;
using System.IO;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewaySmokeInteractiveHoldTests
{
    [Test]
    public void Interactive_hold_is_bounded_discoverable_and_keeps_cleanup_owned_by_the_harness()
    {
        var source = File.ReadAllText(Path.Combine(
            FindSourceRepositoryRoot(),
            "scripts",
            "Invoke-GatewaySmoke.ps1"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("[ValidateRange(0, 3600)]"));
            Assert.That(source, Does.Contain("[int]$InteractiveHoldSeconds = 0"));
            Assert.That(source, Does.Contain("interactive-hold.json"));
            Assert.That(source, Does.Contain("$holdDeadline"));
            Assert.That(source, Does.Contain("$launchedProcess.HasExited"));
            Assert.That(source, Does.Contain("InteractiveHoldSeconds = [int]$InteractiveHoldSeconds"));
        });
    }

    private static string FindSourceRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "scripts", "Invoke-GatewaySmoke.ps1")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not find the source repository root.");
    }
}
