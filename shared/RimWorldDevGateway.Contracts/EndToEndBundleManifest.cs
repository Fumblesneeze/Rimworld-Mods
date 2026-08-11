using System;
using System.Runtime.Serialization;

namespace RimWorldDevGateway.Contracts;

[DataContract]
public sealed class EndToEndBundleManifest
{
    public const string SchemaValue = "RimWorldDevGateway.EndToEndTests/v1";

    [DataMember(Name = "schema", IsRequired = true, Order = 1)]
    public string Schema { get; set; } = SchemaValue;

    [DataMember(Name = "ownerPackageId", IsRequired = true, Order = 2)]
    public string OwnerPackageId { get; set; } = string.Empty;

    [DataMember(Name = "assembly", IsRequired = true, Order = 3)]
    public string Assembly { get; set; } = string.Empty;

    [DataMember(Name = "assemblyIdentity", IsRequired = true, Order = 4)]
    public string AssemblyIdentity { get; set; } = string.Empty;

    [DataMember(Name = "moduleVersionId", IsRequired = true, Order = 5)]
    public string ModuleVersionId { get; set; } = string.Empty;

    [DataMember(Name = "assemblyLength", IsRequired = true, Order = 6)]
    public long AssemblyLength { get; set; }

    [DataMember(Name = "assemblySha256", IsRequired = true, Order = 7)]
    public string AssemblySha256 { get; set; } = string.Empty;

    [DataMember(Name = "dependencies", IsRequired = true, Order = 8)]
    public EndToEndBundleDependency[] Dependencies { get; set; } = System.Array.Empty<EndToEndBundleDependency>();

    [DataMember(Name = "tests", IsRequired = true, Order = 9)]
    public EndToEndBundleTest[] Tests { get; set; } = System.Array.Empty<EndToEndBundleTest>();
}

[DataContract]
public sealed class EndToEndBundleDependency
{
    [DataMember(Name = "name", IsRequired = true, Order = 1)]
    public string Name { get; set; } = string.Empty;

    [DataMember(Name = "version", IsRequired = true, Order = 2)]
    public string Version { get; set; } = string.Empty;

    [DataMember(Name = "culture", IsRequired = true, Order = 3)]
    public string Culture { get; set; } = string.Empty;

    [DataMember(Name = "keyKind", IsRequired = true, Order = 4)]
    public string KeyKind { get; set; } = string.Empty;

    [DataMember(Name = "publicKeyOrToken", IsRequired = true, Order = 5)]
    public string PublicKeyOrToken { get; set; } = string.Empty;

    [DataMember(Name = "identity", IsRequired = true, Order = 6)]
    public string Identity { get; set; } = string.Empty;
}

[DataContract]
public sealed class EndToEndBundleTest
{
    public const string EndToEndKind = "e2e";
    public const string PerformanceKind = "performance";

    [DataMember(Name = "id", IsRequired = true, Order = 1)]
    public string Id { get; set; } = string.Empty;

    [DataMember(Name = "typeName", IsRequired = true, Order = 2)]
    public string TypeName { get; set; } = string.Empty;

    [DataMember(Name = "ownerPackageId", IsRequired = true, Order = 3)]
    public string OwnerPackageId { get; set; } = string.Empty;

    [DataMember(Name = "activePackageIds", IsRequired = true, Order = 4)]
    public string[] ActivePackageIds { get; set; } = System.Array.Empty<string>();

    [DataMember(Name = "maxFrames", IsRequired = true, Order = 5)]
    public int MaxFrames { get; set; }

    [DataMember(Name = "maxGameTicks", IsRequired = true, Order = 6)]
    public int MaxGameTicks { get; set; }

    [DataMember(Name = "maxWallClockSeconds", IsRequired = true, Order = 7)]
    public int MaxWallClockSeconds { get; set; }

    [DataMember(Name = "kind", EmitDefaultValue = false, Order = 8)]
    public string Kind { get; set; } = EndToEndKind;

    [DataMember(Name = "measuredSubjectPackageId", EmitDefaultValue = false, Order = 9)]
    public string? MeasuredSubjectPackageId { get; set; }

    [DataMember(Name = "deterministicSeed", EmitDefaultValue = false, Order = 10)]
    public int DeterministicSeed { get; set; }

    [DataMember(Name = "workloadVersion", EmitDefaultValue = false, Order = 11)]
    public string? WorkloadVersion { get; set; }

    [DataMember(Name = "comparisonId", EmitDefaultValue = false, Order = 12)]
    public string? ComparisonId { get; set; }

    [DataMember(Name = "warmUpTicks", EmitDefaultValue = false, Order = 13)]
    public int WarmUpTicks { get; set; }

    [DataMember(Name = "sampleTicks", EmitDefaultValue = false, Order = 14)]
    public int SampleTicks { get; set; }

