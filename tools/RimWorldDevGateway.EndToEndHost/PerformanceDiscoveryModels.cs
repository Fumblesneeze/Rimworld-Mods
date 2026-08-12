using System.Collections.ObjectModel;

namespace RimWorldDevGateway.EndToEndHost;

public sealed class PerformanceMetadataSelector
{
    public PerformanceMetadataSelector(int kind, string value, string category)
    {
        Kind = kind;
        Value = value;
        Category = category;
    }

    public int Kind { get; }

    public string Value { get; }

    public string Category { get; }
}

public sealed class PerformanceMetadataCheckpoint
{
    public PerformanceMetadataCheckpoint(string id, long minimumCount)
    {
        Id = id;
        MinimumCount = minimumCount;
    }

    public string Id { get; }

    public long MinimumCount { get; }
}

public sealed class PerformanceMetadataDeclaration
{
    public PerformanceMetadataDeclaration(
        string typeName,
        string id,
        string stagingOwnerPackageId,
        string measuredSubjectPackageId,
        IEnumerable<string> activePackageIds,
        int deterministicSeed,
        string workloadVersion,
        string comparisonId,
        int warmUpTicks,
        int sampleTicks,
        int gameSpeed,
        int repetitions,
        int evidenceLens,
        string? productAbsentControlId,
        IEnumerable<PerformanceMetadataSelector> methodSelectors,
        IEnumerable<PerformanceMetadataCheckpoint> throughputCheckpoints,
        bool isConcrete,
        bool implementsContract,
        bool implementsThroughputCounter,
        bool hasPublicParameterlessConstructor)
    {
        TypeName = typeName;
        Id = id;
        StagingOwnerPackageId = stagingOwnerPackageId;
        MeasuredSubjectPackageId = measuredSubjectPackageId;
        ActivePackageIds = Copy(activePackageIds);
        DeterministicSeed = deterministicSeed;
        WorkloadVersion = workloadVersion;
        ComparisonId = comparisonId;
        WarmUpTicks = warmUpTicks;
        SampleTicks = sampleTicks;
        GameSpeed = gameSpeed;
        Repetitions = repetitions;
        EvidenceLens = evidenceLens;
        ProductAbsentControlId = productAbsentControlId;
        MethodSelectors = Copy(methodSelectors);
        ThroughputCheckpoints = Copy(throughputCheckpoints);
        IsConcrete = isConcrete;
        ImplementsContract = implementsContract;
        ImplementsThroughputCounter = implementsThroughputCounter;
        HasPublicParameterlessConstructor = hasPublicParameterlessConstructor;
    }

    public string TypeName { get; }
    public string Id { get; }
    public string StagingOwnerPackageId { get; }
    public string MeasuredSubjectPackageId { get; }
    public IReadOnlyList<string> ActivePackageIds { get; }
    public int DeterministicSeed { get; }
    public string WorkloadVersion { get; }
    public string ComparisonId { get; }
    public int WarmUpTicks { get; }
    public int SampleTicks { get; }
    public int GameSpeed { get; }
    public int Repetitions { get; }
    public int EvidenceLens { get; }
    public string? ProductAbsentControlId { get; }
    public IReadOnlyList<PerformanceMetadataSelector> MethodSelectors { get; }
    public IReadOnlyList<PerformanceMetadataCheckpoint> ThroughputCheckpoints { get; }
    public bool IsConcrete { get; }
    public bool ImplementsContract { get; }
    public bool ImplementsThroughputCounter { get; }
    public bool HasPublicParameterlessConstructor { get; }

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>((values ?? throw new ArgumentNullException(nameof(values))).ToArray());
}

public sealed class PerformanceAssemblyMetadata
{
    public PerformanceAssemblyMetadata(
        EndToEndAssemblyMetadata assembly,
        IEnumerable<PerformanceMetadataDeclaration> declarations)
    {
        Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        Declarations = new ReadOnlyCollection<PerformanceMetadataDeclaration>(declarations.ToArray());
    }

