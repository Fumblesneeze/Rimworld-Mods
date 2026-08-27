using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class WorkshopSubscriptionCleanerTests
{
    [TestCase(0u, false)]
    [TestCase(1u, true)]
    [TestCase(4u, false)]
    [TestCase(5u, true)]
    [TestCase(63u, true)]
    public void IsSubscribed_UsesOnlySteamsSubscribedBit(uint state, bool expected)
    {
        Assert.That(WorkshopSubscriptionCleaner.IsSubscribed(state), Is.EqualTo(expected));
    }

    [Test]
    public void SubscriberFailure_StillRunsMandatoryUnsubscribeCleanup()
    {
        var cleanupCalls = 0;

        var error = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await ReleasePublisher.RunWithMandatoryCleanupAsync(
                () => Task.FromException<string>(new InvalidOperationException("subscriber failed")),
                () =>
                {
                    cleanupCalls++;
                    return Task.FromResult("unsubscribed");
                }));

        Assert.Multiple(() =>
        {
            Assert.That(error!.Message, Is.EqualTo("subscriber failed"));
            Assert.That(cleanupCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task SuccessfulSubscriber_ReturnsVerifiedCleanupResult()
    {
        var result = await ReleasePublisher.RunWithMandatoryCleanupAsync(
            () => Task.FromResult("subscriber passed"),
            () => Task.FromResult("unsubscribed"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Primary, Is.EqualTo("subscriber passed"));
            Assert.That(result.Cleanup, Is.EqualTo("unsubscribed"));
        });
    }
}
