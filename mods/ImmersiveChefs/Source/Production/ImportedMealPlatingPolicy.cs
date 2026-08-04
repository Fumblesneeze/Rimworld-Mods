namespace ImmersiveChefs;

public readonly struct ImportedMealPlatingDecision
{
    public ImportedMealPlatingDecision(
        int platesRequired,
        bool shouldQueuePlating,
        bool allowDining,
        bool recordFailedOpportunity,
        bool wareExempt)
    {
        PlatesRequired = platesRequired;
        ShouldQueuePlating = shouldQueuePlating;
        AllowDining = allowDining;
        RecordFailedOpportunity = recordFailedOpportunity;
        WareExempt = wareExempt;
    }

    public int PlatesRequired { get; }
    public bool ShouldQueuePlating { get; }
    public bool AllowDining { get; }
    public bool RecordFailedOpportunity { get; }
    public bool WareExempt { get; }
}

public static class ImportedMealPlatingPolicy
{
    private const float PlatedTieBreakingBonus = 0.1f;

    public static ImportedMealPlatingDecision Evaluate(
        WareRequirementMode mode,
        bool coveredMeal,
        int servingCount,
        int embeddedPlateCount,
        bool platingOpportunityFailed,
        bool emergency,
        bool canPlateNow)
    {
        var missing = coveredMeal
            ? Math.Max(0, servingCount - Math.Max(0, embeddedPlateCount))
            : 0;
        if (!coveredMeal || missing == 0)
        {
            return new ImportedMealPlatingDecision(
                missing,
                shouldQueuePlating: false,
                allowDining: true,
                recordFailedOpportunity: false,
                wareExempt: !coveredMeal);
        }

        if (mode == WareRequirementMode.Off)
        {
            return new ImportedMealPlatingDecision(
                missing,
                shouldQueuePlating: false,
                allowDining: true,
                recordFailedOpportunity: false,
                wareExempt: true);
        }

        var allowDining = emergency ||
                          (mode == WareRequirementMode.Prefer && platingOpportunityFailed);
        return new ImportedMealPlatingDecision(
            missing,
            shouldQueuePlating: canPlateNow,
            allowDining,
            recordFailedOpportunity: !canPlateNow && !emergency && !platingOpportunityFailed,
            wareExempt: false);
    }

    public static float FoodOptimalityBonus(
        bool coveredMeal,
        int servingCount,
        int embeddedPlateCount)
    {
        return coveredMeal && servingCount > 0 && embeddedPlateCount >= servingCount
            ? PlatedTieBreakingBonus
            : 0f;
    }
}
