using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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
    IReadOnlyList<string> AdditionalPreviewUrls,
    string StateDigest)
{
    private const int MaximumCommunityPageBytes = 2 * 1024 * 1024;
    private const int MaximumPreviewBytes = 1024 * 1024;
    private const string BrowserUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36";

    public static async Task<WorkshopRemoteBaseline> CaptureAsync(
        JsonElement remote,
        CancellationToken cancellationToken)
    {
        var previewUrl = GatewayWorkshopClient.String(remote, "RemotePreviewUrl");
        if (string.IsNullOrWhiteSpace(previewUrl))
            throw new InvalidOperationException("Steam query did not return a primary preview URL.");
        using var http = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = false
        });
        http.DefaultRequestHeaders.UserAgent.ParseAdd("RimWorldModding.Mcp/0.1");
        var previewBytes = await DownloadBoundedAsync(
            http,
            previewUrl,
            MaximumPreviewBytes,
            "images.steamusercontent.com",
            "/ugc/",
            cancellationToken);
        var tags = GatewayWorkshopClient.String(remote, "RemoteTags").Split(',')
            .Select(value => value.Trim()).Where(value => value.Length > 0).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var dependencies = ReadUlongArray(remote, "RemoteDependencies");
        var appDependencies = ReadUlongArray(remote, "RemoteAppDependencies");
        var publishedFileId = GatewayWorkshopClient.UInt64(remote, "PublishedFileId").ToString();
        var additional = await ReadAdditionalPreviewsAsync(
            remote,
            http,
            publishedFileId,
            cancellationToken);
        return Create(
            publishedFileId,
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
            additional.Identities,
            additional.Urls);
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
            UpdatedUnixSeconds, Dependencies, AppDependencies, AdditionalPreviews, AdditionalPreviewUrls);
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
        IReadOnlyList<string> additionalPreviews,
        IReadOnlyList<string>? additionalPreviewUrls = null)
    {
        var sortedTags = tags.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var sortedDependencies = dependencies.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var sortedAppDependencies = appDependencies.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var orderedAdditional = additionalPreviews.ToArray();
        var orderedAdditionalUrls = additionalPreviewUrls?.ToArray() ?? [];
        if (orderedAdditionalUrls.Length != 0 && orderedAdditionalUrls.Length != orderedAdditional.Length)
            throw new InvalidOperationException("Additional-preview URL inventory does not match the retained preview inventory.");
        var canonical = string.Join("\n", new[]
        {
            publishedFileId, title, descriptionSha256, descriptionUtf8Bytes.ToString(),
            string.Join("|", sortedTags), metadata, visibility, previewSha256,
            ownerSteamId, consumerAppId.ToString(), contentBytes.ToString(), updatedUnixSeconds.ToString(),
            string.Join("|", sortedDependencies), string.Join("|", sortedAppDependencies),
            string.Join("|", orderedAdditional), string.Join("|", orderedAdditionalUrls)
        }) + "\n";
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        return new WorkshopRemoteBaseline(publishedFileId, title, descriptionSha256, descriptionUtf8Bytes,
            sortedTags, metadata, visibility, previewUrl, previewSha256, ownerSteamId, consumerAppId,
            contentBytes, updatedUnixSeconds, sortedDependencies, sortedAppDependencies,
            orderedAdditional, orderedAdditionalUrls, digest);
    }

    private static string[] ReadUlongArray(JsonElement root, string name)
    {
        if (!GatewayWorkshopClient.TryProperty(root, name, out var property) || property.ValueKind != JsonValueKind.Array) return [];
        return property.EnumerateArray().Select(item => item.GetUInt64().ToString())
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    private sealed record AdditionalPreviewInventory(string[] Identities, string[] Urls);

    private static async Task<AdditionalPreviewInventory> ReadAdditionalPreviewsAsync(
        JsonElement root,
        HttpClient http,
        string publishedFileId,
        CancellationToken cancellationToken)
    {
        if (!GatewayWorkshopClient.TryProperty(root, "RemoteAdditionalPreviews", out var property) ||
            property.ValueKind != JsonValueKind.Array) return new AdditionalPreviewInventory([], []);
        var items = property.EnumerateArray().ToArray();
        var imageItems = items.Where(item =>
            ReadProperty(item, "Type").Contains("Image", StringComparison.OrdinalIgnoreCase)).ToArray();
        string[] communityImageUrls = [];
        if (imageItems.Any(item => string.IsNullOrWhiteSpace(ReadProperty(item, "Url"))))
        {
            var communityUri = $"https://steamcommunity.com/sharedfiles/filedetails/?id={publishedFileId}";
            var htmlBytes = await DownloadBoundedAsync(
                http,
                communityUri,
                MaximumCommunityPageBytes,
                "steamcommunity.com",
                "/sharedfiles/filedetails/",
                cancellationToken);
            var html = Encoding.UTF8.GetString(htmlBytes);
            communityImageUrls = ParseCommunityImagePreviewUrls(
                html,
                imageItems.Count(item => !string.IsNullOrWhiteSpace(ReadProperty(item, "Url"))),
                imageItems.Length);
        }

        var selectedImageUrls = SelectAdditionalImageUrls(
            imageItems.Select(item => ReadProperty(item, "Url")).ToArray(),
            communityImageUrls);

        var previews = new List<string>();
        var urls = new List<string>();
        var imageIndex = 0;
        foreach (var item in items)
        {
            var type = ReadProperty(item, "Type");
            var url = ReadProperty(item, "Url");
            var contentIdentity = url;
            if (type.Contains("Image", StringComparison.OrdinalIgnoreCase))
            {
                url = selectedImageUrls[imageIndex];
                imageIndex++;
                contentIdentity = string.IsNullOrEmpty(url)
                    ? "missing"
                    : Convert.ToHexString(SHA256.HashData(
                        await DownloadBoundedAsync(
                            http,
                            url,
                            MaximumPreviewBytes,
                            "images.steamusercontent.com",
                            "/ugc/",
                            cancellationToken)));
            }
            previews.Add(string.Join("|", new[]
            {
                ReadProperty(item, "Index"), ReadProperty(item, "OriginalFileName"), type, contentIdentity
            }));
            urls.Add(url);
        }
        return new AdditionalPreviewInventory(previews.ToArray(), urls.ToArray());
    }

    internal static string[] SelectAdditionalImageUrls(
        IReadOnlyList<string> authenticatedUrls,
        IReadOnlyList<string> communityUrls)
    {
        if (authenticatedUrls.Count == 0)
        {
            if (communityUrls.Count != 0)
                throw new InvalidOperationException("Steam Community returned images for an empty authenticated preview inventory.");
            return [];
        }
        if (authenticatedUrls.Count > 10)
            throw new InvalidOperationException("The authenticated Steam image preview count is outside the reviewed bound.");
        if (authenticatedUrls.Any(string.IsNullOrWhiteSpace))
        {
            if (communityUrls.Count > authenticatedUrls.Count)
                throw new InvalidOperationException("Steam Community did not return the exact image preview inventory.");
            if (communityUrls.Count == authenticatedUrls.Count)
                return communityUrls.ToArray();

            var aligned = new string[authenticatedUrls.Count];
            var communityIndex = 0;
            for (var authenticatedIndex = 0; authenticatedIndex < authenticatedUrls.Count; authenticatedIndex++)
            {
                var authenticatedUrl = authenticatedUrls[authenticatedIndex];
                if (!string.IsNullOrWhiteSpace(authenticatedUrl))
                {
                    if (communityIndex >= communityUrls.Count ||
                        !SameSteamPreview(authenticatedUrl, communityUrls[communityIndex]))
                        throw new InvalidOperationException(
                            "Steam Community image previews do not unambiguously align with the authenticated inventory.");
                    aligned[authenticatedIndex] = communityUrls[communityIndex++];
                    continue;
                }

                var nextAuthenticatedAnchor = authenticatedUrls
                    .Skip(authenticatedIndex + 1)
                    .FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));
                if (nextAuthenticatedAnchor is not null &&
                    communityIndex < communityUrls.Count &&
                    SameSteamPreview(nextAuthenticatedAnchor, communityUrls[communityIndex]))
                {
                    aligned[authenticatedIndex] = "";
                    continue;
                }

                if (communityIndex < communityUrls.Count)
                {
                    aligned[authenticatedIndex] = communityUrls[communityIndex++];
                    continue;
                }

                throw new InvalidOperationException(
                    "Steam Community image previews do not unambiguously align with the authenticated inventory.");
            }

            if (communityIndex != communityUrls.Count)
                throw new InvalidOperationException(
                    "Steam Community image previews do not unambiguously align with the authenticated inventory.");
            return aligned;
        }
        return authenticatedUrls.ToArray();
    }

    private static bool SameSteamPreview(string left, string right)
    {
        static string Identity(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps ||
                !uri.Host.Equals("images.steamusercontent.com", StringComparison.OrdinalIgnoreCase) ||
                !uri.AbsolutePath.StartsWith("/ugc/", StringComparison.Ordinal))
                throw new InvalidOperationException("Steam image preview alignment encountered a URL outside the Steam CDN.");
            return uri.AbsolutePath.TrimEnd('/');
        }

        return string.Equals(Identity(left), Identity(right), StringComparison.Ordinal);
    }

    internal static async Task<byte[]> DownloadBoundedAsync(
        HttpClient http,
        string url,
        int maximumBytes,
        string requiredHost,
        string requiredPathPrefix,
        CancellationToken cancellationToken)
    {
        if (maximumBytes < 1 ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uri.Host, requiredHost, StringComparison.OrdinalIgnoreCase) ||
            !uri.AbsolutePath.StartsWith(requiredPathPrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Steam remote evidence URL is outside the admitted HTTPS origin.");

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (uri.Host.Equals("steamcommunity.com", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.UserAgent.ParseAdd(BrowserUserAgent);
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml");
        }
        else
        {
            request.Headers.Accept.ParseAdd("image/*");
        }
        using var response = await http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if ((int)response.StatusCode is >= 300 and < 400)
            throw new InvalidOperationException("Steam remote evidence request attempted a redirect.");
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is { } declaredLength && declaredLength > maximumBytes)
            throw new InvalidOperationException("Steam remote evidence exceeds the reviewed byte bound.");

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var destination = new MemoryStream();
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0) break;
            if (destination.Length + read > maximumBytes)
                throw new InvalidOperationException("Steam remote evidence exceeds the reviewed byte bound.");
            destination.Write(buffer, 0, read);
        }
        return destination.ToArray();
    }

    internal static string[] ParseCommunityImagePreviewUrls(string html, int expectedCount) =>
        ParseCommunityImagePreviewUrls(html, expectedCount, expectedCount);

    internal static string[] ParseCommunityImagePreviewUrls(
        string html,
        int minimumExpectedCount,
        int maximumExpectedCount)
    {
        if (minimumExpectedCount < 0 || maximumExpectedCount < 1 ||
            minimumExpectedCount > maximumExpectedCount || maximumExpectedCount > 10)
            throw new InvalidOperationException("The expected Steam image preview count range is outside the reviewed bound.");
        if (string.IsNullOrWhiteSpace(html) || html.Length > 2 * 1024 * 1024)
            throw new InvalidOperationException("The Steam Community item page is empty or exceeds the reviewed bound.");

        var inventory = Regex.Match(
            html,
            @"var\s+rgFullScreenshotURLs\s*=\s*\[(?<body>.*?)\]\s*;",
            RegexOptions.Singleline | RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));
        if (!inventory.Success)
            throw new InvalidOperationException("Steam Community omitted the exact image preview inventory.");

        var matches = Regex.Matches(
            inventory.Groups["body"].Value,
            "['\"]url['\"]\\s*:\\s*['\"](?<url>[^'\"]+)['\"]",
            RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));
        if (matches.Count < minimumExpectedCount || matches.Count > maximumExpectedCount)
            throw new InvalidOperationException(
                "Steam Community did not return the exact image preview inventory within the reviewed count range.");

        var urls = matches.Select(match => WebUtility.HtmlDecode(match.Groups["url"].Value)).ToArray();
        foreach (var url in urls)
        {
            if (url.Length > 4096 ||
                !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps ||
                !string.Equals(uri.Host, "images.steamusercontent.com", StringComparison.OrdinalIgnoreCase) ||
                !uri.AbsolutePath.StartsWith("/ugc/", StringComparison.Ordinal))
                throw new InvalidOperationException("Steam Community returned an image preview outside the Steam CDN.");
        }
        if (urls.Distinct(StringComparer.Ordinal).Count() != urls.Length)
            throw new InvalidOperationException("Steam Community returned duplicate image preview URLs.");
        return urls;
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
