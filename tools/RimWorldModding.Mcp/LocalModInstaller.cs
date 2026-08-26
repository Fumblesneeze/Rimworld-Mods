using System.Security.Cryptography;

namespace RimWorldModding.Mcp;

public sealed record InstalledModFile(string Path, long Bytes, string Sha256);

public sealed record LocalModInstallResult(
    string PackageId,
    string Source,
    string Destination,
    string? BackupPath,
    IReadOnlyList<InstalledModFile> Files);

public static class LocalModInstaller
{
    public static LocalModInstallResult Sync(
        string sourcePackage,
        string modsRoot,
        string packageId,
        IReadOnlyList<string> includes)
    {
        var source = Path.GetFullPath(sourcePackage).TrimEnd(Path.DirectorySeparatorChar);
        var root = Path.GetFullPath(modsRoot).TrimEnd(Path.DirectorySeparatorChar);
        if (!Directory.Exists(source)) throw new ArgumentException($"Local mod package source does not exist: {source}");
        if (!Directory.Exists(root)) throw new ArgumentException($"RimWorld Mods root does not exist: {root}");
        if (string.IsNullOrWhiteSpace(packageId) || packageId.Any(char.IsWhiteSpace))
            throw new ArgumentException("A canonical package ID is required.");
        if (includes.Count == 0) throw new ArgumentException("At least one package include is required.");

        var destination = Path.GetFullPath(Path.Combine(root, packageId.ToLowerInvariant()));
        AssertChild(root, destination, "destination");
        // Stage beside the install, rather than under Mods or the process temp directory. The
        // final move must stay on the same volume as the destination (Steam libraries commonly
        // live on a different drive than the repository/temp directory). A normal build/deploy
        // replaces the installed package in place and does not retain an install backup.
        if (Directory.Exists(destination) &&
            (File.GetAttributes(destination) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("The existing installed package is a reparse point; refusing to replace it.");
        if (File.Exists(destination))
            throw new InvalidOperationException("The installed package path is a file; refusing to replace it.");
        var rimWorldRoot = Directory.GetParent(root)?.FullName ??
                           throw new ArgumentException("RimWorld Mods root has no parent directory.");
        var stagingContainer = Path.Combine(rimWorldRoot, ".rimworld-modding-mcp", "staging");
        var stagingContainerExisted = Directory.Exists(stagingContainer);
        var recoveryContainer = Directory.GetParent(stagingContainer)?.FullName ?? stagingContainer;
        var recoveryContainerExisted = Directory.Exists(recoveryContainer);
        Directory.CreateDirectory(stagingContainer);
        var staging = Path.Combine(stagingContainer, $"stage-{packageId}-{Guid.NewGuid():N}");
        if (!staging.StartsWith(Path.GetFullPath(stagingContainer).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Staging path escaped its temporary root.");
        Directory.CreateDirectory(staging);

        try
        {
            var files = ResolveFiles(source, includes);
            foreach (var file in files)
            {
                var relative = Path.GetRelativePath(source, file);
                var target = Path.GetFullPath(Path.Combine(staging, relative));
                AssertChild(staging, target, "staged file");
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: false);
            }

            if (Directory.Exists(destination))
                Directory.Delete(destination, recursive: true);

            Directory.Move(staging, destination);

            try
            {
                var manifest = Directory.GetFiles(destination, "*", SearchOption.AllDirectories)
                    .OrderBy(path => Path.GetRelativePath(destination, path), StringComparer.Ordinal)
                    .Select(path => new InstalledModFile(
                        Path.GetRelativePath(destination, path).Replace('\\', '/'),
                        new FileInfo(path).Length,
                        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))))
                    .ToArray();
                if (manifest.Length == 0) throw new InvalidOperationException("Installed package produced an empty manifest.");
                return new LocalModInstallResult(packageId.ToLowerInvariant(), source, destination, null, manifest);
            }
            catch
            {
                if (Directory.Exists(destination)) Directory.Delete(destination, recursive: true);
                throw;
            }
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
            TryDeleteEmptyDirectory(stagingContainer, stagingContainerExisted);
            TryDeleteEmptyDirectory(recoveryContainer, recoveryContainerExisted);
        }
    }

    private static void TryDeleteEmptyDirectory(string path, bool existedBefore)
    {
        if (existedBefore || !Directory.Exists(path)) return;
        try
        {
            if (!Directory.EnumerateFileSystemEntries(path).Any()) Directory.Delete(path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    internal static string RecoveryRoot(string modsRoot)
    {
        var root = Path.GetFullPath(modsRoot).TrimEnd(Path.DirectorySeparatorChar);
        var rimWorldRoot = Directory.GetParent(root)?.FullName ??
                           throw new ArgumentException("RimWorld Mods root has no parent directory.");
        var recovery = Path.GetFullPath(Path.Combine(rimWorldRoot, ".rimworld-modding-mcp", "Mods"));
        AssertChild(rimWorldRoot, recovery, "recovery root");
        if (recovery.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Recovery storage must remain outside RimWorld's scanned Mods directory.");
        return recovery;
    }

    private static IReadOnlyList<string> ResolveFiles(string source, IReadOnlyList<string> includes)
    {
        var files = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var include in includes)
        {
            if (Path.IsPathRooted(include) || include.Split('/', '\\').Any(part => part == ".."))
                throw new ArgumentException($"Package include escapes the source: {include}");
            var candidate = Path.GetFullPath(Path.Combine(source, include.Replace('/', Path.DirectorySeparatorChar)));
            AssertChild(source, candidate, "package include");
            foreach (var segment in ReleaseCandidateBuilder.PathsToInspect(source, candidate))
                RejectReparsePoint(segment, include);
            if (File.Exists(candidate))
            {
                files.Add(candidate);
                continue;
            }

            if (Directory.Exists(candidate))
            {
                foreach (var file in EnumerateFilesWithoutReparsePoints(candidate, include)) files.Add(file);
                continue;
            }

            throw new ArgumentException($"Declared package include does not exist: {include}");
        }

        if (files.Count == 0) throw new ArgumentException("Package includes resolved to zero files.");
        return files.ToArray();
    }

    private static IEnumerable<string> EnumerateFilesWithoutReparsePoints(string root, string include)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            RejectReparsePoint(directory, include);
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                RejectReparsePoint(file, include);
                yield return file;
            }
            foreach (var child in Directory.EnumerateDirectories(directory))
            {
                RejectReparsePoint(child, include);
                pending.Push(child);
            }
        }
    }

    private static void RejectReparsePoint(string path, string include)
    {
        if ((File.Exists(path) || Directory.Exists(path)) &&
            (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new ArgumentException($"Package include crosses a reparse point: {include}");
    }

    private static void AssertChild(string root, string path, string label)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(path).StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Resolved {label} escapes its intended root: {path}");
    }
}
