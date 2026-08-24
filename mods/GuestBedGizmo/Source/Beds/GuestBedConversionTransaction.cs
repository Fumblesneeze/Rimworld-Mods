using System;
using System.Collections.Generic;
using System.Linq;
using GuestBedGizmo.Compatibility.Hospitality;
using RimWorld;
using Verse;

namespace GuestBedGizmo.Beds;

internal static class GuestBedConversionTransaction
{
    internal static bool TryConvert(
        IList<Building_Bed> beds,
        IReadOnlyList<bool> includedStates,
        HospitalityRuntimeAdapter adapter,
        bool expectedGuestState,
        out string? failure)
    {
        failure = null;
        bool[] guestStates = beds.Select(adapter.IsGuestBed).ToArray();
        bool[] replacementStates = beds
            .Select(bed => adapter.CanSwapTo(bed, expectedGuestState, out _))
            .ToArray();
        BedOwnerChoiceKind choice = expectedGuestState
            ? BedOwnerChoiceKind.Guest
            : BedOwnerChoiceKind.Colonist;
        if (!BedSelectionConversionPlan.TryCreate(
                guestStates,
                includedStates,
                replacementStates,
                choice,
                preflightAllowed: true,
                out BedSelectionConversionPlan plan))
        {
            int unsupportedIndex = Enumerable.Range(0, beds.Count)
                .First(index => includedStates[index] &&
                                guestStates[index] != expectedGuestState &&
                                !replacementStates[index]);
            adapter.CanSwapTo(beds[unsupportedIndex], expectedGuestState, out failure);
            return false;
        }

        if (plan.SwapIndexes.Count == 0)
        {
            return true;
        }

        List<object> selectedObjects = Find.Selector.SelectedObjects.ToList();
        var checkpoints = new Dictionary<int, BedSwapCheckpoint>();
        bool converted = ConversionTransactionExecutor.TryExecute(
            beds,
            plan.SwapIndexes,
            (index, source) =>
            {
                checkpoints[index] = BedSwapCheckpoint.Capture(source, adapter.IsGuestBed(source));
                return SwapAndResolve(source, adapter, expectedGuestState);
            },
            (index, original) => RollBack(checkpoints[index], original, adapter),
            out IReadOnlyDictionary<Building_Bed, Building_Bed> replacements,
            out failure);
        RestoreSelection(selectedObjects, replacements);
        return converted;
    }

    private static Building_Bed SwapAndResolve(
        Building_Bed source,
        HospitalityRuntimeAdapter adapter,
        bool expectedGuestState)
    {
        Map map = source.Map;
        IntVec3 position = source.Position;
        Rot4 rotation = source.Rotation;
        ThingDef stuff = source.Stuff;
        string sourceIdentity = $"{source.def.defName}/{source.ThingID}";

        adapter.Swap(source);

        Building_Bed[] candidates = map.thingGrid.ThingsListAt(position)
            .OfType<Building_Bed>()
            .Where(candidate =>
                candidate.Spawned &&
                candidate.Position == position &&
                candidate.Rotation == rotation &&
                candidate.Stuff == stuff &&
                adapter.IsGuestBed(candidate) == expectedGuestState)
            .ToArray();
        if (candidates.Length != 1)
        {
            throw new InvalidOperationException(
                $"Hospitality swap for {sourceIdentity} at {position} produced {candidates.Length} matching replacements");
        }

        return candidates[0];
    }

    private static Building_Bed RollBack(
        BedSwapCheckpoint checkpoint,
        Building_Bed original,
        HospitalityRuntimeAdapter adapter)
    {
        Building_Bed[] current = checkpoint.Map.thingGrid.ThingsListAt(checkpoint.Position)
            .OfType<Building_Bed>()
            .Where(candidate =>
                candidate.Spawned &&
                candidate.Position == checkpoint.Position &&
                candidate.Rotation == checkpoint.Rotation &&
                candidate.Stuff == checkpoint.Stuff)
            .ToArray();
        if (current.Length != 1)
        {
            throw new InvalidOperationException(
                $"rollback for {checkpoint.SourceIdentity} found {current.Length} current beds");
        }

        Building_Bed candidate = current[0];
        if (adapter.IsGuestBed(candidate) != checkpoint.WasGuestBed)
        {
            if (!adapter.CanSwapTo(candidate, checkpoint.WasGuestBed, out string? reason))
            {
                throw new InvalidOperationException(
                    $"rollback for {checkpoint.SourceIdentity} is unavailable: {reason}");
            }

            candidate = SwapAndResolve(candidate, adapter, checkpoint.WasGuestBed);
        }

        return candidate.Destroyed ? original : candidate;
    }

    private static void RestoreSelection(
        IReadOnlyList<object> selectedObjects,
        IReadOnlyDictionary<Building_Bed, Building_Bed> replacements)
    {
        Find.Selector.ClearSelection();
        foreach (object selected in selectedObjects)
        {
            object replacement = selected is Building_Bed bed &&
                                 replacements.TryGetValue(bed, out Building_Bed converted)
                ? converted
                : selected;
            if (replacement is Thing thing && thing.Destroyed)
            {
                continue;
            }

            Find.Selector.Select(replacement, playSound: false, forceDesignatorDeselect: true);
        }
    }

    private sealed class BedSwapCheckpoint
    {
        private BedSwapCheckpoint(
            Map map,
            IntVec3 position,
            Rot4 rotation,
            ThingDef stuff,
            bool wasGuestBed,
            string sourceIdentity)
        {
            Map = map;
            Position = position;
            Rotation = rotation;
            Stuff = stuff;
            WasGuestBed = wasGuestBed;
            SourceIdentity = sourceIdentity;
        }

        internal Map Map { get; }
        internal IntVec3 Position { get; }
        internal Rot4 Rotation { get; }
        internal ThingDef Stuff { get; }
        internal bool WasGuestBed { get; }
        internal string SourceIdentity { get; }

        internal static BedSwapCheckpoint Capture(Building_Bed source, bool wasGuestBed) => new(
            source.Map,
            source.Position,
            source.Rotation,
            source.Stuff,
            wasGuestBed,
            $"{source.def.defName}/{source.ThingID}");
    }
}

internal static class ConversionTransactionExecutor
{
    internal static bool TryExecute<T>(
        IList<T> items,
        IReadOnlyList<int> indexes,
        Func<int, T, T> convert,
        Func<int, T, T> rollBack,
        out IReadOnlyDictionary<T, T> replacements,
        out string? failure)
        where T : notnull
    {
        var attempted = new List<(int Index, T Original)>();
        var resolved = new Dictionary<T, T>();
        try
        {
            foreach (int index in indexes)
            {
                T original = items[index];
                attempted.Add((index, original));
                T replacement = convert(index, original);
                items[index] = replacement;
                resolved[original] = replacement;
            }

            replacements = resolved;
            failure = null;
            return true;
        }
        catch (Exception exception)
        {
            var rollbackFailures = new List<string>();
            for (int step = attempted.Count - 1; step >= 0; step--)
            {
                (int index, T original) = attempted[step];
                try
                {
                    T restored = rollBack(index, original);
                    items[index] = restored;
                    resolved[original] = restored;
                }
                catch (Exception rollbackException)
                {
                    rollbackFailures.Add(rollbackException.GetBaseException().Message);
                }
            }

            failure = exception.GetBaseException().Message;
            if (rollbackFailures.Count > 0)
            {
                failure += "; rollback failed: " + string.Join("; ", rollbackFailures);
            }

            replacements = resolved;
            return false;
        }
    }
}
