using NUnit.Framework;
using ThinWalls.Rendering;
using UnityEngine;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class HybridWallDoorGeometryTests
{
    [TestCase(0f, -0.45f, 0f, 0f, 0.45f, 0f)]
    [TestCase(0.05f, -0.45f, 0f, 0f, 0.45f, 0f)]
    [TestCase(0.5f, -0.60f, -0.15f, 0.15f, 0.60f, 0.30f)]
    [TestCase(1f, -0.75f, -0.30f, 0.30f, 0.75f, 0.60f)]
    public void LeavesRetractLongitudinallyAndExposeTheRequestedOpening(
        float openFraction,
        float leftMin,
        float leftMax,
        float rightMin,
        float rightMax,
        float opening)
    {
        HybridWallDoorPlan plan = HybridWallDoorGeometryCompiler.Compile(openFraction);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Left.Min, Is.EqualTo(leftMin).Within(0.000001f));
            Assert.That(plan.Left.Max, Is.EqualTo(leftMax).Within(0.000001f));
            Assert.That(plan.Left.UvMin, Is.EqualTo(0.05f).Within(0.000001f));
            Assert.That(plan.Left.UvMax, Is.EqualTo(0.5f).Within(0.000001f));
            Assert.That(plan.Right.Min, Is.EqualTo(rightMin).Within(0.000001f));
            Assert.That(plan.Right.Max, Is.EqualTo(rightMax).Within(0.000001f));
            Assert.That(plan.Right.UvMin, Is.EqualTo(0.5f).Within(0.000001f));
            Assert.That(plan.Right.UvMax, Is.EqualTo(0.95f).Within(0.000001f));
            Assert.That(plan.OpeningWidth, Is.EqualTo(opening).Within(0.00001f));
            Assert.That(plan.Left.Max, Is.LessThanOrEqualTo(plan.Right.Min));
        });
    }

    [TestCase(0f)]
    [TestCase(0.5f)]
    [TestCase(1f)]
    public void MovingLeafIntervalsAndUvsNeverOverlapTheFixedEndpointFrames(float openFraction)
    {
        HybridWallDoorPlan plan = HybridWallDoorGeometryCompiler.Compile(openFraction);
        float frame = HybridWallDoorGeometryCompiler.FrameLength;

        Assert.Multiple(() =>
        {
            Assert.That(plan.Left.Min, Is.GreaterThanOrEqualTo(-0.5f + frame - openFraction * 0.5f));
            Assert.That(plan.Right.Max, Is.LessThanOrEqualTo(0.5f - frame + openFraction * 0.5f));
            Assert.That(plan.Left.UvMin, Is.EqualTo(frame).Within(0.000001f));
            Assert.That(plan.Left.UvMax, Is.EqualTo(0.5f).Within(0.000001f));
            Assert.That(plan.Right.UvMin, Is.EqualTo(0.5f).Within(0.000001f));
            Assert.That(plan.Right.UvMax, Is.EqualTo(1f - frame).Within(0.000001f));
        });
    }

    [Test]
    public void RegularContactFramePixelsAndClosedRealtimeLeafPartitionTheWholeEdgeWithoutOverlap()
    {
        HybridWallDoorPlan plan = HybridWallDoorGeometryCompiler.Compile(0f);
        float frame = HybridWallDoorGeometryCompiler.FrameLength;

        Assert.Multiple(() =>
        {
            Assert.That(frame * HybridWallRasterPlan.Size, Is.EqualTo(3f).Within(0.000001f));
            Assert.That(plan.Left.Min, Is.EqualTo(-0.5f + frame).Within(0.000001f));
            Assert.That(plan.Right.Max, Is.EqualTo(0.5f - frame).Within(0.000001f));
            Assert.That(plan.Left.Min - (-0.5f), Is.EqualTo(frame).Within(0.000001f));
            Assert.That(0.5f - plan.Right.Max, Is.EqualTo(frame).Within(0.000001f));
            Assert.That(plan.Left.Max, Is.EqualTo(plan.Right.Min).Within(0.000001f),
                "the two trimmed leaves must still form one closed union between the fixed frames");
        });
    }

    [TestCase(-1f)]
    [TestCase(2f)]
    public void OpenFractionIsClampedToVanillaMoverBounds(float openFraction)
    {
        HybridWallDoorPlan plan = HybridWallDoorGeometryCompiler.Compile(openFraction);

        Assert.That(plan.OpeningWidth, Is.InRange(0f, 1f));
    }

    [Test]
    public void ClosedPhaseUsesOneContinuousLeafUnion()
    {
        HybridWallDoorPlan closed = HybridWallDoorGeometryCompiler.Compile(0f);
        HybridWallDoorPlan threshold = HybridWallDoorGeometryCompiler.Compile(0.05f);

        Assert.Multiple(() =>
        {
            Assert.That(closed.IsFullyClosed, Is.True);
            Assert.That(threshold.IsFullyClosed, Is.True);
            Assert.That(threshold.Left, Is.EqualTo(closed.Left),
                "the closed animation threshold must not slide or stretch into the fixed frame");
            Assert.That(threshold.Right, Is.EqualTo(closed.Right),
                "the closed animation threshold must not slide or stretch into the fixed frame");
            Assert.That(HybridWallDoorGeometryCompiler.Compile(0.051f).IsFullyClosed, Is.False);
        });
    }

    [Test]
    public void FullyOpenDoorRetainsTwoRealLeafTipsWithoutBlockingTheCenteredPassage()
    {
        HybridWallDoorPlan plan = HybridWallDoorGeometryCompiler.Compile(1f);

        Assert.Multiple(() =>
        {
            Assert.That(HybridWallDoorGeometryCompiler.MaximumSlide, Is.EqualTo(0.30f));
            Assert.That(plan.OpeningWidth, Is.EqualTo(0.60f).Within(0.000001f));
            Assert.That(plan.Left.Max, Is.EqualTo(-0.30f).Within(0.000001f));
            Assert.That(plan.Right.Min, Is.EqualTo(0.30f).Within(0.000001f));
            Assert.That(plan.Left.Max - (-0.45f), Is.EqualTo(0.15f).Within(0.000001f),
                "the left leaf must retain a 0.15-cell panel, not the rejected hairline tip");
            Assert.That(0.45f - plan.Right.Min, Is.EqualTo(0.15f).Within(0.000001f),
                "the right leaf must retain a 0.15-cell panel, not the rejected hairline tip");
        });
    }

    [TestCase(0f, -0.45f, 0f, 0f, 0.45f, 0.05f, 0.5f, 0.5f, 0.95f)]
    [TestCase(0.5f, -0.45f, -0.15f, 0.15f, 0.45f, 0.20f, 0.5f, 0.5f, 0.80f)]
    [TestCase(1f, -0.45f, -0.30f, 0.30f, 0.45f, 0.35f, 0.5f, 0.5f, 0.65f)]
    public void VisibleLeavesAreClippedToTheFramedApertureWithMatchingUvs(
        float openFraction,
        float leftMin,
        float leftMax,
        float rightMin,
        float rightMax,
        float leftUvMin,
        float leftUvMax,
        float rightUvMin,
        float rightUvMax)
    {
        HybridWallDoorPlan visible = HybridWallDoorGeometryCompiler.CompileVisible(openFraction);

        Assert.Multiple(() =>
        {
            Assert.That(visible.Left.Min, Is.EqualTo(leftMin).Within(0.000001f));
            Assert.That(visible.Left.Max, Is.EqualTo(leftMax).Within(0.000001f));
            Assert.That(visible.Left.UvMin, Is.EqualTo(leftUvMin).Within(0.000001f));
            Assert.That(visible.Left.UvMax, Is.EqualTo(leftUvMax).Within(0.000001f));
            Assert.That(visible.Right.Min, Is.EqualTo(rightMin).Within(0.000001f));
            Assert.That(visible.Right.Max, Is.EqualTo(rightMax).Within(0.000001f));
            Assert.That(visible.Right.UvMin, Is.EqualTo(rightUvMin).Within(0.000001f));
            Assert.That(visible.Right.UvMax, Is.EqualTo(rightUvMax).Within(0.000001f));
            Assert.That(visible.Left.Min, Is.GreaterThanOrEqualTo(-0.5f + visible.FrameLength));
            Assert.That(visible.Right.Max, Is.LessThanOrEqualTo(0.5f - visible.FrameLength));
            Assert.That(visible.OpeningWidth,
                Is.EqualTo(HybridWallDoorGeometryCompiler.Compile(openFraction).OpeningWidth)
                    .Within(0.000001f),
                "clipping the hidden retracted portions must not change the traversable opening");
        });
    }

    [Test]
    public void OpeningDoorShadowUnionsEachVisibleLeafWithItsFixedFrame()
    {
        HybridWallDoorShadowPlan shadow = HybridWallDoorGeometryCompiler.CompileShadow(1f);

        Assert.Multiple(() =>
        {
            Assert.That(shadow.LeftMin, Is.EqualTo(-0.5f).Within(0.000001f));
            Assert.That(shadow.LeftMax, Is.EqualTo(-0.30f).Within(0.000001f));
            Assert.That(shadow.RightMin, Is.EqualTo(0.30f).Within(0.000001f));
            Assert.That(shadow.RightMax, Is.EqualTo(0.5f).Within(0.000001f));
            Assert.That(shadow.RightMin - shadow.LeftMax, Is.EqualTo(0.60f).Within(0.000001f));
            Assert.That(shadow.LeftMax - shadow.LeftMin, Is.EqualTo(0.20f).Within(0.000001f),
                "the left shadow must include the fixed 0.05 frame plus visible 0.15 leaf panel");
            Assert.That(shadow.RightMax - shadow.RightMin, Is.EqualTo(0.20f).Within(0.000001f),
                "the right shadow must include the visible 0.15 leaf panel plus fixed 0.05 frame");
        });
    }

    [Test]
    public void OnlyDoorRaysRetainedByTheThinVertexUseTheRaisedThinFrameAltitude()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HybridWallDoorGeometryCompiler.HasThinOwnedFrame(
                HybridWallRayMask.West,
                HybridWallRayMask.West), Is.True);
            Assert.That(HybridWallDoorGeometryCompiler.HasThinOwnedFrame(
                HybridWallRayMask.West,
                HybridWallRayMask.East), Is.False,
                "a regular-owned door ray removed from this vertex must not raise a remaining Thin-wall outline");
        });
    }

    [Test]
    public void EndpointFramesAreFlushSlenderJambsRatherThanRaisedPosts()
    {
        HybridWallDoorPlan plan = HybridWallDoorGeometryCompiler.Compile(0.4f);

        Assert.Multiple(() =>
        {
            Assert.That(plan.FrameLength, Is.EqualTo(3f / 60f));
            Assert.That(plan.FrameTopWidth, Is.EqualTo(HybridWallProjectionProfile.ThinTopWidth));
            Assert.That(plan.ThinFrameAboveClosedLeafOffset,
                Is.EqualTo(HybridWallDoorGeometryCompiler.ThinFrameAboveClosedLeafOffset));
            Assert.That(plan.ThinFrameAboveClosedLeafOffset, Is.GreaterThan(0f),
                "a Thin-owned fixed endpoint frame must remain visible above the closed leaf");
            Assert.That(plan.HasRaisedCap, Is.False);
        });
    }

    [Test]
    public void ClosedLeavesStayAboveWallsButOpeningLeavesRetractBeneathThem()
    {
        const float nativeWallAltitude = 15f;
        const float completedWallAltitude = 15.006f;

        float closedLeafAltitude = HybridWallDoorGeometryCompiler.LeafAltitude(
            nativeWallAltitude,
            completedWallAltitude,
            openFraction: 0f);
        float movingLeafAltitude = HybridWallDoorGeometryCompiler.LeafAltitude(
            nativeWallAltitude,
            completedWallAltitude,
            openFraction: 0.5f);
        float thinFrameAltitude = HybridWallDoorGeometryCompiler.ThinFrameAltitude(completedWallAltitude);
        float regularFrameAltitude = HybridWallDoorGeometryCompiler.RegularFrameAltitude(nativeWallAltitude);

        Assert.Multiple(() =>
        {
            Assert.That(closedLeafAltitude,
                Is.EqualTo(completedWallAltitude + 0.004f).Within(0.000001f));
            Assert.That(movingLeafAltitude,
                Is.EqualTo(nativeWallAltitude - 0.001f).Within(0.000001f));
            Assert.That(thinFrameAltitude,
                Is.EqualTo(completedWallAltitude + 0.006f).Within(0.000001f));
            Assert.That(regularFrameAltitude,
                Is.EqualTo(nativeWallAltitude).Within(0.000001f),
                "the regular-owned frame remains part of the native hybrid wall plane");
            Assert.That(closedLeafAltitude, Is.GreaterThan(completedWallAltitude));
            Assert.That(movingLeafAltitude, Is.LessThan(nativeWallAltitude));
            Assert.That(closedLeafAltitude, Is.LessThan(thinFrameAltitude));
            Assert.That(thinFrameAltitude - completedWallAltitude, Is.LessThan(0.01f),
                "the fixed camera must not project the door away from its owned edge");
            Assert.That(HybridWallDoorGeometryCompiler.CustomPlaneTopVerticesAltitudeBias,
                Is.Zero,
                "custom raster perspective is already baked into the texture; mesh tilt would put north wall vertices above the door leaf");
        });
    }

    [Test]
    public void ClosedLeafPanelToneStaysMaterialReadableWithoutBecomingABlackSlab()
    {
        var source = new Color32(100, 80, 60, 211);

        Color32 toned = HybridWallDoorAppearance.PanelTone(source);

        Assert.Multiple(() =>
        {
            Assert.That(toned, Is.EqualTo(new Color32(84, 67, 50, 211)));
            Assert.That(HybridWallDoorAppearance.PanelBrightness, Is.EqualTo(0.84f));
            Assert.That(toned.a, Is.EqualTo(source.a));
        });
    }

    [Test]
    public void PanelToneAppliesOnlyToMovingLeavesAndNeverToTheFixedFrame()
    {
        var source = new Color32(180, 140, 100, 255);

        Assert.Multiple(() =>
        {
            Assert.That(HybridWallDoorAppearance.ApplyPanelTone(source, doorLeaf: true),
                Is.EqualTo(HybridWallDoorAppearance.PanelTone(source)));
            Assert.That(HybridWallDoorAppearance.ApplyPanelTone(source, doorLeaf: false),
                Is.EqualTo(source),
                "the fixed endpoint frame is part of the linked wall union and must remain source-equivalent");
        });
    }
}
