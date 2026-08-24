using System.Xml.Linq;

namespace RimWorldModding.Mcp;

internal static class RepositoryDiscovery
{
    public static IReadOnlyList<ModSummary> DiscoverMods(string repositoryRoot)
    {
        var modsRoot = Path.Combine(repositoryRoot, "mods");
        var testsRoot = Path.Combine(repositoryRoot, "tests");
        var results = new List<ModSummary>();
        var packageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var projectPath in Directory.GetFiles(modsRoot, "*.csproj", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var document = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace);
            var packageId = Property(document, "RimWorldPackageId");
            if (string.IsNullOrWhiteSpace(packageId))
            {
                continue;
            }

            if (!packageIds.Add(packageId))
            {
                throw new InvalidOperationException($"Duplicate RimWorld package ID: {packageId}");
            }

            var modRoot = Path.GetDirectoryName(projectPath)!;
            var displayName = Property(document, "RimWorldModName");
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = ReadAboutName(Path.Combine(modRoot, "About", "About.xml")) ??
                              Path.GetFileNameWithoutExtension(projectPath);
            }

            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            var tests = Directory.Exists(testsRoot)
                ? Directory.GetFiles(testsRoot, $"{projectName}*.csproj", SearchOption.AllDirectories)
                    .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray()
                : Array.Empty<string>();
            var releaseProfile = Path.Combine(modRoot, "Release", "release.json");

            results.Add(new ModSummary(
                displayName.Trim(),
                packageId.Trim().ToLowerInvariant(),
                Path.GetRelativePath(repositoryRoot, projectPath).Replace('\\', '/'),
                (Property(document, "RimWorldDistributionKind") ?? "Unspecified").Trim(),
                File.Exists(releaseProfile)
                    ? Path.GetRelativePath(repositoryRoot, releaseProfile).Replace('\\', '/')
                    : null,
                tests));
        }

        return results.OrderBy(mod => mod.PackageId, StringComparer.Ordinal).ToArray();
    }

    private static string? Property(XDocument document, string name) =>
        document.Descendants().FirstOrDefault(node => node.Name.LocalName == name)?.Value;

    private static string? ReadAboutName(string path)
    {
        if (!File.Exists(path)) return null;
        return XDocument.Load(path).Root?.Elements().FirstOrDefault(node => node.Name.LocalName == "name")?.Value;
    }
}
