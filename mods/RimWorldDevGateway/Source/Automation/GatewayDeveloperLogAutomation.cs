namespace RimWorldDevGateway;

internal sealed record GatewayDeveloperLogState(bool Ready, bool DeveloperMode, int OpenWindows, bool Ambiguous = false)
{
    public static GatewayDeveloperLogState FromOpenWindowTypes(bool ready, bool developerMode,
        Type nativeLogType, IEnumerable<Type> openTypes)
    {
        int exact = 0;
        bool derived = false;
        foreach (var type in openTypes)
            if (type == nativeLogType) exact++;
            else if (nativeLogType.IsAssignableFrom(type)) derived = true;
        return new(ready, developerMode, exact, derived);
    }
}

internal static class GatewayDeveloperLogAutomation
{
    public const string Name = "native-log.set-open";

    public static void Register(GatewayAutomationRegistry registry, Func<GatewayDeveloperLogState> read,
        Action toggle, Func<GatewayAutomationAvailability> availability)
    {
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor(Name, "1.0",
                "Open or close the native developer log through its ordinary toolbar command.",
                new Dictionary<string, object?>
                {
                    ["type"] = "object", ["additionalProperties"] = false,
                    ["required"] = new[] { "version", "open" },
                    ["properties"] = new Dictionary<string, object?>
                    {
                        ["version"] = new Dictionary<string, object?> { ["type"] = "integer", ["const"] = 1 },
                        ["open"] = new Dictionary<string, object?> { ["type"] = "boolean" }
                    }
                }, new[] { "native-developer-log-command", "developer-mode", "settled-ui" }, true),
            (context, arguments) => context.RunStep("native-developer-log-command", () =>
            {
                if (arguments.Count != 2 || !arguments.TryGetValue("version", out var version) ||
                    !(version switch { int n => n == 1, long n => n == 1, decimal n => n == 1,
                        float n => n == 1, double n => n == 1, _ => false }) ||
                    !arguments.TryGetValue("open", out var requested) || requested is not bool desired)
                    throw new GatewayAutomationException("invalid_native_log_arguments",
                        "Only version 1 and a Boolean open state are accepted.");
                var before = read();
                context.AddArtifact("native-log-before", "native-log-state", before);
                if (!before.Ready || !before.DeveloperMode || before.Ambiguous || before.OpenWindows is < 0 or > 1)
                    throw new GatewayAutomationException("native_log_context_unavailable",
                        "Developer mode, a settled UI and at most one native log window are required.");
                bool changed = desired != (before.OpenWindows == 1);
                if (changed)
                {
                    context.AddArtifact("native-log-invocation", "native-log-command", new Dictionary<string, object?>
                        { ["requestedOpen"] = desired, ["attempted"] = true });
                    toggle();
                }
                var after = read();
                context.AddArtifact("native-log-after", "native-log-state", after);
                if (!after.Ready || !after.DeveloperMode || after.Ambiguous || after.OpenWindows != (desired ? 1 : 0))
                    throw new GatewayAutomationException("native_log_state_not_observed",
                        "The native command did not leave the requested log state in a settled developer UI.");
                return new Dictionary<string, object?> { ["open"] = desired, ["wasOpen"] = before.OpenWindows == 1,
                    ["changed"] = changed };
            }), availability);
    }
}
