using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class DiningReservationSafetyTests
{
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Optional_dining_attachment_never_turns_a_successful_native_reservation_into_failure(
        bool nativeReservationSucceeded,
        bool diningAttachmentSucceeded)
    {
        Assert.That(
            DiningReservationSafety.KeepNativeResult(
                nativeReservationSucceeded,
                diningAttachmentSucceeded),
            Is.EqualTo(nativeReservationSucceeded));
    }

    [TestCase(true, false, false, true, "ApproachAndCarry")]
    [TestCase(false, true, false, true, "ReheatAfterVanillaInventoryTransfer")]
    [TestCase(false, true, false, false, "SkipOptionalReheat")]
    [TestCase(false, false, true, false, "AlreadyCarried")]
    [TestCase(false, false, false, true, "SkipOptionalReheat")]
    public void Microwave_pickup_plan_respects_the_meals_holder_and_native_transfer_seam(
        bool spawned,
        bool heldInCarrierInventory,
        bool alreadyCarried,
        bool nativeToilsStartWithInventoryTransfer,
        string expected)
    {
        Assert.That(
            DiningMealPickupPolicy.For(
                spawned,
                heldInCarrierInventory,
                alreadyCarried,
                nativeToilsStartWithInventoryTransfer).ToString(),
            Is.EqualTo(expected));
    }

    [Test]
    public void Inventory_reheat_runs_after_native_transfer_and_before_native_ingestion()
    {
        Assert.That(
            DiningMealToilOrder.InsertAfterInventoryTransfer(
                new[] { "native inventory transfer", "native carry and ingest" },
                new[] { "microwave walk", "microwave heat" }),
            Is.EqualTo(new[]
            {
                "native inventory transfer",
                "microwave walk",
                "microwave heat",
                "native carry and ingest"
            }));
    }

    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    public void Assisted_feeding_reserves_a_microwave_only_when_its_meal_can_use_it(
        bool pasteDispenser,
        bool mealHeldInFeederInventory,
        bool expected)
    {
        Assert.That(
            AssistedFeedingMicrowavePolicy.ShouldReserve(
                pasteDispenser,
                mealHeldInFeederInventory),
            Is.EqualTo(expected));
    }

    [TestCase(false, false, "UseRequestedCell")]
    [TestCase(true, false, "Defer")]
    [TestCase(true, true, "UseResolvedCellDirectly")]
    public void Near_ware_placement_is_resolved_before_one_direct_attempt(
        bool requestedNear,
        bool nearCellResolved,
        string expected)
    {
        Assert.That(
            DiningWarePlacementPolicy.For(requestedNear, nearCellResolved).ToString(),
            Is.EqualTo(expected));
    }
}
