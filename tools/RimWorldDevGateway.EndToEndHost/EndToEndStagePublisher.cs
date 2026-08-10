using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.EndToEndHost;

public sealed class EndToEndStageException : Exception
{
    public EndToEndStageException(string message) : base(message)
    {
    }

    public EndToEndStageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public interface IEndToEndStageFaultInjector
{
    void OnFaultPoint(string point);
}

public sealed class EndToEndStageLease
{
    internal EndToEndStageLease(
        string destinationDirectory,
        string ownerPackageId,
        string rimWorldVersion,
        string transactionId)
    {
        DestinationDirectory = destinationDirectory;
        OwnerPackageId = ownerPackageId;
        RimWorldVersion = rimWorldVersion;
        TransactionId = transactionId;
    }

    public string DestinationDirectory { get; }

    public string OwnerPackageId { get; }

    public string RimWorldVersion { get; }

    public string TransactionId { get; }
}

public static class EndToEndStagePaths
{
    public const string ContentDirectoryName = "DevEndToEndTests";

    public static string GetDestination(string modsRoot, string ownerPackageId, string rimWorldVersion)
    {
        var root = Path.GetFullPath(
            string.IsNullOrWhiteSpace(modsRoot)
                ? throw new EndToEndStageException("A mods root is required.")
                : modsRoot);
        ValidateSegment(ownerPackageId, "owner package ID");
        ValidateSegment(rimWorldVersion, "RimWorld version");
        var destination = Path.GetFullPath(Path.Combine(
            root,
            ownerPackageId,
            rimWorldVersion,
            ContentDirectoryName));
        if (!IsWithin(root, destination))
        {
            throw new EndToEndStageException("The E2E stage destination escapes the mods root.");
        }

        return destination;
    }

    internal static bool IsWithin(string root, string candidate)
    {
        var normalizedRoot = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedCandidate = Path.GetFullPath(candidate);
        return normalizedCandidate.StartsWith(
            normalizedRoot + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateSegment(string value, string description)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value == "." ||
            value == ".." ||
            !StringComparer.Ordinal.Equals(Path.GetFileName(value), value) ||
            value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            value.Contains(Path.DirectorySeparatorChar) ||
            value.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new EndToEndStageException($"The {description} is not a safe path segment: '{value}'.");
        }
    }
}

public sealed class EndToEndStagePublisher
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private readonly IEndToEndStageFaultInjector? faultInjector;

    public EndToEndStagePublisher(IEndToEndStageFaultInjector? faultInjector = null)
    {
        this.faultInjector = faultInjector;
    }

    public EndToEndStageLease Publish(EndToEndOwnerStagePlan plan)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        var expectedDestination = EndToEndStagePaths.GetDestination(
            plan.ModsRoot,
            plan.OwnerPackageId,
            plan.RimWorldVersion);
        if (!StringComparer.OrdinalIgnoreCase.Equals(expectedDestination, Path.GetFullPath(plan.DestinationDirectory)))
        {
            throw new EndToEndStageException("The E2E stage plan destination does not match its owner/version path.");
        }

        var parent = Path.GetDirectoryName(expectedDestination) ??
            throw new EndToEndStageException("The E2E stage destination has no parent directory.");
        using var transactionLock = AcquireDestinationTransactionLock(expectedDestination);
        Directory.CreateDirectory(parent);
        EnsureExistingTreeHasNoReparsePoint(Path.GetFullPath(plan.ModsRoot), parent);

