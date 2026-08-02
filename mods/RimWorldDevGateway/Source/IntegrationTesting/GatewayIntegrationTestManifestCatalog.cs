using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway;

public sealed class GatewayIntegrationTestManifestCandidate
{
    public GatewayIntegrationTestManifestCandidate(string containingPackageId, string manifestPath)
    {
        ContainingPackageId = string.IsNullOrWhiteSpace(containingPackageId)
            ? throw new ArgumentException("A containing package ID is required.", nameof(containingPackageId))
            : containingPackageId;
        ManifestPath = string.IsNullOrWhiteSpace(manifestPath)
            ? throw new ArgumentException("A manifest path is required.", nameof(manifestPath))
            : manifestPath;
    }

    public string ContainingPackageId { get; }

    public string ManifestPath { get; }

    internal Exception? DiscoveryFailure { get; private set; }

    internal static GatewayIntegrationTestManifestCandidate Failed(
        string containingPackageId,
        string identityPath,
        Exception failure)
    {
        var candidate = new GatewayIntegrationTestManifestCandidate(containingPackageId, identityPath);
        candidate.DiscoveryFailure = failure ?? throw new ArgumentNullException(nameof(failure));
        return candidate;
    }
}

public sealed class GatewayIntegrationTestManifestDiscovery
{
    private const int AbsoluteMaximumActivePackages = 1024;
    private const int AbsoluteMaximumManifestCandidates = 1024;

    public GatewayIntegrationTestManifestDiscovery(
        IEnumerable<string> activePackageIds,
        IEnumerable<GatewayIntegrationTestManifestCandidate> manifests)
    {
        if (activePackageIds is null)
        {
            throw new ArgumentNullException(nameof(activePackageIds));
        }

        if (manifests is null)
        {
            throw new ArgumentNullException(nameof(manifests));
        }

        ActivePackageIds = new ReadOnlyCollection<string>(
            MaterializeBounded(activePackageIds, AbsoluteMaximumActivePackages, nameof(activePackageIds)));
        Manifests = new ReadOnlyCollection<GatewayIntegrationTestManifestCandidate>(
            MaterializeBounded(manifests, AbsoluteMaximumManifestCandidates, nameof(manifests)));
    }

    public IReadOnlyList<string> ActivePackageIds { get; }

    public IReadOnlyList<GatewayIntegrationTestManifestCandidate> Manifests { get; }

    private static T[] MaterializeBounded<T>(IEnumerable<T> values, int maximum, string parameterName)
    {
        var items = values.Take(maximum + 1).ToArray();
        if (items.Length > maximum)
        {
            throw new ArgumentException(
                "The integration-test discovery input exceeds its absolute item limit.",
                parameterName);
        }

        return items;
    }
}

public interface IGatewayIntegrationTestManifestSource
{
    IGatewayIntegrationTestManifestDiscoveryCursor BeginDiscovery();
}

public interface IGatewayIntegrationTestManifestDiscoveryCursor : IDisposable
{
    GatewayIntegrationTestManifestDiscoveryStep Advance();
}

public sealed class GatewayIntegrationTestManifestDiscoveryStep
{
    private GatewayIntegrationTestManifestDiscoveryStep(
        bool isComplete,
        string? activePackageId,
        GatewayIntegrationTestManifestCandidate? manifest)
    {
        IsComplete = isComplete;
        ActivePackageId = activePackageId;
        Manifest = manifest;
    }

    public bool IsComplete { get; }

    public string? ActivePackageId { get; }

    public GatewayIntegrationTestManifestCandidate? Manifest { get; }

    public static GatewayIntegrationTestManifestDiscoveryStep Progress() => new(false, null, null);

    public static GatewayIntegrationTestManifestDiscoveryStep ActivePackage(string packageId) =>
        new(false, packageId ?? throw new ArgumentNullException(nameof(packageId)), null);

