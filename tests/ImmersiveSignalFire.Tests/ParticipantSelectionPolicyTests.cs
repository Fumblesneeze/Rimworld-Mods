using ImmersiveSignalFire.Signals;
using NUnit.Framework;

namespace ImmersiveSignalFire.Tests;

[TestFixture]
public sealed class ParticipantSelectionPolicyTests
{
    [Test]
    public void ChoosingFourthPawnAsCallerAtomicallySwapsThePriorCaller()
    {
        object oldCaller = new();
        object helperA = new();
        object helperB = new();
        object requested = new();

        var actual = ParticipantSelectionPolicy.SelectCaller(
            new[] { oldCaller, helperA, helperB },
            oldCaller,
            requested,
            maximumParticipants: 3);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Has.Count.EqualTo(3));
            Assert.That(actual, Does.Contain(requested));
            Assert.That(actual, Does.Not.Contain(oldCaller));
            Assert.That(actual, Does.Contain(helperA));
            Assert.That(actual, Does.Contain(helperB));
        });
    }
}
