using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace RimWorldDevGateway.EndToEndHost;

public sealed record EndToEndXmlFixtureLease(string ModsRoot, string OwnerPackageId, string PackageId,
    string RimWorldVersion, string TransactionId)
{
    public string DestinationDirectory => Path.Combine(ModsRoot, PackageId);
    public string TemporaryDirectory => Path.Combine(Path.GetDirectoryName(ModsRoot)!,
        ".rimworld-modding-mcp", "xml-fixtures", TransactionId);
}

public sealed class EndToEndXmlFixturePublisher
{
    public const string MarkerFileName = ".rimworld-e2e-xml-fixture.owner.json";
    private readonly IEndToEndStageFaultInjector? faults;

    public EndToEndXmlFixturePublisher(IEndToEndStageFaultInjector? faults = null) => this.faults = faults;

    public EndToEndXmlFixtureLease Publish(EndToEndXmlFixturePlan plan,
        Action<EndToEndXmlFixtureLease> persistLease)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(persistLease);
        var lease = new EndToEndXmlFixtureLease(plan.ModsRoot, plan.OwnerPackageId, plan.PackageId,
            plan.RimWorldVersion, Guid.NewGuid().ToString("N"));
        ValidateLease(lease);
        using var transaction = Acquire(lease.DestinationDirectory);
        if (Convert.ToHexString(SHA256.HashData(EndToEndXmlFixturePlanner.ReadBounded(plan.ManifestPath, 64 * 1024))) != plan.ManifestSha256)
            throw new EndToEndStageException("XML fixture manifest changed after planning.");
        if (Path.Exists(lease.DestinationDirectory))
            throw new EndToEndStageException("XML fixture destination already exists; it will not be replaced.");
        persistLease(lease);
        try
        {
            faults?.OnFaultPoint("after-lease-persist");
            ValidateLease(lease);
            if (Path.Exists(lease.TemporaryDirectory))
                throw new EndToEndStageException("XML fixture temporary transaction path already exists.");
            Directory.CreateDirectory(lease.TemporaryDirectory);
            File.WriteAllText(Path.Combine(lease.TemporaryDirectory, MarkerFileName), JsonSerializer.Serialize(lease));
            foreach (var file in plan.Files)
            {
                string source = Path.Combine(plan.SourceDirectory, file.RelativePath);
                EndToEndXmlFixturePlanner.RejectReparseAncestors(source);
                var bytes = EndToEndXmlFixturePlanner.ReadBounded(source, 8 * 1024 * 1024);
                if (bytes.LongLength != file.Length || Convert.ToHexString(SHA256.HashData(bytes)) != file.Sha256)
                    throw new EndToEndStageException("XML fixture source changed after planning: " + file.RelativePath);
                string destination = Path.Combine(lease.TemporaryDirectory, file.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.WriteAllBytes(destination, bytes);
            }
            faults?.OnFaultPoint("after-stage-write");
            ValidateLease(lease);
            Directory.CreateDirectory(lease.ModsRoot);
            Directory.Move(lease.TemporaryDirectory, lease.DestinationDirectory);
            faults?.OnFaultPoint("after-commit");
            return lease;
        }
        catch (Exception exception)
        {
            bool cleaned = TryCleanup(lease, out string? failure);
            throw new EndToEndStageException(cleaned
                ? "XML fixture publication failed; owned paths were cleaned."
                : "XML fixture publication failed; cleanup incomplete: " + failure, exception);
        }
    }

    private static IDisposable Acquire(string destination)
    {
        string identity = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant();
        string name = (OperatingSystem.IsWindows() ? @"Local\" : "") + "RimWorldDevGateway.XmlFixture." +
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        var mutex = new Mutex(false, name);
        try
        {
            bool acquired;
            try { acquired = mutex.WaitOne(TimeSpan.FromSeconds(30)); }
            catch (AbandonedMutexException) { acquired = true; }
            if (!acquired) throw new EndToEndStageException("Timed out waiting for XML fixture destination ownership.");
            return new TransactionLock(mutex);
        }
        catch { mutex.Dispose(); throw; }
    }

    private sealed class TransactionLock(Mutex mutex) : IDisposable
    {
        public void Dispose() { mutex.ReleaseMutex(); mutex.Dispose(); }
    }

    public bool TryCleanup(EndToEndXmlFixtureLease lease, out string? failure)
    {
        try
        {
            ValidateLease(lease);
            using var transaction = Acquire(lease.DestinationDirectory);
            var directories = new[] { lease.TemporaryDirectory, lease.DestinationDirectory }.Where(Path.Exists).ToArray();
            foreach (string directory in directories)
            {
                EndToEndXmlFixturePlanner.RejectReparseAncestors(directory);
                RejectReparseTree(directory);
                var marker = JsonSerializer.Deserialize<EndToEndXmlFixtureLease>(
                    EndToEndXmlFixturePlanner.ReadBounded(Path.Combine(directory, MarkerFileName), 64 * 1024));
                if (marker != lease) throw new EndToEndStageException("XML fixture ownership marker changed; refusing cleanup.");
            }
            foreach (string directory in directories) DeleteOwnedDirectory(directory, lease);
            failure = null;
            return true;
        }
        catch (Exception exception)
        {
            failure = exception.GetType().Name + ": " + exception.Message;
            return false;
        }
    }

    private static void DeleteOwnedDirectory(string directory, EndToEndXmlFixtureLease lease)
    {
        string marker = Path.Combine(directory, MarkerFileName);
        // Keep the recovery identity until every payload entry has actually been removed.
        foreach (string file in Directory.EnumerateFiles(directory))
            if (!StringComparer.OrdinalIgnoreCase.Equals(file, marker)) File.Delete(file);
        foreach (string child in Directory.EnumerateDirectories(directory)) Directory.Delete(child, recursive: true);
        File.Delete(marker);
        try { Directory.Delete(directory); }
        catch
        {
            if (Directory.Exists(directory) && !File.Exists(marker))
                File.WriteAllText(marker, JsonSerializer.Serialize(lease));
            throw;
        }
    }

    private static void RejectReparseTree(string directory)
    {
        foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos())
        {
            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new EndToEndStageException("XML fixture tree contains a reparse point; refusing cleanup.");
            if ((entry.Attributes & FileAttributes.Directory) != 0) RejectReparseTree(entry.FullName);
        }
    }

    internal static void ValidateLease(EndToEndXmlFixtureLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);
        if (!EndToEndXmlFixturePlanner.IsPackageId(lease.PackageId) ||
            !EndToEndXmlFixturePlanner.IsPackageId(lease.OwnerPackageId) || lease.PackageId == lease.OwnerPackageId ||
            lease.RimWorldVersion is not ("1.6" or "1.7") ||
            !Guid.TryParseExact(lease.TransactionId, "N", out _) ||
            !Path.IsPathFullyQualified(lease.ModsRoot) ||
            !StringComparer.Ordinal.Equals(lease.ModsRoot,
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(lease.ModsRoot))) ||
            Path.GetDirectoryName(Path.GetFullPath(lease.ModsRoot)) is null)
            throw new EndToEndStageException("Invalid XML fixture recovery lease.");
        EndToEndXmlFixturePlanner.RejectReparseAncestors(lease.DestinationDirectory);
        EndToEndXmlFixturePlanner.RejectReparseAncestors(lease.TemporaryDirectory);
    }
}
