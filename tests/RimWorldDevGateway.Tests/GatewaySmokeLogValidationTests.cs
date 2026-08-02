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
public sealed class GatewaySmokeLogValidationTests
{
    [TestCase("Type Example.MissingDef is not a Def type or could not be found, in file Stale.xml", 1)]
    [TestCase("System.TypeLoadException: Could not load type Example", 1)]
    [TestCase("Failed Allocations. Bucket layout: 16B", 0)]
    public void Runtime_log_validation_classifies_load_errors_without_matching_allocator_diagnostics(
        string line,
        int expectedCount)
    {
        using var fixture = Fixture.Create(line);

        var run = fixture.Invoke();

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo(expectedCount.ToString()));
        });
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string root;
        private readonly string smokePath;
        private readonly string logPath;

        private Fixture(string root, string smokePath, string logPath)
        {
            this.root = root;
            this.smokePath = smokePath;
            this.logPath = logPath;
        }

        public static Fixture Create(string line)
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "GatewaySmokeLogValidationTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var logPath = Path.Combine(root, "Player.log");
            File.WriteAllText(logPath, line + Environment.NewLine, new UTF8Encoding(false));
            return new Fixture(
                root,
                Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1"),
                logPath);
        }

        public InvocationResult Invoke()
        {
            var invocationPath = Path.Combine(root, "invoke.ps1");
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "Set-StrictMode -Version Latest\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                "$functionAst = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Get-GatewayRuntimeLogErrors' }, $true)\n" +
                "if ($null -eq $functionAst) { [Console]::Error.WriteLine('Function was not found: Get-GatewayRuntimeLogErrors'); exit 91 }\n" +
                "Invoke-Expression $functionAst.Extent.Text\n" +
                $"$errors = @(Get-GatewayRuntimeLogErrors -Path {PowerShellLiteral(logPath)})\n" +
                "Write-Output $errors.Count\n";
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
            throw new TimeoutException("Gateway log-validation probe timed out.");
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
