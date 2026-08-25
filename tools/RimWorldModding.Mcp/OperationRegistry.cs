using System.Text.Json;

namespace RimWorldModding.Mcp;

public sealed class OperationRegistry
{
    private readonly string _repositoryRoot;
    private readonly IReadOnlyDictionary<string, OperationRegistration> _operations;

    private OperationRegistry(string repositoryRoot, IEnumerable<OperationRegistration> operations)
    {
        _repositoryRoot = RepositoryRoot.Resolve(repositoryRoot);
        _operations = operations.ToDictionary(item => item.Descriptor.Name, StringComparer.Ordinal);
    }

    public IReadOnlyList<OperationDescriptor> Descriptors => _operations.Values
        .Select(item => item.Descriptor)
        .OrderBy(item => item.Name, StringComparer.Ordinal)
        .ToArray();

    public static OperationRegistry CreateDefault(string repositoryRoot)
    {
        var registrations = new[]
        {
            Register(
                "operation_list",
                "List typed repository operations and their safety metadata.",
                OperationRisk.Read,
                longRunning: false,
                timeoutSeconds: 10,
                "No durable evidence; result is a live catalog snapshot.",
                static (registry, _, _) => Task.FromResult<object>(registry.Descriptors.ToList())),
            Register(
                "repository_status",
                "Inspect canonical mod, profile, source-revision, and operation status for this repository.",
                OperationRisk.Read,
                longRunning: false,
                timeoutSeconds: 30,
                "No secrets; git and metadata snapshot only.",
                static async (registry, _, cancellationToken) => await registry.ReadRepositoryStatusAsync(cancellationToken)),
            Register(
                "openspec_validate",
                "Run strict non-interactive validation for every OpenSpec change and capability.",
                OperationRisk.WorkspaceWrite,
                longRunning: false,
                timeoutSeconds: 180,
                "Returns bounded validator output; OpenSpec owns any generated cache state.",
                static async (registry, _, cancellationToken) => await RepositoryOperations.ExecuteAsync(
                    RepositoryOperationPlanner.OpenSpecValidate(registry._repositoryRoot), cancellationToken)),
            Register(
                "mod_build",
                "Build one mod selected by canonical package ID with the repository-local SDK.",
                OperationRisk.WorkspaceWrite,
                longRunning: false,
                timeoutSeconds: 300,
                "Writes only ordinary repository build/package artifacts.",
                static async (registry, arguments, cancellationToken) =>
                {
                    var profile = registry.RequiredProfile(RequiredString(arguments, "packageId"));
                    return await RepositoryOperations.ExecuteAsync(
                        RepositoryOperationPlanner.ModBuild(
                            registry._repositoryRoot,
                            profile.Project,
                            OptionalString(arguments, "configuration") ?? "Release"),
                        cancellationToken);
                }),
            Register(
                "test_run",
                "Run one exact registered host-test suite and optional exact dotnet test filter; zero executed tests remain a failure.",
                OperationRisk.WorkspaceWrite,
                longRunning: true,
                timeoutSeconds: 900,
                "The retained test adapter writes canonical logs/TRX beneath artifacts/TestResults.",
                static async (registry, arguments, cancellationToken) => await RepositoryOperations.ExecuteAsync(
                    RepositoryOperationPlanner.TestRun(
                        registry._repositoryRoot,
                        RequiredString(arguments, "suite"),
                        OptionalString(arguments, "filter"),
                        OptionalString(arguments, "configuration") ?? "Release"),
                    cancellationToken)),
            Register(
                "package_validate",
                "Validate and hash the exact allowlisted package inventory selected by one universal release profile.",
                OperationRisk.Read,
                longRunning: false,
                timeoutSeconds: 30,
                "Returns the exact deterministic file inventory and candidate digest without copying files.",
                static (registry, arguments, _) =>
                {
                    var profile = registry.RequiredProfile(RequiredString(arguments, "packageId"));
                    ReleaseEnvironmentValidator.Validate(profile);
                    var candidate = ReleaseCandidateBuilder.Inspect(profile.PackageSource, profile.PackageInclude);
                    return Task.FromResult<object>(ReleasePackageValidator.Validate(profile, candidate));
                }),
            Register(
                "e2e_run_start",
                "Run one exact grouped or test-selected isolated native player workflow through the retained grouped engine.",
                OperationRisk.DestructiveLocal,
                longRunning: true,
                timeoutSeconds: 3900,
                "Retains exact-process product hashes, action/screenshots/results, normal-config hashes, and cleanup evidence.",
                static async (registry, arguments, cancellationToken) => await RepositoryOperations.ExecuteAsync(
                    RepositoryOperationPlanner.EndToEnd(
                        registry._repositoryRoot,
                        OptionalString(arguments, "groupId"),
                        OptionalString(arguments, "testId"),
                        OptionalString(arguments, "language") ?? "English",
                        OptionalInt32(arguments, "timeoutSeconds", 300),
                        OptionalBoolean(arguments, "dryRun", false)),
                    cancellationToken)),
            Register(
                "game_run_start",
                "Start one leased isolated minimized Gateway-backed RimWorld process and return its durable run identity.",
                OperationRisk.DestructiveLocal,
                longRunning: false,
                timeoutSeconds: 30,
                "Retains launcher/game PID identities, isolated artifacts, logs, completion signal, and cleanup output.",
                static (registry, arguments, _) => Task.FromResult<object>(new RunLeaseManager(registry._repositoryRoot).StartGateway(
                    OptionalStringArray(arguments, "packageIds"),
                    OptionalStringArray(arguments, "projectPaths"),
                    OptionalInt32(arguments, "holdSeconds", 1800)))),
            Register(
                "run_status",
                "Inspect one exact leased run without touching any process.",
                OperationRisk.Read,
                longRunning: false,
                timeoutSeconds: 15,
                "Returns exact launcher/game PID identities, live manifest path, and bounded launcher tails.",
                static (registry, arguments, _) => Task.FromResult<object>(new RunLeaseManager(registry._repositoryRoot)
                    .Status(RequiredString(arguments, "runId")))),
            Register(
                "run_cancel",
                "Request graceful completion for one exact leased run, with exact-PID bounded fallback.",
                OperationRisk.DestructiveLocal,
                longRunning: false,
                timeoutSeconds: 60,
                "Writes only the owned completion signal and preserves the launcher's cleanup evidence.",
                static async (registry, arguments, cancellationToken) => await new RunLeaseManager(registry._repositoryRoot)
                    .CancelAsync(RequiredString(arguments, "runId"), cancellationToken)),
            Register(
                "gateway_health",
                "Read authenticated status from the exact live Gateway session selected by one leased run.",
                OperationRisk.Read,
                longRunning: false,
                timeoutSeconds: 180,
                "Uses the credentialed current.json in memory only; returned output never contains its bearer token.",
                static async (registry, arguments, cancellationToken) => await registry.InvokeGatewayDiagnostic(
                    arguments, "status", cancellationToken)),
            Register(
                "gateway_diagnostic",
                "Run one allowlisted read-only Gateway client diagnostic against an exact leased live process.",
                OperationRisk.Read,
                longRunning: false,
                timeoutSeconds: 180,
                "Uses the credentialed current.json in memory only; returned output never contains its bearer token.",
                static async (registry, arguments, cancellationToken) => await registry.InvokeGatewayDiagnostic(
                    arguments, RequiredString(arguments, "command"), cancellationToken)),
            Register(
                "gateway_mutation",
                "Invoke one explicit semantic action or registered automation against an exact leased live process.",
                OperationRisk.DestructiveLocal,
                longRunning: false,
                timeoutSeconds: 180,
                "Mutation remains attributable to the exact run/request; direct mutation is diagnostic/setup evidence, not gameplay acceptance.",
                static async (registry, arguments, cancellationToken) => await registry.InvokeGatewayMutation(arguments, cancellationToken)),
            Register(
                "gateway_raw_mutation",
                "Compile and execute one bounded raw C# mutation in an exact leased live Gateway process for one-off development or diagnosis.",
                OperationRisk.DestructiveLocal,
                longRunning: false,
                timeoutSeconds: 180,
                "Retains only source SHA-256 and bounded response; raw mutation never counts as gameplay acceptance.",
                static async (registry, arguments, cancellationToken) => await new GatewayRawMutation(registry._repositoryRoot)
                    .ExecuteAsync(
                        RequiredString(arguments, "runId"),
                        RequiredString(arguments, "sourceCode"),
                        RequiredString(arguments, "entryType"),
                        OptionalString(arguments, "requestJson") ?? "{}",
                        cancellationToken)),
            Register(
                "evidence_read",
                "Read and hash one bounded UTF-8 evidence file beneath repository artifacts.",
                OperationRisk.Read,
                longRunning: false,
                timeoutSeconds: 10,
                "Restricted to artifacts with a caller-selected bound of at most 1 MiB.",
                static (registry, arguments, _) => Task.FromResult<object>(EvidenceReader.Read(
                    registry._repositoryRoot,
                    RequiredString(arguments, "path"),
                    OptionalInt32(arguments, "maximumBytes", 65536)))),
            Register(
                "release_profile_validate",
                "Validate one universal per-mod release profile and its canonical project identity without side effects.",
                OperationRisk.Read,
                longRunning: false,
                timeoutSeconds: 30,
                "No durable evidence; validated metadata and resolved repository paths only.",
                static (registry, arguments, _) => Task.FromResult<object>(registry.ValidateReleaseProfile(arguments))),
            Register(
                "modlist_inspect",
                "Inspect the user's normal RimWorld active mod list without changing it.",
                OperationRisk.Read,
                longRunning: false,
                timeoutSeconds: 10,
                "Returns the exact config path, hash, and ordered package IDs.",
                static (registry, arguments, _) => Task.FromResult<object>(registry.InspectModList(arguments))),
            Register(
                "modlist_enable",
                "Transactionally enable one canonical package ID after one exact anchor in the user's normal mod list.",
                OperationRisk.DestructiveLocal,
                longRunning: false,
                timeoutSeconds: 30,
                "Retains an exact timestamped backup plus before/after hashes under artifacts/UserConfigBackups.",
                static (registry, arguments, _) => Task.FromResult<object>(registry.EnableMod(arguments))),
            Register(
                "modlist_disable",
                "Transactionally disable one canonical package ID in the user's normal mod list.",
                OperationRisk.DestructiveLocal,
                longRunning: false,
                timeoutSeconds: 30,
                "Retains an exact timestamped backup plus before/after hashes under artifacts/UserConfigBackups.",
                static (registry, arguments, _) => Task.FromResult<object>(registry.DisableMod(arguments))),
            Register(
                "modlist_restore",
                "Restore one exact ModsConfig backup only when the current file still has the caller-approved hash.",
                OperationRisk.DestructiveLocal,
                longRunning: false,
                timeoutSeconds: 30,
                "Validates both current hash and backup XML before atomic replacement.",
                static (registry, arguments, _) => Task.FromResult<object>(registry.RestoreModList(arguments))),
            Register(
                "local_mod_sync",
                "Atomically synchronize one allowlisted built product package into the local RimWorld Mods directory with a recoverable backup.",
                OperationRisk.DestructiveLocal,
                longRunning: false,
                timeoutSeconds: 60,
                "Retains the prior local package outside RimWorld's scanned Mods directory under the sibling .rimworld-modding-mcp recovery root.",
                static (registry, arguments, _) => Task.FromResult<object>(registry.SyncLocalMod(arguments))),
            Register(
                "release_prepare",
                "Build, validate, stage, hash, and remotely preflight one universal release profile without mutating Steam.",
                OperationRisk.WorkspaceWrite,
                longRunning: false,
                timeoutSeconds: 900,
                "Writes an immutable candidate and canonical publication plan under artifacts/Releases.",
                static async (registry, arguments, cancellationToken) =>
                    await new ReleasePreparer(registry._repositoryRoot).PrepareAsync(
                        RequiredString(arguments, "packageId"),
                        cancellationToken)),
            Register(
                "release_status",
                "Revalidate one exact release plan/digest/nonce and inspect its durable publication state without mutating Steam.",
                OperationRisk.Read,
                longRunning: false,
                timeoutSeconds: 30,
                "Reads exact retained plan/candidate hashes and durable state; never reads or returns a bearer token.",
                static (registry, arguments, _) => Task.FromResult<object>(ReleasePlanAdmission.Status(
                    registry._repositoryRoot,
                    RequiredString(arguments, "planPath"),
                    RequiredString(arguments, "planSha256"),
                    RequiredString(arguments, "confirmationNonce")))),
            Register(
                "release_publish",
                "Publish one exact admitted plan through Steamworks, verify remote truth, subscribe/reacquire, run the native subscriber workflow, persist identity, and restore the local package.",
                OperationRisk.ExternalWrite,
                longRunning: true,
                timeoutSeconds: 3600,
                "Retains durable Steam callback state, remote/subscriber receipts, exact process evidence, and a committed Workshop identity.",
                static async (registry, arguments, cancellationToken) => await new ReleaseWorkerCoordinator(registry._repositoryRoot)
                    .PublishAsync(
                        RequiredString(arguments, "planPath"),
                        RequiredString(arguments, "planSha256"),
                        RequiredString(arguments, "confirmationNonce"),
                        cancellationToken)),
            Register(
                "release_accept_subscriber_evidence",
                "Record a caller's concrete personal inspection of the exact retained subscriber screenshots and complete the local release receipt.",
                OperationRisk.WorkspaceWrite,
                longRunning: false,
                timeoutSeconds: 30,
                "Hashes the exact receipt screenshots and retains the caller's observation; it cannot manufacture or infer visual acceptance.",
                static (registry, arguments, _) => Task.FromResult<object>(ReleaseReview.Accept(
                    registry._repositoryRoot,
                    RequiredString(arguments, "planPath"),
                    RequiredString(arguments, "planSha256"),
                    RequiredString(arguments, "confirmationNonce"),
                    RequiredString(arguments, "receiptPath"),
                    RequiredString(arguments, "observation"))))
        };
        return new OperationRegistry(repositoryRoot, registrations);
    }

