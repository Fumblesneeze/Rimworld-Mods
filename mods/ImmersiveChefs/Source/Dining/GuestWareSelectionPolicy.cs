namespace ImmersiveChefs;

public readonly struct ServiceWareCandidate<T>
{
    public ServiceWareCandidate(T item, bool isDirty, float serviceScore)
    {
        Item = item;
        IsDirty = isDirty;
        ServiceScore = serviceScore;
    }

    public T Item { get; }
    public bool IsDirty { get; }
    public float ServiceScore { get; }
}

public static class ServiceWareSelectionPolicy
{
    public static T? Select<T>(
        IEnumerable<ServiceWareCandidate<T>> candidates,
        WareRequirementMode mode,
        DirtyWareFallback dirtyFallback,
        bool isEmergency)
        where T : class
    {
        var ranked = candidates
            .OrderBy(candidate => candidate.IsDirty)
            .ThenByDescending(candidate => candidate.ServiceScore)
            .ToList();
        var selection = WareSelectionPolicy.Select(
            mode,
            dirtyFallback,
            isEmergency,
            ranked.Any(candidate => !candidate.IsDirty),
            ranked.Any(candidate => candidate.IsDirty));
        if (selection.Use is not (WareUse.Clean or WareUse.Dirty))
        {
            return null;
        }

        var wantDirty = selection.Use == WareUse.Dirty;
        return ranked.FirstOrDefault(candidate => candidate.IsDirty == wantDirty).Item;
    }
}

public static class GuestWareSelectionPolicy
{
    public static bool MayUsePersonalInventory(
        bool ordinaryNonHostileGuest,
        bool arrivedHospitalityGuest) =>
        ordinaryNonHostileGuest || arrivedHospitalityGuest;

    public static bool ShouldReturnMealPlateToPersonalInventory(
        bool mayUsePersonalInventory,
        bool mealWasInPersonalInventory,
        bool servedByColony) =>
        mayUsePersonalInventory && mealWasInPersonalInventory && !servedByColony;

    public static T? Select<T>(
        IEnumerable<ServiceWareCandidate<T>> colonyCandidates,
        IEnumerable<ServiceWareCandidate<T>> personalCandidates,
        bool mayUsePersonalInventory,
        WareRequirementMode mode,
        DirtyWareFallback dirtyFallback,
        bool isEmergency)
        where T : class
    {
        var colony = ServiceWareSelectionPolicy.Select(
            colonyCandidates,
            mode,
            dirtyFallback,
            isEmergency);
        if (colony is not null || !mayUsePersonalInventory)
        {
            return colony;
        }

        return ServiceWareSelectionPolicy.Select(
            personalCandidates,
            mode,
            dirtyFallback,
            isEmergency);
    }
}
