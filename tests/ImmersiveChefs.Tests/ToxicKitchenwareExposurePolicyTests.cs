using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class ToxicKitchenwareExposurePolicyTests
{
    [Test]
    public void Only_lead_and_uranium_are_toxic_default_materials()
    {
        var toxic = Enum.GetValues(typeof(KitchenMaterialKind))
            .Cast<KitchenMaterialKind>()
            .Where(ToxicKitchenwareExposurePolicy.IsToxic)
            .ToArray();

        Assert.That(
            toxic,
            Is.EqualTo(new[] { KitchenMaterialKind.Lead, KitchenMaterialKind.Uranium }));
    }

    [TestCase(KitchenMaterialKind.Lead, 0.020f)]
    [TestCase(KitchenMaterialKind.Uranium, 0.020f)]
    [TestCase(KitchenMaterialKind.Steel, 0f)]
    [TestCase(KitchenMaterialKind.Wood, 0f)]
    public void Cookware_dose_is_material_specific(KitchenMaterialKind material, float expected)
    {
        Assert.That(
            ToxicKitchenwareExposurePolicy.Calculate(
                material,
                KitchenMaterialKind.Steel,
                KitchenMaterialKind.Steel,
                exposureScale: 1f,
                humanlike: true,
                nutritionIngested: 0.9f),
            Is.EqualTo(expected).Within(0.0001f));
    }

    [Test]
    public void Full_toxic_setting_uses_bounded_live_scale()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                ToxicKitchenwareExposurePolicy.Calculate(
                    KitchenMaterialKind.Lead,
                    KitchenMaterialKind.Uranium,
                    KitchenMaterialKind.Lead,
                    exposureScale: 1f,
                    humanlike: true,
                    nutritionIngested: 0.9f),
                Is.EqualTo(0.045f).Within(0.0001f));
            Assert.That(
                ToxicKitchenwareExposurePolicy.Calculate(
                    KitchenMaterialKind.Lead,
                    KitchenMaterialKind.Uranium,
                    KitchenMaterialKind.Lead,
                    exposureScale: 3f,
                    humanlike: true,
                    nutritionIngested: 0.9f),
                Is.EqualTo(0.135f).Within(0.0001f));
            Assert.That(
                ToxicKitchenwareExposurePolicy.Calculate(
                    KitchenMaterialKind.Lead,
                    KitchenMaterialKind.Uranium,
                    KitchenMaterialKind.Lead,
                    exposureScale: -1f,
                    humanlike: true,
                    nutritionIngested: 0.9f),
                Is.Zero);
            Assert.That(
                ToxicKitchenwareExposurePolicy.Calculate(
                    KitchenMaterialKind.Lead,
                    KitchenMaterialKind.Uranium,
                    KitchenMaterialKind.Lead,
                    exposureScale: float.NaN,
                    humanlike: true,
                    nutritionIngested: 0.9f),
                Is.Zero);
        });
    }

    [Test]
    public void Each_actual_dish_component_contributes_its_own_dose()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                ToxicKitchenwareExposurePolicy.Calculate(
                    KitchenMaterialKind.Steel,
                    KitchenMaterialKind.Uranium,
                    KitchenMaterialKind.Steel,
                    exposureScale: 1f,
                    humanlike: true,
                    nutritionIngested: 0.9f),
                Is.EqualTo(ToxicKitchenwareExposurePolicy.PlateDose).Within(0.0001f));
            Assert.That(
                ToxicKitchenwareExposurePolicy.Calculate(
                    KitchenMaterialKind.Steel,
                    KitchenMaterialKind.Steel,
                    KitchenMaterialKind.Lead,
                    exposureScale: 1f,
                    humanlike: true,
                    nutritionIngested: 0.9f),
                Is.EqualTo(ToxicKitchenwareExposurePolicy.CutleryDose).Within(0.0001f));
        });
    }

    [TestCase(false, 0.9f)]
    [TestCase(true, 0f)]
    [TestCase(true, float.NaN)]
    public void Animals_and_aborted_ingestion_receive_no_dose(bool humanlike, float nutrition)
    {
        Assert.That(
            ToxicKitchenwareExposurePolicy.Calculate(
                KitchenMaterialKind.Lead,
                KitchenMaterialKind.Lead,
                KitchenMaterialKind.Lead,
                exposureScale: 1f,
                humanlike,
                nutrition),
            Is.Zero);
    }

    [Test]
    public void Uranium_is_a_distinct_modern_metal()
    {
        var classification = KitchenMaterialClassifier.CreateDefault().Classify(
            new KitchenMaterialDescriptor("Uranium", isMetallic: true),
            KitchenwareProduct.Plate);

        Assert.Multiple(() =>
        {
            Assert.That(classification?.Kind, Is.EqualTo(KitchenMaterialKind.Uranium));
            Assert.That(classification?.FabricationTier, Is.EqualTo(FabricationTier.Modern));
        });
    }

    [Test]
    public void Serving_snapshot_preserves_hidden_cookware_material()
    {
        var record = new CulinaryServingRecord(
            72,
            65f,
            ContaminationSources.None,
            0,
            123,
            cookwareMaterial: KitchenMaterialKind.Lead);

        var restored = CulinaryServingRecord.Restore(record.Capture());

        Assert.Multiple(() =>
        {
            Assert.That(record.Capture().SchemaVersion, Is.EqualTo(3));
            Assert.That(restored.CookwareMaterial, Is.EqualTo(KitchenMaterialKind.Lead));
        });
    }
}
