using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Xml;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class GatewaySmokeBackgroundLaunchTests
{
    [Test]
    public void Default_launch_is_minimized_and_keeps_the_isolated_game_running_in_background()
    {
        var artifactRoot = Path.Combine(
            Path.GetTempPath(),
            nameof(GatewaySmokeBackgroundLaunchTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(artifactRoot);
        try
        {
            var result = RunDryRun(artifactRoot);

            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            var prefsPaths = Directory.GetFiles(artifactRoot, "Prefs.xml", SearchOption.AllDirectories);

            Assert.Multiple(() =>
            {
                Assert.That(result.StandardOutput, Does.Contain("\"RunInBackground\":true"));
                Assert.That(result.StandardOutput, Does.Contain("\"MusicVolume\":0"));
                Assert.That(result.StandardOutput, Does.Contain("\"LaunchWindowStyle\":\"Minimized\""));
                Assert.That(result.StandardOutput, Does.Contain("\"Prefs\":"));
                Assert.That(prefsPaths, Has.Exactly(1).Items);
            });

            var preferences = new XmlDocument();
            preferences.Load(prefsPaths.Single());
            Assert.That(
                preferences.SelectSingleNode("/PrefsData/runInBackground")?.InnerText,
                Is.EqualTo("True"));
            Assert.That(
                preferences.SelectSingleNode("/PrefsData/volumeMusic")?.InnerText,
                Is.EqualTo("0"));
        }
        finally
        {
            Directory.Delete(artifactRoot, recursive: true);
        }
    }

    [Test]
    public void Visible_window_override_keeps_background_execution_but_starts_normally()
    {
        var artifactRoot = Path.Combine(
            Path.GetTempPath(),
            nameof(GatewaySmokeBackgroundLaunchTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(artifactRoot);
        try
        {
            var result = RunDryRun(artifactRoot, "-VisibleWindow");

            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.Zero, result.StandardError);
                Assert.That(result.StandardOutput, Does.Contain("\"RunInBackground\":true"));
                Assert.That(result.StandardOutput, Does.Contain("\"LaunchWindowStyle\":\"Normal\""));
                Assert.That(result.StandardOutput, Does.Contain("\"VisibleWindow\":true"));
            });
        }
        finally
        {
            Directory.Delete(artifactRoot, recursive: true);
        }
    }

    [Test]
    public void Desktop_owning_gateway_regression_starts_visibly_without_an_override()
    {
        var artifactRoot = Path.Combine(
            Path.GetTempPath(),
            nameof(GatewaySmokeBackgroundLaunchTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(artifactRoot);
        try
        {
            var result = RunDryRun(artifactRoot, "-Quicktest -Scenario gateway-regression");

            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.Zero, result.StandardError);
                Assert.That(result.StandardOutput, Does.Contain("\"LaunchWindowStyle\":\"Normal\""));
                Assert.That(result.StandardOutput, Does.Contain("\"VisibleWindow\":true"));
                Assert.That(result.StandardOutput, Does.Contain("\"VisibleWindowRequested\":false"));
            });
        }
        finally
        {
            Directory.Delete(artifactRoot, recursive: true);
        }
    }

    [Test]
    public void Process_launch_forwards_the_effective_window_style_to_start_process()
    {
        var script =
            "$ErrorActionPreference = 'Stop'\n" +
            "function Start-Process { param([string]$FilePath, [object[]]$ArgumentList, [string]$WindowStyle, [switch]$PassThru) [pscustomobject]@{ FilePath = $FilePath; Arguments = @($ArgumentList); WindowStyle = $WindowStyle; PassThru = [bool]$PassThru } }\n" +
            LoadFunction("Start-GatewayRimWorldProcess") +
            "$result = Start-GatewayRimWorldProcess -ExecutablePath 'C:\\RimWorldWin64.exe' -LaunchArguments @('-quicktest','-logFile') -WindowStyle 'Minimized'\n" +
            "Write-Output ($result.FilePath + '|' + ($result.Arguments -join ',') + '|' + $result.WindowStyle + '|' + $result.PassThru)\n";

        var result = RunPowerShellScript(script);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(
                result.StandardOutput.Trim(),
                Is.EqualTo("C:\\RimWorldWin64.exe|-quicktest,-logFile|Minimized|True"));
        });
    }

    [Test]
    public void Launcher_does_not_monitor_window_state_after_process_start()
    {
        var smokePath = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
        var script = File.ReadAllText(smokePath);

        Assert.That(script, Does.Not.Contain("Get-GatewaySmokeWindowObservation"));
        Assert.That(script, Does.Not.Contain("Save-GatewaySmokeWindowObservation"));
        Assert.That(script, Does.Not.Contain("window-launch-observation.json"));
        Assert.That(script, Does.Not.Contain("IsIconic"));
    }

    [Test]
    public void Dry_run_exposes_matching_normal_preferences_hashes_without_mutation()
    {
        var artifactRoot = Path.Combine(
            Path.GetTempPath(),
            nameof(GatewaySmokeBackgroundLaunchTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(artifactRoot);
        try
        {
            var profileRoot = Path.Combine(artifactRoot, "Profile");
            var normalPreferencesDirectory = Path.Combine(
                profileRoot,
                "AppData",
                "LocalLow",
                "Ludeon Studios",
                "RimWorld by Ludeon Studios",
                "Config");
            Directory.CreateDirectory(normalPreferencesDirectory);
            File.WriteAllText(
                Path.Combine(normalPreferencesDirectory, "Prefs.xml"),
                "<PrefsData><runInBackground>False</runInBackground></PrefsData>");

            var result = RunDryRun(artifactRoot, userProfile: profileRoot);
            var before = Regex.Match(
                result.StandardOutput,
                "\\\"NormalPrefsHashBefore\\\":\\\"(?<hash>[A-F0-9]{64})\\\"");
            var after = Regex.Match(
                result.StandardOutput,
                "\\\"NormalPrefsHashAfter\\\":\\\"(?<hash>[A-F0-9]{64})\\\"");

            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.Zero, result.StandardError);
                Assert.That(before.Success, Is.True, result.StandardOutput);
                Assert.That(after.Success, Is.True, result.StandardOutput);
                Assert.That(after.Groups["hash"].Value, Is.EqualTo(before.Groups["hash"].Value));
            });
        }
        finally
        {
            Directory.Delete(artifactRoot, recursive: true);
        }
    }

    [Test]
    public void Changed_normal_preferences_hash_is_a_primary_verification_failure()
    {
        var script =
            "$ErrorActionPreference = 'Stop'\n" +
            LoadFunction("Add-GatewaySmokeFailure") +
            LoadFunction("Assert-GatewaySmokeUnchangedHash") +
            "$failures = [System.Collections.ArrayList]::new()\n" +
            "$read = [pscustomobject]@{ Succeeded = $true; Hash = 'AFTER' }\n" +
            "$actual = Assert-GatewaySmokeUnchangedHash -DisplayName 'Normal RimWorld Prefs.xml' -BeforeHash 'BEFORE' -ReadResult $read -Failures $failures\n" +
            "Write-Output ($actual + '|' + $failures.Count + '|' + $failures[0].Category + '|' + $failures[0].Message)\n";

        var result = RunPowerShellScript(script);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.StandardOutput, Does.StartWith("AFTER|1|primary|"));
            Assert.That(result.StandardOutput, Does.Contain("Normal RimWorld Prefs.xml changed"));
            Assert.That(result.StandardOutput, Does.Contain("Before=BEFORE After=AFTER"));
        });
    }

    private static InvocationResult RunDryRun(
        string artifactRoot,
        string additionalArguments = "",
        string? userProfile = null)
    {
        var scriptPath = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh.exe",
            Arguments =
                $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{scriptPath}\" " +
                $"-ArtifactsPath \"{artifactRoot}\" -DryRun -Output json {additionalArguments}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            startInfo.EnvironmentVariables["USERPROFILE"] = userProfile;
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start pwsh.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000))
        {
            process.Kill();
            throw new TimeoutException("Gateway background-launch dry run timed out.");
        }

        Task.WaitAll(output, error);
        return new InvocationResult(process.ExitCode, output.Result, error.Result);
    }

    private static string LoadFunction(string functionName)
    {
        var smokePath = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
        return
            "$tokens = $null; $parseErrors = $null\n" +
            $"$ast = [System.Management.Automation.Language.Parser]::ParseFile('{smokePath.Replace("'", "''")}', [ref]$tokens, [ref]$parseErrors)\n" +
            $"$functionAst = $ast.Find({{ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq '{functionName}' }}, $true)\n" +
            $"if ($null -eq $functionAst) {{ throw 'Function was not found: {functionName}' }}\n" +
            "Invoke-Expression $functionAst.Extent.Text\n";
    }

    private static InvocationResult RunPowerShellScript(string script)
    {
        var root = Path.Combine(Path.GetTempPath(), nameof(GatewaySmokeBackgroundLaunchTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var invocationPath = Path.Combine(root, "invoke.ps1");
            File.WriteAllText(invocationPath, script);
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
                throw new TimeoutException("Gateway process-launch probe timed out.");
            }

            Task.WaitAll(output, error);
            return new InvocationResult(process.ExitCode, output.Result, error.Result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
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

    private sealed class InvocationResult
    {
        internal InvocationResult(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }

        internal int ExitCode { get; }
        internal string StandardOutput { get; }
        internal string StandardError { get; }
    }
}
