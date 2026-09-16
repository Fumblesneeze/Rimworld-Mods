using System.Security.Cryptography;
using System.Text.Json;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class PresentationRenderTests
{
    private string _root = null!;
    private string _workshop = null!;
    private string _script = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"presentation-{Guid.NewGuid():N}");
        _workshop = Path.Combine(_root, "mods", "ImmersiveChefs", "Release", "workshop");
        Directory.CreateDirectory(_workshop);
        Directory.CreateDirectory(Path.Combine(_root, "openspec"));
        Directory.CreateDirectory(Path.Combine(_root, "scripts"));
        File.WriteAllText(Path.Combine(_root, "AGENTS.md"), "fixture");
        var original = TestRepository.FindRoot();
        foreach (var relative in new[]
                 {
                     "mods/ImmersiveChefs/ImmersiveChefs.csproj",
                     "mods/ImmersiveChefs/Release/release.json",
                     "mods/ImmersiveChefs/Release/workshop/description.bbcode",
                     "mods/ImmersiveChefs/Release/workshop/preview-main.png"
                 })
            File.Copy(Path.Combine(original, relative), Path.Combine(_root, relative));
        _script = Path.Combine(_root, "scripts", "Render-Fixture.ps1");
        File.WriteAllText(_script, """
            param([string]$ManifestPath, [ValidateSet('json')][string]$Output)
            $manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
            $target = Join-Path (Split-Path -Parent $ManifestPath) 'rendered.txt'
            [IO.File]::WriteAllText($target, [string]$manifest.copy)
            @{ rendered = $target; sha256 = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash } | ConvertTo-Json -Compress
            """);
        WriteManifest("scripts/Render-Fixture.ps1", Hash(_script));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Test]
    public async Task Cli_render_executes_the_selected_adapter_and_repeats_identical_output()
    {
        var result = await CallCli("json");
        Assert.That(result.ExitCode, Is.Zero, result.Error);
        var target = Path.Combine(_workshop, "rendered.txt");
        Assert.That(File.ReadAllText(target), Is.EqualTo("reviewed feature copy"));
        var firstHash = Hash(target);
        using var response = JsonDocument.Parse(result.Output);
        Assert.That(response.RootElement.GetProperty("operation").GetString(), Is.EqualTo("presentation_render"));
        Assert.That(response.RootElement.GetProperty("exitCode").GetInt32(), Is.Zero);
        Assert.That(response.RootElement.GetProperty("standardOutput").GetString(), Does.Contain(firstHash));

        var repeated = await CallCli("table");
        Assert.That(repeated.ExitCode, Is.Zero, repeated.Error);
        Assert.That(repeated.Output, Does.Contain("presentation_render"));
        Assert.That(Hash(target), Is.EqualTo(firstHash));
    }

    [TestCase("scripts/Render-Fixture.ps1", "changed-hash")]
    [TestCase("mods/ImmersiveChefs/Render.ps1", "outside-scripts")]
    [TestCase("scripts/Render.cmd", "wrong-extension")]
    [TestCase("../Render.ps1", "escape")]
    [TestCase("scripts/Missing.ps1", "missing")]
    public async Task Cli_render_rejects_invalid_renderer_before_execution(string path, string reason)
    {
        WriteManifest(path, reason == "changed-hash" ? new string('0', 64) : Hash(_script));
        var result = await CallCli("json");
        Assert.That(result.ExitCode, Is.EqualTo(2), result.Error);
        Assert.That(result.Error, Is.Not.Empty);
        Assert.That(File.Exists(Path.Combine(_workshop, "rendered.txt")), Is.False);
    }

    [Test]
    public async Task Cli_render_reports_adapter_failure_without_success()
    {
        File.WriteAllText(_script, "param([string]$ManifestPath, [string]$Output)\nWrite-Error 'fixture-render-failed'\nexit 7");
        WriteManifest("scripts/Render-Fixture.ps1", Hash(_script));
        var result = await CallCli("json");
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Error, Does.Contain("fixture-render-failed").And.Contain("exit code 7"));
        Assert.That(result.Output, Is.Empty);
    }

    [Test]
    public async Task Cli_render_requires_a_selected_profile_and_renderer_declaration()
    {
        File.WriteAllText(Path.Combine(_workshop, "presentation.json"), "{}");
        var result = await CallCli("json");
        Assert.That(result.ExitCode, Is.EqualTo(2));
        Assert.That(result.Error, Does.Contain("requires a renderer"));

        var registry = OperationRegistry.CreateDefault(_root);
        Assert.ThrowsAsync<ArgumentException>(async () => await registry.InvokeAsync(
            "presentation_render", JsonSerializer.SerializeToElement(new { packageId = "fumblesneeze.unknown" }),
            CancellationToken.None));
    }

    [Test]
    public async Task Immersive_renderer_rejects_a_different_worktree_manifest_before_writing()
    {
        var root = TestRepository.FindRoot();
        var result = await ProcessRunner.RunAsync("pwsh",
            ["-NoProfile", "-NonInteractive", "-File",
                Path.Combine(root, "scripts", "Build-ImmersiveChefsWorkshopPresentation.ps1"),
                "-ManifestPath", Path.Combine(_workshop, "presentation.json"), "-Output", "json"],
            root, TimeSpan.FromSeconds(30), CancellationToken.None);
        Assert.That(result.ExitCode, Is.Not.Zero);
        Assert.That(result.StandardError, Does.Contain("ManifestPath must select this worktree"));
    }

    private async Task<(int ExitCode, string Output, string Error)> CallCli(string format)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = await McpCli.InvokeAsync(
            ["tool", "call", "presentation_render", "--repository-root", _root,
                "--arguments", "{\"packageId\":\"fumblesneeze.immersivechefs\"}", "-o", format],
            output, error, CancellationToken.None);
        return (exit, output.ToString(), error.ToString());
    }

    private void WriteManifest(string scriptPath, string scriptHash) =>
        File.WriteAllText(Path.Combine(_workshop, "presentation.json"), JsonSerializer.Serialize(new
        {
            renderer = new { path = scriptPath, sha256 = scriptHash },
            copy = "reviewed feature copy"
        }));

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
