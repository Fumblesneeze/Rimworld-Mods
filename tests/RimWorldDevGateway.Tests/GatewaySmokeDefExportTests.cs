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
public sealed class GatewaySmokeDefExportTests
{
    private const string ValidBody =
        "{\"ok\":true,\"result\":{\"IsCanonicalSourceXml\":false," +
        "\"Items\":[{\"DatabaseType\":\"Verse.ThingDef\",\"DefName\":\"Steel\"," +
        "\"Fields\":{\"label\":\"steel\",\"statBases\":[{\"stat\":\"MaxHitPoints\"}]," +
        "\"modExtensions\":[{\"marker\":\"patched-by-gateway-xml\"}]},\"Warnings\":[]}]," +
        "\"Truncated\":false,\"NextCursor\":null,\"SourceWarnings\":[]}}";

    [Test]
    public void Smoke_request_selects_the_three_exact_patch_authoring_fields()
    {
        using var fixture = Fixture.Create();

        var run = fixture.InvokeRequest();

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput, Does.Contain("\"fieldNames\":[\"label\",\"statBases\",\"modExtensions\"]"));
            Assert.That(run.StandardOutput, Does.Contain("\"defTypes\":[\"Verse.ThingDef\"]"));
            Assert.That(run.StandardOutput, Does.Contain("\"defNames\":[\"Steel\"]"));
        });
    }

    [Test]
    public void Complete_single_Steel_projection_with_finalized_patch_marker_is_accepted()
    {
        using var fixture = Fixture.Create();

        var run = fixture.InvokeAssertion(200, ValidBody);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("Steel"));
            Assert.That(File.ReadAllText(fixture.ArtifactPath).Trim(), Is.EqualTo(ValidBody));
        });
    }

    [TestCase("\"label\":\"steel\",")]
    [TestCase("\"statBases\":[{\"stat\":\"MaxHitPoints\"}],")]
    [TestCase("\"modExtensions\":[{\"marker\":\"patched-by-gateway-xml\"}]")]
    public void Missing_requested_field_is_rejected(string fieldFragment)
    {
        using var fixture = Fixture.Create();

        var run = fixture.InvokeAssertion(200, ValidBody.Replace(fieldFragment, string.Empty));

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("exactly label, statBases, and modExtensions"));
        });
    }

    [TestCase("\"label\":\"steel\"", "\"label\":\"\"", "non-empty label")]
    [TestCase("\"statBases\":[{\"stat\":\"MaxHitPoints\"}]", "\"statBases\":[]", "non-empty statBases")]
    [TestCase("patched-by-gateway-xml", "not-the-finalized-marker", "finalized XML-patch marker")]
    [TestCase("\"Truncated\":false", "\"Truncated\":true", "complete, untruncated")]
    [TestCase("\"NextCursor\":null", "\"NextCursor\":\"v1.next\"", "null NextCursor")]
    [TestCase("\"DefName\":\"Steel\"", "\"DefName\":\"WoodLog\"", "Steel Verse.ThingDef")]
    [TestCase("\"DatabaseType\":\"Verse.ThingDef\"", "\"DatabaseType\":\"Verse.StatDef\"", "Steel Verse.ThingDef")]
    public void Incomplete_or_wrong_projection_is_rejected(
        string original,
        string replacement,
        string expectedError)
    {
        using var fixture = Fixture.Create();

        var run = fixture.InvokeAssertion(200, ValidBody.Replace(original, replacement));

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain(expectedError));
        });
    }

    [TestCase("$byteLimit")]
    [TestCase("$projection")]
    [TestCase("$projectionLimit")]
    public void Projection_failure_markers_are_rejected(string marker)
    {
        using var fixture = Fixture.Create();
        var body = ValidBody.Replace(
            "\"label\":\"steel\"",
            $"\"label\":\"steel\",\"{marker}\":{{\"Kind\":\"limit\"}}");

        var run = fixture.InvokeAssertion(200, body);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("projection failure marker"));
        });
    }

    [TestCase("limit")]
    [TestCase("projectionError")]
    public void Nested_projection_failure_kinds_are_rejected_recursively(string kind)
    {
        using var fixture = Fixture.Create();
        var body = ValidBody.Replace(
            "{\"stat\":\"MaxHitPoints\"}",
            $"{{\"stat\":\"MaxHitPoints\",\"nested\":[{{\"Kind\":\"{kind}\"}}]}}");

        var run = fixture.InvokeAssertion(200, body);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("projection failure marker"));
        });
    }

    [TestCase("\"Warnings\":[]", "\"Warnings\":[{\"Code\":\"string_truncated\"}]")]
    [TestCase("\"SourceWarnings\":[]", "\"SourceWarnings\":[{\"Code\":\"def_database_enumeration_failed\"}]")]
    public void Any_item_or_source_warning_rejects_the_exact_Steel_smoke(
        string original,
        string replacement)
    {
        using var fixture = Fixture.Create();

        var run = fixture.InvokeAssertion(200, ValidBody.Replace(original, replacement));

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("warnings"));
        });
    }

    [TestCase(503, "{\"ok\":false,\"error\":{\"code\":\"not_ready\"}}")]
    [TestCase(200, "{\"ok\":false,\"error\":{\"code\":\"failed\"}}")]
    public void Http_or_envelope_failure_remains_fatal_and_is_retained(int statusCode, string body)
    {
        using var fixture = Fixture.Create();

        var run = fixture.InvokeAssertion(statusCode, body);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain($"HTTP {statusCode}"));
            Assert.That(File.ReadAllText(fixture.ArtifactPath).Trim(), Is.EqualTo(body));
        });
    }

    [Test]
    public void Body_over_32_MiB_is_rejected_before_projection_is_trusted()
    {
        using var fixture = Fixture.Create();
        var oversized = ValidBody.Replace(
            "\"IsCanonicalSourceXml\":false",
            $"\"Padding\":\"{new string('x', (32 * 1024 * 1024) + 1)}\",\"IsCanonicalSourceXml\":false");

        var run = fixture.InvokeAssertion(200, oversized, timeoutMilliseconds: 60000);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("32 MiB"));
            Assert.That(new FileInfo(fixture.ArtifactPath).Length, Is.GreaterThan(32L * 1024 * 1024));
        });
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string root;
        private readonly string smokePath;

        private Fixture(string root, string smokePath)
        {
            this.root = root;
            this.smokePath = smokePath;
            ArtifactPath = Path.Combine(root, "def-export.json");
        }

        public string ArtifactPath { get; }

        public static Fixture Create()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "GatewaySmokeDefExportTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new Fixture(
                root,
                Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1"));
        }

        public InvocationResult InvokeRequest() => InvokePowerShell(
            "New-GatewayDefSmokeRequest | ConvertTo-Json -Compress -Depth 5",
            timeoutMilliseconds: 30000);

        public InvocationResult InvokeAssertion(int statusCode, string body, int timeoutMilliseconds = 30000)
        {
            var responsePath = Path.Combine(root, $"response-{Guid.NewGuid():N}.json");
            File.WriteAllText(responsePath, body, new UTF8Encoding(false));
            return InvokePowerShell(
                $"$response = [pscustomobject]@{{ StatusCode = {statusCode}; Content = [System.IO.File]::ReadAllText({PowerShellLiteral(responsePath)}) }}\n" +
                $"$envelope = Assert-GatewayDefExportSmokeResult -Response $response -ArtifactPath {PowerShellLiteral(ArtifactPath)}\n" +
                "Write-Output $envelope.result.Items[0].DefName",
                timeoutMilliseconds);
        }

        private InvocationResult InvokePowerShell(string operation, int timeoutMilliseconds)
        {
            var invocationPath = Path.Combine(root, $"invoke-{Guid.NewGuid():N}.ps1");
            var functionNames = new[]
            {
                "Assert-GatewaySuccess",
                "New-GatewayDefSmokeRequest",
                "Test-GatewayDefProjectionFailureMarker",
                "Assert-GatewayDefExportSmokeResult"
            };
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "Set-StrictMode -Version Latest\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                $"$functionNames = @({string.Join(",", functionNames.Select(PowerShellLiteral))})\n" +
                "foreach ($functionName in $functionNames) {\n" +
                "  $functionAst = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $functionName }, $true)\n" +
                "  if ($null -eq $functionAst) { [Console]::Error.WriteLine(\"Function was not found: $functionName\"); exit 91 }\n" +
                "  Invoke-Expression $functionAst.Extent.Text\n" +
                "}\n" +
                "try {\n" + operation + "\nexit 0\n}\n" +
                "catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));
            return RunPowerShell(invocationPath, timeoutMilliseconds);
        }

        public void Dispose() => Directory.Delete(root, recursive: true);
    }

    private static InvocationResult RunPowerShell(string path, int timeoutMilliseconds)
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
        if (!process.WaitForExit(timeoutMilliseconds))
        {
            process.Kill();
            throw new TimeoutException("Gateway Def-export smoke probe timed out.");
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
