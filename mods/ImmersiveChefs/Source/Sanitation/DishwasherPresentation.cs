namespace ImmersiveChefs;

public enum DishwasherPresentationState
{
    Empty,
    OpenLoaded,
    Washing
}

public static class DishwasherPresentationPolicy
{
    public static bool TryGetContentQuad(DishwasherPresentationState state, int rotation,
        int slotIndex, int containedCount, float graphicWidth, float graphicHeight,
        out DishwasherContentQuad quad)
    {
        quad = default;
        if (!TryGetContentSlot(state, rotation, slotIndex, containedCount, out var slot) ||
            !(graphicWidth > 0f) || !(graphicHeight > 0f) ||
            float.IsInfinity(graphicWidth) || float.IsInfinity(graphicHeight))
            return false;
        var scale = slot.Size / Math.Max(graphicWidth, graphicHeight);
        var width = graphicWidth * scale;
        var height = graphicHeight * scale;
        var left = slot.X - width / 2f;
        var bottom = slot.Z - height / 2f;
        // Selected sprites use 256 pixels/cell. These are the unobstructed chamber
        // interiors, excluding hood, guides, basin rim and the outside staging basket.
        var north = rotation == 0;
        var canvasX = north ? 608f : 512f;
        var canvasY = north ? 512f : 608f;
        var clippedLeft = Math.Max(left, ((north ? 540f : 440f) - canvasX) / 256f);
        var clippedRight = Math.Min(left + width, ((north ? 676f : 584f) - canvasX) / 256f);
        var clippedBottom = Math.Max(bottom, (canvasY - (north ? 572f : 666f)) / 256f);
        var clippedTop = Math.Min(bottom + height, (canvasY - (north ? 516f : 580f)) / 256f);
        if (clippedRight <= clippedLeft || clippedTop <= clippedBottom)
            return false;
        quad = new DishwasherContentQuad((clippedLeft + clippedRight) / 2f,
            (clippedBottom + clippedTop) / 2f, clippedRight - clippedLeft, clippedTop - clippedBottom,
            (clippedLeft - left) / width, (clippedBottom - bottom) / height,
            (clippedRight - left) / width, (clippedTop - bottom) / height);
        return true;
    }

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
        var centerY = north ? 512f : 608f;
        const float span = 80f;
        var x = centerX + (count == 1 ? 0f : span * (slotIndex / (float)(count - 1) - 0.5f));
        slot = new DishwasherContentSlot(
            (x - (north ? 608f : 512f)) / 256f,
            ((north ? 512f : 608f) - centerY) / 256f,
            80f / 256f);
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

public readonly struct DishwasherContentQuad : IEquatable<DishwasherContentQuad>
{
    public DishwasherContentQuad(float x, float z, float width, float height,
        float uMin, float vMin, float uMax, float vMax)
    {
        X = x; Z = z; Width = width; Height = height;
        UMin = uMin; VMin = vMin; UMax = uMax; VMax = vMax;
    }

    public float X { get; }
    public float Z { get; }
    public float Width { get; }
    public float Height { get; }
    public float UMin { get; }
    public float VMin { get; }
    public float UMax { get; }
    public float VMax { get; }
    public bool Equals(DishwasherContentQuad other) => X == other.X && Z == other.Z &&
        Width == other.Width && Height == other.Height && UMin == other.UMin &&
        VMin == other.VMin && UMax == other.UMax && VMax == other.VMax;
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
