using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace RimWorldModding.Mcp;

[McpServerToolType]
public static class McpTools
{
    [McpServerTool(Name = "operation_list"), Description("List typed RimWorld repository operations and their risk/lifecycle metadata.")]
    public static async Task<string> ListOperations(OperationRegistry registry, CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync("operation_list", EmptyArguments(), cancellationToken));

    [McpServerTool(Name = "repository_status"), Description("Inspect canonical mods, release profiles, git revision, worktree state, and available operations in this RimWorld repository.")]
    public static async Task<string> RepositoryStatus(OperationRegistry registry, CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync("repository_status", EmptyArguments(), cancellationToken));

    [McpServerTool(Name = "openspec_validate"), Description("Run strict non-interactive validation for every OpenSpec change and capability.")]
    public static async Task<string> ValidateOpenSpec(OperationRegistry registry, CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync("openspec_validate", EmptyArguments(), cancellationToken));

    [McpServerTool(Name = "mod_build"), Description("Build one canonical mod through its universal release profile.")]
    public static async Task<string> BuildMod(
        OperationRegistry registry,
        [Description("Canonical package ID.")] string packageId,
        [Description("Build configuration: Release or Debug.")] string? configuration,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "mod_build", JsonSerializer.SerializeToElement(new { packageId, configuration }), cancellationToken));

    [McpServerTool(Name = "test_run"), Description("Run one exact registered host-test suite and optional exact dotnet test filter.")]
    public static async Task<string> RunTests(
        OperationRegistry registry,
        [Description("Exact suite ID accepted by the repository test runner.")] string suite,
        [Description("Optional exact dotnet test filter.")] string? filter,
        [Description("Build configuration: Release or Debug.")] string? configuration,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "test_run", JsonSerializer.SerializeToElement(new { suite, filter, configuration }), cancellationToken));

    [McpServerTool(Name = "package_validate"), Description("Validate and hash one universal profile's exact allowlisted product package.")]
    public static async Task<string> ValidatePackage(
        OperationRegistry registry,
        [Description("Canonical package ID.")] string packageId,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "package_validate", JsonSerializer.SerializeToElement(new { packageId }), cancellationToken));

    [McpServerTool(Name = "e2e_run_start"), Description("Run one exact grouped or test-selected isolated native player workflow and retain evidence.")]
    public static async Task<string> RunEndToEnd(
        OperationRegistry registry,
        [Description("Optional exact active-mod group ID; mutually exclusive with testId.")] string? groupId,
        [Description("Optional exact E2E test ID; mutually exclusive with groupId.")] string? testId,
        [Description("RimWorld language folder, default English.")] string? language,
        [Description("Scenario timeout in seconds, 60-3600.")] int? timeoutSeconds,
        [Description("True validates discovery and launch inputs without launching RimWorld.")] bool? dryRun,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "e2e_run_start",
            JsonSerializer.SerializeToElement(new { groupId, testId, language, timeoutSeconds, dryRun }),
            cancellationToken));

    [McpServerTool(Name = "game_run_start"), Description("Start one leased isolated minimized Gateway-backed RimWorld process.")]
    public static async Task<string> StartGame(
        OperationRegistry registry,
        [Description("Optional ordered additional package IDs; Gateway remains last.")] string[]? packageIds,
        [Description("Optional matching repository-owned mod project paths to build/deploy.")] string[]? projectPaths,
        [Description("Interactive hold duration in seconds, 60-7200.")] int? holdSeconds,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "game_run_start",
            JsonSerializer.SerializeToElement(new { packageIds, projectPaths, holdSeconds }),
            cancellationToken));

    [McpServerTool(Name = "run_status"), Description("Inspect one exact leased run, process identities, Gateway manifest, and bounded log tails.")]
    public static async Task<string> RunStatus(
        OperationRegistry registry,
        [Description("Exact run ID returned by game_run_start.")] string runId,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "run_status", JsonSerializer.SerializeToElement(new { runId }), cancellationToken));

    [McpServerTool(Name = "run_cancel"), Description("Request graceful completion for one exact leased run, with exact-PID bounded fallback.")]
    public static async Task<string> CancelRun(
        OperationRegistry registry,
        [Description("Exact run ID returned by game_run_start.")] string runId,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "run_cancel", JsonSerializer.SerializeToElement(new { runId }), cancellationToken));

    [McpServerTool(Name = "gateway_health"), Description("Read authenticated status from the exact live Gateway session selected by a leased run.")]
    public static async Task<string> GatewayHealth(
        OperationRegistry registry,
        [Description("Exact ready run ID.")] string runId,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "gateway_health", JsonSerializer.SerializeToElement(new { runId }), cancellationToken));

    [McpServerTool(Name = "gateway_diagnostic"), Description("Run one allowlisted read-only Gateway diagnostic against an exact leased live process.")]
    public static async Task<string> GatewayDiagnostic(
        OperationRegistry registry,
        [Description("Exact ready run ID.")] string runId,
        [Description("discover, status, ui-state, logs, or automations.")] string command,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "gateway_diagnostic", JsonSerializer.SerializeToElement(new { runId, command }), cancellationToken));

    [McpServerTool(Name = "gateway_mutation"), Description("Invoke one explicit semantic action or registered automation against an exact leased live process.")]
    public static async Task<string> GatewayMutation(
        OperationRegistry registry,
        [Description("Exact ready run ID.")] string runId,
        [Description("action for a semantic action or automation for a registered session automation.")] string kind,
        [Description("Exact registered action or automation name.")] string name,
        [Description("One JSON object containing typed arguments.")] string? argumentsJson,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "gateway_mutation", JsonSerializer.SerializeToElement(new { runId, kind, name, argumentsJson }), cancellationToken));

    [McpServerTool(Name = "gateway_raw_mutation"), Description("Compile and execute one bounded raw C# mutation in an exact leased live Gateway process. For one-off development/diagnosis only; it never proves gameplay acceptance.")]
    public static async Task<string> GatewayRawMutation(
        OperationRegistry registry,
        [Description("Exact ready run ID.")] string runId,
        [Description("Complete bounded C# source with the declared static entry point.")] string sourceCode,
        [Description("Fully qualified static entry type exposing Execute.")] string entryType,
        [Description("One JSON request object passed to Execute.")] string? requestJson,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "gateway_raw_mutation",
            JsonSerializer.SerializeToElement(new { runId, sourceCode, entryType, requestJson }),
            cancellationToken));

    [McpServerTool(Name = "evidence_read"), Description("Read and hash one bounded UTF-8 evidence file beneath repository artifacts.")]
    public static async Task<string> ReadEvidence(
        OperationRegistry registry,
        [Description("Repository-relative evidence file beneath artifacts.")] string path,
        [Description("Maximum returned content bytes, 1-1048576.")] int? maximumBytes,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "evidence_read", JsonSerializer.SerializeToElement(new { path, maximumBytes }), cancellationToken));

    [McpServerTool(Name = "release_profile_validate"), Description("Validate one universal per-mod release profile and canonical identity without building or mutating Steam.")]
    public static async Task<string> ValidateReleaseProfile(
        OperationRegistry registry,
        [Description("Canonical RimWorld package ID, for example author.modname.")] string packageId,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "release_profile_validate",
            JsonSerializer.SerializeToElement(new { packageId }),
            cancellationToken));

    [McpServerTool(Name = "modlist_inspect"), Description("Inspect the exact normal RimWorld ModsConfig.xml path, hash, and ordered active package IDs without changing it.")]
    public static async Task<string> InspectModList(
        OperationRegistry registry,
        [Description("Optional exact ModsConfig.xml path; omit for the current user's normal RimWorld config.")] string? configPath,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "modlist_inspect",
            JsonSerializer.SerializeToElement(new { configPath }),
            cancellationToken));

    [McpServerTool(Name = "modlist_enable"), Description("Transactionally enable one package after an exact anchor in the normal RimWorld mod list, retaining a backup and hashes.")]
    public static async Task<string> EnableMod(
        OperationRegistry registry,
        [Description("Canonical package ID to enable exactly once.")] string packageId,
        [Description("Existing canonical package ID after which the new entry is inserted.")] string anchorPackageId,
        [Description("Optional exact ModsConfig.xml path; omit for the current user's normal RimWorld config.")] string? configPath,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "modlist_enable",
            JsonSerializer.SerializeToElement(new { packageId, anchorPackageId, configPath }),
            cancellationToken));

    [McpServerTool(Name = "modlist_disable"), Description("Transactionally remove one canonical package ID from the normal RimWorld mod list, retaining a backup and hashes.")]
    public static async Task<string> DisableMod(
        OperationRegistry registry,
        [Description("Canonical package ID to disable.")] string packageId,
        [Description("Optional exact ModsConfig.xml path; omit for the current user's normal RimWorld config.")] string? configPath,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "modlist_disable",
            JsonSerializer.SerializeToElement(new { packageId, configPath }),
            cancellationToken));

    [McpServerTool(Name = "modlist_restore"), Description("Restore one retained ModsConfig backup only when the current hash still matches.")]
    public static async Task<string> RestoreModList(
        OperationRegistry registry,
        [Description("Repository-relative backup path beneath artifacts/UserConfigBackups/RimWorld.")] string backupPath,
        [Description("SHA-256 of the current ModsConfig.xml that may be replaced.")] string expectedCurrentSha256,
        [Description("Optional exact ModsConfig.xml path; omit for the current user's normal RimWorld config.")] string? configPath,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "modlist_restore",
            JsonSerializer.SerializeToElement(new { backupPath, expectedCurrentSha256, configPath }),
            cancellationToken));

    [McpServerTool(Name = "local_mod_sync"), Description("Atomically synchronize an allowlisted built mod package into the local RimWorld Mods directory and retain the prior copy as a recoverable backup.")]
    public static async Task<string> SyncLocalMod(
        OperationRegistry registry,
        [Description("Canonical package ID whose universal release profile selects the package files.")] string packageId,
        [Description("Optional exact RimWorld Mods root; omit for the repository's configured local installation.")] string? modsRoot,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "local_mod_sync",
            JsonSerializer.SerializeToElement(new { packageId, modsRoot }),
            cancellationToken));

    [McpServerTool(Name = "release_prepare"), Description("Build, validate, stage, hash, and perform a mutation-free owner/title preflight for one universal release profile. Returns a digest and nonce; never mutates Steam.")]
    public static async Task<string> PrepareRelease(
        OperationRegistry registry,
        [Description("Canonical package ID selected from a universal release profile.")] string packageId,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "release_prepare",
            JsonSerializer.SerializeToElement(new { packageId }),
            cancellationToken));

    [McpServerTool(Name = "release_status"), Description("Revalidate one exact release plan/digest/nonce and inspect durable publication state without mutating Steam.")]
    public static async Task<string> ReleaseStatus(
        OperationRegistry registry,
        [Description("Exact retained publication-plan path returned by release_prepare.")] string planPath,
        [Description("Exact SHA-256 returned by release_prepare.")] string planSha256,
        [Description("Exact one-time nonce returned by release_prepare.")] string confirmationNonce,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "release_status",
            JsonSerializer.SerializeToElement(new { planPath, planSha256, confirmationNonce }),
            cancellationToken));

    [McpServerTool(Name = "release_publish"), Description("Publish one exact admitted plan, verify Steam, reacquire the subscriber copy, run its native player workflow, persist identity, and restore the local package. This mutates Steam.")]
    public static async Task<string> PublishRelease(
        OperationRegistry registry,
        [Description("Exact retained publication-plan path reviewed by the user.")] string planPath,
        [Description("Exact reviewed publication-plan SHA-256.")] string planSha256,
        [Description("Exact reviewed one-time confirmation nonce.")] string confirmationNonce,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "release_publish",
            JsonSerializer.SerializeToElement(new { planPath, planSha256, confirmationNonce }),
            cancellationToken));

    [McpServerTool(Name = "release_accept_subscriber_evidence"), Description("After personally inspecting the exact fresh subscriber screenshots, hash and retain that concrete observation to complete the local release evidence. Never infers visual acceptance.")]
    public static async Task<string> AcceptSubscriberEvidence(
        OperationRegistry registry,
        [Description("Exact retained publication-plan path.")] string planPath,
        [Description("Exact publication-plan SHA-256.")] string planSha256,
        [Description("Exact one-time confirmation nonce.")] string confirmationNonce,
        [Description("Exact publication receipt returned by release_publish.")] string receiptPath,
        [Description("Concrete personally observed action/result in the retained subscriber screenshots.")] string observation,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "release_accept_subscriber_evidence",
            JsonSerializer.SerializeToElement(new { planPath, planSha256, confirmationNonce, receiptPath, observation }),
            cancellationToken));

    private static JsonElement EmptyArguments() => JsonDocument.Parse("{}").RootElement.Clone();
}
