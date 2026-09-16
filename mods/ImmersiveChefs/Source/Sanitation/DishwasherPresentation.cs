namespace ImmersiveChefs;

public enum DishwasherPresentationState
{
    Empty,
    OpenLoaded,
    Washing
}

public static class DishwasherPresentationPolicy
{
    public static string CardinalTexturePath(string familyPath, int rotation) =>
        familyPath + "_" + new Verse.Rot4(rotation).ToStringWord().ToLowerInvariant();

    public static bool TryGetContentSlot(DishwasherPresentationState state, int rotation,
        int slotIndex, int containedCount, out DishwasherContentSlot slot)
    {
        slot = default;
        var count = Math.Min(3, containedCount);
        if (state != DishwasherPresentationState.OpenLoaded || rotation is < 0 or > 3 or 2 ||
            slotIndex < 0 || slotIndex >= count)
        {
            return false;
        }

        var north = rotation == 0;
        var centerX = north ? 608f : 512f;
        var centerY = north ? 556f : 652f;
        const float span = 80f;
        var x = centerX + (count == 1 ? 0f : span * (slotIndex / (float)(count - 1) - 0.5f));
        slot = new DishwasherContentSlot(
            (x - (north ? 608f : 512f)) / 256f,
            ((north ? 512f : 608f) - centerY) / 256f,
            48f / 256f);
        return true;
    }

    public static string TexturePath(string selectedFamily, DishwasherPresentationState state) =>
        state == DishwasherPresentationState.Washing ? selectedFamily + "_Closed" : selectedFamily;

    public static DishwasherPresentationState Resolve(bool hasContents, bool hasUnfinishedLoad, bool canProgress)
    {
        if (!hasContents)
        {
            return DishwasherPresentationState.Empty;
        }

        return hasUnfinishedLoad && canProgress
            ? DishwasherPresentationState.Washing
            : DishwasherPresentationState.OpenLoaded;
    }
}

public readonly struct DishwasherContentSlot
{
    public DishwasherContentSlot(float x, float z, float size)
    {
        X = x;
        Z = z;
        Size = size;
    }

    public float X { get; }
    public float Z { get; }
    public float Size { get; }
}
