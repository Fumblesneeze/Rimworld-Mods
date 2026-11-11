namespace RimWorldDevGateway;

internal readonly record struct GatewayNativePointerPosition(double X, double Y);

// One workflow lease, independent of Unity/Harmony. The runtime supplies its actual pointer reads/moves.
internal sealed class GatewayNativePointerScope
{
    public GatewayNativePointerScope(string handle, GatewayNativePointerPosition original, DateTimeOffset deadlineUtc)
    { Handle = handle; Original = Expected = original; DeadlineUtc = deadlineUtc; }
    public string Handle { get; }
    public GatewayNativePointerPosition Original { get; }
    public GatewayNativePointerPosition Expected { get; private set; }
    public DateTimeOffset DeadlineUtc { get; }
    public bool Ending { get; private set; }
    private bool closing;
    public GatewayDeferredAutomationResult? Result { get; private set; }
    public bool IsExpired(DateTimeOffset now) => now >= DeadlineUtc;
    public void RequestEnd() => Ending = true;
    public GatewayDeferredAutomationResult SnapshotForShutdown() => Result ?? GatewayDeferredAutomationResult.Failure(
        "native_input_shutdown_incomplete", "An owned pointer scope has not completed restoration.",
        new Dictionary<string, object?>
        {
            ["scopeHandle"] = Handle, ["deadlineUtc"] = DeadlineUtc.ToString("O"),
            ["cleanupComplete"] = false, ["pointerRestored"] = false,
            ["pointerScopeState"] = closing ? "restoring" : "pending"
        });
    public string? Reject(DateTimeOffset now, GatewayNativePointerPosition current)
        => RejectLifetime(now) ?? (current != Expected ? "native_pointer_ownership_lost" : null);
    public string? RejectLifetime(DateTimeOffset now)
        => Result != null || Ending ? "native_pointer_scope_closed" : IsExpired(now) ? "native_pointer_scope_expired" : null;
    public string? RejectBeforePress(DateTimeOffset now, Func<GatewayNativePointerPosition> read)
        => Reject(now, read());
    public void Retain(DateTimeOffset now, GatewayNativePointerPosition current, GatewayNativePointerPosition target)
    {
        string? error = RejectLifetime(now);
        if (error != null || current != target)
            throw new GatewayAutomationException(error ?? "native_pointer_ownership_lost", "Pointer scope cannot accept this gesture.");
        Expected = target;
    }
    public GatewayDeferredAutomationResult? Close(Func<GatewayNativePointerPosition> read, Action<GatewayNativePointerPosition> move)
    {
        if (Result != null) return Result;
        // QueueMouseMove invokes callbacks before updating the position. A synchronous cleanup
        // request must observe pending ownership until this outer readback publishes its result.
        if (closing) return null;
        closing = true;
        Ending = true;
        try { return CloseOnce(read, move); }
        finally { closing = false; }
    }

    private GatewayDeferredAutomationResult CloseOnce(Func<GatewayNativePointerPosition> read, Action<GatewayNativePointerPosition> move)
    {
        string? error = null;
        try
        {
            if (read() != Expected) error = "native_pointer_ownership_lost";
            else
            {
                if (Expected != Original) move(Original);
                if (read() != Original) error = "native_pointer_restore_unconfirmed";
            }
        }
        catch { error = "native_pointer_restore_failed"; }
        var receipt = new Dictionary<string, object?>
        {
            ["scopeHandle"] = Handle, ["cleanupComplete"] = error == null,
            ["pointerRestored"] = error == null, ["deadlineUtc"] = DeadlineUtc.ToString("O"),
            ["pointerScopeState"] = error == null ? "closed" : "cleanup-incomplete"
        };
        return Result = error == null ? GatewayDeferredAutomationResult.Success(receipt) :
            GatewayDeferredAutomationResult.Failure(error, "Workflow pointer cleanup did not complete.", receipt);
    }
}
