using System.Security.Cryptography;
using System.Text;

namespace RimWorldModding.Mcp;

public sealed record EvidenceReadResult(
    string Path,
    long Bytes,
    string Sha256,
    bool Truncated,
    string Content);

public static class EvidenceReader
{
    public static EvidenceReadResult Read(string repositoryRoot, string path, int maximumBytes)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        var resolved = RepositoryRoot.ContainedPath(root, path);
        var artifacts = Path.Combine(root, "artifacts") + Path.DirectorySeparatorChar;
        if (!resolved.StartsWith(artifacts, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Evidence reads are restricted to repository artifacts.");
        if (!File.Exists(resolved)) throw new ArgumentException($"Evidence file does not exist: {path}");
        if (maximumBytes is < 1 or > 1048576) throw new ArgumentException("maximumBytes must be between 1 and 1048576.");
        var info = new FileInfo(resolved);
        using var stream = File.OpenRead(resolved);
        var hash = Convert.ToHexString(SHA256.HashData(stream));
        stream.Position = 0;
        var count = (int)Math.Min(info.Length, maximumBytes);
        var buffer = new byte[count];
        _ = stream.Read(buffer, 0, count);
        return new EvidenceReadResult(
            Path.GetRelativePath(root, resolved).Replace('\\', '/'),
            info.Length,
            hash,
            info.Length > count,
            Encoding.UTF8.GetString(buffer));
    }
}
