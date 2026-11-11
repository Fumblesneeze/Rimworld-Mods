namespace RimWorldDevGateway;

/// <summary>Explicit opt-in for a handler that must finish across native frames.</summary>
public interface IGatewayDeferredAutomationOperation
{
    // Called once per registry Advance, on the original handler thread. Never wait for a frame.
    // Null retains the run. A terminal result promises owned-input cleanup is finished.
    // Once stopRequested is true, finish only cleanup and report any already committed result.
    GatewayDeferredAutomationResult? Advance(bool stopRequested);
}

public sealed class GatewayDeferredAutomationResult
{
    private GatewayDeferredAutomationResult(object? result, GatewayAutomationError? error)
    {
        Result = result;
        Error = error;
    }

    public object? Result { get; }
    public GatewayAutomationError? Error { get; }

    public static GatewayDeferredAutomationResult Success(object? result = null) => new(result, null);

    public static GatewayDeferredAutomationResult Failure(string code, string message, object? result = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("A failure code is required.", nameof(code));
        if (message is null) throw new ArgumentNullException(nameof(message));
        return new(result, new GatewayAutomationError(code, message));
    }
}
