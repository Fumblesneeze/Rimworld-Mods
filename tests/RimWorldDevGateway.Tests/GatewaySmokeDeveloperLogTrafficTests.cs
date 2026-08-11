using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class GatewaySmokeDeveloperLogTrafficTests
{
    [Test]
    public void Probe_keeps_the_native_developer_log_open_during_correlated_transport_traffic()
    {
        var script =
            LoadFunction("Invoke-GatewayDeveloperLogTrafficProbe") +
            "$script:opened = $false\n" +
            "$script:uiGets = 0\n" +
            "$script:entries = [System.Collections.Generic.List[object]]::new()\n" +
            "$script:screenshots = [System.Collections.Generic.List[string]]::new()\n" +
            "function Response([int]$status, [object]$body) { [pscustomobject]@{ StatusCode = $status; Content = ($body | ConvertTo-Json -Compress -Depth 12) } }\n" +
            "$get = { param($Uri, $Token, $RequestId)\n" +
            "  if ($Uri -like '*/logs?after=0&limit=1') { return Response 200 ([ordered]@{ ok=$true; result=[ordered]@{ Entries=@(); NewestCursor=10; HistoryEvicted=$false; PageTruncated=$false } }) }\n" +
            "  if ($Uri -like '*/ui-state') { $script:uiGets++; $windows = if ($script:opened -and $script:uiGets -ge 2) { @([ordered]@{ Type='EditWindow_Log' }) } else { @() }; return Response 200 ([ordered]@{ ok=$true; result=[ordered]@{ windows=$windows } }) }\n" +
            "  if ($Uri -like '*/status') { $script:entries.Add([pscustomobject]@{ RequestId=$RequestId; Thread='17'; Severity='Message' }); return Response 200 ([ordered]@{ ok=$true; result=[ordered]@{ programState='Playing' } }) }\n" +
            "  if ($Uri -like '*/game-state') { return Response 200 ([ordered]@{ ok=$true; result=[ordered]@{ Paused=$false; Speed='Normal' } }) }\n" +
            "  if ($Uri -like '*/logs?after=10*') { return Response 200 ([ordered]@{ ok=$true; result=[ordered]@{ Entries=@($script:entries); NewestCursor=20; HistoryEvicted=$false; PageTruncated=$false } }) }\n" +
            "  throw ('unexpected GET ' + $Uri)\n" +
            "}\n" +
            "$textPost = { param($Uri, $Token, $RequestId, $Source)\n" +
            "  if ($Source -like '*new LudeonTK.EditWindow_Log*') { $script:opened = $true; return Response 200 ([ordered]@{ ok=$true; result=[ordered]@{ Succeeded=$true; Value='opened|9' } }) }\n" +
            "  if ($Source -like '*TryRemove*') { $script:opened = $false; return Response 200 ([ordered]@{ ok=$true; result=[ordered]@{ Succeeded=$true; Value='closed' } }) }\n" +
            "  throw 'unexpected source'\n" +
            "}\n" +
            "$capture = { param($BaseUrl, $Token, $RequestId, $Path) if (-not $script:opened) { throw 'developer log was not open' }; $script:screenshots.Add($Path); [pscustomobject]@{ StatusCode=200 } }\n" +
            "$result = Invoke-GatewayDeveloperLogTrafficProbe -BaseUrl 'http://127.0.0.1:1/api/v1' -Token 'token' -TrafficCount 3 -BeforeScreenshotPath 'before.png' -AfterScreenshotPath 'after.png' -GatewayGet $get -GatewayTextPost $textPost -GatewayScreenshot $capture\n" +
            "if ($script:opened) { throw 'probe did not close the native developer log' }\n" +
            "$result | ConvertTo-Json -Compress -Depth 12\n";

        var result = RunPowerShell(script);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.StandardOutput, Does.Contain("\"WindowType\":\"LudeonTK.EditWindow_Log\""));
            Assert.That(result.StandardOutput, Does.Contain("\"TrafficCount\":3"));
            Assert.That(result.StandardOutput, Does.Contain("\"TransportThreads\":[\"17\"]"));
            Assert.That(result.StandardOutput, Does.Contain("\"MainThreadId\":\"9\""));
            Assert.That(result.StandardOutput, Does.Contain("\"MainThreadProbeSucceeded\":true"));
            Assert.That(result.StandardOutput, Does.Contain("\"Screenshots\":[\"before.png\",\"after.png\"]"));
        });
    }

    [Test]
    public void Probe_refuses_to_claim_or_close_a_preexisting_developer_log()
    {
        var script =
            LoadFunction("Invoke-GatewayDeveloperLogTrafficProbe") +
            "$script:opened = $true\n" +
            "$script:closeAttempted = $false\n" +
            "function Response([int]$status, [object]$body) { [pscustomobject]@{ StatusCode = $status; Content = ($body | ConvertTo-Json -Compress -Depth 12) } }\n" +
            "$get = { param($Uri, $Token, $RequestId) if ($Uri -like '*/logs?after=0&limit=1') { return Response 200 ([ordered]@{ ok=$true; result=[ordered]@{ Entries=@(); NewestCursor=10; HistoryEvicted=$false; PageTruncated=$false } }) }; throw ('unexpected GET ' + $Uri) }\n" +
            "$textPost = { param($Uri, $Token, $RequestId, $Source)\n" +
            "  if ($Source -like '*new LudeonTK.EditWindow_Log*') { return Response 200 ([ordered]@{ ok=$true; result=[ordered]@{ Succeeded=$true; Value='already-open|9' } }) }\n" +
            "  if ($Source -like '*TryRemove*') { $script:closeAttempted = $true; $script:opened = $false; return Response 200 ([ordered]@{ ok=$true; result=[ordered]@{ Succeeded=$true; Value='closed' } }) }\n" +
            "  throw 'unexpected source'\n" +
            "}\n" +
            "$capture = { throw 'screenshot must not run for an unowned window' }\n" +
            "$failedAsExpected = $false\n" +
            "try { $null = Invoke-GatewayDeveloperLogTrafficProbe -BaseUrl 'http://127.0.0.1:1/api/v1' -Token 'token' -TrafficCount 1 -BeforeScreenshotPath 'before.png' -AfterScreenshotPath 'after.png' -GatewayGet $get -GatewayTextPost $textPost -GatewayScreenshot $capture } catch { if ($_.Exception.Message -notlike '*already open*') { throw }; $failedAsExpected = $true }\n" +
            "if (-not $failedAsExpected) { throw 'probe accepted a preexisting developer log' }\n" +
            "if (-not $script:opened -or $script:closeAttempted) { throw 'probe closed an unowned developer log' }\n" +
            "'preserved'\n";

        var result = RunPowerShell(script);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.StandardOutput, Does.Contain("preserved"));
        });
    }

    private static string LoadFunction(string functionName)
    {
        var smokePath = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
        return
            "$tokens = $null; $parseErrors = $null\n" +
            $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
            $"$functionAst = $ast.Find({{ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq '{functionName}' }}, $true)\n" +
            $"if ($null -eq $functionAst) {{ throw 'Function was not found: {functionName}' }}\n" +
            "Invoke-Expression $functionAst.Extent.Text\n";
    }

    private static InvocationResult RunPowerShell(string script)
    {
        var root = Path.Combine(Path.GetTempPath(), nameof(GatewaySmokeDeveloperLogTrafficTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "invoke.ps1");
            File.WriteAllText(path, script, new UTF8Encoding(false));
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
            if (!process.WaitForExit(30_000))
            {
                process.Kill();
                throw new TimeoutException("PowerShell probe contract timed out.");
            }

            return new InvocationResult(process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string PowerShellLiteral(string value) =>
        "'" + value.Replace("'", "''") + "'";

    private static string FindSourceRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "scripts", "Invoke-GatewaySmoke.ps1")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the source repository root.");
    }

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
