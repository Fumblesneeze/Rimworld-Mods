using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using NUnit.Framework;

namespace Megamod.Unit.Features.SettingsSearch;

[TestFixture]
public sealed class SettingsForeignBudgetTests
{
    private static readonly SettingsPage Page = new("page", "open", "Foreign", "Foreign", "Author", "foreign");

    [Test]
    public void HighlightExpiresByElapsedTimeEvenWithThousandsOfRepaints()
    {
        var lifetime = new SettingsForeignRevealLifetime();
        lifetime.Start();
        Assert.That(lifetime.NextRepaint(10), Is.EqualTo(SettingsForeignRevealStatus.Pending));
        lifetime.Visible(10);
        for (int i = 0; i < 3000; i++)
            Assert.That(lifetime.NextRepaint(10 + i / 1000d), Is.EqualTo(SettingsForeignRevealStatus.Highlighting));
        // Repeated visible observations must not extend the original three-second clock.
        lifetime.Visible(12.9);
        Assert.That(lifetime.NextRepaint(13), Is.EqualTo(SettingsForeignRevealStatus.Expired));
        lifetime.Start();
        for (int i = 0; i < 120; i++) Assert.That(lifetime.NextRepaint(20), Is.EqualTo(SettingsForeignRevealStatus.Pending));
        Assert.That(lifetime.NextRepaint(20), Is.EqualTo(SettingsForeignRevealStatus.Unavailable));
    }

    [Test]
    public void SlowDrawStopsExplorationButRestorationStillCounts()
    {
        var budget = new SettingsForeignReadBudget();
        budget.RecordDraw(101);
        Assert.That(budget.CanExplore(1), Is.False);
        Assert.That(budget.StopReason, Is.EqualTo("settings-foreign-slow-callback"));
        budget.RecordDraw(25); // The mandatory original tab restoration is not exploration.
        Assert.That(budget.DrawCount, Is.EqualTo(2));
        Assert.That(budget.ElapsedMilliseconds, Is.EqualTo(126));
    }

    [Test]
    public void IndividualCallbacksAndAggregatePageBudgetAreMeasuredSeparately()
    {
        var budget = new SettingsForeignReadBudget();
        for (int i = 0; i < 3; i++) budget.RecordDraw(40);
        Assert.That(budget.CanExplore(3), Is.True);
        Assert.That(budget.StopReason, Is.Null);
        for (int i = 0; i < 4; i++) budget.RecordDraw(95);
        Assert.That(budget.ElapsedMilliseconds, Is.EqualTo(500));
        Assert.That(budget.CanExplore(0), Is.False);
        Assert.That(budget.StopReason, Is.EqualTo("settings-foreign-time-limit"));
    }

    [Test]
    public void DrawLimitReservesMandatoryRestorationBeforeStartingAnotherCallback()
    {
        var budget = new SettingsForeignReadBudget();
        for (int i = 0; i < 93; i++) budget.RecordDraw(1);
        Assert.That(budget.CanExplore(2), Is.True);
        Assert.That(budget.CanExplore(3), Is.False);
        Assert.That(budget.StopReason, Is.EqualTo("settings-foreign-draw-limit"));
    }

    [Test]
    public void TypedAndUnpairedEntriesShareTextCountAndInternedPathBudget()
    {
        var retention = new SettingsForeignRetention();
        var target = new SettingsForeignTarget(Page, new[] { new SettingsForeignTab("group", "tab") },
            SettingsForeignControlKind.Checkbox, "Typed", null);
        var option = new SettingsForeignOption(new(Page, "ignored", 0, "Typed"), target, new(0, 0, 300, 30), Array.Empty<int>());
        Assert.That(retention.TryRetain(option, out var first), Is.True);
        Assert.That(retention.TryRetain(option, out var second), Is.True);
        Assert.That(second.Target.Tabs, Is.SameAs(first.Target.Tabs));
        var raw = new SettingsCaptureRecord(0, SettingsCaptureKind.Tooltip, "Manual help", new(0, 0, 300, 30));
        Assert.That(retention.TryRetainUnpaired(Page, "source", raw, out var rawFirst), Is.True);
        Assert.That(retention.TryRetainUnpaired(Page, "source", raw, out var rawSecond), Is.True);
        Assert.That(rawSecond.PathIdentity, Is.SameAs(rawFirst.PathIdentity));
        Assert.That(rawFirst.Help, Is.EqualTo("Manual help"));
        Assert.That(retention.Count, Is.EqualTo(4));
        Assert.That(retention.TryRetainUnpaired(Page, new string('p', 140000), raw, out _), Is.False);
        Assert.That(retention.Count, Is.EqualTo(4));
    }

    [Test]
    public void RawCapturesFromDifferentTabStatesShareTheSameRetainedTextBudget()
    {
        var retention = new SettingsForeignRetention();
        var record = new SettingsCaptureRecord(0, SettingsCaptureKind.Text, new string('x', 140000), new(0, 0, 300, 30));
        var first = new SettingsCaptureSnapshot(Page, "first", new[] { record }, SettingsCaptureStatus.Partial, SettingsCaptureFailure.None);
        Assert.That(retention.TryRetainCapture(first, out var retainedFirst), Is.True);
        Assert.That(retainedFirst.Records, Has.Count.EqualTo(1));
        Assert.That(retention.TryRetainCapture(first with { PathIdentity = "second" }, out var retainedSecond), Is.False);
        Assert.That(retainedSecond.Status, Is.EqualTo(SettingsCaptureStatus.Truncated));
        Assert.That(retainedSecond.Records, Is.Empty);
        Assert.That(retention.Count, Is.EqualTo(1));
    }
}
