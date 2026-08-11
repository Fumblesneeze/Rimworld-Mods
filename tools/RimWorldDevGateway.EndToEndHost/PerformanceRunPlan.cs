using System.Collections.ObjectModel;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.EndToEndHost;

public enum PerformanceProfilerMode
{
    Circinus = 0,
    DpaDiagnostic = 1
}

public sealed class PerformanceRunPlanGroup
{
    internal PerformanceRunPlanGroup(
        string groupId,
        IEnumerable<string> activePackageIds,
        IEnumerable<PerformanceDiscoveredBenchmark> benchmarks)
    {
        GroupId = groupId;
        ActivePackageIds = new ReadOnlyCollection<string>(activePackageIds.ToArray());
        Benchmarks = new ReadOnlyCollection<PerformanceDiscoveredBenchmark>(benchmarks.ToArray());
    }

    public string GroupId { get; }
    public IReadOnlyList<string> ActivePackageIds { get; }
    public IReadOnlyList<PerformanceDiscoveredBenchmark> Benchmarks { get; }
}

public sealed class PerformanceProcessPlan
{
    internal PerformanceProcessPlan(
        int sequence,
        string groupId,
        PerformanceDiscoveredBenchmark benchmark,
        int repetition,
        int warmUpTicks,
        int sampleTicks,
        string processDirectory,
        PerformanceProfilerMode profiler,
        string? diagnosticSelector)
    {
        Sequence = sequence;
        GroupId = groupId;
        BenchmarkId = benchmark.Id;
        TypeName = benchmark.TypeName;
        ProjectPath = benchmark.ProjectPath;
        AssemblyPath = benchmark.AssemblyPath;
        AssemblyIdentity = benchmark.AssemblyIdentity;
        ModuleVersionId = benchmark.ModuleVersionId;
        AssemblySha256 = benchmark.AssemblySha256;
        StagingOwnerPackageId = benchmark.StagingOwnerPackageId;
        MeasuredSubjectPackageId = benchmark.MeasuredSubjectPackageId;
        DeterministicSeed = benchmark.DeterministicSeed;
        WorkloadVersion = benchmark.WorkloadVersion;
        ComparisonId = benchmark.ComparisonId;
        ProductAbsentControlId = benchmark.ProductAbsentControlId;
        MethodSelectors = new ReadOnlyCollection<PerformanceMetadataSelector>(
            profiler == PerformanceProfilerMode.DpaDiagnostic
                ? new[]
                {
                    new PerformanceMetadataSelector(
                        PerformanceDiscoveryValidator.ExactMethodSelectorKind,
                        diagnosticSelector!,
                        "dpa-internal-call")
                }
                : benchmark.MethodSelectors.ToArray());
        ThroughputCheckpoints = new ReadOnlyCollection<PerformanceMetadataCheckpoint>(
            benchmark.ThroughputCheckpoints.ToArray());
        EvidenceLens = benchmark.EvidenceLens;
        Repetition = repetition;
        WarmUpTicks = warmUpTicks;
        SampleTicks = sampleTicks;
        MaxWallClockSeconds = PerformanceBundleDeadline.Calculate(warmUpTicks, sampleTicks).MaxWallClockSeconds;
        GameSpeed = benchmark.GameSpeed;
        Profiler = profiler;
        DiagnosticSelector = diagnosticSelector;
        var packages = profiler == PerformanceProfilerMode.DpaDiagnostic
            ? DpaPackages(benchmark.ActivePackageIds)
            : benchmark.ActivePackageIds.ToArray();
        ActivePackageIds = new ReadOnlyCollection<string>(packages
            .Concat(new[] { PerformanceDiscoveryValidator.GatewayPackageId })
            .ToArray());
        ProcessDirectory = processDirectory;
        RawCircinusJsonPath = Path.Combine(processDirectory, "circinus.raw.json");
        RawDiagnosticJsonPath = Path.Combine(processDirectory, "dpa.raw.json");
        NormalizedJsonPath = Path.Combine(processDirectory, "normalized.json");
        CsvReportPath = Path.Combine(processDirectory, "metrics.csv");
        MarkdownReportPath = Path.Combine(processDirectory, "summary.md");
    }

