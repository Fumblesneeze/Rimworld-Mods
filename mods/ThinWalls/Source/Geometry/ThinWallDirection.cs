namespace ThinWalls.Geometry;

public static class ThinWallDirection
{
    public static ThinWallSide Rotate(ThinWallSide side, bool clockwise)
    {
        int delta = clockwise ? 1 : -1;
        return (ThinWallSide)(((int)side + delta + 4) % 4);
    }

    public static ThinWallSide SideForDrag(
        int deltaX,
        int deltaZ,
        ThinWallSide singleCellSide) =>
        RightHandSide(deltaX, deltaZ, singleCellSide);

    public static ThinWallSide RightHandSide(int deltaX, int deltaZ, ThinWallSide retainedSide)
    {
        int absoluteX = System.Math.Abs(deltaX);
        int absoluteZ = System.Math.Abs(deltaZ);
        if (absoluteX == 0 && absoluteZ == 0)
        {
            return retainedSide;
        }

        if (absoluteX >= absoluteZ)
        {
            return deltaX >= 0 ? ThinWallSide.South : ThinWallSide.North;
        }

        return deltaZ >= 0 ? ThinWallSide.East : ThinWallSide.West;
    }
}
