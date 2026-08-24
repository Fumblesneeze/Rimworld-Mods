using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RimWorldModding.Mcp;

public sealed record ReleaseProfile(
    string Schema,
    string Path,
    string Project,
    string PackageId,
    string Title,
    string Author,
    string DistributionKind,
    string RimWorldVersion,
    string RimWorldBuild,
    string RimWorldRuntimeBuild,
    string SteamBuildId,
    string ManagedAssemblySha256,
    int SteamAppId,
    string SteamUserId,
    string? PublishedFileId,
    bool AllowFirstPublication,
    string Visibility,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> RequiredWorkshopItems,
    IReadOnlyList<string> HardRuntimeAssemblyReferences,
    string BuildOperation,
    string PresentationOperation,
    string PackageSource,
    IReadOnlyList<string> PackageInclude,
    string Description,
    string Preview,
    string? PreviousChangeNote,
    string ChangeNote,
    string VerificationProfile);

public sealed class ReleaseProfileException(string message) : Exception(message);

public static class ReleaseProfileCatalog
{
    private static readonly HashSet<string> AllowedProperties = new(StringComparer.Ordinal)
    {
        "schema", "project", "packageId", "title", "author", "distributionKind",
        "rimWorldVersion", "rimWorldBuild", "rimWorldRuntimeBuild", "steamBuildId",
        "managedAssemblySha256", "steamAppId", "steamUserId", "publishedFileId",
        "allowFirstPublication", "visibility", "tags", "requiredWorkshopItems", "hardRuntimeAssemblyReferences",
        "buildOperation", "presentationOperation", "packageSource", "packageInclude",
        "description", "preview", "previousChangeNote", "changeNote", "verificationProfile"
    };

    private static readonly HashSet<string> RegisteredBuildOperations = new(StringComparer.Ordinal)
    {
        "mod_build"
    };

    private static readonly HashSet<string> RegisteredPresentationOperations = new(StringComparer.Ordinal)
    {
        "presentation_render"
    };

    public static IReadOnlyList<ReleaseProfile> Discover(string repositoryRoot)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        var profiles = Directory.GetFiles(Path.Combine(root, "mods"), "release.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => Load(root, path))
            .ToArray();
        var duplicate = profiles.GroupBy(profile => profile.PackageId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ReleaseProfileException($"Duplicate release profile packageId '{duplicate.Key}'.");
        }

        return profiles;
    }

