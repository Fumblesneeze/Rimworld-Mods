using NUnit.Framework;
using System.Linq;
using ThinWalls.Geometry;
using ThinWalls.Placement;
using Verse;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class ThinWallPlacementGeometryTests
{
    [Test]
    public void TwoCellFootprintCrossingSharedEdgeIsRejectedByGeometry()
    {
        var footprint = new CellRect(4, 7, 2, 1);
        var edge = new SharedEdge(new IntVec3(4, 0, 7), ThinWallSide.East);

        Assert.That(ThinWallPlacementRules.FootprintCrossesEdge(footprint, edge), Is.True);
    }

    [Test]
    public void OneCellFootprintOnOwnerCellDoesNotCrossEdge()
    {
        var footprint = new CellRect(4, 7, 1, 1);
        var edge = new SharedEdge(new IntVec3(4, 0, 7), ThinWallSide.East);

        Assert.That(ThinWallPlacementRules.FootprintCrossesEdge(footprint, edge), Is.False);
    }

    [Test]
    public void MultiCellFootprintWhollyOnOneSideDoesNotCrossEdge()
    {
        var footprint = new CellRect(3, 7, 2, 2);
        var exterior = new SharedEdge(new IntVec3(4, 0, 7), ThinWallSide.East);

        Assert.That(ThinWallPlacementRules.FootprintCrossesEdge(footprint, exterior), Is.False);
    }

    [Test]
    public void OppositeOwnerDescriptionsConflictOnOnePhysicalSharedEdge()
    {
        var southOwner = new OwnedEdge(new IntVec3(4, 0, 7), ThinWallSide.North);
        var northOwner = new OwnedEdge(new IntVec3(4, 0, 8), ThinWallSide.South);

        Assert.That(ThinWallPlacementRules.ConflictsOnSharedEdge(southOwner, northOwner), Is.True);
    }

    [Test]
    public void DifferentEdgesOfOneCellDoNotConflict()
    {
        var north = new OwnedEdge(new IntVec3(4, 0, 7), ThinWallSide.North);
        var east = new OwnedEdge(new IntVec3(4, 0, 7), ThinWallSide.East);

        Assert.That(ThinWallPlacementRules.ConflictsOnSharedEdge(north, east), Is.False);
    }

    [TestCase(2, 3)]
    [TestCase(3, 2)]
    public void EveryInternalEdgeOfEveryNonSquareRotationIsAForbiddenCrossing(
        int baseWidth,
        int baseHeight)
    {
        for (int rotation = 0; rotation < 4; rotation++)
        {
            var rot = new Rot4(rotation);
            CellRect footprint = GenAdj.OccupiedRect(
                new IntVec3(10, 0, 20),
                rot,
                new IntVec2(baseWidth, baseHeight));
            int width = footprint.Width;
            int height = footprint.Height;
            SharedEdge[] internalEdges = ThinWallPlacementRules.InternalEdges(footprint).ToArray();

            Assert.That(
                internalEdges,
                Has.Length.EqualTo((width - 1) * height + (height - 1) * width),
                $"rotation {rotation}");
            Assert.That(
                internalEdges.All(edge => ThinWallPlacementRules.FootprintCrossesEdge(footprint, edge)),
                Is.True,
                $"rotation {rotation}");

            foreach (SharedEdge perimeter in PerimeterEdges(footprint))
            {
                Assert.That(
                    ThinWallPlacementRules.FootprintCrossesEdge(footprint, perimeter),
                    Is.False,
                    $"rotation {rotation}, perimeter {perimeter}");
            }
        }
    }

    private static SharedEdge[] PerimeterEdges(CellRect footprint)
    {
        var result = new System.Collections.Generic.List<SharedEdge>();
        for (int x = footprint.minX; x <= footprint.maxX; x++)
        {
            result.Add(new OwnedEdge(new IntVec3(x, 0, footprint.minZ), ThinWallSide.South).Shared);
            result.Add(new OwnedEdge(new IntVec3(x, 0, footprint.maxZ), ThinWallSide.North).Shared);
        }
        for (int z = footprint.minZ; z <= footprint.maxZ; z++)
        {
            result.Add(new OwnedEdge(new IntVec3(footprint.minX, 0, z), ThinWallSide.West).Shared);
            result.Add(new OwnedEdge(new IntVec3(footprint.maxX, 0, z), ThinWallSide.East).Shared);
        }
        return result.ToArray();
    }
}
