namespace Megamod.Features.DrugTolerance.Core;

public readonly record struct DrugToleranceReservation(float NativeWidth, float ColumnOffset, float ColumnWidth);

public static class DrugToleranceLayout
{
    public static bool FitsViewport(float x, float y, float width, float height, float viewportWidth, float viewportHeight) =>
        float.IsFinite(x) && float.IsFinite(y) && float.IsFinite(width) && float.IsFinite(height) &&
        float.IsFinite(viewportWidth) && float.IsFinite(viewportHeight) &&
        x >= 0 && y >= 0 && width > 0 && height > 0 && viewportWidth > 0 && viewportHeight > 0 &&
        width <= viewportWidth - x && height <= viewportHeight - y;

    public static bool TryReserve(float width, float measuredColumn, bool header, out DrugToleranceReservation reservation)
    {
        reservation = default;
        if (!float.IsFinite(width) || !float.IsFinite(measuredColumn) || measuredColumn < 0) return false;
        float column = System.Math.Max(120, measuredColumn);
        float native = width - column, usable = native - (header ? 16 : 0);
        if (usable < 600 || usable > 1300) return false;
        reservation = new(native, usable, column);
        return true;
    }
}
