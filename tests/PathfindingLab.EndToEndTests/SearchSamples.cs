using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PathfindingLab.EndToEndTests;

public sealed record BackgroundTicket(long Lane, long Version, int? Window, string Outcome,
    long QueuedTimestamp, long? StartedTimestamp = null, long? FinishedTimestamp = null,
    double? ElapsedMilliseconds = null, bool PendingAtWindowClose = false);

// Queue ownership survives coalescing and window closure; elapsed time measures only builds.
public sealed class BackgroundSamples
{
    private readonly object gate = new();
    private readonly Dictionary<(long Lane, long Version), BackgroundTicket> tickets = new();
    public SearchSamples Timings { get; } = new();
    public bool Queue(long lane, long version, int? window, long timestamp)
    {
        lock (gate) return tickets.TryAdd((lane, version), new(lane, version, window, "pending", timestamp));
    }
    public void Admit(long lane, long version, bool admitted)
    {
        lock (gate)
        {
            if (!tickets.TryGetValue((lane, version), out var row)) return;
            if (!admitted) tickets.Remove((lane, version));
            else if (row.Outcome == "pending") tickets[(lane, version)] = row with { Outcome = "queued" };
        }
    }
    public void Start(long lane, long version, long timestamp)
    {
        lock (gate)
        {
            // A single coalescing lane cannot subsequently execute an older queued revision.
            foreach (var pair in tickets.Where(p => p.Key.Lane == lane && p.Key.Version < version &&
                         p.Value.Outcome is "pending" or "queued").ToArray())
                tickets[pair.Key] = pair.Value with { Outcome = "coalesced" };
            if (tickets.TryGetValue((lane, version), out var row))
                tickets[(lane, version)] = row with { Outcome = "running", StartedTimestamp = timestamp };
        }
    }
    public void Finish(long lane, long version, long timestamp, double elapsedMs, bool succeeded)
    {
        lock (gate) if (tickets.TryGetValue((lane, version), out var row))
            tickets[(lane, version)] = row with { Outcome = succeeded ? "completed" : "failed", FinishedTimestamp = timestamp, ElapsedMilliseconds = elapsedMs };
    }
    public void SettleIdle(long lane, string? failure = null)
    {
        lock (gate) foreach (var pair in tickets.Where(p => p.Key.Lane == lane && p.Value.Outcome is "pending" or "queued").ToArray())
            tickets[pair.Key] = pair.Value with { Outcome = failure == null ? "coalesced" : "not-built: " + failure };
    }
    public void CloseWindow(int window)
    {
        lock (gate) foreach (var pair in tickets.Where(p => p.Value.Window == window && p.Value.Outcome is "pending" or "queued" or "running").ToArray())
            tickets[pair.Key] = pair.Value with { PendingAtWindowClose = true };
    }
    public int Pending { get { lock (gate) return tickets.Values.Count(t => t.Outcome is "pending" or "queued" or "running"); } }
    public BackgroundTicket[] Snapshot() { lock (gate) return tickets.Values.ToArray(); }
    public object Export()
    {
        var rows = Snapshot();
        return new { Pending, Timings = Timings.Export(), TotalTickets = rows.Length,
            Overflowed = rows.Length > SearchSamples.Capacity, Tickets = rows.Take(SearchSamples.Capacity).ToArray() };
    }
}

// Test-only measurements; no game, profiler or Gateway dependency.
public sealed record SearchSample(double ElapsedMilliseconds, double PreparationMilliseconds, bool Succeeded,
    string Source = "unspecified", string? PawnId = null, double RefreshMilliseconds = 0, double GraphMilliseconds = 0, double LockWaitMilliseconds = 0);
public sealed record SearchTotal(string Source, string? PawnId, long Count, long Failed,
    double ElapsedMilliseconds, double PreparationMilliseconds, double RefreshMilliseconds = 0, double GraphMilliseconds = 0, double LockWaitMilliseconds = 0)
{
    public double SearchMilliseconds => ElapsedMilliseconds - PreparationMilliseconds;
}
public sealed record SearchWindowSnapshot(int Pending, int? PendingAtClose, int LateCompletions,
    bool Overflowed, SearchTotal[] Totals, SearchSample[] RawSearches);
public sealed record DoorTimingCoverage(long Workers, long DoorJobs, bool Complete, string[] MismatchedPawns);

public sealed class SearchSamples
{
    public const int Capacity = 4096;
    private readonly List<SearchSample> samples = new();
    private readonly Dictionary<(string Source, string? Pawn), SearchTotal> totals = new();
    private int pending;
    public int Pending => Volatile.Read(ref pending);
    public int? PendingAtClose { get; private set; }
    public int LateCompletions { get; private set; }
    public void Close() { lock (samples) PendingAtClose ??= pending; }
    public void Scheduled() { lock (samples) pending++; }
    public void Completed()
    {
        lock (samples)
        {
            if (pending <= 0) throw new InvalidOperationException("No pending worker.");
            pending--;
            if (PendingAtClose.HasValue) LateCompletions++;
        }
    }
    public bool Overflowed { get; private set; }
    public void Complete(long elapsed, long preparation, long frequency, bool succeeded, string source, string? pawnId, long refresh = 0, long graph = 0, long lockWait = 0)
    {
        lock (samples)
        {
            Add(elapsed, preparation, frequency, succeeded, source, pawnId, refresh, graph, lockWait);
            Completed();
        }
    }
    public SearchWindowSnapshot Export()
    {
        lock (samples) return new(pending, PendingAtClose, LateCompletions, Overflowed, Totals(), Snapshot());
    }

