namespace Megamod.Features.DrugTolerance.Core;

public readonly record struct DrugToleranceReservation(float NativeWidth, float ColumnOffset, float ColumnWidth);

public static class DrugToleranceLayout
{
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
