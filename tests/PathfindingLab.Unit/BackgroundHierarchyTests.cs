using NUnit.Framework;
using PathfindingLab.Core;

namespace PathfindingLab.Unit;

public class BackgroundHierarchyTests
{
    [Test]
    public void Unchanged_million_cell_refresh_allocates_metadata_not_chunk_payloads()
    {
        var changes = new ChunkChanges(1000, 1000);
        Func<int, int> cost = _ => 0;
        Func<int, bool> corner = _ => false;
        var first = GeometrySnapshot.Capture(null, changes.Capture(), cost, corner);
        changes.MarkAll();
        var stamp = changes.Capture();
        long before = GC.GetAllocatedBytesForCurrentThread();
        var unchanged = GeometrySnapshot.Capture(first, stamp, cost, corner);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        TestContext.Progress.WriteLine("Unchanged million-cell capture allocated bytes: " + allocated);
        Assert.That(unchanged.CellsCopied, Is.EqualTo(1000000));
        Assert.That(unchanged.Version, Is.EqualTo(first.Version));
        // One chunk-reference table plus snapshot metadata; no 5KiB payload per dirty chunk.
        Assert.That(allocated, Is.LessThan(64L * stamp.Versions.Length + 1024));
    }

    [Test]
    public void Dirty_notifications_without_geometry_changes_keep_the_ready_guide_usable()
    {
        var queue = new Queue<Action>();
        var changes = new ChunkChanges(64, 32);
        var background = new BackgroundHierarchy(queue.Enqueue);
        var first = GeometrySnapshot.Capture(null, changes.Capture(), _ => 0, _ => false);
        background.Request(first); queue.Dequeue()();
        changes.MarkCell(5, 5);
        var unchanged = GeometrySnapshot.Capture(first, changes.Capture(), _ => 0, _ => false);
        Assert.That(unchanged.Version, Is.EqualTo(first.Version));
        Assert.That(background.Request(unchanged), Is.False);
        Assert.That(queue, Is.Empty);
        Assert.That(background.TryGet(unchanged, out _), Is.True);
        Assert.That(GeometrySnapshot.Capture(unchanged, changes.Capture(), _ => throw new Exception("already acknowledged"), _ => false), Is.SameAs(unchanged));
        changes.MarkCell(5, 5);
        var changed = GeometrySnapshot.Capture(unchanged, changes.Capture(), i => i == 5 ? 3 : 0, _ => false);
        Assert.That(changed.Version, Is.GreaterThan(unchanged.Version));
        Assert.That(background.Request(changed), Is.True);
        Assert.That(background.TryGet(first, out _), Is.False);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void Copy_on_write_preserves_old_geometry_and_reads_clipped_chunks_once(bool changeCost, bool changeCorner)
    {
        const int width = 33, height = 35, edited = width * height - 1;
        var changes = new ChunkChanges(width, height);
        var first = GeometrySnapshot.Capture(null, changes.Capture(), i => i % 97, i => i % 11 == 0);
        changes.MarkAll();
        var reads = new int[width * height]; var cornerReads = new int[reads.Length];
        var next = GeometrySnapshot.Capture(first, changes.Capture(),
            i => { reads[i]++; return i % 97 + (changeCost && i == edited ? 7 : 0); },
            i => { cornerReads[i]++; return (i % 11 == 0) ^ (changeCorner && i == edited); });
        Assert.That(next.Version, Is.EqualTo(first.Version + 1));
        Assert.That(reads.All(n => n == 1) && cornerReads.All(n => n == 1), Is.True);
        for (int i = 0; i < reads.Length; i++)
        {
            Assert.That(first.Cost(i), Is.EqualTo(i % 97));
            Assert.That(first.Corner(i), Is.EqualTo(i % 11 == 0));
            Assert.That(next.Cost(i), Is.EqualTo(i % 97 + (changeCost && i == edited ? 7 : 0)));
            Assert.That(next.Corner(i), Is.EqualTo((i % 11 == 0) ^ (changeCorner && i == edited)));
        }
    }

    [Test]
    public void Edits_coalesce_and_only_the_latest_owned_revision_becomes_usable()
    {
        var queue = new Queue<Action>();
        var changes = new ChunkChanges(64, 32);
        var background = new BackgroundHierarchy(queue.Enqueue);
        var first = GeometrySnapshot.Capture(null, changes.Capture(), _ => 0, _ => false);
        background.Request(first);
        queue.Dequeue()();
        changes.MarkCell(5, 5);
        var second = GeometrySnapshot.Capture(first, changes.Capture(), _ => 10000, _ => true);
        Assert.That(second.CellsCopied, Is.EqualTo(1024));
        background.Request(second);
        changes.MarkCell(40, 5);
        var third = GeometrySnapshot.Capture(second, changes.Capture(), _ => 10000, _ => true);
        background.Request(third);
        background.Request(first); // old native batch cannot roll back a pending revision
        Assert.That(queue.Count, Is.EqualTo(1));
        Assert.That(background.TryGet(first, out _), Is.False);
        Assert.That(background.TryGet(third, out _), Is.False);
        queue.Dequeue()();
        Assert.That(background.TryGet(second, out _), Is.False);
        Assert.That(background.TryGet(third, out var graph), Is.True);
        Assert.That(new PortalRouter(graph!).Plan(0, 63, new MoveCosts(13, 18)), Is.Null);
        Assert.That(background.CompletedBuilds, Is.EqualTo(2));
        Assert.That(first.Cost(0), Is.Zero);
        Assert.That(GeometrySnapshot.Capture(third, first.Stamp, _ => throw new Exception(), _ => false), Is.SameAs(third));
    }

    [Test]
    public void Disposing_a_queued_lane_prevents_late_publication()
    {
        Action? queued = null;
        var background = new BackgroundHierarchy(action => queued = action);
        var snapshot = GeometrySnapshot.Capture(null, new ChunkChanges(4, 4).Capture(), _ => 0, _ => false);
        background.Request(snapshot);
        background.Dispose();
        queued!();
        Assert.That(background.TryGet(snapshot, out _), Is.False);
        Assert.That(background.CompletedBuilds, Is.Zero);
    }

    [Test]
    public void Scheduler_failure_is_observable_and_does_not_retry_the_same_revision()
    {
        int attempts = 0;
        var snapshot = GeometrySnapshot.Capture(null, new ChunkChanges(4, 4).Capture(), _ => 0, _ => false);
        var background = new BackgroundHierarchy(_ => { attempts++; throw new InvalidOperationException("offline"); });
        Assert.DoesNotThrow(() => background.Request(snapshot));
        background.Request(snapshot);
        Assert.That(background.TryGet(snapshot, out _), Is.False);
        Assert.That(attempts, Is.EqualTo(1));
        Assert.That(background.Failure, Does.Contain("offline"));
    }

    [Test]
    public void A_request_never_builds_or_waits_and_owned_snapshot_survives_source_changes()
    {
        var tasks = new Queue<Action>();
        var changes = new ChunkChanges(64, 32);
        var costs = new int[2048];
        var snapshot = GeometrySnapshot.Capture(null, changes.Capture(), i => costs[i], _ => false);
        var background = new BackgroundHierarchy(tasks.Enqueue);
        background.Request(snapshot);
        Assert.That(background.TryGet(snapshot, out _), Is.False);
        Assert.That(tasks.Count, Is.EqualTo(1));
        Array.Fill(costs, 10000); // Native memory can now be changed/disposed without affecting the build.
        tasks.Dequeue()();
        Assert.That(background.TryGet(snapshot, out var graph), Is.True);
        Assert.That(new PortalRouter(graph!).Plan(0, 63, new MoveCosts(13, 18)), Is.Not.Null);
        Assert.That(background.CompletedBuilds, Is.EqualTo(1));
    }
}
