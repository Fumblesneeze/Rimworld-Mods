using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RimWorldDevGateway;

public enum DispatchPhase
{
    Update,
    EndOfFrame
}

public sealed class GatewayDispatchException : Exception
{
    public GatewayDispatchException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}

public sealed class GatewayDispatchOperation<T>
{
    private const int Queued = 0;
    private const int Started = 1;
    private const int CancelledBeforeStart = 2;
    private Action? cancelledBeforeStart;
    private int state;

    internal GatewayDispatchOperation(Task<T> completion, Action? cancelledBeforeStart = null)
    {
        Completion = completion ?? throw new ArgumentNullException(nameof(completion));
        this.cancelledBeforeStart = cancelledBeforeStart;
    }

    public Task<T> Completion { get; }

    public bool HasStarted => Volatile.Read(ref state) == Started;

    public bool TryCancelBeforeStart()
    {
        if (Interlocked.CompareExchange(ref state, CancelledBeforeStart, Queued) != Queued)
        {
            return false;
        }

        cancelledBeforeStart?.Invoke();
        return true;
    }

    internal void OnCancelledBeforeStart(Action callback)
    {
        cancelledBeforeStart += callback ?? throw new ArgumentNullException(nameof(callback));
    }

    internal bool TryMarkStarted() =>
        Interlocked.CompareExchange(ref state, Started, Queued) == Queued;
}

public sealed class GatewayDispatcher
{
    private readonly ConcurrentQueue<WorkItem> updateQueue = new();
    private readonly ConcurrentQueue<WorkItem> endOfFrameQueue = new();
    private readonly object lifecycleSync = new();
    private readonly Func<DateTimeOffset> utcNow;
    private readonly int capacity;
    private int pendingCount;
    private int stopped;

    public GatewayDispatcher(int capacity = 128, Func<DateTimeOffset>? utcNow = null)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        this.capacity = capacity;
        this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public int PendingCount => Volatile.Read(ref pendingCount);

    public bool IsStopped => Volatile.Read(ref stopped) != 0;

    public Task<T> Enqueue<T>(
        string requestId,
        string operation,
        TimeSpan timeout,
        Func<CancellationToken, T> action,
        DispatchPhase phase = DispatchPhase.Update,
        CancellationToken cancellationToken = default)
    {
        return EnqueueOperation(
            requestId,
            operation,
            timeout,
            action,
            phase,
            cancellationToken).Completion;
    }

    public GatewayDispatchOperation<T> EnqueueOperation<T>(
        string requestId,
        string operation,
        TimeSpan timeout,
        Func<CancellationToken, T> action,
        DispatchPhase phase = DispatchPhase.Update,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ArgumentException("A request ID is required.", nameof(requestId));
        }

        if (string.IsNullOrWhiteSpace(operation))
        {
            throw new ArgumentException("An operation name is required.", nameof(operation));
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatchOperation = new GatewayDispatchOperation<T>(
            ConvertResult<T>(completion.Task),
            () => completion.TrySetException(new GatewayDispatchException(
                "cancelled_before_start",
                $"Operation '{operation}' was cancelled before it started.")));
        lock (lifecycleSync)
        {
            if (stopped != 0)
            {
                completion.SetException(StoppingException(operation));
                return dispatchOperation;
            }

            if (pendingCount >= capacity)
            {
                completion.SetException(new GatewayDispatchException(
                    "queue_full",
                    $"The gateway dispatcher is at its {capacity}-item capacity."));
                return dispatchOperation;
            }

            pendingCount++;
            var item = new WorkItem(
                requestId,
                operation,
                utcNow().Add(timeout),
                token => action(token),
                cancellationToken,
                completion,
                dispatchOperation.TryMarkStarted);

            QueueFor(phase).Enqueue(item);
        }

        return dispatchOperation;
    }

    public int Drain(DispatchPhase phase, int maximumItems = 1)
    {
        if (maximumItems <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumItems));
        }

        var queue = QueueFor(phase);
        var drained = 0;
        while (drained < maximumItems)
        {
            WorkItem? item;
            lock (lifecycleSync)
            {
                if (stopped != 0 || !queue.TryDequeue(out item))
                {
                    break;
                }

                pendingCount--;
            }

            drained++;
            Execute(item);
        }

        return drained;
    }

    public int Stop()
    {
        var cancelled = new List<WorkItem>();
        lock (lifecycleSync)
        {
            if (stopped != 0)
            {
                return 0;
            }

            Volatile.Write(ref stopped, 1);
            DequeueAll(updateQueue, cancelled);
            DequeueAll(endOfFrameQueue, cancelled);
            pendingCount = 0;
        }

        foreach (var item in cancelled)
        {
            item.Completion.TrySetException(StoppingException(item.Operation));
        }

        return cancelled.Count;
    }

    private static async Task<T> ConvertResult<T>(Task<object?> task)
    {
        var value = await task.ConfigureAwait(false);
        return (T)value!;
    }

    private ConcurrentQueue<WorkItem> QueueFor(DispatchPhase phase) =>
        phase == DispatchPhase.EndOfFrame ? endOfFrameQueue : updateQueue;

    private static void DequeueAll(ConcurrentQueue<WorkItem> queue, ICollection<WorkItem> target)
    {
        while (queue.TryDequeue(out var item))
        {
            target.Add(item);
        }
    }

    private static GatewayDispatchException StoppingException(string operation) =>
        new("gateway_stopping", $"Operation '{operation}' was rejected because the gateway is stopping.");

    private void Execute(WorkItem item)
    {
        if (item.CancellationToken.IsCancellationRequested)
        {
            item.Completion.TrySetException(new GatewayDispatchException(
                "cancelled_before_start",
                $"Operation '{item.Operation}' was cancelled before it started."));
            return;
        }

        if (utcNow() > item.Deadline)
        {
            item.Completion.TrySetException(new GatewayDispatchException(
                "timed_out_before_start",
                $"Operation '{item.Operation}' exceeded its queue deadline before it started."));
            return;
        }

        try
        {
            if (!item.TryMarkStarted())
            {
                item.Completion.TrySetException(new GatewayDispatchException(
                    "cancelled_before_start",
                    $"Operation '{item.Operation}' was cancelled before it started."));
                return;
            }

            GatewayRequestScope.CurrentRequestId = item.RequestId;
            item.Completion.TrySetResult(item.Action(item.CancellationToken));
        }
        catch (Exception exception)
        {
            item.Completion.TrySetException(exception);
        }
        finally
        {
            GatewayRequestScope.CurrentRequestId = null;
        }
    }

    private sealed class WorkItem
    {
        public WorkItem(
            string requestId,
            string operation,
            DateTimeOffset deadline,
            Func<CancellationToken, object?> action,
            CancellationToken cancellationToken,
            TaskCompletionSource<object?> completion,
            Func<bool> tryMarkStarted)
        {
            RequestId = requestId;
            Operation = operation;
            Deadline = deadline;
            Action = action;
            CancellationToken = cancellationToken;
            Completion = completion;
            TryMarkStarted = tryMarkStarted;
        }

        public string RequestId { get; }

        public string Operation { get; }

        public DateTimeOffset Deadline { get; }

        public Func<CancellationToken, object?> Action { get; }

        public CancellationToken CancellationToken { get; }

        public TaskCompletionSource<object?> Completion { get; }

        public Func<bool> TryMarkStarted { get; }
    }
}

public static class GatewayRequestScope
{
    [ThreadStatic]
    private static string? currentRequestId;

    public static string? CurrentRequestId
    {
        get => currentRequestId;
        internal set => currentRequestId = value;
    }
}
