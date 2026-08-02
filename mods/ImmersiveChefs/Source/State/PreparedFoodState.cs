using System.Collections.ObjectModel;

namespace ImmersiveChefs;

[Flags]
public enum DietaryFlags
{
    None = 0,
    Plant = 1,
    Animal = 2,
    HumanMeat = 4,
    InsectMeat = 8,
    VegetarianCompatible = 16
}

public sealed class IngredientContribution
{
    public IngredientContribution(string defName, float nutrition, int sourceCount, int craftsmanshipScore = 50)
    {
        if (string.IsNullOrWhiteSpace(defName))
        {
            throw new ArgumentException("An ingredient Def name is required.", nameof(defName));
        }

        DefName = defName;
        Nutrition = Math.Max(0f, nutrition);
        SourceCount = Math.Max(1, sourceCount);
        CraftsmanshipScore = Math.Max(0, Math.Min(100, craftsmanshipScore));
    }

    public string DefName { get; }

    public float Nutrition { get; }

    public int SourceCount { get; }

    public int CraftsmanshipScore { get; }
}

public sealed class PreparedFoodSnapshot
{
    internal PreparedFoodSnapshot(
        IReadOnlyList<IngredientContribution> contributions,
        int preparationQuality,
        string? preparerThingId,
        DietaryFlags dietaryFlags,
        bool exactSourcesHidden,
        float ingredientPoisonChance)
    {
        Contributions = contributions;
        PreparationQuality = preparationQuality;
        PreparerThingId = preparerThingId;
        DietaryFlags = dietaryFlags;
        ExactSourcesHidden = exactSourcesHidden;
        IngredientPoisonChance = ingredientPoisonChance;
    }

    internal IReadOnlyList<IngredientContribution> Contributions { get; }

    internal int PreparationQuality { get; }

    internal string? PreparerThingId { get; }

    internal DietaryFlags DietaryFlags { get; }

    internal bool ExactSourcesHidden { get; }

    internal float IngredientPoisonChance { get; }
}

public sealed class PreparedFoodState
{
    private readonly ReadOnlyCollection<IngredientContribution> contributions;

    public PreparedFoodState(
        IEnumerable<IngredientContribution> contributions,
        int preparationQuality,
        string? preparerThingId,
        DietaryFlags dietaryFlags,
        bool exactSourcesHidden,
        float ingredientPoisonChance)
    {
        if (contributions is null)
        {
            throw new ArgumentNullException(nameof(contributions));
        }

        this.contributions = new ReadOnlyCollection<IngredientContribution>(
            contributions.Select(CopyContribution).ToList());
        PreparationQuality = Math.Max(0, Math.Min(100, preparationQuality));
        PreparerThingId = string.IsNullOrWhiteSpace(preparerThingId) ? null : preparerThingId;
        DietaryFlags = dietaryFlags;
        ExactSourcesHidden = exactSourcesHidden;
        IngredientPoisonChance = Math.Max(0f, ingredientPoisonChance);
    }

    public IReadOnlyList<IngredientContribution> Contributions => contributions;

    public float TotalNutrition => contributions.Sum(item => item.Nutrition);

    public int PreparationQuality { get; }

    public string? PreparerThingId { get; }

    public DietaryFlags DietaryFlags { get; }

    public bool ExactSourcesHidden { get; }

    public float IngredientPoisonChance { get; }

    public PreparedFoodSnapshot Capture()
    {
        return new PreparedFoodSnapshot(
            contributions.Select(CopyContribution).ToList().AsReadOnly(),
            PreparationQuality,
            PreparerThingId,
            DietaryFlags,
            ExactSourcesHidden,
            IngredientPoisonChance);
    }

    public static PreparedFoodState Restore(PreparedFoodSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return new PreparedFoodState(
            snapshot.Contributions,
            snapshot.PreparationQuality,
            snapshot.PreparerThingId,
            snapshot.DietaryFlags,
            snapshot.ExactSourcesHidden,
            snapshot.IngredientPoisonChance);
    }

    private static IngredientContribution CopyContribution(IngredientContribution contribution)
    {
        return new IngredientContribution(
            contribution.DefName,
            contribution.Nutrition,
            contribution.SourceCount,
            contribution.CraftsmanshipScore);
    }
}
