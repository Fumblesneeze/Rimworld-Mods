using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;

namespace RimWorldDevGateway;

public sealed class GatewayEndToEndManifestCandidate
{
    public GatewayEndToEndManifestCandidate(string containingPackageId, string manifestPath)
    {
        ContainingPackageId = string.IsNullOrWhiteSpace(containingPackageId)
            ? throw new ArgumentException("A containing package ID is required.", nameof(containingPackageId))
            : containingPackageId.Trim().ToLowerInvariant();
        ManifestPath = string.IsNullOrWhiteSpace(manifestPath)
            ? throw new ArgumentException("A manifest path is required.", nameof(manifestPath))
            : Path.GetFullPath(manifestPath);
    }

    public string ContainingPackageId { get; }

    public string ManifestPath { get; }
}

public sealed class GatewayEndToEndLoadFailure
{
    public GatewayEndToEndLoadFailure(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public string Code { get; }

    public string Message { get; }
}

public sealed class GatewayEndToEndRuntimeTestDescriptor
{
    internal GatewayEndToEndRuntimeTestDescriptor(
        string id,
        string ownerPackageId,
        IEnumerable<string> activePackageIds,
        string typeName,
        int maxFrames,
        int maxGameTicks,
        int maxWallClockSeconds,
        Type testType)
    {
        Id = id;
        OwnerPackageId = ownerPackageId;
        ActivePackageIds = new ReadOnlyCollection<string>(activePackageIds.ToArray());
        TypeName = typeName;
        MaxFrames = maxFrames;
        MaxGameTicks = maxGameTicks;
        MaxWallClockSeconds = maxWallClockSeconds;
        TestType = testType;
    }

    public string Id { get; }

    public string OwnerPackageId { get; }

    public IReadOnlyList<string> ActivePackageIds { get; }

    public string TypeName { get; }

    public int MaxFrames { get; }

    public int MaxGameTicks { get; }

    public int MaxWallClockSeconds { get; }

    public Type TestType { get; }
}

public sealed class GatewayEndToEndAssemblySource
{
    internal GatewayEndToEndAssemblySource(
        string containingPackageId,
        string manifestPath,
        string assemblyIdentity,
        string assemblySha256,
        Assembly assembly,
        IEnumerable<GatewayEndToEndRuntimeTestDescriptor> tests)
    {
        ContainingPackageId = containingPackageId;
        ManifestPath = manifestPath;
        AssemblyIdentity = assemblyIdentity;
        AssemblySha256 = assemblySha256;
        Assembly = assembly;
        Tests = new ReadOnlyCollection<GatewayEndToEndRuntimeTestDescriptor>(tests.ToArray());
    }

    public string ContainingPackageId { get; }

    public string ManifestPath { get; }

    public string AssemblyIdentity { get; }

    public string AssemblySha256 { get; }

    public Assembly Assembly { get; }

    public IReadOnlyList<GatewayEndToEndRuntimeTestDescriptor> Tests { get; }
}

public sealed class GatewayEndToEndBundleInspectionResult
{
    private GatewayEndToEndBundleInspectionResult(
        string state,
        GatewayEndToEndAssemblySource? source,
        GatewayEndToEndLoadFailure? failure)
    {
        State = state;
        Source = source;
        Failure = failure;
    }

    public string State { get; }

    public GatewayEndToEndAssemblySource? Source { get; }

    public GatewayEndToEndLoadFailure? Failure { get; }

    public int AdmittedTestInstanceCount => 0;

    internal static GatewayEndToEndBundleInspectionResult Loaded(GatewayEndToEndAssemblySource source) =>
        new("loaded", source, null);

    internal static GatewayEndToEndBundleInspectionResult Skipped() =>
        new("skipped", null, null);

    internal static GatewayEndToEndBundleInspectionResult Failed(string code, string message) =>
        new("failed", null, new GatewayEndToEndLoadFailure(code, message));
}

public interface IGatewayEndToEndAssemblyLoader
{
    Assembly Load(byte[] assemblyBytes);
}

public interface IGatewayEndToEndBundleInspector
{
    GatewayEndToEndBundleInspectionResult Inspect(
        GatewayEndToEndManifestCandidate candidate,
        IReadOnlyList<string> activePackageIds);
}

public sealed class GatewayEndToEndAssemblyByteLoader : IGatewayEndToEndAssemblyLoader
{
    public Assembly Load(byte[] assemblyBytes)
    {
        if (assemblyBytes is null || assemblyBytes.Length == 0)
        {
            throw new ArgumentException("E2E assembly bytes are required.", nameof(assemblyBytes));
        }

        return Assembly.Load(assemblyBytes);
    }
}
