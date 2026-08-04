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
