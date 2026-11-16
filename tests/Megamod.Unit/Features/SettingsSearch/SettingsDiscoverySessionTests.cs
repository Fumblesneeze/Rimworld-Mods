using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.SettingsSearch;

[TestFixture]
public sealed class SettingsDiscoverySessionTests
{
    private static readonly SettingsPage First = new("page-1", "open-1", "Options", "Unfamiliar Mod", "Author", "unfamiliar.package");
    private static readonly SettingsPage Second = First with { PageIdentity = "page-2", OpenToken = "open-2" };

    [Test]
    public void EqualPageNamesAndPackagesRemainIndependentlyDiscoverableAndNeverClaimCompleteCoverage()
    {
        var session = new SettingsDiscoverySession(new[] { First, Second });
        Assert.That(session.Pages.Select(p => p.Page), Is.EqualTo(new[] { First, Second }));
        Assert.That(session.Pages.All(p => p.Status == SettingsDiscoveryStatus.Pending), Is.True);

        var first = session.BeginNext(10)!;
        Assert.That(first, Is.Not.Null);
        Assert.That(first.Page, Is.SameAs(First));
        Assert.That(first.PageOrdinal, Is.Zero);
        Assert.That(session.Complete(first, Capture(First, "First option"), 4, pageFinished: true), Is.True);
        Assert.That(session.IsFinished, Is.False);

        var second = session.BeginNext(11)!;
        Assert.That(second.Page, Is.SameAs(Second));
        Assert.That(second.PageOrdinal, Is.EqualTo(1));
        Assert.That(session.Complete(second, Capture(Second, "Second option"), 6, pageFinished: true), Is.True);

        Assert.That(session.IsFinished, Is.True);
        Assert.That(session.IsCancelled, Is.False);
        Assert.That(session.Pages.Select(p => p.Status), Is.EqualTo(new[] { SettingsDiscoveryStatus.Partial, SettingsDiscoveryStatus.Partial }));
        Assert.That(session.Pages.Select(p => p.Captures.Single().Records.Single().Text), Is.EqualTo(new[] { "First option", "Second option" }));
    }

    private static SettingsCaptureSnapshot Capture(SettingsPage page, string text, string path = "root")
    {
        var buffer = new SettingsCaptureBuffer(page, path);
        buffer.Add(SettingsCaptureKind.Text, text, new(0, 0, 300, 30));
        return buffer.Complete();
    }

    [Test]
    public void OneCallbackPerRepaintAndExactReceiptsProtectContinuedPageProgress()
    {
        var session = new SettingsDiscoverySession(new[] { First, Second });
        var first = session.BeginNext(10)!;
        Assert.That(session.BeginNext(100), Is.Null, "An in-flight callback prevents further admission without consuming that repaint.");
        Assert.That(session.Complete(first, Capture(First, "First tab", "tab/1"), 4, pageFinished: false), Is.True);
        Assert.That(session.Pages[0].Status, Is.EqualTo(SettingsDiscoveryStatus.Reading));
        Assert.That(session.BeginNext(10), Is.Null);
        Assert.That(session.BeginNext(9), Is.Null);

        var continuation = session.BeginNext(11)!;
        Assert.That(continuation.PageOrdinal, Is.Zero);
        Assert.That(continuation, Is.Not.SameAs(first));
        var unrelatedWork = new SettingsDiscoverySession(new[] { First }).BeginNext(1)!;
        Assert.That(session.Complete(unrelatedWork, Capture(First, "Wrong session"), 99, pageFinished: true), Is.False);
        Assert.That(session.Complete(first, Capture(First, "Replayed result"), 99, pageFinished: true), Is.False);
        Assert.That(session.Complete(continuation, Capture(First, "Second tab", "tab/2"), 6, pageFinished: true), Is.True);
        Assert.That(session.Complete(continuation, null, 99, pageFinished: true), Is.False);

        var page = session.Pages[0];
        Assert.That(page.Captures.Select(c => c.PathIdentity), Is.EqualTo(new[] { "tab/1", "tab/2" }));
        Assert.That(page.Callbacks, Is.EqualTo(2));
        Assert.That(page.ElapsedMilliseconds, Is.EqualTo(10));
        Assert.That(session.BeginNext(11), Is.Null);
        Assert.That(session.BeginNext(12)!.Page, Is.SameAs(Second));
    }

