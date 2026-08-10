using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class PickUpAndHaulAdapterTests
{
    [Test]
    public void Installed_public_shape_is_supported()
    {
        Assert.That(
            PickUpAndHaulCompatibility.IsSupported(
                assemblyName: "PickUpAndHaul",
                assemblyVersion: new Version(1, 0, 0, 0),
                compTypeName: "PickUpAndHaul.CompHauledToInventory",
                compIsPublicThingComp: true,
                registerIsPublicInstanceThingVoid: true,
                getHashSetIsPublicInstanceThingHashSet: true,
                checkerTypeName: "PickUpAndHaul.PawnUnloadChecker",
                checkerIsPublic: true,
                checkIsPublicStaticPawnBoolVoid: true,
                settingsTypeName: "PickUpAndHaul.Settings",
                settingsIsPublicModSettings: true,
                racePolicyIsPublicStaticRacePropertiesBoolean: true,
                unloadDriverTypeName: "PickUpAndHaul.JobDriver_UnloadYourHauledInventory",
                unloadDriverIsPublicJobDriver: true),
            Is.True);
    }

    [TestCase("Lookalike", "1.0.0.0", "PickUpAndHaul.CompHauledToInventory", true, true, true, "PickUpAndHaul.PawnUnloadChecker", true, true, "PickUpAndHaul.JobDriver_UnloadYourHauledInventory", true)]
    [TestCase("PickUpAndHaul", "2.0.0.0", "PickUpAndHaul.CompHauledToInventory", true, true, true, "PickUpAndHaul.PawnUnloadChecker", true, true, "PickUpAndHaul.JobDriver_UnloadYourHauledInventory", true)]
    [TestCase("PickUpAndHaul", "1.0.0.0", "Changed.Comp", true, true, true, "PickUpAndHaul.PawnUnloadChecker", true, true, "PickUpAndHaul.JobDriver_UnloadYourHauledInventory", true)]
    [TestCase("PickUpAndHaul", "1.0.0.0", "PickUpAndHaul.CompHauledToInventory", false, true, true, "PickUpAndHaul.PawnUnloadChecker", true, true, "PickUpAndHaul.JobDriver_UnloadYourHauledInventory", true)]
    [TestCase("PickUpAndHaul", "1.0.0.0", "PickUpAndHaul.CompHauledToInventory", true, false, true, "PickUpAndHaul.PawnUnloadChecker", true, true, "PickUpAndHaul.JobDriver_UnloadYourHauledInventory", true)]
    [TestCase("PickUpAndHaul", "1.0.0.0", "PickUpAndHaul.CompHauledToInventory", true, true, false, "PickUpAndHaul.PawnUnloadChecker", true, true, "PickUpAndHaul.JobDriver_UnloadYourHauledInventory", true)]
    [TestCase("PickUpAndHaul", "1.0.0.0", "PickUpAndHaul.CompHauledToInventory", true, true, true, "Changed.Checker", true, true, "PickUpAndHaul.JobDriver_UnloadYourHauledInventory", true)]
    [TestCase("PickUpAndHaul", "1.0.0.0", "PickUpAndHaul.CompHauledToInventory", true, true, true, "PickUpAndHaul.PawnUnloadChecker", false, true, "PickUpAndHaul.JobDriver_UnloadYourHauledInventory", true)]
    [TestCase("PickUpAndHaul", "1.0.0.0", "PickUpAndHaul.CompHauledToInventory", true, true, true, "PickUpAndHaul.PawnUnloadChecker", true, false, "PickUpAndHaul.JobDriver_UnloadYourHauledInventory", true)]
    [TestCase("PickUpAndHaul", "1.0.0.0", "PickUpAndHaul.CompHauledToInventory", true, true, true, "PickUpAndHaul.PawnUnloadChecker", true, true, "Changed.Driver", true)]
    [TestCase("PickUpAndHaul", "1.0.0.0", "PickUpAndHaul.CompHauledToInventory", true, true, true, "PickUpAndHaul.PawnUnloadChecker", true, true, "PickUpAndHaul.JobDriver_UnloadYourHauledInventory", false)]
    public void Changed_or_lookalike_shape_fails_closed(
        string assemblyName,
        string version,
        string compTypeName,
        bool compIsPublicThingComp,
        bool registerShape,
        bool getHashSetShape,
        string checkerTypeName,
        bool checkerIsPublic,
        bool checkShape,
        string unloadDriverTypeName,
        bool unloadDriverShape)
    {
        Assert.That(
            PickUpAndHaulCompatibility.IsSupported(
                assemblyName,
                Version.Parse(version),
                compTypeName,
                compIsPublicThingComp,
                registerShape,
                getHashSetShape,
                checkerTypeName,
                checkerIsPublic,
                checkShape,
                "PickUpAndHaul.Settings",
                true,
                true,
                unloadDriverTypeName,
                unloadDriverShape),
            Is.False);
    }

    [TestCase("Changed.Settings", true, true)]
    [TestCase("PickUpAndHaul.Settings", false, true)]
    [TestCase("PickUpAndHaul.Settings", true, false)]
    public void Changed_race_policy_shape_fails_closed(
        string settingsTypeName,
        bool settingsIsPublicModSettings,
        bool racePolicyShape)
    {
        Assert.That(
            PickUpAndHaulCompatibility.IsSupported(
                "PickUpAndHaul",
                new Version(1, 0, 0, 0),
                "PickUpAndHaul.CompHauledToInventory",
                true,
                true,
                true,
                "PickUpAndHaul.PawnUnloadChecker",
                true,
                true,
                settingsTypeName,
                settingsIsPublicModSettings,
                racePolicyShape,
                "PickUpAndHaul.JobDriver_UnloadYourHauledInventory",
                true),
            Is.False);
    }

    [Test]
    public void Batch_policy_keeps_only_same_source_nearby_units_that_fit()
    {
        var candidates = new[]
        {
            new DishwashingBatchCandidate("primary", 0, 2, 1f, sameSource: true, eligible: true),
            new DishwashingBatchCandidate("near", 9, 3, 2f, sameSource: true, eligible: true),
            new DishwashingBatchCandidate("other-source", 16, 1, 1f, sameSource: false, eligible: true),
            new DishwashingBatchCandidate("forbidden", 25, 1, 1f, sameSource: true, eligible: false),
            new DishwashingBatchCandidate("far", 169, 1, 1f, sameSource: true, eligible: true)
        };

        var selected = DishwashingBatchPolicy.Select(candidates, availableMass: 5f);

        Assert.That(selected, Is.EqualTo(new[]
        {
            new DishwashingBatchSelection("primary", 2),
            new DishwashingBatchSelection("near", 1)
        }));
    }

    [Test]
    public void Batch_policy_returns_nothing_when_even_the_primary_unit_would_overencumber()
    {
        var selected = DishwashingBatchPolicy.Select(
            new[] { new DishwashingBatchCandidate("primary", 0, 1, 2f, true, true) },
            availableMass: 1f);

        Assert.That(selected, Is.Empty);
    }

    [TestCase(true, 0)]
    [TestCase(false, 1)]
    [TestCase(true, 3)]
    public void Persisted_batch_shape_does_not_depend_on_current_optional_adapter_state(
        bool hasQueuedTargets,
        int collectedCount)
    {
        Assert.That(
            DishwashingBatchPolicy.IsPersistedBatch(hasQueuedTargets, collectedCount),
            Is.True);
    }

    [Test]
    public void Ordinary_job_shape_is_not_mistaken_for_a_persisted_batch()
    {
        Assert.That(DishwashingBatchPolicy.IsPersistedBatch(false, 0), Is.False);
    }
}
