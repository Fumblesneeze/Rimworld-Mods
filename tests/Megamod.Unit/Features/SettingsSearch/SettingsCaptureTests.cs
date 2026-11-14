using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.SettingsSearch;

[TestFixture]
public sealed class SettingsCaptureTests
{
    private static readonly SettingsPage Page = new("provider-page-2", "open-2", "Options", "Unfamiliar Mod",
        "Another Author", "unfamiliar.package");
    private static readonly SettingsCaptureRect Bounds = new(12, 24, 300, 30);

    [Test]
    public void CompletedDrawRetainsDistinctOccurrencesAndOriginalTextWithOpaquePageIdentity()
    {
        var buffer = new SettingsCaptureBuffer(Page, "tab/2");
        buffer.Add(SettingsCaptureKind.Text, "<b>Haul</b>", Bounds);
        buffer.Add(SettingsCaptureKind.Text, "<b>Haul</b>", Bounds);
        buffer.Add(SettingsCaptureKind.Tooltip, "Avoid bleeding", Bounds);

        var snapshot = buffer.Complete();

        Assert.That(snapshot.Page, Is.SameAs(Page));
        Assert.That(snapshot.PathIdentity, Is.EqualTo("tab/2"));
        Assert.That(snapshot.Status, Is.EqualTo(SettingsCaptureStatus.Partial));
        Assert.That(snapshot.Failure, Is.EqualTo(SettingsCaptureFailure.None));
        Assert.That(snapshot.Records, Is.EqualTo(new[]
        {
            new SettingsCaptureRecord(0, SettingsCaptureKind.Text, "<b>Haul</b>", Bounds),
            new SettingsCaptureRecord(1, SettingsCaptureKind.Text, "<b>Haul</b>", Bounds),
            new SettingsCaptureRecord(2, SettingsCaptureKind.Tooltip, "Avoid bleeding", Bounds)
        }));
    }

    [TestCase(2, 100, false)]
    [TestCase(2, 100, true)]
    [TestCase(100, 8, false)]
    [TestCase(100, 8, true)]
    [TestCase(100, 10, true)]
    public void RetentionLimitKeepsOnlyWholeRecordsAndStopsAfterFirstOverflow(int maxRecords, int maxTextUnits,
        bool exceed)
    {
        var buffer = new SettingsCaptureBuffer(Page, "root", maxRecords, maxTextUnits);
        buffer.Add(SettingsCaptureKind.Text, "abcd", Bounds);
        buffer.Add(SettingsCaptureKind.Tooltip, "efgh", Bounds);
        if (exceed)
        {
            buffer.Add(SettingsCaptureKind.Text, "too much", Bounds);
            buffer.Add(SettingsCaptureKind.Text, "x", Bounds);
        }

        var snapshot = buffer.Complete();

        Assert.That(snapshot.Status, Is.EqualTo(exceed ? SettingsCaptureStatus.Truncated : SettingsCaptureStatus.Partial));
        Assert.That(snapshot.Failure, Is.EqualTo(SettingsCaptureFailure.None));
        Assert.That(snapshot.Records.Select(record => record.Text), Is.EqualTo(new[] { "abcd", "efgh" }));
    }

    [TestCase(SettingsCaptureFailure.None)]
    [TestCase(SettingsCaptureFailure.CallbackThrew)]
    [TestCase(SettingsCaptureFailure.BackendReplaced)]
    [TestCase(SettingsCaptureFailure.RecordingFailed)]
    public void PublicationIsImmutableAndTerminalAndFailedCallbacksDiscardTheirRecords(SettingsCaptureFailure failure)
    {
        var buffer = new SettingsCaptureBuffer(Page, "root");
        buffer.Add(SettingsCaptureKind.Text, "Unfinished page", Bounds);

        var snapshot = failure == SettingsCaptureFailure.None ? buffer.Complete() : buffer.Fail(failure);

        Assert.That(snapshot.Failure, Is.EqualTo(failure));
        Assert.That(snapshot.Status, Is.EqualTo(failure == SettingsCaptureFailure.None ?
            SettingsCaptureStatus.Partial : SettingsCaptureStatus.Failed));
        Assert.That(snapshot.Records.Count, Is.EqualTo(failure == SettingsCaptureFailure.None ? 1 : 0));
        if (snapshot.Records is IList<SettingsCaptureRecord> editable)
        {
            Assert.Throws<NotSupportedException>(() => editable.Add(new(99, SettingsCaptureKind.Text, "Changed", Bounds)));
            if (editable.Count != 0)
                Assert.Throws<NotSupportedException>(() => editable[0] = new(99, SettingsCaptureKind.Text, "Changed", Bounds));
        }
        Assert.Throws<InvalidOperationException>(() => buffer.Add(SettingsCaptureKind.Text, "Late", Bounds));
        Assert.Throws<InvalidOperationException>(() => buffer.Complete());
        Assert.Throws<InvalidOperationException>(() => buffer.Fail(SettingsCaptureFailure.CallbackThrew));
        Assert.That(snapshot.Records.Count, Is.EqualTo(failure == SettingsCaptureFailure.None ? 1 : 0));
    }

