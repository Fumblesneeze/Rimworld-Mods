using System.Globalization;
using Megamod.Features.DrugPolicyGeneration.Core;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.DrugPolicyGeneration;

[TestFixture]
public sealed class DrugGenerationPreferenceDraftTests
{
    [TestCase("de-DE", "12,5", .125f)]
    [TestCase("en-US", "0", 0f)]
    [TestCase("en-US", "100", 1f)]
    public void ApplyCommitsLocalePercentageOnlyOnRequest(string culture, string text, float expected)
    {
        var draft = new DrugGenerationPreferenceDraft(false);
        var committed = new List<DrugToleranceChoice>();
        draft.Synchronize(DrugToleranceChoice.Numeric(.3f), CultureInfo.GetCultureInfo(culture));
        draft.Edit(false, text);
        Assert.That(committed, Is.Empty);
        var result = draft.Apply(true, value => { committed.Add(value); return new(true); });
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(committed, Has.Count.EqualTo(1));
            Assert.That(committed.Single().Limit, Is.EqualTo(expected));
            Assert.That(draft.ErrorKey, Is.Null);
        });
    }

    [TestCase("NaN")]
    [TestCase("Infinity")]
    [TestCase("-1")]
    [TestCase("101")]
    [TestCase("")]
    public void InvalidDraftIsRetainedWithoutCallingCommit(string text)
    {
        var draft = new DrugGenerationPreferenceDraft(false);
        draft.Synchronize(DrugToleranceChoice.Numeric(.3f), CultureInfo.InvariantCulture);
        draft.Edit(false, text);
        var result = draft.Apply(true, _ => throw new AssertionException("Invalid input reached storage."));
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(draft.CanApply, Is.False);
            Assert.That(draft.Percentage, Is.EqualTo(text));
            Assert.That(draft.ErrorKey, Is.EqualTo("Megamod_DrugGeneration_InvalidThreshold"));
        });
    }

    [Test]
    public void DisabledApplyCannotCallTheCommonSettingsCommit()
    {
        var draft = new DrugGenerationPreferenceDraft(true);
        draft.Synchronize(DrugToleranceChoice.Any, CultureInfo.InvariantCulture);
        Assert.That(draft.Apply(false, _ => throw new AssertionException("Disabled scope mutated storage.")).Succeeded, Is.False);
    }

    [Test]
    public void AnyAndNumericHundredRemainDistinctAndMoodRejectsAny()
    {
        var draft = new DrugGenerationPreferenceDraft(true);
        draft.Synchronize(DrugToleranceChoice.Any, CultureInfo.InvariantCulture);
        var choices = new List<DrugToleranceChoice>();
        SettingEditResult Commit(DrugToleranceChoice value) { choices.Add(value); return new(true); }
        draft.Edit(false, "100");
        Assert.That(draft.Apply(true, Commit).Succeeded, Is.True);
        draft.Edit(true, "invalid dormant text");
        Assert.That(draft.Apply(true, Commit).Succeeded, Is.True);
        var mood = new DrugGenerationPreferenceDraft(false);
        mood.Synchronize(DrugToleranceChoice.Numeric(1), CultureInfo.InvariantCulture);
        mood.Edit(true, "100");
        Assert.Multiple(() =>
        {
            Assert.That(choices[0].Kind, Is.EqualTo(DrugToleranceChoiceKind.Limit));
            Assert.That(choices[0].Limit, Is.EqualTo(1));
            Assert.That(choices[1].Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
            Assert.That(mood.Apply(true, _ => throw new AssertionException("Mood accepted Any.")).Succeeded, Is.False);
        });
    }

    [Test]
    public void UnchangedExactValueSurvivesFormattingAndARejectedCommitRetainsTheEdit()
    {
        var original = DrugToleranceChoice.Numeric(.12345679f);
        var draft = new DrugGenerationPreferenceDraft(false);
        draft.Synchronize(original, CultureInfo.GetCultureInfo("fr-FR"));
        DrugToleranceChoice? committed = null;
        draft.Apply(true, value => { committed = value; return new(true); });
        Assert.That(committed, Is.SameAs(original));
        draft.Edit(false, "42,5");
        draft.Apply(true, _ => new(false, "stale-binding"));
        draft.Synchronize(DrugToleranceChoice.Numeric(original.Limit), CultureInfo.GetCultureInfo("fr-FR"));
        Assert.Multiple(() =>
        {
            Assert.That(draft.Percentage, Is.EqualTo("42,5"));
            Assert.That(draft.ErrorKey, Is.EqualTo("stale-binding"));
        });
    }

    [Test]
    public void ChangedStoredValueCultureOrScopeResetDiscardsOnlyTransientText()
    {
        var draft = new DrugGenerationPreferenceDraft(false);
        draft.Synchronize(DrugToleranceChoice.Numeric(.3f), CultureInfo.InvariantCulture);
        draft.Edit(false, "invalid");
        draft.Synchronize(DrugToleranceChoice.Numeric(.425f), CultureInfo.GetCultureInfo("de-DE"));
        Assert.That(draft.Percentage, Is.EqualTo("42,5"));
        draft.Edit(false, "17");
        draft.Synchronize(DrugToleranceChoice.Numeric(.425f), CultureInfo.GetCultureInfo("en-US"));
        Assert.That(draft.Percentage, Is.EqualTo("42.5"));
        draft.Edit(false, "19");
        draft.Reset();
        Assert.That(draft.Apply(true, _ => throw new AssertionException("Reset draft retained a target.")).Succeeded, Is.False);
        draft.Synchronize(DrugToleranceChoice.Numeric(.425f), CultureInfo.GetCultureInfo("en-US"));
        Assert.That(draft.Percentage, Is.EqualTo("42.5"));
    }
}
