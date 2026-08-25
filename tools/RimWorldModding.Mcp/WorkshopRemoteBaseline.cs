using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RimWorldModding.Mcp;

public sealed record WorkshopRemoteBaseline(
    string PublishedFileId,
    string Title,
    string DescriptionSha256,
    int DescriptionUtf8Bytes,
    IReadOnlyList<string> Tags,
    string Metadata,
    string Visibility,
    string PreviewUrl,
    string PreviewSha256,
    string OwnerSteamId,
    int ConsumerAppId,
    ulong ContentBytes,
    uint UpdatedUnixSeconds,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> AppDependencies,
    IReadOnlyList<string> AdditionalPreviews,
    string StateDigest)
{
    public static async Task<WorkshopRemoteBaseline> CaptureAsync(
        JsonElement remote,
        CancellationToken cancellationToken)
    {
        var previewUrl = GatewayWorkshopClient.String(remote, "RemotePreviewUrl");
        if (string.IsNullOrWhiteSpace(previewUrl))
            throw new InvalidOperationException("Steam query did not return a primary preview URL.");
        using var http = new HttpClient(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All });
        http.DefaultRequestHeaders.UserAgent.ParseAdd("RimWorldModding.Mcp/0.1");
        var previewBytes = await http.GetByteArrayAsync(previewUrl, cancellationToken);
        var tags = GatewayWorkshopClient.String(remote, "RemoteTags").Split(',')
            .Select(value => value.Trim()).Where(value => value.Length > 0).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var dependencies = ReadUlongArray(remote, "RemoteDependencies");
        var appDependencies = ReadUlongArray(remote, "RemoteAppDependencies");
        var additional = await ReadAdditionalPreviewsAsync(remote, http, cancellationToken);
        return Create(
            GatewayWorkshopClient.UInt64(remote, "PublishedFileId").ToString(),
            GatewayWorkshopClient.String(remote, "RemoteTitle"),
            GatewayWorkshopClient.String(remote, "RemoteDescriptionSha256"),
            ReadInt32(remote, "RemoteDescriptionUtf8Bytes"),
            tags,
            GatewayWorkshopClient.String(remote, "RemoteMetadata"),
            NormalizeVisibility(GatewayWorkshopClient.String(remote, "RemoteVisibility")),
            previewUrl,
            Convert.ToHexString(SHA256.HashData(previewBytes)),
            GatewayWorkshopClient.UInt64(remote, "RemoteOwnerSteamId").ToString(),
            checked((int)GatewayWorkshopClient.UInt64(remote, "RemoteConsumerAppId")),
            GatewayWorkshopClient.UInt64(remote, "RemoteContentBytes"),
            checked((uint)GatewayWorkshopClient.UInt64(remote, "RemoteUpdatedUnixSeconds")),
            dependencies,
            appDependencies,
            additional);
    }

    public static void AssertRuntimeIdentity(JsonElement status, ReleaseProfile profile)
    {
        if (GatewayWorkshopClient.UInt64(status, "CurrentUserSteamId").ToString() != profile.SteamUserId)
            throw new InvalidOperationException("The live RimWorld Steam user does not match the release profile.");
        if (!string.Equals(GatewayWorkshopClient.String(status, "RimWorldVersion"), profile.RimWorldRuntimeBuild, StringComparison.Ordinal))
            throw new InvalidOperationException("The live RimWorld runtime build does not match the release profile.");
    }

    public void AssertValid()
    {
        var rebuilt = Create(PublishedFileId, Title, DescriptionSha256, DescriptionUtf8Bytes, Tags, Metadata,
            Visibility, PreviewUrl, PreviewSha256, OwnerSteamId, ConsumerAppId, ContentBytes,
            UpdatedUnixSeconds, Dependencies, AppDependencies, AdditionalPreviews);
        if (!string.Equals(rebuilt.StateDigest, StateDigest, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Publication plan remote baseline digest is invalid.");
    }

    public void AssertMatches(WorkshopRemoteBaseline current)
    {
        AssertValid();
        current.AssertValid();
        if (!string.Equals(StateDigest, current.StateDigest, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Workshop metadata/content/dependency state drifted after release preparation.");
    }

    public void AssertItemIdentity(ReleaseProfile profile)
    {
        if (PublishedFileId != profile.PublishedFileId ||
            OwnerSteamId != profile.SteamUserId || ConsumerAppId != profile.SteamAppId)
            throw new InvalidOperationException("Authenticated Workshop item ID/owner/app identity does not match the release profile.");
    }

    public static IReadOnlyList<string> DescribeDiff(
        WorkshopRemoteBaseline baseline,
        ReleaseProfile profile,
        ReleaseCandidateStage candidate)
    {
        baseline.AssertValid();
        var desiredDescription = ReleaseCandidateBuilder.Hash(profile.Description);
        var desiredPreview = ReleaseCandidateBuilder.Hash(profile.Preview);
        return new[]
        {
            $"TITLE [{baseline.Title}] -> [{profile.Title}]",
            $"DESCRIPTION {baseline.DescriptionSha256} -> {desiredDescription}",
            $"TAGS [{string.Join(", ", baseline.Tags)}] -> [{string.Join(", ", profile.Tags.OrderBy(value => value, StringComparer.Ordinal))}]",
            $"VISIBILITY {baseline.Visibility} -> {profile.Visibility}",
            $"PREVIEW {baseline.PreviewSha256} -> {desiredPreview}",
            $"DEPENDENCIES [{string.Join(", ", baseline.Dependencies)}] -> [{string.Join(", ", profile.RequiredWorkshopItems.OrderBy(value => value, StringComparer.Ordinal))}]",
            $"APP DEPENDENCIES [{string.Join(", ", baseline.AppDependencies)}] -> [{string.Join(", ", profile.RequiredDlcAppIds.OrderBy(value => value, StringComparer.Ordinal))}]",
            $"CONTENT remote-bytes={baseline.ContentBytes},updated={baseline.UpdatedUnixSeconds} -> {candidate.Files.Count} files,digest={candidate.ContentDigest}",
            $"METADATA {baseline.Metadata} -> exact admitted publication-plan SHA-256",
            $"CHANGE NOTE -> {profile.ChangeNote}"
        };
    }

    internal static WorkshopRemoteBaseline Create(
        string publishedFileId,
        string title,
        string descriptionSha256,
        int descriptionUtf8Bytes,
        IReadOnlyList<string> tags,
        string metadata,
        string visibility,
        string previewUrl,
        string previewSha256,
        string ownerSteamId,
        int consumerAppId,
        ulong contentBytes,
        uint updatedUnixSeconds,
        IReadOnlyList<string> dependencies,
        IReadOnlyList<string> appDependencies,
        IReadOnlyList<string> additionalPreviews)
    {
        var sortedTags = tags.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var sortedDependencies = dependencies.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var sortedAppDependencies = appDependencies.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var sortedAdditional = additionalPreviews.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var canonical = string.Join("\n", new[]
        {
            publishedFileId, title, descriptionSha256, descriptionUtf8Bytes.ToString(),
            string.Join("|", sortedTags), metadata, visibility, previewSha256,
            ownerSteamId, consumerAppId.ToString(), contentBytes.ToString(), updatedUnixSeconds.ToString(),
            string.Join("|", sortedDependencies), string.Join("|", sortedAppDependencies), string.Join("|", sortedAdditional)
        }) + "\n";
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        return new WorkshopRemoteBaseline(publishedFileId, title, descriptionSha256, descriptionUtf8Bytes,
            sortedTags, metadata, visibility, previewUrl, previewSha256, ownerSteamId, consumerAppId,
            contentBytes, updatedUnixSeconds, sortedDependencies, sortedAppDependencies, sortedAdditional, digest);
    }

    private static string[] ReadUlongArray(JsonElement root, string name)
    {
        if (!GatewayWorkshopClient.TryProperty(root, name, out var property) || property.ValueKind != JsonValueKind.Array) return [];
        return property.EnumerateArray().Select(item => item.GetUInt64().ToString())
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    private static async Task<string[]> ReadAdditionalPreviewsAsync(
        JsonElement root,
        HttpClient http,
        CancellationToken cancellationToken)
    {
        if (!GatewayWorkshopClient.TryProperty(root, "RemoteAdditionalPreviews", out var property) ||
            property.ValueKind != JsonValueKind.Array) return [];
        var previews = new List<string>();
        foreach (var item in property.EnumerateArray())
        {
            var type = ReadProperty(item, "Type");
            var url = ReadProperty(item, "Url");
            var contentIdentity = url;
            if (type.Contains("Image", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(url))
                    throw new InvalidOperationException("Steam query returned an image preview without a URL.");
                contentIdentity = Convert.ToHexString(SHA256.HashData(
                    await http.GetByteArrayAsync(url, cancellationToken)));
            }
            previews.Add(string.Join("|", new[]
            {
                ReadProperty(item, "Index"), ReadProperty(item, "OriginalFileName"), type, contentIdentity
            }));
        }
        return previews.OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    private static int ReadInt32(JsonElement root, string name)
    {
        if (!GatewayWorkshopClient.TryProperty(root, name, out var property) || !property.TryGetInt32(out var value))
            throw new InvalidOperationException($"Steam query omitted {name}.");
        return value;
    }

    private static string ReadProperty(JsonElement root, string name)
    {
        if (!GatewayWorkshopClient.TryProperty(root, name, out var property)) return "";
        return property.ValueKind == JsonValueKind.String ? property.GetString() ?? "" : property.ToString();
    }

    private static string NormalizeVisibility(string value) =>
        value.EndsWith("Public", StringComparison.Ordinal) ? "Public" :
        value.EndsWith("Unlisted", StringComparison.Ordinal) ? "Unlisted" :
        value.EndsWith("Private", StringComparison.Ordinal) ? "Private" : value;
}
