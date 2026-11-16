using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RimWorldModding.Mcp;

public sealed record ReleasePublishResult(
    string Status,
    string PackageId,
    string Title,
    string PublishedFileId,
    string WorkshopUrl,
    string PublicationPlanSha256,
    string CandidateDigest,
    string ReceiptPath,
    string ReceiptSha256,
    string IdentityCommit,
    uint? SteamModifiedBeforeUnixSeconds,
    uint SteamModifiedAfterUnixSeconds,
    bool SteamVerified);

public static class WorkshopRelationshipReconciler
{
    public static string[] Plan(string operationPrefix, IReadOnlyCollection<ulong> expected, IReadOnlyCollection<ulong> actual)
    {
        if (operationPrefix is not ("dependency" or "app-dependency"))
            throw new ArgumentException("Unknown Workshop relationship operation prefix.", nameof(operationPrefix));
        var desired = expected.ToHashSet();
        var observed = actual.ToHashSet();
        return observed.Except(desired).OrderBy(value => value)
            .Select(value => $"{operationPrefix}-remove:{value}")
            .Concat(desired.Except(observed).OrderBy(value => value)
                .Select(value => $"{operationPrefix}-add:{value}"))
            .ToArray();
    }
}

public sealed class ReleasePublisher(string repositoryRoot)
{
    internal static void PreservePreviousPackageHistory(string repositoryRoot, ReleaseProfile profile)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        var releaseRoot = Path.Combine(root, "artifacts", "Releases", profile.PackageId);
        var historyPath = Path.Combine(releaseRoot, "package-publication-state.txt");
        var statePath = Path.Combine(releaseRoot, "publication-state.txt");
        if (File.Exists(historyPath) || !File.Exists(statePath)) return;
        var state = File.ReadAllText(statePath).Trim().Split('|');
        if (state.Length != 3 || state[0] is not ("complete-steam-verified" or "complete-reviewed") ||
            state[2] != profile.PublishedFileId) return;
        var result = ReleaseWorkerCoordinator.ReadValidatedResult(root,
            Path.Combine(releaseRoot, "workers", state[1], "worker-result.json"), profile.PackageId, state[1], null, null,
            allowLegacySubscriberResult: state[0] == "complete-reviewed");
        using var document = JsonDocument.Parse(File.ReadAllText(result.ReceiptPath));
        if (GatewayWorkshopClient.String(document.RootElement, "publicationAction") == "page") return;
        if (!GatewayWorkshopClient.TryProperty(document.RootElement, "changeNoteVerification", out var verification) ||
            verification.ValueKind != JsonValueKind.Object) return;
        DurableFile.WriteAllText(historyPath, string.Join('|', state));
    }

    private readonly string _repositoryRoot = RepositoryRoot.Resolve(repositoryRoot);

    public async Task<ReleasePublishResult> PublishAsync(
        string planPath,
        string planSha256,
        string confirmationNonce,
        CancellationToken cancellationToken)
    {
        var admission = ReleasePlanAdmission.ValidateLocal(
            _repositoryRoot,
            planPath,
            planSha256,
            confirmationNonce,
            DateTimeOffset.UtcNow,
            requireCleanRevision: true);
        var artifactsPrefix = Path.Combine(_repositoryRoot, "artifacts", "Releases") + Path.DirectorySeparatorChar;
        if (!admission.PlanPath.StartsWith(artifactsPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Publication plan must be a retained release artifact.");
        var profile = ReleaseProfileCatalog.Discover(_repositoryRoot, validatePresentation: false)
            .Single(item => string.Equals(item.PackageId, admission.Plan.PackageId, StringComparison.OrdinalIgnoreCase));
        if (!string.Equals(profile.Path, admission.Plan.ReleaseProfilePath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Publication plan does not select the canonical universal release profile.");
        if (admission.Plan.PublicationAction != "package") _ = WorkshopLinkPolicy.ValidateSteamPlayerFacingSupport(admission.Plan.WorkshopLinks);
        if (admission.Plan.PublicationAction == "combined") ReleaseEnvironmentValidator.Validate(profile);
        if (admission.Plan.PublicationAction != "page")
        {
            ReleaseChangeNotePolicy.Validate(profile);
            ReleasePackageValidator.Validate(profile, admission.Candidate);
        }
        var previousReleaseEvidence = admission.Plan.PublicationAction != "page"
            ? await WorkshopChangeHistoryVerifier.ValidatePreviousAsync(_repositoryRoot, HistoryProfile(profile, admission.Plan), cancellationToken) : null;
        AssertPreviousReleaseEvidence(admission.Plan, previousReleaseEvidence);

        var mutexName = "Local\\RimWorldModdingMcp.Release." +
                        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(profile.PackageId)))[..24];
        using var mutex = new Semaphore(1, 1, mutexName);
        var acquired = mutex.WaitOne(0);
        if (!acquired) throw new InvalidOperationException("Another publication operation owns this package's release lease.");

        string? gatewayRunId = null;
        var gatewayStopped = false;
        try
        {
            var stateRoot = Path.Combine(_repositoryRoot, "artifacts", "Releases", profile.PackageId);
            Directory.CreateDirectory(stateRoot);
            var identityPath = Path.Combine(stateRoot, "PublishedFileId.txt");
            var statePath = Path.Combine(stateRoot, "publication-state.txt");
            var gatewayAssociationPath = Path.Combine(stateRoot, "publisher-gateway-run.txt");
            RefuseIndeterminateDuplicate(statePath, admission.PlanSha256, admission.Plan.PublishedFileId);
            if (admission.Plan.PublicationAction == "page") PreservePreviousPackageHistory(_repositoryRoot, profile);

            var manager = new RunLeaseManager(_repositoryRoot);
            gatewayRunId = RunLeaseManager.ReserveRunId();
            DurableFile.WriteAllText(gatewayAssociationPath, $"{admission.PlanSha256}|{gatewayRunId}");
            var start = manager.StartGateway(Array.Empty<string>(), Array.Empty<string>(), 3600, gatewayRunId);
            gatewayRunId = start.RunId;
            var ready = await WaitReadyAsync(manager, start.RunId, DateTimeOffset.UtcNow.AddMinutes(8), cancellationToken);
            var client = new GatewayWorkshopClient(
                _repositoryRoot,
                ready.GatewayManifestPath!,
                ready.GameProcessId!.Value);
            await client.RegisterAsync(cancellationToken);
            var status = await client.InvokeAsync(new Dictionary<string, object?> { ["operation"] = "status" }, cancellationToken);
            WorkshopRemoteBaseline.AssertRuntimeIdentity(status, profile, requireProductRuntime: admission.Plan.PublicationAction == "combined");

            ulong publishedId = admission.Plan.PublishedFileId is null
                ? 0UL
                : ulong.Parse(admission.Plan.PublishedFileId);
            var deadline = DateTimeOffset.UtcNow.AddMinutes(12);
            if (publishedId == 0)
            {
                _ = await client.InvokeAsync(new Dictionary<string, object?>
                {
                    ["operation"] = "owner-scan",
                    ["planSha256"] = admission.PlanSha256,
                    ["title"] = admission.Plan.Title
                }, cancellationToken);
                var ownerScan = await client.WaitTerminalAsync(
                    admission.PlanSha256,
                    new HashSet<string>(["owner-scan-complete", "owner-scan-found", "failed"], StringComparer.Ordinal),
                    deadline,
                    cancellationToken);
                if (GatewayWorkshopClient.String(ownerScan, "Status") != "owner-scan-complete")
                    throw new InvalidOperationException("The authenticated Steam owner scan did not prove exact-title absence.");
            }
            else
            {
                var queried = await QueryAsync(client, admission, publishedId, deadline, cancellationToken);
                var current = await WorkshopRemoteBaseline.CaptureAsync(queried, cancellationToken,
                    downloadImages: admission.Plan.PublicationAction == "combined");
                current.AssertItemIdentity(profile);
                admission.Plan.RemoteBaseline!.AssertMatches(current);
            }

            admission = ReleasePlanAdmission.ValidateLocal(
                _repositoryRoot,
                planPath,
                planSha256,
                confirmationNonce,
                DateTimeOffset.UtcNow,
                requireCleanRevision: true);
            profile = admission.Plan.FrozenProfile;
            if (admission.Plan.PublicationAction == "combined") ReleaseEnvironmentValidator.Validate(profile);
            if (admission.Plan.PublicationAction != "page")
            {
                ReleaseChangeNotePolicy.Validate(profile);
                ReleasePackageValidator.Validate(profile, admission.Candidate);
            }
            previousReleaseEvidence = admission.Plan.PublicationAction != "page"
                ? await WorkshopChangeHistoryVerifier.ValidatePreviousAsync(_repositoryRoot, HistoryProfile(profile, admission.Plan), cancellationToken) : null;
            AssertPreviousReleaseEvidence(admission.Plan, previousReleaseEvidence);
            if (admission.Plan.PublicationAction == "page")
            {
                using var http = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false });
                await WorkshopInlineImages.ValidateAsync(_repositoryRoot, admission.Plan.DescriptionPath,
                    admission.Plan.InlineImageManifestPath, http, cancellationToken, admission.Plan.InlineImageRootPath);
            }
            cancellationToken.ThrowIfCancellationRequested();
            var durableToken = CancellationToken.None;
            deadline = DateTimeOffset.UtcNow.AddMinutes(15);
            var repositoryIdentityPath = Path.Combine(Path.GetDirectoryName(profile.Project)!, "About", "PublishedFileId.txt");
            var packageIdentityPath = admission.Plan.PublicationAction == "page" ? repositoryIdentityPath
                : Path.Combine(admission.Plan.PackagePath, "About", "PublishedFileId.txt");
            if (publishedId != 0)
                EnsureExistingPublicationIdentity(
                    identityPath, repositoryIdentityPath, packageIdentityPath, publishedId);
            var publishArguments = BuildPublishArguments(admission, publishedId, identityPath,
                repositoryIdentityPath, packageIdentityPath, statePath);
            _ = await client.InvokeAsync(publishArguments, durableToken);
            var publish = await client.WaitTerminalAsync(
                admission.PlanSha256,
                new HashSet<string>(["succeeded", "failed", "legal-agreement-required"], StringComparer.Ordinal),
                deadline,
                durableToken);
            if (GatewayWorkshopClient.String(publish, "Status") != "succeeded")
                throw new InvalidOperationException("Steam publication did not reach a successful terminal callback: " + publish);
            publishedId = GatewayWorkshopClient.UInt64(publish, "PublishedFileId");
            if (publishedId == 0) throw new InvalidOperationException("Steam publication succeeded without an item ID.");
            var result = await CompleteAfterSteamAsync(
                manager, start.RunId, client, admission, profile, publishedId, stateRoot, statePath,
                allowUnsupportedWorkshopLinkRecovery: false, durableToken);
            gatewayStopped = true;
            return result;
        }
        finally
        {
            if (gatewayRunId is not null && !gatewayStopped && !ShouldPreserveGatewayForRecovery(
                    Path.Combine(_repositoryRoot, "artifacts", "Releases", admission.Plan.PackageId, "publication-state.txt"),
                    admission.PlanSha256))
            {
                try { _ = await new RunLeaseManager(_repositoryRoot).CancelAsync(gatewayRunId, CancellationToken.None); }
                catch { /* durable run evidence retains cleanup failure for recovery */ }
            }
            if (acquired) mutex.Release();
        }
    }

    public async Task<ReleasePublishResult> ResumeAsync(
        string planPath,
        string planSha256,
        string confirmationNonce,
        CancellationToken cancellationToken)
    {
        var admission = ReleasePlanAdmission.ValidateRecovery(
            _repositoryRoot, planPath, planSha256, confirmationNonce);
        var profile = admission.Plan.FrozenProfile;
        if (admission.Plan.PublicationAction == "combined") ReleaseEnvironmentValidator.Validate(profile);
        var mutexName = "Local\\RimWorldModdingMcp.Release." +
                        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(profile.PackageId)))[..24];
        using var mutex = new Semaphore(1, 1, mutexName);
        if (!mutex.WaitOne(0))
            throw new InvalidOperationException("Another publication operation owns this package's release lease.");

        var stateRoot = Path.Combine(_repositoryRoot, "artifacts", "Releases", profile.PackageId);
        var statePath = Path.Combine(stateRoot, "publication-state.txt");
        var associationPath = Path.Combine(stateRoot, "publisher-gateway-run.txt");
        if (!ReleasePlanAdmission.IsRecoverableDurableState(
                File.ReadAllText(statePath), admission.PlanSha256, out var durableIdText))
            throw new InvalidOperationException("The publication is not in a recoverable exact-plan state.");
        if (durableIdText is null)
        {
            durableIdText = ReleasePlanAdmission.TryResolveConsistentIdentity(_repositoryRoot, admission.Plan);
            if (durableIdText is not null)
                DurableFile.WriteAllText(statePath, $"created|{admission.PlanSha256}|{durableIdText}");
        }
        var publishedId = durableIdText is null ? 0UL : ulong.Parse(durableIdText);
        var manager = new RunLeaseManager(_repositoryRoot);
        string? gatewayRunId = null;
        var gatewayStopped = false;
        try
        {
            var attached = TryAttachGateway(manager, associationPath, admission.PlanSha256, out gatewayRunId);
            if (attached is not null)
            {
                var live = await attached.InvokeAsync(
                    new Dictionary<string, object?> { ["operation"] = "status" }, cancellationToken);
                WorkshopRemoteBaseline.AssertRuntimeIdentity(live, profile, requireProductRuntime: admission.Plan.PublicationAction == "combined");
                if (string.Equals(GatewayWorkshopClient.String(live, "PlanSha256"), admission.PlanSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    var liveStatus = GatewayWorkshopClient.String(live, "Status");
                    if (liveStatus != "succeeded" && liveStatus is not ("failed" or "legal-agreement-required"))
                        live = await attached.WaitTerminalAsync(admission.PlanSha256,
                            new HashSet<string>(["succeeded", "failed", "legal-agreement-required"], StringComparer.Ordinal),
                            DateTimeOffset.UtcNow.AddMinutes(15), CancellationToken.None);
                    if (GatewayWorkshopClient.String(live, "Status") == "succeeded")
                    {
                        publishedId = GatewayWorkshopClient.UInt64(live, "PublishedFileId");
                        var result = await CompleteAfterSteamAsync(manager, gatewayRunId!, attached, admission,
                            profile, publishedId, stateRoot, statePath,
                            allowUnsupportedWorkshopLinkRecovery: true, CancellationToken.None);
                        gatewayStopped = true;
                        return result;
                    }
                }
            }

            if (publishedId == 0)
                throw new InvalidOperationException(
                    "The exact first-publication callback is no longer reachable and no durable item ID exists; refusing a duplicate create call.");

            if (gatewayRunId is not null)
            {
                _ = await manager.CancelAsync(gatewayRunId, CancellationToken.None);
                gatewayStopped = true;
            }
            gatewayRunId = RunLeaseManager.ReserveRunId();
            DurableFile.WriteAllText(associationPath, $"{admission.PlanSha256}|{gatewayRunId}");
            var start = manager.StartGateway(Array.Empty<string>(), Array.Empty<string>(), 3600, gatewayRunId);
            gatewayRunId = start.RunId;
            gatewayStopped = false;
            var ready = await WaitReadyAsync(manager, start.RunId, DateTimeOffset.UtcNow.AddMinutes(8), cancellationToken);
            var client = new GatewayWorkshopClient(_repositoryRoot, ready.GatewayManifestPath!, ready.GameProcessId!.Value);
            await client.RegisterAsync(cancellationToken);
            var runtime = await client.InvokeAsync(new Dictionary<string, object?> { ["operation"] = "status" }, cancellationToken);
            WorkshopRemoteBaseline.AssertRuntimeIdentity(runtime, profile, requireProductRuntime: admission.Plan.PublicationAction == "combined");
            var deadline = DateTimeOffset.UtcNow.AddMinutes(15);
            var remote = await QueryAsync(client, admission, publishedId, deadline, cancellationToken);
            RequireRecoveryItemIdentity(remote, admission, publishedId);
            if (!RemoteMatchesPlan(remote, admission, allowUnsupportedWorkshopLinkRecovery: true))
                _ = await WaitRemoteBaseAsync(client, admission, publishedId, deadline,
                    allowUnsupportedWorkshopLinkRecovery: true, CancellationToken.None);
            var recovered = await CompleteAfterSteamAsync(
                manager, start.RunId, client, admission, profile, publishedId, stateRoot, statePath,
                allowUnsupportedWorkshopLinkRecovery: true, CancellationToken.None);
            gatewayStopped = true;
            return recovered;
        }
        finally
        {
            if (gatewayRunId is not null && !gatewayStopped && !ShouldPreserveGatewayForRecovery(statePath, admission.PlanSha256))
            {
                try { _ = await manager.CancelAsync(gatewayRunId, CancellationToken.None); }
                catch { /* exact run lease retains cleanup evidence */ }
            }
            mutex.Release();
        }
    }

    private async Task<ReleasePublishResult> CompleteAfterSteamAsync(
        RunLeaseManager manager,
        string gatewayRunId,
        GatewayWorkshopClient client,
        ReleaseAdmission admission,
        ReleaseProfile profile,
        ulong publishedId,
        string stateRoot,
        string statePath,
        bool allowUnsupportedWorkshopLinkRecovery,
        CancellationToken cancellationToken)
    {
        if (publishedId == 0) throw new InvalidOperationException("Steam completion requires a durable item ID.");
        var identityPath = Path.Combine(stateRoot, "PublishedFileId.txt");
        var repositoryIdentityPath = Path.Combine(Path.GetDirectoryName(profile.Project)!, "About", "PublishedFileId.txt");
        var packageIdentityPath = admission.Plan.PublicationAction == "page" ? repositoryIdentityPath
            : Path.Combine(admission.Plan.PackagePath, "About", "PublishedFileId.txt");
        AssertIdentity(identityPath, publishedId);
        AssertIdentity(repositoryIdentityPath, publishedId);
        AssertIdentity(packageIdentityPath, publishedId);
        var firstPublication = admission.Plan.PublishedFileId is null;
        var identityAlreadyCommitted = firstPublication &&
                                       CanonicalProfileMatchesProjectedIdentity(admission.Plan, publishedId) &&
                                       await IsWorktreeCleanAsync(cancellationToken);
        var commit = IdentityPersistenceMode(admission.Plan, identityAlreadyCommitted) switch
        {
            "persist-first-identity" =>
                await PersistIdentityAsync(admission, repositoryIdentityPath, publishedId, cancellationToken),
            "reuse-first-identity-commit" =>
                await RequireRecoveredFirstIdentityRevisionAsync(
                    admission, repositoryIdentityPath, publishedId, cancellationToken),
            _ => await RequireExistingIdentityRevisionAsync(
                admission, repositoryIdentityPath, publishedId, cancellationToken)
        };
        DurableFile.WriteAllText(statePath, $"steam-item-persisted|{admission.PlanSha256}|{publishedId}");

        var deadline = DateTimeOffset.UtcNow.AddMinutes(15);
        var remote = await WaitRemoteBaseAsync(client, admission, publishedId, deadline,
            allowUnsupportedWorkshopLinkRecovery, cancellationToken);
        if (admission.Plan.PublicationAction != "package")
            remote = await ReconcileRelationshipsAsync(client, admission, publishedId, remote, statePath, deadline, cancellationToken);
        remote = await WaitRemoteBaseAsync(client, admission, publishedId, deadline,
            allowUnsupportedWorkshopLinkRecovery, cancellationToken, requireRelationships: true);
        if (admission.Plan.PublicationAction != "package")
            await VerifyRemotePreviewAsync(remote, admission.Plan.PreviewSha256, cancellationToken);
        object? changeNoteVerification = admission.Plan.PublicationAction != "page"
            ? await WorkshopChangeHistoryVerifier.VerifyPublishedAsync(HistoryProfile(profile, admission.Plan), publishedId.ToString(), cancellationToken) : null;

        await VerifyActionResultAsync(remote, admission, cancellationToken);

        if (admission.Plan.PublicationAction != "page") VerifyExactPublishedCandidate(admission, publishedId);
        var modifiedBefore = admission.Plan.RemoteBaseline?.UpdatedUnixSeconds;
        var modifiedAfter = checked((uint)GatewayWorkshopClient.UInt64(remote, "RemoteUpdatedUnixSeconds"));
        if (!RemoteModifiedTimeAdvanced(remote, admission.Plan.RemoteBaseline))
            throw new InvalidOperationException("Steam did not expose a newer modified time for the exact published item.");
        DurableFile.WriteAllText(statePath, $"steam-verified|{admission.PlanSha256}|{publishedId}");

        _ = await manager.CancelAsync(gatewayRunId, cancellationToken);
        var receiptRoot = Path.Combine(stateRoot, "publication", $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}");
        Directory.CreateDirectory(receiptRoot);
        var receiptPath = Path.Combine(receiptRoot, "publication-receipt.json");
        var unsupportedWorkshopLinks = allowUnsupportedWorkshopLinkRecovery
            ? admission.Plan.WorkshopLinks.Where(link => !WorkshopLinkPolicy.IsSteamPlayerFacingKey(link.Key)).ToArray()
            : [];
        var observedRemoteLinks = WorkshopRemoteBaseline.ReadWorkshopLinks(remote);
        var receipt = new Dictionary<string, object?>
        {
            ["schema"] = "RimWorldModReleaseReceipt/v1",
            ["publishedUtc"] = DateTimeOffset.UtcNow,
            ["packageId"] = profile.PackageId,
            ["publicationAction"] = admission.Plan.PublicationAction,
            ["title"] = profile.Title,
            ["publishedFileId"] = publishedId.ToString(),
            ["workshopUrl"] = $"https://steamcommunity.com/sharedfiles/filedetails/?id={publishedId}",
            ["publicationPlanSha256"] = admission.PlanSha256,
            ["candidateDigest"] = admission.Plan.CandidateDigest,
            ["remote"] = JsonNode.Parse(remote.GetRawText()),
            ["steamModifiedTime"] = new
            {
                BeforeUnixSeconds = modifiedBefore,
                AfterUnixSeconds = modifiedAfter,
                Advanced = modifiedBefore.HasValue ? modifiedAfter > modifiedBefore.Value : modifiedAfter > 0
            },
            ["changeNote"] = admission.Plan.PublicationAction == "page" ? "" : admission.Plan.ChangeNote,
            ["changeNoteVerification"] = changeNoteVerification,
            ["identityCommit"] = commit,
            ["unsupportedWorkshopLinks"] = unsupportedWorkshopLinks.Select(link => new
            {
                link.Key,
                link.Url,
                Status = observedRemoteLinks.Any(candidate =>
                    candidate.Key.Equals(link.Key, StringComparison.OrdinalIgnoreCase) &&
                    candidate.Url.Equals(link.Url, StringComparison.Ordinal))
                    ? "stored-as-generic-key-value-not-player-facing"
                    : "not-stored-platform-does-not-expose-player-facing-custom-link"
            }).ToArray(),
            ["tokenRetained"] = false
        };
        DurableFile.WriteAllText(receiptPath,
            JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
        var receiptSha256 = ReleaseCandidateBuilder.Hash(receiptPath);
        if (admission.Plan.PublicationAction != "page")
            DurableFile.WriteAllText(Path.Combine(stateRoot, "package-publication-state.txt"),
                $"complete-steam-verified|{admission.PlanSha256}|{publishedId}");
        DurableFile.WriteAllText(statePath, $"complete-steam-verified|{admission.PlanSha256}|{publishedId}");
        return new ReleasePublishResult(
            "published-complete-steam-verified",
            profile.PackageId,
            profile.Title,
            publishedId.ToString(),
            $"https://steamcommunity.com/sharedfiles/filedetails/?id={publishedId}",
            admission.PlanSha256,
            admission.Plan.CandidateDigest,
            receiptPath,
            receiptSha256,
            commit,
            modifiedBefore,
            modifiedAfter,
            true);
    }

    private GatewayWorkshopClient? TryAttachGateway(
        RunLeaseManager manager,
        string associationPath,
        string planSha256,
        out string? runId)
    {
        runId = null;
        if (!File.Exists(associationPath)) return null;
        var parts = File.ReadAllText(associationPath).Trim().Split('|');
        if (parts.Length != 2 || !string.Equals(parts[0], planSha256, StringComparison.OrdinalIgnoreCase)) return null;
        try
        {
            var status = manager.Status(parts[1]);
            if (status.State is not ("ready" or "orphaned-game") || status.GatewayManifestPath is null ||
                status.GameProcessId is null) return null;
            runId = parts[1];
            return new GatewayWorkshopClient(_repositoryRoot, status.GatewayManifestPath, status.GameProcessId.Value);
        }
        catch
        {
            return null;
        }
    }

    private static void RequireRecoveryItemIdentity(JsonElement remote, ReleaseAdmission admission, ulong publishedId)
    {
        if (GatewayWorkshopClient.UInt64(remote, "PublishedFileId") != publishedId ||
            GatewayWorkshopClient.UInt64(remote, "RemoteOwnerSteamId").ToString() != admission.Plan.SteamUserId ||
            GatewayWorkshopClient.UInt64(remote, "RemoteConsumerAppId") != (ulong)admission.Plan.SteamAppId)
            throw new InvalidOperationException("Recovery query did not prove the exact Workshop item/owner/app identity.");
    }

    private static bool RemoteMatchesPlan(
        JsonElement remote,
        ReleaseAdmission admission,
        bool allowUnsupportedWorkshopLinkRecovery = false)
    {
        if (admission.Plan.PublicationAction == "package")
        {
            var baseline = admission.Plan.RemoteBaseline;
            return RemoteModifiedTimeAdvanced(remote, baseline) &&
                   GatewayWorkshopClient.String(remote, "RemoteMetadata") == admission.PlanSha256 &&
                   GatewayWorkshopClient.String(remote, "RemoteTitle") == (baseline?.Title ?? admission.Plan.Title) &&
                   GatewayWorkshopClient.String(remote, "RemoteDescriptionSha256") ==
                       (baseline?.DescriptionSha256 ?? Convert.ToHexString(SHA256.HashData([]))) &&
                   GatewayWorkshopClient.String(remote, "RemoteVisibility").EndsWith(baseline?.Visibility ?? "Private", StringComparison.Ordinal) &&
                   GatewayWorkshopClient.String(remote, "RemoteTags").Split(',').Select(value => value.Trim()).Where(value => value.Length > 0)
                       .OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(baseline?.Tags ?? [], StringComparer.Ordinal);
        }
        var tags = GatewayWorkshopClient.String(remote, "RemoteTags").Split(',')
            .Select(value => value.Trim()).Where(value => value.Length > 0).OrderBy(value => value, StringComparer.Ordinal);
        return RemoteModifiedTimeAdvanced(remote, admission.Plan.RemoteBaseline) &&
               GatewayWorkshopClient.String(remote, "RemoteTitle") == admission.Plan.Title &&
               GatewayWorkshopClient.String(remote, "RemoteDescriptionSha256") == admission.Plan.DescriptionSha256 &&
               GatewayWorkshopClient.String(remote, "RemoteMetadata") == admission.PlanSha256 &&
               GatewayWorkshopClient.String(remote, "RemoteVisibility").EndsWith(admission.Plan.Visibility, StringComparison.Ordinal) &&
               (allowUnsupportedWorkshopLinkRecovery
                   ? RemoteLinksMatchRecovery(remote, admission.Plan.WorkshopLinks)
                   : RemoteLinksMatchPlan(remote, admission.Plan.WorkshopLinks)) &&
               tags.SequenceEqual(admission.Plan.Tags.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal);
    }

    private static bool ShouldPreserveGatewayForRecovery(string statePath, string planSha256) =>
        File.Exists(statePath) &&
        ReleasePlanAdmission.IsRecoverableDurableState(File.ReadAllText(statePath), planSha256, out _);

    internal static Dictionary<string, object?> BuildPublishArguments(
        ReleaseAdmission admission, ulong publishedId, string identityPath, string repositoryIdentityPath,
        string packageIdentityPath, string statePath)
    {
        var plan = admission.Plan;
        var request = new Dictionary<string, object?>
        {
            ["operation"] = "publish",
            ["publicationAction"] = plan.PublicationAction,
            ["planSha256"] = admission.PlanSha256,
            ["confirmationNonce"] = plan.ConfirmationNonce,
            ["confirmation"] = ReleasePlanAdmission.AdmissionText(admission),
            ["publishedFileId"] = publishedId == 0 ? "" : publishedId.ToString(),
            ["allowFirstPublication"] = publishedId == 0 && plan.AllowFirstPublication,
            ["identityPath"] = identityPath,
            ["repositoryIdentityPath"] = repositoryIdentityPath,
            ["packageIdentityPath"] = packageIdentityPath,
            ["statePath"] = statePath,
            ["title"] = plan.Title,
            ["changeNote"] = plan.PublicationAction == "page" ? "" : plan.ChangeNote
        };
        if (plan.PublicationAction != "page") request["packagePath"] = plan.PackagePath;
        if (plan.PublicationAction != "package")
        {
            request["descriptionPath"] = plan.DescriptionPath;
            request["previewPath"] = plan.PreviewPath;
            request["tags"] = plan.Tags;
            request["visibility"] = plan.Visibility;
            foreach (var pair in WorkshopLinkGatewayArguments(plan.WorkshopLinks)) request.Add(pair.Key, pair.Value);
            if (plan.PublicationAction == "page")
                request["additionalPreviewPaths"] = plan.AdditionalPreviews.Select(file => file.Path).ToArray();
        }
        return request;
    }

    internal static Dictionary<string, object?> WorkshopLinkGatewayArguments(IReadOnlyList<WorkshopLink> links)
    {
        var validated = WorkshopLinkPolicy.Validate(links);
        return new Dictionary<string, object?>
        {
            ["workshopLinkJson"] = validated.Select(link => JsonSerializer.Serialize(link)).ToArray()
        };
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
                throw new InvalidOperationException("Gateway publisher run exited before readiness: " + status.StandardErrorTail);
            await Task.Delay(1000, cancellationToken);
        }
        throw new TimeoutException("Gateway publisher run did not become ready.");
    }

    private static async Task<JsonElement> QueryAsync(
        GatewayWorkshopClient client,
        ReleaseAdmission admission,
        ulong publishedId,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        _ = await client.InvokeAsync(new Dictionary<string, object?>
        {
            ["operation"] = "query",
            ["planSha256"] = admission.PlanSha256,
            ["publishedFileId"] = publishedId.ToString()
        }, cancellationToken);
        var query = await client.WaitTerminalAsync(
            admission.PlanSha256,
            new HashSet<string>(["queried", "failed"], StringComparer.Ordinal),
            deadline,
            cancellationToken);
        if (GatewayWorkshopClient.String(query, "Status") != "queried")
            throw new InvalidOperationException("Steam item query failed.");
        return query;
    }

    private static async Task<JsonElement> WaitRemoteBaseAsync(
        GatewayWorkshopClient client,
        ReleaseAdmission admission,
        ulong publishedId,
        DateTimeOffset deadline,
        bool allowUnsupportedWorkshopLinkRecovery,
        CancellationToken cancellationToken,
        bool requireRelationships = false)
    {
        JsonElement remote = default;
        while (DateTimeOffset.UtcNow < deadline)
        {
            remote = await QueryAsync(client, admission, publishedId, deadline, cancellationToken);
            if (RemoteMatchesPlan(remote, admission, allowUnsupportedWorkshopLinkRecovery) &&
                GatewayWorkshopClient.UInt64(remote, "RemoteOwnerSteamId").ToString() == admission.Plan.SteamUserId &&
                GatewayWorkshopClient.UInt64(remote, "RemoteConsumerAppId") == (ulong)admission.Plan.SteamAppId &&
                (!requireRelationships || admission.Plan.PublicationAction == "package" ||
                 Dependencies(remote).SetEquals(admission.Plan.RequiredWorkshopItems.Select(ulong.Parse)) &&
                 AppDependencies(remote).SetEquals((admission.Plan.RequiredDlcAppIds ?? []).Select(ulong.Parse))))
                return remote;
            await Task.Delay(1500, cancellationToken);
        }
        throw new TimeoutException("Remote Workshop metadata did not converge to the admitted plan.");
    }

    private static async Task VerifyActionResultAsync(JsonElement remote, ReleaseAdmission admission, CancellationToken cancellationToken)
    {
        var plan = admission.Plan;
        if (plan.PublicationAction == "combined") return;
        var actual = await WorkshopRemoteBaseline.CaptureAsync(remote, cancellationToken,
            downloadImages: plan.PublicationAction == "page");
        var baseline = plan.RemoteBaseline;
        if (plan.PublicationAction == "package")
        {
            if (baseline is not null &&
                (actual.Title != baseline.Title || actual.DescriptionSha256 != baseline.DescriptionSha256 ||
                 actual.Visibility != baseline.Visibility || actual.PreviewUrl != baseline.PreviewUrl ||
                 !actual.Tags.SequenceEqual(baseline.Tags) || !actual.Links.SequenceEqual(baseline.Links) ||
                 !actual.Dependencies.SequenceEqual(baseline.Dependencies) || !actual.AppDependencies.SequenceEqual(baseline.AppDependencies) ||
                 !actual.AdditionalPreviews.SequenceEqual(baseline.AdditionalPreviews)))
                throw new InvalidOperationException("Package upload changed page presentation or relationships.");
            return;
        }
        if (baseline is null || actual.ContentBytes != baseline.ContentBytes || actual.ContentHandle != baseline.ContentHandle)
            throw new InvalidOperationException("Page update changed package content identity or size.");
        if (actual.AdditionalPreviews.Count != plan.AdditionalPreviews.Count)
            throw new InvalidOperationException("Slideshow count differs from the admitted page plan.");
        for (var index = 0; index < plan.AdditionalPreviews.Count; index++)
        {
            var parts = actual.AdditionalPreviews[index].Split('|');
            if (parts.Length != 4 || parts[0] != index.ToString() || parts[3] != plan.AdditionalPreviews[index].Sha256)
                throw new InvalidOperationException($"Slideshow bytes differ from the admitted page plan at slot {index}.");
        }
    }

    internal static bool RemoteModifiedTimeAdvanced(
        JsonElement remote,
        WorkshopRemoteBaseline? baseline)
    {
        var observed = GatewayWorkshopClient.UInt64(remote, "RemoteUpdatedUnixSeconds");
        return observed is > 0 and <= uint.MaxValue &&
               (baseline is null || observed > baseline.UpdatedUnixSeconds);
    }

    internal static bool RemoteLinksMatchPlan(JsonElement remote, IReadOnlyList<WorkshopLink> expected)
    {
        var actual = WorkshopRemoteBaseline.ReadWorkshopLinks(remote);
        return expected.All(link =>
        {
            var sameKey = actual.Where(candidate =>
                candidate.Key.Equals(link.Key, StringComparison.OrdinalIgnoreCase)).ToArray();
            return sameKey.Length == 1 && sameKey[0].Url.Equals(link.Url, StringComparison.Ordinal);
        });
    }

    internal static bool RemoteLinksMatchRecovery(JsonElement remote, IReadOnlyList<WorkshopLink> expected)
    {
        var actual = WorkshopRemoteBaseline.ReadWorkshopLinks(remote);
        return expected.All(link =>
        {
            var sameKey = actual.Where(candidate =>
                candidate.Key.Equals(link.Key, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (!WorkshopLinkPolicy.IsSteamPlayerFacingKey(link.Key))
                return sameKey.Length == 0 ||
                       sameKey.Length == 1 && sameKey[0].Url.Equals(link.Url, StringComparison.Ordinal);
            return sameKey.Length == 1 && sameKey[0].Url.Equals(link.Url, StringComparison.Ordinal);
        });
    }

    private static async Task<JsonElement> ReconcileRelationshipsAsync(
        GatewayWorkshopClient client,
        ReleaseAdmission admission,
        ulong publishedId,
        JsonElement remote,
        string statePath,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        var expectedWorkshopItems = admission.Plan.RequiredWorkshopItems.Select(ulong.Parse).ToHashSet();
        var expectedApplications = (admission.Plan.RequiredDlcAppIds ?? []).Select(ulong.Parse).ToHashSet();
        var operations = WorkshopRelationshipReconciler.Plan(
                "dependency", expectedWorkshopItems, Dependencies(remote))
            .Concat(WorkshopRelationshipReconciler.Plan(
                "app-dependency", expectedApplications, AppDependencies(remote)));
        foreach (var planned in operations)
        {
            var separator = planned.LastIndexOf(':');
            var operation = planned[..separator];
            var id = ulong.Parse(planned[(separator + 1)..]);
            _ = await QueryAsync(client, admission, publishedId, deadline, cancellationToken);
            var arguments = new Dictionary<string, object?>
            {
                ["operation"] = operation,
                ["planSha256"] = admission.PlanSha256,
                ["publishedFileId"] = publishedId.ToString(),
                ["title"] = admission.Plan.Title,
                ["statePath"] = statePath
            };
            arguments[operation.StartsWith("app-dependency-", StringComparison.Ordinal)
                ? "requiredDlcAppId"
                : "requiredWorkshopItemId"] = id.ToString();
            _ = await client.InvokeAsync(arguments, cancellationToken);
            var terminal = await client.WaitTerminalAsync(
                admission.PlanSha256,
                new HashSet<string>(["succeeded", "failed"], StringComparer.Ordinal),
                deadline,
                cancellationToken);
            if (GatewayWorkshopClient.String(terminal, "Status") != "succeeded")
                throw new InvalidOperationException($"Workshop {operation} failed for {id}.");
        }
        while (DateTimeOffset.UtcNow < deadline)
        {
            remote = await QueryAsync(client, admission, publishedId, deadline, cancellationToken);
            if (Dependencies(remote).SetEquals(expectedWorkshopItems) &&
                AppDependencies(remote).SetEquals(expectedApplications)) return remote;
            await Task.Delay(1000, cancellationToken);
        }
        throw new TimeoutException("Remote Workshop item/application dependency graphs did not converge.");
    }

    private static HashSet<ulong> Dependencies(JsonElement remote)
    {
        if (!GatewayWorkshopClient.TryProperty(remote, "RemoteDependencies", out var property) || property.ValueKind != JsonValueKind.Array)
            return [];
        return property.EnumerateArray().Select(value => value.GetUInt64()).ToHashSet();
    }

    internal static void AssertPreviousReleaseEvidence(
        ReleasePublicationPlan plan,
        RetainedPrivateReleaseEvidence? actual)
    {
        if (plan.PublicationAction == "page") return;
        var expected = plan.PreviousPrivateReleaseEvidence;
        if (expected is null)
        {
            if (actual is not null || plan.PublishedFileId is not null && plan.Visibility == "Private")
                throw new InvalidOperationException("Private update evidence is not bound into the exact publication plan.");
            return;
        }
        if (actual is null)
            throw new InvalidOperationException("The exact preceding Private release evidence is no longer available.");
        WorkshopChangeHistoryVerifier.AssertMatches(expected, actual);
    }

    private static ReleaseProfile HistoryProfile(ReleaseProfile profile, ReleasePublicationPlan plan) => profile with
    {
        Visibility = WorkshopChangeHistoryVerifier.EffectiveVisibility(plan.PublicationAction, profile.Visibility, plan.RemoteBaseline?.Visibility)
    };

    private static HashSet<ulong> AppDependencies(JsonElement remote)
    {
        if (!GatewayWorkshopClient.TryProperty(remote, "RemoteAppDependencies", out var property) ||
            property.ValueKind != JsonValueKind.Array)
            return [];
        return property.EnumerateArray().Select(value => value.GetUInt64()).ToHashSet();
    }

    private static async Task VerifyRemotePreviewAsync(JsonElement remote, string expectedSha256, CancellationToken cancellationToken)
    {
        var url = GatewayWorkshopClient.String(remote, "RemotePreviewUrl");
        using var http = new HttpClient(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All });
        http.DefaultRequestHeaders.UserAgent.ParseAdd("RimWorldModding.Mcp/0.1");
        var bytes = await http.GetByteArrayAsync(url, cancellationToken);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        if (!string.Equals(hash, expectedSha256, StringComparison.Ordinal))
            throw new InvalidOperationException("Steam serves preview bytes different from the reviewed image.");
    }

    private static void VerifyExactPublishedCandidate(
        ReleaseAdmission admission,
        ulong publishedId)
    {
        var expectedPaths = admission.Plan.Files.Select(file => file.Path)
            .Append("About/PublishedFileId.txt").ToHashSet(StringComparer.Ordinal);
        var actualPaths = Directory.GetFiles(admission.Plan.PackagePath, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(admission.Plan.PackagePath, path).Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);
        if (!actualPaths.SetEquals(expectedPaths))
            throw new InvalidOperationException("Published candidate inventory changed after Steam admission.");
        foreach (var file in admission.Plan.Files)
        {
            var path = Path.Combine(admission.Plan.PackagePath, file.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path) || new FileInfo(path).Length != file.Bytes ||
                !string.Equals(ReleaseCandidateBuilder.Hash(path), file.Sha256, StringComparison.Ordinal))
                throw new InvalidOperationException("Published candidate bytes changed after Steam admission: " + file.Path);
        }
        AssertIdentity(Path.Combine(admission.Plan.PackagePath, "About", "PublishedFileId.txt"), publishedId);
    }

    private async Task<string> PersistIdentityAsync(
        ReleaseAdmission admission,
        string repositoryIdentityPath,
        ulong publishedId,
        CancellationToken cancellationToken)
    {
        AssertIdentity(repositoryIdentityPath, publishedId);
        var profile = admission.Plan.FrozenProfile;
        var canonicalProfilePath = admission.Plan.ReleaseProfilePath;
        var projected = ProjectFrozenProfileIdentity(admission.Plan, publishedId);
        var currentHash = File.Exists(canonicalProfilePath) ? ReleaseCandidateBuilder.Hash(canonicalProfilePath) : "";
        if (!string.Equals(currentHash, admission.Plan.ReleaseProfileSha256, StringComparison.OrdinalIgnoreCase) &&
            !CanonicalProfileMatchesProjectedIdentity(admission.Plan, publishedId))
            throw new InvalidOperationException(
                "Canonical release profile changed after Steam admission; refusing to overwrite mutable repository state.");
        DurableFile.WriteAllText(canonicalProfilePath, projected);

        var allowed = new[] { canonicalProfilePath, repositoryIdentityPath }
            .Select(path => Path.GetRelativePath(_repositoryRoot, path).Replace('\\', '/')).OrderBy(path => path, StringComparer.Ordinal).ToArray();
        var status = await ProcessRunner.RunAsync(
            "git", ["status", "--porcelain", "--untracked-files=all"], _repositoryRoot, TimeSpan.FromSeconds(30), cancellationToken);
        var changed = status.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Length > 3 ? line[3..].Replace('\\', '/') : "").OrderBy(path => path, StringComparer.Ordinal).ToArray();
        if (!changed.SequenceEqual(allowed, StringComparer.Ordinal))
            throw new InvalidOperationException("Identity persistence produced changes outside the two canonical identity files.");
        var add = await ProcessRunner.RunAsync("git", ["add", "--", .. allowed], _repositoryRoot, TimeSpan.FromSeconds(30), cancellationToken);
        if (add.ExitCode != 0) throw new InvalidOperationException("Could not stage Workshop identity files.");
        var commit = await ProcessRunner.RunAsync(
            "git", ["commit", "-m", $"Record Workshop ID for {profile.PackageId}"], _repositoryRoot, TimeSpan.FromMinutes(2), cancellationToken);
        if (commit.ExitCode != 0) throw new InvalidOperationException("Could not commit Workshop identity: " + commit.StandardError);
        var revision = await ProcessRunner.RunAsync("git", ["rev-parse", "HEAD"], _repositoryRoot, TimeSpan.FromSeconds(30), cancellationToken);
        return revision.StandardOutput.Trim();
    }

    private async Task<string> RequireExistingIdentityRevisionAsync(
        ReleaseAdmission admission,
        string repositoryIdentityPath,
        ulong publishedId,
        CancellationToken cancellationToken)
    {
        var profile = admission.Plan.FrozenProfile;
        if (!string.Equals(profile.PublishedFileId, publishedId.ToString(), StringComparison.Ordinal))
            throw new InvalidOperationException("Published Workshop ID differs from the canonical release profile.");
        AssertIdentity(repositoryIdentityPath, publishedId);
        await Task.CompletedTask;
        return admission.Plan.SourceRevision;
    }

    private async Task<string> RequireRecoveredFirstIdentityRevisionAsync(
        ReleaseAdmission admission,
        string repositoryIdentityPath,
        ulong publishedId,
        CancellationToken cancellationToken)
    {
        if (admission.Plan.PublishedFileId is not null ||
            !CanonicalProfileMatchesProjectedIdentity(admission.Plan, publishedId))
            throw new InvalidOperationException("Recovered first-publication profile does not contain the exact durable item ID.");
        AssertIdentity(repositoryIdentityPath, publishedId);
        var graph = await ProcessRunner.RunAsync(
            "git", ["rev-list", "--all", "--parents"], _repositoryRoot, TimeSpan.FromSeconds(30), cancellationToken);
        if (graph.ExitCode != 0)
            throw new InvalidOperationException("Could not inspect identity commit ancestry.");
        var profileRelative = Path.GetRelativePath(_repositoryRoot, admission.Plan.ReleaseProfilePath).Replace('\\', '/');
        var identityRelative = Path.GetRelativePath(_repositoryRoot, repositoryIdentityPath).Replace('\\', '/');
        var expectedPaths = new[] { identityRelative, profileRelative }.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var projected = NormalizeGitText(ProjectFrozenProfileIdentity(admission.Plan, publishedId));
        var matches = new List<string>();
        foreach (var candidate in DirectChildCommitCandidates(graph.StandardOutput, admission.Plan.SourceRevision))
        {
            var changed = await ProcessRunner.RunAsync(
                "git", ["diff-tree", "--no-commit-id", "--name-only", "-r", candidate], _repositoryRoot,
                TimeSpan.FromSeconds(30), cancellationToken);
            if (changed.ExitCode != 0) continue;
            var paths = changed.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Replace('\\', '/')).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (!paths.SequenceEqual(expectedPaths, StringComparer.Ordinal)) continue;
            var committedProfile = await ProcessRunner.RunAsync(
                "git", ["show", $"{candidate}:{profileRelative}"], _repositoryRoot,
                TimeSpan.FromSeconds(30), cancellationToken);
            var committedIdentity = await ProcessRunner.RunAsync(
                "git", ["show", $"{candidate}:{identityRelative}"], _repositoryRoot,
                TimeSpan.FromSeconds(30), cancellationToken);
            if (committedProfile.ExitCode == 0 && committedIdentity.ExitCode == 0 &&
                NormalizeGitText(committedProfile.StandardOutput) == projected &&
                committedIdentity.StandardOutput.Trim() == publishedId.ToString())
                matches.Add(candidate);
        }
        if (matches.Count != 1)
            throw new InvalidOperationException(
                "Could not resolve one exact identity-only commit directly descended from the admitted source revision.");
        return matches[0];
    }

    internal static IReadOnlyList<string> DirectChildCommitCandidates(string revisionGraph, string sourceRevision) =>
        revisionGraph.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length == 2 && string.Equals(parts[1], sourceRevision, StringComparison.OrdinalIgnoreCase))
            .Select(parts => parts[0])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string NormalizeGitText(string value) => value.Replace("\r\n", "\n").TrimEnd('\n') + "\n";

    internal static string IdentityPersistenceMode(
        ReleasePublicationPlan plan,
        bool projectedIdentityIsClean) =>
        plan.PublishedFileId is not null
            ? "existing-identity"
            : projectedIdentityIsClean
                ? "reuse-first-identity-commit"
                : "persist-first-identity";

    internal static string ProjectFrozenProfileIdentity(ReleasePublicationPlan plan, ulong publishedId)
    {
        var node = JsonNode.Parse(File.ReadAllText(plan.FrozenReleaseProfilePath))?.AsObject() ??
                   throw new InvalidOperationException("Frozen release profile is unreadable during identity persistence.");
        node["publishedFileId"] = publishedId.ToString();
        node["allowFirstPublication"] = false;
        node["previousChangeNote"] = plan.ChangeNote;
        node["changeNote"] = "";
        var includes = node["packageInclude"]?.AsArray() ?? throw new InvalidOperationException("packageInclude is missing.");
        if (!includes.Any(value => value?.GetValue<string>() == "About/PublishedFileId.txt"))
            includes.Add("About/PublishedFileId.txt");
        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine;
    }

    internal static bool CanonicalProfileMatchesProjectedIdentity(ReleasePublicationPlan plan, ulong publishedId) =>
        File.Exists(plan.ReleaseProfilePath) &&
        string.Equals(NormalizeGitText(File.ReadAllText(plan.ReleaseProfilePath)),
            NormalizeGitText(ProjectFrozenProfileIdentity(plan, publishedId)),
            StringComparison.Ordinal);

    private async Task<bool> IsWorktreeCleanAsync(CancellationToken cancellationToken)
    {
        var status = await ProcessRunner.RunAsync(
            "git", ["status", "--porcelain", "--untracked-files=all"], _repositoryRoot,
            TimeSpan.FromSeconds(30), cancellationToken);
        if (status.ExitCode != 0) throw new InvalidOperationException("Could not inspect identity persistence state.");
        return string.IsNullOrWhiteSpace(status.StandardOutput);
    }

    internal static void RefuseIndeterminateDuplicate(string statePath, string planSha256, string? publishedFileId)
    {
        if (!File.Exists(statePath)) return;
        var parts = File.ReadAllText(statePath).Trim().Split('|');
        if (parts.Length != 3) throw new InvalidOperationException("Durable publication state is malformed.");
        if (parts[1].Equals(planSha256, StringComparison.OrdinalIgnoreCase) &&
            ReleasePlanAdmission.IsRecoverableDurableState(string.Join('|', parts), planSha256, out _))
            throw new InvalidOperationException("This exact plan is already admitted; use durable recovery instead of redispatch.");
        if (parts[0] is "complete-reviewed" or "complete-steam-verified" or "create-failed-definite" or "submit-failed-definite" or "dependency-failed-definite")
            return;
        if (HasCompletedLegacyReceipt(statePath, planSha256, publishedFileId)) return;
        var operation = publishedFileId is null ? "creation" : "update";
        throw new InvalidOperationException(
            $"A previous Workshop {operation} is incomplete or indeterminate; refusing another mutation until its durable state is resolved.");
    }

    internal static bool HasCompletedLegacyReceipt(string statePath, string newPlanSha256, string? publishedFileId)
    {
        if (!File.Exists(statePath)) return false;
        var parts = File.ReadAllText(statePath).Trim().Split('|');
        if (parts.Length != 3 || parts[0] != "succeeded" || parts[1].Length != 64 || !parts[1].All(Uri.IsHexDigit) ||
            parts[1].Equals(newPlanSha256, StringComparison.OrdinalIgnoreCase) || parts[2] != publishedFileId) return false;
        var publicationRoot = Path.Combine(Path.GetDirectoryName(statePath)!, "publication");
        if (!Directory.Exists(publicationRoot)) return false;
        var directories = Directory.GetDirectories(publicationRoot).Take(1025).ToArray();
        if (directories.Length > 1024) throw new InvalidOperationException("Legacy receipt inventory exceeds the migration bound.");
        foreach (var directory in directories)
        {
            var receiptPath = Path.Combine(directory, "publication-receipt.json");
            if (!File.Exists(receiptPath) || new FileInfo(receiptPath).Length > 1024 * 1024) continue;
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(receiptPath));
                var receipt = document.RootElement;
                if (receipt.GetProperty("schema").GetString() != "ImmersiveChefs/WorkshopPublicationReceipt/v1" ||
                    receipt.GetProperty("publicationPlanSha256").GetString() != parts[1] ||
                    receipt.GetProperty("publishedFileId").GetString() != publishedFileId ||
                    receipt.GetProperty("tokenRetained").GetBoolean()) continue;
                var remote = receipt.GetProperty("remote");
                var smoke = receipt.GetProperty("subscribedSmoke");
                if (GatewayWorkshopClient.UInt64(remote, "PublishedFileId").ToString() == publishedFileId &&
                    GatewayWorkshopClient.String(remote, "RemoteMetadata") == parts[1] &&
                    smoke.GetProperty("status").GetString() == "passed" && smoke.GetProperty("localProductRestored").GetBoolean()) return true;
            }
            catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException) { }
        }
        return false;
    }

    private static void AssertIdentity(string path, ulong expected)
    {
        if (!File.Exists(path) || !ulong.TryParse(File.ReadAllText(path).Trim(), out var actual) || actual != expected)
            throw new InvalidOperationException("Workshop identity is missing or inconsistent: " + path);
    }

    internal static void EnsureExistingPublicationIdentity(
        string durableIdentityPath,
        string repositoryIdentityPath,
        string packageIdentityPath,
        ulong expected)
    {
        if (expected == 0) throw new ArgumentException("Existing Workshop identity must be nonzero.");
        AssertIdentity(repositoryIdentityPath, expected);
        AssertIdentity(packageIdentityPath, expected);
        if (File.Exists(durableIdentityPath))
        {
            AssertIdentity(durableIdentityPath, expected);
            return;
        }

        var exact = Path.GetFullPath(durableIdentityPath);
        Directory.CreateDirectory(Path.GetDirectoryName(exact)!);
        var temporary = exact + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(
                       temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                       4096, FileOptions.WriteThrough))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(expected.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            try
            {
                File.Move(temporary, exact);
            }
            catch (IOException) when (File.Exists(exact))
            {
                AssertIdentity(exact, expected);
            }
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        AssertIdentity(exact, expected);
    }
}
