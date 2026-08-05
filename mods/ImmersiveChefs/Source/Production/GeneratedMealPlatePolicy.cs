namespace ImmersiveChefs;

public enum GeneratedMealOrigin
{
    GenericOrUnknown,
    ExternalPawnInventory,
    TradeStock
}

public readonly struct GeneratedPlateCandidate
{
    public GeneratedPlateCandidate(
        string plateDefName,
        string? stuffDefName,
        KitchenMaterialKind material,
        float marketValue)
    {
        PlateDefName = plateDefName ?? throw new ArgumentNullException(nameof(plateDefName));
        StuffDefName = stuffDefName;
        Material = material;
        MarketValue = marketValue;
    }

    public string PlateDefName { get; }
    public string? StuffDefName { get; }
    public KitchenMaterialKind Material { get; }
    public float MarketValue { get; }
}

public static class GeneratedMealPlatePolicy
{
    public static bool AllowsOrigin(GeneratedMealOrigin origin) =>
        origin is GeneratedMealOrigin.ExternalPawnInventory or GeneratedMealOrigin.TradeStock;

    public static bool AllowsExternalPawnInventory(
        bool humanlike,
        bool hasFaction,
        bool playerFaction) =>
        humanlike && hasFaction && !playerFaction;

    public static int MissingPlateCount(
        bool covered,
        int servings,
        int existingBindings)
    {
        if (!covered || servings <= 0)
        {
            return 0;
        }

        return Math.Max(0, servings - Math.Max(0, existingBindings));
    }

    public static GeneratedPlateCandidate? SelectForService(
        MealComplexity? complexity,
        IEnumerable<GeneratedPlateCandidate> candidates)
    {
        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        return candidates
            .Where(candidate =>
                PlateMaterialEligibilityPolicy.Allows(complexity, candidate.Material) &&
                !float.IsNaN(candidate.MarketValue) &&
                !float.IsInfinity(candidate.MarketValue) &&
                candidate.MarketValue >= 0f)
            .OrderBy(candidate => ServiceMaterialRank(candidate.Material))
            .ThenBy(candidate => candidate.MarketValue)
            .ThenBy(candidate => candidate.PlateDefName, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.StuffDefName ?? string.Empty, StringComparer.Ordinal)
            .Cast<GeneratedPlateCandidate?>()
            .FirstOrDefault();
    }

    private static int ServiceMaterialRank(KitchenMaterialKind material) =>
        material switch
        {
            KitchenMaterialKind.PrimitiveStone or
                KitchenMaterialKind.Adobe or
                KitchenMaterialKind.Wood => 0,
            KitchenMaterialKind.Silver or
                KitchenMaterialKind.Gold or
                KitchenMaterialKind.Ceramic or
                KitchenMaterialKind.Glitterworld => 2,
            _ => 1
        };
}
