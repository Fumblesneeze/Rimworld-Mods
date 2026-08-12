using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using RimWorldDevGateway.PerformanceTesting;

namespace RimWorldDevGateway.Performance;

internal sealed class GatewayCircinusPerformanceBackend : IGatewayPerformanceRuntimeBackend
{
    private readonly Func<IReadOnlyList<Assembly>> loadedAssemblies;
    private readonly Func<IReadOnlyList<string>> activePackageIds;
    private readonly Func<string> saveDataFolder;
    private readonly string artifactDirectory;
    private PerformanceTestDescriptor? preparedDescriptor;
    private CircinusRuntimeBinding? binding;
    private PerformanceMethodSelection? selection;
    private CircinusRuntimeRun? run;
    private PerformanceTickBoundaryPatch? tickBoundary;

    public GatewayCircinusPerformanceBackend(
        Func<IReadOnlyList<Assembly>> loadedAssemblies,
        Func<IReadOnlyList<string>> activePackageIds,
        Func<string> saveDataFolder,
        string artifactDirectory)
    {
        this.loadedAssemblies = loadedAssemblies ?? throw new ArgumentNullException(nameof(loadedAssemblies));
        this.activePackageIds = activePackageIds ?? throw new ArgumentNullException(nameof(activePackageIds));
        this.saveDataFolder = saveDataFolder ?? throw new ArgumentNullException(nameof(saveDataFolder));
        this.artifactDirectory = Path.GetFullPath(string.IsNullOrWhiteSpace(artifactDirectory)
            ? throw new ArgumentException("A performance artifact directory is required.", nameof(artifactDirectory))
            : artifactDirectory);
    }

    public void Prepare(PerformanceTestDescriptor descriptor)
    {
        if (preparedDescriptor is not null || run is not null)
            throw new InvalidOperationException("A Circinus performance session is already prepared.");
        var assemblies = loadedAssemblies().Where(item => item is not null).ToArray();
        var packages = activePackageIds();
        var circinusActive = packages.Contains(
            PerformanceTestContract.CircinusPackageId,
            StringComparer.OrdinalIgnoreCase);
        if (!CircinusRuntimeAdapter.TryBind(circinusActive, assemblies, out binding, out var bindReason))
            throw new InvalidOperationException(bindReason);
        if (!ReflectionPerformanceHarmonyCatalog.TryBind(assemblies, out var harmony, out var harmonyReason))
            throw new InvalidOperationException(harmonyReason);
        if (!PerformanceTickBoundaryPatch.TryInstall(assemblies, harmony!, out tickBoundary, out var patchReason))
            throw new InvalidOperationException(patchReason);
        try
        {
            selection = PerformanceMethodSelectorResolver.Resolve(
                assemblies,
                harmony!,
                descriptor.MethodSelectors.Select(item =>
                    new PerformanceSelectionRequest(item.Kind, item.Value, item.Category)));
            preparedDescriptor = descriptor;
        }
        catch
        {
            tickBoundary!.Dispose();
            tickBoundary = null;
            binding = null;
            throw;
        }
    }

    public void Start(PerformanceTestDescriptor descriptor)
    {
        RequirePrepared(descriptor);
        if (run is not null) throw new InvalidOperationException("The Circinus performance run already started.");
        if (!binding!.TryBeginRun(descriptor.Id, out run, out var reason))
            throw new InvalidOperationException(reason);
        try
        {
            var armsMethods = descriptor.EvidenceLens != PerformanceEvidenceLens.FullyDisarmed;
            if (armsMethods && !run!.TryArmSelection(selection!, out reason))
                throw new InvalidOperationException(reason);
            run!.AddMarker("gateway.evidence-lens", ControlMode(descriptor.EvidenceLens));
            run.AddMarker("gateway.sample-start", descriptor.WorkloadVersion);
            run.SetSampling(descriptor.EvidenceLens is PerformanceEvidenceLens.ProductInstrumented or
                PerformanceEvidenceLens.ProductAbsentControl);
        }
        catch
        {
            run?.Dispose();
            run = null;
            throw;
        }
    }

    public void ArmTickBoundary(int gameTick)
    {
        if (preparedDescriptor is null || tickBoundary is null)
            throw new InvalidOperationException("The exact performance tick boundary is not prepared.");
        tickBoundary.Arm(gameTick);
    }

    public void ConfirmTickBoundary(int gameTick)
    {
        if (tickBoundary is null)
            throw new InvalidOperationException("The exact performance tick boundary is not prepared.");
        tickBoundary.Confirm(gameTick);
    }

    public void DisarmTickBoundary() => tickBoundary?.Disarm();

    public IReadOnlyDictionary<string, string> Complete(
        PerformanceTestDescriptor descriptor,
        GatewayPerformanceControlWindow controlWindow,
        IReadOnlyDictionary<string, long> throughputCounts)
    {
        RequirePrepared(descriptor);
        return CompleteOwnedRun(ref run, activeRun =>
        {
            activeRun.AddMarker("gateway.sample-end", descriptor.WorkloadVersion);
            activeRun.SetSampling(false);
            if (!activeRun.TryStopAndCapture(ReadPersistedRun, out var capture, out var reason))
                throw new InvalidOperationException(reason);
            var elapsedTicks = (long)controlWindow.EndGameTick - controlWindow.StartGameTick;
            var elapsedWallMilliseconds =
                (controlWindow.EndTimestamp - controlWindow.StartTimestamp) * 1000d / Stopwatch.Frequency;
            var normalized = PerformanceSampleNormalizer.Normalize(
                capture!,
                new PerformanceNormalizationContext(
                    descriptor.WorkloadVersion,
                    descriptor.EvidenceLens,
                    ControlMode(descriptor.EvidenceLens),
                    elapsedTicks,
                    elapsedWallMilliseconds,
                    controlWindow.ManagedMemoryStartBytes,
                    controlWindow.ManagedMemoryEndBytes,
                    controlWindow.GarbageCollectionsStart,
                    controlWindow.GarbageCollectionsEnd,
                    throughputCounts));
            return PerformanceArtifactWriter.Write(artifactDirectory, descriptor.Id, capture!, normalized);
        });
    }

