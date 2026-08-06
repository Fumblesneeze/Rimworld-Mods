using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class PortableTextureVariationPolicyTests
{
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
                Is.EqualTo(PortableTextureFamily.BaseDirty));
        });
    }

    [Test]
    public void Off_and_hidden_dirty_settings_have_exact_cosmetic_fallbacks()
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
                Is.EqualTo(PortableTextureFamily.Base));
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
