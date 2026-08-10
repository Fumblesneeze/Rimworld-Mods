namespace ImmersiveChefs;

public static class DishwashingWorkPolicy
{
    public const int PlateEquivalentTicks = 250;

    public const float MinimumPlateEquivalentsPerPhysicalItem = 0.5f;

    public static int HandwashingDurationTicks(
        float plateEquivalentsPerItem,
        int itemCount,
        float workScale)
    {
        if (plateEquivalentsPerItem <= 0f || itemCount <= 0 || workScale <= 0f)
        {
            return 1;
        }

        return Math.Max(1, (int)Math.Round(
            PlateEquivalentTicks *
            Math.Max(MinimumPlateEquivalentsPerPhysicalItem, plateEquivalentsPerItem) *
            itemCount *
            workScale,
            MidpointRounding.AwayFromZero));
    }
}
