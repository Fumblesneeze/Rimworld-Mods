namespace RimWorldDevGateway;

public sealed class GatewayEndToEndCoordinator : IDisposable
{
    private readonly IGatewayEndToEndManifestSource? source;
    private readonly IGatewayEndToEndInspectionOperationFactory? inspectionFactory;
    private readonly IGatewayEndToEndSessionArtifactStore? artifactStore;
    private readonly Action<string, Exception?>? diagnostics;
    private readonly List<string> activePackageIds = new();
    private readonly List<GatewayEndToEndBundleSnapshot> bundles = new();
    private readonly List<GatewayEndToEndTestSnapshot> tests = new();
    private readonly List<GatewayEndToEndFailureSnapshot> failures = new();
    private readonly GatewayEndToEndSnapshot disabledSnapshot = new(false, "disabled");
    private GatewayEndToEndSnapshot snapshot;
    private volatile GatewayEndToEndSnapshot? publishedSnapshot;
    private IGatewayEndToEndManifestDiscoveryCursor? cursor;
    private IGatewayEndToEndInspectionOperation? inspection;
    private GatewayEndToEndManifestCandidate? inspectionCandidate;
    private IGatewayEndToEndPersistenceOperation? persistence;
    private GatewayEndToEndSnapshot? persistenceCandidate;
    private string? pendingRunId;
    private bool persistenceIsAttachment;
    private bool discoveryComplete;
    private bool activePackageListTrustworthy = true;
    private bool activePackageFailureReported;
    private bool disposed;
    private string? sessionCredential;
    private readonly HashSet<string> reportedOperations = new(StringComparer.Ordinal);

    private GatewayEndToEndCoordinator()
    {
        snapshot = disabledSnapshot;
        publishedSnapshot = disabledSnapshot;
    }

