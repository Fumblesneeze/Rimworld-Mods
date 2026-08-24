using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RimWorldModding.Mcp;

public static class ReleaseEnvironmentValidator
{
    private const string RimWorldRoot = @"F:\Steam\steamapps\common\RimWorld";

    public static void Validate(ReleaseProfile profile)
    {
        var versionPath = Path.Combine(RimWorldRoot, "Version.txt");
        RequireExactText(versionPath, profile.RimWorldBuild, "RimWorld Version.txt");

        var manifestPath = Path.Combine(RimWorldRoot, "..", "..", $"appmanifest_{profile.SteamAppId}.acf");
        if (!File.Exists(manifestPath))
            throw new InvalidOperationException($"Steam app manifest is missing: {Path.GetFullPath(manifestPath)}");
        var match = Regex.Match(File.ReadAllText(manifestPath), "\\\"buildid\\\"\\s*\\\"(?<id>[0-9]+)\\\"");
        if (!match.Success || !string.Equals(match.Groups["id"].Value, profile.SteamBuildId, StringComparison.Ordinal))
            throw new InvalidOperationException("Installed Steam depot build does not match the pinned release profile.");

        var managed = Path.Combine(RimWorldRoot, "RimWorldWin64_Data", "Managed", "Assembly-CSharp.dll");
        if (!File.Exists(managed) ||
            !string.Equals(ReleaseCandidateBuilder.Hash(managed), profile.ManagedAssemblySha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Installed Assembly-CSharp.dll does not match the pinned release profile.");
        ReleasePackageValidator.ValidatePlatformAssemblyInventory(Path.GetDirectoryName(managed)!);
    }

    private static void RequireExactText(string path, string expected, string label)
    {
        if (!File.Exists(path) || !string.Equals(File.ReadAllText(path).Trim(), expected, StringComparison.Ordinal))
            throw new InvalidOperationException($"{label} does not match the pinned release profile.");
    }
}

public static class ReleaseChangeNotePolicy
{
    private static readonly HashSet<string> Generic = new(StringComparer.OrdinalIgnoreCase)
    {
        "update", "updated", "changes", "change", "bug fix", "bug fixes", "fixes", "misc changes"
    };

    public static void Validate(ReleaseProfile profile)
    {
        var note = profile.ChangeNote.Trim();
        if (note.Length < 8 || Generic.Contains(note.TrimEnd('.', '!', ':', ';')))
            throw new InvalidOperationException("Release changeNote must be a specific player-facing note, not blank or generic.");
        if (profile.PublishedFileId is not null)
        {
            if (string.IsNullOrWhiteSpace(profile.PreviousChangeNote))
                throw new InvalidOperationException("An update profile must pin the previous Workshop change note.");
            if (string.Equals(note, profile.PreviousChangeNote.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Release changeNote must differ from the previous Workshop change note.");
        }
    }
}

public static class WorkshopChangeHistoryVerifier
{
    private const string BrowserUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36";
    public sealed record Verification(string Status, string ChangeNote, string? Url);

    public static async Task ValidatePreviousAsync(ReleaseProfile profile, CancellationToken cancellationToken)
    {
        if (profile.PublishedFileId is null) return;
        if (profile.Visibility == "Private")
            throw new InvalidOperationException(
                "An existing private Workshop item cannot be updated because its previous change note cannot be independently verified; publish it as public or unlisted first.");
        var expected = profile.PreviousChangeNote ??
                       throw new InvalidOperationException("An existing public Workshop item requires previousChangeNote.");
        var url = $"https://steamcommunity.com/sharedfiles/filedetails/changelog/{profile.PublishedFileId}";
        var latest = ParseLatest(await DownloadBoundedAsync(url, cancellationToken));
        if (!string.Equals(latest, Normalize(expected), StringComparison.Ordinal))
            throw new InvalidOperationException("The pinned previousChangeNote is not the latest Workshop change-history entry.");
    }

    public static async Task<Verification> VerifyPublishedAsync(
        ReleaseProfile profile,
        string publishedFileId,
        CancellationToken cancellationToken)
    {
        if (profile.Visibility == "Private")
            return new Verification("steam-callback-confirmed-private-history-not-publicly-readable", profile.ChangeNote, null);
        var url = $"https://steamcommunity.com/sharedfiles/filedetails/changelog/{publishedFileId}";
        var deadline = DateTimeOffset.UtcNow.AddMinutes(2);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var latest = ParseLatest(await DownloadBoundedAsync(url, cancellationToken));
            if (string.Equals(latest, Normalize(profile.ChangeNote), StringComparison.Ordinal))
                return new Verification("latest-public-change-note-verified", profile.ChangeNote, url);
            await Task.Delay(3000, cancellationToken);
        }
        throw new TimeoutException("The submitted change note did not become the latest public Workshop history entry.");
    }

    internal static string ParseLatest(string html)
    {
        var match = Regex.Match(
            html,
            "<div[^>]*class=\\\"[^\\\"]*changeLogCtn[^\\\"]*\\\"[^>]*>[\\s\\S]*?<p\\s+id=\\\"[^\\\"]+\\\"[^>]*>(?<note>[\\s\\S]*?)</p>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success) throw new InvalidOperationException("Workshop change history did not expose a latest entry.");
        return Normalize(match.Groups["note"].Value);
    }

    private static string Normalize(string value)
    {
        var decoded = System.Net.WebUtility.HtmlDecode(value);
        return Regex.Replace(Regex.Replace(decoded, "<[^>]+>", " "), "\\s+", " ").Trim();
    }

    private static async Task<string> DownloadBoundedAsync(string url, CancellationToken cancellationToken)
    {
        const int maximumBytes = 2 * 1024 * 1024;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        using var http = new HttpClient(new SocketsHttpHandler { AutomaticDecompression = System.Net.DecompressionMethods.All });
        http.DefaultRequestHeaders.UserAgent.ParseAdd(BrowserUserAgent);
        HttpResponseMessage? response = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            response?.Dispose();
            response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (response.StatusCode != System.Net.HttpStatusCode.TooManyRequests) break;
            if (attempt == 2)
                throw new HttpRequestException("Workshop change history remained rate limited after three bounded attempts.");
            var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(attempt + 1);
            await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(retryAfter.TotalSeconds, 1, 5)), timeout.Token);
        }
        using (response)
        {
            response!.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is > maximumBytes)
            throw new InvalidOperationException("Workshop change history exceeded 2 MiB.");
        await using var input = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var output = new MemoryStream();
        var buffer = new byte[16384];
        while (true)
        {
            var count = await input.ReadAsync(buffer, timeout.Token);
            if (count == 0) break;
            if (output.Length + count > maximumBytes)
                throw new InvalidOperationException("Workshop change history exceeded 2 MiB.");
            output.Write(buffer, 0, count);
        }
        return System.Text.Encoding.UTF8.GetString(output.ToArray());
        }
    }
}

