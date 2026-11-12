using System;
using Megamod.Features.DrugPolicyMaintenance.Core;
using RimWorld;
using Verse;

namespace Megamod.Features.DrugPolicyGeneration.Runtime;

// Native origin read only. Root must supply its actual initial catalog; this does not prove
// applied drug exclusions, current Game authority, pawn BodySize/stage semantics or preview readiness.
public static class NativeDrugGenerationHumanInput
{
    public static (HumanAdultInput Input, float BaselineBodySize) ReadCurrent(DrugCatalogRead catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (!UnityData.IsInMainThread || !PlayDataLoader.Loaded || !catalog.IsCurrent)
            throw new InvalidOperationException("A current finalized native definition boundary is required for the Human baseline.");

        var human = ThingDefOf.Human ?? throw new InvalidOperationException("The finalized Human definition is unavailable.");
        var fullAdult = LifeStageDefOf.HumanlikeAdult ?? throw new InvalidOperationException("The finalized full-grown Human stage is unavailable.");
        var race = human.race ?? throw new InvalidOperationException("The finalized Human race properties are unavailable.");
        var stages = race.lifeStageAges ?? throw new InvalidOperationException("The finalized Human stage list is unavailable.");
        if (stages.Count > HumanAdultInput.MaximumListEntries)
            throw new InvalidOperationException("The native Human stage list exceeds its declared capture bound.");
        var read = new HumanLifeStageInput[stages.Count];
        for (int index = 0; index < read.Length; index++)
        {
            var entry = stages[index] ?? throw new InvalidOperationException("The native Human stage list contains a null entry.");
            var definition = entry.def ?? throw new InvalidOperationException("A native Human stage has no resolved definition.");
            read[index] = new HumanLifeStageInput(entry, definition, entry.minAge,
                (int)definition.developmentalStage, definition.bodySizeFactor);
        }
        var result = HumanAdultInput.FromFullAdult(human, race, stages, (int)race.intelligence,
            race.baseBodySize, read, fullAdult);
        if (!catalog.IsCurrent || !ReferenceEquals(ThingDefOf.Human, human) ||
            !ReferenceEquals(LifeStageDefOf.HumanlikeAdult, fullAdult) ||
            !ReferenceEquals(human.race, race) || !ReferenceEquals(race.lifeStageAges, stages))
            throw new InvalidOperationException("The native Human baseline origins changed during their read.");
        return result;
    }
}
