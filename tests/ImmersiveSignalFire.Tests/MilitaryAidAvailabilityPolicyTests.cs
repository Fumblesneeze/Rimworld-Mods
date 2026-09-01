using ImmersiveSignalFire.Signals;
using NUnit.Framework;

namespace ImmersiveSignalFire.Tests;

[TestFixture]
public sealed class MilitaryAidAvailabilityPolicyTests
{
    [TestCase(10_000, 69_999, 1)]
    [TestCase(10_000, 70_000, 0)]
    [TestCase(-99_999, 1, 0)]
    public void RemainingCooldownMatchesNativeSixtyThousandTickGate(
        int lastRequestTick,
        int currentTick,
        int expectedRemaining)
    {
        Assert.That(
            MilitaryAidAvailabilityPolicy.RemainingCooldownTicks(lastRequestTick, currentTick),
            Is.EqualTo(expectedRemaining));
    }
}
