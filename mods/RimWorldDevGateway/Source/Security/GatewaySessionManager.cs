using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway;

public sealed class GatewaySessionLease : IDisposable
{
    private readonly GatewaySessionManager owner;

    internal GatewaySessionLease(
        GatewaySessionManager owner,
        string token,
        string runId,
        int processId,
        DateTimeOffset processStartUtc,
        DateTimeOffset preparedUtc,
        string gameVersion,
        string modVersion)
    {
        this.owner = owner;
        Token = token;
        RunId = runId;
        ProcessId = processId;
        ProcessStartUtc = processStartUtc;
        PreparedUtc = preparedUtc;
        GameVersion = gameVersion;
        ModVersion = modVersion;
    }

    public string Token { get; }

    public string RunId { get; }

    public bool IsPublished { get; internal set; }

    public bool IsStopped { get; internal set; }

    internal int ProcessId { get; }

    internal DateTimeOffset ProcessStartUtc { get; }

    internal DateTimeOffset PreparedUtc { get; }

    internal string GameVersion { get; }

    internal string ModVersion { get; }

    internal GatewaySessionManifest? ActiveManifest { get; set; }

    public GatewaySessionManifest Publish(int port)
    {
        return owner.Publish(this, port);
    }

    public void Stop()
    {
        owner.Stop(this);
    }

    public void Dispose()
    {
        Stop();
    }
}

internal sealed class GatewayTransientSessionCleanupException : IOException
{
    internal GatewayTransientSessionCleanupException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class GatewaySessionManager
{
    private const string ApiVersion = "1";
    private readonly object sync = new();
    private readonly string saveDataRoot;
    private readonly Func<byte[]> tokenFactory;
    private readonly Func<DateTimeOffset> utcNow;
    private readonly Func<string> runIdFactory;
    private readonly Func<int, DateTimeOffset?> processStartLookup;
    private GatewaySessionLease? currentLease;
    private FileStream? sessionClaim;

    public GatewaySessionManager(
        string saveDataRoot,
        Func<byte[]>? tokenFactory = null,
        Func<DateTimeOffset>? utcNow = null,
        Func<string>? runIdFactory = null,
        Func<int, DateTimeOffset?>? processStartLookup = null)
    {
        if (string.IsNullOrWhiteSpace(saveDataRoot))
        {
            throw new ArgumentException("A save-data root is required.", nameof(saveDataRoot));
        }

        this.saveDataRoot = Path.GetFullPath(saveDataRoot);
        this.tokenFactory = tokenFactory ?? CreateRandomTokenBytes;
        this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        this.runIdFactory = runIdFactory ?? (() => Guid.NewGuid().ToString("N"));
        this.processStartLookup = processStartLookup ?? LookupProcessStartUtc;
    }

    public GatewaySessionLease Prepare(
        int processId,
        DateTimeOffset processStartUtc,
        string gameVersion,
        string modVersion)
    {
        if (processId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(processId));
        }

        lock (sync)
        {
            if (currentLease is not null)
            {
                throw new InvalidOperationException("The gateway session is already prepared or active.");
            }

            AcquireSessionClaim();
            try
            {
                ScrubStaleCurrentSession();

                var tokenBytes = tokenFactory();
                if (tokenBytes is null || tokenBytes.Length != 32)
                {
                    throw new InvalidOperationException("The token factory must return exactly 32 bytes.");
                }

                var runId = runIdFactory();
                if (!IsSafeRunId(runId))
                {
                    throw new InvalidOperationException("The run ID is not a valid path segment.");
                }

                currentLease = new GatewaySessionLease(
                    this,
                    ToBase64Url(tokenBytes),
                    runId,
                    processId,
                    processStartUtc.ToUniversalTime(),
                    utcNow().ToUniversalTime(),
                    gameVersion ?? string.Empty,
                    modVersion ?? string.Empty);
                return currentLease;
            }
            catch
            {
                ReleaseSessionClaim();
                throw;
            }
        }
    }

    public GatewaySessionManifest Start(
        int port,
        int processId,
        DateTimeOffset processStartUtc,
        string gameVersion,
        string modVersion)
    {
        return Prepare(processId, processStartUtc, gameVersion, modVersion).Publish(port);
    }

    public void Stop()
    {
        GatewaySessionLease? lease;
        lock (sync)
        {
            lease = currentLease;
        }

        lease?.Stop();
    }

