using System;
using System.Threading;
using System.Threading.Tasks;

namespace PathfindingLab.Core;

// Immutable, owned chunks. Only Capture's caller reads the engine's leased arrays.
public sealed class GeometrySnapshot
{
    private sealed record Chunk(int[] Costs, bool[] Corners);
    private readonly Chunk[] chunks;
    internal object Identity { get; }
    public ChunkStamp Stamp { get; }
    public long Version { get; }
    public int Width => Stamp.Width;
    public int Height => Stamp.Height;
    public int CellsCopied { get; }
    private GeometrySnapshot(ChunkStamp stamp, long version, Chunk[] chunks, int copied, object identity)
    { Stamp = stamp; Version = version; this.chunks = chunks; CellsCopied = copied; Identity = identity; }
    public static GeometrySnapshot Capture(GeometrySnapshot? previous, ChunkStamp stamp, Func<int, int> cost, Func<int, bool> corner)
    {
        if (previous != null && ReferenceEquals(previous.Stamp, stamp)) return previous;
        if (previous != null && (previous.Width != stamp.Width || previous.Height != stamp.Height))
            throw new ArgumentException("Snapshot dimensions changed.");
        var chunks = previous == null ? new Chunk[stamp.Versions.Length] : (Chunk[])previous.chunks.Clone();
        int copied = 0, chunksX = (stamp.Width + 31) / 32;
        bool geometryChanged = previous == null;
        for (int chunk = 0; chunk < chunks.Length; chunk++)
        {
            if (previous != null && previous.Stamp.Versions[chunk] >= stamp.Versions[chunk]) continue;
            var old = previous?.chunks[chunk];
            int[]? costs = old == null ? new int[1024] : null;
            bool[]? corners = old == null ? new bool[1024] : null;
            int x0 = chunk % chunksX * 32, z0 = chunk / chunksX * 32;
            for (int z = z0; z < Math.Min(z0 + 32, stamp.Height); z++)
                for (int x = x0; x < Math.Min(x0 + 32, stamp.Width); x++)
                {
                    int local = (z - z0) * 32 + x - x0, cell = z * stamp.Width + x;
                    int value = cost(cell); bool blocked = corner(cell); copied++;
                    if (costs == null && (value != old!.Costs[local] || blocked != old.Corners[local]))
                    {
                        // Prior cells were equal. Clone once on first change; never reread native
                        // memory or mutate the snapshot that a background builder may still use.
                        costs = (int[])old.Costs.Clone(); corners = (bool[])old.Corners.Clone();
                    }
                    if (costs != null) { costs[local] = value; corners![local] = blocked; }
                }
            if (costs != null) { chunks[chunk] = new Chunk(costs, corners!); geometryChanged = true; }
        }
        if (copied == 0 && previous != null) return previous;
        // Acknowledge native dirtiness even when it only concerned fog/reservations/etc.
        // Equal owned geometry keeps its revision and the existing guide remains usable.
        return new GeometrySnapshot(stamp, (previous?.Version ?? 0) + (geometryChanged ? 1 : 0), chunks, copied, previous?.Identity ?? new object());
    }
    private (Chunk Chunk, int Local) Locate(int index)
    {
        int x = index % Width, z = index / Width;
        return (chunks[x / 32 + z / 32 * ((Width + 31) / 32)], x % 32 + z % 32 * 32);
    }
    public int Cost(int index) { var p = Locate(index); return p.Chunk.Costs[p.Local]; }
    public bool Corner(int index) { var p = Locate(index); return p.Chunk.Corners[p.Local]; }
    internal bool SameChunk(GeometrySnapshot other, int chunk) => ReferenceEquals(chunks[chunk], other.chunks[chunk]);
}

// One coalescing build lane. The scheduler seam is a real execution boundary, not a
// fake graph builder. No request or main-thread diagnostic waits for graph work.
public sealed class BackgroundHierarchy
{
    private readonly object gate = new();
    private readonly Action<Action> schedule;
    private readonly Action? beforePublication;
    private GeometrySnapshot? desired;
    private PortalRouter? builder;
    private PortalGraph? published;
    private bool running, ready, disposed;
    private long completedBuilds;
    public long CompletedBuilds => Interlocked.Read(ref completedBuilds);
    public bool IsRunning { get { lock (gate) return running; } }
    public string? Failure { get; private set; }
    public BackgroundHierarchy(Action<Action>? scheduler = null)
        : this(scheduler, null) { }
    // Internal execution-boundary observer for deterministic overlap tests. Never replaces Build.
    internal BackgroundHierarchy(Action<Action>? scheduler, Action? beforePublication)
    { schedule = scheduler ?? (work => { _ = Task.Run(work); }); this.beforePublication = beforePublication; }
    public bool Request(GeometrySnapshot snapshot)
    {
        lock (gate)
        {
            if (!disposed && desired != null && !ReferenceEquals(desired.Identity, snapshot.Identity))
                throw new ArgumentException("A map builder cannot accept another map's geometry.");
            if (disposed || desired != null && desired.Version >= snapshot.Version) return false;
            desired = snapshot;
            ready = false;
            Failure = null;
            if (running) return true;
            running = true;
            try { schedule(Run); }
            catch (Exception error) { running = false; Failure = error.GetType().Name + ": " + error.Message; }
            return true;
        }
    }
    public bool TryGet(GeometrySnapshot snapshot, out PortalGraph? graph)
    {
        lock (gate)
        {
            bool usable = !disposed && ready && desired?.Version == snapshot.Version && ReferenceEquals(desired.Identity, snapshot.Identity);
            graph = usable ? published : null;
            return usable;
        }
    }
    public bool IsReady(GeometrySnapshot snapshot)
    {
        return TryGet(snapshot, out _);
    }
    public object Status()
    {
        lock (gate) return new { Ready = ready, Running = running, Disposed = disposed, Version = desired?.Version ?? 0, CompletedBuilds, Failure };
    }
    private void Run()
    {
        while (true)
        {
            GeometrySnapshot snapshot;
            lock (gate)
            {
                if (disposed) { running = false; return; }
                snapshot = desired!;
            }
            string? failure = null;
            PortalGraph? result = null;
            try { result = Build(snapshot); beforePublication?.Invoke(); }
            catch (Exception error) { failure = error.GetType().Name + ": " + error.Message; }
            lock (gate)
            {
                if (disposed) { running = false; return; }
                if (!ReferenceEquals(desired, snapshot)) continue;
                Failure = failure;
                published = result;
                ready = failure == null;
                running = false;
                return;
            }
        }
    }
    // Test-only timing wraps this worker seam. No timers ship in product code.
    public PortalGraph Build(GeometrySnapshot snapshot)
    {
        if (builder == null) builder = new PortalRouter(snapshot);
        else builder.ApplySnapshot(snapshot);
        var result = builder.Publish();
        Interlocked.Increment(ref completedBuilds);
        return result;
    }
    public void Dispose()
    {
        lock (gate) { disposed = true; ready = false; desired = null; published = null; }
    }
}
