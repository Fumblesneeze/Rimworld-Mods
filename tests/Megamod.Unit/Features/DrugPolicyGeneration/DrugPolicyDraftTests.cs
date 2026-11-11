using Megamod.Features.DrugPolicyGeneration.Core;
using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Features.DrugTolerance.Core;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.DrugPolicyGeneration;

[TestFixture]
public sealed class DrugPolicyDraftTests
{
    [Test]
    public void ProjectionKeepsEveryBaselineRowAndAppliesAllEffectivePreferences()
    {
        var supported = Ordinary();
        var unknown = Ordinary() with { Support = DrugGenerationSupport.Unknown };
        var source = DrugPolicySuggestions.Create(new[] { supported, unknown }, 1f);
        var preferences = new DrugGenerationPreferences(.4f, .6f, DrugToleranceChoice.Numeric(.2f));
        var draft = DrugPolicyDraft.Create("My policy", source, preferences, true);
        Assert.Multiple(() =>
        {
            Assert.That(draft.Name, Is.EqualTo("My policy"));
            Assert.That(draft.Rows, Has.Count.EqualTo(2));
            Assert.That(draft.Rows[0].Choices, Is.EqualTo(new DrugPolicyChoices(true, false, true, 3, .4f, .6f, 0)));
            Assert.That(draft.Rows[1].Choices, Is.EqualTo(new DrugPolicyChoices(true, false, false, 1, .4f, .6f, 0)));
            Assert.That(draft.Rows.All(row => row.Tolerance.Limit == .2f), Is.True);
            Assert.That(draft.Rows[1].Suggestion.Reason, Is.EqualTo(DrugSuggestionReason.UnknownPharmacology));
            Assert.That(source.Rows[0].Choices.MoodThreshold, Is.EqualTo(1f));
            Assert.That(source.Rows[0].Tolerance.Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
        });
    }

    [Test]
    public void ReversingEditsToUnrestrictedValuesReplacesTheCompleteChoice()
    {
        var facts = Ordinary();
        var draft = Create(facts);
        var lowered = draft.Edit(facts.Drug.Identity,
            draft.Rows[0].Choices with { Addiction = false, MoodThreshold = .1f, RecreationThreshold = .2f, InventoryTarget = 2 },
            DrugToleranceChoice.Numeric(0));
        var finalChoices = lowered.Rows[0].Choices with { MoodThreshold = 1, RecreationThreshold = 1, InventoryTarget = 0 };
        var restored = lowered.Edit(facts.Drug.Identity, finalChoices, DrugToleranceChoice.Any).Rename("Edited");
        Assert.Multiple(() =>
        {
            Assert.That(restored.Rows[0].Choices, Is.EqualTo(finalChoices));
            Assert.That(restored.Rows[0].Tolerance.Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
            Assert.That(restored.Rows[0].ManuallyEdited, Is.True);
            Assert.That(restored.Name, Is.EqualTo("Edited"));
            Assert.That(draft.Rows[0].Choices.Addiction, Is.True);
            Assert.That(lowered.Rows[0].Choices.MoodThreshold, Is.EqualTo(.1f));
            Assert.That(lowered.Rows[0].Tolerance.Kind, Is.EqualTo(DrugToleranceChoiceKind.Limit));
        });
    }

    [Test]
    public void ResetUsesEntireNewCatalogAndPreferencesWithoutRetainingOldEdits()
    {
        var old = Ordinary();
        var draft = Create(old).Edit(old.Drug.Identity, new(false, true, false, 12, .1f, .2f, 5), DrugToleranceChoice.Numeric(.3f));
        var replacement = Ordinary() with { ToleranceGain = .045f };
        var source = DrugPolicySuggestions.Create(new[] { replacement }, 1f);
        var reset = draft.Reset("Fresh", source, new(1, 1, DrugToleranceChoice.Any), true);
        Assert.Multiple(() =>
        {
            Assert.That(reset.Name, Is.EqualTo("Fresh"));
            Assert.That(reset.Rows, Has.Count.EqualTo(1));
            Assert.That(reset.Rows[0].Suggestion.Facts.Drug.Identity, Is.SameAs(replacement.Drug.Identity));
            Assert.That(reset.Rows[0].Choices, Is.EqualTo(source.Rows[0].Choices));
            Assert.That(reset.Rows[0].Tolerance.Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
            Assert.That(reset.Rows[0].ManuallyEdited, Is.False);
            Assert.That(draft.Rows[0].Choices.InventoryTarget, Is.EqualTo(5));
        });
    }

    [Test]
    public void MissingToleranceSupportProjectsAnyWithoutChangingStoredPreferenceOrHidingNumericEdits()
    {
        var facts = Ordinary();
        var preferences = new DrugGenerationPreferences(.5f, 1f, DrugToleranceChoice.Numeric(.25f));
        var source = DrugPolicySuggestions.Create(new[] { facts }, 1f);
        var unavailable = DrugPolicyDraft.Create("Unavailable", source, preferences, false);
        Assert.Multiple(() =>
        {
            Assert.That(unavailable.ToleranceAvailable, Is.False);
            Assert.That(unavailable.Rows[0].Tolerance.Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
            Assert.That(preferences.InitialTolerance.Limit, Is.EqualTo(.25f));
            Assert.Throws<InvalidOperationException>(() => unavailable.Edit(facts.Drug.Identity,
                unavailable.Rows[0].Choices, DrugToleranceChoice.Numeric(.25f)));
        });
        var refreshed = unavailable.Reset("Supported", source, preferences, true);
        Assert.That(refreshed.Rows[0].Tolerance.Limit, Is.EqualTo(.25f));
        Assert.That(unavailable.Rows[0].Tolerance.Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
    }

    [Test]
    public void EditingCombinedExposureRecomputesProvenanceForEveryAffectedRow()
    {
        var first = Ordinary();
        var second = Ordinary() with { ChemicalIdentity = first.ChemicalIdentity, Support = DrugGenerationSupport.Unknown };
        var draft = DrugPolicyDraft.Create("Shared", DrugPolicySuggestions.Create(new[] { first, second }, 1f),
            new(1, 1, DrugToleranceChoice.Any), true);
        var edited = draft.Edit(second.Drug.Identity, draft.Rows[1].Choices with { Recreation = true }, DrugToleranceChoice.Any);
        Assert.That(edited.Rows.All(row => row.CombinedExposureUncalculated), Is.True);
        Assert.That(draft.Rows.All(row => !row.CombinedExposureUncalculated), Is.True);
    }

    private static DrugPolicyDraft Create(DrugGenerationFacts facts) =>
        DrugPolicyDraft.Create("Generated", DrugPolicySuggestions.Create(new[] { facts }, 1f), new(1, 1, DrugToleranceChoice.Any), true);

    [Test]
    public void UnknownIdentityAndUnresolvedToleranceCannotBecomeSilentDraftEdits()
    {
        var facts = Ordinary();
        var draft = Create(facts);
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => draft.Edit(new object(), draft.Rows[0].Choices, DrugToleranceChoice.Any));
            Assert.Throws<ArgumentException>(() => draft.Edit(facts.Drug.Identity, draft.Rows[0].Choices, DrugToleranceChoice.Decode("future")));
            Assert.Throws<ArgumentException>(() => draft.Reset("Bad", DrugPolicySuggestions.Create(new[] { facts }, 1f),
                new(1, 1, DrugToleranceChoice.Decode("future")), true));
        });
    }

    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(-.1f)]
    [TestCase(1.1f)]
    public void InvalidPreferenceLimitsAreRejectedRatherThanClamped(float value)
    {
        var source = DrugPolicySuggestions.Create(new[] { Ordinary() }, 1f);
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DrugPolicyDraft.Create("Bad", source, new(value, 1, DrugToleranceChoice.Any), true));
            Assert.Throws<ArgumentOutOfRangeException>(() => DrugPolicyDraft.Create("Bad", source, new(1, value, DrugToleranceChoice.Any), true));
        });
    }

    [Test]
    public void InvalidChoicesRemainOutsideTheImmutableDraft()
    {
        var facts = Ordinary() with { Drug = new(new object(), false, false), Addictiveness = 0 };
        var draft = Create(facts);
        var valid = draft.Rows[0].Choices;
        var invalid = new[]
        {
            valid with { Addiction = true }, valid with { Recreation = true },
            valid with { DaysFrequency = float.NaN }, valid with { DaysFrequency = 0 },
            valid with { MoodThreshold = -.1f }, valid with { RecreationThreshold = float.PositiveInfinity },
            valid with { InventoryTarget = -1 }
        };
        foreach (var choices in invalid)
            Assert.Throws<ArgumentException>(() => draft.Edit(facts.Drug.Identity, choices, DrugToleranceChoice.Any));
        Assert.That(draft.Rows[0].Choices, Is.EqualTo(valid));
    }

    private static DrugGenerationFacts Ordinary() => new(new DrugCatalogEntry(new object(), true, true),
        new object(), new object(), DrugGenerationSupport.OrdinaryWholeDose, false, .1f, .025f, .25f,
        .03f, false, 1f, -.03f, 0, 0, 0);
}
