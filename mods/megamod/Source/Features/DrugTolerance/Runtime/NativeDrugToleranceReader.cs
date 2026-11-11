using System.Collections.Generic;
using Megamod.Features.DrugTolerance.Core;
using RimWorld;
using Verse;

namespace Megamod.Features.DrugTolerance.Runtime;

public static class NativeDrugToleranceReader
{
    public static HediffDef? Resolve(ThingDef drug) => drug.IsDrug && drug.IsAddictiveDrug
        ? DrugStatsUtility.GetToleranceGiver(drug)?.hediffDef : null;

    public static DrugToleranceReading Read(ThingDef drug, IReadOnlyList<Hediff>? currentHediffs)
    {
        var tolerance = Resolve(drug);
        if (tolerance == null || currentHediffs == null) return DrugToleranceReading.Unavailable;
        for (int index = 0; index < currentHediffs.Count; index++)
        {
            var current = currentHediffs[index];
            if (current == null) return DrugToleranceReading.Unavailable;
            if (ReferenceEquals(current.def, tolerance))
                return DrugToleranceReading.FromSeverity(current.Severity);
        }
        return DrugToleranceReading.Absent;
    }
}
