using System;
using System.Collections.Generic;
using System.Linq;
using RimWorldDevGateway.IntegrationTesting;

namespace RimWorldDevGateway;

internal interface IGatewayIntegrationTestSessionArtifactStore : IGatewayIntegrationTestArtifactStore
{
    string? ArtifactPath { get; }

    IGatewayIntegrationTestPersistenceOperation BeginAttachSession(
        string runId,
        GatewayIntegrationTestSnapshot initialSnapshot);
}

public sealed class GatewayIntegrationTestCoordinator : IDisposable
{
    private readonly GatewayIntegrationTestRunner? runner;
    private readonly IGatewayIntegrationTestSessionArtifactStore? artifactStore;
    private readonly IDisposable? ownedResource;
    private readonly Action<string, Exception?>? diagnostics;
    private readonly HashSet<string> reportedFaults = new(StringComparer.Ordinal);
    private readonly GatewayIntegrationTestSnapshot disabledSnapshot;
    private bool disposed;
    private string? sessionCredential;
    private string? pendingRunId;
    private GatewayIntegrationTestSnapshot? pendingAttachmentCandidate;
    private IGatewayIntegrationTestPersistenceOperation? pendingAttachment;

    private GatewayIntegrationTestCoordinator()
    {
        disabledSnapshot = new GatewayIntegrationTestSnapshot(
            enabled: false,
            discoveryState: "disabled",
            lifecyclePoints: Enum.GetValues(typeof(RunAt))
                .Cast<RunAt>()
                .Select(point => new GatewayIntegrationTestLifecycleSnapshot(
                    point,
                    "disabled",
                    null,
                    null,
                    0,
                    0,
                    0)));
    }

    private GatewayIntegrationTestCoordinator(
        GatewayIntegrationTestRunner runner,
        IGatewayIntegrationTestSessionArtifactStore artifactStore,
        IDisposable? ownedResource,
        Action<string, Exception?>? diagnostics)
        : this()
    {
        this.runner = runner ?? throw new ArgumentNullException(nameof(runner));
        this.artifactStore = artifactStore ?? throw new ArgumentNullException(nameof(artifactStore));
        this.ownedResource = ownedResource;
        this.diagnostics = diagnostics;
    }

    public GatewayIntegrationTestSnapshot Snapshot => runner?.Snapshot ?? disabledSnapshot;

    public GatewayIntegrationTestSnapshot? PublishedSnapshot =>
        runner is null ? disabledSnapshot : runner.PublishedSnapshot;

    public string? ArtifactPath => artifactStore?.ArtifactPath;

    public static GatewayIntegrationTestCoordinator Create(
        bool enabled,
        Func<GatewayIntegrationTestCoordinator> enabledFactory)
    {
        if (enabledFactory is null)
        {
            throw new ArgumentNullException(nameof(enabledFactory));
        }

        if (!enabled)
        {
            return new GatewayIntegrationTestCoordinator();
        }

        return enabledFactory() ??
            throw new InvalidOperationException("The enabled integration-test coordinator factory returned null.");
    }

