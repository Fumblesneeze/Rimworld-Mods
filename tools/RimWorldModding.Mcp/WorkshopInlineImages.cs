using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RimWorldModding.Mcp;

internal static class WorkshopInlineImages
{
    internal static IReadOnlyList<CandidateFile> FreezeInputs(string repositoryRoot, string manifestPath, string destinationRoot)
    {
        if (!File.Exists(manifestPath)) return [];
        var files = new List<CandidateFile>();
        void Freeze(string source, string relative)
        {
            var destination = ContainedImagePath(repositoryRoot, destinationRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, overwrite: false);
            files.Add(new CandidateFile(Path.GetRelativePath(repositoryRoot, destination).Replace('\\', '/'),
                new FileInfo(destination).Length, ReleaseCandidateBuilder.Hash(destination)));
        }
        Freeze(manifestPath, "description-images.json");
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        foreach (var image in manifest.RootElement.GetProperty("images").EnumerateArray())
        {
            var relative = image.GetProperty("path").GetString()!;
            Freeze(RepositoryRoot.ContainedPath(repositoryRoot, relative), relative);
        }
        return files;
    }

    public static async Task ValidateAsync(string repositoryRoot, string descriptionPath, string manifestPath,
        HttpClient http, CancellationToken cancellationToken, string? localImageRoot = null)
    {
        ReleasePresentationPolicy.ValidateDescription(descriptionPath);
        var description = await File.ReadAllTextAsync(descriptionPath, cancellationToken);
        var tags = Regex.Matches(description, @"\[img\](.*?)\[/img\]", RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Select(match => match.Groups[1].Value).ToArray();
        if (!File.Exists(manifestPath))
        {
            if (tags.Length != 0) throw new InvalidOperationException("Inline images require description-images.json with committed GitHub URLs.");
            return;
        }
        if (new FileInfo(manifestPath).Length > 64 * 1024) throw new InvalidOperationException("Inline image manifest exceeds 64 KiB.");
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath, cancellationToken));
        if (document.RootElement.GetProperty("schema").GetString() != "WorkshopDescriptionImages/v1")
            throw new InvalidOperationException("Unknown inline image manifest schema.");
        var images = document.RootElement.GetProperty("images").EnumerateArray().ToArray();
        if (images.Length > 32 || tags.Length != images.Length)
            throw new InvalidOperationException("Description image tags must match the independent inline inventory exactly.");
        var declared = new HashSet<string>(StringComparer.Ordinal);
        foreach (var image in images)
        {
            var relative = image.GetProperty("path").GetString() ?? "";
            var url = image.GetProperty("url").GetString() ?? "";
            var expectedHash = image.GetProperty("sha256").GetString() ?? "";
            var path = ContainedImagePath(repositoryRoot, localImageRoot ?? repositoryRoot, relative);
            if (Path.IsPathRooted(relative) || relative.Contains('\\') || relative.Split('/').Any(part => part is ".." or "." or "") ||
                !File.Exists(path) || new FileInfo(path).Length is <= 0 or > 8 * 1024 * 1024 ||
                !Regex.IsMatch(expectedHash, "^[A-F0-9]{64}$") || ReleaseCandidateBuilder.Hash(path) != expectedHash)
                throw new InvalidOperationException("Inline image local path or SHA-256 is invalid.");
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "raw.githubusercontent.com" ||
                !uri.IsDefaultPort || uri.UserInfo != "" || uri.Query != "" || uri.Fragment != "")
                throw new InvalidOperationException("Inline image must use an immutable direct GitHub content URL.");
            var match = Regex.Match(uri.AbsolutePath, "^/Fumblesneeze/Rimworld-Mods/(?<revision>[a-f0-9]{40})/(?<path>.+)$");
            if (!match.Success || Uri.UnescapeDataString(match.Groups["path"].Value) != relative ||
                !declared.Add(url) || tags.Count(tag => tag == url) != 1)
                throw new InvalidOperationException("Inline image URL must pin this repository, a commit and its exact declared path.");
            var revision = match.Groups["revision"].Value;
            var committed = await ProcessRunner.RunAsync("git", ["rev-parse", "--verify", revision + ":" + relative],
                repositoryRoot, TimeSpan.FromSeconds(15), cancellationToken);
            var local = await ProcessRunner.RunAsync("git", ["hash-object", "--", path],
                repositoryRoot, TimeSpan.FromSeconds(15), cancellationToken);
            if (committed.ExitCode != 0 || local.ExitCode != 0 || committed.StandardOutput.Trim() != local.StandardOutput.Trim())
                throw new InvalidOperationException("Inline image bytes are not committed at the declared GitHub revision; commit and push them as agent work.");
            var bytes = await WorkshopRemoteBaseline.DownloadBoundedAsync(http, url, 8 * 1024 * 1024,
                "raw.githubusercontent.com", "/Fumblesneeze/Rimworld-Mods/" + revision + "/", cancellationToken);
            if (Convert.ToHexString(SHA256.HashData(bytes)) != expectedHash)
                throw new InvalidOperationException("GitHub mirrored bytes do not match the committed inline image.");
        }
    }

    private static string ContainedImagePath(string repositoryRoot, string imageRoot, string relative)
    {
        var root = RepositoryRoot.ContainedPath(repositoryRoot, imageRoot).TrimEnd(Path.DirectorySeparatorChar);
        var path = RepositoryRoot.ContainedPath(repositoryRoot, Path.Combine(root, relative));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Inline image escapes its retained input root.");
        return path;
    }
}
