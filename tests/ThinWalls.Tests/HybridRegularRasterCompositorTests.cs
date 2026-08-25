using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ThinWalls.Geometry;
using ThinWalls.Rendering;
using UnityEngine;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class HybridRegularRasterCompositorTests
{
    [Test]
    public void TransitionIndexUsesTheExactVanillaDirectionBitsForEveryOutgoingContact()
    {
        var alpha = new byte[60 * 60];
        HybridRegularCompositePlan plan = HybridRegularRasterCompositor.Compile(
            alpha,
            alpha,
            new[]
            {
                new HybridWallCornerRaster(
                    HybridWallQuadrant.NorthEast,
                    HybridWallRayMask.West | HybridWallRayMask.South,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None),
                new HybridWallCornerRaster(
                    HybridWallQuadrant.SouthWest,
                    HybridWallRayMask.East | HybridWallRayMask.North,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None),
            },
            originalLinkIndex: 0);

        Assert.That(plan.TransitionLinkIndex, Is.EqualTo(15),
            "the transition donor must be Core's N+E+S+W state, not an inferred surface or connector");
    }

    [Test]
    public void EveryNativePixelOutsideTheDeclaredApertureRemainsByteIdentical()
    {
        var native = new Color32[60 * 60];
        var nativeMask = new Color32[native.Length];
        var alpha = new byte[native.Length];
        var donor = new Color32[native.Length];
        var donorMask = new Color32[native.Length];
        var donorAlpha = new byte[native.Length];
        for (int index = 0; index < native.Length; index++)
        {
            native[index] = new Color32(
                (byte)(index % 79),
                (byte)((index * 3) % 80),
                (byte)((index * 7) % 81),
                (byte)(index % 5 == 0 ? 0 : 255));
            nativeMask[index] = new Color32(
                (byte)(255 - native[index].r),
                (byte)(255 - native[index].g),
                (byte)(255 - native[index].b),
                native[index].a);
            alpha[index] = native[index].a;
            donor[index] = native[index];
            donor[index].a = 255;
            donorMask[index] = nativeMask[index];
            donorMask[index].a = 255;
            donorAlpha[index] = 255;
        }

        HybridRegularCompositePlan plan = HybridRegularRasterCompositor.Compile(
            alpha,
            donorAlpha,
            new[]
            {
                new HybridWallCornerRaster(
                    HybridWallQuadrant.NorthEast,
                    HybridWallRayMask.West,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None),
            });
        Color32[] output = HybridRegularRasterCompositor.Compose(
            native,
            donor,
            plan,
            _ => new Color32(201, 202, 203, 255),
            (_, _) => new Color32(9, 9, 9, 255));
        Color32[] outputMask = HybridRegularRasterCompositor.Compose(
            nativeMask,
            donorMask,
            plan,
            _ => new Color32(101, 102, 103, 255),
            (_, _) => new Color32(19, 19, 19, 255));

        Assert.Multiple(() =>
        {
            for (int y = 0; y < 60; y++)
            for (int x = 0; x < 60; x++)
            {
                int canvas = (y + HybridRegularRasterCompositor.Padding) *
                             HybridRegularRasterCompositor.CanvasSize + x +
                             HybridRegularRasterCompositor.Padding;
                if (plan.Aperture[canvas])
                {
                    continue;
                }

                Assert.That(output[canvas], Is.EqualTo(native[y * 60 + x]),
                    $"native RGBA changed outside the aperture at ({x},{y})");
                Assert.That(outputMask[canvas], Is.EqualTo(nativeMask[y * 60 + x]),
                    $"native mask RGBA changed outside the aperture at ({x},{y})");
            }
        });
    }

    [Test]
    public void StraightDonorOrdinaryArmStaysInsideNativeRegionAndThinArmStaysInGutter()
    {
        var original = new Color32[60 * 60];
        var transition = new Color32[original.Length];
        var originalAlpha = new byte[original.Length];
        var transitionAlpha = new byte[original.Length];
        for (int index = 0; index < original.Length; index++)
        {
            original[index] = new Color32(180, 170, 160, 0);
            transition[index] = new Color32(90, 80, 70, 255);
            transitionAlpha[index] = 255;
        }

        HybridRegularCompositePlan plan = HybridRegularRasterCompositor.Compile(
            originalAlpha,
            transitionAlpha,
            new[]
            {
                new HybridWallCornerRaster(
                    HybridWallQuadrant.NorthEast,
                    HybridWallRayMask.West,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None),
            },
            originalLinkIndex: 0);
        Color32[] output = HybridRegularRasterCompositor.Compose(
            original,
            transition,
            plan,
            _ => new Color32(201, 202, 203, 255),
            (_, _) => new Color32(9, 9, 9, 255));

        int thinGutter = CanvasIndex(-15, 3);
        int nativeOrdinaryArm = CanvasIndex(5, 30);
        int gutterOutsideThin = CanvasIndex(-15, 20);
        Assert.Multiple(() =>
        {
            Assert.That(HybridRegularRasterCompositor.CanvasSize, Is.EqualTo(120));
            Assert.That(HybridRegularRasterCompositor.Padding, Is.EqualTo(30));
            Assert.That(plan.Aperture[thinGutter], Is.True,
                "the measured seven-pixel Thin top must occupy the gutter");
            Assert.That(plan.Aperture[nativeOrdinaryArm], Is.True,
                "the straight-donor Core arm must remain ordinary-width inside its own tile");
            Assert.That(output[nativeOrdinaryArm], Is.EqualTo(new Color32(90, 80, 70, 255)),
                "the native-region arm must use the regular wall's straight-donor state");
            Assert.That(plan.Aperture[gutterOutsideThin], Is.False,
                "ordinary-width regular-wall pixels may never escape into the gutter");
        });
    }

    [Test]
    public void CompleteProjectedThinFacesAndSidesRemainInThePaddedUnion()
    {
        var alpha = new byte[60 * 60];
        Fill(alpha, (byte)255);
        HybridRegularCompositePlan horizontal = HybridRegularRasterCompositor.Compile(
            alpha,
            alpha,
            new[]
            {
                new HybridWallCornerRaster(
                    HybridWallQuadrant.NorthEast,
                    HybridWallRayMask.West,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None),
            });
        HybridRegularCompositePlan vertical = HybridRegularRasterCompositor.Compile(
            alpha,
            alpha,
            new[]
            {
                new HybridWallCornerRaster(
                    HybridWallQuadrant.NorthEast,
                    HybridWallRayMask.South,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None),
            });

        Assert.Multiple(() =>
        {
            Assert.That(horizontal.ThinAt(15, 20).Surface, Is.EqualTo(HybridWallRasterSurface.Front),
                "the 22-pixel horizontal facade below the native tile must not be clipped");
            Assert.That(horizontal.Aperture[20 * 120 + 15], Is.True);
            Assert.That(vertical.ThinAt(16, 15).Surface, Is.EqualTo(HybridWallRasterSurface.WestSide),
                "the 11-pixel north-south side beyond the native tile must not be clipped");
            Assert.That(vertical.Aperture[15 * 120 + 16], Is.True);
        });
    }

    [Test]
    public void CoreSemanticOwnershipDistinguishesAnOpaqueEndpointSideFromAnOpaqueJoinedArm()
    {
        CoreLinkedSemanticPlan isolated = CoreLinkedSemanticCompiler.Compile(0);
        CoreLinkedSemanticPlan westLinked = CoreLinkedSemanticCompiler.Compile(8);

        Assert.Multiple(() =>
        {
            Assert.That(isolated[5, 33].Surface, Is.EqualTo(HybridWallRasterSurface.WestSide));
            Assert.That(isolated[5, 33].OwnerRays, Is.EqualTo(HybridWallRayMask.West));
            Assert.That(westLinked[5, 33].Surface, Is.EqualTo(HybridWallRasterSurface.Top));
            Assert.That(westLinked[5, 33].OwnerRays, Is.EqualTo(HybridWallRayMask.West));
        });
    }

    [Test]
    public void CoreSemanticSeparatesProjectionDirectionFromTheArmThatCausedIt()
    {
        CoreLinkedSemanticPlan horizontal = CoreLinkedSemanticCompiler.Compile(10);
        CoreLinkedSemanticPlan southT = CoreLinkedSemanticCompiler.Compile(14);

        Assert.Multiple(() =>
        {
            Assert.That(horizontal[20, 10].Surface, Is.EqualTo(HybridWallRasterSurface.Front));
            Assert.That(horizontal[20, 10].OwnerRays.HasFlag(HybridWallRayMask.South), Is.True,
                "the visible facade still faces south");
            Assert.That(horizontal.CausalAt(20, 10), Is.EqualTo(HybridWallRayMask.None),
                "a surface direction must not masquerade as a newly added south arm");
            Assert.That(southT[20, 10].Surface, Is.EqualTo(HybridWallRasterSurface.Top));
            Assert.That(southT.CausalAt(20, 10), Is.EqualTo(HybridWallRayMask.South),
                "the new south arm must remain the explicit cause of its replacement top pixel");
        });
    }

    [Test]
    public void EachContactHalfArmHasExactlyOneRegularWallOwnerAndNoVertexDuplicate()
    {
        HybridWallQuadrant all = HybridWallQuadrant.NorthEast |
                                  HybridWallQuadrant.NorthWest |
                                  HybridWallQuadrant.SouthEast |
                                  HybridWallQuadrant.SouthWest;
        HybridWallRayMask rays = (HybridWallRayMask)15;

        Assert.Multiple(() =>
        {
            Assert.That(HybridRegularContactOwnership.RaysOwnedBy(
                    HybridWallQuadrant.NorthEast, all, rays),
                Is.EqualTo(HybridWallRayMask.West | HybridWallRayMask.South));
            Assert.That(HybridRegularContactOwnership.RaysOwnedBy(
                    HybridWallQuadrant.NorthWest, all, rays),
                Is.EqualTo(HybridWallRayMask.East));
            Assert.That(HybridRegularContactOwnership.RaysOwnedBy(
                    HybridWallQuadrant.SouthEast, all, rays),
                Is.EqualTo(HybridWallRayMask.North));
            Assert.That(HybridRegularContactOwnership.RaysOwnedBy(
                    HybridWallQuadrant.SouthWest, all, rays),
                Is.EqualTo(HybridWallRayMask.None));
            Assert.That(HybridRegularContactOwnership.AllRegularOwnedRays(all, rays), Is.EqualTo(rays),
                "the vertex renderer must omit every half-arm now owned by one padded regular replacement");
        });
    }

    [Test]
    public void AtlasSupportFailsClosedUnlessTheConcreteWallUsesTheMeasuredCoreLayout()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HybridWallAtlasSupport.IsSupported("Wall_Atlas_Bricks", 320, 320), Is.True);
            Assert.That(HybridWallAtlasSupport.IsSupported("Wall_Atlas_Planks", 320, 320), Is.True);
            Assert.That(HybridWallAtlasSupport.IsSupported("Wall_Atlas_Smooth", 320, 320), Is.True);
            Assert.That(HybridWallAtlasSupport.IsSupported("Unknown_Mod_Atlas", 320, 320), Is.False,
                "dimensions alone cannot prove Core's slot semantics");
            Assert.That(HybridWallAtlasSupport.IsSupported("Wall_Atlas_Bricks", 160, 160), Is.False);
            Assert.That(HybridWallAtlasSupport.IsSupported("Wall_Atlas_Bricks", 320, 160), Is.False);
            Assert.That(HybridWallAtlasSupport.IsSupported("Wall_Atlas_Bricks", 512, 512), Is.False);
        });
    }

    [Test]
    public void OpaqueContactBoundaryUsesStraightCoreDonorWhenAugmentedSurfaceDoesNotMatch()
    {
        var originalAlpha = new byte[60 * 60];
        var transitionAlpha = new byte[60 * 60];
        Fill(originalAlpha, (byte)255);
        Fill(transitionAlpha, (byte)255);
        var corners = new[]
        {
            new HybridWallCornerRaster(
                HybridWallQuadrant.NorthEast,
                HybridWallRayMask.West,
                HybridWallRayMask.None,
                HybridWallRayMask.None),
        };
        HybridRegularCompositePlan plan = HybridRegularRasterCompositor.Compile(
            originalAlpha,
            transitionAlpha,
            corners,
            originalLinkIndex: 0);
        int capOpening = -1;
        for (int y = HybridRegularRasterCompositor.Padding;
             y < HybridRegularRasterCompositor.Padding + 60 && capOpening < 0;
             y++)
        for (int x = HybridRegularRasterCompositor.Padding;
             x < HybridRegularRasterCompositor.Padding + 60;
             x++)
        {
            HybridWallRasterPixel thin = plan.ThinAt(x, y);
            HybridWallRasterPixel originalSemantic = plan.OriginalAt(x, y);
            HybridWallRasterPixel transitionSemantic = plan.TransitionAt(x, y);
            if (thin.IsStructural && originalSemantic.Surface != HybridWallRasterSurface.Top &&
                transitionSemantic.Surface != thin.Surface)
            {
                capOpening = y * HybridRegularRasterCompositor.CanvasSize + x;
                break;
            }
        }
        Assert.That(capOpening, Is.GreaterThanOrEqualTo(0),
            "the corner contact must cross at least one opaque source byte outside ordinary top body");

        var native = new Color32[60 * 60];
        var transition = new Color32[native.Length];
        Fill(native, new Color32(180, 170, 160, 255));
        Fill(transition, new Color32(90, 80, 70, 255));
        Color32 straightDonor = new(61, 62, 63, 255);
        HybridWallRasterPixel sampledDonor = default;
        Color32[] output = HybridRegularRasterCompositor.ComposeOwned(
            native,
            transition,
            plan,
            (_, _) => new Color32(201, 202, 203, 255),
            (_, _, _) => new Color32(9, 9, 9, 255),
            sample =>
            {
                sampledDonor = sample;
                return straightDonor;
            });

        Assert.Multiple(() =>
        {
            Assert.That(plan.Aperture[capOpening], Is.True);
            Assert.That(output[capOpening], Is.EqualTo(straightDonor),
                "a native-region shoulder must use regular-wall material even when the augmented same-coordinate surface differs");
            Assert.That(sampledDonor.SourceLinkIndex, Is.AnyOf((byte)5, (byte)10),
                "the fallback donor must be a straight Core slot, never the augmented L/T/+ slot");
            Assert.That(sampledDonor.SourceLinkIndex, Is.Not.EqualTo((byte)plan.TransitionLinkIndex));
        });
    }

    [Test]
    public void MissingStraightDonorAlphaFailsClosedInsteadOfSamplingAugmentedMiter()
    {
        var nativeAlpha = Enumerable.Repeat((byte)255, 60 * 60).ToArray();
        var transitionAlpha = Enumerable.Repeat((byte)255, 60 * 60).ToArray();
        var corners = new[]
        {
            new HybridWallCornerRaster(
                HybridWallQuadrant.NorthEast,
                HybridWallRayMask.South,
                HybridWallRayMask.None,
                HybridWallRayMask.None),
        };
        HybridRegularCompositePlan complete = HybridRegularRasterCompositor.Compile(
            nativeAlpha,
            transitionAlpha,
            corners,
            originalLinkIndex: 10);
        int candidate = Enumerable.Range(0, HybridRegularRasterCompositor.CanvasSize *
                                              HybridRegularRasterCompositor.CanvasSize)
            .First(index =>
            {
                if (!HybridRegularRasterCompositor.IsInsideNativeRegion(index))
                {
                    return false;
                }
                int nativeX = index % HybridRegularRasterCompositor.CanvasSize -
                              HybridRegularRasterCompositor.Padding;
                int nativeY = index / HybridRegularRasterCompositor.CanvasSize -
                              HybridRegularRasterCompositor.Padding;
                HybridWallRasterPixel thin = complete.ThinAt(index);
                return thin.IsStructural && thin.SourceX == nativeX && thin.SourceY == nativeY &&
                       complete.TransitionAt(
                           nativeX + HybridRegularRasterCompositor.Padding,
                           nativeY + HybridRegularRasterCompositor.Padding).Surface == thin.Surface;
            });
        HybridWallRasterPixel candidateSample = complete.ThinAt(candidate);
        Assert.That(HybridRegularSquareDonor.TryMap(
            candidateSample,
            complete.ContactAt(candidate).Ray,
            out int missingLink,
            out int missingX,
            out int missingY), Is.True);

        var native = Enumerable.Repeat(new Color32(211, 17, 19, 255), 60 * 60).ToArray();
        var transition = Enumerable.Repeat(new Color32(23, 191, 29, 255), 60 * 60).ToArray();
        int augmentedFallbackCalls = 0;

        Assert.That(
            () => HybridRegularRasterCompositor.ComposeOwned(
                native,
                transition,
                complete,
                (_, _) => new Color32(31, 37, 41, 255),
                (_, _, _) => new Color32(43, 47, 53, 255),
                sample =>
                {
                    if (sample.SourceLinkIndex is not 5 and not 10)
                    {
                        augmentedFallbackCalls++;
                    }
                    if (sample.SourceLinkIndex == missingLink &&
                        sample.SourceX == missingX && sample.SourceY == missingY)
                    {
                        return default;
                    }
                    return new Color32(211, 17, 19, 255);
                }),
            Throws.TypeOf<HybridRegularCompositionException>(),
            "a missing straight donor must retain the native regular-wall print, not reintroduce augmented bevel bytes");
        Assert.That(augmentedFallbackCalls, Is.Zero,
            "the compositor may not consult an augmented-slot fallback after a required straight donor is absent");
    }

    [TestCase(10, HybridWallQuadrant.NorthWest, HybridWallRayMask.South)]
    [TestCase(10, HybridWallQuadrant.NorthEast, HybridWallRayMask.South)]
    [TestCase(10, HybridWallQuadrant.SouthWest, HybridWallRayMask.North)]
    [TestCase(10, HybridWallQuadrant.SouthEast, HybridWallRayMask.North)]
    [TestCase(5, HybridWallQuadrant.NorthEast, HybridWallRayMask.West)]
    [TestCase(5, HybridWallQuadrant.SouthEast, HybridWallRayMask.West)]
    [TestCase(5, HybridWallQuadrant.NorthWest, HybridWallRayMask.East)]
    [TestCase(5, HybridWallQuadrant.SouthWest, HybridWallRayMask.East)]
    public void MatchingSurfaceLabelsNeverPermitNonStraightCoreRasterBytes(
        int originalLinkIndex,
        HybridWallQuadrant quadrant,
        HybridWallRayMask ray)
    {
        var alpha = Enumerable.Repeat((byte)255, 60 * 60).ToArray();
        HybridRegularCompositePlan plan = HybridRegularRasterCompositor.Compile(
            alpha,
            alpha,
            new[]
            {
                new HybridWallCornerRaster(
                    quadrant,
                    ray,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None),
            },
            originalLinkIndex);
        var native = Enumerable.Repeat(new Color32(17, 19, 23, 255), 60 * 60).ToArray();
        var augmented = Enumerable.Repeat(new Color32(241, 37, 43, 255), 60 * 60).ToArray();
        var straight = new Color32(53, 197, 71, 255);
        var sampledLinks = new List<byte>();

        Color32[] output = HybridRegularRasterCompositor.ComposeOwned(
            native,
            augmented,
            plan,
            (_, _) => new Color32(79, 83, 89, 255),
            (_, _, _) => new Color32(3, 5, 7, 255),
            sample =>
            {
                sampledLinks.Add(sample.SourceLinkIndex);
                return straight;
            });
        int[] affectedNative = Enumerable.Range(0, output.Length)
            .Where(index => HybridRegularRasterCompositor.IsInsideNativeRegion(index) &&
                            plan.ThinAt(index).IsStructural)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(affectedNative, Is.Not.Empty);
            Assert.That(affectedNative.All(index => output[index].Equals(straight)), Is.True,
                "an augmented L/T raster byte must not survive merely because its semantic surface label matches");
            Assert.That(sampledLinks, Is.Not.Empty);
            Assert.That(sampledLinks.All(link => link is 5 or 10), Is.True,
                "all native mixed-contact surfaces must be sampled from one measured straight Core slot");
            Assert.That(sampledLinks, Does.Not.Contain((byte)plan.TransitionLinkIndex));
        });
    }

    [TestCase(HybridWallQuadrant.NorthEast, HybridWallRayMask.West)]
    [TestCase(HybridWallQuadrant.NorthEast, HybridWallRayMask.South)]
    [TestCase(HybridWallQuadrant.NorthWest, HybridWallRayMask.East)]
    [TestCase(HybridWallQuadrant.NorthWest, HybridWallRayMask.South)]
    [TestCase(HybridWallQuadrant.SouthEast, HybridWallRayMask.West)]
    [TestCase(HybridWallQuadrant.SouthEast, HybridWallRayMask.North)]
    [TestCase(HybridWallQuadrant.SouthWest, HybridWallRayMask.East)]
    [TestCase(HybridWallQuadrant.SouthWest, HybridWallRayMask.North)]
    public void NativeRegionOrdinaryArmUsesTheRegularWallMaterialInEveryRotation(
        HybridWallQuadrant quadrant,
        HybridWallRayMask ray)
    {
        var alpha = new byte[60 * 60];
        Fill(alpha, (byte)255);
        HybridRegularCompositePlan plan = HybridRegularRasterCompositor.Compile(
            alpha,
            alpha,
            new[]
            {
                new HybridWallCornerRaster(
                    quadrant,
                    ray,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None),
            });
        int ordinaryArmTop = Enumerable.Range(0, HybridRegularRasterCompositor.CanvasSize *
                                               HybridRegularRasterCompositor.CanvasSize)
            .First(index => HybridRegularRasterCompositor.IsInsideNativeRegion(index) &&
                            plan.ThinAt(index).Surface == HybridWallRasterSurface.Top);

        var native = new Color32[60 * 60];
        var transition = new Color32[native.Length];
        Fill(native, new Color32(180, 20, 20, 255));
        Fill(transition, new Color32(20, 180, 20, 255));
        var thinColor = new Color32(20, 20, 220, 255);
        Color32[] output = HybridRegularRasterCompositor.Compose(
            native,
            transition,
            plan,
            _ => thinColor,
            (_, _) => new Color32(9, 9, 9, 255));

        Assert.That(output[ordinaryArmTop], Is.EqualTo(new Color32(20, 180, 20, 255)),
            "the square native-region arm is regular-wall geometry and must not carry a contrasting Thin wedge");
    }

    [Test]
    public void SideTContactIsOwnedOnceAndOffsetParallelRayDoesNotConnect()
    {
        HybridWallQuadrant run = HybridWallQuadrant.NorthEast | HybridWallQuadrant.NorthWest;
        Assert.Multiple(() =>
        {
            Assert.That(HybridRegularContactOwnership.AllRegularOwnedRays(
                    run,
                    HybridWallRayMask.South),
                Is.EqualTo(HybridWallRayMask.South),
                "a Thin endpoint entering the side of a regular run must form one T contact");
            Assert.That(HybridWallRegularApertureRecipe.EligibleRays(
                    HybridWallQuadrant.NorthEast,
                    HybridWallRayMask.North | HybridWallRayMask.East),
                Is.EqualTo(HybridWallRayMask.None),
                "parallel/away rays that do not leave the occupied perimeter must not infer a connection");
        });
    }

    [Test]
    public void SideTContactUsesBothAdjacentRegularTilesButOnlyOneGutterOwner()
    {
        HybridWallQuadrant run = HybridWallQuadrant.NorthEast | HybridWallQuadrant.NorthWest;

        Assert.Multiple(() =>
        {
            Assert.That(HybridRegularContactOwnership.RaysParticipatingIn(
                    HybridWallQuadrant.NorthWest,
                    run,
                    HybridWallRayMask.South),
                Is.EqualTo(HybridWallRayMask.South),
                "the west regular tile must contribute the west half of the vertex-centered ordinary arm");
            Assert.That(HybridRegularContactOwnership.RaysParticipatingIn(
                    HybridWallQuadrant.NorthEast,
                    run,
                    HybridWallRayMask.South),
                Is.EqualTo(HybridWallRayMask.South),
                "the east regular tile must contribute the east half of the vertex-centered ordinary arm");
            Assert.That(HybridRegularContactOwnership.RaysOwnedBy(
                    HybridWallQuadrant.NorthWest,
                    run,
                    HybridWallRayMask.South),
                Is.EqualTo(HybridWallRayMask.None),
                "the west participant must not duplicate the exterior Thin gutter");
            Assert.That(HybridRegularContactOwnership.RaysOwnedBy(
                    HybridWallQuadrant.NorthEast,
                    run,
                    HybridWallRayMask.South),
                Is.EqualTo(HybridWallRayMask.South),
                "the east participant is the deterministic single gutter owner");
        });
    }

    [TestCase(HybridWallRayMask.South, HybridWallQuadrant.NorthWest, HybridWallQuadrant.NorthEast)]
    [TestCase(HybridWallRayMask.North, HybridWallQuadrant.SouthWest, HybridWallQuadrant.SouthEast)]
    [TestCase(HybridWallRayMask.West, HybridWallQuadrant.SouthEast, HybridWallQuadrant.NorthEast)]
    [TestCase(HybridWallRayMask.East, HybridWallQuadrant.SouthWest, HybridWallQuadrant.NorthWest)]
    public void SideTClassificationMarksBothNativeParticipantsInEveryRotation(
        HybridWallRayMask ray,
        HybridWallQuadrant first,
        HybridWallQuadrant second)
    {
        HybridWallQuadrant run = first | second;
        Assert.Multiple(() =>
        {
            Assert.That(HybridRegularContactOwnership.SideTRaysParticipatingIn(first, run, ray),
                Is.EqualTo(ray));
            Assert.That(HybridRegularContactOwnership.SideTRaysParticipatingIn(second, run, ray),
                Is.EqualTo(ray));
            Assert.That(HybridRegularContactOwnership.SideTRaysParticipatingIn(first, first, ray),
                Is.EqualTo(HybridWallRayMask.None),
                "an isolated one-tile end contact must retain the ordinary complete-arm recipe");
        });
    }

    [Test]
    public void SouthSideTKeepsTheReceivingRunUncoveredAndEmitsTheGutterOnce()
    {
        var alpha = new byte[60 * 60];
        Fill(alpha, (byte)255);
        HybridRegularCompositePlan west = HybridRegularRasterCompositor.Compile(
            alpha,
            alpha,
            new[]
            {
                new HybridWallCornerRaster(
                    HybridWallQuadrant.NorthWest,
                    HybridWallRayMask.South,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None,
                    gutterRays: HybridWallRayMask.None,
                    sideTRays: HybridWallRayMask.South),
            },
            originalLinkIndex: (int)(HybridWallRayMask.East | HybridWallRayMask.West));
        HybridRegularCompositePlan east = HybridRegularRasterCompositor.Compile(
            alpha,
            alpha,
            new[]
            {
                new HybridWallCornerRaster(
                    HybridWallQuadrant.NorthEast,
                    HybridWallRayMask.South,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None,
                    gutterRays: HybridWallRayMask.South,
                    sideTRays: HybridWallRayMask.South),
            },
            originalLinkIndex: (int)(HybridWallRayMask.East | HybridWallRayMask.West));

        int nativeY = HybridRegularRasterCompositor.Padding + 10;
        int westTopCount = Enumerable.Range(0, 60).Count(x =>
            west.ThinAt(HybridRegularRasterCompositor.Padding + x, nativeY).Surface ==
            HybridWallRasterSurface.Top);
        int eastTopCount = Enumerable.Range(0, 60).Count(x =>
            east.ThinAt(HybridRegularRasterCompositor.Padding + x, nativeY).Surface ==
            HybridWallRasterSurface.Top);
        int gutterY = HybridRegularRasterCompositor.Padding - 15;
        var opaque = Enumerable.Repeat(new Color32(180, 170, 160, 255), 60 * 60).ToArray();
        Color32[] westOutput = HybridRegularRasterCompositor.Compose(
            opaque,
            opaque,
            west,
            _ => new Color32(180, 170, 160, 255),
            (_, _) => new Color32(9, 9, 9, 255));
        Color32[] eastOutput = HybridRegularRasterCompositor.Compose(
            opaque,
            opaque,
            east,
            _ => new Color32(180, 170, 160, 255),
            (_, _) => new Color32(9, 9, 9, 255));
        HybridRegularShadowPlan westShadow = HybridRegularShadowCompiler.Compile(west);
        HybridRegularShadowPlan eastShadow = HybridRegularShadowCompiler.Compile(east);

        Assert.Multiple(() =>
        {
            Assert.That(westTopCount, Is.Zero,
                "the west regular tile must retain its native straight run without an overlaid half-arm");
            Assert.That(eastTopCount, Is.Zero,
                "the east regular tile must retain its native straight run without an overlaid half-arm");
            Assert.That(Enumerable.Range(1, 59).Any(y =>
                    west.ThinAt(HybridRegularRasterCompositor.Padding + 30,
                        HybridRegularRasterCompositor.Padding + y).IsStructural),
                Is.False,
                "the non-owner participant may clear only its narrow perimeter contact, never paint a collar");
            Assert.That(Enumerable.Range(1, 59).Any(y =>
                    east.ThinAt(HybridRegularRasterCompositor.Padding + 30,
                        HybridRegularRasterCompositor.Padding + y).IsStructural),
                Is.False,
                "the canonical participant may own the gutter but may not paint a perpendicular native arm");
            Assert.That(west.ThinAt(
                    HybridRegularRasterCompositor.Padding + 59,
                    gutterY).IsStructural,
                Is.False,
                "the non-owner participant must not emit a second exterior gutter");
            Assert.That(east.ThinAt(
                    HybridRegularRasterCompositor.Padding,
                    gutterY).IsStructural,
                Is.True,
                "the canonical participant must retain the actual Thin gutter");
            Assert.That(westOutput[
                    nativeY * HybridRegularRasterCompositor.CanvasSize +
                    HybridRegularRasterCompositor.Padding + 60].a,
                Is.Zero,
                "the west half must not derive an exterior contour on the internal shared-tile boundary");
            Assert.That(eastOutput[
                    nativeY * HybridRegularRasterCompositor.CanvasSize +
                    HybridRegularRasterCompositor.Padding - 1].a,
                Is.Zero,
                "the east half must not derive an exterior contour on the internal shared-tile boundary");
            Assert.That(westShadow.CastingEdges.Any(edge =>
                    edge.MinX == 60 && edge.MaxX == 60 && edge.MinY <= 10 && edge.MaxY > 10),
                Is.False,
                "the west half must not cast an internal wall-height shadow edge against its paired east half");
            Assert.That(eastShadow.CastingEdges.Any(edge =>
                    edge.MinX == 0 && edge.MaxX == 0 && edge.MinY <= 10 && edge.MaxY > 10),
                Is.False,
                "the east half must not cast an internal wall-height shadow edge against its paired west half");
        });
    }

    [TestCase(HybridWallRayMask.South, HybridWallQuadrant.NorthWest, HybridWallQuadrant.NorthEast, 10, 10)]
    [TestCase(HybridWallRayMask.North, HybridWallQuadrant.SouthWest, HybridWallQuadrant.SouthEast, 10, 59)]
    [TestCase(HybridWallRayMask.West, HybridWallQuadrant.SouthEast, HybridWallQuadrant.NorthEast, 5, 10)]
    [TestCase(HybridWallRayMask.East, HybridWallQuadrant.SouthWest, HybridWallQuadrant.NorthWest, 5, 59)]
    public void EverySideTRotationLeavesBothReceivingNativeBodiesUncovered(
        HybridWallRayMask ray,
        HybridWallQuadrant firstQuadrant,
        HybridWallQuadrant secondQuadrant,
        int originalLinkIndex,
        int alongRayCoordinate)
    {
        var alpha = new byte[60 * 60];
        Fill(alpha, (byte)255);
        HybridRegularCompositePlan first = Compile(firstQuadrant, ownsGutter: false);
        HybridRegularCompositePlan second = Compile(secondQuadrant, ownsGutter: true);
        Color32[] native = Enumerable.Range(0, 60 * 60)
            .Select(index => new Color32(
                (byte)(64 + index % 173),
                (byte)(48 + index * 7 % 191),
                (byte)(32 + index * 13 % 211),
                255))
            .ToArray();
        Color32[] firstOutput = HybridRegularRasterCompositor.Compose(
            native,
            native,
            first,
            _ => new Color32(180, 170, 160, 255),
            (_, _) => new Color32(9, 9, 9, 255));
        Color32[] secondOutput = HybridRegularRasterCompositor.Compose(
            native,
            native,
            second,
            _ => new Color32(180, 170, 160, 255),
            (_, _) => new Color32(9, 9, 9, 255));

        int firstBodyOverdrawCount = 0;
        int secondBodyOverdrawCount = 0;
        int firstChangedBodyByteCount = 0;
        int secondChangedBodyByteCount = 0;
        int firstExteriorStructuralCount = 0;
        int secondExteriorStructuralCount = 0;
        for (int canvasY = 0; canvasY < HybridRegularRasterCompositor.CanvasSize; canvasY++)
        for (int canvasX = 0; canvasX < HybridRegularRasterCompositor.CanvasSize; canvasX++)
        {
            bool insideNative = canvasX >= HybridRegularRasterCompositor.Padding &&
                                canvasX < HybridRegularRasterCompositor.Padding + 60 &&
                                canvasY >= HybridRegularRasterCompositor.Padding &&
                                canvasY < HybridRegularRasterCompositor.Padding + 60;
            if (insideNative)
            {
                continue;
            }
            if (first.ThinAt(canvasX, canvasY).IsStructural)
            {
                firstExteriorStructuralCount++;
            }
            if (second.ThinAt(canvasX, canvasY).IsStructural)
            {
                secondExteriorStructuralCount++;
            }
        }
        for (int y = 0; y < 60; y++)
        for (int x = 0; x < 60; x++)
        {
            int canvasX = HybridRegularRasterCompositor.Padding + x;
            int canvasY = HybridRegularRasterCompositor.Padding + y;
            int canvasIndex = canvasY * HybridRegularRasterCompositor.CanvasSize + canvasX;
            int nativeIndex = y * 60 + x;
            if (first.OriginalAt(canvasX, canvasY).IsStructural &&
                first.ThinAt(canvasX, canvasY).IsStructural)
            {
                firstBodyOverdrawCount++;
            }
            if (second.OriginalAt(canvasX, canvasY).IsStructural &&
                second.ThinAt(canvasX, canvasY).IsStructural)
            {
                secondBodyOverdrawCount++;
            }
            if (first.OriginalAt(canvasX, canvasY).IsStructural &&
                !firstOutput[canvasIndex].Equals(native[nativeIndex]))
            {
                firstChangedBodyByteCount++;
            }
            if (second.OriginalAt(canvasX, canvasY).IsStructural &&
                !secondOutput[canvasIndex].Equals(native[nativeIndex]))
            {
                secondChangedBodyByteCount++;
            }
        }
        int firstInternalBoundary;
        int secondInternalBoundary;
        if (ray is HybridWallRayMask.North or HybridWallRayMask.South)
        {
            int y = HybridRegularRasterCompositor.Padding + alongRayCoordinate;
            firstInternalBoundary = y * HybridRegularRasterCompositor.CanvasSize +
                                    HybridRegularRasterCompositor.Padding + 60;
            secondInternalBoundary = y * HybridRegularRasterCompositor.CanvasSize +
                                     HybridRegularRasterCompositor.Padding - 1;
        }
        else
        {
            int x = HybridRegularRasterCompositor.Padding + alongRayCoordinate;
            firstInternalBoundary = (HybridRegularRasterCompositor.Padding + 60) *
                                    HybridRegularRasterCompositor.CanvasSize + x;
            secondInternalBoundary = (HybridRegularRasterCompositor.Padding - 1) *
                                     HybridRegularRasterCompositor.CanvasSize + x;
        }

        Assert.Multiple(() =>
        {
            Assert.That(firstBodyOverdrawCount, Is.Zero,
                $"{firstQuadrant} must not overlay an ordinary-width collar for {ray}");
            Assert.That(secondBodyOverdrawCount, Is.Zero,
                $"{secondQuadrant} must not overlay an ordinary-width collar for {ray}");
            Assert.That(firstChangedBodyByteCount, Is.Zero,
                $"{firstQuadrant} must preserve every receiving regular-wall structural byte for {ray}");
            Assert.That(secondChangedBodyByteCount, Is.Zero,
                $"{secondQuadrant} must preserve every receiving regular-wall structural byte for {ray}");
            Assert.That(firstExteriorStructuralCount, Is.Zero,
                $"{firstQuadrant} is the non-owner and must not emit a duplicate Thin gutter for {ray}");
            Assert.That(secondExteriorStructuralCount, Is.GreaterThan(0),
                $"{secondQuadrant} is the production canonical owner and must emit the Thin gutter for {ray}");
            Assert.That(firstOutput[firstInternalBoundary].a, Is.Zero,
                "the first half must not outline the internal boundary shared with the second regular tile");
            Assert.That(secondOutput[secondInternalBoundary].a, Is.Zero,
                "the second half must not outline the internal boundary shared with the first regular tile");
        });

        HybridRegularCompositePlan Compile(HybridWallQuadrant quadrant, bool ownsGutter) =>
            HybridRegularRasterCompositor.Compile(
                alpha,
                alpha,
                new[]
                {
                    new HybridWallCornerRaster(
                        quadrant,
                        ray,
                        HybridWallRayMask.None,
                        HybridWallRayMask.None,
                        gutterRays: ownsGutter ? ray : HybridWallRayMask.None,
                        sideTRays: ray),
                },
                originalLinkIndex);
    }

    [TestCase(HybridWallQuadrant.NorthWest, HybridWallRayMask.South, 10)]
    [TestCase(HybridWallQuadrant.SouthWest, HybridWallRayMask.North, 10)]
    [TestCase(HybridWallQuadrant.SouthEast, HybridWallRayMask.West, 5)]
    [TestCase(HybridWallQuadrant.SouthWest, HybridWallRayMask.East, 5)]
    public void SideTContourApertureLeavesEveryReceivingBodyPixelByteIdentical(
        HybridWallQuadrant quadrant,
        HybridWallRayMask ray,
        int originalLinkIndex)
    {
        var alpha = Enumerable.Repeat((byte)255, 60 * 60).ToArray();
        HybridRegularCompositePlan plan = HybridRegularRasterCompositor.Compile(
            alpha,
            alpha,
            new[]
            {
                new HybridWallCornerRaster(
                    quadrant,
                    ray,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None,
                    gutterRays: HybridWallRayMask.None,
                    sideTRays: ray),
            },
            originalLinkIndex);
        var native = Enumerable.Range(0, 60 * 60)
            .Select(index => new Color32(
                (byte)(40 + index % 173),
                (byte)(30 + index * 3 % 181),
                (byte)(20 + index * 7 % 191),
                255))
            .ToArray();
        var transition = Enumerable.Repeat(new Color32(3, 251, 5, 255), 60 * 60).ToArray();
        Color32[] output = HybridRegularRasterCompositor.Compose(
            native,
            transition,
            plan,
            _ => new Color32(7, 11, 251, 255),
            (_, _) => new Color32(13, 17, 19, 255));

        int continuationCount = 0;
        for (int y = 0; y < 60; y++)
        for (int x = 0; x < 60; x++)
        {
            int index = (y + HybridRegularRasterCompositor.Padding) *
                        HybridRegularRasterCompositor.CanvasSize + x +
                        HybridRegularRasterCompositor.Padding;
            if (plan.HasSideTContactContinuationAt(
                    x + HybridRegularRasterCompositor.Padding,
                    y + HybridRegularRasterCompositor.Padding))
            {
                continuationCount++;
            }
            HybridWallRasterPixel original = plan.OriginalAt(
                x + HybridRegularRasterCompositor.Padding,
                y + HybridRegularRasterCompositor.Padding);
            if (original.IsStructural)
            {
                Assert.That(plan.ThinAt(index).IsStructural, Is.False,
                    $"side-T {ray} may not overlay receiving-wall body at {x},{y}");
                Assert.That(output[index], Is.EqualTo(native[y * 60 + x]),
                    $"side-T {ray} changed receiving-wall body byte {x},{y}");
            }
        }
        Assert.That(continuationCount, Is.GreaterThan(0),
            "each participant must still expose its analytic half of the narrow contour aperture");
    }

    [TestCase(HybridWallQuadrant.NorthWest, HybridWallRayMask.South, 10)]
    [TestCase(HybridWallQuadrant.NorthEast, HybridWallRayMask.South, 10)]
    [TestCase(HybridWallQuadrant.SouthWest, HybridWallRayMask.North, 10)]
    [TestCase(HybridWallQuadrant.SouthEast, HybridWallRayMask.North, 10)]
    [TestCase(HybridWallQuadrant.SouthEast, HybridWallRayMask.West, 5)]
    [TestCase(HybridWallQuadrant.NorthEast, HybridWallRayMask.West, 5)]
    [TestCase(HybridWallQuadrant.SouthWest, HybridWallRayMask.East, 5)]
    [TestCase(HybridWallQuadrant.NorthWest, HybridWallRayMask.East, 5)]
    public void SideTReplacesTheCompleteReceivingContourDepthWithRegularMaterialContinuation(
        HybridWallQuadrant quadrant,
        HybridWallRayMask ray,
        int originalLinkIndex)
    {
        var alpha = Enumerable.Repeat((byte)255, 60 * 60).ToArray();
        HybridRegularCompositePlan plan = HybridRegularRasterCompositor.Compile(
            alpha,
            alpha,
            new[]
            {
                new HybridWallCornerRaster(
                    quadrant,
                    ray,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None,
                    gutterRays: HybridWallRayMask.None,
                    sideTRays: ray),
            },
            originalLinkIndex);
        var native = Enumerable.Repeat(new Color32(180, 170, 160, 255), 60 * 60).ToArray();
        var continuation = new Color32(24, 212, 83, 255);
        Color32[] output = HybridRegularRasterCompositor.ComposeOwned(
            native,
            native,
            plan,
            (_, _) => new Color32(230, 20, 20, 255),
            (_, _, _) => new Color32(9, 9, 9, 255),
            _ => continuation);

        var continued = new List<(int X, int Y)>();
        var retained = new List<(int X, int Y)>();
        for (int y = 0; y < 60; y++)
        for (int x = 0; x < 60; x++)
        {
            HybridWallRasterSurface surface = plan.OriginalAt(
                x + HybridRegularRasterCompositor.Padding,
                y + HybridRegularRasterCompositor.Padding).Surface;
            if (surface is not (HybridWallRasterSurface.Outline or
                HybridWallRasterSurface.OutlineAntialias))
            {
                continue;
            }

            bool continuationAtBoundary = ray switch
            {
                HybridWallRayMask.South => plan.HasSideTContactContinuationAt(
                    x + HybridRegularRasterCompositor.Padding,
                    HybridRegularRasterCompositor.Padding),
                HybridWallRayMask.North => plan.HasSideTContactContinuationAt(
                    x + HybridRegularRasterCompositor.Padding,
                    HybridRegularRasterCompositor.Padding + 59),
                HybridWallRayMask.West => plan.HasSideTContactContinuationAt(
                    HybridRegularRasterCompositor.Padding,
                    y + HybridRegularRasterCompositor.Padding),
                HybridWallRayMask.East => plan.HasSideTContactContinuationAt(
                    HybridRegularRasterCompositor.Padding + 59,
                    y + HybridRegularRasterCompositor.Padding),
                _ => false,
            };
            bool withinContourDepth = ray switch
            {
                HybridWallRayMask.South => y <= HybridWallRasterCompiler.OutlineDepth,
                HybridWallRayMask.North => y >= 59 - HybridWallRasterCompiler.OutlineDepth,
                HybridWallRayMask.West => x <= HybridWallRasterCompiler.OutlineDepth,
                HybridWallRayMask.East => x >= 59 - HybridWallRasterCompiler.OutlineDepth,
                _ => false,
            };
            bool inContactHalf = continuationAtBoundary && withinContourDepth;
            int outputIndex = (y + HybridRegularRasterCompositor.Padding) *
                              HybridRegularRasterCompositor.CanvasSize + x +
                              HybridRegularRasterCompositor.Padding;
            if (inContactHalf)
            {
                continued.Add((x, y));
                int boundaryX = ray switch
                {
                    HybridWallRayMask.West => 0,
                    HybridWallRayMask.East => 59,
                    _ => x,
                };
                int boundaryY = ray switch
                {
                    HybridWallRayMask.South => 0,
                    HybridWallRayMask.North => 59,
                    _ => y,
                };
                HybridWallRasterPixel boundary = plan.ThinAt(
                    boundaryX + HybridRegularRasterCompositor.Padding,
                    boundaryY + HybridRegularRasterCompositor.Padding);
                HybridWallRasterPixel actual = plan.ThinAt(outputIndex);
                int depth = ray switch
                {
                    HybridWallRayMask.South => y,
                    HybridWallRayMask.North => 59 - y,
                    HybridWallRayMask.West => x,
                    HybridWallRayMask.East => 59 - x,
                    _ => 0,
                };
                int expectedPhase = ray switch
                {
                    HybridWallRayMask.South => PositiveMod(boundary.SourceY + depth, 60),
                    HybridWallRayMask.North => PositiveMod(boundary.SourceY - depth, 60),
                    HybridWallRayMask.West => PositiveMod(boundary.SourceX + depth, 60),
                    HybridWallRayMask.East => PositiveMod(boundary.SourceX - depth, 60),
                    _ => 0,
                };
                int actualPhase = ray is HybridWallRayMask.North or HybridWallRayMask.South
                    ? actual.SourceY
                    : actual.SourceX;
                Assert.Multiple(() =>
                {
                    Assert.That(actual.IsStructural, Is.True,
                        $"side-T {ray}/{quadrant} left a nonstructural seam at {x},{y}");
                    Assert.That(output[outputIndex], Is.EqualTo(continuation),
                        $"side-T {ray}/{quadrant} did not replace contour {x},{y} " +
                        "with regular-material continuation");
                    Assert.That(actualPhase, Is.EqualTo(expectedPhase),
                        $"side-T {ray}/{quadrant} restarted material phase inside contour at {x},{y}");
                });
            }
            else
            {
                retained.Add((x, y));
                Assert.That(output[outputIndex], Is.EqualTo(native[y * 60 + x]),
                    $"side-T {ray}/{quadrant} altered unrelated contour byte {x},{y}");
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(continued.Count, Is.GreaterThan(0),
                "the fixture must exercise a real multi-row/column Core contour continuation");
            Assert.That(retained.Count, Is.GreaterThan(0),
                "the fixture must retain unrelated Core exterior contour bytes");
        });

        static int PositiveMod(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }

    [TestCase(HybridWallQuadrant.NorthWest, HybridWallRayMask.South, 10)]
    [TestCase(HybridWallQuadrant.NorthEast, HybridWallRayMask.South, 10)]
    [TestCase(HybridWallQuadrant.SouthWest, HybridWallRayMask.North, 10)]
    [TestCase(HybridWallQuadrant.SouthEast, HybridWallRayMask.North, 10)]
    [TestCase(HybridWallQuadrant.SouthEast, HybridWallRayMask.West, 5)]
    [TestCase(HybridWallQuadrant.NorthEast, HybridWallRayMask.West, 5)]
    [TestCase(HybridWallQuadrant.SouthWest, HybridWallRayMask.East, 5)]
    [TestCase(HybridWallQuadrant.NorthWest, HybridWallRayMask.East, 5)]
    public void NonOwnerSideTParticipantEmitsNoExteriorPixels(
        HybridWallQuadrant quadrant,
        HybridWallRayMask ray,
        int originalLinkIndex)
    {
        var alpha = Enumerable.Repeat((byte)255, 60 * 60).ToArray();
        HybridRegularCompositePlan plan = HybridRegularRasterCompositor.Compile(
            alpha,
            alpha,
            new[]
            {
                new HybridWallCornerRaster(
                    quadrant,
                    ray,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None,
                    gutterRays: HybridWallRayMask.None,
                    sideTRays: ray),
            },
            originalLinkIndex);
        var native = Enumerable.Repeat(new Color32(180, 170, 160, 255), 60 * 60).ToArray();
        Color32[] output = HybridRegularRasterCompositor.Compose(
            native,
            native,
            plan,
            _ => new Color32(230, 20, 20, 255),
            (_, _) => new Color32(9, 9, 9, 255));

        for (int y = 0; y < HybridRegularRasterCompositor.CanvasSize; y++)
        for (int x = 0; x < HybridRegularRasterCompositor.CanvasSize; x++)
        {
            bool insideNative = x >= HybridRegularRasterCompositor.Padding &&
                                x < HybridRegularRasterCompositor.Padding + 60 &&
                                y >= HybridRegularRasterCompositor.Padding &&
                                y < HybridRegularRasterCompositor.Padding + 60;
            if (!insideNative)
            {
                Assert.That(output[y * HybridRegularRasterCompositor.CanvasSize + x].a, Is.Zero,
                    $"non-owner side-T {ray}/{quadrant} emitted a duplicate exterior pixel at {x},{y}");
            }
        }
    }

    [Test]
    public void SamplingLayersClipTheNativeQuadAndProvideNonRenderingFilterGuards()
    {
        int size = HybridRegularRasterCompositor.CanvasSize;
        int padding = HybridRegularRasterCompositor.Padding;
        var composite = new Color32[size * size];
        for (int y = 0; y < 60; y++)
        for (int x = 0; x < 60; x++)
        {
            composite[(y + padding) * size + x + padding] = new Color32(
                (byte)(40 + x),
                (byte)(50 + y),
                90,
                255);
        }
        composite[padding * size + padding + 30] = default;
        composite[(padding - 1) * size + padding + 30] = new Color32(210, 80, 30, 255);

        HybridRegularSamplingLayers layers = HybridRegularSamplingLayerCompiler.Compile(composite);

        Assert.Multiple(() =>
        {
            Assert.That(layers.Native.Length, Is.EqualTo(60 * 60),
                "the native layer must be a tightly cropped clamped texture with no transparent padding mips");
            Assert.That(layers.Native[12 * 60 + 18],
                Is.EqualTo(composite[(padding + 12) * size + padding + 18]),
                "the clipped native material must retain every visible native byte");
            Assert.That(layers.Native[12 * 60],
                Is.EqualTo(composite[(padding + 12) * size + padding]),
                "the clamped west texture edge must be the exact terminal native texel");
            Assert.That(layers.Native[12 * 60 + 59],
                Is.EqualTo(composite[(padding + 12) * size + padding + 59]),
                "the clamped east texture edge must be the exact terminal native texel");
            Assert.That(layers.Exterior[(padding + 12) * size + padding + 18].a, Is.Zero,
                "the exterior 2x2 material may not redraw the clipped native body");
            Assert.That(layers.Exterior[(padding - 1) * size + padding + 30],
                Is.EqualTo(composite[(padding - 1) * size + padding + 30]),
                "the actual exterior gutter remains on the exterior material");
            Assert.That(layers.Exterior[padding * size + padding + 30].a, Is.Zero,
                "the exterior raster may not place a coplanar guard inside submitted native geometry");
            Assert.That(layers.HasExteriorPixels, Is.True);
            Assert.That(layers.ExteriorRegions, Has.Count.EqualTo(4));
        });

        HybridRegularExteriorSamplingLayer south = layers.ExteriorRegions.Single(layer =>
            layer.Region.CenterZ < 0f && layer.Region.SizeX == 2f);
        Assert.Multiple(() =>
        {
            Assert.That(south.Width, Is.EqualTo(120));
            Assert.That(south.Height, Is.EqualTo(30));
            Assert.That(south.Pixels[(south.Height - 1) * south.Width + padding + 30],
                Is.EqualTo(composite[(padding - 1) * size + padding + 30]),
                "the cropped/clamped south texture must terminate in the actual gutter contact texel");
            Assert.That(south.HasPixels, Is.True);
        });
    }

    [Test]
    public void ExteriorSamplingRegionsCoverOnlyThePaddedRing()
    {
        IReadOnlyList<HybridRegularExteriorRegion> regions =
            HybridRegularExteriorRegionCompiler.Compile();

        Assert.That(regions, Has.Count.EqualTo(4));
        float area = 0f;
        foreach (HybridRegularExteriorRegion region in regions)
        {
            area += region.SizeX * region.SizeZ;
            bool outsideNative = region.MaxX <= -0.5f || region.MinX >= 0.5f ||
                                 region.MaxZ <= -0.5f || region.MinZ >= 0.5f;
            Assert.That(outsideNative, Is.True,
                "an exterior sampling quad may meet but never overlap the clipped native cell");
            Assert.That(region.UvMinX, Is.InRange(0f, 1f));
            Assert.That(region.UvMaxX, Is.InRange(0f, 1f));
            Assert.That(region.UvMinY, Is.InRange(0f, 1f));
            Assert.That(region.UvMaxY, Is.InRange(0f, 1f));
        }
        Assert.That(area, Is.EqualTo(3f).Within(0.000001f),
            "the four regions must tile the 2x2 padded plane minus its 1x1 native center exactly");
    }

    [Test]
    public void CroppedNativeSamplingTextureKeepsOpaqueBoundariesThroughEveryMipLevel()
    {
        int canvas = HybridRegularRasterCompositor.CanvasSize;
        int padding = HybridRegularRasterCompositor.Padding;
        var composite = new Color32[canvas * canvas];
        for (int y = 0; y < 60; y++)
        for (int x = 0; x < 60; x++)
        {
            composite[(y + padding) * canvas + x + padding] = new Color32(80, 90, 100, 255);
        }

        Color32[] mip = HybridRegularSamplingLayerCompiler.Compile(composite).Native;
        int size = 60;
        while (size > 1)
        {
            int nextSize = Math.Max(1, size / 2);
            var next = new Color32[nextSize * nextSize];
            for (int y = 0; y < nextSize; y++)
            for (int x = 0; x < nextSize; x++)
            {
                int samples = 0;
                int alpha = 0;
                for (int offsetY = 0; offsetY < 2; offsetY++)
                for (int offsetX = 0; offsetX < 2; offsetX++)
                {
                    int sourceX = Math.Min(size - 1, x * 2 + offsetX);
                    int sourceY = Math.Min(size - 1, y * 2 + offsetY);
                    alpha += mip[sourceY * size + sourceX].a;
                    samples++;
                }
                next[y * nextSize + x] = new Color32(80, 90, 100, (byte)(alpha / samples));
            }
            Assert.That(next.All(pixel => pixel.a == 255), Is.True,
                $"cropped/clamped native alpha leaked at simulated {nextSize}x{nextSize} mip");
            mip = next;
            size = nextSize;
        }
    }

    [Test]
    public void EveryCroppedExteriorRegionKeepsItsInnerBoundaryOpaqueThroughEveryMipLevel()
    {
        int canvas = HybridRegularRasterCompositor.CanvasSize;
        var composite = new Color32[canvas * canvas];
        foreach (HybridRegularExteriorRegion region in HybridRegularExteriorRegionCompiler.Compile())
        {
            int minX = (int)Math.Round((region.MinX + 1f) * 60f);
            int maxX = (int)Math.Round((region.MaxX + 1f) * 60f);
            int minY = (int)Math.Round((region.MinZ + 1f) * 60f);
            int maxY = (int)Math.Round((region.MaxZ + 1f) * 60f);
            for (int y = minY; y < maxY; y++)
            for (int x = minX; x < maxX; x++)
            {
                composite[y * canvas + x] = new Color32(80, 90, 100, 255);
            }
        }

        HybridRegularSamplingLayers layers = HybridRegularSamplingLayerCompiler.Compile(composite);
        foreach (HybridRegularExteriorSamplingLayer layer in layers.ExteriorRegions)
        {
            Color32[] mip = layer.Pixels;
            int width = layer.Width;
            int height = layer.Height;
            while (width > 1 || height > 1)
            {
                int nextWidth = Math.Max(1, width / 2);
                int nextHeight = Math.Max(1, height / 2);
                var next = new Color32[nextWidth * nextHeight];
                for (int y = 0; y < nextHeight; y++)
                for (int x = 0; x < nextWidth; x++)
                {
                    int alpha = 0;
                    for (int offsetY = 0; offsetY < 2; offsetY++)
                    for (int offsetX = 0; offsetX < 2; offsetX++)
                    {
                        int sourceX = Math.Min(width - 1, x * 2 + offsetX);
                        int sourceY = Math.Min(height - 1, y * 2 + offsetY);
                        alpha += mip[sourceY * width + sourceX].a;
                    }
                    next[y * nextWidth + x] = new Color32(80, 90, 100, (byte)(alpha / 4));
                }
                Assert.That(next.All(pixel => pixel.a == 255), Is.True,
                    $"{layer.Region.CenterX},{layer.Region.CenterZ} leaked alpha at {nextWidth}x{nextHeight} mip");
                mip = next;
                width = nextWidth;
                height = nextHeight;
            }
        }
    }

    [TestCase(HybridWallQuadrant.NorthWest, HybridWallRayMask.South, 10)]
    [TestCase(HybridWallQuadrant.NorthEast, HybridWallRayMask.South, 10)]
    [TestCase(HybridWallQuadrant.SouthWest, HybridWallRayMask.North, 10)]
    [TestCase(HybridWallQuadrant.SouthEast, HybridWallRayMask.North, 10)]
    [TestCase(HybridWallQuadrant.SouthEast, HybridWallRayMask.West, 5)]
    [TestCase(HybridWallQuadrant.NorthEast, HybridWallRayMask.West, 5)]
    [TestCase(HybridWallQuadrant.SouthWest, HybridWallRayMask.East, 5)]
    [TestCase(HybridWallQuadrant.NorthWest, HybridWallRayMask.East, 5)]
    public void SideTReceivingRunCastsNoInternalBoundaryShadow(
        HybridWallQuadrant quadrant,
        HybridWallRayMask ray,
        int originalLinkIndex)
    {
        var alpha = Enumerable.Repeat((byte)255, 60 * 60).ToArray();
        HybridRegularCompositePlan plan = HybridRegularRasterCompositor.Compile(
            alpha,
            alpha,
            new[]
            {
                new HybridWallCornerRaster(
                    quadrant,
                    ray,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None,
                    gutterRays: HybridWallRayMask.None,
                    sideTRays: ray),
            },
            originalLinkIndex);
        HybridRegularShadowPlan shadow = HybridRegularShadowCompiler.Compile(plan);
        CoreLinkedSemanticPlan original = CoreLinkedSemanticCompiler.Compile(originalLinkIndex);

        if (originalLinkIndex == (int)(HybridWallRayMask.East | HybridWallRayMask.West))
        {
            for (int y = 0; y < 60; y++)
            {
                if (original[0, y].Surface == HybridWallRasterSurface.Top)
                {
                    Assert.That(shadow.CastingEdges.Any(edge =>
                            edge.MinX == 0 && edge.MaxX == 0 && edge.MinY <= y && edge.MaxY > y),
                        Is.False,
                        $"linked west boundary cast an internal wall-height edge at y={y}");
                }
                if (original[59, y].Surface == HybridWallRasterSurface.Top)
                {
                    Assert.That(shadow.CastingEdges.Any(edge =>
                            edge.MinX == 60 && edge.MaxX == 60 && edge.MinY <= y && edge.MaxY > y),
                        Is.False,
                        $"linked east boundary cast an internal wall-height edge at y={y}");
                }
            }
        }
        else
        {
            for (int x = 0; x < 60; x++)
            {
                if (original[x, 0].Surface != HybridWallRasterSurface.Top)
                {
                    continue;
                }
                Assert.That(shadow.CastingEdges.Any(edge =>
                        edge.MinY == 0 && edge.MaxY == 0 && edge.MinX <= x && edge.MaxX > x),
                    Is.False,
                    $"linked south boundary cast an internal wall-height edge at x={x}");
            }
        }
    }

    [TestCase(HybridWallRayMask.South, HybridWallQuadrant.NorthWest, 10, 13, 2, 43, 2, HybridWallRasterSurface.WestSide)]
    [TestCase(HybridWallRayMask.South, HybridWallQuadrant.NorthEast, 10, 47, 2, 17, 2, HybridWallRasterSurface.EastSide)]
    [TestCase(HybridWallRayMask.North, HybridWallQuadrant.SouthWest, 10, 13, 58, 43, 58, HybridWallRasterSurface.WestSide)]
    [TestCase(HybridWallRayMask.North, HybridWallQuadrant.SouthEast, 10, 47, 58, 17, 58, HybridWallRasterSurface.EastSide)]
    [TestCase(HybridWallRayMask.West, HybridWallQuadrant.SouthEast, 5, 5, 24, 5, 43, HybridWallRasterSurface.Front)]
    [TestCase(HybridWallRayMask.East, HybridWallQuadrant.SouthWest, 5, 54, 24, 54, 43, HybridWallRasterSurface.Front)]
    public void EverySideTRotationRejectsAugmentedProjectedCollarFaces(
        HybridWallRayMask ray,
        HybridWallQuadrant quadrant,
        int originalLinkIndex,
        int sourceX,
        int sourceY,
        int targetX,
        int targetY,
        HybridWallRasterSurface expectedSurface)
    {
        var alpha = new byte[60 * 60];
        Fill(alpha, (byte)255);
        CoreLinkedSemanticPlan transition = CoreLinkedSemanticCompiler.Compile(originalLinkIndex | (int)ray);
        HybridWallRasterPixel source = transition[sourceX, sourceY];
        HybridRegularCompositePlan actual = HybridRegularRasterCompositor.Compile(
            alpha,
            alpha,
            new[]
            {
                new HybridWallCornerRaster(
                    quadrant,
                    ray,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None,
                    gutterRays: HybridWallRayMask.None,
                    sideTRays: ray),
            },
            originalLinkIndex);
        HybridWallRasterPixel mapped = actual.ThinAt(
            HybridRegularRasterCompositor.Padding + targetX,
            HybridRegularRasterCompositor.Padding + targetY);

        Assert.Multiple(() =>
        {
            Assert.That(source.Surface, Is.EqualTo(expectedSurface),
                "the measured Core transition sample must remain the expected projected surface");
            Assert.That(source.OwnerRays.HasFlag(ray), Is.True,
                "a projected face must retain the incident branch that caused its extrusion");
            Assert.That(mapped.IsStructural, Is.False,
                "the side-T must not map Core's projected branch face into a rectangular connector collar");
        });
    }

    [TestCase(10, HybridWallRayMask.South, 13, 2, 5, 13, 32)]
    [TestCase(10, HybridWallRayMask.South, 47, 2, 5, 47, 32)]
    [TestCase(10, HybridWallRayMask.North, 13, 58, 5, 13, 29)]
    [TestCase(10, HybridWallRayMask.North, 47, 58, 5, 47, 29)]
    [TestCase(5, HybridWallRayMask.West, 5, 24, 10, 5, 24)]
    [TestCase(5, HybridWallRayMask.East, 54, 24, 10, 54, 24)]
    [TestCase(10, HybridWallRayMask.South, 20, 2, 5, 20, 32)]
    [TestCase(10, HybridWallRayMask.North, 20, 58, 5, 20, 29)]
    [TestCase(5, HybridWallRayMask.West, 5, 40, 10, 5, 40)]
    [TestCase(5, HybridWallRayMask.East, 54, 40, 10, 54, 40)]
    public void SquareRegularArmUsesOnlyStraightCoreSurfaceDonors(
        int originalLinkIndex,
        HybridWallRayMask ray,
        int sourceX,
        int sourceY,
        int expectedLinkIndex,
        int expectedX,
        int expectedY)
    {
        HybridWallRasterPixel sample = CoreLinkedSemanticCompiler
            .Compile(originalLinkIndex | (int)ray)[sourceX, sourceY];

        bool mapped = HybridRegularSquareDonor.TryMap(
            sample,
            ray,
            out int donorLinkIndex,
            out int donorX,
            out int donorY);

        Assert.Multiple(() =>
        {
            Assert.That(mapped, Is.True,
                "every added arm surface must have a deterministic straight-slot donor");
            Assert.That(donorLinkIndex, Is.EqualTo(expectedLinkIndex),
                "non-straight T/L/+ atlas slots may classify topology but may not donate their bevel pixels");
            Assert.That(donorX, Is.EqualTo(expectedX),
                "switching to a straight donor must not restart longitudinal material phase");
            Assert.That(donorY, Is.EqualTo(expectedY),
                "switching to a straight donor must not restart longitudinal material phase");
            Assert.That(donorX, Is.EqualTo(sourceX));
            if (ray is HybridWallRayMask.East or HybridWallRayMask.West)
            {
                Assert.That(donorY, Is.EqualTo(sourceY));
            }
        });
    }

    [TestCase(HybridWallRayMask.West, HybridWallQuadrant.NorthEast, 5, 0, 40, -1, 59)]
    [TestCase(HybridWallRayMask.East, HybridWallQuadrant.NorthWest, 5, 59, 40, 60, 0)]
    [TestCase(HybridWallRayMask.South, HybridWallQuadrant.NorthEast, 10, 20, 0, -1, 29)]
    [TestCase(HybridWallRayMask.North, HybridWallQuadrant.SouthEast, 10, 20, 59, 60, 31)]
    public void SquareRegularDonorContinuesTheExactGutterWorldPhase(
        HybridWallRayMask ray,
        HybridWallQuadrant quadrant,
        int originalLinkIndex,
        int nativeX,
        int nativeY,
        int gutterCoordinate,
        int expectedGutterPhase)
    {
        HybridWallRasterPixel nativeSample = CoreLinkedSemanticCompiler
            .Compile(originalLinkIndex | (int)ray)[nativeX, nativeY];
        bool mapped = HybridRegularSquareDonor.TryMap(
            nativeSample,
            ray,
            out _,
            out int nativeDonorX,
            out int nativeDonorY);
        HybridWallRasterWrite gutter = HybridWallRegularApertureRecipe
            .Compile(quadrant, ray, HybridWallRayMask.None, HybridWallRayMask.None)
            .First(write =>
                write.Sample.Surface == HybridWallRasterSurface.Top &&
                (ray is HybridWallRayMask.East or HybridWallRayMask.West
                    ? write.X == gutterCoordinate
                    : write.Y == gutterCoordinate));

        Assert.That(mapped, Is.True);
        if (ray == HybridWallRayMask.West)
        {
            Assert.That(gutter.Sample.SourceX, Is.EqualTo(expectedGutterPhase));
            Assert.That(nativeDonorX, Is.Zero,
                "west gutter x=59 must wrap directly to native-arm x=0");
        }
        else if (ray == HybridWallRayMask.East)
        {
            Assert.That(nativeDonorX, Is.EqualTo(59));
            Assert.That(gutter.Sample.SourceX, Is.EqualTo(expectedGutterPhase),
                "east native-arm x=59 must wrap directly to gutter x=0");
        }
        else if (ray == HybridWallRayMask.South)
        {
            Assert.That(gutter.Sample.SourceY, Is.EqualTo(expectedGutterPhase));
            Assert.That(nativeDonorY, Is.EqualTo(30),
                "south gutter y=29 must continue directly into native-arm y=30");
        }
        else
        {
            Assert.That(nativeDonorY, Is.EqualTo(30));
            Assert.That(gutter.Sample.SourceY, Is.EqualTo(expectedGutterPhase),
                "north native-arm y=30 must continue directly into gutter y=31");
        }
    }

    [TestCase(HybridWallQuadrant.NorthEast, HybridWallRayMask.West)]
    [TestCase(HybridWallQuadrant.NorthEast, HybridWallRayMask.South)]
    [TestCase(HybridWallQuadrant.NorthWest, HybridWallRayMask.East)]
    [TestCase(HybridWallQuadrant.NorthWest, HybridWallRayMask.South)]
    [TestCase(HybridWallQuadrant.SouthEast, HybridWallRayMask.West)]
    [TestCase(HybridWallQuadrant.SouthEast, HybridWallRayMask.North)]
    [TestCase(HybridWallQuadrant.SouthWest, HybridWallRayMask.East)]
    [TestCase(HybridWallQuadrant.SouthWest, HybridWallRayMask.North)]
    public void HybridRegularShadowUsesOnlyTheFinalNativeRegionTopMask(
        HybridWallQuadrant quadrant,
        HybridWallRayMask ray)
    {
        var alpha = new byte[60 * 60];
        Fill(alpha, (byte)255);
        HybridRegularCompositePlan raster = HybridRegularRasterCompositor.Compile(
            alpha,
            alpha,
            new[]
            {
                new HybridWallCornerRaster(
                    quadrant,
                    ray,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None),
            });

        HybridRegularShadowPlan shadow = HybridRegularShadowCompiler.Compile(raster);

        Assert.Multiple(() =>
        {
            Assert.That(shadow.Width, Is.EqualTo(60));
            Assert.That(shadow.Height, Is.EqualTo(60));
            Assert.That(shadow.OccupiedCount, Is.GreaterThan(0));
            Assert.That(shadow.OccupiedCount, Is.LessThan(60 * 60),
                "the affected regular wall may not keep its full rectangular native shadow");
            Assert.That(shadow.Runs, Is.Not.Empty);
            Assert.That(shadow.CastingEdges, Is.Not.Empty);
        });

        for (int y = 0; y < 60; y++)
        for (int x = 0; x < 60; x++)
        {
            int canvasX = x + HybridRegularRasterCompositor.Padding;
            int canvasY = y + HybridRegularRasterCompositor.Padding;
            HybridWallRasterPixel thin = raster.ThinAt(canvasX, canvasY);
            bool expected = thin.IsStructural
                ? thin.Surface == HybridWallRasterSurface.Top
                : raster.OriginalAt(canvasX, canvasY).Surface == HybridWallRasterSurface.Top;
            Assert.That(shadow.Contains(x, y), Is.EqualTo(expected), $"native top mask mismatch at {x},{y}");
        }

        foreach (HybridWallShadowEdge edge in shadow.CastingEdges)
        {
            Assert.That(edge.MinX, Is.InRange(0, 60));
            Assert.That(edge.MaxX, Is.InRange(0, 60));
            Assert.That(edge.MinY, Is.InRange(0, 60));
            Assert.That(edge.MaxY, Is.InRange(0, 60));
            Assert.That(edge.MinX == edge.MaxX || edge.MinY == edge.MaxY, Is.True,
                "shadow boundary edges must remain axis-aligned raster contour segments");
        }
    }

    [Test]
    public void EveryHybridStructuralPixelRetainsItsExactContactVisualIdentity()
    {
        var alpha = new byte[60 * 60];
        Fill(alpha, (byte)255);
        HybridRegularCompositePlan plan = HybridRegularRasterCompositor.Compile(
            alpha,
            alpha,
            new[]
            {
                new HybridWallCornerRaster(
                    HybridWallQuadrant.NorthEast,
                    HybridWallRayMask.West,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None),
                new HybridWallCornerRaster(
                    HybridWallQuadrant.SouthWest,
                    HybridWallRayMask.East,
                    HybridWallRayMask.None,
                    HybridWallRayMask.None),
            });
        var visuals = new[]
        {
            new HybridWallContactVisual(
                new HybridWallContactKey(HybridWallQuadrant.NorthEast, HybridWallRayMask.West),
                materialId: 101,
                ThinWallMaterialFamily.Wood,
                ThinWallDamageGrade.Moderate,
                door: false),
            new HybridWallContactVisual(
                new HybridWallContactKey(HybridWallQuadrant.SouthWest, HybridWallRayMask.East),
                materialId: 202,
                ThinWallMaterialFamily.Metal,
                ThinWallDamageGrade.Severe,
                door: false),
        };
        int westIndex = Enumerable.Range(0, 120 * 120).First(index =>
            plan.ContactAt(index) == visuals[0].Contact && plan.ThinAt(index).IsStructural);
        int eastIndex = Enumerable.Range(0, 120 * 120).First(index =>
            plan.ContactAt(index) == visuals[1].Contact && plan.ThinAt(index).IsStructural);

        Assert.Multiple(() =>
        {
            Assert.That(HybridWallContactVisualSelector.Select(plan, westIndex, visuals), Is.EqualTo(visuals[0]));
            Assert.That(HybridWallContactVisualSelector.Select(plan, eastIndex, visuals), Is.EqualTo(visuals[1]));
        });
    }

    [Test]
    public void DoubledHybridContactKeepsOpposingOwnerMaterialsAndDamageOnTheirOwnHalves()
    {
        var alpha = new byte[60 * 60];
        Fill(alpha, (byte)255);
        HybridWallContactKey contact = new(HybridWallQuadrant.NorthEast, HybridWallRayMask.West);
        HybridRegularCompositePlan plan = HybridRegularRasterCompositor.Compile(
            alpha,
            alpha,
            new[]
            {
                new HybridWallCornerRaster(
                    contact.Quadrant,
                    contact.Ray,
                    contact.Ray,
                    HybridWallRayMask.None),
            });
        var visuals = new[]
        {
            new HybridWallContactVisual(
                contact,
                materialId: 301,
                ThinWallMaterialFamily.Stone,
                ThinWallDamageGrade.Heavy,
                door: false,
                ownerSide: ThinWallSide.North),
            new HybridWallContactVisual(
                contact,
                materialId: 302,
                ThinWallMaterialFamily.Metal,
                ThinWallDamageGrade.None,
                door: false,
                ownerSide: ThinWallSide.South),
        };
        int northIndex = Enumerable.Range(0, 120 * 120).First(index =>
            plan.ContactAt(index) == contact &&
            plan.ThinAt(index).Surface == HybridWallRasterSurface.Front);
        int southIndex = Enumerable.Range(0, 120 * 120).First(index =>
            plan.ContactAt(index) == contact &&
            plan.ThinAt(index).Surface == HybridWallRasterSurface.Top &&
            plan.ThinAt(index).SourceY >= 42);

        Assert.Multiple(() =>
        {
            HybridWallContactVisual north = HybridWallContactVisualSelector.Select(plan, northIndex, visuals);
            HybridWallContactVisual south = HybridWallContactVisualSelector.Select(plan, southIndex, visuals);
            Assert.That(north.MaterialId, Is.EqualTo(301));
            Assert.That(north.Damage, Is.EqualTo(ThinWallDamageGrade.Heavy));
            Assert.That(south.MaterialId, Is.EqualTo(302));
            Assert.That(south.Damage, Is.EqualTo(ThinWallDamageGrade.None));
        });
    }

    private static int CanvasIndex(int nativeX, int nativeY) =>
        (nativeY + HybridRegularRasterCompositor.Padding) * HybridRegularRasterCompositor.CanvasSize +
        nativeX + HybridRegularRasterCompositor.Padding;

    private static void Fill<T>(T[] values, T value)
    {
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = value;
        }
    }
}
