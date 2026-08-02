using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class GatewaySmokeCleanupAggregationTests
{
    [Test]
    public void Primary_process_credential_and_stage_failures_are_all_preserved()
    {
        using var fixture = Fixture.Create();
        var run = fixture.Invoke(
            "$failures = [System.Collections.Generic.List[object]]::new()\n" +
            "Add-GatewaySmokeFailure -Failures $failures -Category 'primary' -Message 'verification failed'\n" +
            "Add-GatewaySmokeFailure -Failures $failures -Category 'process-cleanup' -Message 'process remained alive'\n" +
            "Add-GatewaySmokeFailure -Failures $failures -Category 'credential-cleanup' -Message 'token remained'\n" +
            "Add-GatewaySmokeFailure -Failures $failures -Category 'stage-cleanup' -Message 'stage remained'\n" +
            "$status = New-GatewaySmokeCleanupStatus -ProcessStatus 'failed' -CredentialStatus 'failed' -StageStatus 'failed' -Failures $failures\n" +
            "$status | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $artifactPath -Encoding UTF8\n" +
            "Write-Output ($status.Failures.Count)");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("4"));
            var artifact = File.ReadAllText(fixture.ArtifactPath);
            Assert.That(artifact, Does.Contain("verification failed"));
            Assert.That(artifact, Does.Contain("process remained alive"));
            Assert.That(artifact, Does.Contain("token remained"));
            Assert.That(artifact, Does.Contain("stage remained"));
            Assert.That(artifact, Does.Contain("\"Status\": \"failed\""));
        });
    }

    [Test]
    public void Cleanup_status_explicitly_records_not_required_components()
    {
        using var fixture = Fixture.Create();
        var run = fixture.Invoke(
            "$failures = [System.Collections.Generic.List[object]]::new()\n" +
            "$status = New-GatewaySmokeCleanupStatus -ProcessStatus 'not-started' -CredentialStatus 'not-started' -StageStatus 'not-requested' -Failures $failures\n" +
            "$status | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $artifactPath -Encoding UTF8\n" +
            "Write-Output ($status.Status)");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("completed"));
            var artifact = File.ReadAllText(fixture.ArtifactPath);
            Assert.That(artifact, Does.Contain("not-started"));
            Assert.That(artifact, Does.Contain("not-requested"));
        });
    }

    [Test]
    public void Failing_desktop_cleanup_is_recorded_without_preventing_later_cleanup()
    {
        using var fixture = Fixture.Create();
        var run = fixture.Invoke(
            "$failures = [System.Collections.Generic.List[object]]::new()\n" +
            "$script:laterCleanupRan = $false\n" +
            "Invoke-GatewaySmokeBestEffortCleanupAction -Failures $failures -Category 'desktop-cleanup' -Description 'disconnect' -Operation { throw 'disconnect failed' }\n" +
            "Invoke-GatewaySmokeBestEffortCleanupAction -Failures $failures -Category 'desktop-cleanup' -Description 'service stop' -Operation { $script:laterCleanupRan = $true }\n" +
            "Write-Output ([string]$failures.Count + '|' + [string]$script:laterCleanupRan + '|' + [string]$failures[0].Message)");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("1|True|disconnect failed: disconnect failed"));
        });
    }

    [Test]
    public void Uncertain_disconnect_is_accepted_only_after_disconnected_state_is_observed()
    {
        using var fixture = Fixture.Create();
        var run = fixture.Invoke(
            "$script:calls = [System.Collections.Generic.List[string]]::new()\n" +
            "$invoker = { param($Arguments) $script:calls.Add(($Arguments -join ' ')); if ($Arguments[0] -eq 'disconnect') { throw 'malformed response after disconnect' }; return [pscustomobject]@{ data = [pscustomobject]@{ connected = $false } } }\n" +
            "$result = Complete-GatewaySmokeFlaUiConnectionCleanup -JsonInvoker $invoker\n" +
            "Write-Output ($result.Status + '|' + ($script:calls -join ','))",
            "Complete-GatewaySmokeFlaUiConnectionCleanup");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("completed|disconnect,status"));
        });
    }

    [Test]
    public void Connection_cleanup_fails_when_disconnected_state_cannot_be_observed()
    {
        using var fixture = Fixture.Create();
        var run = fixture.Invoke(
            "$invoker = { param($Arguments) if ($Arguments[0] -eq 'disconnect') { throw 'disconnect timed out' }; return [pscustomobject]@{ data = [pscustomobject]@{ connected = $true } } }\n" +
            "Complete-GatewaySmokeFlaUiConnectionCleanup -JsonInvoker $invoker | Out-Null",
            "Complete-GatewaySmokeFlaUiConnectionCleanup");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("still connected"));
            Assert.That(run.StandardError, Does.Contain("disconnect timed out"));
        });
    }

    [Test]
    public void Uncertain_service_stop_is_accepted_only_after_stopped_state_is_observed()
    {
        using var fixture = Fixture.Create();
        var run = fixture.Invoke(
            "$script:stopAttempts = 0\n" +
            "$rawInvoker = { param($Arguments) $script:stopAttempts++; throw 'non-JSON response after stop' }\n" +
            "$jsonInvoker = { param($Arguments) return [pscustomobject]@{ running = $false } }\n" +
            "$result = Complete-GatewaySmokeFlaUiServiceCleanup -RawInvoker $rawInvoker -JsonInvoker $jsonInvoker -RetryDelayMilliseconds 1\n" +
            "Write-Output ($result.Status + '|' + [string]$script:stopAttempts)",
            "Complete-GatewaySmokeFlaUiServiceCleanup");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("completed|2"));
        });
    }

    [Test]
    public void Service_cleanup_fails_when_stopped_state_cannot_be_observed()
    {
        using var fixture = Fixture.Create();
        var run = fixture.Invoke(
            "$rawInvoker = { param($Arguments) throw 'stop timed out' }\n" +
            "$jsonInvoker = { param($Arguments) return [pscustomobject]@{ running = $true } }\n" +
            "Complete-GatewaySmokeFlaUiServiceCleanup -RawInvoker $rawInvoker -JsonInvoker $jsonInvoker -RetryDelayMilliseconds 1 | Out-Null",
            "Complete-GatewaySmokeFlaUiServiceCleanup");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("still running"));
            Assert.That(run.StandardError, Does.Contain("stop timed out"));
        });
    }

    [Test]
    public void Normal_config_hash_failure_is_aggregated_and_returns_control()
    {
        using var fixture = Fixture.Create();
        var run = fixture.Invoke(
            "$failures = [System.Collections.Generic.List[object]]::new()\n" +
            "function Get-OptionalFileHash { throw 'hash read failed' }\n" +
            "$read = Get-GatewaySmokeOptionalHashSafely -Path 'ignored' -Failures $failures\n" +
            "Write-Output ([string]$failures.Count + '|' + [string]$read.Succeeded + '|' + [string]$read.Hash + '|' + [string]$failures[0].Category)");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("1|False||primary"));
        });
    }

    [Test]
    public void Normal_config_hash_success_is_distinguished_from_a_missing_file_hash()
    {
        using var fixture = Fixture.Create();
        var run = fixture.Invoke(
            "$failures = [System.Collections.Generic.List[object]]::new()\n" +
            "function Get-OptionalFileHash { return $null }\n" +
            "$read = Get-GatewaySmokeOptionalHashSafely -Path 'ignored' -Failures $failures\n" +
            "Write-Output ([string]$failures.Count + '|' + [string]$read.Succeeded + '|' + [string]$read.Hash)");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("0|True|"));
        });
    }

    [Test]
    public void Completion_metadata_is_added_to_a_minimal_success_result_under_strict_mode()
    {
        using var fixture = Fixture.Create();
        var run = fixture.Invoke(
            "$result = [pscustomobject]@{ Status = 'passed' }\n" +
            "$result = Set-GatewaySmokeCompletionMetadata -Result $result -NormalConfigHashAfter 'AFTER' -IntegrationTestStageCleaned $true -CredentialsSanitized $true -CredentialCleanup 'credentials.json' -IntegrationTestStageCleanup 'stage.json' -CleanupStatus 'cleanup.json'\n" +
            "$result | ConvertTo-Json -Compress");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput, Does.Contain("\"NormalConfigHashAfter\":\"AFTER\""));
            Assert.That(run.StandardOutput, Does.Contain("\"IntegrationTestStageCleaned\":true"));
            Assert.That(run.StandardOutput, Does.Contain("\"CredentialsSanitized\":true"));
            Assert.That(run.StandardOutput, Does.Contain("\"CredentialCleanup\":\"credentials.json\""));
            Assert.That(run.StandardOutput, Does.Contain("\"IntegrationTestStageCleanup\":\"stage.json\""));
            Assert.That(run.StandardOutput, Does.Contain("\"CleanupStatus\":\"cleanup.json\""));
        });
    }

    [Test]
    public void Completion_metadata_preserves_a_null_normal_config_hash_and_updates_existing_fields()
    {
        using var fixture = Fixture.Create();
        var run = fixture.Invoke(
            "$original = [pscustomobject]@{ Status = 'passed'; NormalConfigHashAfter = 'BEFORE'; IntegrationTestStageCleaned = $false }\n" +
            "$result = Set-GatewaySmokeCompletionMetadata -Result $original -NormalConfigHashAfter $null -IntegrationTestStageCleaned $true -CredentialsSanitized $true -CredentialCleanup 'credentials.json' -IntegrationTestStageCleanup 'stage.json' -CleanupStatus 'cleanup.json'\n" +
            "Write-Output ([string][object]::ReferenceEquals($original, $result) + '|' + [string]($null -eq $result.NormalConfigHashAfter) + '|' + [string]$result.IntegrationTestStageCleaned)");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|True|True"));
        });
    }

    [Test]
    public void Completion_evidence_is_persisted_with_both_normal_config_hashes()
    {
        using var fixture = Fixture.Create();
        var run = fixture.Invoke(
            "$result = [pscustomobject]@{ Status = 'passed'; NormalConfigHashBefore = 'BEFORE'; NormalConfigHashAfter = 'AFTER' }\n" +
            "Write-GatewaySmokeEvidenceSummary -Result $result -Path $artifactPath -BearerToken 'not-present'\n" +
            "$saved = Get-Content -LiteralPath $artifactPath -Raw | ConvertFrom-Json\n" +
            "Write-Output ($saved.Status + '|' + $saved.NormalConfigHashBefore + '|' + $saved.NormalConfigHashAfter)");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("passed|BEFORE|AFTER"));
            Assert.That(File.Exists(fixture.ArtifactPath), Is.True);
        });
    }

    [Test]
    public void Completion_evidence_refuses_to_persist_the_bearer_token()
    {
        using var fixture = Fixture.Create();
        var run = fixture.Invoke(
            "$result = [pscustomobject]@{ Status = 'passed'; Message = 'prefix-sensitive-token-suffix' }\n" +
            "try { Write-GatewaySmokeEvidenceSummary -Result $result -Path $artifactPath -BearerToken 'sensitive-token'; throw 'Expected token rejection.' }\n" +
            "catch { if ($_.Exception.Message -eq 'Expected token rejection.') { throw }; Write-Output $_.Exception.Message }");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput, Does.Contain("bearer token"));
            Assert.That(File.Exists(fixture.ArtifactPath), Is.False);
        });
    }

    [Test]
    public void Main_smoke_wires_each_failure_category_and_explicit_component_artifact()
    {
        var source = File.ReadAllText(Path.Combine(
            FindSourceRepositoryRoot(),
            "scripts",
            "Invoke-GatewaySmoke.ps1"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("-Category 'primary'"));
            Assert.That(source, Does.Contain("-Category 'process-cleanup'"));
            Assert.That(source, Does.Contain("-Category 'credential-cleanup'"));
            Assert.That(source, Does.Contain("-Category 'stage-cleanup'"));
            Assert.That(source, Does.Contain("process-cleanup.json"));
            Assert.That(source, Does.Contain("credential-cleanup.json"));
            Assert.That(source, Does.Contain("integration-test-stage-cleanup.json"));
            Assert.That(source, Does.Contain("cleanup-status.json"));
            Assert.That(source, Does.Contain("Invoke-GatewaySmokeBestEffortCleanupAction"));
            Assert.That(source, Does.Contain("Get-GatewaySmokeOptionalHashSafely"));
        });
    }

    [Test]
    public void Main_smoke_marks_uncertain_fla_ui_side_effects_before_invocation_and_reconciles_them()
    {
        var source = File.ReadAllText(Path.Combine(
            FindSourceRepositoryRoot(),
            "scripts",
            "Invoke-GatewaySmoke.ps1"));

        var serviceAttempt = source.IndexOf("$flaUiServiceStartAttempted = $true", StringComparison.Ordinal);
        var serviceStart = source.IndexOf("Invoke-FlaUiJson -Arguments @('service', 'start')", StringComparison.Ordinal);
        var connectionAttempt = source.IndexOf("$flaUiConnectionAttempted = $true", StringComparison.Ordinal);
        var connect = source.IndexOf("Invoke-FlaUiJson -Arguments @('connect', '--pid'", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(serviceAttempt, Is.GreaterThanOrEqualTo(0));
            Assert.That(serviceStart, Is.GreaterThan(serviceAttempt));
            Assert.That(connectionAttempt, Is.GreaterThanOrEqualTo(0));
            Assert.That(connect, Is.GreaterThan(connectionAttempt));
            Assert.That(source, Does.Contain("if ($flaUiConnectionAttempted)"));
            Assert.That(source, Does.Contain("Complete-GatewaySmokeFlaUiConnectionCleanup"));
            Assert.That(source, Does.Contain("if ($flaUiServiceStartAttempted -and -not $flaUiServiceWasRunning)"));
            Assert.That(source, Does.Contain("Complete-GatewaySmokeFlaUiServiceCleanup"));
        });
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string root;
        private readonly string smokePath;

        private Fixture(string root, string smokePath)
        {
            this.root = root;
            this.smokePath = smokePath;
            ArtifactPath = Path.Combine(root, "cleanup-status.json");
        }

        public string ArtifactPath { get; }

        public static Fixture Create()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "GatewaySmokeCleanupAggregationTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new Fixture(
                root,
                Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1"));
        }

        public InvocationResult Invoke(string operation, params string[] additionalFunctionNames)
        {
            var invocationPath = Path.Combine(root, $"invoke-{Guid.NewGuid():N}.ps1");
            var functionNames = new[]
            {
                "Add-GatewaySmokeFailure",
                "New-GatewaySmokeCleanupStatus",
                "Invoke-GatewaySmokeBestEffortCleanupAction",
                "Get-GatewaySmokeOptionalHashSafely",
                "Set-GatewaySmokeCompletionMetadata",
                "Write-GatewaySmokeEvidenceSummary"
            }.Concat(additionalFunctionNames).Distinct(StringComparer.Ordinal).ToArray();
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "Set-StrictMode -Version Latest\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                $"$functionNames = @({string.Join(",", functionNames.Select(PowerShellLiteral))})\n" +
                "foreach ($functionName in $functionNames) {\n" +
                "  $functionAst = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $functionName }, $true)\n" +
                "  if ($null -eq $functionAst) { throw \"Function was not found: $functionName\" }\n" +
                "  Invoke-Expression $functionAst.Extent.Text\n" +
                "}\n" +
                $"$artifactPath = {PowerShellLiteral(ArtifactPath)}\n" +
                "try {\n" + operation + "\nexit 0\n}\n" +
                "catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));
            return RunPowerShell(invocationPath);
        }

        public void Dispose() => Directory.Delete(root, recursive: true);
    }

    private static InvocationResult RunPowerShell(string path)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{path}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start pwsh.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000))
        {
            process.Kill();
            throw new TimeoutException("Gateway cleanup aggregation probe timed out.");
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
