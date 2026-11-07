using System.Diagnostics;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

public sealed class EndToEndTimeoutTests
{
    [Test]
    public async Task Nested_process_accepts_full_game_deadline_plus_cleanup_grace()
    {
        string script = Path.Combine(TestRepository.FindRoot(), "scripts", "Invoke-RimWorldEndToEndTests.ps1");
        var start = new ProcessStartInfo("pwsh")
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
        // Extract only this real function's AST: importing the script would launch orchestration.
        string command = "$ErrorActionPreference='Stop'; $ast=[System.Management.Automation.Language.Parser]::ParseFile($args[0],[ref]$null,[ref]$null);" +
            "$fn=$ast.Find({param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq 'Invoke-BoundedProcess'},$true);" +
            ". ([scriptblock]::Create($fn.Extent.Text));" +
            "$r=Invoke-BoundedProcess -ExecutablePath pwsh -Arguments @('-NoProfile','-NonInteractive','-Command','exit 0') -TimeoutMilliseconds 3780000 -Description probe; exit $r.ExitCode";
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-CommandWithArgs", command, script }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        try { await process.WaitForExitAsync(deadline.Token); }
        catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
        Assert.That(process.ExitCode, Is.Zero, await error + await output);
    }

    [TestCase(601, true)]
    [TestCase(3600, true)]
    [TestCase(3601, false)]
    public async Task Adapter_timeout_boundary_matches_the_typed_operation(int seconds, bool allowed)
    {
        string root = TestRepository.FindRoot();
        string script = Path.Combine(root, "scripts", "Invoke-RimWorldEndToEndTests.ps1");
        // Parameter binding runs before the deliberately invalid fixture. Thus this proves the
        // real adapter boundary without building, scanning installed mods or launching a game.
        var start = new ProcessStartInfo("pwsh")
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-File", script, "-TimeoutSeconds", seconds.ToString(),
                     "-DryRun", "-PrelaunchConfigDirectory", Path.Combine(root, "artifacts", "missing-" + Guid.NewGuid()) })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        try { await process.WaitForExitAsync(deadline.Token); }
        catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
        string text = await error + await output;
        Assert.That(process.ExitCode, Is.Not.Zero);
        Assert.That(text.Contains("parameter 'TimeoutSeconds'", StringComparison.Ordinal), Is.EqualTo(!allowed), text);
        if (allowed) Assert.That(text, Does.Contain("prelaunch").IgnoreCase, text);
    }
}
