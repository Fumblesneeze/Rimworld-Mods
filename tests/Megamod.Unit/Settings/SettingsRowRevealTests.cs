using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Settings;

[TestFixture]
public sealed class SettingsRowRevealTests
{
    [Test]
    public void RefusingAnUnknownRegisteredRowReplacesAnActiveRevealAndPreservesTheViewport()
    {
        var reveal = new SettingsRowReveal();
        var layout = new[] { new SettingsRowRevealPlacement("target", 740, 40) };
        reveal.Request("target", 4);
        reveal.Advance(4, layout, 0, 100, 1000, 50, isRepaint: true);

        reveal.Refuse();

        Assert.That(reveal.Status, Is.EqualTo(SettingsRowRevealStatus.Unavailable));
        var next = reveal.Advance(4, layout, 19, 100, 1000, 51, isRepaint: true);
        Assert.That(next.ScrollY, Is.EqualTo(19));
        Assert.That(next.HighlightedRowId, Is.Null, "A later layout must not revive a refused reveal.");
    }

    [Test]
    public void FreshLayoutRevealsTheRowForThreeVisibleRepaintSecondsAndUserCancellationStopsIt()
    {
        var reveal = new SettingsRowReveal();
        Assert.That(reveal.Request("target", 4), Is.EqualTo(SettingsRowRevealStatus.Pending));
        var earlierLayout = new[] { new SettingsRowRevealPlacement("target", 600, 40) };
        var waiting = reveal.Advance(4, earlierLayout, 0, 100, 1000, 10, isRepaint: false);
        Assert.That(waiting.ScrollY, Is.Zero);
        Assert.That(waiting.HighlightedRowId, Is.Null);
        Assert.That(reveal.Status, Is.EqualTo(SettingsRowRevealStatus.Pending));

        // A preceding dynamic row grew before the first actual repaint.
        var currentLayout = new[] { new SettingsRowRevealPlacement("target", 740, 40) };
        var visible = reveal.Advance(4, currentLayout, 0, 100, 1000, 50, isRepaint: true);
        Assert.That(visible.ScrollY, Is.InRange(680f, 740f), "The current complete row must fit in the viewport.");
        Assert.That(visible.HighlightedRowId, Is.EqualTo("target"));
        Assert.That(reveal.Status, Is.EqualTo(SettingsRowRevealStatus.Highlighting));
        var continuing = reveal.Advance(4, currentLayout, visible.ScrollY, 100, 1000, 52.999, isRepaint: true);
        Assert.That(continuing.HighlightedRowId, Is.EqualTo("target"));
        var expired = reveal.Advance(4, currentLayout, visible.ScrollY, 100, 1000, 53, isRepaint: true);
        Assert.That(expired.HighlightedRowId, Is.Null);
        Assert.That(reveal.Status, Is.EqualTo(SettingsRowRevealStatus.Expired));

        reveal.Request("target", 4);
        Assert.That(reveal.Advance(4, currentLayout, 0, 100, 1000, 100, isRepaint: true).HighlightedRowId,
            Is.EqualTo("target"));
        reveal.Cancel();
        var cancelled = reveal.Advance(4, currentLayout, 19, 100, 1000, 101, isRepaint: true);
        Assert.That(cancelled.HighlightedRowId, Is.Null);
        Assert.That(cancelled.ScrollY, Is.EqualTo(19), "Cancellation must preserve the player's new scroll position.");
        Assert.That(reveal.Status, Is.EqualTo(SettingsRowRevealStatus.Cancelled));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ChangedActivationCancelsPendingOrVisibleRevealWithoutTouchingPlayerScroll(bool alreadyVisible)
    {
        var reveal = new SettingsRowReveal();
        var layout = new[] { new SettingsRowRevealPlacement("target", 740, 40) };
        reveal.Request("target", 4);
        if (alreadyVisible) reveal.Advance(4, layout, 0, 100, 1000, 50, isRepaint: true);

        var changed = reveal.Advance(5, layout, 19, 100, 1000, 51, isRepaint: true);

        Assert.That(changed.HighlightedRowId, Is.Null);
        Assert.That(changed.ScrollY, Is.EqualTo(19));
        Assert.That(reveal.Status, Is.EqualTo(SettingsRowRevealStatus.Cancelled));
        Assert.That(reveal.Advance(4, layout, 19, 100, 1000, 52, isRepaint: true).HighlightedRowId, Is.Null,
            "Returning to an old context cannot revive its cancelled reveal.");
    }

    [TestCase("missing")]
    [TestCase("hidden-after-visible")]
    [TestCase("duplicate")]
    public void MissingHiddenOrAmbiguousCurrentPlacementRefusesRevealWithoutGuessing(string condition)
    {
        var reveal = new SettingsRowReveal();
        var row = new SettingsRowRevealPlacement("target", 740, 40);
        reveal.Request("target", 4);
        if (condition == "hidden-after-visible") reveal.Advance(4, new[] { row }, 0, 100, 1000, 50, isRepaint: true);
        var layout = condition == "duplicate" ? new[] { row, new SettingsRowRevealPlacement("target", 200, 40) } : Array.Empty<SettingsRowRevealPlacement>();
        SettingsRowRevealFrame refused = default;

        Assert.DoesNotThrow(() => refused = reveal.Advance(4, layout, 19, 100, 1000, 51, isRepaint: true));
        Assert.That(refused.HighlightedRowId, Is.Null);
        Assert.That(refused.ScrollY, Is.EqualTo(19));
        Assert.That(reveal.Status, Is.EqualTo(SettingsRowRevealStatus.Unavailable));
        Assert.That(reveal.Advance(4, new[] { row }, 19, 100, 1000, 52, isRepaint: true).HighlightedRowId, Is.Null,
            "A later layout cannot silently revive the failed request.");
    }

    [TestCase("row-top")]
    [TestCase("row-height")]
    [TestCase("row-outside-content")]
    [TestCase("overflowing-row")]
    [TestCase("viewport")]
    [TestCase("content")]
    [TestCase("scroll")]
    [TestCase("clock")]
    public void InvalidLayoutOrClockRefusesRevealAndPreservesTheSuppliedScroll(string condition)
    {
        var reveal = new SettingsRowReveal();
        var row = new SettingsRowRevealPlacement("target", 740, 40);
        float scroll = 19, viewport = 100, content = 1000;
        double now = 50;
        switch (condition)
        {
            case "row-top": row = row with { Top = float.NaN }; break;
            case "row-height": row = row with { Height = 0 }; break;
            case "row-outside-content": row = row with { Top = 990 }; break;
            case "overflowing-row": row = row with { Top = float.MaxValue, Height = float.MaxValue }; content = float.MaxValue; break;
            case "viewport": viewport = 0; break;
            case "content": content = float.PositiveInfinity; break;
            case "scroll": scroll = float.NaN; break;
            case "clock": now = double.NaN; break;
        }
        reveal.Request("target", 4);
        SettingsRowRevealFrame refused = default;

        Assert.DoesNotThrow(() => refused = reveal.Advance(4, new[] { row }, scroll, viewport, content, now, isRepaint: true));
        Assert.That(refused.HighlightedRowId, Is.Null);
        Assert.That(refused.ScrollY, Is.EqualTo(scroll));
        Assert.That(reveal.Status, Is.EqualTo(SettingsRowRevealStatus.Unavailable));
    }

    [Test]
    public void ReorderedAndResizedRowsUseTheirCurrentBoundsWithoutRestartingTheHighlightClock()
    {
        var reveal = new SettingsRowReveal();
        reveal.Request("target", 4);
        var first = reveal.Advance(4, new[] { new SettingsRowRevealPlacement("target", 740, 40) },
            0, 100, 1000, 50, isRepaint: true);
        var changed = new[] { new SettingsRowRevealPlacement("other", 740, 40), new SettingsRowRevealPlacement("target", 200, 80) };

        var current = reveal.Advance(4, changed, first.ScrollY, 100, 1000, 51, isRepaint: true);

        Assert.That(current.HighlightedRowId, Is.EqualTo("target"));
        Assert.That(current.ScrollY, Is.InRange(180f, 200f));
        Assert.That(reveal.Advance(4, changed, current.ScrollY, 100, 1000, 53, isRepaint: true).HighlightedRowId, Is.Null);
    }
}
