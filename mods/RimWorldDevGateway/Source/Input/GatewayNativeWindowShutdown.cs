namespace RimWorldDevGateway;

internal static class GatewayNativeWindowShutdown
{
    public static void Cleanup(GatewayAutomationRegistry? registry,
        Func<GatewayDeferredAutomationResult?> shutdownInput, params Action[] remainingCleanup)
    {
        var failures = new List<Exception>();
        Attempt(() =>
        {
            if (registry != null)
                foreach (var run in registry.ListRuns())
                    if (!run.IsTerminal) registry.Cancel(run.RunId);
        }, failures);
        Attempt(() =>
        {
            var receipt = shutdownInput();
            if (receipt == null) return;
            bool complete = receipt.Result is IReadOnlyDictionary<string, object?> state &&
                state.TryGetValue("cleanupComplete", out var cleaned) && cleaned is true;
            if (receipt.Error?.Code == "native_input_shutdown_incomplete" || !complete)
                throw new GatewayAutomationException(receipt.Error?.Code ?? "native_input_cleanup_incomplete",
                    "Native input cleanup did not complete; shutdown ownership is retained.");
        }, failures);
        Attempt(() =>
        {
            registry?.Advance();
            if (registry?.ListRuns().Any(run => !run.IsTerminal) == true)
                throw new GatewayAutomationException("automation_cleanup_incomplete", "Deferred work still owns unfinished cleanup.");
        }, failures);
        foreach (var cleanup in remainingCleanup) Attempt(cleanup, failures);
        if (failures.Count != 0) throw new AggregateException("Gateway shutdown cleanup remains incomplete.", failures);
    }

    private static void Attempt(Action cleanup, List<Exception> failures)
    {
        try { cleanup(); }
        catch (Exception exception) { failures.Add(exception); }
    }
}
