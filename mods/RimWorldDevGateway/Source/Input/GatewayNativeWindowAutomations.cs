namespace RimWorldDevGateway;

internal static class GatewayNativeWindowAutomations
{
    public const string CaptureName = "native-window.capture";
    public const string ClickName = "native-window.click";
    public const string ReplaceTextName = "native-window.replace-text";
    public const string BeginScopeName = "native-pointer-scope.begin";
    public const string EndScopeName = "native-pointer-scope.end";

    public static void Register(GatewayAutomationRegistry registry,
        Func<IReadOnlyList<GatewayNativeWindowDescriptor>> capture,
        Func<string, double, double, GatewayAutomationContext, IGatewayDeferredAutomationOperation> click,
        Func<GatewayAutomationAvailability> availability,
        Func<DateTimeOffset, GatewayAutomationContext, IReadOnlyDictionary<string, object?>>? beginScope = null,
        Func<string, GatewayAutomationContext, IGatewayDeferredAutomationOperation>? endScope = null,
        Func<string, double, double, GatewayAutomationContext, string, IGatewayDeferredAutomationOperation>? scopedClick = null,
        Func<string, string, string, GatewayAutomationContext, IGatewayDeferredAutomationOperation>? replaceText = null,
        Func<GatewayAutomationAvailability>? textAvailability = null)
    {
        if (replaceText != null)
        {
            registry.RegisterBuiltIn(new GatewayAutomationDescriptor(ReplaceTextName, "1.0",
                    "Replace one captured focused native single-line text field through native selection, clear and character input.",
                    new Dictionary<string, object?>
                    {
                        ["type"] = "object", ["additionalProperties"] = false,
                        ["required"] = new[] { "version", "windowHandle", "controlHandle", "text" },
                        ["properties"] = new Dictionary<string, object?>
                        {
                            ["version"] = new Dictionary<string, object?> { ["type"] = "integer", ["const"] = 1 },
                            ["windowHandle"] = new Dictionary<string, object?> { ["type"] = "string", ["minLength"] = 1, ["maxLength"] = 200 },
                            ["controlHandle"] = new Dictionary<string, object?> { ["type"] = "string", ["minLength"] = 1, ["maxLength"] = 200 },
                            ["text"] = new Dictionary<string, object?> { ["type"] = "string", ["maxLength"] = GatewayNativeTextOperation.MaximumCharacters }
                        }
                    }, new[] { "inspected-native-text-provider", "exact-current-focused-text-control", "neutral-input" }, true),
                (context, arguments) =>
                {
                    if (arguments.Count != 4 || !arguments.TryGetValue("version", out var version) || Number(version) != 1 ||
                        !arguments.TryGetValue("windowHandle", out var windowValue) || windowValue is not string window ||
                        string.IsNullOrWhiteSpace(window) || window.Length > 200 ||
                        !arguments.TryGetValue("controlHandle", out var controlValue) || controlValue is not string control ||
                        string.IsNullOrWhiteSpace(control) || control.Length > 200 ||
                        !arguments.TryGetValue("text", out var textValue) || textValue is not string text ||
                        !GatewayNativeTextOperation.ValidText(text))
                        throw Invalid("Replacement requires current window/control handles and at most128 printable BMP characters.");
                    context.AddArtifact("native-text-request", "native-input-request", new Dictionary<string, object?>
                        { ["windowHandle"] = window, ["controlHandle"] = control, ["text"] = text });
                    return replaceText(window, control, text, context);
                }, textAvailability ?? availability);
        }
        if (beginScope != null && endScope != null)
        {
            registry.RegisterBuiltIn(new GatewayAutomationDescriptor(BeginScopeName, "1.0",
                "Own the engine pointer until the explicit workflow deadline; never uses desktop input.",
                ScopeSchema("deadlineUtc"), new[] { "inspected-native-input-provider", "neutral-input" }, true),
                (context, arguments) =>
                {
                    string deadlineText = ScopeArgument(arguments, "deadlineUtc");
                    if (!DateTimeOffset.TryParseExact(deadlineText, "O", System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var deadline)) throw Invalid("deadlineUtc must be an absolute round-trip timestamp.");
                    context.RunStep("native-pointer-scope-request", () => { });
                    var receipt = beginScope(deadline, context);
                    context.AddArtifact("native-pointer-scope", "native-input-scope", receipt);
                    try { context.RunStep("native-pointer-scope-admitted", () => { }); }
                    catch
                    {
                        if (receipt.TryGetValue("scopeHandle", out var value) && value is string handle)
                            endScope(handle, context); // Provider retains mandatory cleanup even without polling.
                        throw;
                    }
                    return receipt;
                }, availability);
            registry.RegisterBuiltIn(new GatewayAutomationDescriptor(EndScopeName, "1.0",
                "End the owned pointer workflow and report confirmed restoration, including after cancellation.",
                ScopeSchema("scopeHandle"), new[] { "owned-pointer-scope" }, true),
                (context, arguments) => endScope(ScopeArgument(arguments, "scopeHandle"), context),
                () => GatewayAutomationAvailability.AvailableNow);
        }
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor(CaptureName, "1.0",
                "Capture current native dialog instance handles and GUI/rendered pixel geometry without input.",
                Schema(false),
                new[] { "inspected-native-input-provider" }, false),
            (context, arguments) =>
            {
                ValidateFields(arguments, false);
                return context.RunStep("capture-native-windows", capture);
            }, availability);
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor(ClickName, scopedClick == null ? "1.0" : "2.0",
                "Click an exact captured native dialog using top-left rendered pixel coordinates; never uses desktop input.",
                scopedClick == null ? Schema(true) : new Dictionary<string, object?>
                    { ["oneOf"] = new[] { Schema(true), Schema(true, true) } },
                new[] { "inspected-native-input-provider", "exact-current-window", "neutral-input" }, true),
            (context, arguments) =>
            {
                bool scoped = arguments.TryGetValue("version", out var version) && Number(version) == 2;
                ValidateFields(arguments, true, scoped);
                string? scopeHandle = null;
                if (scoped)
                {
                    if (scopedClick == null || !arguments.TryGetValue("pointerScopeHandle", out var scope) ||
                        scope is not string value || string.IsNullOrWhiteSpace(value) || value.Length > 200)
                        throw Invalid("Version2 requires one explicit pointerScopeHandle.");
                    scopeHandle = value;
                }
                if (!arguments.TryGetValue("windowHandle", out var handleValue) || handleValue is not string handle ||
                    string.IsNullOrWhiteSpace(handle) || handle.Length > 200)
                    throw Invalid("windowHandle must be one current captured identity.");
                if (!arguments.TryGetValue("pixelX", out var xValue) || !arguments.TryGetValue("pixelY", out var yValue))
                    throw Invalid("Both rendered pixel coordinates are required.");
                var x = Number(xValue);
                var y = Number(yValue);
                context.AddArtifact("native-click-request", "native-input-request", new Dictionary<string, object?>
                    { ["windowHandle"] = handle, ["pixelX"] = x, ["pixelY"] = y,
                        ["pointerScopeHandle"] = scopeHandle });
                // Begin takes ownership of deferred cleanup. Do not wrap its return in a cancellation
                // check that could discard the operation before the registry has retained it.
                return scoped ? scopedClick!(handle, x, y, context, scopeHandle!) : click(handle, x, y, context);
            }, availability);
    }

    private static string ScopeArgument(IReadOnlyDictionary<string, object?> arguments, string field)
    {
        if (arguments.Count != 2 || !arguments.TryGetValue("version", out var version) || Number(version) != 1 ||
            !arguments.TryGetValue(field, out var raw) || raw is not string value ||
            string.IsNullOrWhiteSpace(value) || value.Length > 200) throw Invalid("Invalid pointer scope arguments.");
        return value;
    }

    private static IReadOnlyDictionary<string, object?> ScopeSchema(string field) => new Dictionary<string, object?>
    {
        ["type"] = "object", ["additionalProperties"] = false, ["required"] = new[] { "version", field },
        ["properties"] = new Dictionary<string, object?>
        {
            ["version"] = new Dictionary<string, object?> { ["type"] = "integer", ["const"] = 1 },
            [field] = new Dictionary<string, object?> { ["type"] = "string", ["minLength"] = 1, ["maxLength"] = 200 }
        }
    };

    private static void ValidateFields(IReadOnlyDictionary<string, object?> arguments, bool click, bool scoped = false)
    {
        if (!arguments.TryGetValue("version", out var version) || Number(version) != (scoped ? 2 : 1) ||
            arguments.Keys.Any(key => key != "version" && !(scoped && key == "pointerScopeHandle") &&
                (!click || key is not ("windowHandle" or "pixelX" or "pixelY"))))
            throw Invalid("Unknown field, missing version or unsupported version.");
    }

    private static double Number(object? value)
    {
        var result = value switch
        {
            int integer => integer, long integer => integer, decimal number => (double)number,
            float number => number, double number => number,
            _ => throw Invalid("Version and pixel coordinates must be numbers.")
        };
        if (double.IsNaN(result) || double.IsInfinity(result)) throw Invalid("Numbers must be finite.");
        return result;
    }

    private static GatewayAutomationException Invalid(string message) => new("invalid_native_input_arguments", message);

    private static IReadOnlyDictionary<string, object?> Schema(bool click, bool scoped = false)
    {
        var properties = new Dictionary<string, object?>
            { ["version"] = new Dictionary<string, object?> { ["type"] = "integer", ["const"] = scoped ? 2 : 1 } };
        if (click)
        {
            properties["windowHandle"] = new Dictionary<string, object?> { ["type"] = "string" };
            properties["pixelX"] = new Dictionary<string, object?> { ["type"] = "number" };
            properties["pixelY"] = new Dictionary<string, object?> { ["type"] = "number" };
        }
        if (scoped) properties["pointerScopeHandle"] = new Dictionary<string, object?> { ["type"] = "string" };
        return new Dictionary<string, object?>
        {
            ["type"] = "object", ["additionalProperties"] = false,
            ["required"] = properties.Keys.ToArray(), ["properties"] = properties
        };
    }
}
