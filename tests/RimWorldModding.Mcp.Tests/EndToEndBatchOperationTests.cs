using System.Text.Json;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture, NonParallelizable]
public sealed class EndToEndBatchOperationTests
{
    private string root = null!;

    [SetUp]
    public void SetUp()
    {
        root = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"e2e-batch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "scripts"));
        Directory.CreateDirectory(Path.Combine(root, "mods"));
        Directory.CreateDirectory(Path.Combine(root, "openspec"));
        File.WriteAllText(Path.Combine(root, "AGENTS.md"), "fixture");
        File.WriteAllText(Path.Combine(root, "scripts", "Invoke-RimWorldEndToEndTests.ps1"), """
            param([string]$TestIdsCsv, [string]$Language, [int]$TimeoutSeconds,
                  [switch]$DryRun, [string]$Output)
            if (-not $DryRun) { throw 'This transport fixture must never launch.' }
            [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'invoked.txt'), $TestIdsCsv)
            @{ selected = @($TestIdsCsv.Split(',')); language = $Language } | ConvertTo-Json -Compress
            """);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    [TestCase("json")]
    [TestCase("table")]
    public async Task CliDryRunTransportsTheExactBatchThroughPowerShellFileArguments(string format)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        int exit = await McpCli.InvokeAsync(
            ["tool", "call", "e2e_run_start", "--repository-root", root, "--arguments",
             "{\"testIds\":[\"alpha.first\",\"alpha.second\"],\"dryRun\":true}", "-o", format],
            output, error, CancellationToken.None);
        Assert.That(exit, Is.Zero, error.ToString());
        Assert.That(error.ToString(), Is.Empty);
        if (format == "table")
        {
            Assert.That(output.ToString(), Does.Contain("operation: e2e_run").And.Contain("exit code: 0"));
            Assert.That(File.ReadAllText(Path.Combine(root, "scripts", "invoked.txt")),
                Is.EqualTo("alpha.first,alpha.second"));
            return;
        }
        using var response = JsonDocument.Parse(output.ToString());
        using var adapter = JsonDocument.Parse(response.RootElement.GetProperty("standardOutput").GetString()!);
        Assert.That(adapter.RootElement.GetProperty("selected").EnumerateArray().Select(value => value.GetString()),
            Is.EqualTo(new[] { "alpha.first", "alpha.second" }));
        Assert.That(adapter.RootElement.GetProperty("language").GetString(), Is.EqualTo("English"));
        Assert.That(error.ToString(), Is.Empty);
    }

    [TestCase("[]")]
    [TestCase("[\"alpha.first\",\"alpha.first\"]")]
    [TestCase("[\"alpha.first,beta.second\"]")]
    [TestCase("[\"alpha.first;exit\"]")]
    [TestCase("[null]")]
    [TestCase("[\" \" ]")]
    [TestCase("42")]
    public async Task CliRefusesInvalidBatchesBeforeAdapter(string ids)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        int exit = await McpCli.InvokeAsync(
            ["tool", "call", "e2e_run_start", "--repository-root", root, "--arguments",
             "{\"testIds\":" + ids + ",\"dryRun\":true}", "-o", "json"],
            output, error, CancellationToken.None);
        Assert.That(exit, Is.EqualTo(2), error.ToString());
        Assert.That(output.ToString(), Is.Empty);
        Assert.That(error.ToString(), Does.Contain("testIds"));
        Assert.That(File.Exists(Path.Combine(root, "scripts", "invoked.txt")), Is.False);
    }

    [TestCase("\"testId\":\"alpha.first\"")]
    [TestCase("\"groupId\":\"ludeon.rimworld\"")]
    public async Task CliRefusesCombinedSelectors(string otherSelector)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        int exit = await McpCli.InvokeAsync(
            ["tool", "call", "e2e_run_start", "--repository-root", root, "--arguments",
             "{\"testIds\":[\"alpha.first\"],\"dryRun\":true," + otherSelector + "}", "-o", "json"],
            output, error, CancellationToken.None);
        Assert.That(exit, Is.EqualTo(2));
        Assert.That(error.ToString(), Does.Contain("Exactly one"));
        Assert.That(output.ToString(), Is.Empty);
        Assert.That(File.Exists(Path.Combine(root, "scripts", "invoked.txt")), Is.False);
    }

    [Test]
    public void BatchUsesTheNativeIdLengthBoundaryAndAllowsOneSelectedTest()
    {
        var plan = RepositoryOperationPlanner.EndToEnd(root, null, null, "English", 300,
            dryRun: true, testIds: [new string('a', 160)]);
        Assert.That(plan.Arguments, Does.Contain(new string('a', 160)));
        Assert.Throws<ArgumentException>(() => RepositoryOperationPlanner.EndToEnd(root, null, null,
            "English", 300, dryRun: true, testIds: [new string('a', 161)]));
    }
}
