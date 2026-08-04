using NUnit.Framework;
using Verse.AI;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class StableJobIdentityTests
{
    [Test]
    public void RecycledJobObjectDoesNotMatchItsCapturedLoadIdentity()
    {
        var pooledJob = new Job { loadID = 41 };
        var identity = new StableJobIdentity(pooledJob);

        Assert.That(identity.Matches(pooledJob), Is.True);

        pooledJob.loadID = 42;

        Assert.That(identity.Matches(pooledJob), Is.False);
    }
}