    public static GatewayIntegrationTestManifestDiscoveryStep Candidate(
        GatewayIntegrationTestManifestCandidate candidate) =>
        new(false, null, candidate ?? throw new ArgumentNullException(nameof(candidate)));

    public static GatewayIntegrationTestManifestDiscoveryStep Complete() => new(true, null, null);
}

public sealed class GatewayIntegrationTestManifestDiscoveryCursor :
    IGatewayIntegrationTestManifestDiscoveryCursor
{
    private readonly IReadOnlyList<string> activePackageIds;
    private readonly IReadOnlyList<GatewayIntegrationTestManifestCandidate> manifests;
    private int activePackageIndex;
    private int manifestIndex;
    private bool disposed;

    public GatewayIntegrationTestManifestDiscoveryCursor(GatewayIntegrationTestManifestDiscovery discovery)
    {
        if (discovery is null)
        {
            throw new ArgumentNullException(nameof(discovery));
        }

        activePackageIds = discovery.ActivePackageIds.ToArray();
        manifests = discovery.Manifests
            .Where(manifest => manifest is not null)
            .OrderBy(manifest => manifest.ContainingPackageId, StringComparer.Ordinal)
            .ThenBy(manifest => manifest.ManifestPath, StringComparer.Ordinal)
            .ToArray();
    }

    public GatewayIntegrationTestManifestDiscoveryStep Advance()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(GatewayIntegrationTestManifestDiscoveryCursor));
        }

        if (activePackageIndex < activePackageIds.Count)
        {
            return GatewayIntegrationTestManifestDiscoveryStep.ActivePackage(
                activePackageIds[activePackageIndex++]);
        }

        if (manifestIndex < manifests.Count)
        {
            return GatewayIntegrationTestManifestDiscoveryStep.Candidate(manifests[manifestIndex++]);
        }

        return GatewayIntegrationTestManifestDiscoveryStep.Complete();
    }

    public void Dispose()
    {
        disposed = true;
    }
}

public sealed class GatewayIntegrationTestManifestException : Exception
{
    public GatewayIntegrationTestManifestException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public GatewayIntegrationTestManifestException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}

public sealed class GatewayIntegrationTestManifestCatalog : IGatewayIntegrationTestAssemblyCatalog, IDisposable
{
    private const string ManifestSuffix = ".integrationtests.json";
    private const string TestAssemblySuffix = ".IntegrationTests.dll";
    private const int MaximumPackageMatrixEntries = 64;
    private readonly IGatewayIntegrationTestManifestSource source;
    private readonly Dictionary<GatewayIntegrationTestAssemblySource, LoadPlan> plans = new();
    private readonly List<GatewayIntegrationTestAssemblySource> orderedSources = new();
    private readonly int maximumManifests;
    private readonly int maximumManifestBytes;
    private readonly GatewayIntegrationTestAssemblyByteLoader assemblyLoader;
    private bool discoveryStarted;
    private bool discoveryCompleted;
    private CatalogDiscoveryCursor? activeCursor;
    private bool disposed;

