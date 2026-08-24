using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace RimWorldModding.Mcp;

public sealed record ProcessIdentityState(string State, int ProcessId, DateTimeOffset ExpectedStartUtc, DateTimeOffset? ActualStartUtc);

public sealed record RunLeaseRecord(
    string Schema,
    string RunId,
    string Kind,
    int LauncherProcessId,
    DateTimeOffset LauncherStartUtc,
    string RunRoot,
    string CompletionFile,
    string GameProcessLeaseFile,
    string StandardOutputPath,
    string StandardErrorPath,
    DateTimeOffset StartedUtc);

public sealed record RunStartResult(
    string RunId,
    string State,
    int LauncherProcessId,
    DateTimeOffset LauncherStartUtc,
    string RunRoot,
    string LeasePath,
    string CompletionFile);

public sealed record RunStatusResult(
    string RunId,
    string State,
    int LauncherProcessId,
    DateTimeOffset LauncherStartUtc,
    int? GameProcessId,
    DateTimeOffset? GameProcessStartUtc,
    string? GatewayManifestPath,
    string RunRoot,
    string StandardOutputTail,
    string StandardErrorTail);

internal sealed record RunCancellationDecision(
    bool SignalCompletion,
    bool WaitForLauncher,
    bool GracefullyCloseGame,
    bool AllowExactPidFallback);

internal static class RunCancellationPolicy
{
    public static RunCancellationDecision Decide(string launcherState, string? gameState) => new(
        SignalCompletion: launcherState == "running" || gameState == "running",
        WaitForLauncher: launcherState == "running",
        GracefullyCloseGame: gameState == "running",
        AllowExactPidFallback: launcherState == "running" || gameState == "running");
}

public static class RunProcessIdentity
{
    public static ProcessIdentityState Inspect(int processId, DateTimeOffset expectedStartUtc)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            var actual = new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero);
            return new ProcessIdentityState(
                Math.Abs((actual - expectedStartUtc.ToUniversalTime()).TotalSeconds) <= 1 ? "running" : "pid-reused",
                processId,
                expectedStartUtc,
                actual);
        }
        catch (ArgumentException)
        {
            return new ProcessIdentityState("exited", processId, expectedStartUtc, null);
        }
        catch (InvalidOperationException)
        {
            return new ProcessIdentityState("exited", processId, expectedStartUtc, null);
        }
    }
}

public sealed class RunLeaseManager
{
    private static readonly ConcurrentDictionary<string, (Process Process, Task Stdout, Task Stderr)> Active = new(StringComparer.Ordinal);
    private readonly string _repositoryRoot;
    private readonly string _runsRoot;

    public RunLeaseManager(string repositoryRoot)
    {
        _repositoryRoot = RepositoryRoot.Resolve(repositoryRoot);
        _runsRoot = Path.Combine(_repositoryRoot, "artifacts", "McpRuns");
    }

