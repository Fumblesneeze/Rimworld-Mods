using NUnit.Framework;
using Verse;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class PortableTextureVariationPolicyTests
{
    [TestCase("Base", "ImmersiveChefs/Things/Item/Plate/Plate")]
    [TestCase("BaseDirty", "ImmersiveChefs/Things/Item/Plate/Plate_Dirty")]
    [TestCase("Wood", "ImmersiveChefs/Things/Item/Plate/Plate_Wood")]
    [TestCase("WoodDirty", "ImmersiveChefs/Things/Item/Plate/Plate_WoodDirty")]
    [TestCase("Stone", "ImmersiveChefs/Things/Item/Plate/Plate_Stone")]
    [TestCase("StoneDirty", "ImmersiveChefs/Things/Item/Plate/Plate_StoneDirty")]
    public void Families_resolve_to_bounded_sibling_texture_paths(
        string family,
        string expected)
    {
        Assert.That(
            PortableTextureVariationPaths.Resolve(
                "ImmersiveChefs/Things/Item/Plate/Plate",
                (PortableTextureFamily)Enum.Parse(typeof(PortableTextureFamily), family)),
            Is.EqualTo(expected));
    }

    [Test]
    public void Runtime_selector_is_a_public_single_graphic_with_thing_aware_material_overrides()
    {
        var type = typeof(Graphic_PortableKitchenwareVariation);

        Assert.Multiple(() =>
        {
            Assert.That(type.IsPublic, Is.True);
            Assert.That(type.IsSealed, Is.True);
            Assert.That(type.BaseType, Is.EqualTo(typeof(Graphic_Single)));
            Assert.That(
                type.GetMethod(nameof(Graphic.MatAt))?.DeclaringType,
                Is.EqualTo(type));
            Assert.That(
                type.GetMethod(nameof(Graphic.MatSingleFor))?.DeclaringType,
                Is.EqualTo(type));
            Assert.That(
                type.GetMethod(nameof(Graphic.GetColoredVersion))?.DeclaringType,
                Is.EqualTo(type));
        });
    }

    [TestCase(KitchenwareProduct.Plate, KitchenMaterialKind.Wood, false, "Wood")]
    [TestCase(KitchenwareProduct.Cutlery, KitchenMaterialKind.Wood, false, "Wood")]
    [TestCase(KitchenwareProduct.Plate, KitchenMaterialKind.PrimitiveStone, false, "Stone")]
    [TestCase(KitchenwareProduct.Cookware, KitchenMaterialKind.PrimitiveStone, false, "Stone")]
    [TestCase(KitchenwareProduct.Cookware, KitchenMaterialKind.Steel, false, "Base")]
    [TestCase(KitchenwareProduct.ChefsKnife, KitchenMaterialKind.Steel, false, "Base")]
    [TestCase(KitchenwareProduct.Plate, KitchenMaterialKind.Wood, true, "WoodDirty")]
    [TestCase(KitchenwareProduct.Cookware, KitchenMaterialKind.PrimitiveStone, true, "StoneDirty")]
    [TestCase(KitchenwareProduct.Cutlery, KitchenMaterialKind.Silver, true, "BaseDirty")]
    public void Active_variation_selects_only_product_eligible_material_and_sanitation_families(
        KitchenwareProduct product,
        KitchenMaterialKind material,
        bool dirty,
        string expected)
    {
        var selected = PortableTextureVariationPolicy.Select(
            integrationEnabled: true,
            showDirtyTextures: true,
            product,
            material,
            dirty);

        Assert.That(selected.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public void Ineligible_material_shapes_fall_back_without_changing_product_identity()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                PortableTextureVariationPolicy.Select(
                    true,
                    true,
                    KitchenwareProduct.Cookware,
                    KitchenMaterialKind.Wood,
                    dirty: false),
                Is.EqualTo(PortableTextureFamily.Base));
            Assert.That(
                PortableTextureVariationPolicy.Select(
                    true,
                    true,
                    KitchenwareProduct.Cutlery,
                    KitchenMaterialKind.PrimitiveStone,
                    dirty: false),
                Is.EqualTo(PortableTextureFamily.Base));
            Assert.That(
                PortableTextureVariationPolicy.Select(
                    true,
                    true,
                    KitchenwareProduct.ChefsKnife,
                    KitchenMaterialKind.PrimitiveStone,
                    dirty: true),
                Is.EqualTo(PortableTextureFamily.Base));
        });
    }

    [Test]
    public void Dirty_sanitation_is_base_behavior_while_optional_material_variants_can_be_hidden()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                PortableTextureVariationPolicy.Select(
                    integrationEnabled: false,
                    showDirtyTextures: true,
                    KitchenwareProduct.Plate,
                    KitchenMaterialKind.Wood,
                    dirty: true),
                Is.EqualTo(PortableTextureFamily.BaseDirty));
            Assert.That(
                PortableTextureVariationPolicy.Select(
                    integrationEnabled: true,
                    showDirtyTextures: false,
                    KitchenwareProduct.Plate,
                    KitchenMaterialKind.Wood,
                    dirty: true),
                Is.EqualTo(PortableTextureFamily.Wood));
            Assert.That(
                PortableTextureVariationPolicy.Select(
                    integrationEnabled: true,
                    showDirtyTextures: false,
                    KitchenwareProduct.Cookware,
                    KitchenMaterialKind.PrimitiveStone,
                    dirty: true),
                Is.EqualTo(PortableTextureFamily.Stone));
        });
    }
}
