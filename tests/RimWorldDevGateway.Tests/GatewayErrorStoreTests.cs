using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayErrorStoreTests
{
    [Test]
    public void ExceptionCorrelation_RequiresWholeRenderedException_AndNeverReadsCustomMessage()
    {
        var unsafeException = new UnsafeMessageException();
        Assert.That(GatewayExceptionCorrelation.SafeMessage(unsafeException), Does.Contain("suppressed"));
        Assert.That(unsafeException.Reads, Is.Zero);
        Assert.That(GatewayExceptionCorrelation.CanNormalize(unsafeException), Is.False);
        Assert.That(GatewayExceptionCorrelation.CanNormalize(new Exception("one")), Is.False);
        Assert.That(GatewayExceptionCorrelation.CanNormalize(new InvalidOperationException("outer", unsafeException)), Is.False);
        Assert.That(GatewayExceptionCorrelation.CanNormalize(new InvalidOperationException("outer", new ArgumentException("inner"))), Is.True);
        const string rendered = "System.InvalidOperationException: failed\n  at ExactMethod()";
        Assert.That(GatewayExceptionCorrelation.Matches(rendered, "Different operation failed"), Is.False);
        Assert.That(GatewayExceptionCorrelation.Matches(rendered, "Command error: " + rendered), Is.True);
        Assert.That(GatewayExceptionCorrelation.Matches("", "anything"), Is.False);
        Assert.That(GatewayExceptionCorrelation.CanonicalMessage(rendered, "Command error: " + rendered, "System.InvalidOperationException: failed"),
            Is.EqualTo("Command error: System.InvalidOperationException: failed"));
    }

    private sealed class UnsafeMessageException : Exception
    {
        public int Reads;
        public override string Message { get { Reads++; throw new InvalidOperationException(); } }
    }

    [Test]
    public void TruncatedErrorsNeverClaimEquivalence_AndShareOneRetentionBudget()
    {
        var store = new GatewayErrorStore();
        var causes = Enumerable.Range(0, 8).Select(_ => new GatewayErrorCause("inner", new string('x', 9000),
            new string('s', 40000), Enumerable.Range(0, 64).Select(i => new GatewayCapturedFrame("h", new string('f', 2048))).ToArray())).ToArray();
        var first = store.Add("error", "message", "stack", causes: causes);
        var second = store.Add("error", "message", "stack", causes: causes);
        Assert.That(first.Truncated, Is.True);
        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
        Assert.That(first.Causes.Sum(c => c.Type.Length + c.Message.Length + c.Stack.Length +
            c.Frames.Sum(f => f.MethodHandle.Length + f.Signature.Length)), Is.LessThanOrEqualTo(65536));
        Assert.That(first.Causes.Sum(c => c.Frames.Count), Is.LessThanOrEqualTo(64));
    }

    [Test]
    public void ExactMethodIdentity_RejectsGenericContextRatherThanResolvingDifferentMethod()
    {
        Assert.Throws<ArgumentException>(() => GatewayMethodIdentity.Handle(typeof(List<int>).GetMethod("Add")!));
        Assert.Throws<ArgumentException>(() => GatewayMethodIdentity.Handle(typeof(List<>).GetMethod("Add")!));
        var methods = typeof(Activator).GetMethods().Where(method => method.Name == "CreateInstance").ToArray();
        var handles = methods.Select(GatewayMethodIdentity.TryHandle).ToArray();
        Assert.That(handles.Any(handle => handle is null), Is.True);
        Assert.That(handles.Any(handle => handle is not null), Is.True);
    }

    [Test]
    public void CausesAndBoundedHistory_AreDistinctAndRedacted()
    {
        var store = new GatewayErrorStore(2, value => value.Replace("secret", "[redacted]"));
        var first = store.Add("outer", "secret", "stack", causes: new[] { new GatewayErrorCause("inner", "one", "a") });
        var second = store.Add("outer", "secret", "stack", causes: new[] { new GatewayErrorCause("inner", "two", "b") });
        var third = store.Add("outer", "third", "stack");
        var page = store.Query(limit: 1);
        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.Not.EqualTo(second.Id));
            Assert.That(first.Message, Does.Not.Contain("secret"));
            Assert.That(page.EvictedErrors, Is.EqualTo(1));
            Assert.That(page.PageTruncated, Is.True);
            Assert.That(store.Query(page.NextCursor).Entries.Single().Id, Is.EqualTo(third.Id));
            Assert.That(store.Query(filter: "THIRD").Entries.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void ExactMethodIdentity_DistinguishesOverloadsAndRejectsStaleHandles()
    {
        var methods = typeof(string).GetMethods().Where(method => method.Name == "Substring").ToArray();
        var first = GatewayMethodIdentity.Handle(methods[0]);
        var second = GatewayMethodIdentity.Handle(methods[1]);
        Assert.That(first, Is.Not.EqualTo(second));
        Assert.That(GatewayMethodIdentity.Resolve(first), Is.EqualTo(methods[0]));
        Assert.Throws<ArgumentException>(() => GatewayMethodIdentity.Resolve("stale"));
    }

    [Test]
    public void RepeatedErrors_GroupOccurrencesWithoutMutatingEarlierSnapshots()
    {
        var store = new GatewayErrorStore();
        var first = store.Add("Error", "native command failed", "at Command()", requestId: "one");
        var second = store.Add("Error", "native command failed", "at Command()", requestId: "two");
        Assert.Multiple(() =>
        {
            Assert.That(second.Id, Is.EqualTo(first.Id));
            Assert.That(first.Occurrences, Is.EqualTo(1));
            Assert.That(second.Occurrences, Is.EqualTo(2));
            Assert.That(second.LastRequestId, Is.EqualTo("two"));
            Assert.That(store.Query().Entries.Count, Is.EqualTo(1));
        });
    }
}
