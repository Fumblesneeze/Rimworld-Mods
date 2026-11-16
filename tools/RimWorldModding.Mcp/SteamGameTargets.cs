using System.Text.Json;
using System.Text.Json.Serialization;

namespace RimWorldModding.Mcp;

public sealed record SteamGameTarget(string Id, string Version, string CacheDirectory, string Branch);
public sealed record SteamGameTargetCatalog(string Schema, IReadOnlyList<SteamGameTarget> Targets);

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
            throw new InvalidOperationException("Supported major/minor targets must be unique and include the development target.");
        var selected = requestedIds.Select(id => catalog.SingleOrDefault(target => target.Id == id) ??
            throw new InvalidOperationException("Undeclared RimWorld major/minor target: " + id)).ToArray();
        foreach (var target in selected) ValidateDeclaration(target);
        if (selected.Select(target => target.Version).Distinct(StringComparer.Ordinal).Count() != selected.Length)
            throw new InvalidOperationException("Two targets cannot share one compatibility folder.");
        return selected.OrderBy(target => target.Version, StringComparer.Ordinal).ToArray();
    }

    internal static IReadOnlyList<SteamGameTarget> LoadCatalog(string repositoryRoot)
    {
        var path = RepositoryRoot.ContainedPath(repositoryRoot, "release/rimworld-targets.json");
        if (!File.Exists(path) || new FileInfo(path).Length > 1024 * 1024)
            throw new InvalidOperationException("Missing or oversized RimWorld target catalog.");
        var catalog = JsonSerializer.Deserialize<SteamGameTargetCatalog>(File.ReadAllText(path), Json);
        if (catalog?.Schema != "RimWorldGameTargets/v2" || catalog.Targets is null || catalog.Targets.Count == 0 ||
            catalog.Targets.Select(target => target.Id).Distinct(StringComparer.Ordinal).Count() != catalog.Targets.Count)
            throw new InvalidOperationException("Invalid or duplicate major/minor target catalog.");
        foreach (var target in catalog.Targets) ValidateDeclaration(target);
        return catalog.Targets;
    }

    internal static void ValidateDeclaration(SteamGameTarget target)
    {
        if (target is null || target.Version is not ("1.6" or "1.7") || target.Id != target.Version ||
            !SafeRelative(target.CacheDirectory) ||
            target.Branch != "public" && target.Branch != "unstable" && target.Branch != "version-" + target.Version)
            throw new InvalidOperationException("Targets must select only major/minor versions and a rolling Steam branch. Game revision/build/manifest/hash pins are forbidden.");
    }

    internal static GameInstallation Validate(string repositoryRoot, SteamGameTarget target, string cacheRoot)
    {
        ValidateDeclaration(target);
        var directory = Path.GetFullPath(Path.Combine(Path.GetFullPath(cacheRoot), target.CacheDirectory));
        SteamDepotAcquisition.RequireExternal(repositoryRoot, directory);
        SteamDepotAcquisition.ValidateOwnedTarget(directory);
        var installation = GameInstallation.Inspect(directory);
        if (installation.MajorVersion != target.Version)
            throw new InvalidOperationException("Cached game major/minor version differs from the selected target.");
        return installation;
    }

    private static bool SafeRelative(string? path) => !string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path) &&
        !path.Contains('\\') && !path.Contains(':') && path.Split('/').All(part => part is not ("" or "." or ".."));
}
