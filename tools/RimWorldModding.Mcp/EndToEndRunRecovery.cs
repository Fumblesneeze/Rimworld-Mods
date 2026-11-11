using System.Text.Json;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace RimWorldModding.Mcp;

/// <summary>Owns fixture recovery for the typed E2E operation only.</summary>
internal static class EndToEndRunRecovery
{
    internal static async Task<AdapterOperationResult> ExecuteAsync(
        AdapterCommand command,
        CancellationToken cancellationToken,
        Func<AdapterCommand, CancellationToken, Task<AdapterOperationResult>>? execute = null,
        Func<int, DateTimeOffset, string>? inspectProcess = null)
    {
        execute ??= RepositoryOperations.ExecuteAsync;
        if (command.Arguments.Contains("-DryRun")) return await execute(command, cancellationToken);

        string root = RepositoryRoot.Resolve(command.WorkingDirectory);
        string runDirectory = Path.GetFullPath(command.EvidenceRoot ??
            throw new ArgumentException("E2E recovery requires an exact artifact directory."));
        string grouped = Path.Combine(root, "artifacts", "EndToEndRuns", "Grouped");
        int index = command.Arguments.ToList().IndexOf("-OwnedRunDirectory");
        if (command.Kind != "e2e_run" || index < 0 || index + 1 >= command.Arguments.Count ||
            command.Arguments.Count(value => value == "-OwnedRunDirectory") != 1 ||
            !string.Equals(command.Arguments[index + 1], runDirectory, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetDirectoryName(runDirectory), grouped, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("E2E runner and recovery must share one exact owned artifact directory.");

        foreach (string path in ReleaseCandidateBuilder.PathsToInspect(root, runDirectory))
        {
            if (File.Exists(path) || Directory.Exists(path))
                ReleaseCandidateBuilder.RejectReparseAttributes(File.GetAttributes(path), path);
        }
        if (Directory.Exists(runDirectory) || File.Exists(runDirectory))
            throw new InvalidOperationException("Refusing to reuse an existing E2E artifact directory.");
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(runDirectory);
        using (var marker = new FileStream(Path.Combine(runDirectory, "typed-run.json"),
                   FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        {
            using var writer = new Utf8JsonWriter(marker);
            writer.WriteStartObject();
            writer.WriteString("schema", "RimWorldModdingMcp/OwnedE2ERun/v1");
            writer.WriteString("runDirectory", runDirectory);
            writer.WriteEndObject();
            writer.Flush();
            marker.Flush(flushToDisk: true);
        }
        AdapterOperationResult? result = null;
        Exception? primary = null;
        try { result = await execute(command, cancellationToken); }
        catch (Exception exception)
        {
            primary = exception;
            if (exception is AdapterOperationException failed) result = failed.Result;
        }

        string receipt = Path.Combine(runDirectory, "fixture-recovery-result.json");
        var outcomes = new List<(string Lease, string Status, string? Failure)>();
        string? failure = null;
        try
        {
            using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await RecoverAsync(root, runDirectory, execute, inspectProcess ?? InspectProcess, outcomes, budget.Token);
        }
        catch (Exception exception) { failure = exception.Message; }
        try
        {
            WriteReceipt(root, runDirectory, receipt, primary is OperationCanceledException ? "cancelled" :
                primary is null && result?.ExitCode == 0 ? "completed" : "failed", result?.ExitCode, failure, outcomes);
        }
        catch (Exception exception) { failure = $"Recovery receipt could not be retained: {exception.Message}"; }
        if (failure is not null)
            throw new InvalidOperationException($"E2E fixture recovery incomplete: {failure} " +
                (result is null ? "" : $"Primary runner exit code: {result.ExitCode}. ") + $"See {receipt}", primary);
        if (primary is not null)
        {
            primary.Data["FixtureRecoveryReceipt"] = receipt;
            ExceptionDispatchInfo.Capture(primary).Throw();
        }
        return result!;
    }

    private static async Task RecoverAsync(string repositoryRoot, string runDirectory,
        Func<AdapterCommand, CancellationToken, Task<AdapterOperationResult>> execute,
        Func<int, DateTimeOffset, string> inspectProcess,
        List<(string Lease, string Status, string? Failure)> outcomes, CancellationToken budget)
    {
        using var marker = ReadJson(repositoryRoot, Path.Combine(runDirectory, "typed-run.json"));
        RequireShape(marker.RootElement, "schema", "runDirectory");
        if (marker.RootElement.GetProperty("schema").GetString() != "RimWorldModdingMcp/OwnedE2ERun/v1" ||
            !string.Equals(marker.RootElement.GetProperty("runDirectory").GetString(), runDirectory, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Owned E2E marker changed; recovery refused.");

        // The runner may still create the catalog; its absence is only meaningful after exact runner exit.
        RequireStopped(repositoryRoot, Path.Combine(runDirectory, "runner-process.json"), inspectProcess);
        string catalogPath = Path.Combine(runDirectory, "fixture-recovery.json");
        if (!ExistsSafely(repositoryRoot, catalogPath)) return;
        using var catalog = ReadJson(repositoryRoot, catalogPath);
        RequireShape(catalog.RootElement, "schema", "groups");
        if (catalog.RootElement.GetProperty("schema").GetString() != "RimWorldModdingMcp/E2EFixtureRecovery/v1")
            throw new InvalidOperationException("Unknown fixture recovery catalog schema.");
        var entries = new List<(string Lease, string Process, bool Attempted, string StageProcess, bool StageAttempted)>();
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in catalog.RootElement.GetProperty("groups").EnumerateArray())
        {
            RequireShape(entry, "fixtureLeaseFile", "processLeaseFile", "launchAttempted", "stageProcessLeaseFile", "stageLaunchAttempted");
            string lease = CatalogPath(repositoryRoot, runDirectory, entry.GetProperty("fixtureLeaseFile").GetString(), "fixture.lease.json");
            string process = CatalogPath(repositoryRoot, runDirectory, entry.GetProperty("processLeaseFile").GetString(), "game-process.json");
            string stage = CatalogPath(repositoryRoot, runDirectory, entry.GetProperty("stageProcessLeaseFile").GetString(), "fixture-stage-process.json");
            if (Path.GetDirectoryName(stage) != Path.GetDirectoryName(lease) || !paths.Add(lease) || !paths.Add(process) || !paths.Add(stage))
                throw new InvalidOperationException("Duplicate or mismatched fixture recovery path.");
            entries.Add((lease, process, entry.GetProperty("launchAttempted").GetBoolean(), stage, entry.GetProperty("stageLaunchAttempted").GetBoolean()));
        }
        if (entries.Count == 0) return;
        foreach (var entry in entries)
        {
            budget.ThrowIfCancellationRequested();
            try
            {
                // An orphan publisher can create its lease after runner death: check it before treating absence as clean.
                if (entry.StageAttempted) RequireStopped(repositoryRoot, entry.StageProcess, inspectProcess);
                else if (ExistsSafely(repositoryRoot, entry.StageProcess))
                    throw new InvalidOperationException("Unattempted fixture publication has contradictory process evidence.");
                if (!ExistsSafely(repositoryRoot, entry.Lease))
                {
                    outcomes.Add((entry.Lease, "already-clean", null));
                    continue;
                }
                if (entry.Attempted)
                    RequireStopped(repositoryRoot, entry.Process, inspectProcess);
                else if (ExistsSafely(repositoryRoot, entry.Process))
                    throw new InvalidOperationException("Unattempted game launch has contradictory process evidence.");
                string host = Path.Combine(repositoryRoot, "tools", "RimWorldDevGateway.EndToEndHost", "bin", "Release", "net8.0", "RimWorldDevGateway.EndToEndHost.dll");
                if (!ExistsSafely(repositoryRoot, host)) throw new InvalidOperationException("Existing E2E host binary is unavailable for recovery.");
                var clean = new AdapterCommand("e2e_fixture_recovery", "dotnet",
                    [host, "fixture-clean", "--lease-file", entry.Lease, "--output", "json"],
                    repositoryRoot, TimeSpan.FromSeconds(60), runDirectory);
                AdapterOperationResult cleaned;
                try { cleaned = await execute(clean, budget); }
                catch (Exception exception)
                {
                    // Host stderr is diagnostic output, never copied into the credential-free receipt.
                    throw new InvalidOperationException($"fixture-clean failed ({exception.GetType().Name}); lease retained.");
                }
                if (cleaned.ExitCode != 0 || ExistsSafely(repositoryRoot, entry.Lease))
                    throw new InvalidOperationException("fixture-clean did not clear the exact recovery lease.");
                outcomes.Add((entry.Lease, "cleaned", null));
            }
            catch (Exception exception) { outcomes.Add((entry.Lease, "incomplete", exception.Message)); }
        }
        if (outcomes.Any(value => value.Status == "incomplete"))
            throw new InvalidOperationException("One or more registered fixture leases were retained; inspect recovery outcomes.");
    }

    private static void RequireStopped(string root, string path, Func<int, DateTimeOffset, string> inspect)
    {
        using var identity = ReadJson(root, path);
        var value = identity.RootElement;
        RequireShape(value, "schema", "processId", "processStartUtc");
        if (value.GetProperty("schema").GetString() != "RimWorldDevGateway/ProcessLease/v1" ||
            !value.GetProperty("processId").TryGetInt32(out int pid) || pid <= 0 ||
            !DateTimeOffset.TryParse(value.GetProperty("processStartUtc").GetString(),
                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var start) ||
            start.Offset != TimeSpan.Zero || inspect(pid, start) != "exited")
            throw new InvalidOperationException($"Exact process exit is unproven: {path}");
    }

    private static string InspectProcess(int pid, DateTimeOffset expected)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return process.StartTime.ToUniversalTime().Ticks == expected.UtcTicks ? "running" : "pid-reused";
        }
        catch (ArgumentException) { return "exited"; }
        catch { return "unknown"; }
    }

    private static string CatalogPath(string root, string runDirectory, string? relative, string leaf)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative))
            throw new InvalidOperationException("Recovery paths must be relative to the owned run.");
        string[] parts = relative.Split(['/', '\\']);
        if (parts.Length != 2 || parts[0] is "" or "." or ".." || parts[1] != leaf ||
            parts[0] != parts[0].TrimEnd(' ', '.') ||
            parts[0].IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            (leaf == "game-process.json" && !System.Text.RegularExpressions.Regex.IsMatch(parts[0], "^smoke-[0-9]{3,}$")))
            throw new InvalidOperationException("Unexpected fixture recovery path shape.");
        string path = Path.GetFullPath(Path.Combine(runDirectory, parts[0], parts[1]));
        _ = ExistsSafely(root, path);
        return path;
    }

