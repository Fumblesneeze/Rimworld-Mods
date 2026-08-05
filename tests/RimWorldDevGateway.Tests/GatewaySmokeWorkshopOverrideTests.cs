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
public sealed class GatewaySmokeWorkshopOverrideTests
{
    [Test]
    public void Duplicate_package_selects_the_only_candidate_supporting_the_current_game()
    {
        using var fixture = Fixture.Create();
        fixture.AddWorkshopMod("100", "syrchalis.processor.framework", "1.3", "1.4");
        var expected = fixture.AddWorkshopMod(
            "200",
            "syrchalis.processor.framework",
            "1.3",
            "1.4",
            "1.5",
            "1.6");

        var run = fixture.Invoke(
            "$plans = @(Resolve-GatewaySmokeWorkshopOverridePlans " +
            "-WorkshopPath $workshopPath -ModsRoot $modsRoot " +
            "-PackageIds @('syrchalis.processor.framework') -Version '1.6' -RunId 'test-run')\n" +
            "$plans | ForEach-Object { Write-Output ($_.PackageId + '|' + $_.WorkshopItemId + '|' + $_.SourcePath + '|' + $_.LocalPath) }");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput, Does.Contain("syrchalis.processor.framework|200|"));
            Assert.That(run.StandardOutput, Does.Contain(expected));
            Assert.That(run.StandardOutput, Does.Contain(fixture.ModsRoot));
        });
    }

    [Test]
    public void Multiple_current_candidates_fail_instead_of_selecting_an_arbitrary_binary()
    {
        using var fixture = Fixture.Create();
        fixture.AddWorkshopMod("100", "duplicate.current", "1.6");
        fixture.AddWorkshopMod("200", "duplicate.current", "1.6");

        var run = fixture.Invoke(
            "$null = Resolve-GatewaySmokeWorkshopOverridePlans " +
            "-WorkshopPath $workshopPath -ModsRoot $modsRoot " +
            "-PackageIds @('duplicate.current') -Version '1.6' -RunId 'test-run'");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("multiple Workshop copies support RimWorld 1.6"));
            Assert.That(run.StandardError, Does.Contain("100").And.Contain("200"));
        });
    }

    [Test]
    public void Owned_override_is_a_temporary_physical_copy_and_cleanup_never_changes_the_source()
    {
        using var fixture = Fixture.Create();
        var source = fixture.AddWorkshopMod("200", "duplicate.current", "1.6");
        fixture.AddWorkshopMod("100", "duplicate.current", "1.4");
        var sentinel = Path.Combine(source, "source-sentinel.txt");
        File.WriteAllText(sentinel, "untouched", new UTF8Encoding(false));

        var run = fixture.Invoke(
            "$plan = @(Resolve-GatewaySmokeWorkshopOverridePlans " +
            "-WorkshopPath $workshopPath -ModsRoot $modsRoot " +
            "-PackageIds @('duplicate.current') -Version '1.6' -RunId 'test-run')[0]\n" +
            "$record = Publish-GatewaySmokeWorkshopOverride -Plan $plan -WorkshopPath $workshopPath -ModsRoot $modsRoot\n" +
            "$created = (Test-Path -LiteralPath $record.LocalPath -PathType Container) -and " +
            "(Test-Path -LiteralPath $record.MarkerPath -PathType Leaf) -and " +
            "(Test-Path -LiteralPath $record.ContentMarkerPath -PathType Leaf)\n" +
            "$physical = (((Get-Item -LiteralPath $record.LocalPath -Force).Attributes -band " +
            "[System.IO.FileAttributes]::ReparsePoint) -eq 0)\n" +
            "Remove-GatewaySmokeWorkshopOverride -Record $record -WorkshopPath $workshopPath -ModsRoot $modsRoot\n" +
            "$removed = -not (Test-Path -LiteralPath $record.LocalPath) -and " +
            "-not (Test-Path -LiteralPath $record.MarkerPath)\n" +
            "Write-Output ($created.ToString() + '|' + $physical.ToString() + '|' + $removed.ToString())");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|True|True"));
            Assert.That(File.ReadAllText(sentinel), Is.EqualTo("untouched"));
            Assert.That(Directory.Exists(source), Is.True);
        });
    }

    [Test]
    public void Cleanup_refuses_recursive_deletion_when_the_copy_ownership_marker_is_missing()
    {
        using var fixture = Fixture.Create();
        fixture.AddWorkshopMod("100", "duplicate.current", "1.4");
        var source = fixture.AddWorkshopMod("200", "duplicate.current", "1.6");
        var sentinel = Path.Combine(source, "source-sentinel.txt");
        File.WriteAllText(sentinel, "untouched", new UTF8Encoding(false));

        var run = fixture.Invoke(
            "$plan = @(Resolve-GatewaySmokeWorkshopOverridePlans " +
            "-WorkshopPath $workshopPath -ModsRoot $modsRoot " +
            "-PackageIds @('duplicate.current') -Version '1.6' -RunId 'test-run')[0]\n" +
            "$record = Publish-GatewaySmokeWorkshopOverride -Plan $plan -WorkshopPath $workshopPath -ModsRoot $modsRoot\n" +
            "Remove-Item -LiteralPath $record.ContentMarkerPath -Force\n" +
            "$refused = $false\n" +
            "try { Remove-GatewaySmokeWorkshopOverride -Record $record -WorkshopPath $workshopPath -ModsRoot $modsRoot } " +
            "catch { $refused = $_.Exception.Message.Contains('content ownership marker') }\n" +
            "$retained = Test-Path -LiteralPath $record.LocalPath -PathType Container\n" +
            "Write-Output ($refused.ToString() + '|' + $retained.ToString())");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|True"));
            Assert.That(File.ReadAllText(sentinel), Is.EqualTo("untouched"));
        });
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string root;
        private readonly string smokePath;
        private readonly string workshopPath;
        private readonly string modsRoot;

        private Fixture(string root, string smokePath, string workshopPath, string modsRoot)
        {
            this.root = root;
            this.smokePath = smokePath;
            this.workshopPath = workshopPath;
            this.modsRoot = modsRoot;
        }

        public string ModsRoot => modsRoot;

        public static Fixture Create()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                nameof(GatewaySmokeWorkshopOverrideTests),
                Guid.NewGuid().ToString("N"));
            var workshopPath = Path.Combine(root, "Workshop");
            var modsRoot = Path.Combine(root, "RimWorld", "Mods");
            Directory.CreateDirectory(workshopPath);
            Directory.CreateDirectory(modsRoot);
            return new Fixture(
                root,
                Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1"),
                workshopPath,
                modsRoot);
        }

        public string AddWorkshopMod(string itemId, string packageId, params string[] versions)
        {
            var itemPath = Path.Combine(workshopPath, itemId);
            var aboutPath = Path.Combine(itemPath, "About");
            Directory.CreateDirectory(aboutPath);
            foreach (var version in versions)
            {
                Directory.CreateDirectory(Path.Combine(itemPath, version));
            }

            File.WriteAllText(
                Path.Combine(aboutPath, "About.xml"),
                "<ModMetaData><name>Fixture</name><packageId>" + packageId +
                "</packageId><supportedVersions>" +
                string.Join(string.Empty, versions.Select(version => "<li>" + version + "</li>")) +
                "</supportedVersions></ModMetaData>",
                new UTF8Encoding(false));
            return itemPath;
        }

        public InvocationResult Invoke(string operation)
        {
            var invocationPath = Path.Combine(root, $"invoke-{Guid.NewGuid():N}.ps1");
            var functions = new[]
            {
                "Get-GatewaySmokeWorkshopModCandidates",
                "Resolve-GatewaySmokeWorkshopOverridePlans",
                "Assert-GatewaySmokeWorkshopOverridePath",
                "Publish-GatewaySmokeWorkshopOverride",
                "Remove-GatewaySmokeWorkshopOverride"
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
                $"$workshopPath = {PowerShellLiteral(workshopPath)}\n" +
                $"$modsRoot = {PowerShellLiteral(modsRoot)}\n" +
                "try {\n" + operation + "\nexit 0\n}\n" +
                "catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));
            return RunPowerShell(invocationPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
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
            using var process = Process.Start(startInfo) ??
                                throw new InvalidOperationException("Could not start pwsh.");
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000))
            {
                process.Kill();
                throw new TimeoutException("Gateway Workshop-override probe timed out.");
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
