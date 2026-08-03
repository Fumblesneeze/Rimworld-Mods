using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using RimWorldDevGateway.Contracts;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewaySmokeShutdownTombstoneTests
{
    [Test]
    public async Task Shutdown_wait_waits_for_the_stopped_tombstone_after_the_live_locator_disappears()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            nameof(GatewaySmokeShutdownTombstoneTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var currentPath = Path.Combine(root, "current.json");
            var tombstonePath = Path.Combine(root, "session.json");
            var active = Manifest("active", "secret-token");
            File.WriteAllText(currentPath, GatewayContractJson.Write(active));
            File.WriteAllText(tombstonePath, GatewayContractJson.Write(active));

            var transition = Task.Run(async () =>
            {
                await Task.Delay(100);
                File.Delete(currentPath);
                await Task.Delay(200);
                File.WriteAllText(
                    tombstonePath,
                    GatewayContractJson.Write(Manifest("stopped", null)));
            });

            var invocation = Path.Combine(root, "invoke.ps1");
            var smoke = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
            File.WriteAllText(
                invocation,
                "$ErrorActionPreference = 'Stop'\nSet-StrictMode -Version Latest\n" +
                "$tokens = $null; $errors = $null\n" +
                $"$ast = [Management.Automation.Language.Parser]::ParseFile({Literal(smoke)}, [ref]$tokens, [ref]$errors)\n" +
                "$functionAst = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Wait-GatewayStoppedTombstone' }, $true)\n" +
                "if ($null -eq $functionAst) { throw 'Stopped-tombstone wait helper was not found.' }\n" +
                "Invoke-Expression $functionAst.Extent.Text\n" +
                $"$result = Wait-GatewayStoppedTombstone -LiveManifestPath {Literal(currentPath)} -TombstonePath {Literal(tombstonePath)} -TimeoutMilliseconds 2000\n" +
                "$result | Select-Object state, token | ConvertTo-Json -Compress\n",
                new UTF8Encoding(false));

            var run = RunPowerShell(invocation);
            await transition;

            Assert.Multiple(() =>
            {
                Assert.That(run.ExitCode, Is.Zero, run.StandardError);
                Assert.That(run.StandardOutput, Does.Contain("\"state\":\"stopped\""));
                Assert.That(run.StandardOutput, Does.Not.Contain("secret-token"));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static GatewaySessionManifest Manifest(string state, string? token)
    {
        return new GatewaySessionManifest
        {
            ApiVersion = "1",
            RunId = "test-run",
            State = state,
            BaseUrl = state == "active" ? "http://127.0.0.1:40123/api/v1" : null,
            Token = token,
            ProcessId = 1234,
            ProcessStartUtc = "2026-08-03T09:00:00.0000000+00:00",
            StartedUtc = "2026-08-03T09:00:01.0000000+00:00",
            StoppedUtc = state == "stopped" ? "2026-08-03T09:00:02.0000000+00:00" : null,
            GameVersion = "1.6",
            ModVersion = "0.1",
            UnrestrictedExecution = true
        };
    }

    private static InvocationResult RunPowerShell(string path)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "pwsh",
                Arguments = $"-NoProfile -NonInteractive -File \"{path}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new InvocationResult(process.ExitCode, output, error);
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

        throw new DirectoryNotFoundException("Could not find the source repository root.");
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
