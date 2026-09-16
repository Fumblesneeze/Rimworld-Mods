using System.Collections.Generic;
using ImmersiveChefs.VisualTesting;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class GalleryClearanceTests
{
    private static readonly (int, int, int, int)[] Footprints =
        { (8, 8, 9, 8), (3, 8, 5, 8), (13, 8, 15, 8) };

    [TestCase(8, 8)] // The workstation itself.
    [TestCase(6, 6)] // Its diagonal visual margin.
    [TestCase(17, 10)] // Only the stove's outer margin.
    [TestCase(1, 8)] // Only the butcher's outer margin.
    public void A_blocked_cell_in_any_footprint_or_margin_rejects_the_gallery(int x, int z)
    {
        Assert.That(GalleryClearance.IsClear(Footprints,
            (cellX, cellZ) => cellX != x || cellZ != z), Is.False);
    }

    [Test]
    public void Objects_beyond_the_margin_do_not_prevent_a_clear_gallery()
    {
        var blocked = new HashSet<(int, int)> { (0, 8), (18, 8), (8, 5), (8, 11) };
        Assert.That(GalleryClearance.IsClear(Footprints,
            (x, z) => !blocked.Contains((x, z))), Is.True);
    }

    [Test]
    public void Expanded_footprints_must_stay_inside_the_map()
    {
        Assert.That(GalleryClearance.IsClear(new[] { (1, 8, 2, 8) },
            (x, z) => x >= 0 && z >= 0 && x < 20 && z < 20), Is.False);
    }

    [Test]
    public void Every_clear_cell_allows_the_whole_gallery()
    {
        Assert.That(GalleryClearance.IsClear(Footprints, (_, _) => true), Is.True);
    }

    [Test]
    public void An_attached_mote_entering_the_margin_on_first_draw_rejects_the_gallery()
    {
        var currentMoteCells = new HashSet<(int, int)> { (0, 8) };
        var pendingMoteCells = new[] { (1, 8) };
        Assert.That(GalleryClearance.IsClear(Footprints,
            (x, z) => !currentMoteCells.Contains((x, z)), pendingMoteCells), Is.False);
        Assert.That(GalleryClearance.FindBlockedCell(Footprints,
            (x, z) => !currentMoteCells.Contains((x, z)), pendingMoteCells), Is.EqualTo((1, 8)));
    }

    [TestCase(1, 8, false)] // An already drawn position remains an obstruction.
    [TestCase(0, 8, true)] // Both positions remain outside the complete margin.
    public void Pending_mote_cells_preserve_the_existing_margin(int x, int z, bool expected)
    {
        Assert.That(GalleryClearance.IsClear(Footprints, (_, _) => true,
            new[] { (x, z) }), Is.EqualTo(expected));
    }
}
