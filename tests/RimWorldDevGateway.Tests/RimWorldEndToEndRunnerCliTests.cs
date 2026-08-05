using System;
using System.Diagnostics;
using System.IO;
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

    private static string PowerShellLiteral(string value) => $"'{value.Replace("'", "''")}'";

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
