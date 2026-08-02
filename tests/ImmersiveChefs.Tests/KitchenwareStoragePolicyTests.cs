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
}
