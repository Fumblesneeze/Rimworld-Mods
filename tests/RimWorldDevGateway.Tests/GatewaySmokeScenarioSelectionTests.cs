using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class GatewaySmokeScenarioSelectionTests
{
    [Test]
    public void Quicktest_is_quiet_until_a_named_scenario_is_explicitly_selected()
    {
        var result = InvokeScenarioResolver();

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.StandardOutput.Trim(), Is.EqualTo(
                "none|False|;gateway-regression|True|;caravan-dining|False|caravan-dining.json"));
        });
    }

    [Test]
    public void Scenario_package_requirements_are_checked_against_the_supplied_package_state()
    {
        var script =
            "$ErrorActionPreference = 'Stop'\n" +
            LoadFunction("Test-GatewayPackageId") +
            LoadFunction("Assert-GatewayScenarioRequiredPackages") +
            "$plan = [pscustomobject]@{ Name = 'fixture'; Descriptor = [pscustomobject]@{ requiredPackageIds = @('core', 'product') } }\n" +
            "Assert-GatewayScenarioRequiredPackages -ScenarioPlan $plan -PackageIds @('core', 'product') -PackageState 'configured'\n" +
            "try { Assert-GatewayScenarioRequiredPackages -ScenarioPlan $plan -PackageIds @('core') -PackageState 'loaded'; throw 'Expected rejection.' } catch { Write-Output $_.Exception.Message }\n";

        var result = RunPowerShellScript(script);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.StandardOutput, Does.Contain("requires loaded mod 'product'"));
        });
    }

    [Test]
    public void Scenario_screenshot_names_are_unique_and_scenario_scoped()
    {
        var duplicate = InvokeScenarioResolver(
            "{\"schemaVersion\":1,\"name\":\"caravan-dining\",\"requiredPackageIds\":[],\"steps\":[" +
            "{\"id\":\"before\",\"kind\":\"screenshot\",\"fileName\":\"scenario-view.png\"}," +
            "{\"id\":\"after\",\"kind\":\"screenshot\",\"fileName\":\"scenario-view.png\"}]}");
        var reserved = InvokeScenarioResolver(
            "{\"schemaVersion\":1,\"name\":\"caravan-dining\",\"requiredPackageIds\":[],\"steps\":[" +
            "{\"id\":\"overwrite\",\"kind\":\"screenshot\",\"fileName\":\"gateway-screenshot.png\"}]}");

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.ExitCode, Is.Not.Zero);
            Assert.That(duplicate.StandardError, Does.Contain("duplicate screenshot file name"));
            Assert.That(reserved.ExitCode, Is.Not.Zero);
            Assert.That(reserved.StandardError, Does.Contain("must begin with 'scenario-'"));
        });
    }

    [Test]
    public void Missing_final_player_log_is_rejected()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "Player.log");
        var script =
            "$ErrorActionPreference = 'Stop'\n" +
            LoadFunction("Assert-GatewayFinalPlayerLog") +
            $"try {{ Assert-GatewayFinalPlayerLog -Path {PowerShellLiteral(missingPath)} -BearerToken ''; throw 'Expected rejection.' }} catch {{ Write-Output $_.Exception.Message }}\n";

        var result = RunPowerShellScript(script);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.StandardOutput, Does.Contain("Final Player.log is missing"));
        });
    }

    [Test]
    public void Caravan_scenario_source_shape_clears_random_inventory_and_embeds_the_plate()
    {
        var sourcePath = Path.Combine(
            FindSourceRepositoryRoot(),
            "scripts",
            "Scenarios",
            "immersive-chefs-caravan-dining-setup.csx");
        var source = File.ReadAllText(sourcePath);

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("innerContainer.ClearAndDestroyContents()"));
            Assert.That(source, Does.Contain("TryEmbedPlate(plate)"));
            Assert.That(source, Does.Not.Contain("innerContainer.TryAdd(plate"));
        });
    }

    private static InvocationResult InvokeScenarioResolver(string? descriptorJson = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "GatewaySmokeScenarioSelectionTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "caravan-dining.json"),
                descriptorJson ?? "{\"schemaVersion\":1,\"name\":\"caravan-dining\",\"requiredPackageIds\":[],\"steps\":[]}",
                new UTF8Encoding(false));
            var smokePath = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
            var invocationPath = Path.Combine(root, "invoke.ps1");
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                "$functionAst = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Resolve-GatewaySmokeScenario' }, $true)\n" +
                "if ($null -eq $functionAst) { throw 'Function was not found: Resolve-GatewaySmokeScenario' }\n" +
                "Invoke-Expression $functionAst.Extent.Text\n" +
                $"$none = Resolve-GatewaySmokeScenario -ScenarioName '' -ScenarioDirectory {PowerShellLiteral(root)} -Quicktest $true\n" +
                $"$regression = Resolve-GatewaySmokeScenario -ScenarioName 'gateway-regression' -ScenarioDirectory {PowerShellLiteral(root)} -Quicktest $true\n" +
                $"$custom = Resolve-GatewaySmokeScenario -ScenarioName 'caravan-dining' -ScenarioDirectory {PowerShellLiteral(root)} -Quicktest $true\n" +
                "$parts = @($none, $regression, $custom) | ForEach-Object { [string]$_.Name + '|' + [string]$_.RunsGatewayRegression + '|' + [System.IO.Path]::GetFileName([string]$_.DescriptorPath) }\n" +
                "Write-Output ($parts -join ';')\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));
            return RunPowerShell(invocationPath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string LoadFunction(string functionName)
    {
        var smokePath = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
        return
            "$tokens = $null; $parseErrors = $null\n" +
            $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
            $"$functionAst = $ast.Find({{ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq '{functionName}' }}, $true)\n" +
            $"if ($null -eq $functionAst) {{ throw 'Function was not found: {functionName}' }}\n" +
            "Invoke-Expression $functionAst.Extent.Text\n";
    }

    private static InvocationResult RunPowerShellScript(string script)
    {
        var root = Path.Combine(Path.GetTempPath(), "GatewaySmokeScenarioSelectionTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var invocationPath = Path.Combine(root, "invoke.ps1");
            File.WriteAllText(invocationPath, script, new UTF8Encoding(false));
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
            throw new TimeoutException("Gateway scenario resolver probe timed out.");
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