    public GatewayIntegrationTestManifestCatalog(
        IGatewayIntegrationTestManifestSource source,
        int maximumManifests = 128,
        int maximumManifestBytes = 64 * 1024,
        int maximumAssemblyBytes = 32 * 1024 * 1024)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        this.maximumManifests = maximumManifests > 1
            ? maximumManifests
            : throw new ArgumentOutOfRangeException(nameof(maximumManifests));
        this.maximumManifestBytes = maximumManifestBytes > 0
            ? maximumManifestBytes
            : throw new ArgumentOutOfRangeException(nameof(maximumManifestBytes));
        assemblyLoader = new GatewayIntegrationTestAssemblyByteLoader(maximumAssemblyBytes);
    }

    public IReadOnlyList<GatewayIntegrationTestAssemblySource> DiscoverActiveModAssemblies()
    {
        ThrowIfDisposed();
        if (discoveryCompleted)
        {
            return new ReadOnlyCollection<GatewayIntegrationTestAssemblySource>(orderedSources.ToArray());
        }

        using var cursor = BeginDiscovery();
        while (true)
        {
            var step = cursor.Advance();
            if (step.IsComplete)
            {
                break;
            }
        }

        return new ReadOnlyCollection<GatewayIntegrationTestAssemblySource>(orderedSources.ToArray());
    }

    public IGatewayIntegrationTestAssemblyDiscoveryCursor BeginDiscovery()
    {
        ThrowIfDisposed();
        if (discoveryCompleted)
        {
            return new GatewayIntegrationTestAssemblyDiscoveryCursor(orderedSources);
        }

        if (discoveryStarted)
        {
            throw new InvalidOperationException("Integration-test manifest discovery is already in progress.");
        }

        discoveryStarted = true;
        var sourceCursor = source.BeginDiscovery() ??
            throw new InvalidOperationException("The integration-test manifest source returned a null discovery cursor.");
        activeCursor = new CatalogDiscoveryCursor(this, sourceCursor);
        return activeCursor;
    }

    public Assembly Load(GatewayIntegrationTestAssemblySource source)
    {
        ThrowIfDisposed();
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (!plans.TryGetValue(source, out var plan))
        {
            throw new GatewayIntegrationTestManifestException(
                "unknown_manifest_source",
                "The integration-test assembly source did not come from this catalog discovery.");
        }

        if (plan.Failure is not null)
        {
            throw plan.Failure;
        }

        try
        {
            return assemblyLoader.Load(plan.AssemblyPath!);
        }
        catch (Exception exception)
        {
            throw new GatewayIntegrationTestManifestException(
                "assembly_byte_load_failed",
                "The staged integration-test assembly could not be loaded from bytes.",
                exception);
        }
    }

    public void Dispose()
    {
        Exception? cursorFailure = null;
        try
        {
            activeCursor?.Dispose();
        }
        catch (Exception exception)
        {
            cursorFailure = exception;
        }
        finally
        {
            activeCursor = null;
            assemblyLoader.Dispose();
            disposed = true;
            plans.Clear();
            orderedSources.Clear();
        }

        if (cursorFailure is not null)
        {
            throw new GatewayIntegrationTestManifestException(
                "manifest_cursor_dispose_failed",
                "The integration-test manifest discovery cursor could not be released.",
                cursorFailure);
        }
    }

    private static LoadPlan CreatePlan(
        GatewayIntegrationTestManifestCandidate candidate,
        IReadOnlyList<string> activePackageIds,
        ISet<string> activePackages,
        int maximumManifestBytes)
    {
        try
        {
            if (candidate.DiscoveryFailure is not null)
            {
                throw new GatewayIntegrationTestManifestException(
                    "active_mod_manifest_scan_failed",
                    "An active mod's integration-test manifests could not be scanned.",
                    candidate.DiscoveryFailure);
            }

            var manifestPath = Path.GetFullPath(candidate.ManifestPath);
            if (!manifestPath.EndsWith(ManifestSuffix, StringComparison.Ordinal))
            {
                throw new GatewayIntegrationTestManifestException(
                    "invalid_manifest_name",
                    "An integration-test manifest must end with '" + ManifestSuffix + "'.");
            }

            var manifest = ReadManifestDocument(
                ReadBoundedUtf8(manifestPath, maximumManifestBytes));
            if (!string.Equals(
                    manifest.OwnerPackageId,
                    candidate.ContainingPackageId,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new GatewayIntegrationTestManifestException(
                    "manifest_owner_mismatch",
                    "The integration-test manifest owner does not match its containing active mod.");
            }

            if (!activePackages.Contains(candidate.ContainingPackageId))
            {
                throw new GatewayIntegrationTestManifestException(
                    "manifest_owner_inactive",
                    "The integration-test manifest belongs to a mod that is not active.");
            }

            ValidatePackageList(manifest.RequiredPackageIds, "requiredPackageIds");
            ValidatePackageList(manifest.ForbiddenPackageIds, "forbiddenPackageIds");
            ValidatePackageList(manifest.ActivePackageIds, "activePackageIds");
            var requiredPackages = new HashSet<string>(
                manifest.RequiredPackageIds ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            if ((manifest.ForbiddenPackageIds ?? Array.Empty<string>()).Any(requiredPackages.Contains))
            {
                throw new GatewayIntegrationTestManifestException(
                    "contradictory_package_matrix",
                    "An integration-test package ID cannot be both required and forbidden.");
            }

            var hasExactMode = manifest.ActivePackageSetMode is not null;
            var hasExactSet = manifest.ActivePackageIds is not null;
            if (hasExactMode != hasExactSet)
            {
                throw new GatewayIntegrationTestManifestException(
                    "invalid_active_package_set",
                    "Integration-test manifest activePackageSetMode and activePackageIds must be declared together.");
            }

            if (hasExactMode)
            {
                if (!string.Equals(manifest.ActivePackageSetMode, "exact", StringComparison.Ordinal) ||
                    manifest.ActivePackageIds!.Length == 0)
                {
                    throw new GatewayIntegrationTestManifestException(
                        "invalid_active_package_set",
                        "An exact integration-test active package set must use mode 'exact' and contain at least one package ID.");
                }

                if ((manifest.RequiredPackageIds?.Length ?? 0) > 0 ||
                    (manifest.ForbiddenPackageIds?.Length ?? 0) > 0)
                {
                    throw new GatewayIntegrationTestManifestException(
                        "contradictory_package_matrix",
                        "An exact integration-test active package set cannot also declare required or forbidden package IDs.");
                }

                if (!manifest.ActivePackageIds.SequenceEqual(
                        activePackageIds,
                        StringComparer.OrdinalIgnoreCase))
                {
                    return LoadPlan.Skipped;
                }
            }
            else if ((manifest.RequiredPackageIds ?? Array.Empty<string>())
                         .Any(packageId => !activePackages.Contains(packageId)) ||
                     (manifest.ForbiddenPackageIds ?? Array.Empty<string>())
                         .Any(activePackages.Contains))
            {
                return LoadPlan.Skipped;
            }

            var manifestBaseName = Path.GetFileName(manifestPath)
                .Substring(0, Path.GetFileName(manifestPath).Length - ManifestSuffix.Length);
            var expectedAssemblyName = manifestBaseName + ".dll";
            if (!expectedAssemblyName.EndsWith(TestAssemblySuffix, StringComparison.Ordinal) ||
                !string.Equals(manifest.Assembly, expectedAssemblyName, StringComparison.Ordinal) ||
                !string.Equals(Path.GetFileName(manifest.Assembly), manifest.Assembly, StringComparison.Ordinal))
            {
                throw new GatewayIntegrationTestManifestException(
                    "invalid_manifest_assembly",
                    "The integration-test manifest must name its exact sibling '*.IntegrationTests.dll'.");
            }

            var directory = Path.GetDirectoryName(manifestPath) ??
                throw new GatewayIntegrationTestManifestException(
                    "invalid_manifest_path",
                    "The integration-test manifest has no containing directory.");
            var assemblyPath = Path.GetFullPath(Path.Combine(directory, manifest.Assembly));
            if (!string.Equals(Path.GetDirectoryName(assemblyPath), directory, StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(assemblyPath))
            {
                throw new GatewayIntegrationTestManifestException(
                    "missing_manifest_assembly",
                    "The exact sibling integration-test assembly does not exist.");
            }

            return new LoadPlan(assemblyPath, null);
        }
        catch (GatewayIntegrationTestManifestException exception)
        {
            return new LoadPlan(null, exception);
        }
        catch (Exception exception)
        {
            return new LoadPlan(
                null,
                new GatewayIntegrationTestManifestException(
                    "invalid_integration_test_manifest",
                    "The integration-test manifest could not be validated.",
                    exception));
        }
    }

    private static string ReadBoundedUtf8(string path, int maximumBytes)
    {
        var info = new FileInfo(path);
        if (!info.Exists ||
            (info.Attributes & FileAttributes.ReparsePoint) != 0 ||
            info.Length <= 0)
        {
            throw new GatewayIntegrationTestManifestException(
                "manifest_size_invalid",
                "The integration-test manifest is missing, linked, empty, or exceeds its byte limit.");
        }

        byte[] bytes;
        using (var stream = new FileStream(
                   path,
                   FileMode.Open,
                   FileAccess.Read,
                   FileShare.Read,
                   bufferSize: 16 * 1024,
                   FileOptions.SequentialScan))
        {
            if (stream.Length <= 0 || stream.Length > maximumBytes || stream.Length > int.MaxValue)
            {
                throw new GatewayIntegrationTestManifestException(
                    "manifest_size_invalid",
                    "The integration-test manifest exceeds its byte limit.");
            }

            bytes = new byte[(int)stream.Length];
            var offset = 0;
            while (offset < bytes.Length)
            {
                var read = stream.Read(bytes, offset, bytes.Length - offset);
                if (read == 0)
                {
                    throw new GatewayIntegrationTestManifestException(
                        "manifest_changed_while_reading",
                        "The integration-test manifest changed while it was read.");
                }

                offset += read;
            }

            if (stream.ReadByte() != -1)
            {
                throw new GatewayIntegrationTestManifestException(
                    "manifest_size_invalid",
                    "The integration-test manifest grew beyond its byte limit while it was read.");
            }
        }

        try
        {
            return new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new GatewayIntegrationTestManifestException(
                "manifest_utf8_invalid",
                "The integration-test manifest is not strict UTF-8.",
                exception);
        }
    }

    private static ManifestDocument ReadManifestDocument(string json)
    {
        try
        {
            var allowedProperties = new HashSet<string>(StringComparer.Ordinal)
            {
                "ownerPackageId",
                "assembly",
                "requiredPackageIds",
                "forbiddenPackageIds",
                "activePackageSetMode",
                "activePackageIds"
            };
            var encounteredProperties = new HashSet<string>(StringComparer.Ordinal);
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(
                       Encoding.UTF8.GetBytes(json),
                       XmlDictionaryReaderQuotas.Max))
            {
                var rootSeen = false;
                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element)
                    {
                        continue;
                    }

                    if (reader.Depth == 0)
                    {
                        if (rootSeen ||
                            !string.Equals(reader.LocalName, "root", StringComparison.Ordinal) ||
                            !string.Equals(reader.GetAttribute("type"), "object", StringComparison.Ordinal))
                        {
                            throw new GatewayIntegrationTestManifestException(
                                "invalid_manifest_schema",
                                "An integration-test manifest must contain exactly one top-level JSON object.");
                        }

                        rootSeen = true;
                        continue;
                    }

                    if (reader.Depth != 1)
                    {
                        continue;
                    }

                    var propertyName = reader.LocalName;
                    if (!allowedProperties.Contains(propertyName))
                    {
                        throw new GatewayIntegrationTestManifestException(
                            "invalid_manifest_schema",
                            "The integration-test manifest contains an unknown top-level property.");
                    }

                    if (!encounteredProperties.Add(propertyName))
                    {
                        throw new GatewayIntegrationTestManifestException(
                            "invalid_manifest_schema",
                            "The integration-test manifest contains a duplicate top-level property.");
                    }
                }

                if (!rootSeen)
                {
                    throw new GatewayIntegrationTestManifestException(
                        "invalid_manifest_schema",
                        "An integration-test manifest must contain one top-level JSON object.");
                }
            }

            return GatewayContractJson.Read<ManifestDocument>(json);
        }
        catch (GatewayIntegrationTestManifestException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new GatewayIntegrationTestManifestException(
                "invalid_manifest_schema",
                "The integration-test manifest JSON schema is invalid.",
                exception);
        }
    }

    private static void ValidatePackageList(IReadOnlyList<string>? packageIds, string propertyName)
    {
        if (packageIds is null)
        {
            return;
        }

        if (packageIds.Count > MaximumPackageMatrixEntries)
        {
            throw new GatewayIntegrationTestManifestException(
                "package_matrix_limit_reached",
                "Integration-test manifest property '" + propertyName + "' exceeds its entry limit.");
        }

        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var packageId in packageIds)
        {
            if (!IsValidPackageId(packageId) || !unique.Add(packageId))
            {
                throw new GatewayIntegrationTestManifestException(
                    "invalid_package_matrix",
                    "Integration-test manifest property '" + propertyName + "' contains an invalid or duplicate package ID.");
            }
        }
    }

    private static bool IsValidPackageId(string? packageId)
    {
        if (packageId is null || string.IsNullOrWhiteSpace(packageId) || packageId.Length > 256)
        {
            return false;
        }

        return IsAsciiLetterOrDigit(packageId[0]) && packageId.All(character =>
            IsAsciiLetterOrDigit(character) || character == '.' || character == '_' || character == '-');
    }

    private static bool IsAsciiLetterOrDigit(char character) =>
        character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9';

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(GatewayIntegrationTestManifestCatalog));
        }
    }

    private GatewayIntegrationTestAssemblyDiscoveryStep ProcessCandidate(
        GatewayIntegrationTestManifestCandidate candidate,
        IReadOnlyList<string> activePackageIds,
        ISet<string> activePackages)
    {
        var identity = candidate.ContainingPackageId + "/" + Path.GetFileName(candidate.ManifestPath);
        var assemblySource = new GatewayIntegrationTestAssemblySource(
            candidate.ContainingPackageId,
            identity);
        var plan = CreatePlan(candidate, activePackageIds, activePackages, maximumManifestBytes);
        if (plan.Skip)
        {
            return GatewayIntegrationTestAssemblyDiscoveryStep.Progress();
        }

        plans.Add(assemblySource, plan);
        orderedSources.Add(assemblySource);
        return GatewayIntegrationTestAssemblyDiscoveryStep.Found(assemblySource);
    }

    private GatewayIntegrationTestAssemblyDiscoveryStep AddManifestLimitFailure(
        GatewayIntegrationTestManifestCandidate candidate)
    {
        var limitSource = new GatewayIntegrationTestAssemblySource(
            candidate.ContainingPackageId,
            candidate.ContainingPackageId + "/<manifest-limit>");
        plans.Add(
            limitSource,
            new LoadPlan(
                null,
                new GatewayIntegrationTestManifestException(
                    "manifest_limit_reached",
                    "The active integration-test manifest count exceeds the configured limit.")));
        orderedSources.Add(limitSource);
        return GatewayIntegrationTestAssemblyDiscoveryStep.Found(limitSource);
    }

    private sealed class CatalogDiscoveryCursor : IGatewayIntegrationTestAssemblyDiscoveryCursor
    {
        private readonly GatewayIntegrationTestManifestCatalog owner;
        private readonly IGatewayIntegrationTestManifestDiscoveryCursor sourceCursor;
        private readonly List<string> activePackageIds = new();
        private readonly HashSet<string> activePackages = new(StringComparer.OrdinalIgnoreCase);
        private GatewayIntegrationTestManifestCandidate? deferredCandidate;
        private bool sourceCompleted;
        private bool manifestLimitEmitted;
        private bool disposed;
        private int manifestCount;

        public CatalogDiscoveryCursor(
            GatewayIntegrationTestManifestCatalog owner,
            IGatewayIntegrationTestManifestDiscoveryCursor sourceCursor)
        {
            this.owner = owner;
            this.sourceCursor = sourceCursor;
        }

        public GatewayIntegrationTestAssemblyDiscoveryStep Advance()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(CatalogDiscoveryCursor));
            }

            if (sourceCompleted)
            {
                if (deferredCandidate is not null)
                {
                    var candidate = deferredCandidate;
                    deferredCandidate = null;
                    return owner.ProcessCandidate(candidate, activePackageIds, activePackages);
                }

                owner.discoveryCompleted = true;
                owner.activeCursor = null;
                return GatewayIntegrationTestAssemblyDiscoveryStep.Complete();
            }

            var step = sourceCursor.Advance() ??
                throw new InvalidOperationException("The integration-test manifest discovery cursor returned null.");
            if (step.IsComplete)
            {
                sourceCompleted = true;
                return GatewayIntegrationTestAssemblyDiscoveryStep.Progress();
            }

            if (step.ActivePackageId is not null)
            {
                if (activePackages.Count >= 512)
                {
                    throw new GatewayIntegrationTestManifestException(
                        "active_package_limit_reached",
                        "The active package list exceeds the integration-test discovery limit.");
                }

                if (!IsValidPackageId(step.ActivePackageId) || !activePackages.Add(step.ActivePackageId))
                {
                    throw new GatewayIntegrationTestManifestException(
                        "invalid_active_package_list",
                        "The active package list contains an invalid or duplicate package ID.");
                }

                activePackageIds.Add(step.ActivePackageId);

                return GatewayIntegrationTestAssemblyDiscoveryStep.Progress();
            }

            if (step.Manifest is null)
            {
                return GatewayIntegrationTestAssemblyDiscoveryStep.Progress();
            }

            manifestCount++;
            if (manifestLimitEmitted)
            {
                return GatewayIntegrationTestAssemblyDiscoveryStep.Progress();
            }

            if (manifestCount < owner.maximumManifests)
            {
                return owner.ProcessCandidate(step.Manifest, activePackageIds, activePackages);
            }

            if (manifestCount == owner.maximumManifests)
            {
                deferredCandidate = step.Manifest;
                return GatewayIntegrationTestAssemblyDiscoveryStep.Progress();
            }

            var firstExcluded = deferredCandidate ?? step.Manifest;
            deferredCandidate = null;
            manifestLimitEmitted = true;
            return owner.AddManifestLimitFailure(firstExcluded);
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
                sourceCursor.Dispose();
            }
            finally
            {
                if (ReferenceEquals(owner.activeCursor, this))
                {
                    owner.activeCursor = null;
                }
            }
        }
    }

    [DataContract]
    private sealed class ManifestDocument
    {
        [DataMember(Name = "ownerPackageId", IsRequired = true)]
        public string OwnerPackageId { get; set; } = string.Empty;

        [DataMember(Name = "assembly", IsRequired = true)]
        public string Assembly { get; set; } = string.Empty;

        [DataMember(Name = "requiredPackageIds", EmitDefaultValue = false)]
        public string[]? RequiredPackageIds { get; set; }

        [DataMember(Name = "forbiddenPackageIds", EmitDefaultValue = false)]
        public string[]? ForbiddenPackageIds { get; set; }

        [DataMember(Name = "activePackageSetMode", EmitDefaultValue = false)]
        public string? ActivePackageSetMode { get; set; }

        [DataMember(Name = "activePackageIds", EmitDefaultValue = false)]
        public string[]? ActivePackageIds { get; set; }
    }

    private sealed class LoadPlan
    {
        public static readonly LoadPlan Skipped = new(null, null, skip: true);

        public LoadPlan(
            string? assemblyPath,
            GatewayIntegrationTestManifestException? failure,
            bool skip = false)
        {
            AssemblyPath = assemblyPath;
            Failure = failure;
            Skip = skip;
        }

        public string? AssemblyPath { get; }

        public GatewayIntegrationTestManifestException? Failure { get; }

        public bool Skip { get; }
    }
}
