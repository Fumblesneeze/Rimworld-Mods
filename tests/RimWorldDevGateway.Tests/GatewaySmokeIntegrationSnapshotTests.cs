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
public sealed class GatewaySmokeIntegrationSnapshotTests
{
    private const string Owner = "fumblesneeze.rimworlddevgateway";
    private const string MainMenuA =
        "RimWorldDevGateway.InGame.IntegrationTests.FinalizedDefIntegrationTests.CoreSteelDefExistsInFinalizedDatabase";
    private const string MainMenuB =
        "RimWorldDevGateway.InGame.IntegrationTests.FinalizedDefIntegrationTests.GatewayProbeContainsItsFinalXmlPatch";
    private const string MapA =
        "RimWorldDevGateway.InGame.IntegrationTests.PlayableMapIntegrationTests.A_DeliberateFailureProbeIsStartupControlled";
    private const string MapB =
        "RimWorldDevGateway.InGame.IntegrationTests.PlayableMapIntegrationTests.B_LaterTestStillRunsAfterTheFailureProbe";
    private const string ExtraTest = "Extra.Owner.Tests.Passes";
    private const string FocusOwner = "fumblesneeze.immersivechefs";
    private const string FocusTest = "ImmersiveChefs.Texture.Tests.DmtrOwnership";
    private const string MainMenuCompletedLifecycle =
        "{\"RunAt\":\"MainMenuLoaded\",\"State\":\"completed\",\"StartedUtc\":\"2026-08-01T00:00:00.0000000Z\",\"CompletedUtc\":\"2026-08-01T00:00:02.0000000Z\",\"ExecutedCount\":2,\"PassedCount\":2,\"FailedCount\":0}";
    private const string MainMenuFailedLifecycle =
        "{\"RunAt\":\"MainMenuLoaded\",\"State\":\"failed\",\"StartedUtc\":\"2026-08-01T00:00:00.0000000Z\",\"CompletedUtc\":\"2026-08-01T00:00:02.0000000Z\",\"ExecutedCount\":2,\"PassedCount\":1,\"FailedCount\":1}";
    private const string MainMenuPendingLifecycle =
        "{\"RunAt\":\"MainMenuLoaded\",\"State\":\"pending\",\"StartedUtc\":null,\"CompletedUtc\":null,\"ExecutedCount\":0,\"PassedCount\":0,\"FailedCount\":0}";
    private const string PlayableMapCompletedLifecycle =
        "{\"RunAt\":\"PlayableMapLoaded\",\"State\":\"completed\",\"StartedUtc\":\"2026-08-01T00:00:02.0000000Z\",\"CompletedUtc\":\"2026-08-01T00:00:05.0000000Z\",\"ExecutedCount\":3,\"PassedCount\":3,\"FailedCount\":0}";
    private const string PlayableMapFailedExtraLifecycle =
        "{\"RunAt\":\"PlayableMapLoaded\",\"State\":\"failed\",\"StartedUtc\":\"2026-08-01T00:00:02.0000000Z\",\"CompletedUtc\":\"2026-08-01T00:00:05.0000000Z\",\"ExecutedCount\":3,\"PassedCount\":2,\"FailedCount\":1}";
    private const string PlayableMapIncompleteLifecycle =
        "{\"RunAt\":\"PlayableMapLoaded\",\"State\":\"completed\",\"StartedUtc\":\"2026-08-01T00:00:02.0000000Z\",\"CompletedUtc\":\"2026-08-01T00:00:05.0000000Z\",\"ExecutedCount\":2,\"PassedCount\":2,\"FailedCount\":0}";
    private const string PlayableMapPendingLifecycle =
        "{\"RunAt\":\"PlayableMapLoaded\",\"State\":\"pending\",\"StartedUtc\":null,\"CompletedUtc\":null,\"ExecutedCount\":0,\"PassedCount\":0,\"FailedCount\":0}";
    private const string PlayableMapMalformedPendingLifecycle =
        "{\"RunAt\":\"PlayableMapLoaded\",\"State\":\"pending\",\"StartedUtc\":\"2026-08-01T00:00:02.0000000Z\",\"CompletedUtc\":null,\"ExecutedCount\":2,\"PassedCount\":2,\"FailedCount\":0}";

