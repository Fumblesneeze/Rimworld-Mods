using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Megamod.Features.DrugPolicyGeneration.Core;
using Megamod.Features.DrugPolicyMaintenance.Core;
using RimWorld;
using Verse;

namespace Megamod.Features.DrugPolicyGeneration.Runtime;

/// <summary>
/// Projects definition shapes only. Runtime effective-method admission and current finalized-fact
/// receipts are still required before these detached values may initialize a player preview.
/// </summary>
public static class NativeDrugGenerationFacts
{
    private static readonly FieldInfo? DivideByBodySize = typeof(IngestionOutcomeDoer_GiveHediff)
        .GetField("divideByBodySize", BindingFlags.Instance | BindingFlags.NonPublic);

    public static IReadOnlyList<DrugGenerationFacts> Capture(
        IReadOnlyList<DrugCatalogEntry> catalog, IReadOnlyCollection<ThingDef> unresolvedPartialDoses)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(unresolvedPartialDoses);
        var identities = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var entry in catalog)
            if (entry?.Identity is not ThingDef || !identities.Add(entry.Identity))
                throw new ArgumentException("The captured catalog must contain distinct native drug identities.", nameof(catalog));
        return Array.AsReadOnly(catalog.Select(entry => Read(entry,
            unresolvedPartialDoses.Any(drug => ReferenceEquals(drug, entry.Identity)))).ToArray());
    }

    private static DrugGenerationFacts Read(DrugCatalogEntry entry, bool partial)
    {
        var drug = (ThingDef)entry.Identity;
        var properties = drug.comps?.OfType<CompProperties_Drug>().FirstOrDefault();
        var chemical = properties?.chemical;
        var tolerance = chemical?.toleranceHediff;
        var outcomes = drug.ingestible?.outcomeDoers;
        var givers = outcomes?.OfType<IngestionOutcomeDoer_GiveHediff>()
            .Where(item => tolerance != null && ReferenceEquals(item.hediffDef, tolerance)).ToArray();
        var giver = givers?.FirstOrDefault();
        var decay = tolerance?.comps?.OfType<HediffCompProperties_SeverityPerDay>().FirstOrDefault();
        var bodyFlagKnown = DivideByBodySize?.FieldType == typeof(bool) && giver != null;
        var facts = new DrugGenerationFacts(entry, chemical, tolerance, DrugGenerationSupport.Unknown,
            drug.ingestible?.drugCategory == DrugCategory.Medical, drug.ingestible?.joy ?? float.NaN,
            properties?.addictiveness ?? float.NaN, properties?.minToleranceToAddict ?? float.NaN, giver?.severity ?? float.NaN,
            bodyFlagKnown && (bool)DivideByBodySize!.GetValue(giver)!, tolerance?.maxSeverity ?? float.NaN, decay?.severityPerDay ?? float.NaN,
            properties?.overdoseSeverityOffset.min ?? float.NaN, properties?.overdoseSeverityOffset.max ?? float.NaN,
            properties?.largeOverdoseChance ?? float.NaN);
        if (partial) return facts with { Support = DrugGenerationSupport.UnresolvedPartialDose };
        if (properties == null || drug.category != ThingCategory.Item || drug.ingestible == null ||
            drug.ingestible.drugCategory == DrugCategory.None || entry.Addictive != (properties.addictiveness > 0f) ||
            entry.Pleasure != (drug.ingestible.joy > 0f)) return facts;
        if (drug.thingClass != typeof(ThingWithComps) || properties.GetType() != typeof(CompProperties_Drug) ||
            properties.compClass != typeof(CompDrug) || drug.comps.Count(item => item is CompProperties_Drug) != 1 ||
            drug.comps.Any(item => !KnownItemComp(item)) || drug.ingestible.defaultNumToIngestAtOnce != 1 ||
            drug.ingestible.maxNumToIngestAtOnce != 1)
            return facts with { Support = DrugGenerationSupport.UnsupportedEffects };
        if (chemical?.addictionHediff == null || tolerance == null || givers?.Length != 1 ||
            giver!.GetType() != typeof(IngestionOutcomeDoer_GiveHediff) || giver.chance != 1f ||
            !float.IsFinite(giver.severity) || giver.severity <= 0f || !bodyFlagKnown ||
            (giver.toleranceChemical != null && !ReferenceEquals(giver.toleranceChemical, chemical)))
            return facts with { Support = DrugGenerationSupport.UnsupportedTolerance };
        if (tolerance.hediffClass != typeof(HediffWithComps) || !KnownDecay(tolerance, chemical, out _) ||
            !KnownSecondaryEffects(tolerance, properties.minToleranceToAddict, exclusiveBound: true))
            return facts with { Support = DrugGenerationSupport.UnsupportedDecay };
        var highIdentities = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var outcome in outcomes!)
        {
            if (ReferenceEquals(outcome, giver)) continue;
            if (outcome?.GetType() == typeof(IngestionOutcomeDoer_GiveHediff) &&
                KnownHigh((IngestionOutcomeDoer_GiveHediff)outcome, chemical, highIdentities)) continue;
            if (outcome?.GetType() == typeof(IngestionOutcomeDoer_OffsetNeed))
            {
                var offset = (IngestionOutcomeDoer_OffsetNeed)outcome;
                if (offset.chance == 1f && !offset.perIngested && float.IsFinite(offset.offset) &&
                    offset.need?.needClass == typeof(Need_Rest) &&
                    (offset.toleranceChemical == null || ReferenceEquals(offset.toleranceChemical, chemical))) continue;
            }
            return facts with { Support = DrugGenerationSupport.UnsupportedEffects };
        }
        return facts with { Support = DrugGenerationSupport.OrdinaryWholeDose };
    }

    private static bool KnownItemComp(CompProperties? item) => item != null &&
        ((item.GetType() == typeof(CompProperties_Drug) && item.compClass == typeof(CompDrug)) ||
         (item.GetType() == typeof(CompProperties_Forbiddable) && item.compClass == typeof(CompForbiddable)) ||
         (item.GetType() == typeof(CompProperties_Rottable) && item.compClass == typeof(CompRottable)));

    private static bool KnownDecay(HediffDef hediff, ChemicalDef chemical, out float perDay, float? highPeak = null)
    {
        perDay = float.NaN;
        if (hediff.minSeverity != 0f || !float.IsFinite(hediff.maxSeverity) || hediff.maxSeverity <= 0f ||
            !float.IsFinite(hediff.initialSeverity) || !float.IsFinite(hediff.lethalSeverity) ||
            hediff.lethalSeverity >= 0 || hediff.chemicalNeed != null ||
            hediff.onlyLifeThreateningTo?.Count > 0 ||
            hediff.removeWithTags?.Count > 0 || hediff.abilities?.Count > 0 ||
            hediff.comps == null || !KnownStages(hediff.stages)) return false;
        var decayCount = 0;
        var factorCount = 0;
        foreach (var comp in hediff.comps)
        {
            if (comp?.GetType() == typeof(HediffCompProperties_SeverityPerDay) &&
                comp.compClass == typeof(HediffComp_SeverityPerDay))
            {
                var decay = (HediffCompProperties_SeverityPerDay)comp;
                if (++decayCount != 1 || !float.IsFinite(decay.severityPerDay) || decay.severityPerDay >= 0f ||
                    decay.severityPerDayRange.min != 0f || decay.severityPerDayRange.max != 0f ||
                    decay.reverseSeverityChangeChance != 0f || decay.minAge != 0f || decay.mechanitorFactor != 1f)
                    return false;
                perDay = decay.severityPerDay;
            }
            else if (highPeak == null && comp?.GetType() == typeof(HediffCompProperties_DrugEffectFactor) &&
                     comp.compClass == typeof(HediffComp_DrugEffectFactor) &&
                     ReferenceEquals(((HediffCompProperties_DrugEffectFactor)comp).chemical, chemical))
            {
                if (++factorCount != 1) return false;
            }
            else if (highPeak.HasValue && comp?.GetType() == typeof(HediffCompProperties_Effecter) &&
                     comp.compClass == typeof(HediffComp_Effecter))
            {
                var effecter = (HediffCompProperties_Effecter)comp;
                if (effecter.severityIndices.min <= MaximumStage(hediff, highPeak.Value) ||
                    (effecter.severityIndices.max >= 0 && effecter.severityIndices.max < effecter.severityIndices.min)) return false;
            }
            else return false;
        }
        return decayCount == 1;
    }

    private static bool KnownStages(List<HediffStage>? stages)
    {
        if (stages == null || stages.Count == 0) return true;
        var prior = -1f;
        for (var i = 0; i < stages.Count; i++)
        {
            var stage = stages[i];
            if (stage == null || !float.IsFinite(stage.minSeverity) || stage.minSeverity <= prior ||
                (i == 0 && stage.minSeverity != 0f) || stage.severityGainFactor != 1f) return false;
            prior = stage.minSeverity;
        }
        return true;
    }

    private static bool KnownHigh(IngestionOutcomeDoer_GiveHediff giver, ChemicalDef chemical, HashSet<object> identities)
    {
        var high = giver.hediffDef;
        if (giver.chance != 1f || !float.IsFinite(giver.severity) || giver.severity <= 0f || high == null ||
            !identities.Add(high) || (bool)DivideByBodySize!.GetValue(giver)! ||
            (giver.toleranceChemical != null && !ReferenceEquals(giver.toleranceChemical, chemical)) ||
            (high.hediffClass != typeof(Hediff_High) && high.hediffClass != typeof(Hediff_Alcohol)) ||
            giver.severity > high.maxSeverity || !KnownDecay(high, chemical, out var perDay, giver.severity) ||
            !KnownSecondaryEffects(high, giver.severity, exclusiveBound: false)) return false;
        var maximumStage = MaximumStage(high, giver.severity);
        if (high.hediffClass == typeof(Hediff_Alcohol) && maximumStage >= 3) return false;
        // Earliest C1 interval is two calendar days. This bound fits its one modeled elapsed day.
        var remaining = giver.severity;
        var step = perDay * 0.0033333334f;
        for (var i = 0; i < 297 && remaining > 0; i++)
        {
            var next = Math.Max(0f, remaining + step);
            if (!float.IsFinite(next) || next >= remaining) return false;
            remaining = next;
        }
        return remaining == 0f;
    }

    private static int MaximumStage(HediffDef hediff, float peak)
    {
        var index = 0;
        if (hediff.stages != null)
            for (var i = 1; i < hediff.stages.Count && hediff.stages[i].minSeverity <= peak; i++) index = i;
        return index;
    }

    private static bool KnownSecondaryEffects(HediffDef hediff, float bound, bool exclusiveBound)
    {
        if (!float.IsFinite(bound) || bound <= 0f || !InactiveGivers(hediff.hediffGivers, bound)) return false;
        if (hediff.stages == null) return true;
        foreach (var stage in hediff.stages)
        {
            if (exclusiveBound ? stage.minSeverity >= bound : stage.minSeverity > bound) continue;
            if (!InactiveGivers(stage.hediffGivers, bound) || stage.mentalStateGivers?.Count > 0 ||
                !float.IsFinite(stage.mentalBreakMtbDays) || stage.mentalBreakMtbDays > 0f) return false;
        }
        return true;
    }

    private static bool InactiveGivers(List<HediffGiver>? givers, float bound)
    {
        if (givers == null) return true;
        foreach (var giver in givers)
        {
            if (giver?.GetType() != typeof(HediffGiver_RandomDrugEffect)) return false;
            var effect = (HediffGiver_RandomDrugEffect)giver;
            var curve = effect.severityToMtbDaysCurve;
            if (curve == null)
            {
                if (!float.IsFinite(effect.minSeverity) || effect.minSeverity < bound ||
                    !float.IsFinite(effect.baseMtbDays) || effect.baseMtbDays <= 0f) return false;
                continue;
            }
            if (curve.GetType() != typeof(SimpleCurve) || curve.Points.Count == 0 || curve.Points[0].x < bound) return false;
            var priorX = -1f;
            var priorY = float.PositiveInfinity;
            foreach (var point in curve.Points)
            {
                if (!float.IsFinite(point.x) || point.x <= priorX || !float.IsFinite(point.y) ||
                    point.y <= 0f || point.y > priorY) return false;
                priorX = point.x;
                priorY = point.y;
            }
        }
        return true;
    }
}
