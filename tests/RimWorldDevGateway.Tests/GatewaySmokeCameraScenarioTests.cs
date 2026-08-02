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
public sealed class GatewaySmokeCameraScenarioTests
{
    [Test]
    public void Movement_target_is_the_farthest_queried_object_that_can_be_centered_without_clamping()
    {
        var camera = "{\"Center\":{\"X\":50,\"Z\":50},\"ViewRect\":{\"MinX\":40,\"MinZ\":40,\"MaxX\":60,\"MaxZ\":60},\"MapWidth\":100,\"MapHeight\":100}";
        var candidates = "[" +
            "{\"Handle\":\"unsafe-edge\",\"Position\":{\"X\":99,\"Z\":50}}," +
            "{\"Handle\":\"too-near\",\"Position\":{\"X\":55,\"Z\":50}}," +
            "{\"Handle\":\"safe-target\",\"Position\":{\"X\":80,\"Z\":55}}]";

        var result = Invoke(camera, candidates, 20);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.StandardOutput.Trim(), Is.EqualTo("safe-target|925"));
        });
    }

    [Test]
    public void Movement_target_uses_the_camera_captured_immediately_before_the_move()
    {
        var camera = "{\"Center\":{\"X\":80,\"Z\":50},\"ViewRect\":{\"MinX\":70,\"MinZ\":40,\"MaxX\":90,\"MaxZ\":60},\"MapWidth\":100,\"MapHeight\":100}";
        var candidates = "[" +
            "{\"Handle\":\"old-origin\",\"Position\":{\"X\":50,\"Z\":50}}," +
            "{\"Handle\":\"near-current\",\"Position\":{\"X\":75,\"Z\":50}}]";

        var result = Invoke(camera, candidates, 20);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.StandardOutput.Trim(), Is.EqualTo("old-origin|900"));
        });
    }

    private static InvocationResult Invoke(string cameraJson, string candidatesJson, int minimumDistance)
    {
        var root = Path.Combine(Path.GetTempPath(), "GatewaySmokeCameraScenarioTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var smokePath = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
            var invocationPath = Path.Combine(root, "invoke.ps1");
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                "$functionAst = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Select-GatewayCameraMovementTarget' }, $true)\n" +
                "if ($null -eq $functionAst) { throw 'Function was not found: Select-GatewayCameraMovementTarget' }\n" +
                "Invoke-Expression $functionAst.Extent.Text\n" +
                $"$camera = {PowerShellLiteral(cameraJson)} | ConvertFrom-Json\n" +
                $"$candidates = @({PowerShellLiteral(candidatesJson)} | ConvertFrom-Json)\n" +
                $"$selected = Select-GatewayCameraMovementTarget -Camera $camera -Candidates $candidates -MinimumDistance {minimumDistance}\n" +
                "Write-Output ([string]$selected.Thing.Handle + '|' + [string]$selected.DistanceSquared)\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));
            return RunPowerShell(invocationPath);
        }
        finally
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
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start pwsh.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000))
        {
            process.Kill();
            throw new TimeoutException("Gateway smoke camera target probe timed out.");
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
