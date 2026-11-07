using NUnit.Framework;
using PathfindingLab.Core;

namespace PathfindingLab.Unit;

public class PortalRouterTests
{
    [Test]
    public void Graph_flood_fill_does_not_allocate_per_expanded_cell()
    {
        new PortalRouter(32, 32, new int[1024], new bool[1024]).Prepare();
        var router = new PortalRouter(256, 256, new int[65536], new bool[65536]);
        long before = GC.GetAllocatedBytesForCurrentThread();
        router.Prepare();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        TestContext.Out.WriteLine($"256-square cold graph preparation: {allocated} bytes");
        // Includes durable boundary nodes and temporary boundary metadata, but not cell iterators.
        Assert.That(allocated, Is.LessThan(1000000));
        Assert.That(router.Plan(0, 65535, new MoveCosts(13, 18)), Is.Not.Null);
    }

    [TestCase(20)]
    [TestCase(10000)]
    public void An_unrelated_chunk_edit_preserves_the_existing_coarse_plan(int newCost)
    {
        var router = new PortalRouter(128, 64, new int[8192], new bool[8192]);
        var plan = router.Plan(0, 63, new MoveCosts(13, 18))!;
        var first = router.Refine(plan, 0, 25)!;
        router.UpdateCell(50 * 128 + 110, newCost, false);
        var resumed = router.Refine(plan, first[12], 25);
        Assert.That(resumed, Is.Not.Null);
        Assert.That(resumed![0], Is.EqualTo(first[12]));
    }

    [Test]
    public void Finite_cost_changes_invalidate_local_paths_without_rebuilding_connectivity()
    {
        var router = new PortalRouter(96, 64, new int[6144], new bool[6144]);
        var movement = new MoveCosts(13, 18);
        var first = router.Plan(0, 20, movement)!;
        router.Refine(first, 0, 50);
        long searches = router.LocalSearches;
        router.UpdateCell(10, 500, false);
        Assert.That(router.Refine(first, 0, 50), Is.Null, "A buffered old leg must reject changed dependent costs.");
        var second = router.Plan(0, 20, movement)!;
        var result = router.Refine(second, 0, 50)!;
        Assert.That(router.LastBoundaryPairsRebuilt, Is.EqualTo(0));
        Assert.That(router.LocalSearches, Is.GreaterThan(searches));
        Assert.That(result, Does.Not.Contain(10));
    }

    [Test]
    public void Horizon_changes_preserve_a_live_plan_and_bound_every_new_segment()
    {
        var router = new PortalRouter(512, 32, new int[512 * 32], new bool[512 * 32]);
        var plan = router.Plan(0, 511, new MoveCosts(13, 18))!;
        int current = 0;
        var horizons = new[] { 25, 100, 200, 50 };
        for (int i = 0; current != 511 && i < 100; i++)
        {
            int horizon = horizons[i % horizons.Length];
            var segment = router.Refine(plan, current, horizon)!;
            Assert.That(segment, Is.Not.Null);
            Assert.That(segment.Count, Is.InRange(2, horizon + 1));
            Assert.That(segment[0], Is.EqualTo(current));
            Assert.That(segment.Zip(segment.Skip(1)).All(pair => router.CanStep(pair.First, pair.Second)), Is.True);
            current = segment[Math.Min(segment.Count - 1, Math.Max(1, horizon / 2))];
        }
        Assert.That(current, Is.EqualTo(511));
    }

    [Test]
    public void LongWindingChunkLegIsCachedButReturnedInShortPrefixes()
    {
        var costs = Enumerable.Repeat(-1, 1024).ToArray();
        for (int z = 0; z < 32; z += 2)
        {
            for (int x = 0; x < 32; x++) costs[z * 32 + x] = 0;
            if (z + 1 < 32) costs[(z + 1) * 32 + (z % 4 == 0 ? 31 : 0)] = 0;
        }
        var router = new PortalRouter(32, 32, costs, new bool[1024]);
        var plan = router.Plan(0, 960, new MoveCosts(13, 18))!;
        int current = 0, traveled = 0;
        for (int i = 0; current != 960 && i < 30; i++)
        {
            var segment = router.Refine(plan, current, 50)!;
            Assert.That(segment.Count, Is.InRange(2, 51));
            traveled += segment.Count - 1;
            current = segment[^1];
        }
        Assert.That(current, Is.EqualTo(960));
        Assert.That(traveled, Is.GreaterThan(500));
        Assert.That(router.LocalSearches, Is.EqualTo(1));
    }

    [Test]
    public void InteriorEditOnlyRebuildsAdjacentBoundaries()
    {
        var router = new PortalRouter(256, 256, new int[65536], new bool[65536]);
        Assert.That(router.Plan(0, 65535, new MoveCosts(13, 18)), Is.Not.Null);
        Assert.That(router.LastBoundaryPairsRebuilt, Is.GreaterThan(2000));
        router.UpdateCell(100 * 256 + 100, 10000, false);
        Assert.That(router.Plan(0, 65535, new MoveCosts(13, 18)), Is.Not.Null);
        Assert.That(router.LastBoundaryPairsRebuilt, Is.InRange(1, 128));
    }

