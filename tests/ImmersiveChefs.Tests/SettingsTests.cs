using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class SettingsTests
{
    [Test]
    public void Defaults_match_the_contract_and_out_of_range_values_are_bounded()
    {
        var settings = new ImmersiveChefsSettings();

        Assert.Multiple(() =>
        {
            Assert.That(settings.WareRequirementMode, Is.EqualTo(WareRequirementMode.Strict));
            Assert.That(settings.DirtyWareFallback, Is.EqualTo(DirtyWareFallback.UrgentOnly));
            Assert.That(settings.EmergencyHungerThreshold, Is.EqualTo(0.15f));
            Assert.That(settings.SimpleRecipeTimeMultiplier, Is.EqualTo(0.75f));
            Assert.That(settings.AdvancedRecipeTimeMultiplier, Is.EqualTo(2f));
            Assert.That(settings.ElaborateRecipeTimeMultiplier, Is.EqualTo(3f));
            Assert.That(settings.MaximumAssistants, Is.EqualTo(4));
            Assert.That(settings.CulinaryQualityEnabled, Is.True);
            Assert.That(settings.MealTemperatureEnabled, Is.True);
            Assert.That(settings.ToxicKitchenwareExposureScale, Is.EqualTo(1f));
            Assert.That(settings.Hospitality, Is.EqualTo(OptionalIntegrationMode.Auto));
            Assert.That(settings.CommonSense, Is.EqualTo(OptionalIntegrationMode.Auto));
            Assert.That(settings.AdaptiveMealBill, Is.EqualTo(OptionalIntegrationMode.Auto));
            Assert.That(settings.OvercookedMeals, Is.EqualTo(OptionalIntegrationMode.Auto));
            Assert.That(settings.MealsOnWheels, Is.EqualTo(OptionalIntegrationMode.Auto));
            Assert.That(settings.PrioritizeMeals, Is.EqualTo(OptionalIntegrationMode.Auto));
            Assert.That(settings.FoodTextureVariety, Is.EqualTo(OptionalIntegrationMode.Auto));
            Assert.That(settings.TextureVariationIntegration, Is.EqualTo(OptionalIntegrationMode.Auto));
            Assert.That(settings.ShowDirtyWareTextures, Is.True);
        });

        settings.EmergencyHungerThreshold = 1f;
        settings.PreparedWorkReduction = -1f;
        settings.MaximumAssistants = 99;
        settings.ThermalHalfLifeHours = 0f;
        settings.MicrowaveExtraPoisonChance = 99f;
        settings.ToxicKitchenwareExposureScale = 99f;
        settings.ClampToAllowedRanges();

        Assert.Multiple(() =>
        {
            Assert.That(settings.EmergencyHungerThreshold, Is.EqualTo(0.35f));
            Assert.That(settings.PreparedWorkReduction, Is.Zero);
            Assert.That(settings.MaximumAssistants, Is.EqualTo(4));
            Assert.That(settings.ThermalHalfLifeHours, Is.EqualTo(0.25f));
            Assert.That(settings.MicrowaveExtraPoisonChance, Is.EqualTo(5f));
            Assert.That(settings.ToxicKitchenwareExposureScale, Is.EqualTo(3f));
        });

        settings.ToxicKitchenwareExposureScale = float.NaN;
        settings.ClampToAllowedRanges();
        Assert.That(settings.ToxicKitchenwareExposureScale, Is.EqualTo(1f));
    }
}
