using System.Security.Cryptography;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RimWorldModding.Mcp;

internal sealed record WorkshopPreviewInput(int Index, string Token, string Path, string Sha256);
internal sealed record WorkshopPresentationState(string State, string PlanSha256);

internal static class WorkshopResolvedPresentation
{
    public static async Task ValidateAsync(
        string descriptionPath,
        string provenancePath,
        string workshopRoot,
        string publishedFileId,
        HttpClient http,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(descriptionPath) || !File.Exists(provenancePath))
            throw new InvalidOperationException("Resolved Workshop description provenance is missing.");
        var description = await File.ReadAllTextAsync(descriptionPath, cancellationToken);
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(provenancePath, cancellationToken));
        var root = document.RootElement;
        if (root.GetProperty("schema").GetString() != "ImmersiveChefs/WorkshopDescriptionProvenance/v1" ||
            root.GetProperty("publishedFileId").GetString() != publishedFileId ||
            root.GetProperty("descriptionSha256").GetString() != ReleaseCandidateBuilder.Hash(descriptionPath))
            throw new InvalidOperationException("Resolved Workshop description provenance identity is invalid.");
        var previews = root.GetProperty("previews").EnumerateArray().ToArray();
        if (previews.Length != 7)
            throw new InvalidOperationException("Resolved Immersive Chefs presentation must bind the title plus six feature cards.");
        var inputs = WorkshopPresentationSynchronizer.ReadPreviewInputs(
            workshopRoot,
            Path.Combine(workshopRoot, "preview-main.png"));
        var indices = new HashSet<int>();
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < previews.Length; index++)
        {
            var preview = previews[index];
            var input = inputs[index];
            var token = preview.GetProperty("token").GetString() ?? "";
            var remoteIndex = preview.GetProperty("remoteIndex").GetInt32();
            var url = preview.GetProperty("remoteUrl").GetString() ?? "";
            var localName = preview.GetProperty("localPath").GetString() ?? "";
            var localSha256 = preview.GetProperty("localSha256").GetString() ?? "";
            var remoteSha256 = preview.GetProperty("remoteSha256").GetString() ?? "";
            var remoteType = preview.GetProperty("remoteType").GetString() ?? "";
            if (remoteIndex != index || !indices.Add(remoteIndex) || !tokens.Add(token) ||
                !Regex.IsMatch(token, "^[a-z0-9-]{1,32}$") ||
                token != input.Token || Path.GetFileName(localName) != localName ||
                localName != Path.GetFileName(input.Path) ||
                remoteType != "k_EItemPreviewType_Image" ||
                !Regex.IsMatch(localSha256, "^[A-F0-9]{64}$") ||
                localSha256 != input.Sha256 || remoteSha256 != localSha256)
                throw new InvalidOperationException($"Resolved Workshop preview provenance is invalid at slot {index}.");
            if (!File.Exists(input.Path) || ReleaseCandidateBuilder.Hash(input.Path) != localSha256)
                throw new InvalidOperationException($"Resolved Workshop preview source is invalid at slot {index}.");
            var inlineTag = $"[img]{url}[/img]";
            var occurrences = Regex.Matches(description, Regex.Escape(inlineTag)).Count;
            if (index == 0 ? occurrences != 0 : occurrences != 1)
                throw new InvalidOperationException($"Resolved Workshop description does not contain the expected image identity for slot {index}.");
            byte[] bytes;
            try
            {
                bytes = await WorkshopRemoteBaseline.DownloadBoundedAsync(
                    http, url, 1024 * 1024, "images.steamusercontent.com", "/ugc/", cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                throw new InvalidOperationException(
                    $"Resolved Workshop preview slot {index} is unreachable: HTTP {(int?)exception.StatusCode ?? 0}.", exception);
            }
            if (Convert.ToHexString(SHA256.HashData(bytes)) != localSha256)
                throw new InvalidOperationException($"Resolved Workshop preview slot {index} does not serve the reviewed bytes.");
        }
    }

    public static void AssertRemoteGallery(string provenancePath, WorkshopRemoteBaseline baseline)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(provenancePath));
        var previews = document.RootElement.GetProperty("previews").EnumerateArray().ToArray();
        if (previews.Length != 7 || baseline.AdditionalPreviews.Count != previews.Length ||
            baseline.AdditionalPreviewUrls.Count != previews.Length)
            throw new InvalidOperationException("Authenticated Workshop gallery does not contain the title plus six feature cards.");
        for (var index = 0; index < previews.Length; index++)
        {
            var parts = baseline.AdditionalPreviews[index].Split('|');
            var preview = previews[index];
            if (parts.Length != 4 || parts[0] != index.ToString() ||
                !parts[2].Contains("Image", StringComparison.OrdinalIgnoreCase) ||
                parts[3] != preview.GetProperty("localSha256").GetString() ||
                baseline.AdditionalPreviewUrls[index] != preview.GetProperty("remoteUrl").GetString())
                throw new InvalidOperationException($"Authenticated Workshop gallery differs from resolved presentation slot {index}.");
        }
    }
}

