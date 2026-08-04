using System.Collections.ObjectModel;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.EndToEndHost;

public sealed class EndToEndBundleArtifact
{
    internal EndToEndBundleArtifact(
        string sourceAssemblyPath,
        string assemblyFileName,
        string manifestFileName,
        string manifestJson,
        EndToEndBundleManifest manifest)
    {
        SourceAssemblyPath = sourceAssemblyPath;
        AssemblyFileName = assemblyFileName;
        ManifestFileName = manifestFileName;
        ManifestJson = manifestJson;
        Manifest = manifest;
    }

    public string SourceAssemblyPath { get; }

    public string AssemblyFileName { get; }

    public string ManifestFileName { get; }

    public string ManifestJson { get; }

    public EndToEndBundleManifest Manifest { get; }
}

public sealed class EndToEndOwnerStagePlan
{
    internal EndToEndOwnerStagePlan(
        string modsRoot,
        string ownerPackageId,
        string rimWorldVersion,
        string destinationDirectory,
        IEnumerable<EndToEndBundleArtifact> bundles)
    {
        ModsRoot = modsRoot;
        OwnerPackageId = ownerPackageId;
        RimWorldVersion = rimWorldVersion;
        DestinationDirectory = destinationDirectory;
        Bundles = new ReadOnlyCollection<EndToEndBundleArtifact>(bundles.ToArray());
    }

    public string ModsRoot { get; }

    public string OwnerPackageId { get; }

    public string RimWorldVersion { get; }

    public string DestinationDirectory { get; }

    public IReadOnlyList<EndToEndBundleArtifact> Bundles { get; }
}

public sealed class EndToEndBundlePlan
{
    internal EndToEndBundlePlan(
        EndToEndDiscoveryResult discovery,
        IEnumerable<EndToEndOwnerStagePlan> ownerStages)
    {
        Discovery = discovery;
        OwnerStages = new ReadOnlyCollection<EndToEndOwnerStagePlan>(ownerStages.ToArray());
    }

    public EndToEndDiscoveryResult Discovery { get; }

    public IReadOnlyList<EndToEndOwnerStagePlan> OwnerStages { get; }
}

public static class EndToEndBundlePlanner
{
    public static EndToEndBundlePlan Create(
        IEnumerable<EndToEndAssemblyCandidate> assemblyCandidates,
        IEnumerable<string> resolvablePackageIds,
        string modsRoot,
        string rimWorldVersion)
    {
        var candidates = assemblyCandidates?.ToArray() ??
            throw new ArgumentNullException(nameof(assemblyCandidates));
        var discovery = EndToEndDiscoveryValidator.ValidateAndGroup(candidates, resolvablePackageIds);
        var root = Path.GetFullPath(
            string.IsNullOrWhiteSpace(modsRoot)
                ? throw new ArgumentException("A mods root is required.", nameof(modsRoot))
                : modsRoot);

        var ownerStages = candidates
            .GroupBy(candidate => candidate.ExpectedOwnerPackageId.Trim().ToLowerInvariant(), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => CreateOwnerStage(root, rimWorldVersion, group.Key, group))
            .ToArray();
        return new EndToEndBundlePlan(discovery, ownerStages);
    }

    private static EndToEndOwnerStagePlan CreateOwnerStage(
        string modsRoot,
        string rimWorldVersion,
        string ownerPackageId,
        IEnumerable<EndToEndAssemblyCandidate> candidates)
    {
        var bundles = candidates
            .OrderBy(candidate => candidate.Metadata.AssemblyName, StringComparer.Ordinal)
            .Select(candidate => CreateBundle(ownerPackageId, candidate))
            .ToArray();
        var duplicateFile = bundles
            .SelectMany(bundle => new[] { bundle.AssemblyFileName, bundle.ManifestFileName })
            .GroupBy(fileName => fileName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateFile is not null)
        {
            throw new EndToEndStageException(
                $"Owner '{ownerPackageId}' has duplicate staged file '{duplicateFile.Key}'.");
        }

        return new EndToEndOwnerStagePlan(
            modsRoot,
            ownerPackageId,
            rimWorldVersion,
            EndToEndStagePaths.GetDestination(modsRoot, ownerPackageId, rimWorldVersion),
            bundles);
    }

    private static EndToEndBundleArtifact CreateBundle(
        string ownerPackageId,
        EndToEndAssemblyCandidate candidate)
    {
        var metadata = candidate.Metadata;
        var assemblyFileName = metadata.AssemblyName + ".dll";
        var manifestFileName = metadata.AssemblyName + ".e2etests.json";
        var manifest = new EndToEndBundleManifest
        {
            OwnerPackageId = ownerPackageId,
            Assembly = assemblyFileName,
            AssemblyIdentity = metadata.AssemblyIdentity,
            ModuleVersionId = metadata.ModuleVersionId.ToString("D"),
            AssemblyLength = metadata.Length,
            AssemblySha256 = metadata.Sha256,
            Dependencies = metadata.AssemblyReferences
                .OrderBy(reference => reference.Name, StringComparer.Ordinal)
                .ThenBy(reference => reference.Version, StringComparer.Ordinal)
                .Select(reference => new EndToEndBundleDependency
                {
                    Name = reference.Name,
                    Version = reference.Version,
                    Culture = reference.Culture,
                    KeyKind = reference.KeyKind,
                    PublicKeyOrToken = reference.PublicKeyOrToken,
                    Identity = reference.Identity
                })
                .ToArray(),
            Tests = metadata.Declarations
                .OrderBy(test => test.Id, StringComparer.Ordinal)
                .Select(test => new EndToEndBundleTest
                {
                    Id = test.Id.Trim(),
                    TypeName = test.TypeName,
                    OwnerPackageId = test.OwnerPackageId.Trim().ToLowerInvariant(),
                    ActivePackageIds = test.ActivePackageIds
                        .Select(packageId => packageId.Trim().ToLowerInvariant())
                        .ToArray(),
                    MaxFrames = test.MaxFrames,
                    MaxGameTicks = test.MaxGameTicks,
                    MaxWallClockSeconds = test.MaxWallClockSeconds
                })
                .ToArray()
        };

        return new EndToEndBundleArtifact(
            metadata.AssemblyPath,
            assemblyFileName,
            manifestFileName,
            GatewayContractJson.Write(manifest),
            manifest);
    }
}
