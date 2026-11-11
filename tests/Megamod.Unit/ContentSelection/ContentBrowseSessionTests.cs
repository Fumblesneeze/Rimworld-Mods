using Megamod.Features.ContentSelection.Core;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.ContentSelection;

[TestFixture]
public sealed class ContentBrowseSessionTests
{
    [Test]
    public void CommonStoreRevisionRefreshesRanksWithoutChangingEarlierViewsOrFilters()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var first = new ContentChoiceRow(settings, new("one", "Incident", "A"), new object(), "Alpha", "Incident", "Source");
        var second = new ContentChoiceRow(settings, new("one", "Incident", "B"), new object(), "Beta", "Incident", "Source");
        var session = new ContentBrowseSession(store, new[] { first, second });
        session.SelectSource("one");
        var earlier = session.VisibleRanks();
        Assert.That(earlier[first.Key], Is.Zero);
        store.SetGlobal(second.Definition, false);
        var current = session.VisibleRanks();
        Assert.Multiple(() =>
        {
            Assert.That(current[second.Key], Is.Zero);
            Assert.That(earlier[first.Key], Is.Zero);
            Assert.That(session.SourcePackageId, Is.EqualTo("one"));
            Assert.That(session.Query, Is.Empty);
        });
    }

    [Test]
    public void NativeMenuChoicesStayIndependentAndInvalidIdentityDoesNotSubstituteOrEdit()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var first = new ContentChoiceRow(settings, new("one", "Incident", "First"), new object(), "First", "Incident", "Same");
        var second = new ContentChoiceRow(settings, new("two", "Incident", "Second"), new object(), "Second", "Incident", "Same");
        var session = new ContentBrowseSession(store, new[] { first, second });
        var revision = store.Revision;
        session.SetQuery("First");
        Assert.That(session.SelectType("Incident"), Is.True);
        Assert.That(session.SelectSource("two"), Is.True, "A choice hidden by another filter remains reachable.");
        Assert.That(session.VisibleRanks(), Is.Empty);
        Assert.That(session.SelectSource("missing"), Is.False);
        Assert.That(session.SourcePackageId, Is.EqualTo("two"));
        Assert.That(session.SelectSource(null), Is.True);
        Assert.That(session.VisibleRanks().Keys, Is.EqualTo(new[] { first.Key }));
        Assert.That(session.Query, Is.EqualTo("First"));
        Assert.That(session.TypeName, Is.EqualTo("Incident"));
        Assert.That(store.Revision, Is.EqualTo(revision));
        session.Reset();
        Assert.That(session.VisibleRanks().Count, Is.EqualTo(2));
    }
}
