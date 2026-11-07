using NUnit.Framework;
using PathfindingLab.Core;

namespace PathfindingLab.Unit;

public class WorkspaceLeaseTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void An_active_build_cannot_publish_after_supersession_or_disposal_and_retained_readers_finish(bool dispose)
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var latestEntered = new ManualResetEventSlim();
        using var latestRelease = new ManualResetEventSlim();
        var queue = new Queue<Action>();
        int builds = 0;
        using var pool = new HierarchyProfiles(queue.Enqueue, () =>
        {
            int built = Interlocked.Increment(ref builds);
            if (built == 2) { entered.Set(); if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException(); }
            if (built == 3) { latestEntered.Set(); if (!latestRelease.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException(); }
        });
        var changes = new ChunkChanges(64, 32);
        var initial = GeometrySnapshot.Capture(null, changes.Capture(), _ => 0, _ => false);
        pool.Prepare(initial); queue.Dequeue()();
        Assert.That(pool.TryAcquire(initial, out var retained, out _), Is.True);
        changes.MarkCell(10, 0);
        var second = GeometrySnapshot.Capture(initial, changes.Capture(), i => i == 10 ? 10000 : 0, _ => false);
        pool.Prepare(second);
        var worker = Task.Run(queue.Dequeue());
        try
        {
            Assert.That(entered.Wait(TimeSpan.FromSeconds(10)), Is.True);
            Assert.That(pool.TryAcquire(initial, out _, out _), Is.False);
            Assert.That(pool.TryAcquire(second, out _, out _), Is.False);
            if (dispose) pool.Dispose();
            else
            {
                changes.MarkCell(11, 0);
                var latest = GeometrySnapshot.Capture(second, changes.Capture(), i => i is 10 or 11 ? 10000 : 0, _ => false);
                pool.Prepare(latest);
                release.Set();
                Assert.That(latestEntered.Wait(TimeSpan.FromSeconds(10)), Is.True);
                Assert.That(pool.TryAcquire(second, out _, out _), Is.False, "Superseded completed build must not become current.");
                Assert.That(pool.TryAcquire(latest, out _, out _), Is.False, "Latest build has not published yet.");
                latestRelease.Set();
                Assert.That(worker.Wait(TimeSpan.FromSeconds(10)), Is.True);
                Assert.That(pool.TryAcquire(latest, out var updated, out _), Is.True);
                using (updated) Assert.That(updated.Router.Guide.CanStep(9, 10), Is.False);
            }
            var plan = retained.Router.Guide.Plan(0, 63, new MoveCosts(13, 18));
            Assert.That(retained.Router.Guide.Refine(plan!, 0, 50), Is.Not.Null);
            Assert.That(retained.Router.Guide.CanStep(9, 10), Is.True);
        }
        finally
        {
            release.Set(); latestRelease.Set();
            Assert.That(worker.Wait(TimeSpan.FromSeconds(10)), Is.True);
            retained.Dispose();
        }
        Assert.That(pool.Active, Is.Zero);
        if (dispose) Assert.That(pool.TryAcquire(second, out _, out _), Is.False, "Disposed completed build must not publish.");
    }

    private sealed class Grid(int width, int extra = 0) : IRequestGrid
    {
        public int Cost(int cell) => extra;
        public bool CanStep(int from, int to) => Math.Abs(from % width - to % width) <= 1 && Math.Abs(from / width - to / width) <= 1;
    }
    private static GeometrySnapshot Snapshot(int width = 64, int height = 32) =>
        GeometrySnapshot.Capture(null, new ChunkChanges(width, height).Capture(), _ => 0, _ => false);

    [Test]
    public void Concurrent_requests_share_only_the_graph_and_retain_their_own_results()
    {
        using var pool = new HierarchyProfiles(build => build());
        var snapshot = Snapshot(); pool.Prepare(snapshot);
        using var barrier = new Barrier(HierarchyProfiles.WorkspaceCapacity);
        var jobs = Enumerable.Range(0, HierarchyProfiles.WorkspaceCapacity).Select(i => Task.Factory.StartNew(() =>
        {
            Assert.That(pool.TryAcquire(snapshot, out var lease, out _), Is.True);
            using (lease)
            {
                Assert.That(barrier.SignalAndWait(TimeSpan.FromSeconds(10)), Is.True);
                var path = lease.Router.Find(new Grid(64, i), i * 64, new[] { i * 64 + 63 }, new MoveCosts(13, 18), 50, new RequestCursor());
                Assert.That(barrier.SignalAndWait(TimeSpan.FromSeconds(10)), Is.True);
                Assert.That(path, Is.Not.Null);
                Assert.That(path!.Value.Cells.Span[0], Is.EqualTo(i * 64));
                return (lease.Index, lease.Router, lease.Router.Guide.Graph);
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
        Assert.That(Task.WaitAll(jobs, TimeSpan.FromSeconds(30)), Is.True);
        Assert.That(jobs.Select(j => j.Result.Index).Distinct().Count(), Is.EqualTo(8));
        Assert.That(jobs.Select(j => j.Result.Router).Distinct().Count(), Is.EqualTo(8));
        Assert.That(jobs.Select(j => j.Result.Graph).Distinct().Count(), Is.EqualTo(1));
        Assert.That(pool.PeakActive, Is.EqualTo(8));
        Assert.That(pool.Active, Is.Zero);
    }

    [Test]
    public void Capacity_fallback_is_distinct_and_copied_old_leases_cannot_free_a_new_owner()
    {
        var pending = new Queue<Action>();
        using var pool = new HierarchyProfiles(pending.Enqueue);
        var snapshot = Snapshot(); pool.Prepare(snapshot);
        Assert.That(pool.TryAcquire(snapshot, out _, out var cold), Is.False);
        Assert.That(cold, Is.EqualTo(WorkspaceFallback.GraphUnavailable));
        pending.Dequeue()();
        var leases = new HierarchyProfiles.WorkspaceLease[8];
        for (int i = 0; i < leases.Length; i++) Assert.That(pool.TryAcquire(snapshot, out leases[i], out _), Is.True);
        Assert.That(pool.TryAcquire(snapshot, out _, out var full), Is.False);
        Assert.That(full, Is.EqualTo(WorkspaceFallback.CapacityExhausted));
        var copy = leases[0]; copy.Dispose();
        Assert.That(pool.TryAcquire(snapshot, out leases[0], out _), Is.True);
        copy.Dispose();
        Assert.That(pool.Active, Is.EqualTo(8));
        Assert.That(pool.TryAcquire(snapshot, out _, out _), Is.False);
        pool.Dispose();
        Assert.That(pool.TryAcquire(snapshot, out _, out var disposed), Is.False);
        Assert.That(disposed, Is.EqualTo(WorkspaceFallback.GraphUnavailable));
        foreach (var lease in leases) lease.Dispose();
        Assert.That(pool.Active, Is.Zero);
        Assert.That(pool.GraphUnavailable, Is.EqualTo(2));
        Assert.That(pool.CapacityExhausted, Is.EqualTo(2));
    }

    [Test]
    public void Warm_acquire_release_allocates_nothing_and_readers_do_not_copy_million_cell_geometry()
    {
        using var pool = new HierarchyProfiles(build => build());
        var snapshot = Snapshot(1000, 1000); pool.Prepare(snapshot);
        long before = GC.GetAllocatedBytesForCurrentThread();
        pool.TryAcquire(snapshot, out var first, out _); first.Dispose();
        long firstAllocation = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(firstAllocation, Is.LessThan(64000), "No map-sized cost/component/corner copy per reader.");
        for (int i = 0; i < 100; i++) { pool.TryAcquire(snapshot, out var lease, out _); lease.Dispose(); }
        before = GC.GetAllocatedBytesForCurrentThread();
        bool success = true;
        for (int i = 0; i < 1000; i++) { success &= pool.TryAcquire(snapshot, out var lease, out _); lease.Dispose(); }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(success, Is.True);
        Assert.That(allocated, Is.Zero);
    }

    [Test]
    public void A_cursor_continues_on_another_worker_without_replanning_and_isolated_native_costs_are_revalidated()
    {
        using var pool = new HierarchyProfiles(build => build());
        var snapshot = Snapshot(); pool.Prepare(snapshot);
        pool.TryAcquire(snapshot, out var first, out _);
        pool.TryAcquire(snapshot, out var second, out _);
        using (first) using (second)
        {
            var cursor = new RequestCursor();
            var route = first.Router.Find(new Grid(64), 0, new[] { 63 }, new MoveCosts(13, 18), 25, cursor)!.Value;
            var continued = second.Router.Find(new Grid(64, 7), route.Cells.Span[12], new[] { 63 }, new MoveCosts(13, 18), 25, cursor)!.Value;
            Assert.That(second.Router.Guide.AbstractSearches, Is.Zero);
            Assert.That(continued.Cost, Is.EqualTo((continued.Cells.Length - 1) * 20L));
        }
    }
}
