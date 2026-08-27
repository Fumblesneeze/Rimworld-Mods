using System.Text.Json;
using System.Text.Json.Serialization;

namespace RimWorldModding.Mcp;

public enum OperationRisk
{
    Read,
    WorkspaceWrite,
    ExternalWrite,
    DestructiveLocal
}

public sealed record OperationDescriptor(
    string Name,
    string Description,
    OperationRisk Risk,
    bool LongRunning,
    int TimeoutSeconds,
    string EvidencePolicy);

public sealed record ModSummary(
    string DisplayName,
    string PackageId,
    string ProjectPath,
    string DistributionKind,
    string? ReleaseProfilePath,
    IReadOnlyList<string> TestProjects);

public sealed record RepositoryStatusResult(
    string RepositoryRoot,
    string HeadRevision,
    bool WorktreeClean,
    IReadOnlyList<ModSummary> Mods,
    IReadOnlyList<OperationDescriptor> Operations);

public sealed record ReleaseProfileResult(
    string ProfilePath,
    string PackageId,
    string Title,
    string DistributionKind,
    string Visibility,
    string? PublishedFileId,
    bool AllowFirstPublication,
    IReadOnlyList<string> RequiredWorkshopItems,
    IReadOnlyList<string> RequiredDlcAppIds,
    string Project,
    string PackageSource,
    string Description,
    string Preview);

public sealed record ModBuildResult(
    AdapterOperationResult Build,
    LocalModInstallResult Installation);

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, WriteIndented = true)]
[JsonSerializable(typeof(RepositoryStatusResult))]
[JsonSerializable(typeof(List<OperationDescriptor>))]
[JsonSerializable(typeof(OperationDescriptor[]))]
[JsonSerializable(typeof(ReleaseProfileResult))]
[JsonSerializable(typeof(ModListState))]
[JsonSerializable(typeof(ModListMutationResult))]
[JsonSerializable(typeof(LocalModInstallResult))]
[JsonSerializable(typeof(ReleasePublicationPlan))]
[JsonSerializable(typeof(ReleaseProfile))]
[JsonSerializable(typeof(ReleasePreparationResult))]
[JsonSerializable(typeof(AdapterOperationResult))]
[JsonSerializable(typeof(ModBuildResult))]
[JsonSerializable(typeof(RunLeaseRecord))]
[JsonSerializable(typeof(RunStartResult))]
[JsonSerializable(typeof(RunStatusResult))]
[JsonSerializable(typeof(ReleaseCandidateStage))]
[JsonSerializable(typeof(EvidenceReadResult))]
[JsonSerializable(typeof(ReleasePlanStatusResult))]
[JsonSerializable(typeof(ReleasePublishResult))]
[JsonSerializable(typeof(ReleaseReviewResult))]
[JsonSerializable(typeof(SubscriberVerificationResult))]
[JsonSerializable(typeof(GatewayRawMutationResult))]
[JsonSerializable(typeof(ReleaseWorkerRequest))]
[JsonSerializable(typeof(ReleaseWorkerLease))]
[JsonSerializable(typeof(SubscriberRecoveryRecord))]
[JsonSerializable(typeof(WorkshopSubscriptionCleanupResult))]
internal sealed partial class McpJsonContext : JsonSerializerContext;