    public int Sequence { get; }
    public string GroupId { get; }
    public string BenchmarkId { get; }
    public string TypeName { get; }
    public string ProjectPath { get; }
    public string AssemblyPath { get; }
    public string AssemblyIdentity { get; }
    public Guid ModuleVersionId { get; }
    public string AssemblySha256 { get; }
    public string StagingOwnerPackageId { get; }
    public string MeasuredSubjectPackageId { get; }
    public int DeterministicSeed { get; }
    public string WorkloadVersion { get; }
    public string ComparisonId { get; }
    public string? ProductAbsentControlId { get; }
    public IReadOnlyList<PerformanceMetadataSelector> MethodSelectors { get; }
    public IReadOnlyList<PerformanceMetadataCheckpoint> ThroughputCheckpoints { get; }
    public int EvidenceLens { get; }
    public int Repetition { get; }
    public int WarmUpTicks { get; }
    public int SampleTicks { get; }
    public int MaxWallClockSeconds { get; }
    public int GameSpeed { get; }
    public PerformanceProfilerMode Profiler { get; }
    public string? DiagnosticSelector { get; }
    public IReadOnlyList<string> ActivePackageIds { get; }
    public string ProcessDirectory { get; }
    public string RawCircinusJsonPath { get; }
    public string RawDiagnosticJsonPath { get; }
    public string NormalizedJsonPath { get; }
    public string CsvReportPath { get; }
    public string MarkdownReportPath { get; }

    private static string[] DpaPackages(IReadOnlyList<string> packages)
    {
        var result = packages.ToArray();
        if (result.Length < 3 ||
            !StringComparer.OrdinalIgnoreCase.Equals(result[2], PerformanceDiscoveryValidator.CircinusPackageId))
            throw new ArgumentException("A DPA diagnostic requires the exact canonical Circinus source prefix.");
        result[2] = PerformanceDiscoveryValidator.DpaPackageId;
        return result;
    }
}

public sealed class PerformanceRunPlan
{
    internal PerformanceRunPlan(
        string artifactRoot,
        IEnumerable<PerformanceRunPlanGroup> groups,
        IEnumerable<PerformanceProcessPlan> processes)
    {
        ArtifactRoot = artifactRoot;
        Groups = new ReadOnlyCollection<PerformanceRunPlanGroup>(groups.ToArray());
        Processes = new ReadOnlyCollection<PerformanceProcessPlan>(processes.ToArray());
        AggregateJsonPath = Path.Combine(artifactRoot, "aggregate.json");
        AggregateCsvPath = Path.Combine(artifactRoot, "metrics.csv");
        SummaryMarkdownPath = Path.Combine(artifactRoot, "summary.md");
    }

    public string ArtifactRoot { get; }
    public IReadOnlyList<PerformanceRunPlanGroup> Groups { get; }
    public IReadOnlyList<PerformanceProcessPlan> Processes { get; }
    public string AggregateJsonPath { get; }
    public string AggregateCsvPath { get; }
    public string SummaryMarkdownPath { get; }
}

public static class PerformanceRunPlanBuilder
{
    public const int MaximumRepetitions = 32;
    public const int MaximumPlannedProcesses = 4096;

