using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Settings;

[TestFixture]
public sealed class SettingsRowLayoutTests
{
    [Test]
    public void EachDrawCapturesVisibilityAndRankOnceBeforeOrderingRowsWithSections()
    {
        int visibilityReads = 0;
        int rankReads = 0;
        int rank = 2;
        bool visible = true;
        var rows = new[]
        {
            new SettingsRowLayout("z", "content", 0),
            new SettingsRowLayout("dynamic", "content", 99,
                () => { visibilityReads++; return visible; }, () => { rankReads++; return rank; }),
            new SettingsRowLayout("hidden", "content", -200, () => false,
                () => throw new InvalidOperationException("Hidden row rank must not run.")),
            new SettingsRowLayout("navigation", "content", -100),
            new SettingsRowLayout("a", "content", 0),
            new SettingsRowLayout("other", "alpha", 500)
        };

        var first = SettingsRowLayout.Capture(rows);
        rank = -1;
        var next = SettingsRowLayout.Capture(rows);
        visible = false;
        var filtered = SettingsRowLayout.Capture(rows);

        Assert.Multiple(() =>
        {
            Assert.That(first.Select(row => row.Id), Is.EqualTo(new[] { "other", "navigation", "a", "z", "dynamic" }));
            Assert.That(next.Select(row => row.Id), Is.EqualTo(new[] { "other", "navigation", "dynamic", "a", "z" }));
            Assert.That(filtered.Select(row => row.Id), Is.EqualTo(new[] { "other", "navigation", "a", "z" }));
            Assert.That(first.Single(row => row.Id == "dynamic").Order, Is.EqualTo(2));
            Assert.That(visibilityReads, Is.EqualTo(3));
            Assert.That(rankReads, Is.EqualTo(2));
        });
    }
}
