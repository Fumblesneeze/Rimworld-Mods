using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace RimWorldModding.Mcp;

public sealed record SteamDepotIdentity(string DepotId, string ManifestId, string Sha256);
public sealed record SteamGameTarget(string Id, string Version, string FullVersion, string CacheDirectory,
    string InventorySha256, IReadOnlyList<SteamDepotIdentity> Depots);
public sealed record SteamGameTargetCatalog(string Schema, IReadOnlyList<SteamGameTarget> Targets);
public sealed record SteamGameInventory(string Schema, IReadOnlyList<CandidateFile> Files);

internal static class SteamGameTargets
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { WriteIndented = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    internal static string DefaultCacheRoot => Environment.GetEnvironmentVariable("RIMWORLD_RELEASE_CACHE") ?? @"F:\RimWorldVersions";

    internal static IReadOnlyList<SteamGameTarget> SelectMatrix(IReadOnlyList<SteamGameTarget> catalog,
        IReadOnlyList<string> requestedIds, string developmentTarget)
    {
        if (requestedIds.Count == 0 || requestedIds.Distinct(StringComparer.Ordinal).Count() != requestedIds.Count ||
            !requestedIds.Contains(developmentTarget, StringComparer.Ordinal))
            throw new InvalidOperationException("Supported targets must be unique and include the development target.");
        var selected = requestedIds.Select(id => catalog.SingleOrDefault(target => target.Id == id) ??
            throw new InvalidOperationException("Undeclared RimWorld target: " + id)).ToArray();
        if (selected.Select(target => target.Version).Distinct(StringComparer.Ordinal).Count() != selected.Length)
            throw new InvalidOperationException("Two compile targets cannot share one compatibility folder.");
        return selected.OrderBy(target => target.Version, StringComparer.Ordinal).ToArray();
    }

    internal static IReadOnlyList<SteamGameTarget> LoadCatalog(string repositoryRoot)
    {
        var path = RepositoryRoot.ContainedPath(repositoryRoot, "release/rimworld-targets.json");
        if (!File.Exists(path) || new FileInfo(path).Length > 1024 * 1024)
            throw new InvalidOperationException("Missing or oversized committed RimWorld target catalog.");
        var catalog = JsonSerializer.Deserialize<SteamGameTargetCatalog>(File.ReadAllText(path), Json);
        if (catalog?.Schema != "RimWorldSteamTargets/v1" || catalog.Targets is null || catalog.Targets.Count == 0 ||
            catalog.Targets.Select(target => target.Id).Distinct(StringComparer.Ordinal).Count() != catalog.Targets.Count)
            throw new InvalidOperationException("Invalid or duplicate target catalog identity.");
        foreach (var target in catalog.Targets) ValidateDeclaration(target);
        return catalog.Targets;
    }

    internal static void ValidateDeclaration(SteamGameTarget target)
    {
        if (target is null || !Regex.IsMatch(target.Id ?? "", "^[a-z0-9][a-z0-9.-]{0,99}$") ||
            target.Version is not ("1.6" or "1.7") ||
            !Regex.IsMatch(target.FullVersion ?? "", "^" + Regex.Escape(target.Version) + @"\.[0-9]+ rev[0-9]+$") ||
            !Regex.IsMatch(target.InventorySha256 ?? "", "^[A-F0-9]{64}$") || !SafeRelative(target.CacheDirectory) ||
            target.Depots is null || target.Depots.Count == 0 ||
            target.Depots.Select(depot => depot.DepotId).Distinct(StringComparer.Ordinal).Count() != target.Depots.Count ||
            target.Depots.Any(depot => !uint.TryParse(depot.DepotId, out var id) || id == 0 ||
                !ulong.TryParse(depot.ManifestId, out var manifest) || manifest == 0 ||
                !Regex.IsMatch(depot.Sha256 ?? "", "^[A-F0-9]{64}$")))
            throw new InvalidOperationException("RimWorld target declaration has invalid identity, paths, manifests or hashes.");
    }

    internal static SteamGameTarget Snapshot(string repositoryRoot, string targetId, string cacheRoot, string targetDirectory)
    {
        cacheRoot = Path.GetFullPath(cacheRoot);
        targetDirectory = Path.GetFullPath(targetDirectory);
        SteamDepotAcquisition.RequireExternal(repositoryRoot, cacheRoot);
        SteamDepotAcquisition.RequireExternal(repositoryRoot, targetDirectory);
        var relative = Path.GetRelativePath(cacheRoot, targetDirectory).Replace('\\', '/');
        if (!SafeRelative(relative)) throw new ArgumentException("Target directory must be inside the external cache root.");
        SteamDepotAcquisition.ValidateOwnedTarget(targetDirectory);
        var installation = GameInstallation.Inspect(targetDirectory);
        var manifestRoot = Path.Combine(targetDirectory, ".DepotDownloader");
        var depots = Directory.GetFiles(manifestRoot, "*.manifest").Select(path =>
        {
            var match = Regex.Match(Path.GetFileName(path), @"^(?<depot>[0-9]+)_(?<manifest>[0-9]+)\.manifest$");
            if (!match.Success) throw new InvalidOperationException("Malformed Steam manifest cache identity.");
            return new SteamDepotIdentity(match.Groups["depot"].Value, match.Groups["manifest"].Value, ReleaseCandidateBuilder.Hash(path));
        }).OrderBy(depot => depot.DepotId, StringComparer.Ordinal).ToArray();
        var files = ImmutableFiles(targetDirectory)
            .Select(path => new { Path = path, Relative = Path.GetRelativePath(targetDirectory, path).Replace('\\', '/') })
            .Select(file => new CandidateFile(file.Relative, new FileInfo(file.Path).Length, ReleaseCandidateBuilder.Hash(file.Path)))
            .OrderBy(file => file.Path, StringComparer.Ordinal).ToArray();
        var index = Path.Combine(targetDirectory, "game-files.json");
        DurableFile.WriteAllText(index, JsonSerializer.Serialize(new SteamGameInventory("RimWorldGameFiles/v1", files), Json));
        var target = new SteamGameTarget(targetId, installation.MajorVersion, installation.Version, relative,
            ReleaseCandidateBuilder.Hash(index), depots);
        ValidateDeclaration(target);
        return target;
    }

    internal static GameInstallation Validate(string repositoryRoot, SteamGameTarget target, string cacheRoot)
    {
        ValidateDeclaration(target);
        cacheRoot = Path.GetFullPath(cacheRoot);
        var directory = Path.GetFullPath(Path.Combine(cacheRoot, target.CacheDirectory));
        SteamDepotAcquisition.RequireExternal(repositoryRoot, directory);
        SteamDepotAcquisition.ValidateOwnedTarget(directory);
        var index = Path.Combine(directory, "game-files.json");
        if (!File.Exists(index) || new FileInfo(index).Length > 16 * 1024 * 1024 || ReleaseCandidateBuilder.Hash(index) != target.InventorySha256)
            throw new InvalidOperationException("Approved game inventory changed or is missing.");
        var inventory = JsonSerializer.Deserialize<SteamGameInventory>(File.ReadAllText(index), Json);
        if (inventory?.Schema != "RimWorldGameFiles/v1" || inventory.Files is null || inventory.Files.Count is 0 or > 100000 ||
            inventory.Files.Select(file => file.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != inventory.Files.Count)
            throw new InvalidOperationException("Approved game inventory is invalid.");
        var actualPaths = ImmutableFiles(directory).Select(path => Path.GetRelativePath(directory, path).Replace('\\', '/'));
        if (!new HashSet<string>(actualPaths, StringComparer.OrdinalIgnoreCase).SetEquals(inventory.Files.Select(file => file.Path)))
            throw new InvalidOperationException("Actual game file inventory differs from the approved target.");
        foreach (var file in inventory.Files)
        {
            if (!SafeRelative(file.Path)) throw new InvalidOperationException("Approved game inventory has an unsafe path.");
            var path = Path.Combine(directory, file.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path) || new FileInfo(path).Length != file.Bytes || ReleaseCandidateBuilder.Hash(path) != file.Sha256)
                throw new InvalidOperationException("Approved game file changed: " + file.Path);
        }
        foreach (var depot in target.Depots)
        {
            var path = Path.Combine(directory, ".DepotDownloader", $"{depot.DepotId}_{depot.ManifestId}.manifest");
            if (!File.Exists(path) || ReleaseCandidateBuilder.Hash(path) != depot.Sha256)
                throw new InvalidOperationException("Approved Steam manifest changed: " + depot.DepotId);
        }
        var installation = GameInstallation.Inspect(directory);
        if (installation.Version != target.FullVersion || installation.MajorVersion != target.Version)
            throw new InvalidOperationException("Approved game version changed.");
        return installation;
    }

    private static IEnumerable<string> ImmutableFiles(string directory) => Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
        .Where(path =>
        {
            var relative = Path.GetRelativePath(directory, path).Replace('\\', '/');
            return !relative.StartsWith(".DepotDownloader/", StringComparison.OrdinalIgnoreCase) &&
                relative is not (".rimworld-release-cache" or "game-files.json") &&
                !relative.StartsWith("Mods/", StringComparison.OrdinalIgnoreCase);
        });

    private static bool SafeRelative(string? path) => !string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path) &&
        !path.Contains('\\') && !path.Contains(':') && path.Split('/').All(part => part is not ("" or "." or ".."));

    internal static AdapterOperationResult SnapshotResult(string repositoryRoot, string targetId, string cacheRoot, string targetDirectory) =>
        new("game_target_snapshot", 0, 0, JsonSerializer.Serialize(Snapshot(repositoryRoot, targetId, cacheRoot, targetDirectory), Json), "", null);
}
