using NUnit.Framework;
using System;
using System.IO;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class KitchenwareStoragePolicyTests
{
    [Test]
    public void Integrated_sink_water_gate_requires_a_validated_bridge_and_operation_amount()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                IntegratedSinkWaterPolicy.RequiresSuppliedWater(
                    hasIntegratedSink: true,
                    dubsIntegrationEnabled: true,
                    validatedBridgeAvailable: true),
                Is.True);
            Assert.That(
                IntegratedSinkWaterPolicy.RequiresSuppliedWater(
                    hasIntegratedSink: true,
                    dubsIntegrationEnabled: true,
                    validatedBridgeAvailable: false),
                Is.False,
                "A changed DBH API must disable only the bridge, not water-lock the base prep worktable.");
            Assert.That(IntegratedSinkWaterPolicy.RequiredAmount(1f), Is.EqualTo(1f));
            Assert.That(IntegratedSinkWaterPolicy.RequiredAmount(0f), Is.EqualTo(0.001f));
        });
    }

    [Test]
    public void Clean_and_dirty_filters_are_mutually_exclusive_regardless_of_wash_source()
    {
        Assert.Multiple(() =>
        {
            Assert.That(KitchenwareStoragePolicy.Allows(KitchenwareStorageFilter.Clean, isDirty: false), Is.True);
            Assert.That(KitchenwareStoragePolicy.Allows(KitchenwareStorageFilter.Dirty, isDirty: false), Is.False);
            Assert.That(KitchenwareStoragePolicy.Allows(KitchenwareStorageFilter.Clean, isDirty: true), Is.False);
            Assert.That(KitchenwareStoragePolicy.Allows(KitchenwareStorageFilter.Dirty, isDirty: true), Is.True);
        });
    }

    [Test]
    public void Only_a_validated_operational_supplied_dubs_fixture_is_safe()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                WashSourcePolicy.ClassifyObjectSource(
                    capabilityAvailable: true,
                    capabilityIsSafe: false,
                    operational: true,
                    hasCycleWater: true),
                Is.EqualTo(WashProvenance.WildWater));
            Assert.That(
                WashSourcePolicy.ClassifyObjectSource(
                    capabilityAvailable: true,
                    capabilityIsSafe: true,
                    operational: true,
                    hasCycleWater: true),
                Is.EqualTo(WashProvenance.Safe));
            Assert.That(
                WashSourcePolicy.ClassifyObjectSource(
                    capabilityAvailable: false,
                    capabilityIsSafe: true,
                    operational: true,
                    hasCycleWater: true),
                Is.Null,
                "A shape-incompatible Dubs fixture must fail closed instead of becoming generic safe water.");
            Assert.That(
                WashSourcePolicy.ClassifyObjectSource(
                    capabilityAvailable: true,
                    capabilityIsSafe: true,
                    operational: true,
                    hasCycleWater: false),
                Is.Null);
        });
    }

    [TestCase(0, 0, WashProvenance.Safe)]
    [TestCase(1, 1, WashProvenance.WildWater)]
    [TestCase(2, 2, WashProvenance.WildWater)]
    public void Recognized_handwashing_sources_have_a_stable_fallback_order_and_provenance(
        int kindValue,
        int expectedPriority,
        WashProvenance expectedProvenance)
    {
        var kind = (HandwashingSourceKind)kindValue;
        Assert.Multiple(() =>
        {
            Assert.That(WashSourcePolicy.Priority(kind), Is.EqualTo(expectedPriority));
            Assert.That(
                WashSourcePolicy.ClassifyDubsSource(
                    kind,
                    dubsIntegrationEnabled: true,
                    pawnAllowed: true,
                    operational: true,
                    hasAvailableWater: true),
                Is.EqualTo(expectedProvenance));
        });
    }

    [TestCase(false, true, true, true)]
    [TestCase(true, false, true, true)]
    [TestCase(true, true, false, true)]
    [TestCase(true, true, true, false)]
    public void Recognized_dubs_source_fails_closed_without_integration_pawn_permission_operation_or_water(
        bool integrationEnabled,
        bool pawnAllowed,
        bool operational,
        bool hasAvailableWater)
    {
        Assert.That(
            WashSourcePolicy.ClassifyDubsSource(
                HandwashingSourceKind.HauledWater,
                integrationEnabled,
                pawnAllowed,
                operational,
                hasAvailableWater),
            Is.Null);
    }

    [Test]
    public void Water_fixture_runtime_contains_no_Def_name_or_package_owner_whitelist()
    {
        var root = FindRepositoryRoot();
        var sources = new[]
        {
            Path.Combine(root, "mods", "ImmersiveChefs", "Source", "Sanitation", "DubsWaterAdapter.cs"),
            Path.Combine(root, "mods", "ImmersiveChefs", "Source", "Sanitation", "WorkGiver_DoDishes.cs"),
            Path.Combine(root, "mods", "ImmersiveChefs", "Source", "Sanitation", "JobDriver_DoDishes.cs")
        }.Select(File.ReadAllText).ToArray();
        var forbiddenSemanticTokens = new[]
        {
            "case \"KitchenSink\"",
            "case \"BasinStuff\"",
            "case \"Fountain\"",
            "case \"WashBucket\"",
            "case \"WaterTrough\"",
            "case \"PetWaterBowl\"",
            "case \"PrimitiveWell\"",
            "IsNamedWaterSource",
            "IsFromDubsBadHygiene",
            "source.def.modContentPack?.PackageId"
        };

        Assert.That(
            forbiddenSemanticTokens.Where(token => sources.Any(source =>
                source.IndexOf(token, StringComparison.Ordinal) >= 0)),
            Is.Empty,
            "Water-source eligibility must come from validated behavior, never a Def-name or owner whitelist.");
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ImmersiveChefs.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Immersive Chefs repository root.");
    }
}
