using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class GatewaySmokeStartupPollingTests
{
    [Test]
    public void Exact_queue_timeout_is_retryable_until_startup_status_succeeds()
    {
        using var fixture = Fixture.Create();
        const string queued =
            "{\"ok\":false,\"error\":{\"code\":\"timed_out_before_start\",\"retryable\":true}}";
        const string ready =
            "{\"ok\":true,\"result\":{\"developerOnly\":true,\"unrestrictedExecutionEnabled\":true}}";

        var queuedRun = fixture.Invoke(504, queued);
        var readyRun = fixture.Invoke(200, ready);

        Assert.Multiple(() =>
        {
            Assert.That(queuedRun.ExitCode, Is.Zero, queuedRun.StandardError);
            Assert.That(queuedRun.StandardOutput.Trim(), Is.EqualTo("retry"));
            Assert.That(readyRun.ExitCode, Is.Zero, readyRun.StandardError);
            Assert.That(readyRun.StandardOutput.Trim(), Is.EqualTo("ready"));
        });
    }

    [TestCase(504, "{\"ok\":false,\"error\":{\"code\":\"timed_out_before_start\",\"retryable\":false}}")]
    [TestCase(504, "{\"ok\":false,\"error\":{\"code\":\"TIMED_OUT_BEFORE_START\",\"retryable\":true}}")]
    [TestCase(503, "{\"ok\":false,\"error\":{\"code\":\"timed_out_before_start\",\"retryable\":true}}")]
    [TestCase(200, "{\"ok\":false,\"error\":{\"code\":\"timed_out_before_start\",\"retryable\":true}}")]
    public void Non_exact_startup_response_is_fatal(int statusCode, string content)
    {
        using var fixture = Fixture.Create();

        var run = fixture.Invoke(statusCode, content);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain($"HTTP {statusCode}"));
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
        }

        public static Fixture Create()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "GatewaySmokeStartupPollingTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new Fixture(
                root,
                Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1"));
        }

        public InvocationResult Invoke(int statusCode, string content)
        {
            var responsePath = Path.Combine(root, $"response-{Guid.NewGuid():N}.json");
            var invocationPath = Path.Combine(root, $"invoke-{Guid.NewGuid():N}.ps1");
            File.WriteAllText(responsePath, content, new UTF8Encoding(false));
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "Set-StrictMode -Version Latest\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                "$functionAst = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Resolve-GatewayStartupStatusPollResponse' }, $true)\n" +
                "if ($null -eq $functionAst) { [Console]::Error.WriteLine('Function was not found.'); exit 91 }\n" +
                "Invoke-Expression $functionAst.Extent.Text\n" +
                $"$content = [System.IO.File]::ReadAllText({PowerShellLiteral(responsePath)})\n" +
                $"$response = [pscustomobject]@{{ StatusCode = {statusCode}; Content = $content }}\n" +
                "try {\n" +
                "  $result = Resolve-GatewayStartupStatusPollResponse -Response $response\n" +
                "  [Console]::Out.WriteLine($result)\n" +
                "  exit 0\n" +
                "}\n" +
                "catch {\n" +
                "  [Console]::Error.WriteLine($_.Exception.Message)\n" +
                "  exit 1\n" +
                "}\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File {Quote(invocationPath)}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }) ?? throw new InvalidOperationException("Could not start PowerShell.");
            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return new InvocationResult(process.ExitCode, standardOutput, standardError);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch
            {
                // Test cleanup is best effort.
            }
        }

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

            throw new InvalidOperationException("Could not locate repository root.");
        }

        private static string PowerShellLiteral(string value) =>
            "'" + value.Replace("'", "''") + "'";

        private static string Quote(string value) =>
            "\"" + value.Replace("\"", "\\\"") + "\"";
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