    private GatewayEndToEndCoordinator(
        IGatewayEndToEndManifestSource source,
        IGatewayEndToEndBundleInspector inspector,
        IGatewayEndToEndInspectionOperationFactory inspectionFactory,
        IGatewayEndToEndSessionArtifactStore artifactStore,
        Action<string, Exception?>? diagnostics)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        _ = inspector ?? throw new ArgumentNullException(nameof(inspector));
        this.inspectionFactory = inspectionFactory ?? throw new ArgumentNullException(nameof(inspectionFactory));
        this.artifactStore = artifactStore ?? throw new ArgumentNullException(nameof(artifactStore));
        this.diagnostics = diagnostics;
        snapshot = new GatewayEndToEndSnapshot(true, "awaiting_session");
    }

    public GatewayEndToEndSnapshot Snapshot => snapshot;

    public GatewayEndToEndSnapshot? PublishedSnapshot => publishedSnapshot;

    public string? ArtifactPath => artifactStore?.ArtifactPath;

    public static GatewayEndToEndCoordinator Create(
        bool enabled,
        Func<GatewayEndToEndCoordinator> enabledFactory)
    {
        if (enabledFactory is null)
        {
            throw new ArgumentNullException(nameof(enabledFactory));
        }

        if (!enabled)
        {
            return new GatewayEndToEndCoordinator();
        }

        return enabledFactory() ??
            throw new InvalidOperationException("The enabled E2E coordinator factory returned null.");
    }

    public static GatewayEndToEndCoordinator CreateEnabled(
        string saveDataFolder,
        IGatewayEndToEndManifestSource source,
        Action<string, Exception?>? diagnostics = null)
    {
        var inspector = new GatewayEndToEndBundleCatalog();
        return CreateEnabledWithStore(
            source,
            inspector,
            new GatewayEndToEndTaskInspectionOperationFactory(inspector),
            new GatewayEndToEndSessionArtifactStore(saveDataFolder),
            diagnostics);
    }

    internal static GatewayEndToEndCoordinator CreateEnabledWithStore(
        IGatewayEndToEndManifestSource source,
        IGatewayEndToEndBundleInspector inspector,
        IGatewayEndToEndInspectionOperationFactory inspectionFactory,
        IGatewayEndToEndSessionArtifactStore artifactStore,
        Action<string, Exception?>? diagnostics = null) =>
        new(source, inspector, inspectionFactory, artifactStore, diagnostics);

    public void AttachSession(string runId, string? bearerToken = null)
    {
        ThrowIfDisposed();
        if (source is null || artifactStore is null)
        {
            return;
        }

        if (pendingRunId is not null || artifactStore.IsAttached)
        {
            throw new InvalidOperationException("The E2E coordinator is already attached to a session.");
        }

        pendingRunId = runId;
        sessionCredential = string.IsNullOrEmpty(bearerToken) ? null : bearerToken;
        snapshot = BuildSnapshot("discovering");
        StartPersistence(snapshot, attach: true);
    }

    public void Tick()
    {
        ThrowIfDisposed();
        if (source is null || artifactStore is null)
        {
            return;
        }

        if (persistence is not null)
        {
            AdvancePersistence();
            return;
        }

        if (persistenceCandidate is not null)
        {
            StartPersistence(persistenceCandidate, persistenceIsAttachment);
            return;
        }

        if (discoveryComplete)
        {
            return;
        }

        if (!artifactStore.IsAttached)
        {
            return;
        }

        if (inspection is not null)
        {
            AdvanceInspection();
            return;
        }

        if (cursor is null)
        {
            try
            {
                cursor = source.BeginDiscovery() ??
                    throw new InvalidOperationException("The E2E manifest source returned a null cursor.");
            }
            catch (Exception exception)
            {
                RecordInfrastructureFailure(
                    "discovery_start_failed",
                    "E2E discovery could not start; arbitrary exception text was suppressed.",
                    exception);
                CompleteDiscovery();
            }

            return;
        }

        GatewayEndToEndManifestDiscoveryStep step;
        try
        {
            step = cursor.Advance() ??
                throw new InvalidOperationException("The E2E discovery cursor returned a null step.");
        }
        catch (Exception exception)
        {
            RecordInfrastructureFailure(
                "discovery_advance_failed",
                "E2E discovery failed while advancing; arbitrary exception text was suppressed.",
                exception);
            CompleteDiscovery();
            return;
        }

        ApplyDiscoveryStep(step);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        try
        {
            cursor?.Dispose();
        }
        catch (Exception exception)
        {
            Report("dispose-cursor", exception);
        }

        try
        {
            (source as IDisposable)?.Dispose();
        }
        catch (Exception exception)
        {
            Report("dispose-source", exception);
        }
    }

    private void ApplyDiscoveryStep(GatewayEndToEndManifestDiscoveryStep step)
    {
        switch (step.Kind)
        {
            case GatewayEndToEndManifestDiscoveryStepKind.Progress:
                return;
            case GatewayEndToEndManifestDiscoveryStepKind.ActivePackage:
                var packageId = step.ActivePackageId!;
                if (activePackageIds.Contains(packageId, StringComparer.OrdinalIgnoreCase))
                {
                    activePackageListTrustworthy = false;
                    failures.Add(new GatewayEndToEndFailureSnapshot(
                        "duplicate_active_package",
                        "The active E2E package list contains a duplicate package ID.",
                        packageId));
                }
                else
                {
                    activePackageIds.Add(packageId);
                }

                PersistCurrent("discovering");
                return;
            case GatewayEndToEndManifestDiscoveryStepKind.Candidate:
                inspectionCandidate = step.ManifestCandidate!;
                if (!activePackageListTrustworthy)
                {
                    if (!activePackageFailureReported)
                    {
                        failures.Add(new GatewayEndToEndFailureSnapshot(
                            "active_package_list_untrusted",
                            "No E2E bundle was loaded because the complete active package identity could not be trusted."));
                        activePackageFailureReported = true;
                    }

                    bundles.Add(new GatewayEndToEndBundleSnapshot(
                        inspectionCandidate.ContainingPackageId,
                        inspectionCandidate.ManifestPath,
                        "blocked",
                        null,
                        null,
                        0,
                        "active_package_list_untrusted"));
                    inspectionCandidate = null;
                    PersistCurrent("discovering");
                    return;
                }

                try
                {
                    inspection = inspectionFactory!.Begin(
                        inspectionCandidate,
                        activePackageIds.ToArray()) ??
                        throw new InvalidOperationException("The E2E inspection factory returned a null operation.");
                }
                catch (Exception exception)
                {
                    ApplyInspectionFailure(
                        inspectionCandidate,
                        "inspection_start_failed",
                        "The E2E bundle inspection could not start; arbitrary exception text was suppressed.",
                        exception);
                }

                return;
            case GatewayEndToEndManifestDiscoveryStepKind.Failure:
                failures.Add(step.FailureSnapshot!);
                if (StringComparer.Ordinal.Equals(
                        step.FailureSnapshot!.Code,
                        "active_mod_read_failed"))
                {
                    activePackageListTrustworthy = false;
                }
                PersistCurrent("discovering");
                return;
            case GatewayEndToEndManifestDiscoveryStepKind.Complete:
                CompleteDiscovery();
                return;
            default:
                failures.Add(new GatewayEndToEndFailureSnapshot(
                    "discovery_step_invalid",
                    "The E2E discovery cursor returned an unsupported step kind."));
                CompleteDiscovery();
                return;
        }
    }

    private void AdvanceInspection()
    {
        if (!inspection!.IsCompleted)
        {
            return;
        }

        GatewayEndToEndBundleInspectionResult result;
        try
        {
            result = inspection.GetOutcome() ??
                throw new InvalidOperationException("The E2E inspection operation returned a null outcome.");
        }
        catch (Exception exception)
        {
            ApplyInspectionFailure(
                inspectionCandidate!,
                "inspection_result_failed",
                "The E2E bundle inspection result could not be read; arbitrary exception text was suppressed.",
                exception);
            return;
        }

        var candidate = inspectionCandidate!;
        inspection = null;
        inspectionCandidate = null;
        if (result.State == "failed")
        {
            var failure = result.Failure ?? new GatewayEndToEndLoadFailure(
                "bundle_inspection_failed",
                "The staged E2E bundle failed guarded inspection.");
            failures.Add(new GatewayEndToEndFailureSnapshot(
                failure.Code,
                failure.Message,
                candidate.ContainingPackageId,
                candidate.ManifestPath));
            bundles.Add(new GatewayEndToEndBundleSnapshot(
                candidate.ContainingPackageId,
                candidate.ManifestPath,
                "failed",
                null,
                null,
                0,
                failure.Code));
        }
        else if (result.State == "skipped")
        {
            bundles.Add(new GatewayEndToEndBundleSnapshot(
                candidate.ContainingPackageId,
                candidate.ManifestPath,
                "skipped",
                null,
                null,
                0));
        }
        else if (result.State == "loaded" && result.Source is not null)
        {
            bundles.Add(new GatewayEndToEndBundleSnapshot(
                candidate.ContainingPackageId,
                candidate.ManifestPath,
                "loaded",
                result.Source.AssemblyIdentity,
                result.Source.AssemblySha256,
                result.Source.Tests.Count));
            foreach (var descriptor in result.Source.Tests)
            {
                if (tests.Any(existing => StringComparer.Ordinal.Equals(existing.Id, descriptor.Id)))
                {
                    failures.Add(new GatewayEndToEndFailureSnapshot(
                        "duplicate_test_id",
                        "Two admitted E2E bundles declare the same stable test ID.",
                        candidate.ContainingPackageId,
                        candidate.ManifestPath));
                    continue;
                }

                tests.Add(new GatewayEndToEndTestSnapshot(descriptor));
            }
        }
        else
        {
            failures.Add(new GatewayEndToEndFailureSnapshot(
                "inspection_state_invalid",
                "The E2E bundle inspection returned an unsupported state.",
                candidate.ContainingPackageId,
                candidate.ManifestPath));
        }

        PersistCurrent("discovering");
    }

    private void ApplyInspectionFailure(
        GatewayEndToEndManifestCandidate candidate,
        string code,
        string message,
        Exception exception)
    {
        inspection = null;
        inspectionCandidate = null;
        failures.Add(new GatewayEndToEndFailureSnapshot(
            code,
            message,
            candidate.ContainingPackageId,
            candidate.ManifestPath));
        bundles.Add(new GatewayEndToEndBundleSnapshot(
            candidate.ContainingPackageId,
            candidate.ManifestPath,
            "failed",
            null,
            null,
            0,
            code));
        Report(code, exception);
        PersistCurrent("discovering");
    }

    private void CompleteDiscovery()
    {
        try
        {
            cursor?.Dispose();
        }
        catch (Exception exception)
        {
            failures.Add(new GatewayEndToEndFailureSnapshot(
                "discovery_dispose_failed",
                "The E2E discovery cursor could not be disposed cleanly."));
            Report("dispose-cursor", exception);
        }

        cursor = null;
        if (tests.Count == 0 && !failures.Any(failure =>
                StringComparer.Ordinal.Equals(failure.Code, "no_tests_admitted")))
        {
            failures.Add(new GatewayEndToEndFailureSnapshot(
                "no_tests_admitted",
                "The E2E launch completed discovery without admitting a test for the exact active mod order."));
        }

        var state = failures.Count == 0 ? "completed" : "completed_with_failures";
        snapshot = BuildSnapshot(state);
        StartPersistence(snapshot, attach: false);
        discoveryComplete = true;
    }

    private void RecordInfrastructureFailure(string code, string message, Exception exception)
    {
        failures.Add(new GatewayEndToEndFailureSnapshot(code, message));
        Report(code, exception);
    }

    private void PersistCurrent(string state)
    {
        snapshot = BuildSnapshot(state);
        StartPersistence(snapshot, attach: false);
    }

    private GatewayEndToEndSnapshot BuildSnapshot(string state) => new(
        true,
        state,
        activePackageIds,
        bundles,
        tests,
        failures);

    private void StartPersistence(GatewayEndToEndSnapshot candidate, bool attach)
    {
        persistenceCandidate = candidate;
        persistenceIsAttachment = attach;
        try
        {
            persistence = attach
                ? artifactStore!.BeginAttachSession(pendingRunId!, candidate)
                : artifactStore!.BeginPersist(candidate);
            if (persistence is null)
            {
                throw new InvalidOperationException("The E2E artifact store returned a null persistence operation.");
            }
        }
        catch (Exception exception)
        {
            persistence = null;
            Report(attach ? "attach-session" : "persist", exception);
        }
    }

    private void AdvancePersistence()
    {
        if (!persistence!.IsCompleted)
        {
            return;
        }

        GatewayEndToEndPersistenceOutcome outcome;
        try
        {
            outcome = persistence.GetOutcome();
        }
        catch (Exception exception)
        {
            persistence = null;
            Report(persistenceIsAttachment ? "attach-session" : "persist", exception);
            return;
        }

        persistence = null;
        if (!outcome.Succeeded || !ReferenceEquals(outcome.Snapshot, persistenceCandidate))
        {
            Report(
                persistenceIsAttachment ? "attach-session" : "persist",
                outcome.Failure ?? new InvalidOperationException(
                    "E2E persistence did not commit the exact requested snapshot."));
            return;
        }

        publishedSnapshot = outcome.Snapshot;
        if (persistenceIsAttachment)
        {
            pendingRunId = null;
        }

        persistenceCandidate = null;
    }

    private void Report(string operation, Exception? exception)
    {
        if (!reportedOperations.Add(operation))
        {
            return;
        }

        try
        {
            Exception? safeException = null;
            if (exception is not null)
            {
                var details = GatewayIntegrationTestExceptionFormatter.Format(exception, sessionCredential);
                safeException = new InvalidOperationException(details.Type + ": " + details.Message);
            }

            diagnostics?.Invoke(operation, safeException);
        }
        catch
        {
            // Diagnostics are advisory and must not destabilize Unity's update loop.
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(GatewayEndToEndCoordinator));
        }
    }
}
