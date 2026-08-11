using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class RimWorldPerformanceRunnerCliTests
{
    [Test]
    public void Missing_game_path_is_invalid_usage_before_discovery()
    {
        var run = Invoke("-DryRun", "-RimWorldPath", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("RimWorld path does not exist"));
        });
    }

    [Test]
    public void Real_dry_run_filters_and_overrides_one_fresh_process_without_mutating_artifacts()
    {
        var artifactRoot = Path.Combine(Path.GetTempPath(), "RimWorldPerformanceRunnerCliTests", Guid.NewGuid().ToString("N"));
        var run = Invoke(
            "-DryRun", "-Output", "json",
            "-AvailableModIds", "brrainz.harmony,ludeon.rimworld,astryl.circinus,fumblesneeze.rimworlddevgateway",
            "-BenchmarkId", "gateway.circinus-calibration.instrumented",
            "-WarmUpTicks", "30", "-SampleTicks", "120", "-Repetitions", "2",
            "-ArtifactsPath", artifactRoot);

        Assert.That(run.ExitCode, Is.Zero, run.StandardError);
        PerformanceDryRun root;
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(run.StandardOutput)))
            root = (PerformanceDryRun)new DataContractJsonSerializer(typeof(PerformanceDryRun)).ReadObject(stream)!;
        var processes = root.Processes;
        Assert.Multiple(() =>
        {
            Assert.That(root.Status, Is.EqualTo("dry-run"));
            Assert.That(root.MutatedGame, Is.False);
            Assert.That(processes, Has.Length.EqualTo(2));
            Assert.That(processes.Select(item => item.BenchmarkId),
                Is.All.EqualTo("gateway.circinus-calibration.instrumented"));
            Assert.That(processes.Select(item => item.WarmUpTicks), Is.All.EqualTo(30));
            Assert.That(processes.Select(item => item.SampleTicks), Is.All.EqualTo(120));
            Assert.That(processes.Select(item => item.GameSpeed), Is.All.EqualTo(3),
                "The dense calibration must run uncapped so wrapper and timing costs clear the normal-speed floor.");
            Assert.That(processes.Select(item => item.DeterministicSeed), Is.All.EqualTo(60161));
            Assert.That(processes.Select(item => item.WorkloadVersion), Is.All.EqualTo("gateway-calibration/v4"));
            Assert.That(processes.Select(item => item.Repetition), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(processes[0].RawCircinusJsonPath, Does.EndWith("circinus.raw.json"));
            Assert.That(processes[0].NormalizedJsonPath, Does.EndWith("normalized.json"));
            Assert.That(processes[0].CsvReportPath, Does.EndWith("metrics.csv"));
            Assert.That(processes[0].MarkdownReportPath, Does.EndWith("summary.md"));
            Assert.That(processes[0].MaxWallClockSeconds, Is.GreaterThan(0));
            Assert.That(processes[0].MethodSelectors.Select(item => item.Value), Is.EqualTo(new[]
            {
                "fumblesneeze.rimworlddevgateway",
                "RimWorldDevGateway.GatewayGameControlController::Capture()",
                "RimWorldDevGateway.PerformanceTests.CalibrationTickComponent::MapComponentTick()"
            }));
            Assert.That(Directory.Exists(artifactRoot), Is.False);
        });
    }

    [Test]
    public void Dry_run_transports_a_large_resolvable_package_catalog_through_a_bounded_file_argument()
    {
        var optional = Enumerable.Range(0, 1200).Select(index => $"optional.performance.{index}");
        var packages = string.Join(",", new[]
        {
            "brrainz.harmony", "ludeon.rimworld", "astryl.circinus",
            "fumblesneeze.rimworlddevgateway"
        }.Concat(optional));

        var run = Invoke(
            "-DryRun", "-Output", "json",
            "-AvailableModIds", packages,
            "-BenchmarkId", "gateway.circinus-calibration.instrumented");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput, Does.Contain("\"Status\":\"dry-run\""));
        });
    }

    [Test]
    public void Runtime_runner_hashes_the_normal_Circinus_settings_before_and_after_every_run()
    {
        var source = File.ReadAllText(RunnerPath());
        var normalPath = source.IndexOf("NormalCircinusSettingsPath", StringComparison.Ordinal);
        var before = source.IndexOf("NormalCircinusSettingsBefore", StringComparison.Ordinal);
        var finallyBlock = source.IndexOf("finally {", before, StringComparison.Ordinal);
        var after = source.IndexOf("NormalCircinusSettingsAfter", finallyBlock, StringComparison.Ordinal);
        var unchanged = source.IndexOf("NormalCircinusSettingsUnchanged", after, StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(normalPath, Is.GreaterThanOrEqualTo(0));
            Assert.That(before, Is.GreaterThan(normalPath));
            Assert.That(finallyBlock, Is.GreaterThan(before));
            Assert.That(after, Is.GreaterThan(finallyBlock));
            Assert.That(unchanged, Is.GreaterThan(after));
        });
    }

    [Test]
    public void Runtime_runner_disables_dotnet_build_server_reuse_for_redirected_bounded_children()
    {
        var source = File.ReadAllText(RunnerPath());
        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("MSBUILDDISABLENODEREUSE'] = '1'"));
            Assert.That(source, Does.Contain("DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER'] = '1'"));
        });
    }

    private static InvocationResult Invoke(params string[] arguments)
    {
        var temporaryRoot = Path.Combine(Path.GetTempPath(), "RimWorldPerformanceRunnerCliTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var path = Path.Combine(temporaryRoot, "invoke.ps1");
            var source = "$ErrorActionPreference='Stop'" + Environment.NewLine +
                         $"& {Literal(RunnerPath())} {string.Join(" ", arguments.Select(Argument))}" + Environment.NewLine +
                         "exit $LASTEXITCODE" + Environment.NewLine;
            File.WriteAllText(path, source, new UTF8Encoding(false));
            var info = new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{path}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start pwsh.");
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(120000)) { process.Kill(); throw new TimeoutException("Performance CLI probe timed out."); }
            Task.WaitAll(output, error);
            return new InvocationResult(process.ExitCode, output.Result, error.Result);
        }
        finally { Directory.Delete(temporaryRoot, recursive: true); }
    }

    private static string Argument(string value) => value.StartsWith("-", StringComparison.Ordinal) ? value : Literal(value);
    private static string Literal(string value) => "'" + value.Replace("'", "''") + "'";
    private static string RunnerPath() => Path.Combine(FindRoot(), "scripts", "Invoke-RimWorldPerformanceTests.ps1");
    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "ImmersiveChefs.sln"))) return directory.FullName;
        throw new DirectoryNotFoundException("Could not find repository root.");
    }

    [DataContract]
    private sealed class PerformanceDryRun
    {
        [DataMember] public string Status { get; set; } = string.Empty;
        [DataMember] public bool MutatedGame { get; set; }
        [DataMember] public PerformanceProcess[] Processes { get; set; } = Array.Empty<PerformanceProcess>();
    }

    [DataContract]
    private sealed class PerformanceProcess
    {
        [DataMember(Name = "benchmarkId")] public string BenchmarkId { get; set; } = string.Empty;
        [DataMember(Name = "warmUpTicks")] public int WarmUpTicks { get; set; }
        [DataMember(Name = "sampleTicks")] public int SampleTicks { get; set; }
        [DataMember(Name = "gameSpeed")] public int GameSpeed { get; set; }
        [DataMember(Name = "deterministicSeed")] public int DeterministicSeed { get; set; }
        [DataMember(Name = "workloadVersion")] public string WorkloadVersion { get; set; } = string.Empty;
        [DataMember(Name = "repetition")] public int Repetition { get; set; }
        [DataMember(Name = "maxWallClockSeconds")] public int MaxWallClockSeconds { get; set; }
        [DataMember(Name = "methodSelectors")] public PerformanceSelector[] MethodSelectors { get; set; } = Array.Empty<PerformanceSelector>();
        [DataMember(Name = "rawCircinusJsonPath")] public string RawCircinusJsonPath { get; set; } = string.Empty;
        [DataMember(Name = "normalizedJsonPath")] public string NormalizedJsonPath { get; set; } = string.Empty;
        [DataMember(Name = "csvReportPath")] public string CsvReportPath { get; set; } = string.Empty;
        [DataMember(Name = "markdownReportPath")] public string MarkdownReportPath { get; set; } = string.Empty;
    }

    [DataContract]
    private sealed class PerformanceSelector
    {
        [DataMember(Name = "value")] public string Value { get; set; } = string.Empty;
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
