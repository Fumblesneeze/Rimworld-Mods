using System.Text.RegularExpressions;

namespace RimWorldModding.Mcp;

public sealed record AdapterCommand(
    string Kind,
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    TimeSpan Timeout,
    string? EvidenceRoot = null);

public static class RepositoryOperationPlanner
{
    private static readonly Regex LiteralIdentifier = new(
        "^[A-Za-z0-9][A-Za-z0-9._-]{0,199}$",
        RegexOptions.CultureInvariant);

    public static AdapterCommand OpenSpecValidate(string repositoryRoot)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        if (OperatingSystem.IsWindows())
        {
            var shim = ResolvePathCommand("openspec.ps1") ??
                       throw new InvalidOperationException("openspec.ps1 was not found on PATH.");
            return new AdapterCommand(
                "openspec_validate",
                "pwsh",
                ["-NoProfile", "-File", shim, "validate", "--all", "--strict", "--no-interactive"],
                root,
                TimeSpan.FromMinutes(3));
        }
        return new AdapterCommand(
            "openspec_validate",
            "openspec",
            ["validate", "--all", "--strict", "--no-interactive"],
            root,
            TimeSpan.FromMinutes(3));
    }

    private static string? ResolvePathCommand(string name)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        return path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(directory => Path.Combine(directory, name))
            .FirstOrDefault(File.Exists);
    }

    public static AdapterCommand ModBuild(
        string repositoryRoot,
        string project,
        string configuration)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        var path = RepositoryRoot.ContainedPath(root, project);
        if (!File.Exists(path) || !path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("project must select one existing repository .csproj file.");
        var normalizedConfiguration = Configuration(configuration);
        return new AdapterCommand(
            "mod_build",
            "dotnet",
            ["build", path, "-c", normalizedConfiguration],
            root,
            TimeSpan.FromMinutes(5));
    }

    public static AdapterCommand TestRun(
        string repositoryRoot,
        string suite,
        string? filter,
        string configuration)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        RequireLiteral(suite, "suite");
        if (filter is { Length: > 2048 }) throw new ArgumentException("filter exceeds 2048 characters.");
        if (filter is not null && (filter.Contains('\r') || filter.Contains('\n') || filter.IndexOf('\0') >= 0))
            throw new ArgumentException("filter contains a forbidden control character.");
        var arguments = new List<string>
        {
            "-NoProfile", "-File", Path.Combine(root, "scripts", "Invoke-Tests.ps1"),
            "-Suite", suite,
            "-Configuration", Configuration(configuration)
        };
        if (!string.IsNullOrWhiteSpace(filter))
        {
            arguments.Add("-TestFilter");
            arguments.Add(filter.Trim());
        }
        arguments.Add("-Output");
        arguments.Add("json");
        return new AdapterCommand(
            "test_run",
            "pwsh",
            arguments,
            root,
            TimeSpan.FromMinutes(15),
            Path.Combine(root, "artifacts", "TestResults"));
    }

    public static AdapterCommand EndToEnd(
        string repositoryRoot,
        string? groupId,
        string? testId,
        string language,
        int timeoutSeconds,
        bool dryRun)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        if ((groupId is null) == (testId is null))
            throw new ArgumentException("Exactly one of groupId or testId is required.");
        var selector = groupId ?? testId!;
        if (groupId is null)
        {
            RequireLiteral(selector, "testId");
        }
        else
        {
            var packageIds = selector.Split('|');
            if (packageIds.Length == 0 || packageIds.Any(packageId => !LiteralIdentifier.IsMatch(packageId)))
                throw new ArgumentException("groupId must be one pipe-delimited sequence of bounded package IDs.");
        }
        RequireLiteral(language, "language");
        if (timeoutSeconds is < 60 or > 3600)
            throw new ArgumentException("timeoutSeconds must be between 60 and 3600.");
        var arguments = new List<string>
        {
            "-NoProfile", "-File", Path.Combine(root, "scripts", "Invoke-RimWorldEndToEndTests.ps1"),
            groupId is null ? "-TestId" : "-GroupId", selector,
            "-Language", language,
            "-TimeoutSeconds", timeoutSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "-Output", "json"
        };
        if (dryRun) arguments.Add("-DryRun");
        return new AdapterCommand(
            "e2e_run",
            "pwsh",
            arguments,
            root,
            TimeSpan.FromSeconds(timeoutSeconds + 300),
            Path.Combine(root, "artifacts", "EndToEndRuns", "Grouped"));
    }

    public static AdapterCommand GatewayRun(
        string repositoryRoot,
        string runRoot,
        IReadOnlyList<string> packageIds,
        IReadOnlyList<string> projectPaths,
        int holdSeconds)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        var artifacts = RepositoryRoot.ContainedPath(root, runRoot);
        if (!artifacts.StartsWith(Path.Combine(root, "artifacts") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("runRoot must be beneath repository artifacts.");
        if (holdSeconds is < 60 or > 7200) throw new ArgumentException("holdSeconds must be between 60 and 7200.");
        Directory.CreateDirectory(artifacts);
        foreach (var packageId in packageIds) RequireLiteral(packageId, "packageId");
        var projects = projectPaths.Select(path =>
        {
            var resolved = RepositoryRoot.ContainedPath(root, path);
            if (!File.Exists(resolved) || !resolved.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Additional mod project does not exist: {path}");
            return resolved;
        }).ToArray();
        var arguments = new List<string>
        {
            "-NoProfile", "-File", Path.Combine(root, "scripts", "Invoke-GatewaySmoke.ps1"),
            "-Quicktest",
            "-ArtifactsPath", artifacts,
            "-InteractiveHoldSeconds", holdSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "-InteractiveCompletionFile", Path.Combine(artifacts, "complete.signal"),
            "-ProcessLeaseFile", Path.Combine(artifacts, "game-process.json"),
            "-TimeoutSeconds", "300",
            "-Output", "json"
        };
        if (packageIds.Count > 0)
        {
            var packageIdsPath = Path.Combine(artifacts, "additional-mod-ids.txt");
            File.WriteAllLines(packageIdsPath, packageIds, new System.Text.UTF8Encoding(false, true));
            arguments.Add("-AdditionalModIdsFile");
            arguments.Add(packageIdsPath);
        }
        if (projects.Length > 0)
        {
            arguments.Add("-AdditionalModProjectPaths");
            arguments.Add(string.Join(',', projects));
        }
        return new AdapterCommand(
            "game_run_start",
            "pwsh",
            arguments,
            root,
            TimeSpan.FromSeconds(holdSeconds + 600),
            artifacts);
    }

    private static string Configuration(string configuration) => configuration switch
    {
        "Release" => "Release",
        "Debug" => "Debug",
        _ => throw new ArgumentException("configuration must be Release or Debug.")
    };

    private static void RequireLiteral(string value, string name)
    {
        if (!LiteralIdentifier.IsMatch(value))
            throw new ArgumentException($"{name} must be one bounded literal identifier.");
    }
}
