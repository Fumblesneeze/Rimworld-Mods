using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class KitchenwareStoragePolicyTests
{
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
                    fromDubs: false,
                    dubsIntegrationEnabled: false,
                    validatedDubsFixture: false,
                    operational: true,
                    hasCycleWater: true),
                Is.EqualTo(WashProvenance.WildWater));
            Assert.That(
                WashSourcePolicy.ClassifyObjectSource(
                    fromDubs: true,
                    dubsIntegrationEnabled: true,
                    validatedDubsFixture: true,
                    operational: true,
                    hasCycleWater: true),
                Is.EqualTo(WashProvenance.Safe));
            Assert.That(
                WashSourcePolicy.ClassifyObjectSource(
                    fromDubs: true,
                    dubsIntegrationEnabled: true,
                    validatedDubsFixture: false,
                    operational: true,
                    hasCycleWater: true),
                Is.Null,
                "A shape-incompatible Dubs fixture must fail closed instead of becoming generic safe water.");
            Assert.That(
                WashSourcePolicy.ClassifyObjectSource(
                    fromDubs: true,
                    dubsIntegrationEnabled: true,
                    validatedDubsFixture: true,
                    operational: true,
                    hasCycleWater: false),
                Is.Null);
        });
    }

    [TestCase(0, 0, WashProvenance.Safe)]
    [TestCase(1, 1, WashProvenance.Safe)]
    [TestCase(2, 2, WashProvenance.WildWater)]
    [TestCase(3, 3, WashProvenance.WildWater)]
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
}
