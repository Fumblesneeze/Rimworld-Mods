using Verse;

namespace ThinWalls.Geometry;

public readonly struct OwnedEdge : IEquatable<OwnedEdge>
{
    public OwnedEdge(IntVec3 cell, ThinWallSide side)
    {
        Cell = cell;
        Side = side;
    }

    public IntVec3 Cell { get; }

    public ThinWallSide Side { get; }

    public IntVec3 OppositeCell => Side switch
    {
        ThinWallSide.North => Cell + IntVec3.North,
        ThinWallSide.East => Cell + IntVec3.East,
        ThinWallSide.South => Cell + IntVec3.South,
        ThinWallSide.West => Cell + IntVec3.West,
        _ => throw new System.ArgumentOutOfRangeException(nameof(Side)),
    };

    public SharedEdge Shared
    {
        get
        {
            return Side switch
            {
                ThinWallSide.North => new SharedEdge(Cell, ThinWallSide.North),
                ThinWallSide.East => new SharedEdge(Cell, ThinWallSide.East),
                ThinWallSide.South => new SharedEdge(new IntVec3(Cell.x, Cell.y, Cell.z - 1), ThinWallSide.North),
                ThinWallSide.West => new SharedEdge(new IntVec3(Cell.x - 1, Cell.y, Cell.z), ThinWallSide.East),
                _ => throw new System.ArgumentOutOfRangeException(nameof(Side)),
            };
        }
    }

    public bool Equals(OwnedEdge other) => Cell == other.Cell && Side == other.Side;

    public override bool Equals(object obj) => obj is OwnedEdge other && Equals(other);

    public override int GetHashCode() => Gen.HashCombineInt(Cell.GetHashCode(), (int)Side);
}
