using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using RimWorldDevGateway.Contracts;
using Steamworks;
using Verse.Steam;

namespace GatewaySteamWorkshopPublisher;

public static class Entry
{
    public static string Execute(string requestJson, GatewayAssemblyExecutionContext context)
    {
        Publisher.Register();
        context.RuntimeExtensions.RegisterSessionAutomation(
            new GatewayAssemblyAutomationDescriptor(
                "release.workshop",
                "1",
                "Creates or updates one explicitly reviewed RimWorld Workshop item and reports callback-owned status.",
                true,
                new Dictionary<string, string>
                {
                    ["operation"] = "status|publish|query|subscribe|install-status",
                    ["planSha256"] = "Exact reviewed publication-plan SHA-256.",
                    ["confirmation"] = "Exact operator confirmation phrase for publish."
                },
                new[] { "Steam initialized", "Exact reviewed immutable release candidate" }),
            Publisher.Handle);
        return "release.workshop registered";
    }
}

internal static class Publisher
{
    private const string Confirmation = "publish immersive chefs to steam workshop";
    private static readonly object Gate = new();
    private static Callback<DownloadItemResult_t>? downloadCallback;
    private static CallResult<CreateItemResult_t>? createResult;
    private static CallResult<SubmitItemUpdateResult_t>? submitResult;
    private static CallResult<AddUGCDependencyResult_t>? dependencyResult;
    private static CallResult<RemoteStorageSubscribePublishedFileResult_t>? subscribeResult;
    private static CallResult<SteamUGCQueryCompleted_t>? queryResult;
    private static UGCQueryHandle_t queryHandle;
    private static Request? activeRequest;
    private static Snapshot snapshot = Snapshot.Idle();
    private static ulong lastQueriedId;
    private static ulong lastQueriedOwner;
    private static uint lastQueriedAppId;
    private static string lastQueriedTitle = "";

    public static void Register()
    {
        lock (Gate)
        {
            downloadCallback ??= Callback<DownloadItemResult_t>.Create(OnDownloaded);
        }
    }