    [Test]
    public void CancelKeepsFinishedObservationsButRejectsTheActiveCallbackAndStopsPendingPages()
    {
        var third = First with { PageIdentity = "page-3" };
        var session = new SettingsDiscoverySession(new[] { First, Second, third });
        session.Complete(session.BeginNext(1)!, Capture(First, "Finished page"), 3, pageFinished: true);
        session.Complete(session.BeginNext(2)!, Capture(Second, "Finished callback"), 4, pageFinished: false);
        var active = session.BeginNext(3)!;

        session.Cancel();
        session.Cancel();

        Assert.That(session.IsCancelled, Is.True);
        Assert.That(session.IsFinished, Is.True);
        Assert.That(session.BeginNext(4), Is.Null);
        Assert.That(session.Complete(active, Capture(Second, "Cancelled callback"), 99, pageFinished: true), Is.False);
        var pages = session.Pages;
        Assert.That(pages.Select(p => p.Status), Is.EqualTo(new[] { SettingsDiscoveryStatus.Partial,
            SettingsDiscoveryStatus.Cancelled, SettingsDiscoveryStatus.Cancelled }));
        Assert.That(pages.Select(p => p.StopReason), Is.EqualTo(new[] { SettingsDiscoveryStop.None,
            SettingsDiscoveryStop.Cancelled, SettingsDiscoveryStop.Cancelled }));
        Assert.That(pages[0].Captures.Single().Records.Single().Text, Is.EqualTo("Finished page"));
        Assert.That(pages[1].Captures.Single().Records.Single().Text, Is.EqualTo("Finished callback"));
        Assert.That(pages[1].Callbacks, Is.EqualTo(1));
        Assert.That(pages[1].ElapsedMilliseconds, Is.EqualTo(4));
        Assert.That(pages[2].Captures, Is.Empty);

        session.Refresh(new[] { Second });
        Assert.That(session.IsCancelled, Is.False);
        Assert.That(session.IsFinished, Is.False);
        var refreshed = session.BeginNext(4)!;
        Assert.That(refreshed.Generation, Is.GreaterThan(active.Generation));
        Assert.That(session.Complete(refreshed, Capture(Second, "Refreshed option"), 2, pageFinished: true), Is.True);
        Assert.That(session.Pages.Single().Captures.Single().Records.Single().Text, Is.EqualTo("Refreshed option"));
    }

    [Test]
    public void RefreshDropsOldResultsAndReceiptsEvenWhenThePageDescriptorIsUnchanged()
    {
        var session = new SettingsDiscoverySession(new[] { First });
        session.Complete(session.BeginNext(20)!, Capture(First, "Old language"), 5, pageFinished: false);
        var old = session.BeginNext(21)!;

        session.Refresh(new[] { First });

        Assert.That(session.Pages.Single().Status, Is.EqualTo(SettingsDiscoveryStatus.Pending));
        Assert.That(session.Pages.Single().Captures, Is.Empty);
        Assert.That(session.Pages.Single().Callbacks, Is.Zero);
        Assert.That(session.Complete(old, Capture(First, "Stale language"), 5, pageFinished: true), Is.False);
        var current = session.BeginNext(22)!;
        Assert.That(current.Generation, Is.GreaterThan(old.Generation));
        Assert.That(session.Complete(old, null, 5, pageFinished: true), Is.False);
        Assert.That(session.Complete(current, Capture(First, "New language"), 2, pageFinished: true), Is.True);

        session.Refresh(Array.Empty<SettingsPage>());
        Assert.That(session.IsFinished, Is.True);
        Assert.That(session.IsCancelled, Is.False);
        Assert.That(session.Pages, Is.Empty);
        Assert.That(session.BeginNext(23), Is.Null);
    }

    [Test]
    public void RefreshDoesNotAdmitASecondCallbackInTheSameOrEarlierRepaint()
    {
        var session = new SettingsDiscoverySession(new[] { First });
        var original = session.BeginNext(10)!;
        Assert.That(session.Complete(original, Capture(First, "Old observation"), 2, pageFinished: true), Is.True);

        session.Refresh(new[] { Second });

        Assert.That(session.BeginNext(10), Is.Null, "Refreshing the catalog does not advance the native repaint.");
        Assert.That(session.BeginNext(9), Is.Null);
        Assert.That(session.Pages.Single().Status, Is.EqualTo(SettingsDiscoveryStatus.Pending));
        Assert.That(session.Pages.Single().Callbacks, Is.Zero);
        var refreshed = session.BeginNext(11)!;
        Assert.That(refreshed.Page, Is.SameAs(Second));
        Assert.That(refreshed.Generation, Is.GreaterThan(original.Generation));
        Assert.That(session.Complete(refreshed, Capture(Second, "New observation"), 3, pageFinished: true), Is.True);
        Assert.That(session.Pages.Single().Captures.Single().Records.Single().Text, Is.EqualTo("New observation"));
    }

