namespace ImmersiveChefs;

public readonly struct DiningWareReservation
{
    public DiningWareReservation(int maxPawns, int stackCount)
    {
        MaxPawns = maxPawns;
        StackCount = stackCount;
    }

    public int MaxPawns { get; }
    public int StackCount { get; }
}

public static class DiningCutleryReservationPolicy
{
    public static DiningWareReservation ForStackLimit(int stackLimit) =>
        new DiningWareReservation(Math.Max(1, stackLimit), 1);
}

public static class DiningCutlerySearchPolicy
{
    public static float MapScore(float serviceScore, int distanceSquaredFromDiningPlace) =>
        serviceScore - Math.Max(0, distanceSquaredFromDiningPlace) * 0.01f;
}

public static class DiningCutleryToilOrder
{
    public static IEnumerable<T> InsertBefore<T>(
        IEnumerable<T> nativeToils,
        Func<T, bool> isInsertionPoint,
        IEnumerable<T> acquisitionToils,
        Action? onMissing = null)
    {
        var inserted = false;
        foreach (var toil in nativeToils)
        {
            if (!inserted && isInsertionPoint(toil))
            {
                foreach (var acquisition in acquisitionToils)
                {
                    yield return acquisition;
                }

                inserted = true;
            }

            yield return toil;
        }

        if (!inserted)
        {
            onMissing?.Invoke();
        }
    }
}

public static class DiningCutleryDeferredSelectionPolicy
{
    public static bool ShouldSelect(
        bool hasSelectedCutlery,
        bool hasCarriedCutlery,
        bool hasServingPawn) =>
        !hasSelectedCutlery && !hasCarriedCutlery && !hasServingPawn;
}

public static class DiningCutleryPickupPolicy
{
    public static bool CanTakeSelectedMapCutlery(
        bool isSpawned,
        bool isReservedBySession,
        bool isStillAllowed) =>
        isSpawned && isReservedBySession && isStillAllowed;
}

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

public static class SelfDiningCutlerySelectionPolicy
{
    public static T? Select<T>(
        IEnumerable<ServiceWareCandidate<T>> mapCandidates,
        IEnumerable<ServiceWareCandidate<T>> inventoryCandidates,
        bool preferColonyService,
        WareRequirementMode mode,
        DirtyWareFallback dirtyFallback,
        bool isEmergency)
        where T : class
    {
        var map = mapCandidates.ToList();
        var inventory = inventoryCandidates.ToList();
        if (preferColonyService)
        {
            return GuestWareSelectionPolicy.Select(
                map,
                inventory,
                mayUsePersonalInventory: true,
                mode,
                dirtyFallback,
                isEmergency);
        }

        var all = inventory.Concat(map).ToList();
        var selection = WareSelectionPolicy.Select(
            mode,
            dirtyFallback,
            isEmergency,
            all.Any(candidate => !candidate.IsDirty),
            all.Any(candidate => candidate.IsDirty));
        if (selection.Use is not (WareUse.Clean or WareUse.Dirty))
        {
            return null;
        }

        var wantDirty = selection.Use == WareUse.Dirty;
        return inventory
                   .Where(candidate => candidate.IsDirty == wantDirty)
                   .OrderByDescending(candidate => candidate.ServiceScore)
                   .Select(candidate => candidate.Item)
                   .FirstOrDefault() ??
               map
                   .Where(candidate => candidate.IsDirty == wantDirty)
                   .OrderByDescending(candidate => candidate.ServiceScore)
                   .Select(candidate => candidate.Item)
                   .FirstOrDefault();
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
