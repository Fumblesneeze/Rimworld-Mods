using Megamod.Features.ContentSelection.Core;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.ContentSelection;

[TestFixture]
public sealed class ContentChoiceBrowserTests
{
    [Test]
    public void RequestedExclusionsSortFirstWithStableReadableAndIdentityTies()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        ContentChoiceRow Row(string source, string name, string label) => new(settings,
            new(source, "Incident", name), new object(), label, "Incident", "Same source name");
        var allowedB = Row("source.b", "Second", "Alpha");
        var allowedA = Row("source.a", "First", "Alpha");
        var excluded = Row("source.a", "Excluded", "Zulu");
        store.SetGlobal(excluded.Definition, false);
        var browser = new ContentChoiceBrowser(store, new[] { allowedB, excluded, allowedA });
        Assert.That(browser.Select(""), Is.EqualTo(new[] { excluded, allowedA, allowedB }));
        var binding = store.ActivateSave(new SaveSettingsSnapshot("colony"));
        store.SetUseGlobal(binding, excluded.Definition, false);
        store.SetSaveOverride(binding, excluded.Definition, true);
        Assert.That(browser.Select(""), Is.EqualTo(new[] { allowedA, allowedB, excluded }));
        Assert.That(store.GetGlobal(excluded.Definition), Is.False);
    }

    [Test]
    public void MainSearchIsLiteralWithinIndividualFieldsAndCombinesExactIdentityFiltersWithoutEditing()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var split = new ContentChoiceRow(settings, new("source.one", "Incident", "Split"), new object(), "Alpha", "Incident", "Beta");
        var matching = new ContentChoiceRow(settings, new("source.one", "Incident", "Match"), new object(), "Alpha Beta", "Incident", "Same source name");
        var otherSource = new ContentChoiceRow(settings, new("source.two", "Incident", "Match"), new object(), "Alpha Beta", "Incident", "Same source name");
        var otherType = new ContentChoiceRow(settings, new("source.one", "Other", "Match"), new object(), "Alpha Beta", "Other", "Same source name");
        store.SetGlobal(matching.Definition, false);
        var before = store.Revision;
        var browser = new ContentChoiceBrowser(store, new[] { split, otherSource, matching, otherType });

        Assert.That(browser.Select("aLPHa bETA", "Incident", "source.one"), Is.EqualTo(new[] { matching }));
        Assert.That(browser.Select("Match", "Incident", "source.two"), Is.EqualTo(new[] { otherSource }));
        Assert.That(browser.Select("alpha.*"), Is.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(store.Revision, Is.EqualTo(before));
            Assert.That(store.GetGlobal(matching.Definition), Is.False);
            Assert.That(store.CurrentSaveIdentity, Is.Null);
        });
    }
}
