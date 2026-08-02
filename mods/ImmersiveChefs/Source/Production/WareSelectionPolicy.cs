namespace ImmersiveChefs;

public enum WareUse
{
    Clean,
    Dirty,
    Missing,
    MissingEmergency,
    Exempt
}

public enum WareAdmission
{
    Allowed,
    Blocked
}

public readonly struct WareSelectionResult
{
    public WareSelectionResult(WareUse use, WareAdmission admission)
    {
        Use = use;
        Admission = admission;
    }

    public WareUse Use { get; }
    public WareAdmission Admission { get; }
}

public static class WareSelectionPolicy
{
    public static WareSelectionResult Select(
        WareRequirementMode mode,
        DirtyWareFallback dirtyFallback,
        bool isEmergency,
        bool cleanAvailable,
        bool dirtyAvailable)
    {
        if (mode == WareRequirementMode.Off)
        {
            return new WareSelectionResult(WareUse.Exempt, WareAdmission.Allowed);
        }

        if (cleanAvailable)
        {
            return new WareSelectionResult(WareUse.Clean, WareAdmission.Allowed);
        }

        var dirtyAllowed = dirtyFallback == DirtyWareFallback.Always ||
                           (dirtyFallback == DirtyWareFallback.UrgentOnly && isEmergency);
        if (dirtyAvailable && dirtyAllowed)
        {
            return new WareSelectionResult(WareUse.Dirty, WareAdmission.Allowed);
        }

        if (isEmergency)
        {
            return new WareSelectionResult(WareUse.MissingEmergency, WareAdmission.Allowed);
        }

        return mode == WareRequirementMode.Strict
            ? new WareSelectionResult(WareUse.Missing, WareAdmission.Blocked)
            : new WareSelectionResult(WareUse.Missing, WareAdmission.Allowed);
    }
}
