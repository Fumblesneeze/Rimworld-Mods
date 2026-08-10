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
public sealed class GatewaySmokePackageIdCliTests
{
    [Test]
    public void Active_order_honors_harmony_load_before_and_keeps_core_first_when_harmony_is_absent()
    {
        var repositoryRoot = FindSourceRepositoryRoot();
        var smokePath = Path.Combine(repositoryRoot, "scripts", "Invoke-GatewaySmoke.ps1");
        var temporaryRoot = Path.Combine(Path.GetTempPath(), "GatewaySmokePackageIdCliTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var probePath = Path.Combine(temporaryRoot, "probe.ps1");
            var probe =
                $"$tokens = $null; $errors = $null; $ast = [Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$errors)" + Environment.NewLine +
                "$functionAst = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Get-GatewayActiveModIds' }, $true)" + Environment.NewLine +
                "if ($null -eq $functionAst) { throw 'Get-GatewayActiveModIds was not found.' }" + Environment.NewLine +
                ". ([scriptblock]::Create($functionAst.Extent.Text))" + Environment.NewLine +
                "$withHarmony = @(Get-GatewayActiveModIds -AdditionalPackageIds @('optional.one','BRRAINZ.HARMONY','optional.two'))" + Environment.NewLine +
                "$withoutHarmony = @(Get-GatewayActiveModIds -AdditionalPackageIds @('optional.one','optional.two'))" + Environment.NewLine +
                "[pscustomobject]@{ withHarmony = $withHarmony; withoutHarmony = $withoutHarmony } | ConvertTo-Json -Compress" + Environment.NewLine;
            File.WriteAllText(probePath, probe, new UTF8Encoding(false));
            var run = InvokePowerShell(probePath);

            Assert.Multiple(() =>
            {
                Assert.That(run.ExitCode, Is.Zero, run.StandardError);
                Assert.That(run.StandardOutput.Trim(), Is.EqualTo(
                    "{\"withHarmony\":[\"brrainz.harmony\",\"ludeon.rimworld\",\"optional.one\",\"optional.two\",\"fumblesneeze.rimworlddevgateway\"]," +
                    "\"withoutHarmony\":[\"ludeon.rimworld\",\"optional.one\",\"optional.two\",\"fumblesneeze.rimworlddevgateway\"]}"));
            });
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [TestCase("Example.Mod", "example.mod")]
    [TestCase("LUDEON.RIMWORLD", "ludeon.rimworld")]
    [TestCase("FUMBLESNEEZE.RIMWORLDDEVGATEWAY", "fumblesneeze.rimworlddevgateway")]
    public void Active_package_ids_reject_case_insensitive_duplicates_before_path_validation(
        string additionalPackageId,
        string collidingPackageId)
    {
        var additionalIds = additionalPackageId.StartsWith("Example", StringComparison.Ordinal)
            ? new[] { additionalPackageId, collidingPackageId }
            : new[] { additionalPackageId };
        var run = Invoke(additionalIds);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("Duplicate active mod package ID"));
        });
    }

    [Test]
    public void Additional_package_id_file_uses_the_same_case_insensitive_validation()
    {
        var run = InvokeFromFile(new[] { "Example.Mod", "example.mod" });

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("Duplicate active mod package ID"));
        });
    }

    private static InvocationResult Invoke(string[] additionalIds)
    {
        var repositoryRoot = FindSourceRepositoryRoot();
        var smokePath = Path.Combine(repositoryRoot, "scripts", "Invoke-GatewaySmoke.ps1");
        var temporaryRoot = Path.Combine(Path.GetTempPath(), "GatewaySmokePackageIdCliTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var invocationPath = Path.Combine(temporaryRoot, "invoke.ps1");
            var literals = string.Join(",", additionalIds.Select(PowerShellLiteral));
            var invocation =
                "$parameters = @{" + Environment.NewLine +
                $"  RimWorldPath = {PowerShellLiteral(Path.Combine(temporaryRoot, "missing-game"))}" + Environment.NewLine +
                $"  SteamModContentFolder = {PowerShellLiteral(Path.Combine(temporaryRoot, "missing-workshop"))}" + Environment.NewLine +
                $"  AdditionalModIds = @({literals})" + Environment.NewLine +
                "  DryRun = $true" + Environment.NewLine +
                "}" + Environment.NewLine +
                $"& {PowerShellLiteral(smokePath)} @parameters" + Environment.NewLine +
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
            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start pwsh.");
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000))
            {
                process.Kill();
                throw new TimeoutException("Gateway smoke package-ID probe timed out.");
            }

            Task.WaitAll(output, error);
            return new InvocationResult(process.ExitCode, output.Result, error.Result);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static InvocationResult InvokeFromFile(string[] additionalIds)
    {
        var repositoryRoot = FindSourceRepositoryRoot();
        var smokePath = Path.Combine(repositoryRoot, "scripts", "Invoke-GatewaySmoke.ps1");
        var temporaryRoot = Path.Combine(Path.GetTempPath(), "GatewaySmokePackageIdCliTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var packageFile = Path.Combine(temporaryRoot, "additional-mod-ids.txt");
            File.WriteAllLines(packageFile, additionalIds, new UTF8Encoding(false));
            var invocationPath = Path.Combine(temporaryRoot, "invoke.ps1");
            var invocation =
                "$parameters = @{" + Environment.NewLine +
                $"  RimWorldPath = {PowerShellLiteral(Path.Combine(temporaryRoot, "missing-game"))}" + Environment.NewLine +
                $"  SteamModContentFolder = {PowerShellLiteral(Path.Combine(temporaryRoot, "missing-workshop"))}" + Environment.NewLine +
                $"  AdditionalModIdsFile = {PowerShellLiteral(packageFile)}" + Environment.NewLine +
                "  DryRun = $true" + Environment.NewLine +
                "}" + Environment.NewLine +
                $"& {PowerShellLiteral(smokePath)} @parameters" + Environment.NewLine +
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
            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start pwsh.");
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000))
            {
                process.Kill();
                throw new TimeoutException("Gateway smoke package-ID file probe timed out.");
            }

            Task.WaitAll(output, error);
            return new InvocationResult(process.ExitCode, output.Result, error.Result);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static InvocationResult InvokePowerShell(string scriptPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{scriptPath}\"",
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
            throw new TimeoutException("Gateway smoke package-order probe timed out.");
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
