using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch;
using NUnit.Framework;
using Search = Megamod.Features.SettingsSearch.SettingsSearch;

namespace Megamod.Unit.Tests.Features.SettingsSearch;

[TestFixture]
public sealed class SettingsSearchTests
{
    private static SettingsPage Page(string identity = "page-a", string title = "Example Mod",
        string authors = "Alex", string package = "unfamiliar.package") =>
        new(identity, "open-" + identity, title, title, authors, package);

    [Test]
    public void UnfamiliarAndDuplicatePagesRemainIndependentWhenOnlyHelpMatches()
    {
        var first = new SettingsSearchEntry(Page(), "tab-0/control", 0, "Maximum", "Set work speed");
        var second = first with { Page = Page("page-b"), Occurrence = 1 };
        var irrelevant = first with { Label = "Food", Help = "Set meal size" };
        var search = new Search(new[] { second, irrelevant, first });

        var hits = search.Find("WORK speed");

        Assert.That(hits.Select(hit => hit.Entry), Is.EquivalentTo(new[] { first, second }));
        Assert.That(hits.Select(hit => hit.Entry.Page.OpenToken),
            Is.EquivalentTo(new[] { "open-page-a", "open-page-b" }));
        Assert.That(hits.All(hit => !hit.UsesTypo && !hit.LabelMatch), Is.True);
    }

    [TestCase("mod:\"Example Mod\" \"work speed\" -animals", new[] { "work" })]
    [TestCase("AUTHOR:alex tab:production", new[] { "work" })]
    [TestCase("id:unfamiliar.package -tab:animals", new[] { "work", "literal" })]
    [TestCase("-animals", new[] { "work", "literal" })]
    [TestCase("\"work speed", new[] { "work", "animals" })]
    [TestCase("def:Thing", new[] { "literal" })]
    [TestCase("id:pretend.package", new string[0])]
    [TestCase("mod:pretend.package", new string[0])]
    [TestCase("tab:Alex", new string[0])]
    [TestCase("work maximum", new[] { "work" })]
    [TestCase("\"speed maximum\"", new string[0])]
    [TestCase("", new string[0])]
    [TestCase(" \t\r\n", new string[0])]
    public void QueryLanguageRestrictsNamedFieldsAndKeepsOtherTermsLiteral(string query, string[] expected)
    {
        var search = new Search(new[]
        {
            new SettingsSearchEntry(Page(), "work", 0, "Maximum", "Set work speed; pretend.package", Tab: "Production"),
            new SettingsSearchEntry(Page(), "animals", 0, "Animals", "Set work speed", Tab: "Animals"),
            new SettingsSearchEntry(Page(), "literal", 0, "def:Thing", Tab: "Advanced")
        });

        Assert.That(search.Find(query).Select(hit => hit.Entry.PathIdentity), Is.EquivalentTo(expected));
    }

    [TestCase("<color=#ffeeaa><b>Cafe\u0301</b></color>\n\tSpeed", "\"CAFÉ  speed\"", true)]
    [TestCase("<color=#ffeeaa><b>Cafe\u0301</b></color>", "color", false)]
    [TestCase("<size=14>Скорость работы</size>", "СКОРОСТЬ", true)]
    [TestCase("工作速度", "速度", true)]
    [TestCase("作業速度", "作業", true)]
    [TestCase("Value < 5", "\"value < 5\"", true)]
    [TestCase("Work<br>speed", "\"work speed\"", true)]
    public void ReadableTextIgnoresMarkupAndNormalizesUnicodeAndWhitespace(string label, string query, bool matches)
    {
        var entry = new SettingsSearchEntry(Page(), "one", 0, label);
        var hits = new Search(new[] { entry }).Find(query);
        Assert.That(hits.Count, Is.EqualTo(matches ? 1 : 0));
        if (matches) Assert.That(hits[0].Entry.Label, Is.EqualTo(label), "Display text is never rewritten.");
    }

    [Test]
    public void LabelRankingUsesStableIdentityAndPreservesEveryOccurrence()
    {
        var first = new SettingsSearchEntry(Page(), "tab-a", 2, "Heating");
        var laterOccurrence = first with { Occurrence = 10 };
        var laterPath = first with { PathIdentity = "tab-z" };
        var laterPage = first with { Page = Page("page-z") };
        var helpOnly = first with { Label = "Maximum", Help = "Heat of meals", PathIdentity = "a" };
        var supplied = new[] { helpOnly, laterPage, laterPath, laterOccurrence, first };
        var search = new Search(supplied);
        supplied[4] = first with { Label = "Changed after capture" };

        var hits = search.Find("heat");

        Assert.That(hits.Select(hit => hit.Entry),
            Is.EqualTo(new[] { first, laterOccurrence, laterPath, laterPage, helpOnly }));
        Assert.That(hits.Select(hit => hit.LabelMatch), Is.EqualTo(new[] { true, true, true, true, false }));
    }

    [Test]
    public void PageHitsUseOnlyPageMetadataWithoutHidingAnOptionWhosePageDoesNotMatch()
    {
        var page = new SettingsSearchEntry(Page(title: "Kitchen"), "page", 0, "Foreign option",
            Help: "heat", DisplayedValue: "heat", Tab: "heat", Feature: "heat", Category: "heat", IsPage: true);
        var option = page with { PathIdentity = "option", Label = "Heating", IsPage = false };
        var search = new Search(new[] { page, option });

        Assert.That(search.Find("heat").Select(hit => hit.Entry), Is.EqualTo(new[] { option }));
        Assert.That(search.Find("kitchen").Select(hit => hit.Entry), Is.EquivalentTo(new[] { page, option }));
        Assert.That(search.Find("tab:heat").Select(hit => hit.Entry), Is.EqualTo(new[] { option }));
        Assert.That(search.Find("foreign").Select(hit => hit.Entry), Is.Empty);
    }