    public static GatewayIntegrationTestCoordinator CreateEnabled(
        string saveDataFolder,
        IGatewayIntegrationTestAssemblyCatalog catalog,
        IGatewayIntegrationTestReadiness readiness,
        Action<string, Exception?>? diagnostics = null)
    {
        if (catalog is null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        if (readiness is null)
        {
            throw new ArgumentNullException(nameof(readiness));
        }

        try
        {
            return CreateEnabledWithStore(
                catalog,
                readiness,
                new GatewayIntegrationTestSessionArtifactStore(saveDataFolder),
                diagnostics);
        }
        catch
        {
            (catalog as IDisposable)?.Dispose();
            throw;
        }
    }

    internal static GatewayIntegrationTestCoordinator CreateEnabledWithStore(
        IGatewayIntegrationTestAssemblyCatalog catalog,
        IGatewayIntegrationTestReadiness readiness,
        IGatewayIntegrationTestSessionArtifactStore store,
        Action<string, Exception?>? diagnostics = null)
    {
        if (catalog is null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        if (readiness is null)
        {
            throw new ArgumentNullException(nameof(readiness));
        }

        if (store is null)
        {
            throw new ArgumentNullException(nameof(store));
        }

        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            catalog,
            store,
            readiness,
            diagnostics: diagnostics);
        return new GatewayIntegrationTestCoordinator(
            runner,
            store,
            catalog as IDisposable,
            diagnostics);
    }

    public void AttachSession(string runId, string? bearerToken = null)
    {
        ThrowIfDisposed();
        if (runner is null || artifactStore is null)
        {
            return;
        }

        sessionCredential = string.IsNullOrEmpty(bearerToken) ? null : bearerToken;
        runner.AttachSessionCredential(sessionCredential);
        pendingRunId = runId;
        pendingAttachmentCandidate = runner.Snapshot;
        TryStartAttachment();
    }

    public void Tick()
    {
        ThrowIfDisposed();
        if (runner is null)
        {
            return;
        }

        if (AdvanceAttachment())
        {
            return;
        }

        if (!ObserveBestEffort(RunAt.MainMenuLoaded))
        {
            ObserveBestEffort(RunAt.PlayableMapLoaded);
        }
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
            ownedResource?.Dispose();
        }
        catch (Exception exception)
        {
            ReportOnce("dispose", exception);
        }
    }

    private bool ObserveBestEffort(RunAt lifecyclePoint)
    {
        try
        {
            return runner!.TryObserve(lifecyclePoint);
        }
        catch (Exception exception)
        {
            ReportOnce("tick-" + lifecyclePoint, exception);
            return true;
        }
    }

    private bool AdvanceAttachment()
    {
        if (pendingRunId is null)
        {
            return false;
        }

        if (pendingAttachment is null)
        {
            TryStartAttachment();
            return true;
        }

        if (!pendingAttachment.IsCompleted)
        {
            return true;
        }

        GatewayIntegrationTestPersistenceOutcome outcome;
        try
        {
            outcome = pendingAttachment.GetOutcome();
        }
        catch (Exception exception)
        {
            ReportOnce("attach-session", exception);
            pendingAttachment = null;
            return true;
        }

        pendingAttachment = null;
        if (!outcome.Succeeded ||
            !ReferenceEquals(outcome.Snapshot, pendingAttachmentCandidate))
        {
            ReportOnce(
                "attach-session",
                outcome.Failure ?? new InvalidOperationException(
                    "Session artifact attachment did not commit the exact requested snapshot."));
            return true;
        }

        runner!.ConfirmInitialPublishedSnapshot(outcome.Snapshot);
        pendingRunId = null;
        pendingAttachmentCandidate = null;
        return true;
    }

    private void TryStartAttachment()
    {
        try
        {
            pendingAttachment = artifactStore!.BeginAttachSession(
                                    pendingRunId!,
                                    pendingAttachmentCandidate!) ??
                throw new InvalidOperationException(
                    "The integration-test artifact store returned a null attachment operation.");
        }
        catch (Exception exception)
        {
            pendingAttachment = null;
            ReportOnce("attach-session", exception);
        }
    }

    private void ReportOnce(string operation, Exception exception)
    {
        var details = GatewayIntegrationTestExceptionFormatter.Format(exception, sessionCredential);
        var key = operation + ":" + details.Type + ":" + details.Message;
        if (reportedFaults.Count < 32 && reportedFaults.Add(key))
        {
            try
            {
                diagnostics?.Invoke(
                    operation,
                    new InvalidOperationException(details.Type + ": " + details.Message));
            }
            catch
            {
                // Diagnostics are advisory and must never destabilize the game loop.
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(GatewayIntegrationTestCoordinator));
        }
    }
}
