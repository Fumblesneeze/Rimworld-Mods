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
public sealed class GatewaySmokeManifestValidationTests
{
    private const int ProcessId = 4242;
    private const string ProcessStartUtc = "2026-08-01T12:34:56.1234567+00:00";
    private const string RunId = "0123456789abcdef0123456789abcdef";

    [Test]
    public void Exact_active_manifest_for_the_owned_process_is_admitted()
    {
        using var fixture = Fixture.Create();

        var run = fixture.Invoke();

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo(
                $"{RunId}|http://127.0.0.1:16241/api/v1|{ProcessId}"));
        });
    }

    [TestCase("\"processId\":4242", "\"processId\":\"4242\"")]
    [TestCase("\"unrestrictedExecution\":true", "\"unrestrictedExecution\":\"true\"")]
    [TestCase("\"token\":\"abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG\"", "\"token\":null")]
    [TestCase("\"warning\":\"Developer-only unrestricted gateway.\"", "\"warning\":42")]
    [TestCase("\"warning\":\"Developer-only unrestricted gateway.\"", "\"warning\":\"Developer-only unrestricted gateway.\",\"extra\":true")]
    [TestCase(",\"warning\":\"Developer-only unrestricted gateway.\"", "")]
    [TestCase("\"state\":\"active\"", "\"state\":\"active\",\"state\":\"active\"")]
    public void Schema_names_types_and_uniqueness_are_exact(string original, string replacement)
    {
        using var fixture = Fixture.Create();
        fixture.Replace(original, replacement);

        var run = fixture.Invoke();

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("manifest"));
        });
    }

    [TestCase("\"runId\":\"0123456789abcdef0123456789abcdef\"", "\"runId\":\"../escape\"")]
    [TestCase("http://127.0.0.1:16241/api/v1", "http://localhost:16241/api/v1")]
    [TestCase("http://127.0.0.1:16241/api/v1", "https://127.0.0.1:16241/api/v1")]
    [TestCase("http://127.0.0.1:16241/api/v1", "http://127.0.0.1:16241/api/v1/")]
    [TestCase("http://127.0.0.1:16241/api/v1", "http://127.0.0.1:16241/api/v1?token=x")]
    [TestCase("http://127.0.0.1:16241/api/v1", "http://user@127.0.0.1:16241/api/v1")]
    [TestCase("\"state\":\"active\"", "\"state\":\"stopped\"")]
    [TestCase("\"apiVersion\":\"1\"", "\"apiVersion\":\"v1\"")]
    public void Unsafe_identity_or_non_exact_loopback_api_base_is_rejected(
        string original,
        string replacement)
    {
        using var fixture = Fixture.Create();
        fixture.Replace(original, replacement);

        var run = fixture.Invoke();

        Assert.That(run.ExitCode, Is.EqualTo(1), run.StandardError);
    }

    [Test]
    public void Exact_pid_and_process_start_are_both_required_before_credentials_are_returned()
    {
        using var fixture = Fixture.Create();

        var wrongPid = fixture.Invoke(expectedProcessId: ProcessId + 1);
        var wrongStart = fixture.Invoke(expectedProcessStartUtc: "2026-08-01T12:34:57.1234567+00:00");

        Assert.Multiple(() =>
        {
            Assert.That(wrongPid.ExitCode, Is.EqualTo(1));
            Assert.That(wrongPid.StandardError, Does.Contain("PID"));
            Assert.That(wrongStart.ExitCode, Is.EqualTo(1));
            Assert.That(wrongStart.StandardError, Does.Contain("process start"));
            Assert.That(wrongPid.StandardOutput, Does.Not.Contain("abcdefghijklmnopqrstuvwxyz"));
            Assert.That(wrongStart.StandardOutput, Does.Not.Contain("abcdefghijklmnopqrstuvwxyz"));
        });
    }

    [Test]
    public void Oversized_manifest_is_rejected_before_json_is_trusted()
    {
        using var fixture = Fixture.Create();
        fixture.Replace(
            "\"warning\":\"Developer-only unrestricted gateway.\"",
            $"\"warning\":\"{new string('x', 70 * 1024)}\"");

        var run = fixture.Invoke();

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("64 KiB"));
        });
    }

    [Test]
    public void Manifest_reached_through_a_directory_junction_is_rejected()
    {
        using var fixture = Fixture.Create(useJunction: true);

        var run = fixture.Invoke();

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("reparse point"));
        });
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string root;
        private readonly string smokePath;
        private readonly string invocationManifestPath;
        private readonly string physicalManifestPath;

        private Fixture(
            string root,
            string smokePath,
            string savedDataPath,
            string invocationManifestPath,
            string physicalManifestPath)
        {
            this.root = root;
            this.smokePath = smokePath;
            SavedDataPath = savedDataPath;
            this.invocationManifestPath = invocationManifestPath;
            this.physicalManifestPath = physicalManifestPath;
        }

        public string SavedDataPath { get; }

        public static Fixture Create(bool useJunction = false)
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "GatewaySmokeManifestValidationTests",
                Guid.NewGuid().ToString("N"));
            var savedDataPath = Path.Combine(root, "SavedData");
            var gatewayPath = Path.Combine(savedDataPath, "DevGateway");
            var physicalGatewayPath = useJunction
                ? Path.Combine(root, "PhysicalDevGateway")
                : gatewayPath;
            Directory.CreateDirectory(physicalGatewayPath);
            if (useJunction)
            {
                Directory.CreateDirectory(savedDataPath);
                CreateJunction(gatewayPath, physicalGatewayPath);
            }

            var invocationManifestPath = Path.Combine(gatewayPath, "current.json");
            var physicalManifestPath = Path.Combine(physicalGatewayPath, "current.json");
            File.WriteAllText(physicalManifestPath, ValidManifest, new UTF8Encoding(false));
            return new Fixture(
                root,
                Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1"),
                savedDataPath,
                invocationManifestPath,
                physicalManifestPath);
        }

        public void Replace(string original, string replacement)
        {
            var current = File.ReadAllText(physicalManifestPath);
            File.WriteAllText(
                physicalManifestPath,
                current.Replace(original, replacement),
                new UTF8Encoding(false));
        }

        public InvocationResult Invoke(
            int expectedProcessId = ProcessId,
            string expectedProcessStartUtc = ProcessStartUtc)
        {
            var invocationPath = Path.Combine(root, $"invoke-{Guid.NewGuid():N}.ps1");
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                "$functionAst = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Read-ValidatedGatewayCurrentManifest' }, $true)\n" +
                "if ($null -eq $functionAst) { throw 'Function was not found.' }\n" +
                "Invoke-Expression $functionAst.Extent.Text\n" +
                "try {\n" +
                $"  $manifest = Read-ValidatedGatewayCurrentManifest -Path {PowerShellLiteral(invocationManifestPath)} -SavedDataPath {PowerShellLiteral(SavedDataPath)} -ExpectedProcessId {expectedProcessId} -ExpectedProcessStartUtc ([datetimeoffset]{PowerShellLiteral(expectedProcessStartUtc)})\n" +
                "  Write-Output ([string]$manifest.runId + '|' + [string]$manifest.baseUrl + '|' + [string]$manifest.processId)\n" +
                "  exit 0\n" +
                "}\ncatch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));
            return RunPowerShell(invocationPath);
        }

        public void Dispose()
        {
            var junctionPath = Path.Combine(SavedDataPath, "DevGateway");
            if (Directory.Exists(junctionPath) &&
                (File.GetAttributes(junctionPath) & FileAttributes.ReparsePoint) != 0)
            {
                Directory.Delete(junctionPath);
            }

            Directory.Delete(root, recursive: true);
        }

        private static void CreateJunction(string junctionPath, string targetPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/d /c mklink /J \"{junctionPath}\" \"{targetPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo) ??
                throw new InvalidOperationException("Could not create the test junction.");
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Could not create the test junction: {output}{error}");
            }
        }
    }

    private static readonly string ValidManifest =
        "{\"apiVersion\":\"1\",\"runId\":\"" + RunId + "\",\"state\":\"active\"," +
        "\"baseUrl\":\"http://127.0.0.1:16241/api/v1\"," +
        "\"token\":\"abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG\"," +
        "\"processId\":" + ProcessId + ",\"processStartUtc\":\"" + ProcessStartUtc + "\"," +
        "\"startedUtc\":\"2026-08-01T12:34:55.1234567+00:00\"," +
        "\"gameVersion\":\"1.6.4633 rev1073\",\"modVersion\":\"0.1.0\"," +
        "\"unrestrictedExecution\":true,\"warning\":\"Developer-only unrestricted gateway.\"}";

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
            throw new TimeoutException("Gateway manifest-validation smoke probe timed out.");
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
