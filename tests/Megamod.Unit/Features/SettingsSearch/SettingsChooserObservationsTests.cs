using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch;
using Megamod.Features.SettingsSearch.Core;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.SettingsSearch;

[TestFixture]
public sealed class SettingsChooserObservationsTests
{
    private static readonly SettingsPage Page = new("page", "open", "Example", "Example", "Author");
    private static SettingsForeignOption Option(string label, int occurrence, string value, float y = 0) =>
        new(new(Page, "foreign:", occurrence, label, DisplayedValue: value),
            new(Page, Array.Empty<SettingsForeignTab>(), SettingsForeignControlKind.Checkbox, label, null),
            new(0, y, 200, 24), Array.Empty<int>());

    [Test]
    public void AVisibleSubsetRetainsUnseenTargetsValuesAndBounds()
    {
        var offscreen = Option("Offscreen original", 0, "old hidden", 900);
        var oldVisible = Option("Visible original", 1, "old", 200);
        var visible = Option("Visible original", 0, "new", 20); // Culling changed encounter ordinal.
        var merged = SettingsChooserObservations.MergeVisible(Page, new[] { offscreen, oldVisible }, new[] { visible });
        Assert.That(merged, Has.Count.EqualTo(2));
        Assert.That(merged[0].Entry, Is.EqualTo(offscreen.Entry));
        Assert.That(merged[0].Target.Page, Is.SameAs(offscreen.Target.Page));
        Assert.That(merged[0].Target.Label, Is.EqualTo(offscreen.Target.Label));
        Assert.That(merged[0].Entry.DisplayedValue, Is.EqualTo("old hidden"));
        Assert.That(merged[0].Bounds.Y, Is.EqualTo(900));
        Assert.That(merged[1].Entry.DisplayedValue, Is.EqualTo("new"));
        Assert.That(merged[1].Entry.Occurrence, Is.EqualTo(1));
        Assert.That(merged[1].Bounds, Is.EqualTo(visible.Bounds));
        Assert.That(SettingsChooserObservations.MergeVisible(Page, merged, Array.Empty<SettingsForeignOption>()).Select(option => option.Entry),
            Is.EqualTo(merged.Select(option => option.Entry)));
    }

    [Test]
    public void AmbiguousVisibleOccurrenceDoesNotOverwriteOrMultiplyRetainedDuplicates()
    {
        var first = Option("Same", 0, "first", 10);
        var second = Option("Same", 1, "second", 900);
        IReadOnlyList<SettingsForeignOption> merged = new[] { first, second };
        for (int repaint = 0; repaint < 10; repaint++)
            merged = SettingsChooserObservations.MergeVisible(Page, merged, new[] { Option("Same", 0, "unresolved", 30) });
        Assert.That(merged.Select(option => option.Entry), Is.EqualTo(new[] { first.Entry, second.Entry }));
        Assert.That(merged.Select(option => option.Bounds), Is.EqualTo(new[] { first.Bounds, second.Bounds }));
    }

    [Test]
    public void NewObservedControlsAreAddedOnceAndLaterFreshBoundsAreRetained()
    {
        var existing = Option("Existing", 2, "old");
        var appeared = Option("Appeared", 0, "first", 10);
        var merged = SettingsChooserObservations.MergeVisible(Page, new[] { existing }, new[] { appeared });
        Assert.That(merged, Has.Count.EqualTo(2));
        Assert.That(merged[1].Entry.Occurrence, Is.EqualTo(3));
        merged = SettingsChooserObservations.MergeVisible(Page, merged, new[] { Option("Appeared", 0, "second", 40) });
        Assert.That(merged, Has.Count.EqualTo(2));
        Assert.That(merged[1].Entry.DisplayedValue, Is.EqualTo("second"));
        Assert.That(merged[1].Bounds.Y, Is.EqualTo(40));
        Assert.That(merged[1].Entry.Occurrence, Is.EqualTo(3));
    }

    [Test]
    public void AReplacementPageCannotBorrowOldObservationsEvenWhenMetadataIsEqual()
    {
        var replacement = Page with { };
        Assert.That(() => SettingsChooserObservations.MergeVisible(replacement, new[] { Option("Old", 0, "old") },
            Array.Empty<SettingsForeignOption>()), Throws.ArgumentException);
    }

    [Test]
    public void RepeatedNewControlsCannotEvictOrGrowBeyondTheRetainedPageLimit()
    {
        IReadOnlyList<SettingsForeignOption> retained = Enumerable.Range(0, SettingsForeignRetention.MaximumOptions)
            .Select(index => Option("Retained " + index, index, "old", index)).ToArray();
        for (int draw = 0; draw < 3; draw++)
        {
            retained = SettingsChooserObservations.MergeVisible(Page, retained, new[] { Option("New " + draw, 0, "new") }, out bool truncated);
            Assert.That(truncated, Is.True);
            Assert.That(retained, Has.Count.EqualTo(SettingsForeignRetention.MaximumOptions));
            Assert.That(retained.All(option => option.Entry.Label.StartsWith("Retained ", StringComparison.Ordinal)), Is.True);
        }
    }

