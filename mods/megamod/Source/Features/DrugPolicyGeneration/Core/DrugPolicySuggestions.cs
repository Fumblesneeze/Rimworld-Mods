using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Features.DrugTolerance.Core;

namespace Megamod.Features.DrugPolicyGeneration.Core;

/// <summary>The native reader supplies a positive receipt only after inspecting the complete effect/quantity shape.</summary>
public enum DrugGenerationSupport
{
    Unknown,
    OrdinaryWholeDose,
    UnsupportedEffects,
    UnsupportedTolerance,
    UnsupportedDecay,
    UnresolvedPartialDose
}

/// <summary>Detached facts. Identity handles are canonical references, never labels or names.</summary>
public sealed record DrugGenerationFacts(
    DrugCatalogEntry Drug,
    object? ChemicalIdentity,
    object? ToleranceIdentity,
    DrugGenerationSupport Support,
    bool Medical,
    float Joy,
    float Addictiveness,
    float MinimumTolerance,
    float ToleranceGain,
    bool DivideByBodySize,
    float MaximumTolerance,
    float DecayPerDay,
    float OverdoseMinimum,
    float OverdoseMaximum,
    float LargeOverdoseChance);

public enum DrugSuggestionReason
{
    ModeledSchedule,
    UnknownPharmacology,
    MedicalPurpose,
    NoRoutineModel,
    UnresolvedPartialDose,
    UnsupportedTolerance,
    UnsupportedDecay,
    InvalidFacts,
    BaselineOutsideModel,
    OverdoseRisk,
    NoEstablishedClearance,
    EditorIntervalExceeded,
    SharedExposure
}

public sealed record DrugSuggestion(
    DrugGenerationFacts Facts,
    DrugPolicyChoices Choices,
    DrugToleranceChoice Tolerance,
    DrugSuggestionReason Reason,
    int? ModeledDecayEvents = null);

public sealed record DrugGenerationEdit(object DrugIdentity, DrugPolicyChoices Choices, DrugToleranceChoice Tolerance);

public sealed record DrugSuggestionReview(
    DrugSuggestion Suggestion,
    DrugPolicyChoices Choices,
    DrugToleranceChoice Tolerance,
    bool ManuallyEdited,
    bool CombinedExposureUncalculated);

/// <summary>A C1 model, explicitly not an elapsed-time or pharmacological safety guarantee.</summary>
public sealed class DrugSuggestionSet
{
    internal DrugSuggestionSet(float baselineBodySize, DrugSuggestion[] rows, int[] channels)
    {
        BaselineBodySize = baselineBodySize;
        Rows = Array.AsReadOnly(rows);
        Channels = Array.AsReadOnly(channels);
    }

    public string CalculationVersion => "C1";
    public float BaselineBodySize { get; }
    public bool NativeBatchingMayLeaveTolerance => true;
    public IReadOnlyList<DrugSuggestion> Rows { get; }
    internal IReadOnlyList<int> Channels { get; }

