using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class DiningCutleryAcquisitionPolicyTests
{
    [Test]
    public void One_diner_reserves_one_unit_without_excluding_stack_peers()
    {
        var reservation = DiningCutleryReservationPolicy.ForStackLimit(25);

        Assert.Multiple(() =>
        {
            Assert.That(reservation.MaxPawns, Is.EqualTo(25));
            Assert.That(reservation.StackCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Map_ranking_uses_distance_from_the_resolved_dining_place()
    {
        var besideDiningTable = DiningCutlerySearchPolicy.MapScore(
            serviceScore: 50f,
            distanceSquaredFromDiningPlace: 1);
        var besideStartingPosition = DiningCutlerySearchPolicy.MapScore(
            serviceScore: 50f,
            distanceSquaredFromDiningPlace: 400);

        Assert.That(besideDiningTable, Is.GreaterThan(besideStartingPosition));
    }

    [Test]
    public void Colonist_prefers_already_carried_clean_cutlery_over_clean_map_cutlery()
    {
        var map = new[]
        {
            new ServiceWareCandidate<string>("map", isDirty: false, serviceScore: 100f)
        };
        var inventory = new[]
        {
            new ServiceWareCandidate<string>("inventory", isDirty: false, serviceScore: 20f)
        };

        var selected = SelfDiningCutlerySelectionPolicy.Select(
            map,
            inventory,
            preferColonyService: false,
            WareRequirementMode.Prefer,
            DirtyWareFallback.Always,
            isEmergency: false);

        Assert.That(selected, Is.EqualTo("inventory"));
    }

    [Test]
    public void Cutlery_leg_runs_after_native_dining_resolution_and_before_chewing()
    {
        var ordered = DiningCutleryToilOrder.InsertBefore(
                new[] { "carry meal", "find eat surface", "chew" },
                toil => toil == "chew",
                new[] { "select cutlery", "pick up cutlery", "return to chew spot" })
            .ToArray();

        Assert.That(ordered, Is.EqualTo(new[]
        {
            "carry meal",
            "find eat surface",
            "select cutlery",
            "pick up cutlery",
            "return to chew spot",
            "chew"
        }));
    }

    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, true)]
    public void Existing_or_waiter_owned_session_does_not_select_a_second_setting(
        bool hasSelectedCutlery,
        bool hasCarriedCutlery,
        bool hasServingPawn)
    {
        Assert.That(
            DiningCutleryDeferredSelectionPolicy.ShouldSelect(
                hasSelectedCutlery,
                hasCarriedCutlery,
                hasServingPawn),
            Is.False);
    }

    [Test]
    public void Empty_self_dining_session_selects_cutlery_at_the_dining_place()
    {
        Assert.That(
            DiningCutleryDeferredSelectionPolicy.ShouldSelect(
                hasSelectedCutlery: false,
                hasCarriedCutlery: false,
                hasServingPawn: false),
            Is.True);
    }

    [Test]
    public void Map_pickup_requires_the_selected_stack_to_remain_spawned_and_reserved()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                DiningCutleryPickupPolicy.CanTakeSelectedMapCutlery(
                    isSpawned: true,
                    isReservedBySession: true,
                    isStillAllowed: true),
                Is.True);
            Assert.That(
                DiningCutleryPickupPolicy.CanTakeSelectedMapCutlery(
                    isSpawned: false,
                    isReservedBySession: true,
                    isStillAllowed: true),
                Is.False);
            Assert.That(
                DiningCutleryPickupPolicy.CanTakeSelectedMapCutlery(
                    isSpawned: true,
                    isReservedBySession: false,
                    isStillAllowed: true),
                Is.False);
            Assert.That(
                DiningCutleryPickupPolicy.CanTakeSelectedMapCutlery(
                    isSpawned: true,
                    isReservedBySession: true,
                    isStillAllowed: false),
                Is.False);
        });
    }

    [Test]
    public void Missing_native_chewing_landmark_is_reported()
    {
        var missing = false;

        var ordered = DiningCutleryToilOrder.InsertBefore(
                new[] { "carry meal", "find eat surface" },
                toil => toil == "chew",
                new[] { "select cutlery" },
                onMissing: () => missing = true)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(ordered, Is.EqualTo(new[] { "carry meal", "find eat surface" }));
            Assert.That(missing, Is.True);
        });
    }
}
