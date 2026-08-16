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
}
