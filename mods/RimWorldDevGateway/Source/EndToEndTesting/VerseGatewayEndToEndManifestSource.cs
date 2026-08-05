using System.Collections.Generic;
using System.IO;
using Verse;

namespace RimWorldDevGateway;

internal interface IGatewayEndToEndActiveModSource
{
    int Count { get; }

    GatewayEndToEndActiveMod Read(int index);
}

internal sealed class GatewayEndToEndActiveMod
{
    public GatewayEndToEndActiveMod(
        string packageId,
        string rootDirectory,
        IReadOnlyList<string> resolvedFoldersDescendingPriority)
    {
        PackageId = string.IsNullOrWhiteSpace(packageId)
            ? throw new ArgumentException("An active package ID is required.", nameof(packageId))
            : packageId.Trim().ToLowerInvariant();
        RootDirectory = string.IsNullOrWhiteSpace(rootDirectory)
            ? throw new ArgumentException("An active mod root is required.", nameof(rootDirectory))
            : Path.GetFullPath(rootDirectory);
        ResolvedFoldersDescendingPriority = resolvedFoldersDescendingPriority ??
            throw new ArgumentNullException(nameof(resolvedFoldersDescendingPriority));
    }

    public string PackageId { get; }

    public string RootDirectory { get; }

    public IReadOnlyList<string> ResolvedFoldersDescendingPriority { get; }
}

public sealed class VerseGatewayEndToEndManifestSource : IGatewayEndToEndManifestSource
{
    public IGatewayEndToEndManifestDiscoveryCursor BeginDiscovery() =>
        new GatewayEndToEndResolvedManifestCursor(
            new VerseActiveModSource(LoadedModManager.RunningModsListForReading));

    private sealed class VerseActiveModSource : IGatewayEndToEndActiveModSource
    {
        private readonly IReadOnlyList<ModContentPack> mods;

        public VerseActiveModSource(IReadOnlyList<ModContentPack> mods) =>
            this.mods = mods ?? throw new ArgumentNullException(nameof(mods));

        public int Count => mods.Count;

        public GatewayEndToEndActiveMod Read(int index)
        {
            var mod = mods[index];
            return new GatewayEndToEndActiveMod(
                mod.PackageId,
                mod.RootDir,
                mod.foldersToLoadDescendingOrder);
        }
    }
}

internal sealed class GatewayEndToEndResolvedManifestCursor : IGatewayEndToEndManifestDiscoveryCursor
{
    private const string ContentDirectoryName = "DevEndToEndTests";
    private const string ManifestSuffix = ".e2etests.json";
    private readonly IGatewayEndToEndActiveModSource source;
    private readonly List<GatewayEndToEndActiveMod> readableMods = new();
    private readonly HashSet<string> effectiveManifestNames = new(StringComparer.Ordinal);
    private readonly List<KeyValuePair<string, string>> effectiveManifests = new();
    private readonly int sourceCount;
    private int readIndex;
    private int packageIndex;
    private int scanModIndex;
    private int scanFolderIndex;
    private int sortIndex;
    private int sortInnerIndex;
    private int emitManifestIndex;
    private bool readsComplete;
    private bool packagesComplete;
    private bool disposed;
    private GatewayEndToEndActiveMod? scanMod;
    private string? scanDirectory;
    private IEnumerator<string>? entries;

