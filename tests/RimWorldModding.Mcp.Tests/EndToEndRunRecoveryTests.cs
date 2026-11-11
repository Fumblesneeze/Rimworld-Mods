using NUnit.Framework;
using System.Text.Json;

namespace RimWorldModding.Mcp.Tests;

public sealed class EndToEndRunRecoveryTests
{
    [Test]
    public void Real_nonzero_host_process_preserves_structured_exit_through_recovery()
    {
        string script = Path.Combine(Path.GetTempPath(), "e2e-nonzero-" + Guid.NewGuid().ToString("N") + ".ps1");
        var planned = NewCommand();
        var command = planned with { FileName = "pwsh", Arguments = new[] { "-NoProfile", "-NonInteractive", "-File", script, "-OwnedRunDirectory", planned.EvidenceRoot! } };
        File.WriteAllText(script, """
            param([string]$OwnedRunDirectory)
            $p = [Diagnostics.Process]::GetCurrentProcess()
            try { @{schema='RimWorldDevGateway/ProcessLease/v1';processId=$PID;processStartUtc=$p.StartTime.ToUniversalTime().ToString('O')} | ConvertTo-Json -Compress | Set-Content -LiteralPath (Join-Path $OwnedRunDirectory 'runner-process.json') -Encoding utf8 }
            finally { $p.Dispose() }
            [Console]::Out.WriteLine('scenario-out')
            [Console]::Error.WriteLine('scenario-err')
            exit 17
            """);
        try
        {
            var failure = Assert.CatchAsync(() => EndToEndRunRecovery.ExecuteAsync(command, CancellationToken.None));
            Assert.That(failure!.Message, Does.Contain("exit code 17"));
            using var receipt = JsonDocument.Parse(File.ReadAllText(Path.Combine(command.EvidenceRoot!, "fixture-recovery-result.json")));
            Assert.That(receipt.RootElement.GetProperty("primaryExitCode").GetInt32(), Is.EqualTo(17));
            Assert.That(receipt.RootElement.GetProperty("primaryOutcome").GetString(), Is.EqualTo("failed"));
            Assert.That(receipt.RootElement.GetProperty("status").GetString(), Is.EqualTo("completed"));
        }
        finally { DeleteOwnedRun(command); File.Delete(script); }
    }

    [TestCase(false, "missing")]
    [TestCase(false, "running")]
    [TestCase(false, "pid-reused")]
    [TestCase(false, "unknown")]
    [TestCase(true, "missing")]
    [TestCase(true, "running")]
    [TestCase(true, "pid-reused")]
    [TestCase(true, "unknown")]
    public void Absent_or_empty_catalog_requires_proven_runner_exit(bool emptyCatalog, string state)
    {
        var command = NewCommand();
        try
        {
            Assert.ThrowsAsync<InvalidOperationException>(() => EndToEndRunRecovery.ExecuteAsync(command, CancellationToken.None,
                (child, _) =>
                {
                    Assert.That(child.Kind, Is.EqualTo("e2e_run"));
                    if (state != "missing") WriteIdentity(Path.Combine(command.EvidenceRoot!, "runner-process.json"), 123);
                    if (emptyCatalog) File.WriteAllText(Path.Combine(command.EvidenceRoot!, "fixture-recovery.json"),
                        "{\"schema\":\"RimWorldModdingMcp/E2EFixtureRecovery/v1\",\"groups\":[]}");
                    return Task.FromResult(Success(child));
                }, (_, _) => state));
            using var receipt = JsonDocument.Parse(File.ReadAllText(Path.Combine(command.EvidenceRoot!, "fixture-recovery-result.json")));
            Assert.That(receipt.RootElement.GetProperty("status").GetString(), Is.EqualTo("incomplete"));
        }
        finally { DeleteOwnedRun(command); }
    }

