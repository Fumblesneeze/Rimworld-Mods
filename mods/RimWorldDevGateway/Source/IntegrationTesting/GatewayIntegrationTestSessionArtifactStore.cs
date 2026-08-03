using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RimWorldDevGateway;

public sealed class GatewayIntegrationTestSessionArtifactStore : IGatewayIntegrationTestSessionArtifactStore
{
    private static readonly char[] AllowedRunIdPunctuation = { '-', '_' };
    private static readonly TimeSpan AtomicReplaceRetryWindow = TimeSpan.FromSeconds(1);
    private readonly object sync = new();
    private readonly string saveDataFolder;
    private GatewayIntegrationTestSnapshot? committedSnapshot;
    private string? artifactPath;
    private IGatewayIntegrationTestPersistenceOperation? activeOperation;

    public GatewayIntegrationTestSessionArtifactStore(string saveDataFolder)
    {
        if (string.IsNullOrWhiteSpace(saveDataFolder))
        {
            throw new ArgumentException("A save-data folder is required.", nameof(saveDataFolder));
        }

        this.saveDataFolder = Path.GetFullPath(saveDataFolder);
    }

    public string? ArtifactPath
    {
        get
        {
            lock (sync)
            {
                return artifactPath;
            }
        }
    }

    public bool IsAttached => ArtifactPath is not null;

    public GatewayIntegrationTestSnapshot? CommittedSnapshot
    {
        get
        {
            lock (sync)
            {
                return committedSnapshot;
            }
        }
    }

    public IGatewayIntegrationTestPersistenceOperation BeginAttachSession(
        string runId,
        GatewayIntegrationTestSnapshot initialSnapshot)
    {
        if (!IsValidRunId(runId))
        {
            throw new ArgumentException("The Gateway run ID is not a safe path segment.", nameof(runId));
        }

        if (initialSnapshot is null)
        {
            throw new ArgumentNullException(nameof(initialSnapshot));
        }

        lock (sync)
        {
            var selectedPath = Path.GetFullPath(Path.Combine(
                saveDataFolder,
                "DevGateway",
                "Sessions",
                runId,
                "integration-tests.json"));
            if (artifactPath is not null &&
                !string.Equals(artifactPath, selectedPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The integration-test artifact store is already attached to another Gateway session.");
            }

            EnsureLaneAvailable();
            var operation = new TaskPersistenceOperation(
                Task.Run(() => PersistAndCommit(selectedPath, initialSnapshot, attach: true)));
            activeOperation = operation;
            return operation;
        }
    }

    public IGatewayIntegrationTestPersistenceOperation BeginPersist(
        GatewayIntegrationTestSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        lock (sync)
        {
            if (artifactPath is null)
            {
                throw new InvalidOperationException(
                    "The integration-test artifact store is not attached to a Gateway session.");
            }

            EnsureLaneAvailable();
            var selectedPath = artifactPath;
            var operation = new TaskPersistenceOperation(
                Task.Run(() => PersistAndCommit(selectedPath, snapshot, attach: false)));
            activeOperation = operation;
            return operation;
        }
    }

    private GatewayIntegrationTestPersistenceOutcome PersistAndCommit(
        string selectedPath,
        GatewayIntegrationTestSnapshot snapshot,
        bool attach)
    {
        try
        {
            WriteAtomic(selectedPath, snapshot);
            lock (sync)
            {
                if (attach)
                {
                    artifactPath = selectedPath;
                }

                committedSnapshot = snapshot;
            }

            return GatewayIntegrationTestPersistenceOutcome.Success(snapshot);
        }
        catch (Exception exception)
        {
            return GatewayIntegrationTestPersistenceOutcome.Failed(snapshot, exception);
        }
    }

    private void EnsureLaneAvailable()
    {
        if (activeOperation is not null && !activeOperation.IsCompleted)
        {
            throw new InvalidOperationException(
                "An integration-test artifact persistence operation is already in progress.");
        }
    }

    private static void WriteAtomic(string path, GatewayIntegrationTestSnapshot snapshot)
    {
        var directory = Path.GetDirectoryName(path) ??
            throw new InvalidOperationException("The integration-test artifact has no directory.");
        Directory.CreateDirectory(directory);
        var bytes = GatewayJsonWriter.Write(
            snapshot,
            maxDepth: 8,
            maxNodes: 32 * 1024,
            maxUtf8Bytes: GatewayIntegrationTestSnapshot.MaximumSerializedUtf8Bytes);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 64 * 1024,
                       FileOptions.WriteThrough))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }

            CommitTemporaryFile(temporaryPath, path);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void CommitTemporaryFile(string temporaryPath, string path)
    {
        var retryTimer = Stopwatch.StartNew();
        var delayMilliseconds = 5;
        while (true)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Replace(temporaryPath, path, null, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(temporaryPath, path);
                }

                return;
            }
            catch (IOException) when (
                File.Exists(temporaryPath) && retryTimer.Elapsed < AtomicReplaceRetryWindow)
            {
                // This method runs only on the persistence Task. A brief external reader must not
                // surface a false integration-test failure or ever sleep Unity's update thread.
                Thread.Sleep(delayMilliseconds);
                delayMilliseconds = Math.Min(delayMilliseconds * 2, 50);
            }
        }
    }

    private static bool IsValidRunId(string? runId) =>
        runId is not null &&
        runId.Length is > 0 and <= 128 &&
        runId.All(character => char.IsLetterOrDigit(character) || AllowedRunIdPunctuation.Contains(character));

    private sealed class TaskPersistenceOperation : IGatewayIntegrationTestPersistenceOperation
    {
        private readonly Task<GatewayIntegrationTestPersistenceOutcome> task;

        public TaskPersistenceOperation(Task<GatewayIntegrationTestPersistenceOutcome> task)
        {
            this.task = task ?? throw new ArgumentNullException(nameof(task));
        }

        public bool IsCompleted => task.IsCompleted;

        public GatewayIntegrationTestPersistenceOutcome GetOutcome()
        {
            if (!task.IsCompleted)
            {
                throw new InvalidOperationException("The persistence operation has not completed.");
            }

            return task.GetAwaiter().GetResult();
        }
    }
}
