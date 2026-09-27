using System;

namespace ThinWalls.Rendering;

/// <summary>Stable ownership of Thin rays that participate in a Core-wall vertex.</summary>
public static class HybridRegularContactOwnership
{
    public static HybridWallRayMask RaysParticipatingIn(
        HybridWallQuadrant quadrant,
        HybridWallQuadrant occupiedQuadrants,
        HybridWallRayMask actualThinRays)
    {
        if (!occupiedQuadrants.HasFlag(quadrant)) return HybridWallRayMask.None;
        return HybridWallRegularApertureRecipe.EligibleRays(quadrant, actualThinRays);
    }

    public static HybridWallRayMask RaysOwnedBy(
        HybridWallQuadrant quadrant,
        HybridWallQuadrant occupiedQuadrants,
        HybridWallRayMask actualThinRays)
    {
        HybridWallRayMask owned = HybridWallRayMask.None;
        foreach (HybridWallRayMask ray in CardinalRays)
        {
            if (actualThinRays.HasFlag(ray) && OwnerQuadrant(ray, occupiedQuadrants) == quadrant)
                owned |= ray;
        }
        return owned;
    }

    public static HybridWallRayMask SideTRaysParticipatingIn(
        HybridWallQuadrant quadrant,
        HybridWallQuadrant occupiedQuadrants,
        HybridWallRayMask actualThinRays)
    {
        HybridWallRayMask participating = RaysParticipatingIn(
            quadrant, occupiedQuadrants, actualThinRays);
        HybridWallRayMask result = HybridWallRayMask.None;
        foreach (HybridWallRayMask ray in CardinalRays)
            if (participating.HasFlag(ray) && HasBothSideTQuadrants(ray, occupiedQuadrants)) result |= ray;
        return result;
    }

    public static HybridWallRayMask AllRegularOwnedRays(
        HybridWallQuadrant occupiedQuadrants,
        HybridWallRayMask actualThinRays)
    {
        HybridWallRayMask result = HybridWallRayMask.None;
        foreach (HybridWallQuadrant quadrant in new[]
                 {
                     HybridWallQuadrant.NorthEast,
                     HybridWallQuadrant.NorthWest,
                     HybridWallQuadrant.SouthEast,
                     HybridWallQuadrant.SouthWest,
                 })
            if (occupiedQuadrants.HasFlag(quadrant))
                result |= RaysOwnedBy(quadrant, occupiedQuadrants, actualThinRays);
        return result;
    }

    private static readonly HybridWallRayMask[] CardinalRays =
    {
        HybridWallRayMask.North,
        HybridWallRayMask.East,
        HybridWallRayMask.South,
        HybridWallRayMask.West,
    };

    private static HybridWallQuadrant OwnerQuadrant(
        HybridWallRayMask ray,
        HybridWallQuadrant occupiedQuadrants)
    {
        HybridWallQuadrant[] preferences = ray switch
        {
            HybridWallRayMask.West => new[] { HybridWallQuadrant.NorthEast, HybridWallQuadrant.SouthEast },
            HybridWallRayMask.East => new[] { HybridWallQuadrant.NorthWest, HybridWallQuadrant.SouthWest },
            HybridWallRayMask.South => new[] { HybridWallQuadrant.NorthEast, HybridWallQuadrant.NorthWest },
            HybridWallRayMask.North => new[] { HybridWallQuadrant.SouthEast, HybridWallQuadrant.SouthWest },
            _ => Array.Empty<HybridWallQuadrant>(),
        };
        foreach (HybridWallQuadrant candidate in preferences)
            if (occupiedQuadrants.HasFlag(candidate)) return candidate;
        return HybridWallQuadrant.None;
    }

    private static bool HasBothSideTQuadrants(
        HybridWallRayMask ray,
        HybridWallQuadrant occupiedQuadrants)
    {
        HybridWallQuadrant required = ray switch
        {
            HybridWallRayMask.South => HybridWallQuadrant.NorthWest | HybridWallQuadrant.NorthEast,
            HybridWallRayMask.North => HybridWallQuadrant.SouthWest | HybridWallQuadrant.SouthEast,
            HybridWallRayMask.West => HybridWallQuadrant.NorthEast | HybridWallQuadrant.SouthEast,
            HybridWallRayMask.East => HybridWallQuadrant.NorthWest | HybridWallQuadrant.SouthWest,
            _ => HybridWallQuadrant.None,
        };
        return required != HybridWallQuadrant.None && (occupiedQuadrants & required) == required;
    }
}
