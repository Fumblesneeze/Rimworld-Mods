using System.Diagnostics;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RimWorldModding.Mcp;

internal sealed record ReleaseWorkerRequest(
    string Schema,
    string RepositoryRoot,
    string PlanPath,
    string PlanSha256,
    string ConfirmationNonce,
    string LeasePath,
    string ResultPath,
    string ErrorPath,
    DateTimeOffset RequestedUtc,
    bool Recovery,
    int Attempt);

internal sealed record ReleaseWorkerLease(
    string Schema,
    string PlanSha256,
    int ProcessId,
    DateTimeOffset ProcessStartUtc,
    DateTimeOffset StartedUtc);

public sealed record ReleaseWorkerStatus(
    string State,
    string? LeasePath,
    int? ProcessId,
    DateTimeOffset? ProcessStartUtc,
    string? ResultPath,
    string? ErrorPath);

public sealed class ReleaseWorkerCoordinator(string repositoryRoot)
{
    private const int MaximumAttempts = 9;
    private static readonly ConcurrentDictionary<int, Process> ActiveWorkers = new();
    private readonly string _repositoryRoot = RepositoryRoot.Resolve(repositoryRoot);

    public async Task<ReleasePublishResult> PublishAsync(
        string planPath,
        string planSha256,
        string confirmationNonce,
        CancellationToken cancellationToken)
    {
        var status = ReleasePlanAdmission.Status(_repositoryRoot, planPath, planSha256, confirmationNonce);
        var paths = Paths(status.PackageId, status.PlanSha256);
        var mutexName = "Local\\RimWorldModdingMcp.ReleaseWorker." +
                        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(status.PackageId)))[..24];
        while (true)
        {
            if (TryReadValidResult(paths, status, out var completed)) return completed!;
            using (var mutex = new Semaphore(1, 1, mutexName))
            {
                if (mutex.WaitOne(TimeSpan.FromSeconds(10)))
                {
                    try
                    {
                        if (TryReadValidResult(paths, status, out completed)) return completed!;
                        EnsureWorkerStarted(paths, status, planPath, confirmationNonce);
                    }
                    finally
                    {
                        mutex.Release();
                    }
                }
            }
            try
            {
                return await WaitAsync(paths, status, cancellationToken);
            }
            catch (WorkerNeedsRecoveryException)
            {
                // Re-enter the package worker lease and either restart pre-admission or resume the exact durable item.
            }
        }
    }

    public ReleaseWorkerStatus Status(string packageId, string planSha256)
    {
        var paths = Paths(packageId, planSha256);
        if (File.Exists(paths.Result))
        {
            try
            {
                _ = ReadValidatedResult(_repositoryRoot, paths.Result, packageId, planSha256, null, null);
                return new ReleaseWorkerStatus("completed", paths.Lease, null, null, paths.Result, null);
            }
            catch
            {
                return new ReleaseWorkerStatus("invalid-result", paths.Lease, null, null, paths.Result, null);
            }
        }
        if (File.Exists(paths.Error))
            return new ReleaseWorkerStatus("failed", paths.Lease, null, null, null, paths.Error);
        if (!File.Exists(paths.Request))
            return new ReleaseWorkerStatus("none", null, null, null, null, null);
        if (!File.Exists(paths.Lease))
            return new ReleaseWorkerStatus("starting", paths.Lease, null, null, null, null);
        var lease = ReadLease(paths.Lease, planSha256);
        var identity = RunProcessIdentity.Inspect(lease.ProcessId, lease.ProcessStartUtc);
        return new ReleaseWorkerStatus(identity.State, paths.Lease, lease.ProcessId,
            lease.ProcessStartUtc, null, null);
    }

    internal static async Task<int> RunAsync(string requestPath)
    {
        ReleaseWorkerRequest? request = null;
        try
        {
            request = JsonSerializer.Deserialize(
                File.ReadAllText(requestPath), McpJsonContext.Default.ReleaseWorkerRequest) ??
                      throw new InvalidOperationException("Release worker request is empty.");
            if (request.Schema != "RimWorldModdingMcp/ReleaseWorkerRequest/v1")
                throw new InvalidOperationException("Release worker request schema is unsupported.");
            if (request.Attempt is < 1 or > MaximumAttempts)
                throw new InvalidOperationException("Release worker attempt is outside the bounded range.");
            var root = RepositoryRoot.Resolve(request.RepositoryRoot);
            var workerRoot = Path.Combine(root, "artifacts", "Releases",
                ReadPlanPackageId(root, request.PlanPath), "workers", request.PlanSha256);
            var expectedRequest = RepositoryRoot.ContainedPath(root, Path.Combine(workerRoot, "worker-request.json"));
            if (!string.Equals(RepositoryRoot.ContainedPath(root, requestPath), expectedRequest,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(RepositoryRoot.ContainedPath(root, request.LeasePath), Path.Combine(workerRoot, "worker-lease.json"),
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(RepositoryRoot.ContainedPath(root, request.ResultPath), Path.Combine(workerRoot, "worker-result.json"),
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(RepositoryRoot.ContainedPath(root, request.ErrorPath), Path.Combine(workerRoot, "worker-error.txt"),
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Release worker request is not in its canonical artifact path.");
            using var process = Process.GetCurrentProcess();
            var start = new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero);
            var lease = new ReleaseWorkerLease(
                "RimWorldModdingMcp/ReleaseWorkerLease/v1",
                request.PlanSha256,
                process.Id,
                start,
                DateTimeOffset.UtcNow);
            DurableFile.WriteAllText(request.LeasePath,
                JsonSerializer.Serialize(lease, McpJsonContext.Default.ReleaseWorkerLease) + Environment.NewLine);
            var publisher = new ReleasePublisher(root);
            var result = request.Recovery
                ? await publisher.ResumeAsync(
                    request.PlanPath, request.PlanSha256, request.ConfirmationNonce, CancellationToken.None)
                : await publisher.PublishAsync(
                    request.PlanPath, request.PlanSha256, request.ConfirmationNonce, CancellationToken.None);
            DurableFile.WriteAllText(request.ResultPath,
                JsonSerializer.Serialize(result, McpJsonContext.Default.ReleasePublishResult) + Environment.NewLine);
            return 0;
        }
        catch (Exception exception)
        {
            if (request is not null)
            {
                DurableFile.WriteAllText(request.ErrorPath,
                    exception.GetBaseException().Message + Environment.NewLine);
            }
            return 1;
        }
    }

    private async Task<ReleasePublishResult> WaitAsync(
        WorkerPaths paths,
        ReleasePlanStatusResult planStatus,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddMinutes(61);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (TryReadValidResult(paths, planStatus, out var result)) return result!;
            if (File.Exists(paths.Error))
                throw new WorkerNeedsRecoveryException();
            if (File.Exists(paths.Lease))
            {
                var lease = ReadLease(paths.Lease, planStatus.PlanSha256);
                var identity = RunProcessIdentity.Inspect(lease.ProcessId, lease.ProcessStartUtc);
                if (identity.State != "running")
                    throw new WorkerNeedsRecoveryException();
            }
            else if (File.Exists(paths.Request))
            {
                var request = ReadRequest(paths.Request, planStatus.PlanSha256);
                if (DateTimeOffset.UtcNow - request.RequestedUtc > TimeSpan.FromSeconds(15))
                    throw new WorkerNeedsRecoveryException();
            }
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(1000, cancellationToken);
        }
        throw new TimeoutException("Durable release worker exceeded its 61-minute recovery window.");
    }

    private void EnsureWorkerStarted(
        WorkerPaths paths,
        ReleasePlanStatusResult planStatus,
        string planPath,
        string confirmationNonce)
    {
        var running = false;
        if (File.Exists(paths.Lease))
        {
            var lease = ReadLease(paths.Lease, planStatus.PlanSha256);
            running = RunProcessIdentity.Inspect(lease.ProcessId, lease.ProcessStartUtc).State == "running";
        }
        if (running) return;
        if (!File.Exists(paths.Lease) && File.Exists(paths.Request) && !File.Exists(paths.Error))
        {
            var pending = ReadRequest(paths.Request, planStatus.PlanSha256);
            if (DateTimeOffset.UtcNow - pending.RequestedUtc <= TimeSpan.FromSeconds(15)) return;
        }

        var statePath = Path.Combine(_repositoryRoot, "artifacts", "Releases", planStatus.PackageId,
            "publication-state.txt");
        var durableState = File.Exists(statePath) ? File.ReadAllText(statePath).Trim() : "none";
        if (durableState.StartsWith("complete-reviewed|", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "The release is already complete-reviewed but its exact retained worker result is missing or invalid; refusing to manufacture a replacement.");
        var priorAttempt = File.Exists(paths.Request)
            ? ReadRequest(paths.Request, planStatus.PlanSha256).Attempt
            : 0;
        var recovery = ReleasePlanAdmission.IsRecoverableDurableState(
            durableState, planStatus.PlanSha256, out _);
        if (!recovery && durableState != "none")
            throw new InvalidOperationException(
                "Durable Steam state is definite or indeterminate without a safe same-item recovery path: " + durableState);
        if (!recovery)
        {
            if (priorAttempt > 0) CleanupPreAdmissionGateway(planStatus);
            _ = ReleasePlanAdmission.ValidateLocal(
                _repositoryRoot, planPath, planStatus.PlanSha256, confirmationNonce,
                DateTimeOffset.UtcNow, requireCleanRevision: true);
        }

        var attempt = priorAttempt + 1;
        if (attempt > MaximumAttempts)
            throw new InvalidOperationException(
                $"The durable release worker exhausted {MaximumAttempts} bounded attempts; exact state is retained for diagnosis and no duplicate Steam create was issued.");

        Directory.CreateDirectory(paths.Root);
        DeleteIfExists(paths.Lease);
        DeleteIfExists(paths.Error);
        var request = new ReleaseWorkerRequest(
            "RimWorldModdingMcp/ReleaseWorkerRequest/v1",
            _repositoryRoot,
            RepositoryRoot.ContainedPath(_repositoryRoot, planPath),
            planStatus.PlanSha256,
            confirmationNonce,
            paths.Lease,
            paths.Result,
            paths.Error,
            DateTimeOffset.UtcNow,
            recovery,
            attempt);
        DurableFile.WriteAllText(paths.Request,
            JsonSerializer.Serialize(request, McpJsonContext.Default.ReleaseWorkerRequest) + Environment.NewLine);
        try
        {
            StartWorker(paths.Request);
        }
        catch
        {
            if (!File.Exists(paths.Lease)) DeleteIfExists(paths.Request);
            throw;
        }
    }

    private bool TryReadValidResult(
        WorkerPaths paths,
        ReleasePlanStatusResult planStatus,
        out ReleasePublishResult? result)
    {
        result = null;
        if (!File.Exists(paths.Result)) return false;
        var parsed = ReadValidatedResult(_repositoryRoot, paths.Result, planStatus.PackageId, planStatus.PlanSha256,
            planStatus.Title, planStatus.CandidateDigest);
        result = parsed;
        return true;
    }

    internal static ReleasePublishResult ReadValidatedResult(
        string repositoryRoot,
        string resultPath,
        string packageId,
        string planSha256,
        string? expectedTitle,
        string? expectedCandidateDigest)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        var result = RepositoryRoot.ContainedPath(root, resultPath);
        var expectedResult = Path.GetFullPath(Path.Combine(
            root, "artifacts", "Releases", packageId, "workers", planSha256.ToUpperInvariant(), "worker-result.json"));
        if (!string.Equals(result, expectedResult, StringComparison.OrdinalIgnoreCase) || !File.Exists(result))
            throw new InvalidOperationException("Release worker result is not the exact retained plan result.");
        var parsed = JsonSerializer.Deserialize(
                         File.ReadAllText(result), McpJsonContext.Default.ReleasePublishResult) ??
                     throw new InvalidOperationException("Release worker result is empty.");
        if (!string.Equals(parsed.Status,
                "published-steam-verified-subscriber-evidence-awaiting-personal-review", StringComparison.Ordinal) ||
            !string.Equals(parsed.PackageId, packageId, StringComparison.OrdinalIgnoreCase) ||
            (expectedTitle is not null && !string.Equals(parsed.Title, expectedTitle, StringComparison.Ordinal)) ||
            !string.Equals(parsed.PublicationPlanSha256, planSha256, StringComparison.OrdinalIgnoreCase) ||
            (expectedCandidateDigest is not null &&
             !string.Equals(parsed.CandidateDigest, expectedCandidateDigest, StringComparison.Ordinal)) ||
            !parsed.SteamVerified || !parsed.LocalPackageRestored ||
            !ulong.TryParse(parsed.PublishedFileId, out var publishedId) || publishedId == 0)
            throw new InvalidOperationException("Release worker result does not match the exact admitted plan and completed guarantees.");
        var releaseRoot = Path.Combine(root, "artifacts", "Releases", packageId);
        var statePath = Path.Combine(releaseRoot, "publication-state.txt");
        var state = File.Exists(statePath) ? File.ReadAllText(statePath).Trim().Split('|') : [];
        if (state.Length != 3 || state[0] is not ("subscriber-evidence-awaiting-review" or "complete-reviewed") ||
            !string.Equals(state[1], planSha256, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(state[2], parsed.PublishedFileId, StringComparison.Ordinal))
            throw new InvalidOperationException("Release worker result does not match a final subscriber-evidence durable state.");
        var publicationRoot = Path.Combine(releaseRoot, "publication") +
                               Path.DirectorySeparatorChar;
        var receipt = RepositoryRoot.ContainedPath(root, parsed.ReceiptPath);
        if (!receipt.StartsWith(publicationRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(receipt) ||
            parsed.ReceiptSha256.Length != 64 ||
            !string.Equals(ReleaseCandidateBuilder.Hash(receipt), parsed.ReceiptSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Release worker result is not bound to a retained publisher receipt.");
        using var document = JsonDocument.Parse(File.ReadAllText(receipt));
        var response = document.RootElement;
        if (GatewayWorkshopClient.String(response, "schema") != "RimWorldModReleaseReceipt/v1" ||
            GatewayWorkshopClient.String(response, "packageId") != parsed.PackageId ||
            GatewayWorkshopClient.String(response, "title") != parsed.Title ||
            GatewayWorkshopClient.String(response, "candidateDigest") != parsed.CandidateDigest ||
            GatewayWorkshopClient.String(response, "publicationPlanSha256") != parsed.PublicationPlanSha256 ||
            GatewayWorkshopClient.String(response, "publishedFileId") != parsed.PublishedFileId ||
            GatewayWorkshopClient.String(response, "subscriberEvidenceStatus") != "awaiting-personal-review" ||
            !GatewayWorkshopClient.TryProperty(response, "subscriber", out var subscriber) ||
            GatewayWorkshopClient.String(subscriber, "status") != "passed" ||
            !GatewayWorkshopClient.TryProperty(subscriber, "screenshots", out var screenshotArray) ||
            screenshotArray.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Release worker receipt does not prove exact successful subscriber evidence.");
        var expectedEvidencePrefix = Path.Combine(releaseRoot, "subscriber") + Path.DirectorySeparatorChar;
        var evidenceRoot = Path.GetFullPath(GatewayWorkshopClient.String(subscriber, "evidenceRoot"))
            .TrimEnd(Path.DirectorySeparatorChar);
        if (!evidenceRoot.StartsWith(expectedEvidencePrefix, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(evidenceRoot,
                Path.GetFullPath(parsed.SubscriberEvidenceRoot).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Release worker receipt does not bind the exact subscriber evidence root.");
        var evidencePrefix = evidenceRoot + Path.DirectorySeparatorChar;
        var screenshots = screenshotArray.EnumerateArray()
            .Select(value => Path.GetFullPath(value.GetString() ?? ""))
            .ToArray();
        if (screenshots.Length < 2 || screenshots.Distinct(StringComparer.OrdinalIgnoreCase).Count() != screenshots.Length ||
            screenshots.Any(path => !path.StartsWith(evidencePrefix, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)))
            throw new InvalidOperationException("Release worker receipt does not retain at least two exact subscriber screenshots.");
        return parsed;
    }

    private void CleanupPreAdmissionGateway(ReleasePlanStatusResult planStatus)
    {
        var association = Path.Combine(_repositoryRoot, "artifacts", "Releases", planStatus.PackageId,
            "publisher-gateway-run.txt");
        if (!File.Exists(association)) return;
        var parts = File.ReadAllText(association).Trim().Split('|');
        if (parts.Length != 2 || !string.Equals(parts[0], planStatus.PlanSha256, StringComparison.OrdinalIgnoreCase))
            return;
        var manager = new RunLeaseManager(_repositoryRoot);
        try
        {
            var status = manager.Status(parts[1]);
            if (status.State is "ready" or "starting" or "orphaned-game")
                _ = manager.CancelAsync(parts[1], CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (ArgumentException)
        {
            // The exact reserved run never reached lease creation.
        }
    }

    private static ReleaseWorkerRequest ReadRequest(string path, string planSha256)
    {
        var request = JsonSerializer.Deserialize(
            File.ReadAllText(path), McpJsonContext.Default.ReleaseWorkerRequest) ??
                      throw new InvalidOperationException("Release worker request is empty.");
        if (request.Schema != "RimWorldModdingMcp/ReleaseWorkerRequest/v1" ||
            !string.Equals(request.PlanSha256, planSha256, StringComparison.OrdinalIgnoreCase) ||
            request.Attempt is < 1 or > MaximumAttempts)
            throw new InvalidOperationException("Release worker request does not match the exact publication plan.");
        return request;
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private static void StartWorker(string requestPath)
    {
        var processPath = Environment.ProcessPath ??
                          throw new InvalidOperationException("Could not resolve the current MCP executable.");
        var info = new ProcessStartInfo
        {
            FileName = processPath,
            WorkingDirectory = Path.GetDirectoryName(requestPath)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            var assembly = Environment.GetCommandLineArgs().FirstOrDefault();
            if (string.IsNullOrWhiteSpace(assembly) || !File.Exists(assembly))
                throw new InvalidOperationException("Could not resolve the MCP entry assembly for the release worker.");
            info.ArgumentList.Add(Path.GetFullPath(assembly));
        }
        info.ArgumentList.Add("release-worker");
        info.ArgumentList.Add(requestPath);
        var process = Process.Start(info) ?? throw new InvalidOperationException("Durable release worker did not start.");
        process.StandardInput.Close();
        ActiveWorkers[process.Id] = process;
        var stdoutPath = Path.Combine(Path.GetDirectoryName(requestPath)!, "worker.stdout.log");
        var stderrPath = Path.Combine(Path.GetDirectoryName(requestPath)!, "worker.stderr.log");
        var stdout = DrainAsync(process.StandardOutput, stdoutPath);
        var stderr = DrainAsync(process.StandardError, stderrPath);
        _ = Task.Run(async () =>
        {
            try
            {
                await process.WaitForExitAsync(CancellationToken.None);
                await Task.WhenAll(stdout, stderr);
            }
            finally
            {
                if (ActiveWorkers.TryRemove(process.Id, out var owned)) owned.Dispose();
            }
        });
    }

    private static async Task DrainAsync(StreamReader reader, string path)
    {
        await using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite,
            4096, FileOptions.Asynchronous);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        var buffer = new char[4096];
        while (true)
        {
            var count = await reader.ReadAsync(buffer);
            if (count == 0) break;
            await writer.WriteAsync(buffer.AsMemory(0, count));
            await writer.FlushAsync();
        }
    }

    private static ReleaseWorkerLease ReadLease(string path, string planSha256)
    {
        var lease = JsonSerializer.Deserialize(
            File.ReadAllText(path), McpJsonContext.Default.ReleaseWorkerLease) ??
                    throw new InvalidOperationException("Release worker lease is empty.");
        if (lease.Schema != "RimWorldModdingMcp/ReleaseWorkerLease/v1" ||
            !string.Equals(lease.PlanSha256, planSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Release worker lease does not match the exact publication plan.");
        return lease;
    }

    private static string ReadPlanPackageId(string root, string planPath)
    {
        var plan = JsonSerializer.Deserialize(
            File.ReadAllText(RepositoryRoot.ContainedPath(root, planPath)), McpJsonContext.Default.ReleasePublicationPlan) ??
                   throw new InvalidOperationException("Release publication plan is empty.");
        return plan.PackageId;
    }

    private WorkerPaths Paths(string packageId, string planSha256)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(packageId, "^[a-z0-9][a-z0-9._-]{0,199}$") ||
            planSha256.Length != 64 || planSha256.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException("Release worker identity is invalid.");
        var root = Path.Combine(_repositoryRoot, "artifacts", "Releases", packageId, "workers", planSha256.ToUpperInvariant());
        return new WorkerPaths(root, Path.Combine(root, "worker-request.json"), Path.Combine(root, "worker-lease.json"),
            Path.Combine(root, "worker-result.json"), Path.Combine(root, "worker-error.txt"));
    }

    private sealed record WorkerPaths(string Root, string Request, string Lease, string Result, string Error);

    private sealed class WorkerNeedsRecoveryException : Exception;
}
