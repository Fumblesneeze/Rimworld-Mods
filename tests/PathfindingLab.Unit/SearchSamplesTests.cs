using NUnit.Framework;
using PathfindingLab.EndToEndTests;

namespace PathfindingLab.Unit;

public sealed class SearchSamplesTests
{
    [Test]
    public void Missing_or_misattributed_door_work_cannot_be_reported_as_complete()
    {
        var samples = new SearchSamples();
        samples.Add(10, 0, 1000, true, "experimental-search", "a");
        Assert.That(samples.DoorCoverage().Complete, Is.False, "A missing timer is unknown cost, not zero.");
        samples.Add(2, 2, 1000, true, "vanilla-doors", "b");
        Assert.That(samples.DoorCoverage().Complete, Is.False, "Matching grand totals cannot hide wrong-pawn attribution.");
    }

    [Test]
    public void Door_coverage_requires_successful_drained_exact_counts_including_pawnless_work()
    {
        var samples = new SearchSamples();
        Assert.That(samples.DoorCoverage().Complete, Is.False);
        samples.Add(10, 0, 1000, true, "vanilla-search");
        samples.Add(2, 2, 1000, true, "vanilla-doors");
        samples.Add(1, 0, 1000, true, "wildlife-direct", "idle");
        samples.Add(1, 0, 1000, true, "experimental-readiness-dispatch");
        Assert.That(samples.DoorCoverage().Complete, Is.True);
        samples.Scheduled();
        Assert.That(samples.DoorCoverage().Complete, Is.False);
        samples.Completed();
        samples.Add(2, 2, 1000, true, "vanilla-doors");
        Assert.That(samples.DoorCoverage().Complete, Is.False, "Duplicated timing is not complete coverage.");
        samples.Add(10, 0, 1000, false, "experimental-search");
        Assert.That(samples.DoorCoverage().Complete, Is.False, "Matching counts cannot hide a failed worker.");
    }

    [Test]
    public void Stage_subsets_are_retained_without_adding_them_to_worker_elapsed()
    {
        var samples = new SearchSamples();
        for (int i = 0; i < 2; i++) { samples.Scheduled(); samples.Complete(100, 40, 1000, true, "experimental-search", "a", 30, 10, 20); }
        var total = samples.Totals().Single();
        Assert.That(total.ElapsedMilliseconds, Is.EqualTo(200));
        Assert.That(total.RefreshMilliseconds, Is.EqualTo(60));
        Assert.That(total.GraphMilliseconds, Is.EqualTo(20));
        Assert.That(total.LockWaitMilliseconds, Is.EqualTo(40));
        Assert.That(samples.Snapshot()[0].RefreshMilliseconds, Is.EqualTo(30));
        Assert.Throws<ArgumentOutOfRangeException>(() => samples.Add(100, 40, 1000, true, refresh: 41));
        Assert.Throws<ArgumentOutOfRangeException>(() => samples.Add(100, 40, 1000, true, lockWait: 61));
    }

    [Test]
    public void Export_is_consistent_during_late_concurrent_completions()
    {
        const int count = 2000;
        var samples = new SearchSamples();
        for (int i = 0; i < count; i++) samples.Scheduled();
        samples.Close();
        var work = Task.Run(() => Parallel.For(0, count, _ => samples.Complete(10, 2, 1000, true, "worker", "pawn")));
        for (int i = 0; i < 10000 && !work.IsCompleted; i++) Check(samples.Export());
        work.GetAwaiter().GetResult();
        var final = samples.Export();
        Check(final);
        Assert.That(final.Pending, Is.Zero);
        void Check(SearchWindowSnapshot snapshot)
        {
            Assert.That(snapshot.Pending + snapshot.LateCompletions, Is.EqualTo(snapshot.PendingAtClose));
            Assert.That(snapshot.Totals.Sum(t => t.Count), Is.EqualTo(snapshot.LateCompletions));
            Assert.That(snapshot.RawSearches.Length, Is.EqualTo(snapshot.LateCompletions));
        }
    }

    [Test]
    public void Closing_retains_pending_and_late_worker_completions()
    {
        var samples = new SearchSamples();
        samples.Scheduled(); samples.Scheduled();
        samples.Add(10, 0, 1000, true); samples.Completed();
        samples.Close();
        Assert.That(samples.PendingAtClose, Is.EqualTo(1));
        samples.Add(20, 5, 1000, false); samples.Completed();
        samples.Close();
        Assert.That(samples.PendingAtClose, Is.EqualTo(1), "Repeated cleanup must not replace the boundary snapshot.");
        Assert.That(samples.LateCompletions, Is.EqualTo(1));
        Assert.That(samples.Pending, Is.Zero);
        Assert.That(samples.Totals().Sum(x => x.ElapsedMilliseconds), Is.EqualTo(30));
    }