    public void Add(long elapsed, long preparation, long frequency, bool succeeded, string source = "unspecified", string? pawnId = null, long refresh = 0, long graph = 0, long lockWait = 0)
    {
        if (frequency <= 0 || elapsed < 0 || preparation < 0 || preparation > elapsed ||
            refresh < 0 || graph < 0 || lockWait < 0 || refresh > preparation || graph > preparation - refresh || lockWait > elapsed - preparation)
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        lock (samples)
        {
            double elapsedMs = elapsed * 1000d / frequency, preparationMs = preparation * 1000d / frequency;
            var key = (source, pawnId);
            totals.TryGetValue(key, out var previous);
            totals[key] = new SearchTotal(source, pawnId, (previous?.Count ?? 0) + 1,
                (previous?.Failed ?? 0) + (succeeded ? 0 : 1),
                (previous?.ElapsedMilliseconds ?? 0) + elapsedMs, (previous?.PreparationMilliseconds ?? 0) + preparationMs,
                (previous?.RefreshMilliseconds ?? 0) + refresh * 1000d / frequency,
                (previous?.GraphMilliseconds ?? 0) + graph * 1000d / frequency,
                (previous?.LockWaitMilliseconds ?? 0) + lockWait * 1000d / frequency);
            if (samples.Count == Capacity) { Overflowed = true; return; }
            samples.Add(new SearchSample(elapsedMs, preparationMs, succeeded, source, pawnId,
                refresh * 1000d / frequency, graph * 1000d / frequency, lockWait * 1000d / frequency));
        }
    }

    public SearchSample[] Snapshot() { lock (samples) return samples.ToArray(); }
    public SearchTotal[] Totals() { lock (samples) return totals.Values.OrderBy(x => x.Source).ThenBy(x => x.PawnId).ToArray(); }
    public DoorTimingCoverage DoorCoverage()
    {
        lock (samples)
        {
            var work = totals.Values.Where(t => t.Source is "vanilla-search" or "experimental-search" or "vanilla-doors").ToArray();
            long workers = work.Where(t => t.Source != "vanilla-doors").Sum(t => t.Count);
            long doors = work.Where(t => t.Source == "vanilla-doors").Sum(t => t.Count);
            var mismatches = work.GroupBy(t => t.PawnId).Where(g =>
                g.Where(t => t.Source == "vanilla-doors").Sum(t => t.Count) != g.Where(t => t.Source != "vanilla-doors").Sum(t => t.Count))
                .Select(g => g.Key ?? "<pawnless>").ToArray();
            return new(workers, doors, workers > 0 && pending == 0 && mismatches.Length == 0 && work.All(t => t.Failed == 0), mismatches);
        }
    }
}

public sealed record MovementSample(string PawnId, string Outcome, double? FirstMovementMilliseconds);
public sealed record MovementLatencyTotal(long Count, double SumMilliseconds, double MaximumMilliseconds);

// Main-thread only. Values are native StartPath-to-cost-payment latencies, not arrival times.
public sealed class MovementSamples
{
    private readonly Dictionary<string, (long Started, long Generation)> pending = new();
    private readonly List<MovementSample> samples = new();
    private readonly Dictionary<string, long> outcomes = new();
    private bool closed;
    private long sequence;
    public bool Overflowed { get; private set; }
    public int Pending => pending.Count;
    public MovementLatencyTotal LatencyTotal { get; private set; } = new(0, 0, 0);
    public IReadOnlyDictionary<string, long> Outcomes => new Dictionary<string, long>(outcomes);

    public long Generation(string pawnId) => pending.TryGetValue(pawnId, out var value) ? value.Generation : 0;
    public void FinishGeneration(string pawnId, long generation, long timestamp, long frequency, string outcome = "moved")
    {
        if (generation != 0 && Generation(pawnId) == generation) Finish(pawnId, timestamp, frequency, outcome);
    }
    public void Start(string pawnId, long timestamp, bool alreadyMoving)
    {
        if (closed) throw new InvalidOperationException("Movement window is closed.");
        if (pending.Remove(pawnId)) Add(new MovementSample(pawnId, "superseded", null));
        if (alreadyMoving) Add(new MovementSample(pawnId, "already-moving", null));
        else pending.Add(pawnId, (timestamp, ++sequence));
    }
    public void Finish(string pawnId, long timestamp, long frequency, string outcome)
    {
        if (frequency <= 0) throw new ArgumentOutOfRangeException(nameof(frequency));
        if (!pending.TryGetValue(pawnId, out var start)) return;
        if (timestamp < start.Started) throw new ArgumentOutOfRangeException(nameof(timestamp));
        pending.Remove(pawnId);
        Add(new MovementSample(pawnId, outcome, outcome == "moved" ? (timestamp - start.Started) * 1000d / frequency : null));
    }
    public void Close(long timestamp, long frequency)
    {
        foreach (var pawn in pending.Keys.ToArray()) Finish(pawn, timestamp, frequency, "window-ended");
        closed = true;
    }
    public MovementSample[] Snapshot() => samples.ToArray();
    private void Add(MovementSample sample)
    {
        outcomes.TryGetValue(sample.Outcome, out long count);
        outcomes[sample.Outcome] = count + 1;
        if (sample.FirstMovementMilliseconds is { } latency)
            LatencyTotal = new MovementLatencyTotal(LatencyTotal.Count + 1,
                LatencyTotal.SumMilliseconds + latency, Math.Max(LatencyTotal.MaximumMilliseconds, latency));
        if (samples.Count < SearchSamples.Capacity) samples.Add(sample);
        else Overflowed = true;
    }
}
