namespace ImmersiveChefs;

public static class DiningPawnPolicy
{
    public static bool AppliesDiningConsequences(bool humanlike) => humanlike;

    public static bool AppliesPlateEatingSpeed(bool humanlike) =>
        AppliesDiningConsequences(humanlike);
}

public static class CaravanDiningPolicy
{
    public static bool WasIngested(float nutritionIngested) => nutritionIngested > 0f;

    public static T? SelectWare<T>(
        IEnumerable<ServiceWareCandidate<T>> candidates,
        WareRequirementMode mode,
        DirtyWareFallback dirtyFallback,
        bool isEmergency)
        where T : class
        => ServiceWareSelectionPolicy.Select(candidates, mode, dirtyFallback, isEmergency);
}
