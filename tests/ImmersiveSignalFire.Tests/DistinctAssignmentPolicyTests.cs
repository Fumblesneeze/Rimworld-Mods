using System.Collections.Generic;
using ImmersiveSignalFire.Signals;
using NUnit.Framework;

namespace ImmersiveSignalFire.Tests;

[TestFixture]
public sealed class DistinctAssignmentPolicyTests
{
    [Test]
    public void ConstrainedParticipantGetsOnlyReachableCell()
    {
        var candidates = new Dictionary<string, IReadOnlyList<string>>
        {
            ["flexible"] = new[] { "near", "far" },
            ["constrained"] = new[] { "near" },
        };

        bool found = DistinctAssignmentPolicy.TryAssign(
            new[] { "flexible", "constrained" },
            actor => candidates[actor],
            out IReadOnlyList<string> assigned);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(assigned, Is.EqualTo(new[] { "far", "near" }));
        });
    }

    [Test]
    public void ImpossibleDistinctAssignmentFailsClosed()
    {
        bool found = DistinctAssignmentPolicy.TryAssign(
            new[] { "first", "second" },
            _ => new[] { "only" },
            out IReadOnlyList<string> assigned);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.False);
            Assert.That(assigned, Is.Empty);
        });
    }
}
