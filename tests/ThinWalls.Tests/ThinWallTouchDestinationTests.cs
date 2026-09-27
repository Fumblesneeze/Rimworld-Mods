using System;
using System.Collections.Generic;
using NUnit.Framework;
using ThinWalls.Pathing;
using Verse;

namespace ThinWalls.Tests;

public sealed class ThinWallTouchDestinationTests
{
    private static CellRect TouchRect(IntVec3 target) => new CellRect(target.x - 1, target.z - 1, 3, 3);

    [Test]
    public void CardinalAndCornerCellsAcrossCompletedEdgesAreExcluded()
    {
        IntVec3 target = new IntVec3(10, 0, 10);
        var blocked = new HashSet<IntVec3>();
        // East cardinal neighbor touches across a completed east wall.
        blocked.Add(new IntVec3(11, 0, 10));
        // North-east corner neighbor only reaches the target across a wall endpoint.
        blocked.Add(new IntVec3(11, 0, 11));
        var excluded = new List<IntVec3>();

        ThinWallTouchDestination.ExcludeBlockedTouchCells(
            TouchRect(target), CellRect.SingleCell(target),
            (cell, neighbor) => blocked.Contains(cell), excluded.Add);

        Assert.That(excluded, Does.Contain(new IntVec3(11, 0, 10)));
        Assert.That(excluded, Does.Contain(new IntVec3(11, 0, 11)));
        Assert.That(excluded, Does.Not.Contain(new IntVec3(9, 0, 10)));
        Assert.That(excluded, Does.Not.Contain(new IntVec3(10, 0, 11)));
        Assert.That(excluded, Does.Not.Contain(target));
    }

    [Test]
    public void ACornerCellWithAnyOpenAdjacencyStaysTouchable()
    {
        IntVec3 target = new IntVec3(10, 0, 10);
        var excluded = new List<IntVec3>();

        // The corner cell (11,11) also borders (11,10) and (10,11); neither blocked, so
        // the corner itself keeps at least one open adjacency and must not be excluded.
        ThinWallTouchDestination.ExcludeBlockedTouchCells(
            TouchRect(target), CellRect.SingleCell(target),
            (_, _) => false, excluded.Add);

        Assert.That(excluded, Is.Empty);
    }

    [Test]
    public void TargetRectCellsAreNeverExcluded()
    {
        IntVec3 target = new IntVec3(10, 0, 10);
        CellRect targetRect = new CellRect(9, 0, 3, 3);
        CellRect touchRect = targetRect.ExpandedBy(1);
        var excluded = new List<IntVec3>();

        ThinWallTouchDestination.ExcludeBlockedTouchCells(
            touchRect, targetRect, (_, _) => true, excluded.Add);

        foreach (IntVec3 cell in targetRect.Cells)
        {
            Assert.That(excluded, Does.Not.Contain(cell));
        }

        Assert.That(excluded, Is.Not.Empty);
    }
}