public static class ReleasePackageValidator
{
    private static readonly string[] ForbiddenAssemblyPrefixes =
    {
        "RimWorldDevGateway", "NUnit", "testhost", "Microsoft.TestPlatform", "Mono.CSharp"
    };

    private static readonly HashSet<string> ForbiddenBundledAssemblies = new(StringComparer.OrdinalIgnoreCase)
    {
        "0Harmony", "Assembly-CSharp", "UnityEngine", "UnityEngine.CoreModule", "Mono.CSharp"
    };

    private static readonly HashSet<string> PlatformAssemblyReferences = new(StringComparer.Ordinal)
    {
        // Exact simple-name inventory from the pinned RimWorld 1.6 Managed directory.
        // Prefixes are deliberately not trusted: adding a runtime assembly requires a reviewed inventory update.
        "mscorlib",
        "netstandard",
        "Assembly-CSharp",
        "Assembly-CSharp-firstpass",
        "com.rlabrecque.steamworks.net",
        "ISharpZipLib",
        "Mono.Security",
        "NAudio",
        "NVorbis",
        "System",
        "System.ComponentModel.Composition",
        "System.Configuration",
        "System.Core",
        "System.Data",
        "System.Data.DataSetExtensions",
        "System.Drawing",
        "System.EnterpriseServices",
        "System.IO.Compression",
        "System.IO.Compression.FileSystem",
        "System.Net.Http",
        "System.Numerics",
        "System.Runtime",
        "System.Runtime.Serialization",
        "System.Security",
        "System.ServiceModel.Internals",
        "System.Transactions",
        "System.Xml",
        "System.Xml.Linq",
        "Unity.AI.Navigation",
        "Unity.Burst",
        "Unity.Burst.Unsafe",
        "Unity.Collections",
        "Unity.Collections.LowLevel.ILSupport",
        "Unity.Mathematics",
        "Unity.MemoryProfiler",
        "Unity.Profiling.Core",
        "Unity.TextMeshPro",
        "UnityEngine",
        "UnityEngine.AccessibilityModule",
        "UnityEngine.AIModule",
        "UnityEngine.AndroidJNIModule",
        "UnityEngine.AnimationModule",
        "UnityEngine.ARModule",
        "UnityEngine.AssetBundleModule",
        "UnityEngine.AudioModule",
        "UnityEngine.ClothModule",
        "UnityEngine.ClusterInputModule",
        "UnityEngine.ClusterRendererModule",
        "UnityEngine.ContentLoadModule",
        "UnityEngine.CoreModule",
        "UnityEngine.CrashReportingModule",
        "UnityEngine.DirectorModule",
        "UnityEngine.DSPGraphModule",
        "UnityEngine.GameCenterModule",
        "UnityEngine.GIModule",
        "UnityEngine.GridModule",
        "UnityEngine.HotReloadModule",
        "UnityEngine.ImageConversionModule",
        "UnityEngine.IMGUIModule",
        "UnityEngine.InputLegacyModule",
        "UnityEngine.InputModule",
        "UnityEngine.JSONSerializeModule",
        "UnityEngine.LocalizationModule",
        "UnityEngine.NVIDIAModule",
        "UnityEngine.ParticleSystemModule",
        "UnityEngine.PerformanceReportingModule",
        "UnityEngine.Physics2DModule",
        "UnityEngine.PhysicsModule",
        "UnityEngine.ProfilerModule",
        "UnityEngine.PropertiesModule",
        "UnityEngine.RuntimeInitializeOnLoadManagerInitializerModule",
        "UnityEngine.ScreenCaptureModule",
        "UnityEngine.SharedInternalsModule",
        "UnityEngine.SpriteMaskModule",
        "UnityEngine.SpriteShapeModule",
        "UnityEngine.StreamingModule",
        "UnityEngine.SubstanceModule",
        "UnityEngine.SubsystemsModule",
        "UnityEngine.TerrainModule",
        "UnityEngine.TerrainPhysicsModule",
        "UnityEngine.TextCoreFontEngineModule",
        "UnityEngine.TextCoreTextEngineModule",
        "UnityEngine.TextRenderingModule",
        "UnityEngine.TilemapModule",
        "UnityEngine.TLSModule",
        "UnityEngine.UI",
        "UnityEngine.UIElementsModule",
        "UnityEngine.UIModule",
        "UnityEngine.UmbraModule",
        "UnityEngine.UnityAnalyticsCommonModule",
        "UnityEngine.UnityAnalyticsModule",
        "UnityEngine.UnityConnectModule",
        "UnityEngine.UnityCurlModule",
        "UnityEngine.UnityTestProtocolModule",
        "UnityEngine.UnityWebRequestAssetBundleModule",
        "UnityEngine.UnityWebRequestAudioModule",
        "UnityEngine.UnityWebRequestModule",
        "UnityEngine.UnityWebRequestTextureModule",
        "UnityEngine.UnityWebRequestWWWModule",
        "UnityEngine.VehiclesModule",
        "UnityEngine.VFXModule",
        "UnityEngine.VideoModule",
        "UnityEngine.VirtualTexturingModule",
        "UnityEngine.VRModule",
        "UnityEngine.WindModule",
        "UnityEngine.XRModule"
    };

