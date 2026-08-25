using NUnit.Framework;
using ThinWalls.Geometry;
using ThinWalls.Rendering;
using Verse;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class HybridWallRuntimePlanTests
{
    [Test]
    public void SparseDoorEndpointFramesDisableMipmapsThatBleedAcrossTheWholeVertexPlane()
    {
        HybridWallRuntimePlan wall = HybridWallRuntimePlanCompiler.Compile(
            HybridWallVertexTopology.FromOccupancy(HybridWallQuadrant.None, HybridWallRayMask.East),
            HybridWallRayMask.None,
            HybridWallRayMask.None);
        HybridWallRuntimePlan doorFrame = HybridWallRuntimePlanCompiler.Compile(
            HybridWallVertexTopology.FromOccupancy(HybridWallQuadrant.None, HybridWallRayMask.East),
            HybridWallRayMask.None,
            HybridWallRayMask.East);

        Assert.Multiple(() =>
        {
            Assert.That(HybridWallTextureSampling.UseMipmaps(wall), Is.True);
            Assert.That(HybridWallTextureSampling.UseMipmaps(doorFrame), Is.False,
                "a three-pixel cached frame cannot be mip-averaged over its transparent one-cell plane");
        });
    }

    [Test]
    public void MixedDoorAndWallVertexSelectsMipmapsPerEmittedPartition()
    {
        HybridWallRuntimePlan mixed = HybridWallRuntimePlanCompiler.Compile(
            HybridWallVertexTopology.FromOccupancy(
                HybridWallQuadrant.None,
                HybridWallRayMask.North | HybridWallRayMask.East),
            HybridWallRayMask.None,
            HybridWallRayMask.East);

        Assert.Multiple(() =>
        {
            Assert.That(HybridWallTextureSampling.UseMipmaps(mixed), Is.True,
                "the complete union shadow and outline include a full wall arm");
            Assert.That(HybridWallTextureSampling.UseMipmaps(mixed, HybridWallRayMask.North), Is.True,
                "the full adjoining wall partition retains normal far-zoom filtering");
            Assert.That(HybridWallTextureSampling.UseMipmaps(mixed, HybridWallRayMask.East), Is.False,
                "only the sparse three-pixel door endpoint partition disables mipmaps");
            Assert.That(HybridWallTextureSampling.UseMipmaps(
                    mixed,
                    HybridWallRayMask.North | HybridWallRayMask.East),
                Is.False,
                "a combined partition containing any sparse door frame cannot use mipmaps");
        });
    }

    [Test]
    public void MatchingUndamagedSingleOwnerArmsShareOneStructuralUnionPlane()
    {
        var partitions = new[]
        {
            new HybridWallPartitionVisual(
                HybridWallRayMask.East, 17, ThinWallMaterialFamily.Stone,
                ThinWallDamageGrade.None, doubled: false, door: false),
            new HybridWallPartitionVisual(
                HybridWallRayMask.West, 17, ThinWallMaterialFamily.Stone,
                ThinWallDamageGrade.None, doubled: false, door: false),
        };

        Assert.That(HybridWallPartitionBatching.CanRenderAsOneStructuralUnion(partitions), Is.True);

        var matchingDamagedPartitions = new[]
        {
            new HybridWallPartitionVisual(
                HybridWallRayMask.East, 17, ThinWallMaterialFamily.Stone,
                ThinWallDamageGrade.Moderate, doubled: false, door: false),
            new HybridWallPartitionVisual(
                HybridWallRayMask.West, 17, ThinWallMaterialFamily.Stone,
                ThinWallDamageGrade.Moderate, doubled: false, door: false),
        };
        Assert.That(
            HybridWallPartitionBatching.CanRenderAsOneStructuralUnion(matchingDamagedPartitions),
            Is.False,
            "damage remains owner-clipped even when adjacent owners share the same grade");
    }

    [TestCase(true, false, ThinWallDamageGrade.None, 17)]
    [TestCase(false, true, ThinWallDamageGrade.None, 17)]
    [TestCase(false, false, ThinWallDamageGrade.Moderate, 17)]
    [TestCase(false, false, ThinWallDamageGrade.None, 18)]
    public void DoubledDoorDamagedOrDifferentMaterialArmsRemainOwnerPartitioned(
        bool doubled,
        bool door,
        ThinWallDamageGrade secondDamage,
        int secondMaterial)
    {
        var partitions = new[]
        {
            new HybridWallPartitionVisual(
                HybridWallRayMask.East, 17, ThinWallMaterialFamily.Stone,
                ThinWallDamageGrade.None, doubled: false, door: false),
            new HybridWallPartitionVisual(
                HybridWallRayMask.West, secondMaterial, ThinWallMaterialFamily.Stone,
                secondDamage, doubled, door),
        };

        Assert.That(HybridWallPartitionBatching.CanRenderAsOneStructuralUnion(partitions), Is.False);
    }

    [Test]
    public void EveryQuadrantAndRayCombinationBindsTheTopologyCompilerToTheProductionRaster()
    {
        int compiled = 0;
        for (int quadrants = 0; quadrants < 16; quadrants++)
        for (int rays = 0; rays < 16; rays++)
        {
            HybridWallVertexTopology topology = HybridWallVertexTopology.FromOccupancy(
                (HybridWallQuadrant)quadrants,
                (HybridWallRayMask)rays);
            HybridWallRuntimePlan plan = HybridWallRuntimePlanCompiler.Compile(
                topology,
                HybridWallRayMask.None,
                HybridWallRayMask.None);

            Assert.Multiple(() =>
            {
                Assert.That(plan.Topology.Key, Is.EqualTo(topology.Arms));
                Assert.That(plan.Topology.OrdinaryQuadrants, Is.EqualTo(topology.OrdinaryQuadrants));
                Assert.That(plan.Raster.Rays, Is.EqualTo(topology.ThinRays));
                Assert.That(plan.Raster.ClearedQuadrants, Is.EqualTo(topology.OrdinaryQuadrants));
            });
            compiled++;
        }

        Assert.That(compiled, Is.EqualTo(256));
    }

    [TestCase(HybridWallRayMask.North, 30, 32)]
    [TestCase(HybridWallRayMask.East, 30, 32)]
    [TestCase(HybridWallRayMask.South, 28, 30)]
    [TestCase(HybridWallRayMask.West, 28, 30)]
    public void DoorRayContributesOnlyItsFlushThreePixelEndpointFrame(
        HybridWallRayMask ray,
        int minimumLongitudinal,
        int maximumLongitudinal)
    {
        HybridWallRuntimePlan plan = HybridWallRuntimePlanCompiler.Compile(
            HybridWallVertexTopology.FromOccupancy(HybridWallQuadrant.None, ray),
            HybridWallRayMask.None,
            ray);
        (int min, int max) = LongitudinalBounds(plan, ray);

        Assert.Multiple(() =>
        {
            Assert.That(min, Is.EqualTo(minimumLongitudinal));
            Assert.That(max, Is.EqualTo(maximumLongitudinal));
            Assert.That(plan.DoorRays, Is.EqualTo(ray));
        });
    }

    [Test]
    public void MixedJunctionStructuralPixelsHaveExactlyOneStablePrimaryRayOwner()
    {
        HybridWallRuntimePlan plan = HybridWallRuntimePlanCompiler.Compile(
            HybridWallVertexTopology.FromOccupancy(
                HybridWallQuadrant.None,
                HybridWallRayMask.North | HybridWallRayMask.East |
                HybridWallRayMask.South | HybridWallRayMask.West),
            HybridWallRayMask.None,
            HybridWallRayMask.None);

        for (int y = 0; y < HybridWallRasterPlan.Size; y++)
        for (int x = 0; x < HybridWallRasterPlan.Size; x++)
        {
            if (!plan.Raster[x, y].IsStructural)
            {
                continue;
            }

            HybridWallRayMask owner = plan.PrimaryOwnerAt(x, y);
            Assert.That(owner, Is.AnyOf(
                HybridWallRayMask.North,
                HybridWallRayMask.East,
                HybridWallRayMask.South,
                HybridWallRayMask.West));
        }
    }

    [Test]
    public void SunShadowVolumesUseTheExactStandardDoubledAndMovingDoorTopFootprints()
    {
        var horizontal = new SharedEdge(new IntVec3(10, 0, 20), ThinWallSide.North);
        var vertical = new SharedEdge(new IntVec3(10, 0, 20), ThinWallSide.East);
        HybridWallSunShadowVolume standard = HybridWallSunShadowGeometry.ForEdge(horizontal, ownerCount: 1);
        HybridWallSunShadowVolume doubled = HybridWallSunShadowGeometry.ForEdge(horizontal, ownerCount: 2);
        HybridWallSunShadowVolume verticalStandard = HybridWallSunShadowGeometry.ForEdge(vertical, ownerCount: 1);
        HybridWallSunShadowVolume movingHalf = HybridWallSunShadowGeometry.ForEdgeSpan(
            horizontal,
            ownerCount: 1,
            longitudinalMin: 0f,
            longitudinalMax: 0.5f);

        Assert.Multiple(() =>
        {
            Assert.That(standard.CenterX, Is.EqualTo(10.5f).Within(0.0001f));
            Assert.That(standard.CenterZ, Is.EqualTo(21f).Within(0.0001f));
            Assert.That(standard.SizeX, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(standard.SizeZ, Is.EqualTo(7f / 60f).Within(0.0001f));
            Assert.That(standard.Height, Is.EqualTo(1f));
            Assert.That(doubled.SizeZ, Is.EqualTo(14f / 60f).Within(0.0001f));
            Assert.That(doubled.CenterZ, Is.EqualTo(21f).Within(0.0001f));
            Assert.That(verticalStandard.CenterX, Is.EqualTo(11f).Within(0.0001f));
            Assert.That(verticalStandard.CenterZ, Is.EqualTo(20.5f).Within(0.0001f));
            Assert.That(verticalStandard.SizeX, Is.EqualTo(7f / 60f).Within(0.0001f));
            Assert.That(verticalStandard.SizeZ, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(movingHalf.CenterX, Is.EqualTo(10.25f).Within(0.0001f));
            Assert.That(movingHalf.SizeX, Is.EqualTo(0.5f).Within(0.0001f));
        });
    }

    private static (int Min, int Max) LongitudinalBounds(
        HybridWallRuntimePlan plan,
        HybridWallRayMask ray)
    {
        var values = new List<int>();
        for (int y = 0; y < HybridWallRasterPlan.Size; y++)
        for (int x = 0; x < HybridWallRasterPlan.Size; x++)
        {
            HybridWallRasterPixel pixel = plan.Raster[x, y];
            if (pixel.Surface == HybridWallRasterSurface.Top && pixel.OwnerRays.HasFlag(ray))
            {
                values.Add(ray is HybridWallRayMask.North or HybridWallRayMask.South ? y : x);
            }
        }
        return (values.Min(), values.Max());
    }
}
