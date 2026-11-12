using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Megamod.Features.DrugPolicyMaintenance.Core;
using RimWorld;
using Verse;

namespace Megamod.Features.DrugPolicyGeneration.Runtime;

// Caller must obtain CURRENT inputs at its synchronous native boundary. This record is not a
// provider/lifecycle receipt. In particular, a supplied curve is not proved to be the static field.
public sealed record DrugGenerationFactInputHandles(
    DrugCatalogRead Catalog,
    IReadOnlyCollection<ThingDef> UnresolvedPartialDoses,
    HumanAdultInput HumanAdult,
    SimpleCurve EffectFactorCurve);

// This receipt says only that the declared captured inputs still match supplied CURRENT handles.
// It never claims supported pharmacology, finalized Human/static-field binding, or Create authority.
public sealed class DrugGenerationFactInputs : IDisposable
{
    internal const int MaximumListEntries = HumanAdultInput.MaximumListEntries;
    private DrugCatalogRead? catalog;
    private Graph? captured;
    private bool stale;
    private static readonly FieldInfo? BodyDivision = typeof(IngestionOutcomeDoer_GiveHediff).GetField(
        "divideByBodySize", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

    private DrugGenerationFactInputs(DrugCatalogRead catalog, Graph captured)
    { this.catalog = catalog; this.captured = captured; }

    public static DrugGenerationFactInputs Capture(DrugGenerationFactInputHandles current)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(current.Catalog);
        if (!current.Catalog.IsCurrent) throw new InvalidOperationException("The supplied initial catalog has expired.");
        var graph = Read(current);
        if (!current.Catalog.IsCurrent) throw new InvalidOperationException("The supplied initial catalog expired during capture.");
        return new(current.Catalog, graph);
    }

    public bool MatchesCurrent(DrugGenerationFactInputHandles freshlyReadCurrent)
    {
        if (stale || captured == null || catalog == null) return false;
        try
        {
            if (freshlyReadCurrent == null || !ReferenceEquals(catalog, freshlyReadCurrent.Catalog) || !catalog.IsCurrent)
                return Invalidate();
            var actual = Read(freshlyReadCurrent);
            if (!catalog.IsCurrent || !captured.Equals(actual)) return Invalidate();
            return true;
        }
        catch (Exception) { return Invalidate(); }
    }

    private bool Invalidate() { Dispose(); return false; }
    public void Dispose() { stale = true; captured = null; catalog = null; }

    private static Graph Read(DrugGenerationFactInputHandles input)
    {
        ArgumentNullException.ThrowIfNull(input.UnresolvedPartialDoses);
        ArgumentNullException.ThrowIfNull(input.HumanAdult);
        ArgumentNullException.ThrowIfNull(input.EffectFactorCurve);
        if (BodyDivision?.DeclaringType != typeof(IngestionOutcomeDoer_GiveHediff) ||
            BodyDivision.IsStatic || BodyDivision.FieldType != typeof(bool))
            throw new InvalidOperationException("The declared GiveHediff body-division Boolean field is unavailable.");
        var budget = new Budget();
        // Materialize supplied membership before native field reads. Do not retain an enumerable
        // whose later iteration could observe a different input or call into an external provider.
        var partial = new IdentitySet(input.UnresolvedPartialDoses, budget);
        var entries = input.Catalog.Entries;
        budget.Take(entries.Count);
        var identities = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var drugs = new DrugNode[entries.Count];
        for (int i = 0; i < drugs.Length; i++)
        {
            var entry = entries[i];
            if (entry?.Identity is not ThingDef drug || !identities.Add(drug))
                throw new ArgumentException("Catalog entries must retain distinct native drug identities.");
            drugs[i] = new(new(drug), drug.GetType(), entry.Addictive, entry.Pleasure,
                drug.category, new(drug.thingClass), List(drug.comps, comp => ItemComp(comp, budget), budget),
                Ingestible(drug.ingestible, budget));
        }
        return new(new(new(entries), drugs), partial, Human(input.HumanAdult, budget), Curve(input.EffectFactorCurve, budget)!);
    }

