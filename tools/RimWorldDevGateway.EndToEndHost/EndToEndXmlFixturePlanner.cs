using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace RimWorldDevGateway.EndToEndHost;

public sealed record EndToEndXmlFixtureFile(string RelativePath, long Length, string Sha256);
public sealed record EndToEndXmlFixtureExpectedError(string Message, int Count);

public sealed class EndToEndXmlFixturePlan
{
    internal EndToEndXmlFixturePlan(string manifestPath, string ownerPackageId, string packageId,
        string rimWorldVersion, string modsRoot, IEnumerable<string> activePackageIds,
        IEnumerable<EndToEndXmlFixtureFile> files, string manifestSha256,
        IEnumerable<EndToEndXmlFixtureExpectedError> expectedErrors)
    {
        ManifestPath = manifestPath;
        SourceDirectory = Path.GetDirectoryName(manifestPath)!;
        OwnerPackageId = ownerPackageId;
        PackageId = packageId;
        RimWorldVersion = rimWorldVersion;
        ModsRoot = modsRoot;
        DestinationDirectory = Path.Combine(modsRoot, packageId);
        ActivePackageIds = new ReadOnlyCollection<string>(activePackageIds.ToArray());
        Files = new ReadOnlyCollection<EndToEndXmlFixtureFile>(files.ToArray());
        ManifestSha256 = manifestSha256;
        ExpectedErrors = new ReadOnlyCollection<EndToEndXmlFixtureExpectedError>(expectedErrors.ToArray());
    }

    public string ManifestPath { get; }
    public string SourceDirectory { get; }
    public string OwnerPackageId { get; }
    public string PackageId { get; }
    public string RimWorldVersion { get; }
    public string ModsRoot { get; }
    public string DestinationDirectory { get; }
    public IReadOnlyList<string> ActivePackageIds { get; }
    public IReadOnlyList<EndToEndXmlFixtureFile> Files { get; }
    public string ManifestSha256 { get; }
    public IReadOnlyList<EndToEndXmlFixtureExpectedError> ExpectedErrors { get; }
}

public static class EndToEndXmlFixturePlanner
{
    public static IReadOnlyList<EndToEndXmlFixturePlan> PlanSelected(string repositoryRoot,
        IEnumerable<string> manifestPaths, IEnumerable<string> activePackageIds,
        IEnumerable<string> installedPackageIds, string modsRoot, string rimWorldVersion)
    {
        try { return Plan(repositoryRoot, manifestPaths, activePackageIds, installedPackageIds, modsRoot, rimWorldVersion); }
        catch (Exception exception) when (exception is JsonException or XmlException or IOException or
            ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            throw new EndToEndStageException("XML fixture input is invalid: " + exception.Message, exception);
        }
    }

    private static IReadOnlyList<EndToEndXmlFixturePlan> Plan(string repositoryRoot,
        IEnumerable<string> manifestPaths, IEnumerable<string> activePackageIds,
        IEnumerable<string> installedPackageIds, string modsRoot, string rimWorldVersion)
    {
        var active = activePackageIds.ToArray();
        var installed = new HashSet<string>(installedPackageIds, StringComparer.OrdinalIgnoreCase);
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var plans = new List<EndToEndXmlFixturePlan>();
        foreach (var path in manifestPaths)
        {
            var manifestPath = Path.GetFullPath(path, repositoryRoot);
            var source = Path.GetDirectoryName(manifestPath)!;
            var fixturesRoot = Path.GetFullPath(Path.Combine(repositoryRoot, "tests", "Fixtures"));
            if (!StringComparer.OrdinalIgnoreCase.Equals(Path.GetDirectoryName(source), fixturesRoot) ||
                Path.GetFileName(manifestPath) != "fixture.json")
                throw new EndToEndStageException("XML fixture manifest must be tests/Fixtures/<fixture>/fixture.json.");
            RejectReparseAncestors(manifestPath);
            var manifestBytes = ReadBounded(manifestPath, 64 * 1024);
            using var document = JsonDocument.Parse(manifestBytes);
            var root = document.RootElement;
            var packageId = root.GetProperty("packageId").GetString()!;
            if (!active.Contains(packageId, StringComparer.OrdinalIgnoreCase)) continue;
            ValidateManifest(root, active, rimWorldVersion);
            var expectedErrors = ReadExpectedErrors(root);
            if (installed.Contains(packageId) || !selected.Add(packageId))
                throw new EndToEndStageException("XML fixture package identity is already installed or declared more than once: " + packageId);
            var owner = root.GetProperty("ownerPackageId").GetString()!;
            var includes = root.GetProperty("packageInclude").EnumerateArray().Select(value => value.GetString()!).ToArray();
            if (includes.Length < 2 || !includes.Contains("About/About.xml", StringComparer.Ordinal) ||
                includes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != includes.Length)
                throw new EndToEndStageException("XML fixture inventory requires one About and distinct Def XML files.");
            foreach (string include in includes) ValidateInclude(include);
            var files = includes
                .OrderBy(value => value, StringComparer.Ordinal)
                .Select(relative =>
                {
                    var filePath = Path.Combine(source, relative);
                    RejectReparseAncestors(filePath);
                    var bytes = ReadBounded(filePath, 8 * 1024 * 1024);
                    if (relative == "About/About.xml") ValidateAbout(bytes, packageId, owner, rimWorldVersion);
                    return new EndToEndXmlFixtureFile(relative, bytes.LongLength,
                        Convert.ToHexString(SHA256.HashData(bytes)));
                }).ToArray();
            plans.Add(new EndToEndXmlFixturePlan(manifestPath,
                owner, packageId,
                rimWorldVersion, Path.TrimEndingDirectorySeparator(Path.GetFullPath(modsRoot)), active, files,
                Convert.ToHexString(SHA256.HashData(manifestBytes)), expectedErrors));
        }
        return new ReadOnlyCollection<EndToEndXmlFixturePlan>(plans);
    }

