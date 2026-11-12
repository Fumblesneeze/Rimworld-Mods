using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class NativeWindowInputContractTests
{
    [TestCase(9, 21, "point_outside_window")]
    [TestCase(109.999999999, 21, "point_outside_window")]
    [TestCase(50, 119.999999999, "point_outside_window")]
    [TestCase(50, 21, null)]
    [TestCase(50, 22, null)]
    public void DragEndpointsMustRemainInsideTheWindowAfterNativeFloatProjection(double x, double y, string? error)
    {
        var geometry = new GatewayNativeWindowGeometry(10, 20, 100, 100, 1, 1600, 900, 1);
        var point = GatewayNativeWindowInputContract.ResolveDragPoint(geometry, x, y);
        Assert.That(point.Error, Is.EqualTo(error));
        if (error == null) Assert.That((point.InputX, point.InputY), Is.EqualTo((x, 899 - y)));
    }

    [TestCase(false, 42, 0, "native_slider_busy")]
    [TestCase(true, 43, 42, "native_slider_ownership_lost")]
    [TestCase(true, 0, 42, "native_slider_ownership_lost")]
    [TestCase(true, 42, 0, "native_slider_ownership_lost")]
    [TestCase(false, 0, 0, null)]
    [TestCase(true, 42, 42, null)]
    public void DragCannotBorrowOrContinueThroughAForeignSliderLatch(bool held, int current, int owned, string? error)
        => Assert.That(GatewayNativeWindowInputContract.RejectSliderInput(held, new(current, 0, 0), new(owned, 0, 0)), Is.EqualTo(error));

    [TestCase(42, 0, false, true, true)]
    [TestCase(42, 42, true, false, true)]
    [TestCase(42, 43, true, false, false)]
    [TestCase(42, 42, false, false, false)]
    [TestCase(42, 42, true, true, false)]
    [TestCase(0, 42, true, false, false)]
    public void SliderLatchCleanupPreservesForeignOrUnconfirmedState(int owned, int current, bool window, bool foreign, bool allowed)
        => Assert.That(GatewayNativeWindowInputContract.CanRestoreSliderLatch(new(owned, 0, 0), new(current, 0, 0), window, foreign), Is.EqualTo(allowed));

    [TestCase(0, 81, (byte)1)]
    [TestCase(42, 81, (byte)2)]
    [TestCase(0, 0, (byte)1)]
    public void NativeReleaseCannotClearAnUnownedRangeSliderState(int slider, int range, byte end)
    {
        var current = new GatewayNativeSliderState(slider, range, end);
        Assert.That(GatewayNativeWindowInputContract.RejectSliderInput(false, current, default), Is.EqualTo("native_slider_busy"));
        Assert.That(GatewayNativeWindowInputContract.RejectSliderInput(true, current, new(slider, 0, 0)), Is.EqualTo("native_slider_ownership_lost"));
        Assert.That(GatewayNativeWindowInputContract.CanRestoreSliderLatch(new(slider, 0, 0), current, true, false), Is.False);
    }

    [Test]
    public void MousePassReceiptDistinguishesMissingRejectedAndDeliveredPasses()
    {
        var absent = new GatewayNativeMousePassGate();
        Assert.That(absent.Seen, Is.False);
        var rejected = new GatewayNativeMousePassGate();
        Assert.That(rejected.BeforePass(true, "stale-window", false), Is.False);
        rejected.AfterPass(true);
        Assert.Multiple(() =>
        {
            Assert.That(rejected.Seen, Is.True);
            Assert.That(rejected.Admitted, Is.False);
            Assert.That(rejected.OriginalObserved, Is.True);
            Assert.That(rejected.Dispatched, Is.False);
            Assert.That(rejected.Rejection, Is.EqualTo("stale-window"));
        });
        var delivered = new GatewayNativeMousePassGate();
        Assert.That(delivered.BeforePass(true, null, false), Is.True);
        delivered.AfterPass(true);
        Assert.That(delivered.Seen && delivered.Admitted && delivered.OriginalObserved && delivered.Dispatched, Is.True);
    }
    [TestCase(false)]
    [TestCase(true)]
    public void PublicationLossRejectsActivePassBeforeAbsentOrDisownedProvenance(bool hasProvenance)
    {
        var publication = new object();
        var guard = typeof(NativeWindowInputContractTests).GetMethod(nameof(ObserverMarker))!;
        var rebuilt = new GatewayNativeResetAdmission(publication, true);
        bool proven = rebuilt.TryAccept(publication, new[] { guard }, guard);
        GatewayNativeMouseProvenance? changed = hasProvenance
            ? new GatewayNativeMouseProvenance(false, new object(), true, 1, 1, 1, 20, 30) : null;
        var input = new ClickInput();
        var operation = new GatewayNativeClickOperation(input);
        operation.BeforeDispatch();
        var gate = new GatewayNativeMousePassGate();
        bool? decision = GatewayNativeMousePassGate.BeforeProvenance(true, proven, true, changed);
        Assert.That(decision, Is.False, "A rebuild cannot disguise pending owned input as an unrelated pass.");
        gate.BeforePass(false, "native_mouse_guard_unavailable", true, false);
        operation.AfterDispatch(gate.Dispatched);
        GatewayDeferredAutomationResult result = operation.Advance(false)!;
        Assert.That(result.Error!.Code, Is.EqualTo("native_input_not_dispatched"));
        Assert.That(((IReadOnlyDictionary<string, object?>)result.Result!)["cleanupComplete"], Is.True);
        Assert.That(input.Released, Is.EqualTo(1));
        Assert.That(input.Restored, Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ProviderFailureRejectsActivePassEvenWithoutOwnedProvenance(bool hasProvenance)
    {
        GatewayNativeMouseProvenance? changed = hasProvenance
            ? new GatewayNativeMouseProvenance(false, new object(), true, 1, 1, 1, 20, 30) : null;
        Assert.That(GatewayNativeMousePassGate.BeforeProvenance(true, true, false, changed), Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ProvenUnownedAndUnrelatedPassesRetainNativeBehavior(bool hasProvenance)
    {
        GatewayNativeMouseProvenance? unowned = hasProvenance
            ? new GatewayNativeMouseProvenance(false, new object(), true, 1, 1, 1, 20, 30) : null;
        Assert.That(GatewayNativeMousePassGate.BeforeProvenance(false, false, false, unowned), Is.True);
        Assert.That(GatewayNativeMousePassGate.BeforeProvenance(true, true, true, unowned), Is.True);
        var owned = new GatewayNativeMouseProvenance(true, new object(), false, 0, 0, 0, 20, 30);
        Assert.That(GatewayNativeMousePassGate.BeforeProvenance(true, true, true, owned), Is.Null,
            "An owned pass still requires normal target and cancellation validation.");
    }

    [Test]
    public void EventBodyRejectsInsertedMutationAndKeepsUnchangedDeliveryStream()
    {
        var deliver = typeof(NativeWindowInputContractTests).GetMethod(nameof(ObserverMarker))!;
        var mutate = typeof(NativeWindowInputContractTests).GetMethod(nameof(ForeignMarker))!;
        GatewayNativeInstructionShape Call(object method) => new(System.Reflection.Emit.OpCodes.Call.Value,
            method, Array.Empty<int>(), Array.Empty<(int, Type?)>());
        var original = new[] { Call(deliver) };
        Assert.That(GatewayNativeInstructionShape.SameBody(original, new[] { Call(mutate), Call(deliver) }), Is.False,
            "An earlier transpiler can insert target mutation after the final prefix has checked it.");
        Assert.That(GatewayNativeInstructionShape.SameBody(original, new[] { Call(deliver) }), Is.True,
            "A compatible no-op stream transformation need not be rejected by owner identity.");
        Assert.That(GatewayNativeInstructionShape.SameBody(original, new[] { Call(mutate) }), Is.False);
        Assert.That(GatewayNativeInstructionShape.SameBody(original, new[] { new GatewayNativeInstructionShape(
            System.Reflection.Emit.OpCodes.Call.Value, deliver, new[] { 1 }, Array.Empty<(int, Type?)>()) }), Is.False);
        Assert.That(GatewayNativeInstructionShape.SameBody(original, new[] { new GatewayNativeInstructionShape(
            System.Reflection.Emit.OpCodes.Call.Value, deliver, Array.Empty<int>(), new[] { (1, (Type?)typeof(Exception)) }) }), Is.False);
    }

    [TestCase(true, 0)]
    [TestCase(false, 1)]
    public void MiddlePrefixCannotDisownCapturedMousePassByChangingDrawingOrButton(bool drawing, int button)
    {
        var input = new object();
        var scope = new GatewayNativeMouseProvenance(true, input, false, 0, 1, 1, 20, 30);
        var gate = new GatewayNativeMousePassGate();
        bool matches = scope.Matches(input, drawing, button, 1, 1, 20, 30);
        Assert.That(scope.Owned, Is.True);
        Assert.That(gate.BeforePass(true, matches ? null : "native_mouse_pass_changed", false), Is.False);
    }

    [TestCase(true, false, true, true)]
    [TestCase(false, false, true, false)]
    [TestCase(true, true, true, false)]
    [TestCase(true, false, false, false)]
    public void CleanupPreservesOnlyIdentifiedReplacementOwnership(bool rejectedRelease, bool targetPresent,
        bool otherOwner, bool expected)
        => Assert.That(GatewayNativeWindowInputContract.PreserveReplacementHotControl(rejectedRelease,
            targetPresent, otherOwner), Is.EqualTo(expected));

    [Test]
    public void MouseGateRejectsAPublishedPrefixAfterItButAllowsCompatibleEarlierPrefixes()
    {
        var guard = typeof(NativeWindowInputContractTests).GetMethod(nameof(ObserverMarker))!;
        var foreign = typeof(NativeWindowInputContractTests).GetMethod(nameof(ForeignMarker))!;
        var publication = new object();
        var authority = new GatewayNativeResetAdmission(null, true);
        bool terminal = authority.TryAccept(publication, new[] { guard, foreign }, guard);
        Assert.That(new GatewayNativeMousePassGate().BeforePass(true, null, false, terminal), Is.False,
            "A later priority tie or HarmonyAfter prefix can invalidate the target after our check.");
        terminal = authority.TryAccept(publication, new[] { foreign, guard }, guard);
        Assert.That(new GatewayNativeMousePassGate().BeforePass(true, null, false, terminal), Is.True);
    }

    [Test]
    public void RejectedReleasePreservesReplacementControlWithoutChangingOtherNativeTails()
    {
        var rejected = new GatewayNativeMousePassGate();
        Assert.That(rejected.BeforePass(true, "stale_window_instance", false), Is.False);
        rejected.AfterPass(false);
        int replacementControl = 72;
        rejected.ApplyReleaseTail(true, 0, value => replacementControl = value);
        Assert.That(replacementControl, Is.EqualTo(72), "Rejected inner delivery must also guard the caller's release tail.");
        rejected.ApplyReleaseTail(false, 0, value => replacementControl = value);
        Assert.That(replacementControl, Is.Zero, "Unowned native releases retain their behavior.");
        var delivered = new GatewayNativeMousePassGate();
        delivered.BeforePass(true, null, false);
        delivered.AfterPass(true);
        replacementControl = 72;
        delivered.ApplyReleaseTail(true, 0, value => replacementControl = value);
        Assert.That(replacementControl, Is.Zero, "Delivered releases retain the native reset.");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void LayoutReplacementRejectsOwnedMouseDeliveryAndCleansTheQueuedGesture(bool afterPress)
    {
        var input = new ClickInput();
        var operation = new GatewayNativeClickOperation(input);
        if (afterPress)
        {
            operation.BeforeDispatch();
            operation.AfterDispatch(true);
        }
        operation.BeforeDispatch(); // The native outer prefix already queued this edge.
        var gate = new GatewayNativeMousePassGate();
        bool deliver = gate.BeforePass(true, "stale_window_instance", false); // Layout replaced it.
        gate.AfterPass(deliver);
        operation.AfterDispatch(gate.Dispatched); // The outer dispatcher itself still returned normally.
        GatewayDeferredAutomationResult? result = operation.Advance(false);
        Assert.That(deliver, Is.False);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Error?.Code, Is.EqualTo("native_input_not_dispatched"));
        var receipt = (IReadOnlyDictionary<string, object?>)result.Result!;
        Assert.That(receipt["pressDispatched"], Is.EqualTo(afterPress));
        Assert.That(receipt["releaseDispatched"], Is.False);
        Assert.That(receipt["cleanupComplete"], Is.True);
        Assert.That(input.Released, Is.EqualTo(1));
        Assert.That(input.Restored, Is.EqualTo(1));
    }

    [TestCase(true, false, true)]
    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    public void MousePassReceiptRequiresAdmissionAndOriginalExecution(bool originalWillRun, bool cancelled, bool expected)
    {
        var gate = new GatewayNativeMousePassGate();
        Assert.That(gate.Dispatched, Is.False, "Outer dispatch alone is not mouse-event evidence.");
        bool allowed = gate.BeforePass(originalWillRun, null, cancelled);
        Assert.That(allowed, Is.EqualTo(expected));
        gate.AfterPass(allowed);
        Assert.That(gate.Dispatched, Is.EqualTo(expected));
        Assert.That(gate.BeforePass(true, null, false), Is.False, "One native edge cannot be replayed.");
        var skippedLater = new GatewayNativeMousePassGate();
        Assert.That(skippedLater.BeforePass(true, null, false), Is.True);
        skippedLater.AfterPass(false);
        Assert.That(skippedLater.Dispatched, Is.False, "A later prefix can skip the original.");
    }

    [Test]
    public void EventDispatchRequiresTheExactPrivateInstanceVoidSignature()
    {
        Assert.That(GatewayNativeWindowInputContract.ResolveEventDispatch(typeof(EventDispatcher), typeof(Point)), Is.Not.Null);
        foreach (Type wrong in new[] { typeof(PublicEventDispatcher), typeof(StaticEventDispatcher),
            typeof(WrongResultEventDispatcher), typeof(InheritedEventDispatcher), typeof(GenericEventDispatcher) })
            Assert.That(GatewayNativeWindowInputContract.ResolveEventDispatch(wrong, typeof(Point)), Is.Null);
    }

    [TestCase(0, false, true)]
    [TestCase(1, false, true)]
    [TestCase(-1, false, false)]
    [TestCase(-1, true, false)]
    [TestCase(0, true, false)]
    [TestCase(1, true, false)]
    public void WindowIdentitySupportsTheFirstOrdinaryWindowButRejectsImmediateWindows(
        int nativeId, bool immediate, bool supported)
        => Assert.That(GatewayNativeWindowInputContract.SupportsWindowIdentity(nativeId, immediate),
            Is.EqualTo(supported), "WindowStack starts ordinary IDs at zero; immediate windows have no durable lease.");

    [Test]
    public void ResetReceiptRequiresFreshPublishedMetadataAndTerminalObserver()
    {
        var prior = new object();
        var published = new object();
        var observer = typeof(NativeWindowInputContractTests).GetMethod(nameof(ObserverMarker))!;
        var foreign = typeof(NativeWindowInputContractTests).GetMethod(nameof(ForeignMarker))!;
        var receipt = new GatewayNativeResetAdmission(prior, streamSupported: true);
        Assert.That(receipt.TryAccept(prior, new[] { foreign, observer }, observer), Is.False,
            "Metadata from before the in-flight rebuild cannot establish its final order.");
        Assert.That(receipt.TryAccept(published, new[] { observer, foreign }, observer), Is.False,
            "A HarmonyAfter constraint or later priority tie can put another transpiler after the observer.");
        Assert.That(receipt.IsAccepted(published), Is.False);
        Assert.That(receipt.TryAccept(published, new[] { foreign, observer }, observer), Is.True);
        Assert.That(receipt.IsAccepted(published), Is.True);
        Assert.That(receipt.TryAccept(new object(), new[] { foreign, observer }, observer), Is.False,
            "A later metadata publication needs its own incoming-stream observation.");
        var rebuilding = new GatewayNativeResetAdmission(published, streamSupported: true);
        Assert.That(rebuilding.IsAccepted(published), Is.False,
            "Starting another observation invalidates the previous admission synchronously.");
        Assert.That(new GatewayNativeResetAdmission(prior, streamSupported: false)
            .TryAccept(published, new[] { observer }, observer), Is.False);
    }

    public static void ObserverMarker() { }
    public static void ForeignMarker() { }

    [TestCase(0, 0, 0, true)]
    [TestCase(1, 0, 0, false)]
    [TestCase(0, 1, 0, false)]
    [TestCase(0, 0, 1, false)]
    public void ResetAdmissionRejectsUnprovenInnerPatchesAfterTranspilers(
        int innerPrefixes, int innerPostfixes, int innerFinalizers, bool accepted)
    {
        var publication = new object();
        var observer = typeof(NativeWindowInputContractTests).GetMethod(nameof(ObserverMarker))!;
        var compatibleForeign = typeof(NativeWindowInputContractTests).GetMethod(nameof(ForeignMarker))!;
        var receipt = new GatewayNativeResetAdmission(null, streamSupported: true);
        Assert.That(receipt.TryAccept(publication, new[] { compatibleForeign, observer }, observer,
            innerPrefixes, innerPostfixes, innerFinalizers), Is.EqualTo(accepted),
            "Inner hooks can rewrite the reset call after the verified transpiler stream.");
        Assert.That(receipt.IsAccepted(publication), Is.EqualTo(accepted));
    }

    [Test]
    public void ResetOrderingRejectsFactoriesWithoutInvokingThem()
    {
        factoryCalls = 0;
        var observer = typeof(NativeWindowInputContractTests).GetMethod(nameof(ObserverMarker))!;
        var factory = typeof(NativeWindowInputContractTests).GetMethod(nameof(FactoryMarker))!;
        var receipt = new GatewayNativeResetAdmission(null, streamSupported: true);
        Assert.That(receipt.TryAccept(new object(), new[] { factory, observer }, observer), Is.False);
        Assert.That(factoryCalls, Is.Zero);
    }

    private static int factoryCalls;
    public static System.Reflection.MethodInfo FactoryMarker(System.Reflection.MethodBase original)
    {
        factoryCalls++;
        return typeof(NativeWindowInputContractTests).GetMethod(nameof(ForeignMarker))!;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ProviderFailureBeforeOrAfterPressRemainsPendingUntilCleanupThenReturnsError(bool afterPress)
    {
        var input = new ClickInput();
        var click = new GatewayNativeClickOperation(input);
        string? providerFailure = null;
        var operation = new GatewayNativeInputResultOperation(click, () => providerFailure);
        if (afterPress)
        {
            click.BeforeDispatch();
            click.AfterDispatch(true);
        }
        providerFailure = "native_input_reset_unavailable";
        Assert.That(operation.Advance(false), Is.Null, "Provider failure cannot skip pending cleanup.");
        click.BeforeDispatch(true);
        click.AfterDispatch(false);

        GatewayDeferredAutomationResult raw = click.Advance(false)!;
        GatewayDeferredAutomationResult result = operation.Advance(false)!;
        Assert.That(result.Error?.Code, Is.EqualTo(providerFailure));
        Assert.That(result.Result, Is.SameAs(raw.Result), "Keep the exact partial cleanup receipt.");
        var receipt = (IReadOnlyDictionary<string, object?>)result.Result!;
        Assert.That(receipt["pressDispatched"], Is.EqualTo(afterPress));
        Assert.That(receipt["releaseDispatched"], Is.False);
        Assert.That(receipt["cleanupComplete"], Is.True);
        Assert.That(input.Released, Is.EqualTo(afterPress ? 1 : 0));
        Assert.That(input.Restored, Is.EqualTo(afterPress ? 1 : 0));
    }

    [Test]
    public void CompletedGestureResultCannotBeRewrittenByLaterProviderShutdown()
    {
        var click = new GatewayNativeClickOperation(new ClickInput());
        string? providerFailure = null;
        var operation = new GatewayNativeInputResultOperation(click, () => providerFailure);
        click.BeforeDispatch();
        click.AfterDispatch(true);
        click.BeforeDispatch();
        click.AfterDispatch(true);
        GatewayDeferredAutomationResult completed = operation.Advance(false)!;
        Assert.That(completed.Error, Is.Null);
        providerFailure = "native_input_shutdown";
        Assert.That(operation.Advance(false), Is.SameAs(completed));
    }

    [Test]
    public void CleanupFailureIsNotMaskedByProviderFailure()
    {
        var input = new ClickInput { ThrowRestoration = true };
        var click = new GatewayNativeClickOperation(input);
        var operation = new GatewayNativeInputResultOperation(click, () => "native_input_reset_unavailable");
        click.BeforeDispatch();
        click.AfterDispatch(true);
        click.BeforeDispatch(true);
        click.AfterDispatch(false);
        GatewayDeferredAutomationResult raw = click.Advance(false)!;

        Assert.That(operation.Advance(false), Is.SameAs(raw));
        Assert.That(raw.Error!.Code, Is.EqualTo("native_input_cleanup_incomplete"));
        Assert.That(((IReadOnlyDictionary<string, object?>)raw.Result!)["cleanupComplete"], Is.False);
    }

    [Test]
    public void OnlyExactCallableDispatchAndHeightShapesAreAccepted()
    {
        Assert.That(GatewayNativeWindowInputContract.ResolveDispatch(typeof(Dispatch), typeof(Host)), Is.Not.Null);
        Assert.That(GatewayNativeWindowInputContract.ResolveDispatch(typeof(WrongReturnDispatch), typeof(Host)), Is.Null);
        Assert.That(GatewayNativeWindowInputContract.ResolveDispatch(typeof(StaticDispatch), typeof(Host)), Is.Null);
        Assert.That(GatewayNativeWindowInputContract.ResolveDispatch(typeof(GenericDispatch), typeof(Host)), Is.Null);
        Assert.That(GatewayNativeWindowInputContract.ResolveDispatch(typeof(PrivateDispatch), typeof(Host)), Is.Null);
        Assert.That(GatewayNativeWindowInputContract.ResolveDispatch(typeof(InheritedDispatch), typeof(Host)), Is.Null);
        Assert.That(GatewayNativeWindowInputContract.ResolveDispatch(typeof(WrongHostDispatch), typeof(WrongHeightHost)), Is.Null);
    }

    [Test]
    public void ChangedQueueSignatureCannotBeUsedForInjection()
    {
        Assert.That(GatewayNativeWindowInputContract.HasQueues(typeof(Queues), typeof(Point)), Is.True);
        Assert.That(GatewayNativeWindowInputContract.HasQueues(typeof(WrongQueues), typeof(Point)), Is.False);
        Assert.That(GatewayNativeWindowInputContract.HasQueues(typeof(Queues), typeof(Host)), Is.False);
    }

    [Test]
    public void InputGenerationRequiresTheExactStaticVoidFrameReset()
    {
        Assert.That(GatewayNativeWindowInputContract.ResolveFrameReset(typeof(Queues)), Is.Not.Null);
        Assert.That(GatewayNativeWindowInputContract.ResolveFrameReset(typeof(WrongQueues)), Is.Null);
        Assert.That(GatewayNativeWindowInputContract.ResolveFrameReset(typeof(InstanceReset)), Is.Null);
    }

    [Test]
    public void ResetBoundaryRequiresDeclaredPrivateStaticCallbackForExactApplication()
    {
        Assert.That(GatewayNativeWindowInputContract.ResolveFrameResetBoundary(typeof(Bootstrap), typeof(App)), Is.Not.Null);
        Assert.That(GatewayNativeWindowInputContract.ResolveFrameResetBoundary(typeof(Bootstrap), typeof(Host)), Is.Null);
        Assert.That(GatewayNativeWindowInputContract.ResolveFrameResetBoundary(typeof(PublicBoundary), typeof(App)), Is.Null);
        Assert.That(GatewayNativeWindowInputContract.ResolveFrameResetBoundary(typeof(InstanceBoundary), typeof(App)), Is.Null);
        Assert.That(GatewayNativeWindowInputContract.ResolveFrameResetBoundary(typeof(GenericBoundary), typeof(App)), Is.Null);
    }

    [Test]
    public void ResetMustDominateEveryNormalReturnAndTheOptionalPostResetPhase()
    {
        GatewayNativeResetNode[] Valid() => new[]
        {
            new GatewayNativeResetNode(new[] { 1 }),
            new GatewayNativeResetNode(new[] { 2 }, resets: true),
            new GatewayNativeResetNode(new[] { 3, 4 }),
            new GatewayNativeResetNode(new[] { 4 }, postReset: true),
            new GatewayNativeResetNode(Array.Empty<int>(), returns: true)
        };
        Assert.That(GatewayNativeWindowInputContract.HasRequiredResetFlow(Valid()), Is.True);
        var bypass = Valid();
        bypass[0] = new GatewayNativeResetNode(new[] { 1, 4 });
        Assert.That(GatewayNativeWindowInputContract.HasRequiredResetFlow(bypass), Is.False);
        var earlyPostReset = Valid();
        earlyPostReset[0] = new GatewayNativeResetNode(new[] { 1, 3 });
        Assert.That(GatewayNativeWindowInputContract.HasRequiredResetFlow(earlyPostReset), Is.False);
        var repeated = Valid();
        repeated[2] = new GatewayNativeResetNode(new[] { 1, 3, 4 });
        Assert.That(GatewayNativeWindowInputContract.HasRequiredResetFlow(repeated), Is.False);
        var missing = Valid();
        missing[1] = new GatewayNativeResetNode(new[] { 2 });
        Assert.That(GatewayNativeWindowInputContract.HasRequiredResetFlow(missing), Is.False);
        var duplicate = Valid();
        duplicate[0] = new GatewayNativeResetNode(new[] { 1 }, resets: true);
        Assert.That(GatewayNativeWindowInputContract.HasRequiredResetFlow(duplicate), Is.False);
        var unrelated = Valid();
        unrelated[0] = new GatewayNativeResetNode(new[] { 1, 5 });
        Assert.That(GatewayNativeWindowInputContract.HasRequiredResetFlow(unrelated.Concat(new[]
            { new GatewayNativeResetNode(new[] { 1 }) }).ToArray()), Is.True,
            "An unrelated conditional branch is supported when both paths still perform the reset.");
    }

    [TestCase(false, true, true, false, false, true, TestName = "QueuedReleaseCannotReachAReplacedDialog")]
    [TestCase(false, true, false, true, false, true, TestName = "CancellationDuringQueuedReleaseSuppressesDelivery")]
    [TestCase(true, false, true, false, false, true, TestName = "RejectedHeldPressCannotBecomeDragOnAnotherDialog")]
    [TestCase(true, false, false, true, false, true, TestName = "CancelledHeldPressCannotBecomeDrag")]
    [TestCase(false, false, true, false, false, false, TestName = "RejectionBeforeOwnershipPreservesNativeInput")]
    [TestCase(false, true, false, false, false, false, TestName = "ValidQueuedReleaseCanReachNativeDispatcher")]
    [TestCase(false, false, false, false, true, true, TestName = "UncertainInjectionDoesNotReachNativeDispatcher")]
    public void DispatchGateContainsOnlyTheOwnedGesture(bool ownsButton, bool queuedEdge, bool rejected,
        bool stopRequested, bool injectionFailed, bool expected)
    {
        Assert.That(GatewayNativeWindowInputContract.SuppressDispatch(ownsButton, queuedEdge, rejected,
            stopRequested, injectionFailed), Is.EqualTo(expected));
    }

    [Test]
    public void SameFrameShutdownRecognizesTheInjectedPressButRejectsAReplacementEdge()
    {
        var receipt = new GatewayNativeInputEdgeReceipt();
        receipt.Record(17, pressed: true);

        Assert.That(receipt.HasForeignEdges(17, down: true, up: false, repeat: false), Is.False);
        Assert.That(receipt.HasForeignEdges(17, down: true, up: true, repeat: false), Is.True,
            "Another release/press cycle in the same frame is not our original press.");
        Assert.That(receipt.HasForeignEdges(18, down: true, up: false, repeat: false), Is.True,
            "A fresh press in a later generation cannot inherit ownership.");
        Assert.That(receipt.HasForeignEdges(17, down: true, up: false, repeat: true), Is.True);

        receipt.Record(17, pressed: false);
        Assert.That(receipt.HasForeignEdges(17, down: true, up: true, repeat: false), Is.False,
            "A mandatory owned release can coexist with the owned press edge in the same frame.");
        receipt.Record(18, pressed: false);
        Assert.That(receipt.HasForeignEdges(18, down: false, up: true, repeat: false), Is.False);
        Assert.That(receipt.HasForeignEdges(18, down: true, up: true, repeat: false), Is.True,
            "Recording a later release must not carry the old down-edge ownership forward.");
    }

    [Test]
    public void UncertainResetCannotReusePreviouslyOwnedEdges()
    {
        var receipt = new GatewayNativeInputEdgeReceipt();
        receipt.Record(17, pressed: true);
        receipt.Invalidate();
        Assert.That(receipt.HasForeignEdges(17, down: true, up: false, repeat: false), Is.True);
        Assert.That(receipt.HasForeignEdges(18, down: true, up: false, repeat: false), Is.True);
        Assert.That(receipt.HasForeignEdges(18, down: false, up: false, repeat: false), Is.False,
            "Invalidating an edge receipt does not invent an actual unrelated input edge.");
    }

    [TestCase(false, false, false, false, false, 0, 0, null)]
    [TestCase(false, true, false, false, false, 0, 0, "native_input_busy")]
    [TestCase(true, true, false, false, false, 12, 12, null)]
    [TestCase(true, false, false, false, false, 12, 12, "native_input_ownership_lost")]
    [TestCase(true, true, true, false, false, 12, 12, "native_input_busy")]
    [TestCase(false, false, false, true, false, 0, 0, "native_input_busy")]
    [TestCase(false, false, false, false, true, 0, 0, "native_input_busy")]
    [TestCase(false, false, false, false, false, 12, 0, "native_input_busy")]
    [TestCase(true, true, false, false, false, 99, 12, "native_input_busy")]
    public void AdmissionHonorsOwnedLeftButtonWithoutClearingOtherInput(bool ownsLeft, bool leftPressed,
        bool mouseActivity, bool keyboardActivity, bool scrollActivity, int hotControl, int ownedHotControl,
        string? expected)
    {
        Assert.That(GatewayNativeWindowInputContract.RejectInput(ownsLeft, leftPressed, mouseActivity,
            keyboardActivity, scrollActivity, hotControl, ownedHotControl), Is.EqualTo(expected));
    }

    private readonly struct Point { }
    private sealed class App { }
    private static class Bootstrap { private static void OnPrePollEvents(App app) { } }
    private static class PublicBoundary { public static void OnPrePollEvents(App app) { } }
    private sealed class InstanceBoundary { private void OnPrePollEvents(App app) { } }
    private static class GenericBoundary { private static void OnPrePollEvents<T>(App app) { } }
    private sealed class ClickInput : IGatewayNativeClickBackend
    {
        public int Released { get; private set; }
        public int Restored { get; private set; }
        public bool ThrowRestoration { get; set; }
        public string? Validate(bool ownsButton) => null;
        public void Press() { }
        public void Release() => Released++;
        public void RestorePointer()
        {
            Restored++;
            if (ThrowRestoration) throw new InvalidOperationException("Cleanup failed.");
        }
    }
    private static class Queues
    {
        public static void BeginFrame() { }
        public static void QueueMouseMove(Point position) { }
        public static void QueueMouseButton(int button, bool pressed, Point position) { }
    }
    private static class WrongQueues
    {
        public static bool BeginFrame() => true;
        public static void QueueMouseMove(Point position) { }
        public static bool QueueMouseButton(int button, bool pressed, Point position) => true;
    }
    private sealed class InstanceReset { public void BeginFrame() { } }

    private sealed class Host { public float DisplayHeight => 720; }
    private class EventDispatcher { private void Dispatch(Point point, bool drawing) { } }
    private sealed class PublicEventDispatcher { public void Dispatch(Point point, bool drawing) { } }
    private sealed class StaticEventDispatcher { private static void Dispatch(Point point, bool drawing) { } }
    private sealed class WrongResultEventDispatcher { private bool Dispatch(Point point, bool drawing) => true; }
    private sealed class InheritedEventDispatcher : EventDispatcher { }
    private sealed class GenericEventDispatcher { private void Dispatch<T>(Point point, bool drawing) { } }
    private sealed class WrongHeightHost { public double DisplayHeight => 720; }
    private class Dispatch { public void DispatchInput(Host host) { } }
    private sealed class InheritedDispatch : Dispatch { }
    private sealed class WrongReturnDispatch { public bool DispatchInput(Host host) => true; }
    private sealed class StaticDispatch { public static void DispatchInput(Host host) { } }
    private sealed class GenericDispatch { public void DispatchInput<T>(Host host) { } }
    private sealed class PrivateDispatch { private void DispatchInput(Host host) { } }
    private sealed class WrongHostDispatch { public void DispatchInput(WrongHeightHost host) { } }
}
