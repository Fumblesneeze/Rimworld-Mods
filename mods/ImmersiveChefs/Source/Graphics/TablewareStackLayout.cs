namespace ImmersiveChefs;

public readonly struct TablewareDrawOffset
{
    public TablewareDrawOffset(float x, float z) { X = x; Z = z; }
    public float X { get; }
    public float Z { get; }
}

public static class TablewareStackLayout
{
    public static IReadOnlyList<TablewareDrawOffset> For(KitchenwareProduct product, int stackCount)
    {
        var count = Math.Min(5, Math.Max(0, stackCount));
        var offsets = new TablewareDrawOffset[count];
        for (var i = 0; i < count; i++)
        {
            var centeredIndex = i - (count - 1) / 2f;
            offsets[i] = product == KitchenwareProduct.Plate
                ? new TablewareDrawOffset(0, centeredIndex * .04f)
                : new TablewareDrawOffset(centeredIndex * .10f, 0);
        }
        return offsets;
    }
}
