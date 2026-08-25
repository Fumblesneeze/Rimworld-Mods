using NUnit.Framework;
using ThinWalls.Rendering;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class HybridWallProjectionProfileTests
{
    [Test]
    public void CoreAtlasLayoutMatchesMaterialAtlasPoolSampling()
    {
        HybridWallUvRect none = HybridWallProjectionProfile.InnerTile(0);
        HybridWallUvRect northEastSouthWest = HybridWallProjectionProfile.InnerTile(15);

        Assert.Multiple(() =>
        {
            Assert.That(none, Is.EqualTo(new HybridWallUvRect(10f / 320f, 10f / 320f, 60f / 320f, 60f / 320f)));
            Assert.That(northEastSouthWest,
                Is.EqualTo(new HybridWallUvRect(250f / 320f, 250f / 320f, 60f / 320f, 60f / 320f)));
            Assert.That(none.Width, Is.EqualTo(0.1875f));
            Assert.That(none.Height, Is.EqualTo(0.1875f));
        });
    }

    [Test]
    public void CoreSurfaceBandsAreMeasuredIndependently()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HybridWallProjectionProfile.TopSourcePixels, Is.EqualTo(33));
            Assert.That(HybridWallProjectionProfile.ThinTopPixels, Is.EqualTo(7));
            Assert.That(HybridWallProjectionProfile.FrontFacePixels, Is.EqualTo(22));
            Assert.That(HybridWallProjectionProfile.WestSidePixels, Is.EqualTo(11));
            Assert.That(HybridWallProjectionProfile.EastSidePixels, Is.EqualTo(10));
            Assert.That(HybridWallProjectionProfile.OutlinePixels, Is.EqualTo(2));
            Assert.That(HybridWallProjectionProfile.ThinTopWidth, Is.EqualTo(7f / 60f));
            Assert.That(HybridWallProjectionProfile.FrontFaceDepth, Is.EqualTo(22f / 60f));
            Assert.That(HybridWallProjectionProfile.WestSideDepth, Is.EqualTo(11f / 60f));
            Assert.That(HybridWallProjectionProfile.EastSideDepth, Is.EqualTo(10f / 60f));
        });
    }

    [Test]
    public void UvBandsReferenceTheStraightCoreTilesWithoutRotatingTheFacade()
    {
        HybridWallUvRect horizontal = HybridWallProjectionProfile.InnerTile(10);
        HybridWallUvRect vertical = HybridWallProjectionProfile.InnerTile(5);

        Assert.Multiple(() =>
        {
            Assert.That(HybridWallProjectionProfile.TopUv,
                Is.EqualTo(new HybridWallUvRect(
                    horizontal.X + 14f / 320f,
                    horizontal.Y + 25f / 320f,
                    33f / 320f,
                    33f / 320f)));
            Assert.That(HybridWallProjectionProfile.FrontUv,
                Is.EqualTo(new HybridWallUvRect(
                    horizontal.X,
                    horizontal.Y + 3f / 320f,
                    60f / 320f,
                    22f / 320f)));
            Assert.That(HybridWallProjectionProfile.WestSideUv,
                Is.EqualTo(new HybridWallUvRect(
                    vertical.X + 3f / 320f,
                    vertical.Y,
                    11f / 320f,
                    60f / 320f)));
            Assert.That(HybridWallProjectionProfile.EastSideUv,
                Is.EqualTo(new HybridWallUvRect(
                    vertical.X + 47f / 320f,
                    vertical.Y,
                    10f / 320f,
                    60f / 320f)));
        });
    }

    [Test]
    public void PinnedNeutralBrickContrastKeepsTopDarkerThanFaces()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HybridWallProjectionProfile.BrickTopToFrontLuminance, Is.EqualTo(107.92f / 228.88f).Within(0.0001f));
            Assert.That(HybridWallProjectionProfile.BrickTopToSideLuminance, Is.EqualTo(109.41f / 173.09f).Within(0.0001f));
            Assert.That(HybridWallProjectionProfile.BrickTopToFrontLuminance, Is.LessThan(0.55f));
            Assert.That(HybridWallProjectionProfile.BrickTopToSideLuminance, Is.LessThan(0.70f));
        });
    }

    [Test]
    public void VertexCenteredSpanIsSplitAtTheCellPhaseBoundary()
    {
        HybridWallPhaseSpan[] spans = HybridWallTexturePhase.Split(-0.5f, 0.5f).ToArray();

        Assert.That(spans, Is.EqualTo(new[]
        {
            new HybridWallPhaseSpan(-0.5f, 0f, 0.5f, 1f),
            new HybridWallPhaseSpan(0f, 0.5f, 0f, 0.5f),
        }));
    }

    [Test]
    public void CellAlignedSpanUsesTheWholeMeasuredSourceWithoutASeam()
    {
        HybridWallPhaseSpan[] spans = HybridWallTexturePhase.Split(12f, 13f).ToArray();

        Assert.That(spans, Is.EqualTo(new[]
        {
            new HybridWallPhaseSpan(12f, 13f, 0f, 1f),
        }));
    }

    [TestCase(-0.53333336f, 0.53333336f)]
    [TestCase(4.9416666f, 5.0583334f)]
    [TestCase(-2f, 1f)]
    public void EverySplitStaysInsideOneMeasuredAtlasTile(float min, float max)
    {
        HybridWallPhaseSpan[] spans = HybridWallTexturePhase.Split(min, max).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(spans[0].Min, Is.EqualTo(min).Within(0.00001f));
            Assert.That(spans[spans.Length - 1].Max, Is.EqualTo(max).Within(0.00001f));
            Assert.That(spans, Is.All.Matches<HybridWallPhaseSpan>(span =>
                span.Min < span.Max &&
                span.PhaseMin >= 0f &&
                span.PhaseMax <= 1f &&
                span.PhaseMin < span.PhaseMax));
            for (int index = 1; index < spans.Length; index++)
            {
                Assert.That(spans[index - 1].Max, Is.EqualTo(spans[index].Min).Within(0.00001f));
            }
        });
    }

    [Test]
    public void SurfaceCrossingBothWorldAxesCompilesToFourInTileSamples()
    {
        HybridWallPhaseRect[] rects = HybridWallTexturePhase
            .Split(-0.1f, -0.2f, 0.1f, 0.2f)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(rects, Has.Length.EqualTo(4));
            Assert.That(rects, Is.All.Matches<HybridWallPhaseRect>(rect =>
                rect.UvMinX >= 0f && rect.UvMaxX <= 1f &&
                rect.UvMinZ >= 0f && rect.UvMaxZ <= 1f));
            Assert.That(rects.Sum(rect => (rect.MaxX - rect.MinX) * (rect.MaxZ - rect.MinZ)),
                Is.EqualTo(0.08f).Within(0.00001f));
        });
    }
}
