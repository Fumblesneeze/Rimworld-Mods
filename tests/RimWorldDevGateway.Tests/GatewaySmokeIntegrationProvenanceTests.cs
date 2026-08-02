using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class GatewaySmokeIntegrationProvenanceTests
{
    private const string Owner = "fixture.owner";
    private const string ManifestName = "Fixture.IntegrationTests.integrationtests.json";
    private const string SourceIdentity = Owner + "/" + ManifestName;

    [Test]
    public void Staged_record_reads_the_exact_full_assembly_identity_and_runtime_source_identity()
    {
        using var fixture = Fixture.Create();

        var run = fixture.Invoke("$expected | ConvertTo-Json -Compress");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput, Does.Contain(SourceIdentity));
            Assert.That(run.StandardOutput, Does.Contain(Assembly.GetExecutingAssembly().FullName));
            Assert.That(run.StandardOutput, Does.Contain("Fixture.IntegrationTests.dll"));
        });
    }

    [Test]
    public void Exact_staged_identity_maps_one_to_one_to_discovery_descriptors_and_results()
    {
        using var fixture = Fixture.Create();

        var run = fixture.InvokeAssertion(fixture.ValidSnapshot);

        Assert.That(run.ExitCode, Is.Zero, run.StandardError);
    }

    [Test]
    public void Every_staged_assembly_must_contribute_at_least_one_discovered_test()
    {
        using var fixture = Fixture.Create();

        var run = fixture.InvokeAssertion(fixture.WithoutDescriptorsAndResults());

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("no discovered test").IgnoreCase);
        });
    }

    [TestCase("\"DiscoveredAssemblyCount\":1", "\"DiscoveredAssemblyCount\":2", "count")]
    [TestCase(SourceIdentity, Owner + "/Wrong.integrationtests.json", "source")]
    [TestCase("\"AssemblyIdentity\":\"__ASSEMBLY__\"", "\"AssemblyIdentity\":\"Wrong, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\"", "assembly")]
    [TestCase("\"OwningPackageId\":\"fixture.owner\",\"AssemblyIdentity\":\"__ASSEMBLY__\",\"TestName\":\"Fixture.Tests.Passes\",\"RunAt\":\"MainMenuLoaded\"", "\"OwningPackageId\":\"other.owner\",\"AssemblyIdentity\":\"__ASSEMBLY__\",\"TestName\":\"Fixture.Tests.Passes\",\"RunAt\":\"MainMenuLoaded\"", "descriptor")]
    public void Count_source_assembly_and_descriptor_provenance_mismatches_are_rejected(
        string original,
        string replacement,
        string expectedMessage)
    {
        using var fixture = Fixture.Create();
        original = original.Replace("__ASSEMBLY__", fixture.AssemblyIdentity);
        replacement = replacement.Replace("__ASSEMBLY__", fixture.AssemblyIdentity);
        var malformed = fixture.ValidSnapshot.Replace(original, replacement);

        var run = fixture.InvokeAssertion(malformed);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain(expectedMessage).IgnoreCase);
        });
    }

    [Test]
    public void Every_result_must_match_one_exact_discovered_descriptor_and_assembly()
    {
        using var fixture = Fixture.Create();
        var wrongResult = fixture.ValidSnapshot.Replace(
            $"\"AssemblyIdentity\":\"{fixture.AssemblyIdentity}\",\"TestName\":\"Fixture.Tests.Passes\",\"RunAt\":\"MainMenuLoaded\",\"State\":\"passed\"",
            "\"AssemblyIdentity\":\"Unstaged, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\",\"TestName\":\"Fixture.Tests.Passes\",\"RunAt\":\"MainMenuLoaded\",\"State\":\"passed\"");

        var run = fixture.InvokeAssertion(wrongResult);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("result").IgnoreCase);
        });
    }

    [Test]
    public void Duplicate_result_identity_is_rejected_as_not_one_to_one()
    {
        using var fixture = Fixture.Create();

        var run = fixture.InvokeAssertion(fixture.WithDuplicateResult());

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("result identity").IgnoreCase);
            Assert.That(run.StandardError, Does.Contain("one-to-one"));
        });
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string root;
        private readonly string smokePath;
        private readonly string assemblyPath;
        private readonly string manifestPath;

        private Fixture(
            string root,
            string smokePath,
            string assemblyPath,
            string manifestPath,
            string assemblyIdentity)
        {
            this.root = root;
            this.smokePath = smokePath;
            this.assemblyPath = assemblyPath;
            this.manifestPath = manifestPath;
            AssemblyIdentity = assemblyIdentity;
            ValidSnapshot = Snapshot(assemblyIdentity);
        }

        public string AssemblyIdentity { get; }
        public string ValidSnapshot { get; }

        public static Fixture Create()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "GatewaySmokeIntegrationProvenanceTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var assemblyPath = Path.Combine(root, "Fixture.IntegrationTests.dll");
            File.Copy(Assembly.GetExecutingAssembly().Location, assemblyPath);
            var manifestPath = Path.Combine(root, ManifestName);
            File.WriteAllText(manifestPath, "{}", new UTF8Encoding(false));
            return new Fixture(
                root,
                Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1"),
                assemblyPath,
                manifestPath,
                Assembly.GetExecutingAssembly().FullName);
        }

        public InvocationResult InvokeAssertion(string snapshot)
        {
            var snapshotPath = Path.Combine(root, $"snapshot-{Guid.NewGuid():N}.json");
            File.WriteAllText(snapshotPath, snapshot, new UTF8Encoding(false));
            return Invoke(
                $"$snapshot = Get-Content -LiteralPath {PowerShellLiteral(snapshotPath)} -Raw | ConvertFrom-Json\n" +
                "Assert-IntegrationTestAssemblyProvenance -Snapshot $snapshot -ExpectedAssemblies @($expected)");
        }

        public string WithDuplicateResult()
        {
            const string marker = "\"Results\":[";
            var resultStart = ValidSnapshot.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
            var resultEnd = ValidSnapshot.LastIndexOf("]}", StringComparison.Ordinal);
            if (resultStart < marker.Length || resultEnd <= resultStart)
            {
                throw new InvalidOperationException("The fixture snapshot has no result array.");
            }

            var result = ValidSnapshot.Substring(resultStart, resultEnd - resultStart);
            return ValidSnapshot.Insert(resultEnd, "," + result);
        }

        public string WithoutDescriptorsAndResults()
        {
            var escapedIdentity = AssemblyIdentity.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return
                "{\"DiscoveredAssemblyCount\":1,\"DiscoveredTestCount\":0," +
                "\"DiscoveredAssemblies\":[{\"OwningPackageId\":\"fixture.owner\"," +
                "\"SourceIdentity\":\"" + SourceIdentity + "\",\"AssemblyIdentity\":\"" +
                escapedIdentity + "\"}],\"DiscoveredTests\":[],\"Results\":[]}";
        }

        public InvocationResult Invoke(string operation)
        {
            var invocationPath = Path.Combine(root, $"invoke-{Guid.NewGuid():N}.ps1");
            var functionNames = new[]
            {
                "Get-StagedIntegrationTestAssemblyRecord",
                "Assert-IntegrationTestAssemblyProvenance"
            };
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                $"$functionNames = @({string.Join(",", functionNames.Select(PowerShellLiteral))})\n" +
                "foreach ($functionName in $functionNames) {\n" +
                "  $functionAst = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $functionName }, $true)\n" +
                "  if ($null -eq $functionAst) { throw \"Function was not found: $functionName\" }\n" +
                "  Invoke-Expression $functionAst.Extent.Text\n" +
                "}\n" +
                $"$expected = Get-StagedIntegrationTestAssemblyRecord -OwnerPackageId {PowerShellLiteral(Owner)} -AssemblyPath {PowerShellLiteral(assemblyPath)} -ManifestPath {PowerShellLiteral(manifestPath)}\n" +
                "try {\n" + operation + "\nexit 0\n}\n" +
                "catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));
            return RunPowerShell(invocationPath);
        }

        public void Dispose() => Directory.Delete(root, recursive: true);

        private static string Snapshot(string assemblyIdentity)
        {
            var escapedIdentity = assemblyIdentity.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return (
                "{\"DiscoveredAssemblyCount\":1,\"DiscoveredTestCount\":1," +
                "\"DiscoveredAssemblies\":[{\"OwningPackageId\":\"fixture.owner\"," +
                "\"SourceIdentity\":\"" + SourceIdentity + "\",\"AssemblyIdentity\":\"__ASSEMBLY__\"}]," +
                "\"DiscoveredTests\":[{\"OwningPackageId\":\"fixture.owner\"," +
                "\"AssemblyIdentity\":\"__ASSEMBLY__\",\"TestName\":\"Fixture.Tests.Passes\"," +
                "\"RunAt\":\"MainMenuLoaded\"}]," +
                "\"Results\":[{\"OwningPackageId\":\"fixture.owner\"," +
                "\"AssemblyIdentity\":\"__ASSEMBLY__\",\"TestName\":\"Fixture.Tests.Passes\"," +
                "\"RunAt\":\"MainMenuLoaded\",\"State\":\"passed\"}]}"
            ).Replace("__ASSEMBLY__", escapedIdentity);
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
            throw new TimeoutException("Gateway integration provenance probe timed out.");
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
