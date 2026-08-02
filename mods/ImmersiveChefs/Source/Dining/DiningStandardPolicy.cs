namespace ImmersiveChefs;

public enum ServiceMaterialTier
{
    Basic,
    Durable,
    Refined,
    Silver,
    Gold
}

public readonly struct DiningRequirement : IEquatable<DiningRequirement>
{
    public DiningRequirement(
        ServiceMaterialTier material,
        float minimumComfort,
        MealComplexity complexity,
        int minimumQuality)
    {
        Material = material;
        MinimumComfort = minimumComfort;
        Complexity = complexity;
        MinimumQuality = minimumQuality;
    }

    public ServiceMaterialTier Material { get; }
    public float MinimumComfort { get; }
    public MealComplexity Complexity { get; }
    public int MinimumQuality { get; }

    public bool Equals(DiningRequirement other) =>
        Material == other.Material &&
        MinimumComfort.Equals(other.MinimumComfort) &&
        Complexity == other.Complexity &&
        MinimumQuality == other.MinimumQuality;

    public override bool Equals(object? obj) => obj is DiningRequirement other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Material, MinimumComfort, Complexity, MinimumQuality);
}

public static class DiningStandardPolicy
{
    private static readonly DiningRequirement[] ColonyRows =
    {
        new(ServiceMaterialTier.Basic, 0f, MealComplexity.Simple, 0),
        new(ServiceMaterialTier.Basic, 0.1f, MealComplexity.Simple, 20),
        new(ServiceMaterialTier.Durable, 0.2f, MealComplexity.Simple, 35),
        new(ServiceMaterialTier.Durable, 0.3f, MealComplexity.Simple, 35),
        new(ServiceMaterialTier.Refined, 0.45f, MealComplexity.Advanced, 50),
        new(ServiceMaterialTier.Refined, 0.6f, MealComplexity.Advanced, 65)
    };

    public static DiningRequirement ForExpectationOrder(int order) =>
        ColonyRows[Math.Max(0, Math.Min(ColonyRows.Length - 1, order))];

    public static DiningRequirement ForTitleSeniority(int seniority)
    {
        return seniority switch
        {
            <= 0 => new DiningRequirement(ServiceMaterialTier.Basic, 0f, MealComplexity.Simple, 0),
            <= 2 => new DiningRequirement(ServiceMaterialTier.Refined, 0.4f, MealComplexity.Advanced, 50),
            <= 4 => new DiningRequirement(ServiceMaterialTier.Silver, 0.55f, MealComplexity.Advanced, 65),
            5 => new DiningRequirement(ServiceMaterialTier.Silver, 0.7f, MealComplexity.Elaborate, 65),
            _ => new DiningRequirement(ServiceMaterialTier.Gold, 0.8f, MealComplexity.Elaborate, 80)
        };
    }

    public static DiningRequirement Combine(DiningRequirement first, DiningRequirement second) =>
        new(
            (ServiceMaterialTier)Math.Max((int)first.Material, (int)second.Material),
            Math.Max(first.MinimumComfort, second.MinimumComfort),
            (MealComplexity)Math.Max((int)first.Complexity, (int)second.Complexity),
            Math.Max(first.MinimumQuality, second.MinimumQuality));
}
