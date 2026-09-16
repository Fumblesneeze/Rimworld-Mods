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

    [McpServerTool(Name = "mod_build"), Description("Build one canonical mod and install the successful package in the local RimWorld Mods directory.")]
    public static async Task<string> BuildMod(
        OperationRegistry registry,
        [Description("Canonical package ID.")] string packageId,
        [Description("Build configuration: Release or Debug.")] string? configuration,
        [Description("Optional exact RimWorld Mods root; omit for the configured local installation.")] string? modsRoot,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "mod_build", JsonSerializer.SerializeToElement(new { packageId, configuration, modsRoot }), cancellationToken));

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
        [Description("True enables RimWorld master audio for sound-focused work; omitted or false keeps the isolated run muted.")] bool? enableAudio,
        [Description("Optional repository-contained E2E .csproj path that scopes metadata discovery to that owning project.")] string? projectPath,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "e2e_run_start",
            JsonSerializer.SerializeToElement(new { groupId, testId, language, timeoutSeconds, dryRun, enableAudio, projectPath }),
            cancellationToken));

    [McpServerTool(Name = "game_run_start"), Description("Start one leased isolated minimized Gateway-backed RimWorld process.")]
    public static async Task<string> StartGame(
        OperationRegistry registry,
        [Description("Optional ordered additional package IDs; Gateway remains last.")] string[]? packageIds,
        [Description("Optional matching repository-owned mod project paths to build/deploy.")] string[]? projectPaths,
        [Description("Interactive hold duration in seconds, 60-7200.")] int? holdSeconds,
        [Description("True enables RimWorld master audio for sound-focused work; omitted or false keeps the isolated run muted.")] bool? enableAudio,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "game_run_start",
            JsonSerializer.SerializeToElement(new { packageIds, projectPaths, holdSeconds, enableAudio }),
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

    [McpServerTool(Name = "gateway_logs"), Description("Read chronological logs with an exclusive sequence cursor. Resume using the final returned entry's Sequence.")]
    public static async Task<string> GatewayLogs(OperationRegistry registry, string runId,
        CancellationToken cancellationToken, long after = 0, int limit = 100) =>
        OperationJson.Serialize(await registry.InvokeAsync("gateway_logs",
            JsonSerializer.SerializeToElement(new { runId, after, limit }), cancellationToken));

    [McpServerTool(Name = "gateway_errors"), Description("Query grouped errors or inspect an exact error ID with captured frames, causes and current Harmony attribution.")]
    public static async Task<string> GatewayErrors(OperationRegistry registry, string runId,
        CancellationToken cancellationToken, string? id = null, long after = 0, int limit = 50, string? filter = null) =>
        OperationJson.Serialize(await registry.InvokeAsync("gateway_errors",
            JsonSerializer.SerializeToElement(new { runId, id, after, limit, filter }), cancellationToken));

    [McpServerTool(Name = "gateway_methods"), Description("Discover methods of one exact loaded type; results identify overloads by module MVID and metadata token.")]
    public static async Task<string> GatewayMethods(OperationRegistry registry, string runId, string typeName,
        CancellationToken cancellationToken, string? assemblyName = null, string? methodName = null, int offset = 0, int limit = 50) =>
        OperationJson.Serialize(await registry.InvokeAsync("gateway_methods",
            JsonSerializer.SerializeToElement(new { runId, typeName, assemblyName, methodName, offset, limit }), cancellationToken));

    [McpServerTool(Name = "gateway_decompile"), Description("Decompile one exact method on the host. merged=true reconstructs current Harmony IL and executes transpilers; it never installs the reconstructed method. Retains PE/C#/provenance.")]
    public static async Task<string> GatewayDecompile(OperationRegistry registry, string runId, string methodHandle,
        CancellationToken cancellationToken, bool merged = false) =>
        OperationJson.Serialize(await registry.InvokeAsync("gateway_decompile",
            JsonSerializer.SerializeToElement(new { runId, methodHandle, merged }), cancellationToken));

    [McpServerTool(Name = "gateway_error_report"), Description("Export one exact retained error to credential-free Markdown and JSON under its leased run's evidence directory.")]
    public static async Task<string> GatewayErrorReport(OperationRegistry registry, string runId, string errorId,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync("gateway_error_report",
            JsonSerializer.SerializeToElement(new { runId, errorId }), cancellationToken));

    [McpServerTool(Name = "gateway_screenshot"), Description("Capture the exact leased game's rendered view to a unique PNG under its evidence directory. Returns process identity and PNG hash; does not change game state.")]
    public static async Task<string> GatewayScreenshot(
        OperationRegistry registry,
        [Description("Exact ready run ID.")] string runId,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "gateway_screenshot", JsonSerializer.SerializeToElement(new { runId }), cancellationToken));

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

    [McpServerTool(Name = "gateway_scene_time"), Description("Set current-map local time or restore a captured calendar offset without advancing simulation ticks. Disposable scene setup only.")]
    public static async Task<string> GatewaySceneTime(
        OperationRegistry registry,
        [Description("Exact ready run ID.")] string runId,
        [Description("Exact current map handle, for example map-0.")] string mapHandle,
        [Description("Local minute of day, 0–1439; noon is 720. Supply this or gameStartAbsTick.")] int? minuteOfDay,
        [Description("Previously returned Before.GameStartAbsTick for restoration. Supply this or minuteOfDay.")] int? gameStartAbsTick,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync("gateway_scene_time",
            JsonSerializer.SerializeToElement(new { runId, mapHandle, minuteOfDay, gameStartAbsTick }), cancellationToken));

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

    [McpServerTool(Name = "local_mod_sync"), Description("Synchronize an allowlisted built mod package into the local RimWorld Mods directory without retaining an install backup.")]
    public static async Task<string> SyncLocalMod(
        OperationRegistry registry,
        [Description("Canonical package ID whose universal release profile selects the package files.")] string packageId,
        [Description("Optional exact RimWorld Mods root; omit for the repository's configured local installation.")] string? modsRoot,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "local_mod_sync",
            JsonSerializer.SerializeToElement(new { packageId, modsRoot }),
            cancellationToken));

    [McpServerTool(Name = "presentation_render"), Description("Render the selected profile's authored Workshop images through its hash-pinned offline adapter. Allows uncommitted authoring changes; never launches RimWorld, installs a mod, or changes Steam.")]
    public static async Task<string> RenderPresentation(
        OperationRegistry registry,
        [Description("Canonical package ID selected from a universal release profile.")] string packageId,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "presentation_render",
            JsonSerializer.SerializeToElement(new { packageId }),
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

    [McpServerTool(Name = "release_presentation_sync"), Description("Synchronize reviewed additional Workshop previews, verify their current Steam-hosted bytes, and resolve the versioned description against those current identities. This mutates Steam previews but does not consume a player-facing change note.")]
    public static async Task<string> SynchronizeReleasePresentation(
        OperationRegistry registry,
        [Description("Canonical package ID selected from a universal release profile.")] string packageId,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "release_presentation_sync",
            JsonSerializer.SerializeToElement(new { packageId }),
            cancellationToken));

    [McpServerTool(Name = "release_status"), Description("Revalidate one exact release plan/digest/nonce and inspect durable publication state without mutating Steam.")]
    public static async Task<string> ReleaseStatus(
        OperationRegistry registry,
        [Description("Exact retained publication-plan path returned by release_prepare.")] string planPath,
        [Description("Exact SHA-256 returned by release_prepare.")] string planSha256,
        [Description("Legacy-named exact one-time admission nonce returned by release_prepare; no separate user confirmation is required.")] string confirmationNonce,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "release_status",
            JsonSerializer.SerializeToElement(new { planPath, planSha256, confirmationNonce }),
            cancellationToken));

    [McpServerTool(Name = "release_publish"), Description("Publish one exact admitted plan and complete only after Steam reports the expected item with a newer modified time. This mutates Steam; gameplay testing is a pre-release responsibility.")]
    public static async Task<string> PublishRelease(
        OperationRegistry registry,
        [Description("Exact retained prepared publication-plan path matching the user's publication order.")] string planPath,
        [Description("Exact admitted publication-plan SHA-256.")] string planSha256,
        [Description("Legacy-named exact one-time admission nonce for the prepared release plan; no separate user confirmation is required.")] string confirmationNonce,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "release_publish",
            JsonSerializer.SerializeToElement(new { planPath, planSha256, confirmationNonce }),
            cancellationToken));

    [McpServerTool(Name = "release_subscription_cleanup"), Description("Remove a repository-owned mod's temporary Workshop subscription and verify the exact client item-state is no longer subscribed.")]
    public static async Task<string> CleanupReleaseSubscription(
        OperationRegistry registry,
        [Description("Canonical package ID whose checked-in release profile owns the Workshop item.")] string packageId,
        CancellationToken cancellationToken) =>
        OperationJson.Serialize(await registry.InvokeAsync(
            "release_subscription_cleanup",
            JsonSerializer.SerializeToElement(new { packageId }),
            cancellationToken));

    private static JsonElement EmptyArguments() => JsonDocument.Parse("{}").RootElement.Clone();
}