    public static PerformanceRunPlan Create(
        PerformanceDiscoveryResult discovery,
        IEnumerable<string> benchmarkIds,
        IEnumerable<string> groupIds,
        int? warmUpTicksOverride,
        int? sampleTicksOverride,
        int? repetitionsOverride,
        string artifactRoot,
        PerformanceProfilerMode profiler = PerformanceProfilerMode.Circinus,
        string? diagnosticSelector = null)
    {
        if (discovery is null) throw new ArgumentNullException(nameof(discovery));
        var benchmarks = NormalizeFilters(benchmarkIds, "benchmark");
        var groups = NormalizeFilters(groupIds, "group");
        if (warmUpTicksOverride < 0) throw new ArgumentException("Warm-up ticks override must be non-negative.");
        if (sampleTicksOverride <= 0) throw new ArgumentException("Sample ticks override must be positive.");
        if (repetitionsOverride <= 0) throw new ArgumentException("Repetitions override must be positive.");
        if (repetitionsOverride > MaximumRepetitions)
            throw new ArgumentException($"Repetitions override must not exceed {MaximumRepetitions}.");
        if (!Enum.IsDefined(typeof(PerformanceProfilerMode), profiler))
            throw new ArgumentException("The performance profiler is invalid.");
        if (profiler == PerformanceProfilerMode.DpaDiagnostic &&
            (string.IsNullOrWhiteSpace(diagnosticSelector) ||
             diagnosticSelector.Trim().Length > PerformanceDiscoveryValidator.MaximumSelectorCharacters))
            throw new ArgumentException("A DPA diagnostic requires one bounded exact outer-method selector.");
        if (profiler == PerformanceProfilerMode.Circinus && !string.IsNullOrWhiteSpace(diagnosticSelector))
            throw new ArgumentException("A diagnostic selector is valid only with the DPA profiler.");

        var root = Path.GetFullPath(string.IsNullOrWhiteSpace(artifactRoot)
            ? throw new ArgumentException("An artifact root is required.", nameof(artifactRoot))
            : artifactRoot);
        var matchingGroups = discovery.Groups.Where(group =>
                groups.Count == 0 || groups.Contains(group.GroupId))
            .ToArray();
        if (groups.Any(id => matchingGroups.All(group => !StringComparer.Ordinal.Equals(group.GroupId, id))))
            throw new ArgumentException("The performance group filter selected zero matching groups.");
        var selectedGroups = matchingGroups
            .Select(group => new PerformanceRunPlanGroup(
                profiler == PerformanceProfilerMode.DpaDiagnostic ? group.GroupId + "-dpa" : group.GroupId,
                profiler == PerformanceProfilerMode.DpaDiagnostic
                    ? ReplaceCircinus(group.ActivePackageIds)
                    : group.ActivePackageIds,
                group.Benchmarks.Where(benchmark =>
                    benchmarks.Count == 0 || benchmarks.Contains(benchmark.Id))))
            .Where(group => group.Benchmarks.Count > 0)
            .ToArray();

        if (selectedGroups.Length == 0)
            throw new ArgumentException("The combined performance group and benchmark filters selected zero benchmarks.");
        if (benchmarks.Any(id => selectedGroups.SelectMany(group => group.Benchmarks)
                .All(benchmark => !StringComparer.Ordinal.Equals(benchmark.Id, id))))
            throw new ArgumentException("The performance benchmark filter selected zero matching benchmarks.");

        var processes = new List<PerformanceProcessPlan>();
        foreach (var group in selectedGroups)
        {
            foreach (var benchmark in group.Benchmarks)
            {
                var repetitions = profiler == PerformanceProfilerMode.DpaDiagnostic
                    ? repetitionsOverride ?? 1
                    : repetitionsOverride ?? benchmark.Repetitions;
                if (repetitions is <= 0 or > MaximumRepetitions)
                    throw new ArgumentException(
                        $"Benchmark '{benchmark.Id}' repetitions must be between 1 and {MaximumRepetitions}.");
                for (var repetition = 1; repetition <= repetitions; repetition++)
                {
                    if (processes.Count == MaximumPlannedProcesses)
                        throw new ArgumentException(
                            $"Performance plan exceeds the {MaximumPlannedProcesses}-process ceiling.");
                    var sequence = processes.Count + 1;
                    var directory = Path.Combine(
                        root,
                        $"p{sequence:D4}-r{repetition:D2}");
                    processes.Add(new PerformanceProcessPlan(
                        sequence,
                        group.GroupId,
                        benchmark,
                        repetition,
                        warmUpTicksOverride ?? benchmark.WarmUpTicks,
                        sampleTicksOverride ?? benchmark.SampleTicks,
                        directory,
                        profiler,
                        diagnosticSelector?.Trim()));
                }
            }
        }

        return new PerformanceRunPlan(root, selectedGroups, processes);
    }

    private static IReadOnlyList<string> ReplaceCircinus(IReadOnlyList<string> packages)
    {
        var result = packages.ToArray();
        if (result.Length < 3 ||
            !StringComparer.OrdinalIgnoreCase.Equals(result[2], PerformanceDiscoveryValidator.CircinusPackageId))
            throw new ArgumentException("A DPA diagnostic requires the exact canonical Circinus source prefix.");
        result[2] = PerformanceDiscoveryValidator.DpaPackageId;
        return result;
    }

    private static HashSet<string> NormalizeFilters(IEnumerable<string> values, string description)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (normalized.Length == 0) throw new ArgumentException($"A {description} filter is empty.");
            if (!result.Add(normalized)) throw new ArgumentException($"The {description} filter contains duplicate '{normalized}'.");
        }

        return result;
    }

}
