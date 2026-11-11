using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace Megamod.Features.DrugTolerance.Runtime;

public static class NativeDrugToleranceRows
{
    private static readonly FieldInfo Entries = ResolveEntries();
    private static FieldInfo ResolveEntries()
    {
        var field = typeof(DrugPolicy).GetField("entriesInt", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        if (field?.FieldType != typeof(List<DrugPolicyEntry>) || field.IsStatic) throw new InvalidOperationException("The native policy row storage changed.");
        return field;
    }
    public static IReadOnlyList<DrugPolicyEntry>? Read(DrugPolicy policy) => (List<DrugPolicyEntry>?)Entries.GetValue(policy);
    public static DrugPolicyEntry? FindUnique(DrugPolicy policy, ThingDef drug)
    {
        var rows = Read(policy);
        if (rows == null) return null;
        DrugPolicyEntry? found = null;
        for (int i = 0; i < rows.Count; i++)
            if (ReferenceEquals(rows[i]?.drug, drug))
            {
                if (found != null) return null;
                found = rows[i];
            }
        return found;
    }
    public static bool IsCurrent(DrugPolicy policy, DrugPolicyEntry entry, ThingDef drug) => ReferenceEquals(FindUnique(policy, drug), entry);
}
