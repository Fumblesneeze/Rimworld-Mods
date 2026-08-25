using System.Collections.Generic;
using NUnit.Framework;
using ThinWalls.Geometry;
using ThinWalls.Rendering;
using UnityEngine;
using Verse;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class AdjacentBuildingVisualOffsetTests
{
    private static readonly CellRect Footprint = new(10, 20, 2, 3);

    [Test]
    public void CoreHandTailoringBenchUsesDirectionalWallFacingAlphaRatherThanHalfTotalDepth()
    {
        var alpha = new DirectionalAlphaBounds(
            textureWidth: 224,
            textureHeight: 96,
            minX: 16,
            minY: 16,
            maxXExclusive: 208,
            maxYExclusive: 89);

        DirectionalClearance clearance = AdjacentBuildingVisualOffset.MeasureClearance(
            alpha,
            drawSize: new Vector2(3.5f, 1.5f),
            footprintSize: new IntVec2(3, 1),
            horizontalWallHalfDepth: 17.5f / HybridWallRasterPlan.Size,
            verticalWallHalfDepth: 17f / HybridWallRasterPlan.Size);

        Assert.Multiple(() =>
        {
            Assert.That(clearance.North, Is.EqualTo(19f / 60f).Within(0.000001f),
                "the north-facing half is only 32 pixels deep and needs its own minimum offset plus one safety pixel");
            Assert.That(clearance.South, Is.EqualTo(27f / 60f).Within(0.000001f),
                "the south-facing half is 41 pixels deep; symmetric 22/60 hid this side behind the wall");
            Assert.That(clearance.North, Is.Not.EqualTo(clearance.South),
                "opposite sides may not be assumed symmetric from the total alpha bound");
        });
    }

    [TestCase(ThinWallSide.North, 0f, -19f / 60f)]
    [TestCase(ThinWallSide.South, 0f, 27f / 60f)]
    [TestCase(ThinWallSide.East, -13f / 60f, 0f)]
    [TestCase(ThinWallSide.West, 11f / 60f, 0f)]
    public void EveryExteriorSideMovesTheSpriteAwayFromTheThinEdge(
        ThinWallSide side,
        float expectedX,
        float expectedZ)
    {
        SharedEdge contact = PerimeterEdge(Footprint, side);
        var clearance = new DirectionalClearance(
            north: 19f / 60f,
            east: 13f / 60f,
            south: 27f / 60f,
            west: 11f / 60f);

        Vector3 offset = AdjacentBuildingVisualOffset.Resolve(
            Footprint,
            edge => edge.Equals(contact),
            clearance);

        Assert.That(offset.x, Is.EqualTo(expectedX).Within(0.000001f));
        Assert.That(offset.z, Is.EqualTo(expectedZ).Within(0.000001f));
    }

    [Test]
    public void MultipleSegmentsOnOneSideContributeOnlyOneDirectionalClearance()
    {
        var contacts = new HashSet<SharedEdge>();
        for (int x = Footprint.minX; x <= Footprint.maxX; x++)
        {
            contacts.Add(new OwnedEdge(
                new IntVec3(x, 0, Footprint.maxZ),
                ThinWallSide.North).Shared);
        }

        Vector3 offset = AdjacentBuildingVisualOffset.Resolve(
            Footprint,
            contacts.Contains,
            new DirectionalClearance(19f / 60f, 13f / 60f, 27f / 60f, 11f / 60f));

        Assert.That(offset, Is.EqualTo(new Vector3(0f, 0f, -19f / 60f)));
    }

    [Test]
    public void OppositeSidesCancelAndPerpendicularSidesCompose()
    {
        SharedEdge north = PerimeterEdge(Footprint, ThinWallSide.North);
        SharedEdge south = PerimeterEdge(Footprint, ThinWallSide.South);
        SharedEdge east = PerimeterEdge(Footprint, ThinWallSide.East);

        var clearance = new DirectionalClearance(
            north: 19f / 60f,
            east: 13f / 60f,
            south: 27f / 60f,
            west: 11f / 60f);
        Vector3 combined = AdjacentBuildingVisualOffset.Resolve(
            Footprint,
            edge => edge.Equals(north) || edge.Equals(south),
            clearance);
        Vector3 corner = AdjacentBuildingVisualOffset.Resolve(
            Footprint,
            edge => edge.Equals(north) || edge.Equals(east),
            clearance);

        Assert.Multiple(() =>
        {
            Assert.That(combined.x, Is.EqualTo(0f).Within(0.000001f));
            Assert.That(combined.z, Is.EqualTo(8f / 60f).Within(0.000001f),
                "opposite contacts combine signed direction-specific values and cancel only when equal");
            Assert.That(corner.x, Is.EqualTo(-13f / 60f).Within(0.000001f));
            Assert.That(corner.z, Is.EqualTo(-19f / 60f).Within(0.000001f));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void EastFacingNonSquareGraphicMeasuresItsActualWorldAxes(bool shouldDrawRotated)
    {
        var alpha = new DirectionalAlphaBounds(200, 100, 0, 0, 200, 100);

        DirectionalAlphaExtents extents = AdjacentBuildingVisualOffset.ProjectAlphaBounds(
            alpha,
            new Vector2(4f, 2f),
            Rot4.East,
            shouldDrawRotated,
            flipped: false,
            drawOffset: Vector3.zero,
            rotatedExtraAngle: 0f);

        Assert.Multiple(() =>
        {
            Assert.That(extents.North, Is.EqualTo(2f).Within(0.000001f));
            Assert.That(extents.South, Is.EqualTo(2f).Within(0.000001f));
            Assert.That(extents.East, Is.EqualTo(1f).Within(0.000001f));
            Assert.That(extents.West, Is.EqualTo(1f).Within(0.000001f));
        });
    }

    [Test]
    public void PerRotationGraphicDrawOffsetParticipatesInEveryFacingExtent()
    {
        var alpha = new DirectionalAlphaBounds(200, 100, 0, 0, 200, 100);

        DirectionalAlphaExtents extents = AdjacentBuildingVisualOffset.ProjectAlphaBounds(
            alpha,
            new Vector2(4f, 2f),
            Rot4.North,
            shouldDrawRotated: false,
            flipped: false,
            drawOffset: new Vector3(0.25f, 0f, 0.6f),
            rotatedExtraAngle: 0f);

        Assert.Multiple(() =>
        {
            Assert.That(extents.North, Is.EqualTo(1.6f).Within(0.000001f));
            Assert.That(extents.South, Is.EqualTo(0.4f).Within(0.000001f));
            Assert.That(extents.East, Is.EqualTo(2.25f).Within(0.000001f));
            Assert.That(extents.West, Is.EqualTo(1.75f).Within(0.000001f));
        });
    }

    [Test]
    public void CachedRotatedFlippedGraphicUsesOnlyNativeOneHundredEightyDegreeAngleForAsymmetricAlpha()
    {
        var alpha = new DirectionalAlphaBounds(100, 100, 10, 20, 40, 60);

        DirectionalAlphaExtents extents = AdjacentBuildingVisualOffset.ProjectAlphaBounds(
            alpha,
            new Vector2(4f, 2f),
            Rot4.East,
            shouldDrawRotated: true,
            flipped: true,
            drawOffset: Vector3.zero,
            rotatedExtraAngle: 0f,
            flipExtraRotation: 0f,
            useRealtimeDrawWorker: false);

        Assert.Multiple(() =>
        {
            Assert.That(extents.North, Is.EqualTo(-0.4f).Within(0.000001f));
            Assert.That(extents.South, Is.EqualTo(1.6f).Within(0.000001f));
            Assert.That(extents.East, Is.EqualTo(0.2f).Within(0.000001f));
            Assert.That(extents.West, Is.EqualTo(0.6f).Within(0.000001f));
        });
    }

    [Test]
    public void RealtimeRotatedFlippedGraphicUsesMeshUvFlipAndNativeOneHundredEightyDegreeAngle()
    {
        var alpha = new DirectionalAlphaBounds(100, 100, 10, 20, 40, 60);

        DirectionalAlphaExtents extents = AdjacentBuildingVisualOffset.ProjectAlphaBounds(
            alpha,
            new Vector2(4f, 2f),
            Rot4.East,
            shouldDrawRotated: true,
            flipped: true,
            drawOffset: Vector3.zero,
            rotatedExtraAngle: 0f,
            flipExtraRotation: 0f,
            useRealtimeDrawWorker: true);

        Assert.Multiple(() =>
        {
            Assert.That(extents.North, Is.EqualTo(1.6f).Within(0.000001f));
            Assert.That(extents.South, Is.EqualTo(-0.4f).Within(0.000001f));
            Assert.That(extents.East, Is.EqualTo(0.2f).Within(0.000001f));
            Assert.That(extents.West, Is.EqualTo(0.6f).Within(0.000001f));
        });
    }

    [Test]
    public void NonRotatedFlippedPrintUsesGraphicDataExtraRotationForAsymmetricAlpha()
    {
        var alpha = new DirectionalAlphaBounds(100, 100, 10, 20, 40, 60);

        DirectionalAlphaExtents extents = AdjacentBuildingVisualOffset.ProjectAlphaBounds(
            alpha,
            new Vector2(4f, 2f),
            Rot4.East,
            shouldDrawRotated: false,
            flipped: true,
            drawOffset: Vector3.zero,
            rotatedExtraAngle: 0f,
            flipExtraRotation: 90f);

        Assert.Multiple(() =>
        {
            Assert.That(extents.North, Is.EqualTo(-0.2f).Within(0.000001f));
            Assert.That(extents.South, Is.EqualTo(0.8f).Within(0.000001f));
            Assert.That(extents.East, Is.EqualTo(1.2f).Within(0.000001f));
            Assert.That(extents.West, Is.EqualTo(0.4f).Within(0.000001f));
        });
    }

    [Test]
    public void FrameUsesItsActualOnePointFifteenFootprintPlaneInsteadOfCompletedBuildingSprite()
    {
        DirectionalClearance clearance = AdjacentBuildingVisualOffset.MeasureOpaquePlaneClearance(
            drawSize: new Vector2(3f * 1.15f, 1f * 1.15f),
            footprintSize: new IntVec2(3, 1),
            horizontalWallHalfDepth: 17.5f / HybridWallRasterPlan.Size,
            verticalWallHalfDepth: 17f / HybridWallRasterPlan.Size);

        Assert.Multiple(() =>
        {
            Assert.That(clearance.North, Is.EqualTo(23f / 60f).Within(0.000001f));
            Assert.That(clearance.South, Is.EqualTo(23f / 60f).Within(0.000001f));
        });
    }

    [Test]
    public void EastFacingAttachmentFrameIncludesNativeHalfCellDrawTranslation()
    {
        DirectionalClearance clearance = AdjacentBuildingVisualOffset.MeasureOpaquePlaneClearance(
            drawSize: new Vector2(1.15f, 1.15f),
            footprintSize: new IntVec2(1, 1),
            horizontalWallHalfDepth: 17.5f / HybridWallRasterPlan.Size,
            verticalWallHalfDepth: 17f / HybridWallRasterPlan.Size,
            drawOffset: new Vector3(0.5f, 0f, 0f));

        Assert.Multiple(() =>
        {
            Assert.That(clearance.East, Is.EqualTo(53f / 60f).Within(0.000001f));
            Assert.That(clearance.West, Is.Zero.Within(0.000001f));
        });
    }

    private static SharedEdge PerimeterEdge(CellRect footprint, ThinWallSide side) => side switch
    {
        ThinWallSide.North => new OwnedEdge(
            new IntVec3(footprint.minX, 0, footprint.maxZ), side).Shared,
        ThinWallSide.South => new OwnedEdge(
            new IntVec3(footprint.minX, 0, footprint.minZ), side).Shared,
        ThinWallSide.East => new OwnedEdge(
            new IntVec3(footprint.maxX, 0, footprint.minZ), side).Shared,
        ThinWallSide.West => new OwnedEdge(
            new IntVec3(footprint.minX, 0, footprint.minZ), side).Shared,
        _ => throw new System.ArgumentOutOfRangeException(nameof(side)),
    };
}
