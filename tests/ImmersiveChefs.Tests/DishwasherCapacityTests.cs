using NUnit.Framework;
using System.IO;
using Verse;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class DishwasherCapacityTests
{
    [TestCase(16f, 0f, 1f, 50, 16)]
    [TestCase(16f, 15.5f, 0.5f, 10, 1)]
    [TestCase(16f, 16f, 1f, 1, 0)]
    public void Oversized_stack_can_be_partially_loaded(
        float capacity,
        float used,
        float perItem,
        int stackCount,
        int expected)
    {
        Assert.That(
            DishwasherCapacityPolicy.CountAccepted(capacity, used, perItem, stackCount),
            Is.EqualTo(expected));
    }

    [Test]
    public void Active_load_leaves_all_remaining_capacity_available_to_a_later_stack()
    {
        Assert.That(
            DishwasherCapacityPolicy.CountAccepted(
                capacity: 16f,
                used: 1f,
                perItem: 0.25f,
                stackCount: 100),
            Is.EqualTo(60),
            "An earlier one-plate load must leave fifteen plate-equivalents open for later cutlery.");
    }

    [Test]
    public void Every_admission_captures_its_own_duration()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DishwasherCyclePolicy.CaptureLoadDuration(2500, 1f), Is.EqualTo(2500));
            Assert.That(DishwasherCyclePolicy.CaptureLoadDuration(2500, 2f), Is.EqualTo(5000));
            Assert.That(DishwasherCyclePolicy.CaptureLoadDuration(1800, 0.5f), Is.EqualTo(900));
        });
    }

    [Test]
    public void Later_load_advances_independently_without_resetting_the_earlier_load()
    {
        var earlier = DishwasherCyclePolicy.AdvanceLoad(
            progressTicks: 1000,
            elapsedTicks: 250,
            capturedDurationTicks: 2500);
        var later = DishwasherCyclePolicy.AdvanceLoad(
            progressTicks: 0,
            elapsedTicks: 250,
            capturedDurationTicks: 2500);

        Assert.Multiple(() =>
        {
            Assert.That(earlier, Is.EqualTo(1250));
            Assert.That(later, Is.EqualTo(250));
            Assert.That(earlier, Is.GreaterThan(later),
                "A late load must start at zero without inheriting or resetting the earlier load.");
        });
    }

    [Test]
    public void Processor_charges_each_admission_for_only_its_own_load()
    {
        var plateCharge = DishwasherCyclePolicy.AdmissionWaterCharge(
            admittedPlateEquivalentLoad: 1f,
            waterPerPlateEquivalent: 0.1f);
        var cutleryCharge = DishwasherCyclePolicy.AdmissionWaterCharge(
            admittedPlateEquivalentLoad: 0.25f,
            waterPerPlateEquivalent: 0.1f);

        Assert.Multiple(() =>
        {
            Assert.That(plateCharge, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(cutleryCharge, Is.EqualTo(0.025f).Within(0.0001f));
            Assert.That(plateCharge + cutleryCharge, Is.EqualTo(0.125f).Within(0.0001f));
        });
    }

    [TestCase(false, false, false, false, false, true)]
    [TestCase(true, false, true, false, false, false)]
    [TestCase(true, true, false, false, false, false)]
    [TestCase(true, true, true, false, false, true)]
    [TestCase(true, true, true, true, false, false)]
    [TestCase(true, true, true, true, true, true)]
    public void Debited_processor_loads_require_their_connection_and_only_preexisting_residual_supply(
        bool requiresDubsWater,
        bool waterDebited,
        bool hasSuppliedConnection,
        bool residualSupplyRequired,
        bool hasResidualSupply,
        bool expected)
    {
        Assert.That(
            DishwasherCyclePolicy.CanContinueWithWater(
                requiresDubsWater,
                waterDebited,
                hasSuppliedConnection,
                residualSupplyRequired,
                hasResidualSupply),
            Is.EqualTo(expected));
    }

    [TestCase(0.25f, 0.25f)]
    [TestCase(1f, 1f)]
    [TestCase(4f, 4f)]
    [TestCase(0f, 0.01f)]
    public void Processor_capacity_factor_uses_each_ware_defs_plate_equivalent(
        float configuredPlateEquivalent,
        float expectedCapacityFactor)
    {
        Assert.That(
            DishwasherCapacityPolicy.ProcessorCapacityFactor(configuredPlateEquivalent),
            Is.EqualTo(expectedCapacityFactor).Within(0.0001f));
    }

    [TestCase(false, false, true)]
    [TestCase(false, true, true)]
    [TestCase(true, true, true)]
    [TestCase(true, false, false)]
    public void Processor_fill_reservation_rejects_only_clean_dishwasher_ware(
        bool isDishwasher,
        bool isDirtyWare,
        bool expected)
    {
        Assert.That(
            ProcessorFrameworkAdapter.AllowsFillReservation(isDishwasher, isDirtyWare),
            Is.EqualTo(expected));
    }

    [Test]
    public void Processor_string_field_materializes_translated_tagged_text()
    {
        TaggedString translated = "Wash steel plate";

        Assert.That(
            ProcessorFrameworkAdapter.CoerceFieldValue(typeof(string), translated),
            Is.EqualTo("Wash steel plate"));
    }

    [Test]
    public void Processor_field_coercion_rejects_unrelated_shape_drift()
    {
        Assert.That(
            () => ProcessorFrameworkAdapter.CoerceFieldValue(typeof(int), "not an integer"),
            Throws.ArgumentException);
    }

    [Test]
    public void Processor_shape_guard_rejects_assignable_but_widened_fields()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                ProcessorFrameworkAdapter.HasExactFieldShape(
                    typeof(ExactProcessorShape), nameof(ExactProcessorShape.label), typeof(string)),
                Is.True);
            Assert.That(
                ProcessorFrameworkAdapter.HasExactFieldShape(
                    typeof(WidenedProcessorShape), nameof(WidenedProcessorShape.label), typeof(string)),
                Is.False,
                "A widened object field must fail before the adapter mutates any Processor Def.");
        });
    }

    [Test]
    public void Restart_capacity_scale_is_materialized_once_for_local_and_processor_paths()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DishwasherCapacityPolicy.ScaleForRestart(16f, 0.5f), Is.EqualTo(8f));
            Assert.That(DishwasherCapacityPolicy.ScaleForRestart(16f, 2.5f), Is.EqualTo(40f));
            Assert.That(DishwasherCapacityPolicy.ScaleForRestart(64f, 4f), Is.EqualTo(256f));
        });

        var root = FindRepositoryRoot();
        var dishwasher = File.ReadAllText(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Source",
            "Sanitation",
            "CompDishwasher.cs"));
        var bootstrap = File.ReadAllText(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Source",
            "Defs",
            "ImmersiveChefsDefBootstrap.cs"));
        var processor = File.ReadAllText(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Source",
            "Integrations",
            "ProcessorFrameworkAdapter.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(dishwasher,
                Does.Contain("public float Capacity => Props.basePlateCapacity;"));
            Assert.That(dishwasher,
                Does.Not.Contain("Settings.DishwasherCapacityScale"));
            Assert.That(bootstrap,
                Does.Contain("ApplyDishwasherCapacityScale"));
            Assert.That(processor,
                Does.Contain("CompProperties_Dishwasher"));
            Assert.That(processor,
                Does.Not.Contain("capacity * ImmersiveChefsMod.Settings.DishwasherCapacityScale"));
        });
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "RimWorldMods.sln")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new DirectoryNotFoundException(
            "Could not locate repository root from the test directory.");
    }

    private sealed class ExactProcessorShape
    {
#pragma warning disable CS0649
        public string? label;
#pragma warning restore CS0649
    }

    private sealed class WidenedProcessorShape
    {
#pragma warning disable CS0649
        public object? label;
#pragma warning restore CS0649
    }
}
