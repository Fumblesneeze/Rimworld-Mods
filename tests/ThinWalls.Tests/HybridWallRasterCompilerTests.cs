using NUnit.Framework;
using ThinWalls.Rendering;
using UnityEngine;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class HybridWallRasterCompilerTests
{
    [Test]
    public void HorizontalStraightPreservesSevenPixelTopAndFullTwentyTwoPixelFront()
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.East | HybridWallRayMask.West,
            includeOutline: false);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Count(HybridWallRasterSurface.Top), Is.EqualTo(60 * 7));
            Assert.That(plan.Count(HybridWallRasterSurface.Front), Is.EqualTo(60 * 22));
            Assert.That(plan[30, 38].Surface, Is.EqualTo(HybridWallRasterSurface.Top));
            Assert.That(plan[30, 16].Surface, Is.EqualTo(HybridWallRasterSurface.Front));
            Assert.That(plan[30, 37].Surface, Is.EqualTo(HybridWallRasterSurface.Front));
        });
    }

    [Test]
    public void VerticalStraightPreservesSevenPixelTopAndBothMeasuredSideBands()
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.North | HybridWallRayMask.South,
            includeOutline: false);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Count(HybridWallRasterSurface.Top), Is.EqualTo(60 * 7));
            Assert.That(plan.Count(HybridWallRasterSurface.WestSide), Is.EqualTo(60 * 11));
            Assert.That(plan.Count(HybridWallRasterSurface.EastSide), Is.EqualTo(60 * 10));
            Assert.That(plan[16, 30].Surface, Is.EqualTo(HybridWallRasterSurface.WestSide));
            Assert.That(plan[43, 30].Surface, Is.EqualTo(HybridWallRasterSurface.EastSide));
        });
    }

    [Test]
    public void NorthSouthEndpointKeepsTheSmallTopButUsesTheCompleteProjectedFacadeEnvelope()
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.North,
            includeOutline: false);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Count(HybridWallRasterSurface.Top), Is.EqualTo(30 * 7));
            Assert.That(plan.Count(HybridWallRasterSurface.Front), Is.EqualTo(28 * 22));
            Assert.That(plan[16, 8].Surface, Is.EqualTo(HybridWallRasterSurface.Front));
            Assert.That(plan[43, 29].Surface, Is.EqualTo(HybridWallRasterSurface.Front));
            Assert.That(plan[16, 8].SourceLinkIndex, Is.EqualTo(10));
            Assert.That(plan[43, 8].SourceLinkIndex, Is.EqualTo(10));
            Assert.That(plan[16, 8].SourceX, Is.Not.EqualTo(plan[43, 8].SourceX));
            Assert.That(plan[27, 30].Surface, Is.EqualTo(HybridWallRasterSurface.Top));
            Assert.That(plan[33, 30].Surface, Is.EqualTo(HybridWallRasterSurface.Top));
        });
    }

    [Test]
    public void HorizontalAndVerticalStraightsUseTheirOwnCoreDonorsAcrossTheFullLongitudinalAxis()
    {
        HybridWallRasterPlan horizontal = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.East | HybridWallRayMask.West,
            includeOutline: false);
        HybridWallRasterPlan vertical = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.North | HybridWallRayMask.South,
            includeOutline: false);

        Assert.Multiple(() =>
        {
            Assert.That(Enumerable.Range(0, 60).Select(x => horizontal[x, 41].SourceX),
                Is.EqualTo(Enumerable.Range(0, 60).Select(value => (byte)((value + 30) % 60))));
            Assert.That(horizontal[0, 41].SourceLinkIndex, Is.EqualTo(10));
            Assert.That(horizontal[59, 41].SourceLinkIndex, Is.EqualTo(10));
            Assert.That(horizontal[30, 38].SourceY, Is.EqualTo(25));
            Assert.That(horizontal[30, 44].SourceY, Is.EqualTo(57));

            Assert.That(Enumerable.Range(0, 60).Select(y => vertical[30, y].SourceY),
                Is.EqualTo(Enumerable.Range(0, 60).Select(value => (byte)value)));
            Assert.That(vertical[30, 0].SourceLinkIndex, Is.EqualTo(5));
            Assert.That(vertical[30, 59].SourceLinkIndex, Is.EqualTo(5));
            Assert.That(vertical[27, 30].SourceX, Is.EqualTo(14));
            Assert.That(vertical[33, 30].SourceX, Is.EqualTo(46));
        });
    }

    [Test]
    public void TwoVertexPartitionsAssembleOneHorizontalEdgeWithoutResettingBrickPhaseAtItsMidpoint()
    {
        HybridWallRasterPlan leftVertex = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.East,
            includeOutline: false);
        HybridWallRasterPlan rightVertex = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.West,
            includeOutline: false);

        byte[] assembledSourcePhase = Enumerable.Range(30, 30)
            .Select(x => leftVertex[x, 18].SourceX)
            .Concat(Enumerable.Range(0, 30).Select(x => rightVertex[x, 18].SourceX))
            .ToArray();

        Assert.That(assembledSourcePhase,
            Is.EqualTo(Enumerable.Range(0, 60).Select(value => (byte)value)));
    }

    [Test]
    public void EveryLinkedUnionUsesOnlyStraightAxisDonorsForThinArmSurfaces()
    {
        for (int mask = 1; mask < 16; mask++)
        {
            HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile((HybridWallRayMask)mask);
            foreach (HybridWallRasterPixel pixel in plan.Pixels.Where(pixel => pixel.IsStructural))
            {
                HybridWallRayMask horizontal = pixel.OwnerRays &
                                               (HybridWallRayMask.East | HybridWallRayMask.West);
                HybridWallRayMask vertical = pixel.OwnerRays &
                                             (HybridWallRayMask.North | HybridWallRayMask.South);
                if (pixel.Surface == HybridWallRasterSurface.Front)
                {
                    Assert.That(pixel.SourceLinkIndex, Is.EqualTo(10), ((HybridWallRayMask)mask).ToString());
                }
                else if (pixel.Surface is HybridWallRasterSurface.WestSide or HybridWallRasterSurface.EastSide)
                {
                    Assert.That(pixel.SourceLinkIndex, Is.EqualTo(5), ((HybridWallRayMask)mask).ToString());
                }
                else if (pixel.Surface == HybridWallRasterSurface.Top)
                {
                    Assert.That(pixel.SourceLinkIndex,
                        Is.EqualTo(horizontal != HybridWallRayMask.None ? 10 : 5),
                        ((HybridWallRayMask)mask).ToString());
                }
            }
        }
    }

    [Test]
    public void EveryThinMaskTerminatesHorizontalRaysFlushWithoutLongitudinalSideSlabs()
    {
        for (int bits = 0; bits < 16; bits++)
        {
            var rays = (HybridWallRayMask)bits;
            HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(rays);

            foreach (HybridWallRasterPixel pixel in plan.Pixels.Where(pixel =>
                         pixel.Surface is HybridWallRasterSurface.WestSide or HybridWallRasterSurface.EastSide))
            {
                HybridWallRayMask horizontalOwner = pixel.OwnerRays &
                                                    (HybridWallRayMask.East | HybridWallRayMask.West);
                HybridWallRayMask verticalOwner = pixel.OwnerRays &
                                                  (HybridWallRayMask.North | HybridWallRayMask.South);
                Assert.That(horizontalOwner != HybridWallRayMask.None && verticalOwner == HybridWallRayMask.None,
                    Is.False,
                    $"mask {rays} retained a horizontal endpoint side slab owned by {pixel.OwnerRays}");
            }
        }
    }

    [Test]
    public void EveryRequestedRayFillsItsCompleteExpectedHalfCellBoundaryBand()
    {
        for (int bits = 0; bits < 16; bits++)
        {
            var rays = (HybridWallRayMask)bits;
            foreach (HybridWallRayMask doubledRays in new[] { HybridWallRayMask.None, rays })
            {
                HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
                    rays,
                    doubledRays,
                    includeOutline: false);
                int width = doubledRays == HybridWallRayMask.None
                    ? HybridWallRasterCompiler.StandardTopWidth
                    : HybridWallRasterCompiler.DoubledTopWidth;
                int min = 30 - width / 2;
                int horizontalMin = 30 + ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ;

                if (rays.HasFlag(HybridWallRayMask.North))
                    Assert.That(Enumerable.Range(min, width).All(x => plan[x, 59].Surface == HybridWallRasterSurface.Top), Is.True, $"{rays}/{doubledRays} North");
                if (rays.HasFlag(HybridWallRayMask.East))
                    Assert.That(Enumerable.Range(horizontalMin, width).All(y => plan[59, y].Surface == HybridWallRasterSurface.Top), Is.True, $"{rays}/{doubledRays} East");
                if (rays.HasFlag(HybridWallRayMask.South))
                    Assert.That(Enumerable.Range(min, width).All(x => plan[x, 0].Surface == HybridWallRasterSurface.Top), Is.True, $"{rays}/{doubledRays} South");
                if (rays.HasFlag(HybridWallRayMask.West))
                    Assert.That(Enumerable.Range(horizontalMin, width).All(y => plan[0, y].Surface == HybridWallRasterSurface.Top), Is.True, $"{rays}/{doubledRays} West");
            }
        }
    }

    [TestCase(HybridWallRayMask.West, HybridWallRayMask.East)]
    [TestCase(HybridWallRayMask.East, HybridWallRayMask.West)]
    public void AsymmetricHorizontalDoubledArmsRetainIndependentFullDonorBands(
        HybridWallRayMask standardRay,
        HybridWallRayMask doubledRay)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.East | HybridWallRayMask.West,
            doubledRay,
            includeOutline: false);
        int standardX = standardRay == HybridWallRayMask.West ? 10 : 50;
        int doubledX = doubledRay == HybridWallRayMask.West ? 10 : 50;

        Assert.Multiple(() =>
        {
            Assert.That(plan[standardX, 38].SourceY, Is.EqualTo((byte)25));
            Assert.That(plan[standardX, 44].SourceY, Is.EqualTo((byte)57));
            Assert.That(plan[doubledX, 38].SourceY, Is.EqualTo((byte)25));
            Assert.That(plan[doubledX, 51].SourceY, Is.EqualTo((byte)57));
        });
    }

    [TestCase(HybridWallRayMask.South, HybridWallRayMask.North)]
    [TestCase(HybridWallRayMask.North, HybridWallRayMask.South)]
    public void AsymmetricVerticalDoubledArmsRetainIndependentFullDonorBands(
        HybridWallRayMask standardRay,
        HybridWallRayMask doubledRay)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.North | HybridWallRayMask.South,
            doubledRay,
            includeOutline: false);
        int standardY = standardRay == HybridWallRayMask.South ? 10 : 50;
        int doubledY = doubledRay == HybridWallRayMask.South ? 10 : 50;

        Assert.Multiple(() =>
        {
            Assert.That(plan[27, standardY].SourceX, Is.EqualTo((byte)14));
            Assert.That(plan[33, standardY].SourceX, Is.EqualTo((byte)46));
            Assert.That(plan[23, doubledY].SourceX, Is.EqualTo((byte)14));
            Assert.That(plan[36, doubledY].SourceX, Is.EqualTo((byte)46));
        });
    }

    [TestCase(HybridWallRayMask.East)]
    [TestCase(HybridWallRayMask.West)]
    [TestCase(HybridWallRayMask.East | HybridWallRayMask.West)]
    public void NorthArmJoinedToAnyHorizontalWallHasNoTerminalFacade(HybridWallRayMask horizontalRays)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.North | horizontalRays,
            includeOutline: false);

        for (int y = 8; y <= 29; y++)
        for (int x = 16; x <= 43; x++)
        {
            Assert.That(
                plan[x, y].Surface == HybridWallRasterSurface.Front &&
                plan[x, y].OwnerRays == HybridWallRayMask.North,
                Is.False,
                $"connected North retained its exposed endpoint facade at {x},{y}");
        }
    }

    [Test]
    public void AllThinLinkStatesProduceOneCoherentRasterWithoutStructuralHoles()
    {
        for (int bits = 1; bits < 16; bits++)
        {
            var rays = (HybridWallRayMask)bits;
            HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(rays);

            Assert.Multiple(() =>
            {
                Assert.That(plan.Pixels.Any(pixel => pixel.Surface == HybridWallRasterSurface.Top), Is.True, rays.ToString());
                Assert.That(plan.Pixels.Any(pixel => pixel.Surface == HybridWallRasterSurface.Outline), Is.True, rays.ToString());
                Assert.That(StructuralPixelsAreConnected(plan), Is.True, rays.ToString());
            });
        }
    }

    [Test]
    public void HorizontalFacadeStaysInFrontOfNorthSouthArmsAtEveryJunction()
    {
        foreach (HybridWallRayMask rays in new[]
                 {
                     HybridWallRayMask.East | HybridWallRayMask.South,
                     HybridWallRayMask.East | HybridWallRayMask.West | HybridWallRayMask.South,
                     HybridWallRayMask.North | HybridWallRayMask.East |
                     HybridWallRayMask.South | HybridWallRayMask.West,
                 })
        {
            HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(rays, includeOutline: false);
            Assert.Multiple(() =>
            {
                Assert.That(plan[30, 20].Surface, Is.EqualTo(HybridWallRasterSurface.Front), rays.ToString());
                Assert.That(plan[30, 20].OwnerRays,
                    Is.EqualTo(HybridWallRayMask.East), rays.ToString());
                Assert.That(plan[30, 40].Surface, Is.EqualTo(HybridWallRasterSurface.Top), rays.ToString());
            });
        }
    }

    [TestCase(HybridWallRayMask.North | HybridWallRayMask.East, 4)]
    [TestCase(HybridWallRayMask.North | HybridWallRayMask.East | HybridWallRayMask.West, 7)]
    [TestCase(HybridWallRayMask.North | HybridWallRayMask.East |
              HybridWallRayMask.South | HybridWallRayMask.West, 13)]
    public void PerpendicularThinJoinsUseOnlyOnePixelInternalDiagonalPhaseSeams(
        HybridWallRayMask rays,
        int expectedSeamPixels)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(rays, includeOutline: false);
        IReadOnlyList<HybridWallTreatmentMark> marks = HybridWallSurfaceTreatmentRecipe.Compile(
            plan,
            ThinWallMaterialFamily.Stone,
            ThinWallDamageGrade.None,
            door: false);
        int[] seamIndices = marks
            .Select((mark, index) => (mark, index))
            .Where(item => item.mark == HybridWallTreatmentMark.JunctionSeam)
            .Select(item => item.index)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(seamIndices, Has.Length.EqualTo(expectedSeamPixels));
            Assert.That(seamIndices.All(index => plan.Pixels[index].Surface == HybridWallRasterSurface.Top),
                Is.True, "the miter is an internal top-plane material seam, never new geometry");
            Assert.That(seamIndices.All(index => plan.Pixels[index].IsStructural), Is.True);
            Assert.That(seamIndices.All(index =>
                    HybridWallTreatmentCoverage.Alpha(173, marks[index]) == 173),
                Is.True, "the seam must not change structural alpha or create a diagonal gap");
        });

        AssertDiagonal(HybridWallRayMask.North, HybridWallRayMask.East, +1, +1);
        AssertDiagonal(HybridWallRayMask.North, HybridWallRayMask.West, -1, +1);
        AssertDiagonal(HybridWallRayMask.South, HybridWallRayMask.East, +1, -1);
        AssertDiagonal(HybridWallRayMask.South, HybridWallRayMask.West, -1, -1);

        void AssertDiagonal(
            HybridWallRayMask vertical,
            HybridWallRayMask horizontal,
            int dx,
            int dy)
        {
            if (!rays.HasFlag(vertical) || !rays.HasFlag(horizontal))
            {
                return;
            }

            for (int step = 0; step <= 3; step++)
            {
                int x = 30 + (dx * step);
                int y = 41 + (dy * step);
                Assert.That(marks[y * HybridWallRasterPlan.Size + x],
                    Is.EqualTo(HybridWallTreatmentMark.JunctionSeam),
                    $"missing {vertical}+{horizontal} phase seam at {x},{y}");
            }
        }
    }

    [Test]
    public void CollinearThinRunsDoNotAcquireJunctionSeams()
    {
        foreach (HybridWallRayMask rays in new[]
                 {
                     HybridWallRayMask.East | HybridWallRayMask.West,
                     HybridWallRayMask.North | HybridWallRayMask.South,
                 })
        {
            HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(rays);
            IReadOnlyList<HybridWallTreatmentMark> marks = HybridWallSurfaceTreatmentRecipe.Compile(
                plan,
                ThinWallMaterialFamily.Stone,
                ThinWallDamageGrade.None,
                door: false);
            Assert.That(marks, Does.Not.Contain(HybridWallTreatmentMark.JunctionSeam), rays.ToString());
        }
    }

    [TestCase(HybridWallRayMask.East | HybridWallRayMask.South)]
    [TestCase(HybridWallRayMask.West | HybridWallRayMask.South)]
    [TestCase(HybridWallRayMask.East | HybridWallRayMask.West | HybridWallRayMask.South)]
    [TestCase(HybridWallRayMask.North | HybridWallRayMask.East |
              HybridWallRayMask.South | HybridWallRayMask.West)]
    public void BalancedSouthJunctionStaysConnectedBehindTheHorizontalFacadeWithoutAVisibleTopTab(
        HybridWallRayMask rays)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(rays);

        Assert.Multiple(() =>
        {
            for (int y = 31; y <= 37; y++)
            for (int x = 27; x <= 33; x++)
            {
                Assert.That(plan[x, y].IsStructural, Is.True,
                    $"the translated south/horizontal junction retained a terrain notch at {x},{y}");
                Assert.That(plan[x, y].Surface,
                    Is.Not.EqualTo(HybridWallRasterSurface.Outline),
                    $"the translated top bridge retained an internal outline at {x},{y}");
            }

            for (int y = 31; y <= 37; y++)
            for (int x = 27; x <= 33; x++)
            {
                Assert.That(plan[x, y].Surface, Is.EqualTo(HybridWallRasterSurface.Front),
                    "the bridge must remain structural behind the facade without exposing a pasted rectangular top tab");
                Assert.That(plan[x, y].OwnerRays &
                            (HybridWallRayMask.East | HybridWallRayMask.West),
                    Is.Not.EqualTo(HybridWallRayMask.None),
                    "the visually foreground surface must be owned by an incident horizontal arm");
            }

            for (int y = 38; y <= 44; y++)
            for (int x = 27; x <= 33; x++)
            {
                Assert.That(plan[x, y].Surface, Is.EqualTo(HybridWallRasterSurface.Top),
                    "only the top band geometrically behind the facade may remain exposed");
            }
        });
    }

    [Test]
    public void DoubledSouthJunctionOccludesItsCompleteBridgeBehindTheHorizontalFacade()
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.East | HybridWallRayMask.South,
            doubledRays: HybridWallRayMask.South,
            includeOutline: false);

        Assert.Multiple(() =>
        {
            for (int y = 31; y <= 37; y++)
            for (int x = 23; x <= 36; x++)
            {
                Assert.That(plan[x, y].Surface, Is.EqualTo(HybridWallRasterSurface.Front),
                    $"the doubled bridge must not leave an exposed top tab at {x},{y}");
            }
        });
    }

    [TestCase(HybridWallRayMask.East, 30, 32)]
    [TestCase(HybridWallRayMask.West, 28, 30)]
    public void SouthWallToHorizontalDoorUsesOnlyTheThreePixelFixedFrameBridge(
        HybridWallRayMask doorRay,
        int frameMinX,
        int frameMaxX)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.South | doorRay,
            doorRays: doorRay,
            includeOutline: false);

        Assert.Multiple(() =>
        {
            for (int y = 31; y <= 37; y++)
            for (int x = 27; x <= 33; x++)
            {
                if (x >= frameMinX && x <= frameMaxX)
                {
                    Assert.That(plan[x, y].Surface, Is.EqualTo(HybridWallRasterSurface.Top),
                        $"the fixed {doorRay} frame must remain connected to the South wall at {x},{y}");
                    Assert.That(plan[x, y].OwnerRays & doorRay, Is.EqualTo(doorRay));
                }
                else
                {
                    Assert.That(plan[x, y].Surface, Is.EqualTo(HybridWallRasterSurface.Transparent),
                        $"a horizontal door must not manufacture a full-width South top tab at {x},{y}");
                }
            }
        });
    }

    [TestCase(HybridWallRayMask.East, 30, 32)]
    [TestCase(HybridWallRayMask.West, 28, 30)]
    public void DoubledSouthWallStillNarrowsToTheHorizontalDoorsFixedFrameBridge(
        HybridWallRayMask doorRay,
        int frameMinX,
        int frameMaxX)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.South | doorRay,
            doubledRays: HybridWallRayMask.South,
            doorRays: doorRay,
            includeOutline: false);

        Assert.Multiple(() =>
        {
            for (int y = 31; y <= 37; y++)
            for (int x = 23; x <= 36; x++)
            {
                bool inFrame = x >= frameMinX && x <= frameMaxX;
                Assert.That(plan[x, y].Surface,
                    Is.EqualTo(inFrame
                        ? HybridWallRasterSurface.Top
                        : HybridWallRasterSurface.Transparent),
                    $"doubled South ownership must not widen the fixed {doorRay} frame at {x},{y}");
            }
        });
    }

    [Test]
    public void SouthWallWithOneHorizontalWallAndOneHorizontalDoorUsesTheWallFacadeBridge()
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.South | HybridWallRayMask.East | HybridWallRayMask.West,
            doorRays: HybridWallRayMask.East,
            includeOutline: false);

        Assert.Multiple(() =>
        {
            for (int y = 31; y <= 37; y++)
            for (int x = 27; x <= 33; x++)
            {
                Assert.That(plan[x, y].Surface, Is.EqualTo(HybridWallRasterSurface.Front));
                Assert.That(plan[x, y].OwnerRays & HybridWallRayMask.West,
                    Is.EqualTo(HybridWallRayMask.West),
                    "the solid horizontal wall facade owns the foreground bridge while the opposing door remains an aperture");
            }
        });
    }

    [TestCase(HybridWallRayMask.East, false)]
    [TestCase(HybridWallRayMask.West, false)]
    [TestCase(HybridWallRayMask.East | HybridWallRayMask.West, false)]
    [TestCase(HybridWallRayMask.East, true)]
    [TestCase(HybridWallRayMask.West, true)]
    [TestCase(HybridWallRayMask.East | HybridWallRayMask.West, true)]
    public void NorthWallMeetingHorizontalWallHasNoHangingSouthFacingEndpointTab(
        HybridWallRayMask horizontalRays,
        bool doubledNorth)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.North | horizontalRays,
            doubledRays: doubledNorth ? HybridWallRayMask.North : HybridWallRayMask.None,
            includeOutline: false);
        int facadeMinX = doubledNorth ? 12 : 16;
        int facadeMaxX = doubledNorth ? 46 : 43;

        Assert.Multiple(() =>
        {
            foreach (HybridWallRayMask ray in new[] { HybridWallRayMask.West, HybridWallRayMask.East })
            {
                if (!horizontalRays.HasFlag(ray))
                {
                    continue;
                }

                int minX = ray == HybridWallRayMask.West ? facadeMinX : 30;
                int maxX = ray == HybridWallRayMask.West && horizontalRays.HasFlag(HybridWallRayMask.East)
                    ? 29
                    : ray == HybridWallRayMask.West
                        ? 30
                        : facadeMaxX;
                for (int y = 8; y <= 15; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    Assert.That(plan[x, y].Surface, Is.EqualTo(HybridWallRasterSurface.Transparent),
                        $"the connected {ray} half may not retain a hanging North endpoint at {x},{y}");
                }

                for (int y = 16; y <= 37; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    Assert.That(plan[x, y].Surface, Is.EqualTo(HybridWallRasterSurface.Front),
                        $"the {ray} horizontal facade must own its complete North-junction half at {x},{y}");
                    Assert.That(plan[x, y].OwnerRays & ray, Is.EqualTo(ray),
                        $"the North junction must read as the incident {ray} wall rather than a pasted post at {x},{y}");
                }

                for (int y = 38; y <= 44; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    Assert.That(plan[x, y].Surface, Is.EqualTo(HybridWallRasterSurface.Top));
                    Assert.That(plan[x, y].OwnerRays & ray, Is.EqualTo(ray));
                }
            }
        });
    }

    [TestCase(HybridWallRayMask.East, false)]
    [TestCase(HybridWallRayMask.West, false)]
    [TestCase(HybridWallRayMask.East, true)]
    [TestCase(HybridWallRayMask.West, true)]
    public void SingleHorizontalArmRemovesTheCompleteOppositeNorthTerminalProjection(
        HybridWallRayMask horizontalRay,
        bool doubledNorth)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.North | horizontalRay,
            doubledRays: doubledNorth ? HybridWallRayMask.North : HybridWallRayMask.None,
            includeOutline: false);
        int facadeMinX = doubledNorth ? 12 : 16;
        int facadeMaxX = doubledNorth ? 46 : 43;
        int oppositeMinX = horizontalRay == HybridWallRayMask.East ? facadeMinX : 31;
        int oppositeMaxX = horizontalRay == HybridWallRayMask.East ? 29 : facadeMaxX;

        Assert.Multiple(() =>
        {
            for (int y = 8; y <= 37; y++)
            for (int x = oppositeMinX; x <= oppositeMaxX; x++)
            {
                Assert.That(plan[x, y].Surface, Is.EqualTo(HybridWallRasterSurface.Transparent),
                    $"{horizontalRay} L retained the obsolete opposite North terminal projection at {x},{y}");
            }

            int topMinX = doubledNorth ? 23 : 27;
            int topWidth = doubledNorth ? 14 : 7;
            Assert.That(Enumerable.Range(topMinX, topWidth)
                    .All(x => plan[x, 38].Surface == HybridWallRasterSurface.Top),
                Is.True,
                "the clipped North member must still meet the horizontal top as one connected L union");
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OutlinedNorthHorizontalTIsOneConnectedUnionWithoutDetachedTab(bool doubledNorth)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.North | HybridWallRayMask.East | HybridWallRayMask.West,
            doubledRays: doubledNorth ? HybridWallRayMask.North : HybridWallRayMask.None);
        int facadeMinX = doubledNorth ? 12 : 16;
        int facadeMaxX = doubledNorth ? 46 : 43;

        Assert.Multiple(() =>
        {
            Assert.That(StructuralPixelsAreConnected(plan), Is.True);
            Assert.That(Enumerable.Range(facadeMinX, facadeMaxX - facadeMinX + 1)
                    .SelectMany(x => Enumerable.Range(8, 8).Select(y => plan[x, y]))
                    .Any(pixel => pixel.IsStructural),
                Is.False,
                "the outlined T may contain only the one contour derived around the facade, never a detached structural endpoint tab");
            Assert.That(Enumerable.Range(facadeMinX, facadeMaxX - facadeMinX + 1)
                    .SelectMany(x => Enumerable.Range(0, 13).Select(y => plan[x, y]))
                    .Any(pixel => pixel.Surface != HybridWallRasterSurface.Transparent),
                Is.False,
                "the final contour may extend three rows below the y=16 facade but may not retain the old y=8 endpoint silhouette");
        });
    }

    [TestCase(HybridWallRayMask.East, 30, 32, false)]
    [TestCase(HybridWallRayMask.West, 28, 30, false)]
    [TestCase(HybridWallRayMask.East, 30, 32, true)]
    [TestCase(HybridWallRayMask.West, 28, 30, true)]
    public void NorthWallMeetingHorizontalDoorKeepsOnlyTheThreePixelFrameFacade(
        HybridWallRayMask doorRay,
        int frameMinX,
        int frameMaxX,
        bool doubledNorth)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.North | doorRay,
            doubledRays: doubledNorth ? HybridWallRayMask.North : HybridWallRayMask.None,
            doorRays: doorRay,
            includeOutline: false);
        int facadeMinX = doubledNorth ? 12 : 16;
        int facadeMaxX = doubledNorth ? 46 : 43;
        int incidentMinX = doorRay == HybridWallRayMask.West ? facadeMinX : 30;
        int incidentMaxX = doorRay == HybridWallRayMask.West ? 30 : facadeMaxX;
        int oppositeMinX = doorRay == HybridWallRayMask.West ? 31 : facadeMinX;
        int oppositeMaxX = doorRay == HybridWallRayMask.West ? facadeMaxX : 29;

        Assert.Multiple(() =>
        {
            for (int y = 8; y <= 37; y++)
            for (int x = incidentMinX; x <= incidentMaxX; x++)
            {
                bool inFrameFacade = y >= 16 && x >= frameMinX && x <= frameMaxX;
                Assert.That(plan[x, y].Surface,
                    Is.EqualTo(inFrameFacade
                        ? HybridWallRasterSurface.Front
                        : HybridWallRasterSurface.Transparent),
                    $"a horizontal door may retain only its three-pixel fixed-frame facade at {x},{y}");
            }

            for (int y = 8; y <= 37; y++)
            for (int x = oppositeMinX; x <= oppositeMaxX; x++)
            {
                Assert.That(plan[x, y].Surface,
                    Is.EqualTo(HybridWallRasterSurface.Transparent),
                    $"the unoccupied half opposite a {doorRay} door retained the obsolete North terminal projection at {x},{y}");
            }
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NorthWallWithHorizontalWallAndDoorClipsEachHalfWithoutAEndpointSlab(bool doubledNorth)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.North | HybridWallRayMask.East | HybridWallRayMask.West,
            doubledRays: doubledNorth ? HybridWallRayMask.North : HybridWallRayMask.None,
            doorRays: HybridWallRayMask.East,
            includeOutline: false);
        int facadeMinX = doubledNorth ? 12 : 16;
        int facadeMaxX = doubledNorth ? 46 : 43;

        Assert.Multiple(() =>
        {
            for (int y = 8; y <= 15; y++)
            for (int x = facadeMinX; x <= facadeMaxX; x++)
            {
                Assert.That(plan[x, y].Surface, Is.EqualTo(HybridWallRasterSurface.Transparent));
            }

            for (int y = 16; y <= 37; y++)
            {
                for (int x = facadeMinX; x <= 30; x++)
                {
                    Assert.That(plan[x, y].Surface, Is.EqualTo(HybridWallRasterSurface.Front));
                    Assert.That(plan[x, y].OwnerRays & HybridWallRayMask.West,
                        Is.EqualTo(HybridWallRayMask.West),
                        "the solid West wall facade must own its complete half, including the shared center column");
                }
                for (int x = 31; x <= facadeMaxX; x++)
                {
                    bool inDoorFrame = x <= 32;
                    Assert.That(plan[x, y].Surface,
                        Is.EqualTo(inDoorFrame
                            ? HybridWallRasterSurface.Front
                            : HybridWallRasterSurface.Transparent),
                        $"the East door half may retain only frame columns 31..32 at {x},{y}");
                }
            }
        });
    }

    [Test]
    public void CoreBricksDonorCarriesItsOwnCoursePhaseWithoutAlgorithmicRephasing()
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.East | HybridWallRayMask.West,
            includeOutline: false);

        Assert.Multiple(() =>
        {
            Assert.That(plan[30, 10].SourceX, Is.EqualTo((byte)0));
            Assert.That(plan[30, 17].SourceX, Is.EqualTo((byte)0));
            Assert.That(plan[30, 24].SourceX, Is.EqualTo((byte)0));
            Assert.That(new[] { plan[30, 10].SourceX, plan[30, 17].SourceX, plan[30, 24].SourceX }
                .Distinct().Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public void VerticalStoneSidesBorrowThreeCourseDetailFromTheHorizontalCoreFacade()
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.North | HybridWallRayMask.South,
            includeOutline: false);
        HybridWallRasterPixel westInner = plan.Pixels.First(pixel =>
            pixel.Surface == HybridWallRasterSurface.WestSide && pixel.SourceX == 13);
        HybridWallRasterPixel westOuter = plan.Pixels.First(pixel =>
            pixel.Surface == HybridWallRasterSurface.WestSide && pixel.SourceX == 3);
        HybridWallRasterPixel eastInner = plan.Pixels.First(pixel =>
            pixel.Surface == HybridWallRasterSurface.EastSide && pixel.SourceX == 47);
        HybridWallRasterPixel eastOuter = plan.Pixels.First(pixel =>
            pixel.Surface == HybridWallRasterSurface.EastSide && pixel.SourceX == 56);

        Assert.Multiple(() =>
        {
            AssertDonor(westInner, 24);
            AssertDonor(westOuter, 3);
            AssertDonor(eastInner, 24);
            AssertDonor(eastOuter, 3);
        });

        static void AssertDonor(HybridWallRasterPixel source, int expectedFacadeY)
        {
            Assert.That(HybridWallStoneFacadeDonor.TryMap(source, out HybridWallRasterPixel donor), Is.True);
            Assert.That(donor.SourceLinkIndex, Is.EqualTo(10),
                "vertical stone detail must come from the Core East|West front-face donor");
            Assert.That(donor.SourceY, Is.EqualTo(expectedFacadeY));
            Assert.That(donor.Surface, Is.EqualTo(source.Surface),
                "the link-5 side profile remains the final geometry even though its color detail is borrowed");
        }
    }

    [Test]
    public void HorizontalEndpointAndHorizontalOwnedMixedCapsKeepTheirNativeSideDetail()
    {
        var horizontalCap = new HybridWallRasterPixel(
            HybridWallRasterSurface.WestSide,
            13,
            40,
            10,
            HybridWallRayMask.East);
        var sharedMixedCap = new HybridWallRasterPixel(
            HybridWallRasterSurface.EastSide,
            47,
            40,
            3,
            HybridWallRayMask.North | HybridWallRayMask.East);

        Assert.Multiple(() =>
        {
            Assert.That(HybridWallStoneFacadeDonor.TryMap(horizontalCap, out _), Is.False,
                "east/west endpoint caps are not north-south wall sides");
            Assert.That(HybridWallStoneFacadeDonor.TryMap(sharedMixedCap, out _), Is.False,
                "ambiguous H/V intersection caps must retain native detail rather than receive a north-south-only remap");
        });
    }

    [Test]
    public void ClearingAnOrdinaryQuadrantRemovesStructuralAndJoinOutlinePixelsFromThinOwner()
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.West | HybridWallRayMask.East,
            clearedQuadrants: HybridWallQuadrant.NorthEast);

        Assert.Multiple(() =>
        {
            for (int y = 30; y < 60; y++)
            for (int x = 30; x < 60; x++)
            {
                Assert.That(plan[x, y].Surface, Is.EqualTo(HybridWallRasterSurface.Transparent));
            }

            Assert.That(plan[29, 32].Surface, Is.Not.EqualTo(HybridWallRasterSurface.Outline));
            Assert.That(plan[10, 32].IsStructural, Is.True);
        });
    }

    [TestCase(HybridWallRayMask.West, HybridWallQuadrant.NorthEast)]
    [TestCase(HybridWallRayMask.East, HybridWallQuadrant.NorthWest)]
    [TestCase(HybridWallRayMask.West, HybridWallQuadrant.SouthEast)]
    [TestCase(HybridWallRayMask.East, HybridWallQuadrant.SouthWest)]
    public void OrdinaryCornerRemovesTheCompleteTranslatedHorizontalEndpointCap(
        HybridWallRayMask ray,
        HybridWallQuadrant quadrant)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            ray,
            HybridWallRayMask.None,
            quadrant,
            includeOutline: true);

        int minX = ray == HybridWallRayMask.West ? 31 : 27;
        int maxX = ray == HybridWallRayMask.West ? 33 : 29;
        int minY = 8 + ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ -
                   (HybridWallRasterCompiler.OutlineDepth + 1);
        int maxY = 30 + ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ +
                   HybridWallRasterCompiler.StandardTopWidth - 1 +
                   HybridWallRasterCompiler.OutlineDepth + 1;

        Assert.Multiple(() =>
        {
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                Assert.That(plan[x, y].Surface,
                    Is.Not.EqualTo(HybridWallRasterSurface.Outline),
                    $"projected {ray}-to-{quadrant} join retained a cap at ({x},{y})");
                Assert.That(plan[x, y].Surface,
                    Is.Not.EqualTo(HybridWallRasterSurface.OutlineAntialias),
                    $"projected {ray}-to-{quadrant} join retained antialias at ({x},{y})");
            }
        });
    }

    [Test]
    public void DoubledPlanChangesOnlyTopWidthRecipe()
    {
        HybridWallRasterPlan standard = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.East | HybridWallRayMask.West,
            includeOutline: false);
        HybridWallRasterPlan doubled = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.East | HybridWallRayMask.West,
            doubled: true,
            includeOutline: false);

        Assert.Multiple(() =>
        {
            Assert.That(standard.TopWidth, Is.EqualTo(7));
            Assert.That(doubled.TopWidth, Is.EqualTo(14));
            Assert.That(doubled.Count(HybridWallRasterSurface.Top), Is.EqualTo(60 * 14));
            Assert.That(doubled.Count(HybridWallRasterSurface.Front), Is.EqualTo(60 * 22));
        });
    }

    [TestCase(HybridWallQuadrant.NorthEast, HybridWallRayMask.West | HybridWallRayMask.South)]
    [TestCase(HybridWallQuadrant.NorthWest, HybridWallRayMask.East | HybridWallRayMask.South)]
    [TestCase(HybridWallQuadrant.SouthEast, HybridWallRayMask.West | HybridWallRayMask.North)]
    [TestCase(HybridWallQuadrant.SouthWest, HybridWallRayMask.East | HybridWallRayMask.North)]
    public void RegularApertureUsesOnlyRaysThatVisiblyLeaveItsCorner(
        HybridWallQuadrant quadrant,
        HybridWallRayMask expected)
    {
        Assert.That(
            HybridWallRegularApertureRecipe.EligibleRays(quadrant, (HybridWallRayMask)15),
            Is.EqualTo(expected));
    }

    [TestCase(HybridWallQuadrant.NorthEast, HybridWallRayMask.West, 0)]
    [TestCase(HybridWallQuadrant.SouthEast, HybridWallRayMask.West, 0)]
    [TestCase(HybridWallQuadrant.NorthWest, HybridWallRayMask.East, 59)]
    [TestCase(HybridWallQuadrant.SouthWest, HybridWallRayMask.East, 59)]
    public void HorizontalContactStopsAtNativeBoundaryWithoutAnInteriorMiter(
        HybridWallQuadrant quadrant,
        HybridWallRayMask ray,
        int boundaryX)
    {
        IReadOnlyList<HybridWallRasterWrite> writes = HybridWallRegularApertureRecipe.Compile(
            quadrant,
            ray,
            HybridWallRayMask.None,
            HybridWallRayMask.None);
        Assert.Multiple(() =>
        {
            Assert.That(writes.Any(write => write.X == boundaryX && write.Sample.IsStructural), Is.True);
            Assert.That(writes.Any(write => write.X is > 0 and < 59 && write.Sample.IsStructural), Is.False,
                "the Thin recipe must stop at the native perimeter; the augmented Core arm owns the interior");
        });
    }

    [TestCase(HybridWallQuadrant.NorthWest, HybridWallRayMask.South, 0)]
    [TestCase(HybridWallQuadrant.NorthEast, HybridWallRayMask.South, 0)]
    [TestCase(HybridWallQuadrant.SouthWest, HybridWallRayMask.North, 59)]
    [TestCase(HybridWallQuadrant.SouthEast, HybridWallRayMask.North, 59)]
    public void VerticalContactStopsAtNativeBoundaryWithoutAnInteriorMiter(
        HybridWallQuadrant quadrant,
        HybridWallRayMask ray,
        int boundaryY)
    {
        IReadOnlyList<HybridWallRasterWrite> writes = HybridWallRegularApertureRecipe.Compile(
            quadrant,
            ray,
            HybridWallRayMask.None,
            HybridWallRayMask.None);

        Assert.Multiple(() =>
        {
            Assert.That(writes.Any(write => write.Y == boundaryY && write.Sample.IsStructural), Is.True);
            Assert.That(writes.Any(write => write.Y is > 0 and < 59 && write.Sample.IsStructural), Is.False,
                "the Thin recipe must not draw a diagonal or stair-step run inside the native tile");
        });
    }

    [Test]
    public void RegularHybridUsesOnePaddedUnionAtNativeTexelDensity()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HybridRegularRasterCompositor.CanvasSize, Is.EqualTo(120));
            Assert.That(HybridRegularRasterCompositor.Padding, Is.EqualTo(30));
        });
    }

    [Test]
    public void RegularDoorContactUsesTheSameAugmentedLinkedStateAsAWallContact()
    {
        var alpha = new byte[60 * 60];
        var wallCorner = new HybridWallCornerRaster(
            HybridWallQuadrant.NorthEast,
            HybridWallRayMask.West,
            HybridWallRayMask.None,
            HybridWallRayMask.None);
        var doorCorner = new HybridWallCornerRaster(
            HybridWallQuadrant.NorthEast,
            HybridWallRayMask.West,
            HybridWallRayMask.None,
            HybridWallRayMask.West);

        Assert.That(
            HybridRegularRasterCompositor.Compile(alpha, alpha, new[] { doorCorner }).TransitionLinkIndex,
            Is.EqualTo(HybridRegularRasterCompositor.Compile(alpha, alpha, new[] { wallCorner }).TransitionLinkIndex));
    }

    [Test]
    public void RegularDoorContactCachesOnlyItsSquareBoundaryAndThreePixelFixedFrame()
    {
        IReadOnlyList<HybridWallRasterWrite> writes = HybridWallRegularApertureRecipe.Compile(
            HybridWallQuadrant.NorthEast,
            HybridWallRayMask.West,
            HybridWallRayMask.None,
            HybridWallRayMask.West);
        int[] outerStructuralColumns = writes
            .Where(write => write.X < 0 && write.Sample.IsStructural)
            .Select(write => write.X)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(outerStructuralColumns, Is.EqualTo(new[] { -2, -1 }),
                "pixel zero is the shared vertex; only two additional pixels may extend into the door span");
            Assert.That(writes.Where(write => write.X < 0 && write.Sample.IsStructural)
                    .All(write => write.Sample.Surface == HybridWallRasterSurface.Top), Is.True,
                "a regular wall body is the jamb; its three-pixel gutter frame may not add a hanging facade");
            Assert.That(writes.Any(write => write.X == 0 && write.Sample.IsStructural), Is.True,
                "the fixed frame must meet the regular wall exactly at its tile boundary");
            Assert.That(writes.Any(write => write.X is > 0 and < 59 && write.Sample.IsStructural), Is.False,
                "a door contact may not cache a miter inside the regular tile");
            Assert.That(writes.Any(write => write.X == -3 && write.Sample.IsStructural), Is.False,
                "the cached regular union must never fill the moving door span beyond its three-pixel frame");
        });
    }

    [TestCase(HybridWallQuadrant.NorthEast, HybridWallRayMask.South, -2, -1)]
    [TestCase(HybridWallQuadrant.SouthEast, HybridWallRayMask.North, 60, 61)]
    public void VerticalRegularDoorContactAddsOnlyThreeTopPixelsOutsideTheNativeWall(
        HybridWallQuadrant quadrant,
        HybridWallRayMask ray,
        int minOuterY,
        int maxOuterY)
    {
        IReadOnlyList<HybridWallRasterWrite> writes = HybridWallRegularApertureRecipe.Compile(
            quadrant,
            ray,
            HybridWallRayMask.None,
            ray);
        HybridWallRasterWrite[] outer = writes
            .Where(write => write.Y >= minOuterY && write.Y <= maxOuterY && write.Sample.IsStructural)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(outer, Is.Not.Empty);
            Assert.That(outer.All(write => write.Sample.Surface == HybridWallRasterSurface.Top), Is.True,
                "the regular body/miter supplies the jamb; no gutter side extrusion is allowed");
            Assert.That(outer.Select(write => write.Y).Distinct(),
                Is.EqualTo(Enumerable.Range(minOuterY, maxOuterY - minOuterY + 1)));
        });
    }

    [TestCase(HybridWallRayMask.East, 30, 32)]
    [TestCase(HybridWallRayMask.West, 28, 30)]
    public void HorizontalDoorEndpointKeepsOnlyItsThreePixelFrameAndLateralFacade(
        HybridWallRayMask ray,
        int expectedMinX,
        int expectedMaxX)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            ray,
            includeOutline: false,
            doorRays: ray);

        int[] topColumns = Enumerable.Range(0, HybridWallRasterCompiler.Size)
            .Where(x => plan[x, 38].Surface == HybridWallRasterSurface.Top)
            .ToArray();
        int[] facadeColumns = Enumerable.Range(0, HybridWallRasterCompiler.Size)
            .Where(x => plan[x, 37].Surface == HybridWallRasterSurface.Front)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(topColumns, Is.EqualTo(Enumerable.Range(expectedMinX, 3)));
            Assert.That(facadeColumns, Is.EqualTo(Enumerable.Range(expectedMinX, 3)),
                "the visible facade may be only as wide as the fixed frame");
            Assert.That(plan.Count(HybridWallRasterSurface.WestSide), Is.Zero,
                "a horizontal door frame must not grow a longitudinal west cap");
            Assert.That(plan.Count(HybridWallRasterSurface.EastSide), Is.Zero,
                "a horizontal door frame must not grow a longitudinal east cap");
        });
    }

    [TestCase(HybridWallRayMask.North, 30, 32)]
    [TestCase(HybridWallRayMask.South, 28, 30)]
    public void VerticalDoorEndpointKeepsOnlyItsThreePixelFrameAndLateralSides(
        HybridWallRayMask ray,
        int expectedMinY,
        int expectedMaxY)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            ray,
            includeOutline: false,
            doorRays: ray);

        int[] topRows = Enumerable.Range(0, HybridWallRasterCompiler.Size)
            .Where(y => plan[30, y].Surface == HybridWallRasterSurface.Top)
            .ToArray();
        int[] westSideRows = Enumerable.Range(0, HybridWallRasterCompiler.Size)
            .Where(y => plan[26, y].Surface == HybridWallRasterSurface.WestSide)
            .ToArray();
        int[] eastSideRows = Enumerable.Range(0, HybridWallRasterCompiler.Size)
            .Where(y => plan[34, y].Surface == HybridWallRasterSurface.EastSide)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(topRows, Is.EqualTo(Enumerable.Range(expectedMinY, 3)));
            Assert.That(westSideRows, Is.EqualTo(Enumerable.Range(expectedMinY, 3)));
            Assert.That(eastSideRows, Is.EqualTo(Enumerable.Range(expectedMinY, 3)));
            Assert.That(plan.Count(HybridWallRasterSurface.Front), Is.Zero,
                "a vertical door frame must not grow a longitudinal projected facade");
        });
    }

    [Test]
    public void MixedThinWallAndDoorEndpointStopsAfterTheWallHalfAndThreePixelFrame()
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.East | HybridWallRayMask.West,
            includeOutline: false,
            doorRays: HybridWallRayMask.East);

        int[] topColumns = Enumerable.Range(0, HybridWallRasterCompiler.Size)
            .Where(x => plan[x, 38].Surface == HybridWallRasterSurface.Top)
            .ToArray();
        int[] facadeColumns = Enumerable.Range(0, HybridWallRasterCompiler.Size)
            .Where(x => plan[x, 37].Surface == HybridWallRasterSurface.Front)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(topColumns, Is.EqualTo(Enumerable.Range(0, 33)));
            Assert.That(facadeColumns, Is.EqualTo(Enumerable.Range(0, 33)));
            Assert.That(plan[33, 37].Surface, Is.EqualTo(HybridWallRasterSurface.Transparent),
                "the cached endpoint must leave the moving leaf span open");
        });
    }

    [TestCase(HybridWallQuadrant.NorthEast, HybridWallRayMask.South, -30, 0)]
    [TestCase(HybridWallQuadrant.SouthEast, HybridWallRayMask.North, 59, 30)]
    public void VerticalRegularContactContinuesTheOmittedVertexPartitionPhase(
        HybridWallQuadrant quadrant,
        HybridWallRayMask ray,
        int firstRow,
        int firstSourceRow)
    {
        IReadOnlyList<HybridWallRasterWrite> writes = HybridWallRegularApertureRecipe.Compile(
            quadrant,
            ray,
            HybridWallRayMask.None,
            HybridWallRayMask.None);
        byte[] sourceRows = Enumerable.Range(firstRow, 30)
            .Select(row => writes.First(write =>
                write.Y == row && write.Sample.Surface == HybridWallRasterSurface.Top).Sample.SourceY)
            .ToArray();

        Assert.That(sourceRows,
            Is.EqualTo(Enumerable.Range(firstSourceRow, 30).Select(value => (byte)value)),
            "the regular-owned half must be the exact missing half of the ordinary vertex raster");
    }

    [Test]
    public void DoorSeamAndMaterialDetailsAreClippedToStructuralPixels()
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.East | HybridWallRayMask.West);
        IReadOnlyList<HybridWallTreatmentMark> wood = HybridWallSurfaceTreatmentRecipe.Compile(
            plan,
            ThinWallMaterialFamily.Wood,
            ThinWallDamageGrade.None,
            door: true);
        IReadOnlyList<HybridWallTreatmentMark> metal = HybridWallSurfaceTreatmentRecipe.Compile(
            plan,
            ThinWallMaterialFamily.Metal,
            ThinWallDamageGrade.None,
            door: true);

        Assert.Multiple(() =>
        {
            Assert.That(wood.Count(mark => mark == HybridWallTreatmentMark.DoorSeam), Is.GreaterThan(20));
            Assert.That(wood.Count(mark => mark == HybridWallTreatmentMark.DetailDark), Is.GreaterThan(0));
            Assert.That(metal.Count(mark => mark == HybridWallTreatmentMark.DetailLight), Is.GreaterThan(0));
            for (int index = 0; index < plan.Pixels.Count; index++)
            {
                if (!plan.Pixels[index].IsStructural)
                {
                    Assert.That(wood[index], Is.EqualTo(HybridWallTreatmentMark.None));
                    Assert.That(metal[index], Is.EqualTo(HybridWallTreatmentMark.None));
                }
            }
        });
    }

    [TestCase(HybridWallRayMask.East | HybridWallRayMask.West)]
    [TestCase(HybridWallRayMask.North | HybridWallRayMask.South)]
    public void ClosedDoorHasPairedLeafHardwareWithoutMarkingOrdinaryWalls(HybridWallRayMask rays)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(rays, includeOutline: false);
        IReadOnlyList<HybridWallTreatmentMark> door = HybridWallSurfaceTreatmentRecipe.Compile(
            plan,
            ThinWallMaterialFamily.Wood,
            ThinWallDamageGrade.None,
            door: true);
        IReadOnlyList<HybridWallTreatmentMark> wall = HybridWallSurfaceTreatmentRecipe.Compile(
            plan,
            ThinWallMaterialFamily.Wood,
            ThinWallDamageGrade.None,
            door: false);

        Assert.Multiple(() =>
        {
            Assert.That(door.Count(mark => mark == HybridWallTreatmentMark.DoorHardwareDark),
                Is.GreaterThanOrEqualTo(4));
            Assert.That(door.Count(mark => mark == HybridWallTreatmentMark.DoorHardwareLight),
                Is.GreaterThanOrEqualTo(2));
            Assert.That(wall.Any(mark => mark is HybridWallTreatmentMark.DoorHardwareDark or
                HybridWallTreatmentMark.DoorHardwareLight), Is.False);
        });
    }

    [TestCase(ThinWallMaterialFamily.Wood)]
    [TestCase(ThinWallMaterialFamily.Metal)]
    public void PanelDetailNeverDrawsAFullHeightVertexPartitionSeam(ThinWallMaterialFamily family)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.East | HybridWallRayMask.West,
            includeOutline: false);
        IReadOnlyList<HybridWallTreatmentMark> marks = HybridWallSurfaceTreatmentRecipe.Compile(
            plan,
            family,
            ThinWallDamageGrade.None,
            door: false);

        int[] partitionRows = Enumerable.Range(8, 22)
            .Where(y => marks[y * HybridWallRasterPlan.Size + 29] == HybridWallTreatmentMark.DetailDark)
            .ToArray();

        Assert.That(partitionRows.Length, Is.LessThan(8),
            "OSB and plate divisions must be staggered details, not a full-height sprite boundary");
    }

    [TestCase(ThinWallMaterialFamily.Stone)]
    [TestCase(ThinWallMaterialFamily.Wood)]
    [TestCase(ThinWallMaterialFamily.Metal)]
    public void SevereDamageCombinesRecessHighlightAndTopPlaneBreakup(ThinWallMaterialFamily family)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.East | HybridWallRayMask.West,
            includeOutline: false);
        IReadOnlyList<HybridWallTreatmentMark> marks = HybridWallSurfaceTreatmentRecipe.Compile(
            plan,
            family,
            ThinWallDamageGrade.Severe,
            door: false);

        int[] damaged = Enumerable.Range(0, marks.Count)
            .Where(index => marks[index] is HybridWallTreatmentMark.DamageDark or
                HybridWallTreatmentMark.DamageChip or HybridWallTreatmentMark.DamageLight or
                HybridWallTreatmentMark.DamageVoid)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(damaged.Any(index => marks[index] == HybridWallTreatmentMark.DamageDark), Is.True);
            Assert.That(damaged.Any(index => marks[index] == HybridWallTreatmentMark.DamageLight), Is.True,
                "material damage needs an exposed edge, not only a dark symbol");
            Assert.That(damaged.Any(index => plan.Pixels[index].Surface == HybridWallRasterSurface.Top), Is.True,
                "severe damage must disturb the dark top as well as the facade");
            Assert.That(damaged.All(index => plan.Pixels[index].IsStructural), Is.True);
            int[] voids = damaged.Where(index => marks[index] == HybridWallTreatmentMark.DamageVoid).ToArray();
            Assert.That(voids.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(voids.Any(left => voids.Any(right => left != right &&
                Math.Abs(left % HybridWallRasterPlan.Size - right % HybridWallRasterPlan.Size) <= 1 &&
                Math.Abs(left / HybridWallRasterPlan.Size - right / HybridWallRasterPlan.Size) <= 1)), Is.True,
                "severe damage must clear an adjacent structural notch rather than one isolated alpha pinhole");
        });
    }

    [TestCase(ThinWallMaterialFamily.Stone)]
    [TestCase(ThinWallMaterialFamily.Wood)]
    [TestCase(ThinWallMaterialFamily.Metal)]
    public void ModerateAndHeavyDamageMeetOrdinaryZoomCoverageThresholds(ThinWallMaterialFamily family)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.East | HybridWallRayMask.West,
            includeOutline: false);
        IReadOnlyList<HybridWallTreatmentMark> moderate = HybridWallSurfaceTreatmentRecipe.Compile(
            plan, family, ThinWallDamageGrade.Moderate, door: false);
        IReadOnlyList<HybridWallTreatmentMark> heavy = HybridWallSurfaceTreatmentRecipe.Compile(
            plan, family, ThinWallDamageGrade.Heavy, door: false);

        int[] moderateFace = Enumerable.Range(0, moderate.Count)
            .Where(index => plan.Pixels[index].Surface is HybridWallRasterSurface.Front or
                HybridWallRasterSurface.WestSide or HybridWallRasterSurface.EastSide)
            .Where(index => IsDamage(moderate[index]))
            .ToArray();
        int moderateDark = moderateFace.Count(index => moderate[index] == HybridWallTreatmentMark.DamageDark);
        int moderateHighlight = moderateFace.Count(index => moderate[index] is
            HybridWallTreatmentMark.DamageLight or HybridWallTreatmentMark.DamageChip);
        int moderateWidth = moderateFace.Max(index => index % HybridWallRasterPlan.Size) -
                            moderateFace.Min(index => index % HybridWallRasterPlan.Size) + 1;
        int moderateHeight = moderateFace.Max(index => index / HybridWallRasterPlan.Size) -
                             moderateFace.Min(index => index / HybridWallRasterPlan.Size) + 1;
        int moderateTotal = moderate.Count(IsDamage);
        int heavyTotal = heavy.Count(IsDamage);
        int heavyFaceComponents = CountComponents(heavy, plan, includeTop: false);

        Assert.Multiple(() =>
        {
            Assert.That(moderateDark, Is.GreaterThanOrEqualTo(12));
            Assert.That(moderateHighlight, Is.GreaterThanOrEqualTo(4));
            Assert.That(moderateWidth, Is.GreaterThanOrEqualTo(8));
            Assert.That(moderateHeight, Is.GreaterThanOrEqualTo(3));
            Assert.That(heavyTotal, Is.GreaterThanOrEqualTo((int)Math.Ceiling(moderateTotal * 1.6)));
            Assert.That(heavyFaceComponents, Is.GreaterThanOrEqualTo(2));
            Assert.That(Enumerable.Range(0, heavy.Count).Any(index =>
                plan.Pixels[index].Surface == HybridWallRasterSurface.Top && IsDamage(heavy[index])), Is.True,
                "heavy damage must add a visible top-plane defect");
        });

        static bool IsDamage(HybridWallTreatmentMark mark) => mark is
            HybridWallTreatmentMark.DamageDark or HybridWallTreatmentMark.DamageChip or
            HybridWallTreatmentMark.DamageLight or HybridWallTreatmentMark.DamageVoid;

        static int CountComponents(
            IReadOnlyList<HybridWallTreatmentMark> marks,
            HybridWallRasterPlan raster,
            bool includeTop)
        {
            var remaining = Enumerable.Range(0, marks.Count)
                .Where(index => IsDamage(marks[index]))
                .Where(index => includeTop || raster.Pixels[index].Surface != HybridWallRasterSurface.Top)
                .ToHashSet();
            int count = 0;
            while (remaining.Count > 0)
            {
                count++;
                int first = remaining.First();
                remaining.Remove(first);
                var pending = new Queue<int>();
                pending.Enqueue(first);
                while (pending.Count > 0)
                {
                    int current = pending.Dequeue();
                    int x = current % HybridWallRasterPlan.Size;
                    int y = current / HybridWallRasterPlan.Size;
                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        int candidateX = x + offsetX;
                        int candidateY = y + offsetY;
                        if (candidateX < 0 || candidateX >= HybridWallRasterPlan.Size ||
                            candidateY < 0 || candidateY >= HybridWallRasterPlan.Size)
                        {
                            continue;
                        }
                        int candidate = candidateY * HybridWallRasterPlan.Size + candidateX;
                        if (remaining.Remove(candidate))
                        {
                            pending.Enqueue(candidate);
                        }
                    }
                }
            }
            return count;
        }
    }

    [Test]
    public void DamageRecessAndExposedEdgeContrastSurviveCoreStuffTinting()
    {
        var source = new Color32(120, 100, 80, 233);

        Assert.Multiple(() =>
        {
            Assert.That(HybridWallTreatmentColorizer.Apply(source, HybridWallTreatmentMark.DamageDark),
                Is.EqualTo(new Color32(30, 25, 20, 233)),
                "the coherent recess must remain dark after ordinary-zoom filtering");
            Assert.That(HybridWallTreatmentColorizer.Apply(source, HybridWallTreatmentMark.DamageChip),
                Is.EqualTo(new Color32(12, 10, 8, 233)));
            Assert.That(HybridWallTreatmentColorizer.Apply(source, HybridWallTreatmentMark.DamageLight),
                Is.EqualTo(new Color32(208, 201, 194, 233)),
                "the exposed edge must remain a visible counterpart to the recess");
        });
    }

    [TestCase(ThinWallMaterialFamily.Stone)]
    [TestCase(ThinWallMaterialFamily.Wood)]
    [TestCase(ThinWallMaterialFamily.Metal)]
    public void StuffTintedDamageGradesRemainDistinctAfterOrdinaryZoomDownsampling(
        ThinWallMaterialFamily family)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.East | HybridWallRayMask.West,
            includeOutline: false);
        Color32 stuffTint = family switch
        {
            ThinWallMaterialFamily.Wood => new Color32(151, 104, 62, 255),
            ThinWallMaterialFamily.Metal => new Color32(112, 126, 133, 255),
            _ => new Color32(126, 107, 146, 255),
        };
        float[] clean = Downsample(ThinWallDamageGrade.None);
        float[] moderate = Downsample(ThinWallDamageGrade.Moderate);
        float[] heavy = Downsample(ThinWallDamageGrade.Heavy);
        int moderateChanged = Changed(clean, moderate, minimumDelta: 10f);
        int heavyChanged = Changed(clean, heavy, minimumDelta: 10f);
        int gradeChanged = Changed(moderate, heavy, minimumDelta: 10f);
        float moderateContrast = Difference(clean, moderate);
        float heavyContrast = Difference(clean, heavy);

        Assert.Multiple(() =>
        {
            Assert.That(moderateChanged, Is.GreaterThanOrEqualTo(8));
            Assert.That(heavyChanged, Is.GreaterThan(moderateChanged));
            Assert.That(gradeChanged, Is.GreaterThanOrEqualTo(8));
            Assert.That(heavyContrast, Is.GreaterThan(moderateContrast * 1.35f));
        });

        float[] Downsample(ThinWallDamageGrade grade)
        {
            IReadOnlyList<HybridWallTreatmentMark> marks = HybridWallSurfaceTreatmentRecipe.Compile(
                plan, family, grade, door: false);
            var result = new float[30 * 30];
            for (int targetY = 0; targetY < 30; targetY++)
            for (int targetX = 0; targetX < 30; targetX++)
            {
                float sum = 0f;
                for (int offsetY = 0; offsetY < 2; offsetY++)
                for (int offsetX = 0; offsetX < 2; offsetX++)
                {
                    int sourceX = targetX * 2 + offsetX;
                    int sourceY = targetY * 2 + offsetY;
                    int index = sourceY * HybridWallRasterPlan.Size + sourceX;
                    Color32 source = plan.Pixels[index].IsStructural ? stuffTint : new Color32(0, 0, 0, 0);
                    Color32 treated = HybridWallTreatmentColorizer.Apply(source, marks[index]);
                    sum += treated.a == 0 ? 0f : (treated.r + treated.g + treated.b) / 3f;
                }
                result[targetY * 30 + targetX] = sum / 4f;
            }
            return result;
        }

        static int Changed(IReadOnlyList<float> left, IReadOnlyList<float> right, float minimumDelta) =>
            Enumerable.Range(0, left.Count).Count(index => Math.Abs(left[index] - right[index]) >= minimumDelta);

        static float Difference(IReadOnlyList<float> left, IReadOnlyList<float> right) =>
            Enumerable.Range(0, left.Count).Sum(index => Math.Abs(left[index] - right[index]));
    }

    [Test]
    public void SevereMaterialDamageUsesIrregularNonSymbolicCenters()
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.East | HybridWallRayMask.West,
            includeOutline: false);

        IReadOnlyList<HybridWallTreatmentMark> stone = HybridWallSurfaceTreatmentRecipe.Compile(
            plan, ThinWallMaterialFamily.Stone, ThinWallDamageGrade.Severe, door: false);
        IReadOnlyList<HybridWallTreatmentMark> wood = HybridWallSurfaceTreatmentRecipe.Compile(
            plan, ThinWallMaterialFamily.Wood, ThinWallDamageGrade.Severe, door: false);
        IReadOnlyList<HybridWallTreatmentMark> metal = HybridWallSurfaceTreatmentRecipe.Compile(
            plan, ThinWallMaterialFamily.Metal, ThinWallDamageGrade.Severe, door: false);

        Assert.Multiple(() =>
        {
            Assert.That(stone.Count(mark => mark == HybridWallTreatmentMark.DamageChip), Is.GreaterThanOrEqualTo(9),
                "severe masonry needs a readable irregular chip rather than hairline cracks only");
            Assert.That(stone.Count(mark => mark == HybridWallTreatmentMark.DamageVoid), Is.GreaterThan(0));
            Assert.That(wood.Count(mark => mark == HybridWallTreatmentMark.DamageVoid), Is.GreaterThan(0));
            Assert.That(wood.Count(mark => mark == HybridWallTreatmentMark.DamageDark), Is.GreaterThan(8));
            Assert.That(wood.Count(mark => mark == HybridWallTreatmentMark.DamageLight), Is.GreaterThan(4),
                "wood uses an offset splinter edge instead of a symmetric plus sign");
            Assert.That(metal.Count(mark => mark == HybridWallTreatmentMark.DamageVoid), Is.GreaterThan(0));
            Assert.That(metal.Count(mark => mark == HybridWallTreatmentMark.DamageDark), Is.GreaterThan(12),
                "the dent center must be recessed rather than left empty like a ring");
            Assert.That(metal.Count(mark => mark == HybridWallTreatmentMark.DamageLight), Is.GreaterThan(2),
                "the dent highlight must be asymmetric and one-sided");
        });
    }

    [Test]
    public void SevereDamageVoidClearsDiffuseAndStuffMaskCoverageTogether()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HybridWallTreatmentCoverage.Alpha(255, HybridWallTreatmentMark.DamageVoid), Is.Zero);
            Assert.That(HybridWallTreatmentCoverage.Alpha(173, HybridWallTreatmentMark.DamageVoid), Is.Zero);
            Assert.That(HybridWallTreatmentCoverage.Alpha(173, HybridWallTreatmentMark.DamageChip), Is.EqualTo(173));
            Assert.That(HybridWallTreatmentCoverage.Alpha(173, HybridWallTreatmentMark.DamageLight), Is.EqualTo(173));
        });
    }

    [Test]
    public void MaterialSpecificDamageIsProgressiveAndNeverLeavesTheWallRaster()
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            HybridWallRayMask.North | HybridWallRayMask.South);
        foreach (ThinWallMaterialFamily family in Enum.GetValues(typeof(ThinWallMaterialFamily)))
        {
            int moderate = DamageCount(ThinWallDamageGrade.Moderate);
            int heavy = DamageCount(ThinWallDamageGrade.Heavy);
            int severe = DamageCount(ThinWallDamageGrade.Severe);
            Assert.Multiple(() =>
            {
                Assert.That(moderate, Is.GreaterThan(0), family.ToString());
                Assert.That(heavy, Is.GreaterThan(moderate), family.ToString());
                Assert.That(severe, Is.GreaterThan(heavy), family.ToString());
            });

            int DamageCount(ThinWallDamageGrade grade)
            {
                IReadOnlyList<HybridWallTreatmentMark> marks = HybridWallSurfaceTreatmentRecipe.Compile(
                    plan,
                    family,
                    grade,
                    door: false);
                for (int index = 0; index < marks.Count; index++)
                {
                    if (marks[index] is HybridWallTreatmentMark.DamageDark or HybridWallTreatmentMark.DamageChip or
                        HybridWallTreatmentMark.DamageLight or HybridWallTreatmentMark.DamageVoid)
                    {
                        Assert.That(plan.Pixels[index].IsStructural, Is.True);
                    }
                }
                return marks.Count(mark => mark is HybridWallTreatmentMark.DamageDark or HybridWallTreatmentMark.DamageChip or
                    HybridWallTreatmentMark.DamageLight or HybridWallTreatmentMark.DamageVoid);
            }
        }
    }

    [TestCase(HybridWallRayMask.East | HybridWallRayMask.West)]
    [TestCase(HybridWallRayMask.North | HybridWallRayMask.South)]
    public void ModerateStoneDamageUsesOnlyLocalizedMarksWithoutLongDiagonalBands(HybridWallRayMask rays)
    {
        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(rays);
        IReadOnlyList<HybridWallTreatmentMark> marks = HybridWallSurfaceTreatmentRecipe.Compile(
            plan,
            ThinWallMaterialFamily.Stone,
            ThinWallDamageGrade.Moderate,
            door: false);
        var remaining = Enumerable.Range(0, marks.Count)
            .Where(index => marks[index] is HybridWallTreatmentMark.DamageDark or HybridWallTreatmentMark.DamageChip or
                HybridWallTreatmentMark.DamageLight or HybridWallTreatmentMark.DamageVoid)
            .ToHashSet();
        var components = new List<HashSet<int>>();
        while (remaining.Count > 0)
        {
            int start = remaining.First();
            var component = new HashSet<int> { start };
            var pending = new Queue<int>();
            pending.Enqueue(start);
            remaining.Remove(start);
            while (pending.Count > 0)
            {
                int current = pending.Dequeue();
                int x = current % HybridWallRasterPlan.Size;
                int y = current / HybridWallRasterPlan.Size;
                for (int offsetY = -2; offsetY <= 2; offsetY++)
                for (int offsetX = -2; offsetX <= 2; offsetX++)
                {
                    if (offsetX != 0 || offsetY != 0)
                    {
                        Visit(x + offsetX, y + offsetY);
                    }
                }

                void Visit(int candidateX, int candidateY)
                {
                    if (candidateX < 0 || candidateX >= HybridWallRasterPlan.Size ||
                        candidateY < 0 || candidateY >= HybridWallRasterPlan.Size)
                    {
                        return;
                    }

                    int candidate = candidateY * HybridWallRasterPlan.Size + candidateX;
                    if (remaining.Remove(candidate))
                    {
                        component.Add(candidate);
                        pending.Enqueue(candidate);
                    }
                }
            }
            components.Add(component);
        }

        Assert.That(components, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            foreach (HashSet<int> component in components)
            {
                int[] xs = component.Select(index => index % HybridWallRasterPlan.Size).ToArray();
                int[] ys = component.Select(index => index / HybridWallRasterPlan.Size).ToArray();
                Assert.That(xs.Max() - xs.Min() + 1, Is.LessThanOrEqualTo(12), "damage component width");
                Assert.That(ys.Max() - ys.Min() + 1, Is.LessThanOrEqualTo(12), "damage component height");
            }
        });
    }

    private static bool StructuralPixelsAreConnected(HybridWallRasterPlan plan)
    {
        int start = Enumerable.Range(0, plan.Pixels.Count)
            .First(index => plan.Pixels[index].IsStructural);
        var visited = new HashSet<int> { start };
        var pending = new Queue<int>();
        pending.Enqueue(start);
        while (pending.Count > 0)
        {
            int current = pending.Dequeue();
            int x = current % HybridWallRasterPlan.Size;
            int y = current / HybridWallRasterPlan.Size;
            Visit(x - 1, y);
            Visit(x + 1, y);
            Visit(x, y - 1);
            Visit(x, y + 1);
        }

        return visited.Count == plan.Pixels.Count(pixel => pixel.IsStructural);

        void Visit(int x, int y)
        {
            if (x < 0 || x >= HybridWallRasterPlan.Size || y < 0 || y >= HybridWallRasterPlan.Size)
            {
                return;
            }

            int index = y * HybridWallRasterPlan.Size + x;
            if (plan.Pixels[index].IsStructural && visited.Add(index))
            {
                pending.Enqueue(index);
            }
        }
    }
}