    private static bool ExistsSafely(string root, string path)
    {
        foreach (string segment in ReleaseCandidateBuilder.PathsToInspect(root, path))
        {
            try { ReleaseCandidateBuilder.RejectReparseAttributes(File.GetAttributes(segment), segment); }
            catch (FileNotFoundException) { return false; }
            catch (DirectoryNotFoundException) { return false; }
        }
        return true;
    }

    private static JsonDocument ReadJson(string root, string path)
    {
        if (!ExistsSafely(root, path)) throw new InvalidOperationException($"Required recovery evidence is missing: {path}");
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > 65536) throw new InvalidOperationException("Recovery evidence exceeds 64 KiB.");
        var bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        if (stream.ReadByte() != -1) throw new InvalidOperationException("Recovery evidence changed during read.");
        return JsonDocument.Parse(bytes);
    }

    private static void RequireShape(JsonElement value, params string[] names)
    {
        var remaining = new HashSet<string>(names, StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
            if (!remaining.Remove(property.Name)) throw new InvalidOperationException("Unknown or duplicate recovery field.");
        if (remaining.Count != 0) throw new InvalidOperationException("Required recovery field is missing.");
    }

    private static void WriteReceipt(string root, string runDirectory, string path, string primaryOutcome,
        int? primaryExitCode, string? failure, List<(string Lease, string Status, string? Failure)> outcomes)
    {
        _ = ExistsSafely(root, path);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using var writer = new Utf8JsonWriter(stream);
                writer.WriteStartObject();
                writer.WriteString("schema", "RimWorldModdingMcp/E2EFixtureRecoveryResult/v1");
                writer.WriteString("runDirectory", runDirectory);
                writer.WriteString("primaryOutcome", primaryOutcome);
                if (primaryExitCode is { } exitCode) writer.WriteNumber("primaryExitCode", exitCode);
                else writer.WriteNull("primaryExitCode");
                writer.WriteString("status", failure is null ? "completed" : "incomplete");
                writer.WriteString("failure", failure);
                writer.WriteStartArray("groups");
                foreach (var outcome in outcomes)
                {
                    writer.WriteStartObject();
                    writer.WriteString("fixtureLeaseFile", outcome.Lease);
                    writer.WriteString("status", outcome.Status);
                    writer.WriteString("failure", outcome.Failure);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.Flush();
                stream.Flush(true);
            }
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
