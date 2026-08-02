using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Verse;

namespace RimWorldDevGateway;

public sealed class VerseGatewayIntegrationTestManifestSource : IGatewayIntegrationTestManifestSource
{
    private const int MaximumGlobalManifestCandidates = 256;
    private const int MaximumActiveMods = 512;
    private readonly GatewayIntegrationTestResolvedFolderScanner scanner;

    public VerseGatewayIntegrationTestManifestSource()
        : this(new GatewayIntegrationTestResolvedFolderScanner())
    {
    }

    public VerseGatewayIntegrationTestManifestSource(
        GatewayIntegrationTestResolvedFolderScanner scanner)
    {
        this.scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
    }

    public GatewayIntegrationTestManifestDiscovery Capture()
    {
        var activePackageIds = new List<string>();
        var manifests = new List<GatewayIntegrationTestManifestCandidate>();
        using var cursor = BeginDiscovery();
        while (true)
        {
            var step = cursor.Advance();
            if (step.IsComplete)
            {
                break;
            }

            if (step.ActivePackageId is not null)
            {
                activePackageIds.Add(step.ActivePackageId);
            }
            else if (step.Manifest is not null)
            {
                manifests.Add(step.Manifest);
            }
        }

        return new GatewayIntegrationTestManifestDiscovery(activePackageIds, manifests);
    }

    public IGatewayIntegrationTestManifestDiscoveryCursor BeginDiscovery() =>
        new VerseManifestDiscoveryCursor(
            LoadedModManager.RunningModsListForReading.Take(MaximumActiveMods + 1).ToArray(),
            scanner);

    private static void AddGlobalLimitFailure(
        List<GatewayIntegrationTestManifestCandidate> manifests,
        string packageId)
    {
        while (manifests.Count >= MaximumGlobalManifestCandidates)
        {
            manifests.RemoveAt(manifests.Count - 1);
        }

        manifests.Add(GatewayIntegrationTestManifestCandidate.Failed(
            packageId,
            "<global-manifest-limit>.integrationtests.json",
            new InvalidOperationException(
                "The active mod list exceeds the global integration-test manifest limit.")));
    }

    private sealed class ActiveMod
    {
        public ActiveMod(ModContentPack content, string packageId)
        {
            Content = content;
            PackageId = packageId;
        }

        public ModContentPack Content { get; }

        public string PackageId { get; }
    }

    private sealed class VerseManifestDiscoveryCursor : IGatewayIntegrationTestManifestDiscoveryCursor
    {
        private readonly IReadOnlyList<ModContentPack> activeMods;
        private readonly GatewayIntegrationTestResolvedFolderScanner scanner;
        private readonly List<ActiveMod> readableMods = new();
        private readonly List<GatewayIntegrationTestManifestCandidate> pendingManifests = new();
        private int identityIndex;
        private int packageIndex;
        private int scanIndex;
        private int emittedManifestCount;
        private bool identitiesComplete;
        private bool packageIdsComplete;
        private bool stopAfterPending;
        private bool disposed;

        public VerseManifestDiscoveryCursor(
            IReadOnlyList<ModContentPack> activeMods,
            GatewayIntegrationTestResolvedFolderScanner scanner)
        {
            this.activeMods = activeMods;
            this.scanner = scanner;
        }

