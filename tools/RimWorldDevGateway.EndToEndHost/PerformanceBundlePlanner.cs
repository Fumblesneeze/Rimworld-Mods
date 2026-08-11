using System.Collections.ObjectModel;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.EndToEndHost;

public sealed class PerformanceBundlePlan
{
    internal PerformanceBundlePlan(
        PerformanceDiscoveryResult discovery,
        IEnumerable<EndToEndOwnerStagePlan> ownerStages)
    {
        Discovery = discovery;
        OwnerStages = new ReadOnlyCollection<EndToEndOwnerStagePlan>(ownerStages.ToArray());
    }

    public PerformanceDiscoveryResult Discovery { get; }
    public IReadOnlyList<EndToEndOwnerStagePlan> OwnerStages { get; }
}

public static class PerformanceBundlePlanner
{
    public static PerformanceBundlePlan Create(
        IEnumerable<PerformanceAssemblyCandidate> assemblyCandidates,
        IEnumerable<string> resolvablePackageIds,
        string modsRoot,
        string rimWorldVersion)
    {
        var candidates = MaterializeCandidates(assemblyCandidates);
        var discovery = PerformanceDiscoveryValidator.ValidateAndGroup(candidates, resolvablePackageIds);
        var root = Path.GetFullPath(string.IsNullOrWhiteSpace(modsRoot)
            ? throw new ArgumentException("A mods root is required.", nameof(modsRoot))
            : modsRoot);
        var ownerStages = candidates
            .GroupBy(candidate => candidate.ExpectedOwnerPackageId.Trim().ToLowerInvariant(), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => CreateOwnerStage(root, rimWorldVersion, group.Key, group))
            .ToArray();
        return new PerformanceBundlePlan(discovery, ownerStages);
    }

    private static EndToEndOwnerStagePlan CreateOwnerStage(
        string modsRoot,
        string rimWorldVersion,
        string ownerPackageId,
        IEnumerable<PerformanceAssemblyCandidate> candidates)
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
        PerformanceAssemblyCandidate candidate)
    {
        var metadata = candidate.Metadata;
        var assemblyFileName = metadata.AssemblyName + ".dll";
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
                }).ToArray(),
            Tests = metadata.Declarations
                .OrderBy(test => test.Id, StringComparer.Ordinal)
                .Select(test => ToManifestTest(ownerPackageId, test))
                .ToArray()
        };
        return new EndToEndBundleArtifact(
            metadata.AssemblyPath,
            assemblyFileName,
            metadata.AssemblyName + ".e2etests.json",
            GatewayContractJson.Write(manifest),
            manifest);
    }

    private static EndToEndBundleTest ToManifestTest(
        string ownerPackageId,
        PerformanceMetadataDeclaration test)
    {
        PerformanceBundleDeadline deadline;
        try
        {
            deadline = PerformanceBundleDeadline.Calculate(test.WarmUpTicks, test.SampleTicks);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new EndToEndStageException(
                "The declared performance phases cannot fit the E2E deadline representation.",
                exception);
        }
        return new EndToEndBundleTest
        {
            Id = test.Id.Trim(),
            TypeName = test.TypeName,
            OwnerPackageId = ownerPackageId,
            ActivePackageIds = test.ActivePackageIds.Select(value => value.Trim().ToLowerInvariant()).ToArray(),
            MaxFrames = deadline.MaxFrames,
            MaxGameTicks = deadline.MaxGameTicks,
            MaxWallClockSeconds = deadline.MaxWallClockSeconds,
            Kind = EndToEndBundleTest.PerformanceKind,
            MeasuredSubjectPackageId = test.MeasuredSubjectPackageId.Trim().ToLowerInvariant(),
            DeterministicSeed = test.DeterministicSeed,
            WorkloadVersion = test.WorkloadVersion,
            ComparisonId = test.ComparisonId,
            WarmUpTicks = test.WarmUpTicks,
            SampleTicks = test.SampleTicks,
            GameSpeed = test.GameSpeed,
            Repetitions = test.Repetitions,
            EvidenceLens = test.EvidenceLens,
            ProductAbsentControlId = test.ProductAbsentControlId,
            MethodSelectors = test.MethodSelectors.Select(selector => new PerformanceBundleMethodSelector
            {
                Kind = selector.Kind,
                Value = selector.Value,
                Category = selector.Category
            }).ToArray(),
            ThroughputCheckpoints = test.ThroughputCheckpoints.Select(checkpoint =>
                new PerformanceBundleThroughputCheckpoint
                {
                    Id = checkpoint.Id,
                    MinimumCount = checkpoint.MinimumCount
                }).ToArray()
        };
    }

    private static PerformanceAssemblyCandidate[] MaterializeCandidates(
        IEnumerable<PerformanceAssemblyCandidate>? assemblyCandidates)
    {
        if (assemblyCandidates is null) throw new ArgumentNullException(nameof(assemblyCandidates));
        var candidates = new List<PerformanceAssemblyCandidate>();
        using var enumerator = assemblyCandidates.GetEnumerator();
        while (enumerator.MoveNext())
        {
            if (candidates.Count == PerformanceDiscoveryValidator.MaximumBenchmarks)
            {
                throw new EndToEndDiscoveryException(
                    $"Performance discovery exceeds the published " +
                    $"{PerformanceDiscoveryValidator.MaximumBenchmarks}-candidate-assembly ceiling " +
                    "(the same ceiling as total declarations).");
            }

            candidates.Add(enumerator.Current ??
                           throw new EndToEndDiscoveryException(
                               "Performance discovery contains a null assembly candidate."));
        }

        return candidates.ToArray();
    }
}

