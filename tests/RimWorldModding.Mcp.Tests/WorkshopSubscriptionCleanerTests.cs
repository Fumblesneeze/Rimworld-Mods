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
    public void CleanupResult_HasOneDeterministicCliProjection()
    {
        var result = new WorkshopSubscriptionCleanupResult(
            "already-unsubscribed",
            "fumblesneeze.immersivechefs",
            "3782589902",
            0,
            0,
            false,
            false,
            "subscription-cleanup.json",
            true);

        var json = OperationJson.Serialize(result);
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("status").GetString(), Is.EqualTo("already-unsubscribed"));
            Assert.That(root.GetProperty("isSubscribed").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("gatewayStopped").GetBoolean(), Is.True);
        });
    }
}
