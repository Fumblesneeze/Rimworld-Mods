using NUnit.Framework;
using ThinWalls.Geometry;
using ThinWalls.Pathing;
using Verse;

namespace ThinWalls.Tests;

public sealed class ThinEdgeBridgeReachabilityTests
{
    [Test]
    public void TwoOwnedDoorCrossingsCanBeJoinedByANativeNonlocalLeg()
    {
        var first = new OwnedEdge(new IntVec3(10, 0, 10), ThinWallSide.East);
        var second = new OwnedEdge(new IntVec3(10, 0, 200), ThinWallSide.East);
        IntVec3 start = first.Cell + IntVec3.West;
        IntVec3 end = second.OppositeCell + IntVec3.East;
        bool Native(IntVec3 a, IntVec3 b) => a == b || (a == start && b == first.Cell) ||
            (a == first.OppositeCell && b == second.Cell) || (a == second.OppositeCell && b == end);
        Assert.That(ThinEdgeBridgeReachability.CanReach(start, new[] { second.Shared, first.Shared },
            Native, from => Native(from, end), _ => true), Is.True);
    }

    [Test]
    public void UnrelatedNativeRejectionAndUnusableExitAreNotPromoted()
    {
        var edge = new OwnedEdge(new IntVec3(10, 0, 10), ThinWallSide.East);
        Assert.That(ThinEdgeBridgeReachability.CanReach(edge.Cell, new[] { edge.Shared },
            (_, _) => false, _ => true, _ => true), Is.False);
        Assert.That(ThinEdgeBridgeReachability.CanReach(edge.Cell, new[] { edge.Shared },
            (a, b) => a == b, _ => true, cell => cell != edge.OppositeCell), Is.False);
        Assert.That(ThinEdgeBridgeReachability.CanReach(edge.Cell, Array.Empty<SharedEdge>(),
            (_, _) => true, _ => true, _ => true), Is.False);
    }

    [Test]
    public void APawnAlreadyOnTheFarSideCanStillReachTheFirstCrossingsExit()
    {
        // Regression: a drafted pawn that drifted into the sealed interior stands inside
        // the same region as the destination, so the single door crossing offers no useful
        // exit on the first visit. The bridge must still re-process the crossing from the
        // opened outside cell instead of dropping it after one attempt.
        var edge = new OwnedEdge(new IntVec3(10, 0, 10), ThinWallSide.East);
        IntVec3 start = new IntVec3(8, 0, 10);
        IntVec3 destination = new IntVec3(9, 0, 10);
        bool Inside(IntVec3 cell) => cell != edge.OppositeCell;
        bool Native(IntVec3 a, IntVec3 b) => a == b || Inside(a) == Inside(b);
        Assert.That(ThinEdgeBridgeReachability.CanReach(start, new[] { edge.Shared },
            Native, from => Native(from, destination), _ => true), Is.True);
    }
}
