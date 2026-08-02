namespace ImmersiveChefs;

public readonly struct CaravanWareCandidate<T>
{
    public CaravanWareCandidate(T item, bool isDirty, float serviceScore)
    {
        Item = item;
        IsDirty = isDirty;
        ServiceScore = serviceScore;
    }

    public T Item { get; }
    public bool IsDirty { get; }
    public float ServiceScore { get; }
}

public static class CaravanDiningPolicy
{
    public static bool WasIngested(float nutritionIngested) => nutritionIngested > 0f;

    public static T? SelectWare<T>(
        IEnumerable<CaravanWareCandidate<T>> candidates,
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
        if (selection.Use != WareUse.Clean && selection.Use != WareUse.Dirty)
        {
            return null;
        }

        var wantDirty = selection.Use == WareUse.Dirty;
        return ranked.FirstOrDefault(candidate => candidate.IsDirty == wantDirty).Item;
    }
}