        var transactionId = Guid.NewGuid().ToString("N");
        var temporary = Path.Combine(parent, ".DevEndToEndTests.stage." + transactionId);
        var backup = Path.Combine(parent, ".DevEndToEndTests.backup." + transactionId);
        var movedExisting = false;
        var committedNewStage = false;
        try
        {
            if (Directory.Exists(temporary) || Directory.Exists(backup))
            {
                throw new EndToEndStageException("The generated E2E stage transaction path already exists.");
            }

            Directory.CreateDirectory(temporary);
            WriteBundle(plan, temporary, transactionId);
            faultInjector?.OnFaultPoint("after-stage-write");

            if (Directory.Exists(expectedDestination))
            {
                ReadAndValidateMarker(
                    expectedDestination,
                    plan.OwnerPackageId,
                    plan.RimWorldVersion,
                    expectedTransactionId: null);
                MoveDirectoryWithTransientRetry(expectedDestination, backup);
                movedExisting = true;
                faultInjector?.OnFaultPoint("after-backup");
            }

            MoveDirectoryWithTransientRetry(temporary, expectedDestination);
            committedNewStage = true;
            faultInjector?.OnFaultPoint("after-commit");
            if (Directory.Exists(backup))
            {
                DeleteDirectory(backup, parent);
            }

            return new EndToEndStageLease(
                expectedDestination,
                plan.OwnerPackageId,
                plan.RimWorldVersion,
                transactionId);
        }
        catch (Exception exception)
        {
            var rollbackFailures = new List<Exception>();
            TryRollback(
                expectedDestination,
                temporary,
                backup,
                parent,
                plan.OwnerPackageId,
                plan.RimWorldVersion,
                transactionId,
                movedExisting,
                committedNewStage,
                rollbackFailures);
            if (rollbackFailures.Count > 0)
            {
                rollbackFailures.Insert(0, exception);
                throw new EndToEndStageException(
                    "E2E stage publication failed and rollback was incomplete.",
                    new AggregateException(rollbackFailures));
            }

            if (exception is EndToEndStageException)
            {
                throw;
            }

            throw new EndToEndStageException(
                "E2E stage publication failed and was rolled back. " +
                exception.GetType().Name + ": " + exception.Message,
                exception);
        }
    }

    public bool TryCleanup(EndToEndStageLease lease)
    {
        if (lease is null)
        {
            throw new ArgumentNullException(nameof(lease));
        }

        using var transactionLock = AcquireDestinationTransactionLock(lease.DestinationDirectory);
        if (!Directory.Exists(lease.DestinationDirectory))
        {
            return true;
        }

        try
        {
            ReadAndValidateMarker(
                lease.DestinationDirectory,
                lease.OwnerPackageId,
                lease.RimWorldVersion,
                lease.TransactionId);
        }
        catch
        {
            return false;
        }

        var parent = Path.GetDirectoryName(lease.DestinationDirectory) ?? string.Empty;
        DeleteDirectory(lease.DestinationDirectory, parent);
        return true;
    }

    private static void WriteBundle(
        EndToEndOwnerStagePlan plan,
        string temporaryDirectory,
        string transactionId)
    {
        foreach (var bundle in plan.Bundles)
        {
            ValidateFileName(bundle.AssemblyFileName);
            ValidateFileName(bundle.ManifestFileName);
            var bytes = File.ReadAllBytes(bundle.SourceAssemblyPath);
            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if (bytes.LongLength != bundle.Manifest.AssemblyLength ||
                !StringComparer.Ordinal.Equals(hash, bundle.Manifest.AssemblySha256))
            {
                throw new EndToEndStageException(
                    $"E2E assembly changed after metadata discovery: {bundle.SourceAssemblyPath}");
            }

            File.WriteAllBytes(Path.Combine(temporaryDirectory, bundle.AssemblyFileName), bytes);
            File.WriteAllText(
                Path.Combine(temporaryDirectory, bundle.ManifestFileName),
                bundle.ManifestJson,
                Utf8WithoutBom);
        }

        var marker = new EndToEndStageMarker
        {
            OwnerPackageId = plan.OwnerPackageId,
            RimWorldVersion = plan.RimWorldVersion,
            TransactionId = transactionId,
            Assemblies = plan.Bundles.Select(bundle => bundle.AssemblyFileName).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            Manifests = plan.Bundles.Select(bundle => bundle.ManifestFileName).OrderBy(value => value, StringComparer.Ordinal).ToArray()
        };
        File.WriteAllText(
            Path.Combine(temporaryDirectory, EndToEndStageMarker.FileName),
            GatewayContractJson.Write(marker),
            Utf8WithoutBom);
    }

    private static EndToEndStageMarker ReadAndValidateMarker(
        string directory,
        string expectedOwner,
        string expectedVersion,
        string? expectedTransactionId)
    {
        var markerPath = Path.Combine(directory, EndToEndStageMarker.FileName);
        if (!File.Exists(markerPath))
        {
            throw new EndToEndStageException(
                "The existing E2E stage has no repository ownership marker; refuse replacement or cleanup.");
        }

        EndToEndStageMarker marker;
        try
        {
            marker = GatewayContractJson.ReadFile<EndToEndStageMarker>(markerPath);
        }
        catch (Exception exception)
        {
            throw new EndToEndStageException(
                "The existing E2E stage ownership marker is invalid; refuse replacement or cleanup.",
                exception);
        }

        if (!StringComparer.Ordinal.Equals(marker.Schema, EndToEndStageMarker.SchemaValue) ||
            !StringComparer.OrdinalIgnoreCase.Equals(marker.OwnerPackageId, expectedOwner) ||
            !StringComparer.Ordinal.Equals(marker.RimWorldVersion, expectedVersion) ||
            (expectedTransactionId is not null &&
             !StringComparer.Ordinal.Equals(marker.TransactionId, expectedTransactionId)))
        {
            throw new EndToEndStageException(
                "The existing E2E stage ownership marker does not match; refuse replacement or cleanup.");
        }

        return marker;
    }

    private void TryRollback(
        string destination,
        string temporary,
        string backup,
        string parent,
        string ownerPackageId,
        string rimWorldVersion,
        string transactionId,
        bool movedExisting,
        bool committedNewStage,
        ICollection<Exception> failures)
    {
        try
        {
            if (committedNewStage && Directory.Exists(destination))
            {
                ReadAndValidateMarker(
                    destination,
                    ownerPackageId,
                    rimWorldVersion,
                    transactionId);
                DeleteDirectory(destination, parent);
            }
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        try
        {
            if (movedExisting && Directory.Exists(backup))
            {
                if (Directory.Exists(destination))
                {
                    throw new EndToEndStageException(
                        "E2E stage rollback cannot restore the previous owned stage because the " +
                        "destination became occupied.");
                }

                MoveDirectoryWithTransientRetry(backup, destination);
            }
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        try
        {
            if (Directory.Exists(temporary))
            {
                DeleteDirectory(temporary, parent);
            }
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

    }

    private static void ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            !StringComparer.Ordinal.Equals(Path.GetFileName(fileName), fileName) ||
            fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new EndToEndStageException($"Unsafe E2E bundle file name: '{fileName}'.");
        }
    }

    private void MoveDirectoryWithTransientRetry(string source, string destination)
    {
        var deadline = Stopwatch.StartNew();
        var delayMilliseconds = 25;
        while (true)
        {
            try
            {
                Directory.Move(source, destination);
                return;
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException &&
                Directory.Exists(source) &&
                !Directory.Exists(destination) &&
                deadline.Elapsed < TimeSpan.FromSeconds(2))
            {
                faultInjector?.OnFaultPoint("move-retry");
                Thread.Sleep(delayMilliseconds);
                delayMilliseconds = Math.Min(delayMilliseconds * 2, 250);
            }
        }
    }

    private static IDisposable AcquireDestinationTransactionLock(string destination)
    {
        var canonicalDestination = Path.GetFullPath(destination)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .ToUpperInvariant();
        var nameHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(canonicalDestination)))
            .ToLowerInvariant();
        var mutexName = OperatingSystem.IsWindows()
            ? @"Local\RimWorldDevGateway.E2EStage." + nameHash
            : "RimWorldDevGateway.E2EStage." + nameHash;
        var mutex = new Mutex(initiallyOwned: false, mutexName);
        var acquired = false;
        try
        {
            try
            {
                acquired = mutex.WaitOne(TimeSpan.FromSeconds(30));
            }
            catch (AbandonedMutexException)
            {
                acquired = true;
            }

            if (!acquired)
            {
                throw new EndToEndStageException(
                    "Timed out waiting for exclusive ownership of the E2E stage destination.");
            }

            return new DestinationTransactionLock(mutex);
        }
        catch
        {
            if (acquired)
            {
                mutex.ReleaseMutex();
            }

            mutex.Dispose();
            throw;
        }
    }

    private sealed class DestinationTransactionLock : IDisposable
    {
        private Mutex? mutex;

        internal DestinationTransactionLock(Mutex mutex)
        {
            this.mutex = mutex;
        }

        public void Dispose()
        {
            var owned = Interlocked.Exchange(ref mutex, null);
            if (owned is null)
            {
                return;
            }

            owned.ReleaseMutex();
            owned.Dispose();
        }
    }

    private static void DeleteDirectory(string directory, string expectedParent)
    {
        var fullDirectory = Path.GetFullPath(directory);
        var fullParent = Path.GetFullPath(expectedParent);
        if (!StringComparer.OrdinalIgnoreCase.Equals(Path.GetDirectoryName(fullDirectory), fullParent) ||
            (new DirectoryInfo(fullDirectory).Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new EndToEndStageException("Refusing to delete an unsafe E2E stage directory.");
        }

        Directory.Delete(fullDirectory, recursive: true);
    }

    private static void EnsureExistingTreeHasNoReparsePoint(string root, string directory)
    {
        var rootPath = Path.GetFullPath(root);
        var current = new DirectoryInfo(Path.GetFullPath(directory));
        while (true)
        {
            if ((current.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new EndToEndStageException("The E2E stage path crosses a reparse point.");
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(current.FullName, rootPath))
            {
                return;
            }

            current = current.Parent ??
                throw new EndToEndStageException("The E2E stage path escapes the mods root.");
        }
    }
}