    public EndToEndAssemblyMetadata Assembly { get; }
    public string AssemblyPath => Assembly.AssemblyPath;
    public string AssemblyName => Assembly.AssemblyName;
    public string AssemblyVersion => Assembly.AssemblyVersion;
    public string AssemblyIdentity => Assembly.AssemblyIdentity;
    public Guid ModuleVersionId => Assembly.ModuleVersionId;
    public long Length => Assembly.Length;
    public string Sha256 => Assembly.Sha256;
    public IReadOnlyList<EndToEndAssemblyReference> AssemblyReferences => Assembly.AssemblyReferences;
    public IReadOnlyList<PerformanceMetadataDeclaration> Declarations { get; }
}

public sealed class PerformanceProjectRecord
{
    public PerformanceProjectRecord(
        string projectPath,
        string ownerPackageId,
        string assemblyName,
        string targetFramework)
    {
        ProjectPath = projectPath;
        OwnerPackageId = ownerPackageId;
        AssemblyName = assemblyName;
        TargetFramework = targetFramework;
    }

    public string ProjectPath { get; }
    public string OwnerPackageId { get; }
    public string AssemblyName { get; }
    public string TargetFramework { get; }
}

public sealed class PerformanceAssemblyCandidate
{
    public PerformanceAssemblyCandidate(
        string projectPath,
        string expectedOwnerPackageId,
        string expectedAssemblyName,
        PerformanceAssemblyMetadata metadata)
    {
        ProjectPath = projectPath;
        ExpectedOwnerPackageId = expectedOwnerPackageId;
        ExpectedAssemblyName = expectedAssemblyName;
        Metadata = metadata;
    }

    public string ProjectPath { get; }
    public string ExpectedOwnerPackageId { get; }
    public string ExpectedAssemblyName { get; }
    public PerformanceAssemblyMetadata Metadata { get; }
}

public sealed class PerformanceDiscoveredBenchmark
{
    internal PerformanceDiscoveredBenchmark(
        PerformanceAssemblyCandidate candidate,
        PerformanceMetadataDeclaration declaration)
    {
        Candidate = candidate;
        Declaration = declaration;
        Id = declaration.Id.Trim();
        TypeName = declaration.TypeName;
        StagingOwnerPackageId = Normalize(declaration.StagingOwnerPackageId);
        MeasuredSubjectPackageId = Normalize(declaration.MeasuredSubjectPackageId);
        ActivePackageIds = new ReadOnlyCollection<string>(
            declaration.ActivePackageIds.Select(Normalize).ToArray());
    }

    internal PerformanceAssemblyCandidate Candidate { get; }
    internal PerformanceMetadataDeclaration Declaration { get; }
    public string Id { get; }
    public string TypeName { get; }
    public string StagingOwnerPackageId { get; }
    public string MeasuredSubjectPackageId { get; }
    public IReadOnlyList<string> ActivePackageIds { get; }
    public int DeterministicSeed => Declaration.DeterministicSeed;
    public string WorkloadVersion => Declaration.WorkloadVersion;
    public string ComparisonId => Declaration.ComparisonId;
    public int WarmUpTicks => Declaration.WarmUpTicks;
    public int SampleTicks => Declaration.SampleTicks;
    public int GameSpeed => Declaration.GameSpeed;
    public int Repetitions => Declaration.Repetitions;
    public int EvidenceLens => Declaration.EvidenceLens;
    public string? ProductAbsentControlId => Declaration.ProductAbsentControlId;
    public IReadOnlyList<PerformanceMetadataSelector> MethodSelectors => Declaration.MethodSelectors;
    public IReadOnlyList<PerformanceMetadataCheckpoint> ThroughputCheckpoints => Declaration.ThroughputCheckpoints;
    public string ProjectPath => Candidate.ProjectPath;
    public string AssemblyPath => Candidate.Metadata.AssemblyPath;
    public string AssemblyIdentity => Candidate.Metadata.AssemblyIdentity;
    public Guid ModuleVersionId => Candidate.Metadata.ModuleVersionId;
    public string AssemblySha256 => Candidate.Metadata.Sha256;

    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;
}

public sealed class PerformanceTestGroup
{
    internal PerformanceTestGroup(
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

public sealed class PerformanceDiscoveryResult
{
    internal PerformanceDiscoveryResult(IEnumerable<PerformanceTestGroup> groups)
    {
        Groups = new ReadOnlyCollection<PerformanceTestGroup>(groups.ToArray());
    }

    public IReadOnlyList<PerformanceTestGroup> Groups { get; }
}
