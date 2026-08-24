namespace RimWorldModding.Mcp;

public static class RepositoryRoot
{
    public static string Resolve(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            throw new ArgumentException("A repository root is required.");
        }

        var root = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!File.Exists(Path.Combine(root, "AGENTS.md")) ||
            !Directory.Exists(Path.Combine(root, "mods")) ||
            !Directory.Exists(Path.Combine(root, "openspec")))
        {
            throw new ArgumentException($"The repository root is missing AGENTS.md, mods, or openspec: {root}");
        }

        return root;
    }

    public static string ContainedPath(string root, string path)
    {
        var resolvedRoot = Resolve(root);
        var resolved = Path.GetFullPath(path, resolvedRoot);
        var prefix = resolvedRoot + Path.DirectorySeparatorChar;
        if (!resolved.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(resolved, resolvedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Path escapes the repository root: {path}");
        }

        return resolved;
    }
}