    public GatewayEndToEndResolvedManifestCursor(IGatewayEndToEndActiveModSource source)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        sourceCount = source.Count;
        if (sourceCount < 0)
        {
            throw new InvalidOperationException("The active mod source returned a negative count.");
        }
    }

    public GatewayEndToEndManifestDiscoveryStep Advance()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(GatewayEndToEndResolvedManifestCursor));
        }

        if (!readsComplete)
        {
            if (readIndex < sourceCount)
            {
                try
                {
                    readableMods.Add(source.Read(readIndex) ??
                        throw new InvalidOperationException("The active mod source returned null."));
                    readIndex++;
                    return GatewayEndToEndManifestDiscoveryStep.Progress();
                }
                catch
                {
                    readIndex++;
                    return GatewayEndToEndManifestDiscoveryStep.Failure(
                        "active_mod_read_failed",
                        "An active mod could not be read for E2E discovery; arbitrary exception text was suppressed.");
                }
            }

            readsComplete = true;
            return GatewayEndToEndManifestDiscoveryStep.Progress();
        }

        if (!packagesComplete)
        {
            if (packageIndex < readableMods.Count)
            {
                return GatewayEndToEndManifestDiscoveryStep.ActivePackage(
                    readableMods[packageIndex++].PackageId);
            }

            packagesComplete = true;
            return GatewayEndToEndManifestDiscoveryStep.Progress();
        }

        if (sortIndex > 0)
        {
            return AdvanceOneManifestSortTransition();
        }

        if (emitManifestIndex > 0)
        {
            if (emitManifestIndex <= effectiveManifests.Count)
            {
                var path = effectiveManifests[emitManifestIndex - 1].Value;
                emitManifestIndex++;
                return GatewayEndToEndManifestDiscoveryStep.Candidate(
                    new GatewayEndToEndManifestCandidate(scanMod!.PackageId, path));
            }

            ResetScannedMod();
            return GatewayEndToEndManifestDiscoveryStep.Progress();
        }

        if (entries is not null)
        {
            return AdvanceOneDirectoryEntry();
        }

        if (scanMod is not null)
        {
            if (scanFolderIndex < scanMod.ResolvedFoldersDescendingPriority.Count)
            {
                return BeginNextResolvedFolder();
            }

            if (effectiveManifests.Count > 1)
            {
                sortIndex = 1;
                sortInnerIndex = 1;
            }
            else if (effectiveManifests.Count == 1)
            {
                emitManifestIndex = 1;
            }
            else
            {
                ResetScannedMod();
            }

            return GatewayEndToEndManifestDiscoveryStep.Progress();
        }

        if (scanModIndex < readableMods.Count)
        {
            scanMod = readableMods[scanModIndex++];
            scanFolderIndex = 0;
            return GatewayEndToEndManifestDiscoveryStep.Progress();
        }

        return GatewayEndToEndManifestDiscoveryStep.Complete();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        DisposeEntries();
        effectiveManifestNames.Clear();
        effectiveManifests.Clear();
    }

    private GatewayEndToEndManifestDiscoveryStep BeginNextResolvedFolder()
    {
        var unresolvedFolder = scanMod!.ResolvedFoldersDescendingPriority[scanFolderIndex++];
        try
        {
            var root = scanMod.RootDirectory;
            var folder = Path.GetFullPath(unresolvedFolder);
            if (!IsWithin(root, folder))
            {
                throw new InvalidOperationException("A resolved folder escapes its active mod root.");
            }

            if (!Directory.Exists(folder))
            {
                return GatewayEndToEndManifestDiscoveryStep.Progress();
            }

            EnsureDirectoryIsSafe(root, folder);
            var directory = Path.GetFullPath(Path.Combine(folder, ContentDirectoryName));
            if (!IsWithin(folder, directory) || !Directory.Exists(directory))
            {
                return GatewayEndToEndManifestDiscoveryStep.Progress();
            }

            EnsureDirectoryIsSafe(root, directory);
            scanDirectory = directory;
            entries = Directory.EnumerateFileSystemEntries(
                    directory,
                    "*",
                    SearchOption.TopDirectoryOnly)
                .GetEnumerator();
            return GatewayEndToEndManifestDiscoveryStep.Progress();
        }
        catch
        {
            DisposeEntries();
            return GatewayEndToEndManifestDiscoveryStep.Failure(
                "resolved_folder_scan_failed",
                "A resolved E2E test folder could not be scanned safely; arbitrary exception text was suppressed.",
                scanMod.PackageId);
        }
    }

    private GatewayEndToEndManifestDiscoveryStep AdvanceOneDirectoryEntry()
    {
        string? path;
        try
        {
            if (!entries!.MoveNext())
            {
                DisposeEntries();
                return GatewayEndToEndManifestDiscoveryStep.Progress();
            }

            path = entries.Current;
            var attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException("A linked E2E stage entry is unsafe.");
            }

            if ((attributes & FileAttributes.Directory) != 0)
            {
                return GatewayEndToEndManifestDiscoveryStep.Progress();
            }

            var fullPath = Path.GetFullPath(path);
            if (!IsWithin(scanDirectory!, fullPath))
            {
                throw new InvalidOperationException("An E2E manifest escapes its stage directory.");
            }

            var fileName = Path.GetFileName(fullPath);
            if (!fileName.EndsWith(ManifestSuffix, StringComparison.Ordinal))
            {
                return GatewayEndToEndManifestDiscoveryStep.Progress();
            }

            if (effectiveManifestNames.Add(fileName))
            {
                effectiveManifests.Add(new KeyValuePair<string, string>(fileName, fullPath));
            }

            return GatewayEndToEndManifestDiscoveryStep.Progress();
        }
        catch
        {
            DisposeEntries();
            return GatewayEndToEndManifestDiscoveryStep.Failure(
                "stage_entry_scan_failed",
                "An E2E stage entry could not be inspected safely; arbitrary exception text was suppressed.",
                scanMod?.PackageId,
                scanDirectory);
        }
    }

    private void DisposeEntries()
    {
        try
        {
            entries?.Dispose();
        }
        catch
        {
            // A hostile iterator cannot contribute arbitrary exception text or stop later mods.
        }

        entries = null;
        scanDirectory = null;
    }

    private GatewayEndToEndManifestDiscoveryStep AdvanceOneManifestSortTransition()
    {
        if (sortIndex >= effectiveManifests.Count)
        {
            sortIndex = 0;
            sortInnerIndex = 0;
            emitManifestIndex = 1;
            return GatewayEndToEndManifestDiscoveryStep.Progress();
        }

        if (sortInnerIndex > 0 && StringComparer.Ordinal.Compare(
                effectiveManifests[sortInnerIndex - 1].Key,
                effectiveManifests[sortInnerIndex].Key) > 0)
        {
            var swap = effectiveManifests[sortInnerIndex - 1];
            effectiveManifests[sortInnerIndex - 1] = effectiveManifests[sortInnerIndex];
            effectiveManifests[sortInnerIndex] = swap;
            sortInnerIndex--;
            return GatewayEndToEndManifestDiscoveryStep.Progress();
        }

        sortIndex++;
        sortInnerIndex = sortIndex;
        return GatewayEndToEndManifestDiscoveryStep.Progress();
    }

    private void ResetScannedMod()
    {
        scanMod = null;
        sortIndex = 0;
        sortInnerIndex = 0;
        emitManifestIndex = 0;
        effectiveManifestNames.Clear();
        effectiveManifests.Clear();
    }

    private static void EnsureDirectoryIsSafe(string root, string directory)
    {
        var current = new DirectoryInfo(directory);
        while (true)
        {
            if ((current.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException("A resolved E2E directory crosses a reparse point.");
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(current.FullName, root))
            {
                return;
            }

            current = current.Parent ??
                throw new InvalidOperationException("A resolved E2E directory escapes its active mod root.");
        }
    }

    private static bool IsWithin(string root, string candidate)
    {
        if (StringComparer.OrdinalIgnoreCase.Equals(root, candidate))
        {
            return true;
        }

        var prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                     Path.DirectorySeparatorChar;
        return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
