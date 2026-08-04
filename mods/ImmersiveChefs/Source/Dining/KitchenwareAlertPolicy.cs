namespace ImmersiveChefs;

internal static class KitchenwareAlertPolicy
{
    internal static bool IsRelevantBill(
        WareRequirementMode mode,
        bool playerOwnedStation,
        bool alertEnabledStation,
        bool stationOperational,
        bool billSuspended,
        bool billShouldDoNow,
        bool coveredRecipe,
        bool hasEligibleCook) =>
        mode == WareRequirementMode.Strict &&
        playerOwnedStation &&
        alertEnabledStation &&
        stationOperational &&
        !billSuspended &&
        billShouldDoNow &&
        coveredRecipe &&
        hasEligibleCook;

    internal static bool IsAlertWorthyAbsence(
        bool inventoryScanReliable,
        bool productPresent) =>
        inventoryScanReliable && !productPresent;

    internal static bool IsEligibleAlertWorker(
        bool drafted,
        bool downed,
        bool inMentalState) =>
        !drafted && !downed && !inMentalState;
}
