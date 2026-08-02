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
public sealed class GatewaySmokeStagePlanTests
{
    [Test]
    public void Dry_run_plan_registers_every_exact_owner_stage_before_publication()
    {
        using var fixture = Fixture.Create();
        var run = fixture.Invoke(
            "$records = [System.Collections.Generic.List[object]]::new()\n" +
            "$plan = Get-Content -LiteralPath $planPath -Raw | ConvertFrom-Json\n" +
            "Register-IntegrationTestStageCandidates -PlanResult $plan -ActivePackageIds @('ludeon.rimworld','alpha.mod','zeta.mod','fumblesneeze.rimworlddevgateway') -ModsRoot $modsRoot -Version '1.6' -Records $records\n" +
            "$records | ForEach-Object { Write-Output ([string]$_.OwnerPackageId + '|' + [string]$_.StagePath) }");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput, Does.Contain("alpha.mod"));
            Assert.That(run.StandardOutput, Does.Contain("zeta.mod"));
            Assert.That(run.StandardOutput, Does.Contain(Path.Combine("alpha.mod", "1.6", "DevIntegrationTests")));
            Assert.That(run.StandardOutput, Does.Contain(Path.Combine("zeta.mod", "1.6", "DevIntegrationTests")));
        });
    }

    [Test]
    public void Published_result_must_match_the_pre_registered_plan_exactly()
    {
        using var fixture = Fixture.Create();
        var accepted = fixture.Invoke(
            "$plan = Get-Content -LiteralPath $planPath -Raw | ConvertFrom-Json\n" +
            "$actual = Get-Content -LiteralPath $actualPath -Raw | ConvertFrom-Json\n" +
            "Assert-IntegrationTestBundleMatchesPlan -PlanResult $plan -ActualResult $actual");
        fixture.ReplaceActualOwner("unexpected.mod");
        var rejected = fixture.Invoke(
            "$plan = Get-Content -LiteralPath $planPath -Raw | ConvertFrom-Json\n" +
            "$actual = Get-Content -LiteralPath $actualPath -Raw | ConvertFrom-Json\n" +
            "Assert-IntegrationTestBundleMatchesPlan -PlanResult $plan -ActualResult $actual");

        Assert.Multiple(() =>
        {
            Assert.That(accepted.ExitCode, Is.Zero, accepted.StandardError);
            Assert.That(rejected.ExitCode, Is.EqualTo(1));
            Assert.That(rejected.StandardError, Does.Contain("does not match its pre-registered dry-run plan"));
        });
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string root;
        private readonly string smokePath;
        private readonly string planPath;
        private readonly string actualPath;
        private readonly string modsRoot;

        private Fixture(string root, string smokePath, string planPath, string actualPath, string modsRoot)
        {
            this.root = root;
            this.smokePath = smokePath;
            this.planPath = planPath;
            this.actualPath = actualPath;
            this.modsRoot = modsRoot;
        }

        public static Fixture Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "GatewaySmokeStagePlanTests", Guid.NewGuid().ToString("N"));
            var modsRoot = Path.Combine(root, "RimWorld", "Mods");
            Directory.CreateDirectory(modsRoot);
            var planPath = Path.Combine(root, "plan.json");
            var actualPath = Path.Combine(root, "actual.json");
            var projects = new[]
            {
                Project("alpha.mod", "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj", modsRoot, includeBundle: false),
                Project("zeta.mod", "tests/Zeta.IntegrationTests/Zeta.IntegrationTests.csproj", modsRoot, includeBundle: false)
            };
            var actualProjects = new[]
            {
                Project("alpha.mod", "tests/Alpha.IntegrationTests/Alpha.IntegrationTests.csproj", modsRoot, includeBundle: true),
                Project("zeta.mod", "tests/Zeta.IntegrationTests/Zeta.IntegrationTests.csproj", modsRoot, includeBundle: true)
            };
            File.WriteAllText(planPath, Envelope("planned", true, projects), new UTF8Encoding(false));
            File.WriteAllText(actualPath, Envelope("staged", false, actualProjects), new UTF8Encoding(false));
            return new Fixture(root, Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1"), planPath, actualPath, modsRoot);
        }

        public void ReplaceActualOwner(string owner)
        {
            var text = File.ReadAllText(actualPath);
            File.WriteAllText(actualPath, text.Replace("zeta.mod", owner), new UTF8Encoding(false));
        }

        public InvocationResult Invoke(string operation)
        {
            var invocationPath = Path.Combine(root, $"invoke-{Guid.NewGuid():N}.ps1");
            var functions = new[]
            {
                "Test-GatewayPackageId",
                "Assert-IntegrationTestStagePath",
                "Register-IntegrationTestStageCandidates",
                "Assert-IntegrationTestBundleMatchesPlan"
            };
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                $"$functionNames = @({string.Join(",", functions.Select(PowerShellLiteral))})\n" +
                "foreach ($functionName in $functionNames) {\n" +
                "  $functionAst = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $functionName }, $true)\n" +
                "  if ($null -eq $functionAst) { throw \"Function was not found: $functionName\" }\n" +
                "  Invoke-Expression $functionAst.Extent.Text\n" +
                "}\n" +
                $"$planPath = {PowerShellLiteral(planPath)}\n" +
                $"$actualPath = {PowerShellLiteral(actualPath)}\n" +
                $"$modsRoot = {PowerShellLiteral(modsRoot)}\n" +
                "try {\n" + operation + "\nexit 0\n}\n" +
                "catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));
            return RunPowerShell(invocationPath);
        }

        public void Dispose() => Directory.Delete(root, recursive: true);

        private static string Project(string owner, string project, string modsRoot, bool includeBundle)
        {
            var destination = Path.Combine(modsRoot, owner, "1.6", "DevIntegrationTests").Replace("\\", "\\\\");
            var bundle = includeBundle
                ? ",\"Assembly\":\"Fixture.IntegrationTests.dll\",\"Manifest\":\"Fixture.IntegrationTests.integrationtests.json\""
                : ",\"Assembly\":null,\"Manifest\":null";
            return $"{{\"OwnerPackageId\":\"{owner}\",\"Project\":\"{project}\"{bundle},\"Destination\":\"{destination}\"}}";
        }

        private static string Envelope(string status, bool dryRun, string[] projects) =>
            $"{{\"Status\":\"{status}\",\"DryRun\":{dryRun.ToString().ToLowerInvariant()},\"ProjectCount\":{projects.Length},\"Projects\":[{string.Join(",", projects)}]}}";
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
            throw new TimeoutException("Gateway smoke stage-plan probe timed out.");
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
