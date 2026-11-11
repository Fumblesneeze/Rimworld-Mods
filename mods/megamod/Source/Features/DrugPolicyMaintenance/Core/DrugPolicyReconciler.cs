using System;
using System.Collections.Generic;
using Megamod.Features.DrugTolerance.Core;

namespace Megamod.Features.DrugPolicyMaintenance.Core;

public sealed record DrugPolicyChoices(
    bool Addiction,
    bool Recreation,
    bool Scheduled,
    float DaysFrequency,
    float MoodThreshold,
    float RecreationThreshold,
    int InventoryTarget);

// These handles identify existing engine objects without loading Verse in the policy layer.
public sealed record DrugPolicyRow(
    object? RowIdentity, object? DrugIdentity, DrugPolicyChoices Choices, DrugToleranceChoice? ToleranceChoice = null);

public sealed record DrugCatalogEntry(object Identity, bool Addictive, bool Pleasure);

public sealed record DrugPolicyPlan(
    IReadOnlyList<DrugPolicyRow> Rows, int Removed, int Duplicates, int Added = 0, int Normalized = 0)
{
    public bool Changed => Removed + Duplicates + Added + Normalized != 0;
}

public static class DrugPolicyReconciler
{
    public static DrugPolicyPlan Plan(
        IReadOnlyList<DrugPolicyRow?>? rows,
        IReadOnlyList<DrugCatalogEntry> catalog,
        Action<List<DrugPolicyRow>>? applyNativeOrdering = null)
    {
        var eligible = new Dictionary<object, DrugCatalogEntry>(ReferenceEqualityComparer.Instance);
        foreach (var drug in catalog)
            eligible.Add(drug.Identity, drug);

        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var retained = new List<DrugPolicyRow>();
        var removed = 0;
        var duplicates = 0;
        var normalized = 0;
        foreach (var row in rows ?? Array.Empty<DrugPolicyRow?>())
        {
            if (row?.DrugIdentity is null || !eligible.TryGetValue(row.DrugIdentity, out var drug))
            {
                removed++;
                continue;
            }

            if (!seen.Add(row.DrugIdentity))
            {
                duplicates++;
                continue;
            }

            var choices = row.Choices with
            {
                Addiction = row.Choices.Addiction && drug.Addictive,
                Recreation = row.Choices.Recreation && drug.Pleasure
            };
            if (choices != row.Choices)
                normalized++;
            retained.Add(row with { Choices = choices });
        }

        var added = 0;
        foreach (var drug in catalog)
        {
            if (!seen.Contains(drug.Identity))
            {
                added++;
                retained.Add(new DrugPolicyRow(null, drug.Identity,
                    new DrugPolicyChoices(drug.Addictive, false, false, 1f, 1f, 1f, 0)));
            }
        }

        if (added != 0)
        {
            if (applyNativeOrdering is null)
                throw new InvalidOperationException("Drug additions require the admitted native ordering operation.");
            var expectedRows = new HashSet<DrugPolicyRow>(retained, ReferenceEqualityComparer.Instance);
            applyNativeOrdering(retained);
            if (retained.Count != expectedRows.Count || !expectedRows.SetEquals(retained))
                throw new InvalidOperationException("Native ordering changed the planned policy rows.");
        }

        return new DrugPolicyPlan(Array.AsReadOnly(retained.ToArray()), removed, duplicates, added, normalized);
    }
}
