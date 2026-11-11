using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace Megamod.Patching;

public sealed class NativeDefinitionConversionGuard
{
    private static readonly ConcurrentDictionary<MethodBase, NativeDefinitionConversionGuard> Routes = new();
    private readonly NativeDefinitionConversionPatch current, legacy;
    private readonly Func<bool> hasActiveFrame, safeToRefresh;
    private readonly Action invalidateCurrent;
    private readonly OwnedHarmonyPatch? currentReceipt, legacyReceipt;
    private volatile bool enabled;
    private PairPublication? publication;
    private sealed class PairPublication
    {
        private readonly NativeDefinitionConversionGuard owner;
        internal readonly object Current, Legacy;
        internal PairPublication(NativeDefinitionConversionGuard owner, object current, object legacy)
        { this.owner = owner; Current = current; Legacy = legacy; }
        internal bool IsCurrent() => owner.enabled && ReferenceEquals(this, Volatile.Read(ref owner.publication)) &&
            ReferenceEquals(Current, owner.current.PublishedGeneration) && ReferenceEquals(Legacy, owner.legacy.PublishedGeneration);
    }
    private static MethodInfo Callback => typeof(NativeDefinitionConversionGuard).GetMethod(nameof(Prefix), BindingFlags.NonPublic | BindingFlags.Static)!;
    public NativeDefinitionConversionGuard(NativeDefinitionConversionPatch current, NativeDefinitionConversionPatch legacy,
        Func<bool> hasActiveFrame, Action invalidateCurrent, Func<bool> safeToRefresh)
    {
        this.current = current; this.legacy = legacy;
        this.hasActiveFrame = hasActiveFrame; this.invalidateCurrent = invalidateCurrent; this.safeToRefresh = safeToRefresh;
        currentReceipt = Receipt(current); legacyReceipt = Receipt(legacy);
        Candidates = new[] { Candidate("DDI-GUARD-NEW", current, currentReceipt), Candidate("DDI-GUARD-LEGACY", legacy, legacyReceipt) };
    }
    public IReadOnlyList<PatchCandidate> Candidates { get; }
    public bool Available => Volatile.Read(ref publication)?.IsCurrent() == true;
    // Capture one immutable pair; later successful admission must not revive an older scope.
    public Func<bool>? CaptureValidity()
    {
        var captured = Volatile.Read(ref publication);
        return captured?.IsCurrent() == true ? captured.IsCurrent : null;
    }
    public void Enable()
    {
        if (!Refresh()) throw new InvalidOperationException("Both native definition entry paths require trusted coverage.");
        enabled = true;
    }
    private static OwnedHarmonyPatch? Receipt(NativeDefinitionConversionPatch route) => route.Target == null ? null :
        new OwnedHarmonyPatch(route.Target, HarmonyPatchType.Prefix, Callback, () => ReadOwnedSlot(route.Target));
    private static IReadOnlyList<Patch> ReadOwnedSlot(MethodBase target)
    {
        Patch[]? result = null;
        if (!HarmonyPublication.Inspect(target, snapshot =>
        {
            result = snapshot.Prefixes.Where(patch => patch.Owner == OwnedHarmonyPatch.OwnerId).Select(patch => patch.ToOrderingPatch()).ToArray();
            return true;
        })) throw new InvalidOperationException("Native definition entry ownership metadata is busy.");
        return result!;
    }
    private PatchCandidate Candidate(string id, NativeDefinitionConversionPatch route, OwnedHarmonyPatch? receipt) => new(id, id,
        route.Candidate.Target, "Definition context on both native loaders", "Ordinary diagnostics; independent warning capture",
        () => Validate(route.Target),
        () =>
        {
            if (route.Target == null || receipt == null || !Routes.TryAdd(route.Target, this))
                throw new InvalidOperationException("The native definition entry already has a guard or is missing.");
            receipt.Install();
            if (!HarmonyPublication.Inspect(route.Target, snapshot => GuardFirst(route.Target, snapshot)))
                throw new InvalidOperationException("The native definition guard is not the unique first prefix.");
        },
        () =>
        {
            enabled = false;
            Volatile.Write(ref publication, null);
            try { receipt?.Rollback(); }
            finally { if (route.Target != null && Routes.TryGetValue(route.Target, out var owner) && ReferenceEquals(owner, this)) Routes.TryRemove(route.Target, out _); }
        });
    private static string? Validate(MethodBase? target)
    {
        if (target == null) return "The native definition entry is missing.";
        string? failure = null;
        if (!HarmonyPublication.Inspect(target, snapshot =>
        {
            if (snapshot.Prefixes.Any(patch => patch.IsFactory)) failure = "A native definition prefix factory prevents safe coverage inspection.";
            else if (snapshot.Prefixes.Any(patch => patch.Owner == OwnedHarmonyPatch.OwnerId)) failure = "The owned native entry prefix slot is already occupied.";
            return true;
        })) return "Native definition entry metadata is busy.";
        return failure;
    }
    private bool Refresh()
    {
        bool admitted = HarmonyPublication.InspectPair(current.Target!, legacy.Target!, (first, second) =>
        {
            if (!GuardFirst(current.Target!, first) || !GuardFirst(legacy.Target!, second)) return false;
            if (!current.RefreshPublished(first) || !legacy.RefreshPublished(second)) return false;
            var currentGeneration = current.PublishedGeneration; var legacyGeneration = legacy.PublishedGeneration;
            if (currentGeneration == null || legacyGeneration == null) return false;
            var previous = Volatile.Read(ref publication);
            if (previous == null || !ReferenceEquals(previous.Current, currentGeneration) || !ReferenceEquals(previous.Legacy, legacyGeneration))
                Volatile.Write(ref publication, new PairPublication(this, currentGeneration, legacyGeneration));
            return true;
        });
        if (!admitted) Volatile.Write(ref publication, null);
        return admitted;
    }
    private static bool GuardFirst(MethodBase target, HarmonyPublication.Snapshot snapshot)
    {
        if (snapshot.Prefixes.Any(patch => patch.IsFactory) ||
            snapshot.Prefixes.Count(patch => patch.Owner == OwnedHarmonyPatch.OwnerId) != 1 ||
            snapshot.Prefixes.Count(patch => patch.Method == Callback) != 1 ||
            !snapshot.Prefixes.Any(patch => patch.Owner == OwnedHarmonyPatch.OwnerId && patch.Method == Callback)) return false;
        var sorted = PatchProcessor.GetSortedPatchMethods(target, snapshot.Prefixes.Select(patch => patch.ToOrderingPatch()).ToArray());
        return sorted.Count != 0 && sorted[0] == Callback;
    }
    [HarmonyPriority(int.MaxValue)]
    private static void Prefix(MethodBase __originalMethod)
    {
        if (!Routes.TryGetValue(__originalMethod, out var owner)) return;
        try
        {
            if (owner.enabled && owner.safeToRefresh())
            {
                // Nested work always checks fresh coverage, including removal of the body adapter.
                if (!owner.hasActiveFrame() && owner.Available) return;
                if (owner.Refresh()) return;
            }
        }
        catch (Exception) { /* Ordinary native conversion remains authoritative. */ }
        Volatile.Write(ref owner.publication, null);
        owner.invalidateCurrent();
    }
}
