namespace ImmersiveChefs;

public readonly struct DirtyCookwareActionState
{
    public DirtyCookwareActionState(
        bool requirementsActive,
        bool billOtherwiseRunnable,
        bool cookEligible,
        bool cleanPlatesAvailable,
        bool cleanCookwareAvailable,
        bool dirtyCookwareAvailable,
        bool washingDestinationAvailable)
    {
        RequirementsActive = requirementsActive;
        BillOtherwiseRunnable = billOtherwiseRunnable;
        CookEligible = cookEligible;
        CleanPlatesAvailable = cleanPlatesAvailable;
        CleanCookwareAvailable = cleanCookwareAvailable;
        DirtyCookwareAvailable = dirtyCookwareAvailable;
        WashingDestinationAvailable = washingDestinationAvailable;
    }

    public bool RequirementsActive { get; }
    public bool BillOtherwiseRunnable { get; }
    public bool CookEligible { get; }
    public bool CleanPlatesAvailable { get; }
    public bool CleanCookwareAvailable { get; }
    public bool DirtyCookwareAvailable { get; }
    public bool WashingDestinationAvailable { get; }
}

public static class DirtyCookwareActionPolicy
{
    public static bool ShouldOfferOneJobOverride(DirtyCookwareActionState state) =>
        IsExactDirtyCookwareBlocker(state);

    public static bool ShouldRunPrerequisiteWash(DirtyCookwareActionState state) =>
        IsExactDirtyCookwareBlocker(state) && state.WashingDestinationAvailable;

    private static bool IsExactDirtyCookwareBlocker(DirtyCookwareActionState state) =>
        state.RequirementsActive &&
        state.BillOtherwiseRunnable &&
        state.CookEligible &&
        state.CleanPlatesAvailable &&
        !state.CleanCookwareAvailable &&
        state.DirtyCookwareAvailable;
}
