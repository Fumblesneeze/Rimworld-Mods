using System.Collections.Generic;

namespace GuestBedGizmo.Beds;

internal sealed class BedSelectionConversionPlan
{
    private BedSelectionConversionPlan(int[] swapIndexes, bool applyVanillaOwnerType)
    {
        SwapIndexes = swapIndexes;
        ApplyVanillaOwnerType = applyVanillaOwnerType;
    }

    internal IReadOnlyList<int> SwapIndexes { get; }

    internal bool ApplyVanillaOwnerType { get; }

    internal static bool TryCreate(
        IReadOnlyList<bool> guestStates,
        IReadOnlyList<bool> includedStates,
        IReadOnlyList<bool> replacementAvailableStates,
        BedOwnerChoiceKind choice,
        bool preflightAllowed,
        out BedSelectionConversionPlan plan)
    {
        if (guestStates.Count != includedStates.Count ||
            guestStates.Count != replacementAvailableStates.Count)
        {
            throw new System.ArgumentException("Conversion preflight state counts must match.");
        }

        bool targetGuestState = choice == BedOwnerChoiceKind.Guest;
        if (!preflightAllowed)
        {
            plan = new BedSelectionConversionPlan(
                System.Array.Empty<int>(),
                applyVanillaOwnerType: !targetGuestState);
            return false;
        }

        var swapIndexes = new List<int>();
        for (int index = 0; index < guestStates.Count; index++)
        {
            if (!includedStates[index] || guestStates[index] == targetGuestState)
            {
                continue;
            }

            if (!replacementAvailableStates[index])
            {
                plan = new BedSelectionConversionPlan(
                    System.Array.Empty<int>(),
                    applyVanillaOwnerType: !targetGuestState);
                return false;
            }

            swapIndexes.Add(index);
        }

        plan = new BedSelectionConversionPlan(
            swapIndexes.ToArray(),
            applyVanillaOwnerType: !targetGuestState);
        return true;
    }
}
