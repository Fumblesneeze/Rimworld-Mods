namespace RimWorldDevGateway;

internal sealed record GatewayMainTabContext(object Game, object Root, object Ui, object Bar,
    object Stack, object Tabs, object Target, object Worker, object? Map)
{
    internal bool Matches(GatewayMainTabContext? other) => other != null &&
        ReferenceEquals(Game, other.Game) && ReferenceEquals(Root, other.Root) &&
        ReferenceEquals(Ui, other.Ui) && ReferenceEquals(Bar, other.Bar) &&
        ReferenceEquals(Stack, other.Stack) && ReferenceEquals(Tabs, other.Tabs) &&
        ReferenceEquals(Target, other.Target) && ReferenceEquals(Worker, other.Worker) &&
        ReferenceEquals(Map, other.Map);
}

internal sealed record GatewayMainTabState(string DefName, bool Ready, int MatchCount,
    bool Supported, bool Visible, bool Disabled, bool ReceivesInput, bool IsOpen, string? OpenTabDefName)
{
    // Live references are invocation-only, never part of the serialized observation.
    internal GatewayMainTabContext? Context { get; init; }
}

internal interface IGatewayMainTabRuntime
{
    GatewayMainTabState Read(string defName);
    void Activate(GatewayMainTabState expected, Action beforeNativeInvoke);
}

internal static class GatewayMainTabAutomation
{
    public const string Name = "native-main-tab.set-open";

    public static void Register(GatewayAutomationRegistry registry, IGatewayMainTabRuntime runtime,
        Func<GatewayAutomationAvailability> availability)
    {
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor(Name, "1.0",
                "Open or close one exact supported bottom tab through its native button command.",
                new Dictionary<string, object?>
                {
                    ["type"] = "object", ["additionalProperties"] = false,
                    ["required"] = new[] { "version", "defName", "open" },
                    ["properties"] = new Dictionary<string, object?>
                    {
                        ["version"] = new Dictionary<string, object?> { ["type"] = "integer", ["const"] = 1 },
                        ["defName"] = new Dictionary<string, object?> { ["type"] = "string", ["minLength"] = 1 },
                        ["open"] = new Dictionary<string, object?> { ["type"] = "boolean" }
                    }
                }, new[] { "native-toggle-tab", "settled-player-control", "bottom-bar-input" }, true),
            (context, arguments) =>
            {
                if (arguments.Count != 3 || !arguments.TryGetValue("version", out var version) ||
                    !(version switch { int n => n == 1, long n => n == 1, decimal n => n == 1,
                        float n => n == 1, double n => n == 1, _ => false }) ||
                    !arguments.TryGetValue("defName", out var target) || target is not string name ||
                    string.IsNullOrWhiteSpace(name) || !arguments.TryGetValue("open", out var value) || value is not bool desired)
                    throw new GatewayAutomationException("invalid_native_main_tab_arguments",
                        "Only version 1, an exact nonempty defName and a Boolean open state are accepted.");
                var before = runtime.Read(name);
                context.AddArtifact("native-main-tab-before", "native-main-tab-state", before with { Context = null });
                RequireAvailable(before);
                bool changed = desired != before.IsOpen;
                try
                {
                    if (changed) runtime.Activate(before, () =>
                    {
                        context.RunStep("native-main-tab-dispatch-admission", () => { });
                        context.AddArtifact("native-main-tab-invocation", "native-main-tab-command",
                            new Dictionary<string, object?> { ["defName"] = name, ["requestedOpen"] = desired, ["attempted"] = true });
                    });
                }
                catch
                {
                    try { ObserveAfter(); }
                    catch (Exception observation)
                    {
                        context.AddArtifact("native-main-tab-after-unavailable", "native-main-tab-observation-error",
                            new Dictionary<string, object?> { ["exceptionType"] = observation.GetBaseException().GetType().Name });
                    }
                    throw;
                }
                var after = ObserveAfter();
                RequireAvailable(after);
                if (after.DefName != name || !before.Context!.Matches(after.Context) || after.IsOpen != desired)
                    throw new GatewayAutomationException("native_main_tab_state_not_observed",
                        "The native button did not leave the requested tab state in the same game/UI context.");
                return new Dictionary<string, object?> { ["defName"] = name, ["open"] = after.IsOpen,
                    ["wasOpen"] = before.IsOpen, ["changed"] = changed, ["previousOpenTab"] = before.OpenTabDefName };

                GatewayMainTabState ObserveAfter()
                {
                    var observed = runtime.Read(name);
                    context.AddArtifact("native-main-tab-after", "native-main-tab-state", observed with { Context = null });
                    return observed;
                }
            }, availability);
    }

    internal static void RequireAvailable(GatewayMainTabState state)
    {
        string? failure = !state.Ready ? "native_main_tab_context_unavailable" :
            state.MatchCount != 1 ? "native_main_tab_target_unavailable" :
            !state.Supported ? "native_main_tab_unsupported" :
            state.Context == null ? "native_main_tab_context_unavailable" :
            !state.Visible ? "native_main_tab_hidden" :
            state.Disabled ? "native_main_tab_disabled" :
            !state.ReceivesInput ? "native_main_tab_input_blocked" : null;
        if (failure != null) throw new GatewayAutomationException(failure,
            "The exact tab requires a supported visible enabled button and settled unobscured player control.");
    }
}
