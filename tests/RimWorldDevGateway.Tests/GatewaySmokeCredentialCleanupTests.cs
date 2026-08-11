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
public sealed class GatewaySmokeCredentialCleanupTests
{
    private const int ProcessId = 4242;
    private const string RunId = "0123456789abcdef0123456789abcdef";
    private const string Token = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG";
    private const string SecondRunId = "fedcba9876543210fedcba9876543210";
    private const string SecondToken = "ABCDEFG0123456789abcdefghijklmnopqrstuvwxyz";

    [Test]
    public void Matching_locator_and_session_are_sanitized_only_after_exact_pid_death()
    {
        using var fixture = Fixture.Create();
        var refused = fixture.Invoke(processHasExited: false, includeExpectedIdentity: true);

        Assert.Multiple(() =>
        {
            Assert.That(refused.ExitCode, Is.EqualTo(1));
            Assert.That(refused.StandardError, Does.Contain("must be confirmed dead"));
            Assert.That(File.Exists(fixture.CurrentPath), Is.True);
            Assert.That(File.ReadAllText(fixture.SessionPath), Does.Contain(Token));
        });

        var sanitized = fixture.Invoke(processHasExited: true, includeExpectedIdentity: true);

        Assert.Multiple(() =>
        {
            Assert.That(sanitized.ExitCode, Is.Zero, sanitized.StandardError);
            Assert.That(File.Exists(fixture.CurrentPath), Is.False);
            Assert.That(File.Exists(fixture.SessionPath), Is.True);
            Assert.That(File.ReadAllText(fixture.SessionPath), Does.Not.Contain(Token));
            Assert.That(File.ReadAllText(fixture.SessionPath), Does.Contain("host-sanitized"));
        });
    }

