namespace RimWorldDevGateway;

internal enum GatewayNativeTextPhase { SelectAllPress, SelectAllRelease, ClearPress, ClearRelease, Character }

internal interface IGatewayNativeTextBackend
{
    string? Validate();
    void Queue(GatewayNativeTextPhase phase, char character);
    void Cleanup();
}

internal sealed class GatewayNativeTextOperation : IGatewayDeferredAutomationOperation
{
    // Repository workload policy: native policy names are at most27 characters; numeric fields
    // and invalid-input probes need fewer. This bounds dispatch work without claiming an engine limit.
    public const int MaximumCharacters = 128;
    private readonly IGatewayNativeTextBackend backend;
    private readonly string text;
    private GatewayDeferredAutomationResult? result;
    private int completed;
    private bool queued, stopped, cleanup, uncertain;
    private string? failure;
    public GatewayNativeTextOperation(IGatewayNativeTextBackend backend, string text)
    {
        this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
        if (!ValidText(text)) throw new ArgumentException("Replacement text must be bounded printable BMP text.", nameof(text));
        this.text = text;
    }
    public static bool ValidText(string? value) => value != null && value.Length <= MaximumCharacters &&
        !value.Any(character => char.GetUnicodeCategory(character) is System.Globalization.UnicodeCategory.Control or
            System.Globalization.UnicodeCategory.Format or System.Globalization.UnicodeCategory.Surrogate or
            System.Globalization.UnicodeCategory.LineSeparator or System.Globalization.UnicodeCategory.ParagraphSeparator or
            System.Globalization.UnicodeCategory.OtherNotAssigned);
    public bool IsQuarantined { get; private set; }
    public void BeforeDispatch(bool stopRequested = false)
    {
        if (result != null) return;
        stopped |= stopRequested;
        if (stopped) { cleanup = true; return; }
        if (queued || cleanup) return;
        try
        {
            failure = backend.Validate();
            if (failure != null) { cleanup = true; return; }
            queued = true;
            backend.Queue(completed < 4 ? (GatewayNativeTextPhase)completed : GatewayNativeTextPhase.Character,
                completed < 4 ? '\0' : text[completed - 4]);
        }
        catch
        {
            uncertain = queued;
            queued = false;
            failure = "native_input_failed";
            cleanup = true;
        }
    }
    public void AfterDispatch(bool delivered, string? rejection = null)
    {
        if (result != null) return;
        if (queued)
        {
            if (delivered) completed++;
            else failure = rejection ?? "native_input_not_dispatched";
            queued = false;
        }
        cleanup |= stopped || failure != null || completed == text.Length + 4;
        if (!cleanup) return;
        string? initial = failure;
        IReadOnlyList<IReadOnlyDictionary<string, object?>> cleanupFailures = Array.Empty<IReadOnlyDictionary<string, object?>>();
        bool cleanupFailuresTruncated = false;
        try { backend.Cleanup(); }
        catch (Exception exception)
        {
            failure = "native_input_cleanup_incomplete"; IsQuarantined = true;
            // Three fixed keys and one character queue are the entire cleanup surface. Keep
            // each top-level failure, never traverse an arbitrary foreign exception graph.
            const int maximumFailures = 4;
            IReadOnlyList<Exception> errors = exception is AggregateException aggregate ? aggregate.InnerExceptions : new[] { exception };
            cleanupFailuresTruncated = errors.Count > maximumFailures;
            cleanupFailures = errors.Take(maximumFailures).Select(error =>
                (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
                {
                    ["code"] = error is GatewayAutomationException gateway && gateway.Code is
                        "native_input_ownership_lost" or "native_text_queue_ownership_lost"
                        ? gateway.Code : "native_text_cleanup_failed",
                    // Reuse the existing bounded/trusted-type formatter but persist no message,
                    // stack, Data or arbitrary Gateway error code, even on known exception types.
                    ["exceptionType"] = GatewayIntegrationTestExceptionFormatter.Format(error,
                        maximumTypeCharacters: 128, maximumMessageCharacters: 4, maximumStackCharacters: 4).Type
                }).ToArray();
        }
        var receipt = new Dictionary<string, object?>
        {
            ["selectAllDispatched"] = completed >= 1, ["selectionReleaseDispatched"] = completed >= 2,
            ["clearDispatched"] = completed >= 3, ["clearReleaseDispatched"] = completed >= 4,
            ["charactersDispatched"] = Math.Max(0, completed - 4), ["requestedCharacters"] = text.Length,
            ["cleanupComplete"] = !IsQuarantined, ["stopped"] = stopped,
            ["dispatchUncertain"] = uncertain, ["initialFailure"] = initial,
            ["cleanupFailures"] = cleanupFailures, ["cleanupFailuresTruncated"] = cleanupFailuresTruncated
        };
        result = failure == null ? GatewayDeferredAutomationResult.Success(receipt) :
            GatewayDeferredAutomationResult.Failure(failure, "Native text replacement did not complete.", receipt);
    }
    public GatewayDeferredAutomationResult? Advance(bool stopRequested)
    {
        if (result == null) stopped |= stopRequested;
        return result;
    }
}