    private static IngestibleNode? Ingestible(IngestibleProperties? value, Budget budget) => value == null ? null :
        new(new(value), value.GetType(), value.drugCategory, S(value.joy), value.defaultNumToIngestAtOnce,
            value.maxNumToIngestAtOnce, List(value.outcomeDoers, item => Outcome(item, budget), budget));

    private static ItemCompNode? ItemComp(CompProperties? value, Budget budget)
    {
        if (value == null) return null;
        // Preserve declared base fields even for a rejected derived Drug comp. Nothing here
        // certifies that subclass's extra state or effective callbacks.
        DrugCompNode? drug = value is CompProperties_Drug item ? new(
            Chemical(item.chemical, budget), S(item.addictiveness), S(item.minToleranceToAddict),
            S(item.overdoseSeverityOffset.min), S(item.overdoseSeverityOffset.max),
            S(item.largeOverdoseChance), S(item.listOrder)) : null;
        return new(new(value), value.GetType(), new(value.compClass), drug);
    }

    private static ChemicalNode? Chemical(ChemicalDef? value, Budget budget) => value == null ? null :
        new(new(value), value.GetType(), new(value.addictionHediff), Hediff(value.toleranceHediff, budget));

    private static OutcomeNode? Outcome(IngestionOutcomeDoer? value, Budget budget)
    {
        if (value == null) return null;
        GiveNode? give = value is IngestionOutcomeDoer_GiveHediff giver ? new(
            Hediff(giver.hediffDef, budget), S(giver.severity), new(giver.toleranceChemical),
            (bool)BodyDivision!.GetValue(giver)!, giver.multiplyByGeneToleranceFactors) : null;
        OffsetNode? offset = value is IngestionOutcomeDoer_OffsetNeed need ? new(
            new(need.need), need.need?.GetType(), new(need.need?.needClass), S(need.offset),
            new(need.toleranceChemical), need.perIngested) : null;
        return new(new(value), value.GetType(), S(value.chance), give, offset);
    }

    private static HediffNode? Hediff(HediffDef? value, Budget budget) => value == null ? null : new(
        new(value), value.GetType(), new(value.hediffClass), S(value.minSeverity), S(value.maxSeverity),
        S(value.initialSeverity), S(value.lethalSeverity), S(value.chanceToCauseNoPain), new(value.chemicalNeed),
        List(value.onlyLifeThreateningTo, item => new Identity(item), budget),
        List(value.removeWithTags, item => item, budget),
        List(value.abilities, item => new Identity(item), budget),
        List(value.comps, item => HediffComp(item), budget),
        List(value.stages, item => Stage(item, budget), budget),
        List(value.hediffGivers, item => Giver(item, budget), budget),
        new(value.taleOnVisible), List(value.aptitudes, item => item == null ? null :
            new AptitudeNode(new(item), item.GetType(), new(item.skill), item.level), budget), value.clearsEgo);

    private static HediffCompNode? HediffComp(HediffCompProperties? value)
    {
        if (value == null) return null;
        DecayNode? decay = value is HediffCompProperties_SeverityPerDay item ? new(
            S(item.severityPerDay), S(item.severityPerDayRange.min), S(item.severityPerDayRange.max),
            S(item.reverseSeverityChangeChance), S(item.minAge), S(item.mechanitorFactor)) : null;
        Identity chemical = new(value is HediffCompProperties_DrugEffectFactor factor ? factor.chemical : null);
        EffecterNode? effecter = value is HediffCompProperties_Effecter effect ? new(
            effect.severityIndices.min, effect.severityIndices.max, new(effect.stateEffecter)) : null;
        return new(new(value), value.GetType(), new(value.compClass), decay, chemical, effecter);
    }

    private static StageNode? Stage(HediffStage? value, Budget budget) => value == null ? null : new(
        new(value), value.GetType(), S(value.minSeverity), S(value.severityGainFactor),
        List(value.hediffGivers, item => Giver(item, budget), budget),
        List(value.mentalStateGivers, item => item == null ? null : new MentalNode(new(item), item.GetType(),
            S(item.mtbDays), new(item.mentalState)), budget), S(value.mentalBreakMtbDays),
        S(value.vomitMtbDays), S(value.forgetMemoryThoughtMtbDays), S(value.deathMtbDays),
        value.mtbDeathDestroysBrain, new(value.tale), value.destroyPart,
        S(value.pctConditionalThoughtsNullified), S(value.pctAllThoughtNullification));

