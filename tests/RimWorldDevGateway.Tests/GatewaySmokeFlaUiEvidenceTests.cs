using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class GatewaySmokeFlaUiEvidenceTests
{
    [Test]
    public void Redundant_desktop_failure_is_retained_when_the_gateway_png_exists()
    {
        using var fixture = new Fixture();
        File.WriteAllBytes(fixture.GatewayScreenshot, new byte[] { 1, 2, 3 });

        var run = fixture.Invoke(
            "$invoker = { param($arguments) throw 'desktop capture cancelled' }\n" +
            "$result = Get-GatewaySmokeFlaUiDesktopEvidence -GatewayScreenshotPath $gatewayScreenshot -FlaUiScreenshotPath $desktopScreenshot -ArtifactPath $artifact -FlaUiInvoker $invoker\n" +
            "$result | ConvertTo-Json -Compress -Depth 5");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput, Does.Contain("\"Status\":\"unavailable\""));
            Assert.That(run.StandardOutput, Does.Contain("\"Screenshot\":null"));
            Assert.That(run.StandardOutput, Does.Contain("desktop capture cancelled"));
            Assert.That(File.Exists(fixture.Artifact), Is.True);
            Assert.That(File.Exists(fixture.DesktopScreenshot), Is.False);
            Assert.That(File.ReadAllText(fixture.Artifact), Does.Contain("\"Status\": \"unavailable\""));
        });
    }

    [Test]
    public void Desktop_failure_remains_fatal_without_a_nonempty_gateway_png()
    {
        using var fixture = new Fixture();

        var run = fixture.Invoke(
            "$invoker = { param($arguments) throw 'desktop capture cancelled' }\n" +
            "Get-GatewaySmokeFlaUiDesktopEvidence -GatewayScreenshotPath $gatewayScreenshot -FlaUiScreenshotPath $desktopScreenshot -ArtifactPath $artifact -FlaUiInvoker $invoker | Out-Null");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("mandatory Gateway screenshot").IgnoreCase);
            Assert.That(File.Exists(fixture.Artifact), Is.True);
            Assert.That(File.ReadAllText(fixture.Artifact), Does.Contain("\"Status\": \"unavailable\""));
        });
    }

    [Test]
    public void Screenshot_failure_preserves_an_available_window_count()
    {
        using var fixture = new Fixture();
        File.WriteAllBytes(fixture.GatewayScreenshot, new byte[] { 1, 2, 3 });

        var run = fixture.Invoke(
            "$invoker = { param($arguments) if ($arguments[0] -eq 'window') { return [pscustomobject]@{ data = @('one','two') } }; throw 'desktop screenshot cancelled' }\n" +
            "$result = Get-GatewaySmokeFlaUiDesktopEvidence -GatewayScreenshotPath $gatewayScreenshot -FlaUiScreenshotPath $desktopScreenshot -ArtifactPath $artifact -FlaUiInvoker $invoker\n" +
            "$result | ConvertTo-Json -Compress -Depth 5");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput, Does.Contain("\"Status\":\"unavailable\""));
            Assert.That(run.StandardOutput, Does.Contain("\"WindowCount\":2"));
            Assert.That(run.StandardOutput, Does.Contain("desktop screenshot cancelled"));
            Assert.That(File.Exists(fixture.DesktopScreenshot), Is.False);
        });
    }

    [Test]
    public void Unconfirmed_child_termination_is_never_downgraded_to_optional_evidence()
    {
        using var fixture = new Fixture();
        File.WriteAllBytes(fixture.GatewayScreenshot, new byte[] { 1, 2, 3 });

        var run = fixture.Invoke(
            "$invoker = { param($arguments) $exception = [TimeoutException]::new('hung FlaUI child PID 42 could not be terminated'); $exception.Data['GatewaySmokeChildTerminationConfirmed'] = $false; throw $exception }\n" +
            "Get-GatewaySmokeFlaUiDesktopEvidence -GatewayScreenshotPath $gatewayScreenshot -FlaUiScreenshotPath $desktopScreenshot -ArtifactPath $artifact -FlaUiInvoker $invoker | Out-Null");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("could not be terminated"));
            Assert.That(File.Exists(fixture.Artifact), Is.True);
            Assert.That(File.ReadAllText(fixture.Artifact), Does.Contain("\"Status\": \"unavailable\""));
        });
    }

    [Test]
    public void Mandatory_gateway_screenshot_is_revalidated_after_successful_desktop_capture()
    {
        using var fixture = new Fixture();
        File.WriteAllBytes(fixture.GatewayScreenshot, new byte[] { 1, 2, 3 });

        var run = fixture.Invoke(
            "$invoker = { param($arguments) if ($arguments[0] -eq 'window') { return [pscustomobject]@{ data = @('one') } }; [IO.File]::Delete($gatewayScreenshot); [IO.File]::WriteAllBytes($desktopScreenshot, [byte[]](9,8,7)); return [pscustomobject]@{ success = $true } }\n" +
            "Get-GatewaySmokeFlaUiDesktopEvidence -GatewayScreenshotPath $gatewayScreenshot -FlaUiScreenshotPath $desktopScreenshot -ArtifactPath $artifact -FlaUiInvoker $invoker | Out-Null");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("mandatory Gateway screenshot").IgnoreCase);
            Assert.That(File.Exists(fixture.Artifact), Is.True);
            Assert.That(File.ReadAllText(fixture.Artifact), Does.Contain("\"Status\": \"completed\""));
            Assert.That(new FileInfo(fixture.DesktopScreenshot).Length, Is.EqualTo(3));
        });
    }

    [Test]
    public void Locked_partial_desktop_file_does_not_suppress_the_unavailable_artifact()
    {
        using var fixture = new Fixture();
        File.WriteAllBytes(fixture.GatewayScreenshot, new byte[] { 1, 2, 3 });

        var run = fixture.Invoke(
            "$script:lockedStream = $null\n" +
            "$invoker = { param($arguments) if ($arguments[0] -eq 'window') { return [pscustomobject]@{ data = @('one') } }; $script:lockedStream = [IO.File]::Open($desktopScreenshot, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None); $script:lockedStream.WriteByte(9); throw 'desktop screenshot cancelled' }\n" +
            "try { $result = Get-GatewaySmokeFlaUiDesktopEvidence -GatewayScreenshotPath $gatewayScreenshot -FlaUiScreenshotPath $desktopScreenshot -ArtifactPath $artifact -FlaUiInvoker $invoker } finally { if ($null -ne $script:lockedStream) { $script:lockedStream.Dispose() } }\n" +
            "$result | ConvertTo-Json -Compress -Depth 5");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput, Does.Contain("\"Status\":\"unavailable\""));
            Assert.That(run.StandardOutput, Does.Contain("desktop screenshot cancelled"));
            Assert.That(File.Exists(fixture.Artifact), Is.True);
        });
    }

    [Test]
    public void Successful_desktop_capture_retains_the_real_file_and_window_count()
    {
        using var fixture = new Fixture();
        File.WriteAllBytes(fixture.GatewayScreenshot, new byte[] { 1, 2, 3 });

        var run = fixture.Invoke(
            "$invoker = { param($arguments) if ($arguments[0] -eq 'window') { return [pscustomobject]@{ data = @('one','two') } }; [IO.File]::WriteAllBytes($desktopScreenshot, [byte[]](9,8,7)); return [pscustomobject]@{ success = $true } }\n" +
            "$result = Get-GatewaySmokeFlaUiDesktopEvidence -GatewayScreenshotPath $gatewayScreenshot -FlaUiScreenshotPath $desktopScreenshot -ArtifactPath $artifact -FlaUiInvoker $invoker\n" +
            "$result | ConvertTo-Json -Compress -Depth 5");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput, Does.Contain("\"Status\":\"completed\""));
            Assert.That(run.StandardOutput, Does.Contain("\"WindowCount\":2"));
            Assert.That(run.StandardOutput, Does.Contain(fixture.DesktopScreenshot.Replace("\\", "\\\\")));
            Assert.That(new FileInfo(fixture.DesktopScreenshot).Length, Is.EqualTo(3));
        });
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string root = Path.Combine(
            Path.GetTempPath(),
            "GatewaySmokeFlaUiEvidenceTests",
            Guid.NewGuid().ToString("N"));

        public Fixture()
        {
            Directory.CreateDirectory(root);
        }

        public string GatewayScreenshot => Path.Combine(root, "gateway.png");
        public string DesktopScreenshot => Path.Combine(root, "desktop.png");
        public string Artifact => Path.Combine(root, "flaui-evidence.json");

        public InvocationResult Invoke(string operation)
        {
            var smoke = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
            var invocation = Path.Combine(root, "invoke.ps1");
            var script =
                "$ErrorActionPreference = 'Stop'\nSet-StrictMode -Version Latest\n" +
                "$tokens = $null; $errors = $null\n" +
                $"$ast = [Management.Automation.Language.Parser]::ParseFile({Literal(smoke)}, [ref]$tokens, [ref]$errors)\n" +
                "$functionAst = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Get-GatewaySmokeFlaUiDesktopEvidence' }, $true)\n" +
                "if ($null -eq $functionAst) { throw 'FlaUI evidence helper was not found.' }\n" +
                "Invoke-Expression $functionAst.Extent.Text\n" +
                $"$gatewayScreenshot = {Literal(GatewayScreenshot)}\n" +
                $"$desktopScreenshot = {Literal(DesktopScreenshot)}\n" +
                $"$artifact = {Literal(Artifact)}\n" +
                "try {\n" + operation + "\nexit 0\n}\n" +
                "catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }\n";
            File.WriteAllText(invocation, script, new UTF8Encoding(false));
            return RunPowerShell(invocation);
        }

        public void Dispose()
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static InvocationResult RunPowerShell(string path)
    {
        var start = new ProcessStartInfo
        {
            FileName = "pwsh.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{path}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start pwsh.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000))
        {
            process.Kill();
            throw new TimeoutException("FlaUI evidence helper probe timed out.");
        }
        return new InvocationResult(process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
    }

    private static string FindSourceRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "scripts", "Invoke-GatewaySmoke.ps1")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate source repository root.");
    }

    private static string Literal(string value) => "'" + value.Replace("'", "''") + "'";

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
