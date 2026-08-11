using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using RimWorldDevGateway.PerformanceTesting;

namespace RimWorldDevGateway.Performance;

internal sealed class GatewayDpaPerformanceBackend :
    IGatewayPerformanceRuntimeBackend,
    IDeferredGatewayPerformanceCleanup
{
    private readonly Func<IReadOnlyList<Assembly>> loadedAssemblies;
    private readonly Func<IReadOnlyList<string>> activePackageIds;
    private readonly string artifactDirectory;
    private PerformanceTestDescriptor? descriptor;
    private DpaRuntimeBinding? binding;
    private DpaRuntimeRun? run;
    private MethodInfo? selector;
    private PerformanceTickBoundaryPatch? tickBoundary;

    public GatewayDpaPerformanceBackend(
        Func<IReadOnlyList<Assembly>> loadedAssemblies,
        Func<IReadOnlyList<string>> activePackageIds,
        string artifactDirectory)
    {
        this.loadedAssemblies = loadedAssemblies ?? throw new ArgumentNullException(nameof(loadedAssemblies));
        this.activePackageIds = activePackageIds ?? throw new ArgumentNullException(nameof(activePackageIds));
        this.artifactDirectory = Path.GetFullPath(string.IsNullOrWhiteSpace(artifactDirectory)
            ? throw new ArgumentException("A performance artifact directory is required.", nameof(artifactDirectory))
            : artifactDirectory);
    }

    public void Prepare(PerformanceTestDescriptor requested)
    {
        if (descriptor is not null || run is not null)
            throw new InvalidOperationException("A DPA diagnostic is already prepared.");
        if (requested.Profiler != PerformanceProfilerKind.DpaDiagnostic)
            throw new InvalidOperationException("The descriptor is not a DPA diagnostic.");
        if (requested.MethodSelectors.Count != 1 ||
            requested.MethodSelectors[0].Kind != PerformanceMethodSelectorKind.Method)
            throw new InvalidOperationException("A DPA diagnostic requires one exact outer-method selector.");
        var assemblies = loadedAssemblies().Take(DpaRuntimeAdapter.MaximumLoadedAssemblies + 1).ToArray();
        var packages = activePackageIds();
        if (packages.Contains(PerformanceTestContract.CircinusPackageId, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("A DPA diagnostic must not load Circinus.");
        if (!DpaRuntimeAdapter.TryBind(
                packages.Contains(PerformanceTestContract.DpaPackageId, StringComparer.OrdinalIgnoreCase),
                assemblies,
                out binding,
                out var reason))
            throw new InvalidOperationException(reason);
        if (!ReflectionPerformanceHarmonyCatalog.TryBind(assemblies, out var harmony, out reason))
            throw new InvalidOperationException(reason);
        if (!PerformanceTickBoundaryPatch.TryInstall(assemblies, harmony!, out tickBoundary, out reason))
            throw new InvalidOperationException(reason);
        try
        {
            var selection = PerformanceMethodSelectorResolver.Resolve(
                assemblies,
                harmony!,
                new[]
                {
                    new PerformanceSelectionRequest(
                        PerformanceMethodSelectorKind.Method,
                        requested.MethodSelectors[0].Value,
                        requested.MethodSelectors[0].Category)
                });
            if (selection.Unsupported.Count != 0 || selection.Methods.Count != 1 ||
                selection.Methods[0].Method is not MethodInfo exact)
                throw new InvalidOperationException(
                    "The DPA diagnostic selector did not resolve to one supported exact MethodInfo.");
            selector = exact;
            descriptor = requested;
        }
        catch
        {
            tickBoundary!.Dispose();
            tickBoundary = null;
            binding = null;
            throw;
        }
    }

    public void Start(PerformanceTestDescriptor requested)
    {
        RequirePrepared(requested);
        if (run is not null) throw new InvalidOperationException("The DPA diagnostic already started.");
        if (!binding!.TryBeginInternalCallDiagnostic(
                new[] { selector! }, DpaDiagnosticCategory.Tick, out run, out var reason))
            throw new InvalidOperationException(reason);
        if (run is null || !run.TryStart(out reason))
        {
            run?.RequestCleanup();
            throw new InvalidOperationException(reason);
        }
    }

    public void ArmTickBoundary(int gameTick)
    {
        if (tickBoundary is null) throw new InvalidOperationException("The DPA tick boundary is not prepared.");
        tickBoundary.Arm(gameTick);
    }

    public void ConfirmTickBoundary(int gameTick)
    {
        if (tickBoundary is null) throw new InvalidOperationException("The DPA tick boundary is not prepared.");
        tickBoundary.Confirm(gameTick);
    }

    public void DisarmTickBoundary() => tickBoundary?.Disarm();

    public IReadOnlyDictionary<string, string> Complete(
        PerformanceTestDescriptor requested,
        GatewayPerformanceControlWindow controlWindow,
        IReadOnlyDictionary<string, long> throughputCounts)
    {
        RequirePrepared(requested);
        var active = run ?? throw new InvalidOperationException("The DPA diagnostic has not started.");
        var checkpoints = new List<string>
        {
            "sample-start-tick=" + controlWindow.StartGameTick,
            "sample-end-tick=" + controlWindow.EndGameTick
        };
        checkpoints.AddRange(throughputCounts.OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => item.Key + "=" + item.Value));
        if (!active.TryStopAndCapture(
                requested.WorkloadVersion,
                new[] { requested.MethodSelectors[0].Value },
                checkpoints,
                out var capture,
                out var reason))
            throw new InvalidOperationException(reason);
        var artifacts = DpaDiagnosticArtifactWriter.Write(artifactDirectory, requested.Id, capture!);
        active.RequestCleanup();
        return artifacts;
    }

    public bool TryFinalize(out string reason)
    {
        if (run is null)
        {
            reason = string.Empty;
            return true;
        }
        if (!run.TryConfirmCleanup(out reason)) return false;
        run = null;
        return true;
    }

    public void Cleanup()
    {
        if (!TryCleanup(out var reason)) throw new InvalidOperationException(reason);
    }

    public bool TryCleanup(out string reason)
    {
        if (run is not null)
        {
            try
            {
                run.RequestCleanup();
                if (!run.TryConfirmCleanup(out reason)) return false;
                run = null;
            }
            catch (Exception exception)
            {
                reason = "DPA diagnostic cleanup could not be confirmed: " +
                         CircinusRuntimeAdapter.Describe(exception);
                return false;
            }
        }
        try
        {
            GatewayCircinusPerformanceBackend.DisposeOwned(ref tickBoundary);
        }
        catch (Exception exception)
        {
            reason = "DPA diagnostic tick-boundary cleanup could not be confirmed: " +
                     CircinusRuntimeAdapter.Describe(exception);
            return false;
        }
        selector = null;
        binding = null;
        descriptor = null;
        reason = string.Empty;
        return true;
    }

    private void RequirePrepared(PerformanceTestDescriptor requested)
    {
        if (descriptor is null || !ReferenceEquals(descriptor, requested))
            throw new InvalidOperationException("The DPA backend was not prepared for this exact descriptor.");
    }
}

internal static class DpaDiagnosticArtifactWriter
{
    public static IReadOnlyDictionary<string, string> Write(
        string artifactRoot,
        string benchmarkId,
        DpaDiagnosticCapture capture)
    {
        if (capture.BaselineEligible)
            throw new InvalidOperationException("A DPA diagnostic must never be baseline eligible.");
        var root = Path.GetFullPath(artifactRoot);
        Directory.CreateDirectory(root);
        if (capture is null) throw new ArgumentNullException(nameof(capture));
        var destination = Path.Combine(root, "dpa-" + PerformanceArtifactWriter.HashArtifactIdentity(benchmarkId)
            .Substring(0, 20));
        if (Directory.Exists(destination))
            throw new IOException("The exact DPA diagnostic artifact directory already exists.");
        var temporary = Path.Combine(root, ".tmp-dpa-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(temporary);
            var path = Path.Combine(temporary, "dpa.raw.json");
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                new DataContractJsonSerializer(typeof(DpaDiagnosticCapture)).WriteObject(stream, capture);
            Directory.Move(temporary, destination);
        }
        catch
        {
            if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true);
            throw;
        }
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["performance.diagnostic.profiler"] = "dpa",
            ["performance.diagnostic.baseline_eligible"] = "false",
            ["performance.diagnostic.raw"] = Path.Combine(destination, "dpa.raw.json")
        };
    }
}
