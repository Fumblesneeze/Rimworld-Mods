using System;
using System.Collections.Generic;
using System.Linq;

namespace Megamod.Features.DrugPolicyGeneration.Runtime;

public sealed record HumanLifeStageInput(
    object EntryIdentity, object DefinitionIdentity, float MinimumAge,
    int DevelopmentalStage, float BodySizeFactor);

public sealed class HumanAdultInput
{
    // Single source for the existing F0 collection bound. DrugGenerationFactInputs aliases this.
    internal const int MaximumListEntries = 4096;

    // Existing raw snapshot constructor retains its current semantics, including malformed scalar
    // payloads needed by the comparison receipt. It does not certify an adult baseline.
    public HumanAdultInput(object humanIdentity, object raceIdentity, object lifeStagesIdentity,
        int raceIntelligence, float raceBaseBodySize, IEnumerable<HumanLifeStageInput> lifeStages,
        object adultEntryIdentity)
    {
        HumanIdentity = humanIdentity ?? throw new ArgumentNullException(nameof(humanIdentity));
        RaceIdentity = raceIdentity ?? throw new ArgumentNullException(nameof(raceIdentity));
        LifeStagesIdentity = lifeStagesIdentity ?? throw new ArgumentNullException(nameof(lifeStagesIdentity));
        RaceIntelligence = raceIntelligence;
        RaceBaseBodySize = raceBaseBodySize;
        AdultEntryIdentity = adultEntryIdentity ?? throw new ArgumentNullException(nameof(adultEntryIdentity));
        LifeStages = Array.AsReadOnly((lifeStages ?? throw new ArgumentNullException(nameof(lifeStages)))
            .Take(MaximumListEntries + 1).ToArray());
        if (LifeStages.Count > MaximumListEntries)
            throw new ArgumentException("The supplied life-stage read exceeds its declared capture bound.");
        if (LifeStages.Any(stage => stage == null || stage.EntryIdentity == null || stage.DefinitionIdentity == null) ||
            LifeStages.Count(stage => ReferenceEquals(stage.EntryIdentity, adultEntryIdentity)) != 1)
            throw new ArgumentException("The supplied adult entry must occur exactly once in the supplied life-stage read.");
    }
    public object HumanIdentity { get; }
    public object RaceIdentity { get; }
    public object LifeStagesIdentity { get; }
    public int RaceIntelligence { get; }
    public float RaceBaseBodySize { get; }
    public IReadOnlyList<HumanLifeStageInput> LifeStages { get; }
    public object AdultEntryIdentity { get; }

    // Pure rule over a caller-supplied current read. The native reader must supply the ACTUAL
    // LifeStageDefOf.HumanlikeAdult reference; this method cannot prove a supplied object's origin.
    public static (HumanAdultInput Input, float BaselineBodySize) FromFullAdult(
        object humanIdentity, object raceIdentity, object lifeStagesIdentity,
        int raceIntelligence, float raceBaseBodySize, IEnumerable<HumanLifeStageInput> lifeStages,
        object fullAdultDefinitionIdentity)
    {
        ArgumentNullException.ThrowIfNull(lifeStages);
        ArgumentNullException.ThrowIfNull(fullAdultDefinitionIdentity);
        var stages = lifeStages.Take(MaximumListEntries + 1).ToArray();
        if (stages.Length > MaximumListEntries)
            throw new ArgumentException("The supplied life-stage read exceeds its declared capture bound.");
        var fullAdult = stages.Where(stage => stage != null &&
            ReferenceEquals(stage.DefinitionIdentity, fullAdultDefinitionIdentity)).ToArray();
        if (fullAdult.Length != 1)
            throw new InvalidOperationException("The current Human stage list has missing or ambiguous full-grown adult membership.");
        if (!float.IsFinite(fullAdult[0].BodySizeFactor) || fullAdult[0].BodySizeFactor <= 0f ||
            !float.IsFinite(raceBaseBodySize) || raceBaseBodySize <= 0f)
            throw new InvalidOperationException("The current full-grown Human body-size factors are not positive finite values.");
        // Keep native Single operands and order. Age/classification are captured, not interpreted here.
        float baseline = fullAdult[0].BodySizeFactor * raceBaseBodySize;
        if (!float.IsFinite(baseline) || baseline <= 0f)
            throw new InvalidOperationException("The current full-grown Human body-size product is outside the model.");
        return (new HumanAdultInput(humanIdentity, raceIdentity, lifeStagesIdentity, raceIntelligence,
            raceBaseBodySize, stages, fullAdult[0].EntryIdentity), baseline);
    }
}
