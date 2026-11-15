using System.Globalization;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.SettingsSearch;

[TestFixture]
public sealed class SettingsChooserSearchTests
{
    private static readonly SettingsChooserLabels Labels = new("Various Authors", "Unknown", "HugsLib-generated",
        "Vanilla Expanded", "Sarg Bjornson", "Helixien", "Untitled");

    [Test]
    public void HelpOnlyOptionHitSurvivesAnUnmatchedPageAndClearingRestoresOriginalGrouping()
    {
        var first = new SettingsPage("first", "first-open", "Workshop", "Example", "Alex");
        var second = new SettingsPage("second", "second-open", "Animals", "Example", "Alex");
        var groups = new SettingsChooser(new[] { first, second }, Labels, CultureInfo.InvariantCulture).Project();
        var option = new SettingsSearchEntry(first, "tab/row", 0, "Maximum", "Set work speed");
        var view = new SettingsChooserSearch(groups, new[] { option });

        var result = view.Find("\"work speed\"");

        Assert.That(result.Pages, Is.Empty);
        Assert.That(result.Options.Single().Entry, Is.SameAs(option));
        Assert.That(result.Options.Single().Entry.Page, Is.SameAs(first));
        var cleared = view.Find(" ");
        Assert.That(cleared.Options, Is.Empty);
        Assert.That(cleared.Pages.Single().Heading, Is.EqualTo("Alex"));
        Assert.That(cleared.Pages.Single().Rows.Select(row => row.Page), Is.EqualTo(groups.Single().Rows.Select(row => row.Page)));
    }

    [TestCase(true, "author:\"Sarg Bjornson\"", new[] { "alias" })]
    [TestCase(false, "author:\"Sarg Bjornson\"", new[] { "alias" })]
    [TestCase(true, "\"Various Authors\"", new[] { "alias", "solo" })]
    [TestCase(false, "\"Various Authors\"", new string[0])]
    [TestCase(true, "Factory", new[] { "first" })]
    public void SharedQueryIncludesCurrentGroupAndNormalizedAuthorWithoutRegroupingMatches(
        bool byAuthor, string query, string[] expected)
    {
        var pages = new[]
        {
            new SettingsPage("first", "first", "Factory", "Example", "Alex"),
            new SettingsPage("second", "second", "Laboratory", "Example", "Alex"),
            new SettingsPage("alias", "alias", "Food", "Cooking", "Sarg123"),
            new SettingsPage("solo", "solo", "Plants", "Growing", "Dana")
        };
        var groups = new SettingsChooser(pages, Labels, CultureInfo.InvariantCulture).Project(byAuthor: byAuthor);
        var view = new SettingsChooserSearch(groups, Array.Empty<SettingsSearchEntry>());

        var result = view.Find(query);

        Assert.That(result.Pages.SelectMany(group => group.Rows).Select(row => row.Page.PageIdentity), Is.EquivalentTo(expected));
        if (query == "Factory") Assert.That(result.Pages.Single().Heading, Is.EqualTo("Alex"));
        Assert.That(result.Options, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AnOptionFromAnotherOrCopiedPageCannotEnterThisCatalogSnapshot(bool copied)
    {
        var page = new SettingsPage("page", "open", "Tools", "Tools", "Alex");
        var groups = new SettingsChooser(new[] { page }, Labels, CultureInfo.InvariantCulture).Project();
        var stale = copied ? page with { } : page with { PageIdentity = "other", OpenToken = "other-open" };
        var option = new SettingsSearchEntry(stale, "row", 0, "Value");

        Assert.Throws<ArgumentException>(() => new SettingsChooserSearch(groups, new[] { option }));
    }

    [Test]
    public void ExactPageTextPreventsOptionTypoFallbackEvenWhenAFieldFilterExcludesThatPage()
    {
        var exact = new SettingsPage("exact", "open-exact", "Power tools", "Power tools", "Alex", "exact.mod");
        var other = new SettingsPage("other", "open-other", "Inventions", "Inventions", "Dana", "option.mod");
        var groups = new SettingsChooser(new[] { exact, other }, Labels, CultureInfo.InvariantCulture).Project();
        var view = new SettingsChooserSearch(groups, new[] { new SettingsSearchEntry(other, "row", 0, "Powder") });

        var result = view.Find("id:option.mod power");

        Assert.That(result.Pages, Is.Empty);
        Assert.That(result.Options, Is.Empty);
    }

    [Test]
    public void EqualMetadataPageOccurrencesKeepTheirExactOptionIdentities()
    {
        var first = new SettingsPage("page", "open", "Tools", "Tools", "Alex");
        var second = first with { };
        var groups = new SettingsChooser(new[] { first, second }, Labels, CultureInfo.InvariantCulture).Project();
        var firstOption = new SettingsSearchEntry(first, "one", 0, "Work speed");
        var secondOption = new SettingsSearchEntry(second, "two", 0, "Work speed");
        var options = new List<SettingsSearchEntry> { firstOption, secondOption };
        var view = new SettingsChooserSearch(groups, options);
        options.Clear();

        var result = view.Find("\"work speed\"");

        Assert.That(result.Options, Has.Count.EqualTo(2));
        Assert.That(result.Options[0].Entry, Is.SameAs(firstOption));
        Assert.That(result.Options[1].Entry, Is.SameAs(secondOption));
        Assert.That(result.Options[0].Entry.Page, Is.Not.SameAs(result.Options[1].Entry.Page));
    }

    [Test]
    public void MissingMetadataPageCanBeFoundByItsActualTranslatedFallbackTitle()
    {
        var page = new SettingsPage("unknown", "open-unknown", null, null, null);
        var labels = Labels with { UntitledPage = "Unbenannte Einstellungen" };
        var groups = new SettingsChooser(new[] { page }, labels, CultureInfo.InvariantCulture).Project();
        var view = new SettingsChooserSearch(groups, Array.Empty<SettingsSearchEntry>());

        var result = view.Find("\"Unbenannte Einstellungen\"");

        Assert.That(result.Pages.Single().Rows.Single().Page, Is.SameAs(page));
        Assert.That(result.Pages.Single().Rows.Single().Title, Is.EqualTo(labels.UntitledPage));
        Assert.That(result.Options, Is.Empty);
    }
}
