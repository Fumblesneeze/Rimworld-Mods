using System.Threading.Tasks;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayDispatcherTests
{
    [Test]
    public void Drain_executes_queued_work_in_fifo_order_and_not_before()
    {
        var dispatcher = new GatewayDispatcher(capacity: 4);
        var order = new List<int>();

        var first = dispatcher.Enqueue(
            "request-1",
            "first",
            TimeSpan.FromSeconds(5),
            _ => { order.Add(1); return "one"; });
        var second = dispatcher.Enqueue(
            "request-2",
            "second",
            TimeSpan.FromSeconds(5),
            _ => { order.Add(2); return "two"; });

        Assert.Multiple(() =>
        {
            Assert.That(first.IsCompleted, Is.False);
            Assert.That(second.IsCompleted, Is.False);
            Assert.That(order, Is.Empty);
        });

        var drained = dispatcher.Drain(DispatchPhase.Update, maximumItems: 8);

        Assert.Multiple(() =>
        {
            Assert.That(drained, Is.EqualTo(2));
            Assert.That(order, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(first.GetAwaiter().GetResult(), Is.EqualTo("one"));
            Assert.That(second.GetAwaiter().GetResult(), Is.EqualTo("two"));
        });
    }

    [Test]
    public void Default_drain_budget_executes_only_one_operation_per_frame()
    {
        var dispatcher = new GatewayDispatcher(capacity: 4);
        var order = new List<int>();
        var first = dispatcher.Enqueue(
            "request-frame-1",
            "first-heavy-operation",
            TimeSpan.FromSeconds(5),
            _ => { order.Add(1); return "one"; });
        var second = dispatcher.Enqueue(
            "request-frame-2",
            "second-heavy-operation",
            TimeSpan.FromSeconds(5),
            _ => { order.Add(2); return "two"; });

        var firstFrame = dispatcher.Drain(DispatchPhase.Update);

        Assert.Multiple(() =>
        {
            Assert.That(firstFrame, Is.EqualTo(1));
            Assert.That(order, Is.EqualTo(new[] { 1 }));
            Assert.That(first.GetAwaiter().GetResult(), Is.EqualTo("one"));
            Assert.That(second.IsCompleted, Is.False);
            Assert.That(dispatcher.PendingCount, Is.EqualTo(1));
        });

        var secondFrame = dispatcher.Drain(DispatchPhase.Update);

        Assert.Multiple(() =>
        {
            Assert.That(secondFrame, Is.EqualTo(1));
            Assert.That(order, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(second.GetAwaiter().GetResult(), Is.EqualTo("two"));
        });
    }

    [Test]
    public void Drain_skips_work_that_expired_in_the_queue()
    {
        var now = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        var dispatcher = new GatewayDispatcher(capacity: 1, utcNow: () => now);
        var executed = false;
        var result = dispatcher.Enqueue(
            "request-expired",
            "expired",
            TimeSpan.FromMilliseconds(50),
            _ => { executed = true; return "unexpected"; });

        now = now.AddMilliseconds(51);
        dispatcher.Drain(DispatchPhase.Update, maximumItems: 1);

        Assert.Multiple(() =>
        {
            Assert.That(executed, Is.False);
            Assert.That(
                () => result.GetAwaiter().GetResult(),
                Throws.TypeOf<GatewayDispatchException>()
                    .With.Property(nameof(GatewayDispatchException.Code)).EqualTo("timed_out_before_start"));
        });
    }

    [Test]
    public void Operation_handle_distinguishes_queued_from_started_work()
    {
        var dispatcher = new GatewayDispatcher(capacity: 2);
        var operation = dispatcher.EnqueueOperation(
            "request-state",
            "stateful",
            TimeSpan.FromSeconds(5),
            _ => 42);

        Assert.Multiple(() =>
        {
            Assert.That(operation.HasStarted, Is.False);
            Assert.That(operation.Completion.IsCompleted, Is.False);
        });

        dispatcher.Drain(DispatchPhase.Update, maximumItems: 1);

        Assert.Multiple(() =>
        {
            Assert.That(operation.HasStarted, Is.True);
            Assert.That(operation.Completion.GetAwaiter().GetResult(), Is.EqualTo(42));
        });
    }

    [Test]
    public void Stop_cancels_queued_work_and_rejects_all_later_admission_idempotently()
    {
        var dispatcher = new GatewayDispatcher(capacity: 4);
        var update = dispatcher.Enqueue(
            "request-update",
            "update",
            TimeSpan.FromSeconds(5),
            _ => "unexpected");
        var endOfFrame = dispatcher.Enqueue(
            "request-frame",
            "frame",
            TimeSpan.FromSeconds(5),
            _ => "unexpected",
            DispatchPhase.EndOfFrame);

        var cancelled = dispatcher.Stop();
        var cancelledAgain = dispatcher.Stop();
        var rejected = dispatcher.Enqueue(
            "request-late",
            "late",
            TimeSpan.FromSeconds(5),
            _ => "unexpected");

        Assert.Multiple(() =>
        {
            Assert.That(cancelled, Is.EqualTo(2));
            Assert.That(cancelledAgain, Is.Zero);
            Assert.That(dispatcher.IsStopped, Is.True);
            Assert.That(dispatcher.PendingCount, Is.Zero);
            Assert.That(dispatcher.Drain(DispatchPhase.Update), Is.Zero);
            AssertStopping(update);
            AssertStopping(endOfFrame);
            AssertStopping(rejected);
        });
    }

    private static void AssertStopping(Task<string> result)
    {
        Assert.That(
            () => result.GetAwaiter().GetResult(),
            Throws.TypeOf<GatewayDispatchException>()
                .With.Property(nameof(GatewayDispatchException.Code)).EqualTo("gateway_stopping"));
    }
}