    public void Cleanup()
    {
        var failures = new List<Exception>();
        try
        {
            DisposeOwned(ref run);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }
        try
        {
            DisposeOwned(ref tickBoundary);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        if (failures.Count > 0)
            throw new AggregateException(
                "Circinus performance cleanup retained one or more retryable owned resources.",
                failures);

        selection = null;
        binding = null;
        preparedDescriptor = null;
    }

    public bool TryFinalize(out string reason)
    {
        reason = string.Empty;
        return true;
    }

    internal static void DisposeOwned<T>(ref T? owned) where T : class, IDisposable
    {
        var retained = owned;
        if (retained is null) return;
        retained.Dispose();
        owned = null;
    }

    private string? ReadPersistedRun(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId) || !StringComparer.Ordinal.Equals(Path.GetFileName(runId), runId))
            return null;
        var root = Path.GetFullPath(saveDataFolder());
        var runs = Path.GetFullPath(Path.Combine(root, "Circinus", "Runs"));
        var path = Path.GetFullPath(Path.Combine(runs, runId + ".json"));
        if (!path.StartsWith(runs + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(path))
            return null;
        return File.ReadAllText(path, Encoding.UTF8);
    }

    private void RequirePrepared(PerformanceTestDescriptor descriptor)
    {
        if (preparedDescriptor is null ||
            !ReferenceEquals(preparedDescriptor, descriptor))
            throw new InvalidOperationException("The Circinus backend was not prepared for this exact descriptor.");
    }

    private static string ControlMode(PerformanceEvidenceLens lens) => lens switch
    {
        PerformanceEvidenceLens.ProductInstrumented => "instrumented",
        PerformanceEvidenceLens.ArmedDisabledWrapper => "armed-disabled-wrapper",
        PerformanceEvidenceLens.FullyDisarmed => "fully-disarmed",
        PerformanceEvidenceLens.ProductAbsentControl => "product-absent-control",
        _ => throw new ArgumentOutOfRangeException(nameof(lens))
    };

    internal static TResult CompleteOwnedRun<TResult>(
        ref CircinusRuntimeRun? ownedRun,
        Func<CircinusRuntimeRun, TResult> complete)
    {
        var activeRun = ownedRun ??
                        throw new InvalidOperationException("The Circinus performance run has not started.");
        if (complete is null) throw new ArgumentNullException(nameof(complete));
        try
        {
            return complete(activeRun);
        }
        finally
        {
            activeRun.Dispose();
            ownedRun = null;
        }
    }
}

internal static class PerformanceArtifactWriter
{
    public static IReadOnlyDictionary<string, string> Write(
        string artifactRoot,
        string benchmarkId,
        CircinusCapture capture,
        PerformanceNormalizedSample normalized)
    {
        if (capture is null) throw new ArgumentNullException(nameof(capture));
        if (normalized is null) throw new ArgumentNullException(nameof(normalized));
        var root = Path.GetFullPath(artifactRoot);
        Directory.CreateDirectory(root);
        // Isolated SavedData session roots are already deep. Keep both the published and
        // transactional siblings below legacy Mono/Windows MAX_PATH rather than relying
        // on host long-path policy.
        var name = "p-" + HashArtifactIdentity(benchmarkId).Substring(0, 12);
        var destination = Path.Combine(root, name);
        if (Directory.Exists(destination))
            throw new IOException("The exact performance artifact directory already exists.");
        var temporary = Path.Combine(root, "t-" + Guid.NewGuid().ToString("N").Substring(0, 12));
        try
        {
            Directory.CreateDirectory(temporary);
            var inMemory = Path.Combine(temporary, "circinus.in-memory.json");
            var persisted = Path.Combine(temporary, "circinus.persisted.json");
            var normalizedPath = Path.Combine(temporary, "performance.normalized.json");
            File.WriteAllText(inMemory, capture.InMemoryJson, new UTF8Encoding(false));
            File.WriteAllText(persisted, capture.PersistedJson, new UTF8Encoding(false));
            using (var stream = new FileStream(normalizedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                new DataContractJsonSerializer(typeof(PerformanceNormalizedSample)).WriteObject(stream, normalized);
            Directory.Move(temporary, destination);
        }
        catch
        {
            if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true);
            throw;
        }

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["performance.run_id"] = capture.RunId,
            ["performance.raw.in_memory"] = Path.Combine(destination, "circinus.in-memory.json"),
            ["performance.raw.persisted"] = Path.Combine(destination, "circinus.persisted.json"),
            ["performance.normalized"] = Path.Combine(destination, "performance.normalized.json")
        };
    }

    internal static string HashArtifactIdentity(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A benchmark ID is required.", nameof(value));
        using var algorithm = SHA256.Create();
        return string.Concat(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(item => item.ToString("x2")));
    }
}
