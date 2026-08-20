using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class MealHeatingPolicyTests
{
    [TestCase(14.9f, 30f, true)]
    [TestCase(15f, 30f, false)]
    [TestCase(20f, 30f, false)]
    [TestCase(9.9f, 10f, true)]
    [TestCase(10f, 10f, false)]
    public void Automatic_heating_stops_at_room_temperature_and_honors_the_lower_setting(
        float temperature,
        float configuredThreshold,
        bool expected)
    {
        Assert.That(
            MealHeatingPolicy.ShouldAutomaticallyHeat(temperature, configuredThreshold),
            Is.EqualTo(expected));
    }

    [TestCase((int)MealHeatingSourceKind.Microwave, 180, 60f, 0, true)]
    [TestCase((int)MealHeatingSourceKind.Stove, 450, 55f, 3, false)]
    [TestCase((int)MealHeatingSourceKind.Campfire, 750, 45f, 6, false)]
    [TestCase((int)MealHeatingSourceKind.AmbientHeater, 1800, 20f, 10, false)]
    public void Heating_profiles_make_each_fallback_slower_and_more_damaging(
        int sourceKind,
        int expectedTicks,
        float expectedTarget,
        int expectedPenalty,
        bool expectedMicrowaveCount)
    {
        var profile = MealHeatingPolicy.ProfileFor(
            (MealHeatingSourceKind)sourceKind,
            microwaveHeatingTicks: 180);

        Assert.Multiple(() =>
        {
            Assert.That(profile.HeatingTicks, Is.EqualTo(expectedTicks));
            Assert.That(profile.TargetTemperatureCelsius, Is.EqualTo(expectedTarget));
            Assert.That(profile.QualityLossPenalty, Is.EqualTo(expectedPenalty));
            Assert.That(profile.IncrementsMicrowaveCount, Is.EqualTo(expectedMicrowaveCount));
        });
    }

    [TestCase(true, true, false, false, false, (int)MealHeatingSourceKind.Microwave)]
    [TestCase(true, false, true, false, true, (int)MealHeatingSourceKind.Stove)]
    [TestCase(true, false, true, true, true, (int)MealHeatingSourceKind.Campfire)]
    [TestCase(true, false, false, false, true, (int)MealHeatingSourceKind.AmbientHeater)]
    [TestCase(true, false, false, true, true, (int)MealHeatingSourceKind.AmbientHeater)]
    [TestCase(true, false, true, false, false, null)]
    [TestCase(false, false, false, false, true, null)]
    public void Source_classification_uses_heating_capabilities_instead_of_labels(
        bool building,
        bool microwave,
        bool mealSource,
        bool openFlame,
        bool positiveHeatEmitter,
        int? expectedKind)
    {
        var kind = MealHeatingPolicy.Classify(new MealHeatingSourceFacts(
            building,
            microwave,
            mealSource,
            openFlame,
            positiveHeatEmitter));

        Assert.That(
            kind.HasValue ? (int?)kind.Value : null,
            Is.EqualTo(expectedKind));
    }

    [Test]
    public void Source_tier_outranks_distance_then_nearest_wins_within_the_tier()
    {
        var chosen = MealHeatingPolicy.Choose(new[]
        {
            new MealHeatingCandidate<string>("near campfire", MealHeatingSourceKind.Campfire, 1),
            new MealHeatingCandidate<string>("far stove", MealHeatingSourceKind.Stove, 100),
            new MealHeatingCandidate<string>("near stove", MealHeatingSourceKind.Stove, 25),
            new MealHeatingCandidate<string>("heater", MealHeatingSourceKind.AmbientHeater, 0)
        });

        Assert.That(chosen?.Value, Is.EqualTo("near stove"));
    }

    [Test]
    public void Non_microwave_heating_applies_depth_and_source_loss_without_microwave_risk_count()
    {
        var serving = new CulinaryServingRecord(
            qualityScore: 80,
            temperatureCelsius: -10f,
            contamination: ContaminationSources.None,
            microwaveReheatCount: 0,
            lastThermalTick: 100);

        serving.Heat(
            targetTemperature: 55f,
            baseQualityLoss: 5,
            sourceQualityLossPenalty: 3,
            currentTick: 200,
            incrementMicrowaveCount: false);

        Assert.Multiple(() =>
        {
            Assert.That(serving.TemperatureCelsius, Is.EqualTo(55f));
            Assert.That(serving.QualityScore, Is.EqualTo(67));
            Assert.That(serving.MicrowaveReheatCount, Is.Zero);
            Assert.That(serving.LastThermalTick, Is.EqualTo(200));
        });
    }

    [TestCase(true, true, true, true, true, true)]
    [TestCase(true, true, true, true, false, false)]
    [TestCase(true, false, true, true, true, false)]
    [TestCase(true, true, false, true, true, false)]
    [TestCase(false, true, true, true, true, false)]
    public void Operational_sources_must_actually_emit_heat_and_honor_common_state(
        bool spawned,
        bool powered,
        bool fueled,
        bool flickedOn,
        bool emittingHeat,
        bool expected)
    {
        var allowed = MealHeatingPolicy.IsOperational(new MealHeatingOperationalFacts(
            spawned,
            powered,
            fueled,
            flickedOn,
            brokenDown: false,
            emittingHeat));

        Assert.That(allowed, Is.EqualTo(expected));
    }

    [TestCase((int)MealHeatingSourceKind.Microwave, false, true)]
    [TestCase((int)MealHeatingSourceKind.Microwave, true, false)]
    [TestCase((int)MealHeatingSourceKind.Stove, false, false)]
    [TestCase((int)MealHeatingSourceKind.Campfire, false, false)]
    [TestCase((int)MealHeatingSourceKind.AmbientHeater, false, false)]
    public void Only_native_ingestion_at_a_lost_microwave_cancels_transactionally(
        int sourceKind,
        bool normalDeliveryFallback,
        bool expectedCancellation)
    {
        Assert.That(
            MealHeatingPolicy.ShouldCancelWhenUnavailable(
                (MealHeatingSourceKind)sourceKind,
                normalDeliveryFallback),
            Is.EqualTo(expectedCancellation));
    }

    [TestCase(false, true, true)]
    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, false)]
    public void An_interrupted_heating_cycle_never_applies_even_if_the_source_recovers(
        bool interrupted,
        bool sourceOperational,
        bool expected)
    {
        Assert.That(
            MealHeatingPolicy.CanApplyCompletedCycle(interrupted, sourceOperational),
            Is.EqualTo(expected));
    }
}
