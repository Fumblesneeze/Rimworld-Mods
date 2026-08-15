using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class GatewaySmokeProcessDumpTests
{
    [TestCase("Invoke-GatewaySmoke.ps1")]
    [TestCase("Invoke-RimWorldSmoke.ps1")]
    public void Owned_process_dump_uses_the_native_dump_api_and_captures_a_path_with_spaces(
        string smokeScriptName)
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "Gateway smoke process dump",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Process? target = null;
        try
        {
            target = Process.Start(new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = "-NoProfile -NonInteractive -Command \"Start-Sleep -Seconds 60\"",
                UseShellExecute = false,
                CreateNoWindow = true
            }) ?? throw new InvalidOperationException("Could not start the owned dump target.");

            var dumpPath = Path.Combine(root, "owned target.dmp");
            var invocationPath = Path.Combine(root, "invoke.ps1");
            var smokePath = Path.Combine(
                FindSourceRepositoryRoot(),
                "scripts",
                smokeScriptName);
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                "$functionAst = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Capture-OwnedProcessDump' }, $true)\n" +
                "if ($null -eq $functionAst) { throw 'Function was not found: Capture-OwnedProcessDump' }\n" +
                "Invoke-Expression $functionAst.Extent.Text\n" +
                $"$target = Get-Process -Id {target.Id}\n" +
                $"$result = Capture-OwnedProcessDump -Process $target -DumpPath {PowerShellLiteral(dumpPath)}\n" +
                "$result | ConvertTo-Json -Compress\n" +
                "if ($result.Status -cne 'captured') { exit 81 }\n" +
                "if ($result.Detail -cne 'Native MiniDumpWriteDump completed.') { exit 82 }\n" +
                $"if (-not (Test-Path -LiteralPath {PowerShellLiteral(dumpPath)} -PathType Leaf)) {{ exit 83 }}\n" +
                $"if ((Get-Item -LiteralPath {PowerShellLiteral(dumpPath)}).Length -le 0) {{ exit 84 }}\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));

            var result = RunPowerShell(invocationPath);

            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.Zero, result.StandardOutput + result.StandardError);
                Assert.That(result.StandardOutput, Does.Contain("\"Status\":\"captured\""));
                Assert.That(new FileInfo(dumpPath).Length, Is.GreaterThan(0));
                Assert.That(
                    Encoding.ASCII.GetString(File.ReadAllBytes(dumpPath), 0, 4),
                    Is.EqualTo("MDMP"));
            });
        }
        finally
        {
            if (target is not null)
            {
                try
                {
                    if (!target.HasExited)
                    {
                        target.Kill();
                        target.WaitForExit(10_000);
                    }
                }
                finally
                {
                    target.Dispose();
                }
            }

            Directory.Delete(root, recursive: true);
        }
    }

    [TestCase("Invoke-GatewaySmoke.ps1")]
    [TestCase("Invoke-RimWorldSmoke.ps1")]
    public void Force_fallback_dumps_and_terminates_through_the_retained_process_handle(
        string smokeScriptName)
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "Gateway smoke retained handle cleanup",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Process? target = null;
        try
        {
            target = Process.Start(new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = "-NoProfile -NonInteractive -Command \"Start-Sleep -Seconds 90\"",
                UseShellExecute = false,
                CreateNoWindow = true
            }) ?? throw new InvalidOperationException("Could not start the force-fallback target.");

            var dumpPath = Path.Combine(root, "force fallback.dmp");
            var invocationPath = Path.Combine(root, "invoke-force-fallback.ps1");
            var smokePath = Path.Combine(FindSourceRepositoryRoot(), "scripts", smokeScriptName);
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                "$captureAst = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Capture-OwnedProcessDump' }, $true)\n" +
                "$stopAst = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Stop-OwnedProcessGracefully' }, $true)\n" +
                "Invoke-Expression $captureAst.Extent.Text\n" +
                "Invoke-Expression $stopAst.Extent.Text\n" +
                $"$target = Get-Process -Id {target.Id}\n" +
                $"$result = Stop-OwnedProcessGracefully -Process $target -DumpPath {PowerShellLiteral(dumpPath)}\n" +
                "$result | ConvertTo-Json -Compress -Depth 5\n" +
                "if ($result.Method -cne 'force-fallback' -or -not $result.Forced) { exit 81 }\n" +
                "if ($result.Dump.Status -cne 'captured') { exit 82 }\n" +
                "if (-not $target.HasExited) { exit 83 }\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));

            var result = RunPowerShell(invocationPath);

            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.Zero, result.StandardOutput + result.StandardError);
                Assert.That(result.StandardOutput, Does.Contain("\"Method\":\"force-fallback\""));
                Assert.That(target.HasExited, Is.True);
                Assert.That(Encoding.ASCII.GetString(File.ReadAllBytes(dumpPath), 0, 4), Is.EqualTo("MDMP"));
            });
        }
        finally
        {
            if (target is not null)
            {
                try
                {
                    if (!target.HasExited)
                    {
                        target.Kill();
                        target.WaitForExit(10_000);
                    }
                }
                finally
                {
                    target.Dispose();
                }
            }

            Directory.Delete(root, recursive: true);
        }
    }

    [TestCase("Invoke-GatewaySmoke.ps1")]
    [TestCase("Invoke-RimWorldSmoke.ps1")]
    public void Invalid_dump_destination_fails_without_starting_or_retaining_a_helper(
        string smokeScriptName)
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "Gateway smoke failed process dump",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Process? target = null;
        try
        {
            target = Process.Start(new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = "-NoProfile -NonInteractive -Command \"Start-Sleep -Seconds 60\"",
                UseShellExecute = false,
                CreateNoWindow = true
            }) ?? throw new InvalidOperationException("Could not start the exited dump target.");

            var dumpPath = Path.Combine(root, "missing", "failed target.dmp");
            var invocationPath = Path.Combine(root, "invoke-failed.ps1");
            var smokePath = Path.Combine(
                FindSourceRepositoryRoot(),
                "scripts",
                smokeScriptName);
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                "$functionAst = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Capture-OwnedProcessDump' }, $true)\n" +
                "Invoke-Expression $functionAst.Extent.Text\n" +
                $"$target = Get-Process -Id {target.Id}\n" +
                $"$result = Capture-OwnedProcessDump -Process $target -DumpPath {PowerShellLiteral(dumpPath)}\n" +
                "$result | ConvertTo-Json -Compress\n" +
                "if ($result.Status -cne 'failed') { exit 81 }\n" +
                $"if (Test-Path -LiteralPath {PowerShellLiteral(dumpPath)} -PathType Leaf) {{ exit 82 }}\n" +
                $"if (@(Get-ChildItem -LiteralPath {PowerShellLiteral(root)} -Filter '.rimworld-minidump-*.ps1').Count -ne 0) {{ exit 83 }}\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));

            var result = RunPowerShell(invocationPath);

            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.Zero, result.StandardOutput + result.StandardError);
                Assert.That(result.StandardOutput, Does.Contain("\"Status\":\"failed\""));
                Assert.That(File.Exists(dumpPath), Is.False);
                Assert.That(Directory.GetFiles(root, ".rimworld-minidump-*.ps1"), Is.Empty);
            });

        }
        finally
        {
            if (target is not null)
            {
                try
                {
                    if (!target.HasExited)
                    {
                        target.Kill();
                        target.WaitForExit(10_000);
                    }
                }
                finally
                {
                    target.Dispose();
                }
            }

            Directory.Delete(root, recursive: true);
        }
    }

    [TestCase("Invoke-GatewaySmoke.ps1")]
    [TestCase("Invoke-RimWorldSmoke.ps1")]
    public void Timed_out_dump_helper_is_terminated_and_its_partial_artifact_is_removed(
        string smokeScriptName)
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "Gateway smoke dump timeout cleanup",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Process? helper = null;
        try
        {
            helper = Process.Start(new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = "-NoProfile -NonInteractive -Command \"Start-Sleep -Seconds 60\"",
                UseShellExecute = false,
                CreateNoWindow = true
            }) ?? throw new InvalidOperationException("Could not start the timed-out dump helper fixture.");
            var partialPath = Path.Combine(root, "partial.tmp");
            File.WriteAllBytes(partialPath, Encoding.ASCII.GetBytes("partial"));
            var invocationPath = Path.Combine(root, "invoke-timeout-cleanup.ps1");
            var smokePath = Path.Combine(FindSourceRepositoryRoot(), "scripts", smokeScriptName);
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                "$functionAst = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Stop-OwnedDumpHelper' }, $true)\n" +
                "Invoke-Expression $functionAst.Extent.Text\n" +
                $"$helper = Get-Process -Id {helper.Id}\n" +
                $"$result = Stop-OwnedDumpHelper -Helper $helper -PartialDumpPath {PowerShellLiteral(partialPath)}\n" +
                "$result | ConvertTo-Json -Compress\n" +
                "if ($result.Status -cne 'timed-out' -or $null -ne $result.Path) { exit 81 }\n" +
                "if (-not $helper.HasExited) { exit 82 }\n" +
                $"if (Test-Path -LiteralPath {PowerShellLiteral(partialPath)} -PathType Leaf) {{ exit 83 }}\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));

            var result = RunPowerShell(invocationPath);

            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.Zero, result.StandardOutput + result.StandardError);
                Assert.That(result.StandardOutput, Does.Contain("\"Status\":\"timed-out\""));
                Assert.That(result.StandardOutput, Does.Contain($"\"HelperProcessId\":{helper.Id}"));
                Assert.That(result.StandardOutput, Does.Contain("\"HelperAlive\":false"));
                Assert.That(helper.HasExited, Is.True);
                Assert.That(File.Exists(partialPath), Is.False);
            });
        }
        finally
        {
            if (helper is not null)
            {
                try
                {
                    if (!helper.HasExited)
                    {
                        helper.Kill();
                        helper.WaitForExit(10_000);
                    }
                }
                finally
                {
                    helper.Dispose();
                }
            }

            Directory.Delete(root, recursive: true);
        }
    }

    [TestCase("Invoke-GatewaySmoke.ps1")]
    [TestCase("Invoke-RimWorldSmoke.ps1")]
    public void Timed_out_dump_helper_cleanup_failure_retains_exact_identity_and_fails_explicitly(
        string smokeScriptName)
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "Gateway smoke dump timeout cleanup failure",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Process? helper = null;
        FileStream? lockStream = null;
        try
        {
            helper = Process.Start(new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = "-NoProfile -NonInteractive -Command \"Start-Sleep -Seconds 60\"",
                UseShellExecute = false,
                CreateNoWindow = true
            }) ?? throw new InvalidOperationException("Could not start the cleanup-failure helper fixture.");
            var partialPath = Path.Combine(root, "locked-partial.tmp");
            lockStream = new FileStream(
                partialPath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None);
            lockStream.Write(Encoding.ASCII.GetBytes("partial"), 0, 7);
            lockStream.Flush(flushToDisk: true);
            var invocationPath = Path.Combine(root, "invoke-timeout-cleanup-failure.ps1");
            var smokePath = Path.Combine(FindSourceRepositoryRoot(), "scripts", smokeScriptName);
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                "$functionAst = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Stop-OwnedDumpHelper' }, $true)\n" +
                "Invoke-Expression $functionAst.Extent.Text\n" +
                $"$helper = Get-Process -Id {helper.Id}\n" +
                $"$result = Stop-OwnedDumpHelper -Helper $helper -PartialDumpPath {PowerShellLiteral(partialPath)}\n" +
                "$result | ConvertTo-Json -Compress\n" +
                "if ($result.Status -cne 'cleanup-failed' -or $result.HelperAlive) { exit 81 }\n" +
                "if (-not $helper.HasExited) { exit 82 }\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));

            var result = RunPowerShell(invocationPath);

            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.Zero, result.StandardOutput + result.StandardError);
                Assert.That(result.StandardOutput, Does.Contain("\"Status\":\"cleanup-failed\""));
                Assert.That(result.StandardOutput, Does.Contain($"\"HelperProcessId\":{helper.Id}"));
                Assert.That(result.StandardOutput, Does.Contain("\"HelperAlive\":false"));
                Assert.That(helper.HasExited, Is.True);
                Assert.That(File.Exists(partialPath), Is.True);
            });
        }
        finally
        {
            lockStream?.Dispose();
            if (helper is not null)
            {
                try
                {
                    if (!helper.HasExited)
                    {
                        helper.Kill();
                        helper.WaitForExit(10_000);
                    }
                }
                finally
                {
                    helper.Dispose();
                }
            }

            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Gateway_launcher_persists_structured_dump_cleanup_failure_as_a_failed_outcome()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "Gateway smoke structured cleanup outcome",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var invocationPath = Path.Combine(root, "invoke-structured-outcome.ps1");
            var smokePath = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                "$functionAst = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'ConvertTo-GatewayProcessCleanupArtifact' }, $true)\n" +
                "Invoke-Expression $functionAst.Extent.Text\n" +
                "$cleanup = [pscustomobject]@{ Method='force-fallback'; Dump=[pscustomobject]@{ Status='cleanup-failed'; Path='partial.tmp'; Detail='locked'; HelperProcessId=42; HelperProcessStartUtc='2026-08-11T12:00:00Z'; HelperAlive=$true } }\n" +
                "$artifact = ConvertTo-GatewayProcessCleanupArtifact -ProcessCleanup $cleanup\n" +
                "$artifact | ConvertTo-Json -Compress -Depth 6\n" +
                "if ($artifact.Status -cne 'failed' -or $artifact.Result.Dump.Path -cne 'partial.tmp' -or -not $artifact.Result.Dump.HelperAlive -or $artifact.Result.Dump.HelperProcessId -ne 42) { exit 81 }\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));

            var result = RunPowerShell(invocationPath);

            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.Zero, result.StandardOutput + result.StandardError);
                Assert.That(result.StandardOutput, Does.Contain("\"Status\":\"failed\""));
                Assert.That(result.StandardOutput, Does.Contain("\"Path\":\"partial.tmp\""));
                Assert.That(result.StandardOutput, Does.Contain("\"HelperAlive\":true"));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Hang_dump_probe_outcome_requires_force_fallback_valid_dump_and_token_free_retention()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "Gateway smoke hang dump outcome",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var dumpPath = Path.Combine(root, "RimWorldWin64-hang.dmp");
            File.WriteAllBytes(dumpPath, Encoding.ASCII.GetBytes("MDMP token-free native dump"));
            var invocationPath = Path.Combine(root, "invoke-hang-outcome.ps1");
            var smokePath = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                "$functionNames = @('Get-GatewaySmokeFileSha256','Test-FileContainsBearerToken','Assert-GatewayHangDumpProbeOutcome')\n" +
                "foreach ($functionName in $functionNames) { $functionAst = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $functionName }, $true); Invoke-Expression $functionAst.Extent.Text }\n" +
                $"$cleanup = [pscustomobject]@{{ Method='force-fallback'; Dump=[pscustomobject]@{{ Status='captured'; Path={PowerShellLiteral(dumpPath)}; TargetProcessId=42; TargetProcessStartUtc='2026-08-11T12:00:00Z' }} }}\n" +
                $"$outcome = Assert-GatewayHangDumpProbeOutcome -ExpectedTransitionObserved $true -ProcessCleanup $cleanup -ProcessCleanupStatus completed -CredentialCleanupStatus completed -ExpectedDumpPath {PowerShellLiteral(dumpPath)} -BearerToken 'secret-token'\n" +
                "$outcome | ConvertTo-Json -Compress\n" +
                "if ($outcome.Status -cne 'passed' -or $outcome.ProcessMethod -cne 'force-fallback' -or $outcome.ProcessId -ne 42) { exit 81 }\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));

            var result = RunPowerShell(invocationPath);

            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.Zero, result.StandardOutput + result.StandardError);
                Assert.That(result.StandardOutput, Does.Contain("\"Status\":\"passed\""));
                Assert.That(result.StandardOutput, Does.Contain("\"CredentialsChecked\":true"));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static InvocationResult RunPowerShell(string path)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "pwsh.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{path}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("Could not start the dump probe.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(60_000))
        {
            process.Kill();
            throw new TimeoutException("Owned process dump probe timed out.");
        }

        Task.WaitAll(output, error);
        return new InvocationResult(process.ExitCode, output.Result, error.Result);
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

    private static string PowerShellLiteral(string value) => $"'{value.Replace("'", "''")}'";

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
