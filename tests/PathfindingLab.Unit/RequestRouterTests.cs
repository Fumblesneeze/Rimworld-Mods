using NUnit.Framework;
using PathfindingLab.Core;

namespace PathfindingLab.Unit;

public class RequestRouterTests
{
    [Test]
    public void Repair_memory_survives_other_cursors_and_renews_without_tail_copies()
    {
        var grid = new Grid(1000, 1) { Rule = (a, b) => b == a + 1 };
        var router = new RequestRouter(1000, 1, grid.Costs, new bool[1000]);
        var a = new RequestCursor();
        var b = new RequestCursor();
        var movement = new MoveCosts(0, 0); // Request-only search, no optimistic reverse proof.
        ReadOnlySpan<int> goals = stackalloc int[] { 999 };
        var first = router.Find(grid, 0, goals, movement, 25, a)!.Value;
        var independent = first.Cells.ToArray();
        router.Find(grid, 40, stackalloc int[] { 800 }, movement, 0, b);
        Assert.That(first.Cells.Span.SequenceEqual(independent), Is.True);
        long before = GC.GetAllocatedBytesForCurrentThread();
        int checksum = 0;
        for (int i = 0; i < 100; i++)
            checksum += router.Find(grid, 12, goals, movement, 25, a)!.Value.Cells.Span[^1];
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        TestContext.Out.WriteLine($"100 repair renewals: {allocated} bytes; checksum {checksum}");
        Assert.That(checksum, Is.EqualTo(3700));
        Assert.That(allocated, Is.Zero);
        Assert.That(router.Find(grid, 12, goals, movement, 0, a)!.Value.Cells.Span[^1], Is.EqualTo(999));
    }

    [Test]
    public void Reused_local_nodes_and_borrowed_goals_do_not_leak_between_windows()
    {
        var grid = new Grid(128, 128);
        var router = new RequestRouter(128, 128, grid.Costs, new bool[128 * 128]);
        var movement = new MoveCosts(13, 18);
        Span<int> goals = stackalloc int[1];
        for (int i = 0; i < 40; i++)
        {
            int start = i % 2 == 0 ? 0 : 120 * 128 + 120;
            goals[0] = i % 2 == 0 ? 20 : 100 * 128 + 100;
            grid.Costs[goals[0]] = i % 3 == 0 ? 10000 : 7;
            var route = router.TryLocal(grid, start, goals, movement, 0);
            if (i % 3 == 0) Assert.That(route, Is.Null);
            else
            {
                Assert.That(route, Is.Not.Null);
                Assert.That(route!.Value.Cells.Span[0], Is.EqualTo(start));
                Assert.That(route.Value.Cells.Span[^1], Is.EqualTo(goals[0]));
                Assert.That(route.Value.Cost, Is.EqualTo(i % 2 == 0 ? 267 : 367));
            }
        }
    }

    [Test]
    public void Warm_local_requests_reuse_storage_without_managed_allocations()
    {
        var grid = new Grid(1000, 1000);
        var router = new RequestRouter(1000, 1000, grid.Costs, new bool[1000000]);
        int[] goals = [3020];
        var movement = new MoveCosts(13, 18);
        for (int i = 0; i < 100; i++)
            router.TryLocal(grid, 0, goals, movement, 25);
        long before = GC.GetAllocatedBytesForCurrentThread();
        long total = 0;
        for (int i = 0; i < 100; i++)
            total += router.TryLocal(grid, 0, goals, movement, 25)!.Value.Cost;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        TestContext.Out.WriteLine($"100 warmed local requests: {allocated} bytes; cost checksum {total}");
        Assert.That(total, Is.EqualTo(27500));
        Assert.That(allocated, Is.Zero);
    }