    private static GiverNode? Giver(HediffGiver? value, Budget budget)
    {
        if (value == null) return null;
        RandomEffectNode? effect = value is HediffGiver_RandomDrugEffect random ? new(
            S(random.minSeverity), S(random.baseMtbDays), Curve(random.severityToMtbDaysCurve, budget)) : null;
        // Other giver payloads are opaque. A stable unknown reachable giver is still unsupported.
        return new(new(value), value.GetType(), new(value.hediff), effect);
    }

    private static CurveNode? Curve(SimpleCurve? value, Budget budget) => value == null ? null :
        new(new(value), value.GetType(), List(value.Points, point => new PointNode(S(point.x), S(point.y)), budget));

    private static HumanNode Human(HumanAdultInput value, Budget budget)
    {
        budget.Take(value.LifeStages.Count);
        var stages = value.LifeStages.Select(stage => new HumanStageNode(
            new(stage.EntryIdentity), stage.EntryIdentity.GetType(), new(stage.DefinitionIdentity),
            stage.DefinitionIdentity.GetType(), S(stage.MinimumAge), stage.DevelopmentalStage, S(stage.BodySizeFactor))).ToArray();
        return new(new(value.HumanIdentity), value.HumanIdentity.GetType(), new(value.RaceIdentity),
            value.RaceIdentity.GetType(), value.RaceIntelligence, S(value.RaceBaseBodySize),
            new(new(value.LifeStagesIdentity), stages), new(value.AdultEntryIdentity));
    }

    private static NodeList<TTarget>? List<TSource, TTarget>(List<TSource>? values,
        Func<TSource, TTarget> read, Budget budget)
    {
        if (values == null) return null;
        budget.Take(values.Count);
        var result = new TTarget[values.Count];
        for (int index = 0; index < result.Length; index++) result[index] = read(values[index]);
        return new(new(values), result);
    }
    private static int S(float value) => BitConverter.SingleToInt32Bits(value);