    /// <summary>Projects provenance only; creation must additionally validate current facts and all edited values.</summary>
    public IReadOnlyList<DrugSuggestionReview> ReviewEdits(IReadOnlyList<DrugGenerationEdit> edits)
    {
        ArgumentNullException.ThrowIfNull(edits);
        var byIdentity = new Dictionary<object, DrugGenerationEdit>(ReferenceEqualityComparer.Instance);
        var known = new HashSet<object>(Rows.Select(row => row.Facts.Drug.Identity), ReferenceEqualityComparer.Instance);
        foreach (var edit in edits)
        {
            ArgumentNullException.ThrowIfNull(edit);
            ArgumentNullException.ThrowIfNull(edit.Choices);
            ArgumentNullException.ThrowIfNull(edit.Tolerance);
            if (!known.Contains(edit.DrugIdentity) || !byIdentity.TryAdd(edit.DrugIdentity, edit))
                throw new ArgumentException("Each edit must identify one distinct row in this suggestion set.", nameof(edits));
        }
        var reviewed = new DrugSuggestionReview[Rows.Count];
        for (var i = 0; i < Rows.Count; i++)
        {
            var row = Rows[i];
            byIdentity.TryGetValue(row.Facts.Drug.Identity, out var edit);
            var choices = edit?.Choices ?? row.Choices;
            var tolerance = edit?.Tolerance ?? row.Tolerance;
            var sameTolerance = tolerance.Kind == row.Tolerance.Kind && tolerance.Limit == row.Tolerance.Limit &&
                (tolerance.Kind != DrugToleranceChoiceKind.NeedsReview || tolerance.Encoding == row.Tolerance.Encoding);
            reviewed[i] = new DrugSuggestionReview(row, choices, tolerance,
                choices != row.Choices || !sameTolerance, false);
        }
        foreach (var group in Enumerable.Range(0, Rows.Count).GroupBy(index => Channels[index]))
        {
            if (group.Any(index => reviewed[index].Choices.Recreation) || group.Count(index => reviewed[index].Choices.Scheduled) > 1)
                foreach (var index in group)
                    reviewed[index] = reviewed[index] with { CombinedExposureUncalculated = true };
        }
        return Array.AsReadOnly(reviewed);
    }
}

public static class DrugPolicySuggestions
{
    // This is the reviewed C1 cadence model, not a bound on actual native clearance.
    private const int MaximumDecayEvents = (24 * 60000 - 415) / 200;

