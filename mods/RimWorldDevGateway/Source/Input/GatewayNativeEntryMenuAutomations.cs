using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway;

internal static class GatewayNativeEntryMenuAutomations
{
    public const string CaptureName = "native-entry-menu.capture";
    public const string ClickName = "native-entry-menu.click";
    public static void Register(GatewayAutomationRegistry registry,
        Func<IReadOnlyList<EndToEndNativeEntryMenuControl>> capture,
        Func<string, GatewayAutomationContext, IGatewayDeferredAutomationOperation> click,
        Func<GatewayAutomationAvailability> availability)
    {
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor(CaptureName, "1.0",
            "Capture the exact rendered Options and developer quicktest controls at settled Entry.",
            Schema(false), new[] { "inspected-native-input-provider", "settled-entry-menu" }, false),
            (_, arguments) => { Validate(arguments, false); return capture(); }, availability);
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor(ClickName, "1.0",
            "Click the captured Entry control center through native input and retain owned cleanup.",
            Schema(true), new[] { "inspected-native-input-provider", "exact-current-entry-control", "neutral-input" }, true),
            (context, arguments) =>
            {
                Validate(arguments, true);
                string handle = (string)arguments["controlHandle"]!;
                context.AddArtifact("native-entry-click-request", "native-input-request",
                    new Dictionary<string, object?> { ["controlHandle"] = handle });
                return click(handle, context);
            }, availability);
    }

    private static void Validate(IReadOnlyDictionary<string, object?> arguments, bool click)
    {
        if (arguments.Count != (click ? 2 : 1) || !arguments.TryGetValue("version", out var version) ||
            version is not (1 or 1L or 1m or 1d or 1f) ||
            click && (!arguments.TryGetValue("controlHandle", out var value) || value is not string handle ||
                string.IsNullOrWhiteSpace(handle) || handle.Length > 200))
            throw new GatewayAutomationException("invalid_native_input_arguments", "Entry input requires version1 and one current control handle for a click.");
    }

    private static IReadOnlyDictionary<string, object?> Schema(bool click)
    {
        var properties = new Dictionary<string, object?>
        { ["version"] = new Dictionary<string, object?> { ["type"] = "integer", ["const"] = 1 } };
        if (click) properties["controlHandle"] = new Dictionary<string, object?> { ["type"] = "string", ["minLength"] = 1, ["maxLength"] = 200 };
        return new Dictionary<string, object?> { ["type"] = "object", ["additionalProperties"] = false,
            ["required"] = properties.Keys.ToArray(), ["properties"] = properties };
    }
}
