using NUnit.Framework;
using PathfindingLab.Core;

namespace PathfindingLab.Unit;

public class ImmutableGraphTests
{
    [Test]
    public void Equal_dimensions_do_not_make_different_maps_interchangeable()
    {
        var first = GeometrySnapshot.Capture(null, new ChunkChanges(64, 32).Capture(), _ => 0, _ => false);
        var changes = new ChunkChanges(64, 32);
        var other = GeometrySnapshot.Capture(null, changes.Capture(), _ => 0, _ => false);
        changes.MarkAll();
        other = GeometrySnapshot.Capture(other, changes.Capture(), _ => 1, _ => false);
        var builder = new PortalRouter(first);
        builder.Publish();
        Assert.Throws<ArgumentException>(() => builder.ApplySnapshot(other));
        using var pool = new HierarchyProfiles(build => build());
        pool.Prepare(first);
        Assert.That(pool.TryAcquire(other, out _, out var reason), Is.False);
        Assert.That(reason, Is.EqualTo(WorkspaceFallback.GraphUnavailable));
        Assert.Throws<ArgumentException>(() => pool.Prepare(other));
    }

    [Test]
    public void Publishing_an_edit_does_not_mutate_an_existing_reader()
    {
        var changes = new ChunkChanges(64, 32);
        var initial = GeometrySnapshot.Capture(null, changes.Capture(), _ => 0, _ => false);
        var builder = new PortalRouter(initial);
        var first = builder.Publish();
        var reader = new PortalRouter(first);
        var plan = reader.Plan(0, 63, new MoveCosts(13, 18))!;
        changes.MarkCell(10, 0);
        var changed = GeometrySnapshot.Capture(initial, changes.Capture(), i => i == 10 ? 10000 : 0, _ => false);
        builder.ApplySnapshot(changed);
        var second = builder.Publish();
        var next = new PortalRouter(second);
        Assert.That(reader.CanStep(9, 10), Is.True, "A published reader must not see later builder writes.");
        Assert.That(next.CanStep(9, 10), Is.False);
        Assert.That(reader.Refine(plan, 0, 50), Is.Not.Null);
        Assert.That(next.Refine(plan, 0, 50), Is.Null, "A changed dependency invalidates the old plan.");
    }
}
