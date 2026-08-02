using System.Collections.ObjectModel;

namespace ImmersiveChefs;

public sealed class PlateBinding
{
    public PlateBinding(
        string plateDefName,
        string? stuffDefName,
        int quality,
        int hitPoints,
        bool isDirty,
        WashProvenance washProvenance)
    {
        if (string.IsNullOrWhiteSpace(plateDefName))
        {
            throw new ArgumentException("A plate Def name is required.", nameof(plateDefName));
        }

        PlateDefName = plateDefName;
        StuffDefName = string.IsNullOrWhiteSpace(stuffDefName) ? null : stuffDefName;
        Quality = quality;
        HitPoints = Math.Max(1, hitPoints);
        IsDirty = isDirty;
        WashProvenance = washProvenance;
    }

    public string PlateDefName { get; }

    public string? StuffDefName { get; }

    public int Quality { get; }

    public int HitPoints { get; }

    public bool IsDirty { get; }

    public WashProvenance WashProvenance { get; }
}

public sealed class MealStackState
{
    private readonly List<PlateBinding> plateBindings;

    public MealStackState(IEnumerable<PlateBinding>? plateBindings = null)
    {
        this.plateBindings = plateBindings?.ToList() ?? new List<PlateBinding>();
        PlateBindings = new ReadOnlyCollection<PlateBinding>(this.plateBindings);
    }

    public IReadOnlyList<PlateBinding> PlateBindings { get; }

    public MealStackState SplitOff(int count)
    {
        if (count <= 0 || count > plateBindings.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var startIndex = plateBindings.Count - count;
        var transferred = plateBindings.GetRange(startIndex, count);
        plateBindings.RemoveRange(startIndex, count);
        return new MealStackState(transferred);
    }

    public void AppendFrom(MealStackState other, int count)
    {
        if (other is null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        var transferred = other.SplitOff(count);
        plateBindings.AddRange(transferred.PlateBindings);
    }
}
