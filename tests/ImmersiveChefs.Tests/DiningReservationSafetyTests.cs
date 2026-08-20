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

    [TestCase(true, false, false, "ApproachAndCarry")]
    [TestCase(false, true, false, "ReheatAfterVanillaInventoryTransfer")]
    [TestCase(false, false, true, "AlreadyCarried")]
    [TestCase(false, false, false, "SkipOptionalReheat")]
    public void Microwave_pickup_plan_respects_the_meals_actual_holder(
        bool spawned,
        bool heldInCarrierInventory,
        bool alreadyCarried,
        string expected)
    {
        Assert.That(
            DiningMealPickupPolicy.For(
                spawned,
                heldInCarrierInventory,
                alreadyCarried).ToString(),
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
}