    [TestCase(2, 500, 100, 4, 6, SettingsDiscoveryStop.CallbackLimit)]
    [TestCase(96, 10, 100, 4, 6, SettingsDiscoveryStop.TimeLimit)]
    [TestCase(96, 500, 100, 4, 101, SettingsDiscoveryStop.SlowCallback)]
    public void APageStopsAfterReturningOverBudgetAndOtherPagesRemainEligible(int callbacks, double pageMs,
        double callbackMs, double firstMs, double secondMs, SettingsDiscoveryStop reason)
    {
        var session = new SettingsDiscoverySession(new[] { First, Second }, callbacks, pageMs, callbackMs);
        session.Complete(session.BeginNext(1)!, Capture(First, "First tab", "tab/1"), firstMs, pageFinished: false);
        var work = session.BeginNext(2)!;
        Assert.That(session.Pages[0].StopReason, Is.EqualTo(SettingsDiscoveryStop.None), "An admitted callback is not preempted.");
        Assert.That(session.Complete(work, Capture(First, "Second tab", "tab/2"), secondMs, pageFinished: false), Is.True);

        var page = session.Pages[0];
        Assert.That(page.Status, Is.EqualTo(SettingsDiscoveryStatus.Partial));
        Assert.That(page.StopReason, Is.EqualTo(reason));
        Assert.That(page.Callbacks, Is.EqualTo(2));
        Assert.That(page.ElapsedMilliseconds, Is.EqualTo(firstMs + secondMs));
        Assert.That(page.Captures.Count, Is.EqualTo(2));
        Assert.That(session.BeginNext(2), Is.Null);
        Assert.That(session.BeginNext(3)!.Page, Is.SameAs(Second));
    }

    [TestCase(1, 500, 100, 3)]
    [TestCase(96, 10, 100, 10)]
    [TestCase(96, 500, 100, 100)]
    public void AFinishedPageDoesNotReportAnUnneededContinuationLimit(int callbacks, double pageMs,
        double callbackMs, double elapsed)
    {
        var session = new SettingsDiscoverySession(new[] { First }, callbacks, pageMs, callbackMs);
        Assert.That(session.Complete(session.BeginNext(1)!, Capture(First, "Done"), elapsed, pageFinished: true), Is.True);
        Assert.That(session.IsFinished, Is.True);
        Assert.That(session.Pages.Single().StopReason, Is.EqualTo(SettingsDiscoveryStop.None));
    }

    [Test]
    public void ASingleCallbackAtItsExactLimitCanContinueButAnOverrunIsReportedEvenIfItFinishesThePage()
    {
        var session = new SettingsDiscoverySession(new[] { First });
        session.Complete(session.BeginNext(1)!, null, 100, pageFinished: false);
        Assert.That(session.IsFinished, Is.False);
        Assert.That(session.Pages[0].StopReason, Is.EqualTo(SettingsDiscoveryStop.None));
        session.Complete(session.BeginNext(2)!, Capture(First, "Done"), 100.01, pageFinished: true);
        Assert.That(session.IsFinished, Is.True);
        Assert.That(session.Pages[0].StopReason, Is.EqualTo(SettingsDiscoveryStop.SlowCallback));
    }

    [TestCase(SettingsCaptureStatus.Failed, SettingsDiscoveryStatus.Failed, SettingsDiscoveryStop.CaptureFailed)]
    [TestCase(SettingsCaptureStatus.Truncated, SettingsDiscoveryStatus.Partial, SettingsDiscoveryStop.RetentionLimit)]
    public void FailedAndTruncatedCapturesStopOnlyTheirPageWithTheAppropriateRetainedResults(
        SettingsCaptureStatus captureStatus, SettingsDiscoveryStatus pageStatus, SettingsDiscoveryStop reason)
    {
        var session = new SettingsDiscoverySession(new[] { First, Second });
        session.Complete(session.BeginNext(1)!, Capture(First, "Earlier observation"), 2, pageFinished: false);
        var buffer = new SettingsCaptureBuffer(First, "tab/2", maxRecords: 1);
        buffer.Add(SettingsCaptureKind.Text, "Retained prefix", new(0, 0, 300, 30));
        buffer.Add(SettingsCaptureKind.Text, "Over budget", new(0, 30, 300, 30));
        var capture = captureStatus == SettingsCaptureStatus.Failed ? buffer.Fail(SettingsCaptureFailure.CallbackThrew) : buffer.Complete();

        Assert.That(session.Complete(session.BeginNext(2)!, capture, 110, pageFinished: false), Is.True);

        var page = session.Pages[0];
        Assert.That(page.Status, Is.EqualTo(pageStatus));
        Assert.That(page.StopReason, Is.EqualTo(reason));
        Assert.That(page.Callbacks, Is.EqualTo(2));
        Assert.That(page.ElapsedMilliseconds, Is.EqualTo(112));
        Assert.That(page.Captures.SelectMany(c => c.Records).Select(r => r.Text),
            Is.EqualTo(captureStatus == SettingsCaptureStatus.Failed ? Array.Empty<string>() :
                new[] { "Earlier observation", "Retained prefix" }));
        Assert.That(session.BeginNext(3)!.Page, Is.SameAs(Second));
    }

