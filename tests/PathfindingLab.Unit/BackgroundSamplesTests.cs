using NUnit.Framework;
using PathfindingLab.EndToEndTests;

namespace PathfindingLab.Unit;

public class BackgroundSamplesTests
{
    [Test]
    public void Superseded_revision_is_not_pending_when_later_build_finishes_before_close()
    {
        var ledger = new BackgroundSamples();
        ledger.Queue(1, 1, 0, 10); ledger.Admit(1, 1, true);
        ledger.Queue(1, 2, 0, 20); ledger.Admit(1, 2, true);
        ledger.Start(1, 2, 30); ledger.Finish(1, 2, 40, 10, true);
        ledger.CloseWindow(0);
        var old = ledger.Snapshot().Single(row => row.Version == 1);
        Assert.That(old.Outcome, Is.EqualTo("coalesced"));
        Assert.That(old.PendingAtWindowClose, Is.False);
        Assert.That(ledger.Pending, Is.Zero);
    }

    [Test]
    public void Execution_keeps_origin_after_window_close_and_ignored_requests_do_not_count()
    {
        var ledger = new BackgroundSamples();
        ledger.Queue(1, 1, 0, 10); ledger.Admit(1, 1, true); ledger.Start(1, 1, 20);
        ledger.CloseWindow(0);
        ledger.Queue(2, 1, 1, 25); ledger.Admit(2, 1, false);
        ledger.Finish(1, 1, 40, 20, false);
        var rows = ledger.Snapshot();
        Assert.That(rows.Length, Is.EqualTo(1));
        Assert.That(rows[0].Window, Is.Zero);
        Assert.That(rows[0].Outcome, Is.EqualTo("failed"));
        Assert.That(rows[0].PendingAtWindowClose, Is.True);
        Assert.That(ledger.Pending, Is.Zero);
    }

    [Test]
    public void Queued_work_keeps_its_window_and_coalesced_work_is_not_lost_or_timed_as_execution()
    {
        var ledger = new BackgroundSamples();
        Assert.That(ledger.Queue(1, 1, 0, 100), Is.True);
        ledger.Admit(1, 1, true);
        Assert.That(ledger.Queue(1, 1, 0, 110), Is.False);
        ledger.Queue(1, 2, 1, 120); ledger.Admit(1, 2, true);
        ledger.CloseWindow(0);
        Assert.That(ledger.Pending, Is.EqualTo(2));
        ledger.Start(1, 2, 130);
        ledger.Finish(1, 2, 160, 30, true);
        ledger.SettleIdle(1);
        var rows = ledger.Snapshot();
        Assert.That(rows.Single(r => r.Version == 1).Outcome, Is.EqualTo("coalesced"));
        Assert.That(rows.Single(r => r.Version == 1).Window, Is.EqualTo(0));
        Assert.That(rows.Single(r => r.Version == 1).PendingAtWindowClose, Is.True);
        Assert.That(rows.Single(r => r.Version == 2).ElapsedMilliseconds, Is.EqualTo(30));
        Assert.That(ledger.Pending, Is.Zero);
    }
}
