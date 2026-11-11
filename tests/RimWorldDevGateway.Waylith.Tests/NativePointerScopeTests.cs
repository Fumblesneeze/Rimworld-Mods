using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class NativePointerScopeTests
{
    [Test]
    public void NonterminalScopeShutdownSnapshotCannotPublishAFalseTerminalEnd()
    {
        var now = DateTimeOffset.UtcNow;
        var scope = new GatewayNativePointerScope("owned", new(10, 20), now.AddMinutes(1));
        var pending = scope.SnapshotForShutdown();
        Assert.That(pending.Error!.Code, Is.EqualTo("native_input_shutdown_incomplete"));
        Assert.That(((IReadOnlyDictionary<string, object?>)pending.Result!)["pointerScopeState"], Is.EqualTo("pending"));
        Assert.That(scope.Result, Is.Null, "Shutdown reporting must not replace the eventual end result.");
        var terminal = scope.Close(() => scope.Original, _ => Assert.Fail("Already restored pointer must not move."));
        Assert.That(terminal!.Error, Is.Null);
        Assert.That(scope.SnapshotForShutdown(), Is.SameAs(terminal));
    }

    [Test]
    public void ShutdownInsideExpiryRestorationRetainsOwnershipWithoutAnotherPendingOperation()
    {
        var now = DateTimeOffset.UtcNow;
        var scope = new GatewayNativePointerScope("expired", new(10, 20), now.AddSeconds(1));
        var pointer = new GatewayNativePointerPosition(30, 40);
        scope.Retain(now, pointer, pointer);
        now = now.AddSeconds(2);
        Assert.That(scope.IsExpired(now), Is.True);
        scope.RequestEnd(); // Expiry tick; there is no gesture, registry run, or explicit end operation.
        var shutdown = new GatewayShutdownLifecycle();
        GatewayShutdownAttempt? duringRestore = null;
        GatewayDeferredAutomationResult? observedShutdown = null, nestedEnd = null;
        int moves = 0;
        Action<GatewayNativePointerPosition>? move = null;
        move = point =>
        {
            moves++;
            nestedEnd = scope.Close(() => pointer, move!);
            observedShutdown = scope.SnapshotForShutdown();
            duringRestore = shutdown.TryShutdown(() =>
                GatewayNativeWindowShutdown.Cleanup(null, () => observedShutdown), true, now);
            pointer = point; // Native NotifyEvent/quit callbacks precede the actual position write.
        };
        var completed = scope.Close(() => pointer, move);
        Assert.That(duringRestore!.RetainsOwnership, Is.True,
            "A nonterminal owned scope must not become shutdown's null/no-work receipt.");
        Assert.That(duringRestore.IsTerminal, Is.False);
        Assert.That(observedShutdown!.Error!.Code, Is.EqualTo("native_input_shutdown_incomplete"));
        var pending = (IReadOnlyDictionary<string, object?>)observedShutdown.Result!;
        Assert.That(pending["cleanupComplete"], Is.False);
        Assert.That(pending["pointerRestored"], Is.False);
        Assert.That(pending["scopeHandle"], Is.EqualTo("expired"));
        Assert.That(nestedEnd, Is.Null, "Nested end/poll must remain pending, not terminally failed.");
        Assert.That(moves, Is.EqualTo(1));
        Assert.That(completed!.Error, Is.Null);
        Assert.That(scope.SnapshotForShutdown(), Is.SameAs(completed));
        var afterRestore = shutdown.TryShutdown(() =>
            GatewayNativeWindowShutdown.Cleanup(null, scope.SnapshotForShutdown), true, now.AddSeconds(2));
        Assert.That(afterRestore.IsTerminal, Is.True);
        Assert.That(afterRestore.RecoveredAfterRetainedFailure, Is.True);
    }

    [Test]
    public void SynchronousRestoreCallbackSeesPendingAndCannotRestoreTwice()
    {
        var now = DateTimeOffset.UtcNow;
        var scope = new GatewayNativePointerScope("owned", new(10, 20), now.AddMinutes(1));
        var pointer = new GatewayNativePointerPosition(30, 40);
        scope.Retain(now, pointer, pointer);
        int moves = 0;
        GatewayDeferredAutomationResult? nested = null, observedDuringCallback = null;
        Action<GatewayNativePointerPosition>? move = null;
        move = point =>
        {
            moves++;
            if (moves == 1) // Bound the RED: one synchronous NotifyEvent-style reentry is sufficient.
            {
                nested = scope.Close(() => pointer, move!);
                observedDuringCallback = scope.Result;
            }
            pointer = point; // The engine writes its new position only after callbacks return.
        };
        var result = scope.Close(() => pointer, move);
        Assert.That(moves, Is.EqualTo(1));
        Assert.That(nested, Is.Null, "The recursive cleanup call must truthfully remain pending.");
        Assert.That(observedDuringCallback, Is.Null);
        Assert.That(result!.Error, Is.Null);
        Assert.That(scope.Result, Is.SameAs(result));
        Assert.That(pointer, Is.EqualTo(scope.Original));
    }

    [Test]
    public void ForeignMoveAfterAdmissionIsRejectedAtDeferredFirstPressWithoutMutation()
    {
        var now = DateTimeOffset.UtcNow;
        var scope = new GatewayNativePointerScope("owned", new(10, 20), now.AddMinutes(1));
        var input = new DeferredScopeInput(scope, now);
        Assert.That(scope.Reject(now, input.Pointer), Is.Null, "Initial admission was valid.");
        var click = new GatewayNativeClickOperation(input);
        input.Pointer = new(99, 99); // Another source moves after Begin, before native dispatch.
        click.BeforeDispatch();
        click.AfterDispatch(false);
        var result = click.Advance(false)!;
        Assert.That(result.Error!.Code, Is.EqualTo("native_pointer_ownership_lost"));
        Assert.That(input.Presses, Is.Zero);
        Assert.That(input.Restores, Is.Zero);
        Assert.That(input.Pointer, Is.EqualTo(new GatewayNativePointerPosition(99, 99)));
    }

    private sealed class DeferredScopeInput : IGatewayNativeClickBackend
    {
        private readonly GatewayNativePointerScope scope;
        private readonly DateTimeOffset now;
        public GatewayNativePointerPosition Pointer = new(10, 20);
        public int Presses, Restores;
        public DeferredScopeInput(GatewayNativePointerScope scope, DateTimeOffset now)
        { this.scope = scope; this.now = now; }
        public string? Validate(bool ownsButton) => scope.RejectBeforePress(now, () => Pointer);
        public void Press() { Presses++; Pointer = new(30, 40); }
        public void Release() { }
        public void RestorePointer() { Restores++; Pointer = scope.Original; }
    }

    [Test]
    public void WorkflowCleanupNeverOverwritesAChangedPointer()
    {
        var now = DateTimeOffset.UtcNow;
        var scope = new GatewayNativePointerScope("owned", new(10, 20), now.AddMinutes(1));
        scope.Retain(now, new(30, 40), new(30, 40));
        int moves = 0;
        var result = scope.Close(() => new(99, 99), _ => moves++)!;
        Assert.That(result.Error!.Code, Is.EqualTo("native_pointer_ownership_lost"));
        Assert.That(moves, Is.Zero);
        Assert.That(((IReadOnlyDictionary<string, object?>)result.Result!)["pointerRestored"], Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ExpiredOrEndingScopeRejectsAnotherTransferButStillRestores(bool ending)
    {
        var now = DateTimeOffset.UtcNow;
        var scope = new GatewayNativePointerScope("owned", new(10, 20), now.AddSeconds(1));
        scope.Retain(now, new(30, 40), new(30, 40));
        if (ending) scope.RequestEnd(); else now = now.AddSeconds(1);
        Assert.Throws<GatewayAutomationException>(() => scope.Retain(now, new(50, 60), new(50, 60)));
        var pointer = new GatewayNativePointerPosition(30, 40);
        Assert.That(scope.Close(() => pointer, point => pointer = point)!.Error, Is.Null);
        Assert.That(pointer, Is.EqualTo(new GatewayNativePointerPosition(10, 20)));
    }

    [Test]
    public void CallbackMovementDuringRestorationCannotClaimCleanupComplete()
    {
        var now = DateTimeOffset.UtcNow;
        var scope = new GatewayNativePointerScope("owned", new(10, 20), now.AddMinutes(1));
        var pointer = new GatewayNativePointerPosition(30, 40);
        scope.Retain(now, pointer, pointer);
        var result = scope.Close(() => pointer, _ => pointer = new(99, 99))!;
        Assert.That(result.Error!.Code, Is.EqualTo("native_pointer_restore_unconfirmed"));
        Assert.That(((IReadOnlyDictionary<string, object?>)result.Result!)["cleanupComplete"], Is.False);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ClickTransfersOnlyAfterSuccessfulReleaseAndKeepsFailureCleanup(bool successful)
    {
        var input = new ScopedInput();
        var click = new GatewayNativeClickOperation(input);
        click.BeforeDispatch();
        click.AfterDispatch(true);
        click.BeforeDispatch(!successful);
        click.AfterDispatch(successful);
        Assert.That(input.Completed, Is.EqualTo(successful));
        Assert.That(input.LegacyRestored, Is.False);
        var receipt = (IReadOnlyDictionary<string, object?>)click.Advance(false)!.Result!;
        Assert.That(receipt["pointerRestored"], Is.EqualTo(!successful));
        Assert.That(receipt["cleanupComplete"], Is.True);
    }

    private sealed class ScopedInput : IGatewayNativeClickBackend, IGatewayNativeScopedClickBackend
    {
        public bool? Completed;
        public bool LegacyRestored;
        public string? Validate(bool ownsButton) => null;
        public void Press() { }
        public void Release() { }
        public void RestorePointer() => LegacyRestored = true;
        public void CompletePointer(bool successfulGesture) => Completed = successfulGesture;
        public IReadOnlyDictionary<string, object?> PointerReceipt => new Dictionary<string, object?>
            { ["pointerRestored"] = Completed == false };
    }

    [Test]
    public void SuccessfulGestureTransfersPointerUntilExplicitWorkflowCleanup()
    {
        var now = DateTimeOffset.UtcNow;
        var original = new GatewayNativePointerPosition(51, 340);
        var target = new GatewayNativePointerPosition(577, 648);
        var scope = new GatewayNativePointerScope("opaque", original, now.AddMinutes(1));
        var pointer = target;
        scope.Retain(now, pointer, target);
        Assert.That(scope.Expected, Is.EqualTo(target), "A successful gesture keeps the pointer at the menu.");
        int moves = 0;
        var receipt = scope.Close(() => pointer, point => { moves++; pointer = point; })!;
        Assert.That(receipt.Error, Is.Null);
        Assert.That(pointer, Is.EqualTo(original));
        Assert.That(moves, Is.EqualTo(1));
        Assert.That(scope.Close(() => pointer, _ => Assert.Fail("Repeated cleanup must not move input.")), Is.SameAs(receipt));
    }
}
