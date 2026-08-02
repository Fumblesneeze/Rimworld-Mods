using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RimWorldDevGateway;

public sealed class GatewayIntegrationTestAssemblyByteLoader : IDisposable
{
    private const int MaximumApprovedDirectories = 128;
    private const int MaximumResolvedDependencies = 256;
    private readonly object sync = new();
    private readonly int maximumAssemblyBytes;
    private readonly int maximumDependencyBytes;
    private readonly List<string> approvedDirectories = new();
    private readonly Dictionary<Assembly, string> assemblyDirectories = new();
    private readonly Dictionary<string, Assembly> loadedPaths =
        new(StringComparer.OrdinalIgnoreCase);
    [ThreadStatic]
    private static string? currentlyLoadingDirectory;
    private bool disposed;
    private int resolvedDependencyCount;

    public GatewayIntegrationTestAssemblyByteLoader(
        int maximumAssemblyBytes = 32 * 1024 * 1024,
        int maximumDependencyBytes = 16 * 1024 * 1024)
    {
        this.maximumAssemblyBytes = maximumAssemblyBytes > 0
            ? maximumAssemblyBytes
            : throw new ArgumentOutOfRangeException(nameof(maximumAssemblyBytes));
        this.maximumDependencyBytes = maximumDependencyBytes > 0
            ? maximumDependencyBytes
            : throw new ArgumentOutOfRangeException(nameof(maximumDependencyBytes));
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
    }

    public Assembly Load(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            throw new ArgumentException("An integration-test assembly path is required.", nameof(assemblyPath));
        }

        var fullPath = Path.GetFullPath(assemblyPath);
        var directory = Path.GetDirectoryName(fullPath) ??
            throw new InvalidOperationException("The integration-test assembly has no containing directory.");
        lock (sync)
        {
            ThrowIfDisposed();
            ApproveDirectory(directory);
            if (loadedPaths.TryGetValue(fullPath, out var cached))
            {
                return cached;
            }
        }

        var previousLoadingDirectory = currentlyLoadingDirectory;
        Assembly assembly;
        try
        {
            currentlyLoadingDirectory = directory;
            assembly = Assembly.Load(ReadBounded(fullPath, maximumAssemblyBytes));
        }
        finally
        {
            currentlyLoadingDirectory = previousLoadingDirectory;
        }
        lock (sync)
        {
            ThrowIfDisposed();
            loadedPaths[fullPath] = assembly;
            assemblyDirectories[assembly] = directory;
        }