    [Test]
    public void ExplicitDisplayedPageTitleRanksAsItsLabelWithoutAdmittingArbitraryOptionText()
    {
        var page = new SettingsPage("missing", "open", null, null, null);
        var title = new SettingsSearchEntry(page, "page", 0, "Unrelated option text", IsPage: true,
            PageDisplayTitle: "Unbenannte Einstellungen");
        var option = new SettingsSearchEntry(page, "option", 0, "Maximum", Help: "Unbenannte Einstellungen");
        var search = new Search(new[] { option, title });

        var hits = search.Find("\"Unbenannte Einstellungen\"");

        Assert.That(hits.Select(hit => hit.Entry), Is.EqualTo(new[] { title, option }));
        Assert.That(hits.Select(hit => hit.LabelMatch), Is.EqualTo(new[] { true, false }));
        Assert.That(search.Find("\"Unrelated option text\""), Is.Empty);
    }

    [TestCase("water", "watre", true)]
    [TestCase("water", "waterr", true)]
    [TestCase("water", "watar", true)]
    [TestCase("heating", "heatng", true)]
    [TestCase("water", "wter", false)]
    [TestCase("heating", "hxxing", false)]
    [TestCase("production", "producxxon", true)]
    [TestCase("production", "proxxxtion", false)]
    [TestCase("zzzzzABCzz", "zzzzzCAzz", true)]
    [TestCase("water", "\"watre\"", false)]
    [TestCase("water", "tab:watre", false)]
    public void TypoFallbackUsesWordLengthAndLiteralTermBoundaries(string label, string query, bool matches)
    {
        var entry = new SettingsSearchEntry(Page(), "one", 0, label);
        var hits = new Search(new[] { entry }).Find(query);
        Assert.That(hits.Count, Is.EqualTo(matches ? 1 : 0));
        if (matches) Assert.That(hits.Single().UsesTypo, Is.True);
    }

    [Test]
    public void ExactOccurrenceAnywhereSuppressesThatTermsFallbackBeforeOwnerFiltering()
    {
        var intended = new SettingsSearchEntry(Page(), "one", 0, "Water");
        var literalElsewhere = intended with { Page = Page("other", "Other Mod"), Label = "Watre" };
        var search = new Search(new[] { intended, literalElsewhere });

        Assert.That(search.Find("mod:example watre"), Is.Empty);
        Assert.That(search.Find("watre").Select(hit => hit.Entry), Is.EqualTo(new[] { literalElsewhere }));
        Assert.That(new Search(new[] { intended }).Find("-watre").Select(hit => hit.Entry),
            Is.EqualTo(new[] { intended }), "Exclusions never expand to typos.");
    }

    [Test]
    public void LongWordsAcceptEveryGeneratedPairOfTheFourAllowedEditOperations()
    {
        const string word = "abcaabczzzz";
        var entry = new SettingsSearchEntry(Page(), "one", 0, word);
        var search = new Search(new[] { entry });
        var variants = Edits(word).SelectMany(Edits).Distinct().ToArray();

        Assert.That(variants.Length, Is.GreaterThan(100));
        Assert.That(variants.Where(query => search.Find(query).Count != 1).Take(8), Is.Empty);
    }

    [TestCase("STRAẞE", 1)]
    [TestCase("\"STRAẞE\"", 1)]
    [TestCase("author:STRAẞE", 1)]
    [TestCase("mod:STRAẞE", 1)]
    [TestCase("-STRAẞE", 0)]
    public void UnicodeCaseVariantsMatchLiterallyAcrossUnqualifiedPhrasesFieldsAndExclusions(string query, int count)
    {
        var entry = new SettingsSearchEntry(Page(title: "Straße", authors: "Straße"), "one", 0, "Straße");
        var hits = new Search(new[] { entry }).Find(query);
        Assert.That(hits.Count, Is.EqualTo(count));
        Assert.That(hits.All(hit => !hit.UsesTypo), Is.True);
    }

    [Test]
    public void SupplementaryLettersAreWholeCharactersInTypoWords()
    {
        var entry = new SettingsSearchEntry(Page(), "one", 0, "\U00010428ater");
        var hits = new Search(new[] { entry }).Find("\U00010400atre");
        Assert.That(hits.Select(hit => hit.Entry), Is.EqualTo(new[] { entry }));
        Assert.That(hits.Single().UsesTypo, Is.True);
    }

    [Test]
    public void EmptyPageTitleRanksItsVisibleModNameFallbackAsTheLabel()
    {
        var page = new SettingsPage("page", "open", "", "Heating", "Author");
        var entry = new SettingsSearchEntry(page, "page", 0, "Heating", IsPage: true);
        Assert.That(new Search(new[] { entry }).Find("heating").Single().LabelMatch, Is.True);
    }

    private static IEnumerable<string> Edits(string word)
    {
        for (var index = 0; index <= word.Length; index++)
        {
            foreach (var letter in new[] { 'a', 'b' })
            {
                yield return word.Insert(index, letter.ToString());
                if (index < word.Length) yield return word.Remove(index, 1).Insert(index, letter.ToString());
            }
            if (index == word.Length) continue;
            yield return word.Remove(index, 1);
            if (index + 1 < word.Length)
                yield return word.Substring(0, index) + word[index + 1] + word[index] + word.Substring(index + 2);
        }
    }
}