    [TestCase("running")]
    [TestCase("missing")]
    public void Stage_publisher_must_stop_even_when_fixture_lease_is_not_yet_written(string state)
    {
        var command = NewCommand();
        try
        {
            Assert.ThrowsAsync<InvalidOperationException>(() => EndToEndRunRecovery.ExecuteAsync(command, CancellationToken.None,
                (child, _) =>
                {
                    Assert.That(child.Kind, Is.EqualTo("e2e_run"), "No cleanup may race an unresolved publisher.");
                    WriteRecoveryInputs(command.EvidenceRoot!, false);
                    File.Delete(Path.Combine(command.EvidenceRoot!, "group-001", "fixture.lease.json"));
                    string catalog = Path.Combine(command.EvidenceRoot!, "fixture-recovery.json");
                    var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(catalog))!;
                    json["groups"]![0]!["stageLaunchAttempted"] = true;
                    File.WriteAllText(catalog, json.ToJsonString());
                    if (state != "missing") WriteIdentity(Path.Combine(command.EvidenceRoot!, "group-001", "fixture-stage-process.json"), 789);
                    return Task.FromResult(Success(child));
                }, (pid, _) => pid == 789 ? state : "exited"));
        }
        finally { DeleteOwnedRun(command); }
    }

    [TestCase("traversal")]
    [TestCase("duplicate-field")]
    [TestCase("duplicate-path")]
    [TestCase("contradictory-launch")]
    [TestCase("cleanup-failure")]
    public void Unsafe_recovery_evidence_or_failed_cleanup_preserves_lease(string mode)
    {
        var command = NewCommand();
        int cleanupCalls = 0;
        try
        {
            var caught = Assert.ThrowsAsync<InvalidOperationException>(() => EndToEndRunRecovery.ExecuteAsync(command,
                CancellationToken.None, (child, _) =>
                {
                    if (child.Kind == "e2e_run")
                    {
                        WriteRecoveryInputs(command.EvidenceRoot!, false);
                        string path = Path.Combine(command.EvidenceRoot!, "fixture-recovery.json");
                        string text = File.ReadAllText(path);
                        if (mode == "traversal") text = text.Replace("group-001/fixture.lease.json", "../fixture.lease.json");
                        if (mode == "duplicate-field") text = text.Replace("\"launchAttempted\":false", "\"launchAttempted\":true,\"launchAttempted\":false");
                        if (mode == "duplicate-path")
                        {
                            var json = System.Text.Json.Nodes.JsonNode.Parse(text)!;
                            json["groups"]!.AsArray().Add(json["groups"]![0]!.DeepClone());
                            text = json.ToJsonString();
                        }
                        File.WriteAllText(path, text);
                        if (mode == "contradictory-launch")
                        {
                            Directory.CreateDirectory(Path.Combine(command.EvidenceRoot!, "smoke-001"));
                            WriteIdentity(Path.Combine(command.EvidenceRoot!, "smoke-001", "game-process.json"), 456);
                        }
                        return Task.FromResult(new AdapterOperationResult(child.Kind, 17, 0, "failed", "", child.EvidenceRoot));
                    }
                    cleanupCalls++;
                    return Task.FromResult(new AdapterOperationResult(child.Kind, 1, 0, "", "storage locked", child.EvidenceRoot));
                }, (_, _) => "exited"));
            Assert.That(caught!.Message, Does.Contain("Primary runner exit code: 17"));
            Assert.That(cleanupCalls, Is.EqualTo(mode == "cleanup-failure" ? 1 : 0));
            Assert.That(File.Exists(Path.Combine(command.EvidenceRoot!, "group-001", "fixture.lease.json")), Is.True);
        }
        finally { DeleteOwnedRun(command); }
    }

    [Test]
    public async Task Nonzero_runner_exit_remains_explicit_in_recovery_receipt()
    {
        var command = NewCommand();
        try
        {
            var result = await EndToEndRunRecovery.ExecuteAsync(command, CancellationToken.None,
                (child, _) =>
                {
                    WriteIdentity(Path.Combine(command.EvidenceRoot!, "runner-process.json"), 123);
                    return Task.FromResult(new AdapterOperationResult(child.Kind, 17, 0, "failed scenario", "details", child.EvidenceRoot));
                }, (_, _) => "exited");
            Assert.That(result.ExitCode, Is.EqualTo(17));
            using var receipt = JsonDocument.Parse(File.ReadAllText(Path.Combine(command.EvidenceRoot!, "fixture-recovery-result.json")));
            Assert.That(receipt.RootElement.GetProperty("primaryOutcome").GetString(), Is.EqualTo("failed"));
            Assert.That(receipt.RootElement.GetProperty("primaryExitCode").GetInt32(), Is.EqualTo(17));
        }
        finally { DeleteOwnedRun(command); }
    }

    [TestCase("runner", "running")]
    [TestCase("runner", "pid-reused")]
    [TestCase("runner", "unknown")]
    [TestCase("child", "running")]
    [TestCase("child", "pid-reused")]
    [TestCase("child", "unknown")]
    [TestCase("child", "missing")]
    [TestCase("runner", "missing")]
    public void Unproven_process_exit_retains_fixture_and_primary_failure(string target, string state)
    {
        var command = NewCommand();
        var primary = new OperationCanceledException("cancelled");
        int cleanupCalls = 0;
        try
        {
            var caught = Assert.ThrowsAsync<InvalidOperationException>(() => EndToEndRunRecovery.ExecuteAsync(command,
                CancellationToken.None, (child, _) =>
                {
                    if (child.Kind == "e2e_run")
                    {
                        WriteRecoveryInputs(command.EvidenceRoot!, true);
                        Directory.CreateDirectory(Path.Combine(command.EvidenceRoot!, "smoke-001"));
                        WriteIdentity(Path.Combine(command.EvidenceRoot!, "smoke-001", "game-process.json"), 456);
                        if (state == "missing") File.Delete(Path.Combine(command.EvidenceRoot!, target == "runner" ? "runner-process.json" : "smoke-001/game-process.json"));
                        throw primary;
                    }
                    cleanupCalls++;
                    return Task.FromResult(Success(child));
                }, (pid, _) => pid == (target == "runner" ? 123 : 456) ? state : "exited"));
            Assert.That(caught!.InnerException, Is.SameAs(primary));
            Assert.That(cleanupCalls, Is.Zero);
            Assert.That(File.Exists(Path.Combine(command.EvidenceRoot!, "group-001", "fixture.lease.json")), Is.True);
        }
        finally { DeleteOwnedRun(command); }
    }

    [TestCase("success")]
    [TestCase("failure")]
    [TestCase("cancellation")]
    public async Task Stopped_game_child_allows_exact_fixture_recovery_and_preserves_primary_outcome(string outcome)
    {
        var command = NewCommand();
        string lease = Path.Combine(command.EvidenceRoot!, "group-001", "fixture.lease.json");
        Exception? primary = outcome == "success" ? null : outcome == "failure" ?
            new InvalidOperationException("scenario failed") : new OperationCanceledException("caller cancelled");
        try
        {
            async Task<AdapterOperationResult> Invoke() => await EndToEndRunRecovery.ExecuteAsync(command,
                CancellationToken.None, (child, _) =>
                {
                    if (child.Kind == "e2e_run")
                    {
                        WriteRecoveryInputs(command.EvidenceRoot!, true);
                        Directory.CreateDirectory(Path.Combine(command.EvidenceRoot!, "smoke-001"));
                        WriteIdentity(Path.Combine(command.EvidenceRoot!, "smoke-001", "game-process.json"), 456);
                        if (primary is not null) throw primary;
                    }
                    else File.Delete(lease);
                    return Task.FromResult(Success(child));
                }, (_, _) => "exited");
            if (primary is null) Assert.That((await Invoke()).ExitCode, Is.Zero);
            else Assert.That(Assert.CatchAsync(() => Invoke()), Is.SameAs(primary));
            Assert.That(File.Exists(lease), Is.False);
            using var receipt = JsonDocument.Parse(File.ReadAllText(Path.Combine(command.EvidenceRoot!, "fixture-recovery-result.json")));
            Assert.That(receipt.RootElement.GetProperty("status").GetString(), Is.EqualTo("completed"));
        }
        finally { DeleteOwnedRun(command); }
    }

    [Test]
    public void Cancellation_before_game_invocation_recovers_registered_fixture_with_an_independent_budget()
    {
        var command = NewCommand();
        using var cancellation = new CancellationTokenSource();
        var primary = new OperationCanceledException("caller cancelled", cancellation.Token);
        string lease = Path.Combine(command.EvidenceRoot!, "group-001", "fixture.lease.json");
        try
        {
            var caught = Assert.ThrowsAsync<OperationCanceledException>(() => EndToEndRunRecovery.ExecuteAsync(
                command, cancellation.Token, (child, token) =>
                {
                    if (child.Kind == "e2e_run")
                    {
                        WriteRecoveryInputs(command.EvidenceRoot!, launchAttempted: false);
                        cancellation.Cancel();
                        throw primary;
                    }
                    Assert.That(token.IsCancellationRequested, Is.False);
                    Assert.That(child.FileName, Is.EqualTo("dotnet"));
                    Assert.That(child.Arguments, Does.Contain("fixture-clean"));
                    Assert.That(child.Arguments, Does.Contain(lease));
                    Assert.That(child.Arguments, Does.Not.Contain("run"));
                    File.Delete(lease); // External host CLI boundary: successful fixture-clean clears its journal.
                    return Task.FromResult(Success(child));
                }, (_, _) => "exited"));

            Assert.That(caught, Is.SameAs(primary));
            Assert.That(File.Exists(lease), Is.False, "Cancellation must recover a registered unlaunched fixture.");
            Assert.That(File.Exists(Path.Combine(command.EvidenceRoot!, "fixture-recovery-result.json")), Is.True);
        }
        finally { DeleteOwnedRun(command); }
    }

    private static AdapterCommand NewCommand() => RepositoryOperationPlanner.EndToEnd(TestRepository.FindRoot(),
        null, "gateway.example", "English", 300, false);

    private static AdapterOperationResult Success(AdapterCommand command) => new(command.Kind, 0, 0, "done", "", command.EvidenceRoot);

    private static void WriteRecoveryInputs(string root, bool launchAttempted)
    {
        Directory.CreateDirectory(Path.Combine(root, "group-001"));
        File.WriteAllText(Path.Combine(root, "group-001", "fixture.lease.json"), "fixture journal placeholder");
        WriteIdentity(Path.Combine(root, "runner-process.json"), 123);
        File.WriteAllText(Path.Combine(root, "fixture-recovery.json"), JsonSerializer.Serialize(new
        {
            schema = "RimWorldModdingMcp/E2EFixtureRecovery/v1",
            groups = new[] { new { fixtureLeaseFile = "group-001/fixture.lease.json",
                processLeaseFile = "smoke-001/game-process.json", launchAttempted,
                stageProcessLeaseFile = "group-001/fixture-stage-process.json", stageLaunchAttempted = false } }
        }));
    }

    private static void WriteIdentity(string path, int processId, DateTimeOffset? start = null) =>
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            schema = "RimWorldDevGateway/ProcessLease/v1", processId,
            processStartUtc = (start ?? DateTimeOffset.UtcNow).ToString("O")
        }));

    private static void DeleteOwnedRun(AdapterCommand command)
    {
        if (Directory.Exists(command.EvidenceRoot)) Directory.Delete(command.EvidenceRoot!, true);
    }

    [Test]
    public async Task Typed_boundary_persists_ownership_before_invoking_runner()
    {
        var command = RepositoryOperationPlanner.EndToEnd(TestRepository.FindRoot(), null,
            "gateway.example", "English", 300, false);
        try
        {
            var result = await EndToEndRunRecovery.ExecuteAsync(command, CancellationToken.None, (child, _) =>
            {
                string marker = Path.Combine(child.EvidenceRoot!, "typed-run.json");
                Assert.That(File.Exists(marker), Is.True, "Ownership must be durable before the runner starts.");
                using var json = JsonDocument.Parse(File.ReadAllText(marker));
                Assert.That(json.RootElement.GetProperty("schema").GetString(),
                    Is.EqualTo("RimWorldModdingMcp/OwnedE2ERun/v1"));
                Assert.That(json.RootElement.GetProperty("runDirectory").GetString(), Is.EqualTo(child.EvidenceRoot));
                WriteIdentity(Path.Combine(child.EvidenceRoot!, "runner-process.json"), 123);
                return Task.FromResult(new AdapterOperationResult(child.Kind, 0, 0, "done", "", child.EvidenceRoot));
            }, (_, _) => "exited");
            Assert.That(result.StandardOutput, Is.EqualTo("done"));
            using var receipt = JsonDocument.Parse(File.ReadAllText(Path.Combine(command.EvidenceRoot!, "fixture-recovery-result.json")));
            Assert.That(receipt.RootElement.GetProperty("status").GetString(), Is.EqualTo("completed"));
            Assert.That(receipt.RootElement.GetProperty("groups").GetArrayLength(), Is.Zero,
                "A no-catalog run must not discover or clean any fixtures.");
        }
        finally
        {
            if (Directory.Exists(command.EvidenceRoot)) Directory.Delete(command.EvidenceRoot!, true);
        }
    }

    [Test]
    public void Real_runs_reserve_distinct_exact_artifact_paths_without_mutating_dry_run()
    {
        string root = TestRepository.FindRoot();
        var first = RepositoryOperationPlanner.EndToEnd(root, null, "gateway.example", "English", 300, false);
        var second = RepositoryOperationPlanner.EndToEnd(root, null, "gateway.example", "English", 300, false);
        var dry = RepositoryOperationPlanner.EndToEnd(root, null, "gateway.example", "English", 300, true);
        string grouped = Path.Combine(root, "artifacts", "EndToEndRuns", "Grouped");

        Assert.Multiple(() =>
        {
            Assert.That(first.EvidenceRoot, Is.Not.EqualTo(second.EvidenceRoot));
            Assert.That(Path.GetDirectoryName(first.EvidenceRoot), Is.EqualTo(grouped));
            Assert.That(first.Arguments, Does.Contain("-OwnedRunDirectory"));
            Assert.That(first.Arguments, Does.Contain(first.EvidenceRoot));
            Assert.That(Directory.Exists(first.EvidenceRoot), Is.False);
            Assert.That(dry.Arguments, Does.Not.Contain("-OwnedRunDirectory"));
            Assert.That(dry.EvidenceRoot, Is.EqualTo(grouped));
        });
    }
}
