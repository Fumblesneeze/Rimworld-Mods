using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class NativeClickOperationTests
{
    [TestCase("cancel-before-move")]
    [TestCase("changed-before-move")]
    [TestCase("missing-native-drag")]
    [TestCase("move-throws")]
    [TestCase("cancel-after-move")]
    [TestCase("changed-before-release")]
    public void InterruptedDragRetainsObservedPhasesAndCleansTheOwnedPress(string fault)
    {
        var input = new RecordingInput(); var operation = new GatewayNativeClickOperation(input, drag: true);
        operation.BeforeDispatch(); operation.AfterDispatch(true);
        if (fault == "cancel-before-move") operation.Advance(true);
        if (fault == "changed-before-move") input.Rejection = "stale_window_instance";
        if (fault == "move-throws") input.ThrowOn = "move-held";
        operation.BeforeDispatch();
        var nativePass = new GatewayNativeMousePassGate();
        if (fault != "missing-native-drag") { nativePass.BeforePass(true, null, false); nativePass.AfterPass(true); }
        operation.AfterDispatch(nativePass.Dispatched);
        if (fault is "cancel-after-move" or "changed-before-release")
        {
            if (fault == "cancel-after-move") operation.Advance(true);
            else input.Rejection = "stale_window_instance";
            operation.BeforeDispatch(); operation.AfterDispatch(true);
        }
        var result = operation.Advance(false)!;
        Assert.That(result, Is.Not.Null);
        var receipt = (IReadOnlyDictionary<string, object?>)result.Result!;
        Assert.That(receipt["pressDispatched"], Is.True);
        Assert.That(receipt["dragDispatched"], Is.EqualTo(fault is "cancel-after-move" or "changed-before-release"));
        Assert.That(receipt["releaseDispatched"], Is.False);
        Assert.That(receipt["cleanupComplete"], Is.True);
        Assert.That(input.Events.TakeLast(2), Is.EqualTo(new[] { "release", "restore" }));
        if (fault == "missing-native-drag") Assert.That(result.Error?.Code, Is.EqualTo("native_input_not_dispatched"));
    }

    [Test]
    public void DragRequiresASeparateNativeHeldMovementBeforeRelease()
    {
        var input = new RecordingInput();
        var operation = new GatewayNativeClickOperation(input, drag: true);
        operation.BeforeDispatch(); operation.AfterDispatch(true);
        operation.BeforeDispatch();
        Assert.That(input.Events, Is.EqualTo(new[] { "validate:False", "press", "validate:True", "move-held" }));
        operation.AfterDispatch(true);
        Assert.That(operation.Advance(false), Is.Null);
        operation.BeforeDispatch(); operation.AfterDispatch(true);
        var result = operation.Advance(false)!;
        Assert.That(result.Error, Is.Null);
        Assert.That(((IReadOnlyDictionary<string, object?>)result.Result!)["dragDispatched"], Is.True);
        Assert.That(input.Events.TakeLast(3), Is.EqualTo(new[] { "validate:True", "release", "restore" }));
    }

    [Test]
    public void ClickCompletesOnlyAfterNativePressAndReleaseDispatch()
    {
        var input = new RecordingInput();
        var operation = new GatewayNativeClickOperation(input);
        Assert.That(operation.Advance(false), Is.Null);

        operation.BeforeDispatch();
        Assert.That(input.Events, Is.EqualTo(new[] { "validate:False", "press" }));
        Assert.That(operation.Advance(false), Is.Null);
        operation.AfterDispatch(true);
        Assert.That(operation.Advance(false), Is.Null);

        operation.BeforeDispatch();
        Assert.That(input.Events, Is.EqualTo(new[] { "validate:False", "press", "validate:True", "release" }));
        Assert.That(operation.Advance(false), Is.Null);
        operation.AfterDispatch(true);

        Assert.That(operation.Advance(false), Is.Not.Null);
        Assert.That(operation.Advance(false)!.Error, Is.Null);
        Assert.That(input.Events.Last(), Is.EqualTo("restore"));
        operation.BeforeDispatch();
        operation.AfterDispatch(true);
        Assert.That(input.Events.Count, Is.EqualTo(5), "Completed gestures cannot repeat input or cleanup.");
    }

    [Test]
    public void InvalidTargetBeforePressNeverTouchesInput()
    {
        var input = new RecordingInput { Rejection = "stale_window_instance" };
        var operation = new GatewayNativeClickOperation(input);

        operation.BeforeDispatch();
        operation.AfterDispatch(true);

        Assert.That(input.Events, Is.EqualTo(new[] { "validate:False" }));
        Assert.That(operation.Advance(false)!.Error!.Code, Is.EqualTo("stale_window_instance"));
    }

    [Test]
    public void ChangedWindowReleasesOwnedButtonAfterDispatchWithoutSendingItToTheNewWindow()
    {
        var input = new RecordingInput();
        var operation = new GatewayNativeClickOperation(input);
        operation.BeforeDispatch();
        operation.AfterDispatch(true);
        input.Rejection = "stale_window_instance";

        operation.BeforeDispatch();
        Assert.That(input.Events, Is.EqualTo(new[] { "validate:False", "press", "validate:True" }));
        Assert.That(operation.Advance(false), Is.Null, "Cleanup has not run yet.");
        operation.AfterDispatch(true);

        Assert.That(input.Events, Is.EqualTo(new[] { "validate:False", "press", "validate:True", "release", "restore" }));
        Assert.That(operation.Advance(false)!.Error!.Code, Is.EqualTo("stale_window_instance"));
        var receipt = (IReadOnlyDictionary<string, object?>)operation.Advance(false)!.Result!;
        Assert.That(receipt["pressDispatched"], Is.True);
        Assert.That(receipt["releaseDispatched"], Is.False);
        Assert.That(receipt["cleanupComplete"], Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CancellationStopsTargetInputAndCleansOnlyAlreadyOwnedState(bool afterPress)
    {
        var input = new RecordingInput();
        var operation = new GatewayNativeClickOperation(input);
        if (afterPress) { operation.BeforeDispatch(); operation.AfterDispatch(true); }

        operation.Advance(true);
        operation.BeforeDispatch();
        Assert.That(input.Events, Is.EqualTo(afterPress ? new[] { "validate:False", "press" } : Array.Empty<string>()));
        operation.AfterDispatch(true);

        Assert.That(input.Events, Is.EqualTo(afterPress ? new[] { "validate:False", "press", "release", "restore" } : Array.Empty<string>()));
        var receipt = (IReadOnlyDictionary<string, object?>)operation.Advance(false)!.Result!;
        Assert.That(receipt["pressDispatched"], Is.EqualTo(afterPress));
        Assert.That(receipt["releaseDispatched"], Is.False);
        Assert.That(receipt["stopped"], Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SkippedNativeDispatchFailsAndStillReleasesOwnedInput(bool afterPress)
    {
        var input = new RecordingInput();
        var operation = new GatewayNativeClickOperation(input);
        if (afterPress) { operation.BeforeDispatch(); operation.AfterDispatch(true); }
        operation.BeforeDispatch();

        operation.AfterDispatch(false);

        var outcome = operation.Advance(false);
        Assert.That(outcome, Is.Not.Null);
        Assert.That(outcome!.Error!.Code, Is.EqualTo("native_input_not_dispatched"));
        Assert.That(input.Events.TakeLast(2), Is.EqualTo(new[] { "release", "restore" }));
        var receipt = (IReadOnlyDictionary<string, object?>)outcome.Result!;
        Assert.That(receipt["pressDispatched"], Is.EqualTo(afterPress));
        Assert.That(receipt["releaseDispatched"], Is.False);
    }

    [Test]
    public void QueueFailureIsContainedAndReportsUncertainDispatchAfterCleanup()
    {
        var input = new RecordingInput { ThrowOn = "press" };
        var operation = new GatewayNativeClickOperation(input);

        Assert.DoesNotThrow(() => operation.BeforeDispatch());
        Assert.DoesNotThrow(() => operation.AfterDispatch(true));

        Assert.That(input.Events, Is.EqualTo(new[] { "validate:False", "press", "release", "restore" }));
        var outcome = operation.Advance(false)!;
        Assert.That(outcome.Error!.Code, Is.EqualTo("native_input_failed"));
        var receipt = (IReadOnlyDictionary<string, object?>)outcome.Result!;
        Assert.That(receipt["dispatchUncertain"], Is.True);
        Assert.That(receipt["cleanupComplete"], Is.True);
    }

    [TestCase("release")]
    [TestCase("restore")]
    public void CleanupFailureIsExplicitAndQuarantinedWithoutEndlessRetries(string failingAction)
    {
        var input = new RecordingInput();
        var operation = new GatewayNativeClickOperation(input);
        operation.BeforeDispatch();
        operation.AfterDispatch(true);
        input.ThrowOn = failingAction;
        operation.BeforeDispatch(true);

        Assert.DoesNotThrow(() => operation.AfterDispatch(true));

        Assert.That(input.Events.TakeLast(2), Is.EqualTo(new[] { "release", "restore" }));
        var outcome = operation.Advance(false)!;
        Assert.That(outcome.Error!.Code, Is.EqualTo("native_input_cleanup_incomplete"));
        Assert.That(((IReadOnlyDictionary<string, object?>)outcome.Result!)["cleanupComplete"], Is.False);
        Assert.That(operation.IsQuarantined, Is.True);
        int attempts = input.Events.Count;
        operation.BeforeDispatch();
        operation.AfterDispatch(true);
        Assert.That(input.Events.Count, Is.EqualTo(attempts));
        Assert.That(operation.Advance(true), Is.SameAs(outcome));
    }

    private sealed class RecordingInput : IGatewayNativeDragBackend
    {
        public List<string> Events { get; } = new();
        public string? Rejection { get; set; }
        public string? ThrowOn { get; set; }
        public string? Validate(bool ownsButton) { Events.Add("validate:" + ownsButton); return Rejection; }
        public void Press() => Record("press");
        public void MoveHeld() => Record("move-held");
        public void Release() => Record("release");
        public void RestorePointer() => Record("restore");
        private void Record(string action)
        {
            Events.Add(action);
            if (ThrowOn == action) throw new InvalidOperationException("native " + action + " failed");
        }
    }
}