    [Test]
    public void AnEqualButDifferentPageCannotPublishOrConsumeTheCurrentReceipt()
    {
        var session = new SettingsDiscoverySession(new[] { First });
        var work = session.BeginNext(1)!;
        var equalButDifferentPage = First with { };
        Assert.That(session.Complete(work, Capture(equalButDifferentPage, "Wrong page"), 2, pageFinished: true), Is.False);
        Assert.That(session.Pages[0].Captures, Is.Empty);
        Assert.That(session.Pages[0].Callbacks, Is.Zero);
        Assert.That(session.Pages[0].Status, Is.EqualTo(SettingsDiscoveryStatus.Reading));
        Assert.That(session.Complete(work, Capture(First, "Correct page"), 2, pageFinished: true), Is.True);
        Assert.That(session.Pages[0].Captures.Single().Records.Single().Text, Is.EqualTo("Correct page"));
    }

    [Test]
    public void MutableInputCollectionsAndLaterProgressCannotChangePublishedSnapshots()
    {
        var equalPage = First with { };
        var catalog = new List<SettingsPage> { First, equalPage };
        var session = new SettingsDiscoverySession(catalog);
        var pending = session.Pages;
        catalog.Clear(); catalog.Add(Second);
        var records = new List<SettingsCaptureRecord> { new(0, SettingsCaptureKind.Text, "Original", new(0, 0, 300, 30)) };
        var input = new SettingsCaptureSnapshot(First, "root", records, SettingsCaptureStatus.Partial, SettingsCaptureFailure.None);

        session.Complete(session.BeginNext(1)!, input, 1, pageFinished: true);
        var observed = session.Pages;
        records[0] = records[0] with { Text = "Mutated" };
        records.Clear();

        Assert.That(observed[0].Captures.Single().Records.Single().Text, Is.EqualTo("Original"));
        Assert.That(session.Pages[0].Captures.Single().Records.Single().Text, Is.EqualTo("Original"));
        Assert.That(pending.All(p => p.Status == SettingsDiscoveryStatus.Pending && p.Captures.Count == 0), Is.True);
        Assert.Throws<NotSupportedException>(() => ((IList<SettingsDiscoveryPage>)observed)[0] = observed[1]);
        Assert.Throws<NotSupportedException>(() => ((IList<SettingsCaptureSnapshot>)observed[0].Captures).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<SettingsCaptureRecord>)observed[0].Captures[0].Records).Clear());
        var second = session.BeginNext(2)!;
        Assert.That(second.Page, Is.SameAs(equalPage));
        Assert.That(second.PageOrdinal, Is.EqualTo(1));
        session.Complete(second, Capture(equalPage, "Independent equal page"), 2, pageFinished: true);
        Assert.That(observed[1].Status, Is.EqualTo(SettingsDiscoveryStatus.Pending));
        session.Refresh(Array.Empty<SettingsPage>());
        Assert.That(observed[0].Captures.Single().Records.Single().Text, Is.EqualTo("Original"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void NonpositiveCallbackBudgetsAreRejected(int budget)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new SettingsDiscoverySession(new[] { First }, maxCallbacksPerPage: budget));
        Assert.That(error!.ParamName, Is.EqualTo("maxCallbacksPerPage"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void NonpositiveOrNonfiniteTimeBudgetsAreRejected(double budget)
    {
        var pageError = Assert.Throws<ArgumentOutOfRangeException>(() => new SettingsDiscoverySession(new[] { First }, maxPageMilliseconds: budget));
        Assert.That(pageError!.ParamName, Is.EqualTo("maxPageMilliseconds"));
        var callbackError = Assert.Throws<ArgumentOutOfRangeException>(() => new SettingsDiscoverySession(new[] { First }, maxCallbackMilliseconds: budget));
        Assert.That(callbackError!.ParamName, Is.EqualTo("maxCallbackMilliseconds"));
    }

    [TestCase(-0.1)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void InvalidCallbackTimingCannotPublishOrConsumeTheReceipt(double elapsed)
    {
        var session = new SettingsDiscoverySession(new[] { First });
        var work = session.BeginNext(1)!;
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => session.Complete(work, Capture(First, "Invalid time"), elapsed, pageFinished: true));
        Assert.That(error!.ParamName, Is.EqualTo("elapsedMilliseconds"));
        Assert.That(session.Pages[0].Callbacks, Is.Zero);
        Assert.That(session.Pages[0].Captures, Is.Empty);
        Assert.That(session.IsFinished, Is.False);
        Assert.That(session.Complete(work, Capture(First, "Valid zero elapsed"), 0, pageFinished: true), Is.True);
    }

    [Test]
    public void InvalidCatalogRefreshLeavesTheCurrentReceiptAndObservationsIntact()
    {
        Assert.Throws<ArgumentNullException>(() => new SettingsDiscoverySession(null!));
        Assert.Throws<ArgumentException>(() => new SettingsDiscoverySession(new[] { First, null! }));
        var session = new SettingsDiscoverySession(new[] { First });
        session.Complete(session.BeginNext(1)!, Capture(First, "Retained"), 2, pageFinished: false);
        var active = session.BeginNext(2)!;

        Assert.Throws<ArgumentNullException>(() => session.Refresh(null!));
        Assert.Throws<ArgumentException>(() => session.Refresh(new[] { Second, null! }));

        Assert.That(session.Pages.Single().Captures.Single().Records.Single().Text, Is.EqualTo("Retained"));
        Assert.That(session.Pages.Single().Page, Is.SameAs(First));
        Assert.That(session.Complete(active, null, 2, pageFinished: true), Is.True);
        Assert.That(session.Pages.Single().Callbacks, Is.EqualTo(2));
    }

    [Test]
    public void MultiDrawStepCountsActualCallbacksAndKeepsIndividualTimingSeparate()
    {
        var session = new SettingsDiscoverySession(new[] { First, Second }, maxCallbacksPerPage: 5);
        Assert.That(session.Complete(session.BeginNext(1)!, Capture(First, "First path"), 120,
            pageFinished: false, callbackCount: 3, maximumCallbackMilliseconds: 40), Is.True);
        Assert.That(session.Pages[0].Callbacks, Is.EqualTo(3));
        Assert.That(session.Pages[0].ElapsedMilliseconds, Is.EqualTo(120));
        Assert.That(session.Pages[0].StopReason, Is.EqualTo(SettingsDiscoveryStop.None));
        Assert.That(session.Complete(session.BeginNext(2)!, Capture(First, "Second path"), 20,
            pageFinished: false, callbackCount: 2, maximumCallbackMilliseconds: 10), Is.True);
        Assert.That(session.Pages[0].Callbacks, Is.EqualTo(5));
        Assert.That(session.Pages[0].StopReason, Is.EqualTo(SettingsDiscoveryStop.CallbackLimit));
        Assert.That(session.BeginNext(3)!.Page, Is.SameAs(Second));
    }

    [Test]
    public void RefusalBeforeDrawingDoesNotInventACallbackAndInvalidCountsKeepTheReceipt()
    {
        var session = new SettingsDiscoverySession(new[] { First });
        var work = session.BeginNext(1)!;
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Complete(work,
            Capture(First, "Invalid"), 0, pageFinished: true, callbackCount: -1));
        Assert.That(session.Pages[0].Captures, Is.Empty);
        Assert.That(session.Complete(work, null, 0, pageFinished: true, callbackCount: 0), Is.True);
        Assert.That(session.Pages[0].Callbacks, Is.Zero);
    }

    [Test]
    public void OneSlowDrawStopsFurtherExplorationEvenWithinAMultiDrawStep()
    {
        var session = new SettingsDiscoverySession(new[] { First });
        Assert.That(session.Complete(session.BeginNext(1)!, Capture(First, "Retained"), 140,
            pageFinished: false, callbackCount: 2, maximumCallbackMilliseconds: 110), Is.True);
        Assert.That(session.Pages[0].StopReason, Is.EqualTo(SettingsDiscoveryStop.SlowCallback));
        Assert.That(session.Pages[0].Captures.Single().Records.Single().Text, Is.EqualTo("Retained"));
        Assert.That(session.BeginNext(2), Is.Null);
    }
}