    [DataMember(Name = "gameSpeed", EmitDefaultValue = false, Order = 15)]
    public int GameSpeed { get; set; }

    [DataMember(Name = "repetitions", EmitDefaultValue = false, Order = 16)]
    public int Repetitions { get; set; }

    [DataMember(Name = "evidenceLens", EmitDefaultValue = false, Order = 17)]
    public int EvidenceLens { get; set; }

    [DataMember(Name = "productAbsentControlId", EmitDefaultValue = false, Order = 18)]
    public string? ProductAbsentControlId { get; set; }

    [DataMember(Name = "methodSelectors", EmitDefaultValue = false, Order = 19)]
    public PerformanceBundleMethodSelector[]? MethodSelectors { get; set; }

    [DataMember(Name = "throughputCheckpoints", EmitDefaultValue = false, Order = 20)]
    public PerformanceBundleThroughputCheckpoint[]? ThroughputCheckpoints { get; set; }

    [DataMember(Name = "performanceProfiler", EmitDefaultValue = false, Order = 21)]
    public string? PerformanceProfiler { get; set; }

    [DataMember(Name = "diagnosticSelector", EmitDefaultValue = false, Order = 22)]
    public string? DiagnosticSelector { get; set; }
}

[DataContract]
public sealed class PerformanceBundleMethodSelector
{
    [DataMember(Name = "kind", IsRequired = true, Order = 1)]
    public int Kind { get; set; }

    [DataMember(Name = "value", IsRequired = true, Order = 2)]
    public string Value { get; set; } = string.Empty;

    [DataMember(Name = "category", IsRequired = true, Order = 3)]
    public string Category { get; set; } = string.Empty;
}

[DataContract]
public sealed class PerformanceBundleThroughputCheckpoint
{
    [DataMember(Name = "id", IsRequired = true, Order = 1)]
    public string Id { get; set; } = string.Empty;

    [DataMember(Name = "minimumCount", IsRequired = true, Order = 2)]
    public long MinimumCount { get; set; }
}

public readonly struct PerformanceBundleDeadline
{
    private const int SetupAndCleanupTickAllowance = 6000;
    private const int SetupAndCleanupWallSeconds = 180;
    private const int AssumedMaximumRenderedFramesPerSecond = 120;

    private PerformanceBundleDeadline(int maxFrames, int maxGameTicks, int maxWallClockSeconds)
    {
        MaxFrames = maxFrames;
        MaxGameTicks = maxGameTicks;
        MaxWallClockSeconds = maxWallClockSeconds;
    }

    public int MaxFrames { get; }
    public int MaxGameTicks { get; }
    public int MaxWallClockSeconds { get; }

    public static PerformanceBundleDeadline Calculate(int warmUpTicks, int sampleTicks)
    {
        if (warmUpTicks < 0 || sampleTicks <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleTicks));
        var totalTicks = checked((long)warmUpTicks + sampleTicks);
        if (totalTicks + SetupAndCleanupTickAllowance > int.MaxValue)
            throw new ArgumentOutOfRangeException(
                nameof(sampleTicks),
                "The performance phases cannot fit the E2E game-tick deadline representation.");
        var wallSeconds = checked((int)Math.Max(
            SetupAndCleanupWallSeconds,
            totalTicks / 60L + SetupAndCleanupWallSeconds));
        var frames = wallSeconds > int.MaxValue / AssumedMaximumRenderedFramesPerSecond
            ? int.MaxValue
            : wallSeconds * AssumedMaximumRenderedFramesPerSecond;
        return new PerformanceBundleDeadline(
            frames,
            checked((int)(totalTicks + SetupAndCleanupTickAllowance)),
            wallSeconds);
    }
}

[DataContract]
public sealed class EndToEndStageMarker
{
    public const string SchemaValue = "RimWorldDevGateway.EndToEndStage/v1";
    public const string FileName = ".rimworld-dev-gateway-e2e-stage.json";

    [DataMember(Name = "schema", IsRequired = true, Order = 1)]
    public string Schema { get; set; } = SchemaValue;

    [DataMember(Name = "ownerPackageId", IsRequired = true, Order = 2)]
    public string OwnerPackageId { get; set; } = string.Empty;

    [DataMember(Name = "rimWorldVersion", IsRequired = true, Order = 3)]
    public string RimWorldVersion { get; set; } = string.Empty;

    [DataMember(Name = "transactionId", IsRequired = true, Order = 4)]
    public string TransactionId { get; set; } = string.Empty;

    [DataMember(Name = "assemblies", IsRequired = true, Order = 5)]
    public string[] Assemblies { get; set; } = System.Array.Empty<string>();

    [DataMember(Name = "manifests", IsRequired = true, Order = 6)]
    public string[] Manifests { get; set; } = System.Array.Empty<string>();
}