    [Test]
    public void AnOversizedVisibleUpdateRetainsPriorValuesAndReportsTheSharedTextLimit()
    {
        var old = Option("Original", 0, "old", 900);
        var oversized = Option("Original", 0, new string('x', SettingsForeignRetention.MaximumTextUnits), 20);
        var merged = SettingsChooserObservations.MergeVisible(Page, new[] { old }, new[] { oversized }, out bool truncated);
        Assert.That(truncated, Is.True);
        Assert.That(merged.Single().Entry.DisplayedValue, Is.EqualTo("old"));
        Assert.That(merged.Single().Bounds, Is.EqualTo(old.Bounds));
    }

    [Test]
    public void TypedControlsAndUnpairedObservationsShareOneBoundedPagePrefix()
    {
        var retention = new SettingsForeignRetention();
        for (int index = 0; index < SettingsForeignRetention.MaximumOptions - 1; index++)
            Assert.That(retention.TryRetain(Option("Typed " + index, index, "old"), out _), Is.True);
        var text = new SettingsCaptureRecord(0, SettingsCaptureKind.Text, "Custom original text", new(0, 900, 200, 24));
        var tooltip = new SettingsCaptureRecord(1, SettingsCaptureKind.Tooltip, "Custom help", new(0, 900, 200, 24));
        Assert.That(retention.TryRetainUnpaired(Page, "custom", text, out var retained), Is.True);
        Assert.That(retained.Page, Is.SameAs(Page));
        Assert.That(retained.Label, Is.EqualTo(text.Text));
        Assert.That(retained.PathIdentity, Is.EqualTo("custom:unpaired"));
        Assert.That(retention.TryRetainUnpaired(Page, "custom", tooltip, out _), Is.False);
        Assert.That(retention.Count, Is.EqualTo(SettingsForeignRetention.MaximumOptions));

        // Generated fallback paths must also fit the shared text budget before allocation.
        retention = new SettingsForeignRetention();
        Assert.That(retention.TryRetain(Option("Typed", 0, new string('x', SettingsForeignRetention.MaximumTextUnits - 32)), out _), Is.True);
        Assert.That(retention.TryRetainUnpaired(Page, "custom", tooltip, out _), Is.False);
        Assert.That(retention.Count, Is.EqualTo(1));
    }

    [Test]
    public void SourceReplacementQueuesOneRefreshWithoutRepeatingPendingOrFailedAcquisition()
    {
        var refresh = new SettingsChooserSourceRefresh();
        object first = new(), replacement = new();
        Assert.That(refresh.ObserveSource(first), Is.True);
        Assert.That(refresh.NeedsRequest, Is.True);
        refresh.MarkRequested();
        for (int repaint = 0; repaint < 3; repaint++)
        {
            Assert.That(refresh.ObserveSource(first), Is.False);
            Assert.That(refresh.NeedsRequest, Is.False);
        }
        Assert.That(refresh.ObserveSource(replacement), Is.True);
        Assert.That(refresh.NeedsRequest, Is.True);
        refresh.MarkRequested();
        Assert.That(refresh.ObserveSource(replacement), Is.False);
        Assert.That(refresh.NeedsRequest, Is.False);
        refresh.RequestRefresh();
        Assert.That(refresh.NeedsRequest, Is.True);
    }

    [TestCase(SettingsDiscoveryStop.CallbackLimit, null, "ReasonBudget")]
    [TestCase(SettingsDiscoveryStop.None, "settings-reader-prerequisites-changed", "ReasonChanged")]
    [TestCase(SettingsDiscoveryStop.CaptureFailed, "settings-read-window-construction", "ReasonUnsupported")]
    [TestCase(SettingsDiscoveryStop.None, "settings-foreign-tab-state-limit", "ReasonBudget")]
    [TestCase(SettingsDiscoveryStop.None, "settings-foreign-slow-callback", "ReasonBudget")]
    [TestCase(SettingsDiscoveryStop.CaptureFailed, "settings-reader-callback-failed", "ReasonCallback")]
    [TestCase(SettingsDiscoveryStop.None, null, null)]
    public void CoverageUsesHumanReasonCategories(SettingsDiscoveryStop stop, string? failure, string? expected) =>
        Assert.That(SettingsChooserObservations.ReasonKey(stop, failure), Is.EqualTo(expected));
}