    [Test]
    public void Matching_session_is_discovered_and_sanitized_when_startup_failed_before_manifest_parse()
    {
        using var fixture = Fixture.Create(deleteCurrent: true);
        var run = fixture.Invoke(processHasExited: true, includeExpectedIdentity: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(File.ReadAllText(fixture.SessionPath), Does.Not.Contain(Token));
            Assert.That(File.ReadAllText(fixture.SessionPath), Does.Contain("host-sanitized"));
        });
    }

    [Test]
    public void Startup_failure_sanitizes_an_exact_duplicate_token_even_when_the_last_value_is_empty()
    {
        using var fixture = Fixture.Create(
            deleteCurrent: true,
            manifestJson: DuplicateTokenManifestJson(ProcessId, RunId, Token, string.Empty, "active"));

        var run = fixture.Invoke(processHasExited: true, includeExpectedIdentity: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(File.Exists(fixture.SessionPath), Is.True);
            Assert.That(File.ReadAllText(fixture.SessionPath), Does.Not.Contain(Token));
            Assert.That(File.ReadAllText(fixture.SessionPath), Does.Contain("host-sanitized"));
        });
    }

    [Test]
    public void Startup_failure_sanitizes_case_variant_token_properties_without_leaving_either_value()
    {
        using var fixture = Fixture.Create(
            deleteCurrent: true,
            manifestJson: CaseVariantTokenManifestJson(ProcessId, RunId, Token, SecondToken, "active"));

        var run = fixture.Invoke(processHasExited: true, includeExpectedIdentity: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(File.Exists(fixture.SessionPath), Is.True);
            Assert.That(File.ReadAllText(fixture.SessionPath), Does.Not.Contain(Token));
            Assert.That(File.ReadAllText(fixture.SessionPath), Does.Not.Contain(SecondToken));
            Assert.That(File.ReadAllText(fixture.SessionPath), Does.Contain("host-sanitized"));
        });
    }

    [Test]
    public void Startup_failure_sanitizes_every_session_owned_by_the_exact_dead_pid_without_guessing_a_run_id()
    {
        using var fixture = Fixture.Create(deleteCurrent: true);
        var secondSessionPath = fixture.AddSession(
            SecondRunId,
            ManifestJson(ProcessId, SecondRunId, SecondToken, "active"));

        var run = fixture.Invoke(processHasExited: true, includeExpectedIdentity: false);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(File.ReadAllText(fixture.SessionPath), Does.Not.Contain(Token));
            Assert.That(File.ReadAllText(secondSessionPath), Does.Not.Contain(SecondToken));
            Assert.That(File.ReadAllText(fixture.SessionPath), Does.Contain("host-sanitized"));
            Assert.That(File.ReadAllText(secondSessionPath), Does.Contain("host-sanitized"));
            Assert.That(run.StandardOutput, Does.Contain("\"RunId\":null"));
            Assert.That(run.StandardOutput, Does.Contain("\"SessionsSanitized\":2"));
        });
    }

    [Test]
    public void Tokenless_stopped_tombstones_are_cleaned_under_the_real_strict_mode()
    {
        using var fixture = Fixture.Create(tokenlessTombstone: true);

        var run = fixture.Invoke(processHasExited: true, includeExpectedIdentity: true);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(File.Exists(fixture.CurrentPath), Is.False);
            Assert.That(File.Exists(fixture.SessionPath), Is.True);
            Assert.That(File.ReadAllText(fixture.SessionPath), Does.Not.Contain("\"token\""));
        });
    }

    [Test]
    public void Cleanup_fails_if_any_retained_artifact_still_contains_the_bearer_token()
    {
        using var fixture = Fixture.Create();
        File.WriteAllText(
            Path.Combine(fixture.RunDirectory, "unexpected.txt"),
            $"accidentally retained {Token}",
            new UTF8Encoding(false));

        var run = fixture.Invoke(processHasExited: true, includeExpectedIdentity: true);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("retains a bearer token"));
            Assert.That(File.Exists(fixture.CurrentPath), Is.False);
            Assert.That(File.ReadAllText(fixture.SessionPath), Does.Not.Contain(Token));
        });
    }

    [Test]
    public void Token_free_native_minidump_is_retained_for_hang_analysis()
    {
        using var fixture = Fixture.Create();
        File.WriteAllBytes(fixture.HangDumpPath, Encoding.ASCII.GetBytes("MDMP token-free diagnostic"));

        var run = fixture.Invoke(processHasExited: true, includeExpectedIdentity: true);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(File.Exists(fixture.HangDumpPath), Is.True);
            Assert.That(new FileInfo(fixture.HangDumpPath).Length, Is.GreaterThan(0));
        });
    }

    [Test]
    public void Native_minidump_containing_the_bearer_token_is_removed_before_artifacts_are_retained()
    {
        using var fixture = Fixture.Create();
        File.WriteAllBytes(
            fixture.HangDumpPath,
            Encoding.ASCII.GetBytes("MDMP accidental " + Token + " diagnostic"));

        var run = fixture.Invoke(processHasExited: true, includeExpectedIdentity: true);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(File.Exists(fixture.HangDumpPath), Is.False);
        });
    }

    [Test]
    public void Native_minidump_containing_a_utf16_bearer_token_is_removed_before_artifacts_are_retained()
    {
        using var fixture = Fixture.Create();
        File.WriteAllBytes(
            fixture.HangDumpPath,
            Encoding.Unicode.GetBytes("MDMP accidental " + Token + " diagnostic"));

        var run = fixture.Invoke(processHasExited: true, includeExpectedIdentity: true);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(File.Exists(fixture.HangDumpPath), Is.False);
        });
    }

    [Test]
    public void Native_minidump_is_deleted_fail_closed_when_credential_discovery_fails()
    {
        using var fixture = Fixture.Create();
        File.WriteAllText(fixture.CurrentPath, "{ malformed", new UTF8Encoding(false));
        File.WriteAllBytes(fixture.HangDumpPath, Encoding.ASCII.GetBytes("MDMP token-free diagnostic"));

        var run = fixture.Invoke(processHasExited: true, includeExpectedIdentity: true);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Not.Zero);
            Assert.That(File.Exists(fixture.HangDumpPath), Is.False);
            Assert.That(File.Exists(fixture.CurrentPath), Is.False);
            Assert.That(File.Exists(fixture.SessionPath), Is.True);
            Assert.That(File.ReadAllText(fixture.SessionPath), Does.Not.Contain(Token));
        });
    }

    [Test]
    public void Matching_shape_invalid_manifests_are_deleted_while_other_credentials_are_sanitized()
    {
        using var fixture = Fixture.Create();
        File.WriteAllText(
            fixture.CurrentPath,
            ManifestJson(ProcessId, "unsafe/run", Token, "active"),
            new UTF8Encoding(false));
        var inconsistentSession = fixture.AddSession(
            "other-run",
            ManifestJson(ProcessId, "different-run", Token, "active"));
        File.WriteAllBytes(fixture.HangDumpPath, Encoding.ASCII.GetBytes("MDMP token-free diagnostic"));

        var run = fixture.Invoke(processHasExited: true, includeExpectedIdentity: true);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Not.Zero);
            Assert.That(File.Exists(fixture.HangDumpPath), Is.False);
            Assert.That(File.Exists(fixture.CurrentPath), Is.False);
            Assert.That(File.Exists(inconsistentSession), Is.False);
            Assert.That(File.Exists(fixture.SessionPath), Is.True);
            Assert.That(File.ReadAllText(fixture.SessionPath), Does.Not.Contain(Token));
        });
    }

    [Test]
    public void Atomic_redaction_never_creates_a_credential_backup_when_the_host_exits_after_replace()
    {
        using var fixture = Fixture.Create();

        var run = fixture.InvokeAbruptAtomicRedaction();

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(73));
            Assert.That(File.Exists(fixture.SessionPath), Is.True);
            Assert.That(File.ReadAllText(fixture.SessionPath), Does.Not.Contain(Token));
            Assert.That(
                Directory.GetFiles(fixture.SessionDirectory, "*.sanitize-backup.*"),
                Is.Empty);
            Assert.That(
                Directory.GetFiles(fixture.SessionDirectory, "*.sanitize.*"),
                Is.Empty);
        });
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string root;
        private readonly string smokePath;

        private Fixture(string root, string smokePath, string runDirectory, string savedDataPath)
        {
            this.root = root;
            this.smokePath = smokePath;
            RunDirectory = runDirectory;
            SavedDataPath = savedDataPath;
            CurrentPath = Path.Combine(savedDataPath, "DevGateway", "current.json");
            SessionPath = Path.Combine(savedDataPath, "DevGateway", "Sessions", RunId, "session.json");
        }

        public string RunDirectory { get; }
        public string SavedDataPath { get; }
        public string CurrentPath { get; }
        public string SessionPath { get; }
        public string SessionDirectory => Path.GetDirectoryName(SessionPath)!;
        public string HangDumpPath => Path.Combine(RunDirectory, "RimWorldWin64-hang.dmp");

        public string AddSession(string runId, string json)
        {
            var sessionDirectory = Path.Combine(SavedDataPath, "DevGateway", "Sessions", runId);
            Directory.CreateDirectory(sessionDirectory);
            var sessionPath = Path.Combine(sessionDirectory, "session.json");
            File.WriteAllText(sessionPath, json, new UTF8Encoding(false));
            return sessionPath;
        }

        public static Fixture Create(
            bool deleteCurrent = false,
            bool tokenlessTombstone = false,
            string? manifestJson = null)
        {
            var root = Path.Combine(Path.GetTempPath(), "GatewaySmokeCredentialCleanupTests", Guid.NewGuid().ToString("N"));
            var runDirectory = Path.Combine(root, "run");
            var savedDataPath = Path.Combine(runDirectory, "SavedData");
            var sessionDirectory = Path.Combine(savedDataPath, "DevGateway", "Sessions", RunId);
            Directory.CreateDirectory(sessionDirectory);
            var fixture = new Fixture(
                root,
                Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1"),
                runDirectory,
                savedDataPath);
            var json = manifestJson ?? (tokenlessTombstone
                ? TokenlessManifestJson(ProcessId, RunId, "stopped")
                : ManifestJson(ProcessId, RunId, Token, "active"));
            File.WriteAllText(fixture.SessionPath, json, new UTF8Encoding(false));
            Directory.CreateDirectory(Path.GetDirectoryName(fixture.CurrentPath)!);
            File.WriteAllText(fixture.CurrentPath, json, new UTF8Encoding(false));
            if (deleteCurrent)
            {
                File.Delete(fixture.CurrentPath);
            }

            return fixture;
        }

        public InvocationResult Invoke(bool processHasExited, bool includeExpectedIdentity)
        {
            var invocationPath = Path.Combine(root, $"invoke-{Guid.NewGuid():N}.ps1");
            var functionNames = new[]
            {
                "Assert-SafeGatewayArtifactPath",
                "Test-FileContainsBearerToken",
                "Assert-NoRetainedBearerToken",
                "Read-GatewayCredentialManifest",
                "ConvertTo-GatewayCredentialFreeManifestJson",
                "Set-GatewayCredentialFreeSessionManifestAtomically",
                "Protect-GatewayCredentialArtifacts"
            };
            var identityArguments = includeExpectedIdentity
                ? $" -ExpectedRunId {PowerShellLiteral(RunId)} -BearerToken {PowerShellLiteral(Token)}"
                : string.Empty;
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "Set-StrictMode -Version Latest\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                $"$functionNames = @({string.Join(",", functionNames.Select(PowerShellLiteral))})\n" +
                "foreach ($functionName in $functionNames) {\n" +
                "  $functionAst = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $functionName }, $true)\n" +
                "  if ($null -eq $functionAst) { throw \"Function was not found: $functionName\" }\n" +
                "  Invoke-Expression $functionAst.Extent.Text\n" +
                "}\n" +
                "try {\n" +
                $"$result = Protect-GatewayCredentialArtifacts -RunDirectory {PowerShellLiteral(RunDirectory)} -SavedDataPath {PowerShellLiteral(SavedDataPath)} -ExpectedProcessId {ProcessId}{identityArguments} -ProcessHasExited:${processHasExited.ToString().ToLowerInvariant()}\n" +
                "$result | ConvertTo-Json -Compress\n" +
                "exit 0\n" +
                "}\ncatch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));
            return RunPowerShell(invocationPath);
        }

        public InvocationResult InvokeAbruptAtomicRedaction()
        {
            var invocationPath = Path.Combine(root, $"invoke-abrupt-{Guid.NewGuid():N}.ps1");
            var functionNames = new[]
            {
                "Assert-SafeGatewayArtifactPath",
                "Set-GatewayCredentialFreeSessionManifestAtomically"
            };
            var sanitizedJson = TokenlessManifestJson(ProcessId, RunId, "host-sanitized");
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "Set-StrictMode -Version Latest\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                $"$functionNames = @({string.Join(",", functionNames.Select(PowerShellLiteral))})\n" +
                "foreach ($functionName in $functionNames) {\n" +
                "  $functionAst = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $functionName }, $true)\n" +
                "  if ($null -eq $functionAst) { throw \"Function was not found: $functionName\" }\n" +
                "  Invoke-Expression $functionAst.Extent.Text\n" +
                "}\n" +
                $"Set-GatewayCredentialFreeSessionManifestAtomically -Path {PowerShellLiteral(SessionPath)} -SanitizedJson {PowerShellLiteral(sanitizedJson)} -RunDirectory {PowerShellLiteral(RunDirectory)} -AfterReplace {{ [Environment]::Exit(73) }}\n" +
                "exit 0\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));
            return RunPowerShell(invocationPath);
        }

        public void Dispose() => Directory.Delete(root, recursive: true);
    }

    private static string ManifestJson(int processId, string runId, string token, string state) =>
        $"{{\"apiVersion\":\"v1\",\"runId\":\"{runId}\",\"state\":\"{state}\",\"token\":\"{token}\",\"processId\":{processId}}}";

    private static string TokenlessManifestJson(int processId, string runId, string state) =>
        $"{{\"apiVersion\":\"v1\",\"runId\":\"{runId}\",\"state\":\"{state}\",\"processId\":{processId}}}";

    private static string DuplicateTokenManifestJson(
        int processId,
        string runId,
        string firstToken,
        string secondToken,
        string state) =>
        $"{{\"apiVersion\":\"v1\",\"runId\":\"{runId}\",\"state\":\"{state}\"," +
        $"\"token\":\"{firstToken}\",\"token\":\"{secondToken}\",\"processId\":{processId}}}";

    private static string CaseVariantTokenManifestJson(
        int processId,
        string runId,
        string firstToken,
        string secondToken,
        string state) =>
        $"{{\"apiVersion\":\"v1\",\"runId\":\"{runId}\",\"state\":\"{state}\"," +
        $"\"token\":\"{firstToken}\",\"Token\":\"{secondToken}\",\"processId\":{processId}}}";

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
            throw new TimeoutException("Gateway smoke credential-cleanup probe timed out.");
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
