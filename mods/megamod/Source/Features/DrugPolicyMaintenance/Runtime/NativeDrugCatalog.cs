using System;
using System.Collections.Generic;
using Megamod.Features.DrugPolicyMaintenance.Core;
using RimWorld;
using Verse;

namespace Megamod.Features.DrugPolicyMaintenance.Runtime;

public static class NativeDrugCatalog
{
    public static IReadOnlyList<DrugCatalogEntry> Capture(Func<ThingDef, bool> isAvailable)
    {
        if (isAvailable == null)
            throw new ArgumentNullException(nameof(isAvailable));
        var catalog = new List<DrugCatalogEntry>();
        foreach (var definition in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (definition.category == ThingCategory.Item && definition.IsDrug && isAvailable(definition))
            {
                if (definition.comps == null || definition.GetCompProperties<CompProperties_Drug>() == null)
                    throw new InvalidOperationException($"ThingDef '{definition.defName}' lacks CompProperties_Drug required by native drug-policy ordering.");
                catalog.Add(new DrugCatalogEntry(definition, definition.IsAddictiveDrug, definition.IsPleasureDrug));
            }
        }
        return catalog.AsReadOnly();
    }
}
