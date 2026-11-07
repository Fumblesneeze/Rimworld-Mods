using System;
using System.Collections.Generic;
using System.Threading;

namespace PathfindingLab.Core;

public enum RoutingCohort { Player, Wildlife, Allied, Hostile, Neutral }

public sealed class ChunkChanges(int width, int height)
{
    private ChunkStamp current = new(width, height);
    private readonly HashSet<int> dirty = new();
    public ChunkStamp Capture()
    {
        if (dirty.Count == 0) return current;
        int[] versions = (int[])current.Versions.Clone();
        foreach (int chunk in dirty) versions[chunk] = checked(versions[chunk] + 1);
        dirty.Clear();
        return current = new ChunkStamp(width, height, versions);
    }
    public void MarkCell(int x, int z)
    {
        if ((uint)x < width && (uint)z < height) dirty.Add(x / 32 + z / 32 * ((width + 31) / 32));
    }
    public void MarkRect(int minX, int minZ, int maxX, int maxZ)
    {
        minX = Math.Max(0, minX); minZ = Math.Max(0, minZ);
        maxX = Math.Min(width - 1, maxX); maxZ = Math.Min(height - 1, maxZ);
        if (minX > maxX || minZ > maxZ) return;
        for (int z = minZ / 32; z <= maxZ / 32; z++)
            for (int x = minX / 32; x <= maxX / 32; x++) dirty.Add(x + z * ((width + 31) / 32));
    }
    public void MarkAll() => MarkRect(0, 0, width - 1, height - 1);
}

public sealed class ChunkStamp
{
    internal readonly int Width, Height;
    internal readonly int[] Versions;
    internal ChunkStamp(int width, int height)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        Width = width; Height = height;
        Versions = new int[((width + 31) / 32) * ((height + 31) / 32)];
        Array.Fill(Versions, 1);
    }
    internal ChunkStamp(int width, int height, int[] ownedVersions)
    { Width = width; Height = height; Versions = ownedVersions; }
}

public enum WorkspaceFallback { None, GraphUnavailable, CapacityExhausted }

public sealed class HierarchyProfiles : IDisposable
{
    public const int WorkspaceCapacity = 8;
    private sealed class Slot { internal long Ticket; internal RequestRouter? Router; }
    private readonly Slot[] slots = new Slot[WorkspaceCapacity];
    private readonly HierarchyProfile[] profiles = new HierarchyProfile[5];
    private readonly BackgroundHierarchy background;
    private long nextTicket, unavailable, exhausted;
    private int disposed, active, peak;
    public HierarchyProfiles(Action<Action>? scheduler = null)
        : this(scheduler, null) { }
    internal HierarchyProfiles(Action<Action>? scheduler, Action? beforePublication)
    {
        background = new BackgroundHierarchy(scheduler, beforePublication);
        for (int i = 0; i < slots.Length; i++) slots[i] = new Slot();
        for (int i = 0; i < profiles.Length; i++) profiles[i] = new HierarchyProfile();
    }
    public BackgroundHierarchy Background(RoutingCohort cohort) { Get(cohort); return background; }
    public void Prepare(GeometrySnapshot snapshot) { if (Volatile.Read(ref disposed) == 0) background.Request(snapshot); }
    public bool IsReady(GeometrySnapshot snapshot) => Volatile.Read(ref disposed) == 0 && background.IsReady(snapshot);
    public bool IsRunning => background.IsRunning;
    public long GraphUnavailable => Interlocked.Read(ref unavailable);
    public long CapacityExhausted => Interlocked.Read(ref exhausted);
    public int Active => Volatile.Read(ref active);
    public int PeakActive => Volatile.Read(ref peak);
    public HierarchyProfile Get(RoutingCohort cohort)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        if ((uint)cohort >= profiles.Length) throw new ArgumentOutOfRangeException(nameof(cohort));
        return profiles[(int)cohort];
    }
    // Nonblocking admission. The immutable publication is retained by the leased router.
    public bool TryAcquire(GeometrySnapshot snapshot, out WorkspaceLease lease, out WorkspaceFallback fallback)
    {
        lease = default;
        if (Volatile.Read(ref disposed) != 0 || !background.TryGet(snapshot, out var graph))
        { Interlocked.Increment(ref unavailable); fallback = WorkspaceFallback.GraphUnavailable; return false; }
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            long ticket = Interlocked.Increment(ref nextTicket);
            if (ticket == 0) ticket = Interlocked.Increment(ref nextTicket);
            if (Interlocked.CompareExchange(ref slot.Ticket, ticket, 0) != 0) continue;
            try
            {
                if (slot.Router == null) slot.Router = new RequestRouter(graph!);
                else slot.Router.Guide.Bind(graph!);
                int count = Interlocked.Increment(ref active), prior;
                while (count > (prior = Volatile.Read(ref peak)) && Interlocked.CompareExchange(ref peak, count, prior) != prior) { }
                lease = new WorkspaceLease(this, i, ticket, slot.Router);
                fallback = WorkspaceFallback.None;
                return true;
            }
            catch { Volatile.Write(ref slot.Ticket, 0); throw; }
        }
        Interlocked.Increment(ref exhausted); fallback = WorkspaceFallback.CapacityExhausted; return false;
    }
    private void Release(int index, long ticket)
    {
        // A copied/disposed lease must never release a subsequently reused slot (ABA).
        if (Interlocked.CompareExchange(ref slots[index].Ticket, -ticket, ticket) != ticket) return;
        Interlocked.Decrement(ref active);
        Volatile.Write(ref slots[index].Ticket, 0);
    }
    public readonly struct WorkspaceLease : IDisposable
    {
        private readonly HierarchyProfiles? owner;
        private readonly long ticket;
        public int Index { get; }
        public RequestRouter Router { get; }
        internal WorkspaceLease(HierarchyProfiles owner, int index, long ticket, RequestRouter router)
        { this.owner = owner; Index = index; this.ticket = ticket; Router = router; }
        public void Dispose() => owner?.Release(Index, ticket);
    }
    public void Dispose() { Interlocked.Exchange(ref disposed, 1); background.Dispose(); }
}

// Cohorts account for usage; topology and workspace capacity belong to the map, not a faction.
public sealed class HierarchyProfile
{
    private long requests, localHits, abstractSearches, localSearches, cacheHits, graphUnavailable, capacityExhausted;
    public long Requests => Interlocked.Read(ref requests);
    public long LocalHits => Interlocked.Read(ref localHits);
    public long AbstractSearches => Interlocked.Read(ref abstractSearches);
    public long LocalSearches => Interlocked.Read(ref localSearches);
    public long CacheHits => Interlocked.Read(ref cacheHits);
    public long GraphUnavailable => Interlocked.Read(ref graphUnavailable);
    public long CapacityExhausted => Interlocked.Read(ref capacityExhausted);
    public void RecordFallback(WorkspaceFallback reason)
    {
        if (reason == WorkspaceFallback.GraphUnavailable) Interlocked.Increment(ref graphUnavailable);
        else if (reason == WorkspaceFallback.CapacityExhausted) Interlocked.Increment(ref capacityExhausted);
    }
    public void RecordRequest(bool local) { Interlocked.Increment(ref requests); if (local) Interlocked.Increment(ref localHits); }
    public void RecordSearches(long abstractCount, long localCount, long hits)
    { Interlocked.Add(ref abstractSearches, abstractCount); Interlocked.Add(ref localSearches, localCount); Interlocked.Add(ref cacheHits, hits); }
}
