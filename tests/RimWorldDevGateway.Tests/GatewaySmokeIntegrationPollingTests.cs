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
public sealed class GatewaySmokeIntegrationPollingTests
{
    [Test]
    public void Exact_initial_artifact_pending_response_is_retained_and_retryable_before_success()
    {
        using var fixture = Fixture.Create();
        const string pending =
            "{\"ok\":false,\"error\":{\"code\":\"integration_test_status_pending\",\"message\":\"Initial artifact commit pending.\",\"retryable\":true}}";
        const string ready = "{\"ok\":true,\"result\":{\"DiscoveryState\":\"discovering\"}}";

        var pendingRun = fixture.Invoke(503, pending);
        var readyRun = fixture.Invoke(200, ready);

        Assert.Multiple(() =>
        {
            Assert.That(pendingRun.ExitCode, Is.Zero, pendingRun.StandardError);
            Assert.That(pendingRun.StandardOutput.Trim(), Is.EqualTo("pending"));
            Assert.That(File.ReadAllText(fixture.PendingArtifactPath), Is.EqualTo(pending));
            Assert.That(readyRun.ExitCode, Is.Zero, readyRun.StandardError);
            Assert.That(readyRun.StandardOutput.Trim(), Is.EqualTo("ready"));
            Assert.That(File.ReadAllText(fixture.ReadyArtifactPath), Is.EqualTo(ready));
            Assert.That(File.ReadAllText(fixture.PendingArtifactPath), Is.EqualTo(pending));
        });
    }

    [TestCase("{\"ok\":false,\"error\":{\"code\":\"integration_test_status_pending\"}}")]
    [TestCase("{\"ok\":false,\"error\":{\"code\":\"integration_test_status_pending\",\"retryable\":false}}")]
    [TestCase("{\"ok\":false,\"error\":{\"code\":\"integration_test_status_pending\",\"retryable\":\"true\"}}")]
    [TestCase("{\"ok\":false,\"error\":{\"code\":\"integration_test_status_pending\",\"Retryable\":true}}")]
    public void Pending_retryability_must_be_the_exact_case_sensitive_boolean_true(string content)
    {
        using var fixture = Fixture.Create();

        var run = fixture.Invoke(503, content);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("HTTP 503"));
            Assert.That(File.ReadAllText(fixture.ReadyArtifactPath), Is.EqualTo(content));
            Assert.That(File.Exists(fixture.PendingArtifactPath), Is.False);
        });
    }

    [TestCase(500, false, "integration_test_status_pending")]
    [TestCase(503, false, "another_error")]
    [TestCase(503, true, "integration_test_status_pending")]
    [TestCase(200, false, "integration_test_status_pending")]
    [TestCase(503, false, "INTEGRATION_TEST_STATUS_PENDING")]
    public void Every_non_exact_pending_error_remains_immediately_fatal(
        int statusCode,
        bool ok,
        string errorCode)
    {
        using var fixture = Fixture.Create();
        var content =
            $"{{\"ok\":{ok.ToString().ToLowerInvariant()},\"error\":{{\"code\":\"{errorCode}\",\"message\":\"fatal\"}}}}";

        var run = fixture.Invoke(statusCode, content);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain($"HTTP {statusCode}"));
            Assert.That(File.ReadAllText(fixture.ReadyArtifactPath), Is.EqualTo(content));
            Assert.That(File.Exists(fixture.PendingArtifactPath), Is.False);
        });
    }

    [Test]
    public void Malformed_response_is_retained_and_fatal()
    {
        using var fixture = Fixture.Create();
        const string malformed = "{not-json";

        var run = fixture.Invoke(503, malformed);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("invalid JSON"));
            Assert.That(File.ReadAllText(fixture.ReadyArtifactPath), Is.EqualTo(malformed));
            Assert.That(File.Exists(fixture.PendingArtifactPath), Is.False);
        });
    }

    [Test]
    public void Pending_response_after_any_ready_response_is_retained_but_fatal()
    {
        using var fixture = Fixture.Create();
        const string pending =
            "{\"ok\":false,\"error\":{\"code\":\"integration_test_status_pending\",\"message\":\"Initial artifact commit pending.\",\"retryable\":true}}";

        var run = fixture.Invoke(503, pending, readyAlreadyObserved: true);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("regressed"));
            Assert.That(File.ReadAllText(fixture.PendingArtifactPath), Is.EqualTo(pending));
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
            ReadyArtifactPath = Path.Combine(root, "integration-tests.json");
            PendingArtifactPath = Path.Combine(root, "integration-tests-pending.json");
        }

        public string ReadyArtifactPath { get; }
        public string PendingArtifactPath { get; }

        public static Fixture Create()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "GatewaySmokeIntegrationPollingTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new Fixture(
                root,
                Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1"));
        }

        public InvocationResult Invoke(int statusCode, string content, bool readyAlreadyObserved = false)
        {
            var responsePath = Path.Combine(root, $"response-{Guid.NewGuid():N}.json");
            var invocationPath = Path.Combine(root, $"invoke-{Guid.NewGuid():N}.ps1");
            File.WriteAllText(responsePath, content, new UTF8Encoding(false));
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "Set-StrictMode -Version Latest\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                "$functionAst = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Resolve-IntegrationTestPollResponse' }, $true)\n" +
                "if ($null -eq $functionAst) { [Console]::Error.WriteLine('Function was not found.'); exit 91 }\n" +
                "Invoke-Expression $functionAst.Extent.Text\n" +
                $"$content = [System.IO.File]::ReadAllText({PowerShellLiteral(responsePath)})\n" +
                $"$response = [pscustomobject]@{{ StatusCode = {statusCode}; Content = $content }}\n" +
                "try {\n" +
                $"  $result = Resolve-IntegrationTestPollResponse -Response $response -ReadyArtifactPath {PowerShellLiteral(ReadyArtifactPath)} -PendingArtifactPath {PowerShellLiteral(PendingArtifactPath)} -Operation 'In-game integration-test status' -ReadyAlreadyObserved:${readyAlreadyObserved.ToString().ToLowerInvariant()}\n" +
                "  Write-Output $result.State\n" +
                "  exit 0\n" +
                "}\n" +
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
            throw new TimeoutException("Gateway integration polling probe timed out.");
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
