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
        });

        settings.EmergencyHungerThreshold = 1f;
        settings.PreparedWorkReduction = -1f;
        settings.MaximumAssistants = 99;
        settings.ThermalHalfLifeHours = 0f;
        settings.MicrowaveExtraPoisonChance = 99f;
        settings.ClampToAllowedRanges();

        Assert.Multiple(() =>
        {
            Assert.That(settings.EmergencyHungerThreshold, Is.EqualTo(0.35f));
            Assert.That(settings.PreparedWorkReduction, Is.Zero);
            Assert.That(settings.MaximumAssistants, Is.EqualTo(4));
            Assert.That(settings.ThermalHalfLifeHours, Is.EqualTo(0.25f));
            Assert.That(settings.MicrowaveExtraPoisonChance, Is.EqualTo(5f));
        });
    }
}