        public GatewayIntegrationTestManifestDiscoveryStep Advance()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(VerseManifestDiscoveryCursor));
            }

            if (!identitiesComplete)
            {
                if (identityIndex < activeMods.Count)
                {
                    var mod = activeMods[identityIndex++];
                    try
                    {
                        readableMods.Add(new ActiveMod(mod, mod.PackageId));
                    }
                    catch (Exception exception)
                    {
                        EnqueueBoundedFailure(
                            "unknown.active.mod",
                            "<package-id-failed>.integrationtests.json",
                            exception);
                    }

                    return GatewayIntegrationTestManifestDiscoveryStep.Progress();
                }

                identitiesComplete = true;
                return GatewayIntegrationTestManifestDiscoveryStep.Progress();
            }

            if (!packageIdsComplete)
            {
                if (packageIndex < readableMods.Count)
                {
                    return GatewayIntegrationTestManifestDiscoveryStep.ActivePackage(
                        readableMods[packageIndex++].PackageId);
                }

                packageIdsComplete = true;
                return GatewayIntegrationTestManifestDiscoveryStep.Progress();
            }

            if (pendingManifests.Count > 0)
            {
                emittedManifestCount++;
                var manifest = pendingManifests[0];
                pendingManifests.RemoveAt(0);
                return GatewayIntegrationTestManifestDiscoveryStep.Candidate(manifest);
            }

            if (stopAfterPending || scanIndex >= readableMods.Count)
            {
                return GatewayIntegrationTestManifestDiscoveryStep.Complete();
            }

            var activeMod = readableMods[scanIndex++];
            try
            {
                var paths = scanner.Scan(
                    activeMod.Content.RootDir,
                    activeMod.Content.foldersToLoadDescendingOrder);
                var remaining = MaximumGlobalManifestCandidates - 1 - emittedManifestCount;
                if (paths.Count > remaining)
                {
                    EnqueueGlobalLimitFailure(activeMod.PackageId);
                }
                else
                {
                    foreach (var path in paths)
                    {
                        pendingManifests.Add(
                            new GatewayIntegrationTestManifestCandidate(activeMod.PackageId, path));
                    }
                }
            }
            catch (Exception exception)
            {
                EnqueueBoundedFailure(
                    activeMod.PackageId,
                    "<manifest-scan-failed>.integrationtests.json",
                    exception);
            }

            return GatewayIntegrationTestManifestDiscoveryStep.Progress();
        }

        public void Dispose()
        {
            disposed = true;
            pendingManifests.Clear();
        }

        private void EnqueueBoundedFailure(string packageId, string path, Exception exception)
        {
            if (emittedManifestCount + pendingManifests.Count >= MaximumGlobalManifestCandidates - 1)
            {
                EnqueueGlobalLimitFailure(packageId);
                return;
            }

            pendingManifests.Add(
                GatewayIntegrationTestManifestCandidate.Failed(packageId, path, exception));
        }

        private void EnqueueGlobalLimitFailure(string packageId)
        {
            while (emittedManifestCount + pendingManifests.Count >= MaximumGlobalManifestCandidates)
            {
                pendingManifests.RemoveAt(pendingManifests.Count - 1);
            }

            pendingManifests.Add(GatewayIntegrationTestManifestCandidate.Failed(
                packageId,
                "<global-manifest-limit>.integrationtests.json",
                new InvalidOperationException(
                    "The active mod list exceeds the global integration-test manifest limit.")));
            stopAfterPending = true;
        }
    }
}

public sealed class GatewayIntegrationTestResolvedFolderScanner
{
    private const string ContentFolder = "DevIntegrationTests";
    private const string ManifestSuffix = ".integrationtests.json";
    private readonly int maximumResolvedFolders;
    private readonly int maximumDirectoryEntries;
    private readonly int maximumManifests;

    public GatewayIntegrationTestResolvedFolderScanner(
        int maximumResolvedFolders = 32,
        int maximumDirectoryEntries = 256,
        int maximumManifests = 64)
    {
        this.maximumResolvedFolders = maximumResolvedFolders > 0
            ? maximumResolvedFolders
            : throw new ArgumentOutOfRangeException(nameof(maximumResolvedFolders));
        this.maximumDirectoryEntries = maximumDirectoryEntries > 0
            ? maximumDirectoryEntries
            : throw new ArgumentOutOfRangeException(nameof(maximumDirectoryEntries));
        this.maximumManifests = maximumManifests > 0
            ? maximumManifests
            : throw new ArgumentOutOfRangeException(nameof(maximumManifests));
    }

