using NUnit.Framework;
using Verse;

namespace Hospitality
{
    public sealed class CompGuest
    {
    }
}

namespace Hospitality.Utilities
{
    public static class GuestUtility
    {
        public static bool IsArrivedGuest(Pawn pawn, out Hospitality.CompGuest comp)
        {
            comp = new Hospitality.CompGuest();
            return true;
        }
    }
}

namespace ImmersiveChefs.Tests
{
    [TestFixture]
    public sealed class HospitalityAdapterTests
    {
        [Test]
        public void Adapter_accepts_and_invokes_the_exact_public_static_guest_shape()
        {
            var bound = HospitalityAdapter.TryBind(
                typeof(Hospitality.Utilities.GuestUtility),
                out var isArrivedGuest,
                out var reason);

            Assert.Multiple(() =>
            {
                Assert.That(bound, Is.True, reason);
                Assert.That(isArrivedGuest, Is.Not.Null);
                Assert.That(isArrivedGuest!(null!), Is.True);
            });
        }

        [Test]
        public void Adapter_rejects_a_changed_guest_utility_shape()
        {
            var changedMethod = typeof(ChangedGuestUtility).GetMethod(
                nameof(ChangedGuestUtility.IsArrivedGuest));
            var bound = HospitalityAdapter.TryBindMethod(
                changedMethod,
                out var isArrivedGuest,
                out var reason);

            Assert.Multiple(() =>
            {
                Assert.That(bound, Is.False);
                Assert.That(isArrivedGuest, Is.Null);
                Assert.That(reason, Does.Contain("Hospitality.CompGuest"));
            });
        }

        private static class ChangedGuestUtility
        {
            public static bool IsArrivedGuest(Pawn pawn) => true;
        }
    }
}