    internal static byte[] ReadBounded(string path, int maximumBytes)
    {
        RejectReparseAncestors(path);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > maximumBytes)
            throw new EndToEndStageException("XML fixture file exceeds its byte bound: " + path);
        var bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        if (stream.ReadByte() != -1) throw new EndToEndStageException("XML fixture file changed during bounded read: " + path);
        return bytes;
    }

    internal static void RejectReparseAncestors(string path)
    {
        for (string? current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new EndToEndStageException("XML fixture path crosses a reparse point: " + current);
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
    }

    private static void ValidateInclude(string include)
    {
        if (string.IsNullOrWhiteSpace(include) || Path.IsPathRooted(include) || include.Contains('\\') ||
            include.Split('/').Any(segment => segment is "" or "." or ".." ||
                segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || segment.EndsWith(' ') || segment.EndsWith('.')) ||
            (include != "About/About.xml" && !(include.StartsWith("Defs/", StringComparison.Ordinal) &&
                include.EndsWith(".xml", StringComparison.Ordinal))))
            throw new EndToEndStageException("XML fixture include must be a contained About or Def XML file: " + include);
    }

    private static void ValidateManifest(JsonElement root, string[] active, string version)
    {
        var expected = new[] { "schema", "ownerPackageId", "packageId", "rimWorldVersion", "packageInclude" }
            .Concat(root.TryGetProperty("expectedErrors", out _) ? new[] { "expectedErrors" } : Array.Empty<string>()).ToArray();
        var names = root.EnumerateObject().Select(property => property.Name).ToArray();
        if (names.Length != expected.Length || !names.OrderBy(value => value, StringComparer.Ordinal)
                .SequenceEqual(expected.OrderBy(value => value, StringComparer.Ordinal)) ||
            root.GetProperty("schema").GetString() != "RimWorldDevGateway/XmlFixture/v1" ||
            version is not ("1.6" or "1.7") || root.GetProperty("rimWorldVersion").GetString() != version)
            throw new EndToEndStageException("XML fixture schema, properties or game version do not match.");
        string owner = root.GetProperty("ownerPackageId").GetString()!;
        string package = root.GetProperty("packageId").GetString()!;
        if (!IsPackageId(owner) || !IsPackageId(package) || owner == package ||
            active.Count(value => StringComparer.OrdinalIgnoreCase.Equals(value, owner)) != 1 ||
            active.Count(value => StringComparer.OrdinalIgnoreCase.Equals(value, package)) != 1 ||
            Array.FindIndex(active, value => StringComparer.OrdinalIgnoreCase.Equals(value, owner)) >=
            Array.FindIndex(active, value => StringComparer.OrdinalIgnoreCase.Equals(value, package)))
            throw new EndToEndStageException("XML fixture requires distinct canonical identities and its owner before it exactly once.");
    }

    internal static bool IsPackageId(string? value) => value is not null && value.Length <= 128 &&
        Regex.IsMatch(value, "^fumblesneeze\\.[a-z0-9]+(?:[._-][a-z0-9]+)*$", RegexOptions.CultureInvariant);

    private static EndToEndXmlFixtureExpectedError[] ReadExpectedErrors(JsonElement root)
    {
        if (!root.TryGetProperty("expectedErrors", out var entries)) return Array.Empty<EndToEndXmlFixtureExpectedError>();
        if (entries.ValueKind != JsonValueKind.Array || entries.GetArrayLength() > 16)
            throw new EndToEndStageException("XML fixture expectedErrors must contain at most 16 exact messages.");
        var result = new List<EndToEndXmlFixtureExpectedError>();
        foreach (var entry in entries.EnumerateArray())
        {
            var names = entry.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal);
            if (!names.SequenceEqual(new[] { "count", "message" }))
                throw new EndToEndStageException("XML fixture expected error requires only message and count.");
            var message = entry.GetProperty("message").GetString();
            if (string.IsNullOrWhiteSpace(message) || message.Length > 4096 || message.Any(char.IsControl) ||
                message != message.Trim() || !entry.GetProperty("count").TryGetInt32(out int count) || count is < 1 or > 16 ||
                result.Any(value => value.Message == message))
                throw new EndToEndStageException("XML fixture expected error requires a distinct exact first-line message and count 1–16.");
            result.Add(new EndToEndXmlFixtureExpectedError(message, count));
        }
        return result.ToArray();
    }

    private static void ValidateAbout(byte[] bytes, string package, string owner, string version)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1024 * 1024
        });
        var root = XDocument.Load(reader).Root;
        if (root?.Name != "ModMetaData" || root.Elements("packageId").Count() != 1 ||
            root.Element("packageId")!.Value.Trim() != package ||
            root.Element("supportedVersions")?.Elements("li").Any(value => value.Value.Trim() == version) != true ||
            root.Element("modDependencies")?.Elements("li").Any(value =>
                value.Element("packageId")?.Value.Trim() == owner) != true)
            throw new EndToEndStageException("XML fixture About identity, supported version or owner dependency does not match.");
    }
}