public sealed class WorkshopPresentationSynchronizer(string repositoryRoot)
{
    private readonly string _repositoryRoot = RepositoryRoot.Resolve(repositoryRoot);

    public async Task<WorkshopPresentationSyncResult> SynchronizeAsync(
        string packageId,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(packageId, "fumblesneeze.immersivechefs", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The checked-in presentation synchronizer currently supports only fumblesneeze.immersivechefs.");
        await RequireCleanRevisionAsync(cancellationToken);
        var profile = ReleaseProfileCatalog.Discover(_repositoryRoot)
            .Single(item => string.Equals(item.PackageId, packageId, StringComparison.OrdinalIgnoreCase));
        if (!ulong.TryParse(profile.PublishedFileId, out var publishedFileId) || publishedFileId == 0)
            throw new InvalidOperationException("Presentation synchronization requires one retained nonzero Workshop identity.");
        ReleaseEnvironmentValidator.Validate(profile);
        var workshopRoot = Path.GetDirectoryName(profile.Preview)!;
        var previews = ReadPreviewInputs(workshopRoot, profile.Preview);
        var sourceRevision = (await RunGitAsync(["rev-parse", "HEAD"], cancellationToken)).Trim();
        var planSha256 = HashText(string.Join("\n", new[]
        {
            "RimWorldModdingMcp/WorkshopPresentationSync/v1",
            sourceRevision,
            profile.PackageId,
            publishedFileId.ToString(),
            profile.Title
        }.Concat(previews.Select(item => $"{item.Index}|{item.Token}|{item.Sha256}"))) + "\n");
        var runId = $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}";
        var evidenceRoot = Path.Combine(_repositoryRoot, "artifacts", "Releases", profile.PackageId,
            "presentation-sync", runId);
        Directory.CreateDirectory(evidenceRoot);
        var stateRoot = Path.Combine(_repositoryRoot, "artifacts", "Releases", profile.PackageId);
        Directory.CreateDirectory(stateRoot);
        var statePath = Path.Combine(stateRoot, "presentation-preview-state.txt");
        var identityPath = Path.Combine(Path.GetDirectoryName(profile.Project)!, "About", "PublishedFileId.txt");
        RequireIdentity(identityPath, publishedFileId);

        var mutexName = "Local\\RimWorldModdingMcp.Release." +
                        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(profile.PackageId)))[..24];
        using var mutex = new Semaphore(1, 1, mutexName);
        if (!mutex.WaitOne(0))
            throw new InvalidOperationException("Another publication operation owns this package's release lease.");

        var manager = new RunLeaseManager(_repositoryRoot);
        string? gatewayRunId = null;
        var gatewayStopped = false;
        Exception? primaryFailure = null;
        WorkshopPresentationSyncResult? result = null;
        try
        {
            gatewayRunId = RunLeaseManager.ReserveRunId();
            var start = manager.StartGateway([], [], 1800, gatewayRunId);
            var ready = await WaitReadyAsync(manager, start.RunId, DateTimeOffset.UtcNow.AddMinutes(8), cancellationToken);
            var client = new GatewayWorkshopClient(_repositoryRoot, ready.GatewayManifestPath!, ready.GameProcessId!.Value);
            await client.RegisterAsync(cancellationToken);
            var runtime = await client.InvokeAsync(new Dictionary<string, object?> { ["operation"] = "status" }, cancellationToken);
            WorkshopRemoteBaseline.AssertRuntimeIdentity(runtime, profile);
            var deadline = DateTimeOffset.UtcNow.AddMinutes(12);
            var preflight = await QueryAsync(client, planSha256, publishedFileId, deadline, cancellationToken);
            AssertRemoteIdentity(preflight, profile, publishedFileId);
            await RequirePlanInputsUnchangedAsync(sourceRevision, workshopRoot, profile.Preview, previews, cancellationToken);
            var priorState = ReadPresentationState(statePath, publishedFileId);
            var remoteMayAlreadyBeExact = priorState is { State: "succeeded" } &&
                                          string.Equals(priorState.PlanSha256, planSha256, StringComparison.OrdinalIgnoreCase);
            var remoteIsExact = remoteMayAlreadyBeExact &&
                                await IsExactRemoteInventoryAsync(preflight, previews, cancellationToken);
            var mutation = DecideMutation(priorState, planSha256, remoteIsExact);
            if (mutation == "submit")
            {
                var confirmationNonce = HashText("presentation-sync-nonce\n" + planSha256 + "\n");
                _ = await client.InvokeAsync(BuildPreviewSyncRequest(
                    planSha256, confirmationNonce, publishedFileId, identityPath, statePath,
                    profile.Title, previews.Select(item => item.Path).ToArray()), cancellationToken);
                var synchronized = await client.WaitTerminalAsync(
                    planSha256,
                    new HashSet<string>(["succeeded", "failed", "legal-agreement-required"], StringComparer.Ordinal),
                    deadline,
                    cancellationToken);
                if (GatewayWorkshopClient.String(synchronized, "Status") != "succeeded")
                    throw new InvalidOperationException("Steam did not accept the reviewed additional-preview synchronization.");
            }

            var remote = mutation == "skip"
                ? preflight
                : await WaitForExactPreviewInventoryAsync(
                    client, planSha256, publishedFileId, previews, deadline, cancellationToken);
            var inventoryPath = await RetainVerifiedInventoryAsync(
                remote, previews, publishedFileId, planSha256, evidenceRoot, cancellationToken);
            DurableFile.WriteAllText(statePath, $"succeeded|{planSha256}|{publishedFileId}");
            await RequirePlanInputsUnchangedAsync(sourceRevision, workshopRoot, profile.Preview, previews, cancellationToken);
            var resolverPath = Path.Combine(_repositoryRoot, "scripts", "Resolve-ImmersiveChefsWorkshopDescription.ps1");
            var resolver = await ProcessRunner.RunAsync(
                "pwsh",
                ["-NoProfile", "-NonInteractive", "-File", resolverPath,
                    "-InventoryPath", inventoryPath,
                    "-DestinationPath", profile.Description,
                    "-Output", "json"],
                _repositoryRoot,
                TimeSpan.FromMinutes(2),
                cancellationToken);
            if (resolver.ExitCode != 0)
                throw new InvalidOperationException("Workshop description resolution failed: " + Bound(resolver.StandardError + resolver.StandardOutput));
            using var resolution = ParseSingleJson(resolver.StandardOutput, "description resolver");
            var resolved = resolution.RootElement;
            var descriptionPath = Path.GetFullPath(resolved.GetProperty("destinationPath").GetString()!);
            var descriptionSha256 = resolved.GetProperty("sha256").GetString()!;
            var provenancePath = Path.GetFullPath(resolved.GetProperty("provenancePath").GetString()!);
            if (!string.Equals(descriptionPath, profile.Description, StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(descriptionPath) || !File.Exists(provenancePath) ||
                !string.Equals(ReleaseCandidateBuilder.Hash(descriptionPath), descriptionSha256, StringComparison.Ordinal))
                throw new InvalidOperationException("Resolved Workshop description/provenance does not match the owned presentation output.");
            result = new WorkshopPresentationSyncResult(
                "presentation-synchronized",
                profile.PackageId,
                publishedFileId.ToString(),
                $"https://steamcommunity.com/sharedfiles/filedetails/?id={publishedFileId}",
                planSha256,
                inventoryPath,
                previews.Count,
                descriptionPath,
                descriptionSha256,
                provenancePath,
                false,
                gatewayRunId,
                false);
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
        }
        finally
        {
            try
            {
                if (gatewayRunId is not null)
                {
                    _ = await manager.CancelAsync(gatewayRunId, CancellationToken.None);
                    gatewayStopped = true;
                }
            }
            catch (Exception cleanupFailure) when (primaryFailure is not null)
            {
                primaryFailure = new AggregateException(
                    "Presentation synchronization and exact Gateway cleanup both failed.",
                    primaryFailure,
                    cleanupFailure);
            }
            mutex.Release();
        }
        if (primaryFailure is not null) ExceptionDispatchInfo.Capture(primaryFailure).Throw();
        return result! with { GatewayStopped = gatewayStopped };
    }

    internal static IReadOnlyList<WorkshopPreviewInput> ReadPreviewInputs(string workshopRoot, string titlePath)
    {
        var root = Path.GetFullPath(workshopRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        titlePath = RequireContainedFile(root, titlePath);
        var manifestPath = RequireContainedFile(root, Path.Combine(root, "presentation.json"));
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var manifest = document.RootElement;
        if (!manifest.TryGetProperty("schema", out var schema) ||
            schema.GetString() != "ImmersiveChefs/WorkshopPresentation/v1" ||
            !manifest.TryGetProperty("carouselCards", out var carousel) ||
            carousel.ValueKind != JsonValueKind.Array ||
            !manifest.TryGetProperty("cards", out var cards) ||
            cards.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Workshop presentation manifest is invalid.");
        var tokens = carousel.EnumerateArray().Select(item => item.GetString() ?? "").ToArray();
        if (tokens.Length != 6 || tokens.Distinct(StringComparer.Ordinal).Count() != tokens.Length ||
            tokens.Any(token => !Regex.IsMatch(token, "^[a-z0-9-]{1,32}$")))
            throw new InvalidOperationException("Immersive Chefs requires exactly six unique ordered feature cards.");
        var cardPaths = cards.EnumerateArray().ToDictionary(
            card => card.GetProperty("token").GetString() ?? "",
            card => card.GetProperty("path").GetString() ?? "",
            StringComparer.Ordinal);
        if (cardPaths.Count != tokens.Length || !cardPaths.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(tokens))
            throw new InvalidOperationException("Workshop feature-card inventory does not match its carousel order.");

        var inputs = new List<WorkshopPreviewInput>
        {
            CreateInput(0, "immersive-chefs", titlePath)
        };
        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index];
            var relative = cardPaths[token].Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(relative) || relative.Split(Path.DirectorySeparatorChar).Contains("..", StringComparer.Ordinal))
                throw new InvalidOperationException($"Workshop feature-card path is unsafe for '{token}'.");
            inputs.Add(CreateInput(index + 1, token, RequireContainedFile(root, Path.Combine(root, relative))));
        }
        return inputs;
    }

    private static WorkshopPreviewInput CreateInput(int index, string token, string path)
    {
        var length = new FileInfo(path).Length;
        if (length is <= 0 or >= 1024 * 1024)
            throw new InvalidOperationException($"Workshop preview '{token}' must be nonempty and below one MiB.");
        return new WorkshopPreviewInput(index, token, path,
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
    }

    private static string RequireContainedFile(string root, string path)
    {
        var full = Path.GetFullPath(path);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(full))
            throw new InvalidOperationException("Workshop presentation input is missing or outside its owned root.");
        return full;
    }

    private static WorkshopPresentationState? ReadPresentationState(string path, ulong publishedFileId)
    {
        if (!File.Exists(path)) return null;
        var parts = File.ReadAllText(path).Trim().Split('|');
        if (parts.Length != 3 || !ulong.TryParse(parts[2], out var itemId) || itemId != publishedFileId ||
            !Regex.IsMatch(parts[1], "^[A-Fa-f0-9]{64}$"))
            throw new InvalidOperationException("Durable presentation-preview state is malformed or targets another item.");
        return new WorkshopPresentationState(parts[0], parts[1]);
    }

    internal static string DecideMutation(
        WorkshopPresentationState? priorState,
        string planSha256,
        bool remoteIsExact)
    {
        if (priorState is null) return "submit";
        if (priorState.State == "legal-agreement-required")
            throw new InvalidOperationException("Steam requires Workshop legal-agreement acceptance before presentation recovery can continue.");
        var samePlan = string.Equals(priorState.PlanSha256, planSha256, StringComparison.OrdinalIgnoreCase);
        if (!samePlan)
        {
            if (priorState.State is "succeeded" or "preview-submit-failed-definite") return "submit";
            throw new InvalidOperationException("A different presentation plan still has admitted or indeterminate Steam ownership.");
        }
        return priorState.State switch
        {
            "preview-submit-admitted" or "preview-submitted" or "preview-submit-indeterminate" => "resume",
            "succeeded" when remoteIsExact => "skip",
            "succeeded" or "preview-submit-failed-definite" => "submit",
            _ => throw new InvalidOperationException("Durable presentation-preview state is not a recognized safe transition.")
        };
    }

    internal static IReadOnlyDictionary<string, object?> BuildPreviewSyncRequest(
        string planSha256,
        string confirmationNonce,
        ulong publishedFileId,
        string identityPath,
        string statePath,
        string title,
        string[] previewPaths)
    {
        if (Path.GetFileName(statePath) != "presentation-preview-state.txt")
            throw new InvalidOperationException("Presentation sync requires the canonical durable state path.");
        return new Dictionary<string, object?>
        {
            ["operation"] = "preview-sync",
            ["planSha256"] = planSha256,
            ["confirmationNonce"] = confirmationNonce,
            ["confirmation"] = $"publish {planSha256} {confirmationNonce}",
            ["publishedFileId"] = publishedFileId.ToString(),
            ["identityPath"] = identityPath,
            ["statePath"] = statePath,
            ["title"] = title,
            ["changeNote"] = "",
            ["additionalPreviewPaths"] = previewPaths
        };
    }

    private static async Task<bool> IsExactRemoteInventoryAsync(
        JsonElement remote,
        IReadOnlyList<WorkshopPreviewInput> previews,
        CancellationToken cancellationToken)
    {
        if (!GatewayWorkshopClient.TryProperty(remote, "RemoteAdditionalPreviews", out var property) ||
            property.ValueKind != JsonValueKind.Array) return false;
        var items = property.EnumerateArray().ToArray();
        if (items.Length != previews.Count) return false;
        using var http = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = false
        });
        http.DefaultRequestHeaders.UserAgent.ParseAdd("RimWorldModding.Mcp/0.1");
        for (var index = 0; index < previews.Count; index++)
        {
            var item = items[index];
            if (ReadInt32(item, "Index") != index ||
                GatewayWorkshopClient.String(item, "Type") != "k_EItemPreviewType_Image") return false;
            try
            {
                var bytes = await WorkshopRemoteBaseline.DownloadBoundedAsync(
                    http, GatewayWorkshopClient.String(item, "Url"), 1024 * 1024,
                    "images.steamusercontent.com", "/ugc/", cancellationToken);
                if (Convert.ToHexString(SHA256.HashData(bytes)) != previews[index].Sha256) return false;
            }
            catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
            {
                return false;
            }
        }
        return true;
    }

    private async Task<string> RetainVerifiedInventoryAsync(
        JsonElement remote,
        IReadOnlyList<WorkshopPreviewInput> previews,
        ulong publishedFileId,
        string planSha256,
        string evidenceRoot,
        CancellationToken cancellationToken)
    {
        if (!GatewayWorkshopClient.TryProperty(remote, "RemoteAdditionalPreviews", out var property) ||
            property.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Steam did not return an additional-preview inventory.");
        var remoteItems = property.EnumerateArray().ToArray();
        if (remoteItems.Length != previews.Count)
            throw new InvalidOperationException("Steam did not return the exact reviewed additional-preview count.");
        using var http = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = false
        });
        http.DefaultRequestHeaders.UserAgent.ParseAdd("RimWorldModding.Mcp/0.1");
        var retained = new List<object>();
        for (var index = 0; index < previews.Count; index++)
        {
            var local = previews[index];
            var item = remoteItems[index];
            var remoteIndex = ReadInt32(item, "Index");
            var remoteUrl = GatewayWorkshopClient.String(item, "Url");
            var remoteType = GatewayWorkshopClient.String(item, "Type");
            if (remoteIndex != index || remoteType != "k_EItemPreviewType_Image")
                throw new InvalidOperationException($"Steam returned an unexpected preview type/order at slot {index}.");
            var bytes = await WorkshopRemoteBaseline.DownloadBoundedAsync(
                http, remoteUrl, 1024 * 1024, "images.steamusercontent.com", "/ugc/", cancellationToken);
            var remoteSha256 = Convert.ToHexString(SHA256.HashData(bytes));
            if (!string.Equals(remoteSha256, local.Sha256, StringComparison.Ordinal))
                throw new InvalidOperationException($"Steam preview slot {index} differs from the reviewed local image.");
            var downloadPath = Path.Combine(evidenceRoot, $"remote-additional-preview-{index:D2}.png");
            await File.WriteAllBytesAsync(downloadPath, bytes, cancellationToken);
            retained.Add(new
            {
                token = local.Token,
                localPath = local.Path,
                localSha256 = local.Sha256,
                remoteIndex,
                remoteUrl,
                remoteOriginalFileName = GatewayWorkshopClient.String(item, "OriginalFileName"),
                remoteType,
                remoteDownloadedPath = downloadPath,
                remoteSha256
            });
        }
        var inventoryPath = Path.Combine(evidenceRoot, "presentation-preview-inventory.json");
        DurableFile.WriteAllText(inventoryPath, JsonSerializer.Serialize(new
        {
            schema = "ImmersiveChefs/WorkshopRemotePreviewInventory/v1",
            publishedFileId = publishedFileId.ToString(),
            publicationPlanSha256 = planSha256,
            previews = retained
        }, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
        return inventoryPath;
    }

    private static async Task<JsonElement> WaitForExactPreviewInventoryAsync(
        GatewayWorkshopClient client,
        string planSha256,
        ulong publishedFileId,
        IReadOnlyList<WorkshopPreviewInput> expectedPreviews,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        JsonElement last = default;
        while (DateTimeOffset.UtcNow < deadline)
        {
            last = await QueryAsync(client, planSha256, publishedFileId, deadline, cancellationToken);
            if (await IsExactRemoteInventoryAsync(last, expectedPreviews, cancellationToken))
                return last;
            await Task.Delay(1500, cancellationToken);
        }
        throw new TimeoutException("Steam did not expose the exact reviewed additional-preview bytes and order before the deadline.");
    }

    private static async Task<JsonElement> QueryAsync(
        GatewayWorkshopClient client,
        string planSha256,
        ulong publishedFileId,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        _ = await client.InvokeAsync(new Dictionary<string, object?>
        {
            ["operation"] = "query",
            ["planSha256"] = planSha256,
            ["publishedFileId"] = publishedFileId.ToString()
        }, cancellationToken);
        var query = await client.WaitTerminalAsync(
            planSha256,
            new HashSet<string>(["queried", "failed"], StringComparer.Ordinal),
            deadline,
            cancellationToken);
        if (GatewayWorkshopClient.String(query, "Status") != "queried")
            throw new InvalidOperationException("Steam item query failed.");
        return query;
    }

    private static void AssertRemoteIdentity(JsonElement remote, ReleaseProfile profile, ulong publishedFileId)
    {
        if (GatewayWorkshopClient.UInt64(remote, "PublishedFileId") != publishedFileId ||
            GatewayWorkshopClient.UInt64(remote, "RemoteOwnerSteamId").ToString() != profile.SteamUserId ||
            GatewayWorkshopClient.UInt64(remote, "RemoteConsumerAppId") != (ulong)profile.SteamAppId ||
            GatewayWorkshopClient.String(remote, "RemoteTitle") != profile.Title)
            throw new InvalidOperationException("Existing Workshop item failed exact ID/owner/app/title preflight.");
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
                throw new InvalidOperationException("Gateway presentation run exited before readiness: " + status.StandardErrorTail);
            await Task.Delay(1000, cancellationToken);
        }
        throw new TimeoutException("Gateway presentation run did not become ready.");
    }

    private async Task RequireCleanRevisionAsync(CancellationToken cancellationToken)
    {
        var status = await RunGitAsync(["status", "--porcelain", "--untracked-files=all"], cancellationToken);
        if (!string.IsNullOrWhiteSpace(status))
            throw new InvalidOperationException("Workshop presentation synchronization requires a clean committed worktree.");
    }

    private async Task RequirePlanInputsUnchangedAsync(
        string expectedRevision,
        string workshopRoot,
        string titlePath,
        IReadOnlyList<WorkshopPreviewInput> expectedPreviews,
        CancellationToken cancellationToken)
    {
        await RequireCleanRevisionAsync(cancellationToken);
        var revision = (await RunGitAsync(["rev-parse", "HEAD"], cancellationToken)).Trim();
        var current = ReadPreviewInputs(workshopRoot, titlePath);
        if (!string.Equals(revision, expectedRevision, StringComparison.Ordinal) ||
            current.Count != expectedPreviews.Count ||
            current.Where((item, index) => item != expectedPreviews[index]).Any())
            throw new InvalidOperationException("Workshop presentation source changed after synchronization admission.");
    }

    private async Task<string> RunGitAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var result = await ProcessRunner.RunAsync("git", arguments, _repositoryRoot, TimeSpan.FromSeconds(30), cancellationToken);
        if (result.ExitCode != 0)
            throw new InvalidOperationException("Git inspection failed: " + Bound(result.StandardError));
        return result.StandardOutput;
    }

    private static void RequireIdentity(string path, ulong expected)
    {
        if (!File.Exists(path) || !ulong.TryParse(File.ReadAllText(path).Trim(), out var actual) || actual != expected)
            throw new InvalidOperationException("Checked-in Workshop identity is missing or inconsistent.");
    }

    private static int ReadInt32(JsonElement root, string name)
    {
        if (!GatewayWorkshopClient.TryProperty(root, name, out var value) || !value.TryGetInt32(out var parsed))
            throw new InvalidOperationException($"Workshop response is missing integer '{name}'.");
        return parsed;
    }

    private static JsonDocument ParseSingleJson(string value, string label)
    {
        try { return JsonDocument.Parse(value.Trim()); }
        catch (JsonException exception) { throw new InvalidOperationException($"{label} did not return one JSON object.", exception); }
    }

    private static string HashText(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Bound(string value) => value.Length <= 8192 ? value.Trim() : value[..8192].Trim();
}
