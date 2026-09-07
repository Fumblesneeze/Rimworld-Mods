using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class PickUpAndHaulAdapterTests
{
    [TestCase("1.0.0.0")]
    [TestCase("9.8.7.6")]
    public void Installed_public_shape_is_supported(string version)
    {
        Assert.That(
            PickUpAndHaulCompatibility.IsSupported(
                assemblyName: "PickUpAndHaul",
                assemblyVersion: new Version(version),
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

    [Test]
    public void Dishwasher_batch_policy_stops_before_remaining_plate_equivalent_capacity()
    {
        var selected = DishwashingBatchPolicy.Select(
            new[]
            {
                new DishwashingBatchCandidate("plate", 0, 2, 0.2f, true, true, 1f),
                new DishwashingBatchCandidate("cutlery", 1, 2, 0.1f, true, true, 0.25f),
                new DishwashingBatchCandidate("cookware", 4, 1, 0.5f, true, true, 4f)
            },
            availableMass: 10f,
            availablePlateEquivalentCapacity: 1.25f);

        Assert.That(selected, Is.EqualTo(new[]
        {
            new DishwashingBatchSelection("plate", 1),
            new DishwashingBatchSelection("cutlery", 1)
        }));
    }

    [Test]
    public void Resolved_tracking_release_is_verified_before_holder_transfer()
    {
        var tracked = new HashSet<Verse.Thing>(ReferenceComparer.Instance);
        var thing = new Verse.Thing();
        tracked.Add(thing);

        Assert.That(
            PickUpAndHaulAdapter.TryRemoveResolvedTrackedItem(tracked, thing, out var reason),
            Is.True,
            reason);
        Assert.That(tracked, Does.Not.Contain(thing));
        Assert.That(
            PickUpAndHaulAdapter.TryRemoveResolvedTrackedItem(tracked, thing, out reason),
            Is.False);
        Assert.That(reason, Does.Contain("did not release"));
    }

    private sealed class ReferenceComparer : IEqualityComparer<Verse.Thing>
    {
        internal static readonly ReferenceComparer Instance = new();

        public bool Equals(Verse.Thing? x, Verse.Thing? y) => ReferenceEquals(x, y);

        public int GetHashCode(Verse.Thing obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
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

    [Test]
    public void Dishwasher_output_batch_takes_every_completed_unit_that_fits()
    {
        var selected = DishwasherOutputBatchPolicy.Select(
            new[]
            {
                new DishwasherOutputBatchCandidate("plates", 4, 0.5f, progress: 1f, ruined: false),
                new DishwasherOutputBatchCandidate("active-cutlery", 8, 0.1f, progress: 0.99f, ruined: false),
                new DishwasherOutputBatchCandidate("ruined-cookware", 1, 2f, progress: 1f, ruined: true),
                new DishwasherOutputBatchCandidate("cutlery", 10, 0.1f, progress: 1.25f, ruined: false)
            },
            availableMass: 2.45f);

        Assert.That(selected, Is.EqualTo(new[]
        {
            new DishwasherOutputBatchSelection("cutlery", 10),
            new DishwasherOutputBatchSelection("plates", 2)
        }));
    }

    [Test]
    public void Dishwasher_output_batch_splits_only_the_completed_stack_at_capacity()
    {
        var selected = DishwasherOutputBatchPolicy.Select(
            new[]
            {
                new DishwasherOutputBatchCandidate("plates", 6, 0.5f, progress: 1f, ruined: false)
            },
            availableMass: 1.1f);

        Assert.That(selected, Is.EqualTo(new[]
        {
            new DishwasherOutputBatchSelection("plates", 2)
        }));
    }

    [TestCase(true, true)]
    [TestCase(false, false)]
    public void Processor_dishwasher_fill_and_empty_drivers_replace_only_the_appliance_delay(
        bool processorDishwasher,
        bool expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                ProcessorDishwasherTransferPolicy.ShouldReplaceFillDriver(processorDishwasher),
                Is.EqualTo(expected));
            Assert.That(
                ProcessorDishwasherTransferPolicy.ShouldReplaceEmptyDriver(processorDishwasher),
                Is.EqualTo(expected));
        });
    }

    [Test]
    public void Processor_dishwasher_arrival_latch_matches_direct_delivery()
    {
        Assert.That(
            ProcessorDishwasherTransferPolicy.ArrivalLatchTicks,
            Is.EqualTo(DishwashingBatchPolicy.DishwasherAdmissionTicksPerUnit));
        Assert.That(ProcessorDishwasherTransferPolicy.ArrivalLatchTicks, Is.EqualTo(2));
    }

    [TestCase(true, true, true, true)]
    [TestCase(true, true, false, false)]
    [TestCase(true, false, true, false)]
    [TestCase(false, true, true, false)]
    public void Tracked_output_batch_requires_both_validated_integrations(
        bool processorDishwasher,
        bool canTrack,
        bool hasFittingNaturalOutput,
        bool expected)
    {
        Assert.That(
            ProcessorDishwasherTransferPolicy.ShouldUseTrackedOutputBatch(
                processorDishwasher,
                canTrack,
                hasFittingNaturalOutput),
            Is.EqualTo(expected));
    }

    [Test]
    public void Empty_now_does_not_turn_an_active_load_into_batch_output()
    {
        var selected = DishwasherOutputBatchPolicy.Select(
            new[]
            {
                new DishwasherOutputBatchCandidate(
                    "empty-now-but-active",
                    1,
                    0.5f,
                    progress: 0.75f,
                    ruined: false)
            },
            availableMass: 10f);

        Assert.That(selected, Is.Empty);
    }

    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, true)]
    public void Appliance_arrival_revalidation_selects_immediate_one_output_when_nothing_fits(
        bool canTrack,
        bool hasFittingNaturalOutput,
        bool expectedImmediateOneOutput)
    {
        Assert.That(
            ProcessorDishwasherTransferPolicy.ShouldUseImmediateOneOutputFallback(
                canTrack,
                hasFittingNaturalOutput),
            Is.EqualTo(expectedImmediateOneOutput));
    }

    [TestCase(true, false, false, false)]
    [TestCase(false, true, false, false)]
    [TestCase(false, false, false, true)]
    [TestCase(true, false, true, true)]
    public void Stock_emptying_lifecycle_preserves_processor_fail_conditions(
        bool anyComplete,
        bool anyRuined,
        bool empty,
        bool expectedFailure)
    {
        Assert.That(
            ProcessorEmptyLifecyclePolicy.ShouldFail(anyComplete, anyRuined, empty),
            Is.EqualTo(expectedFailure));
    }

    [TestCase(false, false)]
    [TestCase(true, true)]
    public void Stock_emptying_lifecycle_succeeds_only_when_processor_is_empty(
        bool empty,
        bool expectedSuccess)
    {
        Assert.That(ProcessorEmptyLifecyclePolicy.ShouldSucceed(empty), Is.EqualTo(expectedSuccess));
    }

    [TestCase("1.0.0.0")]
    [TestCase("9.8.7.6")]
    public void Installed_processor_filling_shape_is_supported(string version)
    {
        Assert.That(
            ProcessorDishwasherInputCompatibility.IsSupported(
                assemblyName: "ProcessorFramework",
                assemblyVersion: new Version(version),
                driverTypeName: "ProcessorFramework.JobDriver_FillProcessor",
                driverIsPublicJobDriver: true,
                makeNewToilsIsProtectedInstanceEnumerable: true,
                fillJobUsesDriver: true,
                reservationsArePublicInstanceBoolean: true,
                processFilterHasAllowedIngredientList: true),
            Is.True);
    }

    [TestCase("ChangedFramework", "1.0.0.0", "ProcessorFramework.JobDriver_FillProcessor", true, true, true, true, true)]
    [TestCase("ProcessorFramework", "1.0.0.0", "Changed.FillDriver", true, true, true, true, true)]
    [TestCase("ProcessorFramework", "1.0.0.0", "ProcessorFramework.JobDriver_FillProcessor", false, true, true, true, true)]
    [TestCase("ProcessorFramework", "1.0.0.0", "ProcessorFramework.JobDriver_FillProcessor", true, false, true, true, true)]
    [TestCase("ProcessorFramework", "1.0.0.0", "ProcessorFramework.JobDriver_FillProcessor", true, true, false, true, true)]
    [TestCase("ProcessorFramework", "1.0.0.0", "ProcessorFramework.JobDriver_FillProcessor", true, true, true, false, true)]
    [TestCase("ProcessorFramework", "1.0.0.0", "ProcessorFramework.JobDriver_FillProcessor", true, true, true, true, false)]
    public void Changed_processor_filling_shape_falls_back_to_stock(
        string assemblyName,
        string version,
        string driverTypeName,
        bool driverShape,
        bool toilShape,
        bool jobShape,
        bool reservationShape,
        bool processFilterShape)
    {
        Assert.That(
            ProcessorDishwasherInputCompatibility.IsSupported(
                assemblyName,
                Version.Parse(version),
                driverTypeName,
                driverShape,
                toilShape,
                jobShape,
                reservationShape,
                processFilterShape),
            Is.False);
    }

    [TestCase("1.0.0.0")]
    [TestCase("9.8.7.6")]
    public void Installed_processor_emptying_shape_is_supported(string version)
    {
        Assert.That(
            ProcessorDishwasherOutputCompatibility.IsSupported(
                assemblyName: "ProcessorFramework",
                assemblyVersion: new Version(version),
                driverTypeName: "ProcessorFramework.JobDriver_EmptyProcessor",
                driverIsPublicJobDriver: true,
                makeNewToilsIsProtectedInstanceEnumerable: true,
                emptyJobUsesDriver: true,
                ruinedIsPublicInstanceBoolean: true,
                progressIsPublicInstanceFloat: true,
                lifecyclePropertiesArePublicInstanceBoolean: true),
            Is.True);
    }

    [TestCase("ChangedFramework", "1.0.0.0", "ProcessorFramework.JobDriver_EmptyProcessor", true, true, true, true, true, true)]
    [TestCase("ProcessorFramework", "1.0.0.0", "Changed.EmptyDriver", true, true, true, true, true, true)]
    [TestCase("ProcessorFramework", "1.0.0.0", "ProcessorFramework.JobDriver_EmptyProcessor", false, true, true, true, true, true)]
    [TestCase("ProcessorFramework", "1.0.0.0", "ProcessorFramework.JobDriver_EmptyProcessor", true, false, true, true, true, true)]
    [TestCase("ProcessorFramework", "1.0.0.0", "ProcessorFramework.JobDriver_EmptyProcessor", true, true, false, true, true, true)]
    [TestCase("ProcessorFramework", "1.0.0.0", "ProcessorFramework.JobDriver_EmptyProcessor", true, true, true, false, true, true)]
    [TestCase("ProcessorFramework", "1.0.0.0", "ProcessorFramework.JobDriver_EmptyProcessor", true, true, true, true, false, true)]
    [TestCase("ProcessorFramework", "1.0.0.0", "ProcessorFramework.JobDriver_EmptyProcessor", true, true, true, true, true, false)]
    public void Changed_processor_emptying_shape_falls_back_to_stock(
        string assemblyName,
        string version,
        string driverTypeName,
        bool driverShape,
        bool toilShape,
        bool jobShape,
        bool ruinedShape,
        bool progressShape,
        bool lifecycleShape)
    {
        Assert.That(
            ProcessorDishwasherOutputCompatibility.IsSupported(
                assemblyName,
                Version.Parse(version),
                driverTypeName,
                driverShape,
                toilShape,
                jobShape,
                ruinedShape,
                progressShape,
                lifecycleShape),
            Is.False);
    }
}