        return assembly;
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            AppDomain.CurrentDomain.AssemblyResolve -= Resolve;
            approvedDirectories.Clear();
            assemblyDirectories.Clear();
            loadedPaths.Clear();
        }
    }

    private Assembly? Resolve(object? sender, ResolveEventArgs args)
    {
        try
        {
            var requested = new AssemblyName(args.Name);
            var simpleName = requested.Name;
            if (string.IsNullOrWhiteSpace(simpleName) ||
                !string.Equals(Path.GetFileName(simpleName), simpleName, StringComparison.Ordinal))
            {
                return null;
            }

            lock (sync)
            {
                if (disposed || resolvedDependencyCount >= MaximumResolvedDependencies)
                {
                    return null;
                }

                var alreadyLoaded = FindMatchingLoadedAssembly(requested);
                if (alreadyLoaded is not null)
                {
                    return alreadyLoaded;
                }

                var directories = ResolutionDirectories(args.RequestingAssembly);
                foreach (var directory in directories)
                {
                    var candidatePath = Path.GetFullPath(Path.Combine(directory, simpleName + ".dll"));
                    if (!string.Equals(
                            Path.GetDirectoryName(candidatePath),
                            directory,
                            StringComparison.OrdinalIgnoreCase) ||
                        !File.Exists(candidatePath))
                    {
                        continue;
                    }

                    if (loadedPaths.TryGetValue(candidatePath, out var cached))
                    {
                        AssemblyName? cachedIdentity = null;
                        try
                        {
                            cachedIdentity = cached.GetName();
                        }
                        catch
                        {
                            // Ignore a corrupted cached identity and continue to the bounded file check.
                        }

                        if (cachedIdentity is not null &&
                            GatewayIntegrationTestAssemblyIdentity.Matches(requested, cachedIdentity))
                        {
                            return cached;
                        }

                        continue;
                    }

                    EnsureBoundedFile(candidatePath, maximumDependencyBytes);
                    var candidateName = AssemblyName.GetAssemblyName(candidatePath);
                    if (!GatewayIntegrationTestAssemblyIdentity.Matches(requested, candidateName))
                    {
                        continue;
                    }

                    resolvedDependencyCount++;
                    var previousLoadingDirectory = currentlyLoadingDirectory;
                    Assembly dependency;
                    try
                    {
                        currentlyLoadingDirectory = directory;
                        dependency = Assembly.Load(ReadBounded(candidatePath, maximumDependencyBytes));
                    }
                    finally
                    {
                        currentlyLoadingDirectory = previousLoadingDirectory;
                    }

                    loadedPaths[candidatePath] = dependency;
                    assemblyDirectories[dependency] = directory;
                    return dependency;
                }
            }
        }
        catch
        {
            // The CLR reports its normal load failure; one broken staged dependency must not escape the resolver.
        }

        return null;
    }

    private static Assembly? FindMatchingLoadedAssembly(AssemblyName requested)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                if (GatewayIntegrationTestAssemblyIdentity.Matches(requested, assembly.GetName()))
                {
                    return assembly;
                }
            }
            catch
            {
                // One unusual dynamic/modded assembly identity must not abort dependency resolution.
            }
        }

        return null;
    }

    private IReadOnlyList<string> ResolutionDirectories(Assembly? requestingAssembly)
    {
        if (requestingAssembly is not null &&
            assemblyDirectories.TryGetValue(requestingAssembly, out var requestingDirectory))
        {
            return new[] { requestingDirectory };
        }

        if (currentlyLoadingDirectory is not null &&
            approvedDirectories.Contains(currentlyLoadingDirectory, StringComparer.OrdinalIgnoreCase))
        {
            return new[] { currentlyLoadingDirectory };
        }

        return Array.Empty<string>();
    }

    private void ApproveDirectory(string directory)
    {
        if (approvedDirectories.Contains(directory, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        if (approvedDirectories.Count == MaximumApprovedDirectories)
        {
            throw new InvalidOperationException("The staged integration-test dependency-directory limit was reached.");
        }

        approvedDirectories.Add(directory);
    }

    private static byte[] ReadBounded(string path, int maximumBytes)
    {
        EnsureBoundedFile(path, maximumBytes);
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.SequentialScan);
        if (stream.Length <= 0 || stream.Length > maximumBytes || stream.Length > int.MaxValue)
        {
            throw new InvalidDataException("The staged assembly is empty or exceeds its byte limit.");
        }

        var bytes = new byte[(int)stream.Length];
        var offset = 0;
        while (offset < bytes.Length)
        {
            var read = stream.Read(bytes, offset, bytes.Length - offset);
            if (read == 0)
            {
                throw new EndOfStreamException("The staged assembly changed while it was read.");
            }

            offset += read;
        }

        if (stream.ReadByte() != -1)
        {
            throw new InvalidDataException("The staged assembly grew beyond its byte limit while it was read.");
        }

        return bytes;
    }

    private static void EnsureBoundedFile(string path, int maximumBytes)
    {
        var info = new FileInfo(path);
        if (!info.Exists ||
            (info.Attributes & FileAttributes.ReparsePoint) != 0 ||
            info.Length <= 0 ||
            info.Length > maximumBytes ||
            info.Length > int.MaxValue)
        {
            throw new InvalidDataException(
                "The staged assembly is missing, linked, empty, or exceeds its byte limit.");
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(GatewayIntegrationTestAssemblyByteLoader));
        }
    }
}

internal static class GatewayIntegrationTestAssemblyIdentity
{
    private static readonly Version ZeroVersion = new(0, 0, 0, 0);

    public static bool Matches(AssemblyName requested, AssemblyName candidate)
    {
        if (requested is null)
        {
            throw new ArgumentNullException(nameof(requested));
        }

        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }

        if (!string.Equals(
                NormalizeName(requested.Name),
                NormalizeName(candidate.Name),
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if ((requested.Version ?? ZeroVersion) != (candidate.Version ?? ZeroVersion))
        {
            return false;
        }

        if (!string.Equals(
                NormalizeCulture(requested.CultureName),
                NormalizeCulture(candidate.CultureName),
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var requestedToken = requested.GetPublicKeyToken() ?? Array.Empty<byte>();
        var candidateToken = candidate.GetPublicKeyToken() ?? Array.Empty<byte>();
        return requestedToken.SequenceEqual(candidateToken);
    }

    private static string NormalizeName(string? name) => (name ?? string.Empty).Trim();

    private static string NormalizeCulture(string? culture) =>
        string.IsNullOrWhiteSpace(culture) ||
        string.Equals(culture, "neutral", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : culture!.Trim();
}
