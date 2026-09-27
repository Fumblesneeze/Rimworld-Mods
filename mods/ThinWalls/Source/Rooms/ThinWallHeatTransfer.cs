using ThinWalls.Geometry;
using Verse;

namespace ThinWalls.Rooms;

public static class ThinWallHeatTransfer
{
    // Temperature-cell energy entering the first room in one native thermal interval.
    // Zero capacity describes a fixed outdoor reservoir, not a solid occupied cell.
    public static float Energy(float firstTemperature, int firstCells, float secondTemperature, int secondCells,
        bool inVacuum = false) =>
        firstCells == 0 && secondCells == 0 ? 0f :
        (secondTemperature - firstTemperature) * 120f * RoomTempTracker.WallEqualizeFactor * 2f *
        (inVacuum ? 0.00005f : 1f);

    internal static void Equalize(Map map, SharedEdge edge)
    {
        IntVec3 firstCell = edge.AnchorCell;
        IntVec3 secondCell = firstCell + (edge.PositiveSide == ThinWallSide.North ? IntVec3.North : IntVec3.East);
        if (!firstCell.InBounds(map) || !secondCell.InBounds(map)) return;
        Room first = firstCell.GetRoom(map), second = secondCell.GetRoom(map);
        if (first == null || second == null || ReferenceEquals(first, second)) return;
        int firstCells = first.UsesOutdoorTemperature ? 0 : first.CellCount;
        int secondCells = second.UsesOutdoorTemperature ? 0 : second.CellCount;
        float energy = Energy(first.Temperature, firstCells, second.Temperature, secondCells, map.Biome.inVacuum);
        // Each finite room has >=1 air cell: 0.0408*(1/Va+1/Vb) <= 0.0816.
        // Every pair update is therefore conservative and cannot cross its equilibrium.
        if (firstCells > 0) first.Temperature += energy / firstCells;
        if (secondCells > 0) second.Temperature -= energy / secondCells;
    }
}