    public static string Handle(string argumentsJson, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = Request.Parse(argumentsJson);
        lock (Gate)
        {
            if (request.Operation == "status")
            {
                if (SteamManager.Initialized && SteamUser.BLoggedOn())
                {
                    snapshot.CurrentUserSteamId = SteamUser.GetSteamID().m_SteamID;
                }
                return snapshot.ToJson();
            }

            if (request.Operation == "install-status")
            {
                return InstallStatus(request.PublishedFileId).ToJson();
            }

            if (!SteamManager.Initialized || !SteamUser.BLoggedOn())
            {
                throw new InvalidOperationException("Steam is not initialized and logged on in this RimWorld process.");
            }

            if (request.Operation == "subscribe")
            {
                EnsureIdle();
                if (request.PublishedFileId == 0) throw new InvalidOperationException("subscribe requires a nonzero publishedFileId.");
                activeRequest = request;
                snapshot = Snapshot.Pending("subscribing", request.PlanSha256, request.PublishedFileId);
                subscribeResult = CallResult<RemoteStorageSubscribePublishedFileResult_t>.Create(OnSubscribed);
                subscribeResult.Set(SteamUGC.SubscribeItem(new PublishedFileId_t(request.PublishedFileId)));
                return snapshot.ToJson();
            }

            if (request.Operation == "query")
            {
                EnsureIdle();
                if (request.PublishedFileId == 0) throw new InvalidOperationException("query requires a nonzero publishedFileId.");
                activeRequest = request;
                snapshot = Snapshot.Pending("querying", request.PlanSha256, request.PublishedFileId);
                queryHandle = SteamUGC.CreateQueryUGCDetailsRequest(
                    new[] { new PublishedFileId_t(request.PublishedFileId) },
                    1u);
                if (!SteamUGC.SetReturnLongDescription(queryHandle, true) ||
                    !SteamUGC.SetReturnMetadata(queryHandle, true) ||
                    !SteamUGC.SetReturnChildren(queryHandle, true) ||
                    !SteamUGC.SetReturnAdditionalPreviews(queryHandle, true))
                {
                    ReleaseQuery();
                    snapshot = Snapshot.Failed(request.PlanSha256, request.PublishedFileId, "Steam rejected query options.");
                    activeRequest = null;
                    return snapshot.ToJson();
                }

                queryResult = CallResult<SteamUGCQueryCompleted_t>.Create(OnQueried);
                queryResult.Set(SteamUGC.SendQueryUGCRequest(queryHandle));
                return snapshot.ToJson();
            }

            if (request.Operation == "dependency")
            {
                EnsureIdle();
                if (request.PublishedFileId == 0 || request.RequiredWorkshopItemId == 0)
                    throw new InvalidOperationException("dependency requires both Workshop item IDs.");
                RequireExistingPreflight(request);
                activeRequest = request;
                snapshot = Snapshot.Pending("dependency", request.PlanSha256, request.PublishedFileId);
                dependencyResult = CallResult<AddUGCDependencyResult_t>.Create(OnDependencyAdded);
                dependencyResult.Set(SteamUGC.AddDependency(
                    new PublishedFileId_t(request.PublishedFileId),
                    new PublishedFileId_t(request.RequiredWorkshopItemId)));
                return snapshot.ToJson();
            }

            if (request.Operation != "publish") throw new InvalidOperationException("Unsupported operation.");
            EnsureIdle();
            request.ValidatePublish(Confirmation);
            activeRequest = request;
            snapshot = Snapshot.Pending(
                request.PublishedFileId == 0 ? "creating" : "updating",
                request.PlanSha256,
                request.PublishedFileId);
            if (request.PublishedFileId == 0)
            {
                if (!request.AllowFirstPublication) throw new InvalidOperationException("First publication is not allowed by this plan.");
                if (File.Exists(request.IdentityPath)) throw new InvalidOperationException("A local Workshop identity already exists; refusing a second item.");
                WriteStateAtomically(request.StatePath, "create-admitted|" + request.PlanSha256 + "|0");
                createResult = CallResult<CreateItemResult_t>.Create(OnCreated);
                createResult.Set(SteamUGC.CreateItem(new AppId_t(294100), EWorkshopFileType.k_EWorkshopFileTypeFirst));
            }
            else
            {
                RequireExactIdentity(request.IdentityPath, request.PublishedFileId, "durable identity");
                RequireExactIdentity(request.PackageIdentityPath, request.PublishedFileId, "package identity");
                RequireExistingPreflight(request);
                BeginUpdate(request.PublishedFileId);
            }

            return snapshot.ToJson();
        }
    }

    private static void OnCreated(CreateItemResult_t result, bool ioFailure)
    {
        lock (Gate)
        {
            createResult = null;
            if (ioFailure || result.m_eResult != EResult.k_EResultOK || result.m_nPublishedFileId.m_PublishedFileId == 0)
            {
                Fail("create", result.m_eResult, ioFailure);
                return;
            }

            var request = RequiredRequest();
            var id = result.m_nPublishedFileId.m_PublishedFileId;
            Directory.CreateDirectory(Path.GetDirectoryName(request.IdentityPath)!);
            WriteIdentityAtomically(request.IdentityPath, id.ToString());
            Directory.CreateDirectory(Path.GetDirectoryName(request.PackageIdentityPath)!);
            WriteIdentityAtomically(request.PackageIdentityPath, id.ToString());
            WriteStateAtomically(request.StatePath, "created|" + request.PlanSha256 + "|" + id);
            snapshot = Snapshot.Pending("created", request.PlanSha256, id, result.m_bUserNeedsToAcceptWorkshopLegalAgreement);
            BeginUpdate(id);
        }
    }

