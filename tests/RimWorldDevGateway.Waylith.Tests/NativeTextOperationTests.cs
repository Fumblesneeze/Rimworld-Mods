using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeTextOperationTests
{
    [Test]
    public void FirstFocusRejectionSnapshotIsRetainedBeforeCleanupChangesTheObservedState()
    {
        double observedWidth = 148;
        int diagnosticReads = 0;
        var backend = new Backend { CleanupAction = () => observedWidth = 200 };
        var operation = new GatewayNativeTextOperation(backend, "123", () =>
        {
            diagnosticReads++;
            return new Dictionary<string, object?> { ["sameEditor"] = true, ["sameControl"] = true,
                ["sameWindow"] = true, ["capturedWidth"] = 200d, ["currentWidth"] = observedWidth };
        });
        for (int phase = 0; phase < 6; phase++) { operation.BeforeDispatch(); operation.AfterDispatch(true); }
        operation.BeforeDispatch(); operation.AfterDispatch(false, "stale_native_text_focus");
        var result = operation.Advance(false)!;
        var receipt = Receipt(result);
        var diagnostic = (IReadOnlyDictionary<string, object?>)receipt["focusFailure"]!;
        Assert.That(result.Error!.Code, Is.EqualTo("stale_native_text_focus"));
        Assert.That(receipt["charactersDispatched"], Is.EqualTo(2));
        Assert.That(diagnostic["currentWidth"], Is.EqualTo(148d));
        Assert.That(observedWidth, Is.EqualTo(200));
        Assert.That(receipt["cleanupComplete"], Is.True);
        operation.BeforeDispatch(); operation.AfterDispatch(false, "different-failure");
        Assert.That(operation.Advance(false), Is.SameAs(result));
        Assert.That(diagnosticReads, Is.EqualTo(1));
        Assert.That(backend.Cleanups, Is.EqualTo(1));
        Assert.That(System.Text.Encoding.UTF8.GetString(GatewayJsonWriter.Write(result.Result)), Does.Not.Contain("123"));
    }

    [Test]
    public void AFailedOptionalDiagnosticCannotReplaceTheInputFailureOrPreventOwnedCleanup()
    {
        var backend = new Backend();
        var operation = new GatewayNativeTextOperation(backend, "12", () => throw new InvalidOperationException("secret diagnostic detail"));
        operation.BeforeDispatch(); operation.AfterDispatch(false, "stale_native_text_focus");
        var result = operation.Advance(false)!;
        Assert.That(result.Error!.Code, Is.EqualTo("stale_native_text_focus"));
        Assert.That(Receipt(result)["focusFailure"], Is.Null);
        Assert.That(Receipt(result)["cleanupComplete"], Is.True);
        Assert.That(backend.Cleanups, Is.EqualTo(1));
        Assert.That(System.Text.Encoding.UTF8.GetString(GatewayJsonWriter.Write(result.Result)), Does.Not.Contain("secret"));
    }

    [Test]
    public void FinalReceiptRetainsOriginalRejectionAndEverySanitizedCleanupFailure()
    {
        var keys = new GatewayNativeTextKeys();
        foreach (int key in new[] { 1, 2 }) { keys.BeforePress(key); keys.Record(key, 5, true); }
        var backend = new Backend { CleanupAction = () => keys.Cleanup(key =>
            throw new GatewayAutomationException("native_input_ownership_lost", "credential-secret key " + key),
            () => throw new GatewayAutomationException("native_text_queue_ownership_lost", "credential-secret character")) };
        var operation = new GatewayNativeTextOperation(backend, "12");
        operation.BeforeDispatch(); operation.AfterDispatch(false, "stale_native_text_focus");
        var result = operation.Advance(false)!; var receipt = Receipt(result);
        Assert.That(receipt["initialFailure"], Is.EqualTo("stale_native_text_focus"));
        Assert.That(result.Error!.Code, Is.EqualTo("native_input_cleanup_incomplete"));
        Assert.That(receipt["cleanupComplete"], Is.False); Assert.That(operation.IsQuarantined, Is.True);
        var failures = (IReadOnlyList<IReadOnlyDictionary<string, object?>>)receipt["cleanupFailures"]!;
        Assert.That(failures.Select(item => item["code"]), Is.EqualTo(new[] {
            "native_input_ownership_lost", "native_input_ownership_lost", "native_text_queue_ownership_lost" }));
        Assert.That(failures.All(item => item["exceptionType"] is string type && type == typeof(GatewayAutomationException).FullName), Is.True);
        Assert.That(receipt["cleanupFailuresTruncated"], Is.False);
        Assert.That(System.Text.Encoding.UTF8.GetString(GatewayJsonWriter.Write(result.Result)), Does.Not.Contain("credential-secret"));
        Assert.That(operation.Advance(true), Is.SameAs(result));
        operation.BeforeDispatch(); operation.AfterDispatch(true);
        Assert.That(backend.Cleanups, Is.EqualTo(1)); Assert.That(backend.Queued, Has.Count.EqualTo(1));
    }

    [Test]
    public void FinalReceiptBoundsUnexpectedAggregatesWithoutReadingArbitraryExceptionPayloads()
    {
        var failures = Enumerable.Range(0, 20).Select(index => index == 0
            ? (Exception)new GatewayAutomationException("credential-secret-code", "credential-secret-message")
            : new UnsafeException()).ToArray();
        var operation = new GatewayNativeTextOperation(new Backend { CleanupAction = () => throw new AggregateException(failures) }, "");
        operation.BeforeDispatch(true); operation.AfterDispatch(false);
        var result = operation.Advance(false)!; var receipt = Receipt(result);
        var retained = (IReadOnlyList<IReadOnlyDictionary<string, object?>>)receipt["cleanupFailures"]!;
        Assert.That(retained, Has.Count.EqualTo(4));
        Assert.That(retained.All(item => item["code"] is "native_text_cleanup_failed"), Is.True);
        Assert.That(retained.All(item => item["exceptionType"] is string type && type.Length <= 128), Is.True);
        Assert.That(receipt["cleanupFailuresTruncated"], Is.True); Assert.That(receipt["initialFailure"], Is.Null);
        Assert.That(result.Error!.Code, Is.EqualTo("native_input_cleanup_incomplete")); Assert.That(operation.IsQuarantined, Is.True);
        Assert.That(System.Text.Encoding.UTF8.GetString(GatewayJsonWriter.Write(result.Result)), Does.Not.Contain("credential-secret"));
    }

    private sealed class UnsafeException : Exception
    {
        public override string Message => throw new AssertionException("Never read arbitrary exception Message.");
        public override string StackTrace => throw new AssertionException("Never read arbitrary exception StackTrace.");
    }

    [TestCase("")]
    [TestCase("-0.5")]
    [TestCase("Médicine")]
    public void ReplacementQueuesOnlyOneNativePhasePerDispatchAndPollingSendsNoInput(string text)
    {
        var backend = new Backend();
        var operation = new GatewayNativeTextOperation(backend, text);
        Assert.That(operation.Advance(false), Is.Null);
        Assert.That(backend.Queued, Is.Empty);
        for (int phase = 0; phase < text.Length + 4; phase++)
        {
            operation.BeforeDispatch();
            Assert.That(backend.Queued, Has.Count.EqualTo(phase + 1));
            operation.BeforeDispatch();
            Assert.That(backend.Queued, Has.Count.EqualTo(phase + 1), "A repeated hook cannot queue a second edge.");
            Assert.That(operation.Advance(false), Is.Null);
            operation.AfterDispatch(true);
        }
        Assert.That(backend.Queued.Take(4).Select(item => item.Phase), Is.EqualTo(new[]
        {
            GatewayNativeTextPhase.SelectAllPress, GatewayNativeTextPhase.SelectAllRelease,
            GatewayNativeTextPhase.ClearPress, GatewayNativeTextPhase.ClearRelease
        }));
        Assert.That(new string(backend.Queued.Skip(4).Select(item => item.Character).ToArray()), Is.EqualTo(text));
        var result = operation.Advance(false)!;
        Assert.That(result.Error, Is.Null);
        Assert.That(Receipt(result)["charactersDispatched"], Is.EqualTo(text.Length));
        Assert.That(Receipt(result)["cleanupComplete"], Is.True);
        Assert.That(backend.Cleanups, Is.EqualTo(1));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(4)]
    public void CancellationRetainsPartialDeliveryAndCleansOwnedInput(int completed)
    {
        var backend = new Backend();
        var operation = new GatewayNativeTextOperation(backend, "42");
        for (int i = 0; i < completed; i++) { operation.BeforeDispatch(); operation.AfterDispatch(true); }
        operation.Advance(true);
        operation.BeforeDispatch(); operation.AfterDispatch(false);
        var result = operation.Advance(false)!;
        Assert.That(backend.Queued, Has.Count.EqualTo(completed));
        Assert.That(backend.Cleanups, Is.EqualTo(1));
        Assert.That(Receipt(result)["stopped"], Is.True);
        Assert.That(Receipt(result)["cleanupComplete"], Is.True);
        Assert.That(Receipt(result)["charactersDispatched"], Is.EqualTo(Math.Max(0, completed - 4)));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RefusedDeliveryNeverCountsAsInputAndAmbiguousCleanupQuarantines(bool cleanupFails)
    {
        var backend = new Backend { CleanupFails = cleanupFails };
        var operation = new GatewayNativeTextOperation(backend, "123");
        operation.BeforeDispatch();
        operation.AfterDispatch(false, "stale_native_text_focus");
        var result = operation.Advance(false)!;
        Assert.That(result.Error!.Code, Is.EqualTo(cleanupFails ? "native_input_cleanup_incomplete" : "stale_native_text_focus"));
        Assert.That(Receipt(result)["selectAllDispatched"], Is.False);
        Assert.That(Receipt(result)["cleanupComplete"], Is.EqualTo(!cleanupFails));
        Assert.That(operation.IsQuarantined, Is.EqualTo(cleanupFails));
        operation.BeforeDispatch(); operation.AfterDispatch(true);
        Assert.That(backend.Queued, Has.Count.EqualTo(1));
        Assert.That(backend.Cleanups, Is.EqualTo(1));
    }

    private static IReadOnlyDictionary<string, object?> Receipt(GatewayDeferredAutomationResult result) =>
        (IReadOnlyDictionary<string, object?>)result.Result!;

    private sealed class Backend : IGatewayNativeTextBackend
    {
        public readonly List<(GatewayNativeTextPhase Phase, char Character)> Queued = new();
        public int Cleanups;
        public bool CleanupFails;
        public Action? CleanupAction;
        public string? Validate() => null;
        public void Queue(GatewayNativeTextPhase phase, char character) => Queued.Add((phase, character));
        public void Cleanup() { Cleanups++; CleanupAction?.Invoke(); if (CleanupFails) throw new InvalidOperationException("Owned ambiguity."); }
    }
}