    public static ReleaseProfile Load(string repositoryRoot, string profilePath)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(profilePath));
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            throw new ReleaseProfileException($"Release profile could not be read: {exception.Message}");
        }

        using (document)
        {
            var element = document.RootElement;
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new ReleaseProfileException("Release profile root must be one JSON object.");
            }

            var unknown = element.EnumerateObject().Select(property => property.Name)
                .Where(name => !AllowedProperties.Contains(name)).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            if (unknown.Length > 0)
            {
                throw new ReleaseProfileException($"Release profile has unknown properties: {string.Join(", ", unknown)}");
            }

            var schema = RequiredString(element, "schema");
            if (schema != "RimWorldModRelease/v1")
                throw new ReleaseProfileException($"Unsupported release profile schema '{schema}'.");

            var projectRelative = RequiredString(element, "project");
            var project = ContainedProfilePath(root, projectRelative);
            if (!File.Exists(project)) throw new ReleaseProfileException($"Release project does not exist: {projectRelative}");
            var packageId = RequiredString(element, "packageId").ToLowerInvariant();
            if (!Regex.IsMatch(packageId, "^[a-z0-9][a-z0-9._-]{0,199}$"))
                throw new ReleaseProfileException("packageId must be one canonical path-safe package identifier.");
            var title = RequiredString(element, "title");
            var author = RequiredString(element, "author");
            var distributionKind = RequiredString(element, "distributionKind");
            var packageSourceRelative = RequiredString(element, "packageSource");
            _ = ContainedProfilePath(root, packageSourceRelative);

            var buildOperation = RequiredString(element, "buildOperation");
            if (!RegisteredBuildOperations.Contains(buildOperation))
                throw new ReleaseProfileException($"Unknown registered buildOperation '{buildOperation}'; arbitrary commands are forbidden.");
            var presentationOperation = RequiredString(element, "presentationOperation");
            if (!RegisteredPresentationOperations.Contains(presentationOperation))
                throw new ReleaseProfileException($"Unknown registered presentationOperation '{presentationOperation}'; arbitrary commands are forbidden.");

            var publishedFileId = OptionalString(element, "publishedFileId");
            var allowFirst = RequiredBoolean(element, "allowFirstPublication");
            if (string.IsNullOrWhiteSpace(publishedFileId) && !allowFirst)
                throw new ReleaseProfileException("A release profile requires publishedFileId or allowFirstPublication=true.");
            if (!string.IsNullOrWhiteSpace(publishedFileId) && !Regex.IsMatch(publishedFileId, "^[1-9][0-9]{5,19}$"))
                throw new ReleaseProfileException("publishedFileId must be a nonzero Steam item ID.");
            if (!string.IsNullOrWhiteSpace(publishedFileId) && allowFirst)
                throw new ReleaseProfileException("allowFirstPublication must be false after publishedFileId is known.");

            var visibility = RequiredString(element, "visibility");
            if (visibility is not ("Private" or "Public" or "Unlisted"))
                throw new ReleaseProfileException("visibility must be Private, Public, or Unlisted.");
            if (string.IsNullOrWhiteSpace(publishedFileId) && visibility != "Private")
                throw new ReleaseProfileException("First publication visibility must be Private.");

            var managedHash = RequiredString(element, "managedAssemblySha256").ToUpperInvariant();
            if (!Regex.IsMatch(managedHash, "^[A-F0-9]{64}$"))
                throw new ReleaseProfileException("managedAssemblySha256 must be 64 hexadecimal characters.");
            var rimWorldVersion = RequiredString(element, "rimWorldVersion");
            var rimWorldBuild = RequiredString(element, "rimWorldBuild");
            var rimWorldRuntimeBuild = RequiredString(element, "rimWorldRuntimeBuild");
            var steamBuildId = RequiredString(element, "steamBuildId");
            var steamUserId = RequiredString(element, "steamUserId");
            if (!Regex.IsMatch(rimWorldVersion, "^1\\.[0-9]+$") ||
                !Regex.IsMatch(rimWorldBuild, "^1\\.[0-9]+\\.[0-9]+ rev[0-9]+$") ||
                !Regex.IsMatch(rimWorldRuntimeBuild, "^1\\.[0-9]+\\.[0-9]+ rev[0-9]+$") ||
                !Regex.IsMatch(steamBuildId, "^[1-9][0-9]+$") ||
                !Regex.IsMatch(steamUserId, "^[1-9][0-9]{16,19}$"))
                throw new ReleaseProfileException("Pinned RimWorld/Steam build or user identity has an invalid shape.");

            var tags = RequiredStringArray(element, "tags");
            var dependencies = RequiredStringArray(element, "requiredWorkshopItems", allowEmpty: true);
            if (dependencies.Any(value => !Regex.IsMatch(value, "^[1-9][0-9]{5,19}$")))
                throw new ReleaseProfileException("requiredWorkshopItems must contain nonzero Steam item IDs.");
            var hardRuntimeReferences = RequiredStringArray(element, "hardRuntimeAssemblyReferences", allowEmpty: true);
            if (hardRuntimeReferences.Any(value => !Regex.IsMatch(value, "^[A-Za-z0-9_.-]{1,200}$")) ||
                hardRuntimeReferences.Distinct(StringComparer.OrdinalIgnoreCase).Count() != hardRuntimeReferences.Length)
                throw new ReleaseProfileException("hardRuntimeAssemblyReferences must contain unique assembly simple names.");
            var includes = RequiredStringArray(element, "packageInclude");
            foreach (var include in includes)
            {
                if (Path.IsPathRooted(include) || include.Split('/', '\\').Any(part => part == ".."))
                    throw new ReleaseProfileException($"packageInclude path escapes the package: {include}");
            }

            var releaseDirectory = Path.GetDirectoryName(Path.GetFullPath(profilePath))!;
            var description = ContainedProfilePath(releaseDirectory, RequiredString(element, "description"));
            var preview = ContainedProfilePath(releaseDirectory, RequiredString(element, "preview"));
            if (!File.Exists(description)) throw new ReleaseProfileException($"Workshop description does not exist: {description}");
            if (!File.Exists(preview)) throw new ReleaseProfileException($"Workshop preview does not exist: {preview}");

            ValidateProjectIdentity(project, packageId, title, author, distributionKind);

            var verificationProfile = ContainedProfilePath(root, RequiredString(element, "verificationProfile"));
            if (!File.Exists(verificationProfile))
                throw new ReleaseProfileException("Subscriber verification profile does not exist.");
            _ = SubscriberVerificationProfiles.Load(root, verificationProfile);

            return new ReleaseProfile(
                schema,
                Path.GetFullPath(profilePath),
                project,
                packageId,
                title,
                author,
                distributionKind,
                rimWorldVersion,
                rimWorldBuild,
                rimWorldRuntimeBuild,
                steamBuildId,
                managedHash,
                RequiredInt32(element, "steamAppId"),
                steamUserId,
                publishedFileId,
                allowFirst,
                visibility,
                tags,
                dependencies,
                hardRuntimeReferences,
                buildOperation,
                presentationOperation,
                ContainedProfilePath(root, packageSourceRelative),
                includes,
                description,
                preview,
                OptionalString(element, "previousChangeNote"),
                RequiredStringAllowEmpty(element, "changeNote"),
                verificationProfile);
        }
    }

    private static void ValidateProjectIdentity(
        string project,
        string packageId,
        string title,
        string author,
        string distributionKind)
    {
        var document = XDocument.Load(project);
        string? Property(string name) => document.Descendants().FirstOrDefault(node => node.Name.LocalName == name)?.Value.Trim();
        if (!string.Equals(Property("RimWorldPackageId"), packageId, StringComparison.OrdinalIgnoreCase))
            throw new ReleaseProfileException("Release profile packageId does not match the project.");
        if (!string.Equals(Property("RimWorldModName"), title, StringComparison.Ordinal))
            throw new ReleaseProfileException("Release profile title does not match the project.");
        if (!string.Equals(Property("Authors"), author, StringComparison.Ordinal))
            throw new ReleaseProfileException("Release profile author does not match the project.");
        if (!string.Equals(Property("RimWorldDistributionKind"), distributionKind, StringComparison.Ordinal))
            throw new ReleaseProfileException("Release profile distributionKind does not match the project.");
    }

    private static string ContainedProfilePath(string root, string relative)
    {
        try
        {
            var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var path = Path.GetFullPath(relative, normalizedRoot);
            if (!path.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new ReleaseProfileException($"Release profile path escapes its root: {relative}");
            return path;
        }
        catch (Exception exception) when (exception is not ReleaseProfileException)
        {
            throw new ReleaseProfileException($"Invalid release profile path '{relative}': {exception.Message}");
        }
    }

    private static string RequiredString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(property.GetString()))
            throw new ReleaseProfileException($"Release profile requires nonblank string '{name}'.");
        return property.GetString()!.Trim();
    }

    private static string RequiredStringAllowEmpty(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String)
            throw new ReleaseProfileException($"Release profile requires string '{name}'.");
        return property.GetString()!.Trim();
    }

    private static string? OptionalString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var property) || property.ValueKind == JsonValueKind.Null) return null;
        if (property.ValueKind != JsonValueKind.String)
            throw new ReleaseProfileException($"Release profile '{name}' must be string or null.");
        return property.GetString()?.Trim();
    }

    private static bool RequiredBoolean(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var property) || property.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new ReleaseProfileException($"Release profile requires boolean '{name}'.");
        return property.GetBoolean();
    }

    private static int RequiredInt32(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var property) || !property.TryGetInt32(out var value) || value <= 0)
            throw new ReleaseProfileException($"Release profile requires positive integer '{name}'.");
        return value;
    }

    private static string[] RequiredStringArray(JsonElement root, string name, bool allowEmpty = false)
    {
        if (!root.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.Array)
            throw new ReleaseProfileException($"Release profile requires string array '{name}'.");
        var values = property.EnumerateArray().Select(value =>
        {
            if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
                throw new ReleaseProfileException($"Release profile '{name}' contains a non-string or blank value.");
            return value.GetString()!.Trim();
        }).Distinct(StringComparer.Ordinal).ToArray();
        if (!allowEmpty && values.Length == 0) throw new ReleaseProfileException($"Release profile '{name}' must not be empty.");
        return values;
    }
}
