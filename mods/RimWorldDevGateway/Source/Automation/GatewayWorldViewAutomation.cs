namespace RimWorldDevGateway;

internal static class GatewayWorldViewRestorePolicy
{
    internal static bool TryClose(bool targeter, bool routePlanner, bool tilePicker, bool drag, Action close)
    {
        if (targeter || routePlanner || tilePicker || drag) return false;
        close(); return true;
    }

    internal static bool DrainHistory(System.Collections.IList history)
    {
        // Native World frames append even with no drag. Bound work per advance, not total history.
        for (int removed = 0; removed < 256 && history.Count != 0; removed++) history.RemoveAt(history.Count - 1);
        return history.Count == 0;
    }
}

internal sealed record GatewayWorldViewState(bool Open, string? OpenMainTab, bool Current, bool Ready = true);
internal sealed record GatewayWorldRestoreState(bool Complete, bool Restored, string Phase, string? Error = null)
{
    public IReadOnlyDictionary<string, string> Checks { get; init; } = new Dictionary<string, string>();
}
internal interface IGatewayWorldViewEvidence
{
    IReadOnlyDictionary<string, object?> BaselineEvidence { get; }
}

internal interface IGatewayWorldViewSession
{
    GatewayWorldViewState Observe();
    void Activate(Action beforeNativeCommand);
    GatewayWorldRestoreState Restore();
}

internal interface IGatewayWorldViewRuntime
{
    GatewayAutomationAvailability Availability();
    IGatewayWorldViewSession Capture();
}

internal sealed class GatewayWorldViewAutomation
{
    private readonly IGatewayWorldViewRuntime runtime;
    private IGatewayWorldViewSession? session;
    private string? handle;
    private bool restoring;
    private readonly Dictionary<string, GatewayWorldRestoreState> history = new(StringComparer.Ordinal);

