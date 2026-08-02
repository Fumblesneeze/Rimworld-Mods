using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class GatewaySmokeBoundedProcessTests
{
    [Test]
    public void Timed_out_process_is_never_reacquired_by_pid_for_termination()
    {
        var source = File.ReadAllText(Path.Combine(
            FindSourceRepositoryRoot(),
            "scripts",
            "Invoke-GatewaySmoke.ps1"));
        var start = source.IndexOf("function Invoke-GatewaySmokeBoundedProcess", StringComparison.Ordinal);
        var end = source.IndexOf("function Invoke-FlaUiJson", start, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0));
        Assert.That(end, Is.GreaterThan(start));

        var extent = source.Substring(start, end - start);
        Assert.That(
            extent,
            Does.Not.Contain("Stop-Process"),
            "A timed-out child must only be terminated through its retained Process handle; a cached PID can be reused.");
    }

    [Test]
    public void Final_retained_handle_state_overrides_historical_kill_diagnostics()
    {
        var source = File.ReadAllText(Path.Combine(
            FindSourceRepositoryRoot(),
            "scripts",
            "Invoke-GatewaySmoke.ps1"));
        var start = source.IndexOf("function Invoke-GatewaySmokeBoundedProcess", StringComparison.Ordinal);
        var end = source.IndexOf("function Invoke-FlaUiJson", start, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0));
        Assert.That(end, Is.GreaterThan(start));

        var extent = source.Substring(start, end - start);
        Assert.That(
            extent,
            Does.Not.Contain("-not $process.HasExited -or -not [string]::IsNullOrWhiteSpace($terminationFailure)"),
            "Once the retained handle reports HasExited, an earlier kill exception is diagnostic rather than an ownership failure.");
        Assert.That(extent, Does.Contain("$terminationFailure"));
    }

    [Test]
    public void Timed_out_child_process_is_terminated_before_returning()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "GatewaySmokeBoundedProcessTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var marker = Path.Combine(root, "late-completion.txt");
            var slowCommand = Path.Combine(root, "slow-command.ps1");
            File.WriteAllText(
                slowCommand,
                "Start-Sleep -Seconds 1\n" +
                $"[IO.File]::WriteAllText({Literal(marker)}, 'completed')\n" +
                "[Console]::Out.WriteLine('{\"success\":true}')\n",
                new UTF8Encoding(false));

            var invocation = Path.Combine(root, "invoke.ps1");
            var smoke = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
            File.WriteAllText(
                invocation,
                "$ErrorActionPreference = 'Stop'\nSet-StrictMode -Version Latest\n" +
                "$tokens = $null; $errors = $null\n" +
                $"$ast = [Management.Automation.Language.Parser]::ParseFile({Literal(smoke)}, [ref]$tokens, [ref]$errors)\n" +
                "$functionAst = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Invoke-GatewaySmokeBoundedProcess' }, $true)\n" +
                "if ($null -eq $functionAst) { throw 'Bounded process helper was not found.' }\n" +
                "Invoke-Expression $functionAst.Extent.Text\n" +
                $"$slowCommand = {Literal(slowCommand)}\n" +
                $"$marker = {Literal(marker)}\n" +
                "$message = $null\n" +
                "try {\n" +
                "  $null = Invoke-GatewaySmokeBoundedProcess -ExecutablePath (Get-Command pwsh).Source -Arguments @('-NoProfile','-NonInteractive','-File',$slowCommand) -TimeoutMilliseconds 200 -DisplayName 'fake FlaUI command'\n" +
                "  $message = 'command unexpectedly completed'\n" +
                "}\ncatch { $message = $_.Exception.Message }\n" +
                "Start-Sleep -Milliseconds 1300\n" +
                "[pscustomobject]@{ Message = $message; MarkerExists = (Test-Path -LiteralPath $marker) } | ConvertTo-Json -Compress\n",
                new UTF8Encoding(false));

            var run = RunPowerShell(invocation);

            Assert.Multiple(() =>
            {
                Assert.That(run.ExitCode, Is.Zero, run.StandardError);
                Assert.That(run.StandardOutput, Does.Contain("timed out after 200 ms"));
                Assert.That(run.StandardOutput, Does.Contain("\"MarkerExists\":false"));
                Assert.That(File.Exists(marker), Is.False);
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Raw_cleanup_command_accepts_non_json_output_when_exit_code_is_zero()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "GatewaySmokeBoundedProcessTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var rawCommand = Path.Combine(root, "raw-command.ps1");
            File.WriteAllText(
                rawCommand,
                "[Console]::Out.WriteLine('not-json-but-exit-zero')\nexit 0\n",
                new UTF8Encoding(false));

            var invocation = Path.Combine(root, "invoke-raw.ps1");
            var smoke = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
            File.WriteAllText(
                invocation,
                "$ErrorActionPreference = 'Stop'\nSet-StrictMode -Version Latest\n" +
                "$tokens = $null; $errors = $null\n" +
                $"$ast = [Management.Automation.Language.Parser]::ParseFile({Literal(smoke)}, [ref]$tokens, [ref]$errors)\n" +
                "foreach ($name in @('Invoke-GatewaySmokeBoundedProcess','Invoke-GatewaySmokeFlaUiCommand')) {\n" +
                "  $functionAst = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $name }, $true)\n" +
                "  if ($null -eq $functionAst) { throw \"Required function was not found: $name\" }\n" +
                "  Invoke-Expression $functionAst.Extent.Text\n" +
                "}\n" +
                $"$rawCommand = {Literal(rawCommand)}\n" +
                "$result = Invoke-GatewaySmokeFlaUiCommand -Arguments @('-NoProfile','-NonInteractive','-File',$rawCommand) -ExecutablePath (Get-Command pwsh).Source -TimeoutMilliseconds 2000\n" +
                "$result | ConvertTo-Json -Compress -Depth 5\n",
                new UTF8Encoding(false));

            var run = RunPowerShell(invocation);

            Assert.Multiple(() =>
            {
                Assert.That(run.ExitCode, Is.Zero, run.StandardError);
                Assert.That(run.StandardOutput, Does.Contain("not-json-but-exit-zero"));
                Assert.That(run.StandardOutput, Does.Contain("\"ExitCode\":0"));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static InvocationResult RunPowerShell(string path)
    {
        var start = new ProcessStartInfo
        {
            FileName = "pwsh.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{path}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start pwsh.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000))
        {
            process.Kill();
            throw new TimeoutException("Bounded process helper probe timed out.");
        }
        return new InvocationResult(process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
    }

    private static string FindSourceRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "scripts", "Invoke-GatewaySmoke.ps1")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate source repository root.");
    }

    private static string Literal(string value) => "'" + value.Replace("'", "''") + "'";

    private sealed class InvocationResult
    {
        public InvocationResult(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }

        public int ExitCode { get; }

        public string StandardOutput { get; }

        public string StandardError { get; }
    }
}