    private static void BeginUpdate(ulong publishedFileId)
    {
        var request = RequiredRequest();
        var handle = SteamUGC.StartItemUpdate(new AppId_t(294100), new PublishedFileId_t(publishedFileId));
        if (!AcceptSetter(SteamUGC.SetItemUpdateLanguage(handle, "english"), "SetItemUpdateLanguage") ||
            !AcceptSetter(SteamUGC.SetItemTitle(handle, request.Title), "SetItemTitle") ||
            !AcceptSetter(SteamUGC.SetItemDescription(handle, File.ReadAllText(request.DescriptionPath, Encoding.UTF8)), "SetItemDescription") ||
            !AcceptSetter(SteamUGC.SetItemVisibility(handle, request.Visibility), "SetItemVisibility") ||
            !AcceptSetter(SteamUGC.SetItemTags(handle, request.Tags), "SetItemTags") ||
            !AcceptSetter(SteamUGC.SetItemContent(handle, request.PackagePath), "SetItemContent") ||
            !AcceptSetter(SteamUGC.SetItemPreview(handle, request.PreviewPath), "SetItemPreview") ||
            !AcceptSetter(SteamUGC.SetItemMetadata(handle, request.PlanSha256), "SetItemMetadata"))
        {
            return;
        }
        snapshot = Snapshot.Pending("submitting", request.PlanSha256, publishedFileId);
        WriteStateAtomically(request.StatePath, "submit-admitted|" + request.PlanSha256 + "|" + publishedFileId);
        submitResult = CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitted);
        submitResult.Set(SteamUGC.SubmitItemUpdate(handle, request.ChangeNote));
    }

    private static void OnSubmitted(SubmitItemUpdateResult_t result, bool ioFailure)
    {
        lock (Gate)
        {
            submitResult = null;
            if (ioFailure || result.m_eResult != EResult.k_EResultOK)
            {
                Fail("submit", result.m_eResult, ioFailure);
                return;
            }

            var request = RequiredRequest();
            var id = result.m_nPublishedFileId.m_PublishedFileId;
            WriteStateAtomically(request.StatePath, "submitted|" + request.PlanSha256 + "|" + id);
            if (result.m_bUserNeedsToAcceptWorkshopLegalAgreement)
            {
                snapshot = Snapshot.LegalAgreementRequired(request.PlanSha256, id);
                activeRequest = null;
                return;
            }

            snapshot = Snapshot.Pending("dependency", request.PlanSha256, id, result.m_bUserNeedsToAcceptWorkshopLegalAgreement);
            dependencyResult = CallResult<AddUGCDependencyResult_t>.Create(OnDependencyAdded);
            dependencyResult.Set(SteamUGC.AddDependency(new PublishedFileId_t(id), new PublishedFileId_t(request.RequiredWorkshopItemId)));
        }
    }

    private static void OnDependencyAdded(AddUGCDependencyResult_t result, bool ioFailure)
    {
        lock (Gate)
        {
            dependencyResult = null;
            if (ioFailure || (result.m_eResult != EResult.k_EResultOK && result.m_eResult != EResult.k_EResultDuplicateRequest))
            {
                Fail("dependency", result.m_eResult, ioFailure);
                return;
            }

            var request = RequiredRequest();
            WriteStateAtomically(request.StatePath, "succeeded|" + request.PlanSha256 + "|" + result.m_nPublishedFileId.m_PublishedFileId);
            snapshot = Snapshot.Succeeded(request.PlanSha256, result.m_nPublishedFileId.m_PublishedFileId);
            activeRequest = null;
        }
    }

    private static void OnSubscribed(RemoteStorageSubscribePublishedFileResult_t result, bool ioFailure)
    {
        lock (Gate)
        {
            subscribeResult = null;
            if (ioFailure || result.m_eResult != EResult.k_EResultOK)
            {
                Fail("subscribe", result.m_eResult, ioFailure);
                return;
            }

            var request = RequiredRequest();
            var id = result.m_nPublishedFileId.m_PublishedFileId;
            snapshot = Snapshot.Pending("downloading", request.PlanSha256, id);
            if (!SteamUGC.DownloadItem(new PublishedFileId_t(id), true))
            {
                snapshot = Snapshot.Failed(request.PlanSha256, id, "DownloadItem returned false.");
                activeRequest = null;
            }
        }
    }

    private static void OnDownloaded(DownloadItemResult_t result)
    {
        lock (Gate)
        {
            if (activeRequest == null || activeRequest.Operation != "subscribe" || activeRequest.PublishedFileId != result.m_nPublishedFileId.m_PublishedFileId) return;
            if (result.m_eResult != EResult.k_EResultOK)
            {
                Fail("download", result.m_eResult, false);
                return;
            }

            snapshot = InstallStatus(result.m_nPublishedFileId.m_PublishedFileId);
            activeRequest = null;
        }
    }

    private static void OnQueried(SteamUGCQueryCompleted_t result, bool ioFailure)
    {
        lock (Gate)
        {
            queryResult = null;
            try
            {
                var request = RequiredRequest();
                if (ioFailure || result.m_eResult != EResult.k_EResultOK || result.m_unNumResultsReturned != 1)
                {
                    Fail("query", result.m_eResult, ioFailure);
                    return;
                }

                SteamUGCDetails_t details;
                if (!SteamUGC.GetQueryUGCResult(queryHandle, 0u, out details) || details.m_eResult != EResult.k_EResultOK)
                {
                    snapshot = Snapshot.Failed(request.PlanSha256, request.PublishedFileId, "Steam did not return one usable item detail record.");
                    activeRequest = null;
                    return;
                }

                string metadata;
                if (!SteamUGC.GetQueryUGCMetadata(queryHandle, 0u, out metadata, 4097u)) metadata = "";
                string previewUrl;
                if (!SteamUGC.GetQueryUGCPreviewURL(queryHandle, 0u, out previewUrl, 4097u)) previewUrl = "";
                var childCount = Math.Min(details.m_unNumChildren, 64u);
                var children = new PublishedFileId_t[childCount];
                if (childCount > 0 && !SteamUGC.GetQueryUGCChildren(queryHandle, 0u, children, childCount))
                {
                    snapshot = Snapshot.Failed(request.PlanSha256, request.PublishedFileId, "Steam did not return the declared dependencies.");
                    activeRequest = null;
                    return;
                }

                snapshot = Snapshot.Queried(
                    request.PlanSha256,
                    details,
                    metadata,
                    previewUrl,
                    children.Select(child => child.m_PublishedFileId).ToArray());
                lastQueriedId = details.m_nPublishedFileId.m_PublishedFileId;
                lastQueriedOwner = details.m_ulSteamIDOwner;
                lastQueriedAppId = details.m_nConsumerAppID.m_AppId;
                lastQueriedTitle = details.m_rgchTitle ?? "";
                activeRequest = null;
            }
            finally
            {
                ReleaseQuery();
            }
        }
    }

    private static Snapshot InstallStatus(ulong id)
    {
        if (id == 0) return Snapshot.Failed("", 0, "install-status requires a nonzero publishedFileId.");
        ulong size;
        string folder;
        uint timestamp;
        var installed = SteamUGC.GetItemInstallInfo(new PublishedFileId_t(id), out size, out folder, 4096u, out timestamp);
        var state = SteamUGC.GetItemState(new PublishedFileId_t(id));
        return installed
            ? Snapshot.Installed(id, folder, size, state)
            : Snapshot.Pending("install-pending", "", id, false, state.ToString());
    }

    private static bool AcceptSetter(bool accepted, string name)
    {
        if (accepted) return true;
        var request = RequiredRequest();
        snapshot = Snapshot.Failed(request.PlanSha256, snapshot.PublishedFileId, name + " returned false.");
        activeRequest = null;
        return false;
    }

    private static void ReleaseQuery()
    {
        if (queryHandle.m_UGCQueryHandle != 0) SteamUGC.ReleaseQueryUGCRequest(queryHandle);
        queryHandle = new UGCQueryHandle_t(0);
    }

    private static void EnsureIdle()
    {
        if (activeRequest != null) throw new InvalidOperationException("A Workshop operation is already active.");
    }

    private static Request RequiredRequest() => activeRequest ?? throw new InvalidOperationException("No Workshop request is active.");

    private static void Fail(string stage, EResult result, bool ioFailure)
    {
        var request = activeRequest;
        snapshot = Snapshot.Failed(request?.PlanSha256 ?? "", snapshot.PublishedFileId, stage + " failed: " + result + "; ioFailure=" + ioFailure);
        activeRequest = null;
    }

    private static void WriteIdentityAtomically(string path, string value)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var owned = false;
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                owned = true;
                writer.Write(value);
            }

            if (File.Exists(path)) throw new IOException("Workshop identity appeared concurrently; refusing overwrite.");
            File.Move(temporary, path);
            owned = false;
        }
        finally
        {
            if (owned && File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static void WriteStateAtomically(string path, string value)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("statePath is invalid.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, value, new UTF8Encoding(false));
            if (File.Exists(path))
            {
                var backup = path + "." + Guid.NewGuid().ToString("N") + ".bak";
                try { File.Replace(temporary, path, backup); }
                finally { if (File.Exists(backup)) File.Delete(backup); }
            }
            else
            {
                File.Move(temporary, path);
            }
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static void RequireExactIdentity(string path, ulong expected, string label)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path) ||
            !ulong.TryParse(File.ReadAllText(path).Trim(), out var actual) || actual != expected)
        {
            throw new InvalidOperationException("The " + label + " does not match the declared Workshop item.");
        }
    }

    private static void RequireExistingPreflight(Request request)
    {
        var currentUser = SteamUser.GetSteamID().m_SteamID;
        if (lastQueriedId != request.PublishedFileId ||
            lastQueriedOwner != currentUser ||
            lastQueriedAppId != 294100u ||
            !string.Equals(lastQueriedTitle, request.Title, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The existing Workshop item has not passed the exact in-process ID/owner/app/title preflight.");
        }
    }

    internal sealed class Request
    {
        public string Operation = "";
        public string PlanSha256 = "";
        public string Confirmation = "";
        public ulong PublishedFileId;
        public bool AllowFirstPublication;
        public string IdentityPath = "";
        public string PackageIdentityPath = "";
        public string StatePath = "";
        public string Title = "";
        public string PackagePath = "";
        public string DescriptionPath = "";
        public string PreviewPath = "";
        public string ChangeNote = "";
        public ulong RequiredWorkshopItemId;
        public List<string> Tags = new();
        public ERemoteStoragePublishedFileVisibility Visibility;

        public static Request Parse(string json)
        {
            RequestJson objectValue;
            try { objectValue = Json.Read<RequestJson>(json); }
            catch (Exception exception) { throw new InvalidOperationException("Invalid JSON request.", exception); }
            if (objectValue == null) throw new InvalidOperationException("Invalid JSON request.");
            var operation = objectValue.operation ?? "";
            var request = new Request
            {
                Operation = operation,
                PlanSha256 = objectValue.planSha256 ?? "",
                Confirmation = objectValue.confirmation ?? "",
                PublishedFileId = ParseId(objectValue.publishedFileId, allowEmpty: true),
                AllowFirstPublication = objectValue.allowFirstPublication,
                IdentityPath = objectValue.identityPath ?? "",
                PackageIdentityPath = objectValue.packageIdentityPath ?? "",
                StatePath = objectValue.statePath ?? "",
                Title = objectValue.title ?? "",
                PackagePath = objectValue.packagePath ?? "",
                DescriptionPath = objectValue.descriptionPath ?? "",
                PreviewPath = objectValue.previewPath ?? "",
                ChangeNote = objectValue.changeNote ?? "",
                RequiredWorkshopItemId = ParseId(objectValue.requiredWorkshopItemId, allowEmpty: true),
                Tags = (objectValue.tags ?? Array.Empty<string>()).ToList(),
                Visibility = string.IsNullOrWhiteSpace(objectValue.visibility)
                    ? ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate
                    : ParseVisibility(objectValue.visibility)
            };
            if (string.IsNullOrWhiteSpace(request.Operation)) throw new InvalidOperationException("operation is required.");
            return request;
        }

        public void ValidatePublish(string exactConfirmation)
        {
            if (Confirmation != exactConfirmation) throw new InvalidOperationException("Exact publish confirmation is required.");
            if (PlanSha256.Length != 64 || PlanSha256.Any(c => !Uri.IsHexDigit(c))) throw new InvalidOperationException("planSha256 is invalid.");
            if (string.IsNullOrWhiteSpace(Title) || Title.Length > 128) throw new InvalidOperationException("title is invalid.");
            RequireFile(DescriptionPath, "descriptionPath");
            RequireFile(PreviewPath, "previewPath");
            RequireDirectory(PackagePath, "packagePath");
            if (string.IsNullOrWhiteSpace(IdentityPath) || Path.GetFileName(IdentityPath) != "PublishedFileId.txt") throw new InvalidOperationException("identityPath is invalid.");
            if (string.IsNullOrWhiteSpace(PackageIdentityPath) || Path.GetFileName(PackageIdentityPath) != "PublishedFileId.txt") throw new InvalidOperationException("packageIdentityPath is invalid.");
            if (string.IsNullOrWhiteSpace(StatePath) || Path.GetFileName(StatePath) != "publication-state.txt") throw new InvalidOperationException("statePath is invalid.");
            if (RequiredWorkshopItemId == 0) throw new InvalidOperationException("requiredWorkshopItemId is invalid.");
            if (Tags.Count == 0 || Tags.Count > 16 || Tags.Any(string.IsNullOrWhiteSpace)) throw new InvalidOperationException("tags are invalid.");
        }

        private static ulong ParseId(string? value, bool allowEmpty)
        {
            if (string.IsNullOrWhiteSpace(value)) return allowEmpty ? 0UL : throw new InvalidOperationException("A Workshop item ID is required.");
            if (!ulong.TryParse(value, out var parsed) || parsed == 0) throw new InvalidOperationException("Workshop item ID is invalid.");
            return parsed;
        }

        private static ERemoteStoragePublishedFileVisibility ParseVisibility(string? value) => value switch
        {
            "Public" => ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic,
            "Unlisted" => ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityUnlisted,
            "Private" => ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate,
            _ => throw new InvalidOperationException("visibility is invalid.")
        };

        private static void RequireFile(string path, string name)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) throw new InvalidOperationException(name + " does not exist.");
        }

        private static void RequireDirectory(string path, string name)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) throw new InvalidOperationException(name + " does not exist.");
        }
    }

    [DataContract]
    internal sealed class RequestJson
    {
        [DataMember(Name = "operation")]
        public string? operation;
        [DataMember(Name = "planSha256")]
        public string? planSha256;
        [DataMember(Name = "confirmation")]
        public string? confirmation;
        [DataMember(Name = "publishedFileId")]
        public string? publishedFileId;
        [DataMember(Name = "allowFirstPublication")]
        public bool allowFirstPublication;
        [DataMember(Name = "identityPath")]
        public string? identityPath;
        [DataMember(Name = "packageIdentityPath")]
        public string? packageIdentityPath;
        [DataMember(Name = "statePath")]
        public string? statePath;
        [DataMember(Name = "title")]
        public string? title;
        [DataMember(Name = "packagePath")]
        public string? packagePath;
        [DataMember(Name = "descriptionPath")]
        public string? descriptionPath;
        [DataMember(Name = "previewPath")]
        public string? previewPath;
        [DataMember(Name = "changeNote")]
        public string? changeNote;
        [DataMember(Name = "requiredWorkshopItemId")]
        public string? requiredWorkshopItemId;
        [DataMember(Name = "tags")]
        public string[]? tags;
        [DataMember(Name = "visibility")]
        public string? visibility;
    }

    [DataContract]
    internal sealed class Snapshot
    {
        [DataMember]
        public string Status = "idle";
        [DataMember]
        public string Stage = "idle";
        [DataMember]
        public string PlanSha256 = "";
        [DataMember]
        public ulong PublishedFileId;
        [DataMember]
        public bool UserNeedsLegalAgreement;
        [DataMember]
        public string Error = "";
        [DataMember]
        public string InstallFolder = "";
        [DataMember]
        public ulong InstallBytes;
        [DataMember]
        public string ItemState = "";
        [DataMember]
        public ulong CurrentUserSteamId;
        [DataMember]
        public string RemoteTitle = "";
        [DataMember]
        public string RemoteDescriptionSha256 = "";
        [DataMember]
        public int RemoteDescriptionUtf8Bytes;
        [DataMember]
        public string RemoteTags = "";
        [DataMember]
        public string RemoteMetadata = "";
        [DataMember]
        public string RemoteVisibility = "";
        [DataMember]
        public string RemotePreviewUrl = "";
        [DataMember]
        public ulong RemoteOwnerSteamId;
        [DataMember]
        public uint RemoteConsumerAppId;
        [DataMember]
        public ulong[] RemoteDependencies = Array.Empty<ulong>();

        public string ToJson() => Json.Write(this);
        public static Snapshot Idle() => new();
        public static Snapshot Pending(string stage, string plan, ulong id, bool legal = false, string state = "") => new() { Status = "pending", Stage = stage, PlanSha256 = plan, PublishedFileId = id, UserNeedsLegalAgreement = legal, ItemState = state };
        public static Snapshot Succeeded(string plan, ulong id) => new() { Status = "succeeded", Stage = "complete", PlanSha256 = plan, PublishedFileId = id };
        public static Snapshot LegalAgreementRequired(string plan, ulong id) => new() { Status = "legal-agreement-required", Stage = "legal-agreement-required", PlanSha256 = plan, PublishedFileId = id, UserNeedsLegalAgreement = true, Error = "Accept the Steam Workshop legal agreement, then rerun the exact reviewed update." };
        public static Snapshot Failed(string plan, ulong id, string error) => new() { Status = "failed", Stage = "failed", PlanSha256 = plan, PublishedFileId = id, Error = error };
        public static Snapshot Installed(ulong id, string folder, ulong bytes, uint state) => new() { Status = "installed", Stage = "installed", PublishedFileId = id, InstallFolder = folder, InstallBytes = bytes, ItemState = state.ToString() };
        public static Snapshot Queried(string plan, SteamUGCDetails_t details, string metadata, string previewUrl, ulong[] dependencies)
        {
            var description = details.m_rgchDescription ?? "";
            var bytes = Encoding.UTF8.GetBytes(description);
            string hash;
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
            return new Snapshot
            {
                Status = "queried",
                Stage = "queried",
                PlanSha256 = plan,
                PublishedFileId = details.m_nPublishedFileId.m_PublishedFileId,
                RemoteTitle = details.m_rgchTitle ?? "",
                RemoteDescriptionSha256 = hash,
                RemoteDescriptionUtf8Bytes = bytes.Length,
                RemoteTags = details.m_rgchTags ?? "",
                RemoteMetadata = metadata ?? "",
                RemoteVisibility = details.m_eVisibility.ToString(),
                RemotePreviewUrl = previewUrl ?? "",
                RemoteOwnerSteamId = details.m_ulSteamIDOwner,
                RemoteConsumerAppId = details.m_nConsumerAppID.m_AppId,
                RemoteDependencies = dependencies
            };
        }
    }

    private static class Json
    {
        public static T? Read<T>(string value)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
            return (T?)serializer.ReadObject(stream);
        }

        public static string Write<T>(T value)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using var stream = new MemoryStream();
            serializer.WriteObject(stream, value);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
