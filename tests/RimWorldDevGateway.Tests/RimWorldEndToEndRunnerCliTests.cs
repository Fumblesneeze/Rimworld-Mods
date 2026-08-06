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
public sealed class RimWorldEndToEndRunnerCliTests
{
    [Test]
    public void Runner_composes_exact_group_launches_and_guaranteed_stage_cleanup()
    {
        var source = File.ReadAllText(RunnerPath());

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
