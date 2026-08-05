namespace RimWorldDevGateway;

public interface IGatewayEndToEndManifestSource
{
    IGatewayEndToEndManifestDiscoveryCursor BeginDiscovery();
}

public interface IGatewayEndToEndManifestDiscoveryCursor : IDisposable
{
    GatewayEndToEndManifestDiscoveryStep Advance();
}

public enum GatewayEndToEndManifestDiscoveryStepKind
{
    Progress,
    ActivePackage,
    Candidate,
    Failure,
    Complete
}

public sealed class GatewayEndToEndManifestDiscoveryStep
{
    private GatewayEndToEndManifestDiscoveryStep(
        GatewayEndToEndManifestDiscoveryStepKind kind,
        string? activePackageId = null,
        GatewayEndToEndManifestCandidate? candidate = null,
        GatewayEndToEndFailureSnapshot? failure = null)
    {
        Kind = kind;
        ActivePackageId = activePackageId;
        ManifestCandidate = candidate;
        FailureSnapshot = failure;
    }

    public GatewayEndToEndManifestDiscoveryStepKind Kind { get; }

    public string? ActivePackageId { get; }

    public GatewayEndToEndManifestCandidate? ManifestCandidate { get; }

    public GatewayEndToEndFailureSnapshot? FailureSnapshot { get; }

    public static GatewayEndToEndManifestDiscoveryStep Progress() =>
        new(GatewayEndToEndManifestDiscoveryStepKind.Progress);

    public static GatewayEndToEndManifestDiscoveryStep ActivePackage(string packageId) =>
        new(
            GatewayEndToEndManifestDiscoveryStepKind.ActivePackage,
            activePackageId: string.IsNullOrWhiteSpace(packageId)
                ? throw new ArgumentException("An active package ID is required.", nameof(packageId))
                : packageId.Trim().ToLowerInvariant());

    public static GatewayEndToEndManifestDiscoveryStep Candidate(
        GatewayEndToEndManifestCandidate candidate) =>
        new(
            GatewayEndToEndManifestDiscoveryStepKind.Candidate,
            candidate: candidate ?? throw new ArgumentNullException(nameof(candidate)));

    public static GatewayEndToEndManifestDiscoveryStep Failure(
        string code,
        string message,
        string? packageId = null,
        string? source = null) =>
        new(
            GatewayEndToEndManifestDiscoveryStepKind.Failure,
            failure: new GatewayEndToEndFailureSnapshot(code, message, packageId, source));

    public static GatewayEndToEndManifestDiscoveryStep Complete() =>
        new(GatewayEndToEndManifestDiscoveryStepKind.Complete);
}

public interface IGatewayEndToEndInspectionOperationFactory
{
    IGatewayEndToEndInspectionOperation Begin(
        GatewayEndToEndManifestCandidate candidate,
        IReadOnlyList<string> activePackageIds);
}

public interface IGatewayEndToEndInspectionOperation
{
    bool IsCompleted { get; }

    GatewayEndToEndBundleInspectionResult GetOutcome();
}

public interface IGatewayEndToEndArtifactStore
{
    bool IsAttached { get; }

    GatewayEndToEndSnapshot? CommittedSnapshot { get; }

    IGatewayEndToEndPersistenceOperation BeginPersist(GatewayEndToEndSnapshot snapshot);
}

internal interface IGatewayEndToEndSessionArtifactStore : IGatewayEndToEndArtifactStore
{
    string? ArtifactPath { get; }

    IGatewayEndToEndPersistenceOperation BeginAttachSession(
        string runId,
        GatewayEndToEndSnapshot initialSnapshot);
}

public interface IGatewayEndToEndPersistenceOperation
{
    bool IsCompleted { get; }

    GatewayEndToEndPersistenceOutcome GetOutcome();
}

public sealed class GatewayEndToEndPersistenceOutcome
{
    private GatewayEndToEndPersistenceOutcome(
        bool succeeded,
        GatewayEndToEndSnapshot snapshot,
        Exception? failure)
    {
        Succeeded = succeeded;
        Snapshot = snapshot;
        Failure = failure;
    }

    public bool Succeeded { get; }

    public GatewayEndToEndSnapshot Snapshot { get; }

    public Exception? Failure { get; }

    public static GatewayEndToEndPersistenceOutcome Success(GatewayEndToEndSnapshot snapshot) =>
        new(true, snapshot ?? throw new ArgumentNullException(nameof(snapshot)), null);

    public static GatewayEndToEndPersistenceOutcome Failed(
        GatewayEndToEndSnapshot snapshot,
        Exception failure) =>
        new(
            false,
            snapshot ?? throw new ArgumentNullException(nameof(snapshot)),
            failure ?? throw new ArgumentNullException(nameof(failure)));
}