    [Test]
    public void Local_expansion_budget_escalates_even_when_the_entire_route_is_inside_its_radius()
    {
        var grid = new Grid(32, 32);
        Array.Fill(grid.Costs, 10000);
        for (int z = 0; z < 32; z += 2)
        {
            for (int x = 0; x < 32; x++) grid.Costs[z * 32 + x] = 0;
            if (z + 1 < 32) grid.Costs[(z + 1) * 32 + (z % 4 == 0 ? 31 : 0)] = 0;
        }
        Assert.That(new RequestRouter(32, 32, grid.Costs, new bool[1024]).TryLocal(grid, 0, new[] { 960 }, new MoveCosts(13, 18), 25), Is.Null);
        var router = new RequestRouter(32, 32, grid.Costs, new bool[1024]);
        var cursor = new RequestCursor();
        int current = 0, steps = 0;
        for (int i = 0; i < 40 && current != 960; i++)
        {
            var next = router.Find(grid, current, new[] { 960 }, new MoveCosts(13, 18), 25, cursor);
            Assert.That(next, Is.Not.Null);
            Assert.That(next!.Value.Cells.Length, Is.InRange(2, 26));
            steps += next.Value.Cells.Length - 1;
            current = next.Value.Cells.Span[^1];
        }
        Assert.That(current, Is.EqualTo(960));
        Assert.That(steps, Is.GreaterThan(500));
    }

