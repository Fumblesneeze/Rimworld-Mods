using System.Globalization;
using Megamod.Features.DrugTolerance.Core;
using NUnit.Framework;

namespace Megamod.Unit.Features.DrugTolerance;

[TestFixture]
public sealed class DrugToleranceEditDraftTests
{
    [Test]
    public void NumericZeroAndHundredRemainDistinctFromAnyAndDoNotMutateTheOriginal()
    {
        var original = DrugToleranceChoice.Numeric(0.25f);
        var draft = new DrugToleranceEditDraft(original, CultureInfo.InvariantCulture) { Percentage = "0" };
        Assert.That(draft.TryGetChoice(out var zero), Is.True);
        Assert.That(zero.Kind, Is.EqualTo(DrugToleranceChoiceKind.Limit));
        Assert.That(zero.Limit, Is.Zero);
        draft.Percentage = "100";
        Assert.That(draft.TryGetChoice(out var hundred), Is.True);
        Assert.That(hundred.Kind, Is.EqualTo(DrugToleranceChoiceKind.Limit));
        Assert.That(hundred.Limit, Is.EqualTo(1f));
        draft.Any = true;
        Assert.That(draft.TryGetChoice(out var any), Is.True);
        Assert.That(any.Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
        Assert.That(original.Limit, Is.EqualTo(0.25f));
    }

    [TestCase("")]
    [TestCase("not a number")]
    [TestCase("NaN")]
    [TestCase("Infinity")]
    [TestCase("-0.1")]
    [TestCase("100.1")]
    public void InvalidNumericDraftCannotRepairAnUnknownSavedChoice(string text)
    {
        var unknown = DrugToleranceChoice.Decode("2:future:retain-exactly");
        var draft = new DrugToleranceEditDraft(unknown, CultureInfo.InvariantCulture) { Percentage = text };
        Assert.That(draft.Any, Is.False);
        Assert.That(draft.TryGetChoice(out var result), Is.False);
        Assert.That(result, Is.SameAs(unknown));
        draft.Any = true;
        Assert.That(draft.TryGetChoice(out var repaired), Is.True);
        Assert.That(repaired.Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
        Assert.That(unknown.Encoding, Is.EqualTo("2:future:retain-exactly"));
    }

    [TestCase("en-US", "12.5")]
    [TestCase("de-DE", "12,5")]
    [TestCase("fr-FR", "12,5")]
    public void NumericDraftUsesTheCapturedLocale(string language, string text)
    {
        var draft = new DrugToleranceEditDraft(DrugToleranceChoice.Any, CultureInfo.GetCultureInfo(language))
            { Any = false, Percentage = text };
        Assert.That(draft.TryGetChoice(out var choice), Is.True);
        Assert.That(choice.Limit, Is.EqualTo(0.125f));
    }

    [Test]
    public void AnUnchangedValidChoiceKeepsItsExactStoredEncoding()
    {
        var original = DrugToleranceChoice.Decode("1:limit:0.123456789");
        var draft = new DrugToleranceEditDraft(original, CultureInfo.GetCultureInfo("de-DE"));
        Assert.That(draft.TryGetChoice(out var same), Is.True);
        Assert.That(same, Is.SameAs(original));
        var absent = DrugToleranceChoice.Decode(null);
        Assert.That(new DrugToleranceEditDraft(absent, CultureInfo.InvariantCulture).TryGetChoice(out var unchangedAny), Is.True);
        Assert.That(unchangedAny, Is.SameAs(absent));
    }
}
