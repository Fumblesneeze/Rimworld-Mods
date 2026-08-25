using NUnit.Framework;
using ThinWalls.Geometry;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class ThinWallDirectionTests
{
    [Test]
    public void EastwardDragOwnsSouthEdge()
    {
        ThinWallSide actual = ThinWallDirection.RightHandSide(7, 0, ThinWallSide.North);

        Assert.That(actual, Is.EqualTo(ThinWallSide.South));
    }

    [TestCase(0, 5, ThinWallSide.East)]
    [TestCase(-3, 0, ThinWallSide.North)]
    [TestCase(0, -9, ThinWallSide.West)]
    public void CardinalDragOwnsEdgeOnItsRight(int deltaX, int deltaZ, ThinWallSide expected)
    {
        ThinWallSide actual = ThinWallDirection.RightHandSide(deltaX, deltaZ, ThinWallSide.South);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ZeroLengthDragRetainsLastSide()
    {
        ThinWallSide actual = ThinWallDirection.RightHandSide(0, 0, ThinWallSide.West);

        Assert.That(actual, Is.EqualTo(ThinWallSide.West));
    }

    [TestCase(ThinWallSide.North, true, ThinWallSide.East)]
    [TestCase(ThinWallSide.East, true, ThinWallSide.South)]
    [TestCase(ThinWallSide.South, false, ThinWallSide.East)]
    [TestCase(ThinWallSide.West, false, ThinWallSide.South)]
    public void SingleCellPreviewRotatesWithQOrE(
        ThinWallSide current,
        bool clockwise,
        ThinWallSide expected)
    {
        Assert.That(ThinWallDirection.Rotate(current, clockwise), Is.EqualTo(expected));
    }

    [Test]
    public void DragDirectionOverridesPreviewOnlyAfterLeavingStartCell()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ThinWallDirection.SideForDrag(0, 0, ThinWallSide.West), Is.EqualTo(ThinWallSide.West));
            Assert.That(ThinWallDirection.SideForDrag(1, 0, ThinWallSide.West), Is.EqualTo(ThinWallSide.South));
        });
    }

    [Test]
    public void ReturningToStartCellRestoresTheOrientationChosenBeforeMouseDown()
    {
        ThinWallSide chosenBeforeMouseDown = ThinWallSide.North;

        Assert.Multiple(() =>
        {
            Assert.That(
                ThinWallDirection.SideForDrag(1, 0, chosenBeforeMouseDown),
                Is.EqualTo(ThinWallSide.South),
                "A real second cell gives the drag vector authority while the drag remains extended.");
            Assert.That(
                ThinWallDirection.SideForDrag(0, 0, chosenBeforeMouseDown),
                Is.EqualTo(ThinWallSide.North),
                "Returning to the origin before release must not turn pointer jitter into an orientation change.");
        });
    }
}
