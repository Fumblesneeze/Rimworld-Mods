using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class KitchenMaterialClassifierTests
{
    [TestCase("Silver", KitchenMaterialKind.Silver, FabricationTier.Intermediate, true)]
    [TestCase("EM_Bronze", KitchenMaterialKind.Bronze, FabricationTier.Intermediate, true)]
    [TestCase("Steel", KitchenMaterialKind.Steel, FabricationTier.Modern, true)]
    [TestCase("ABSPolymer", KitchenMaterialKind.Plastic, FabricationTier.Modern, true)]
    [TestCase("WoodLog", KitchenMaterialKind.Wood, FabricationTier.Soft, false)]
    public void Machining_table_tableware_accepts_all_supported_metals_and_plastics(
        string defName,
        KitchenMaterialKind kind,
        FabricationTier tier,
        bool expected)
    {
        var classification = new KitchenMaterialClassification(kind, tier);

        Assert.That(
            KitchenMaterialFabricationPolicy.Allows(
                KitchenwareProduct.Plate,
                FabricationTier.Modern,
                classification),
            Is.EqualTo(expected),
            defName);
    }

    [TestCase("BlocksGranite", KitchenwareProduct.Cookware, FabricationTier.PrimitiveStone)]
    [TestCase("BlocksGranite", KitchenwareProduct.Plate, FabricationTier.PrimitiveStone)]
    [TestCase("WoodLog", KitchenwareProduct.Plate, FabricationTier.Soft)]
    [TestCase("EM_Lead", KitchenwareProduct.Cutlery, FabricationTier.Soft)]
    [TestCase("EM_Bronze", KitchenwareProduct.Cookware, FabricationTier.Intermediate)]
    [TestCase("Silver", KitchenwareProduct.Plate, FabricationTier.Intermediate)]
    [TestCase("Steel", KitchenwareProduct.Cookware, FabricationTier.Modern)]
    [TestCase("EM_StainlessSteel", KitchenwareProduct.Plate, FabricationTier.Modern)]
    [TestCase("ABSPolymer", KitchenwareProduct.Cutlery, FabricationTier.Modern)]
    public void Known_materials_receive_product_aware_fabrication_tiers(
        string defName,
        KitchenwareProduct product,
        FabricationTier expectedTier)
    {
        var classifier = KitchenMaterialClassifier.CreateDefault();

        var classification = classifier.Classify(
            new KitchenMaterialDescriptor(
                defName,
                isMetallic: defName != "WoodLog" && !defName.StartsWith("Blocks"),
                isStony: defName.StartsWith("Blocks")),
            product);

        Assert.That(classification?.FabricationTier, Is.EqualTo(expectedTier));
    }

    [Test]
    public void Broad_stony_is_primitive_but_explicit_registration_still_wins()
    {
        var classifier = KitchenMaterialClassifier.CreateDefault();

        var moddedStone = classifier.Classify(
            new KitchenMaterialDescriptor("EM_ModdedStoneBlocks", isStony: true),
            KitchenwareProduct.Cookware);
        var abs = classifier.Classify(
            new KitchenMaterialDescriptor("ABSPolymer", isMetallic: true, isWoody: true, isStony: true),
            KitchenwareProduct.Plate);

        Assert.Multiple(() =>
        {
            Assert.That(moddedStone?.Kind, Is.EqualTo(KitchenMaterialKind.PrimitiveStone));
            Assert.That(moddedStone?.FabricationTier, Is.EqualTo(FabricationTier.PrimitiveStone));
            Assert.That(abs?.Kind, Is.EqualTo(KitchenMaterialKind.Plastic));
            Assert.That(abs?.FabricationTier, Is.EqualTo(FabricationTier.Modern));
        });
    }

    [Test]
    public void Explicit_exclusion_wins_over_stony_category()
    {
        var classifier = KitchenMaterialClassifier.CreateDefault();
        classifier.Exclude("UnsafeModStone");

        var classification = classifier.Classify(
            new KitchenMaterialDescriptor("UnsafeModStone", isStony: true),
            KitchenwareProduct.Plate);

        Assert.That(classification, Is.Null);
    }
}
