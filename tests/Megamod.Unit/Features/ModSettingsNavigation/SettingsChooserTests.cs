using System.Globalization;
using Megamod.Features.ModSettingsNavigation;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.ModSettingsNavigation;

[TestFixture]
public sealed class SettingsChooserTests
{
    private static readonly SettingsChooserLabels Labels = new("Various Authors", "Unknown author", "HugsLib-generated",
        "Vanilla Expanded", "Sarg Bjornson", "Helixien", "Untitled settings page");
    private static SettingsChooser Create(params SettingsPage[] pages) => new(pages, Labels, CultureInfo.GetCultureInfo("en-US"));

    [Test]
    public void NameSortRetainsIndependentSameNamedPagesAndTheirOpeningTokens()
    {
        var second = new SettingsPage("page-2", "open-2", "Shared", "Same mod", "Author", "same.package", "pack-2");
        var first = new SettingsPage("page-1", "open-1", "Shared", "Same mod", "Author", "same.package", "pack-1");
        var earlier = new SettingsPage("earlier", "open-earlier", "Alpha", "Another", "Author");
        var result = Create(second, first, earlier).Project(byAuthor: false);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Heading, Is.Null);
        Assert.That(result[0].Rows.Select(row => row.Page), Is.EqualTo(new[] { earlier, first, second }));
        Assert.That(result[0].Rows.Select(row => row.Page.OpenToken), Is.EqualTo(new[] { "open-earlier", "open-1", "open-2" }));
        Assert.That(result[0].Rows.All(row => row.ShowAuthor), Is.True);
    }

    [Test]
    public void AuthorModeGroupsSingletonsLastAndCanRestoreSeparateAuthorHeadings()
    {
        var chooser = Create(new("z2", "z2", "Zulu", "Z", "Zed"), new("solo", "solo", "Solo", "S", "Aardvark"),
            new("b2", "b2", "Second", "B", "Beta"), new("z1", "z1", "Alpha", "Z", "Zed"),
            new("b1", "b1", "First", "B", "Beta"));
        var grouped = chooser.Project();
        Assert.That(grouped.Select(group => group.Heading), Is.EqualTo(new[] { "Beta", "Zed", "Various Authors" }));
        Assert.That(grouped[1].Rows.Select(row => row.Title), Is.EqualTo(new[] { "Alpha", "Zulu" }));
        Assert.That(grouped[0].Rows.All(row => !row.ShowAuthor && row.DisplayGroup == "Beta"), Is.True);
        Assert.That(grouped[2].Rows.Single().ShowAuthor, Is.True);
        Assert.That(chooser.Project(groupSingles: false).Select(group => group.Heading),
            Is.EqualTo(new[] { "Aardvark", "Beta", "Zed" }));
        Assert.That(chooser.Project(byAuthor: false).Single().Rows.All(row => row.DisplayGroup is null), Is.True);
        Assert.That(chooser.Project().Select(group => group.Heading), Is.EqualTo(grouped.Select(group => group.Heading)));
    }

    [Test]
    public void NormalizationUsesPositiveProviderIdentityThenSourceAliasPrecedenceWithoutLosingRawAuthors()
    {
        var pages = new[]
        {
            new SettingsPage("1", "1", "1", "Vanilla Expanded", "sarg helixien", IsHugsLibGenerated: true),
            new SettingsPage("2", "2", "2", "EXPANDED things for VANILLA", "sarg helixien"),
            new SettingsPage("3", "3", "3", "Other", "Team SARG and HELIXIEN"),
            new SettingsPage("4", "4", "4", "Other", "Team HELIXIEN"),
            new SettingsPage("5", "5", "5", null, null),
            new SettingsPage("6", "6", "6", "Other", ""),
            new SettingsPage("7", "7", "7", "Other", "Alice, Bob and Carol")
        };
        var rows = Create(pages).Project(byAuthor: false).Single().Rows;
        Assert.That(rows.Select(row => row.NormalizedAuthor), Is.EqualTo(new[]
        { "HugsLib-generated", "Vanilla Expanded", "Sarg Bjornson", "Helixien", "Unknown author", "Unknown author", "Alice, Bob and Carol" }));
        Assert.That(rows.Select(row => row.Page.RawAuthors), Is.EqualTo(pages.Select(page => page.RawAuthors)));
        var grouped = Create(pages).Project();
        Assert.That(grouped.Select(group => group.Heading), Is.EqualTo(new[] { "Unknown author", "Various Authors" }));
        Assert.That(grouped[1].Rows, Has.Count.EqualTo(5));
    }

    [TestCase("ttINGS", new[] { "first" })]
    [TestCase("CONTENT", new[] { "first" })]
    [TestCase("raw", new[] { "first" })]
    [TestCase("BJORN", new[] { "first", "second" })]
    [TestCase("various", new[] { "solo" })]
    [TestCase("no match", new string[0])]
    public void SearchUsesAllVisibleIdentityFieldsAndFullListGroupMembership(string query, string[] expected)
    {
        var chooser = Create(new("first", "first", "First settings", "My content", "Sarg and Raw Collaborator"),
            new("second", "second", "Second", "Another mod", "sarg"),
            new("solo", "solo", "Solo", "Unrelated", "Solo author"));
        var result = chooser.Project(query);
        Assert.That(result.SelectMany(group => group.Rows).Select(row => row.Page.PageIdentity), Is.EqualTo(expected));
        Assert.That(result.All(group => group.Rows.Count > 0), Is.True);
        if (expected.Contains("first")) Assert.That(result[0].Heading, Is.EqualTo("Sarg Bjornson"));
        Assert.That(chooser.Project("").SelectMany(group => group.Rows).Select(row => row.Page.PageIdentity),
            Is.EqualTo(new[] { "first", "second", "solo" }));
    }

    [Test]
    public void NameSearchOmitsHiddenGroupButRetainsRawAndNormalizedAuthorMatches()
    {
        var chooser = Create(new("a", "open-a", "Alpha", "Content", "Sarg and Alice"),
            new("b", "open-b", "Beta", "Content", "Bob"));
        Assert.That(chooser.Project("Various", byAuthor: false), Is.Empty);
        Assert.That(chooser.Project("Various", groupSingles: false), Is.Empty);
        Assert.That(chooser.Project("ALICE", byAuthor: false).Single().Rows.Single().Page.PageIdentity, Is.EqualTo("a"));
        Assert.That(chooser.Project("BJORN", byAuthor: false).Single().Rows.Single().Page.PageIdentity, Is.EqualTo("a"));
        Assert.That(chooser.Project("Various").Single().Rows, Has.Count.EqualTo(2));
        var literal = Create(new SettingsPage("c", "c", "Various tools", "Content", "Chris"));
        Assert.That(literal.Project("various", byAuthor: false).Single().Rows.Single().Page.PageIdentity, Is.EqualTo("c"));
    }

    [TestCase("", "Content title", "package.id", "Content title")]
    [TestCase(null, "", "package.id", "package.id")]
    [TestCase(null, null, null, "Untitled settings page")]
    public void MissingPageTitleUsesSafeMetadataFallbackWithoutChangingTheOpeningIdentity(
        string? title, string? modName, string? package, string expected)
    {
        var page = new SettingsPage("page", "exact-open", title, modName, null, package);
        var row = Create(page).Project(byAuthor: false).Single().Rows.Single();
        Assert.That(row.Title, Is.EqualTo(expected));
        Assert.That(row.Page, Is.SameAs(page));
        Assert.That(Create(page).Project(expected, byAuthor: false).Single().Rows.Single().Page.OpenToken, Is.EqualTo("exact-open"));
    }

    [Test]
    public void StableTiesUseContentPackageProviderAndFinallyOriginalInputOrder()
    {
        var pages = new[]
        {
            new SettingsPage("ordinal-first", "1", "Shared", "B", "Author", "b", "b"),
            new SettingsPage("ordinal-second", "2", "Shared", "B", "Author", "b", "b"),
            new SettingsPage("provider", "3", "Shared", "B", "Author", "b", "a"),
            new SettingsPage("package", "4", "Shared", "B", "Author", "a", "z"),
            new SettingsPage("content", "5", "Shared", "A", "Author", "z", "z")
        };
        var chooser = Create(pages);
        var expected = new[] { "content", "package", "provider", "ordinal-first", "ordinal-second" };
        foreach (var byAuthor in new[] { false, true })
            Assert.That(chooser.Project("Shared", byAuthor).Single().Rows.Select(row => row.Page.PageIdentity), Is.EqualTo(expected));
    }

    [Test]
    public void ProjectionUsesCapturedLanguageAndDoesNotExposeMutableSourceOrResults()
    {
        var pages = new List<SettingsPage>
        {
            new("umlaut", "u", "Älg", "Mod", "Älg"), new("zed", "z", "Zebra", "Mod", "Zebra")
        };
        var chooser = new SettingsChooser(pages, Labels, CultureInfo.GetCultureInfo("sv-SE"));
        var captured = chooser.Project(groupSingles: false);
        pages.Clear();
        Assert.That(captured.Select(group => group.Heading), Is.EqualTo(new[] { "Zebra", "Älg" }));
        Assert.That(chooser.Project(byAuthor: false).Single().Rows.Select(row => row.Title), Is.EqualTo(new[] { "Zebra", "Älg" }));
        Assert.Throws<NotSupportedException>(() => ((IList<SettingsChooserGroup>)captured).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<SettingsChooserRow>)captured[0].Rows).Clear());
        Assert.That(captured[0].Rows.Single().Page.OpenToken, Is.EqualTo("z"));
        Assert.That(Create().Project(), Is.Empty);
        Assert.That(Create().Project(byAuthor: false), Is.Empty);
    }

    [Test]
    public void DisablingSingletonGroupingSortsEvenAnAuthorNamedLikeTheLocalizedBucketNormally()
    {
        var chooser = Create(new("z", "z", "Zulu", "Content", "Zed"),
            new("v", "v", "Various", "Content", Labels.VariousAuthors));
        Assert.That(chooser.Project(groupSingles: false).Select(group => group.Heading),
            Is.EqualTo(new[] { "Various Authors", "Zed" }));
    }

    [TestCase("Various Authors")]
    [TestCase("Verschiedene Autoren")]
    public void EnabledSingletonBucketStaysSeparateFromARealAuthorWithTheSameLocalizedName(string bucketLabel)
    {
        var chooser = new SettingsChooser(new[]
        {
            new SettingsPage("real-2", "open-real-2", "Real second", "Content", bucketLabel),
            new SettingsPage("alice", "open-alice", "Alice title", "Content", "Alice"),
            new SettingsPage("zed-2", "open-zed-2", "Zed second", "Content", "Zed"),
            new SettingsPage("real-1", "open-real-1", "Real first", "Content", bucketLabel),
            new SettingsPage("zed-1", "open-zed-1", "Zed first", "Content", "Zed")
        }, Labels with { VariousAuthors = bucketLabel }, CultureInfo.GetCultureInfo("en-US"));

        var groups = chooser.Project();
        Assert.That(groups.Select(group => group.Heading), Is.EqualTo(new[] { bucketLabel, "Zed", bucketLabel }));
        Assert.That(groups[0].Rows.Select(row => row.Page.OpenToken), Is.EqualTo(new[] { "open-real-1", "open-real-2" }));
        Assert.That(groups[0].Rows.All(row => !row.ShowAuthor && row.NormalizedAuthor == bucketLabel), Is.True);
        Assert.That(groups[2].Rows.Single().Page.OpenToken, Is.EqualTo("open-alice"));
        Assert.That(groups[2].Rows.Single().ShowAuthor, Is.True);
        Assert.That(groups[2].Rows.Single().NormalizedAuthor, Is.EqualTo("Alice"));

        var byDisplayedGroup = chooser.Project(bucketLabel);
        Assert.That(byDisplayedGroup.Select(group => group.Rows.Count), Is.EqualTo(new[] { 2, 1 }));
        Assert.That(chooser.Project(bucketLabel, byAuthor: false).Single().Rows.Select(row => row.Page.OpenToken),
            Is.EqualTo(new[] { "open-real-1", "open-real-2" }));
        var oneRealPage = chooser.Project("Real first").Single().Rows.Single();
        Assert.That(oneRealPage.DisplayGroup, Is.EqualTo(bucketLabel));
        Assert.That(oneRealPage.ShowAuthor, Is.False);
        Assert.That(chooser.Project("Alice").Single().Rows.Single().ShowAuthor, Is.True);
    }
}
