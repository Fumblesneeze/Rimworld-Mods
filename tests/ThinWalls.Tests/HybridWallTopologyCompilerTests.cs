using NUnit.Framework;
using ThinWalls.Rendering;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class HybridWallTopologyCompilerTests
{
    private const float ThinWidth = 7f / 60f;
    private const float OrdinaryWidth = 33f / 60f;

    [Test]
    public void EveryDirectionalStateCompilesToOneConnectedNonOverlappingUnion()
    {
        int compiled = 0;
        foreach (HybridWallArmKind north in ArmKinds())
        foreach (HybridWallArmKind east in ArmKinds())
        foreach (HybridWallArmKind south in ArmKinds())
        foreach (HybridWallArmKind west in ArmKinds())
        {
            var key = new HybridWallTopologyKey(north, east, south, west);
            HybridWallTopologyPlan plan = HybridWallTopologyCompiler.Compile(key);

            Assert.Multiple(() =>
            {
                Assert.That(plan.Key, Is.EqualTo(key));
                Assert.That(plan.IsConnected, Is.True, $"{key} must compile as one union");
                Assert.That(plan.HasOverlappingSurfaceCells, Is.False, $"{key} may not contain coplanar overlap");
                Assert.That(plan.InternalContours, Is.Empty, $"{key} may not retain endpoint outlines");
                Assert.That(plan.OccupiedDirections, Is.EquivalentTo(key.OccupiedDirections));
                Assert.That(plan.ExteriorContour, key.IsEmpty ? Is.Empty : Is.Not.Empty);
            });
            compiled++;
        }

        Assert.That(compiled, Is.EqualTo(81));
    }

    [Test]
    public void EquivalentConstructionOrderProducesTheSamePlan()
    {
        HybridWallTopologyKey[] permutations =
        {
            HybridWallTopologyKey.FromArms(
                (HybridWallDirection.North, HybridWallArmKind.Ordinary),
                (HybridWallDirection.West, HybridWallArmKind.Thin),
                (HybridWallDirection.East, HybridWallArmKind.Ordinary)),
            HybridWallTopologyKey.FromArms(
                (HybridWallDirection.East, HybridWallArmKind.Ordinary),
                (HybridWallDirection.North, HybridWallArmKind.Ordinary),
                (HybridWallDirection.West, HybridWallArmKind.Thin)),
            HybridWallTopologyKey.FromArms(
                (HybridWallDirection.West, HybridWallArmKind.Thin),
                (HybridWallDirection.East, HybridWallArmKind.Ordinary),
                (HybridWallDirection.North, HybridWallArmKind.Ordinary)),
        };

        string[] signatures = permutations
            .Select(key => HybridWallTopologyCompiler.Compile(key).StableSignature)
            .ToArray();

        Assert.That(signatures.Distinct().ToArray(), Has.Length.EqualTo(1));
    }

    [Test]
    public void OrdinaryDominatesAThinArmOnTheSameRay()
    {
        HybridWallTopologyKey key = HybridWallTopologyKey.FromArms(
            (HybridWallDirection.North, HybridWallArmKind.Thin),
            (HybridWallDirection.North, HybridWallArmKind.Ordinary));

        HybridWallTopologyPlan plan = HybridWallTopologyCompiler.Compile(key);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Key.North, Is.EqualTo(HybridWallArmKind.Ordinary));
            Assert.That(plan.WidthFor(HybridWallDirection.North), Is.EqualTo(OrdinaryWidth));
            Assert.That(plan.OccupiedDirections, Is.EqualTo(new[] { HybridWallDirection.North }));
        });
    }

    [Test]
    public void ThinAndOrdinaryWidthsUseTheMeasuredCoreTopBands()
    {
        HybridWallTopologyPlan plan = HybridWallTopologyCompiler.Compile(
            new HybridWallTopologyKey(
                HybridWallArmKind.Thin,
                HybridWallArmKind.Ordinary,
                HybridWallArmKind.None,
                HybridWallArmKind.None));

        Assert.Multiple(() =>
        {
            Assert.That(plan.WidthFor(HybridWallDirection.North), Is.EqualTo(ThinWidth));
            Assert.That(plan.WidthFor(HybridWallDirection.East), Is.EqualTo(OrdinaryWidth));
            Assert.That(plan.WidthFor(HybridWallDirection.South), Is.Zero);
            Assert.That(plan.WidthFor(HybridWallDirection.West), Is.Zero);
        });
    }

    [Test]
    public void MixedTUsesSquareUnionWithoutDiagonalOrTransitionComponents()
    {
        HybridWallTopologyPlan plan = HybridWallTopologyCompiler.Compile(
            new HybridWallTopologyKey(
                HybridWallArmKind.Ordinary,
                HybridWallArmKind.Ordinary,
                HybridWallArmKind.None,
                HybridWallArmKind.Thin));

        Assert.Multiple(() =>
        {
            Assert.That(plan.OccupiedDirections, Is.EquivalentTo(new[]
            {
                HybridWallDirection.North,
                HybridWallDirection.East,
                HybridWallDirection.West,
            }));
            Assert.That(plan.SurfaceCells, Is.All.Matches<HybridWallSurfaceCell>(cell =>
                cell.MinX < cell.MaxX && cell.MinZ < cell.MaxZ));
            Assert.That(plan.SurfaceCells.SelectMany(cell => cell.Edges),
                Is.All.Matches<HybridWallLine>(line => line.IsAxisAligned));
            Assert.That(plan.ComponentKinds, Is.EqualTo(new[] { HybridWallComponentKind.UnionSurface }));
        });
    }

    [Test]
    public void EmptyStateHasNoGeometryButRemainsAValidPlan()
    {
        HybridWallTopologyPlan plan = HybridWallTopologyCompiler.Compile(HybridWallTopologyKey.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(plan.IsConnected, Is.True);
            Assert.That(plan.SurfaceCells, Is.Empty);
            Assert.That(plan.ExteriorContour, Is.Empty);
            Assert.That(plan.OccupiedDirections, Is.Empty);
        });
    }

    [TestCase(HybridWallQuadrant.NorthEast, HybridWallRayMask.West)]
    [TestCase(HybridWallQuadrant.SouthWest, HybridWallRayMask.North)]
    [TestCase(HybridWallQuadrant.NorthEast | HybridWallQuadrant.SouthEast, HybridWallRayMask.West)]
    public void OrdinaryQuadrantsRemainConcreteSurfacesInsteadOfInventedOppositeRays(
        HybridWallQuadrant quadrants,
        HybridWallRayMask thinRays)
    {
        HybridWallVertexTopology topology = HybridWallVertexTopology.FromOccupancy(quadrants, thinRays);
        HybridWallTopologyPlan plan = HybridWallTopologyCompiler.Compile(topology);

        Assert.Multiple(() =>
        {
            Assert.That(topology.OrdinaryQuadrants, Is.EqualTo(quadrants));
            Assert.That(topology.ThinRays, Is.EqualTo(thinRays));
            Assert.That(topology.Arms.OccupiedDirections, Is.EquivalentTo(Directions(thinRays)),
                "wall quadrants may not manufacture half-cell rays toward a wall center");
            Assert.That(plan.OrdinaryQuadrants, Is.EqualTo(quadrants));
            Assert.That(plan.IsConnected, Is.True);
            foreach (HybridWallQuadrant quadrant in Quadrants(quadrants))
            {
                Assert.That(plan.SurfaceCells.Any(cell => CellCenterIsInQuadrant(cell, quadrant)), Is.True,
                    $"{quadrant} must contribute its concrete quarter-cell footprint");
            }
        });
    }

    [Test]
    public void OrdinaryQuadrantUnionsWithCoincidentThinRayWithoutChangingRayIdentity()
    {
        HybridWallVertexTopology topology = HybridWallVertexTopology.FromOccupancy(
            HybridWallQuadrant.NorthEast,
            HybridWallRayMask.North | HybridWallRayMask.West);
        HybridWallTopologyPlan plan = HybridWallTopologyCompiler.Compile(topology);

        Assert.Multiple(() =>
        {
            Assert.That(topology.Arms.North, Is.EqualTo(HybridWallArmKind.Thin));
            Assert.That(topology.Arms.West, Is.EqualTo(HybridWallArmKind.Thin));
            Assert.That(plan.OrdinaryQuadrants, Is.EqualTo(HybridWallQuadrant.NorthEast));
            Assert.That(plan.IsConnected, Is.True);
            Assert.That(plan.InternalContours, Is.Empty);
            Assert.That(plan.SurfaceCells.Any(cell =>
                CellContains(cell, 0.25f, 0.25f)), Is.True, "the concrete wall quadrant must remain full width");
            Assert.That(plan.SurfaceCells.Any(cell =>
                CellContains(cell, -0.25f, 0f)), Is.True, "the west Thin ray must reach the wall quadrant");
            Assert.That(plan.SurfaceCells.Any(cell =>
                CellContains(cell, 0.25f, -0.25f)), Is.False,
                "the compiler may not invent a south-east wall quadrant or center-seeking connector");
        });
    }

    [Test]
    public void AllQuadrantAndThinRayMasksCompileWithoutOverlapOrInternalContour()
    {
        int compiled = 0;
        for (int quadrantBits = 0; quadrantBits < 16; quadrantBits++)
        for (int rayBits = 0; rayBits < 16; rayBits++)
        {
            HybridWallVertexTopology topology = HybridWallVertexTopology.FromOccupancy(
                (HybridWallQuadrant)quadrantBits,
                (HybridWallRayMask)rayBits);
            HybridWallTopologyPlan plan = HybridWallTopologyCompiler.Compile(topology);

            Assert.Multiple(() =>
            {
                Assert.That(plan.OrdinaryQuadrants, Is.EqualTo((HybridWallQuadrant)quadrantBits));
                Assert.That(plan.HasOverlappingSurfaceCells, Is.False);
                Assert.That(plan.InternalContours, Is.Empty);
                if (rayBits != 0)
                {
                    Assert.That(plan.IsConnected, Is.True,
                        $"a Thin ray through the common corner must connect quadrants={(HybridWallQuadrant)quadrantBits}; rays={(HybridWallRayMask)rayBits}");
                }
            });
            compiled++;
        }

        Assert.That(compiled, Is.EqualTo(256));
    }

    [Test]
    public void DoubledOwnersWidenOnlyTheirThinArmAndRemainBelowOrdinaryWidth()
    {
        var key = new HybridWallTopologyKey(
            HybridWallArmKind.Thin,
            HybridWallArmKind.Ordinary,
            HybridWallArmKind.Thin,
            HybridWallArmKind.None);

        HybridWallTopologyPlan plan = HybridWallTopologyCompiler.Compile(
            key,
            HybridWallRayMask.North | HybridWallRayMask.East);

        Assert.Multiple(() =>
        {
            Assert.That(plan.DoubledRays, Is.EqualTo(HybridWallRayMask.North | HybridWallRayMask.East));
            Assert.That(plan.WidthFor(HybridWallDirection.North), Is.EqualTo(14f / 60f));
            Assert.That(plan.WidthFor(HybridWallDirection.East), Is.EqualTo(OrdinaryWidth),
                "ordinary width dominates even if the coincident ray is flagged doubled");
            Assert.That(plan.WidthFor(HybridWallDirection.South), Is.EqualTo(ThinWidth));
            Assert.That(plan.IsConnected, Is.True);
            Assert.That(plan.InternalContours, Is.Empty);
        });
    }

    private static IEnumerable<HybridWallArmKind> ArmKinds()
    {
        yield return HybridWallArmKind.None;
        yield return HybridWallArmKind.Thin;
        yield return HybridWallArmKind.Ordinary;
    }

    private static IEnumerable<HybridWallDirection> Directions(HybridWallRayMask mask)
    {
        if (mask.HasFlag(HybridWallRayMask.North)) yield return HybridWallDirection.North;
        if (mask.HasFlag(HybridWallRayMask.East)) yield return HybridWallDirection.East;
        if (mask.HasFlag(HybridWallRayMask.South)) yield return HybridWallDirection.South;
        if (mask.HasFlag(HybridWallRayMask.West)) yield return HybridWallDirection.West;
    }

    private static IEnumerable<HybridWallQuadrant> Quadrants(HybridWallQuadrant mask)
    {
        foreach (HybridWallQuadrant quadrant in new[]
                 {
                     HybridWallQuadrant.NorthEast,
                     HybridWallQuadrant.NorthWest,
                     HybridWallQuadrant.SouthEast,
                     HybridWallQuadrant.SouthWest,
                 })
        {
            if (mask.HasFlag(quadrant)) yield return quadrant;
        }
    }

    private static bool CellCenterIsInQuadrant(HybridWallSurfaceCell cell, HybridWallQuadrant quadrant)
    {
        float x = (cell.MinX + cell.MaxX) * 0.5f;
        float z = (cell.MinZ + cell.MaxZ) * 0.5f;
        return quadrant switch
        {
            HybridWallQuadrant.NorthEast => x > 0f && z > 0f,
            HybridWallQuadrant.NorthWest => x < 0f && z > 0f,
            HybridWallQuadrant.SouthEast => x > 0f && z < 0f,
            HybridWallQuadrant.SouthWest => x < 0f && z < 0f,
            _ => false,
        };
    }

    private static bool CellContains(HybridWallSurfaceCell cell, float x, float z) =>
        cell.MinX <= x && cell.MaxX >= x && cell.MinZ <= z && cell.MaxZ >= z;
}
