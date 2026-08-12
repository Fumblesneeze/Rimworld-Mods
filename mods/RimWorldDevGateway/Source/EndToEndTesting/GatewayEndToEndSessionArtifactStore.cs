using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RimWorldDevGateway;

public sealed class GatewayEndToEndSessionArtifactStore : IGatewayEndToEndSessionArtifactStore
{
    private static readonly char[] AllowedRunIdPunctuation = { '-', '_' };
    private static readonly TimeSpan AtomicReplaceRetryWindow = TimeSpan.FromSeconds(1);
    private readonly object sync = new();
    private readonly string saveDataFolder;
    private GatewayEndToEndSnapshot? committedSnapshot;
    private string? artifactPath;
    private IGatewayEndToEndPersistenceOperation? activeOperation;

    public GatewayEndToEndSessionArtifactStore(string saveDataFolder)
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

    public GatewayEndToEndSnapshot? CommittedSnapshot
    {
        get
        {
            lock (sync)
            {
                return committedSnapshot;
            }
        }
    }

    public IGatewayEndToEndPersistenceOperation BeginAttachSession(
        string runId,
        GatewayEndToEndSnapshot initialSnapshot)
    {
        if (!IsValidRunId(runId))
        {
            throw new ArgumentException("The Gateway run ID is not a safe path segment.", nameof(runId));
        }

        var selectedPath = Path.GetFullPath(Path.Combine(
            saveDataFolder,
            "DevGateway",
            "Sessions",
            runId,
            "end-to-end-tests.json"));
        lock (sync)
        {
            if (artifactPath is not null &&
                !StringComparer.OrdinalIgnoreCase.Equals(artifactPath, selectedPath))
            {
                throw new InvalidOperationException("The E2E artifact store is attached to another session.");
            }

            return Start(selectedPath, initialSnapshot, attach: true);
        }
    }

    public IGatewayEndToEndPersistenceOperation BeginPersist(GatewayEndToEndSnapshot snapshot)
    {
        lock (sync)
        {
            if (artifactPath is null)
            {
                throw new InvalidOperationException("The E2E artifact store is not attached to a session.");
            }

            return Start(artifactPath, snapshot, attach: false);
        }
    }

    private IGatewayEndToEndPersistenceOperation Start(
        string path,
        GatewayEndToEndSnapshot snapshot,
        bool attach)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (activeOperation is not null && !activeOperation.IsCompleted)
        {
            throw new InvalidOperationException("An E2E artifact persistence operation is already running.");
        }

        var operation = new TaskPersistenceOperation(Task.Run(() => Persist(path, snapshot, attach)));
        activeOperation = operation;
        return operation;
    }

    private GatewayEndToEndPersistenceOutcome Persist(
        string path,
        GatewayEndToEndSnapshot snapshot,
        bool attach)
    {
        try
        {
            WriteAtomic(path, snapshot);
            lock (sync)
            {
                if (attach)
                {
                    artifactPath = path;
                }

                committedSnapshot = snapshot;
            }

            return GatewayEndToEndPersistenceOutcome.Success(snapshot);
        }
        catch (Exception exception)
        {
            return GatewayEndToEndPersistenceOutcome.Failed(snapshot, exception);
        }
    }

    private static void WriteAtomic(string path, GatewayEndToEndSnapshot snapshot)
    {
        var directory = Path.GetDirectoryName(path) ??
            throw new InvalidOperationException("The E2E artifact path has no directory.");
        Directory.CreateDirectory(directory);
        var bytes = GatewayJsonWriter.Write(
            snapshot,
            maxDepth: 16,
            maxNodes: int.MaxValue,
            maxUtf8Bytes: int.MaxValue);
        string? temporaryPath = null;
        try
        {
            // The short sibling leaf keeps an otherwise valid destination beneath RimWorld
            // Mono's legacy Windows path limit. Creation establishes ownership before cleanup.
            using (var stream = GatewayTemporaryFile.CreateSibling(path, FileOptions.WriteThrough))
            {
                temporaryPath = stream.Name;
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }

            CommitTemporaryFile(temporaryPath, path);
        }
        finally
        {
            if (temporaryPath is not null && File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void CommitTemporaryFile(string temporaryPath, string path)
    {
        var timer = Stopwatch.StartNew();
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
            catch (IOException) when (File.Exists(temporaryPath) && timer.Elapsed < AtomicReplaceRetryWindow)
            {
                Thread.Sleep(delayMilliseconds);
                delayMilliseconds = Math.Min(delayMilliseconds * 2, 50);
            }
        }
    }

    private static bool IsValidRunId(string? runId) =>
        runId is not null &&
        runId.Length is > 0 and <= 128 &&
        runId.All(character => char.IsLetterOrDigit(character) || AllowedRunIdPunctuation.Contains(character));

    private sealed class TaskPersistenceOperation : IGatewayEndToEndPersistenceOperation
    {
        private readonly Task<GatewayEndToEndPersistenceOutcome> task;

        public TaskPersistenceOperation(Task<GatewayEndToEndPersistenceOutcome> task) =>
            this.task = task ?? throw new ArgumentNullException(nameof(task));

        public bool IsCompleted => task.IsCompleted;

        public GatewayEndToEndPersistenceOutcome GetOutcome()
        {
            if (!task.IsCompleted)
            {
                throw new InvalidOperationException("The E2E persistence operation has not completed.");
            }

            return task.GetAwaiter().GetResult();
        }
    }
}
