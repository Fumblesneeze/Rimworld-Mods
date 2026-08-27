using System.IO;

namespace RimWorldDevGateway;

internal static class GatewayTemporaryFile
{
    private const int MaximumCreateAttempts = 32;

    internal static FileStream CreateSibling(
        string destinationPath,
        FileOptions options = FileOptions.None,
        Func<string>? leafFactory = null)
    {
        var directory = Path.GetDirectoryName(destinationPath) ??
            throw new InvalidOperationException("The atomic destination has no directory.");
        var createLeaf = leafFactory ?? CreateLeaf;
        for (var attempt = 0; attempt < MaximumCreateAttempts; attempt++)
        {
            Directory.CreateDirectory(directory);
            var leaf = createLeaf();
            if (string.IsNullOrWhiteSpace(leaf) ||
                leaf.Length > 24 ||
                !leaf.EndsWith(".tmp", StringComparison.Ordinal) ||
                !StringComparer.Ordinal.Equals(Path.GetFileName(leaf), leaf))
            {
                throw new InvalidOperationException("The temporary-file leaf is invalid.");
            }

            var path = Path.Combine(directory, leaf);
            try
            {
                return new FileStream(
                    path,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 64 * 1024,
                    options);
            }
            catch (DirectoryNotFoundException)
            {
                // Startup and shutdown cleanup can remove the exact run-owned directory
                // between preparation and CreateNew. Re-establish it on the next bounded try.
            }
            catch (IOException) when (File.Exists(path))
            {
                // A name collision is not ownership. Leave that sibling untouched and retry.
            }
        }

        throw new IOException("Could not reserve a unique temporary sibling within the bounded retry count.");
    }

    private static string CreateLeaf() =>
        "." + Guid.NewGuid().ToString("N").Substring(0, 6) + ".tmp";
}
