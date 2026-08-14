using ImmersiveChefs;
using NUnit.Framework;
using Verse;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class DishwasherUtilityAndInspectPolicyTests
{
    [Test]
    public void Connected_but_empty_water_supply_blocks_the_cycle()
    {
        Assert.That(
            DishwasherUtilityPolicy.Resolve(
                powerOn: true,
                activePowerAvailable: true,
                requiresDubsWater: true,
                waterDebited: false,
                hasSuppliedConnection: true,
                hasCycleWater: false),
            Is.EqualTo(DishwasherUtilityBlocker.NoWater));
    }

    [Test]
    public void Negative_power_balance_without_stored_energy_is_not_active_power()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DishwasherUtilityPolicy.HasActivePower(true, -0.01f, 0f), Is.False);
            Assert.That(DishwasherUtilityPolicy.HasActivePower(true, -0.01f, 100f), Is.True,
                "A charged battery may sustain the active load.");
            Assert.That(DishwasherUtilityPolicy.HasActivePower(true, 0f, 0f), Is.True);
            Assert.That(DishwasherUtilityPolicy.HasActivePower(false, 100f, 100f), Is.False);
        });
    }

    [Test]
    public void Vanilla_load_shedding_does_not_make_an_under_capacity_net_look_sufficient()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                DishwasherUtilityPolicy.HasActivePower(
                    powerOn: true,
                    currentEnergyGainRate: 0f,
                    currentStoredEnergy: 0f,
                    hasUnpoweredDesiredLoad: true),
                Is.False,
                "Vanilla balances an overloaded empty net by shedding another desired consumer.");
            Assert.That(
                DishwasherUtilityPolicy.HasActivePower(
                    powerOn: true,
                    currentEnergyGainRate: 0f,
                    currentStoredEnergy: 100f,
                    hasUnpoweredDesiredLoad: true),
                Is.True,
                "Stored battery energy may restore the intentionally powered network.");
            Assert.That(
                DishwasherUtilityPolicy.HasActivePower(
                    powerOn: true,
                    currentEnergyGainRate: 0f,
                    currentStoredEnergy: 0f,
                    hasUnpoweredDesiredLoad: false),
                Is.True,
                "An exactly balanced network with no shed desired consumers remains valid.");
        });
    }

    [Test]
    public void Dubs_pipe_diagnostics_are_hidden_only_on_immersive_chefs_dishwashers()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DishwasherInspectPolicy.ShouldSuppressOptionalDiagnostic(
                "ImmersiveChefs_Dishwasher", "BadHygiene", "DubsBadHygiene.CompPipe"), Is.True);
            Assert.That(DishwasherInspectPolicy.ShouldSuppressOptionalDiagnostic(
                "ImmersiveChefs_IndustrialDishwasher", "BadHygiene", "DubsBadHygiene.CompSewageOutlet"), Is.True);
            Assert.That(DishwasherInspectPolicy.ShouldSuppressOptionalDiagnostic(
                "ImmersiveChefs_Dishwasher", "ImmersiveChefs", "ImmersiveChefs.CompDishwasher"), Is.False);
            Assert.That(DishwasherInspectPolicy.ShouldSuppressOptionalDiagnostic(
                "OtherMod_Dishwasher", "BadHygiene", "DubsBadHygiene.CompPipe"), Is.False);
        });
    }

    [Test]
    public void Dubs_inspect_patch_targets_only_the_exact_ThingComp_override()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DubsWaterAdapter.FindPipeInspectOverride(typeof(ExactPipeComp)), Is.Not.Null);
            Assert.That(DubsWaterAdapter.FindPipeInspectOverride(typeof(InheritedThingComp)), Is.Null);
        });
    }

    private sealed class ExactPipeComp : ThingComp
    {
        public override string CompInspectStringExtra() => "Grid ID: 42";
    }

    private sealed class InheritedThingComp : ThingComp
    {
    }
}
