using System.Collections.ObjectModel;

namespace RimWorldDevGateway;

public sealed class GatewayEndToEndSnapshot
{
    public GatewayEndToEndSnapshot(
        bool enabled,
        string discoveryState,
        IEnumerable<string>? activePackageIds = null,
        IEnumerable<GatewayEndToEndBundleSnapshot>? bundles = null,
        IEnumerable<GatewayEndToEndTestSnapshot>? tests = null,
        IEnumerable<GatewayEndToEndFailureSnapshot>? failures = null)
    {
        Enabled = enabled;
        DiscoveryState = string.IsNullOrWhiteSpace(discoveryState)
            ? throw new ArgumentException("A discovery state is required.", nameof(discoveryState))
            : discoveryState;
        ActivePackageIds = ReadOnly(activePackageIds ?? Array.Empty<string>());
        Bundles = ReadOnly((bundles ?? Array.Empty<GatewayEndToEndBundleSnapshot>())
            .OrderBy(bundle => bundle.ManifestPath, StringComparer.Ordinal));
        Tests = ReadOnly((tests ?? Array.Empty<GatewayEndToEndTestSnapshot>())
            .OrderBy(test => test.Id, StringComparer.Ordinal));
        Failures = ReadOnly(failures ?? Array.Empty<GatewayEndToEndFailureSnapshot>());
    }

    public bool Enabled { get; }

    public string DiscoveryState { get; }

    public IReadOnlyList<string> ActivePackageIds { get; }

    public IReadOnlyList<GatewayEndToEndBundleSnapshot> Bundles { get; }

    public IReadOnlyList<GatewayEndToEndTestSnapshot> Tests { get; }

    public IReadOnlyList<GatewayEndToEndFailureSnapshot> Failures { get; }

    private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>(values.ToArray());
}

public sealed class GatewayEndToEndBundleSnapshot
{
    public GatewayEndToEndBundleSnapshot(
        string containingPackageId,
        string manifestPath,
        string state,
        string? assemblyIdentity,
        string? assemblySha256,
        int admittedTestCount,
        string? failureCode = null)
    {
        ContainingPackageId = containingPackageId;
        ManifestPath = manifestPath;
        State = state;
        AssemblyIdentity = assemblyIdentity;
        AssemblySha256 = assemblySha256;
        AdmittedTestCount = admittedTestCount;
        FailureCode = failureCode;
    }

    public string ContainingPackageId { get; }

    public string ManifestPath { get; }

    public string State { get; }

    public string? AssemblyIdentity { get; }

    public string? AssemblySha256 { get; }

    public int AdmittedTestCount { get; }

    public string? FailureCode { get; }
}

public sealed class GatewayEndToEndTestSnapshot
{
    internal GatewayEndToEndTestSnapshot(GatewayEndToEndRuntimeTestDescriptor descriptor)
    {
        Id = descriptor.Id;
        OwnerPackageId = descriptor.OwnerPackageId;
        ActivePackageIds = new ReadOnlyCollection<string>(descriptor.ActivePackageIds.ToArray());
        TypeName = descriptor.TypeName;
        MaxFrames = descriptor.MaxFrames;
        MaxGameTicks = descriptor.MaxGameTicks;
        MaxWallClockSeconds = descriptor.MaxWallClockSeconds;
    }

    public string Id { get; }

    public string OwnerPackageId { get; }

    public IReadOnlyList<string> ActivePackageIds { get; }

    public string TypeName { get; }

    public int MaxFrames { get; }

    public int MaxGameTicks { get; }

    public int MaxWallClockSeconds { get; }
}

public sealed class GatewayEndToEndFailureSnapshot
{
    public GatewayEndToEndFailureSnapshot(
        string code,
        string message,
        string? packageId = null,
        string? source = null)
    {
        Code = string.IsNullOrWhiteSpace(code)
            ? throw new ArgumentException("A failure code is required.", nameof(code))
            : code;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        PackageId = packageId;
        Source = source;
    }

    public string Code { get; }

    public string Message { get; }

    public string? PackageId { get; }

    public string? Source { get; }
}
