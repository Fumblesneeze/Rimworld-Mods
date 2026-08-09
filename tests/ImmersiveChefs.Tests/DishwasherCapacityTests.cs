using NUnit.Framework;
using System.IO;

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

    [TestCase(0, false, true)]
    [TestCase(250, false, false)]
    [TestCase(0, true, false)]
    public void Additional_ware_can_join_only_before_cleaning_or_water_debit_starts(
        int progressTicks,
        bool waterDebited,
        bool expected)
    {
        Assert.That(
            DishwasherCyclePolicy.CanAcceptAdditionalWare(progressTicks, waterDebited),
            Is.EqualTo(expected));
    }

    [Test]
    public void Additional_ware_cannot_join_after_the_loading_batch_is_captured()
    {
        Assert.That(
            DishwasherCyclePolicy.CanAcceptAdditionalWare(
                progressTicks: 0,
                waterDebitedForCycle: false,
                batchCaptured: true),
            Is.False);
    }

    [Test]
    public void Every_admission_reopens_one_bounded_loading_window()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DishwasherCyclePolicy.ResetLoadingWindow(500), Is.EqualTo(500));
            Assert.That(DishwasherCyclePolicy.AdvanceLoadingWindow(500, 250), Is.EqualTo(250));
            Assert.That(DishwasherCyclePolicy.AdvanceLoadingWindow(250, 250), Is.Zero);
            Assert.That(DishwasherCyclePolicy.AdvanceLoadingWindow(0, 250), Is.Zero);
        });
    }

    [Test]
    public void Default_loading_window_can_collect_one_place_setting_through_serial_processor_jobs()
    {
        const int processorWorkTicksPerAdmission = 200;
        const int serializedAdmissions = 3;
        const int boundedTravelMarginTicks = 250;

        Assert.That(
            new CompProperties_Dishwasher().baseLoadingTicks,
            Is.GreaterThanOrEqualTo(
                (processorWorkTicksPerAdmission * serializedAdmissions) + boundedTravelMarginTicks),
            "The processor admits only one hauled stack at a time; cookware, a plate, and cutlery " +
            "must all have time to join the same dishwasher batch.");
    }

    [Test]
    public void Processor_batch_requests_water_once_only_after_the_final_loading_window_closes()
    {
        var loadingTicks = DishwasherCyclePolicy.ResetLoadingWindow(500);

        Assert.That(
            DishwasherCyclePolicy.ShouldRequestWater(
                hasContents: true,
                loadingTicksRemaining: loadingTicks,
                batchCaptured: false,
                waterDebited: false),
            Is.False,
            "The first admitted dish must not debit water while matching ware can still join.");

        loadingTicks = DishwasherCyclePolicy.AdvanceLoadingWindow(loadingTicks, 250);
        loadingTicks = DishwasherCyclePolicy.ResetLoadingWindow(500);
        loadingTicks = DishwasherCyclePolicy.AdvanceLoadingWindow(loadingTicks, 500);

        Assert.Multiple(() =>
        {
            Assert.That(loadingTicks, Is.Zero);
            Assert.That(
                DishwasherCyclePolicy.ShouldRequestWater(
                    hasContents: true,
                    loadingTicksRemaining: loadingTicks,
                    batchCaptured: true,
                    waterDebited: false),
                Is.True,
                "The closed batch must request its one atomic water debit.");
            Assert.That(
                DishwasherCyclePolicy.CaptureWaterCharge(
                    finalPlateEquivalentLoad: 1.25f,
                    waterPerPlateEquivalent: 0.1f),
                Is.EqualTo(0.125f).Within(0.0001f),
                "The charge must include the final plate and cutlery load.");
            Assert.That(
                DishwasherCyclePolicy.ShouldRequestWater(
                    hasContents: true,
                    loadingTicksRemaining: loadingTicks,
                    batchCaptured: true,
                    waterDebited: true),
                Is.False,
                "A resumed captured batch must never request a second debit.");
        });
    }

    [TestCase(false, false, false, false, false, true)]
    [TestCase(true, false, true, false, false, false)]
    [TestCase(true, true, false, false, false, false)]
    [TestCase(true, true, true, false, false, true)]
    [TestCase(true, true, true, true, false, false)]
    [TestCase(true, true, true, true, true, true)]
    public void Debited_processor_batch_requires_its_captured_connection_and_only_preexisting_residual_supply(
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
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "ImmersiveChefs.sln")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new DirectoryNotFoundException(
            "Could not locate repository root from the test directory.");
    }
}