    public static ReleaseCandidateStage Validate(ReleaseProfile profile, ReleaseCandidateStage candidate)
    {
        var project = XDocument.Load(profile.Project);
        var assemblyName = project.Descendants()
            .FirstOrDefault(node => node.Name.LocalName == "AssemblyName")?.Value.Trim();
        if (string.IsNullOrWhiteSpace(assemblyName))
            throw new InvalidOperationException("Release project does not declare AssemblyName.");

        var dlls = candidate.Files.Where(file => Path.GetExtension(file.Path).Equals(".dll", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (dlls.Length != 1 || !string.Equals(Path.GetFileNameWithoutExtension(dlls[0].Path), assemblyName, StringComparison.Ordinal))
            throw new InvalidOperationException("A product package must contain exactly its one declared product assembly.");
        foreach (var file in candidate.Files)
        {
            var simple = Path.GetFileNameWithoutExtension(file.Path);
            if (Path.GetExtension(file.Path).Equals(".dll", StringComparison.OrdinalIgnoreCase) &&
                (ForbiddenBundledAssemblies.Contains(simple) || ForbiddenAssemblyPrefixes.Any(prefix => simple.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))))
                throw new InvalidOperationException($"Forbidden bundled assembly: {file.Path}");
        }

        ValidateAssemblyReferences(
            Path.Combine(candidate.PackagePath, dlls[0].Path.Replace('/', Path.DirectorySeparatorChar)),
            profile,
            project);
        ValidateAbout(profile, candidate.PackagePath, project);
        return candidate;
    }

    private static void ValidateAssemblyReferences(string path, ReleaseProfile profile, XDocument project)
    {
        var includeHarmony = string.Equals(
            project.Descendants().FirstOrDefault(node => node.Name.LocalName == "IncludeHarmony")?.Value.Trim(),
            "true",
            StringComparison.OrdinalIgnoreCase);
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        if (!pe.HasMetadata) throw new InvalidOperationException("Product DLL does not contain managed metadata.");
        var metadata = pe.GetMetadataReader();
        foreach (var handle in metadata.AssemblyReferences)
        {
            var reference = metadata.GetString(metadata.GetAssemblyReference(handle).Name);
            if (ForbiddenAssemblyPrefixes.Any(prefix => reference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Product DLL has a forbidden AssemblyRef: {reference}");
            if (string.Equals(reference, "0Harmony", StringComparison.OrdinalIgnoreCase) && !includeHarmony)
                throw new InvalidOperationException("Product DLL references 0Harmony but the project does not declare IncludeHarmony=true.");
            if (!IsPlatformReference(reference) &&
                !(includeHarmony && string.Equals(reference, "0Harmony", StringComparison.OrdinalIgnoreCase)) &&
                !profile.HardRuntimeAssemblyReferences.Contains(reference, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Product DLL has undeclared runtime AssemblyRef '{reference}'. Optional integrations must be reflection/Def-resolved and absent-safe; hard references must be explicitly declared.");
        }
    }

    internal static bool IsPlatformReference(string reference) => PlatformAssemblyReferences.Contains(reference);

    internal static void ValidatePlatformAssemblyInventory(string managedDirectory)
    {
        var actual = Directory.GetFiles(managedDirectory, "*.dll", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.Ordinal);
        if (!actual.SetEquals(PlatformAssemblyReferences))
        {
            var missing = actual.Except(PlatformAssemblyReferences, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal);
            var stale = PlatformAssemblyReferences.Except(actual, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal);
            throw new InvalidOperationException(
                "Pinned RimWorld Managed AssemblyRef inventory drifted. Missing declarations: [" +
                string.Join(", ", missing) + "]; no longer present: [" + string.Join(", ", stale) + "].");
        }
    }

    private static void ValidateAbout(ReleaseProfile profile, string packagePath, XDocument project)
    {
        var aboutPath = Path.Combine(packagePath, "About", "About.xml");
        if (!File.Exists(aboutPath)) throw new InvalidOperationException("Release package is missing About/About.xml.");
        var about = XDocument.Load(aboutPath);
        var root = about.Root ?? throw new InvalidOperationException("About.xml has no root element.");
        string? Value(string name) => root.Element(name)?.Value.Trim();
        if (Value("name") != profile.Title || Value("author") != profile.Author ||
            !string.Equals(Value("packageId"), profile.PackageId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("About.xml identity does not match the release profile.");
        var versions = root.Element("supportedVersions")?.Elements("li").Select(node => node.Value.Trim()).ToArray() ?? [];
        if (!versions.SequenceEqual([profile.RimWorldVersion], StringComparer.Ordinal))
            throw new InvalidOperationException("About.xml supportedVersions does not exactly match the release profile.");

        var expectedLoadAfter = project.Descendants()
            .Where(node => node.Name.LocalName == "RimWorldLoadAfter")
            .Select(node => node.Attribute("Include")?.Value.Trim() ?? "")
            .Where(value => value.Length > 0).ToList();
        var explicitDependencies = project.Descendants()
            .Where(node => node.Name.LocalName == "RimWorldSteamModDependency")
            .Select(node => node.Attribute("Include")?.Value.Trim() ?? "")
            .Where(value => value.Length > 0).ToArray();
        var includeHarmony = project.Descendants().FirstOrDefault(node => node.Name.LocalName == "IncludeHarmony")?.Value.Trim();
        var expectedHardDependencies = explicitDependencies.ToList();
        if (string.Equals(includeHarmony, "true", StringComparison.OrdinalIgnoreCase))
            expectedHardDependencies.Add("brrainz.harmony");
        var actualHardDependencies = root.Element("modDependencies")?.Elements("li")
            .Select(item => item.Element("packageId")?.Value.Trim() ?? "")
            .Where(value => value.Length > 0).ToArray() ?? [];
        if (!actualHardDependencies.SequenceEqual(expectedHardDependencies, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("About.xml hard dependencies do not match the release project.");

        var hardWorkshopIds = root.Element("modDependencies")?.Elements("li")
            .Select(item => item.Element("steamWorkshopUrl")?.Value.Trim() ?? "")
            .Select(url => Regex.Match(url, "(?<id>[1-9][0-9]{5,19})$").Groups["id"].Value)
            .Where(id => id.Length > 0).Distinct(StringComparer.Ordinal).ToArray() ?? [];
        if (hardWorkshopIds.Any(id => !profile.RequiredWorkshopItems.Contains(id, StringComparer.Ordinal)))
            throw new InvalidOperationException("Every hard Workshop dependency must also be declared as a Steam required item.");

        if (string.Equals(includeHarmony, "true", StringComparison.OrdinalIgnoreCase)) expectedLoadAfter.Insert(0, "brrainz.harmony");
        expectedLoadAfter.InsertRange(0, explicitDependencies);
        var actualLoadAfter = root.Element("loadAfter")?.Elements("li").Select(node => node.Value.Trim()).ToArray() ?? [];
        if (!actualLoadAfter.SequenceEqual(expectedLoadAfter, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("About.xml loadAfter order does not match the release project.");

        var idPath = Path.Combine(packagePath, "About", "PublishedFileId.txt");
        if (profile.PublishedFileId is null)
        {
            if (File.Exists(idPath)) throw new InvalidOperationException("First-publication package unexpectedly contains PublishedFileId.txt.");
        }
        else if (!File.Exists(idPath) || File.ReadAllText(idPath).Trim() != profile.PublishedFileId)
        {
            throw new InvalidOperationException("Package PublishedFileId.txt does not match the release profile.");
        }
    }
}