    internal GatewayWorldViewAutomation(GatewayAutomationRegistry registry, IGatewayWorldViewRuntime runtime)
    {
        this.runtime = runtime;
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor("native-world.capture", "1.0",
            "Passively capture one bounded map-to-World restoration lease.", GatewayCaravanArguments.Schema(),
            new[] { "settled-map-control", "empty-selection", "inactive-world-camera" }, false), Capture, runtime.Availability);
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor("native-world.set-open", "1.0",
            "Invoke the displayed native World button once when its desired state differs.",
            GatewayCaravanArguments.Schema("viewHandle", "open"), new[] { "current-world-view-lease", "native-world-button" }, true),
            SetOpen, runtime.Availability);
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor("native-world.restore", "1.0",
            "Restore the exact captured view through bounded cleanup and a later native-frame verification.",
            GatewayCaravanArguments.Schema("viewHandle"), new[] { "exact-world-view-lease" }, true),
            (context, arguments) =>
            {
                GatewayCaravanArguments.Validate(arguments, "viewHandle");
                string requested = (string)arguments["viewHandle"]!;
                if (history.TryGetValue(requested, out var prior)) return prior.Restored ? prior : new FailedRestoreReplay(prior);
                if (session == null || requested != handle || restoring)
                    throw new GatewayAutomationException("stale_world_view_handle", "The view lease is absent or already restoring.");
                restoring = true;
                return new RestoreOperation(this, session, requested, context);
            }, runtime.Availability);
    }

    private object Capture(GatewayAutomationContext context, IReadOnlyDictionary<string, object?> arguments)
    {
        GatewayCaravanArguments.Validate(arguments);
        if (session != null) throw new GatewayAutomationException("native_world_view_busy", "Restore the current World view lease first.");
        var captured = runtime.Capture();
        var baseline = captured.Observe();
        if (!baseline.Current || !baseline.Ready)
            throw new GatewayAutomationException("native_world_capture_context_changed", "The passive view baseline changed before ownership could be retained.");
        session = captured;
        handle = Guid.NewGuid().ToString("N");
        var result = new Dictionary<string, object?> { ["viewHandle"] = handle, ["baseline"] = baseline,
            ["capturedFields"] = (captured as IGatewayWorldViewEvidence)?.BaselineEvidence };
        context.AddArtifact("native-world-lease", "native-world-view-lease", result);
        return result;
    }

    internal IGatewayWorldViewSession Require(string viewHandle)
    {
        if (session == null || handle != viewHandle || restoring)
            throw new GatewayAutomationException("stale_world_view_handle", "The view lease is no longer active.");
        if (!session.Observe().Current)
            throw new GatewayAutomationException("native_world_context_changed", "The captured Game, World or view owner changed.");
        return session;
    }

    private object SetOpen(GatewayAutomationContext context, IReadOnlyDictionary<string, object?> arguments)
    {
        GatewayCaravanArguments.Validate(arguments, "viewHandle", "open");
        var view = Require((string)arguments["viewHandle"]!);
        bool desired = (bool)arguments["open"]!;
        var before = view.Observe();
        context.AddArtifact("native-world-before", "native-world-state", before);
        if (!before.Ready) throw new GatewayAutomationException("native_world_input_blocked", "World commands require settled unobstructed player control, including no-ops.");
        try
        {
            if (before.Open != desired)
                view.Activate(() =>
                {
                    context.RunStep("native-world-command-admission", () => { });
                    context.AddArtifact("native-world-invocation", "native-world-command",
                        new Dictionary<string, object?> { ["requestedOpen"] = desired, ["attempted"] = true });
                });
        }
        catch
        {
            try { context.AddArtifact("native-world-after", "native-world-state", view.Observe()); }
            catch { context.AddArtifact("native-world-after-unavailable", "native-observation-error", true); }
            throw;
        }
        var after = view.Observe();
        context.AddArtifact("native-world-after", "native-world-state", after);
        if (!after.Current || after.Open != desired)
            throw new GatewayAutomationException("native_world_state_not_observed", "The native command did not produce the requested state in the same view.");
        return new Dictionary<string, object?> { ["open"] = after.Open, ["changed"] = before.Open != after.Open };
    }

    private sealed class RestoreOperation : IGatewayDeferredAutomationOperation
    {
        private readonly GatewayWorldViewAutomation owner;
        private readonly IGatewayWorldViewSession view;
        private readonly string handle;
        private readonly GatewayAutomationContext context;
        private readonly DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        private GatewayDeferredAutomationResult? terminal;
        internal RestoreOperation(GatewayWorldViewAutomation owner, IGatewayWorldViewSession view,
            string handle, GatewayAutomationContext context)
        { this.owner = owner; this.view = view; this.handle = handle; this.context = context; }

        public GatewayDeferredAutomationResult? Advance(bool stopRequested)
        {
            if (terminal != null) return terminal;
            GatewayWorldRestoreState state;
            try { state = view.Restore(); }
            catch (Exception exception)
            { state = new(true, false, "failed", exception is GatewayAutomationException known ? known.Code : exception.GetBaseException().GetType().Name); }
            if (!state.Complete && DateTimeOffset.UtcNow < deadline) return null;
            if (!state.Complete) state = state with { Complete = true, Error = state.Error ?? "native_world_restore_deadline" };
            owner.history[handle] = state;
            if (owner.history.Count > 8) owner.history.Remove(owner.history.Keys.First());
            if (state.Restored) { owner.session = null; owner.handle = null; owner.restoring = false; }
            context.AddArtifact("native-world-restoration", "native-world-restoration", state);
            return terminal = state.Restored ? GatewayDeferredAutomationResult.Success(state) :
                GatewayDeferredAutomationResult.Failure(state.Error ?? "native_world_restore_failed", "The captured World view was not fully restored.", state);
        }
    }
    private sealed class FailedRestoreReplay : IGatewayDeferredAutomationOperation
    {
        private readonly GatewayWorldRestoreState state;
        internal FailedRestoreReplay(GatewayWorldRestoreState state) => this.state = state;
        public GatewayDeferredAutomationResult Advance(bool stopRequested) => GatewayDeferredAutomationResult.Failure(
            state.Error ?? "native_world_restore_failed", "The historical World restoration remains incomplete; no new writes were made.", state);
    }
}

internal static class GatewayCaravanArguments
{
    internal static void Validate(IReadOnlyDictionary<string, object?> arguments, params string[] keys)
    {
        if (arguments.Count != keys.Length + 1 || !arguments.TryGetValue("version", out var version) ||
            version is not (1 or 1L or 1m or 1d or 1f) || keys.Any(key => !arguments.ContainsKey(key)))
            throw new GatewayAutomationException("invalid_native_caravan_arguments", "Only version 1 and the exact declared arguments are accepted.");
        foreach (string key in keys)
            if (key == "open" ? arguments[key] is not bool : arguments[key] is not string text || string.IsNullOrWhiteSpace(text) || text.Length > 200)
                throw new GatewayAutomationException("invalid_native_caravan_arguments", "Expected a bounded identity or Boolean desired state.");
    }

    internal static IReadOnlyDictionary<string, object?> Schema(params string[] keys)
    {
        var properties = new Dictionary<string, object?>
        { ["version"] = new Dictionary<string, object?> { ["type"] = "integer", ["const"] = 1 } };
        foreach (string key in keys)
            properties[key] = key == "open" ? new Dictionary<string, object?> { ["type"] = "boolean" } :
                new Dictionary<string, object?> { ["type"] = "string", ["minLength"] = 1, ["maxLength"] = 200 };
        return new Dictionary<string, object?> { ["type"] = "object", ["additionalProperties"] = false,
            ["required"] = properties.Keys.ToArray(), ["properties"] = properties };
    }
}
