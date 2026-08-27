using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RimWorldModding.Mcp;

public sealed record WorkshopSubscriptionCleanupResult(
    string Status,
    string PackageId,
    string PublishedFileId,
    uint BeforeItemState,
    uint AfterItemState,
    bool WasSubscribed,
    bool IsSubscribed,
    string EvidencePath,
    bool GatewayStopped);

public sealed class WorkshopSubscriptionCleaner(string repositoryRoot)
{
    private const uint SubscribedItemState = 1u;
    private readonly string _repositoryRoot = RepositoryRoot.Resolve(repositoryRoot);

    public async Task<WorkshopSubscriptionCleanupResult> CleanAsync(
        string packageId,
        CancellationToken cancellationToken)
    {
        var profile = ReleaseProfileCatalog.Discover(_repositoryRoot)
            .Single(item => string.Equals(item.PackageId, packageId, StringComparison.OrdinalIgnoreCase));
        if (profile.PublishedFileId is null || !ulong.TryParse(profile.PublishedFileId, out var publishedId) || publishedId == 0)
            throw new InvalidOperationException("Workshop subscription cleanup requires one canonical nonzero publishedFileId.");
        var checkedInIdentity = Path.Combine(Path.GetDirectoryName(profile.Project)!, "About", "PublishedFileId.txt");
        if (!File.Exists(checkedInIdentity) || File.ReadAllText(checkedInIdentity).Trim() != profile.PublishedFileId)
            throw new InvalidOperationException("Workshop subscription cleanup requires the matching checked-in item identity.");

        var correlation = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            profile.PackageId + "|" + profile.PublishedFileId))).ToUpperInvariant();
        var evidenceRoot = Path.Combine(
            _repositoryRoot,
            "artifacts",
            "Releases",
            profile.PackageId,
            "subscription-cleanup",
            $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}");
        Directory.CreateDirectory(evidenceRoot);
        var evidencePath = Path.Combine(evidenceRoot, "subscription-cleanup.json");
        var manager = new RunLeaseManager(_repositoryRoot);
        var runId = RunLeaseManager.ReserveRunId();
        var stopped = false;
        uint beforeState = 0;
        uint afterState = 0;
        try
        {
            var start = manager.StartGateway(Array.Empty<string>(), Array.Empty<string>(), 900, runId);
            var ready = await WaitReadyAsync(manager, start.RunId, DateTimeOffset.UtcNow.AddMinutes(8), cancellationToken);
            var client = new GatewayWorkshopClient(
                _repositoryRoot,
                ready.GatewayManifestPath!,
                ready.GameProcessId!.Value);
            await client.RegisterAsync(cancellationToken);
            var runtime = await client.InvokeAsync(
                new Dictionary<string, object?> { ["operation"] = "status" }, cancellationToken);
            WorkshopRemoteBaseline.AssertRuntimeIdentity(runtime, profile);

            _ = await client.InvokeAsync(new Dictionary<string, object?>
            {
                ["operation"] = "query",
                ["planSha256"] = correlation,
                ["publishedFileId"] = publishedId.ToString()
            }, cancellationToken);
            var remote = await client.WaitTerminalAsync(
                correlation,
                new HashSet<string>(["queried", "failed"], StringComparer.Ordinal),
                DateTimeOffset.UtcNow.AddMinutes(5),
                cancellationToken);
            if (GatewayWorkshopClient.String(remote, "Status") != "queried" ||
                GatewayWorkshopClient.UInt64(remote, "PublishedFileId") != publishedId ||
                GatewayWorkshopClient.UInt64(remote, "RemoteOwnerSteamId").ToString() != profile.SteamUserId ||
                GatewayWorkshopClient.UInt64(remote, "RemoteConsumerAppId") != (ulong)profile.SteamAppId)
                throw new InvalidOperationException("Workshop subscription cleanup could not prove the exact item, owner, and app.");

            beforeState = await ReadItemStateAsync(client, correlation, publishedId, cancellationToken);
            if (IsSubscribed(beforeState))
            {
                _ = await client.InvokeAsync(new Dictionary<string, object?>
                {
                    ["operation"] = "unsubscribe",
                    ["planSha256"] = correlation,
                    ["publishedFileId"] = publishedId.ToString()
                }, cancellationToken);
                var terminal = await client.WaitTerminalAsync(
                    correlation,
                    new HashSet<string>(["unsubscribe-callback-confirmed", "failed"], StringComparer.Ordinal),
                    DateTimeOffset.UtcNow.AddMinutes(5),
                    cancellationToken);
                if (GatewayWorkshopClient.String(terminal, "Status") != "unsubscribe-callback-confirmed")
                    throw new InvalidOperationException("Steam did not confirm the exact Workshop unsubscribe callback.");
            }
            afterState = await WaitForUnsubscribedStateAsync(
                client,
                correlation,
                publishedId,
                DateTimeOffset.UtcNow.AddSeconds(30),
                cancellationToken);
            if (IsSubscribed(afterState))
                throw new InvalidOperationException("Steam still reports the exact Workshop item as subscribed.");

            _ = await manager.CancelAsync(start.RunId, CancellationToken.None);
            stopped = true;
            var result = new WorkshopSubscriptionCleanupResult(
                IsSubscribed(beforeState) ? "unsubscribed-and-verified" : "already-unsubscribed",
                profile.PackageId,
                profile.PublishedFileId,
                beforeState,
                afterState,
                IsSubscribed(beforeState),
                IsSubscribed(afterState),
                evidencePath,
                true);
            DurableFile.WriteAllText(
                evidencePath,
                JsonSerializer.Serialize(result, McpJsonContext.Default.WorkshopSubscriptionCleanupResult) + Environment.NewLine);
            return result;
        }
        finally
        {
            if (!stopped)
            {
                try { _ = await manager.CancelAsync(runId, CancellationToken.None); }
                catch { /* the exact run lease retains cleanup diagnostics */ }
            }
        }
    }

    internal static bool IsSubscribed(uint state) => (state & SubscribedItemState) != 0;

    private static async Task<uint> WaitForUnsubscribedStateAsync(
        GatewayWorkshopClient client,
        string correlation,
        ulong publishedId,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        uint state;
        do
        {
            state = await ReadItemStateAsync(client, correlation, publishedId, cancellationToken);
            if (!IsSubscribed(state)) return state;
            await Task.Delay(500, cancellationToken);
        } while (DateTimeOffset.UtcNow < deadline);

        return state;
    }

    private static async Task<uint> ReadItemStateAsync(
        GatewayWorkshopClient client,
        string correlation,
        ulong publishedId,
        CancellationToken cancellationToken)
    {
        var state = await client.InvokeAsync(new Dictionary<string, object?>
        {
            ["operation"] = "item-state",
            ["planSha256"] = correlation,
            ["publishedFileId"] = publishedId.ToString()
        }, cancellationToken);
        if (GatewayWorkshopClient.String(state, "Status") != "item-state" ||
            GatewayWorkshopClient.UInt64(state, "PublishedFileId") != publishedId ||
            !uint.TryParse(GatewayWorkshopClient.String(state, "ItemState"), out var flags))
            throw new InvalidOperationException("Steam did not return one exact bounded item-state snapshot.");
        return flags;
    }

    private static async Task<RunStatusResult> WaitReadyAsync(
        RunLeaseManager manager,
        string runId,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        while (DateTimeOffset.UtcNow < deadline)
        {
            var status = manager.Status(runId);
            if (status.State == "ready") return status;
            if (status.State is "exited" or "pid-reused")
                throw new InvalidOperationException("Gateway subscription-cleanup run exited before readiness: " + status.StandardErrorTail);
            await Task.Delay(1000, cancellationToken);
        }
        throw new TimeoutException("Gateway subscription-cleanup run did not become ready.");
    }
}