public static class EndToEndStageTransaction
{
    public static IReadOnlyList<EndToEndStageLease> PublishAll(
        IEnumerable<EndToEndOwnerStagePlan> ownerStages,
        Func<EndToEndOwnerStagePlan, Action<EndToEndStageLease>, EndToEndStageLease> publish,
        Func<EndToEndStageLease, bool> cleanup,
        Action<IReadOnlyList<EndToEndStageLease>> persistLeases,
        Action clearLeases)
    {
        if (ownerStages is null) throw new ArgumentNullException(nameof(ownerStages));
        if (publish is null) throw new ArgumentNullException(nameof(publish));
        if (cleanup is null) throw new ArgumentNullException(nameof(cleanup));
        if (persistLeases is null) throw new ArgumentNullException(nameof(persistLeases));
        if (clearLeases is null) throw new ArgumentNullException(nameof(clearLeases));

        var stages = ownerStages.ToArray();
        if (stages.Length == 0) throw new EndToEndStageException("Staging selected zero owners.");
        var leases = new List<EndToEndStageLease>();
        try
        {
            foreach (var owner in stages)
            {
                EndToEndStageLease? prepared = null;
                var published = publish(owner, lease =>
                {
                    if (lease is null)
                        throw new EndToEndStageException("A stage published a null lease state.");
                    if (prepared is null)
                    {
                        prepared = lease;
                        leases.Add(prepared);
                    }
                    else if (!ReferenceEquals(prepared, lease))
                    {
                        throw new EndToEndStageException("A stage changed lease identity during publication.");
                    }

                    if (lease.State == EndToEndStageLeaseState.RolledBack)
                    {
                        leases.Remove(lease);
                        if (leases.Count == 0) clearLeases();
                        else persistLeases(leases.ToArray());
                        return;
                    }

                    try
                    {
                        persistLeases(leases.ToArray());
                    }
                    catch
                    {
                        if (lease.State == EndToEndStageLeaseState.Prepared) leases.Remove(lease);
                        throw;
                    }
                });
                if (prepared is null || !ReferenceEquals(prepared, published) ||
                    published.State != EndToEndStageLeaseState.Committed)
                    throw new EndToEndStageException(
                        "A stage did not return its exact pre-commit lease instance.");
            }

            return new ReadOnlyCollection<EndToEndStageLease>(leases.ToArray());
        }
        catch (Exception primary)
        {
            var cleanupFailures = new List<Exception>();
            for (var index = leases.Count - 1; index >= 0; index--)
            {
                try
                {
                    if (cleanup(leases[index]))
                    {
                        leases.RemoveAt(index);
                    }
                    else
                    {
                        cleanupFailures.Add(new EndToEndStageException(
                            $"Cleanup refused retained stage '{leases[index].DestinationDirectory}'."));
                    }
                }
                catch (Exception exception)
                {
                    cleanupFailures.Add(exception);
                }
            }

            try
            {
                if (leases.Count == 0) clearLeases();
                else persistLeases(leases.ToArray());
            }
            catch (Exception exception)
            {
                cleanupFailures.Add(exception);
            }

            if (cleanupFailures.Count == 0) throw;
            cleanupFailures.Insert(0, primary);
            throw new EndToEndStageException(
                "Stage publication failed and cleanup was incomplete; retained leases were persisted.",
                new AggregateException(cleanupFailures));
        }
    }
}
