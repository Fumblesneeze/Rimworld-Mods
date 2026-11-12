namespace RimWorldDevGateway;

// The runtime adapter owns one exact window and its captured geometry/pointer. Calls are on the
// native GUI thread. A successful operation records dispatch, never the control's gameplay result.
internal interface IGatewayNativeClickBackend
{
    string? Validate(bool ownsButton);
    void Press();
    void Release();
    void RestorePointer();
}

internal interface IGatewayNativeScopedClickBackend
{
    void CompletePointer(bool successfulGesture);
    IReadOnlyDictionary<string, object?> PointerReceipt { get; }
}

internal interface IGatewayNativeDragBackend : IGatewayNativeClickBackend
{
    void MoveHeld();
}

internal sealed class GatewayNativeClickOperation : IGatewayDeferredAutomationOperation
{
    private readonly IGatewayNativeClickBackend backend;
    private readonly bool drag;
    private Phase phase;
    private GatewayDeferredAutomationResult? result;
    private bool ownsButton;
    private bool ownsPointer;
    private bool pressDispatched;
    private bool releaseDispatched;
    private bool dragDispatched;
    private bool stopped;
    private bool dispatchUncertain;
    private string? failure;
    private string? initialFailure;
    public Exception? Failure { get; private set; }
    public bool IsQuarantined => phase == Phase.Quarantined;

    public GatewayNativeClickOperation(IGatewayNativeClickBackend backend, bool drag = false)
    {
        this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
        if (drag && backend is not IGatewayNativeDragBackend) throw new ArgumentException("Drag requires a held-movement backend.", nameof(backend));
        this.drag = drag;
    }
    public void BeforeDispatch(bool stopRequested = false)
    {
        try { QueueNextPhase(stopRequested); }
        catch (Exception exception)
        {
            Failure = exception;
            failure = "native_input_failed";
            dispatchUncertain = ownsPointer;
            phase = Phase.Cleanup;
            if (!ownsPointer) Finish();
        }
    }

    private void QueueNextPhase(bool stopRequested)
    {
        if (phase is Phase.Complete or Phase.Quarantined) return;
        stopped |= stopRequested;
        if (stopped && phase != Phase.Complete)
        {
            phase = Phase.Cleanup;
            if (!ownsPointer) Finish();
            return;
        }
        if (phase is not (Phase.WaitingPress or Phase.WaitingMove or Phase.WaitingRelease)) return;
        string? rejection = backend.Validate(phase != Phase.WaitingPress);
        if (rejection != null)
        {
            failure = rejection;
            phase = Phase.Cleanup;
            if (!ownsPointer) Finish();
            return;
        }
        if (phase == Phase.WaitingPress)
        {
            ownsPointer = ownsButton = true;
            backend.Press();
            phase = Phase.PressQueued;
        }
        else if (phase == Phase.WaitingMove)
        {
            ((IGatewayNativeDragBackend)backend).MoveHeld();
            phase = Phase.MoveQueued;
        }
        else if (phase == Phase.WaitingRelease)
        {
            backend.Release();
            ownsButton = false;
            phase = Phase.ReleaseQueued;
        }
    }
    public void AfterDispatch(bool originalRan)
    {
        if (!originalRan && phase is Phase.PressQueued or Phase.MoveQueued or Phase.ReleaseQueued)
        {
            failure = "native_input_not_dispatched";
            phase = Phase.Cleanup;
        }
        else if (phase == Phase.PressQueued)
        {
            pressDispatched = true;
            phase = drag ? Phase.WaitingMove : Phase.WaitingRelease;
        }
        else if (phase == Phase.MoveQueued)
        {
            dragDispatched = true;
            phase = Phase.WaitingRelease;
        }
        else if (phase == Phase.ReleaseQueued)
        {
            releaseDispatched = true;
            phase = Phase.Cleanup;
        }
        if (phase != Phase.Cleanup) return;
        // Cleanup after native input consumption lets BeginFrame clear the release edge, so a
        // replacement window receives no synthetic mouse-up. Never release somebody else's input.
        Exception? cleanupFailure = null;
        try { if (ownsButton) { backend.Release(); ownsButton = false; } }
        catch (Exception exception) { cleanupFailure = exception; }
        try
        {
            if (ownsPointer)
            {
                if (backend is IGatewayNativeScopedClickBackend scoped)
                    scoped.CompletePointer(cleanupFailure == null && failure == null && !stopped && releaseDispatched);
                else backend.RestorePointer();
                ownsPointer = false;
            }
        }
        catch (Exception exception)
        {
            cleanupFailure = cleanupFailure == null ? exception : new AggregateException(cleanupFailure, exception);
        }
        if (cleanupFailure != null)
        {
            Failure = Failure == null ? cleanupFailure : new AggregateException(Failure, cleanupFailure);
            initialFailure = failure;
            failure = "native_input_cleanup_incomplete";
        }
        Finish(cleanupFailure == null);
    }
    public GatewayDeferredAutomationResult? Advance(bool stopRequested)
    {
        if (result == null) stopped |= stopRequested;
        return result;
    }

    private void Finish(bool cleanupComplete = true)
    {
        phase = cleanupComplete ? Phase.Complete : Phase.Quarantined;
        var receipt = new Dictionary<string, object?>
        {
            ["pressDispatched"] = pressDispatched, ["releaseDispatched"] = releaseDispatched,
            ["cleanupComplete"] = cleanupComplete, ["stopped"] = stopped,
            ["dispatchUncertain"] = dispatchUncertain, ["initialFailure"] = initialFailure
        };
        if (drag) receipt["dragDispatched"] = dragDispatched;
        if (backend is IGatewayNativeScopedClickBackend scoped)
            foreach (var pair in scoped.PointerReceipt) receipt[pair.Key] = pair.Value;
        result = failure == null ? GatewayDeferredAutomationResult.Success(receipt)
            : GatewayDeferredAutomationResult.Failure(failure, drag ? "Native drag context is unavailable." : "Native click context is unavailable.", receipt);
    }

    private enum Phase { WaitingPress, PressQueued, WaitingMove, MoveQueued, WaitingRelease, ReleaseQueued, Cleanup, Complete, Quarantined }
}
