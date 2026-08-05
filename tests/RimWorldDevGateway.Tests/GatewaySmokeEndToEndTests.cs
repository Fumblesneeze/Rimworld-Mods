using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class GatewaySmokeEndToEndTests
{
    [Test]
    public void End_to_end_launch_is_explicit_pre_staged_and_terminally_verified()
    {
        var source = File.ReadAllText(SmokePath());

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("[switch]$RunEndToEndTests"));
            Assert.That(source, Does.Contain("[switch]$SkipBuildDeploy"));
            Assert.That(source, Does.Contain("-devGatewayRunEndToEndTests"));
            Assert.That(source, Does.Contain("$baseUrl/end-to-end-tests"));
            Assert.That(source, Does.Contain("$endToEndTestsEnvelope.result.Execution.IsTerminal"));
            Assert.That(source, Does.Contain("[string]$_.Status -ne 'passed'"));
            Assert.That(source, Does.Contain("EndToEndTestsPersisted"));
        });
    }

    [Test]
    public void End_to_end_mode_rejects_a_non_quicktest_launch_before_path_validation()
    {
        var run = Invoke("-RunEndToEndTests");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("requires -Quicktest"));
        });
    }

    private static InvocationResult Invoke(params string[] arguments)
    {
        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "GatewaySmokeEndToEndTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var invocationPath = Path.Combine(temporaryRoot, "invoke.ps1");
            var invocation =
                $"& '{SmokePath().Replace("'", "''")}' {string.Join(" ", arguments)} " +
                $"-RimWorldPath '{Path.Combine(temporaryRoot, "missing-game").Replace("'", "''")}' " +
                $"-SteamModContentFolder '{Path.Combine(temporaryRoot, "missing-workshop").Replace("'", "''")}'" +
                Environment.NewLine +
                "exit $LASTEXITCODE" + Environment.NewLine;
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));
            var startInfo = new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{invocationPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo) ??
                                throw new InvalidOperationException("Could not start pwsh.");
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000))
            {
                process.Kill();
                throw new TimeoutException("Gateway smoke E2E validation probe timed out.");
            }

            Task.WaitAll(output, error);
            return new InvocationResult(process.ExitCode, output.Result, error.Result);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static string SmokePath() => Path.Combine(
        FindSourceRepositoryRoot(),
        "scripts",
        "Invoke-GatewaySmoke.ps1");

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
