using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class DishwasherPresentationTests
{
    [TestCase(0, "north")]
    [TestCase(1, "east")]
    [TestCase(2, "south")]
    [TestCase(3, "west")]
    public void Closed_family_lookup_uses_native_cardinal_texture_keys(int rotation, string suffix)
    {
        foreach (var family in new[] { "IndustrialDishwasher", "IndustrialDishwasher_Variant01" })
        {
            const string root = "ImmersiveChefs/Things/Building/Dishwasher/";
            var closedPath = DishwasherPresentationPolicy.TexturePath(root + family,
                DishwasherPresentationState.Washing);
            Assert.That(DishwasherPresentationPolicy.CardinalTexturePath(closedPath, rotation),
                Is.EqualTo(root + family + "_Closed_" + suffix));
        }
    }

    [TestCase(0, 608f, 512f, 540f, 532f, 136f, 56f)]
    [TestCase(1, 512f, 608f, 432f, 600f, 160f, 86f)]
    [TestCase(3, 512f, 608f, 432f, 600f, 160f, 86f)]
    public void Complete_ware_quads_stay_inside_the_authored_basket_aperture(
        int rotation, float canvasCenterX, float canvasCenterY,
        float apertureX, float apertureY, float apertureWidth, float apertureHeight)
    {
        foreach (var containedCount in new[] { 1, 2, 3, 64 })
        {
            for (var index = 0; index < Math.Min(containedCount, 3); index++)
            {
                Assert.That(DishwasherPresentationPolicy.TryGetContentSlot(
                    DishwasherPresentationState.OpenLoaded, rotation, index, containedCount, out var slot), Is.True);
                var pixelX = canvasCenterX + slot.X * 256f;
                var pixelY = canvasCenterY - slot.Z * 256f;
                var halfSize = slot.Size * 256f / 2f;
                Assert.Multiple(() =>
                {
                    Assert.That(halfSize, Is.GreaterThan(0));
                    Assert.That(pixelX - halfSize, Is.GreaterThanOrEqualTo(apertureX));
                    Assert.That(pixelX + halfSize, Is.LessThanOrEqualTo(apertureX + apertureWidth));
                    Assert.That(pixelY - halfSize, Is.GreaterThanOrEqualTo(apertureY));
                    Assert.That(pixelY + halfSize, Is.LessThanOrEqualTo(apertureY + apertureHeight));
                });
            }
        }
    }

    [TestCase(DishwasherPresentationState.Empty, 0, 3, 0)]
    [TestCase(DishwasherPresentationState.Washing, 0, 3, 0)]
    [TestCase(DishwasherPresentationState.OpenLoaded, 2, 3, 0)]
    [TestCase(DishwasherPresentationState.OpenLoaded, 0, 0, 0)]
    [TestCase(DishwasherPresentationState.OpenLoaded, 0, 1, 1)]
    [TestCase(DishwasherPresentationState.OpenLoaded, 1, 2, 2)]
    [TestCase(DishwasherPresentationState.OpenLoaded, 3, 64, 3)]
    public void Basket_slots_show_only_present_items_in_unoccluded_open_views(
        DishwasherPresentationState state, int rotation, int containedCount, int expectedSlots)
    {
        var visible = 0;
        for (var index = 0; index < 5; index++)
        {
            if (DishwasherPresentationPolicy.TryGetContentSlot(state, rotation, index, containedCount, out _))
                visible++;
        }
        Assert.That(visible, Is.EqualTo(expectedSlots));
    }

    [TestCase("IndustrialDishwasher", DishwasherPresentationState.Washing, "IndustrialDishwasher_Closed")]
    [TestCase("IndustrialDishwasher_Variant01", DishwasherPresentationState.Washing, "IndustrialDishwasher_Variant01_Closed")]
    [TestCase("IndustrialDishwasher_Variant01", DishwasherPresentationState.OpenLoaded, "IndustrialDishwasher_Variant01")]
    [TestCase("IndustrialDishwasher", DishwasherPresentationState.Empty, "IndustrialDishwasher")]
    public void Operating_state_preserves_the_selected_cosmetic_family(
        string family, DishwasherPresentationState state, string expected)
    {
        const string root = "ImmersiveChefs/Things/Building/Dishwasher/";
        Assert.That(DishwasherPresentationPolicy.TexturePath(root + family, state), Is.EqualTo(root + expected));
    }

    [TestCase(false, false, true, DishwasherPresentationState.Empty)]
    [TestCase(true, true, true, DishwasherPresentationState.Washing)]
    [TestCase(true, true, false, DishwasherPresentationState.OpenLoaded)]
    [TestCase(true, false, true, DishwasherPresentationState.OpenLoaded)]
    public void Industrial_hood_closes_only_for_a_real_load_that_can_wash(
        bool hasContents, bool hasUnfinishedLoad, bool canProgress, DishwasherPresentationState expected)
    {
        Assert.That(DishwasherPresentationPolicy.Resolve(hasContents, hasUnfinishedLoad, canProgress),
            Is.EqualTo(expected));
    }
}
