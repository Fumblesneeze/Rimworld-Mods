using Megamod.Features.DrugPolicyGeneration.Core;
using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Features.DrugTolerance.Core;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.DrugPolicyGeneration;

[TestFixture]
public sealed class DrugPolicySuggestionsTests
{
    [Test]
    public void GeneratedSchedulePreservesMedicalPermissionAndDisclosesModelLimits()
    {
        var facts = Ordinary();
        var result = DrugPolicySuggestions.Create(new[] { facts }, 1f);
        Assert.Multiple(() =>
        {
            Assert.That(result.Rows, Has.Count.EqualTo(1));
            Assert.That(result.Rows[0].Choices, Is.EqualTo(new DrugPolicyChoices(true, false, true, 3f, 1f, 1f, 0)));
            Assert.That(result.Rows[0].Reason, Is.EqualTo(DrugSuggestionReason.ModeledSchedule));
            Assert.That(result.Rows[0].Tolerance.Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
            Assert.That(result.Rows[0].Facts.Drug.Identity, Is.SameAs(facts.Drug.Identity));
            Assert.That(result.NativeBatchingMayLeaveTolerance, Is.True);
            Assert.That(result.BaselineBodySize, Is.EqualTo(1f));
        });
    }

    [TestCaseSource(nameof(UnsupportedFacts))]
    public void UnsupportedRowsRemainVisibleWithNativeDefaultsAndAReason(DrugGenerationFacts facts, DrugSuggestionReason reason)
    {
        var row = DrugPolicySuggestions.Create(new[] { facts }, 1f).Rows.Single();
        Assert.Multiple(() =>
        {
            Assert.That(row.Facts, Is.SameAs(facts));
            Assert.That(row.Reason, Is.EqualTo(reason));
            Assert.That(row.Choices, Is.EqualTo(new DrugPolicyChoices(facts.Drug.Addictive, false, false, 1f, 1f, 1f, 0)));
            Assert.That(row.ModeledDecayEvents, Is.Null);
        });
    }

    private static IEnumerable<TestCaseData> UnsupportedFacts()
    {
        var ordinary = Ordinary();
        yield return Case(ordinary with { Support = DrugGenerationSupport.Unknown }, DrugSuggestionReason.UnknownPharmacology);
        yield return Case(ordinary with { Support = DrugGenerationSupport.UnsupportedEffects }, DrugSuggestionReason.UnknownPharmacology);
        yield return Case(ordinary with { Support = DrugGenerationSupport.UnsupportedTolerance }, DrugSuggestionReason.UnsupportedTolerance);
        yield return Case(ordinary with { Support = DrugGenerationSupport.UnsupportedDecay }, DrugSuggestionReason.UnsupportedDecay);
        yield return Case(ordinary with { Support = DrugGenerationSupport.UnresolvedPartialDose }, DrugSuggestionReason.UnresolvedPartialDose);
        yield return Case(ordinary with { Support = (DrugGenerationSupport)999 }, DrugSuggestionReason.UnknownPharmacology);
        yield return Case(ordinary with { Medical = true }, DrugSuggestionReason.MedicalPurpose);
        yield return Case(ordinary with { Joy = 0f }, DrugSuggestionReason.NoRoutineModel);
        yield return Case(ordinary with { Addictiveness = 0f, Drug = ordinary.Drug with { Addictive = false } }, DrugSuggestionReason.NoRoutineModel);
        yield return Case(ordinary with { ChemicalIdentity = null }, DrugSuggestionReason.UnknownPharmacology);
        yield return Case(ordinary with { ToleranceIdentity = null }, DrugSuggestionReason.UnsupportedTolerance);
        yield return Case(ordinary with { MinimumTolerance = 0f }, DrugSuggestionReason.BaselineOutsideModel);
        yield return Case(ordinary with { Addictiveness = 1f }, DrugSuggestionReason.BaselineOutsideModel);
        yield return Case(ordinary with { ToleranceGain = 0.25f }, DrugSuggestionReason.BaselineOutsideModel);
        yield return Case(ordinary with { MaximumTolerance = 0.01f }, DrugSuggestionReason.BaselineOutsideModel);
        yield return Case(ordinary with { OverdoseMaximum = 0.01f }, DrugSuggestionReason.OverdoseRisk);
        yield return Case(ordinary with { LargeOverdoseChance = 0.001f }, DrugSuggestionReason.OverdoseRisk);
        yield return Case(ordinary with { DecayPerDay = 0f }, DrugSuggestionReason.NoEstablishedClearance);
        yield return Case(ordinary with { DecayPerDay = 0.001f }, DrugSuggestionReason.NoEstablishedClearance);
        yield return Case(ordinary with { DecayPerDay = -float.Epsilon }, DrugSuggestionReason.NoEstablishedClearance);
        yield return Case(ordinary with { DecayPerDay = -1e-20f }, DrugSuggestionReason.NoEstablishedClearance);
        yield return Case(ordinary with { ToleranceGain = float.NaN }, DrugSuggestionReason.InvalidFacts);
        yield return Case(ordinary with { DecayPerDay = float.NegativeInfinity }, DrugSuggestionReason.InvalidFacts);
        yield return Case(ordinary with { Joy = float.PositiveInfinity }, DrugSuggestionReason.InvalidFacts);
        yield return Case(ordinary with { OverdoseMinimum = -0.1f }, DrugSuggestionReason.InvalidFacts);
        yield return Case(ordinary with { OverdoseMinimum = 0.1f, OverdoseMaximum = 0f }, DrugSuggestionReason.InvalidFacts);
        yield return Case(ordinary with { LargeOverdoseChance = float.NaN }, DrugSuggestionReason.InvalidFacts);
        yield return Case(ordinary with { MinimumTolerance = -0.01f }, DrugSuggestionReason.InvalidFacts);
        yield return Case(ordinary with { Drug = ordinary.Drug with { Addictive = false } }, DrugSuggestionReason.InvalidFacts);
    }

    private static TestCaseData Case(DrugGenerationFacts facts, DrugSuggestionReason reason) => new(facts, reason);

    [Test]
    public void ConnectedChemicalOrToleranceChannelsWithholdOnlyConflictingCandidates()
    {
        var first = Ordinary();
        var bridge = Ordinary() with { ChemicalIdentity = first.ChemicalIdentity, Support = DrugGenerationSupport.UnsupportedEffects };
        var last = Ordinary() with { ToleranceIdentity = bridge.ToleranceIdentity };
        var unrelated = Ordinary();
        var rows = DrugPolicySuggestions.Create(new[] { first, bridge, last, unrelated }, 1f).Rows;
        Assert.Multiple(() =>
        {
            Assert.That(rows.Select(r => r.Reason), Is.EqualTo(new[] {
                DrugSuggestionReason.SharedExposure, DrugSuggestionReason.UnknownPharmacology,
                DrugSuggestionReason.SharedExposure, DrugSuggestionReason.ModeledSchedule }));
            Assert.That(rows.Select(r => r.Choices.Scheduled), Is.EqualTo(new[] { false, false, false, true }));
            Assert.That(rows[0].Choices.DaysFrequency, Is.EqualTo(1f));
            Assert.That(rows[0].ModeledDecayEvents, Is.Null);
            Assert.That(rows[0].Choices.Addiction, Is.True);
        });
    }

    [Test]
    public void ManualCombinedUsePreservesEditsWithoutClaimingTheModelCoversExtraExposure()
    {
        var supported = Ordinary();
        var unknown = Ordinary() with { ChemicalIdentity = supported.ChemicalIdentity, Support = DrugGenerationSupport.UnsupportedEffects };
        var unrelated = Ordinary();
        var proposals = DrugPolicySuggestions.Create(new[] { supported, unknown, unrelated }, 1f);
        var extraUse = proposals.Rows[1].Choices with { Recreation = true, Addiction = false };
        var reviewed = proposals.ReviewEdits(new[] {
            new DrugGenerationEdit(unknown.Drug.Identity, extraUse, DrugToleranceChoice.Numeric(0.2f)) });
        Assert.Multiple(() =>
        {
            Assert.That(reviewed[1].Choices, Is.SameAs(extraUse));
            Assert.That(reviewed[1].Tolerance.Limit, Is.EqualTo(0.2f));
            Assert.That(reviewed[1].ManuallyEdited, Is.True);
            Assert.That(reviewed.Select(r => r.CombinedExposureUncalculated), Is.EqualTo(new[] { true, true, false }));
            Assert.That(reviewed[2].ManuallyEdited, Is.False);
            Assert.That(proposals.Rows[1].Choices.Recreation, Is.False);
            Assert.That(proposals.Rows[0].Reason, Is.EqualTo(DrugSuggestionReason.ModeledSchedule));
        });
    }

    [Test]
    public void DuplicateCatalogIdentityCannotPublishAmbiguousSuggestions()
    {
        var facts = Ordinary();
        Assert.Throws<ArgumentException>(() => DrugPolicySuggestions.Create(new[] { facts, facts }, 1f));
    }

    [TestCase(0.016f, -0.016f, 301, 3f)]
    [TestCase(0.03f, -0.015f, 600, 4f)]
    [TestCase(0.032f, -0.020f, 481, 3f)]
    public void ModelKeepsBinary32ResidueAndCalendarAllowance(float gain, float decay, int events, float days)
    {
        var row = DrugPolicySuggestions.Create(new[] { Ordinary() with { ToleranceGain = gain, DecayPerDay = decay } }, 1f).Rows.Single();
        Assert.Multiple(() =>
        {
            Assert.That(row.ModeledDecayEvents, Is.EqualTo(events));
            Assert.That(row.Choices.DaysFrequency, Is.EqualTo(days));
            Assert.That(row.Choices.Scheduled, Is.True);
        });
    }

    [Test]
    public void ModeledFrequencyRespondsToDoseAndDecayWithoutANameDependency()
    {
        var ordinary = Ordinary();
        var original = DrugPolicySuggestions.Create(new[] { ordinary }, 1f).Rows.Single();
        var smaller = DrugPolicySuggestions.Create(new[] { ordinary with { ToleranceGain = 0.001f } }, 1f).Rows.Single();
        var slower = DrugPolicySuggestions.Create(new[] { ordinary with { DecayPerDay = -0.008f } }, 1f).Rows.Single();
        var largerBody = DrugPolicySuggestions.Create(new[] { ordinary }, 2f).Rows.Single();
        var unscaled = DrugPolicySuggestions.Create(new[] { ordinary with { DivideByBodySize = false } }, 2f).Rows.Single();
        Assert.Multiple(() =>
        {
            Assert.That(smaller.Choices.DaysFrequency, Is.LessThan(original.Choices.DaysFrequency));
            Assert.That(slower.Choices.DaysFrequency, Is.GreaterThan(original.Choices.DaysFrequency));
            Assert.That(largerBody.Choices.DaysFrequency, Is.LessThan(original.Choices.DaysFrequency));
            Assert.That(unscaled.Choices.DaysFrequency, Is.EqualTo(original.Choices.DaysFrequency));
        });
    }

    [Test]
    public void NativeEditorBoundaryWithholdsRatherThanShortensAnUnrepresentableInterval()
    {
        var boundary = Ordinary() with { ToleranceGain = 7197f, MinimumTolerance = 10000f, MaximumTolerance = 10000f, DecayPerDay = -300f };
        var admitted = DrugPolicySuggestions.Create(new[] { boundary }, 1f).Rows.Single();
        var exceeded = DrugPolicySuggestions.Create(new[] { boundary with { ToleranceGain = 7198f } }, 1f).Rows.Single();
        Assert.Multiple(() =>
        {
            Assert.That(admitted.Choices.DaysFrequency, Is.EqualTo(25f));
            Assert.That(admitted.Choices.Scheduled, Is.True);
            Assert.That(exceeded.Reason, Is.EqualTo(DrugSuggestionReason.EditorIntervalExceeded));
            Assert.That(exceeded.Choices.Scheduled, Is.False);
            Assert.That(exceeded.Choices.DaysFrequency, Is.EqualTo(1f));
        });
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.Epsilon)]
    public void UnusableBodyBaselineNeverPublishesAnAutomaticSchedule(float bodySize)
    {
        var row = DrugPolicySuggestions.Create(new[] { Ordinary() }, bodySize).Rows.Single();
        Assert.That(row.Reason, Is.EqualTo(DrugSuggestionReason.BaselineOutsideModel));
        Assert.That(row.Choices.Scheduled, Is.False);
    }