    public async Task<object> InvokeAsync(string name, JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!_operations.TryGetValue(name, out var registration))
        {
            throw new ArgumentException($"Unknown operation '{name}'. Use operation_list to discover supported operations.");
        }

        if (arguments.ValueKind is not JsonValueKind.Object)
        {
            throw new ArgumentException("Operation arguments must be one JSON object.");
        }

        return await registration.Handler(this, arguments, cancellationToken);
    }

    private async Task<object> ReadRepositoryStatusAsync(CancellationToken cancellationToken)
    {
        var head = await ProcessRunner.RunAsync(
            "git", ["rev-parse", "HEAD"], _repositoryRoot, TimeSpan.FromSeconds(10), cancellationToken);
        if (head.ExitCode != 0)
        {
            throw new InvalidOperationException($"Could not read git revision: {Bounded(head.StandardError)}");
        }

        var status = await ProcessRunner.RunAsync(
            "git", ["status", "--porcelain"], _repositoryRoot, TimeSpan.FromSeconds(10), cancellationToken);
        if (status.ExitCode != 0)
        {
            throw new InvalidOperationException($"Could not read git status: {Bounded(status.StandardError)}");
        }

        return new RepositoryStatusResult(
            _repositoryRoot,
            head.StandardOutput.Trim(),
            string.IsNullOrWhiteSpace(status.StandardOutput),
            RepositoryDiscovery.DiscoverMods(_repositoryRoot),
            Descriptors);
    }

    private ReleaseProfileResult ValidateReleaseProfile(JsonElement arguments)
    {
        var packageId = RequiredString(arguments, "packageId").ToLowerInvariant();
        var profile = ReleaseProfileCatalog.Discover(_repositoryRoot)
            .SingleOrDefault(item => string.Equals(item.PackageId, packageId, StringComparison.OrdinalIgnoreCase)) ??
            throw new ArgumentException($"No universal release profile exists for packageId '{packageId}'.");
        return new ReleaseProfileResult(
            Path.GetRelativePath(_repositoryRoot, profile.Path).Replace('\\', '/'),
            profile.PackageId,
            profile.Title,
            profile.DistributionKind,
            profile.Visibility,
            profile.PublishedFileId,
            profile.AllowFirstPublication,
            profile.RequiredWorkshopItems,
            profile.RequiredDlcAppIds,
            Path.GetRelativePath(_repositoryRoot, profile.Project).Replace('\\', '/'),
            Path.GetRelativePath(_repositoryRoot, profile.PackageSource).Replace('\\', '/'),
            Path.GetRelativePath(_repositoryRoot, profile.Description).Replace('\\', '/'),
            Path.GetRelativePath(_repositoryRoot, profile.Preview).Replace('\\', '/'));
    }

    private ModListState InspectModList(JsonElement arguments)
    {
        var path = OptionalString(arguments, "configPath") ?? ModListEditor.DefaultConfigPath();
        return ModListEditor.Read(path);
    }

    private ModListMutationResult EnableMod(JsonElement arguments)
    {
        RefuseRunningRimWorld();
        var path = OptionalString(arguments, "configPath") ?? ModListEditor.DefaultConfigPath();
        return ModListEditor.Enable(
            path,
            RequiredString(arguments, "packageId"),
            RequiredString(arguments, "anchorPackageId"),
            Path.Combine(_repositoryRoot, "artifacts", "UserConfigBackups", "RimWorld"));
    }

    private ModListMutationResult DisableMod(JsonElement arguments)
    {
        RefuseRunningRimWorld();
        var path = OptionalString(arguments, "configPath") ?? ModListEditor.DefaultConfigPath();
        return ModListEditor.Disable(
            path,
            RequiredString(arguments, "packageId"),
            Path.Combine(_repositoryRoot, "artifacts", "UserConfigBackups", "RimWorld"));
    }

    private ModListState RestoreModList(JsonElement arguments)
    {
        RefuseRunningRimWorld();
        var path = OptionalString(arguments, "configPath") ?? ModListEditor.DefaultConfigPath();
        var backup = RepositoryRoot.ContainedPath(_repositoryRoot, RequiredString(arguments, "backupPath"));
        var allowedRoot = Path.Combine(_repositoryRoot, "artifacts", "UserConfigBackups", "RimWorld") + Path.DirectorySeparatorChar;
        if (!backup.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("backupPath must be beneath artifacts/UserConfigBackups/RimWorld.");
        return ModListEditor.Restore(path, backup, RequiredString(arguments, "expectedCurrentSha256"));
    }

    private LocalModInstallResult SyncLocalMod(JsonElement arguments)
    {
        RefuseRunningRimWorld();
        var packageId = RequiredString(arguments, "packageId").ToLowerInvariant();
        var profile = ReleaseProfileCatalog.Discover(_repositoryRoot)
            .SingleOrDefault(item => string.Equals(item.PackageId, packageId, StringComparison.OrdinalIgnoreCase)) ??
            throw new ArgumentException($"No universal release profile exists for packageId '{packageId}'.");
        var modsRoot = OptionalString(arguments, "modsRoot") ??
                       Path.Combine(@"F:\Steam\steamapps\common\RimWorld", "Mods");
        return LocalModInstaller.Sync(profile.PackageSource, modsRoot, profile.PackageId, profile.PackageInclude);
    }

    private ReleaseProfile RequiredProfile(string packageId) => ReleaseProfileCatalog.Discover(_repositoryRoot)
        .SingleOrDefault(item => string.Equals(item.PackageId, packageId, StringComparison.OrdinalIgnoreCase)) ??
        throw new ArgumentException($"No universal release profile exists for packageId '{packageId}'.");

    private async Task<object> InvokeGatewayDiagnostic(
        JsonElement arguments,
        string command,
        CancellationToken cancellationToken)
    {
        var status = new RunLeaseManager(_repositoryRoot).Status(RequiredString(arguments, "runId"));
        if (status.State != "ready" || status.GameProcessId is null || status.GatewayManifestPath is null)
            throw new ArgumentException("The selected run has no exact ready Gateway process and manifest.");
        return await RepositoryOperations.ExecuteAsync(
            GatewayClientPlanner.Diagnostic(
                _repositoryRoot,
                status.GatewayManifestPath,
                status.GameProcessId.Value,
                command,
                null),
            cancellationToken);
    }

    private async Task<object> InvokeGatewayMutation(JsonElement arguments, CancellationToken cancellationToken)
    {
        var status = new RunLeaseManager(_repositoryRoot).Status(RequiredString(arguments, "runId"));
        if (status.State != "ready" || status.GameProcessId is null || status.GatewayManifestPath is null)
            throw new ArgumentException("The selected run has no exact ready Gateway process and manifest.");
        return await RepositoryOperations.ExecuteAsync(
            GatewayClientPlanner.Mutation(
                _repositoryRoot,
                status.GatewayManifestPath,
                status.GameProcessId.Value,
                RequiredString(arguments, "kind"),
                RequiredString(arguments, "name"),
                OptionalString(arguments, "argumentsJson") ?? "{}"),
            cancellationToken);
    }

    private static void RefuseRunningRimWorld()
    {
        var processes = System.Diagnostics.Process.GetProcesses()
            .Where(process => process.ProcessName.Equals("RimWorldWin64", StringComparison.OrdinalIgnoreCase) ||
                              process.ProcessName.Equals("RimWorld", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        try
        {
            if (processes.Length > 0)
                throw new ArgumentException("Refusing to change the normal mod list while RimWorld is running.");
        }
        finally
        {
            foreach (var process in processes) process.Dispose();
        }
    }

    private static string RequiredString(JsonElement arguments, string name)
    {
        if (!arguments.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new ArgumentException($"Operation requires nonblank string argument '{name}'.");
        }

        return property.GetString()!.Trim();
    }

    private static string? OptionalString(JsonElement arguments, string name)
    {
        if (!arguments.TryGetProperty(name, out var property) || property.ValueKind == JsonValueKind.Null) return null;
        if (property.ValueKind != JsonValueKind.String)
            throw new ArgumentException($"Operation argument '{name}' must be a string or null.");
        return string.IsNullOrWhiteSpace(property.GetString()) ? null : property.GetString()!.Trim();
    }

    private static int OptionalInt32(JsonElement arguments, string name, int fallback)
    {
        if (!arguments.TryGetProperty(name, out var property) || property.ValueKind == JsonValueKind.Null) return fallback;
        if (!property.TryGetInt32(out var value)) throw new ArgumentException($"Operation argument '{name}' must be an integer.");
        return value;
    }

    private static bool OptionalBoolean(JsonElement arguments, string name, bool fallback)
    {
        if (!arguments.TryGetProperty(name, out var property) || property.ValueKind == JsonValueKind.Null) return fallback;
        if (property.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new ArgumentException($"Operation argument '{name}' must be boolean.");
        return property.GetBoolean();
    }

    private static string[] OptionalStringArray(JsonElement arguments, string name)
    {
        if (!arguments.TryGetProperty(name, out var property) || property.ValueKind == JsonValueKind.Null)
            return Array.Empty<string>();
        if (property.ValueKind != JsonValueKind.Array)
            throw new ArgumentException($"Operation argument '{name}' must be a string array.");
        return property.EnumerateArray().Select(value =>
        {
            if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
                throw new ArgumentException($"Operation argument '{name}' contains a blank or non-string value.");
            return value.GetString()!.Trim();
        }).ToArray();
    }

    private static OperationRegistration Register(
        string name,
        string description,
        OperationRisk risk,
        bool longRunning,
        int timeoutSeconds,
        string evidencePolicy,
        Func<OperationRegistry, JsonElement, CancellationToken, Task<object>> handler) =>
        new(new OperationDescriptor(name, description, risk, longRunning, timeoutSeconds, evidencePolicy), handler);

    private static string Bounded(string value) => value.Length <= 4096 ? value.Trim() : value[..4096].Trim();

    private sealed record OperationRegistration(
        OperationDescriptor Descriptor,
        Func<OperationRegistry, JsonElement, CancellationToken, Task<object>> Handler);
}