    [TestCase(float.NaN, 0, 10, 10)]
    [TestCase(0, float.PositiveInfinity, 10, 10)]
    [TestCase(0, 0, float.NaN, 10)]
    [TestCase(0, 0, 10, float.NegativeInfinity)]
    [TestCase(0, 0, 0, 10)]
    [TestCase(0, 0, 10, -1)]
    [TestCase(float.MaxValue, 0, float.MaxValue, 10)]
    public void InvalidGeometryIsOmittedWithoutConsumingBudgetOrDiscardingOtherObservations(
        float x, float y, float width, float height)
    {
        var buffer = new SettingsCaptureBuffer(Page, "root", maxRecords: 2, maxTextUnits: 10);
        buffer.Add(SettingsCaptureKind.Text, "first", Bounds);
        buffer.Add(SettingsCaptureKind.Text, "unusable observation", new(x, y, width, height));
        buffer.Add(SettingsCaptureKind.Tooltip, "last", Bounds);

        var snapshot = buffer.Complete();

        Assert.That(snapshot.Status, Is.EqualTo(SettingsCaptureStatus.Partial));
        Assert.That(snapshot.Failure, Is.EqualTo(SettingsCaptureFailure.None));
        Assert.That(snapshot.Records.Select(record => (record.Occurrence, record.Text)),
            Is.EqualTo(new[] { (0, "first"), (1, "last") }));
    }

    [Test]
    public void EmptyTextIsIgnoredAndNegativeScreenCoordinatesRemainUsable()
    {
        var buffer = new SettingsCaptureBuffer(Page, "root", maxRecords: 1, maxTextUnits: 4);
        var offscreen = new SettingsCaptureRect(-30, -20, 100, 10);
        buffer.Add(SettingsCaptureKind.Text, null, Bounds);
        buffer.Add(SettingsCaptureKind.Text, string.Empty, Bounds);
        buffer.Add(SettingsCaptureKind.Text, "text", offscreen);
        buffer.Add(SettingsCaptureKind.Text, string.Empty, Bounds);

        var snapshot = buffer.Complete();

        Assert.That(snapshot.Status, Is.EqualTo(SettingsCaptureStatus.Partial));
        Assert.That(snapshot.Records, Is.EqualTo(new[] { new SettingsCaptureRecord(0, SettingsCaptureKind.Text, "text", offscreen) }));
    }

    [Test]
    public void InvalidLimitsAndMissingFailureReasonAreRejectedBeforePublication()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SettingsCaptureBuffer(Page, "root", maxRecords: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SettingsCaptureBuffer(Page, "root", maxTextUnits: -1));
        var buffer = new SettingsCaptureBuffer(Page, "root");
        buffer.Add(SettingsCaptureKind.Text, "Valid", Bounds);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Fail(SettingsCaptureFailure.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Fail((SettingsCaptureFailure)42));
        Assert.That(buffer.Complete().Records.Single().Text, Is.EqualTo("Valid"));
    }

    [TestCase(0, 10)]
    [TestCase(10, 0)]
    public void ZeroRetentionBudgetReportsTruncationForTheFirstNonemptyRecord(int maxRecords, int maxTextUnits)
    {
        var buffer = new SettingsCaptureBuffer(Page, "root", maxRecords, maxTextUnits);
        buffer.Add(SettingsCaptureKind.Text, "A", Bounds);
        var snapshot = buffer.Complete();
        Assert.That(snapshot.Status, Is.EqualTo(SettingsCaptureStatus.Truncated));
        Assert.That(snapshot.Records, Is.Empty);
    }
}