    [Test]
    public void Movement_aggregates_remain_complete_after_raw_detail_overflows()
    {
        var moves = new MovementSamples();
        for (int i = 0; i < SearchSamples.Capacity + 10; i++)
        { moves.Start("a", 100, false); moves.Finish("a", 125, 1000, "moved"); }
        Assert.That(moves.Overflowed, Is.True);
        Assert.That(moves.LatencyTotal.Count, Is.EqualTo(SearchSamples.Capacity + 10));
        Assert.That(moves.LatencyTotal.SumMilliseconds, Is.EqualTo((SearchSamples.Capacity + 10) * 25));
        Assert.That(moves.LatencyTotal.MaximumMilliseconds, Is.EqualTo(25));
        Assert.That(moves.Outcomes["moved"], Is.EqualTo(moves.LatencyTotal.Count));
    }

    [Test]
    public void Previous_leg_cannot_satisfy_a_new_start_inside_the_same_native_tick()
    {
        var moves = new MovementSamples();
        moves.Start("a", 100, false);
        long previous = moves.Generation("a");
        moves.Start("a", 120, false);
        moves.FinishGeneration("a", previous, 140, 1000);
        Assert.That(moves.Pending, Is.EqualTo(1));
        moves.FinishGeneration("a", moves.Generation("a"), 150, 1000);
        Assert.That(moves.Snapshot().Single(x => x.Outcome == "moved").FirstMovementMilliseconds, Is.EqualTo(30));
    }

    [Test]
    public void Movement_latency_retains_retargets_cancellations_already_moving_and_censoring()
    {
        var moves = new MovementSamples();
        moves.Start("a", 100, false);
        moves.Start("b", 100, false);
        moves.Start("a", 110, false);
        moves.Finish("a", 135, 1000, "moved");
        moves.Start("c", 150, true);
        moves.Start("d", 150, false);
        moves.Finish("d", 180, 1000, "cancelled");
        moves.Close(200, 1000);
        var rows = moves.Snapshot();
        Assert.That(rows.Select(x => x.Outcome), Is.EquivalentTo(new[] { "superseded", "moved", "already-moving", "cancelled", "window-ended" }));
        Assert.That(rows.Single(x => x.Outcome == "moved").FirstMovementMilliseconds, Is.EqualTo(25));
        Assert.That(rows.Where(x => x.Outcome != "moved").All(x => x.FirstMovementMilliseconds == null), Is.True);
        moves.Finish("b", 210, 1000, "moved");
        Assert.That(moves.Snapshot().Length, Is.EqualTo(5), "Closed-window movement cannot leak into later data.");
    }

    [Test]
    public void Aggregates_include_overflow_and_separate_shared_preparation_from_pawn_searches()
    {
        var samples = new SearchSamples();
        Parallel.For(0, SearchSamples.Capacity + 10, i => samples.Add(30, 10, 1000, i % 2 == 0, "experimental", "pawn-" + i % 100));
        samples.Add(8, 8, 1000, true, "vanilla-grid", null);
        var totals = samples.Totals();
        Assert.That(totals.Sum(x => x.Count), Is.EqualTo(SearchSamples.Capacity + 11));
        Assert.That(totals.Sum(x => x.ElapsedMilliseconds), Is.EqualTo((SearchSamples.Capacity + 10) * 30 + 8));
        Assert.That(totals.Sum(x => x.SearchMilliseconds), Is.EqualTo((SearchSamples.Capacity + 10) * 20));
        Assert.That(totals.Single(x => x.Source == "vanilla-grid").PawnId, Is.Null);
        Assert.That(totals.Where(x => x.Source == "experimental").Sum(x => x.Failed), Is.EqualTo((SearchSamples.Capacity + 10) / 2));
    }

    [Test]
    public void Concurrent_samples_are_bounded_and_report_overflow()
    {
        var samples = new SearchSamples();
        Parallel.For(0, SearchSamples.Capacity + 10, _ => samples.Add(1, 0, 1000, true));
        Assert.That(samples.Snapshot().Length, Is.EqualTo(SearchSamples.Capacity));
        Assert.That(samples.Overflowed, Is.True);
        Assert.Throws<ArgumentOutOfRangeException>(() => samples.Add(1, 2, 1000, true));
    }

    [Test]
    public void Samples_retain_work_and_nested_preparation_without_conflating_empty_with_zero()
    {
        var samples = new SearchSamples();
        Assert.That(samples.Snapshot(), Is.Empty);
        samples.Add(30, 10, 1000, true);
        samples.Add(20, 0, 1000, false);
        var snapshot = samples.Snapshot();
        samples.Add(1, 0, 1000, true);
        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Length, Is.EqualTo(2));
            Assert.That(snapshot[0].ElapsedMilliseconds, Is.EqualTo(30));
            Assert.That(snapshot[0].PreparationMilliseconds, Is.EqualTo(10));
            Assert.That(snapshot[1].Succeeded, Is.False);
        });
    }
}
