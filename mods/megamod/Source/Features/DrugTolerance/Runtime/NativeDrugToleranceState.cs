using System;
using System.Runtime.CompilerServices;
using Megamod.Features.DrugTolerance.Core;
using RimWorld;

namespace Megamod.Features.DrugTolerance.Runtime;

/// <summary>Game-thread-owned state on exact native rows; weak keys never retain a disposed save.</summary>
public static class NativeDrugToleranceState
{
    private sealed class EntryState
    {
        public DrugToleranceChoice Choice = DrugToleranceChoice.Any;
    }

    private static readonly ConditionalWeakTable<DrugPolicyEntry, EntryState> Entries = new();

    public static DrugToleranceChoice Read(DrugPolicyEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return Entries.TryGetValue(entry, out var state) ? state.Choice : DrugToleranceChoice.Any;
    }

    public static void Write(DrugPolicyEntry entry, DrugToleranceChoice choice)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(choice);
        Entries.GetValue(entry, _ => new EntryState()).Choice = choice;
    }

    public static string? ReadEncoded(DrugPolicyEntry entry) => Read(entry).Encoding;

    public static void WriteEncoded(DrugPolicyEntry entry, string? encoding) =>
        Write(entry, DrugToleranceChoice.Decode(encoding));

    public static void Copy(DrugPolicyEntry source, DrugPolicyEntry destination) =>
        Write(destination, Read(source));
}