    [Test]
    public void Local_budget_miss_escalates_to_a_complete_route_outside_the_local_radius()
    {
        const int width = 64, height = 80;
        var grid = new Grid(width, height);
        for (int z = 0; z < 50; z++) grid.Costs[z * width + 20] = 10000;
        int start = 10 * width + 10, goal = 10 * width + 30;
        Assert.That(new RequestRouter(width, height, grid.Costs, new bool[width * height]).TryLocal(grid, start, new[] { goal }, new MoveCosts(13, 18), 0), Is.Null);
        var router = new RequestRouter(width, height, new int[width * height], new bool[width * height]);
        var result = router.Find(grid, start, new[] { goal }, new MoveCosts(13, 18), 0, new RequestCursor());
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Cells.Span[^1], Is.EqualTo(goal));
        Assert.That(result.Value.Cells.ToArray().Any(cell => cell / width >= 50), Is.True);
    }

    [Test]
    public void Local_search_proves_a_complete_request_aware_route_before_returning_a_prefix()
    {
        var grid = new Grid(64, 4) { Rule = (a, b) => b > a };
        grid.Costs[10] = 10000;
        var result = new RequestRouter(64, 4, grid.Costs, new bool[256]).TryLocal(grid, 0, new[] { 20, 84 }, new MoveCosts(13, 18), 5);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Partial, Is.True);
        Assert.That(result.Value.Cells.Length, Is.EqualTo(6));
        Assert.That(result.Value.Cells.ToArray().Zip(result.Value.Cells.ToArray().Skip(1)).All(p => grid.CanStep(p.First, p.Second)), Is.True);
        var full = new RequestRouter(64, 4, grid.Costs, new bool[256]).TryLocal(grid, 0, new[] { 20, 84 }, new MoveCosts(13, 18), 0)!;
        Assert.That(full.Value.Partial, Is.False);
        Assert.That(full.Value.Cells.Span[^1], Is.EqualTo(84));
        Assert.That(full.Value.Cost, Is.EqualTo(19 * 13 + 18));
    }

    [Test]
    public void A_cached_directed_repair_is_revalidated_before_passing_the_last_detour()
    {
        var router = new RequestRouter(128, 2, new int[256], new bool[256]);
        bool changed = false;
        var grid = new Grid(128, 2)
        {
            Rule = (a, b) => a < 128
                ? b == a + 1 || changed && a == 12 && b == 140
                : b >= 128 && Math.Abs(a - b) == 1 || a == 255 && b == 127
        };
        var cursor = new RequestCursor();
        var first = router.Find(grid, 0, new[] { 127 }, new MoveCosts(13, 18), 25, cursor)!;
        int current = first.Value.Cells.Span[12];
        changed = true;
        grid.DirectedBlock = (70, 71);
        for (int i = 0; i < 30 && current != 127; i++)
        {
            var next = router.Find(grid, current, new[] { 127 }, new MoveCosts(13, 18), 25, cursor);
            Assert.That(next, Is.Not.Null, $"Stranded after changed repair at {current}");
            current = next!.Value.Cells.Span[Math.Min(12, next.Value.Cells.Length - 1)];
        }
        Assert.That(current, Is.EqualTo(127));
    }

    [Test]
    public void A_partial_route_does_not_commit_into_an_irreversible_dead_end()
    {
        var router = new RequestRouter(96, 2, new int[192], new bool[192]);
        var grid = new Grid(96, 2)
        {
            Rule = (a, b) => a < 96
                ? a < 80 && b == a + 1 || a == 0 && b == 96
                : b >= 96 && Math.Abs(a - b) == 1 || a == 191 && b == 95
        };
        var cursor = new RequestCursor();
        int current = 0;
        for (int i = 0; i < 30 && current != 95; i++)
        {
            var route = router.Find(grid, current, new[] { 95 }, new MoveCosts(13, 18), 25, cursor);
            Assert.That(route, Is.Not.Null, $"Stranded at {current}");
            current = route!.Value.Cells.Span[Math.Min(12, route.Value.Cells.Length - 1)];
        }
        Assert.That(current, Is.EqualTo(95));
    }

    [Test]
    public void Full_queries_reach_an_alternative_goal_and_preserve_directed_edges()
    {
        var router = new RequestRouter(256, 2, new int[512], new bool[512]);
        var grid = new Grid(256, 2);
        grid.DirectedBlock = (254, 255);
        grid.Costs[511] = 10000;
        var result = router.Find(grid, 0, new[] { 255, 510 }, new MoveCosts(13, 18), 0, new RequestCursor());
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Partial, Is.False);
        Assert.That(new[] { 255, 510 }, Does.Contain(result.Value.Cells.Span[^1]));
        Assert.That(result.Value.Cells.Length, Is.GreaterThan(200));
        Assert.That(result.Value.Cells.ToArray().Zip(result.Value.Cells.ToArray().Skip(1)).All(p => grid.CanStep(p.First, p.Second)), Is.True);
        Assert.That(result.Value.Cost, Is.GreaterThan(0));
    }

    [Test]
    public void Dynamic_door_closure_repairs_guidance_and_reaches_a_touch_destination()
    {
        const int width = 96, height = 32;
        var router = new RequestRouter(width, height, new int[width * height], new bool[width * height]);
        var grid = new Grid(width, height);
        // A request-specific closed door invalidates the optimistic straight route.
        for (int z = 0; z < 31; z++) grid.Costs[z * width + 48] = 10000;
        var goals = new[] { 16 * width + 90, 17 * width + 90 };
        var cursor = new RequestCursor();
        int current = 16 * width + 3;
        bool repaired = false;
        for (int i = 0; i < 60 && !goals.Contains(current); i++)
        {
            var route = router.Find(grid, current, goals, new MoveCosts(13, 18), 25, cursor);
            Assert.That(route, Is.Not.Null);
            Assert.That(route!.Value.Cells.Length, Is.InRange(2, 26));
            Assert.That(route.Value.Cells.ToArray().Zip(route.Value.Cells.ToArray().Skip(1)).All(p => grid.CanStep(p.First, p.Second)), Is.True);
            repaired |= route.Value.Repaired;
            current = route.Value.Cells.Span[Math.Min(12, route.Value.Cells.Length - 1)];
        }
        Assert.That(goals, Does.Contain(current));
        Assert.That(repaired, Is.True);
    }

    private sealed class Grid(int width, int height) : IRequestGrid
    {
        public (int From, int To)? DirectedBlock;
        public Func<int, int, bool>? Rule;
        public int[] Costs { get; } = new int[width * height];
        public int Cost(int cell) => Costs[cell];
        public bool CanStep(int from, int to)
        {
            if (DirectedBlock == (from, to)) return false;
            if (Rule != null && !Rule(from, to)) return false;
            int dx = to % width - from % width, dz = to / width - from / width;
            if (to < 0 || to >= Costs.Length || Math.Abs(dx) > 1 || Math.Abs(dz) > 1 || dx == 0 && dz == 0 || Cost(to) >= 10000) return false;
            return dx == 0 || dz == 0 || Cost(from + dx) < 10000 && Cost(from + dz * width) < 10000;
        }
    }
}