    public IReadOnlyList<string> Scan(
        string modRoot,
        IEnumerable<string> resolvedFoldersDescendingPriority)
    {
        if (string.IsNullOrWhiteSpace(modRoot))
        {
            throw new ArgumentException("A mod root is required.", nameof(modRoot));
        }

        if (resolvedFoldersDescendingPriority is null)
        {
            throw new ArgumentNullException(nameof(resolvedFoldersDescendingPriority));
        }

        var root = Path.GetFullPath(modRoot);
        EnsureDirectoryIsSafe(root, root);
        var folders = resolvedFoldersDescendingPriority.Take(maximumResolvedFolders + 1).ToArray();
        if (folders.Length > maximumResolvedFolders)
        {
            throw new GatewayIntegrationTestManifestException(
                "resolved_folder_limit_reached",
                "The mod exceeds the resolved load-folder scan limit.");
        }

        var uniqueFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var effective = new Dictionary<string, string>(StringComparer.Ordinal);
        var examinedEntries = 0;
        var examinedManifests = 0;
        foreach (var unresolvedFolder in folders)
        {
            var folder = Path.GetFullPath(unresolvedFolder);
            if (!IsWithin(root, folder) || !uniqueFolders.Add(folder))
            {
                throw new GatewayIntegrationTestManifestException(
                    "unsafe_resolved_folder",
                    "A resolved load folder escapes the active mod root or is duplicated.");
            }

            if (!Directory.Exists(folder))
            {
                continue;
            }

            EnsureDirectoryIsSafe(root, folder);
            var testDirectory = Path.GetFullPath(Path.Combine(folder, ContentFolder));
            if (!IsWithin(folder, testDirectory) || !Directory.Exists(testDirectory))
            {
                continue;
            }

            EnsureDirectoryIsSafe(root, testDirectory);
            foreach (var path in Directory.EnumerateFileSystemEntries(
                         testDirectory,
                         "*",
                         SearchOption.TopDirectoryOnly))
            {
                examinedEntries++;
                if (examinedEntries > maximumDirectoryEntries)
                {
                    throw new GatewayIntegrationTestManifestException(
                        "manifest_scan_work_limit_reached",
                        "The mod exceeds the bounded integration-test directory-entry scan limit.");
                }

                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new GatewayIntegrationTestManifestException(
                        "unsafe_manifest_path",
                        "The staged integration-test directory contains a linked entry.");
                }

                if ((attributes & FileAttributes.Directory) != 0)
                {
                    continue;
                }

                var file = new FileInfo(path);
                if (!file.Name.EndsWith(ManifestSuffix, StringComparison.Ordinal))
                {
                    continue;
                }

                examinedManifests++;
                if (examinedManifests > maximumManifests)
                {
                    throw new GatewayIntegrationTestManifestException(
                        "per_mod_manifest_limit_reached",
                        "The mod exceeds the integration-test manifest limit.");
                }

                if (!IsWithin(testDirectory, file.FullName))
                {
                    throw new GatewayIntegrationTestManifestException(
                        "unsafe_manifest_path",
                        "A staged integration-test manifest is linked or escapes its resolved folder.");
                }

                var relativeKey = ContentFolder + Path.DirectorySeparatorChar + file.Name;
                if (!effective.ContainsKey(relativeKey))
                {
                    effective.Add(relativeKey, file.FullName);
                }
            }
        }

        return effective
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => pair.Value)
            .ToArray();
    }

    private static void EnsureDirectoryIsSafe(string root, string directory)
    {
        var current = new DirectoryInfo(directory);
        while (true)
        {
            if ((current.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new GatewayIntegrationTestManifestException(
                    "unsafe_resolved_folder",
                    "A resolved integration-test directory crosses a reparse point.");
            }

            if (string.Equals(current.FullName, root, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            current = current.Parent ??
                throw new GatewayIntegrationTestManifestException(
                    "unsafe_resolved_folder",
                    "A resolved integration-test directory escapes its mod root.");
        }
    }

    private static bool IsWithin(string root, string candidate)
    {
        if (string.Equals(root, candidate, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                     Path.DirectorySeparatorChar;
        return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
