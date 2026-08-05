using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndTaskStepOperationTests
{
    [Test]
    public void Pending_task_is_polled_without_blocking_and_cannot_be_read_early()
    {
        var source = new TaskCompletionSource<GatewayEndToEndStepOutcome>();
        var operation = new GatewayEndToEndTaskStepOperation(
            source.Task,
            "async_failed",
            "The asynchronous action failed.");

        Assert.Multiple(() =>
        {
            Assert.That(operation.IsCompleted, Is.False);
            Assert.Throws<InvalidOperationException>(() => operation.GetOutcome());
        });
    }

    [Test]
    public void Successful_task_returns_its_exact_outcome()
    {
        var expected = GatewayEndToEndStepOutcome.Pass(
            new Dictionary<string, string> { ["screenshot"] = "evidence.png" });
        var operation = new GatewayEndToEndTaskStepOperation(
            Task.FromResult(expected),
            "async_failed",
            "The asynchronous action failed.");

        Assert.Multiple(() =>
        {
            Assert.That(operation.IsCompleted, Is.True);
            Assert.That(operation.GetOutcome(), Is.SameAs(expected));
        });
    }

    [Test]
    public void Faulted_or_cancelled_task_returns_only_the_owned_bounded_failure()
    {
        const string secret = "do-not-leak-this-token";
        var faulted = new GatewayEndToEndTaskStepOperation(
            Task.FromException<GatewayEndToEndStepOutcome>(new InvalidOperationException(secret)),
            "async_failed",
            "The asynchronous action failed.");
        var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = new GatewayEndToEndTaskStepOperation(
            Task.FromCanceled<GatewayEndToEndStepOutcome>(cancellation.Token),
            "async_cancelled",
            "The asynchronous action was cancelled.");

        Assert.Multiple(() =>
        {
            Assert.That(faulted.GetOutcome().FailureCode, Is.EqualTo("async_failed"));
            Assert.That(faulted.GetOutcome().FailureMessage, Does.Not.Contain(secret));
            Assert.That(cancelled.GetOutcome().FailureCode, Is.EqualTo("async_cancelled"));
        });
    }
}
