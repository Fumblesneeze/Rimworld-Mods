using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class RimWorldEndToEndRunnerCliTests
{
    [Test]
    public void Runner_composes_exact_group_launches_and_guaranteed_stage_cleanup()
    {
        var source = File.ReadAllText(RunnerPath());
        var support = File.ReadAllText(SupportModulePath());

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("[switch]$DryRun"));
            Assert.That(source, Does.Contain("foreach ($group in $selectedGroups)"));
            Assert.That(source, Does.Contain("'-RunEndToEndTests'"));
            Assert.That(source, Does.Contain("'-SkipBuildDeploy'"));
            Assert.That(source, Does.Contain("'-AdditionalModIdsFile'"));
            Assert.That(source, Does.Contain("[string[]]$TestId"));
            Assert.That(source, Does.Contain("'-EndToEndTestIds'"));
            Assert.That(source, Does.Contain("[System.IO.File]::WriteAllLines"));
            Assert.That(source, Does.Contain("Join-Path $runDirectory ('smoke-{0:D3}' -f $groupIndex)"));
            Assert.That(source, Does.Contain("finally"));
            Assert.That(source, Does.Contain("'clean'"));
            Assert.That(source, Does.Contain("aggregate.json"));
            Assert.That(source, Does.Contain("results.junit.xml"));
            Assert.That(source, Does.Contain("product-deployment-evidence.json"));
            Assert.That(source, Does.Contain("Get-RimWorldDeployedProductEvidence"));
            Assert.That(source, Does.Contain("ProductDeploymentEvidence"));
            Assert.That(source, Does.Contain("ConvertTo-RimWorldProductEvidenceJson"));
            Assert.That(support, Does.Contain("[System.IO.FileAttributes]::ReparsePoint"));
            Assert.That(support, Does.Contain("Get-ChildItem -LiteralPath $packageRoot -Recurse -Force -ErrorAction Stop"));
            Assert.That(support, Does.Contain("-LiteralPath $file.FullName").And.Contain("-ErrorAction Stop"));
        });
    }

    [Test]
    public void Missing_game_path_is_invalid_usage_before_any_mutation()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var run = Invoke("-DryRun", "-RimWorldPath", missing);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("RimWorld path does not exist"));
        });
    }

    [Test]
    public void Junit_writer_replaces_xml_invalid_terminal_controls_and_preserves_unicode()
    {
        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "RimWorldEndToEndRunnerCliTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var errorPath = Path.Combine(temporaryRoot, "stderr.txt");
            var junitPath = Path.Combine(temporaryRoot, "result.xml");
            File.WriteAllText(
                errorPath,
                "red \u001b[31m failure \ud83e\uddc0 \0 done",
                new UTF8Encoding(false));
            var invocation = string.Join(Environment.NewLine, new[]
            {
                $"Import-Module {PowerShellLiteral(SupportModulePath())} -Force",
                "$group = [pscustomobject]@{",
                "  GroupId = 'ludeon.rimworld|example.mod'",
                "  Status = 'failed'",
                "  Message = \"failed $([char]0x1b) visibly\"",
                $"  StandardError = {PowerShellLiteral(errorPath)}",
                "}",
                $"Write-RimWorldEndToEndJUnitReport -Path {PowerShellLiteral(junitPath)} -GroupResults @($group)"
            });

            var run = InvokeSource(invocation, temporaryRoot);
            var xml = File.ReadAllText(junitPath);
            var document = System.Xml.Linq.XDocument.Parse(xml);
            var failure = document.Descendants("failure").Single();

            Assert.Multiple(() =>
            {
                Assert.That(run.ExitCode, Is.EqualTo(0), run.StandardError);
                Assert.That(xml, Does.Not.Contain("\u001b").And.Not.Contain("\0"));
                Assert.That(failure.Attribute("message")!.Value, Is.EqualTo("failed \ufffd visibly"));
                Assert.That(failure.Value, Does.Contain("red \ufffd[31m failure \ud83e\uddc0 \ufffd done"));
            });
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Test]
    public void Deployment_evidence_hashes_the_exact_product_package_and_excludes_test_staging()
    {
        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "RimWorldEndToEndRunnerCliTests",
            Guid.NewGuid().ToString("N"));
        var game = Path.Combine(temporaryRoot, "Game");
        var package = Path.Combine(game, "Mods", "example.product");
        var assembly = Path.Combine(package, "1.6", "Assemblies", "Example.dll");
        var texture = Path.Combine(package, "1.6", "Textures", "Example.png");
        var stagedTest = Path.Combine(package, "1.6", "DevEndToEndTests", "Example.Tests.dll");
        var project = Path.Combine(temporaryRoot, "Example.csproj");
        var buildLog = Path.Combine(temporaryRoot, "build.log");
        Directory.CreateDirectory(Path.GetDirectoryName(assembly)!);
        Directory.CreateDirectory(Path.GetDirectoryName(texture)!);
        Directory.CreateDirectory(Path.GetDirectoryName(stagedTest)!);
        File.WriteAllText(assembly, "assembly", new UTF8Encoding(false));
        File.WriteAllText(texture, "texture", new UTF8Encoding(false));
        File.WriteAllText(stagedTest, "test-only", new UTF8Encoding(false));
        File.WriteAllText(project, "<Project />", new UTF8Encoding(false));
        File.WriteAllText(buildLog, "build", new UTF8Encoding(false));
        try
        {
            var invocation = string.Join(Environment.NewLine, new[]
            {
                $"Import-Module {PowerShellLiteral(SupportModulePath())} -Force",
                "$evidence = Get-RimWorldDeployedProductEvidence " +
                "-PackageId 'example.product' " +
                $"-ProjectPath {PowerShellLiteral(project)} " +
                $"-RimWorldPath {PowerShellLiteral(game)} " +
                $"-BuildLogPath {PowerShellLiteral(buildLog)}",
                "$evidence | ConvertTo-Json -Depth 8 -Compress"
            });
            var run = InvokeSource(invocation, temporaryRoot);

            Assert.That(run.ExitCode, Is.EqualTo(0), run.StandardError);
            Assert.Multiple(() =>
            {
                Assert.That(run.StandardOutput, Does.Contain("\"PackageId\":\"example.product\""));
                Assert.That(run.StandardOutput, Does.Contain("1.6/Assemblies/Example.dll"));
                Assert.That(run.StandardOutput, Does.Contain("1.6/Textures/Example.png"));
                Assert.That(run.StandardOutput, Does.Contain(HashFile(assembly)));
                Assert.That(run.StandardOutput, Does.Contain(HashFile(texture)));
                Assert.That(run.StandardOutput, Does.Not.Contain("DevEndToEndTests"));
            });
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Test]
    public void Product_evidence_json_keeps_an_array_root_for_one_or_two_products()
    {
        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "RimWorldEndToEndRunnerCliTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var invocation = string.Join(Environment.NewLine, new[]
            {
                $"Import-Module {PowerShellLiteral(SupportModulePath())} -Force",
                "ConvertTo-RimWorldProductEvidenceJson -Evidence @([pscustomobject]@{ PackageId = 'one' })",
                "ConvertTo-RimWorldProductEvidenceJson -Evidence @(" +
                "[pscustomobject]@{ PackageId = 'one' }, " +
                "[pscustomobject]@{ PackageId = 'two' })"
            });
            var run = InvokeSource(invocation, temporaryRoot);
            var lines = run.StandardOutput
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Multiple(() =>
            {
                Assert.That(run.ExitCode, Is.EqualTo(0), run.StandardError);
                Assert.That(lines, Has.Length.EqualTo(2));
                Assert.That(lines[0], Is.EqualTo("[{\"PackageId\":\"one\"}]"));
                Assert.That(lines[1], Is.EqualTo(
                    "[{\"PackageId\":\"one\"},{\"PackageId\":\"two\"}]"));
            });
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static InvocationResult Invoke(params string[] arguments)
    {
        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "RimWorldEndToEndRunnerCliTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var invocationPath = Path.Combine(temporaryRoot, "invoke.ps1");
            var escapedArguments = string.Join(" ", Array.ConvertAll(arguments, PowerShellLiteral));
            var invocation =
                $"& {PowerShellLiteral(RunnerPath())} {escapedArguments}" + Environment.NewLine +
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
                throw new TimeoutException("E2E runner validation probe timed out.");
            }

            Task.WaitAll(output, error);
            return new InvocationResult(process.ExitCode, output.Result, error.Result);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static InvocationResult InvokeSource(string source, string temporaryRoot)
    {
        var invocationPath = Path.Combine(temporaryRoot, "support-probe.ps1");
        File.WriteAllText(
            invocationPath,
            source + Environment.NewLine + "exit $LASTEXITCODE" + Environment.NewLine,
            new UTF8Encoding(false));
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
            throw new TimeoutException("E2E runner support probe timed out.");
        }

        Task.WaitAll(output, error);
        return new InvocationResult(process.ExitCode, output.Result, error.Result);
    }

    private static string PowerShellLiteral(string value) => $"'{value.Replace("'", "''")}'";

    private static string HashFile(string path)
    {
        using var algorithm = SHA256.Create();
        using var stream = File.OpenRead(path);
        return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
    }

    private static string SupportModulePath() => Path.Combine(
        FindSourceRepositoryRoot(),
        "scripts",
        "RimWorldEndToEndRunner.Support.psm1");

    private static string RunnerPath() => Path.Combine(
        FindSourceRepositoryRoot(),
        "scripts",
        "Invoke-RimWorldEndToEndTests.ps1");

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