    public RunStartResult StartGateway(
        IReadOnlyList<string> packageIds,
        IReadOnlyList<string> projectPaths,
        int holdSeconds,
        string? reservedRunId = null)
    {
        RefuseRunningRimWorld();
        var runId = reservedRunId ?? ReserveRunId();
        if (!System.Text.RegularExpressions.Regex.IsMatch(runId, "^[0-9]{8}T[0-9]{9}Z-[a-f0-9]{32}$"))
            throw new ArgumentException("Reserved runId is invalid.");
        var runRoot = Path.Combine(_runsRoot, runId);
        if (Directory.Exists(runRoot)) throw new InvalidOperationException("Reserved Gateway run already exists.");
        Directory.CreateDirectory(runRoot);
        var command = RepositoryOperationPlanner.GatewayRun(
            _repositoryRoot,
            runRoot,
            packageIds,
            projectPaths,
            holdSeconds);
        var stdoutPath = Path.Combine(runRoot, "launcher.stdout.log");
        var stderrPath = Path.Combine(runRoot, "launcher.stderr.log");
        var info = new ProcessStartInfo
        {
            FileName = command.FileName,
            WorkingDirectory = command.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in command.Arguments) info.ArgumentList.Add(argument);
        var process = new Process { StartInfo = info, EnableRaisingEvents = true };
        try
        {
            if (!process.Start()) throw new InvalidOperationException("Gateway launcher did not start.");
            process.StandardInput.Close();
            var start = new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero);
            var stdout = PumpAsync(process.StandardOutput, stdoutPath);
            var stderr = PumpAsync(process.StandardError, stderrPath);
            Active[runId] = (process, stdout, stderr);
            var record = new RunLeaseRecord(
                "RimWorldModdingMcp/RunLease/v1",
                runId,
                "gateway",
                process.Id,
                start,
                runRoot,
                Path.Combine(runRoot, "complete.signal"),
                Path.Combine(runRoot, "game-process.json"),
                stdoutPath,
                stderrPath,
                DateTimeOffset.UtcNow);
            var leasePath = LeasePath(runId);
            WriteJsonAtomically(leasePath, JsonSerializer.Serialize(record, McpJsonContext.Default.RunLeaseRecord));
            return new RunStartResult(runId, "starting", process.Id, start, runRoot, leasePath, record.CompletionFile);
        }
        catch
        {
            process.Dispose();
            throw;
        }
    }

    public static string ReserveRunId() => $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}";

    public RunStatusResult Status(string runId)
    {
        var record = Read(runId);
        var launcher = RunProcessIdentity.Inspect(record.LauncherProcessId, record.LauncherStartUtc);
        var (gamePid, gameStart) = ReadGameIdentity(record.GameProcessLeaseFile);
        var gameState = gamePid.HasValue && gameStart.HasValue
            ? RunProcessIdentity.Inspect(gamePid.Value, gameStart.Value).State
            : null;
        var manifest = Directory.Exists(record.RunRoot)
            ? Directory.GetFiles(record.RunRoot, "current.json", SearchOption.AllDirectories)
                .Where(path => path.EndsWith($"{Path.DirectorySeparatorChar}DevGateway{Path.DirectorySeparatorChar}current.json", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault()
            : null;
        var state = launcher.State == "running"
            ? manifest is not null && gameState == "running" ? "ready" : "starting"
            : gameState == "running" ? "orphaned-game" : launcher.State;
        return new RunStatusResult(
            runId,
            state,
            record.LauncherProcessId,
            record.LauncherStartUtc,
            gamePid,
            gameStart,
            manifest,
            record.RunRoot,
            Tail(record.StandardOutputPath),
            Tail(record.StandardErrorPath));
    }

    public async Task<RunStatusResult> CancelAsync(string runId, CancellationToken cancellationToken)
    {
        var record = Read(runId);
        var identity = RunProcessIdentity.Inspect(record.LauncherProcessId, record.LauncherStartUtc);
        var (gamePid, gameStart) = ReadGameIdentity(record.GameProcessLeaseFile);
        var initialGameIdentity = gamePid.HasValue && gameStart.HasValue
            ? RunProcessIdentity.Inspect(gamePid.Value, gameStart.Value)
            : null;
        if (initialGameIdentity?.State == "pid-reused")
            throw new InvalidOperationException("Refusing cancellation because the leased game PID was reused.");
        var decision = RunCancellationPolicy.Decide(identity.State, initialGameIdentity?.State);
        if (decision.SignalCompletion)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(record.CompletionFile)!);
            if (!File.Exists(record.CompletionFile))
                File.WriteAllText(record.CompletionFile, "stop", new UTF8Encoding(false));
        }
        if (decision.WaitForLauncher)
        {
            using var process = Process.GetProcessById(record.LauncherProcessId);
            using var bounded = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            bounded.CancelAfter(TimeSpan.FromSeconds(45));
            try { await process.WaitForExitAsync(bounded.Token); }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                var exact = RunProcessIdentity.Inspect(record.LauncherProcessId, record.LauncherStartUtc);
                if (exact.State == "running")
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync(CancellationToken.None);
                }
            }
        }
        (gamePid, gameStart) = ReadGameIdentity(record.GameProcessLeaseFile);
        var finalGameIdentity = gamePid.HasValue && gameStart.HasValue
            ? RunProcessIdentity.Inspect(gamePid.Value, gameStart.Value)
            : null;
        if (finalGameIdentity?.State == "pid-reused")
            throw new InvalidOperationException("Refusing cancellation because the leased game PID was reused.");
        var finalDecision = RunCancellationPolicy.Decide(identity.State, finalGameIdentity?.State);
        if (finalDecision.GracefullyCloseGame && gamePid.HasValue && gameStart.HasValue)
        {
            var gameIdentity = RunProcessIdentity.Inspect(gamePid.Value, gameStart.Value);
            if (gameIdentity.State == "pid-reused")
                throw new InvalidOperationException("Refusing cancellation because the leased game PID was reused.");
            if (gameIdentity.State == "running")
            {
                using var game = Process.GetProcessById(gamePid.Value);
                var closeRequested = game.CloseMainWindow();
                if (closeRequested)
                {
                    using var graceful = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    graceful.CancelAfter(TimeSpan.FromSeconds(15));
                    try { await game.WaitForExitAsync(graceful.Token); }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }
                }
                var exact = RunProcessIdentity.Inspect(gamePid.Value, gameStart.Value);
                if (finalDecision.AllowExactPidFallback && exact.State == "running")
                {
                    game.Kill(entireProcessTree: true);
                    await game.WaitForExitAsync(CancellationToken.None);
                }
            }
        }
        CleanupActive(runId);
        return Status(runId);
    }

    private RunLeaseRecord Read(string runId)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(runId, "^[0-9]{8}T[0-9]{9}Z-[a-f0-9]{32}$"))
            throw new ArgumentException("runId is invalid.");
        var path = LeasePath(runId);
        if (!File.Exists(path)) throw new ArgumentException($"Unknown runId '{runId}'.");
        return JsonSerializer.Deserialize(File.ReadAllText(path), McpJsonContext.Default.RunLeaseRecord) ??
               throw new InvalidOperationException("Run lease is unreadable.");
    }

    private string LeasePath(string runId) => Path.Combine(_runsRoot, runId, "mcp-run.json");

    private static (int? Pid, DateTimeOffset? Start) ReadGameIdentity(string path)
    {
        if (!File.Exists(path)) return (null, null);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("processId", out var pid) || !pid.TryGetInt32(out var processId) ||
            !document.RootElement.TryGetProperty("processStartUtc", out var start) ||
            !DateTimeOffset.TryParse(start.GetString(), out var startUtc)) return (null, null);
        return (processId, startUtc);
    }

    private static async Task PumpAsync(StreamReader reader, string path)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        var buffer = new char[4096];
        int count;
        while ((count = await reader.ReadAsync(buffer)) > 0)
        {
            await writer.WriteAsync(buffer.AsMemory(0, count));
            await writer.FlushAsync();
        }
    }

    private static string Tail(string path)
    {
        if (!File.Exists(path)) return "";
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var length = Math.Min(stream.Length, 16384);
        stream.Seek(-length, SeekOrigin.End);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        return reader.ReadToEnd().Trim();
    }

    private static void WriteJsonAtomically(string path, string json)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(temporary, json + Environment.NewLine, new UTF8Encoding(false));
        File.Move(temporary, path);
    }

    private static void CleanupActive(string runId)
    {
        if (!Active.TryRemove(runId, out var active)) return;
        active.Process.Dispose();
    }

    private static void RefuseRunningRimWorld()
    {
        var processes = Process.GetProcesses()
            .Where(process => process.ProcessName.Equals("RimWorldWin64", StringComparison.OrdinalIgnoreCase) ||
                              process.ProcessName.Equals("RimWorld", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        try
        {
            if (processes.Length > 0) throw new ArgumentException("Refusing to launch while RimWorld is already running.");
        }
        finally
        {
            foreach (var process in processes) process.Dispose();
        }
    }
}
