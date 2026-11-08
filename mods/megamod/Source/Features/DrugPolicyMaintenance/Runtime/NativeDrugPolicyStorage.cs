using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Megamod.Features.DrugPolicyMaintenance.Core;
using RimWorld;
using Verse;

namespace Megamod.Features.DrugPolicyMaintenance.Runtime;

public sealed class NativeDrugPolicyStorage : IDrugPolicyStorage
{
    private static readonly FieldInfo Entries = typeof(DrugPolicy).GetField("entriesInt", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private readonly DrugPolicy policy;

    public NativeDrugPolicyStorage(DrugPolicy policy) => this.policy = policy;

    public DrugPolicyStorageSnapshot Capture()
    {
        var entries = (List<DrugPolicyEntry>?)Entries.GetValue(policy);
        return new DrugPolicyStorageSnapshot(entries, entries?.Select(entry => entry == null ? null :
            new DrugPolicyRow(entry, entry.drug, new DrugPolicyChoices(
                entry.allowedForAddiction, entry.allowedForJoy, entry.allowScheduled,
                entry.daysFrequency, entry.onlyIfMoodBelow, entry.onlyIfJoyBelow, entry.takeToInventory))).ToArray());
    }

    public void Apply(DrugPolicyPlan plan)
    {
        var entries = new List<DrugPolicyEntry>();
        foreach (var row in plan.Rows)
        {
            var entry = row.RowIdentity == null ? new DrugPolicyEntry() : (DrugPolicyEntry)row.RowIdentity;
            Write(entry, row);
            entries.Add(entry);
        }
        Entries.SetValue(policy, entries);
    }

    public void Restore(DrugPolicyStorageSnapshot snapshot)
    {
        var entries = (List<DrugPolicyEntry>?)snapshot.StorageIdentity;
        if (entries != null)
        {
            entries.Clear();
            foreach (var row in snapshot.Rows!)
            {
                var entry = (DrugPolicyEntry?)row?.RowIdentity;
                if (row != null)
                    Write(entry!, row);
                entries.Add(entry!);
            }
        }
        Entries.SetValue(policy, entries);
    }

    private static void Write(DrugPolicyEntry entry, DrugPolicyRow row)
    {
        entry.drug = (ThingDef)row.DrugIdentity!;
        entry.allowedForAddiction = row.Choices.Addiction;
        entry.allowedForJoy = row.Choices.Recreation;
        entry.allowScheduled = row.Choices.Scheduled;
        entry.daysFrequency = row.Choices.DaysFrequency;
        entry.onlyIfMoodBelow = row.Choices.MoodThreshold;
        entry.onlyIfJoyBelow = row.Choices.RecreationThreshold;
        entry.takeToInventory = row.Choices.InventoryTarget;
    }
}
