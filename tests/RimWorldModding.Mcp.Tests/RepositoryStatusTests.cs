using System.Text.Json;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class RepositoryStatusTests
{
    [Test]
    public async Task RepositoryStatus_UsesCanonicalMetadataAndSharedRegistry()
    {
        var root = TestRepository.FindRoot();
        var registry = OperationRegistry.CreateDefault(root);

        var descriptor = registry.Descriptors.Single(item => item.Name == "repository_status");
        Assert.That(descriptor.Risk, Is.EqualTo(OperationRisk.Read));
        Assert.That(descriptor.LongRunning, Is.False);

        var result = await registry.InvokeAsync("repository_status", JsonDocument.Parse("{}").RootElement, CancellationToken.None);
        var status = (RepositoryStatusResult)result;

        Assert.That(status.RepositoryRoot, Is.EqualTo(root));
        Assert.That(status.Mods.Select(mod => mod.PackageId), Does.Contain("fumblesneeze.rimworlddevgateway"));
        Assert.That(status.Mods.Select(mod => mod.PackageId), Does.Contain("fumblesneeze.guestbedgizmo"));
        Assert.That(status.Mods.Single(mod => mod.PackageId == "fumblesneeze.guestbedgizmo").DisplayName, Is.EqualTo("Hospitality + Ideology Patch"));
    }

    [Test]
    public async Task Cli_EmitsDeterministicJsonAndValidationExitCodes()
    {
        var root = TestRepository.FindRoot();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var success = await McpCli.InvokeAsync(
            ["tool", "call", "repository_status", "--repository-root", root, "--arguments", "{}", "--output", "json"],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.That(success, Is.EqualTo(0), stderr.ToString());
        using var payload = JsonDocument.Parse(stdout.ToString());
        Assert.That(payload.RootElement.GetProperty("repositoryRoot").GetString(), Is.EqualTo(root));
        Assert.That(stderr.ToString(), Is.Empty);

        stdout.GetStringBuilder().Clear();
        stderr.GetStringBuilder().Clear();

        var invalid = await McpCli.InvokeAsync(
            ["tool", "call", "repository_status", "--repository-root", root, "--arguments", "{", "--output", "yaml"],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.That(invalid, Is.EqualTo(2));
        Assert.That(stdout.ToString(), Is.Empty);
        Assert.That(stderr.ToString(), Does.Contain("output").IgnoreCase);
    }
}

internal static class TestRepository
{
    public static string FindRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AGENTS.md")) && Directory.Exists(Path.Combine(current.FullName, "mods")))
            {
                return current.FullName.TrimEnd(Path.DirectorySeparatorChar);
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root not found from test directory.");
    }
}
