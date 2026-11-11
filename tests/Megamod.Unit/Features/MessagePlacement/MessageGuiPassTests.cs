using Megamod.Features.MessagePlacement;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.MessagePlacement;

[TestFixture]
public sealed class MessageGuiPassTests
{
    [Test]
    public void NativeObservationsAreCommittedTogetherOnlyAfterSuccessfulPassCompletion()
    {
        var pass = new MessageGuiPass();
        var values = new List<MessageBounds>();
        var token = pass.Begin();
        Assert.That(pass.Observe(new object(), new(new(10, 10, 100, 25), values.Add, () => {})), Is.True);
        Assert.That(pass.Observe(new object(), new(new(10, 51, 90, 25), values.Add, () => {})), Is.True);
        Assert.That(values, Is.Empty);
        Assert.That(pass.Complete(token, true, 1000, 800, null, new(10, 0)), Is.True);
        Assert.That(values, Is.EqualTo(new[] { new MessageBounds(110, 10, 100, 25), new MessageBounds(110, 51, 90, 25) }));
        Assert.That(pass.Observe(new object(), new(new(10, 10, 1, 1), values.Add, () => {})), Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AbortedOrInvalidatedPassDoesNotMoveAnythingOrLeakIntoTheNextPass(bool invalidate)
    {
        var pass = new MessageGuiPass();
        int writes = 0;
        var first = pass.Begin();
        pass.Observe(new object(), new(new(10, 10, 100, 25), _ => writes++, () => {}));
        if (invalidate) pass.Invalidate();
        Assert.That(pass.Complete(first, invalidate, 1000, 800, null, new(10, 0)), Is.False);
        Assert.That(writes, Is.Zero);
        var second = pass.Begin();
        pass.Observe(new object(), new(new(10, 10, 100, 25), _ => writes++, () => {}));
        Assert.That(pass.Complete(second, true, 1000, 800, null, default), Is.True);
        Assert.That(writes, Is.EqualTo(1));
        Assert.That(pass.Complete(first, true, 1000, 800, null, default), Is.False);
    }

    [Test]
    public void NestedPassesAreRejectedTogetherAndLeaveNoActiveState()
    {
        var pass = new MessageGuiPass();
        int writes = 0;
        MessageSurface Surface() => new(new(10, 10, 100, 25), _ => writes++, () => {});
        var outer = pass.Begin();
        pass.Observe(new object(), Surface());
        var inner = pass.Begin();
        pass.Observe(new object(), Surface());
        Assert.That(pass.Complete(inner, true, 1000, 800, null, new(10, 0)), Is.False);
        Assert.That(pass.Complete(outer, true, 1000, 800, null, new(10, 0)), Is.False);
        Assert.That(writes, Is.Zero);
        var next = pass.Begin();
        pass.Observe(new object(), Surface());
        Assert.That(pass.Complete(next, true, 1000, 800, null, default), Is.True);
        Assert.That(writes, Is.EqualTo(1));
    }

    [Test]
    public void RepeatedMessageIdentityInvalidatesTheWholeObservedGroup()
    {
        var pass = new MessageGuiPass();
        var message = new object();
        int writes = 0;
        var token = pass.Begin();
        pass.Observe(message, new(new(10, 10, 100, 25), _ => writes++, () => {}));
        Assert.That(pass.Observe(message, new(new(10, 40, 100, 25), _ => writes++, () => {})), Is.False);
        Assert.That(pass.Complete(token, true, 1000, 800, null, new(10, 0)), Is.False);
        Assert.That(writes, Is.Zero);
    }
}