    // Known fixed-depth shape. These private immutable records are never returned to callers.
    private sealed record Graph(NodeList<DrugNode> Drugs, IdentitySet Partial, HumanNode Human, CurveNode EffectCurve);
    private sealed record DrugNode(Identity Identity, Type Type, bool Addictive, bool Pleasure,
        ThingCategory Category, Identity ThingClass, NodeList<ItemCompNode?>? Comps, IngestibleNode? Ingestible);
    private sealed record IngestibleNode(Identity Identity, Type Type, DrugCategory Category, int Joy,
        int DefaultQuantity, int MaximumQuantity, NodeList<OutcomeNode?>? Outcomes);
    private sealed record ItemCompNode(Identity Identity, Type Type, Identity CompClass, DrugCompNode? Drug);
    private sealed record DrugCompNode(ChemicalNode? Chemical, int Addictiveness, int Threshold,
        int OverdoseMin, int OverdoseMax, int LargeChance, int ListOrder);
    private sealed record ChemicalNode(Identity Identity, Type Type, Identity Addiction, HediffNode? Tolerance);
    private sealed record OutcomeNode(Identity Identity, Type Type, int Chance, GiveNode? Give, OffsetNode? Offset);
    private sealed record GiveNode(HediffNode? Hediff, int Severity, Identity Chemical, bool DivideByBody, bool GeneFactor);
    private sealed record OffsetNode(Identity Need, Type? NeedType, Identity NeedClass, int Offset, Identity Chemical, bool PerIngested);
    private sealed record HediffNode(Identity Identity, Type Type, Identity HediffClass, int Min, int Max, int Initial,
        int Lethal, int NoPainChance, Identity ChemicalNeed, NodeList<Identity>? BodyParts,
        NodeList<string>? RemoveTags, NodeList<Identity>? Abilities, NodeList<HediffCompNode?>? Comps,
        NodeList<StageNode?>? Stages, NodeList<GiverNode?>? Givers, Identity TaleOnVisible,
        NodeList<AptitudeNode?>? Aptitudes, bool ClearsEgo);
    private sealed record HediffCompNode(Identity Identity, Type Type, Identity CompClass, DecayNode? Decay,
        Identity Chemical, EffecterNode? Effecter);
    private sealed record DecayNode(int PerDay, int RangeMin, int RangeMax, int ReverseChance, int MinAge, int MechanitorFactor);
    private sealed record EffecterNode(int MinIndex, int MaxIndex, Identity Effecter);
    private sealed record StageNode(Identity Identity, Type Type, int Minimum, int GainFactor,
        NodeList<GiverNode?>? Givers, NodeList<MentalNode?>? MentalGivers, int MentalBreakMtb,
        int VomitMtb, int ForgetMtb, int DeathMtb, bool DestroysBrain, Identity Tale, bool DestroyPart,
        int ConditionalThoughtNullification, int AllThoughtNullification);
    private sealed record MentalNode(Identity Identity, Type Type, int Mtb, Identity MentalState);
    private sealed record GiverNode(Identity Identity, Type Type, Identity Hediff, RandomEffectNode? Effect);
    private sealed record RandomEffectNode(int Minimum, int BaseMtb, CurveNode? Curve);
    private sealed record CurveNode(Identity Identity, Type Type, NodeList<PointNode>? Points);
    private sealed record PointNode(int X, int Y);
    private sealed record HumanNode(Identity Human, Type HumanType, Identity Race, Type RaceType,
        int Intelligence, int BaseSize, NodeList<HumanStageNode> Stages, Identity AdultEntry);
    private sealed record HumanStageNode(Identity Entry, Type EntryType, Identity Def, Type DefType,
        int MinimumAge, int DevelopmentalStage, int BodyFactor);
    private sealed record AptitudeNode(Identity Identity, Type Type, Identity Skill, int Level);

    private readonly struct Identity : IEquatable<Identity>
    {
        private readonly object? value;
        internal Identity(object? value) => this.value = value;
        public bool Equals(Identity other) => ReferenceEquals(value, other.value);
        public override bool Equals(object? other) => other is Identity identity && Equals(identity);
        public override int GetHashCode() => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
    }

    private sealed class NodeList<T> : IEquatable<NodeList<T>>
    {
        private readonly Identity identity;
        private readonly T[] items;
        internal NodeList(Identity identity, T[] ownedItems) { this.identity = identity; items = ownedItems; }
        public bool Equals(NodeList<T>? other) => other != null && identity.Equals(other.identity) && items.SequenceEqual(other.items);
        public override bool Equals(object? other) => other is NodeList<T> list && Equals(list);
        public override int GetHashCode() => HashCode.Combine(identity, items.Length);
    }

    private sealed class IdentitySet : IEquatable<IdentitySet>
    {
        private readonly HashSet<object> members = new(ReferenceEqualityComparer.Instance);
        internal IdentitySet(IReadOnlyCollection<ThingDef> values, Budget budget)
        {
            int expectedCount = values.Count;
            budget.Take(expectedCount);
            int observed = 0;
            foreach (var value in values)
            {
                if (value == null || ++observed > expectedCount)
                    throw new ArgumentException("Partial-dose membership is null or changed during its supplied read.");
                members.Add(value);
            }
            if (observed != expectedCount || values.Count != expectedCount)
                throw new ArgumentException("Partial-dose membership changed during its supplied read.");
        }
        public bool Equals(IdentitySet? other) => other != null && members.SetEquals(other.members);
        public override bool Equals(object? other) => other is IdentitySet set && Equals(set);
        public override int GetHashCode() => members.Count;
    }

    private sealed class Budget
    {
        private int remaining = 65536;
        internal void Take(int count)
        {
            if (count < 0 || count > MaximumListEntries || count > remaining)
                throw new InvalidOperationException("The complete consumed-input graph exceeds its declared capture bound.");
            remaining -= count;
        }
    }
}