    internal GatewaySessionManifest Publish(GatewaySessionLease lease, int port)
    {
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port));
        }

        lock (sync)
        {
            EnsureCurrent(lease);
            if (lease.IsStopped)
            {
                throw new InvalidOperationException("A stopped gateway session cannot be published.");
            }

            if (lease.IsPublished)
            {
                throw new InvalidOperationException("The gateway session is already published.");
            }

            var active = CreateActiveManifest(lease, port);
            lease.ActiveManifest = active;
            var runPath = RunManifestPath(lease.RunId);
            var currentPath = CurrentManifestPath;
            try
            {
                WriteAtomic(runPath, active);
                WriteAtomic(currentPath, active);
                lease.IsPublished = true;
                return active;
            }
            catch
            {
                StopCore(lease, throwOnFailure: false);
                throw;
            }
        }
    }

    internal void Stop(GatewaySessionLease lease)
    {
        lock (sync)
        {
            if (!ReferenceEquals(currentLease, lease) || lease.IsStopped)
            {
                return;
            }

            StopCore(lease, throwOnFailure: true);
        }
    }

    internal GatewayRequestJournal CreateRequestJournal(GatewaySessionLease lease)
    {
        lock (sync)
        {
            EnsureCurrent(lease);
            if (lease.IsStopped)
            {
                throw new InvalidOperationException("A stopped gateway session cannot create a request journal.");
            }

            return new GatewayRequestJournal(
                Path.Combine(saveDataRoot, "DevGateway", "Sessions", lease.RunId),
                utcNow);
        }
    }

    private void StopCore(GatewaySessionLease lease, bool throwOnFailure)
    {
        Exception? failure = null;
        var active = lease.ActiveManifest;
        var currentPath = CurrentManifestPath;

        try
        {
            if (File.Exists(currentPath) && CurrentLocatorBelongsTo(lease, currentPath))
            {
                File.Delete(currentPath);
            }
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        if (active is not null)
        {
            try
            {
                WriteAtomic(RunManifestPath(lease.RunId), CreateStoppedManifest(active, "stopped"));
            }
            catch (Exception exception)
            {
                failure ??= exception;
            }
        }

        if (failure is null)
        {
            lease.IsStopped = true;
            currentLease = null;
            ReleaseSessionClaim();
            return;
        }

        if (throwOnFailure)
        {
            if (IsTransientSharingViolation(failure))
            {
                throw new GatewayTransientSessionCleanupException(
                    "The gateway session cleanup was blocked by a transient file lock.",
                    failure);
            }

            throw new IOException("The gateway session could not be cleaned up completely.", failure);
        }
    }

    private void ScrubStaleCurrentSession()
    {
        var currentPath = CurrentManifestPath;
        if (!File.Exists(currentPath))
        {
            return;
        }

        GatewaySessionManifest manifest;
        try
        {
            manifest = GatewayContractJson.ReadFile<GatewaySessionManifest>(currentPath);
        }
        catch
        {
            File.Delete(currentPath);
            return;
        }

        if (IsLive(manifest))
        {
            throw new InvalidOperationException(
                "The save-data folder already advertises a live RimWorld Dev Gateway session.");
        }

        File.Delete(currentPath);
        if (!IsSafeRunId(manifest.RunId))
        {
            return;
        }

        WriteAtomic(RunManifestPath(manifest.RunId), CreateStoppedManifest(manifest, "stale"));
    }

    private bool IsLive(GatewaySessionManifest manifest)
    {
        if (!string.Equals(manifest.State, "active", StringComparison.Ordinal) ||
            string.IsNullOrEmpty(manifest.Token) ||
            manifest.ProcessId <= 0 ||
            !DateTimeOffset.TryParseExact(
                manifest.ProcessStartUtc,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var expectedStart))
        {
            return false;
        }

        var actualStart = processStartLookup(manifest.ProcessId);
        return actualStart.HasValue &&
               actualStart.Value.ToUniversalTime() == expectedStart.ToUniversalTime();
    }

    private GatewaySessionManifest CreateActiveManifest(GatewaySessionLease lease, int port)
    {
        return new GatewaySessionManifest
        {
            ApiVersion = ApiVersion,
            RunId = lease.RunId,
            State = "active",
            BaseUrl = $"http://127.0.0.1:{port}/api/v1",
            Token = lease.Token,
            ProcessId = lease.ProcessId,
            ProcessStartUtc = lease.ProcessStartUtc.ToString("O", CultureInfo.InvariantCulture),
            StartedUtc = lease.PreparedUtc.ToString("O", CultureInfo.InvariantCulture),
            GameVersion = lease.GameVersion,
            ModVersion = lease.ModVersion,
            UnrestrictedExecution = true,
            Warning = RimWorldDevGatewayMod.DangerWarning
        };
    }

    private GatewaySessionManifest CreateStoppedManifest(GatewaySessionManifest active, string state)
    {
        return new GatewaySessionManifest
        {
            ApiVersion = active.ApiVersion,
            RunId = active.RunId,
            State = state,
            BaseUrl = active.BaseUrl,
            Token = null,
            ProcessId = active.ProcessId,
            ProcessStartUtc = active.ProcessStartUtc,
            StartedUtc = active.StartedUtc,
            StoppedUtc = utcNow().ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            GameVersion = active.GameVersion,
            ModVersion = active.ModVersion,
            UnrestrictedExecution = true,
            Warning = active.Warning
        };
    }

    private void EnsureCurrent(GatewaySessionLease lease)
    {
        if (!ReferenceEquals(currentLease, lease))
        {
            throw new InvalidOperationException("The gateway session lease does not belong to the active preparation.");
        }
    }

    private string CurrentManifestPath => Path.Combine(saveDataRoot, "DevGateway", "current.json");

    private string ClaimPath => Path.Combine(saveDataRoot, "DevGateway", "session.lock");

    private string RunManifestPath(string runId) =>
        Path.Combine(saveDataRoot, "DevGateway", "Sessions", runId, "session.json");

    private void AcquireSessionClaim()
    {
        if (sessionClaim is not null)
        {
            throw new InvalidOperationException("The gateway save-data root is already claimed by this manager.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ClaimPath)!);
        try
        {
            sessionClaim = new FileStream(
                ClaimPath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 1,
                FileOptions.WriteThrough);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException(
                "The gateway save-data root is already claimed by another live gateway process.",
                exception);
        }
    }

    private void ReleaseSessionClaim()
    {
        var claim = sessionClaim;
        sessionClaim = null;
        if (claim is null)
        {
            return;
        }

        claim.Dispose();
        try
        {
            if (File.Exists(ClaimPath))
            {
                File.Delete(ClaimPath);
            }
        }
        catch (IOException)
        {
            // Another process may have acquired the now-unlocked claim between close and cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // The zero-secret lock file may remain; OpenOrCreate can reuse it on the next run.
        }
    }

    private static bool CurrentLocatorBelongsTo(GatewaySessionLease lease, string currentPath)
    {
        GatewaySessionManifest current;
        try
        {
            current = GatewayContractJson.ReadFile<GatewaySessionManifest>(currentPath);
        }
        catch
        {
            // This manager holds the exclusive save-root claim. An unreadable locator cannot
            // identify another valid owner and may still contain this run's live credential.
            return true;
        }

        return string.Equals(current.RunId, lease.RunId, StringComparison.Ordinal) &&
               current.ProcessId == lease.ProcessId &&
               string.Equals(
                   current.ProcessStartUtc,
                   lease.ProcessStartUtc.ToString("O", CultureInfo.InvariantCulture),
                   StringComparison.Ordinal);
    }

    private static bool IsSafeRunId(string? runId)
    {
        if (runId is null || string.IsNullOrWhiteSpace(runId))
        {
            return false;
        }

        return runId != "." &&
               runId != ".." &&
               runId.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
               runId.IndexOf(Path.DirectorySeparatorChar) < 0 &&
               runId.IndexOf(Path.AltDirectorySeparatorChar) < 0;
    }

    private static DateTimeOffset? LookupProcessStartUtc(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static byte[] CreateRandomTokenBytes()
    {
        var bytes = new byte[32];
        using var random = RandomNumberGenerator.Create();
        random.GetBytes(bytes);
        return bytes;
    }

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd(new[] { '=' }).Replace('+', '-').Replace('/', '_');

    private static void WriteAtomic<T>(string path, T value)
    {
        var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Manifest path has no directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, GatewayContractJson.Write(value), new System.Text.UTF8Encoding(false));
            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    internal static bool IsTransientSharingViolation(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is IOException ioException)
            {
                var nativeError = ioException.HResult & 0xFFFF;
                if (nativeError is 32 or 33)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
