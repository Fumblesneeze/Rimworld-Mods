using NUnit.Framework;
using PathfindingLab.Core;

namespace PathfindingLab.Unit;

public class HierarchyProfilesTests
{
    [Test]
    public void Captured_notifications_and_unchanged_preparation_have_zero_warm_managed_allocation()
    {
        var changes = new ChunkChanges(64, 32);
        var profiles = new HierarchyProfiles(build => build());
        changes.MarkCell(10, 10);
        var stamp = changes.Capture();
        var snapshot = GeometrySnapshot.Capture(null, stamp, _ => 0, _ => false);
        profiles.Prepare(snapshot);
        for (int i = 0; i < 100; i++) { changes.Capture(); profiles.Prepare(snapshot); profiles.IsReady(snapshot); }
        long before = GC.GetAllocatedBytesForCurrentThread();
        bool ready = true, same = true;
        for (int i = 0; i < 1000; i++)
        {
            same &= ReferenceEquals(changes.Capture(), stamp);
            profiles.Prepare(snapshot);
            ready &= profiles.IsReady(snapshot);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(same && ready, Is.True);
        Assert.That(allocated, Is.Zero);
        profiles.Dispose();
    }

    [Test]
    public void Proactive_edits_coalesce_for_absent_cohorts_and_disposal_cannot_restart_them()
    {
        var pending = new Queue<Action>();
        var profiles = new HierarchyProfiles(pending.Enqueue);
        var changes = new ChunkChanges(64, 32);
        var initial = GeometrySnapshot.Capture(null, changes.Capture(), _ => 0, _ => false);
        profiles.Prepare(initial);
        while (pending.TryDequeue(out var build)) build();
        changes.MarkCell(10, 10);
        var edit = GeometrySnapshot.Capture(initial, changes.Capture(), i => i == 650 ? 10000 : 0, _ => false);
        profiles.Prepare(edit);
        Assert.That(profiles.IsReady(initial), Is.False);
        Assert.That(profiles.IsReady(edit), Is.False);
        changes.MarkCell(11, 10);
        var latest = GeometrySnapshot.Capture(edit, changes.Capture(), i => i is 650 or 651 ? 10000 : 0, _ => false);
        profiles.Prepare(latest);
        Assert.That(pending.Count, Is.EqualTo(1));
        while (pending.TryDequeue(out var build)) build();
        Assert.That(profiles.IsReady(latest), Is.True);
        profiles.Prepare(initial);
        Assert.That(pending, Is.Empty);
        Assert.That(profiles.IsReady(latest), Is.True);
        foreach (RoutingCohort cohort in Enum.GetValues<RoutingCohort>())
        {
            Assert.That(profiles.Get(cohort).Requests, Is.Zero);
            Assert.That(profiles.TryAcquire(latest, out var lease, out _), Is.True);
            using (lease) Assert.That(lease.Router.Guide.CanStep(649, 650), Is.False);
            Assert.That(profiles.Background(cohort).CompletedBuilds, Is.EqualTo(2));
        }
        profiles.Dispose();
        profiles.Prepare(latest);
        Assert.That(profiles.IsReady(latest), Is.False);
        Assert.That(pending, Is.Empty);
        Assert.Throws<ObjectDisposedException>(() => profiles.Background(RoutingCohort.Hostile));
    }

    [Test]
    public void Disposing_before_queued_preparation_prevents_publication_or_fresh_lanes()
    {
        var pending = new Queue<Action>();
        var profiles = new HierarchyProfiles(pending.Enqueue);
        var snapshot = GeometrySnapshot.Capture(null, new ChunkChanges(32, 32).Capture(), _ => 0, _ => false);
        profiles.Prepare(snapshot);
        profiles.Dispose();
        while (pending.TryDequeue(out var build)) build();
        profiles.Prepare(snapshot);
        Assert.That(profiles.IsReady(snapshot), Is.False);
        Assert.That(profiles.IsRunning, Is.False);
        Assert.That(pending, Is.Empty);
    }

    [Test]
    public void Prepare_builds_every_cohort_before_any_path_request_and_reuses_unchanged_geometry()
    {
        var pending = new Queue<Action>();
        var profiles = new HierarchyProfiles(pending.Enqueue);
        var snapshot = GeometrySnapshot.Capture(null, new ChunkChanges(64, 32).Capture(), _ => 0, _ => false);
        profiles.Prepare(snapshot);
        Assert.That(profiles.IsReady(snapshot), Is.False);
        Assert.That(pending.Count, Is.EqualTo(1), "All cohorts must share one eagerly scheduled immutable graph.");
        while (pending.TryDequeue(out var build)) build();
        Assert.That(profiles.IsReady(snapshot), Is.True);
        foreach (RoutingCohort cohort in Enum.GetValues<RoutingCohort>())
        {
            Assert.That(profiles.Get(cohort).Requests, Is.Zero);
            Assert.That(profiles.Background(cohort).CompletedBuilds, Is.EqualTo(1));
            Assert.That(profiles.Background(cohort), Is.SameAs(profiles.Background(RoutingCohort.Player)));
        }
        profiles.Prepare(snapshot);
        Assert.That(pending, Is.Empty);
        profiles.Dispose();
    }

    [Test]
    public void Clipped_rectangles_and_full_invalidations_cover_edge_chunks()
    {
        var changes = new ChunkChanges(35, 34);
        var snapshot = GeometrySnapshot.Capture(null, changes.Capture(), _ => 0, _ => false);
        var builder = new PortalRouter(snapshot);
        builder.Publish();
        changes.MarkRect(32, 32, 100, 100);
        snapshot = GeometrySnapshot.Capture(snapshot, changes.Capture(), _ => 10000, _ => true);
        builder.ApplySnapshot(snapshot);
        Assert.That(snapshot.CellsCopied, Is.EqualTo(6));
        Assert.That(new PortalRouter(builder.Publish()).CanStep(0, 1), Is.True);
        changes.MarkAll();
        snapshot = GeometrySnapshot.Capture(snapshot, changes.Capture(), _ => 10000, _ => true);
        builder.ApplySnapshot(snapshot);
        Assert.That(snapshot.CellsCopied, Is.EqualTo(1190));
        Assert.That(new PortalRouter(builder.Publish()).CanStep(0, 1), Is.False);
    }

    [Test]
    public void A_local_edit_refreshes_one_chunk_without_rebuilding_or_replaying_old_stamps()
    {
        const int width = 100, height = 70;
        var costs = new int[width * height];
        var background = new BackgroundHierarchy(build => build());
        var changes = new ChunkChanges(width, height);
        var initial = GeometrySnapshot.Capture(null, changes.Capture(), i => costs[i], _ => false);
        background.Request(initial);
        costs[10 * width + 10] = 10000;
        changes.MarkCell(10, 10);
        var updated = GeometrySnapshot.Capture(initial, changes.Capture(), i => costs[i], _ => false);
        background.Request(updated);
        Assert.That(updated.CellsCopied, Is.EqualTo(1024));
        background.Request(initial);
        Assert.That(background.TryGet(updated, out var graph), Is.True);
        Assert.That(new PortalRouter(graph!).CanStep(10 * width + 9, 10 * width + 10), Is.False);
        Assert.That(graph!.ChangedChunks, Is.LessThan(9));
        Assert.That(background.CompletedBuilds, Is.EqualTo(2));
    }

    [Test]
    public void Cohorts_share_graph_and_reuse_workspace_without_duplicating_topology()
    {
        using var profiles = new HierarchyProfiles(build => build());
        var snapshot = GeometrySnapshot.Capture(null, new ChunkChanges(64, 32).Capture(), _ => 0, _ => false);
        profiles.Prepare(snapshot);
        Assert.That(profiles.TryAcquire(snapshot, out var first, out _), Is.True);
        var graph = first.Router.Guide.Graph;
        foreach (RoutingCohort cohort in Enum.GetValues<RoutingCohort>())
        {
            Assert.That(profiles.TryAcquire(snapshot, out var next, out _), Is.True);
            using (next)
            {
                Assert.That(next.Router, Is.Not.SameAs(first.Router));
                Assert.That(next.Router.Guide.Graph, Is.SameAs(graph));
            }
            Assert.That(profiles.Background(cohort).CompletedBuilds, Is.EqualTo(1));
        }
        first.Dispose();
        Assert.That(profiles.TryAcquire(snapshot, out var reused, out _), Is.True);
        using (reused) Assert.That(reused.Router, Is.SameAs(first.Router));
    }
}
