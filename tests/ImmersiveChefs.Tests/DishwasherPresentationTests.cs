using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class DishwasherPresentationTests
{
    [Test]
    public void Ware_beneath_the_raised_hood_is_cropped_without_squeezing_the_complete_sprite_into_view()
    {
        Assert.That(DishwasherPresentationPolicy.TryGetContentQuad(
            DishwasherPresentationState.OpenLoaded, 0, 0, 1, 1f, 1f, out var quad), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(quad.VMax, Is.LessThan(1f), "The hood must hide the rear portion of a plate.");
            Assert.That(quad.Height / (quad.VMax - quad.VMin), Is.EqualTo(80f / 256f).Within(.0001f),
                "Cropping must preserve the plate's original scale, rather than shrink it into the opening.");
            Assert.That(512f - (quad.Z + quad.Height / 2f) * 256f, Is.EqualTo(516f).Within(.001f),
                "The visible quad starts immediately below the selected hood's lower contour.");
        });
    }

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

    [TestCase(0, 608f, 512f, 540f, 516f, 136f, 56f)]
    [TestCase(1, 512f, 608f, 440f, 580f, 144f, 86f)]
    [TestCase(3, 512f, 608f, 440f, 580f, 144f, 86f)]
    public void Visible_ware_fragments_stay_inside_the_chamber_and_preserve_source_aspect(
        int rotation, float canvasCenterX, float canvasCenterY,
        float apertureX, float apertureY, float apertureWidth, float apertureHeight)
    {
        foreach (var containedCount in new[] { 1, 2, 3, 64 })
        {
            for (var index = 0; index < Math.Min(containedCount, 3); index++)
            {
                foreach (var aspect in new[] { .5f, 1f, 2f })
                {
                    Assert.That(DishwasherPresentationPolicy.TryGetContentQuad(
                        DishwasherPresentationState.OpenLoaded, rotation, index, containedCount,
                        aspect, 1f, out var quad), Is.True);
                    var pixelX = canvasCenterX + quad.X * 256f;
                    var pixelY = canvasCenterY - quad.Z * 256f;
                    var halfWidth = quad.Width * 256f / 2f;
                    var halfHeight = quad.Height * 256f / 2f;
                    Assert.Multiple(() =>
                    {
                        Assert.That(pixelX - halfWidth, Is.GreaterThanOrEqualTo(apertureX));
                        Assert.That(pixelX + halfWidth, Is.LessThanOrEqualTo(apertureX + apertureWidth));
                        Assert.That(pixelY - halfHeight, Is.GreaterThanOrEqualTo(apertureY));
                        Assert.That(pixelY + halfHeight, Is.LessThanOrEqualTo(apertureY + apertureHeight));
                        Assert.That(quad.UMin, Is.InRange(0f, 1f));
                        Assert.That(quad.UMax, Is.InRange(quad.UMin, 1f));
                        Assert.That(quad.VMin, Is.InRange(0f, 1f));
                        Assert.That(quad.VMax, Is.InRange(quad.VMin, 1f));
                        Assert.That((quad.Width / (quad.UMax - quad.UMin)) /
                                    (quad.Height / (quad.VMax - quad.VMin)), Is.EqualTo(aspect).Within(.0001f));
                    });
                }
            }
        }
    }

    [TestCase(DishwasherPresentationState.Empty, 0, 1f)]
    [TestCase(DishwasherPresentationState.Washing, 1, 1f)]
    [TestCase(DishwasherPresentationState.OpenLoaded, 2, 1f)]
    [TestCase(DishwasherPresentationState.OpenLoaded, 0, .05f)]
    public void Fully_hidden_contents_do_not_get_a_visible_quad(
        DishwasherPresentationState state, int rotation, float height)
    {
        Assert.That(DishwasherPresentationPolicy.TryGetContentQuad(state, rotation,
            0, 3, 1f, height, out _), Is.False);
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
