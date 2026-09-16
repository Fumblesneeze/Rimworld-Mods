using System.Text.Json;

namespace RimWorldModding.Mcp;

internal static class OperationJson
{
    public static string Serialize(object value) => value switch
    {
        RepositoryStatusResult status => JsonSerializer.Serialize(status, McpJsonContext.Default.RepositoryStatusResult),
        List<OperationDescriptor> descriptors => JsonSerializer.Serialize(descriptors, McpJsonContext.Default.ListOperationDescriptor),
        ReleaseProfileResult profile => JsonSerializer.Serialize(profile, McpJsonContext.Default.ReleaseProfileResult),
        ModListState state => JsonSerializer.Serialize(state, McpJsonContext.Default.ModListState),
        ModListMutationResult mutation => JsonSerializer.Serialize(mutation, McpJsonContext.Default.ModListMutationResult),
        LocalModInstallResult install => JsonSerializer.Serialize(install, McpJsonContext.Default.LocalModInstallResult),
        ModBuildResult build => JsonSerializer.Serialize(build, McpJsonContext.Default.ModBuildResult),
        WorkshopPresentationSyncResult presentationSync => JsonSerializer.Serialize(presentationSync, McpJsonContext.Default.WorkshopPresentationSyncResult),
        ReleasePreparationResult preparation => JsonSerializer.Serialize(preparation, McpJsonContext.Default.ReleasePreparationResult),
        AdapterOperationResult adapter => JsonSerializer.Serialize(adapter, McpJsonContext.Default.AdapterOperationResult),
        RunStartResult runStart => JsonSerializer.Serialize(runStart, McpJsonContext.Default.RunStartResult),
        RunStatusResult runStatus => JsonSerializer.Serialize(runStatus, McpJsonContext.Default.RunStatusResult),
        ReleaseCandidateStage candidate => JsonSerializer.Serialize(candidate, McpJsonContext.Default.ReleaseCandidateStage),
        EvidenceReadResult evidence => JsonSerializer.Serialize(evidence, McpJsonContext.Default.EvidenceReadResult),
        ReleasePlanStatusResult releaseStatus => JsonSerializer.Serialize(releaseStatus, McpJsonContext.Default.ReleasePlanStatusResult),
        ReleasePublishResult releasePublish => JsonSerializer.Serialize(releasePublish, McpJsonContext.Default.ReleasePublishResult),
        WorkshopSubscriptionCleanupResult subscriptionCleanup => JsonSerializer.Serialize(subscriptionCleanup, McpJsonContext.Default.WorkshopSubscriptionCleanupResult),
        GatewayRawMutationResult rawMutation => JsonSerializer.Serialize(rawMutation, McpJsonContext.Default.GatewayRawMutationResult),
        GatewayScreenshotResult screenshot => JsonSerializer.Serialize(screenshot, McpJsonContext.Default.GatewayScreenshotResult),
        GatewayDiagnosticResult diagnostic => JsonSerializer.Serialize(diagnostic, McpJsonContext.Default.GatewayDiagnosticResult),
        _ => throw new InvalidOperationException(
            $"No deterministic JSON projection is registered for {value.GetType().FullName}.")
    };
}