    [Test]
    public void EqualLabelsAreNotSharedNativeIdentitiesAndOffRowsDoNotSuppressOneCandidate()
    {
        var first = Ordinary() with { ChemicalIdentity = new Identity("same"), ToleranceIdentity = new Identity("same") };
        var second = Ordinary() with { ChemicalIdentity = new Identity("same"), ToleranceIdentity = new Identity("same") };
        var off = Ordinary() with { ChemicalIdentity = first.ChemicalIdentity, Support = DrugGenerationSupport.UnsupportedEffects };
        var rows = DrugPolicySuggestions.Create(new[] { first, second, off }, 1f).Rows;
        Assert.That(rows.Select(row => row.Choices.Scheduled), Is.EqualTo(new[] { true, true, false }));
    }

    [Test]
    public void ShortenedManualIntervalAndReversalHaveIndependentProvenance()
    {
        var fact = Ordinary();
        var set = DrugPolicySuggestions.Create(new[] { fact }, 1f);
        var shorter = set.Rows[0].Choices with { DaysFrequency = 1f };
        var changed = set.ReviewEdits(new[] { new DrugGenerationEdit(fact.Drug.Identity, shorter, DrugToleranceChoice.Numeric(0f)) });
        var reversed = set.ReviewEdits(new[] { new DrugGenerationEdit(fact.Drug.Identity, set.Rows[0].Choices, DrugToleranceChoice.Decode(null)) });
        Assert.Multiple(() =>
        {
            Assert.That(changed[0].ManuallyEdited, Is.True);
            Assert.That(changed[0].CombinedExposureUncalculated, Is.False);
            Assert.That(reversed[0].ManuallyEdited, Is.False);
            Assert.That(reversed[0].Choices.DaysFrequency, Is.EqualTo(3f));
            Assert.That(changed[0].Choices.DaysFrequency, Is.EqualTo(1f));
        });
    }

    [Test]
    public void EditsRequireDistinctCurrentRowIdentitiesAndDoNotMutatePreviousViews()
    {
        var fact = Ordinary();
        var set = DrugPolicySuggestions.Create(new[] { fact }, 1f);
        var edit = new DrugGenerationEdit(fact.Drug.Identity, set.Rows[0].Choices, DrugToleranceChoice.Any);
        Assert.Throws<ArgumentException>(() => set.ReviewEdits(new[] { edit, edit }));
        Assert.Throws<ArgumentException>(() => set.ReviewEdits(new[] { edit with { DrugIdentity = new object() } }));
        Assert.That(set.ReviewEdits(Array.Empty<DrugGenerationEdit>())[0].ManuallyEdited, Is.False);
        Assert.That(DrugPolicySuggestions.Create(Array.Empty<DrugGenerationFacts>(), 1f).Rows, Is.Empty);
    }

    private sealed record Identity(string Label);

    private static DrugGenerationFacts Ordinary() => new(
        new DrugCatalogEntry(new object(), true, true), new object(), new object(),
        DrugGenerationSupport.OrdinaryWholeDose, false, 0.17f, 0.01f, 0.25f,
        0.016f, true, 1f, -0.016f, 0f, 0f, 0f);
}
