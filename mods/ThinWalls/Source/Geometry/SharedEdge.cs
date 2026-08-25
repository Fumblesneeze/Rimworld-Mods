using System;
using Verse;

namespace ThinWalls.Geometry;

public readonly struct SharedEdge : IEquatable<SharedEdge>
{
    public SharedEdge(IntVec3 anchorCell, ThinWallSide positiveSide)
    {
        AnchorCell = anchorCell;
        PositiveSide = positiveSide;
    }

    public IntVec3 AnchorCell { get; }

    public ThinWallSide PositiveSide { get; }

    public bool Equals(SharedEdge other) => AnchorCell == other.AnchorCell && PositiveSide == other.PositiveSide;

    public override bool Equals(object obj) => obj is SharedEdge other && Equals(other);

    public override int GetHashCode() => Gen.HashCombineInt(AnchorCell.GetHashCode(), (int)PositiveSide);
}