    [Test]
    public void DoesNotConnectDisconnectedAreasInsideOneChunkOrCutBlockedCorners()
    {
        var costs = new int[64 * 32];
        for (int x = 0; x < 64; x++) costs[16 * 64 + x] = -1;
        var router = new PortalRouter(64, 32, costs, new bool[costs.Length]);
        Assert.That(router.Plan(2 * 64 + 2, 25 * 64 + 50, new MoveCosts(13, 18)), Is.Null);
        Assert.That(router.CanStep(15 * 64 + 2, 16 * 64 + 3), Is.False);
        var corners = new bool[4] { false, true, false, false };
        var small = new PortalRouter(2, 2, new int[4], corners);
        Assert.That(small.CanStep(0, 3), Is.False);
    }

    [Test]
    public void CacheIsBoundedAndPlanRejectsUnrelatedPosition()
    {
        var router = new PortalRouter(32, 32, new int[1024], new bool[1024]);
        for (int start = 0; start < 600; start++)
        {
            var plan = router.Plan(start, 1023, new MoveCosts(13, 18))!;
            Assert.That(router.Refine(plan, start, 50), Is.Not.Null);
        }
        Assert.That(router.CachedPaths, Is.InRange(1, 512));
        var retained = router.Plan(0, 1023, new MoveCosts(13, 18))!;
        Assert.That(router.Refine(retained, 2, 50), Is.Null);
    }

    [Test]
    public void LocalEditInvalidatesOldPlanAndCachedLegs()
    {
        var router = new PortalRouter(64, 32, new int[64 * 32], new bool[64 * 32]);
        int start = 16 * 64 + 2, target = 16 * 64 + 60;
        var stale = router.Plan(start, target, new MoveCosts(13, 18))!;
        Assert.That(router.Refine(stale, start, 50), Is.Not.Null);
        for (int z = 0; z < 32; z++) router.UpdateCell(z * 64 + 31, -1, true);
        Assert.That(router.Refine(stale, start, 50), Is.Null);
        Assert.That(router.Plan(start, target, new MoveCosts(13, 18)), Is.Null);
        router.UpdateCell(16 * 64 + 31, 0, false);
        var replacement = router.Plan(start, target, new MoveCosts(13, 18));
        Assert.That(replacement, Is.Not.Null);
        Assert.That(router.Refine(replacement!, start, 50)!.Contains(16 * 64 + 31), Is.True);
    }

    [Test]
    public void RepeatedRouteReusesBoundedLocalCacheButNotDifferentMovementCosts()
    {
        var router = new PortalRouter(160, 64, new int[160 * 64], new bool[160 * 64]);
        void Travel(MoveCosts movement)
        {
            int start = 10 * 160 + 10, target = 10 * 160 + 150;
            var plan = router.Plan(start, target, movement)!;
            for (int i = 0; i < 30 && start != target; i++)
            {
                var segment = router.Refine(plan, start, 50)!;
                start = segment[^1];
            }
            Assert.That(start, Is.EqualTo(target));
        }
        Travel(new MoveCosts(13, 18));
        long coldSearches = router.LocalSearches;
        Assert.That(coldSearches, Is.GreaterThan(0));
        Travel(new MoveCosts(13, 18));
        Assert.That(router.LocalSearches, Is.EqualTo(coldSearches), "warm route must not repeat local searches");
        Travel(new MoveCosts(20, 28));
        Assert.That(router.LocalSearches, Is.GreaterThan(coldSearches));
    }

    [Test]
    public void RefinesBoundedSegmentsAroundBarrierUntilOriginalDestination()
    {
        const int width = 128, height = 96;
        var costs = new int[width * height];
        for (int z = 0; z < 80; z++) costs[z * width + 63] = -1;
        var router = new PortalRouter(width, height, costs, new bool[costs.Length]);
        int start = 10 * width + 10, target = 10 * width + 110;
        var plan = router.Plan(start, target, new MoveCosts(13, 18));
        Assert.That(plan, Is.Not.Null);
        var visited = new List<int> { start };
        for (int iteration = 0; start != target && iteration < 40; iteration++)
        {
            var segment = router.Refine(plan!, start, 50);
            Assert.That(segment, Is.Not.Null);
            Assert.That(segment!.Count, Is.InRange(2, 51));
            Assert.That(segment[0], Is.EqualTo(start));
            foreach (int cell in segment) Assert.That(costs[cell], Is.GreaterThanOrEqualTo(0));
            for (int i = 1; i < segment.Count; i++)
                Assert.That(router.CanStep(segment[i - 1], segment[i]), Is.True);
            int advance = Math.Min(25, segment.Count - 1);
            visited.AddRange(segment.Skip(1).Take(advance));
            start = segment[advance];
        }
        Assert.That(start, Is.EqualTo(target));
        Assert.That(visited.Any(cell => cell / width >= 80), Is.True);
    }
}
