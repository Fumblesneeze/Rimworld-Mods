using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace RimWorldDevGateway.EndToEndHost;

public static class EndToEndProjectDiscovery
{
    public static IReadOnlyList<EndToEndProjectRecord> Discover(string repositoryRoot) =>
        new ReadOnlyCollection<EndToEndProjectRecord>(MarkedProjectDiscovery.Discover(
                repositoryRoot,
                "RimWorldEndToEndTest",
                "RimWorldEndToEndTestOwnerPackageId",
                "E2E",
                int.MaxValue)
            .Select(record => new EndToEndProjectRecord(
                record.ProjectPath,
                record.OwnerPackageId,
                record.AssemblyName,
                record.TargetFramework))
            .ToArray());

    public static IReadOnlyList<EndToEndProjectRecord> SelectExactProjects(
        IEnumerable<EndToEndProjectRecord> projects,
        IEnumerable<string> selectedProjectPaths)
    {
        if (projects is null) throw new ArgumentNullException(nameof(projects));
        if (selectedProjectPaths is null) throw new ArgumentNullException(nameof(selectedProjectPaths));
        var all = projects.ToArray();
        var exactPaths = new HashSet<string>(
            selectedProjectPaths.Select(Path.GetFullPath),
            StringComparer.OrdinalIgnoreCase);
        if (exactPaths.Count == 0) return new ReadOnlyCollection<EndToEndProjectRecord>(all);
        var selected = all.Where(project => exactPaths.Contains(Path.GetFullPath(project.ProjectPath)))
            .OrderBy(project => project.ProjectPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var resolved = new HashSet<string>(selected.Select(project => Path.GetFullPath(project.ProjectPath)), StringComparer.OrdinalIgnoreCase);
        var missing = exactPaths.Where(path => !resolved.Contains(path)).ToArray();
        if (missing.Length != 0)
            throw new EndToEndDiscoveryException($"E2E staging could not resolve {missing.Length} selected project path(s)." );
        return new ReadOnlyCollection<EndToEndProjectRecord>(selected);
    }
}

public static class PerformanceProjectDiscovery
{
    public static IReadOnlyList<PerformanceProjectRecord> Discover(string repositoryRoot) =>
        new ReadOnlyCollection<PerformanceProjectRecord>(MarkedProjectDiscovery.Discover(
                repositoryRoot,
                "RimWorldPerformanceTest",
                "RimWorldPerformanceTestOwnerPackageId",
                "performance",
                PerformanceDiscoveryValidator.MaximumBenchmarks)
            .Select(record => new PerformanceProjectRecord(
                record.ProjectPath,
                record.OwnerPackageId,
                record.AssemblyName,
                record.TargetFramework))
            .ToArray());

    public static IReadOnlyList<PerformanceProjectRecord> SelectExactProjects(
        IEnumerable<PerformanceProjectRecord> projects,
        IEnumerable<string> selectedProjectPaths)
    {
        if (projects is null) throw new ArgumentNullException(nameof(projects));
        if (selectedProjectPaths is null) throw new ArgumentNullException(nameof(selectedProjectPaths));
        var exactPaths = new HashSet<string>(
            selectedProjectPaths.Select(Path.GetFullPath),
            StringComparer.OrdinalIgnoreCase);
        if (exactPaths.Count == 0)
            throw new EndToEndDiscoveryException("Performance staging requires at least one exact selected project path.");
        var selected = projects.Where(project => exactPaths.Contains(Path.GetFullPath(project.ProjectPath)))
            .OrderBy(project => project.ProjectPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var resolved = new HashSet<string>(
            selected.Select(project => Path.GetFullPath(project.ProjectPath)),
            StringComparer.OrdinalIgnoreCase);
        var missing = exactPaths.Where(path => !resolved.Contains(path)).ToArray();
        if (missing.Length != 0)
            throw new EndToEndDiscoveryException(
                $"Performance staging could not resolve {missing.Length} selected project path(s)." );
        return new ReadOnlyCollection<PerformanceProjectRecord>(selected);
    }
}

internal sealed record MarkedProjectRecord(
    string ProjectPath,
    string OwnerPackageId,
    string AssemblyName,
    string TargetFramework);

internal static class MarkedProjectDiscovery
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", "artifacts", "bin", "obj", "TestResults"
    };

    public static IReadOnlyList<MarkedProjectRecord> Discover(
        string repositoryRoot,
        string markerProperty,
        string ownerProperty,
        string description,
        int maximumProjects)
    {
        if (string.IsNullOrWhiteSpace(repositoryRoot))
            throw new ArgumentException("A repository root is required.", nameof(repositoryRoot));

        var root = Path.GetFullPath(repositoryRoot);
        if (!Directory.Exists(root))
            throw new EndToEndDiscoveryException($"{description} discovery root does not exist: {root}");

        var records = new List<MarkedProjectRecord>();
        foreach (var projectPath in EnumerateProjectPaths(root))
        {
            var document = XDocument.Load(projectPath, LoadOptions.None);
            var optIns = Values(document, markerProperty).ToArray();
            if (optIns.Length == 0 || !optIns.Any(value =>
                    StringComparer.OrdinalIgnoreCase.Equals(value, "true")))
                continue;
            if (optIns.Length != 1 || !StringComparer.OrdinalIgnoreCase.Equals(optIns[0], "true"))
                throw new EndToEndDiscoveryException(
                    $"Marked project must declare exactly one literal {markerProperty}=true: {projectPath}");

            var owners = Values(document, ownerProperty).ToArray();
            if (owners.Length != 1 || string.IsNullOrWhiteSpace(owners[0]))
                throw new EndToEndDiscoveryException(
                    $"Marked project must declare exactly one {ownerProperty}: {projectPath}");

            var assemblyNames = Values(document, "AssemblyName").ToArray();
            var targetFrameworks = Values(document, "TargetFramework").ToArray();
            if (targetFrameworks.Length != 1 || string.IsNullOrWhiteSpace(targetFrameworks[0]))
                throw new EndToEndDiscoveryException(
                    $"Marked project must declare exactly one TargetFramework: {projectPath}");

            if (records.Count == maximumProjects)
                throw new EndToEndDiscoveryException(
                    $"{description} discovery exceeds the published {maximumProjects}-project ceiling.");
            records.Add(new MarkedProjectRecord(
                projectPath,
                owners[0].Trim().ToLowerInvariant(),
                assemblyNames.Length == 1 && !string.IsNullOrWhiteSpace(assemblyNames[0])
                    ? assemblyNames[0].Trim()
                    : Path.GetFileNameWithoutExtension(projectPath),
                targetFrameworks[0].Trim()));
        }

        return new ReadOnlyCollection<MarkedProjectRecord>(records
            .OrderBy(record => record.ProjectPath, StringComparer.OrdinalIgnoreCase)
            .ToArray());
    }

    private static IReadOnlyList<string> EnumerateProjectPaths(string root)
    {
        var projects = new List<string>();
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            projects.AddRange(Directory.EnumerateFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly));
            foreach (var child in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
            {
                var info = new DirectoryInfo(child);
                if (IgnoredDirectories.Contains(info.Name) ||
                    (info.Attributes & FileAttributes.ReparsePoint) != 0)
                    continue;
                pending.Push(child);
            }
        }

        projects.Sort(StringComparer.OrdinalIgnoreCase);
        return projects;
    }

    private static IEnumerable<string> Values(XDocument document, string localName) =>
        document.Descendants()
            .Where(element => StringComparer.Ordinal.Equals(element.Name.LocalName, localName))
            .Select(element => element.Value.Trim());
}
