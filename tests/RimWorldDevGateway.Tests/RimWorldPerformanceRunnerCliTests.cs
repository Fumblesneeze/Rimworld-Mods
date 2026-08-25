using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
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
            Assert.That(processes.Select(item => item.WorkloadVersion), Is.All.EqualTo("gateway-calibration/v4"));
            Assert.That(processes.Select(item => item.Repetition), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(processes[0].RawCircinusJsonPath, Does.EndWith("circinus.raw.json"));
            Assert.That(processes[0].NormalizedJsonPath, Does.EndWith("normalized.json"));
            Assert.That(processes[0].CsvReportPath, Does.EndWith("metrics.csv"));
            Assert.That(processes[0].MarkdownReportPath, Does.EndWith("summary.md"));
            Assert.That(root.Baseline, Is.Not.Null);
            Assert.That(root.Baseline!.Mode, Is.EqualTo("compare"));
            Assert.That(root.Baseline.Directory, Does.EndWith(Path.Combine("performance", "baselines")));
            Assert.That(root.Baseline.Policy, Does.EndWith(Path.Combine("performance", "thresholds.json")));
            Assert.That(root.Baseline.InformationalCrossVersion, Is.False);
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
    public void Dpa_diagnostic_dry_run_replaces_Circinus_and_exposes_no_baseline_operation()
    {
        var artifactRoot = Path.Combine(Path.GetTempPath(), "RimWorldPerformanceRunnerCliTests", Guid.NewGuid().ToString("N"));
        const string selector = "Verse.Map::MapPreTick()";
        var run = Invoke(
            "-DryRun", "-Output", "json",
            "-AvailableModIds",
            "brrainz.harmony,ludeon.rimworld,dubwise.dubsperformanceanalyzer.steam,fumblesneeze.rimworlddevgateway",
            "-BenchmarkId", "gateway.circinus-calibration.instrumented",
            "-DiagnosticProfiler", "Dpa", "-DiagnosticSelector", selector,
            "-WarmUpTicks", "30", "-SampleTicks", "120",
            "-ArtifactsPath", artifactRoot);

        Assert.That(run.ExitCode, Is.Zero, run.StandardError);
        PerformanceDryRun root;
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(run.StandardOutput)))
            root = (PerformanceDryRun)new DataContractJsonSerializer(typeof(PerformanceDryRun)).ReadObject(stream)!;
        var process = root.Processes.Single();
        Assert.Multiple(() =>
        {
            Assert.That(process.Profiler, Is.EqualTo("DpaDiagnostic"));
            Assert.That(process.DiagnosticSelector, Is.EqualTo(selector));
            Assert.That(process.ActivePackageIds, Is.EqualTo(new[]
            {
                "brrainz.harmony", "ludeon.rimworld", "dubwise.dubsperformanceanalyzer.steam",
                "fumblesneeze.rimworlddevgateway"
            }));
            Assert.That(process.RawDiagnosticJsonPath, Does.EndWith("dpa.raw.json"));
            Assert.That(process.MethodSelectors.Select(item => item.Value), Is.EqualTo(new[] { selector }));
            Assert.That(process.Repetition, Is.EqualTo(1));
            Assert.That(root.Baseline, Is.Null, "Diagnostic output must not offer a Circinus baseline operation.");
            Assert.That(Directory.Exists(artifactRoot), Is.False);
        });
    }

    [Test]
    public void Dpa_diagnostic_rejects_Circinus_baseline_candidate_mode_before_discovery()
    {
        var run = Invoke(
            "-DryRun", "-CreateBaselineCandidate",
            "-DiagnosticProfiler", "Dpa", "-DiagnosticSelector", "Verse.Map::MapPreTick()");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("cannot create or compare Circinus baselines"));
        });
    }

    [Test]
    public void Dry_run_exposes_explicit_non_overwriting_candidate_mode_without_writing_it()
    {
        var artifactRoot = Path.Combine(Path.GetTempPath(), "RimWorldPerformanceRunnerCliTests", Guid.NewGuid().ToString("N"));
        var candidate = Path.Combine(artifactRoot, "review", "candidate.json");

        var run = Invoke(
            "-DryRun", "-Output", "json", "-CreateBaselineCandidate",
            "-BaselineCandidatePath", candidate,
            "-AvailableModIds", "brrainz.harmony,ludeon.rimworld,astryl.circinus,fumblesneeze.rimworlddevgateway",
            "-BenchmarkId", "gateway.circinus-calibration.instrumented",
            "-ArtifactsPath", artifactRoot);

        Assert.That(run.ExitCode, Is.Zero, run.StandardError);
        PerformanceDryRun root;
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(run.StandardOutput)))
            root = (PerformanceDryRun)new DataContractJsonSerializer(typeof(PerformanceDryRun)).ReadObject(stream)!;
        Assert.Multiple(() =>
        {
            Assert.That(root.Baseline, Is.Not.Null);
            Assert.That(root.Baseline!.Mode, Is.EqualTo("candidate"));
            Assert.That(root.Baseline.Candidate, Is.EqualTo(candidate));
            Assert.That(File.Exists(candidate), Is.False);
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

    [Test]
    public void Runtime_runner_surfaces_non_gating_cross_version_results_as_informational_not_passed()
    {
        var source = File.ReadAllText(RunnerPath());
        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("$performanceOutcome"));
            Assert.That(source, Does.Contain("[string]$baselineResult.status -ceq 'informational'"));
            Assert.That(source, Does.Contain("Status = $performanceOutcome"));
        });
    }

    [Test]
    public void Runtime_snapshot_retains_distinct_shared_empty_sidecars_but_rejects_any_nonempty_duplicate()
    {
        var expectedHash = Sha256("Product.PatchA\nProduct.PatchB");
        var source =
            "$tokens=$null; $errors=$null; " +
            "$ast=[System.Management.Automation.Language.Parser]::ParseFile(" + Literal(RunnerPath()) + ",[ref]$tokens,[ref]$errors); " +
            "if($errors.Count -ne 0){throw 'parse failed'}; " +
            "$wanted=@('Get-PerformanceTextSha256','Resolve-PerformanceMetricSidecar'); " +
            "$ast.FindAll({param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -in $wanted},$true) | ForEach-Object { Invoke-Expression $_.Extent.Text }; " +
            "$a=[pscustomobject]@{methodIdentity='Product.PatchA';empty=$true;noRowReason='empty-or-uninvoked'}; " +
            "$b=[pscustomobject]@{methodIdentity='Product.PatchB';empty=$true;noRowReason='empty-or-uninvoked'}; " +
            "$resolved=Resolve-PerformanceMetricSidecar -Sidecars @($b,$a) -MetricKey 'shared'; " +
            "Write-Output $resolved.ExactMethod; " +
            "$b.empty=$false; try { Resolve-PerformanceMetricSidecar -Sidecars @($a,$b) -MetricKey 'shared' | Out-Null; exit 71 } catch { Write-Output $_.Exception.Message }";

        var run = InvokeScript(source);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput, Does.Contain("empty-sidecars-sha256:" + expectedHash));
            Assert.That(run.StandardOutput, Does.Contain("including a non-empty profiler"));
        });
    }

    [Test]
    public void Runtime_snapshot_accepts_ordinary_fixture_state_drift_and_rejects_only_missing_health_manifests()
    {
        var source =
            "$tokens=$null; $errors=$null; " +
            "$ast=[System.Management.Automation.Language.Parser]::ParseFile(" + Literal(RunnerPath()) + ",[ref]$tokens,[ref]$errors); " +
            "if($errors.Count -ne 0){throw 'parse failed'}; " +
            "$wanted=@('Assert-PerformanceFixtureManifestCompatibility'); " +
            "$ast.FindAll({param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -in $wanted},$true) | ForEach-Object { Invoke-Expression $_.Extent.Text }; " +
            "$same='{\"pawn\":\"A\",\"layout\":\"L\"}'; $drift='{\"pawn\":\"B\",\"layout\":\"L\"}'; $terminal='{\"terminal\":\"T\"}'; " +
            "$records=@(" +
            "[pscustomobject]@{BenchmarkId='present.instrumented';ComparisonId='present';ProductAbsentControlId='absent';ManifestJson=$same;TerminalManifestJson=$terminal}," +
            "[pscustomobject]@{BenchmarkId='present.instrumented';ComparisonId='present';ProductAbsentControlId='absent';ManifestJson=$same;TerminalManifestJson=$terminal}," +
            "[pscustomobject]@{BenchmarkId='present.armed';ComparisonId='present';ProductAbsentControlId='absent';ManifestJson=$same;TerminalManifestJson=$terminal}," +
            "[pscustomobject]@{BenchmarkId='present.disarmed';ComparisonId='present';ProductAbsentControlId='absent';ManifestJson=$same;TerminalManifestJson=$terminal}," +
            "[pscustomobject]@{BenchmarkId='absent';ComparisonId='absent';ProductAbsentControlId='';ManifestJson=$same;TerminalManifestJson=$terminal}); " +
            "$records[1].ManifestJson=$drift; $records[2].ManifestJson=$drift; $records[4].ManifestJson='{\"pawn\":\"C\"}'; " +
            "Assert-PerformanceFixtureManifestCompatibility -Records $records; Write-Output 'ordinary-drift-accepted'; " +
            "$records[3].ManifestJson=''; try { Assert-PerformanceFixtureManifestCompatibility -Records $records; exit 71 } catch { Write-Output ('missing|' + $_.Exception.Message) }";

        var run = InvokeScript(source);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput, Does.Contain("ordinary-drift-accepted"));
            Assert.That(run.StandardOutput, Does.Contain("missing|").And.Contain("missing fixture health manifest"));
        });
    }

    [Test]
    public void Runtime_snapshot_rejects_nonmanifest_compatibility_drift_and_weights_only_observed_calls()
    {
        var source =
            "$tokens=$null; $errors=$null; " +
            "$ast=[System.Management.Automation.Language.Parser]::ParseFile(" + Literal(RunnerPath()) + ",[ref]$tokens,[ref]$errors); " +
            "if($errors.Count -ne 0){throw 'parse failed'}; " +
            "$wanted=@('Assert-PerformanceRepetitionCompatibility','Get-PerformanceMetricDistribution','Get-PerformancePerCallAggregate'); " +
            "$ast.FindAll({param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -in $wanted},$true) | ForEach-Object { Invoke-Expression $_.Extent.Text }; " +
            "$plan=[pscustomobject]@{processes=@([pscustomobject]@{benchmarkId='b'},[pscustomobject]@{benchmarkId='b'},[pscustomobject]@{benchmarkId='b'})}; " +
            "$measure={param($calls,$value,$context) $rows=@([pscustomobject]@{Scope='method';Selector='m';MetricName='calls';Value=$calls;Calls=$calls;TimedCalls=$calls;SamplingContextIdentity=$context}); if($calls -gt 0){$rows+= [pscustomobject]@{Scope='method';Selector='m';MetricName='gross-ms-per-estimated-call';Value=$value;Calls=$calls;TimedCalls=$calls;SamplingContextIdentity=$context}}; return $rows}; " +
            "$members=@([pscustomobject]@{IdentityJson='same';Compatibility=[pscustomobject]@{BenchmarkId='b'};Measurements=@(&$measure 0 0 'c0')},[pscustomobject]@{IdentityJson='same';Compatibility=[pscustomobject]@{BenchmarkId='b'};Measurements=@(&$measure 2 10 'c2')},[pscustomobject]@{IdentityJson='same';Compatibility=[pscustomobject]@{BenchmarkId='b'};Measurements=@(&$measure 4 20 'c4')}); " +
            "Assert-PerformanceRepetitionCompatibility -Plan $plan -Observations $members; " +
            "$weighted=Get-PerformancePerCallAggregate -Members $members -Scope 'method' -Selector 'm' -BenchmarkId 'b'; $distribution=Get-PerformanceMetricDistribution -Values $weighted.RepetitionValues; Write-Output ('weighted|' + $weighted.HasObservation + '|' + $weighted.Value + '|' + $weighted.ContextRows.Count + '|' + (($weighted.ContextRows|Measure-Object -Property Calls -Average).Average) + '|' + (($weighted.ContextRows.SamplingContextIdentity) -join ',') + '|values=' + (($distribution.Values | ForEach-Object { if($null -eq $_){'null'}else{[string]$_} }) -join ',') + '|count=' + $distribution.Count + '|weights=' + ($weighted.RepetitionWeights -join ',')); " +
            "$members[2].IdentityJson='drift'; try { Assert-PerformanceRepetitionCompatibility -Plan $plan -Observations $members; exit 71 } catch { Write-Output ('drift|' + $_.Exception.Message) }";

        var run = InvokeScript(source);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput, Does.Contain("weighted|True|16.666").And.Contain("|3|2|c0,c2,c4|values=null,10,20|count=3|weights=0,2,4"));
            Assert.That(run.StandardOutput, Does.Contain("drift|Benchmark 'b' has repetition compatibility drift."));
        });
    }

    [Test]
    public void Runtime_snapshot_retains_an_explicit_observation_rate_when_every_per_call_sample_is_unobserved()
    {
        var source =
            "$tokens=$null; $errors=$null; " +
            "$ast=[System.Management.Automation.Language.Parser]::ParseFile(" + Literal(RunnerPath()) + ",[ref]$tokens,[ref]$errors); " +
            "if($errors.Count -ne 0){throw 'parse failed'}; " +
            "$wanted=@('Get-PerformanceMetricDistribution','Get-PerformancePerCallAggregate','Get-PerformancePerCallObservationDistribution'); " +
            "$ast.FindAll({param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -in $wanted},$true) | ForEach-Object { Invoke-Expression $_.Extent.Text }; " +
            "$row={param($context) [pscustomobject]@{Scope='method';Selector='m';MetricName='calls';Value=0;Calls=0;TimedCalls=0;SamplingContextIdentity=$context}}; " +
            "$members=@([pscustomobject]@{Measurements=@(&$row 'c1')},[pscustomobject]@{Measurements=@(&$row 'c2')},[pscustomobject]@{Measurements=@(&$row 'c3')}); " +
            "$aggregate=Get-PerformancePerCallAggregate -Members $members -Scope 'method' -Selector 'm' -BenchmarkId 'b'; " +
            "$availability=Get-PerformancePerCallObservationDistribution -Aggregate $aggregate; " +
            "Write-Output ('unobserved|' + $aggregate.HasObservation + '|' + $availability.Mean + '|' + $availability.Count + '|' + ($availability.Values -join ','))";

        var run = InvokeScript(source);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput, Does.Contain("unobserved|False|0|3|0,0,0"));
        });
    }

    [Test]
    public void Runtime_snapshot_reports_the_distribution_of_ordinary_repetitions()
    {
        var source =
            "$tokens=$null; $errors=$null; " +
            "$ast=[System.Management.Automation.Language.Parser]::ParseFile(" + Literal(RunnerPath()) + ",[ref]$tokens,[ref]$errors); " +
            "if($errors.Count -ne 0){throw 'parse failed'}; " +
            "$wanted=@('Get-PerformanceMetricDistribution'); " +
            "$ast.FindAll({param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -in $wanted},$true) | ForEach-Object { Invoke-Expression $_.Extent.Text }; " +
            "$d=Get-PerformanceMetricDistribution -Values @(10.0,20.0,30.0); " +
            "Write-Output ($d.Count.ToString()+'|'+$d.Mean.ToString()+'|'+$d.Minimum.ToString()+'|'+$d.Maximum.ToString()+'|'+$d.SampleStandardDeviation.ToString('F6')+'|'+($d.Values -join ','))";

        var run = InvokeScript(source);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Replace(',', '.'), Does.Contain("3|20|10|30|10.000000|10.20.30"));
        });
    }

    [Test]
    public void Default_plan_runs_only_instrumented_averages_while_controls_are_opt_in()
    {
        var source =
            "$tokens=$null; $errors=$null; " +
            "$ast=[System.Management.Automation.Language.Parser]::ParseFile(" + Literal(RunnerPath()) + ",[ref]$tokens,[ref]$errors); " +
            "if($errors.Count -ne 0){throw 'parse failed'}; " +
            "$wanted=@('Select-OrdinaryPerformancePlan'); " +
            "$ast.FindAll({param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -in $wanted},$true) | ForEach-Object { Invoke-Expression $_.Extent.Text }; " +
            "$processes=@(" +
            "[pscustomobject]@{benchmarkId='calibration';evidenceLens=0}," +
            "[pscustomobject]@{benchmarkId='base';evidenceLens=0},[pscustomobject]@{benchmarkId='base';evidenceLens=0},[pscustomobject]@{benchmarkId='base';evidenceLens=0}," +
            "[pscustomobject]@{benchmarkId='present';evidenceLens=0;productAbsentControlId='absent'},[pscustomobject]@{benchmarkId='present';evidenceLens=0;productAbsentControlId='absent'},[pscustomobject]@{benchmarkId='present';evidenceLens=0;productAbsentControlId='absent'}," +
            "[pscustomobject]@{benchmarkId='armed';evidenceLens=1},[pscustomobject]@{benchmarkId='disarmed';evidenceLens=2},[pscustomobject]@{benchmarkId='absent';evidenceLens=3}); " +
            "$group=[pscustomobject]@{groupId='g';activePackageIds=@('core');benchmarks=@('calibration','base','present','armed','disarmed','absent')}; " +
            "$ordinary=Select-OrdinaryPerformancePlan -Plan ([pscustomobject]@{processes=$processes;groups=@($group)}); " +
            "Write-Output ('ordinary|' + (($ordinary.processes.benchmarkId | Select-Object -Unique) -join ',') + '|' + ($ordinary.groups[0].benchmarks -join ',')); " +
            "$all=Select-OrdinaryPerformancePlan -Plan ([pscustomobject]@{processes=$processes;groups=@($group)}) -IncludeControls; " +
            "Write-Output ('controls|' + (($all.processes.benchmarkId | Select-Object -Unique) -join ','))";

        var run = InvokeScript(source);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput, Does.Contain("ordinary|base,present|base,present"));
            Assert.That(run.StandardOutput, Does.Contain("controls|calibration,base,present,armed,disarmed"));
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

    private static InvocationResult InvokeScript(string source)
    {
        var temporaryRoot = Path.Combine(
            Path.GetTempPath(), "RimWorldPerformanceRunnerCliTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var path = Path.Combine(temporaryRoot, "probe.ps1");
            File.WriteAllText(path, "$ErrorActionPreference='Stop'" + Environment.NewLine + source,
                new UTF8Encoding(false));
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
            if (!process.WaitForExit(30000)) { process.Kill(); throw new TimeoutException("Performance script probe timed out."); }
            Task.WaitAll(output, error);
            return new InvocationResult(process.ExitCode, output.Result, error.Result);
        }
        finally { Directory.Delete(temporaryRoot, recursive: true); }
    }

    private static string Sha256(string value)
    {
        using var algorithm = SHA256.Create();
        return string.Concat(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value))
            .Select(item => item.ToString("x2")));
    }

    private static string Argument(string value) => value.StartsWith("-", StringComparison.Ordinal) ? value : Literal(value);
    private static string Literal(string value) => "'" + value.Replace("'", "''") + "'";
    private static string RunnerPath() => Path.Combine(FindRoot(), "scripts", "Invoke-RimWorldPerformanceTests.ps1");
    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "RimWorldMods.sln"))) return directory.FullName;
        throw new DirectoryNotFoundException("Could not find repository root.");
    }

    [DataContract]
    private sealed class PerformanceDryRun
    {
        [DataMember] public string Status { get; set; } = string.Empty;
        [DataMember] public bool MutatedGame { get; set; }
        [DataMember] public PerformanceProcess[] Processes { get; set; } = Array.Empty<PerformanceProcess>();
        [DataMember] public PerformanceBaselinePlan? Baseline { get; set; }
    }

    [DataContract]
    private sealed class PerformanceBaselinePlan
    {
        [DataMember] public string Mode { get; set; } = string.Empty;
        [DataMember] public string Directory { get; set; } = string.Empty;
        [DataMember] public string Policy { get; set; } = string.Empty;
        [DataMember] public string Candidate { get; set; } = string.Empty;
        [DataMember] public bool InformationalCrossVersion { get; set; }
    }

    [DataContract]
    private sealed class PerformanceProcess
    {
        [DataMember(Name = "benchmarkId")] public string BenchmarkId { get; set; } = string.Empty;
        [DataMember(Name = "warmUpTicks")] public int WarmUpTicks { get; set; }
        [DataMember(Name = "sampleTicks")] public int SampleTicks { get; set; }
        [DataMember(Name = "gameSpeed")] public int GameSpeed { get; set; }
        [DataMember(Name = "workloadVersion")] public string WorkloadVersion { get; set; } = string.Empty;
        [DataMember(Name = "repetition")] public int Repetition { get; set; }
        [DataMember(Name = "profiler")] public string Profiler { get; set; } = string.Empty;
        [DataMember(Name = "diagnosticSelector")] public string? DiagnosticSelector { get; set; }
        [DataMember(Name = "activePackageIds")] public string[] ActivePackageIds { get; set; } = Array.Empty<string>();
        [DataMember(Name = "maxWallClockSeconds")] public int MaxWallClockSeconds { get; set; }
        [DataMember(Name = "methodSelectors")] public PerformanceSelector[] MethodSelectors { get; set; } = Array.Empty<PerformanceSelector>();
        [DataMember(Name = "rawCircinusJsonPath")] public string RawCircinusJsonPath { get; set; } = string.Empty;
        [DataMember(Name = "rawDiagnosticJsonPath")] public string RawDiagnosticJsonPath { get; set; } = string.Empty;
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