    public static DrugSuggestionSet Create(IReadOnlyList<DrugGenerationFacts> facts, float baselineBodySize)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var rows = new DrugSuggestion[facts.Count];
        var identities = new HashSet<object>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < facts.Count; i++)
        {
            var fact = facts[i];
            ArgumentNullException.ThrowIfNull(fact);
            ArgumentNullException.ThrowIfNull(fact.Drug);
            ArgumentNullException.ThrowIfNull(fact.Drug.Identity);
            if (!identities.Add(fact.Drug.Identity))
                throw new ArgumentException("Drug identities must be distinct in the current catalog.", nameof(facts));
            var baseline = new DrugPolicyChoices(fact.Drug.Addictive, false, false, 1f, 1f, 1f, 0);
            var reason = Classify(fact, baselineBodySize);
            if (reason != DrugSuggestionReason.ModeledSchedule)
            {
                rows[i] = new DrugSuggestion(fact, baseline, DrugToleranceChoice.Any, reason);
                continue;
            }
            var remaining = fact.DivideByBodySize ? fact.ToleranceGain / baselineBodySize : fact.ToleranceGain;
            var step = fact.DecayPerDay * 0.0033333334f;
            var count = 0;
            while (remaining > 0f && count < MaximumDecayEvents)
            {
                var next = Math.Max(0f, remaining + step);
                if (!float.IsFinite(next) || next >= remaining)
                {
                    reason = DrugSuggestionReason.NoEstablishedClearance;
                    break;
                }
                remaining = next;
                count++;
            }
            if (reason == DrugSuggestionReason.ModeledSchedule && remaining > 0f)
                reason = DrugSuggestionReason.EditorIntervalExceeded;
            var days = 1 + (200 * count + 415 + 59999) / 60000;
            rows[i] = new DrugSuggestion(fact,
                reason == DrugSuggestionReason.ModeledSchedule ? baseline with { Scheduled = true, DaysFrequency = days } : baseline,
                DrugToleranceChoice.Any, reason, reason == DrugSuggestionReason.ModeledSchedule ? count : null);
        }
        var channels = ConnectChannels(rows);
        var candidates = rows.Select((row, index) => (row, index))
            .Where(pair => pair.row.Reason == DrugSuggestionReason.ModeledSchedule)
            .GroupBy(pair => channels[pair.index]);
        foreach (var channel in candidates.Where(group => group.Count() > 1))
            foreach (var pair in channel)
                rows[pair.index] = pair.row with
                {
                    Reason = DrugSuggestionReason.SharedExposure,
                    Choices = pair.row.Choices with { Scheduled = false, DaysFrequency = 1f },
                    ModeledDecayEvents = null
                };
        return new DrugSuggestionSet(baselineBodySize, rows, channels);
    }

    private static int[] ConnectChannels(IReadOnlyList<DrugSuggestion> rows)
    {
        var roots = Enumerable.Range(0, rows.Count).ToArray();
        var chemicals = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
        var tolerances = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < rows.Count; i++)
        {
            Connect(rows[i].Facts.ChemicalIdentity, i, chemicals);
            Connect(rows[i].Facts.ToleranceIdentity, i, tolerances);
        }
        for (var i = 0; i < roots.Length; i++)
            roots[i] = Root(i);
        return roots;

        void Connect(object? identity, int index, Dictionary<object, int> seen)
        {
            if (identity == null)
                return;
            if (seen.TryGetValue(identity, out var prior))
                roots[Root(index)] = Root(prior);
            else
                seen.Add(identity, index);
        }

        int Root(int index)
        {
            while (roots[index] != index)
            {
                roots[index] = roots[roots[index]];
                index = roots[index];
            }
            return index;
        }
    }

    private static DrugSuggestionReason Classify(DrugGenerationFacts fact, float baselineBodySize)
    {
        if (fact.Support != DrugGenerationSupport.OrdinaryWholeDose)
            return fact.Support switch
            {
                DrugGenerationSupport.UnsupportedTolerance => DrugSuggestionReason.UnsupportedTolerance,
                DrugGenerationSupport.UnsupportedDecay => DrugSuggestionReason.UnsupportedDecay,
                DrugGenerationSupport.UnresolvedPartialDose => DrugSuggestionReason.UnresolvedPartialDose,
                _ => DrugSuggestionReason.UnknownPharmacology
            };
        if (!float.IsFinite(fact.Joy) || !float.IsFinite(fact.Addictiveness) ||
            !float.IsFinite(fact.MinimumTolerance) || !float.IsFinite(fact.ToleranceGain) ||
            !float.IsFinite(fact.MaximumTolerance) || !float.IsFinite(fact.DecayPerDay) ||
            !float.IsFinite(fact.OverdoseMinimum) || !float.IsFinite(fact.OverdoseMaximum) ||
            !float.IsFinite(fact.LargeOverdoseChance) || fact.Addictiveness < 0f ||
            fact.MinimumTolerance < 0f || fact.ToleranceGain < 0f || fact.MaximumTolerance <= 0f ||
            fact.OverdoseMinimum < 0f || fact.OverdoseMaximum < fact.OverdoseMinimum ||
            fact.LargeOverdoseChance < 0f || fact.LargeOverdoseChance > 1f ||
            fact.Drug.Addictive != (fact.Addictiveness > 0f))
            return DrugSuggestionReason.InvalidFacts;
        if (fact.Medical)
            return DrugSuggestionReason.MedicalPurpose;
        if (fact.Joy <= 0f || fact.Addictiveness == 0f)
            return DrugSuggestionReason.NoRoutineModel;
        if (fact.ChemicalIdentity == null)
            return DrugSuggestionReason.UnknownPharmacology;
        if (fact.ToleranceIdentity == null)
            return DrugSuggestionReason.UnsupportedTolerance;
        if (fact.OverdoseMaximum > 0f || fact.LargeOverdoseChance > 0f)
            return DrugSuggestionReason.OverdoseRisk;
        var gain = fact.DivideByBodySize ? fact.ToleranceGain / baselineBodySize : fact.ToleranceGain;
        if (!float.IsFinite(baselineBodySize) || baselineBodySize <= 0f ||
            !float.IsFinite(gain) || gain <= 0f || gain >= fact.MinimumTolerance ||
            gain > fact.MaximumTolerance || fact.Addictiveness >= 1f)
            return DrugSuggestionReason.BaselineOutsideModel;
        return fact.DecayPerDay >= 0f
            ? DrugSuggestionReason.NoEstablishedClearance : DrugSuggestionReason.ModeledSchedule;
    }
}
