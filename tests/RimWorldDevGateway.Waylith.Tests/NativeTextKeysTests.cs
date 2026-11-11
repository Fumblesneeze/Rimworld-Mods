using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeTextKeysTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void RevocationCannotMistakeReplacementForeignKeysForOwnedInput(bool down)
    {
        var keys = Chord(); keys.Invalidate();
        var released = new List<int>();
        Assert.Throws<AggregateException>(() => keys.Cleanup(key =>
        {
            if (keys.HasForeignEdges(key, 5, down, false, false)) throw new InvalidOperationException("foreign key");
            released.Add(key);
        }, () => { }));
        Assert.That(released, Is.Empty);
        Assert.Throws<InvalidOperationException>(() => keys.Record(1, 5, true), "A queue callback must not restore revoked ownership.");
    }

    [TestCase("ambiguous")]
    [TestCase("release-throws")]
    public void CleanupAttemptsIndependentKeysAndCharacterAfterOneKeyFails(string failure)
    {
        var keys = Chord(); var attempted = new List<int>(); var released = new List<int>(); bool characterCleaned = false;
        var error = Assert.Throws<AggregateException>(() => keys.Cleanup(key =>
        {
            attempted.Add(key);
            if (key == 1) throw new InvalidOperationException(failure);
            keys.Record(key, 5, false); keys.Released(key); released.Add(key);
        }, () => characterCleaned = true));
        Assert.That(attempted, Is.EquivalentTo(new[] { 1, 2 }));
        Assert.That(released, Is.EqualTo(new[] { 2 }));
        Assert.That(keys.IsHeld(1), Is.True); Assert.That(keys.IsHeld(2), Is.False);
        Assert.That(characterCleaned, Is.True); Assert.That(error!.InnerExceptions.Single().Message, Is.EqualTo(failure));
    }

    [Test]
    public void CleanupAggregatesEachFailureAndDoesNotHideTheCharacterFailure()
    {
        var keys = Chord();
        var error = Assert.Throws<AggregateException>(() => keys.Cleanup(key => throw new InvalidOperationException("key" + key),
            () => throw new InvalidOperationException("character")));
        Assert.That(error!.InnerExceptions.Select(item => item.Message), Is.EquivalentTo(new[] { "key1", "key2", "character" }));
    }
    private static GatewayNativeTextKeys Chord()
    {
        var keys = new GatewayNativeTextKeys();
        foreach (int key in new[] { 1, 2 }) { keys.BeforePress(key); keys.Record(key, 5, true); }
        return keys;
    }
}
