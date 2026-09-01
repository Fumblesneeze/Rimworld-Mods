using ImmersiveSignalFire.Signals;
using NUnit.Framework;

namespace ImmersiveSignalFire.Tests;

[TestFixture]
public sealed class SignalQualityTests
{
    [TestCase(20, 20, 1f)]
    [TestCase(10, 10, 0.5f)]
    [TestCase(0, 0, 0f)]
    [TestCase(-4, 25, 0.5f)]
    public void IndividualScoreClampsBothSkills(int melee, int social, float expected)
    {
        Assert.That(SignalQuality.Individual(melee, social), Is.EqualTo(expected).Within(0.0001f));
    }

    [Test]
    public void GroupScoreEquallyAveragesEverySelectedParticipant()
    {
        float quality = SignalQuality.Group(new[]
        {
            new SignalSkills(20, 20),
            new SignalSkills(10, 10),
            new SignalSkills(0, 0),
        });

        Assert.That(quality, Is.EqualTo(0.5f).Within(0.0001f));
    }

    [TestCase(0.5001f, SignalOutcome.Immediate)]
    [TestCase(0.5f, SignalOutcome.Delayed)]
    [TestCase(0.3001f, SignalOutcome.Delayed)]
    [TestCase(0.3f, SignalOutcome.Misunderstood)]
    [TestCase(0.15f, SignalOutcome.Misunderstood)]
    [TestCase(0.1499f, SignalOutcome.Unnoticed)]
    public void StrictRequestedThresholdsHaveCompleteBoundaryBehavior(float quality, SignalOutcome expected)
    {
        Assert.That(SignalQuality.Outcome(quality), Is.EqualTo(expected));
    }

    [Test]
    public void EmptyParticipantSetIsRejected()
    {
        Assert.That(
            () => SignalQuality.Group(System.Array.Empty<SignalSkills>()),
            Throws.ArgumentException);
    }
}
