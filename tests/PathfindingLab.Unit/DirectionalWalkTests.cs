using NUnit.Framework;
using PathfindingLab.Core;

namespace PathfindingLab.Unit;

public class DirectionalWalkTests
{
    [Test]
    public void Mountain_and_herd_boundary_turn_the_walker_without_revisiting()
    {
        const int width = 40, root = 20 * width + 20;
        var heading = new WanderHeading { Direction = 0 };
        var walk = DirectionalWalk.Build(width, 40, root + 4, 10, heading,
            (_, to) => to % width < 25 && DirectionalWalk.WithinCircle(width, to, root, 5), _ => 0);
        Assert.That(walk.Count, Is.GreaterThanOrEqualTo(4));
        Assert.That(walk.All(c => c % width < 25 && DirectionalWalk.WithinCircle(width, c, root, 5)), Is.True);
        Assert.That(walk.Distinct().Count(), Is.EqualTo(walk.Count));
        Assert.That(walk[^1], Is.Not.EqualTo(walk[0]));
    }

    [Test]
    public void Trapped_walk_is_bounded_and_changes_its_heading()
    {
        int checks = 0;
        var heading = new WanderHeading();
        var walk = DirectionalWalk.Build(20, 20, 210, 10, heading, (_, _) => { checks++; return false; }, _ => 0);
        Assert.That(walk.Count, Is.EqualTo(1));
        Assert.That(checks, Is.EqualTo(8));
        Assert.That(heading.Direction, Is.EqualTo(4));
    }

    [Test]
    public void An_open_walk_keeps_heading_and_returns_only_adjacent_cells()
    {
        var state = new WanderHeading { Direction = 0 };
        var walk = DirectionalWalk.Build(40, 40, 20 * 40 + 20, 8, state, (_, _) => true, _ => 0);
        Assert.That(walk.Count, Is.EqualTo(9));
        Assert.That(walk[^1], Is.EqualTo(20 * 40 + 28));
        Assert.That(walk.Zip(walk.Skip(1)).All(p => p.Second == p.First + 1), Is.True);
    }
}