    [Test]
    public void Normal_smoke_globally_rejects_non_target_failures_top_level_failures_and_omissions()
    {
        var accepted = InvokeAssertion(ValidNormalSnapshot, failureProbe: false);
        var nonTargetFailure = InvokeAssertion(
            ValidNormalSnapshot
                .Replace(MainMenuCompletedLifecycle, MainMenuFailedLifecycle)
                .Replace(Result(MainMenuA, "MainMenuLoaded", "passed", "00:00:01"),
                    Result(MainMenuA, "MainMenuLoaded", "failed", "00:00:01")),
            failureProbe: false);
        var reportedFailure = InvokeAssertion(
            ValidNormalSnapshot.Replace(
                "\"Failures\":[]",
                "\"Failures\":[{\"Code\":\"assembly_discovery_failed\",\"TestName\":null}]"),
            failureProbe: false);
        var omittedResult = InvokeAssertion(
            ValidNormalSnapshot.Replace("\"OmittedResultCount\":0", "\"OmittedResultCount\":1"),
            failureProbe: false);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.ExitCode, Is.Zero, accepted.StandardError);
            Assert.That(nonTargetFailure.ExitCode, Is.EqualTo(1));
            Assert.That(nonTargetFailure.StandardError, Does.Contain(MainMenuA));
            Assert.That(reportedFailure.ExitCode, Is.EqualTo(1));
            Assert.That(reportedFailure.StandardError, Does.Contain("Failures"));
            Assert.That(omittedResult.ExitCode, Is.EqualTo(1));
            Assert.That(omittedResult.StandardError, Does.Contain("omitted"));
        });
    }

    [Test]
    public void Gateway_fixture_contract_requires_four_exact_descriptors_while_allowing_extra_owner_tests()
    {
        var accepted = InvokeAssertion(ValidNormalSnapshot, failureProbe: false);
        var renamedDescriptor = InvokeAssertion(
            ValidNormalSnapshot.Replace(
                Descriptor(MapB, "PlayableMapLoaded"),
                Descriptor(MapB + "Renamed", "PlayableMapLoaded")),
            failureProbe: false);
        var wrongOwner = InvokeAssertion(
            ValidNormalSnapshot.Replace(
                Descriptor(MainMenuA, "MainMenuLoaded"),
                Descriptor(MainMenuA, "MainMenuLoaded", "other.owner")),
            failureProbe: false);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.ExitCode, Is.Zero, accepted.StandardError);
            Assert.That(renamedDescriptor.ExitCode, Is.EqualTo(1));
            Assert.That(renamedDescriptor.StandardError, Does.Contain("four exact Gateway fixture descriptors"));
            Assert.That(wrongOwner.ExitCode, Is.EqualTo(1));
        });
    }

    [Test]
    public void Explicit_product_expectation_requires_one_exact_descriptor_and_passed_result()
    {
        var accepted = InvokeAssertion(
            ValidNormalSnapshot,
            failureProbe: false,
            expectedOwner: "extra.owner",
            expectedRunAt: "PlayableMapLoaded",
            expectedTestName: ExtraTest);
        var missing = InvokeAssertion(
            ValidNormalSnapshot,
            failureProbe: false,
            expectedOwner: "extra.owner",
            expectedRunAt: "PlayableMapLoaded",
            expectedTestName: ExtraTest + "Missing");
        var wrongOwner = InvokeAssertion(
            ValidNormalSnapshot,
            failureProbe: false,
            expectedOwner: "wrong.owner",
            expectedRunAt: "PlayableMapLoaded",
            expectedTestName: ExtraTest);
        var wrongLifecycle = InvokeAssertion(
            ValidNormalSnapshot,
            failureProbe: false,
            expectedOwner: "extra.owner",
            expectedRunAt: "MainMenuLoaded",
            expectedTestName: ExtraTest);
        var failed = InvokeAssertion(
            ValidNormalSnapshot
                .Replace(PlayableMapCompletedLifecycle, PlayableMapFailedExtraLifecycle)
                .Replace(
                    Result(ExtraTest, "PlayableMapLoaded", "passed", "00:00:05", "extra.owner", "Extra"),
                    Result(ExtraTest, "PlayableMapLoaded", "failed", "00:00:05", "extra.owner", "Extra")),
            failureProbe: true,
            expectedOwner: "extra.owner",
            expectedRunAt: "PlayableMapLoaded",
            expectedTestName: ExtraTest);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.ExitCode, Is.Zero, accepted.StandardError);
            Assert.That(missing.ExitCode, Is.EqualTo(1));
            Assert.That(wrongOwner.ExitCode, Is.EqualTo(1));
            Assert.That(wrongLifecycle.ExitCode, Is.EqualTo(1));
            Assert.That(failed.ExitCode, Is.EqualTo(1));
            Assert.That(failed.StandardError, Does.Contain("expected integration test").IgnoreCase);
        });
    }

    [Test]
    public void Focused_product_snapshot_does_not_require_the_unstaged_gateway_fixture_suite()
    {
        var accepted = InvokeAssertion(
            ValidFocusedProductSnapshot,
            failureProbe: false,
            targetRunAt: "MainMenuLoaded",
            expectedOwner: FocusOwner,
            expectedRunAt: "MainMenuLoaded",
            expectedTestName: FocusTest,
            minimumDiscoveredTestCount: 1);

        Assert.That(accepted.ExitCode, Is.Zero, accepted.StandardError);
    }

    [Test]
    public void Every_discovered_descriptor_in_a_terminal_lifecycle_requires_one_exact_result()
    {
        var missingOtherOwnerResult = InvokeAssertion(
            ValidNormalSnapshot
                .Replace(PlayableMapCompletedLifecycle, PlayableMapIncompleteLifecycle)
                .Replace("," + Result(
                    ExtraTest,
                    "PlayableMapLoaded",
                    "passed",
                    "00:00:05",
                    "extra.owner",
                    "Extra"), string.Empty),
            failureProbe: false);

        Assert.Multiple(() =>
        {
            Assert.That(missingOtherOwnerResult.ExitCode, Is.EqualTo(1));
            Assert.That(missingOtherOwnerResult.StandardError, Does.Contain(ExtraTest));
        });
    }

    [Test]
    public void Lifecycle_inventory_is_exact_and_pending_points_are_unstarted_and_empty()
    {
        var missingMapLifecycle = InvokeAssertion(
            ValidMainMenuSnapshot.Replace("," + PlayableMapPendingLifecycle, string.Empty),
            failureProbe: false,
            targetRunAt: "MainMenuLoaded");
        var malformedPendingMap = InvokeAssertion(
            ValidMainMenuSnapshot.Replace(
                PlayableMapPendingLifecycle,
                PlayableMapMalformedPendingLifecycle),
            failureProbe: false,
            targetRunAt: "MainMenuLoaded");
        var unknownRunAt = InvokeAssertion(
            ValidNormalSnapshot
                .Replace(PlayableMapCompletedLifecycle, PlayableMapIncompleteLifecycle)
                .Replace(
                    Descriptor(ExtraTest, "PlayableMapLoaded", "extra.owner"),
                    Descriptor(ExtraTest, "FuturePoint", "extra.owner"))
                .Replace(
                    Result(ExtraTest, "PlayableMapLoaded", "passed", "00:00:05", "extra.owner", "Extra"),
                    Result(ExtraTest, "FuturePoint", "passed", "00:00:05", "extra.owner", "Extra")),
            failureProbe: false);

        Assert.Multiple(() =>
        {
            Assert.That(missingMapLifecycle.ExitCode, Is.EqualTo(1));
            Assert.That(missingMapLifecycle.StandardError, Does.Contain("PlayableMapLoaded"));
            Assert.That(malformedPendingMap.ExitCode, Is.EqualTo(1));
            Assert.That(malformedPendingMap.StandardError, Does.Contain("pending").IgnoreCase);
            Assert.That(unknownRunAt.ExitCode, Is.EqualTo(1));
            Assert.That(unknownRunAt.StandardError, Does.Contain("FuturePoint"));
        });
    }

    [Test]
    public void Deliberate_probe_requires_exact_map_A_failure_and_exact_map_B_success_with_every_other_result_passing()
    {
        var accepted = InvokeAssertion(ValidFailureProbeSnapshot, failureProbe: true);
        var wrongFailure = InvokeAssertion(
            ValidFailureProbeSnapshot.Replace(
                Result(MapA, "PlayableMapLoaded", "failed", "00:00:03"),
                Result(MapA + "Unexpected", "PlayableMapLoaded", "failed", "00:00:03")),
            failureProbe: true);
        var missingMapB = InvokeAssertion(
            ValidFailureProbeSnapshot.Replace("," + Result(MapB, "PlayableMapLoaded", "passed", "00:00:04"), string.Empty),
            failureProbe: true);
        var nonTargetFailure = InvokeAssertion(
            ValidFailureProbeSnapshot
                .Replace(MainMenuCompletedLifecycle, MainMenuFailedLifecycle)
                .Replace(Result(MainMenuB, "MainMenuLoaded", "passed", "00:00:02"),
                    Result(MainMenuB, "MainMenuLoaded", "failed", "00:00:02")),
            failureProbe: true);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.ExitCode, Is.Zero, accepted.StandardError);
            Assert.That(wrongFailure.ExitCode, Is.EqualTo(1));
            Assert.That(missingMapB.ExitCode, Is.EqualTo(1));
            Assert.That(missingMapB.StandardError, Does.Contain(MapB));
            Assert.That(nonTargetFailure.ExitCode, Is.EqualTo(1));
            Assert.That(nonTargetFailure.StandardError, Does.Contain(MainMenuB));
        });
    }

    [Test]
    public void Quicktest_map_lifecycle_accepts_a_skipped_main_menu_but_rejects_stray_main_menu_results()
    {
        var accepted = InvokeAssertion(ValidQuicktestFailureProbeSnapshot, failureProbe: true);
        var normalAccepted = InvokeAssertion(ValidQuicktestNormalSnapshot, failureProbe: false);
        var strayMainMenuResult = InvokeAssertion(
            ValidQuicktestFailureProbeSnapshot.Replace(
                "\"Results\":[",
                "\"Results\":[" + Result(MainMenuA, "MainMenuLoaded", "passed", "00:00:01") + ","),
            failureProbe: true);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.ExitCode, Is.Zero, accepted.StandardError);
            Assert.That(normalAccepted.ExitCode, Is.Zero, normalAccepted.StandardError);
            Assert.That(strayMainMenuResult.ExitCode, Is.EqualTo(1));
            Assert.That(strayMainMenuResult.StandardError, Does.Contain("Pending integration-test lifecycle MainMenuLoaded"));
        });
    }

    [Test]
    public void Persisted_terminal_snapshot_is_parsed_validated_and_compared_by_identity_and_timestamps()
    {
        var accepted = InvokePersistedAssertion(ValidNormalSnapshot, ValidNormalSnapshot);
        var changedTimestamp = InvokePersistedAssertion(
            ValidNormalSnapshot,
            ValidNormalSnapshot.Replace("2026-08-01T00:00:04.0000000Z", "2026-08-01T00:00:09.0000000Z"));
        var malformed = InvokePersistedAssertion(ValidNormalSnapshot, "{not-json");

        Assert.Multiple(() =>
        {
            Assert.That(accepted.ExitCode, Is.Zero, accepted.StandardError);
            Assert.That(changedTimestamp.ExitCode, Is.EqualTo(1));
            Assert.That(changedTimestamp.StandardError, Does.Contain("terminal snapshots differ"));
            Assert.That(malformed.ExitCode, Is.EqualTo(1));
            Assert.That(malformed.StandardError, Does.Contain("valid JSON"));
        });
    }

    private static InvocationResult InvokeAssertion(
        string snapshotJson,
        bool failureProbe,
        string targetRunAt = "PlayableMapLoaded",
        string? expectedOwner = null,
        string? expectedRunAt = null,
        string? expectedTestName = null,
        int minimumDiscoveredTestCount = 4)
    {
        var expectedArgument = expectedOwner is null
            ? string.Empty
            : " -ExpectedTests @([pscustomobject]@{ OwningPackageId=" + PowerShellLiteral(expectedOwner) +
              "; RunAt=" + PowerShellLiteral(expectedRunAt!) +
              "; TestName=" + PowerShellLiteral(expectedTestName!) + " })";
        return
        InvokePowerShell(
            snapshotJson,
            null,
            "$snapshot = Get-Content -LiteralPath $snapshotPath -Raw | ConvertFrom-Json" + Environment.NewLine +
            $"Assert-IntegrationTestTerminalSnapshot -Snapshot $snapshot -TargetRunAt {PowerShellLiteral(targetRunAt)} " +
            $"-FailureProbe:${failureProbe.ToString().ToLowerInvariant()} -MinimumDiscoveredTestCount {minimumDiscoveredTestCount}" +
            expectedArgument + " | Out-Null");
    }

    private static InvocationResult InvokePersistedAssertion(string endpointJson, string persistedJson) =>
        InvokePowerShell(
            endpointJson,
            persistedJson,
            "$endpoint = Get-Content -LiteralPath $snapshotPath -Raw | ConvertFrom-Json" + Environment.NewLine +
            "$persisted = Read-IntegrationTestSnapshotFile -Path $persistedPath" + Environment.NewLine +
            "Assert-IntegrationTestTerminalSnapshot -Snapshot $endpoint -TargetRunAt 'PlayableMapLoaded' -MinimumDiscoveredTestCount 4 | Out-Null" + Environment.NewLine +
            "Assert-IntegrationTestTerminalSnapshot -Snapshot $persisted -TargetRunAt 'PlayableMapLoaded' -MinimumDiscoveredTestCount 4 | Out-Null" + Environment.NewLine +
            "Assert-MatchingIntegrationTestSnapshots -Expected $endpoint -Actual $persisted");

    private static InvocationResult InvokePowerShell(
        string snapshotJson,
        string? persistedJson,
        string operation)
    {
        var repositoryRoot = FindSourceRepositoryRoot();
        var smokePath = Path.Combine(repositoryRoot, "scripts", "Invoke-GatewaySmoke.ps1");
        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "GatewaySmokeIntegrationSnapshotTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var snapshotPath = Path.Combine(temporaryRoot, "snapshot.json");
            var persistedPath = Path.Combine(temporaryRoot, "persisted.json");
            var invocationPath = Path.Combine(temporaryRoot, "invoke.ps1");
            File.WriteAllText(snapshotPath, snapshotJson, new UTF8Encoding(false));
            if (persistedJson is not null)
            {
                File.WriteAllText(persistedPath, persistedJson, new UTF8Encoding(false));
            }

            var functionNames = new[]
            {
                "Test-GatewayPackageId",
                "Get-IntegrationTestTerminalFingerprint",
                "Read-IntegrationTestSnapshotFile",
                "Assert-MatchingIntegrationTestSnapshots",
                "Assert-IntegrationTestTerminalSnapshot"
            };
            var invocation =
                "$ErrorActionPreference = 'Stop'" + Environment.NewLine +
                "Set-StrictMode -Version Latest" + Environment.NewLine +
                "$tokens = $null" + Environment.NewLine +
                "$parseErrors = $null" + Environment.NewLine +
                $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)" + Environment.NewLine +
                $"$functionNames = @({string.Join(",", functionNames.Select(PowerShellLiteral))})" + Environment.NewLine +
                "foreach ($functionName in $functionNames) {" + Environment.NewLine +
                "  $functionAst = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $functionName }, $true)" + Environment.NewLine +
                "  if ($null -eq $functionAst) { [Console]::Error.WriteLine(\"Function was not found: $functionName\"); exit 91 }" + Environment.NewLine +
                "  Invoke-Expression $functionAst.Extent.Text" + Environment.NewLine +
                "}" + Environment.NewLine +
                $"$snapshotPath = {PowerShellLiteral(snapshotPath)}" + Environment.NewLine +
                $"$persistedPath = {PowerShellLiteral(persistedPath)}" + Environment.NewLine +
                "try {" + Environment.NewLine +
                operation + Environment.NewLine +
                "  exit 0" + Environment.NewLine +
                "}" + Environment.NewLine +
                "catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }" + Environment.NewLine;
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));

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
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000))
            {
                process.Kill();
                throw new TimeoutException("Gateway smoke assertion did not finish within 30 seconds.");
            }

            Task.WaitAll(standardOutput, standardError);
            return new InvocationResult(process.ExitCode, standardOutput.Result, standardError.Result);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static string Descriptor(string testName, string runAt, string owner = Owner)
    {
        var assemblyIdentity = owner == Owner ? "GatewayFixture" : "Extra";
        return $"{{\"OwningPackageId\":\"{owner}\",\"AssemblyIdentity\":\"{assemblyIdentity}\",\"TestName\":\"{testName}\",\"RunAt\":\"{runAt}\"}}";
    }

    private static string Result(
        string testName,
        string runAt,
        string state,
        string time,
        string owner = Owner,
        string assemblyIdentity = "GatewayFixture") =>
        $"{{\"OwningPackageId\":\"{owner}\",\"AssemblyIdentity\":\"{assemblyIdentity}\",\"TestName\":\"{testName}\",\"RunAt\":\"{runAt}\",\"State\":\"{state}\",\"StartedUtc\":\"2026-08-01T{time}.0000000Z\",\"CompletedUtc\":\"2026-08-01T{time}.0000000Z\",\"DurationMilliseconds\":1,\"ExceptionType\":null,\"Message\":null,\"StackTrace\":null}}";

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

    private static readonly string Descriptors = string.Join(",", new[]
    {
        Descriptor(MainMenuA, "MainMenuLoaded"),
        Descriptor(MainMenuB, "MainMenuLoaded"),
        Descriptor(MapA, "PlayableMapLoaded"),
        Descriptor(MapB, "PlayableMapLoaded"),
        Descriptor(ExtraTest, "PlayableMapLoaded", "extra.owner")
    });

    private static readonly string CommonPrefix =
        "{\"Enabled\":true,\"DiscoveryState\":\"completed\",\"DiscoveredAssemblyCount\":2," +
        "\"DiscoveredAssemblies\":[" +
        "{\"OwningPackageId\":\"fumblesneeze.rimworlddevgateway\",\"SourceIdentity\":\"gateway-fixture-source\",\"AssemblyIdentity\":\"GatewayFixture\"}," +
        "{\"OwningPackageId\":\"extra.owner\",\"SourceIdentity\":\"extra-fixture-source\",\"AssemblyIdentity\":\"Extra\"}]," +
        "\"DiscoveredTestCount\":5," +
        "\"DiscoveredTests\":[" + Descriptors + "],\"Failures\":[]," +
        "\"OmittedFailureCount\":0,\"OmittedResultCount\":0," +
        "\"LifecyclePoints\":[" + MainMenuCompletedLifecycle + ",";

    private static readonly string ValidNormalSnapshot = CommonPrefix +
        PlayableMapCompletedLifecycle + "]," +
        "\"Results\":[" + string.Join(",", new[]
        {
            Result(MainMenuA, "MainMenuLoaded", "passed", "00:00:01"),
            Result(MainMenuB, "MainMenuLoaded", "passed", "00:00:02"),
            Result(MapA, "PlayableMapLoaded", "passed", "00:00:03"),
            Result(MapB, "PlayableMapLoaded", "passed", "00:00:04"),
            Result(ExtraTest, "PlayableMapLoaded", "passed", "00:00:05", "extra.owner", "Extra")
        }) + "]}";

    private static readonly string ValidFailureProbeSnapshot = CommonPrefix +
        "{\"RunAt\":\"PlayableMapLoaded\",\"State\":\"failed\",\"StartedUtc\":\"2026-08-01T00:00:02.0000000Z\",\"CompletedUtc\":\"2026-08-01T00:00:05.0000000Z\",\"ExecutedCount\":3,\"PassedCount\":2,\"FailedCount\":1}]," +
        "\"Results\":[" + string.Join(",", new[]
        {
            Result(MainMenuA, "MainMenuLoaded", "passed", "00:00:01"),
            Result(MainMenuB, "MainMenuLoaded", "passed", "00:00:02"),
            Result(MapA, "PlayableMapLoaded", "failed", "00:00:03"),
            Result(MapB, "PlayableMapLoaded", "passed", "00:00:04"),
            Result(ExtraTest, "PlayableMapLoaded", "passed", "00:00:05", "extra.owner", "Extra")
        }) + "]}";

    private static readonly string ValidQuicktestFailureProbeSnapshot = ValidFailureProbeSnapshot
        .Replace(MainMenuCompletedLifecycle, MainMenuPendingLifecycle)
        .Replace(Result(MainMenuA, "MainMenuLoaded", "passed", "00:00:01") + ",", string.Empty)
        .Replace(Result(MainMenuB, "MainMenuLoaded", "passed", "00:00:02") + ",", string.Empty);

    private static readonly string ValidQuicktestNormalSnapshot = ValidNormalSnapshot
        .Replace(MainMenuCompletedLifecycle, MainMenuPendingLifecycle)
        .Replace(Result(MainMenuA, "MainMenuLoaded", "passed", "00:00:01") + ",", string.Empty)
        .Replace(Result(MainMenuB, "MainMenuLoaded", "passed", "00:00:02") + ",", string.Empty);

    private static readonly string ValidMainMenuSnapshot = ValidNormalSnapshot
        .Replace(PlayableMapCompletedLifecycle, PlayableMapPendingLifecycle)
        .Replace("," + Result(MapA, "PlayableMapLoaded", "passed", "00:00:03"), string.Empty)
        .Replace("," + Result(MapB, "PlayableMapLoaded", "passed", "00:00:04"), string.Empty)
        .Replace("," + Result(ExtraTest, "PlayableMapLoaded", "passed", "00:00:05", "extra.owner", "Extra"), string.Empty);

    private static readonly string ValidFocusedProductSnapshot =
        "{\"Enabled\":true,\"DiscoveryState\":\"completed\",\"DiscoveredAssemblyCount\":1," +
        "\"DiscoveredAssemblies\":[" +
        "{\"OwningPackageId\":\"" + FocusOwner + "\",\"SourceIdentity\":\"focused-source\",\"AssemblyIdentity\":\"Extra\"}]," +
        "\"DiscoveredTestCount\":1," +
        "\"DiscoveredTests\":[" + Descriptor(FocusTest, "MainMenuLoaded", FocusOwner) + "]," +
        "\"Failures\":[],\"OmittedFailureCount\":0,\"OmittedResultCount\":0," +
        "\"LifecyclePoints\":[" +
        "{\"RunAt\":\"MainMenuLoaded\",\"State\":\"completed\",\"StartedUtc\":\"2026-08-01T00:00:00.0000000Z\",\"CompletedUtc\":\"2026-08-01T00:00:01.0000000Z\",\"ExecutedCount\":1,\"PassedCount\":1,\"FailedCount\":0}," +
        PlayableMapPendingLifecycle + "]," +
        "\"Results\":[" + Result(
            FocusTest,
            "MainMenuLoaded",
            "passed",
            "00:00:01",
            FocusOwner,
            "Extra") + "]}";
}
