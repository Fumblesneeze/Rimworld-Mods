using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RimWorldModding.Mcp;

/// <summary>External Steam onboarding; branch discovery is not a release target approval.</summary>
internal static class SteamDepotAcquisition
{
    // Official 3.4.0 Windows x64 ZIP: 41C9E9F0DF54B3AD02E67A11726756E5C73283BD7C2E1B04ACFA5AE4C2ED3767.
    // ZIP checksum is independently published by microsoft/winget-pkgs.
    internal const string DownloaderSha256 = "6281279EFCE8F1E20DB9532A58E42382F81AFB9E3827A8B965FFCB43FBE4531F";

    public static async Task<AdapterOperationResult> DownloadAsync(string repositoryRoot, string downloaderPath,
        string targetDirectory, string branch, string? depotId, string? manifestId, bool interactiveLogin,
        string? accountName, bool manifestOnly, CancellationToken cancellationToken)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        var target = Path.GetFullPath(targetDirectory);
        var executable = Path.GetFullPath(downloaderPath);
        RequireExternal(root, target);
        RequireExternal(root, executable);
        ValidateOwnedTarget(target);
        if (!Regex.IsMatch(branch, "^[a-zA-Z0-9._-]{1,64}$") ||
            (depotId is null) != (manifestId is null) ||
            depotId is not null && (!uint.TryParse(depotId, out var depot) || depot == 0 ||
                !ulong.TryParse(manifestId, out var manifest) || manifest == 0))
            throw new ArgumentException("Require a safe branch and either no depot selection or one exact nonzero depot/manifest pair.");
        if (!File.Exists(executable) || ReleaseCandidateBuilder.Hash(executable) != DownloaderSha256)
            throw new ArgumentException("Downloader must be the pinned official DepotDownloader 3.4.0 Windows x64 executable.");
        if ((!interactiveLogin || accountName is not null) && (string.IsNullOrWhiteSpace(accountName) || !Regex.IsMatch(accountName, "^[a-zA-Z0-9_]{1,64}$")))
            throw new ArgumentException("Cached authentication requires the Steam account name; use interactiveLogin for QR authentication.");
        if (File.Exists(Path.Combine(target, "Version.txt")) && depotId is null)
            throw new ArgumentException("Branch onboarding requires a fresh directory; existing installations require exact depot/manifest selection.");
        Directory.CreateDirectory(target);
        var marker = Path.Combine(target, ".rimworld-release-cache");
        if (!File.Exists(marker)) DurableFile.WriteAllText(marker, "RimWorldSteamCache/v1\n" + target);
        var sessionRoot = Path.Combine(Path.GetDirectoryName(executable)!, "session");
        RequireExternal(root, sessionRoot);
        Directory.CreateDirectory(sessionRoot);
        // DepotDownloader's account store is per-user IsolatedStorage, independent of its install path.
        var leaseRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fumblesneeze.RimWorldRelease");
        Directory.CreateDirectory(leaseRoot);
        using var lease = new FileStream(Path.Combine(leaseRoot, "acquisition.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        var start = new ProcessStartInfo(executable)
        {
            WorkingDirectory = sessionRoot,
            UseShellExecute = interactiveLogin,
            CreateNoWindow = !interactiveLogin,
            WindowStyle = interactiveLogin ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
            RedirectStandardOutput = !interactiveLogin,
            RedirectStandardError = !interactiveLogin,
            RedirectStandardInput = !interactiveLogin
        };
        foreach (var argument in new[] { "-app", "294100", "-branch", branch, "-os", "windows", "-osarch", "64",
                     "-dir", target, "-validate", "-loginid", "16417001", "-remember-password" })
            start.ArgumentList.Add(argument);
        if (depotId is not null)
            foreach (var argument in new[] { "-depot", depotId, "-manifest", manifestId! }) start.ArgumentList.Add(argument);
        if (manifestOnly) start.ArgumentList.Add("-manifest-only");
        if (interactiveLogin && accountName is null) start.ArgumentList.Add("-qr");
        else
        {
            start.ArgumentList.Add("-username");
            start.ArgumentList.Add(accountName!);
            start.ArgumentList.Add("-no-mobile");
        }
        var began = DateTimeOffset.UtcNow;
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start DepotDownloader.");
        Task<string>? stdout = null, stderr = null;
        if (!interactiveLogin)
        {
            process.StandardInput.Close();
            stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
            stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(45));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            throw;
        }
        // Authentication output is intentionally never written to durable evidence or returned.
        if (stdout is not null) _ = await stdout;
        if (stderr is not null) _ = await stderr;
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"DepotDownloader exited with {process.ExitCode}; inspect the interactive console or retry authentication, not a different manifest.");
        var manifests = Directory.GetFiles(Path.Combine(target, ".DepotDownloader"), "*.manifest")
            .Select(path => new { file = Path.GetFileName(path), sha256 = ReleaseCandidateBuilder.Hash(path) })
            .OrderBy(item => item.file, StringComparer.Ordinal).ToArray();
        if (manifests.Length == 0 || depotId is not null && !manifests.Any(item => item.file == $"{depotId}_{manifestId}.manifest"))
            throw new InvalidOperationException("Downloader did not retain the requested Steam manifest identity.");
        var installation = manifestOnly ? null : GameInstallation.Inspect(target);
        var evidence = new
        {
            schema = "RimWorldSteamAcquisition/v1", source = "DepotDownloader-3.4.0", downloaderSha256 = DownloaderSha256,
            branchDiscoveryOnly = depotId is null, branch, depotId, manifestId, targetDirectory = target,
            processId = process.Id, processStartUtc = began, completedUtc = DateTimeOffset.UtcNow,
            installation, manifests, credentialsRetained = false
        };
        var evidenceRoot = Path.Combine(root, "artifacts", "GameAcquisition", DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssfffZ"));
        Directory.CreateDirectory(evidenceRoot);
        var json = JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true });
        DurableFile.WriteAllText(Path.Combine(evidenceRoot, "acquisition.json"), json);
        return new AdapterOperationResult("game_depot_download", 0, (DateTimeOffset.UtcNow - began).TotalSeconds, json, "", evidenceRoot);
    }

    internal static void ValidateOwnedTarget(string target)
    {
        if (target.Replace('\\', '/').Contains("/steamapps/common/", StringComparison.OrdinalIgnoreCase) ||
            target.Replace('\\', '/').Contains("/steamapps/workshop/", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Downloads must use an owned release cache, never a Steam-managed installation.");
        if (!Directory.Exists(target)) return;
        var entries = Directory.GetFileSystemEntries(target);
        var marker = Path.Combine(target, ".rimworld-release-cache");
        if (entries.Length != 0 && (!File.Exists(marker) || new FileInfo(marker).Length > 4096 ||
            File.ReadAllText(marker) != "RimWorldSteamCache/v1\n" + target))
            throw new ArgumentException("Existing download target is not an owned release cache; select a new empty directory.");
        var pending = new Stack<string>();
        pending.Push(target);
        while (pending.Count > 0)
            foreach (var entry in Directory.GetFileSystemEntries(pending.Pop()))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new ArgumentException("Owned download targets must not contain symlinks or junctions.");
                if ((attributes & FileAttributes.Directory) != 0) pending.Push(entry);
            }
    }

    internal static void RequireExternal(string root, string path)
    {
        if (!Path.IsPathFullyQualified(path) || Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar)
                .StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase) || string.Equals(Path.GetFullPath(path), root, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Steam content and downloader/session files must stay in an external directory outside the repository.");
        var directory = Directory.Exists(path) ? new DirectoryInfo(path) : new DirectoryInfo(Path.GetDirectoryName(path)!);
        while (directory is not null)
        {
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("External cache paths must not traverse symlinks or junctions.");
            directory = directory.Parent;
        }
    }
}
