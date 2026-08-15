using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class GatewaySmokePackageEvidenceTests
{
    [Test]
    public void Managed_package_evidence_contains_exact_identity_length_and_sha256()
    {
        var root = Path.Combine(Path.GetTempPath(), "GatewaySmokePackageEvidenceTests", Guid.NewGuid().ToString("N"));
        var assemblies = Path.Combine(root, "Assemblies");
        Directory.CreateDirectory(assemblies);
        try
        {
            var source = typeof(GatewaySmokePackageEvidenceTests).Assembly.Location;
            var deployed = Path.Combine(assemblies, "Gateway.Evidence.Fixture.dll");
            File.Copy(source, deployed);
            var expectedHash = Hash(deployed);
            var expectedIdentity = AssemblyName.GetAssemblyName(deployed).FullName;
            var expectedLength = new FileInfo(deployed).Length;

            var run = Invoke(
                root,
                assemblies,
                "$records = @(Get-GatewaySmokeAssemblyEvidence -AssembliesPath $assembliesPath -RequiredAssemblies @('Gateway.Evidence.Fixture.dll'))\n" +
                "[pscustomobject]@{ Records = $records } | ConvertTo-Json -Compress -Depth 5");

            Assert.Multiple(() =>
            {
                Assert.That(run.ExitCode, Is.Zero, run.StandardError);
                Assert.That(run.StandardOutput, Does.Contain("\"FileName\":\"Gateway.Evidence.Fixture.dll\""));
                Assert.That(run.StandardOutput, Does.Contain("\"AssemblyIdentity\":\"" + expectedIdentity + "\""));
                Assert.That(run.StandardOutput, Does.Contain("\"Length\":" + expectedLength));
                Assert.That(run.StandardOutput, Does.Contain("\"Sha256\":\"" + expectedHash + "\""));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static InvocationResult Invoke(string root, string assembliesPath, string operation)
    {
        var smokePath = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
        var invocationPath = Path.Combine(root, "invoke.ps1");
        var invocation =
            "$ErrorActionPreference = 'Stop'\n" +
            "Set-StrictMode -Version Latest\n" +
            "$tokens = $null; $parseErrors = $null\n" +
            $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
            "$functionNames = @('Get-GatewaySmokeFileSha256','Get-GatewaySmokeAssemblyEvidence')\n" +
            "foreach ($functionName in $functionNames) { $functionAst = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $functionName }, $true); if ($null -eq $functionAst) { throw ('Function was not found: ' + $functionName) }; Invoke-Expression $functionAst.Extent.Text }\n" +
            $"$assembliesPath = {PowerShellLiteral(assembliesPath)}\n" +
            "try {\n" + operation + "\nexit 0\n}\n" +
            "catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }\n";
        File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));
        return RunPowerShell(invocationPath);
    }

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("X2")));
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
            throw new TimeoutException("Gateway package-evidence probe timed out.");
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
